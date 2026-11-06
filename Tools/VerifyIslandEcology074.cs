using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Mavis;
using Object=UnityEngine.Object;

public static class VerifyIslandEcology074
{
    const string W=".codex/island-ecology074";
    static void Json(string n,object v)=>File.WriteAllText(W+"/"+n+".json",Newtonsoft.Json.JsonConvert.SerializeObject(v,Newtonsoft.Json.Formatting.Indented));
    public static object FinalState()
    {
        var ecology=Object.FindFirstObjectByType<IslandEcologyRenderer>();var grass=Object.FindFirstObjectByType<NaturalGrassScatter>();
        var scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i)).ToArray();
        var errors=ShaderUtil.GetShaderMessages(Shader.Find("Mavis/FoliageWind")).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
        var result=new{passed=!Application.isPlaying&&scenes.Length==5&&scenes.All(s=>!s.isDirty)&&ecology&&ecology.IsReady&&ecology.InstanceCount==7998&&grass&&grass.IsReady&&grass.InstanceCount==185170&&errors.Length==0,playing=Application.isPlaying,scenes=scenes.Select(s=>new{s.path,s.isDirty}).ToArray(),ecology=ecology?ecology.InstanceCount:0,grass=grass?grass.InstanceCount:0,shaderErrors=errors,saveOverride=typeof(GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null)};Json("final-state",result);return result;
    }
    public static object Materials()
    {
        var mats=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene.path.Contains("First Island")).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name=="Mavis/FoliageWind").Distinct().Select(m=>new{m.name,path=AssetDatabase.GetAssetPath(m),color=m.GetColor("_BaseColor").ToString(),texture=AssetDatabase.GetAssetPath(m.GetTexture("_MainTex")),normal=m.HasProperty("_BotanicalNormal")?AssetDatabase.GetAssetPath(m.GetTexture("_BotanicalNormal")):null}).ToArray();Json("existing-materials",mats);return mats;
    }
    public static object UnlitTrees()
    {
        return Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).Contains("Environment/Trees/")).Select(f=>new{f,r=f.GetComponent<MeshRenderer>()}).Where(x=>x.r&&x.r.sharedMaterials.Any(m=>m&&m.shader.name!="Mavis/FoliageWind")).Select(x=>new{x.f.name,mesh=x.f.sharedMesh.name,p=x.f.transform.position.ToString(),materials=x.r.sharedMaterials.Select(m=>m?new{name=m.name,path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,texture=m.mainTexture?AssetDatabase.GetAssetPath(m.mainTexture):null,color=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():""}:null).ToArray()}).Take(30).ToArray();
    }
    public static object Meadow()
    {
        var scatter=Object.FindFirstObjectByType<NaturalGrassScatter>();var d=scatter.data;
        var ground=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled).GetComponent<MeshCollider>();
        var mat=ground.GetComponent<Renderer>().sharedMaterial;float sand=mat.GetFloat("_SeaLevel")+mat.GetFloat("_BeachHeight");
        int count=0,roots=0,floating=0,beach=0,missing=0,outside=0;float max=float.MinValue,min=float.MaxValue;
        var rootVertices=d.meshes.Select(m=>m.vertices.Where((v,i)=>i%7<2).ToArray()).ToArray();
        foreach(var cell in d.cells)for(int i=0;i<cell.positions.Length;i++)
        {
            var p=cell.positions[i];var pos=new Vector3(p.x,p.y,p.z);count++;
            if(!ground.Raycast(new Ray(new Vector3(p.x,180,p.z),Vector3.down),out var h,400))missing++;else if(h.point.y<sand)beach++;
            if(count%47!=0)continue;
            float scale=.86f+Mathf.Repeat(p.w*.071f,1)*.28f;var q=Quaternion.FromToRotation(Vector3.up,cell.normals[i])*Quaternion.Euler(0,p.w,0);
            foreach(var root in rootVertices[i%d.meshes.Length])
            {
                var wp=pos+q*root*scale;roots++;if(!cell.bounds.Contains(wp))outside++;
                if(!ground.Raycast(new Ray(new Vector3(wp.x,180,wp.z),Vector3.down),out var hit,400)){missing++;continue;}
                float gap=wp.y-hit.point.y;max=Mathf.Max(max,gap);min=Mathf.Min(min,gap);if(gap>.035f)floating++;
            }
        }
        var result=new{passed=floating==0&&beach==0&&missing==0&&outside==0,instances=count,sampledRoots=roots,floating,beach,missing,outside,maxRootGap=max,minRootGap=min,ready=scatter.IsReady,nearTriangles=d.meshes[0].triangles.Length/3,mediumTriangles=d.mediumMeshes[0].triangles.Length/3,farTriangles=d.farMeshes[0].triangles.Length/3};Json("meadow-validation",result);return result;
    }
    public static object WindAndInteraction()
    {
        if(Application.isPlaying)throw new Exception("Stopped fixture required");
        var wind=Shader.GetGlobalVector("_MavisWindDir");var parameters=Shader.GetGlobalVector("_MavisWindParams");
        int bodyCount=Shader.GetGlobalInt("_MavisFoliageBodyCount");var bodies=Shader.GetGlobalVectorArray("_MavisFoliageBodies");var motion=Shader.GetGlobalVectorArray("_MavisFoliageMotion");
        var go=new GameObject("Ecology wind GPU fixture",typeof(MeshFilter),typeof(MeshRenderer));go.layer=30;go.hideFlags=HideFlags.HideAndDontSave;
        go.transform.position=new Vector3(5000,1000,5000);var mesh=new Mesh();
        mesh.vertices=new[]{new Vector3(-.09f,0,0),new Vector3(.09f,0,0),new Vector3(-.07f,1,0),new Vector3(.07f,1,0),new Vector3(-.035f,1.7f,0),new Vector3(.035f,1.7f,0),new Vector3(0,2.2f,0)};
        mesh.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5,4,6,5};mesh.colors=mesh.vertices.Select(v=>new Color(1,1,1,v.y/2.2f)).ToArray();mesh.uv=mesh.vertices.Select(v=>new Vector2(.5f,v.y/2.2f)).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        var mat=new Material(Shader.Find("Mavis/FoliageWind")){enableInstancing=true};mat.SetFloat("_UseBladeHeight",1);mat.SetFloat("_WindBend",1);mat.SetFloat("_PlayerPushStrength",1);mat.SetFloat("_Cull",0);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        var cg=new GameObject("GPU root pin camera",typeof(Camera));cg.hideFlags=HideFlags.HideAndDontSave;var c=cg.GetComponent<Camera>();c.enabled=false;c.cullingMask=1<<30;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;c.orthographic=true;c.orthographicSize=1.5f;c.nearClipPlane=.1f;c.farClipPlane=20;
        c.transform.SetPositionAndRotation(go.transform.position+new Vector3(0,1.1f,-6),Quaternion.identity);
        var rt=RenderTexture.GetTemporary(256,256,24);var active=RenderTexture.active;c.targetTexture=rt;
        Color32[] Capture(string name)
        {
            c.Render();RenderTexture.active=rt;var img=new Texture2D(256,256,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,256,256),0,0);img.Apply();File.WriteAllBytes(W+"/"+name+".png",img.EncodeToPNG());var pixels=img.GetPixels32();Object.DestroyImmediate(img);return pixels;
        }
        int Diff(Color32[] a,Color32[] b,int minY,int maxY)
        {int count=0;for(int y=minY;y<maxY;y++)for(int x=0;x<256;x++){int i=y*256+x;if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>24)count++;}return count;}
        int RootSilhouette(Color32[] a,Color32[] b,int rootY)
        {int count=0;for(int y=rootY-2;y<rootY+3;y++)for(int x=0;x<256;x++){int i=y*256+x;if((a[i].r+a[i].g+a[i].b>40)!=(b[i].r+b[i].g+b[i].b>40))count++;}return count;}
        try
        {
            Shader.SetGlobalInt("_MavisFoliageBodyCount",0);Shader.SetGlobalVector("_MavisWindDir",new Vector4(1,0,0,0));Shader.SetGlobalVector("_MavisWindParams",Vector4.zero);
            var still=Capture("gpu-still");Shader.SetGlobalVector("_MavisWindParams",new Vector4(.8f,.4f,1,.65f));Shader.SetGlobalVector("_MavisWindDir",new Vector4(1,0,0,3.1f));var sway=Capture("gpu-wind");
            Shader.SetGlobalVector("_MavisWindParams",Vector4.zero);var p=go.transform.position;var bs=new Vector4[12];var ms=new Vector4[12];bs[0]=new Vector4(p.x-.25f,p.y,p.z,1.4f);ms[0]=new Vector4(1,0,1,0);Shader.SetGlobalVectorArray("_MavisFoliageBodies",bs);Shader.SetGlobalVectorArray("_MavisFoliageMotion",ms);Shader.SetGlobalInt("_MavisFoliageBodyCount",1);var pushed=Capture("gpu-interaction");
            int rootY=Mathf.RoundToInt(c.WorldToScreenPoint(p).y),windPixels=Diff(still,sway,rootY+8,256),interactionPixels=Diff(still,pushed,rootY+8,256),rootWind=RootSilhouette(still,sway,rootY),rootPush=RootSilhouette(still,pushed,rootY);
            var errors=ShaderUtil.GetShaderMessages(mat.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
            var result=new{passed=windPixels>100&&interactionPixels>100&&rootWind<10&&rootPush<10&&errors.Length==0,windPixels,interactionPixels,rootWind,rootPush,rootY,shaderErrors=errors,forward=mat.FindPass("ForwardLit"),depth=mat.FindPass("DepthOnly"),shadow=mat.FindPass("ShadowCaster")};Json("gpu-wind-interaction",result);return result;
        }
        finally
        {
            Shader.SetGlobalVector("_MavisWindDir",wind);Shader.SetGlobalVector("_MavisWindParams",parameters);Shader.SetGlobalInt("_MavisFoliageBodyCount",bodyCount);if(bodies.Length>0)Shader.SetGlobalVectorArray("_MavisFoliageBodies",bodies);if(motion.Length>0)Shader.SetGlobalVectorArray("_MavisFoliageMotion",motion);
            RenderTexture.active=active;c.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(go);Object.DestroyImmediate(cg);Object.DestroyImmediate(mesh);Object.DestroyImmediate(mat);
        }
    }
    public static object Gallery(int group=0)
    {
        var data=AssetDatabase.LoadAssetAtPath<IslandEcologyData>("Assets/Generated/IslandEcology074/Ecology.asset");
        var root=new GameObject("Ecology specimen review");root.hideFlags=HideFlags.HideAndDontSave;var origin=new Vector3(5000,1200,5000);
        var cg=new GameObject("Ecology specimen camera",typeof(Camera));cg.hideFlags=HideFlags.HideAndDontSave;var c=cg.GetComponent<Camera>();c.enabled=false;c.cullingMask=1<<30;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.17f,.19f,.20f);c.orthographic=true;c.orthographicSize=group==1?5.5f:3.8f;c.farClipPlane=100;c.nearClipPlane=.1f;
        if(group==3)c.orthographicSize=4.3f;
        c.transform.SetPositionAndRotation(origin+new Vector3(0,group==1?5:3.8f,-18),Quaternion.Euler(7,0,0));
        var rt=RenderTexture.GetTemporary(1600,800,24);var active=RenderTexture.active;
        try
        {
            int[] ids=group==0?new[]{2,3,4,5,6}:group==1?new[]{7,8}:group==2?new[]{9,10,11,12}:new[]{16,17,18,19};
            for(int i=0;i<ids.Length;i++)
            {
                var s=data.species[ids[i]];var go=new GameObject(s.name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=30;go.transform.SetParent(root.transform);go.transform.position=origin+new Vector3((i-(ids.Length-1)*.5f)*3.1f,0,0);go.GetComponent<MeshFilter>().sharedMesh=s.near;go.GetComponent<MeshRenderer>().sharedMaterial=s.material;
            }
            c.targetTexture=rt;c.Render();RenderTexture.active=rt;var img=new Texture2D(1600,800,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,800),0,0);img.Apply();string file=W+"/specimens-"+group+".png";File.WriteAllBytes(file,img.EncodeToPNG());Object.DestroyImmediate(img);return new{file,names=ids.Select(i=>data.species[i].name).ToArray()};
        }
        finally {RenderTexture.active=active;c.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(root);Object.DestroyImmediate(cg);}
    }
}
