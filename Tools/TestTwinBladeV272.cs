using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Mavis;

public static class TestTwinBladeV272
{
    const string Folder = ".codex/twin-blade-v272";
    static readonly BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    static Task pending;
    static Keyboard keyboard, oldKeyboard;
    static Mouse mouse, oldMouse;
    static bool left;
    static int hz;
    static SahurAttack attack;
    static ThirdPersonPlayerController motor;
    static SahurWeaponLoadout weapons;
    static PlayerStamina stamina;
    static GameObject player;
    static Vector3 home;
    static readonly List<object> checks = new List<object>(), poses = new List<object>();
    static readonly List<InputDevice> physical = new List<InputDevice>();
    public static object Open()
    {
        if (Application.isPlaying) throw new Exception("Use stopped Editor");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(s => UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)) throw new Exception("Preserve dirty scenes");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/scene-setup.json", Newtonsoft.Json.JsonConvert.SerializeObject(setup));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return new { starting = true };
    }
    public static object Restore()
    {
        if (Application.isPlaying) throw new Exception("Stop before restore");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Folder + "/scene-setup.json")));
        return new { restored = true };
    }
    static void Check(bool pass, string message)
    { checks.Add(new { pass, message }); if (!pass) throw new Exception(message); }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds; var deadline = DateTime.UtcNow.AddSeconds(35);
        while (Time.time < end) { if (DateTime.UtcNow > deadline) throw new Exception("Native player loop stalled"); await Task.Delay(5); }
    }
    static async Task Until(Func<bool> predicate, float seconds = 5)
    { float end = Time.time + seconds; while (!predicate()) { if (Time.time > end) throw new Exception("Native state timeout: " + attack.CurrentComboStage + "/" + attack.CurrentAttackPhase); await Wait(.001f); } }
    static void Input()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState()); InputState.Change(mouse, new MouseState().WithButton(MouseButton.Left, left));
    }
    static int Hash => (int)typeof(SahurAttack).GetField("activeAttackHash", F).GetValue(attack);
    static async Task Tap() { left = true; await Wait(2f / hz); left = false; await Wait(2f / hz); }
    static async Task Idle()
    { await Until(() => !attack.IsCombatMotionActive); await Wait(.6f); stamina.currentStamina = 100; }
    static async Task Reset()
    {
        left = false; attack.SuspendForSwimming(); player.GetComponent<CombatLocalHitStop>()?.Release();
        motor.RestoreSavedPose(home, Quaternion.identity); stamina.currentStamina = 100; await Wait(.4f);
    }
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static void Capture(string name)
    {
        var camera = CombatFeelPreview.Camera; var chest = attack.animator.GetBoneTransform(HumanBodyBones.Chest);
        camera.transform.position = player.transform.position + new Vector3(7, 5.5f, -10); camera.transform.LookAt(chest.position); camera.fieldOfView = 48;
        var rt = RenderTexture.GetTemporary(640, 480, 24); var texture = new Texture2D(640, 480, TextureFormat.RGB24, false); var active = RenderTexture.active;
        try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, 640, 480), 0, 0, false); File.WriteAllBytes(Folder + "/" + hz + "-" + name + ".png", texture.EncodeToPNG()); }
        finally { camera.targetTexture = null; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); }
    }
    static float BodyYaw()
    {
        var a = attack.animator;
        var right = a.GetBoneTransform(HumanBodyBones.RightUpperArm).position - a.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
        var up = a.GetBoneTransform(HumanBodyBones.Head).position - a.GetBoneTransform(HumanBodyBones.Hips).position;
        var forward = player.transform.InverseTransformDirection(Vector3.Cross(right, up));
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
    public static object Start(int rate = 60)
    { if (!Application.isPlaying) throw new Exception("Play Mode required"); hz = rate; pending = Run(); return new { started = true, hz }; }
    static async Task Run()
    {
        float delta = Time.captureDeltaTime; checks.Clear(); poses.Clear(); string error = null;
        RuntimeAnimatorController controller = null; AnimatorOverrideController overrides = null;
        oldKeyboard = Keyboard.current; oldMouse = Mouse.current;
        try
        {
            CombatFeelPreview.Spawn(); Time.captureDeltaTime = 1f / hz;
            foreach (var d in InputSystem.devices.ToArray()) if ((d is Mouse || d is Keyboard) && d.enabled) { physical.Add(d); InputSystem.DisableDevice(d); }
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); InputSystem.onBeforeUpdate += Input;
            player = CombatFeelPreview.Player; attack = player.GetComponent<SahurAttack>(); motor = player.GetComponent<ThirdPersonPlayerController>();
            stamina = player.GetComponent<PlayerStamina>(); weapons = player.GetComponent<SahurWeaponLoadout>();
            var input = player.GetComponent<PlayerInput>(); InputUser.PerformPairingWithDevice(keyboard, input.user); InputUser.PerformPairingWithDevice(mouse, input.user); input.ActivateInput();
            await Wait(.6f); weapons.Restore(true, SahurWeaponLoadout.Weapon.CapriTwinBlades); await Wait(.4f);
            home = player.transform.position; CombatFeelPreview.Target.transform.position = home + Vector3.forward * 35;
            Check(attack.UsesTwinBlades && motor.CanUseGroundAttack, "Native equipped actor is grounded");
            // Sample both original and revised slash through the actual Animator.
            controller = attack.animator.runtimeAnimatorController;
            overrides = new AnimatorOverrideController(controller);
            var slash = Resources.Load<AnimationClip>("Combat/Combinations/BladeSlashV272");
            var oldSlash = Resources.Load<AnimationClip>("Equipment/Capri/CapriSlash");
            var yawPeaks = new List<float>();
            foreach (string label in new[] { "before", "after" })
            {
                overrides[slash.name] = label == "before" ? oldSlash : slash; attack.animator.runtimeAnimatorController = overrides;
                await Reset(); typeof(SahurAttack).GetField("nextTwinBladeSwing", F).SetValue(attack, 0);
                await Tap(); await Until(() => attack.CurrentComboStage == 0);
                float peak = 0;
                for (int i = 0; i < 5; i++)
                {
                    await Until(() => attack.CurrentAttackPhase >= .27f + i * .08f);
                    float yaw = BodyYaw(); peak = Mathf.Max(peak, Mathf.Abs(yaw));
                    poses.Add(new { label, phase = attack.CurrentAttackPhase, yaw }); Capture(label + "-" + i);
                }
                yawPeaks.Add(peak); await Idle();
            }
            Check(yawPeaks[1] < yawPeaks[0] - 5, "Rendered slash body yaw reduced: " + yawPeaks[0] + " -> " + yawPeaks[1]);
            attack.animator.runtimeAnimatorController = controller; await Reset();
            var sides = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                await Tap(); Check(attack.CurrentComboStage == 0, "Separated tap " + i + " starts a new chain");
                sides.Add(Hash); await Idle();
            }
            Check(sides[0] != sides[1] && sides[0] == sides[2] && sides[1] == sides[3], "Four separated clicks alternate sides without granting a finisher");
            await Reset(); await Tap(); await Until(() => attack.CurrentAttackPhase >= .35f);
            int interrupted = Hash; attack.SuspendForSwimming(); await Wait(.6f); await Tap();
            Check(attack.CurrentComboStage == 0 && Hash != interrupted, "Interrupted swing keeps the next side, resets combo count"); await Idle();
            // A chain starting from either side still needs three presses.
            for (int sequence = 0; sequence < 2; sequence++)
            {
                await Reset(); await Tap(); int first = Hash;
                Check(attack.CurrentComboStage == 0 && Hash != Animator.StringToHash("Base Layer.Capri Combo 3"), "First input cannot spin");
                await Until(() => attack.CurrentAttackPhase >= .67f); await Tap(); await Until(() => attack.CurrentComboStage == 1);
                Check(Hash != first && Hash != Animator.StringToHash("Base Layer.Capri Combo 3"), "Second input uses opposite side, cannot spin");
                await Until(() => attack.CurrentAttackPhase >= .67f); await Tap(); await Until(() => attack.CurrentComboStage == 2);
                Check(Hash == Animator.StringToHash("Base Layer.Capri Combo 3"), "Third consecutive input starts finisher");
                float turn = 0, previous = BodyYaw(); int sample = 0;
                while (attack.CurrentComboStage == 2)
                {
                    float yaw = BodyYaw(); turn += Mathf.DeltaAngle(previous, yaw); previous = yaw;
                    poses.Add(new { label = "spin", sequence, phase = attack.CurrentAttackPhase, yaw, turn });
                    if (sequence == 0 && attack.CurrentAttackPhase >= .26f + sample * .055f && sample < 10) Capture("spin-" + sample++);
                    await Wait(1f / hz);
                }
                Check(Mathf.Abs(turn) > 680 && Mathf.Abs(turn) < 770, "Rendered finisher completes two turns: " + turn);
                Check(Quaternion.Angle(player.transform.rotation, Quaternion.identity) < 1, "Finisher preserves aimed controller heading");
                await Idle(); await Tap(); await Idle(); // Change the next chain's starting side.
            }
            // Real weapon contacts in front must survive the orientation correction on both sides.
            for (int side = 0; side < 2; side++)
            {
                await Reset();
                CombatFeelPreview.Target.transform.position = home + Vector3.forward * 3.8f;
                Physics.SyncTransforms();
                var targetHealth = CombatFeelPreview.Target.GetComponent<NailongHealth>();
                float before = targetHealth.currentHealth;
                await Tap(); await Idle();
                Check(targetHealth.currentHealth < before, "Alternating slash " + side + " contacts actual target straight ahead");
                float after = targetHealth.currentHealth; await Wait(.3f);
                Check(targetHealth.currentHealth == after, "No phantom damage after slash " + side + " recovery");
            }
            CombatFeelPreview.Target.transform.position = home + Vector3.forward * 35;
            await Reset(); stamina.currentStamina = 0;
            int next = (int)typeof(SahurAttack).GetField("nextTwinBladeSwing", F).GetValue(attack);
            await Tap(); Check(!attack.IsCombatMotionActive && next == (int)typeof(SahurAttack).GetField("nextTwinBladeSwing", F).GetValue(attack), "Rejected stamina input does not advance side");
            stamina.currentStamina = 100; Check(weapons.TryEquip(SahurWeaponLoadout.Weapon.Stick), "Switch to original club");
            await Wait(.3f); await Tap(); Check(Hash == Animator.StringToHash("Base Layer." + attack.comboOneState), "Club retains original first combo animation");
            await Reset(); Check(!attack.stickHitbox.enabled, "Cancellation clears damage volume");
        }
        catch (Exception e) { error = e.ToString(); }
        finally
        {
            InputSystem.onBeforeUpdate -= Input;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse); keyboard = null; mouse = null;
            foreach (var d in physical) if (d.added) InputSystem.EnableDevice(d); physical.Clear();
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent(); if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            if (attack && controller) attack.animator.runtimeAnimatorController = controller;
            if (overrides) UnityEngine.Object.DestroyImmediate(overrides);
            CombatFeelPreview.Clear(); Time.captureDeltaTime = delta;
            File.WriteAllText(Folder + "/tests-" + hz + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = error == null, error, hz, checks, poses }, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
