using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Ephemeral Play Mode check. Unlike VerifySahurFacing, a natural attack is
/// allowed to twist its chest/head; only the actor's control heading is fixed.
/// </summary>
public static class VerifySahurNaturalCombo
{
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly HumanBodyBones[] PoseBones = {
        HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.Head,
        HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
        HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm
    };

    static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    static string Format(Vector3 value) => $"({value.x:F3}, {value.y:F3}, {value.z:F3})";

    public static Task<string> Run() => RunConfigured("report", "", 0.35f);

    /// <param name="sourceMode">
    /// report: report signed travel only; fixed/accumulated: compare with an
    /// independent no-collision source Animator in the selected root frame.
    /// </param>
    /// <param name="signedReferenceCsv">
    /// Optional measured vectors, e.g. "x,y,z;x,y,z;x,y,z", in the attack's
    /// initial local frame. Overrides sourceMode integration when provided.
    /// </param>
    public static async Task<string> RunConfigured(string sourceMode, string signedReferenceCsv,
        float translationToleranceMeters)
    {
        Check(EditorApplication.isPlaying && !PauseSettingsMenu.IsOpen,
            "Enter Play Mode with settings closed before testing natural combo motion.");
        Check(sourceMode == "report" || sourceMode == "fixed" || sourceMode == "accumulated",
            "sourceMode must be report, fixed, or accumulated.");
        Check(translationToleranceMeters > 0f, "Specify a positive signed-translation tolerance.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null, "Sahur prefab is missing.");
        var disabled = new List<Behaviour>();
        GameObject holder = null;
        GameObject floor = null;
        Action<ScriptableRenderContext, Camera> inspect = null;
        float savedTimeScale = Time.timeScale;
        var savedCursor = Cursor.lockState;
        bool savedVisible = Cursor.visible;
        Camera mainCamera = Camera.main;
        Vector3 savedCameraPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        Quaternion savedCameraRotation = mainCamera != null ? mainCamera.transform.rotation : Quaternion.identity;
        var report = new StringBuilder();
        try
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
                if (component.enabled) { component.enabled = false; disabled.Add(component); }
            foreach (var component in UnityEngine.Object.FindObjectsByType<SahurAttack>(FindObjectsSortMode.None))
                if (component.enabled) { component.enabled = false; disabled.Add(component); }
            Time.timeScale = 1f;
            holder = new GameObject("Temporary Sahur natural combo regression");
            holder.SetActive(false);
            holder.transform.position = new Vector3(2000f, 300f, 2000f);
            floor = new GameObject("Temporary natural combo floor");
            floor.transform.position = holder.transform.position - Vector3.up * 0.5f;
            floor.AddComponent<BoxCollider>().size = new Vector3(150f, 1f, 150f);
            var actor = UnityEngine.Object.Instantiate(prefab, holder.transform);
            actor.transform.localPosition = Vector3.up * 3f;
            var movement = actor.GetComponent<ThirdPersonPlayerController>();
            var attack = actor.GetComponent<SahurAttack>();
            movement.snapSpawnToIslandSurface = false;
            movement.waterSplashes = false;
            movement.cameraCollision = false;
            foreach (var input in actor.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;

            var reference = UnityEngine.Object.Instantiate(prefab, holder.transform);
            reference.name = "Independent no-collision source Animator";
            reference.transform.localPosition = Vector3.right * 40f;
            foreach (var behaviour in reference.GetComponentsInChildren<Behaviour>(true))
                if (!(behaviour is Animator)) behaviour.enabled = false;
            foreach (var collider in reference.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in reference.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            holder.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            await Task.Delay(400);

            var capsule = actor.GetComponent<CharacterController>();
            float soleOffset = (capsule.center.y - capsule.height * 0.5f) * actor.transform.lossyScale.y;
            capsule.enabled = false;
            actor.transform.position = holder.transform.position + Vector3.up * (0.025f - soleOffset);
            capsule.enabled = true;
            Physics.SyncTransforms();
            await Task.Delay(350);
            Check(capsule.isGrounded, "Scaled test actor failed to settle on the floor.");

            Animator animator = attack.animator;
            Transform visual = animator.transform;
            Quaternion visualBase = visual.localRotation;
            Animator referenceAnimator = reference.transform.Find("Pbr Sahur Visual").GetComponent<Animator>();
            referenceAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
            referenceAnimator.avatar = animator.avatar;
            referenceAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            referenceAnimator.applyRootMotion = true;
            referenceAnimator.Rebind();
            referenceAnimator.Play("Locomotion", 0, 0f);
            referenceAnimator.Update(0f);
            referenceAnimator.enabled = false;
            Quaternion referenceVisualBase = referenceAnimator.transform.localRotation;
            Vector3 referenceVisualPosition = referenceAnimator.transform.localPosition;
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Check(head != null && chest != null, "Humanoid head/chest bones are missing.");
            Vector3 headAxis = head.InverseTransformDirection(actor.transform.forward);
            Vector3 chestAxis = chest.InverseTransformDirection(actor.transform.forward);
            Vector3[] expected = ParseReferences(signedReferenceCsv);
            if (expected == null && sourceMode != "report")
            {
                expected = new Vector3[3];
                for (int stage = 0; stage < 3; stage++)
                    expected[stage] = await IntegrateSource(referenceAnimator, referenceVisualBase,
                        referenceVisualPosition, stage, sourceMode == "accumulated");
                report.AppendLine("No-collision source frame: " + sourceMode + "; XYZ references: " +
                    string.Join("; ", expected.Select(Format)));
            }

            Quaternion expectedHeading = actor.transform.rotation;
            int renderedSamples = 0;
            int comparedPoses = 0;
            int lastFrame = -1;
            float peakRootYaw = 0f;
            float peakVisualOffset = 0f;
            float peakSourcePoseError = 0f;
            float peakRecoveryVisualStep = 0f;
            float minHeadYaw = float.PositiveInfinity, maxHeadYaw = float.NegativeInfinity;
            float minChestYaw = float.PositiveInfinity, maxChestYaw = float.NegativeInfinity;
            Quaternion previousVisual = visual.localRotation;
            bool observing = false;
            string failure = null;
            inspect = (context, camera) =>
            {
                if (!observing || failure != null || camera.cameraType != CameraType.Game || lastFrame == Time.frameCount) return;
                lastFrame = Time.frameCount;
                try
                {
                    renderedSamples++;
                    float rootError = Quaternion.Angle(actor.transform.rotation, expectedHeading);
                    float visualError = Quaternion.Angle(visual.localRotation, visualBase);
                    peakRootYaw = Mathf.Max(peakRootYaw, rootError);
                    peakVisualOffset = Mathf.Max(peakVisualOffset, visualError);
                    Check(rootError <= 0.15f, "Control-root heading changed during attack/recovery.");
                    Check(visualError <= 0.15f, "The visual parent was rotated to cancel animated body/head motion.");
                    if (!attack.IsGroundComboActive)
                        peakRecoveryVisualStep = Mathf.Max(peakRecoveryVisualStep,
                            Quaternion.Angle(previousVisual, visual.localRotation));
                    previousVisual = visual.localRotation;
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (!attack.IsGroundComboActive || animator.IsInTransition(0) || state.normalizedTime < 0.08f ||
                        state.normalizedTime > 0.96f || !state.IsName("Combo " + (attack.CurrentComboStage + 1))) return;

                    float headYaw = Vector3.SignedAngle(actor.transform.forward,
                        Vector3.ProjectOnPlane(head.TransformDirection(headAxis), Vector3.up), Vector3.up);
                    float chestYaw = Vector3.SignedAngle(actor.transform.forward,
                        Vector3.ProjectOnPlane(chest.TransformDirection(chestAxis), Vector3.up), Vector3.up);
                    minHeadYaw = Mathf.Min(minHeadYaw, headYaw); maxHeadYaw = Mathf.Max(maxHeadYaw, headYaw);
                    minChestYaw = Mathf.Min(minChestYaw, chestYaw); maxChestYaw = Mathf.Max(maxChestYaw, chestYaw);

                    referenceAnimator.transform.localPosition = referenceVisualPosition;
                    referenceAnimator.transform.localRotation = referenceVisualBase;
                    referenceAnimator.Play(state.fullPathHash, 0, state.normalizedTime);
                    referenceAnimator.Update(0f);
                    foreach (HumanBodyBones boneId in PoseBones)
                    {
                        Transform actualBone = animator.GetBoneTransform(boneId);
                        Transform sourceBone = referenceAnimator.GetBoneTransform(boneId);
                        if (actualBone == null || sourceBone == null) continue;
                        Quaternion actualLocal = Quaternion.Inverse(visual.rotation) * actualBone.rotation;
                        Quaternion sourceLocal = Quaternion.Inverse(referenceAnimator.transform.rotation) * sourceBone.rotation;
                        float poseError = Quaternion.Angle(actualLocal, sourceLocal);
                        peakSourcePoseError = Mathf.Max(peakSourcePoseError, poseError);
                        Check(poseError <= 3f, $"{boneId} no longer matches the natural source pose ({poseError:F2}deg).");
                    }
                    comparedPoses++;
                }
                catch (Exception exception) { failure = exception.Message; }
            };
            RenderPipelineManager.beginCameraRendering += inspect;

            async Task WaitForAttack(bool chain)
            {
                var deadline = DateTime.UtcNow.AddSeconds(14);
                var visited = new HashSet<int>();
                while (attack.IsCombatMotionActive && DateTime.UtcNow < deadline)
                {
                    Check(failure == null, failure);
                    if (attack.CurrentComboStage >= 0)
                    {
                        visited.Add(attack.CurrentComboStage);
                        var state = animator.GetCurrentAnimatorStateInfo(0);
                        if (chain && attack.CurrentComboStage < 2 && state.normalizedTime > 0.2f && state.normalizedTime < 0.9f)
                            Set(attack, "comboContinueQueued", true);
                    }
                    await Task.Delay(15);
                }
                Check(!attack.IsCombatMotionActive, "Attack did not return to locomotion.");
                if (chain) Check(visited.Count == 3, "Queued full combo failed to visit all three stages.");
                await Task.Delay(300);
                Check(failure == null, failure);
                Check(Quaternion.Angle(visual.localRotation, visualBase) <= 0.15f,
                    "Recovery left the visual parent offset.");
            }

            for (int stage = 0; stage < 3; stage++)
            {
                expectedHeading = Quaternion.Euler(0f, 37f + stage * 83f, 0f);
                actor.transform.rotation = expectedHeading;
                Vector3 start = actor.transform.position;
                previousVisual = visual.localRotation;
                observing = true;
                Call(attack, "StartComboStage", stage);
                await WaitForAttack(false);
                observing = false;
                Vector3 signedTravel = Quaternion.Inverse(expectedHeading) * (actor.transform.position - start);
                Check(Vector3.ProjectOnPlane(signedTravel, Vector3.up).magnitude > 0.1f,
                    "Source attack translation was lost.");
                if (expected != null)
                {
                    Vector3 error = signedTravel - expected[stage];
                    Check(Mathf.Abs(error.x) <= translationToleranceMeters && Mathf.Abs(error.z) <= translationToleranceMeters,
                        $"Hit {stage + 1} changed signed X/Z travel: actual {Format(signedTravel)}, reference {Format(expected[stage])}.");
                    // Floor support legitimately corrects initial contact/skin Y.
                    Check(Mathf.Abs(error.y) <= Mathf.Max(0.15f, translationToleranceMeters),
                        $"Hit {stage + 1} changed source vertical travel: {Format(signedTravel)} vs {Format(expected[stage])}.");
                }
                report.AppendLine($"Hit {stage + 1}: signed local XYZ travel {Format(signedTravel)}m; stable actor/visual heading.");
            }
            expectedHeading = Quaternion.Euler(0f, -68f, 0f);
            actor.transform.rotation = expectedHeading;
            previousVisual = visual.localRotation;
            observing = true;
            Call(attack, "StartComboStage", 0);
            await WaitForAttack(true);
            observing = false;
            Check(renderedSamples > 0 && comparedPoses >= 6, "Too few rendered/source-pose samples were collected.");
            float headRange = maxHeadYaw - minHeadYaw;
            float chestRange = maxChestYaw - minChestYaw;
            Check(headRange > 8f && chestRange > 8f,
                $"Natural head/chest twist was flattened: head range {headRange:F2}deg, chest range {chestRange:F2}deg.");
            Check(peakRecoveryVisualStep <= 0.15f, "Visual-parent heading popped on recovery.");
            report.AppendLine($"Full queued combo: passed; {renderedSamples} rendered frames, {comparedPoses} independent source-pose comparisons.");
            report.AppendLine($"Peak control heading drift {peakRootYaw:F4}deg; visual-parent correction {peakVisualOffset:F4}deg; recovery parent pop {peakRecoveryVisualStep:F4}deg.");
            report.AppendLine($"Natural twist ranges: head {headRange:F2}deg, chest {chestRange:F2}deg; peak source bone-pose error {peakSourcePoseError:F3}deg.");
            return report.ToString();
        }
        finally
        {
            if (inspect != null) RenderPipelineManager.beginCameraRendering -= inspect;
            if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            foreach (var behaviour in disabled) if (behaviour != null) behaviour.enabled = true;
            if (mainCamera != null) mainCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
            Time.timeScale = savedTimeScale;
            Cursor.lockState = savedCursor;
            Cursor.visible = savedVisible;
        }
    }

    static Vector3[] ParseReferences(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        string[] items = csv.Split(';');
        Check(items.Length == 3, "Expected three signed X/Y/Z reference vectors separated by semicolons.");
        return items.Select(item =>
        {
            float[] xyz = item.Split(',').Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            Check(xyz.Length == 3, "Each reference vector must have X,Y,Z components.");
            return new Vector3(xyz[0], xyz[1], xyz[2]);
        }).ToArray();
    }

    static async Task<Vector3> IntegrateSource(Animator animator, Quaternion visualBase,
        Vector3 visualPosition, int stage, bool accumulateYaw)
    {
        animator.transform.parent.rotation = Quaternion.identity;
        animator.transform.localPosition = visualPosition;
        animator.transform.localRotation = visualBase;
        animator.Play("Combo " + (stage + 1), 0, 0f);
        animator.Update(0f);
        var state = animator.GetCurrentAnimatorStateInfo(0);
        AnimationClip sourceClip = animator.runtimeAnimatorController.animationClips
            .FirstOrDefault(clip => clip.name == "SahurSwordCombo" + (stage + 1));
        Check(sourceClip != null, "Source combo slice is missing from the reference Animator.");
        // AnimatorStateInfo.length can already reflect effective state timing;
        // use the clip's source duration to avoid applying state speed twice.
        float duration = sourceClip.length / Mathf.Max(0.01f, state.speed * state.speedMultiplier);
        int steps = Mathf.Max(1, Mathf.CeilToInt(duration * 120f));
        float deltaTime = duration / steps;
        Vector3 travel = Vector3.zero;
        Quaternion sourceYaw = Quaternion.identity;
        Quaternion referenceRotation = animator.transform.rotation;
        for (int index = 0; index < steps; index++)
        {
            animator.transform.localPosition = visualPosition;
            animator.transform.localRotation = visualBase;
            animator.Update(deltaTime);
            Vector3 sourceDelta = animator.deltaPosition;
            if (accumulateYaw)
                sourceDelta = referenceRotation * sourceYaw * Quaternion.Inverse(referenceRotation) * sourceDelta;
            travel += sourceDelta;
            Vector3 rotatedForward = Vector3.ProjectOnPlane(animator.deltaRotation * Vector3.forward, Vector3.up);
            if (rotatedForward.sqrMagnitude > 0.0001f)
                sourceYaw *= Quaternion.LookRotation(rotatedForward, Vector3.up);
            if (index % 12 == 0) await Task.Yield();
        }
        animator.transform.localPosition = visualPosition;
        animator.transform.localRotation = visualBase;
        animator.Play("Locomotion", 0, 0f);
        animator.Update(0f);
        return travel;
    }
}
