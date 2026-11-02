using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class Guidance040
{
    const string Root=".codex/guidance040";
    public static object Configure()
    {
        if(Application.isPlaying)throw new Exception("Configure stopped assets only");
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Guidance"))AssetDatabase.CreateFolder("Assets/Resources","Guidance");
        var shader=Shader.Find("DarkBrine/Quest Guiding Thread");if(!shader)throw new Exception("Import guiding shader first");
        const string path="Assets/Resources/Guidance/QuestThread.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader){name="Gold and jade main quest thread"};AssetDatabase.CreateAsset(material,path);}
        AssetDatabase.SaveAssetIfDirty(material);
        return new{material=path,shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,m.line}).ToArray()};
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



