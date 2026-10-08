using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallEncounterTraversal
{
    public static object InstallJump()
    {
        const string target="Assets/Resources/Encounter/v003/CH1_SahurBoardJump_v003.anim";
        if(AssetDatabase.LoadAssetAtPath<AnimationClip>(target)!=null)throw new Exception("Jump revision exists.");
        var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/Jumping.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        var clip=UnityEngine.Object.Instantiate(source);clip.name="CH1_SahurBoardJump_v003";
        AssetDatabase.CreateAsset(clip,target);AssetDatabase.SaveAssetIfDirty(clip);
        return new{clip.name,clip.length,clip.frameRate,clip.isHumanMotion,preservedSource=true};
    }
    public static object Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required.");
        const string folder="Assets/Resources/Encounter/v003";
        if(AssetDatabase.IsValidFolder(folder))throw new Exception("Revision already installed.");
        AssetDatabase.CreateFolder("Assets/Resources/Encounter","v003");
        var error=AssetDatabase.MoveAsset("Assets/AnimationStaging/CH1_SharkBreach_v003",folder+"/CH1_SharkBreach_v003");
        if(error!="")throw new Exception(error);
        const string previous="Assets/Resources/Encounter/v001";
        AssetDatabase.CopyAsset(previous+"/EncounterShark.controller",folder+"/EncounterShark.controller");
        AssetDatabase.CopyAsset(previous+"/EncounterShark.prefab",folder+"/EncounterShark.prefab");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/EncounterShark.controller");
        var clip=AssetDatabase.LoadAllAssetsAtPath(folder+"/CH1_SharkBreach_v003/CH1_SharkBreach_v003.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        var state=controller.layers[0].stateMachine.AddState("Breach");state.motion=clip;state.writeDefaultValues=true;
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/EncounterShark.prefab"));
        var animator=instance.GetComponentInChildren<Animator>();animator.enabled=false;animator.runtimeAnimatorController=controller;
        var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();Bounds bounds=skin.localBounds;
        for(int i=0;i<=32;i++){clip.SampleAnimation(animator.gameObject,i*.1f);skin.BakeMesh(mesh);foreach(var v in mesh.vertices)bounds.Encapsulate(v);}
        bounds.Expand(.1f);skin.localBounds=bounds;controller.animationClips.Single(c=>c.name=="Swim_Cruise").SampleAnimation(animator.gameObject,0);animator.enabled=true;
        var prefab=PrefabUtility.SaveAsPrefabAsset(instance,folder+"/EncounterShark.prefab");
        AssetDatabase.CopyAsset("Assets/Resources/SharkAnimation/TralaleroAnimations.asset",folder+"/PreviousAnimationSet.asset");
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
        set.rigPrefab=prefab;EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(mesh);
        return new{installed=true,clip=clip.name,clip.length,clip.frameRate,originalRigPreserved=true,previousControllerPreserved=true};
    }
}
