using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class GroundedVfx039
{
    const string Root=".codex/vfx039";
    public static object Configure()
    {
        if(Application.isPlaying)throw new Exception("Configure stopped asset only");
        var style=AssetDatabase.LoadAssetAtPath<Mavis.BrineVfxStyle>("Assets/Resources/Combat/BrineVfxStyle.asset");
        style.body=new Color(.18f,.165f,.14f);style.edge=new Color(.72f,.62f,.44f);style.contact=new Color(.55f,.34f,.13f);
        style.fireCore=new Color(1.15f,.65f,.23f);style.fireBody=new Color(.55f,.19f,.025f);style.smoke=new Color(.095f,.085f,.072f);
        EditorUtility.SetDirty(style);AssetDatabase.SaveAssetIfDirty(style);
        var result=new{configured=true,shaderErrors=new[]{style.surface,style.stone,style.blast}.Select(m=>new{m.shader.name,messages=ShaderUtil.GetShaderMessages(m.shader).Select(e=>e.message).ToArray()}).ToArray()};
        File.WriteAllText(Root+"/configure.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object State()=>new{playing=Application.isPlaying,paused=EditorApplication.isPaused,
        scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).path,SceneManager.GetSceneAt(i).isDirty}).ToArray(),
        inputs=InputSystem.devices.Select(d=>new{d.name,d.enabled}).ToArray()};
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


