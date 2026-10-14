using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallBiteTrialV069
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        var validation=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/opening-optimization-20261012/bite-import-v069.json"));
        if(!(bool)validation["pass"])throw new Exception("Original native rig validation required");
        const string path="Assets/Resources/Encounter/v006/EncounterShark.controller",parent="Assets/Resources/Encounter/v069";
        if(!AssetDatabase.IsValidFolder(parent))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v069");
        string backup=parent+"/PreviousEncounterShark.controller";
        if(AssetDatabase.LoadMainAssetAtPath(backup)!=null)throw new Exception("Already installed: never overwrite the backup");
        if(!AssetDatabase.CopyAsset(path,backup))throw new Exception("Controller backup failed");
        string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/CH1_SharkBite_v069",parent+"/CH1_SharkBite_v069");
        if(!string.IsNullOrEmpty(error))throw new Exception(error);
        var clip=AssetDatabase.LoadAllAssetsAtPath(parent+"/CH1_SharkBite_v069/CH1_SharkBite_v069.fbx").OfType<AnimationClip>().Single(c=>c.name=="CH1_SharkBite_v069");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Bite_Lunge").state;
        state.motion=clip;EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        return new{integrationTrial=true,backup,clip=clip.name,clip.length,clip.frameRate,originalRigPreserved=true};
    }
}
