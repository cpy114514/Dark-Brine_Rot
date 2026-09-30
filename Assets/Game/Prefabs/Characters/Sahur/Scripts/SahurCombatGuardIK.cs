using UnityEngine;

/// <summary>Keeps Sahur's free arm in a compact guard during stick attacks.</summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public sealed class SahurCombatGuardIK : MonoBehaviour
{
    [Range(0f, 1f)] public float guardStrength = 0.9f;
    [Min(0.01f)] public float blendInSeconds = 0.09f;
    [Min(0.01f)] public float blendOutSeconds = 0.16f;

    Animator animator;
    Mavis.SahurAttack attack;
    Transform chest;
    Transform leftShoulder;
    float weight;
    float chargeWeight;
    int chargeLayer = -1;
    Transform rightShoulder;

    void Awake()
    {
        animator = GetComponent<Animator>();
        attack = GetComponentInParent<Mavis.SahurAttack>();
        chargeLayer = animator.GetLayerIndex("Charge Upper Body");
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
            return;
        // The visual controller is assigned by the player after this component's Awake.
        if (chargeLayer < 0) chargeLayer = animator.GetLayerIndex("Charge Upper Body");

        bool charging = attack != null && attack.enabled && attack.IsCharging;
        // Apply on the final upper-body layer, after its animation has been evaluated.
        // The base-layer IK would otherwise be overwritten by the charge clip.
        if (layerIndex == chargeLayer)
        {
            ApplyRightChargePull(charging);
            return;
        }
        if (layerIndex != 0) return;
        bool guarding = attack != null && attack.enabled &&
                        attack.IsCombatMotionActive && !charging;
        float target = guarding ? guardStrength : 0f;
        float seconds = guarding ? blendInSeconds : charging ? 0.08f : blendOutSeconds;
        weight = Mathf.MoveTowards(weight, target, Time.deltaTime / seconds);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, weight * 0.65f);
        if (weight <= 0f) return;

        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (leftShoulder == null) leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        if (chest == null || leftShoulder == null) return;

        Transform root = attack.transform;
        // Keep the free hand compact during attacks; fade this guard out during charge.
        Vector3 handTarget = chest.position - root.right * 0.17f +
                             root.forward * 0.21f + Vector3.up * 0.03f;
        Vector3 elbowHint = leftShoulder.position - root.right * 0.29f +
                            root.forward * 0.10f - Vector3.up * 0.23f;
        animator.SetIKPosition(AvatarIKGoal.LeftHand, handTarget);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow, elbowHint);
    }

    void ApplyRightChargePull(bool charging)
    {
        // Release immediately gives the original swing full control again.
        chargeWeight = charging ? Mathf.SmoothStep(0f, 1f, attack.Charge01) : 0f;
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, chargeWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, chargeWeight);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, chargeWeight);
        if (chargeWeight <= 0f) return;

        if (rightShoulder == null)
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        if (rightShoulder == null || hand == null || forearm == null) return;

        Transform root = attack.transform;
        float reach = Vector3.Distance(rightShoulder.position, forearm.position) +
                      Vector3.Distance(forearm.position, hand.position);
        // Actor-relative right, independent of the camera or the character's heading.
        Vector3 pull = root.right * 0.62f - root.forward * 0.48f - root.up * 0.10f;
        animator.SetIKPosition(AvatarIKGoal.RightHand, rightShoulder.position + pull * reach);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow,
            rightShoulder.position + (root.right * 0.60f - root.forward * 0.15f - root.up * 0.40f) * reach);

        // Aim the attached bat to the same side as the pull, rather than across the left shoulder.
        var box = attack.stickHitbox as BoxCollider;
        var capsule = attack.stickHitbox as CapsuleCollider;
        if (box != null || capsule != null)
        {
            Vector3 axis;
            Vector3 center;
            if (capsule != null)
            {
                axis = capsule.direction == 0 ? Vector3.right :
                       capsule.direction == 1 ? Vector3.up : Vector3.forward;
                center = capsule.center;
            }
            else
            {
                Vector3 size = box.size;
                axis = size.x >= size.y && size.x >= size.z ? Vector3.right :
                       size.y >= size.z ? Vector3.up : Vector3.forward;
                center = box.center;
            }
            Transform hitbox = attack.stickHitbox.transform;
            Vector3 worldAxis = hitbox.TransformDirection(axis);
            if (Vector3.Dot(worldAxis, hitbox.TransformPoint(center) - hand.position) < 0f)
                worldAxis = -worldAxis;
            Vector3 direction = (root.right * 0.85f - root.forward * 0.50f + root.up * 0.15f).normalized;
            animator.SetIKRotation(AvatarIKGoal.RightHand,
                Quaternion.FromToRotation(worldAxis, direction) *
                animator.GetIKRotation(AvatarIKGoal.RightHand));
        }
        else animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }

    void OnDisable()
    {
        weight = 0f;
        chargeWeight = 0f;
    }
}
