using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class VerifyWorldShadersV226
{
    const string Root=".codex/whole-game-performance-20261019";
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var owned=new List<UnityEngine.Object>();var reports=new List<PixelResult>();
        var oldWindDir=Shader.GetGlobalVector("_MavisWindDir");var oldWind=Shader.GetGlobalVector("_MavisWindParams");int oldBody=Shader.GetGlobalInt("_MavisFoliageBodyCount");
        var oldBodies=Shader.GetGlobalVectorArray("_MavisFoliageBodies");var oldMotion=Shader.GetGlobalVectorArray("_MavisFoliageMotion");
        try
        {
            var camGo=new GameObject("Temporary shader verification camera");SceneManager.MoveGameObjectToScene(camGo,scene);var cam=camGo.AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.nearClipPlane=.05f;cam.farClipPlane=30000;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);SceneManager.MoveGameObjectToScene(sphere,scene);UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());sphere.transform.localScale=Vector3.one*20000;
            var renderer=sphere.GetComponent<Renderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            var before=MakeShader("Assets/Ocean/ProceduralSky.shader",true,true);var after=MakeShader("Assets/Ocean/ProceduralSky.shader",false,true);owned.Add(before);owned.Add(after);
            var bm=new Material(before);var am=new Material(after);owned.Add(bm);owned.Add(am);
            int k=0;
            foreach(float coverage in new[]{.1f,.55f,.95f})foreach(float detail in new[]{2f,4f,5f})foreach(float elevation in new[]{10f,50f})
            {
                cam.transform.rotation=Quaternion.Euler(-elevation,33,0);
                foreach(var m in new[]{bm,am}){m.SetFloat("_V226TestTime",123.45f+k*31);m.SetFloat("_CloudCoverage",coverage);m.SetFloat("_CloudDetail",detail);m.SetFloat("_NightFactor",k%2);m.SetFloat("_Daylight",1-k%2);m.SetVector("_SunDirection",new Vector4(.32f,.78f,.49f,0));m.SetVector("_MoonDirection",new Vector4(-.32f,-.78f,-.49f,0));}
                renderer.sharedMaterial=bm;var a=Pixels(cam,320,180);renderer.sharedMaterial=am;var b=Pixels(cam,320,180);reports.Add(Compare("sky-"+k++,a,b));
            }
            sphere.SetActive(false);
            before=MakeShader("Assets/Shaders/MavisFoliageWind.shader",true,false);after=MakeShader("Assets/Shaders/MavisFoliageWind.shader",false,false);owned.Add(before);owned.Add(after);bm=new Material(before);am=new Material(after);owned.Add(bm);owned.Add(am);
            var plant=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(plant,scene);UnityEngine.Object.DestroyImmediate(plant.GetComponent<Collider>());plant.transform.localScale=new Vector3(2,4,.1f);plant.transform.position=new Vector3(0,2,0);renderer=plant.GetComponent<Renderer>();
            // Subdivided blades exercise influence away from roots, including the
            // footprint boundary; a primitive cube's end vertices would miss it.
            var grid=new Mesh();owned.Add(grid);var verts=new Vector3[17*17];var uv=new Vector2[verts.Length];var normals=new Vector3[verts.Length];var ix=new int[16*16*6];
            for(int y=0;y<=16;y++)for(int x=0;x<=16;x++){int i=y*17+x;verts[i]=new Vector3(x/4f-2,y/4f,0);uv[i]=new Vector2(x/16f,y/16f);normals[i]=Vector3.back;}
            for(int y=0;y<16;y++)for(int x=0;x<16;x++){int v=y*17+x,i=(y*16+x)*6;ix[i]=v;ix[i+1]=v+17;ix[i+2]=v+1;ix[i+3]=v+1;ix[i+4]=v+17;ix[i+5]=v+18;}
            grid.vertices=verts;grid.normals=normals;grid.uv=uv;grid.triangles=ix;plant.GetComponent<MeshFilter>().sharedMesh=grid;plant.transform.localScale=Vector3.one;plant.transform.position=Vector3.zero;
            var lightGo=new GameObject("Test sun");SceneManager.MoveGameObjectToScene(lightGo,scene);var sun=lightGo.AddComponent<Light>();sun.type=LightType.Directional;lightGo.transform.rotation=Quaternion.Euler(40,20,0);
            cam.transform.position=new Vector3(0,2,-9);cam.transform.rotation=Quaternion.identity;
            Shader.SetGlobalVector("_MavisWindDir",new Vector4(.7f,0,.7f,23.4f));Shader.SetGlobalVector("_MavisWindParams",new Vector4(.5f,1,1,.6f));
            k=0;foreach(int count in new[]{0,1,4,12})foreach(float distance in new[]{0f,1.3999f,1.4001f,100f})
            {
                var positions=new Vector4[12];var motions=new Vector4[12];for(int i=0;i<12;i++){positions[i]=new Vector4(distance+i*.2f,0,0,1.4f);motions[i]=new Vector4(.2f,.3f,.7f,0);}Shader.SetGlobalVectorArray("_MavisFoliageBodies",positions);Shader.SetGlobalVectorArray("_MavisFoliageMotion",motions);Shader.SetGlobalInt("_MavisFoliageBodyCount",count);
                foreach(var m in new[]{bm,am}){m.SetFloat("_MavisWindInvHeight",.25f);m.SetFloat("_MavisWindResponse",1);m.SetColor("_BaseColor",new Color(.3f,.7f,.2f,1));}
                renderer.sharedMaterial=bm;var a=Pixels(cam,320,180);renderer.sharedMaterial=am;var b=Pixels(cam,320,180);reports.Add(Compare("foliage-"+k++,a,b));
            }
            var result=new{passed=reports.All(x=>x.maxChannelError<=1),reports};File.WriteAllText(Path.Combine(Root,"shader-equivalence.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);foreach(var o in owned)if(o)UnityEngine.Object.DestroyImmediate(o);Shader.SetGlobalVector("_MavisWindDir",oldWindDir);Shader.SetGlobalVector("_MavisWindParams",oldWind);Shader.SetGlobalInt("_MavisFoliageBodyCount",oldBody);if(oldBodies.Length>0)Shader.SetGlobalVectorArray("_MavisFoliageBodies",oldBodies);if(oldMotion.Length>0)Shader.SetGlobalVectorArray("_MavisFoliageMotion",oldMotion);
        }
    }
    static Shader MakeShader(string path,bool before,bool sky)
    {
        string source=File.ReadAllText(before?Path.Combine(Root,"before",path):path);
        source=source.Replace(sky?"Shader \"DarkBrine/Procedural Sky\"":"Shader \"Mavis/FoliageWind\"","Shader \"Hidden/V226/"+(sky?"Sky":"Foliage")+(before?"Before":"After")+"\"");
        if(sky)source=source.Replace("_Time.y","_V226TestTime").Replace("float Hash31(float3 p)","float _V226TestTime; float Hash31(float3 p)");
        var shader=ShaderUtil.CreateShaderAsset(source,false);var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();if(errors.Length>0)throw new Exception(string.Join(";",errors.Select(x=>x.message)));return shader;
    }
    static Color32[] Pixels(Camera camera,int w,int h)
    {
        var rt=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var active=RenderTexture.active;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false,true);var target=camera.targetTexture;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();return tex.GetPixels32();}finally{camera.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    sealed class PixelResult {public string name;public int maxChannelError,changedPixels,litPixels;public double meanMaxError;}
    static PixelResult Compare(string name,Color32[] a,Color32[] b){int max=0,changed=0,lit=0;long sum=0;for(int i=0;i<a.Length;i++){int d=Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));max=Math.Max(max,d);sum+=d;if(d>0)changed++;if(a[i].r+a[i].g+a[i].b>0)lit++;}if(lit==0)throw new Exception("Blank reference render: "+name);return new PixelResult{name=name,maxChannelError=max,changedPixels=changed,litPixels=lit,meanMaxError=sum/(double)a.Length};}
}
