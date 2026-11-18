using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class AuditIsland2Water
{
    const string Out=".codex/island2-water-system";
    public static object Run()
    {
        Directory.CreateDirectory(Out);var g=UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        using(var w=new StreamWriter(Out+"/terrain.csv")){w.WriteLine("x,z,y,normal");for(int z=700;z<=2500;z+=40)for(int x=-1300;x<=1100;x+=40)if(g.Raycast(new Ray(new Vector3(x,g.bounds.max.y+20,z),Vector3.down),out var h,g.bounds.size.y+40))w.WriteLine($"{x},{z},{h.point.y},{h.normal.y}");}
        var f=g.GetComponent<MeshFilter>();
        return new{g.name,bounds=g.bounds.ToString(),vertices=f.sharedMesh.vertexCount,triangles=f.sharedMesh.triangles.Length/3,readable=f.sharedMesh.isReadable,groundPath=AssetDatabase.GetAssetPath(f.sharedMesh),surfaces=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name=="default").Select(r=>new{r.name,active=r.gameObject.activeInHierarchy,r.transform.position,mat=r.sharedMaterial.name,filter=r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh.name:"none"}).ToArray()};
    }
    public static object Capture()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        var go=new GameObject("Temporary water audit camera");var c=go.AddComponent<Camera>();c.CopyFrom(Camera.main);c.enabled=false;c.farClipPlane=6000;c.fieldOfView=60;
        c.transform.position=new Vector3(-147,4400,1610);c.transform.rotation=Quaternion.Euler(90,0,0);var rt=new RenderTexture(1800,1400,24);var prev=RenderTexture.active;
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1800,1400,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1800,1400),0,0);t.Apply();File.WriteAllBytes(Out+"/island-top.png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}finally{RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}return Out+"/island-top.png";
    }
}
