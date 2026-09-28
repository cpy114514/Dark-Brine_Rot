using System;
using System.Reflection;
using System.Text;
using Mavis;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deterministic regression for current/next source-phase cache promotion.
/// No input, waits, automatic playback, or persistent asset changes.
/// </summary>
public static class VerifySahurComboTravelCache
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static void Call(object target, string method) =>
        target.GetType().GetMethod(method, Private).Invoke(target, null);

    static void SetSample(ThirdPersonPlayerController movement, string fieldName,
        int stateHash, float phase, bool valid)
    {
        var field = typeof(ThirdPersonPlayerController).GetField(fieldName, Private);
        object sample = Activator.CreateInstance(field.FieldType);
        field.FieldType.GetField("hash", Fields).SetValue(sample, stateHash);
        field.FieldType.GetField("phase", Fields).SetValue(sample, phase);
        field.FieldType.GetField("valid", Fields).SetValue(sample, valid);
        field.SetValue(movement, sample);
    }

    static Vector3 Consume(ThirdPersonPlayerController movement)
    {
        var arguments = new object[] { Vector3.zero };
        bool success = (bool)typeof(ThirdPersonPlayerController)
            .GetMethod("TryGetComboSourceTravel", Private).Invoke(movement, arguments);
        if (!success) throw new InvalidOperationException("Prefab is missing source-travel metadata.");
        return (Vector3)arguments[0];
    }

    public static string Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        if (prefab == null) throw new InvalidOperationException("Sahur prefab is missing.");
        GameObject holder = null;
        var savedCursor = Cursor.lockState;
        bool savedVisible = Cursor.visible;
        var report = new StringBuilder();
        try
        {
            holder = new GameObject("Temporary deterministic combo cache regression");
            holder.SetActive(false);
            holder.transform.position = new Vector3(2800f, 300f, 2800f);
            var actor = UnityEngine.Object.Instantiate(prefab, holder.transform);
            var movement = actor.GetComponent<ThirdPersonPlayerController>();
            var attack = actor.GetComponent<SahurAttack>();
            foreach (var behaviour in actor.GetComponentsInChildren<Behaviour>(true))
                if (!(behaviour is Animator)) behaviour.enabled = false;
            movement.snapSpawnToIslandSurface = false;
            movement.waterSplashes = false;
            movement.cameraCollision = false;
            holder.SetActive(true);
            // In Play Mode Awake already ran synchronously on activation.
            // Edit Mode requires explicit initialization of this disposable clone.
            if (!Application.isPlaying)
            {
                Call(movement, "Awake");
                Call(attack, "Awake");
            }
            Call(movement, "Start");
            var animator = attack.animator;
            if (animator == null) throw new InvalidOperationException("Visual Animator was not initialized.");
            animator.speed = 0f;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var visual = animator.transform;
            var sourceX = movement.comboSourceX;
            var sourceZ = movement.comboSourceZ;
            if (sourceX == null || sourceZ == null || sourceX.Length != 3 || sourceZ.Length != 3 ||
                sourceX[0] == null || sourceZ[0] == null)
                throw new InvalidOperationException("Generate the prefab's source X/Z curves before testing.");
            int comboHash = Animator.StringToHash("Base Layer." + attack.comboOneState);
            int locomotionHash = Animator.StringToHash("Base Layer.Locomotion");

            Vector3 Expected(float from, float to) => Vector3.Scale(new Vector3(
                sourceX[0].Evaluate(to) - sourceX[0].Evaluate(from), 0f,
                sourceZ[0].Evaluate(to) - sourceZ[0].Evaluate(from)), visual.lossyScale) * animator.humanScale;

            void Case(string label, float currentSeed, float incomingSeed, bool incomingValid,
                float evaluatedPhase, float expectedFrom, bool outgoingIsDifferent = false)
            {
                animator.Play(comboHash, 0, evaluatedPhase);
                animator.Update(0f);
                if (animator.IsInTransition(0))
                    throw new InvalidOperationException("Manual sampling unexpectedly left an active transition.");
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash != comboHash)
                    throw new InvalidOperationException("Manual sampling did not enter the requested combo state.");
                SetSample(movement, "comboCurrentSample", outgoingIsDifferent ? locomotionHash : comboHash,
                    currentSeed, true);
                SetSample(movement, "comboNextSample", comboHash, incomingSeed, incomingValid);
                Vector3 actual = Consume(movement);
                Vector3 expected = Expected(expectedFrom, Mathf.Clamp01(state.normalizedTime));
                float error = Vector3.Distance(actual, expected);
                if (error > 0.00002f)
                    throw new InvalidOperationException($"{label}: expected {expected.ToString("F6")}, actual {actual.ToString("F6")}; error {error:F7}m.");
                // Promotion must be consumed exactly once, not carried forward
                // as an incoming zero-phase sample on subsequent evaluations.
                Vector3 repeated = Consume(movement);
                if (repeated.sqrMagnitude > 0.0000000004f)
                    throw new InvalidOperationException(label + " replayed source displacement without any phase advance.");
                report.AppendLine($"{label}: source {expectedFrom:F2} -> {state.normalizedTime:F2}, travel {actual.ToString("F6")}m; error {error:F7}m; repeated same-phase consume=0.");
            }

            report.AppendLine($"Source scale: visual world scale {visual.lossyScale.ToString("F4")}, humanoid scale {animator.humanScale:F6}.");
            Case("Same-hash restart promotes zero-phase incoming", 0.05f, 0f, true, 0.12f, 0f);
            Case("Same-hash transition completion continues incoming phase", 0.05f, 0.04f, true, 0.12f, 0.04f);
            Case("Ordinary current playback has no incoming cache", 0.05f, 0f, false, 0.12f, 0.05f);
            Case("Slow first evaluation retains full incoming travel", 0.05f, 0f, true, 0.60f, 0f, true);
            return report.ToString();
        }
        finally
        {
            if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            Cursor.lockState = savedCursor;
            Cursor.visible = savedVisible;
        }
    }
}
