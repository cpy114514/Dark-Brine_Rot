using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallSahurGetUp
{
    public const string Output="Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurShoreGetUp.anim";
    public static object Build()
    {
        if(Application.isPlaying) throw new Exception("Stop Play before installing the get-up clip.");
        var native=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx").OfType<AnimationClip>().Single(c=>c.name=="Armature|LayToIdle");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output);
        if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Output);}
        EditorUtility.CopySerialized(native,clip);clip.name="SahurShoreGetUp";
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=settings.loopBlend=false;
        settings.loopBlendOrientation=false;settings.loopBlendPositionY=settings.loopBlendPositionXZ=true;
        settings.keepOriginalOrientation=false;settings.keepOriginalPositionY=false;settings.keepOriginalPositionXZ=false;settings.heightFromFeet=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        return new {clip=Output,native=native.name,clip.length,clip.isHumanMotion};
    }
    public static object Install()
    {
        var built=Build();
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        if(!controller.parameters.Any(p=>p.name=="ArrivalGetUpPhase"))controller.AddParameter("ArrivalGetUpPhase",AnimatorControllerParameterType.Float);
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.FirstOrDefault(s=>s.state.name=="Shore Get Up").state??machine.AddState("Shore Get Up");
        state.motion=clip;state.speed=1;state.speedParameterActive=false;state.timeParameter="ArrivalGetUpPhase";state.timeParameterActive=true;
        foreach(var t in state.transitions) state.RemoveTransition(t);
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(state);AssetDatabase.SaveAssetIfDirty(controller);
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Gameplay.unity");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Gameplay.unity",OpenSceneMode.Additive);
        var arrival=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FirstIslandArrival>(true)).Single();
        var serialized=new SerializedObject(arrival);serialized.FindProperty("getUpClip").objectReferenceValue=clip;serialized.FindProperty("getUpSeconds").floatValue=2.3f;serialized.ApplyModifiedProperties();
        var root=UnityEngine.Object.Instantiate(arrival.player.gameObject,new Vector3(0,500,0),Quaternion.identity);
        var baked=new Mesh();
        try{
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            root.GetComponent<CharacterController>().enabled=false;
            var original=root.GetComponent<Animator>();var animator=root.transform.Find("Pbr Sahur Visual").GetComponent<Animator>();
            animator.avatar=original.avatar;animator.runtimeAnimatorController=controller;original.enabled=false;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;animator.Rebind();
            float Bottom(){float bottom=float.PositiveInfinity;
                foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
                    renderer.BakeMesh(baked,false);
                    foreach(var vertex in baked.vertices)bottom=Mathf.Min(bottom,renderer.transform.TransformPoint(vertex).y-root.transform.position.y);
                }
                return bottom;
            }
            var offsets=new AnimationCurve();
            for(int k=0;k<=60;k++){
                float phase=k/60f;animator.SetFloat("ArrivalGetUpPhase",phase);animator.Play("Shore Get Up",0,phase);animator.Update(0);
                offsets.AddKey(new Keyframe(phase,Bottom()));
            }
            animator.Play("Locomotion",0,0);animator.Update(0);
            serialized.Update();serialized.FindProperty("getUpBottomOffset").animationCurveValue=offsets;
            serialized.FindProperty("standingBottomOffset").floatValue=Bottom();serialized.ApplyModifiedProperties();
        }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(baked);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return built;
    }
}
