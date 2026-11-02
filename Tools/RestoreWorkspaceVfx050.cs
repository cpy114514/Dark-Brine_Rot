using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RestoreWorkspaceVfx050
{
    public static object RestorePreferences()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        var prefs=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/vfx050/prefs-before.json"));
        string key="DarkBrine.HealingPacks.Campaign";
        if((bool)prefs["exists"])PlayerPrefs.SetString(key,(string)prefs["campaign"]);else PlayerPrefs.DeleteKey(key);
        key="DarkBrine.HealingPacks.Remaining."+(string)prefs["campaign"];
        if((bool)prefs["countExists"])PlayerPrefs.SetInt(key,(int)prefs["count"]);else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        typeof(Mavis.HealingPackSupply).GetMethod("ResetCache",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
        var result=new{campaign=PlayerPrefs.GetString("DarkBrine.HealingPacks.Campaign","legacy"),count=PlayerPrefs.GetInt(key,3),restored=true};
        File.WriteAllText(".codex/vfx050/preferences-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Restore()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string root=".codex/vfx050";
        Directory.CreateDirectory(root+"/workspace-recovery");
        // Preserve any unsaved in-memory scene state before restoring the user's
        // original workspace; saveAsCopy does not overwrite original scene files.
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);if(!scene.isDirty)continue;
            string target=Path.GetFullPath(root+"/workspace-recovery/"+scene.name+".unity");
            if(File.Exists(target))throw new Exception("Preserve prior recovery copy: "+target);
            if(!EditorSceneManager.SaveScene(scene,target,true)||!File.Exists(target))throw new Exception("Could not preserve dirty scene "+scene.path);
        }
        var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(root+"/scene-setup.json"));
        // Do not rewrite saves: require byte equality, otherwise leave them alone.
        var saveRoot=Application.persistentDataPath;
        bool saveEqual=Directory.GetFiles(root+"/save-before").All(p=>File.Exists(Path.Combine(saveRoot,Path.GetFileName(p)))&&File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(saveRoot,Path.GetFileName(p)))));
        EditorSceneManager.RestoreSceneManagerSetup(setup);EditorApplication.isPaused=false;
        var result=new{restored=true,saveEqual,scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>new{s.path,s.isActive}),inputs=UnityEngine.InputSystem.InputSystem.devices.Select(d=>new{d.name,d.enabled}),campaign=PlayerPrefs.GetString("DarkBrine.HealingPacks.Campaign","legacy")};
        File.WriteAllText(root+"/workspace-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
