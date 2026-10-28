using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Mavis;

public sealed class AutoQaContactProbe : MonoBehaviour, IDamageable
{
    public int hits;
    public void ApplyDamage(float amount, Vector3 point) { hits++; }
}

public static class AutoQaCombatControlRegression
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);

    public static object Run(string label)
    {
        if (!Application.isPlaying || Time.deltaTime <= 0 || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene with positive native delta");
        var checks = new List<object>(); var observations = new List<object>(); string error = null;
        void Check(bool ok, string name) { checks.Add(new { name, passed = ok }); }
        var oldKeyboard = Keyboard.current; var oldMouse = Mouse.current;
        var roster = InputSystem.devices.Select(d => (d.deviceId, d.enabled)).ToArray();
        var oldCursor = Cursor.lockState; bool oldVisible = Cursor.visible;
        var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c => (camera:c, tag:c.tag)).ToArray();
        GameObject container = null; Keyboard keyboard = null; Mouse mouse = null;
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>("AutoQaControlKeyboard"); mouse = InputSystem.AddDevice<Mouse>("AutoQaControlMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent(); InputState.Change(keyboard, new KeyboardState()); InputState.Change(mouse, new MouseState());
            container = new GameObject("AUTO_QA Combat Control"); container.SetActive(false); container.hideFlags = HideFlags.DontSave;
            container.transform.position = new Vector3(12000, 500, 12000);
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), container.transform);
            actor.AddComponent<GameSaveExcluded>();
            foreach (var script in actor.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            container.SetActive(true);
            foreach (var script in actor.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            var motor = actor.GetComponent<ThirdPersonPlayerController>(); var attack = actor.GetComponent<SahurAttack>();
            var weapons = actor.GetComponent<SahurWeaponLoadout>(); var stamina = actor.GetComponent<PlayerStamina>(); var body = actor.GetComponent<CharacterController>();
            motor.enabled = attack.enabled = weapons.enabled = true; motor.seaLevel = -1000; motor.waterSplashes = false;
            foreach (var c in cameras) if (c.tag == "MainCamera") c.camera.tag = "Untagged";
            var camera = new GameObject("AUTO_QA Contact Camera"); camera.transform.SetParent(container.transform, false); camera.AddComponent<Camera>().enabled = false; camera.tag = "MainCamera";
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(container.transform, false); floor.transform.localPosition = Vector3.down * .5f; floor.transform.localScale = new Vector3(200, 1, 200);
            var home = new Vector3(12000, 500.03f - (body.center.y - body.height * .5f) * actor.transform.lossyScale.y, 12000);
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere); target.transform.SetParent(container.transform, false); target.tag = "Enemy";
            var probe = target.AddComponent<AutoQaContactProbe>(); target.AddComponent<GameSaveExcluded>(); var targetCollider = target.GetComponent<Collider>(); targetCollider.isTrigger = true;
            void Reset()
            {
                keyboard.MakeCurrent(); mouse.MakeCurrent(); InputState.Change(keyboard, new KeyboardState()); InputState.Change(mouse, new MouseState());
                motor.enabled = true; motor.ExternalControlLock = motor.ExternalMovementLock = false; attack.CinematicHandoffPending = false;
                attack.SuspendForSwimming(); actor.GetComponent<CombatLocalHitStop>()?.Release();
                motor.RestoreSavedPose(home, Quaternion.identity); stamina.currentStamina = 100;
                target.transform.position = home + Vector3.up * 30; probe.hits = 0;
                Physics.SyncTransforms(); body.Move(Vector3.down * .2f); Set(motor, "verticalSpeed", -motor.groundStickSpeed);
                Set(attack, "movementCancelFrame", -1); Set(attack, "lastAttackInputFrame", -1); Set(attack, "lastFireTime", Time.time - attack.cooldown - 1);
                Cursor.lockState = CursorLockMode.Locked;
            }
            void Queue() { Set(attack, "attackQueued", true); Set(attack, "queueExpiresAt", Time.time + 1); }
            string[] controllers = { "Assets/Resources/Encounter/v006/SahurGameplay.controller", "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller", "Assets/Resources/Equipment/M4A1/SahurRifle.controller" };
            foreach (string controller in controllers) foreach (bool blades in new[] { false, true })
            {
                Reset();
                attack.SuspendForSwimming(); attack.animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controller); attack.animator.Rebind();
                weapons.Restore(true, blades ? SahurWeaponLoadout.Weapon.CapriTwinBlades : SahurWeaponLoadout.Weapon.Stick);
                Reset();
                string context = controller + (blades ? " blades: " : " club: ");
                if (weapons.UsesTwinBlades != blades || attack.UsesTwinBlades != blades) throw new Exception("Fixture did not select requested weapon: " + context);
                Check(true, context + "requested native weapon mode confirmed");
                if (!motor.CanUseGroundAttack) throw new Exception("Native fixture is not grounded: " + context);
                attack.TriggerAttack(); Check(attack.IsGroundComboActive, context + "unlocked callback starts combo");
                float spent = stamina.Current; attack.TriggerAttack(); Check(stamina.Current == spent && attack.CurrentComboStage == 0, context + "duplicate same-frame callback does not spend or advance twice");
                Reset(); motor.ExternalControlLock = true; float before = stamina.Current; attack.TriggerAttack();
                observations.Add(new { controller, blades, test = "locked callback", attacking = attack.IsCombatMotionActive, staminaSpent = before - stamina.Current });
                Check(!attack.IsCombatMotionActive && stamina.Current == before, context + "total control lock rejects light callback without cost");
                Reset(); motor.ExternalControlLock = true; Queue(); before = stamina.Current; Call(attack, "Update");
                observations.Add(new { controller, blades, test = "locked buffer", attacking = attack.IsCombatMotionActive, queued = Get<bool>(attack, "attackQueued"), staminaSpent = before - stamina.Current });
                Check(!attack.IsCombatMotionActive && !Get<bool>(attack, "attackQueued") && stamina.Current == before, context + "total control lock discards pending attack without cost");
                Reset(); motor.enabled = false; Cursor.lockState = CursorLockMode.Locked; before = stamina.Current; attack.TriggerAttack();
                Check(!attack.IsCombatMotionActive && stamina.Current == before, context + "disabled movement owner rejects attack callback");
                Reset(); motor.ExternalControlLock = true; Set(motor, "verticalSpeed", 2f); before = stamina.Current; attack.TriggerAttack();
                Check(!attack.IsCombatMotionActive && stamina.Current == before, context + "total control lock rejects aerial callback");
                Reset(); attack.CinematicHandoffPending = true; attack.TriggerAttack(); Check(!attack.IsCombatMotionActive, context + "existing cinematic handoff gate retained");
                Reset(); Queue(); Set(attack, "queueExpiresAt", Time.time - .1f); Call(attack, "Update"); Check(!attack.IsCombatMotionActive && !Get<bool>(attack, "attackQueued"), context + "expired normal attack buffer is discarded");
                Reset(); Queue(); Call(attack, "Update"); Check(attack.IsGroundComboActive, context + "valid unlocked attack buffer is consumed");
                Reset(); motor.ExternalMovementLock = true; attack.TriggerAttack(); Check(attack.IsGroundComboActive, context + "movement-only platform lock preserves existing light-attack behavior");
                Reset(); attack.TriggerAttack(); int hash = Get<int>(attack, "activeAttackHash");
                attack.animator.Play(hash, 0, .7f); attack.animator.Update(0); Set(attack, "attackStartedAt", Time.time - 1); Set(attack, "activeAttackEntered", true);
                motor.ExternalControlLock = true; Call(attack, "LateUpdate");
                Check(!attack.stickHitbox.enabled, context + "total control lock prevents late hitbox reactivation");
                // Confirm real collider penetration, then deliver the trigger callback directly.
                // This isolates the ownership filter; it is not an input/rendered playthrough.
                attack.stickHitbox.enabled = true; Physics.SyncTransforms(); target.transform.position = attack.stickHitbox.bounds.center; Physics.SyncTransforms();
                bool penetration = Physics.ComputePenetration(attack.stickHitbox, attack.stickHitbox.transform.position, attack.stickHitbox.transform.rotation, targetCollider, target.transform.position, target.transform.rotation, out _, out _);
                Check(penetration, context + "contact fixture has native weapon/target penetration");
                probe.hits = 0; Call(attack, "OnTriggerEnter", targetCollider);
                observations.Add(new { controller, blades, test = "locked trigger contact", penetration, hits = probe.hits });
                Check(probe.hits == 0, context + "total control lock rejects already-delivered trigger contact");
                motor.ExternalControlLock = false; Call(attack, "OnTriggerEnter", targetCollider);
                Check(probe.hits == 1, context + "returning control permits normal confirmed contact");
                Call(attack, "OnTriggerEnter", targetCollider);
                Check(probe.hits == 1, context + "repeated confirmed contact still hits once per swing");
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (container) UnityEngine.Object.DestroyImmediate(container);
            foreach (var c in cameras) if (c.camera) c.camera.tag = c.tag;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent(); if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            Cursor.lockState = oldCursor; Cursor.visible = oldVisible;
        }
        bool inputRestored = roster.SequenceEqual(InputSystem.devices.Select(d => (d.deviceId, d.enabled)).ToArray());
        var serialized = checks.Select(c => Newtonsoft.Json.Linq.JObject.FromObject(c)).ToArray(); int failed = serialized.Count(c => !(bool)c["passed"]);
        var result = new { passed = error == null && failed == 0 && inputRestored, error, failed, inputRestored, checks, observations };
        Directory.CreateDirectory("AUTO_QA/Evidence"); File.WriteAllText("AUTO_QA/Evidence/combat-control-" + label + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { result.passed, error, failed, checks = checks.Count, inputRestored };
    }
}
