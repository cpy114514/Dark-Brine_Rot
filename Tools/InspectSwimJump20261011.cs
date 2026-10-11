using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectSwimJump20261011
{
    public static object Inspect()
    {
        var live=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        object liveState=live==null ? null : new {position=live.transform.position.ToString(), swimming=live.Swimming,
            velocity=live.WorldVelocity.ToString(), controller=live.CharacterAnimator?.runtimeAnimatorController?.name,
            state=live.CharacterAnimator?.GetCurrentAnimatorStateInfo(0).shortNameHash,
            movementLock=live.ExternalMovementLock, controlLock=live.ExternalControlLock};
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var holder=new GameObject("Temporary swimming/jump pose inspection");holder.SetActive(false);
        holder.transform.position=new Vector3(3000,500,3000);
        try
        {
            var visual=UnityEngine.Object.Instantiate(prefab.transform.Find("Pbr Sahur Visual").gameObject,holder.transform);
            foreach(var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            var animator=visual.GetComponent<Animator>();var root=prefab.GetComponent<Animator>();
            animator.avatar=root.avatar;animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("Encounter/v006/SahurGameplay");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
            holder.SetActive(true);animator.Rebind();animator.Update(0);
            var samples=new System.Collections.Generic.List<object>();
            var controller=animator.runtimeAnimatorController;
            foreach(string variant in new[]{"Original","Body","Feet"})
            {
            AnimatorOverrideController replacement=null;
            if(variant!="Original")
            {
                replacement=new AnimatorOverrideController(controller);
                foreach(string clipName in new[]{"PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006"})
                {
                    var clip=AssetDatabase.LoadAllAssetsAtPath($"Assets/AnimationStaging/SwimJumpFix20261011/{clipName}_{variant}.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
                    replacement[clipName]=clip;
                }
                animator.runtimeAnimatorController=replacement;
            }
            foreach(var name in (variant=="Original" ? new[]{"Locomotion","Swim Idle","Swim Forward","Swim Fast","Jump Start","Jump Loop","Land"} : new[]{"Jump Start","Jump Loop","Land"}))
            foreach(float phase in new[]{0f,.25f,.5f,.75f,.99f})
            {
                animator.Play(name,0,phase);animator.Update(0);
                var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var head=animator.GetBoneTransform(HumanBodyBones.Head);
                var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var feet=(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position+animator.GetBoneTransform(HumanBodyBones.RightFoot).position)*.5f;
                samples.Add(new{variant,name,phase,headAhead=Vector3.Dot(head.position-hips.position,holder.transform.forward),
                    shouldersForward=Vector3.Dot(Vector3.Cross(right.position-left.position,Vector3.up).normalized,holder.transform.forward),
                    headY=head.position.y-holder.transform.position.y,feetY=feet.y-holder.transform.position.y,
                    clip=animator.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name,visualYaw=visual.transform.localEulerAngles.y});
            }
            animator.runtimeAnimatorController=controller;
            if(replacement!=null)UnityEngine.Object.DestroyImmediate(replacement);
            }
            return new{liveState,samples};
        }
        finally{UnityEngine.Object.DestroyImmediate(holder);}
    }
}
