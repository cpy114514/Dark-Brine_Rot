using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class Island2DecorationAudit
{
    public const string Out=".codex/island2-decoration";
    public static Collider Ground() => UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
    public static object Run()
    {
        Directory.CreateDirectory(Out);
        var g=Ground();var b=g.bounds;
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();
        var data=new {ground=new{g.name,center=b.center.ToString(),size=b.size.ToString()},roots=roots.Select(r=>new{r.name,renderers=r.GetComponentsInChildren<Renderer>(true).Length,colliders=r.GetComponentsInChildren<Collider>(true).Length}),
            gameplay=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Contains("Fairy")||t.name.Contains("Candy")||t.name.Contains("Player")||t.name.Contains("Spawn")).Select(t=>new{t.name,p=t.position.ToString()}).ToArray()};
        File.WriteAllText(Out+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
        using(var w=new StreamWriter(Out+"/heights.csv")){w.WriteLine("x,z,y,normalY");for(float z=1510;z<=2240;z+=15)for(float x=-410;x<=230;x+=15)if(g.Raycast(new Ray(new Vector3(x,b.max.y+10,z),Vector3.down),out var h,b.size.y+20))w.WriteLine($"{x},{z},{h.point.y},{h.normal.y}");}
        return data;
    }
    public static object Capture(string tag,float x,float z,float height,float tx,float tz,float targetHeight=0)
    {
        Directory.CreateDirectory(Out);var g=Ground();float gy=g.bounds.max.y;
        Vector3 At(float a,float c){return g.Raycast(new Ray(new Vector3(a,gy+10,c),Vector3.down),out var h,g.bounds.size.y+30)?h.point:new Vector3(a,2460,c);}
        var go=new GameObject("Decoration audit temporary camera"){hideFlags=HideFlags.HideAndDontSave};
        var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.enabled=false;c.fieldOfView=58;c.farClipPlane=2400;c.nearClipPlane=.3f;
        go.transform.position=At(x,z)+Vector3.up*height;go.transform.LookAt(At(tx,tz)+Vector3.up*targetHeight);
        var rt=new RenderTexture(1600,1000,24);var prev=RenderTexture.active;
        try {c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1600,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,1000),0,0);t.Apply();File.WriteAllBytes(Out+"/"+tag+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
        finally{RenderTexture.active=prev;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        return Out+"/"+tag+".png";
    }
    public static object Before(){Run();Capture("before-overview",-105,1860,510,-105,1860);Capture("before-arrival",-142,1630,8,-70,1715);Capture("before-east",135,1900,18,70,1940);return "captured";}
    public static object After(){Capture("after-overview",-105,1860,510,-105,1860);Capture("after-arrival",-142,1630,8,-70,1715);Capture("after-east",135,1900,18,70,1940);Capture("after-river",-39,1881,5,-24,1857,1);Capture("after-spring",23,1978,7,61,1980,2);Capture("after-wreck",-227,1945,5,-246,1970,3);Capture("after-valley",-117,1810,8,-35,1850,2);Capture("after-coast",990,1600,8,1015,1610,0);return "captured";}
}
