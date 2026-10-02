using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class AuditChargeJitter
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.identity);
        float time=Time.timeScale;var original=root.GetComponent<Mavis.SahurAttack>().animator.runtimeAnimatorController;
        var overrides=new List<AnimatorOverrideController>();
        try{
            Time.timeScale=0;root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;var animator=attack.animator;
            animator.GetComponent<SahurRootMotionRelay>().enabled=false;animator.GetComponent<SahurCombatGuardIK>().enabled=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
            var restored=attack.chargedStrikeClip?attack.chargedStrikeClip:attack.chargeClip;
            var downloaded=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/SwordAndShieldSlash_ThreeHit.fbx").OfType<AnimationClip>().Single(c=>c.name=="SahurSwordCombo1");
            var results=new List<object>();
            foreach(var clip in new[]{restored,downloaded}){
                var replacement=new AnimatorOverrideController(original);replacement[restored.name]=clip;overrides.Add(replacement);
                animator.runtimeAnimatorController=replacement;animator.Rebind();animator.applyRootMotion=false;animator.SetLayerWeight(1,1);animator.SetFloat("Speed",0);
                var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var forearm=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                animator.Play("Locomotion",0,0);animator.Play("Heavy Attack",1,clip==restored&&attack.chargedStrikeClip?0:.47f);animator.Update(0);
                var cap=(CapsuleCollider)attack.stickHitbox;Vector3 axis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
                Quaternion oldHand=hand.rotation,oldForearm=forearm.rotation,oldArm=arm.rotation;
                var frames=new List<object>();float maxHand=0,maxForearm=0,maxArm=0;
                var peaks=new List<object>();
                for(int i=0;i<(clip==restored&&attack.chargedStrikeClip?72:34);i++){
                    animator.Update(1f/60);float phase=animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
                    float handAngle=Quaternion.Angle(hand.rotation,oldHand),forearmAngle=Quaternion.Angle(forearm.rotation,oldForearm),armAngle=Quaternion.Angle(arm.rotation,oldArm);
                    maxHand=Mathf.Max(maxHand,handAngle);maxForearm=Mathf.Max(maxForearm,forearmAngle);maxArm=Mathf.Max(maxArm,armAngle);
                    Vector3 shaft=cap.transform.TransformDirection(axis);if(Vector3.Dot(shaft,cap.transform.TransformPoint(cap.center)-hand.position)<0)shaft=-shaft;
                    var f=new{phase,handAngle,forearmAngle,armAngle,shaftX=shaft.x,shaftY=shaft.y,shaftZ=shaft.z,handX=hand.position.x,handY=hand.position.y-500,handZ=hand.position.z};frames.Add(f);
                    if(handAngle>20||forearmAngle>15||armAngle>15)peaks.Add(f);
                    oldHand=hand.rotation;oldForearm=forearm.rotation;oldArm=arm.rotation;
                }
                results.Add(new{clip=clip.name,maxHand,maxForearm,maxArm,peaks});
                File.WriteAllText("Tools/"+clip.name+"-jitter.json",Newtonsoft.Json.JsonConvert.SerializeObject(frames,Newtonsoft.Json.Formatting.Indented));
            }
            return results;
        }finally{Time.timeScale=time;UnityEngine.Object.DestroyImmediate(root);foreach(var o in overrides)UnityEngine.Object.DestroyImmediate(o);}
    }
}
