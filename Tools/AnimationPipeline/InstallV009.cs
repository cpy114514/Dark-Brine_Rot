using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallV009
{
    public static object Install()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        const string folder="Assets/Resources/Encounter/v009",name="PLAYER_Climb_v009";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v009");
        if(!AssetDatabase.IsValidFolder(folder+"/"+name)){string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,folder+"/"+name);if(error!="")throw new Exception(error);}
        const string path="Assets/Resources/Encounter/v006/SahurGameplay.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/PreviousSahurGameplay.controller")==null)AssetDatabase.CopyAsset(path,folder+"/PreviousSahurGameplay.controller");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Wreck Climb").state;
        state.motion=AssetDatabase.LoadAllAssetsAtPath(folder+"/"+name+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return new{climbInstalled=true};
    }
}
