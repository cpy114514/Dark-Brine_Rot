using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorkspaceOpening054
{
    public static object Restore()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string root=".codex/opening054";
        string location=File.ReadAllText(root+"/save-location.txt");bool unchanged=true;
        foreach(string p in Directory.GetFiles(root+"/save-before"))
        {
            string target=Path.Combine(location,Path.GetFileName(p));
            if(File.Exists(target)&&File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(target)))continue;
            unchanged=false;if(File.Exists(target))File.Copy(target,root+"/test-"+DateTime.UtcNow.Ticks+"-"+Path.GetFileName(p));File.Copy(p,target,true);
        }
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve dirty scene before restoring workspace");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(root+"/scene-setup.json")));
        var prefs=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(root+"/prefs.json"));
        if((bool)prefs["exists"])PlayerPrefs.SetString("DarkBrine.HealingPacks.Campaign",(string)prefs["campaign"]);else PlayerPrefs.DeleteKey("DarkBrine.HealingPacks.Campaign");
        string key="DarkBrine.HealingPacks.Remaining."+(string)prefs["campaign"];
        if((bool)prefs["countExists"])PlayerPrefs.SetInt(key,(int)prefs["count"]);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();EditorApplication.isPaused=false;
        typeof(Mavis.HealingPackSupply).GetMethod("ResetCache",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
        var result=new{restored=true,saveUnchanged=unchanged,inputs=UnityEngine.InputSystem.InputSystem.devices.Select(d=>new{d.name,d.enabled}),scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path)};
        File.WriteAllText(root+"/restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Snapshot()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        const string root=".codex/opening054";
        if(File.Exists(root+"/scene-setup.json"))throw new Exception("Preserve snapshot");
        Directory.CreateDirectory(root+"/save-before");
        File.WriteAllText(root+"/scene-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
        File.WriteAllText(root+"/save-location.txt",Application.persistentDataPath);
        foreach(string p in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save*"))File.Copy(p,root+"/save-before/"+Path.GetFileName(p));
        string campaign=PlayerPrefs.GetString("DarkBrine.HealingPacks.Campaign","legacy"),key="DarkBrine.HealingPacks.Remaining."+campaign;
        File.WriteAllText(root+"/prefs.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{campaign,exists=PlayerPrefs.HasKey("DarkBrine.HealingPacks.Campaign"),countExists=PlayerPrefs.HasKey(key),count=PlayerPrefs.GetInt(key,3)}));
        return new{captured=true,scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).ToArray()};
    }
}
