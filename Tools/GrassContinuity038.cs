using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class GrassContinuity038
{
    const string Root=".codex/grass038";
    public static object Inspect()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var manager=Resources.FindObjectsOfTypeAll<Mavis.WorldPerformanceManager>().FirstOrDefault(w=>w.gameObject.scene.isLoaded);
        var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.GetLODs().SelectMany(l=>l.renderers).Any(r=>r&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("VerifiedGrass"))).ToArray();
        var registry=manager?(System.Collections.IDictionary)manager.GetType().GetField("grassLods",flags).GetValue(manager):null;
        var report=new{ready=manager&&manager.IsReady,groups=groups.Length,owned=registry?.Count,
            representatives=groups.Take(3).Select(g=>new{g.name,position=g.transform.position.ToString(),g.size,g.fadeMode,
            levels=g.GetLODs().Select(l=>new{height=l.screenRelativeTransitionHeight,width=l.fadeTransitionWidth,renderers=l.renderers.Select(r=>new{r.name,r.enabled,off=(r as MeshRenderer).forceRenderingOff,
            mesh=r.GetComponent<MeshFilter>().sharedMesh.name,triangles=r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3,
            materials=r.sharedMaterials.Select(m=>new{m.name,shader=m.shader.name,m.enableInstancing}).ToArray()}).ToArray()}).ToArray()}).ToArray()};
        Directory.CreateDirectory(Root);File.WriteAllText(Root+"/inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    public static object Camera()
    {
        var c=new GameObject("Grass038 camera",typeof(Camera)).GetComponent<Camera>();c.tag="MainCamera";c.fieldOfView=60;c.farClipPlane=1000;
        c.transform.SetPositionAndRotation(new Vector3(288,29,346),Quaternion.Euler(8,-25,0));return new{created=true};
    }
    public static object Preview(bool constant=true)
    {
        if(!Application.isPlaying)throw new Exception("Play only");
        var manager=Resources.FindObjectsOfTypeAll<Mavis.WorldPerformanceManager>().First(w=>w.gameObject.scene.isLoaded);
        
        var registry=(System.Collections.IDictionary)manager.GetType().GetField("grassLods",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(manager);
        int changed=0;
        foreach(LODGroup g in registry.Keys)
        {
            var levels=g.GetLODs();if(levels.Length!=1)continue;
            var level=levels[0];level.screenRelativeTransitionHeight=constant?0:.0463295f;level.fadeTransitionWidth=constant?0:.0161934f;
            g.SetLODs(new[]{level});g.fadeMode=constant?LODFadeMode.None:LODFadeMode.CrossFade;changed++;
        }
        return new{changed,constant};
    }
    public static object State()=>new{playing=Application.isPlaying,paused=EditorApplication.isPaused,
        scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).path,SceneManager.GetSceneAt(i).isDirty}).ToArray(),
        inputs=InputSystem.devices.Select(d=>new{d.name,d.enabled}).ToArray()};
    public static object StoppedAudit()
    {
        if(Application.isPlaying)throw new Exception("Stopped audit only");
        var groups=Resources.FindObjectsOfTypeAll<LODGroup>().Where(g=>g.gameObject.scene.isLoaded&&g.GetLODs().SelectMany(l=>l.renderers).Any(r=>r&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh&&r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("VerifiedGrass"))).ToArray();
        int wrong=groups.Count(g=>g.GetLODs().Length!=3||g.fadeMode!=LODFadeMode.None||g.GetLODs().Any(l=>l.fadeTransitionWidth!=0));
        var report=new{passed=groups.Length>=11298&&wrong==0&&Shader.GetGlobalVector("_MavisGrassFadeRange")==Vector4.zero,groups=groups.Length,wrong,fade=Shader.GetGlobalVector("_MavisGrassFadeRange").ToString(),state=State()};
        File.WriteAllText(Root+"/stopped-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    public static object Sandbox(string scenePath=null)
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve current Editor work");
        Directory.CreateDirectory(Root);
        File.WriteAllText(Root+"/scene-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
        string source=Application.persistentDataPath;
        Directory.CreateDirectory(Root+"/save-before");
        foreach(string p in Directory.GetFiles(source,"dark-brine-rot-save*"))File.Copy(p,Root+"/save-before/"+Path.GetFileName(p),true);
        File.WriteAllText(Root+"/save-location.txt",source);
        const string campaignKey="DarkBrine.HealingPacks.Campaign";
        string campaign=PlayerPrefs.GetString(campaignKey,"legacy");
        string countKey="DarkBrine.HealingPacks.Remaining."+campaign;
        File.WriteAllText(Root+"/prefs-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            exists=PlayerPrefs.HasKey(campaignKey),campaign,countExists=PlayerPrefs.HasKey(countKey),count=PlayerPrefs.GetInt(countKey,3)}));
        if(string.IsNullOrEmpty(scenePath))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        else EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        if(scenePath=="Assets/Scenes/First Island/Environment.unity")EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
        EditorApplication.isPlaying=true;
        return new{starting=true};
    }
    public static object Repeat()
    {
        if(Application.isPlaying||!File.Exists(Root+"/scene-setup.json")||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Stopped clean saved workspace required");
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
        EditorApplication.isPlaying=true;return new{starting=true};
    }
    public static object Restore()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        string source=File.ReadAllText(Root+"/save-location.txt");
        bool unchanged=true;
        foreach(string p in Directory.GetFiles(Root+"/save-before"))
        {
            string target=Path.Combine(source,Path.GetFileName(p));
            if(File.Exists(target)&&File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(p)))continue;
            unchanged=false;
            if(File.Exists(target))File.Copy(target,Root+"/test-"+DateTime.UtcNow.Ticks+"-"+Path.GetFileName(p));
            File.Copy(p,target,true);
        }
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/scene-setup.json")));
        EditorApplication.isPaused=false;
        var prefs=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/prefs-before.json"));
        if((bool)prefs["exists"])PlayerPrefs.SetString("DarkBrine.HealingPacks.Campaign",(string)prefs["campaign"]);
        else PlayerPrefs.DeleteKey("DarkBrine.HealingPacks.Campaign");
        string countKey="DarkBrine.HealingPacks.Remaining."+(string)prefs["campaign"];
        if((bool)prefs["countExists"])PlayerPrefs.SetInt(countKey,(int)prefs["count"]);else PlayerPrefs.DeleteKey(countKey);
        PlayerPrefs.Save();
        typeof(Mavis.HealingPackSupply).GetMethod("ResetCache",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
        return new{saveUnchanged=unchanged,restored=true,scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).ToArray(),
            inputs=InputSystem.devices.Select(d=>new{d.name,d.enabled}).ToArray()};
    }
}

