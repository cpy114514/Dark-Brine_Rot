using UnityEngine;

/// <summary>Free-arm guard for ordinary attacks. Charged attacks leave the left arm on locomotion.</summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public sealed class SahurCombatGuardIK : MonoBehaviour
{
    [Range(0f, 1f)] public float guardStrength = 0.9f;
    [Min(0.01f)] public float blendInSeconds = 0.09f;
    [Min(0.01f)] public float blendOutSeconds = 0.16f;
    Animator animator;
    Mavis.SahurAttack attack;
    Transform chest, leftShoulder;
    float weight;
    int chargeLayer = -1;
    void Awake() { animator=GetComponent<Animator>(); attack=GetComponentInParent<Mavis.SahurAttack>(); }
    void OnAnimatorIK(int layerIndex)
    {
        if (!animator || !animator.runtimeAnimatorController || !animator.isInitialized || !animator.isHuman) return;
        if (chargeLayer < 0) chargeLayer = animator.GetLayerIndex("Charge Upper Body");
        if (layerIndex == chargeLayer)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,0);
            return;
        }
        if(layerIndex!=0) return;
        bool authored=attack && (attack.IsCharging || attack.IsHeavyAttackActive);
        bool guarding=attack && attack.enabled && attack.IsCombatMotionActive && !authored &&
            !(attack.GetComponent<Mavis.SahurBoomerang>()?.IsThrowing ?? false);
        weight=authored ? 0 : Mathf.MoveTowards(weight,guarding ? guardStrength : 0,
            Time.deltaTime/(guarding ? blendInSeconds : blendOutSeconds));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,weight);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,weight*.65f);
        if(weight<=0) return;
        if(!chest) chest=animator.GetBoneTransform(HumanBodyBones.Chest);
        if(!leftShoulder) leftShoulder=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var forearm=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        var hand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
        if(!chest || !leftShoulder || !forearm || !hand) return;
        var root=attack.transform;
        float reach=Vector3.Distance(leftShoulder.position,forearm.position)+Vector3.Distance(forearm.position,hand.position);
        animator.SetIKPosition(AvatarIKGoal.LeftHand,leftShoulder.position+(-root.right*.22f+root.forward*.42f-root.up*.48f)*reach);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow,leftShoulder.position+(-root.right*.55f+root.forward*.08f-root.up*.40f)*reach);
    }
    void OnDisable() { weight=0; }
}
