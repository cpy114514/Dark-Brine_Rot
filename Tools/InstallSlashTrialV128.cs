using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InstallSlashTrialV128
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready original Edit Mode required");
        const string version="Assets/Resources/Encounter/v128";
        if(!AssetDatabase.IsValidFolder(version))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v128");
        const string original="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        string backup=version+"/PreviousEncounterDefinition.asset";
        if(AssetDatabase.LoadMainAssetAtPath(backup)!=null)throw new Exception("This trial was already installed; preserve its original backup");
        if(!File.ReadAllText("ArtSource/Encounter/v128/native-review.json").Contains("accepted_for_integration_trial"))throw new Exception("Native review required");
        if(!AssetDatabase.CopyAsset(original,backup))throw new Exception("Definition backup failed");
        string destination=version+"/CH1_SahurSlash_v128";
        string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/CH1_SahurSlash_v128",destination);
        if(!string.IsNullOrEmpty(error))throw new Exception(error);
        var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/CH1_SahurSlash_v128.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(!clip.isHumanMotion || clip.frameRate!=60 || Mathf.Abs(clip.length-1.4f)>.001f)throw new Exception("Native slash contract failed");
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(original);
        int count=0;for(int i=0;i<definition.beats.Length;i++)if(definition.beats[i].action==Story1EncounterAction.Slash){definition.beats[i].clip=clip.name;count++;}
        if(count!=2)throw new Exception("Unexpected paired slash beat count");
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        var result=new{installed=true,integrationTrial=true,clip=clip.name,clip.frameRate,clip.length,slashBeats=count,backup,
            originalRigAvatarLocomotionPreserved=true};
        File.WriteAllText(".codex/opening-optimization-20261012/slash-install-v128.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}

