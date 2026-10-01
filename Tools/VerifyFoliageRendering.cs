using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class VerifyFoliageRendering
{
    static Texture2D Capture(Camera camera, RenderTexture rt)
    {
        camera.Render(); var old = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply(); RenderTexture.active=old; return image;
    }
    static float Center(Texture2D image, int low, int high)
    {
        var pixels=image.GetPixels32(); float sum=0,count=0;
        for (int y=low;y<high;y++) for(int x=0;x<image.width;x++)
            if(pixels[y*image.width+x].r>25) {sum+=x;count++;}
        if(count==0) throw new Exception("Test blade absent in rendered band"); return sum/count;
    }
    public static Task<object> Run()
    {
        var shader=Shader.Find("Mavis/FoliageWind"); var material=new Material(shader);
        var mesh=new Mesh(); mesh.vertices=new[]{new Vector3(-.1f,0,0),new Vector3(.1f,0,0),new Vector3(.1f,2,0),new Vector3(-.1f,2,0)};
        mesh.triangles=new[]{0,2,1,0,3,2}; mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up}; mesh.RecalculateNormals();
        var blade=new GameObject("QA wind blade"); blade.layer=31; blade.transform.position=new Vector3(0,10000,0);
        blade.AddComponent<MeshFilter>().sharedMesh=mesh; blade.AddComponent<MeshRenderer>().sharedMaterial=material;
        material.SetFloat("_MavisWindAnchorY",10000); material.SetFloat("_MavisWindInvHeight",.5f); material.SetFloat("_WindBend",1); material.SetFloat("_Cull",0);
        var cameraObject=new GameObject("QA foliage camera"); var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false;
        camera.transform.position=new Vector3(0,10001,-6); camera.orthographic=true; camera.orthographicSize=1.5f; camera.cullingMask=1<<31; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        var rt=new RenderTexture(512,512,24); camera.targetTexture=rt;
        Vector4 wind=Shader.GetGlobalVector("_MavisWindDir"),parameters=Shader.GetGlobalVector("_MavisWindParams"); int bodies=Shader.GetGlobalInt("_MavisFoliageBodyCount");
        Texture2D still=null,sway=null,brush=null;
        try
        {
            Shader.SetGlobalInt("_MavisFoliageBodyCount",0); Shader.SetGlobalVector("_MavisWindParams",new Vector4(0,1,1,.6f));
            Shader.SetGlobalVector("_MavisWindDir",new Vector4(1,0,0,2.1f)); still=Capture(camera,rt);
            Shader.SetGlobalVector("_MavisWindParams",new Vector4(.7f,1,1,.6f)); sway=Capture(camera,rt);
            float rootShift=Mathf.Abs(Center(still,86,98)-Center(sway,86,98)); float tipShift=Mathf.Abs(Center(still,375,405)-Center(sway,375,405));
            if(rootShift>2f || tipShift<8f) throw new Exception("Wind anchoring failed: root="+rootShift+" tip="+tipShift);
            Shader.SetGlobalVector("_MavisWindParams",new Vector4(0,1,1,.6f));
            var positions=new Vector4[12]; var motions=new Vector4[12]; positions[0]=new Vector4(.2f,10000,0,1.4f); motions[0]=new Vector4(1,0,1,0);
            Shader.SetGlobalVectorArray("_MavisFoliageBodies",positions); Shader.SetGlobalVectorArray("_MavisFoliageMotion",motions); Shader.SetGlobalInt("_MavisFoliageBodyCount",1);
            brush=Capture(camera,rt); float brushShift=Mathf.Abs(Center(still,330,355)-Center(brush,330,355));
            if(brushShift<5f) throw new Exception("Actor did not bend rendered blade");
            File.WriteAllBytes(Path.GetFullPath("Tools/FoliageWindRendering.png"),sway.EncodeToPNG());
            var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray(); if(errors.Length>0) throw new Exception(string.Join(";",errors.Select(m=>m.message)));
            return Task.FromResult<object>(new {success=true,rootShiftPixels=rootShift,tipShiftPixels=tipShift,brushShiftPixels=brushShift,shaderErrors=errors.Length});
        }
        finally
        {
            Shader.SetGlobalVector("_MavisWindDir",wind); Shader.SetGlobalVector("_MavisWindParams",parameters); Shader.SetGlobalInt("_MavisFoliageBodyCount",bodies);
            camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(blade); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            if(still) UnityEngine.Object.DestroyImmediate(still); if(sway) UnityEngine.Object.DestroyImmediate(sway); if(brush) UnityEngine.Object.DestroyImmediate(brush);
        }
    }
}
