using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifySahurEvasion
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    public static async Task<string> Run()
    {
        Check(EditorApplication.isPlaying && !PauseSettingsMenu.IsOpen, "Enter Play Mode with settings closed.");
        var originalKeyboard = Keyboard.current;
        var originalMouse = Mouse.current;
        var cursor = Cursor.lockState;
        bool visible = Cursor.visible;
        float timeScale = Time.timeScale;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var disabled = new List<Behaviour>();
        foreach (var component in UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
            if (component.enabled) { component.enabled = false; disabled.Add(component); }
        foreach (var component in UnityEngine.Object.FindObjectsByType<Mavis.SahurAttack>(FindObjectsSortMode.None))
            if (component.enabled) { component.enabled = false; disabled.Add(component); }
        var holder = new GameObject("Temporary evasion regression");
        holder.SetActive(false);
        holder.transform.position = new Vector3(2000f, 300f, 2000f);
        var floor = new GameObject("Temporary evasion floor");
        floor.transform.position = holder.transform.position - Vector3.up * 0.5f;
        floor.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var actor = UnityEngine.Object.Instantiate(prefab, holder.transform);
        actor.transform.localPosition = Vector3.up;
        var movement = actor.GetComponent<ThirdPersonPlayerController>();
        movement.enabled = false;
        movement.snapSpawnToIslandSurface = false;
        movement.waterSplashes = false;
        movement.cameraCollision = false;
        actor.GetComponent<Mavis.SahurAttack>().enabled = false;
        foreach (var input in actor.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
        var report = new StringBuilder();
        Key dodge = GameInputSettings.Get(GameInputSettings.Action.Dodge);
        Key jump = GameInputSettings.Get(GameInputSettings.Action.Jump);
        Key forward = GameInputSettings.Get(GameInputSettings.Action.Forward);
        Key sprint = GameInputSettings.Get(GameInputSettings.Action.Sprint);
        try
        {
            Time.timeScale = 1f;
            holder.SetActive(true);
            Call(movement, "Start");
            var capsule = actor.GetComponent<CharacterController>();
            float soleOffset = (capsule.center.y - capsule.height * 0.5f) * actor.transform.lossyScale.y;
            capsule.enabled = false;
            actor.transform.position = holder.transform.position + Vector3.up * (0.05f - soleOffset);
            capsule.enabled = true;
            Physics.SyncTransforms();
            async Task Step(params Key[] keys)
            {
                await Task.Delay(20);
                keyboard.MakeCurrent(); mouse.MakeCurrent();
                Cursor.lockState = CursorLockMode.Locked;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Call(movement, "Update");
                Call(movement, "UpdateAirFlipVisual");
            }
            async Task Settle()
            {
                for (int i = 0; i < 70; i++)
                {
                    await Step();
                    if (actor.GetComponent<CharacterController>().isGrounded && Get<float>(movement, "rollCooldownTimer") <= 0f &&
                        Get<float>(movement, "airFlipTimer") <= 0f) break;
                }
                Set(movement, "planarVelocity", Vector3.zero);
                Check(capsule.isGrounded, $"Test actor failed to settle on the temporary floor: y={actor.transform.position.y}, sole={soleOffset}, center={capsule.center}, height={capsule.height}.");
            }
            await Settle();
            actor.transform.rotation = Quaternion.Euler(0f, 67f, 0f);
            Set(movement, "yaw", -40f); // Stationary dodge must not follow the camera.
            Vector3 facing = actor.transform.forward;
            Vector3 start = actor.transform.position;
            await Step(dodge);
            Check(Get<float>(movement, "rollTimer") > 0f, $"Stationary tap did not start a roll: cursor={Cursor.lockState}, pressed={keyboard[dodge].wasPressedThisFrame}, held={keyboard[dodge].isPressed}, grounded={actor.GetComponent<CharacterController>().isGrounded}, y={actor.transform.position.y}, vy={Get<float>(movement, "verticalSpeed")}, dt={Time.deltaTime}, cooldown={Get<float>(movement, "rollCooldownTimer")}, swimming={typeof(ThirdPersonPlayerController).GetMethod("IsSwimming", Private).Invoke(movement, null)}.");
            Check(Mathf.Abs(Get<Vector3>(movement, "planarVelocity").magnitude - movement.rollSpeed) < 0.05f, "Stationary roll inherited unwanted momentum.");
            while (Get<float>(movement, "rollTimer") > 0f) await Step();
            Vector3 travel = Vector3.ProjectOnPlane(actor.transform.position - start, Vector3.up);
            Check(travel.magnitude > 1f && Vector3.Angle(travel, facing) < 1f, "Stationary roll did not travel along actor facing.");
            report.AppendLine($"Stationary Ctrl tap: {travel.magnitude:F2}m forward, heading error {Vector3.Angle(travel, facing):F2}deg.");

            foreach (bool running in new[] { false, true })
            {
                await Settle();
                Set(movement, "yaw", 0f);
                for (int i = 0; i < 35; i++) await Step(running ? new[] { forward, sprint } : new[] { forward });
                float baseSpeed = movement.moveSpeed * (running ? movement.sprintMultiplier : 1f);
                await Step(running ? new[] { forward, sprint, dodge } : new[] { forward, dodge });
                float initialSpeed = Get<Vector3>(movement, "planarVelocity").magnitude;
                Check(Mathf.Abs(initialSpeed - baseSpeed - movement.rollSpeed) < 0.15f, "Moving roll speed is not movement + dodge.");
                float lowestSpeed = initialSpeed;
                while (Get<float>(movement, "rollTimer") > 0f)
                {
                    await Step(running ? new[] { forward, sprint } : new[] { forward });
                    lowestSpeed = Mathf.Min(lowestSpeed, Get<Vector3>(movement, "planarVelocity").magnitude);
                }
                Check(lowestSpeed >= baseSpeed - 0.05f, "Roll braking also slowed normal movement.");
                report.AppendLine($"{(running ? "Sprint" : "Walk")} roll: {baseSpeed:F2} + {movement.rollSpeed:F2} = {initialSpeed:F2}m/s; exit {lowestSpeed:F2}m/s.");
            }

            async Task<float> JumpPeak(bool flip)
            {
                await Settle();
                float startY = actor.transform.position.y;
                await Step(jump);
                await Step();
                if (flip) await Step(dodge);
                float peak = actor.transform.position.y;
                bool survivedRelease = false;
                int frames = 0;
                for (; frames < 110; frames++)
                {
                    await Step();
                    peak = Mathf.Max(peak, actor.transform.position.y);
                    if (flip && frames == 0)
                    {
                        survivedRelease = Get<float>(movement, "airFlipTimer") > 0f;
                        float speedBeforeSecondTap = Get<float>(movement, "verticalSpeed");
                        await Step(dodge); // A second tap must not grant another air boost.
                        Check(Get<bool>(movement, "airFlipUsed"), "Air flip use latch was cleared before landing.");
                        Check(Get<float>(movement, "verticalSpeed") <= speedBeforeSecondTap, "A second Ctrl tap supplied another airborne boost.");
                    }
                    if (Get<float>(movement, "verticalSpeed") <= 0 && actor.GetComponent<CharacterController>().isGrounded) break;
                }
                Check(frames < 110, "Jump did not land.");
                if (flip) Check(survivedRelease, "Releasing Ctrl canceled the flip.");
                return peak - startY;
            }
            float normalPeak = await JumpPeak(false);
            float flipPeak = await JumpPeak(true);
            Check(flipPeak > normalPeak + 0.8f, "Air flip did not increase jump height sufficiently.");
            report.AppendLine($"Tap/release air flip: normal jump {normalPeak:F2}m, flip jump {flipPeak:F2}m; release does not cancel.");

            await Settle();
            await Step(jump);
            for (int i = 0; i < 70 && Get<float>(movement, "verticalSpeed") > 0f; i++) await Step();
            Check(!actor.GetComponent<CharacterController>().isGrounded, "Missed airborne descent test window.");
            await Step(dodge);
            Check(Get<float>(movement, "airFlipTimer") > 0f && Get<float>(movement, "verticalSpeed") > 0f, "Descending Ctrl tap did not trigger flip/lift.");
            await Step();
            Check(Get<float>(movement, "airFlipTimer") > 0f, "Descending flip canceled after release.");
            await Settle();
            report.AppendLine("Apex/descent Ctrl tap: accepted with upward lift; one flip per airborne cycle.");

            await Step(jump, dodge);
            Check(Get<float>(movement, "airFlipTimer") > 0f && Get<float>(movement, "rollTimer") <= 0f, "Simultaneous Jump+Ctrl incorrectly started a ground roll.");
            report.AppendLine("Jump+Ctrl simultaneous tap: jump + air flip, not a ground roll.");
            return report.ToString();
        }
        finally
        {
            UnityEngine.Object.Destroy(holder);
            UnityEngine.Object.Destroy(floor);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            originalKeyboard?.MakeCurrent(); originalMouse?.MakeCurrent();
            foreach (var component in disabled) if (component != null) component.enabled = true;
            Time.timeScale = timeScale;
            Cursor.lockState = cursor; Cursor.visible = visible;
        }
    }
}
