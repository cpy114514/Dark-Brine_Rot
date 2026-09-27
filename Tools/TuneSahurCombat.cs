using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

public static class TuneSahurCombat
{
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const string ControllerPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";

    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before saving combat tuning.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var report = new StringBuilder();
        for (int i = 1; i <= 3; i++)
        {
            var state = controller.layers[0].stateMachine.states.Single(item => item.state.name == "Combo " + i).state;
            state.speed = 1.25f;
            EditorUtility.SetDirty(state);
            report.AppendLine($"{state.name}: speed={state.speed}, duration={((AnimationClip)state.motion).length / state.speed:F3}s");
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var attack = root.GetComponent<SahurAttack>();
            attack.chargePoseTime = SahurAttack.SafeChargePoseTime;
            EditorUtility.SetDirty(attack);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            report.AppendLine("Charge holds at safe phase " + attack.chargePoseTime + "; original stick grip unchanged.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return report.ToString();
    }

    public static string Audit()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var report = new StringBuilder();
        foreach (var state in controller.layers[0].stateMachine.states.Select(item => item.state).Where(state => state.name.StartsWith("Combo") || state.name.Contains("Charge") || state.name.Contains("Heavy")))
            report.AppendLine($"{state.name}: speed={state.speed}, clip={state.motion.name}, timeParameter={state.timeParameter}, source={AssetDatabase.GetAssetPath(state.motion)}");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var attack = root.GetComponent<SahurAttack>();
            report.AppendLine($"chargePose={attack.chargePoseTime}, fullCharge={attack.fullChargeTime}, cooldown={attack.cooldown}, clip={attack.chargeClip.name}");
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                report.AppendLine(collider.name + " " + collider.GetType().Name + " parent=" + collider.transform.parent?.name);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return report.ToString();
    }

    public static Task<string> AuditCharge() => CaptureCharge(false);
    public static Task<string> VerifyCharge() => CaptureCharge(true);

    static async Task<string> CaptureCharge(bool verify)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode is required for the retargeted pose check.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var holder = new GameObject("Temporary charge pose audit");
        holder.SetActive(false);
        var actor = UnityEngine.Object.Instantiate(prefab, holder.transform);
        actor.transform.localPosition = Vector3.zero;
        actor.GetComponent<ThirdPersonPlayerController>().snapSpawnToIslandSurface = false;
        foreach (var input in actor.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
        var report = new StringBuilder();
        var savedCursor = Cursor.lockState;
        bool savedVisible = Cursor.visible;
        GameObject cameraObject = null;
        RenderTexture render = null;
        Texture2D texture = null;
        var active = RenderTexture.active;
        try
        {
            holder.SetActive(true);
            await Task.Delay(350);
            var movement = actor.GetComponent<ThirdPersonPlayerController>();
            var attack = actor.GetComponent<SahurAttack>();
            movement.enabled = false;
            attack.enabled = false;
            var animator = attack.animator;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var headColliders = head.GetComponentsInChildren<Collider>(true);
            var shaft = (CapsuleCollider)attack.stickHitbox;
            report.AppendLine("Head=" + head.name + " colliders=" + string.Join(",", headColliders.Select(collider => collider.name)));
            cameraObject = new GameObject("Temporary charge preview camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.22f, 0.24f, 0.26f);
            camera.fieldOfView = 35f;
            foreach (Transform child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            render = new RenderTexture(960, 720, 24);
            texture = new Texture2D(960, 720, TextureFormat.RGB24, false);
            camera.targetTexture = render;
            var light = cameraObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.cullingMask = 1 << 31;
            cameraObject.transform.rotation = Quaternion.Euler(25f, -30f, 0f);
            animator.Play("Charge Windup", 0, 0f);
            for (int i = 0; i <= 16; i++)
            {
                float phase = i * 0.025f;
                if (verify)
                {
                    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    // Exercise old scenes with the former 0.25 serialized override.
                    attack.chargePoseTime = 0.25f;
                    typeof(SahurAttack).GetField("charging", flags).SetValue(attack, true);
                    float amount = i / 16f;
                    typeof(SahurAttack).GetField("chargeStartedAt", flags).SetValue(attack,
                        Time.time - amount * attack.fullChargeTime);
                    typeof(SahurAttack).GetMethod("UpdateChargePose", flags).Invoke(attack, null);
                    phase = animator.GetFloat("ChargePhase");
                    if (Mathf.Abs(phase - SahurAttack.SafeChargePoseTime * amount) > 0.001f)
                        throw new InvalidOperationException("Charge phase did not clamp the old scene override.");
                    if (attack.stickHitbox.enabled) throw new InvalidOperationException("Charge enabled a damaging hitbox.");
                }
                else animator.SetFloat("ChargePhase", phase);
                animator.Update(0f);
                await Task.Delay(80);
                Physics.SyncTransforms();
                float penetration = 0f;
                foreach (var collider in headColliders)
                    if (Physics.ComputePenetration(shaft, shaft.transform.position, shaft.transform.rotation,
                        collider, collider.transform.position, collider.transform.rotation, out _, out float depth))
                        penetration = Mathf.Max(penetration, depth);
                Vector3 hand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                report.AppendLine($"phase={phase:F3}: head={actor.transform.InverseTransformPoint(head.position)}, hand={actor.transform.InverseTransformPoint(hand)}, penetration={penetration:F4}m");
                if (verify ? i == 16 : (i == 0 || i == 4 || i == 6 || i == 8 || i == 10 || i == 12))
                {
                    var target = actor.transform.position + Vector3.up * 0.45f;
                    for (int view = 0; view < (verify ? 3 : 1); view++)
                    {
                        var offset = view == 0 ? new Vector3(2.8f, 0.4f, 4.0f) :
                            view == 1 ? new Vector3(-4.7f, 0.4f, 0f) : new Vector3(2.8f, 0.4f, -4f);
                        camera.transform.SetPositionAndRotation(target + offset, Quaternion.LookRotation(-offset));
                        camera.Render();
                        RenderTexture.active = render;
                        texture.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                        texture.Apply();
                        string name = verify ? "sahur-charge-safe-" + view : "sahur-charge-" + i;
                        File.WriteAllBytes(Path.Combine(Path.GetTempPath(), name + ".png"), texture.EncodeToPNG());
                    }
                }
            }
            if (verify)
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float releaseOffset = animator.GetFloat("ChargePhase") * attack.chargeClip.length;
                typeof(SahurAttack).GetMethod("ReleaseCharge", flags).Invoke(attack, null);
                animator.Update(0f);
                float actualOffset = animator.GetNextAnimatorStateInfo(0).normalizedTime * attack.chargeClip.length;
                if (Mathf.Abs(releaseOffset - actualOffset) > 0.002f || attack.IsCharging)
                    throw new InvalidOperationException($"Charge release offset expected={releaseOffset:F5}, actual={actualOffset:F5}, charging={attack.IsCharging}, current={animator.GetCurrentAnimatorStateInfo(0).normalizedTime:F5}, transitioning={animator.IsInTransition(0)}.");
                report.AppendLine("PASS: full-charge clamp, disabled charge hitbox, and matching heavy-release offset=" + actualOffset);
            }
            return report.ToString();
        }
        finally
        {
            RenderTexture.active = active;
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if (render != null) UnityEngine.Object.DestroyImmediate(render);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(holder);
            Cursor.lockState = savedCursor;
            Cursor.visible = savedVisible;
        }
    }
}
