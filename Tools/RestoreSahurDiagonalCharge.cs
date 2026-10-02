using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Retain the downloaded shoulder/elbow strike, with a stable one-handed grip.
public static class RestoreSahurDiagonalCharge
{
    const string PrefabPath="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const string ControllerPath="Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    const string SourcePath="Assets/Game/Prefabs/Characters/Sahur/Animations/Source/SwordAndShieldSlash_ThreeHit.fbx";
    const string ClipPath="Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurDiagonalChargedStrike.anim";
    public const float HoldPhase=.47f;
    static bool RightMuscle(string name)=>name.StartsWith("Right Shoulder") || name.StartsWith("Right Arm") ||
        name.StartsWith("Right Forearm") || name.StartsWith("Right Hand") || new[]{"Right Thumb","Right Index","Right Middle","Right Ring","Right Little"}.Any(name.StartsWith);

    public static object Author()
    {
        if(!Application.isPlaying)throw new Exception("Author on the initialized avatar in Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),new Vector3(0,500,0),Quaternion.identity);
        var temp=new AnimatorController();HumanPoseHandler handler=null;
        try{
            root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;var animator=attack.animator;
            animator.GetComponent<SahurRootMotionRelay>().enabled=false;animator.GetComponent<SahurCombatGuardIK>().enabled=false;
            var live=(AnimatorController)animator.runtimeAnimatorController;
            temp.AddLayer("Base Layer");temp.layers[0].stateMachine.AddState("Idle").motion=((BlendTree)live.layers[0].stateMachine.states.Single(s=>s.state.name=="Locomotion").state.motion).children[0].motion;
            temp.AddLayer("Right arm");var layers=temp.layers;layers[1].avatarMask=live.layers[1].avatarMask;layers[1].defaultWeight=1;temp.layers=layers;
            var source=AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().Single(c=>c.name=="SahurSwordCombo1");
            temp.layers[1].stateMachine.AddState("Source").motion=source;
            animator.runtimeAnimatorController=temp;animator.Rebind();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            handler=new HumanPoseHandler(animator.avatar,animator.transform);
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            animator.Play("Idle",0,0);animator.Play("Source",1,0);animator.Update(0);
            var cap=(CapsuleCollider)attack.stickHitbox;
            Vector3 axis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
            Vector3 shaft=cap.transform.TransformDirection(axis);
            if(Vector3.Dot(shaft,cap.transform.TransformPoint(cap.center)-hand.position)<0)shaft=-shaft;
            Vector3 handShaft=Quaternion.Inverse(hand.rotation)*shaft;
            var indices=Enumerable.Range(0,HumanTrait.MuscleCount).Where(i=>RightMuscle(HumanTrait.MuscleName[i])).ToArray();
            var keys=indices.ToDictionary(i=>i,i=>new List<Keyframe>());
            animator.Play("Source",1,HoldPhase);animator.Update(0);
            Vector3 readyDirection=root.transform.TransformDirection(new Vector3(.20f,.74f,-.65f).normalized);
            Quaternion heldRotation=Quaternion.FromToRotation(hand.rotation*handShaft,readyDirection)*hand.rotation;
            hand.rotation=heldRotation;HumanPose heldPose=new HumanPose();handler.GetHumanPose(ref heldPose);
            var wristNames=new[]{"Right Forearm Twist In-Out","Right Hand Down-Up","Right Hand In-Out"};
            var wristIndices=wristNames.Select(n=>Array.IndexOf(HumanTrait.MuscleName,n)).ToArray();
            animator.Play("Source",1,.82f);animator.Update(0);
            Vector3 followThrough=root.transform.TransformDirection(new Vector3(-.25f,-.80f,.54f).normalized);
            hand.rotation=Quaternion.FromToRotation(hand.rotation*handShaft,followThrough)*hand.rotation;
            HumanPose endPose=new HumanPose();handler.GetHumanPose(ref endPose);
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,ClipPath);}
            clip.ClearCurves();clip.name="SahurDiagonalChargedStrike";clip.frameRate=60;
            const int count=181;
            for(int k=0;k<count;k++){
                float phase=k/(float)(count-1);
                animator.Play("Idle",0,0);animator.Play("Source",1,phase);animator.Update(0);
                HumanPose pose=new HumanPose();handler.GetHumanPose(ref pose);
                // Lift around the outside of the right shoulder, rather than rotating
                // the club through the face on the way to the held pose.
                Vector3 direction=Vector3.Slerp(new Vector3(.70f,.28f,.65f).normalized,
                    new Vector3(.20f,.74f,-.65f).normalized,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.20f,phase)));
                if(phase<=HoldPhase){
                    Vector3 desired=root.transform.TransformDirection(direction);
                    Quaternion corrected=Quaternion.FromToRotation(hand.rotation*handShaft,desired)*hand.rotation;
                    hand.rotation=Quaternion.Slerp(hand.rotation,corrected,Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.06f,phase)));
                    handler.GetHumanPose(ref pose);
                }else{
                    // Interpolate in muscle space to avoid the wrist decomposition
                    // jumping by 360 degrees as the hand passes an Euler singularity.
                    float turn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(HoldPhase,.82f,phase));
                    for(int w=0;w<wristIndices.Length;w++)pose.muscles[wristIndices[w]]=Mathf.Lerp(heldPose.muscles[wristIndices[w]],endPose.muscles[wristIndices[w]],turn);
                }
                foreach(int i in indices)keys[i].Add(new Keyframe(phase*source.length,pose.muscles[i]));
            }
            foreach(int i in indices){
                var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),HumanTrait.MuscleName[i]);
                var curve=new AnimationCurve(keys[i].ToArray());
                for(int k=0;k<curve.length;k++){
                    AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                }
                // The source is retargeted from a sword grip. Keep Sahur's fingers
                // closed around his club throughout anticipation and the strike.
                if(HumanTrait.MuscleName[i].Contains("Stretched")){
                    string name=HumanTrait.MuscleName[i];float grip=name.StartsWith("Right Thumb")?-.25f:name.Contains(" 2 ")?-.65f:-.45f;
                    curve=AnimationCurve.Linear(0,grip,source.length,grip);
                }
                AnimationUtility.SetEditorCurve(clip,binding,curve);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlend=false;
            settings.startTime=0;settings.stopTime=source.length;settings.keepOriginalOrientation=true;
            settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
            return new{source=SourcePath,sourceClip=source.name,output=ClipPath,clip.length,clip.humanMotion,stableGrip=true,HoldPhase};
        }
        finally{handler?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(temp);}
    }
    public static object Install()
    {
        if(Application.isPlaying)throw new Exception("Install outside Play Mode.");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);if(!clip||!clip.humanMotion)throw new Exception("Author the restored strike first.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        foreach(var layer in controller.layers)foreach(var child in layer.stateMachine.states){
            var state=child.state;if(state.name!="Heavy Attack"&&state.name!="Charge Windup")continue;
            Undo.RecordObject(state,"Restore downloaded diagonal strike");state.motion=clip;state.speed=1.05f;state.writeDefaultValues=false;
            state.speedParameterActive=false;state.timeParameterActive=state.name=="Charge Windup";state.timeParameter="ChargePhase";
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{Configure(root.GetComponent<Mavis.SahurAttack>());PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        const string demo="Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoCombatDemo.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(demo);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(demo,OpenSceneMode.Additive);
        try{
            foreach(var a in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Mavis.SahurAttack>(true))){Undo.RecordObject(a,"Restore diagonal charge");Configure(a);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        return new{clip=clip.name,windup=.42f,fullCharge=1.8f,HoldPhase,hitWindow=".62–.84"};
    }
    static void Configure(Mavis.SahurAttack attack){
        attack.chargeClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);attack.chargeWindupTime=.42f;
        attack.fullChargeTime=1.8f;attack.maxChargePosePhase=HoldPhase;attack.heavySwingWindowStart=.62f;attack.heavySwingWindowEnd=.84f;
        EditorUtility.SetDirty(attack);
    }
}
