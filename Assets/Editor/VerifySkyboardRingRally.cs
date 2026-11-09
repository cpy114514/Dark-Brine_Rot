using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Runs the circuit with the actual board controls and an isolated save.</summary>
public static class VerifySkyboardRingRally
{
    public static string Status = "idle", Result = "";
    static Keyboard keyboard;
    static ThirdPersonPlayerController player;
    static CharacterController body;
    static SkyboardRingRally rally;
    static SkyboardReward board;
    static bool steer;
    static Key[] keys = Array.Empty<Key>();
    static readonly FieldInfo Yaw = typeof(ThirdPersonPlayerController).GetField("yaw", BindingFlags.Instance | BindingFlags.NonPublic);

    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        if (steer && rally.Running && board.Flying)
        {
            var gate = rally.RingTransform(rally.NextRing);
            if (gate)
            {
                Vector3 center = body.bounds.center;
                Vector3 delta = gate.position + gate.forward * 6 - center;
                Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
                if (flat.sqrMagnitude > .01f) Yaw.SetValue(player, Quaternion.LookRotation(flat).eulerAngles.y);
                float vertical = gate.position.y - center.y;
                var forward = GameInputSettings.Get(GameInputSettings.Action.Forward);
                keys = vertical > .7f ? new[] { forward, GameInputSettings.Get(GameInputSettings.Action.Jump) } :
                    vertical < -.7f ? new[] { forward, GameInputSettings.Get(GameInputSettings.Action.Dodge) } : new[] { forward };
            }
        }
        InputState.Change(keyboard, new KeyboardState(keys));
    }

    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds;
        await Until(() => Time.time >= end, seconds + 8, "wait");
    }

    static async Task Until(Func<bool> condition, float seconds, string step)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup > end) throw new Exception("Timed out at " + step);
            await Task.Delay(20);
        }
    }

    static async Task Tap(Key key)
    {
        keys = new[] { key }; await Wait(.13f); keys = Array.Empty<Key>(); await Wait(.15f);
    }

    static void Place(Vector3 desiredCenter, Quaternion facing)
    {
        player.RestoreSavedPose(desiredCenter - (body.bounds.center - player.transform.position), facing);
        board.RefreshRallyPosition();
    }

    static void CheckVisibleGate(int expected)
    {
        for (int i = 0; i < rally.TotalRings; i++)
            if (rally.RingTransform(i).gameObject.activeInHierarchy != (i == expected)) throw new Exception("Wrong active gate at index " + i);
    }

    public static async void Run()
    {
        Status = "running"; Result = "";
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Enter Play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, Path.GetFullPath("Temp/skyboard-circuit-test.json"));
            var opening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if (opening) UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.5f);
            player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            rally = UnityEngine.Object.FindFirstObjectByType<SkyboardRingRally>();
            board = UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            body = player.GetComponent<CharacterController>(); keyboard = Keyboard.current;
            if (!rally || !board || keyboard == null || rally.TotalRings != 10 || !board.CollectSpawnTestKit()) throw new Exception("Missing board or circuit.");
            player.GetComponent<PlayerHealth>().GrantProtection(120);
            var camera = Camera.main;
            var clearFlags = camera.clearFlags;
            var background = camera.backgroundColor;
            var island = player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var entrance = rally.StartMarker.position;
            if (!island.Raycast(new Ray(entrance + new Vector3(0, 100, -5), Vector3.down), out var hit, 200)) throw new Exception("No ground at sign.");
            Place(hit.point + Vector3.up * (body.bounds.extents.y + .12f), Quaternion.identity);
            CheckVisibleGate(-1);
            InputSystem.onBeforeUpdate += Inject; await Wait(.35f);

            Status = "sign, teleport and countdown";
            board.RestoreProgress(false, 180);
            await Tap(Key.F);
            if (!rally.InRace || rally.Running || !board.Flying || !board.RallySteeringLocked || rally.EntranceRing.gameObject.activeSelf ||
                Vector3.Distance(entrance, player.transform.position) < 1000 || GameSaveManager.SaveCurrentGame())
                throw new Exception("Race start, teleport, flight lock or save isolation failed.");
            if (camera.clearFlags != CameraClearFlags.SolidColor) throw new Exception("Race backdrop did not switch.");
            CheckVisibleGate(0);
            var initial = body.bounds.center;
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) }; await Wait(.55f); keys = Array.Empty<Key>();
            if (Vector3.Distance(body.bounds.center, initial) > .2f || rally.NextRing != 0) throw new Exception("The board could move during countdown.");
            await Until(() => rally.Running, 5, "green light");
            if (board.RallySteeringLocked) throw new Exception("The board stayed locked at GO.");

            Status = "direction, moving gate and recovery";
            var first = rally.RingTransform(0);
            var mesh = first.GetComponent<MeshCollider>();
            if (!mesh || !first.GetComponent<BoxCollider>().isTrigger ||
                !mesh.Raycast(new Ray(first.position + first.right * 6.5f + first.forward * 4, -first.forward), out _, 8) ||
                mesh.Raycast(new Ray(first.position + first.forward * 4, -first.forward), out _, 8)) throw new Exception("Gate rim or opening collision is incorrect.");
            Place(first.position + first.forward * 6, Quaternion.LookRotation(-first.forward)); await Wait(.2f);
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) }; await Wait(.75f); keys = Array.Empty<Key>();
            if (rally.NextRing != 0) throw new Exception("Reverse crossing counted.");
            Vector3 oldGate = first.position;
            await Wait(Mathf.Max(.1f, rally.TimeUntilMove - .45f));
            if (rally.Relocating || Vector3.Distance(first.position, oldGate) > .05f) throw new Exception("Gate moved before 15 seconds.");
            await Until(() => rally.Relocating, 3, "15-second move");
            await Until(() => !rally.Relocating, 3, "gate settles");
            float shift = Vector3.Distance(first.position, oldGate);
            if (shift < 1 || shift > 100 || rally.NextRing != 0) throw new Exception("Incorrect gate shift or score.");
            CheckVisibleGate(0);
            float beforeRecovery = rally.Elapsed;
            Place(body.bounds.center - Vector3.up * 60, Quaternion.identity); await Wait(.4f);
            if (!rally.Running || rally.Elapsed < beforeRecovery + 3 || Vector3.Distance(body.bounds.center, initial) > .5f)
                throw new Exception("Out-of-bounds recovery or time penalty failed.");
            await Tap(Key.R);
            if (!rally.InRace || rally.Running || rally.NextRing != 0 || !board.RallySteeringLocked || rally.Elapsed > .1f)
                throw new Exception("Restart did not reset the grid and clock.");
            await Wait(.15f);
            await Tap(Key.F);
            if (rally.InRace || board.Flying || Mathf.Abs(board.CooldownRemaining - 180) > 2 || Vector3.Distance(entrance, player.transform.position) > 15 ||
                camera.clearFlags != clearFlags || camera.backgroundColor != background || !rally.EntranceRing.gameObject.activeSelf)
                throw new Exception("Exit did not restore the entrance, camera or board cooldown.");

            Status = "portal entry";
            board.RestoreProgress(false, 0);
            if (!board.TryStartFlight()) throw new Exception("Regular board did not launch.");
            var portal = rally.EntranceRing;
            Place(portal.position - portal.forward * 8, portal.rotation); await Wait(.2f);
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) };
            await Until(() => rally.InRace, 4, "forward portal crossing"); keys = Array.Empty<Key>();
            if (!board.RallyFlight || rally.NextRing != 0) throw new Exception("Portal did not start a separate race.");
            await Until(() => rally.Running, 5, "second green light");
            int beforeScrolls = board.Scrolls;
            float speed = board.horizontalSpeed;
            int prior = -1; float changed = Time.time; bool sawBoost = false;
            steer = true;
            while (rally.InRace && Time.time - changed < 9 && board.Flying)
            {
                if (board.horizontalSpeed > speed * 1.1f) sawBoost = true;
                if (rally.NextRing != prior)
                {
                    prior = rally.NextRing; changed = Time.time;
                    Status = "flying ring " + (prior + 1) + "/10";
                    CheckVisibleGate(prior);
                }
                await Task.Delay(25);
            }
            steer = false; keys = Array.Empty<Key>(); await Wait(.3f);
            if (rally.InRace || board.Flying || !rally.RewardClaimed || !sawBoost || board.Scrolls != beforeScrolls + 1 ||
                board.CooldownRemaining < board.cooldownSeconds - 3 || Mathf.Abs(board.horizontalSpeed - speed) > .01f ||
                Vector3.Distance(entrance, player.transform.position) > 15)
                throw new Exception($"Full race failed: progress {rally.NextRing}, best {rally.BestTime}, boost {sawBoost}, scrolls {board.Scrolls}.");
            float best = rally.BestTime;
            int scrolls = board.Scrolls;
            CheckVisibleGate(-1);
            if (!GameSaveManager.QueueContinue()) throw new Exception("Could not queue the race result save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            await Until(() => !GameSaveManager.IsContinuePending, 16, "Continue"); await Wait(.5f);
            rally = UnityEngine.Object.FindFirstObjectByType<SkyboardRingRally>();
            board = UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!rally.RewardClaimed || Mathf.Abs(rally.BestTime - best) > .1f || board.Scrolls != scrolls || Vector3.Distance(entrance, player.transform.position) > 15)
                throw new Exception("Continue did not restore the result and island position.");
            Status = "passed";
            Result = $"Sign and portal entry, locked countdown, one-way gates, {shift:F1}-unit move after 15 seconds, recovery, restart, exit, all 10 gates ({best:F1}s), boost, cooldown, reward and Continue passed.";
        }
        catch (Exception e) { Status = "failed"; Result = e.ToString(); }
        finally
        {
            steer = false; keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject;
            if (keyboard != null) InputState.Change(keyboard, new KeyboardState());
        }
        Debug.Log("Skyboard circuit test: " + Status + " " + Result);
    }
}
