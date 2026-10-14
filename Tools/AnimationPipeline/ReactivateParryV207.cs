using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ReactivateParryV207
{
    public static object Start()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string path="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        const string backup=".codex/animation-continuation-20261014/EncounterDefinition-before-v207.asset";
        const string source="Assets/Resources/Encounter/v198/CH1_SahurParry_v198/CH1_SahurParry_v198.fbx";
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);
        int i=Array.FindIndex(definition.beats,b=>b.action==Story1EncounterAction.Parry);
        if(i<0 || definition.beats[i].clip!="CH1_SahurParry_v171" || File.Exists(backup))throw new Exception("Protect existing revision and backup");
        var clip=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(Mathf.Abs(clip.length-1.1f)>.001f || clip.frameRate!=60 || clip.isLooping)throw new Exception("Native clip timing mismatch");
        File.Copy(path,backup);
        var beat=definition.beats[i];beat.clip="CH1_SahurParry_v198";definition.beats[i]=beat;
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        return new{reactivatedForNativeTrial=true,finalApproval=false,clip=beat.clip,historyAndOriginalAvatarRetained=true};
    }
}
