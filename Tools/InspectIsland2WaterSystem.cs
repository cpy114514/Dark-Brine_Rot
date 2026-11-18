using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class InspectIsland2WaterSystem
{
    const string Out=".codex/island2-water-system";
    static Collider Ground()=>UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
    public static Vector3 Floor(float x,float z)
    {var g=Ground();return g.Raycast(new Ray(new Vector3(x,2800,z),Vector3.down),out var h,1900)?h.point:new Vector3(x,2200,z);}
    static void Shot(string name,Vector3 pos,Vector3 target,int width=1600,int height=1000)
    {
        var go=new GameObject("Water system review camera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.enabled=false;c.nearClipPlane=.3f;c.farClipPlane=4500;c.fieldOfView=58;c.transform.position=pos;c.transform.LookAt(target);
        var rt=new RenderTexture(width,height,24);var prev=RenderTexture.active;
        try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var t=new Texture2D(width,height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();File.WriteAllBytes(Out+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
        finally{RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static object Capture()
    {
        Directory.CreateDirectory(Out);
        Shot("lake",new Vector3(-110,2555,1985),new Vector3(0,2523,2070));
        Shot("lake-shore",Floor(-100,2075)+Vector3.up*6,new Vector3(20,2523,2065));
        Shot("outlet",Floor(65,2017)+Vector3.up*7,new Vector3(26,2521,2010));
        Shot("river",Floor(-39,1881)+Vector3.up*6,Floor(-24,1857)+Vector3.up*1);
        Shot("gorge",Floor(-260,2020)+Vector3.up*20,Floor(-300,2040));
        Shot("cloudfall",new Vector3(-415,2440,2590),new Vector3(-250,2290,2460));
        Shot("cloudfall-profile",new Vector3(-470,2410,2420),new Vector3(-250,2260,2490));
        Shot("cloudfall-lip",Floor(-290,2330)+Vector3.up*10,new Vector3(-255,2375,2362));
        Shot("system-overview",new Vector3(-100,3120,2130),new Vector3(-110,2460,2050));
        return "Captured lake, outlet, river, gorge, cloudfall and connected system";
    }
    public static object FloatingConfections()
    {
        var root=GameObject.Find("Island 2 - Environment Decoration");var holders=root.GetComponentsInChildren<Transform>(true).Where(t=>t.Find("Confection")&&t.gameObject.activeInHierarchy);
        var data=holders.Select(t=>new{t.name,p=t.position.ToString(),gap=t.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y)-Floor(t.position.x,t.position.z).y}).Where(a=>a.gap>1.5f).ToArray();
        File.WriteAllText(Out+"/floating-confections.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;
    }
    public static object Validate()
    {
        var root=GameObject.Find("Lake to Cloudfall - connected chocolate water system");var meshes=root.GetComponentsInChildren<MeshFilter>();var shader=Shader.Find("Dark Brine/Second Island/Viscous Chocolate");
        var river=meshes.First(f=>f.name.StartsWith("Continuous chocolate river"));var v=river.sharedMesh.vertices;int rows=v.Length/21;int submerged=0;float worst=0;int uphill=0;
        for(int i=0;i<rows;i++){var p=v[i*21+10];float d=Floor(p.x,p.z).y-p.y;if(d> .01f)submerged++;worst=Mathf.Max(worst,d);if(i>0&&p.y>v[(i-1)*21+10].y+.01f)uphill++;}
        var fall=meshes.First(f=>f.name.StartsWith("The Cloudfall"));var fv=fall.sharedMesh.vertices;int fallInside=0;float minClear=10000;
        for(int i=1;i<fv.Length/33;i++){var p=fv[i*33+16];var g=Ground();if(g.Raycast(new Ray(new Vector3(p.x,2800,p.z),Vector3.down),out var h,1900)){float clear=p.y-h.point.y;minClear=Mathf.Min(minClear,clear);if(clear<-.15f)fallInside++;}}
        var terrain=Ground().GetComponent<MeshFilter>();var old=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Game/Prefabs/Environment/CottonCandyIsland/Models/CottonCandy_Island01_HighRes.obj").vertices;var shaped=terrain.sharedMesh.vertices;float protectedBossDelta=0,hopDelta=0;
        for(int i=0;i<old.Length;i++){var p=terrain.transform.TransformPoint(old[i]);float delta=(shaped[i]-old[i]).magnitude*200;
            if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(-185,1850))<51||Vector2.Distance(new Vector2(p.x,p.z),new Vector2(-105,1755))<48)protectedBossDelta=Mathf.Max(protectedBossDelta,delta);
            if(Mathf.Abs(p.x+147)<10&&p.z>1940&&p.z<2215)hopDelta=Mathf.Max(hopDelta,delta);}
        var result=new{shaderSupported=shader.isSupported,shaderErrors=ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray(),rows,riverCenterHiddenByTerrain=submerged,worstSubmersion=worst,uphillRows=uphill,fallInsideTerrainSamples=fallInside,minimumFallTerrainClearance=minClear,meshCount=meshes.Length,
            normals=meshes.Select(f=>new{f.name,minY=f.sharedMesh.normals.Min(n=>n.y),meanY=f.sharedMesh.normals.Average(n=>n.y)}),triangles=meshes.Sum(f=>f.sharedMesh.triangles.Length/3),newColliders=root.GetComponentsInChildren<Collider>().Length,
            bossFloors=new[]{Floor(-185,1850).ToString(),Floor(-105,1755).ToString()},protectedBossMeshDelta=protectedBossDelta,centralHopRouteMeshDelta=hopDelta,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
        File.WriteAllText(Out+"/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
