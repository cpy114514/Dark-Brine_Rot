using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;

public static class AutoQaNativeInputSmoke
{
    static Keyboard keyboard, oldKeyboard;
    static Mouse mouse, oldMouse;
    static InputSettings oldSettings, settings;
    static HideFlags oldSettingsFlags;
    static readonly List<InputDevice> physical = new List<InputDevice>();
    static (int, bool)[] roster;
    static bool left, right, armed, ownsPreview;
    static Key[] keys = Array.Empty<Key>();
    static GameObject actor;
    static SahurAttack attack;
    static ThirdPersonPlayerController motor;
    static SahurWeaponLoadout weapons;
    static PlayerStamina stamina;
    static Vector3 home;
    static float oldCapture;
    static string folder, section;
    static int lastFrame;
    static Task pending;
    static readonly List<string> checks = new List<string>();
    static readonly List<object> frames = new List<object>();
    static readonly List<object> boundaryCases = new List<object>();
    static bool boundaryRun, previousDodgeRequest, watchEvasion;
    static int simulationHz = 60;
    static float dodgeInjectedAt, evasionAt, injectedDelta, evasionDelta, injectedPhase;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    static void Check(bool condition, string text) { if (!condition) throw new Exception(text); checks.Add(text); }
    static void Save(string file, object data) => File.WriteAllText(folder + "/" + file, Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));

    public static object Start(string take) => Begin(take, false, 60);
    static object Begin(string take, bool boundaries, int hz)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (armed || SessionState.GetBool("CombatCombinationTests", false) || CombatFeelPreview.Player) throw new Exception("Another native fixture owns input");
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$")) throw new Exception("Invalid take name");
        folder = "AUTO_QA/Evidence/native-input/" + take;
        if (Directory.Exists(folder)) throw new Exception("Preserve previous take");
        boundaryRun = boundaries; simulationHz = hz;
        Directory.CreateDirectory(folder); checks.Clear(); frames.Clear(); boundaryCases.Clear(); section = "setup";
        previousDodgeRequest = watchEvasion = false; dodgeInjectedAt = evasionAt = -1;
        pending = Run(); return new { started = true, folder };
    }

    public static object StartBoundary(string take, int hz = 60)
    {
        if (hz != 30 && hz != 60) throw new Exception("Use 30 or 60 Hz boundary probes");
        return Begin(take, true, hz);
    }

    static void Inject()
    {
        if (!armed || keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        bool dodgeRequest = keys.Contains(GameInputSettings.Get(GameInputSettings.Action.Dodge));
        if (dodgeRequest && !previousDodgeRequest) { dodgeInjectedAt = Time.unscaledTime; injectedDelta = Time.unscaledDeltaTime; injectedPhase = attack ? attack.CurrentAttackPhase : 0; }
        previousDodgeRequest = dodgeRequest;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, left).WithButton(MouseButton.Right, right));
    }
    static void Observe()
    {
        if (!armed || !actor || !attack || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (watchEvasion && evasionAt < 0 && (motor.IsDodging || Get<float>(motor, "airFlipTimer") > 0)) { evasionAt = Time.unscaledTime; evasionDelta = Time.unscaledDeltaTime; }
        if (frames.Count < 5000) frames.Add(new { frame = lastFrame, time = Time.time, section,
            stage = attack.CurrentComboStage, phase = attack.CurrentAttackPhase, charging = attack.IsCharging,
            chargeElapsed = attack.ChargeElapsed, heavy = attack.IsHeavyAttackActive, jumpSlash = attack.IsJumpSlashActive,
            ground = motor.CanUseGroundAttack, dodging = motor.IsDodging, airFlip = Get<float>(motor, "airFlipTimer"),
            position = actor.transform.position.ToString("F4"), weapon = weapons.CurrentWeapon.ToString(),
            keys = string.Join(",", keys), left, right, cursor = Cursor.lockState.ToString(),
            keyboardId = Keyboard.current?.deviceId, expectedKeyboard = keyboard.deviceId,
            unscaledTime = Time.unscaledTime, unscaledDelta = Time.unscaledDeltaTime,
            motorDodgeExpiry = Get<float>(motor, "dodgeBufferUntil"), attackDodgeExpiry = Get<float>(attack, "dodgeBufferedUntil") });
        if (lastFrame % 30 == 0) Save("progress.json", new { section, frame = lastFrame, checks = checks.Count, time = Time.time });
    }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds; var deadline = DateTime.UtcNow.AddSeconds(40);
        while (Time.time < end)
        {
            if (!armed || !Application.isPlaying) throw new OperationCanceledException("Native input fixture interrupted");
            if (DateTime.UtcNow > deadline) throw new TimeoutException("No sufficient native frame progress: " + section);
            await Task.Delay(5);
        }
    }
    static async Task Until(Func<bool> condition, float seconds = 3)
    {
        float end = Time.time + seconds;
        while (!condition()) { if (Time.time > end) throw new Exception("State timeout: " + section); await Wait(.001f); }
    }
    static async Task TapLeft() { left = true; await Wait(.05f); left = false; await Wait(.025f); }
    static async Task Reset(string next)
    {
        left = right = false; keys = Array.Empty<Key>(); await Wait(.12f);
        attack.SuspendForSwimming(); actor.GetComponent<CombatLocalHitStop>()?.Release();
        motor.ExternalControlLock = motor.ExternalMovementLock = false;
        motor.RestoreSavedPose(home, Quaternion.identity); stamina.currentStamina = 100;
        await Wait(.5f); section = next;
    }
    static async Task Boundaries()
    {
        int stale = 0;
        var feel = attack.feel ? attack.feel : CombatFeelProfile.Default;
        foreach (var weapon in new[] { SahurWeaponLoadout.Weapon.Stick, SahurWeaponLoadout.Weapon.CapriTwinBlades })
        {
            Check(weapons.TryEquip(weapon), "Boundary equip " + weapon);
            foreach (float phase in new[] { .2f, .45f, .6f })
            {
                await Reset(weapon + " dodge tap at " + phase.ToString("F2"));
                await TapLeft(); await Until(() => attack.IsGroundComboActive && attack.CurrentAttackPhase >= phase);
                dodgeInjectedAt = evasionAt = -1; watchEvasion = true;
                keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Dodge) }; await Wait(.025f); keys = Array.Empty<Key>();
                await Wait(1.6f); watchEvasion = false;
                Check(dodgeInjectedAt >= 0, weapon + " dodge intent traversed native input update at " + phase);
                float age = evasionAt < 0 ? -1 : evasionAt - dodgeInjectedAt;
                float tolerance = 2f * Mathf.Max(injectedDelta, evasionDelta) + .002f;
                bool late = age > feel.inputBuffer + tolerance;
                if (late) stale++;
                boundaryCases.Add(new { weapon = weapon.ToString(), requestedPhase = phase, actualPhase = injectedPhase,
                    inputAt = dodgeInjectedAt, consumedAt = evasionAt, inputAge = age, configuredBuffer = feel.inputBuffer,
                    frameTolerance = tolerance, stale = late, simulationHz });
            }
            await Reset(weapon + " dodge waits for stamina");
            await TapLeft(); await Until(() => attack.IsGroundComboActive && attack.CurrentAttackPhase >= .75f);
            stamina.currentStamina = 0; dodgeInjectedAt = evasionAt = -1; watchEvasion = true;
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Dodge) }; await Wait(.025f); keys = Array.Empty<Key>();
            await Wait(.04f); stamina.currentStamina = 100; await Wait(1.2f); watchEvasion = false;
            float readyAge = evasionAt < 0 ? -1 : evasionAt - dodgeInjectedAt;
            float readyTolerance = 2f * Mathf.Max(injectedDelta, evasionDelta) + .002f;
            boundaryCases.Add(new { weapon = weapon.ToString(), scenario = "readiness returns", inputAt = dodgeInjectedAt,
                consumedAt = evasionAt, inputAge = readyAge, configuredBuffer = feel.inputBuffer, frameTolerance = readyTolerance, simulationHz });
            Check(readyAge >= 0 && readyAge <= feel.inputBuffer + readyTolerance, weapon + " fresh dodge survives temporary stamina unavailability within its original deadline");

            await Reset(weapon + " spin dodge"); left = right = true; await Until(() => attack.IsSpinning, .6f); await Wait(.25f);
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Dodge) }; await Wait(.05f);
            keys = Array.Empty<Key>(); left = right = false;
            await Until(() => motor.IsDodging || Get<float>(motor, "airFlipTimer") > 0, .7f);
            Check(!attack.IsSpinning, weapon + " native dodge still exits established continuous spin");
            if (weapon == SahurWeaponLoadout.Weapon.CapriTwinBlades)
            {
                await Reset("blade guard dodge"); var guard = GameInputSettings.Get(GameInputSettings.Action.Guard);
                keys = new[] { guard }; await Until(() => attack.IsGuarding, .6f); await Wait(.2f);
                keys = new[] { guard, GameInputSettings.Get(GameInputSettings.Action.Dodge) }; await Wait(.05f); keys = Array.Empty<Key>();
                await Until(() => motor.IsDodging || Get<float>(motor, "airFlipTimer") > 0, .7f);
                Check(!attack.IsGuarding, "Native dodge still exits blade guard");
            }
        }
        Check(stale == 0, "No evasion consumes a released dodge tap older than the configured input buffer plus two observed native frames; stale cases=" + stale);
    }
    static void Exiting(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) Restore(); }
    public static object Cancel()
    {
        // run_script compiles a new assembly per call; find the live fixture owner.
        int stopped = 0; var flags = BindingFlags.Static | BindingFlags.NonPublic;
        foreach (var type in AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AutoQaNativeInputSmoke")).Where(t => t != null))
            if ((bool)type.GetField("armed", flags).GetValue(null)) { type.GetMethod("Restore", flags).Invoke(null, null); stopped++; }
        return new { cancelled = true, ownersStopped = stopped };
    }
    static void Restore()
    {
        if (!armed) return;
        armed = false; InputSystem.onBeforeUpdate -= Inject; EditorApplication.update -= Observe;
        if (oldSettings) { InputSystem.settings = oldSettings; oldSettings.hideFlags = oldSettingsFlags; }
        if (settings && InputSystem.settings != settings) UnityEngine.Object.DestroyImmediate(settings); settings = null;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse); keyboard = null; mouse = null;
        foreach (var device in physical) if (device.added) InputSystem.EnableDevice(device); physical.Clear();
        if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
        if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
        Time.captureDeltaTime = oldCapture;
        if (ownsPreview) CombatFeelPreview.Clear(); ownsPreview = false;
        SessionState.SetBool("CombatCombinationTests", false);
        AssemblyReloadEvents.beforeAssemblyReload -= Restore; EditorApplication.playModeStateChanged -= Exiting; EditorApplication.quitting -= Restore;
    }
    static async Task Run()
    {
        string error = null;
        try
        {
            oldKeyboard = Keyboard.current; oldMouse = Mouse.current; oldSettings = InputSystem.settings;
            if (!oldSettings) throw new Exception("Restore invalid native input settings before testing");
            oldSettingsFlags = oldSettings.hideFlags;
            oldCapture = Time.captureDeltaTime; roster = InputSystem.devices.Select(d => (d.deviceId, d.enabled)).ToArray();
            armed = true; SessionState.SetBool("CombatCombinationTests", true);
            AssemblyReloadEvents.beforeAssemblyReload += Restore; EditorApplication.playModeStateChanged += Exiting; EditorApplication.quitting += Restore;
            // InputManager destroys replaced settings with exactly HideAndDontSave.
            // Protect the original temporary default, then restore its exact flags.
            if (oldSettingsFlags == HideFlags.HideAndDontSave) oldSettings.hideFlags &= ~HideFlags.NotEditable;
            settings = UnityEngine.Object.Instantiate(oldSettings); settings.hideFlags = HideFlags.DontSave;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = settings;
            CombatFeelPreview.Spawn(); ownsPreview = true; Time.captureDeltaTime = 1f / simulationHz;
            actor = CombatFeelPreview.Player; attack = actor.GetComponent<SahurAttack>(); motor = actor.GetComponent<ThirdPersonPlayerController>();
            weapons = actor.GetComponent<SahurWeaponLoadout>(); stamina = actor.GetComponent<PlayerStamina>();
            CombatFeelPreview.Target.transform.position += Vector3.forward * 60; Physics.SyncTransforms();
            foreach (var device in InputSystem.devices.ToArray()) if ((device is Keyboard || device is Mouse) && device.enabled) { physical.Add(device); InputSystem.DisableDevice(device); }
            keyboard = InputSystem.AddDevice<Keyboard>("AutoQaNativeKeyboard"); mouse = InputSystem.AddDevice<Mouse>("AutoQaNativeMouse");
            left = right = false; keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate += Inject; EditorApplication.update += Observe;
            var input = actor.GetComponent<PlayerInput>(); InputUser.PerformPairingWithDevice(keyboard, input.user); InputUser.PerformPairingWithDevice(mouse, input.user); input.ActivateInput();
            await Wait(.7f); home = actor.transform.position; weapons.Restore(true, SahurWeaponLoadout.Weapon.Stick);
            if (boundaryRun) { await Boundaries(); return; }
            foreach (var weapon in new[] { SahurWeaponLoadout.Weapon.Stick, SahurWeaponLoadout.Weapon.CapriTwinBlades })
            {
                Check(weapons.TryEquip(weapon), "Equip " + weapon); await Reset(weapon + " combo");
                Check(motor.CanUseGroundAttack, weapon + " native actor grounded");
                await TapLeft(); Check(attack.IsGroundComboActive && attack.CurrentComboStage == 0, weapon + " queued LMB starts combo one through PlayerInput");
                await Until(() => attack.IsGroundComboActive && attack.CurrentAttackPhase >= .3f); await TapLeft();
                await Until(() => attack.CurrentComboStage == 1); Check(true, weapon + " second real click continues combo");
                await Until(() => attack.CurrentAttackPhase >= .35f); await TapLeft();
                await Until(() => attack.CurrentComboStage == 2); Check(true, weapon + " third real click enters finisher");
                await Until(() => !attack.IsCombatMotionActive, 4); Check(true, weapon + " combo recovers without further input");

                await Reset(weapon + " dodge cancel"); await TapLeft(); await Until(() => attack.CanDodgeCancel);
                keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Dodge) }; await Wait(.08f); keys = Array.Empty<Key>();
                await Until(() => motor.IsDodging || Get<float>(motor, "airFlipTimer") > 0, .8f);
                Check(!attack.IsCombatMotionActive, weapon + " real dodge key cancels recovery and starts evasion");

                await Reset(weapon + " moving charge"); keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) }; right = true;
                Vector3 from = actor.transform.position; await Wait(.55f);
                Check(attack.IsCharging && attack.ChargeElapsed > .4f && Vector3.Distance(from, actor.transform.position) > .3f, weapon + " held RMB charges while moving");
                right = false; await Until(() => attack.IsHeavyAttackActive, .5f); Check(true, weapon + " real RMB release starts heavy strike");
                keys = Array.Empty<Key>(); await Until(() => !attack.IsCombatMotionActive, 4);

                await Reset(weapon + " simultaneous jump attack"); keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Jump) }; left = true;
                await Wait(.06f); left = false; keys = Array.Empty<Key>();
                await Until(() => attack.IsJumpSlashActive, .6f); Check(attack.CurrentComboStage < 0, weapon + " simultaneous jump/LMB selects aerial chop");
                await Until(() => motor.CanUseGroundAttack && !attack.IsCombatMotionActive, 4); Check(true, weapon + " jump chop lands and recovers");

                await Reset(weapon + " total ownership lock"); motor.ExternalControlLock = true; float before = stamina.Current;
                await TapLeft(); Check(!attack.IsCombatMotionActive && stamina.Current == before, weapon + " real LMB respects total control lock");
                motor.ExternalControlLock = false; await TapLeft(); Check(attack.IsGroundComboActive, weapon + " fresh LMB works after control returns");
            }
            await Reset("keyboard weapon switching"); keys = new[] { Key.Digit1 }; await Wait(.08f); keys = Array.Empty<Key>(); await Wait(.15f);
            Check(weapons.CurrentWeapon == SahurWeaponLoadout.Weapon.Stick, "Real digit1 equips stick");
            keys = new[] { Key.Digit2 }; await Wait(.08f); keys = Array.Empty<Key>(); await Wait(.2f);
            Check(weapons.CurrentWeapon == SahurWeaponLoadout.Weapon.CapriTwinBlades && attack.UsesTwinBlades, "Real digit2 equips twin blades continuously");
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            var finalSection = section; Restore();
            bool inputRestored = roster != null && roster.SequenceEqual(InputSystem.devices.Select(d => (d.deviceId, d.enabled)).ToArray()) && InputSystem.settings == oldSettings;
            Save("result.json", new { passed = error == null && inputRestored, error, finalSection, checks, inputRestored, boundaryCases, simulationHz, frames });
        }
    }
}
