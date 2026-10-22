using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;

// Actual input, normal Animator evaluation and final SRP camera frames.
public static class ReviewBladeAnimationV236
{
    static Keyboard keyboard;
    static Mouse mouse;
    static bool left, right, recording;
    static Key[] keys = Array.Empty<Key>();
    static GameObject player;
    static SahurAttack attack;
    static ThirdPersonPlayerController motor;
    static PlayerStamina stamina;
    static Animator animator;
    static Transform[] bones;
    static readonly HumanBodyBones[] BoneIds = { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.LeftUpperArm,
        HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };
    static Camera camera;
    static RenderTexture rt;
    static Texture2D texture;
    static string folder, section;
    static Task pending;
    static int lastFrame;
    static bool expectPolish;
    static readonly List<object> frames = new List<object>(), checks = new List<object>();
    static Vector3 origin;
    static void Input()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(keys));
        InputState.Change(mouse, new MouseState().WithButton(MouseButton.Left, left).WithButton(MouseButton.Right, right));
    }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds; var timeout = DateTime.UtcNow.AddSeconds(45);
        while (Time.time < end) { if (DateTime.UtcNow > timeout) throw new Exception("Stalled: " + section); await Task.Delay(5); }
    }
    static async Task Until(Func<bool> condition, float seconds = 5)
    {
        float end = Time.time + seconds;
        while (!condition()) { if (Time.time > end) throw new Exception("State timeout: " + section); await Wait(.001f); }
    }
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static float[] Q(Quaternion value) => new[] { value.x, value.y, value.z, value.w };
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks.Add(text); }
    public static object Start(string take)
    {
        if (!Application.isPlaying || SessionState.GetBool("CombatCombinationTests", false)) throw new Exception("Ready Play Mode required");
        folder = ".codex/blade-animation-polish-20261021/" + take;
        expectPolish = take.StartsWith("after");
        if (Directory.Exists(folder)) throw new Exception("Preserve prior takes");
        Directory.CreateDirectory(folder + "/frames");
        SessionState.SetBool("CombatCombinationTests", true); EditorApplication.isPaused = false;
        pending = Run(); return new { started = true, folder };
    }
    static async Task Reset(string next)
    {
        recording = false; left = right = false; keys = Array.Empty<Key>(); await Wait(.12f);
        attack.SuspendForSwimming(); player.GetComponent<CombatLocalHitStop>()?.Release(); animator.speed = 1;
        motor.EndAttackFacing(true); motor.RestoreSavedPose(origin, Quaternion.identity); stamina.currentStamina = 100;
        await Wait(.5f); section = next; recording = true;
    }
    static void Frame(ScriptableRenderContext context, Camera rendered)
    {
        if (!recording || rendered != camera || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        var state = animator.GetCurrentAnimatorStateInfo(0); var next = animator.GetNextAnimatorStateInfo(0);
        int layer = animator.GetLayerIndex("Twin Blade Charge");
        frames.Add(new { frame = Time.frameCount, time = Time.time, section, phase = attack.CurrentAttackPhase,
            stage = attack.CurrentComboStage, charging = attack.IsCharging, heavy = attack.IsHeavyAttackActive,
            spin = attack.IsSpinning, guard = attack.IsGuarding, stop = CombatLocalHitStop.Active(player),
            state = state.fullPathHash, statePhase = state.normalizedTime, next = next.fullPathHash,
            transition = animator.IsInTransition(0) ? animator.GetAnimatorTransitionInfo(0).normalizedTime : -1,
            chargeWeight = layer >= 0 ? animator.GetLayerWeight(layer) : 0, chargePose = attack.ChargePose01,
            root = V(player.transform.position), rotations = bones.Select(b => Q(b.localRotation)).ToArray(),
            positions = bones.Select(b => V(player.transform.InverseTransformPoint(b.position))).ToArray(),
            bladeActive = attack.stickHitbox && attack.stickHitbox.enabled });
        var active = RenderTexture.active;
        try { RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            File.WriteAllBytes(folder + "/frames/" + frames.Count.ToString("D5") + ".png", texture.EncodeToPNG()); }
        finally { RenderTexture.active = active; }
    }
    public static void PositionCamera()
    {
        if (!recording || !camera || !player) return;
        Vector3 center = player.GetComponent<CharacterController>().bounds.center + Vector3.up * .2f;
        camera.transform.position = center + new Vector3(6, 2.8f, -8); camera.transform.LookAt(center); camera.fieldOfView = 46;
    }
    static async Task Run()
    {
        float capture = Time.captureDeltaTime; var oldKeyboard = Keyboard.current; var oldMouse = Mouse.current;
        bool passed = false; frames.Clear(); checks.Clear(); lastFrame = -1;
        try
        {
            CombatFeelPreview.Spawn(); Time.captureDeltaTime = 1f / 60;
            player = CombatFeelPreview.Player; attack = player.GetComponent<SahurAttack>();
            motor = player.GetComponent<ThirdPersonPlayerController>(); stamina = player.GetComponent<PlayerStamina>();
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            left = right = false; keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate += Input;
            var input = player.GetComponent<PlayerInput>(); InputUser.PerformPairingWithDevice(keyboard, input.user);
            InputUser.PerformPairingWithDevice(mouse, input.user); input.ActivateInput();
            await Wait(.7f); var loadout = player.GetComponent<SahurWeaponLoadout>();
            loadout.Restore(true, SahurWeaponLoadout.Weapon.M4A1, true); await Wait(.3f);
            Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades), "Actual rifle to blade equip");
            await Wait(.4f); animator = attack.animator; origin = player.transform.position;
            bones = BoneIds.Select(id => animator.GetBoneTransform(id)).ToArray();
            CombatFeelPreview.Target.transform.position = new Vector3(2000, 500, 2040); Physics.SyncTransforms();
            camera = CombatFeelPreview.Camera;
            rt = new RenderTexture(960, 540, 24) { name = "Temporary blade review capture" }; rt.Create();
            texture = new Texture2D(960, 540, TextureFormat.RGB24, false); camera.targetTexture = rt;
            player.AddComponent<BladeReviewCameraV236>(); RenderPipelineManager.endCameraRendering += Frame;
            await Reset("Idle to movement"); await Wait(.4f);
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) }; await Wait(.7f);
            keys = Array.Empty<Key>(); await Wait(.7f);
            await Reset("Three hit combo"); await Wait(.25f); left = true;
            await Until(() => attack.CurrentComboStage == 0); left = false;
            await Until(() => attack.CurrentAttackPhase >= .76f); left = true; await Wait(.04f); left = false;
            await Until(() => attack.CurrentComboStage == 1); await Until(() => attack.CurrentAttackPhase >= .76f);
            left = true; await Wait(.04f); left = false; await Until(() => attack.CurrentComboStage == 2);
            await Until(() => !attack.IsCombatMotionActive); await Wait(.6f); Check(!attack.stickHitbox.enabled, "Combo recovery clears hit volume");
            await Reset("Charged rush"); await Wait(.3f); right = true;
            await Until(() => attack.IsCharging); await Wait(1.35f); right = false;
            await Until(() => attack.IsHeavyAttackActive);
            if(expectPolish) Check(animator.GetLayerWeight(animator.GetLayerIndex("Twin Blade Charge"))>.5f,"Rush release retains outgoing X pose instead of exposing idle");
            Vector3 start = player.transform.position;
            await Until(() => !attack.IsCombatMotionActive); await Wait(.5f);
            Check(Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude > 15.5f, "Approved 16m rush preserved");
            await Reset("Early charge cancel"); await Wait(.3f); right = true;
            await Until(() => attack.IsCharging); await Wait(.6f); right = false;
            await Until(() => !attack.IsCharging);
            if(expectPolish) Check(animator.GetLayerWeight(animator.GetLayerIndex("Twin Blade Charge"))>.5f,"Early cancellation begins a visible authored-pose blend");
            await Wait(.7f);
            Check(animator.GetLayerWeight(animator.GetLayerIndex("Twin Blade Charge"))<.001f,"Cancelled charge layer fully releases ownership");
            Check(!attack.IsCombatMotionActive, "Early charge release never launches a rush");
            await Reset("Guard release"); await Wait(.3f); keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Guard) };
            await Until(() => attack.GuardReady); await Wait(.5f); keys = Array.Empty<Key>(); await Wait(.65f);
            Check(!attack.IsGuarding, "Guard releases");
            await Reset("Spin release"); await Wait(.3f); left = right = true;
            await Until(() => attack.IsSpinning); await Wait(1.15f); left = right = false; await Wait(.75f);
            Check(!attack.IsSpinning && !attack.stickHitbox.enabled, "Spin release clears damage");
            await Reset("Confirmed blade contact"); CombatFeelPreview.Target.transform.position = origin + new Vector3(1.5f, 0, 3);
            Physics.SyncTransforms(); left = true; await Until(() => attack.CurrentComboStage == 0); left = false;
            await Until(() => !attack.IsCombatMotionActive); await Wait(.5f);
            passed = true;
        }
        catch (Exception error) { File.WriteAllText(folder + "/error.txt", error.ToString()); }
        finally
        {
            recording = false; RenderPipelineManager.endCameraRendering -= Frame; InputSystem.onBeforeUpdate -= Input;
            File.WriteAllText(folder + "/review.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed, simulationHz = 60, capture = "Native endCameraRendering", bones = BoneIds.Select(x => x.ToString()).ToArray(), checks, frames }, Newtonsoft.Json.Formatting.Indented));
            if (camera) camera.targetTexture = null;
            if (rt) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            if (texture) UnityEngine.Object.DestroyImmediate(texture);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
            keyboard = null; mouse = null;
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            CombatFeelPreview.Clear(); Time.captureDeltaTime = capture; SessionState.SetBool("CombatCombinationTests", false);
        }
    }
}
[DefaultExecutionOrder(32000)] public sealed class BladeReviewCameraV236 : MonoBehaviour
{
    void LateUpdate() => ReviewBladeAnimationV236.PositionCamera();
}
