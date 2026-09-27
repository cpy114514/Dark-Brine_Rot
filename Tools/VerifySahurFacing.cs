using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public static class VerifySahurFacing
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);

    public static async Task<string> Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode for the real Animator regression check.");
        if (PauseSettingsMenu.IsOpen) throw new InvalidOperationException("Close settings before the regression check.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var holder = new GameObject("Temporary Sahur facing regression");
        holder.SetActive(false);
        holder.transform.position = new Vector3(2000f, 300f, 2000f);
        var floor = new GameObject("Temporary regression floor");
        floor.transform.position = holder.transform.position - Vector3.up * 0.5f;
        floor.AddComponent<BoxCollider>().size = new Vector3(80f, 1f, 80f);
        var actor = UnityEngine.Object.Instantiate(prefab, holder.transform);
        actor.transform.localPosition = Vector3.up;
        var movement = actor.GetComponent<ThirdPersonPlayerController>();
        var attack = actor.GetComponent<SahurAttack>();
        movement.snapSpawnToIslandSurface = false;
        movement.waterSplashes = false;
        movement.cameraCollision = false;
        foreach (var input in actor.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
        float originalTimeScale = Time.timeScale;
        var originalCursor = Cursor.lockState;
        bool originalCursorVisible = Cursor.visible;
        var report = new StringBuilder();
        Action<ScriptableRenderContext, Camera> inspectRenderedPose = null;
        try
        {
            Time.timeScale = 1f;
            holder.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            await Task.Delay(700);
            var visual = actor.transform.Find("Pbr Sahur Visual");
            Quaternion originalVisualRotation = visual.localRotation;
            var head = attack.animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 headFacingAxis = head.InverseTransformDirection(actor.transform.forward);
            int renderedThirdSamples = 0;
            float renderedThirdPeakYaw = 0f;
            string renderedThirdError = null;
            // Animator evaluation and LateUpdate display correction happen
            // after Update tasks. Inspect the final pose presented to the
            // renderer, rather than an intermediate uncorrected bone frame.
            inspectRenderedPose = (context, camera) =>
            {
                if (camera.cameraType != CameraType.Game || attack.CurrentComboStage != 2) return;
                var state = attack.animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName("Combo 3") || state.normalizedTime < attack.comboThreeHitWindow.x ||
                    state.normalizedTime > attack.comboThreeHitWindow.y) return;
                var face = Vector3.ProjectOnPlane(head.TransformDirection(headFacingAxis), Vector3.up).normalized;
                float yaw = Mathf.Abs(Vector3.SignedAngle(actor.transform.forward, face, Vector3.up));
                renderedThirdSamples++;
                renderedThirdPeakYaw = Mathf.Max(renderedThirdPeakYaw, yaw);
                if (yaw > 5f)
                    renderedThirdError = $"Rendered third hit changed face heading: phase={state.normalizedTime:F3}, faceYaw={yaw:F2}, visualYaw={visual.localEulerAngles.y:F2}.";
            };
            RenderPipelineManager.beginCameraRendering += inspectRenderedPose;
            // Measured from the approved original-coordinate clips before
            // visual-heading correction; allow normal frame/blend variance.
            float[] referenceTravel = { 1.588f, 1.528f, 1.322f };
            float totalTravel = 0f;
            for (int stage = 0; stage < 3; stage++)
            {
                Quaternion facing = Quaternion.Euler(0f, 37f + stage * 83f, 0f);
                actor.transform.rotation = facing;
                Vector3 start = actor.transform.position;
                var startedAt = Time.time;
                Call(attack, "StartComboStage", stage);
                float peakTurn = await WaitForAttack(attack, movement, actor.transform, facing, false);
                AssertFacing(actor.transform, visual, facing, originalVisualRotation, "single hit " + (stage + 1));
                float travel = Vector3.ProjectOnPlane(actor.transform.position - start, Vector3.up).magnitude;
                if (Mathf.Abs(travel - referenceTravel[stage]) > referenceTravel[stage] * 0.15f)
                    throw new InvalidOperationException($"Hit {stage + 1} changed authored travel: expected about {referenceTravel[stage]:F3}m, got {travel:F3}m.");
                totalTravel += travel;
                report.AppendLine($"Hit {stage + 1}: peak heading error={peakTurn:F4}deg, final drift={Quaternion.Angle(facing, actor.transform.rotation):F4}deg, authored travel={travel:F3}m, attack+recovery={Time.time - startedAt:F3}s");
            }
            if (totalTravel < 0.05f) throw new InvalidOperationException("Authored attack movement was lost.");

            Quaternion comboFacing = Quaternion.Euler(0f, -68f, 0f);
            actor.transform.rotation = comboFacing;
            Call(attack, "StartComboStage", 0);
            float comboTurn = await WaitForAttack(attack, movement, actor.transform, comboFacing, true);
            AssertFacing(actor.transform, visual, comboFacing, originalVisualRotation, "full three-hit combo");
            report.AppendLine($"Full combo: one shared starting heading, peak heading error={comboTurn:F4}deg, final drift={Quaternion.Angle(comboFacing, actor.transform.rotation):F4}deg");
            if (renderedThirdSamples == 0) throw new InvalidOperationException("No Game-camera render frames were available to verify third-hit facing.");
            if (renderedThirdError != null) throw new InvalidOperationException(renderedThirdError);
            report.AppendLine($"Third-hit rendered pose: {renderedThirdSamples} samples, peak face yaw={renderedThirdPeakYaw:F2}deg; face heading remains aligned with the attack.");

            Quaternion heavyFacing = Quaternion.Euler(0f, 126f, 0f);
            actor.transform.rotation = heavyFacing;
            Call(attack, "PlayAttack", true, attack.damage, 0f);
            await WaitForAttack(attack, movement, actor.transform, heavyFacing, false);
            AssertFacing(actor.transform, visual, heavyFacing, originalVisualRotation, "heavy attack");
            report.AppendLine("Heavy attack: heading restored.");

            actor.transform.rotation = Quaternion.Euler(0f, 51f, 0f);
            Quaternion chargeFacing = actor.transform.rotation;
            Call(attack, "StartCharge");
            actor.transform.rotation *= Quaternion.Euler(0f, 45f, 0f);
            Call(attack, "CancelCharge");
            await Task.Delay(250);
            AssertFacing(actor.transform, visual, chargeFacing, originalVisualRotation, "cancelled charge");
            report.AppendLine("Cancelled charge: heading restored.");

            actor.transform.rotation = Quaternion.Euler(0f, -111f, 0f);
            Quaternion interruptedFacing = actor.transform.rotation;
            Call(attack, "StartComboStage", 0);
            await Task.Delay(450);
            attack.enabled = false;
            if (Quaternion.Angle(actor.transform.rotation, interruptedFacing) > 0.1f)
                throw new InvalidOperationException("Disabling combat left the actor rotated.");
            report.AppendLine("Interrupted/disabled attack: heading restored immediately.");

            // Also cover a new click arriving during recovery rather than after it.
            actor.transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            movement.BeginAttackFacing();
            actor.transform.rotation = Quaternion.Euler(8f, 154f, 12f);
            movement.EndAttackFacing();
            movement.BeginAttackFacing();
            movement.EndAttackFacing(true);
            if (Quaternion.Angle(actor.transform.rotation, Quaternion.Euler(0f, 24f, 0f)) > 0.1f)
                throw new InvalidOperationException("Immediate follow-up attack accumulated recovery drift.");
            movement.BeginAttackFacing();
            actor.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
            movement.EndAttackFacing();
            Call(movement, "UpdateAttackFacingRecovery", true);
            if (Get<bool>(movement, "recoveringAttackFacing"))
                throw new InvalidOperationException("Recovery ignored player movement intent.");
            report.AppendLine("Rapid restart and movement takeover: passed.");
            return report.ToString();
        }
        finally
        {
            if (inspectRenderedPose != null) RenderPipelineManager.beginCameraRendering -= inspectRenderedPose;
            UnityEngine.Object.DestroyImmediate(holder);
            UnityEngine.Object.DestroyImmediate(floor);
            Time.timeScale = originalTimeScale;
            Cursor.lockState = originalCursor;
            Cursor.visible = originalCursorVisible;
        }
    }

    static async Task<float> WaitForAttack(SahurAttack attack, ThirdPersonPlayerController movement, Transform actor, Quaternion facing, bool chain)
    {
        var deadline = DateTime.UtcNow.AddSeconds(14);
        float peakTurn = 0f;
        var visitedStages = new HashSet<int>();
        while (attack.IsCombatMotionActive && DateTime.UtcNow < deadline)
        {
            peakTurn = Mathf.Max(peakTurn, Quaternion.Angle(actor.rotation, facing));
            if (attack.CurrentComboStage >= 0)
            {
                // Check every sampled attack frame, including the initial
                // crossfade. A final-heading-only assertion misses startup turns.
                if (Quaternion.Angle(actor.rotation, facing) > 0.1f)
                    throw new InvalidOperationException("Combo changed heading during its animation: " + actor.eulerAngles);
                var current = attack.animator.GetCurrentAnimatorStateInfo(0);
                if (current.IsName("Combo " + (attack.CurrentComboStage + 1)) && Mathf.Abs(current.speed - 1.25f) > 0.001f)
                    throw new InvalidOperationException("The combo did not use the requested 1.25x playback speed.");
            }
            if (Get<bool>(movement, "hasAttackFacing") && Quaternion.Angle(Get<Quaternion>(movement, "attackFacing"), facing) > 0.1f)
                throw new InvalidOperationException("A combo stage replaced the initial heading.");
            if (chain && attack.CurrentComboStage >= 0)
            {
                visitedStages.Add(attack.CurrentComboStage);
                float phase = attack.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                if (attack.CurrentComboStage < 2 && phase > 0.2f && phase < 0.9f)
                    Set(attack, "comboContinueQueued", true);
            }
            await Task.Delay(15);
        }
        if (attack.IsCombatMotionActive) throw new InvalidOperationException("Attack did not return to locomotion.");
        if (chain && !new[] { 0, 1, 2 }.All(visitedStages.Contains))
            throw new InvalidOperationException("The full combo did not visit all three stages.");
        await Task.Delay(250);
        return peakTurn;
    }

    static void AssertFacing(Transform actor, Transform visual, Quaternion expected, Quaternion expectedVisual, string label)
    {
        if (Quaternion.Angle(actor.rotation, expected) > 0.1f || Vector3.Dot(actor.up, Vector3.up) < 0.9999f)
            throw new InvalidOperationException(label + " left the actor turned/tilted: " + actor.eulerAngles);
        if (Quaternion.Angle(visual.localRotation, expectedVisual) > 0.1f)
            throw new InvalidOperationException(label + " left the visual offset rotated.");
    }
}
