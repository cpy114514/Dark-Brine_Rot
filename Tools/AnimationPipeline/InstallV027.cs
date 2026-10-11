using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InstallV027
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string folder="Assets/Resources/Encounter/v027",path="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        if(!File.Exists("ArtSource/Encounter/v027/native-blocking-review.json"))throw new Exception("Native blocking review required");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v027");
        if(AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(folder+"/PreviousEncounterDefinition.asset")==null)
            if(!AssetDatabase.CopyAsset(path,folder+"/PreviousEncounterDefinition.asset"))throw new Exception("Definition backup failed");
        foreach(string suffix in new[]{"Guard","Parry","Slash","Brace"})
        {
            string name="CH1_Sahur"+suffix+"_v027",destination=folder+"/"+name;
            if(!AssetDatabase.IsValidFolder(destination))
            {
                string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);
                if(!string.IsNullOrEmpty(error))throw new Exception(error);
            }
            var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            if(!clip.isHumanMotion || clip.frameRate!=60)throw new Exception("Native Humanoid import invalid");
        }
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);
        for(int i=0;i<definition.beats.Length;i++)
        {
            var beat=definition.beats[i];
            string suffix=beat.action==Story1EncounterAction.Guard || beat.action==Story1EncounterAction.Dive ? "Guard" :
                beat.action==Story1EncounterAction.Parry ? "Parry" :
                beat.action==Story1EncounterAction.Slash || beat.action==Story1EncounterAction.Miss ? "Slash" :
                beat.action==Story1EncounterAction.Brace ? "Brace" : null;
            if(suffix!=null)definition.beats[i].clip="CH1_Sahur"+suffix+"_v027";
        }
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        return new{cinematicBlockingTrial=true,originalGameplayControllerUnchanged=true,duration=definition.duration,fps=definition.fps};
    }
}
