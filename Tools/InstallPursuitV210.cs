using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InstallPursuitV210
{
    public static object Start()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string report=".codex/chase-entry-20261014/native-clips-v3/review.json";
        var review=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(report));
        var hero=review["rows"].Where(r=>(string)r["name"]=="PLAYER_PursuitSwim_v210").ToArray();
        if(hero.Length!=7 || hero.Any(r=>(float)r["markers"][0]["position"][2]<.1f))throw new Exception("Native swim heading must face +Z");
        var result=new System.Collections.Generic.List<object>();
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Encounter/v210"))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v210");
        foreach(string name in new[]{"PLAYER_PursuitSwim_v210","CH1_SharkPursuit_v210"})
        {
            string source="Assets/AnimationStaging/"+name,destination="Assets/Resources/Encounter/v210/"+name;
            if(AssetDatabase.IsValidFolder(destination))throw new Exception("Protect existing versioned resource");
            var clip=AssetDatabase.LoadAllAssetsAtPath(source+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            if(!clip.isLooping || clip.frameRate!=60)throw new Exception("Native clip timing changed");
            if(!AssetDatabase.CopyAsset(source,destination))throw new Exception("Staged resource copy failed");
            result.Add(new{name,clip.length,clip.frameRate,destination});
        }
        File.WriteAllText(".codex/chase-entry-20261014/install-clips.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new{clips=result,oldPlayerControllerUnchanged=true,originalAvatarRetained=true,stage="FIRST PASS integration"};
    }
}
