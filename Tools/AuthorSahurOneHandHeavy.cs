using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuthorSahurOneHandHeavy
{
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const string ClipPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurOneHandHeavy.anim";
    const string ControllerPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    const float Duration = 1.12f;
    public const float HoldPhase = .45f / Duration;

    public static object Author()
    {
        if (!Application.isPlaying) throw new Exception("Author on the initialized gameplay avatar in Play Mode.");
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), new Vector3(0,500,0), Quaternion.identity);
        HumanPoseHandler handler = null;
        try
        {
            root.GetComponent<ThirdPersonPlayerController>().enabled = false;
            root.GetComponent<CharacterController>().enabled = false;
            var attack = root.GetComponent<Mavis.SahurAttack>(); attack.enabled = false;
            var animator = attack.animator;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var relay = animator.GetComponent<SahurRootMotionRelay>(); if (relay) relay.enabled = false;
            var guard = animator.GetComponent<SahurCombatGuardIK>(); if (guard) guard.enabled = false;
            animator.SetFloat("Speed",0); animator.SetLayerWeight(1,0);
            animator.Play("Locomotion",0,0); animator.Update(0);
            handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var rigTransforms = animator.GetComponentsInChildren<Transform>(true);
            var restPositions = rigTransforms.Select(t=>t.localPosition).ToArray();
            var restRotations = rigTransforms.Select(t=>t.localRotation).ToArray();
            var upper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Vector3 shoulder = root.transform.InverseTransformPoint(upper.position);
            float reach = (Vector3.Distance(upper.position,lower.position) + Vector3.Distance(lower.position,hand.position)) / root.transform.lossyScale.x;
            var hitbox = (CapsuleCollider)attack.stickHitbox;
            Vector3 axis = hitbox.direction == 0 ? Vector3.right : hitbox.direction == 1 ? Vector3.up : Vector3.forward;
            Vector3 clubAxis = hitbox.transform.TransformDirection(axis).normalized;
            if (Vector3.Dot(clubAxis,hitbox.transform.TransformPoint(hitbox.center)-hand.position) < 0) clubAxis = -clubAxis;
            Vector3 handClubAxis = Quaternion.Inverse(hand.rotation) * clubAxis;
            Quaternion idleWrist = hand.rotation;

            float[] times = {0,.14f,.45f,.49f,.58f,.67f,.77f,.92f,Duration};
            Vector3[] offsets = {
                Vector3.zero,
                new Vector3(.48f,.20f,-.10f), new Vector3(.50f,.40f,-.36f), new Vector3(.50f,.40f,-.36f),
                new Vector3(.48f,.32f,.02f), new Vector3(.40f,-.14f,.63f), new Vector3(.40f,-.46f,.48f),
                new Vector3(.45f,-.60f,.12f), Vector3.zero
            };
            Vector3[] directions = {
                clubAxis, new Vector3(.15f,.48f,-.78f), new Vector3(.15f,.75f,-.70f), new Vector3(.15f,.75f,-.70f),
                new Vector3(.14f,.65f,.70f), new Vector3(.10f,-.24f,1f), new Vector3(.12f,-.68f,.72f),
                new Vector3(.10f,-.80f,.50f), clubAxis
            };
            var muscleIndices = Enumerable.Range(0,HumanTrait.MuscleCount).Where(i =>
                HumanTrait.MuscleName[i].StartsWith("Right Shoulder") || HumanTrait.MuscleName[i].StartsWith("Right Arm") ||
                HumanTrait.MuscleName[i].StartsWith("Right Forearm") || HumanTrait.MuscleName[i].StartsWith("Right Hand") ||
                new[]{"Right Thumb","Right Index","Right Middle","Right Ring","Right Little"}.Any(p=>HumanTrait.MuscleName[i].StartsWith(p))).ToArray();
            var keys = muscleIndices.ToDictionary(i=>i,i=>new List<Keyframe>());
            for (int k=0;k<times.Length;k++)
            {
                for (int b=0;b<rigTransforms.Length;b++)
                {
                    rigTransforms[b].localPosition = restPositions[b];
                    rigTransforms[b].localRotation = restRotations[b];
                }
                if (k != 0 && k != times.Length-1)
                {
                    Vector3 target = root.transform.TransformPoint(shoulder + offsets[k] * reach);
                    Vector3 hint = root.transform.TransformPoint(shoulder + new Vector3(.9f,-.35f,-.15f) * reach);
                    SolveArm(upper,lower,hand,target,hint);
                    Vector3 desired = root.transform.TransformDirection(directions[k].normalized);
                    hand.rotation = Quaternion.FromToRotation(idleWrist * handClubAxis,desired) * idleWrist;
                }
                HumanPose pose = new HumanPose(); handler.GetHumanPose(ref pose);
                foreach (int i in muscleIndices)
                {
                    float value=pose.muscles[i];
                    string name=HumanTrait.MuscleName[i];
                    if(k>0 && k<times.Length-1 && name.Contains("Stretched"))
                        value=name.StartsWith("Right Thumb") ? -.25f : name.Contains(" 2 ") ? -.65f : -.45f;
                    keys[i].Add(new Keyframe(times[k],value));
                }
            }
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip,ClipPath); }
            clip.ClearCurves(); clip.name = "SahurOneHandHeavy"; clip.frameRate = 60;
            foreach (int i in muscleIndices)
            {
                var curve = new AnimationCurve(keys[i].ToArray());
                for (int k=0;k<curve.length;k++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),HumanTrait.MuscleName[i]),curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; settings.loopBlend = false; settings.startTime = 0; settings.stopTime = Duration;
            settings.keepOriginalPositionXZ = true; settings.keepOriginalPositionY = true; settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
            return new { clip=ClipPath,clip.length,clip.humanMotion,muscleCurves=muscleIndices.Length,HoldPhase,reach };
        }
        finally { handler?.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
    }

    static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 hint)
    {
        Vector3 origin = upper.position;
        float a = Vector3.Distance(origin,lower.position), b = Vector3.Distance(lower.position,hand.position);
        Vector3 direction = (target-origin).normalized;
        float d = Mathf.Clamp(Vector3.Distance(origin,target),Mathf.Abs(a-b)+.001f,a+b-.001f);
        Vector3 bend = Vector3.ProjectOnPlane(hint-origin,direction).normalized;
        float x = (a*a-b*b+d*d)/(2*d), y = Mathf.Sqrt(Mathf.Max(0,a*a-x*x));
        Vector3 elbow = origin + direction*x + bend*y;
        upper.rotation = Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation = Quaternion.FromToRotation(hand.position-lower.position,origin+direction*d-lower.position)*lower.rotation;
    }

    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode before installing.");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (!clip || !clip.humanMotion) throw new Exception("The one-handed clip is not ready.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        foreach (var layer in controller.layers)
        {
            foreach (var child in layer.stateMachine.states)
            {
                var state = child.state;
                if (state.name != "Charge Windup" && state.name != "Heavy Attack") continue;
                Undo.RecordObject(state,"Install one-handed charged strike");
                state.motion = clip; state.speed = 1f; state.writeDefaultValues = false;
                state.timeParameterActive = state.name == "Charge Windup"; state.timeParameter = "ChargePhase";
                EditorUtility.SetDirty(state);
            }
            if (layer.name == "Charge Upper Body")
            {
                Undo.RecordObject(layer.avatarMask,"Keep charged strike on right arm");
                for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++) layer.avatarMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,
                    i==(int)AvatarMaskBodyPart.RightArm || i==(int)AvatarMaskBodyPart.RightFingers);
                for(int i=0;i<layer.avatarMask.transformCount;i++) layer.avatarMask.SetTransformActive(i,false);
                EditorUtility.SetDirty(layer.avatarMask); AssetDatabase.SaveAssetIfDirty(layer.avatarMask);
            }
        }
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try { Configure(root.GetComponent<Mavis.SahurAttack>(),clip); PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var scenes = new[]{"Assets/Scenes/First Island/Gameplay.unity","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoCombatDemo.unity"};
        var updated = new List<string>();
        foreach (string path in scenes)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) continue;
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            bool changed = false;
            try
            {
                foreach (var attack in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Mavis.SahurAttack>(true)))
                {
                    if (!attack.animator || attack.animator.runtimeAnimatorController != controller) continue;
                    Undo.RecordObject(attack,"Use one-handed charge timing"); Configure(attack,clip);
                    if (PrefabUtility.IsPartOfPrefabInstance(attack)) PrefabUtility.RecordPrefabInstancePropertyModifications(attack);
                    changed = true;
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); updated.Add(path); }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        }
        return new { clip=clip.name,HoldPhase,hitWindow="0.51–0.70",updated };
    }

    static void Configure(Mavis.SahurAttack attack, AnimationClip clip)
    {
        attack.chargeClip = clip; attack.fullChargeTime = 1.8f; attack.maxChargePosePhase = HoldPhase;
        attack.heavySwingWindowStart = .51f; attack.heavySwingWindowEnd = .70f;
        EditorUtility.SetDirty(attack);
    }
}
