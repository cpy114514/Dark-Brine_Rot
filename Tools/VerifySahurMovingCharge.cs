using System;
using System.Reflection;
using UnityEngine;

/// <summary>Read-only Play-mode pose check for Sahur's masked moving charge.</summary>
public static class VerifySahurMovingCharge
{
    public static string CombatFlow()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var attack = UnityEngine.Object.FindFirstObjectByType<Mavis.SahurAttack>();
        if (attack == null || attack.animator == null)
            throw new InvalidOperationException("Active Sahur Animator is missing.");
        var animator = attack.animator;
        int layer = animator.GetLayerIndex("Charge Upper Body");
        int charge = Animator.StringToHash("Charge Upper Body.Charge Windup");
        int locomotion = Animator.StringToHash("Base Layer.Locomotion");
        int heavy = Animator.StringToHash("Base Layer.Heavy Attack");
        if (layer <= 0) throw new InvalidOperationException("Charge layer is missing.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var start = typeof(Mavis.SahurAttack).GetMethod("StartCharge", flags);
        var cancel = typeof(Mavis.SahurAttack).GetMethod("CancelCharge", flags);
        var release = typeof(Mavis.SahurAttack).GetMethod("ReleaseCharge", flags);
        var disable = typeof(Mavis.SahurAttack).GetMethod("OnDisable", flags);
        if (start == null || cancel == null || release == null || disable == null)
            throw new InvalidOperationException("Sahur combat methods are missing.");
        try
        {
            start.Invoke(attack, null);
            animator.Update(0f);
            if (!attack.IsCharging || animator.GetCurrentAnimatorStateInfo(layer).fullPathHash != charge ||
                animator.GetCurrentAnimatorStateInfo(0).fullPathHash != locomotion)
                throw new InvalidOperationException("Charge did not keep base locomotion and arm overlay.");

            animator.SetLayerWeight(layer, 0.6f);
            cancel.Invoke(attack, null);
            if (attack.IsCharging || animator.GetLayerWeight(layer) > 0.001f)
                throw new InvalidOperationException("Cancelled charge left its arm overlay active.");

            start.Invoke(attack, null);
            animator.Update(0f);
            animator.SetFloat(Animator.StringToHash("ChargePhase"), 0.16f);
            animator.SetLayerWeight(layer, 1f);
            release.Invoke(attack, null);
            animator.Update(0.02f);
            bool heavyPlaying = animator.GetCurrentAnimatorStateInfo(0).fullPathHash == heavy ||
                (animator.IsInTransition(0) &&
                 animator.GetNextAnimatorStateInfo(0).fullPathHash == heavy);
            if (attack.IsCharging || !attack.UsesAnimationRootMotion || !heavyPlaying)
                throw new InvalidOperationException("Release did not start the original full-body heavy attack.");
            return "Charge flow passed: base locomotion + arm overlay, cancel clears overlay, " +
                   "release enters original heavy attack with root motion.";
        }
        finally
        {
            disable.Invoke(attack, null);
            animator.SetLayerWeight(layer, 0f);
            animator.SetFloat(Animator.StringToHash("ChargePhase"), 0f);
            animator.Play(locomotion, 0, 0f);
            animator.Update(0f);
        }
    }

    public static string Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var attack = UnityEngine.Object.FindFirstObjectByType<Mavis.SahurAttack>();
        if (attack == null || attack.animator == null)
            throw new InvalidOperationException("Active Sahur Animator is missing.");
        var animator = attack.animator;
        if (!animator.isHuman) throw new InvalidOperationException("Sahur Avatar is not Humanoid.");
        int layer = animator.GetLayerIndex("Charge Upper Body");
        int locomotion = Animator.StringToHash("Base Layer.Locomotion");
        int charge = Animator.StringToHash("Charge Upper Body.Charge Windup");
        if (layer <= 0 || !animator.HasState(0, locomotion) || !animator.HasState(layer, charge))
            throw new InvalidOperationException("Moving-charge Animator states are missing.");
        var leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        var rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        if (leftLeg == null || rightLeg == null || rightArm == null)
            throw new InvalidOperationException("Sahur Humanoid bones are missing.");

        int speed = Animator.StringToHash("Speed");
        int phase = Animator.StringToHash("ChargePhase");
        var oldBase = animator.GetCurrentAnimatorStateInfo(0);
        var oldUpper = animator.GetCurrentAnimatorStateInfo(layer);
        float oldSpeed = animator.GetFloat(speed);
        float oldPhase = animator.GetFloat(phase);
        float oldWeight = animator.GetLayerWeight(layer);
        try
        {
            animator.SetFloat(speed, 9.6f);
            animator.SetFloat(phase, 0.16f);
            animator.SetLayerWeight(layer, 0f);
            animator.Play(locomotion, 0, 0f);
            animator.Play(charge, layer, 0f);
            animator.Update(0.12f);
            Quaternion baseLeft = leftLeg.localRotation;
            Quaternion baseRight = rightLeg.localRotation;
            Quaternion baseArm = rightArm.localRotation;

            animator.SetLayerWeight(layer, 1f);
            animator.Play(locomotion, 0, 0f);
            animator.Play(charge, layer, 0f);
            animator.Update(0.12f);
            float legMaskError = Mathf.Max(Quaternion.Angle(baseLeft, leftLeg.localRotation),
                Quaternion.Angle(baseRight, rightLeg.localRotation));
            float armPoseChange = Quaternion.Angle(baseArm, rightArm.localRotation);
            Quaternion walkLeft = leftLeg.localRotation;
            Quaternion walkRight = rightLeg.localRotation;
            animator.Update(0.30f);
            float walkLegSwing = Mathf.Max(Quaternion.Angle(walkLeft, leftLeg.localRotation),
                Quaternion.Angle(walkRight, rightLeg.localRotation));

            animator.SetFloat(speed, 16.8f);
            animator.Play(locomotion, 0, 0f);
            animator.Update(0.12f);
            Quaternion runLeft = leftLeg.localRotation;
            Quaternion runRight = rightLeg.localRotation;
            animator.Update(0.24f);
            float runLegSwing = Mathf.Max(Quaternion.Angle(runLeft, leftLeg.localRotation),
                Quaternion.Angle(runRight, rightLeg.localRotation));

            if (legMaskError > 1.5f || armPoseChange < 2f ||
                walkLegSwing < 4f || runLegSwing < 4f)
                throw new InvalidOperationException($"Charge pose failed: legMaskError={legMaskError:F2}, " +
                    $"armPoseChange={armPoseChange:F2}, walkLegSwing={walkLegSwing:F2}, " +
                    $"runLegSwing={runLegSwing:F2} degrees.");
            return $"Charge pose passed: legMaskError={legMaskError:F2}, " +
                   $"armPoseChange={armPoseChange:F2}, walkLegSwing={walkLegSwing:F2}, " +
                   $"runLegSwing={runLegSwing:F2} degrees.";
        }
        finally
        {
            animator.SetFloat(speed, oldSpeed);
            animator.SetFloat(phase, oldPhase);
            animator.SetLayerWeight(layer, oldWeight);
            animator.Play(oldBase.fullPathHash != 0 ? oldBase.fullPathHash : locomotion,
                0, oldBase.normalizedTime);
            animator.Play(oldUpper.fullPathHash != 0 ? oldUpper.fullPathHash : charge,
                layer, oldUpper.normalizedTime);
            animator.Update(0f);
        }
    }
}
