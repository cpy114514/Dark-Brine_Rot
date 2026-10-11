using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InstallV036
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string folder="Assets/Resources/Encounter/v036",name="CH1_SahurSlash_v036",path="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        if(!File.Exists("ArtSource/Encounter/v036/native-blocking-review.json"))throw new Exception("Native blocking review required");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v036");
        if(AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(folder+"/PreviousEncounterDefinition.asset")==null)
            if(!AssetDatabase.CopyAsset(path,folder+"/PreviousEncounterDefinition.asset"))throw new Exception("Definition backup failed");
        if(!AssetDatabase.IsValidFolder(folder+"/"+name))
        {
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,folder+"/"+name);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var clip=AssetDatabase.LoadAllAssetsAtPath(folder+"/"+name+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(!clip.isHumanMotion || clip.frameRate!=60 || Mathf.Abs(clip.length-1.4f)>.001f)throw new Exception("Native clip import invalid");
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);
        for(int i=0;i<definition.beats.Length;i++)
            if(definition.beats[i].action==Story1EncounterAction.Slash || definition.beats[i].action==Story1EncounterAction.Miss)definition.beats[i].clip=name;
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        return new{cinematicBlockingTrial=true,clip=name,originalGameplayControllerUnchanged=true,duration=definition.duration,fps=definition.fps};
    }
}

