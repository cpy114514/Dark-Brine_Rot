using UnityEngine;

/// <summary>Keeps Sahur's free arm in a compact guard during stick attacks.</summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public sealed class SahurCombatGuardIK : MonoBehaviour
{
    [Range(0f, 1f)] public float guardStrength = 0.9f;
    [Min(0.01f)] public float blendInSeconds = 0.09f;
    [Min(0.01f)] public float blendOutSeconds = 0.16f;
    [Header("Charge windup")]
    [Range(0f, 150f)] public float chargeHandTurnDegrees = 125f;
    [Min(0.01f)] public float chargeTurnInSeconds = 0.18f;
    [Min(0.01f)] public float chargeTurnOutSeconds = 0.09f;

    Animator animator;
    Mavis.SahurAttack attack;
    Transform chest;
    Transform leftShoulder;
    Transform rightHand;
    float weight;
    float chargeTurnWeight;

    void Awake()
    {
        animator = GetComponent<Animator>();
        attack = GetComponentInParent<Mavis.SahurAttack>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != 0 || animator == null || !animator.isHuman)
            return;

        bool charging = attack != null && attack.enabled && attack.IsCharging;
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
        // The source combo expects a shield here; the Blender charge does not.
        Vector3 handTarget = chest.position - root.right * 0.17f +
                             root.forward * 0.21f + Vector3.up * 0.03f;
        Vector3 elbowHint = leftShoulder.position - root.right * 0.29f +
                            root.forward * 0.10f - Vector3.up * 0.23f;
        animator.SetIKPosition(AvatarIKGoal.LeftHand, handTarget);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow, elbowHint);
    }

    void LateUpdate()
    {
        if (animator == null || !animator.isHuman || attack == null) return;
        if (rightHand == null) rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null) return;

        // The Blender arm take pulls the grip around the right shoulder. Match
        // the stick's turn to charge progress so it follows that arc gradually.
        bool charging = attack.enabled && attack.IsCharging;
        float seconds = charging ? chargeTurnInSeconds : chargeTurnOutSeconds;
        chargeTurnWeight = Mathf.MoveTowards(chargeTurnWeight,
            charging ? attack.Charge01 : 0f,
            Time.deltaTime / seconds);
        if (chargeTurnWeight > 0f)
            rightHand.rotation = Quaternion.AngleAxis(
                chargeHandTurnDegrees * chargeTurnWeight, attack.transform.up) * rightHand.rotation;
    }

    void OnDisable()
    {
        weight = 0f;
        chargeTurnWeight = 0f;
    }
}
