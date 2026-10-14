using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InstallParryTrialV090
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready original Edit Mode required");
        const string version="Assets/Resources/Encounter/v090";
        if(!AssetDatabase.IsValidFolder(version))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v090");
        const string original="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        string backup=version+"/PreviousEncounterDefinition.asset";
        if(AssetDatabase.LoadMainAssetAtPath(backup)!=null)throw new Exception("This trial was already installed; preserve its original backup");
        if(!File.ReadAllText("ArtSource/Encounter/v090/native-review.json").Contains("accepted_for_integration_trial"))throw new Exception("Native review required");
        if(!AssetDatabase.CopyAsset(original,backup))throw new Exception("Definition backup failed");
        string destination=version+"/CH1_SahurParry_v090";
        string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/CH1_SahurParry_v090",destination);
        if(!string.IsNullOrEmpty(error))throw new Exception(error);
        var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/CH1_SahurParry_v090.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(!clip.isHumanMotion || clip.frameRate!=60 || Mathf.Abs(clip.length-1.1f)>.001f)throw new Exception("Native parry contract failed");
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(original);
        int count=0;for(int i=0;i<definition.beats.Length;i++)if(definition.beats[i].action==Story1EncounterAction.Parry){definition.beats[i].clip=clip.name;count++;}
        if(count!=1)throw new Exception("Unexpected paired parry beat count");
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        var result=new{installed=true,integrationTrial=true,clip=clip.name,clip.frameRate,clip.length,parryBeats=count,backup,
            originalRigAvatarLocomotionPreserved=true};
        File.WriteAllText(".codex/opening-optimization-20261012/parry-install-v090.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
