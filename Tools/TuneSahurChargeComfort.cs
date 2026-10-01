using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneSahurChargeComfort
{
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play mode before installing.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Configure(root.GetComponent<Mavis.SahurAttack>(), false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        const string path = "Assets/Scenes/First Island/Gameplay.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        foreach (var attack in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Mavis.SahurAttack>(true)))
            Configure(attack, true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { fullChargeTime = 1.8f, authoredWindupEndpointPreserved = true };
    }

    static void Configure(Mavis.SahurAttack attack, bool undo)
    {
        if (attack == null) return;
        if (undo) Undo.RecordObject(attack, "Shorten and slow charge windup");
        attack.fullChargeTime = 1.8f;
        // Keep the downloaded clip's authored windup endpoint.
        EditorUtility.SetDirty(attack);
        if (undo && PrefabUtility.IsPartOfPrefabInstance(attack)) PrefabUtility.RecordPrefabInstancePropertyModifications(attack);
    }

    public static object Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play mode.");
        var attack = UnityEngine.Object.FindFirstObjectByType<Mavis.SahurAttack>();
        return VerifyFor(attack);
    }

    public static object VerifyIsolated()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play mode.");
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), new Vector3(0, 500, 0), Quaternion.identity);
        try
        {
            root.GetComponent<ThirdPersonPlayerController>().enabled = false;
            var attack = root.GetComponent<Mavis.SahurAttack>();
            attack.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            attack.animator.applyRootMotion = false;
            return VerifyFor(attack);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static object VerifyFor(Mavis.SahurAttack attack)
    {
        var animator = attack.animator;
        var guard = animator.GetComponent<SahurCombatGuardIK>();
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var leg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        int layer = animator.GetLayerIndex("Charge Upper Body");
        var start = typeof(Mavis.SahurAttack).GetMethod("StartCharge", Private);
        var cancel = typeof(Mavis.SahurAttack).GetMethod("CancelCharge", Private);
        var started = typeof(Mavis.SahurAttack).GetField("chargeStartedAt", Private);
        var updatePose = typeof(Mavis.SahurAttack).GetMethod("UpdateChargePose", Private);
        float oldStamina = attack.GetComponent<Mavis.PlayerStamina>().currentStamina;
        try
        {
            attack.GetComponent<Mavis.PlayerStamina>().currentStamina = 100f;
            start.Invoke(attack, null);
            started.SetValue(attack, Time.time - 0.15f);
            animator.SetLayerWeight(layer, 1f);
            updatePose.Invoke(attack, null);
            animator.Update(0.02f);
            started.SetValue(attack, Time.time - 0.9f);
            updatePose.Invoke(attack, null);
            float halfPhase = animator.GetFloat("ChargePhase");
            if (Mathf.Abs(halfPhase - attack.maxChargePosePhase * .5f) > 0.002f) throw new Exception("Wrong half-charge phase.");
            started.SetValue(attack, Time.time - 1.8f);
            updatePose.Invoke(attack, null);
            float fullPhase = animator.GetFloat("ChargePhase");
            started.SetValue(attack, Time.time - 5f);
            updatePose.Invoke(attack, null);
            if (Mathf.Abs(fullPhase - attack.maxChargePosePhase) > 0.002f || Mathf.Abs(animator.GetFloat("ChargePhase") - fullPhase) > 0.002f || attack.Charge01 < 0.999f)
                throw new Exception("Overcharging went beyond the safe pose or prevented full charge.");
            animator.SetFloat("Speed", 16.8f);
            Vector3 firstHand = Vector3.zero;
            Quaternion firstLeg = Quaternion.identity;
            Quaternion firstArm = Quaternion.identity;
            float maxArmSwing = 0;
            float maxHandDrift = 0f, maxLegSwing = 0f;
            for (int i = 0; i < 8; i++)
            {
                animator.Play("Locomotion", 0, i * 0.125f);
                animator.Play("Charge Windup", layer, 0f);
                animator.Update(0.02f);
                var localHand = attack.transform.InverseTransformPoint(hand.position);
                if (i == 0) { firstHand = localHand; firstLeg = leg.localRotation; firstArm = arm.localRotation; }
                else
                {
                    maxHandDrift = Mathf.Max(maxHandDrift, Vector3.Distance(firstHand, localHand) * attack.transform.lossyScale.x);
                    maxLegSwing = Mathf.Max(maxLegSwing, Quaternion.Angle(firstLeg, leg.localRotation));
                    maxArmSwing = Mathf.Max(maxArmSwing, Quaternion.Angle(firstArm, arm.localRotation));
                }
            }
            if (maxArmSwing > 2f || maxLegSwing < 4f) throw new Exception("Run isolation failed: arm swing=" + maxArmSwing + ", hand drift=" + maxHandDrift + ", leg swing=" + maxLegSwing);
            cancel.Invoke(attack, null);
            animator.Update(0.02f);
            float weight = animator.GetLayerWeight(layer);
            if (weight > 0.001f || animator.GetLayerWeight(layer) > 0.001f) throw new Exception("Cancel left a locked arm.");
            return new { halfPhase, fullPhase, fullDamageCharge = true, maxHandDriftMetres = maxHandDrift, maxArmSwingDegrees=maxArmSwing, maxLegSwingDegrees = maxLegSwing, cancelledIK = weight };
        }
        finally
        {
            attack.SuspendForSwimming();
            animator.SetFloat("Speed", 0f);
            animator.Play("Locomotion", 0, 0f);
            animator.Update(0f);
            attack.GetComponent<Mavis.PlayerStamina>().currentStamina = oldStamina;
        }
    }
}
