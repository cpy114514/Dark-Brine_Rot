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

/// <summary>Same-frame, same-blend diagnostic against cloned baked-root clips.</summary>
public static class CompareSahurSourceTravel
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);
    static string V(Vector3 value) => $"({value.x:F4}, {value.y:F4}, {value.z:F4})";

    public static async Task<string> Run()
    {
        if (!EditorApplication.isPlaying || PauseSettingsMenu.IsOpen)
            throw new InvalidOperationException("Enter Play Mode with settings closed.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        if (prefab == null) throw new InvalidOperationException("Sahur prefab is missing.");
        var disabled = new List<Behaviour>();
        var clonedClips = new List<AnimationClip>();
        AnimatorOverrideController overrideController = null;
        GameObject holder = null, floor = null;
        Action<ScriptableRenderContext, Camera> inspect = null;
        Camera mainCamera = Camera.main;
        Vector3 savedCameraPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        Quaternion savedCameraRotation = mainCamera != null ? mainCamera.transform.rotation : Quaternion.identity;
        var savedCursor = Cursor.lockState;
        bool savedVisible = Cursor.visible;
        float savedTimeScale = Time.timeScale;
        var report = new StringBuilder();
        try
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
                if (component.enabled) { component.enabled = false; disabled.Add(component); }
            foreach (var component in UnityEngine.Object.FindObjectsByType<SahurAttack>(FindObjectsSortMode.None))
                if (component.enabled) { component.enabled = false; disabled.Add(component); }
            Time.timeScale = 1f;
            holder = new GameObject("Temporary same-blend source comparison");
            holder.SetActive(false);
            holder.transform.position = new Vector3(2400f, 300f, 2400f);
            floor = new GameObject("Temporary scaled comparison floor");
            floor.transform.position = holder.transform.position - Vector3.up * 0.5f;
            floor.AddComponent<BoxCollider>().size = new Vector3(180f, 1f, 180f);
            var actors = new GameObject[2];
            var movement = new ThirdPersonPlayerController[2];
            var attack = new SahurAttack[2];
            for (int i = 0; i < 2; i++)
            {
                actors[i] = UnityEngine.Object.Instantiate(prefab, holder.transform);
                actors[i].name = i == 0 ? "Natural extracted-yaw actor" : "Baked-root native-travel actor";
                actors[i].transform.localPosition = new Vector3(i == 0 ? -25f : 25f, 3f, 0f);
                movement[i] = actors[i].GetComponent<ThirdPersonPlayerController>();
                attack[i] = actors[i].GetComponent<SahurAttack>();
                movement[i].snapSpawnToIslandSurface = false;
                movement[i].waterSplashes = false;
                movement[i].cameraCollision = false;
                foreach (var input in actors[i].GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
            }
            movement[1].comboSourceX = null;
            movement[1].comboSourceZ = null;
            holder.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            await Task.Delay(400);
            var animators = new[] { attack[0].animator, attack[1].animator };
            var sourceClips = new AnimationClip[3];
            var sourceX = new AnimationCurve[3];
            var sourceZ = new AnimationCurve[3];
            for (int stage = 0; stage < 3; stage++)
            {
                sourceClips[stage] = animators[0].runtimeAnimatorController.animationClips
                    .First(clip => clip.name == "SahurSwordCombo" + (stage + 1));
                sourceX[stage] = AnimationUtility.GetEditorCurve(sourceClips[stage],
                    EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.x"));
                sourceZ[stage] = AnimationUtility.GetEditorCurve(sourceClips[stage],
                    EditorCurveBinding.FloatCurve("", typeof(Animator), "RootT.z"));
                if (sourceX[stage] == null || sourceZ[stage] == null)
                    throw new InvalidOperationException("RootT.x/z curves are missing from combo slice " + (stage + 1));
            }
            float rootCurveScale = animators[0].humanScale * animators[0].transform.lossyScale.y;
            report.AppendLine($"Raw RootT scale: humanScale={animators[0].humanScale:F6}, world scale Y={animators[0].transform.lossyScale.y:F6}, product={rootCurveScale:F6}; product / known pure-motion ratio 1.78={rootCurveScale / 1.78f:F6}.");
            for (int stage = 0; stage < 3; stage++)
            {
                float duration = sourceClips[stage].length;
                var pureDelta = new Vector3(sourceX[stage].Evaluate(duration) - sourceX[stage].Evaluate(0f),
                    0f, sourceZ[stage].Evaluate(duration) - sourceZ[stage].Evaluate(0f));
                report.AppendLine($"  Pure RootT hit {stage + 1}: raw {V(pureDelta)}, scaled {V(pureDelta * rootCurveScale)}m (Y intentionally zero).");
            }
            for (int i = 0; i < 2; i++)
            {
                var capsule = actors[i].GetComponent<CharacterController>();
                float soleOffset = (capsule.center.y - capsule.height * 0.5f) * actors[i].transform.lossyScale.y;
                var position = actors[i].transform.position;
                position.y = holder.transform.position.y + 0.025f - soleOffset;
                capsule.enabled = false;
                actors[i].transform.position = position;
                capsule.enabled = true;
                var dummyCameraObject = new GameObject("Disabled isolated actor camera " + i);
                dummyCameraObject.transform.SetParent(holder.transform, false);
                var dummyCamera = dummyCameraObject.AddComponent<Camera>();
                dummyCamera.enabled = false;
                Set(movement[i], "playerCamera", dummyCamera);
            }
            Physics.SyncTransforms();

            overrideController = new AnimatorOverrideController(animators[0].runtimeAnimatorController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);
            int replaced = 0;
            for (int index = 0; index < overrides.Count; index++)
            {
                AnimationClip original = overrides[index].Key;
                if (!original.name.StartsWith("SahurSwordCombo")) continue;
                var clone = UnityEngine.Object.Instantiate(original);
                clone.name = original.name + "_BakedYawDiagnostic";
                clone.hideFlags = HideFlags.HideAndDontSave;
                var settings = AnimationUtility.GetAnimationClipSettings(clone);
                settings.keepOriginalOrientation = true;
                settings.loopBlendOrientation = true;
                AnimationUtility.SetAnimationClipSettings(clone, settings);
                clonedClips.Add(clone);
                overrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(original, clone);
                replaced++;
            }
            if (replaced != 3) throw new InvalidOperationException("Expected three source combo clips in the override controller.");
            overrideController.ApplyOverrides(overrides);
            animators[1].runtimeAnimatorController = overrideController;
            animators[1].Rebind();
            for (int i = 0; i < 2; i++)
            {
                animators[i].Play("Locomotion", 0, 0f);
                animators[i].Update(0f);
            }
            await Task.Delay(350);
            var hips = animators[1].GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) throw new InvalidOperationException("Reference humanoid hips are missing.");
            Vector3 hipAxis = hips.InverseTransformDirection(actors[1].transform.forward);
            Quaternion[] visualBase = animators.Select(animator => animator.transform.localRotation).ToArray();
            Quaternion heading = Quaternion.identity;
            Vector3[] startPositions = null;
            bool observing = false;
            int lastRenderFrame = -1, sourceSamples = 0;
            float peakBakedRootDelta = 0f, peakBakedHipsYaw = 0f;
            float peakNaturalVisualOffset = 0f, peakReferenceVisualOffset = 0f;
            float peakRootDrift = 0f, peakPosePhaseDifference = 0f;
            float blendPathDifference = 0f, bodyPathDifference = 0f;
            Vector3 rawCurveTravel = Vector3.zero;
            var priorPhaseByState = new Dictionary<int, float>();
            int rawCurveSamples = 0;

            Vector3 IntegrateRawState(AnimatorStateInfo state, float weight)
            {
                int sourceStage = -1;
                for (int stage = 0; stage < 3; stage++)
                    if (state.IsName("Combo " + (stage + 1))) { sourceStage = stage; break; }
                if (sourceStage < 0) return Vector3.zero;
                float phase = Mathf.Clamp01(state.normalizedTime);
                float previous;
                if (!priorPhaseByState.TryGetValue(state.fullPathHash, out previous) || phase < previous - 0.0001f)
                    previous = 0f;
                // Track phase even at weight zero, so entering a state at zero
                // blend weight cannot re-apply its elapsed source motion later.
                priorPhaseByState[state.fullPathHash] = phase;
                float duration = sourceClips[sourceStage].length;
                float fromTime = previous * duration;
                float toTime = phase * duration;
                return new Vector3(sourceX[sourceStage].Evaluate(toTime) - sourceX[sourceStage].Evaluate(fromTime),
                    0f, sourceZ[sourceStage].Evaluate(toTime) - sourceZ[sourceStage].Evaluate(fromTime)) *
                    (Mathf.Clamp01(weight) * rootCurveScale);
            }

            inspect = (context, camera) =>
            {
                if (!observing || camera.cameraType != CameraType.Game || lastRenderFrame == Time.frameCount) return;
                lastRenderFrame = Time.frameCount;
                for (int i = 0; i < 2; i++)
                    peakRootDrift = Mathf.Max(peakRootDrift, Quaternion.Angle(heading, actors[i].transform.rotation));
                peakNaturalVisualOffset = Mathf.Max(peakNaturalVisualOffset,
                    Quaternion.Angle(visualBase[0], animators[0].transform.localRotation));
                peakReferenceVisualOffset = Mathf.Max(peakReferenceVisualOffset,
                    Quaternion.Angle(visualBase[1], animators[1].transform.localRotation));
                var naturalState = animators[0].GetCurrentAnimatorStateInfo(0);
                var referenceState = animators[1].GetCurrentAnimatorStateInfo(0);
                // Integrate source translation directly in its authored axes.
                // This diagnostic intentionally does not consume deltaPosition
                // or deltaRotation and uses end-of-rendered-frame blend weights.
                bool transitioning = animators[0].IsInTransition(0);
                float nextWeight = transitioning
                    ? Mathf.Clamp01(animators[0].GetAnimatorTransitionInfo(0).normalizedTime) : 0f;
                rawCurveTravel += IntegrateRawState(naturalState, 1f - nextWeight);
                if (transitioning)
                    rawCurveTravel += IntegrateRawState(animators[0].GetNextAnimatorStateInfo(0), nextWeight);
                rawCurveSamples++;
                if (naturalState.fullPathHash == referenceState.fullPathHash)
                    peakPosePhaseDifference = Mathf.Max(peakPosePhaseDifference,
                        Mathf.Abs(naturalState.normalizedTime - referenceState.normalizedTime));
                if (attack[1].IsGroundComboActive && !animators[1].IsInTransition(0) &&
                    referenceState.IsName("Combo " + (attack[1].CurrentComboStage + 1)))
                {
                    sourceSamples++;
                    peakBakedRootDelta = Mathf.Max(peakBakedRootDelta,
                        Quaternion.Angle(Quaternion.identity, animators[1].deltaRotation));
                    float hipsYaw = Mathf.Abs(Vector3.SignedAngle(actors[1].transform.forward,
                        Vector3.ProjectOnPlane(hips.TransformDirection(hipAxis), Vector3.up), Vector3.up));
                    peakBakedHipsYaw = Mathf.Max(peakBakedHipsYaw, hipsYaw);
                }
                Vector3 difference = (actors[0].transform.position - startPositions[0]) -
                                     (actors[1].transform.position - startPositions[1]);
                float planarDifference = Vector3.ProjectOnPlane(difference, Vector3.up).magnitude;
                if (animators[0].IsInTransition(0) || animators[1].IsInTransition(0))
                    blendPathDifference = Mathf.Max(blendPathDifference, planarDifference);
                else bodyPathDifference = Mathf.Max(bodyPathDifference, planarDifference);
            };
            RenderPipelineManager.beginCameraRendering += inspect;

            async Task Compare(int stage, bool chain)
            {
                heading = Quaternion.Euler(0f, chain ? -68f : 37f + stage * 83f, 0f);
                foreach (var actor in actors) actor.transform.rotation = heading;
                startPositions = actors.Select(actor => actor.transform.position).ToArray();
                blendPathDifference = bodyPathDifference = 0f;
                rawCurveTravel = Vector3.zero;
                rawCurveSamples = 0;
                priorPhaseByState.Clear();
                observing = true;
                // No await between starts: both Animators see the same frame,
                // same source phase, same clip-to-idle transition durations.
                Call(attack[0], "StartComboStage", stage);
                Call(attack[1], "StartComboStage", stage);
                var deadline = DateTime.UtcNow.AddSeconds(14);
                var visited = new[] { new HashSet<int>(), new HashSet<int>() };
                while (attack.Any(item => item.IsCombatMotionActive) && DateTime.UtcNow < deadline)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (attack[i].CurrentComboStage < 0) continue;
                        visited[i].Add(attack[i].CurrentComboStage);
                        float phase = animators[i].GetCurrentAnimatorStateInfo(0).normalizedTime;
                        if (chain && attack[i].CurrentComboStage < 2 && phase > 0.2f && phase < 0.9f)
                            Set(attack[i], "comboContinueQueued", true);
                    }
                    await Task.Delay(15);
                }
                if (attack.Any(item => item.IsCombatMotionActive)) throw new InvalidOperationException("Comparison attack timed out.");
                await Task.Delay(300);
                observing = false;
                Vector3 natural = Quaternion.Inverse(heading) * (actors[0].transform.position - startPositions[0]);
                Vector3 baked = Quaternion.Inverse(heading) * (actors[1].transform.position - startPositions[1]);
                report.AppendLine($"{(chain ? "Full chain" : "Single hit " + (stage + 1))}: natural XYZ {V(natural)}m; baked-native XYZ {V(baked)}m; signed difference {V(natural - baked)}m.");
                // Raw curves are already in the common attack-start local frame;
                // actor heading is only used to convert the measured instances.
                Vector3 planarBaked = new Vector3(baked.x, 0f, baked.z);
                report.AppendLine($"  Raw curve XYZ {V(rawCurveTravel)}m vs baked-native XZ {V(planarBaked)}m; signed difference {V(rawCurveTravel - planarBaked)}m; {rawCurveSamples} rendered samples.");
                report.AppendLine($"  Peak planar path difference: during blends {blendPathDifference:F4}m, outside blends {bodyPathDifference:F4}m; visited {visited[0].Count}/{visited[1].Count} stages.");
            }
            for (int stage = 0; stage < 3; stage++) await Compare(stage, false);
            await Compare(0, true);
            bool bakeWorks = sourceSamples > 0 && peakBakedRootDelta < 0.2f && peakBakedHipsYaw > 10f;
            report.AppendLine($"Cloned bake validation {(bakeWorks ? "PASSED" : "FAILED")}: {sourceSamples} non-blend samples, peak reference root delta {peakBakedRootDelta:F4}deg, peak reference hips yaw {peakBakedHipsYaw:F2}deg.");
            report.AppendLine($"Visual parent offset natural/reference {peakNaturalVisualOffset:F4}/{peakReferenceVisualOffset:F4}deg; control-root drift {peakRootDrift:F4}deg; matching-state phase difference {peakPosePhaseDifference:F6}.");
            report.AppendLine("Raw RootT integration uses current/next normalized phase increments and transition.normalizedTime weights; Y is zero and blend quadrature is rendered-frame resolution, not an asserted exact Animator baseline.");
            if (!bakeWorks)
                report.AppendLine("WARNING: changing cloned AnimationClip settings did not recreate a usable baked-root reference. Do not treat its positional output as an original-import baseline.");
            return report.ToString();
        }
        finally
        {
            if (inspect != null) RenderPipelineManager.beginCameraRendering -= inspect;
            if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (overrideController != null) UnityEngine.Object.DestroyImmediate(overrideController);
            foreach (var clip in clonedClips) if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            foreach (var behaviour in disabled) if (behaviour != null) behaviour.enabled = true;
            if (mainCamera != null) mainCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
            Time.timeScale = savedTimeScale;
            Cursor.lockState = savedCursor;
            Cursor.visible = savedVisible;
        }
    }
}
