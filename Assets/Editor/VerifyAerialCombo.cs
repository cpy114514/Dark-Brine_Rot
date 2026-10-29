using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public sealed class AerialComboTestTarget : MonoBehaviour, IDamageable
{
    public ThirdPersonPlayerController player;
    public SahurAttack attack;
    public readonly List<int> hits = new List<int>();
    public readonly List<float> damage = new List<float>();
    void Update() { if (player) transform.position = player.transform.position + player.transform.forward * 2f + Vector3.up * 3f; }
    public void ApplyDamage(float amount, Vector3 point) { hits.Add(attack.CurrentAirComboStage); damage.Add(amount); }
}

// Runs real input against the current island in Play Mode; no scene data is saved.
public static class VerifyAerialCombo
{
    static Keyboard keyboard, previousKeyboard;
    static Mouse mouse, previousMouse;
    static Key[] keys = Array.Empty<Key>();
    static bool left;
    static ThirdPersonPlayerController player;
    static SahurAttack attack;
    static AerialComboTestTarget target;
    static string oldEnemyTag;
    static Task pending;
    static readonly List<int> stages = new List<int>();
    static readonly HashSet<int> activeWindows = new HashSet<int>();
    static readonly List<object> frames = new List<object>();
    static readonly HashSet<string> shots = new HashSet<string>();
    static string label;
    static readonly HumanBodyBones[] legBones = { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot };
    static Quaternion[] legPose;
    static float maxLegAngle;
    static int legSamples;
    static int legWarmFrames;
    const string Folder = "Temp/AerialComboReview";
    public static string Status = "idle";
    public static object Summary;

    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(keys));
        var state = new MouseState();
        if (left) state = state.WithButton(MouseButton.Left);
        InputState.Change(mouse, state);
    }

    static async Task Step(float seconds, bool click = false, params Key[] held)
    {
        keys = held; left = click;
        float end = Time.time + seconds;
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (Time.time < end)
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Play mode stopped or stalled");
            int stage = attack.CurrentAirComboStage;
            if (stage >= 0 && (stages.Count == 0 || stages[stages.Count - 1] != stage)) stages.Add(stage);
            if (stage >= 0 && attack.stickHitbox && attack.stickHitbox.enabled) activeWindows.Add(stage);
            var animator = attack.animator;
            int legLayer = animator.GetLayerIndex("Air Attack Legs");
            if (stage >= 0 && !attack.IsJumpSlashRecovering && !player.CanUseGroundAttack &&
                legLayer > 0 && animator.GetLayerWeight(legLayer) >= .99f)
            {
                if (++legWarmFrames > 4)
                {
                    var rotations = legBones.Select(b => animator.GetBoneTransform(b).localRotation).ToArray();
                    if (legPose == null) legPose = rotations;
                    else for (int i = 0; i < rotations.Length; i++)
                        maxLegAngle = Mathf.Max(maxLegAngle, Quaternion.Angle(legPose[i], rotations[i]));
                    legSamples++;
                }
            }
            if (stage >= 0)
            {
                string key = label + "-" + stage + "-" + (attack.CurrentAttackPhase < .38f ? "windup" : "strike");
                if (shots.Add(key)) ScreenCapture.CaptureScreenshot(Folder + "/" + key + ".png");
            }
            frames.Add(new {label, stage, phase=attack.CurrentAttackPhase, grounded=player.CanUseGroundAttack,
                vertical=player.VerticalSpeed, hitbox=attack.stickHitbox && attack.stickHitbox.enabled});
            await Task.Delay(15);
        }
    }

    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static object Start()
    {
        if (pending != null && !pending.IsCompleted) return "already running";
        pending = Run(); return "Native aerial combo playthrough started";
    }
    static async Task Run()
    {
        try
        {
            Directory.CreateDirectory(Folder); Status = "playing"; Summary = null; frames.Clear(); shots.Clear();
            player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            attack = player.GetComponent<SahurAttack>();
            oldEnemyTag = attack.enemyTag; attack.enemyTag = "";
            var dummy = new GameObject("Temporary aerial combo target");
            var box = dummy.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(8, 10, 8);
            target = dummy.AddComponent<AerialComboTestTarget>(); target.player = player; target.attack = attack;
            Check(player.GetComponent<MarshmallowTraversal>().Active, "Second island traversal must be active");
            previousKeyboard = Keyboard.current; previousMouse = Mouse.current;
            // PlayerInput is paired to the existing devices; inject into those
            // so its action callbacks see the same presses as a real player.
            keyboard = previousKeyboard; mouse = previousMouse;
            Check(keyboard != null && mouse != null, "Keyboard and mouse are available");
            InputSystem.onBeforeUpdate += Inject;
            var outcomes = new List<object>();
            foreach (int scenario in new[] { 0, 1, 2 })
            {
                bool blades = scenario == 1;
                var traversal = player.GetComponent<MarshmallowTraversal>();
                traversal.enabled = scenario != 2;
                label = scenario == 2 ? "normal-gravity" : blades ? "blades" : "stick";
                var loadout = player.GetComponent<SahurWeaponLoadout>();
                if (blades) loadout.Restore(true, SahurWeaponLoadout.Weapon.CapriTwinBlades);
                else Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.Stick), "Equip stick");
                await Step(.5f);
                Check(loadout.UsesTwinBlades == blades && player.CanUseGroundAttack, "Ready on island with requested weapon");
                player.GetComponent<PlayerStamina>().currentStamina = 100;
                stages.Clear(); activeWindows.Clear(); target.hits.Clear(); target.damage.Clear();
                legPose = null; maxLegAngle = 0; legSamples = 0; legWarmFrames = 0;
                await Step(.09f, false, GameInputSettings.Get(GameInputSettings.Action.Jump));
                await Step(.10f);
                await Step(.07f, true); await Step(.07f); await Step(.07f, true);
                await Step(.07f); await Step(.07f, true); await Step(2.4f);
                outcomes.Add(new {label, stages=stages.ToArray(), windows=activeWindows.OrderBy(x=>x).ToArray(),
                    hits=target.hits.ToArray(), damage=target.damage.ToArray(),
                    legSamples, maxLegAngle,
                    stamina=player.GetComponent<PlayerStamina>().Current, grounded=player.CanUseGroundAttack});
                Check(stages.SequenceEqual(new[] {0,1,2}), label + " stages: " + string.Join(",", stages));
                Check(activeWindows.SetEquals(new[] {0,1,2}), label + " damaging windows: " + string.Join(",", activeWindows));
                Check(target.hits.SequenceEqual(new[] {0,1,2}), label + " actual hits: " + string.Join(",", target.hits));
                Check(legSamples > 8 && maxLegAngle < 2f, label + " airborne legs moved: samples=" + legSamples + ", angle=" + maxLegAngle);
                Check(player.CanUseGroundAttack && !attack.IsJumpSlashActive, label + " returns to normal after landing");
            }
            player.GetComponent<MarshmallowTraversal>().enabled = true;
            await Step(.4f);
            label = "low-stamina"; stages.Clear(); activeWindows.Clear(); target.hits.Clear();
            var stamina = player.GetComponent<PlayerStamina>();
            stamina.currentStamina = stamina.jumpCost + stamina.jumpSlashCost;
            await Step(.09f, false, GameInputSettings.Get(GameInputSettings.Action.Jump));
            await Step(.10f); await Step(.07f, true); await Step(.07f); await Step(.07f, true);
            await Step(.07f); await Step(.07f, true); await Step(2.4f);
            Check(stages.SequenceEqual(new[] {0}), "Insufficient stamina must prevent later strikes: " + string.Join(",", stages));
            Check(player.CanUseGroundAttack && !attack.IsJumpSlashActive, "Low-stamina attack recovers");
            outcomes.Add(new {label, stages=stages.ToArray(), grounded=player.CanUseGroundAttack});

            label = "late-landing"; stages.Clear(); activeWindows.Clear(); target.hits.Clear();
            stamina.currentStamina = 100;
            await Step(.09f, false, GameInputSettings.Get(GameInputSettings.Action.Jump));
            await Step(1.65f); await Step(.07f, true); await Step(.07f); await Step(.07f, true);
            await Step(.07f); await Step(.07f, true); await Step(1.3f);
            Check(stages.SequenceEqual(new[] {0}), "Landing must discard queued aerial strikes: " + string.Join(",", stages));
            Check(player.CanUseGroundAttack && !attack.IsJumpSlashActive, "Late attack recovers after landing");
            outcomes.Add(new {label, stages=stages.ToArray(), grounded=player.CanUseGroundAttack});
            Summary = outcomes; Status = "completed";
        }
        catch (Exception e) { Status = "failed: " + e; }
        finally
        {
            if (player && player.GetComponent<MarshmallowTraversal>()) player.GetComponent<MarshmallowTraversal>().enabled = true;
            if (attack) attack.enemyTag = oldEnemyTag;
            if (target) UnityEngine.Object.Destroy(target.gameObject);
            left = false; keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject;
            keyboard = null; mouse = null;
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            File.WriteAllText(Folder + "/frames.json", Newtonsoft.Json.JsonConvert.SerializeObject(frames, Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(Folder + "/result.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {Status, Summary}, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
