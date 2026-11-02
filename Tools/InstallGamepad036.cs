using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class InstallGamepad036
{
    const string Root=".codex/gamepad036";
    public static object Bindings()
    {
        if(Application.isPlaying)throw new Exception("Stop Play before importing bindings");
        const string path="Assets/InputSystem_Actions.inputactions";
        Directory.CreateDirectory(Root);
        if(!File.Exists(Root+"/InputSystem_Actions.before.inputactions"))File.Copy(path,Root+"/InputSystem_Actions.before.inputactions");
        var asset=InputActionAsset.FromJson(File.ReadAllText(path));
        var map=asset.FindActionMap("Player",true);
        for(int i=0;i<map.bindings.Count;i++)
        {
            var binding=map.bindings[i];if(!binding.path.StartsWith("<Gamepad>"))continue;
            if(binding.action=="Attack")map.ChangeBinding(i).WithPath("<Gamepad>/rightTrigger");
            if(binding.action=="Interact")map.ChangeBinding(i).WithPath("<Gamepad>/buttonWest");
        }
        File.WriteAllText(path,asset.ToJson());UnityEngine.Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        return new{asset=path,attack="RT",interact="X"};
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
        EditorApplication.isPlaying=true;
        return new{starting=true};
    }
    public static object Opening(string label,bool controller)
    {
        string output=Root+"/"+label+"/review.json";
        if(Directory.Exists(Path.GetDirectoryName(output)))throw new Exception("Preserve previous take");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText("Temp/opening-review-request.txt",output);
        File.WriteAllText("Temp/opening-review-helm.flag","true");
        if(controller)File.WriteAllText("Temp/opening-review-pad.flag","true");
        else if(File.Exists("Temp/opening-review-pad.flag"))File.Delete("Temp/opening-review-pad.flag");
        return Sandbox("Assets/Scenes/First story/Story1.unity");
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
