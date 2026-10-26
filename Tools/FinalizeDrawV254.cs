using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
using Mavis;

public static class FinalizeDrawV254
{
    public static object RestoreWorkspace()
    {
        if(Application.isPlaying || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Preserve open work before restoration");
        const string root=".codex/draw-retrieve-v254/";
        var before=JObject.Parse(File.ReadAllText(root+"workspace-before.json"));
        var paths=before["scenes"].Select(x=>(string)x).ToArray();
        for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
        var environment=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if(environment.IsValid()&&environment.isLoaded)SceneManager.SetActiveScene(environment);
        HealingPackSupply.RestoreCampaign((string)before["campaign"],(int)before["packs"]);
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        File.Copy(root+"human-save-before.json",save,true);
        var result=new {stopped=!Application.isPlaying,saveRestored=File.ReadAllBytes(save).SequenceEqual(File.ReadAllBytes(root+"human-save-before.json")),campaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new {path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
        File.WriteAllText(root+"final-editor.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
