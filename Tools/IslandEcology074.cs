using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
using Object = UnityEngine.Object;

public static class IslandEcology074
{
    public const string Work = ".codex/island-ecology074";
    public const string ScenePath = "Assets/Scenes/First Island/Environment.unity";
    public static void Json(string name, object value) { Directory.CreateDirectory(Work); File.WriteAllText(Work+"/"+name+".json", Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented)); }
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
    public static object Capture(string label="before",int view=0)
    {
        var ground=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled).GetComponent<MeshCollider>();
        var positions=new[]{new Vector3(285,0,437),new Vector3(130,0,646),new Vector3(328,0,343),new Vector3(253,0,496)};
        var aims=new[]{new Vector3(245,0,470),new Vector3(99,0,675),new Vector3(277,0,397),new Vector3(211,0,535)};
        Vector3 Surface(Vector3 p) { if(ground.Raycast(new Ray(p+Vector3.up*250,Vector3.down),out var h,500))p=h.point;return p; }
        var p=Surface(positions[view])+Vector3.up*(view==3?18:4.5f);var aim=Surface(aims[view])+Vector3.up*1.5f;
        var go=new GameObject("Ecology074 review camera",typeof(Camera));go.hideFlags=HideFlags.HideAndDontSave;
        var c=go.GetComponent<Camera>();if(Camera.main)c.CopyFrom(Camera.main);c.enabled=false;c.fieldOfView=57;c.farClipPlane=1500;
        c.transform.SetPositionAndRotation(p,Quaternion.LookRotation(aim-p));
        var rt=RenderTexture.GetTemporary(1920,1080,24);var old=RenderTexture.active;Texture2D img=null;
        try {c.targetTexture=rt;foreach(var s in Object.FindObjectsByType<NaturalGrassScatter>(FindObjectsSortMode.None))s.Submit(c);
            c.Render();RenderTexture.active=rt;img=new Texture2D(1920,1080,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1920,1080),0,0);img.Apply();
            string file=Work+"/"+label+"-"+view+".png";File.WriteAllBytes(file,img.EncodeToPNG());return new{file,p=p.ToString(),aim=aim.ToString()};}
        finally {RenderTexture.active=old;c.targetTexture=null;RenderTexture.ReleaseTemporary(rt);if(img)Object.DestroyImmediate(img);Object.DestroyImmediate(go);}
    }
    public static object Audit()
    {
        var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
        var rs=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var report=new {
            playing=Application.isPlaying, scenes=scenes.Select(s=>new{s.path,s.isDirty,roots=s.GetRootGameObjects().Select(g=>new{g.name,g.activeSelf,renderers=g.GetComponentsInChildren<Renderer>(true).Length,position=g.transform.position.ToString()}).ToArray()}).ToArray(),
            islands=Object.FindObjectsByType<ProceduralIsland>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(i=>new{path=PathOf(i.transform),i.isActiveAndEnabled,scene=i.gameObject.scene.path,bounds=i.GetComponent<Collider>().bounds.ToString(),position=i.transform.position.ToString(),i.shorelineRadius,i.beachHeight,i.seed}).ToArray(),
            grass=Object.FindObjectsByType<NaturalGrassScatter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(s=>new{s.name,s.enabled,data=AssetDatabase.GetAssetPath(s.data),s.InstanceCount,cells=s.data?s.data.cells.Length:0}).ToArray(),
            geometry=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy).GroupBy(r=> {var f=r.GetComponent<MeshFilter>();return f&&f.sharedMesh?AssetDatabase.GetAssetPath(f.sharedMesh)+" :: "+f.sharedMesh.name:"none";}).Select(g=>new{mesh=g.Key,count=g.Count(),example=PathOf(g.First().transform)}).OrderByDescending(g=>g.count).ToArray(),
            protection=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>new[]{"path","road","boss","trigger","quest","supply","landmark","spawn","camp","bridge"}.Any(w=>t.name.ToLowerInvariant().Contains(w))).Select(t=>new{path=PathOf(t),p=t.position.ToString(),scene=t.gameObject.scene.path}).Take(400).ToArray(),
            colliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).GroupBy(c=>c.gameObject.scene.path).Select(g=>new{scene=g.Key,count=g.Count()}).ToArray(),
            prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Game/Prefabs/Environment"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("Candy")&&!p.Contains("Marshmallow")&&!p.Contains("Chocolate")).ToArray(),
            camera=Camera.main?new{position=Camera.main.transform.position.ToString(),rotation=Camera.main.transform.eulerAngles.ToString()}:null
        }; Json("audit-before",report);return new{scenes=report.scenes.Select(s=>new{s.path,s.isDirty}),report=Work+"/audit-before.json",renderers=rs.Length};
    }
}
