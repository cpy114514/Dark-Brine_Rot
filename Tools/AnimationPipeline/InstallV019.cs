using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallV019
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string folder="Assets/Resources/Encounter/v019",controllerPath="Assets/Resources/Encounter/v006/EncounterShark.controller",setPath="Assets/Resources/SharkAnimation/TralaleroAnimations.asset";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v019");
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/PreviousEncounterShark.controller")==null)AssetDatabase.CopyAsset(controllerPath,folder+"/PreviousEncounterShark.controller");
        if(AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(folder+"/PreviousAnimationSet.asset")==null)AssetDatabase.CopyAsset(setPath,folder+"/PreviousAnimationSet.asset");
        AnimationClip Clip(string name)
        {
            string destination=folder+"/"+name;
            if(!AssetDatabase.IsValidFolder(destination)){string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);if(!string.IsNullOrEmpty(error))throw new Exception(error);}
            return AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        }
        var tail=Clip("CH1_SharkTail_v019");var ship=Clip("CH1_SharkShipSmash_v019");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        foreach(var row in controller.layers[0].stateMachine.states)
        {
            if(row.state.name=="Tail_Strike"){row.state.motion=tail;EditorUtility.SetDirty(row.state);}
            if(row.state.name=="Ship_Smash"){row.state.motion=ship;EditorUtility.SetDirty(row.state);}
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(setPath);var actor=UnityEngine.Object.Instantiate(set.rigPrefab);
        try
        {
            var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var marker=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");animator.Rebind();animator.Update(0);
            if(!TralaleroSwimAnimator.FindVisibleTailContact(skin,marker,out var bone,out var local))throw new Exception("No actual caudal point");
            tail.SampleAnimation(animator.gameObject,.65f);set.tailStrikeContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            ship.SampleAnimation(animator.gameObject,.65f);set.shipSmashContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
            var result=new{tail=tail.name,ship=ship.name,seconds=tail.length,fps=tail.frameRate,tailContact=set.tailStrikeContact.ToString(),shipContact=set.shipSmashContact.ToString(),originalRigAndCaudalSkinRetained=true};
            Directory.CreateDirectory(".codex/encounter-v019");File.WriteAllText(".codex/encounter-v019/import-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);}
    }
}
