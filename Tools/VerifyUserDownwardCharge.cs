using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyUserDownwardCharge
{
    public static object Verify(){
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.identity);
        float time=Time.timeScale;AnimatorOverrideController replacement=null;
        try{
            Time.timeScale=0;root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;var animator=attack.animator;
            animator.GetComponent<SahurRootMotionRelay>().enabled=false;animator.GetComponent<SahurCombatGuardIK>().enabled=false;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx").OfType<AnimationClip>().Single(c=>c.name=="SahurStandingMeleeDownward");
            var extracted=attack.chargeClip;var original=animator.runtimeAnimatorController;
            replacement=new AnimatorOverrideController(original);replacement[extracted]=source;
            int layer=animator.GetLayerIndex("Charge Upper Body");
            var bones=new[]{HumanBodyBones.RightShoulder,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand}.Select(animator.GetBoneTransform).ToArray();
            float maxRotationError=0,maxPositionError=0;
            for(int sample=0;sample<=120;sample++){
                float phase=sample/120f;
                animator.runtimeAnimatorController=original;animator.Rebind();animator.SetLayerWeight(layer,1);animator.SetFloat("Speed",0);
                animator.Play("Locomotion",0,0);animator.Play("Heavy Attack",layer,phase);animator.Update(0);
                var rotations=bones.Select(b=>b.localRotation).ToArray();var position=bones[3].position;
                animator.runtimeAnimatorController=replacement;animator.Rebind();animator.SetLayerWeight(layer,1);animator.SetFloat("Speed",0);
                animator.Play("Locomotion",0,0);animator.Play("Heavy Attack",layer,phase);animator.Update(0);
                for(int i=0;i<bones.Length;i++)maxRotationError=Mathf.Max(maxRotationError,Quaternion.Angle(rotations[i],bones[i].localRotation));
                maxPositionError=Mathf.Max(maxPositionError,Vector3.Distance(position,bones[3].position));
            }
            if(maxRotationError>.1f||maxPositionError>.001f)throw new Exception("The supplied arm motion changed: "+maxRotationError+" degrees, "+maxPositionError+" m.");
            if(attack.chargedStrikeClip||Mathf.Abs(attack.maxChargePosePhase-.28f)>.0001f)throw new Exception("Charge and release do not use the same source animation.");
            return new{samples=121,maxRotationError,maxPositionError,sameAnimationForChargeAndStrike=true,holdFrame=.28f*68,hitFrames="23.8–29.2",rightArmOnly=true};
        }finally{Time.timeScale=time;UnityEngine.Object.DestroyImmediate(root);if(replacement)UnityEngine.Object.DestroyImmediate(replacement);}
    }
}
