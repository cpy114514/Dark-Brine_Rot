using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InstallOpeningTrialV051
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready edit mode required");
        foreach(string version in new[]{"v050","v051"})
            if(!AssetDatabase.IsValidFolder("Assets/Resources/Encounter/"+version))AssetDatabase.CreateFolder("Assets/Resources/Encounter",version);
        const string path="Assets/Resources/Encounter/v006/EncounterDefinition.asset",backup="Assets/Resources/Encounter/v051/PreviousEncounterDefinition.asset";
        if(AssetDatabase.LoadMainAssetAtPath(backup)==null && !AssetDatabase.CopyAsset(path,backup))throw new Exception("Backup failed");
        foreach(string name in new[]{"CH1_SahurGuard_v050","CH1_SahurSlash_v051"})
        {
            string version=name.Substring(name.LastIndexOf('_')+1),destination="Assets/Resources/Encounter/"+version+"/"+name;
            if(!AssetDatabase.IsValidFolder(destination))
            {
                string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);if(!string.IsNullOrEmpty(error))throw new Exception(error);
            }
            var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            if(!clip.isHumanMotion || clip.frameRate!=60)throw new Exception("Expected retained Humanoid source at60 FPS");
        }
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);
        for(int i=0;i<definition.beats.Length;i++)
        {
            var action=definition.beats[i].action;
            if(action==Story1EncounterAction.Guard || action==Story1EncounterAction.Dive)definition.beats[i].clip="CH1_SahurGuard_v050";
            else if(action==Story1EncounterAction.Slash)definition.beats[i].clip="CH1_SahurSlash_v051";
        }
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        return new{trialOnly=true,definition.duration,definition.fps,originalWalkRunRollRetained=true};
    }
}
