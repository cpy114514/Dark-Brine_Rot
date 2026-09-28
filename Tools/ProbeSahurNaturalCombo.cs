using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ProbeSahurNaturalCombo
{
    const string Root = "Assets/Game/Prefabs/Characters/Sahur/";
    public static string DumpRootCurves()
    {
        var report = new StringBuilder();
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(Root + "Animations/Source/SwordAndShieldSlash_ThreeHit.fbx").OfType<AnimationClip>())
        {
            report.AppendLine(clip.name);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.propertyName.StartsWith("Root") || b.propertyName.StartsWith("Motion")))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                report.AppendLine($"{binding.path}:{binding.propertyName} start={curve.Evaluate(0f):F5}, mid={curve.Evaluate(clip.length * .5f):F5}, end={curve.Evaluate(clip.length):F5}");
            }
        }
        return report.ToString();
    }
    public static string ExtractRootRotation()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "Animations/Source/SwordAndShieldSlash_ThreeHit.fbx");
        var clips = importer.clipAnimations;
        foreach (var clip in clips.Where(c => c.name.StartsWith("SahurSwordCombo")))
        {
            clip.keepOriginalOrientation = false;
            clip.lockRootRotation = false;
            clip.rotationOffset = 0f;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        return "Extracted body root yaw; translation, feet-based height, slices and clip IDs unchanged.";
    }

    public static async Task<string> Capture(string label)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Use Play Mode.");
        var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "SahurPlayer.prefab"));
        var movement = actor.GetComponent<ThirdPersonPlayerController>();
        movement.snapSpawnToIslandSurface = false;
        movement.waterSplashes = false;
        movement.cameraCollision = false;
        actor.transform.position = new Vector3(2300f, 300f, 2300f);
        foreach (var input in actor.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
        foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)) renderer.updateWhenOffscreen = true;
        var cursor = Cursor.lockState;
        bool visible = Cursor.visible;
        GameObject cameraObject = null;
        RenderTexture render = null;
        Texture2D pixels = null;
        var previousRender = RenderTexture.active;
        try
        {
            await Task.Delay(250);
            movement.enabled = false;
            var attack = actor.GetComponent<Mavis.SahurAttack>();
            attack.enabled = false;
            actor.transform.rotation = Quaternion.identity;
            var animator = attack.animator;
            var visual = animator.transform;
            var baseRotation = visual.localRotation;
            var basePosition = visual.localPosition;
            animator.Play("Locomotion", 0, 0f); animator.Update(0f);
            var bones = new[] { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.Head };
            var axes = bones.Select(b => animator.GetBoneTransform(b).InverseTransformDirection(Vector3.forward)).ToArray();
            var report = new StringBuilder(label + "\n");
            cameraObject = new GameObject("Temporary natural combo camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.18f, .19f, .21f);
            camera.fieldOfView = 35f;
            foreach (Transform child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            var light = cameraObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 2f; light.cullingMask = 1 << 31;
            render = new RenderTexture(720, 540, 24);
            camera.targetTexture = render;
            pixels = new Texture2D(720, 540, TextureFormat.RGB24, false);
            for (int stage = 1; stage <= 3; stage++)
            {
                visual.localRotation = baseRotation; visual.localPosition = basePosition;
                animator.Play("Combo " + stage, 0, 0f); animator.Update(0f);
                var state = animator.GetCurrentAnimatorStateInfo(0);
                report.AppendLine($"Stage {stage} start: animator.rootRotation={animator.rootRotation.eulerAngles}, bodyRotation={animator.bodyRotation.eulerAngles}");
                var clip = AssetDatabase.LoadAllAssetsAtPath(Root + "Animations/Source/SwordAndShieldSlash_ThreeHit.fbx")
                    .OfType<AnimationClip>().First(c => c.name == "SahurSwordCombo" + stage);
                float duration = clip.length / state.speed;
                Vector3 direct = Vector3.zero, originalFrame = Vector3.zero;
                Quaternion rootYaw = Quaternion.identity;
                for (int i = 0; i < 80; i++)
                {
                    visual.localRotation = baseRotation; visual.localPosition = basePosition;
                    animator.Update(duration / 80f);
                    Vector3 delta = animator.deltaPosition;
                    direct += delta;
                    originalFrame += rootYaw * delta;
                    rootYaw *= animator.deltaRotation;
                }
                report.AppendLine($"Stage {stage}: unrotated path=({direct.x:F4},{direct.y:F4},{direct.z:F4}), root-reconstructed path=({originalFrame.x:F4},{originalFrame.y:F4},{originalFrame.z:F4}), root yaw={Mathf.DeltaAngle(0f, rootYaw.eulerAngles.y):F2}");
                if (movement.comboSourceX != null && movement.comboSourceZ != null && movement.comboSourceX.Length == 3)
                {
                    var x = movement.comboSourceX[stage - 1]; var z = movement.comboSourceZ[stage - 1];
                    Vector3 raw = new Vector3(x.Evaluate(1f) - x.Evaluate(0f), 0f, z.Evaluate(1f) - z.Evaluate(0f));
                    report.AppendLine($"Stage {stage}: original source XZ={(raw * animator.humanScale * visual.lossyScale.y).ToString("F4")}");
                }
                foreach (float phase in new[] { 0f, .25f, .5f, .75f, .99f })
                {
                    visual.localRotation = baseRotation; visual.localPosition = basePosition;
                    animator.Play("Combo " + stage, 0, phase); animator.Update(0f);
                    string yaws = string.Join(", ", bones.Select((bone, index) =>
                    {
                        Vector3 forward = Vector3.ProjectOnPlane(animator.GetBoneTransform(bone).TransformDirection(axes[index]), Vector3.up);
                        return bone + "=" + Vector3.SignedAngle(Vector3.forward, forward, Vector3.up).ToString("F2");
                    }));
                    report.AppendLine($"Stage {stage} phase {phase:F2}: {yaws}");
                    if (phase == .5f)
                    {
                        animator.speed = 0f;
                        await Task.Delay(40);
                        var bounds = actor.GetComponentInChildren<SkinnedMeshRenderer>().bounds;
                        Vector3 offset = new Vector3(2f, .7f, 4.3f).normalized * 11f;
                        camera.transform.SetPositionAndRotation(bounds.center + offset, Quaternion.LookRotation(-offset));
                        camera.Render(); RenderTexture.active = render;
                        pixels.ReadPixels(new Rect(0, 0, 720, 540), 0, 0); pixels.Apply();
                        string path = Path.Combine(Path.GetTempPath(), $"sahur-{label}-stage{stage}.png");
                        File.WriteAllBytes(path, pixels.EncodeToPNG());
                        report.AppendLine("Preview: " + path);
                        animator.speed = 1f;
                    }
                }
            }
            return report.ToString();
        }
        finally
        {
            RenderTexture.active = previousRender;
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if (render != null) UnityEngine.Object.DestroyImmediate(render);
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(actor);
            Cursor.lockState = cursor; Cursor.visible = visible;
        }
    }
}
