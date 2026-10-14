using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Native integration trial. Does not touch walk, run, roll, rig or Avatar.</summary>
public static class InstallOpeningTrialV047
{
    static void Folder(string version)
    {
        string path="Assets/Resources/Encounter/"+version;
        if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder("Assets/Resources/Encounter",version);
    }
    static AnimationClip Clip(string name)
    {
        string version=name.Substring(name.LastIndexOf('_')+1);Folder(version);
        string destination="Assets/Resources/Encounter/"+version+"/"+name;
        if(!AssetDatabase.IsValidFolder(destination))
        {
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(clip.frameRate!=60 || clip.isHumanMotion!=name.Contains("Sahur"))throw new Exception("Unexpected native clip rig or FPS: "+name);
        return clip;
    }
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready edit mode required");
        Folder("v047");Folder("v045");Folder("v040");
        const string definitionPath="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        const string controllerPath="Assets/Resources/Encounter/v006/EncounterShark.controller";
        const string setPath="Assets/Resources/SharkAnimation/TralaleroAnimations.asset";
        foreach(var row in new[]{(definitionPath,"Assets/Resources/Encounter/v047/PreviousEncounterDefinition.asset"),(controllerPath,"Assets/Resources/Encounter/v045/PreviousEncounterShark.controller"),(setPath,"Assets/Resources/Encounter/v045/PreviousAnimationSet.asset")})
            if(AssetDatabase.LoadMainAssetAtPath(row.Item2)==null && !AssetDatabase.CopyAsset(row.Item1,row.Item2))throw new Exception("Backup failed: "+row.Item1);
        foreach(string name in new[]{"CH1_SahurGuard_v047","CH1_SahurBrace_v047","CH1_SahurBoardHop_v047","CH1_SahurParry_v040","CH1_SahurSlash_v040","CH1_SahurMiss_v040"})Clip(name);
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(definitionPath);
        for(int i=0;i<definition.beats.Length;i++)
        {
            var action=definition.beats[i].action;
            string name=action==Story1EncounterAction.Guard || action==Story1EncounterAction.Dive ? "CH1_SahurGuard_v047" : action==Story1EncounterAction.Brace ? "CH1_SahurBrace_v047" : action==Story1EncounterAction.Parry ? "CH1_SahurParry_v040" : action==Story1EncounterAction.Slash ? "CH1_SahurSlash_v040" : action==Story1EncounterAction.Miss ? "CH1_SahurMiss_v040" : action==Story1EncounterAction.Hop ? "CH1_SahurBoardHop_v047" : null;
            if(name!=null)definition.beats[i].clip=name;
        }
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        foreach(var row in new[]{("Tail_Strike","CH1_SharkTail_v045"),("Ship_Smash","CH1_SharkShipSmash_v045"),("Bite_Lunge","CH1_SharkBite_v045"),("Breach","CH1_SharkBreach_v045")})
        {
            var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==row.Item1).state;state.motion=Clip(row.Item2);EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(setPath);var actor=UnityEngine.Object.Instantiate(set.rigPrefab);
        try
        {
            var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var marker=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");animator.Rebind();animator.Update(0);
            if(!TralaleroSwimAnimator.FindVisibleTailContact(skin,marker,out var bone,out var local))throw new Exception("Missing actual tail contact");
            animator.runtimeAnimatorController=controller;animator.Play("Tail_Strike",0,.65f/1.5f);animator.Update(0);set.tailStrikeContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            animator.Play("Ship_Smash",0,.65f/1.5f);animator.Update(0);set.shipSmashContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);}
        return new{installedForIntegrationTrial=true,duration=definition.duration,fps=definition.fps,legacyWalkRunRollRetained=true};
    }
}
