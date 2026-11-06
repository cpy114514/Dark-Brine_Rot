using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Traverses the actual course using jump and movement input in Play Mode.</summary>
public static class VerifyMarshmallowHopChallenge
{
    public static string Status = "idle", Result = "";
    static Keyboard keyboard;
    static Key[] pressed = Array.Empty<Key>();
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(pressed));
    }
    static async Task Wait(float seconds)
    {
        float until = Time.time + seconds;
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (Time.time < until)
        {
            if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(20);
        }
    }
    public static async void Run(bool normalGravity, bool leftBranch = false)
    {
        Status = "running"; Result = "";
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Enter play mode first.");
            var opening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if (opening) UnityEngine.Object.DestroyImmediate(opening.gameObject);
            var guide = UnityEngine.Object.FindFirstObjectByType<MarshmallowChallengeGuide>();
            if (guide) guide.enabled = false;
            var enemies = GameObject.Find("Marshmallow Friends - Second Island"); if (enemies) enemies.SetActive(false);
            var boss = UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>(); if (boss) boss.gameObject.SetActive(false);
            var root = GameObject.Find("Marshmallow Hop Challenge - 33");
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!root || !player) throw new Exception("Course or player missing.");
            var body = player.GetComponent<CharacterController>();
            var stamina = player.GetComponent<Mavis.PlayerStamina>();
            var traversal = player.GetComponent<MarshmallowTraversal>();
            var recovery = root.GetComponent<MarshmallowHopRecovery>();
            int initialRespawns = recovery ? recovery.RespawnCount : 0;
            keyboard = Keyboard.current;
            if (!body || !stamina || !traversal || keyboard == null) throw new Exception("Player components missing.");
            var rows = new List<Transform>();
            foreach (Transform child in root.transform) if (child.name.StartsWith("Row ")) rows.Add(child);
            if (rows.Count != 22) throw new Exception("The course must contain 22 rows.");
            int pieces = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                int count = i % 2 == 0 ? 1 : 2;
                if (rows[i].childCount != count) throw new Exception("Incorrect row " + (i + 1));
                pieces += count;
            }
            if (pieces != 33) throw new Exception("Course must contain exactly 33 marshmallows.");
            traversal.SetNormalGravity(normalGravity);
            InputSystem.onBeforeUpdate += Inject;
            player.GetComponent<Mavis.PlayerHealth>().GrantProtection(120);
            typeof(ThirdPersonPlayerController).GetField("yaw", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 0f);
            var first = rows[0].GetChild(0).GetComponentInChildren<BoxCollider>();
            float rootOffset = -(body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
            var experience = player.GetComponent<MarshmallowIslandExperience>();
            var ground = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
            Vector3 starting = first.bounds.center - Vector3.forward * 5f;
            if (!ground || !ground.Raycast(new Ray(starting + Vector3.up * 100, Vector3.down), out var groundHit, 250))
                throw new Exception("No walkable ground before the first row.");
            starting.y = groundHit.point.y + rootOffset + .04f;
            player.RestoreSavedPose(starting, Quaternion.identity);
            stamina.currentStamina = stamina.maxStamina;
            await Wait(.7f);
            if (!body.isGrounded) throw new Exception("Could not stand on the ground before the challenge.");
            pressed = new[] { GameInputSettings.Get(GameInputSettings.Action.Jump), GameInputSettings.Get(GameInputSettings.Action.Forward) };
            await Wait(.11f);
            bool entryAirborne = !body.isGrounded, entered = false;
            float entryDeadline = Time.time + 6;
            while (Time.time < entryDeadline)
            {
                float dz = first.bounds.center.z - player.transform.position.z;
                pressed = dz > .7f ? new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) } :
                    dz < -.7f ? new[] { GameInputSettings.Get(GameInputSettings.Action.Back) } : Array.Empty<Key>();
                entryAirborne |= !body.isGrounded;
                if (entryAirborne && body.isGrounded && Mathf.Abs(body.bounds.min.y - first.bounds.max.y) < .20f &&
                    Mathf.Abs(dz) < 1.3f) { entered = true; break; }
                if (body.bounds.min.y < first.bounds.max.y - 6) break;
                await Task.Delay(20);
            }
            pressed = Array.Empty<Key>();
            if (!entered) throw new Exception($"Could not jump from the island to the first row: player={player.transform.position}, feet={body.bounds.min.y:F2}, target={first.bounds.center}, top={first.bounds.max.y:F2}.");
            await Wait(.22f);
            if (recovery && (recovery.CurrentRow != 1 || !recovery.RecoveryArmed))
                throw new Exception($"Starting row was tracked incorrectly: row={recovery.CurrentRow}, armed={recovery.RecoveryArmed}.");
            Result = $"33 pieces in 1/2 rows; {(normalGravity ? "normal" : "low")} gravity; {(leftBranch ? "left" : "right")} branch; island to 1; ";

            for (int i = 1; i < rows.Count; i++)
            {
                // Rest naturally when needed; each jump and movement still costs SP.
                if (stamina.currentStamina < stamina.jumpCost + 8f)
                {
                    pressed = Array.Empty<Key>();
                    await Wait(1.7f);
                }
                var destination = rows[i].GetChild(i % 2 == 0 || leftBranch ? 0 : 1).GetComponentInChildren<BoxCollider>();
                Vector3 target = destination.bounds.center;
                float targetTop = destination.bounds.max.y;
                Vector3 initialDelta = target - player.transform.position;
                var takeoff = new List<Key> { GameInputSettings.Get(GameInputSettings.Action.Jump) };
                if (initialDelta.z > .7f) takeoff.Add(GameInputSettings.Get(GameInputSettings.Action.Forward));
                if (initialDelta.x > .7f) takeoff.Add(GameInputSettings.Get(GameInputSettings.Action.Right));
                else if (initialDelta.x < -.7f) takeoff.Add(GameInputSettings.Get(GameInputSettings.Action.Left));
                pressed = takeoff.ToArray();
                await Wait(.11f);
                bool airborne = !body.isGrounded;
                float deadline = Time.time + 6f;
                bool landed = false;
                while (Time.time < deadline)
                {
                    // The target is drifting; aim at its current position and height.
                    target = destination.bounds.center;
                    targetTop = destination.bounds.max.y;
                    Vector3 delta = target - player.transform.position;
                    var keys = new List<Key>();
                    if (delta.z > .7f) keys.Add(GameInputSettings.Get(GameInputSettings.Action.Forward));
                    else if (delta.z < -.7f) keys.Add(GameInputSettings.Get(GameInputSettings.Action.Back));
                    if (delta.x > .7f) keys.Add(GameInputSettings.Get(GameInputSettings.Action.Right));
                    else if (delta.x < -.7f) keys.Add(GameInputSettings.Get(GameInputSettings.Action.Left));
                    pressed = keys.ToArray();
                    airborne |= !body.isGrounded;
                    if (airborne && body.isGrounded && Mathf.Abs(body.bounds.min.y - targetTop) < .20f &&
                        Mathf.Abs(delta.x) < 1.5f && Mathf.Abs(delta.z) < 1.5f)
                    { landed = true; break; }
                    if (body.bounds.min.y < targetTop - 6f) break;
                    await Task.Delay(20);
                }
                pressed = Array.Empty<Key>();
                if (!landed)
                    throw new Exception($"Failed jump to row {i + 1}: player={player.transform.position}, feet={body.bounds.min.y:F2}, target={target}, top={targetTop:F2}, grounded={body.isGrounded}, stamina={stamina.currentStamina:F1}.");
                await Wait(.22f);
                if (recovery && (recovery.CurrentRow != i + 1 || i >= 2 && !recovery.RecoveryArmed))
                    throw new Exception($"Landing on row {i + 1} was not tracked: row={recovery.CurrentRow}, armed={recovery.RecoveryArmed}.");
                Result += (i + 1) + " ";
            }
            Result += "reached finish; ";
            var motion = root.GetComponent<MarshmallowHopMotion>();
            if (!motion || motion.PlatformCount != 33) throw new Exception("The moving-platform controller is missing.");
            var all = new List<Transform>();
            foreach (var row in rows) foreach (Transform tile in row) all.Add(tile);
            var previous = new Vector3[all.Count];
            var travel = new float[all.Count];
            for (int i = 0; i < all.Count; i++) previous[i] = all[i].position;
            var last = rows[rows.Count - 1].GetChild(leftBranch ? 0 : 1);
            Vector3 relativeStart = player.transform.position - last.position;
            int supportedSamples = 0;
            float auditEnd = Time.time + 4f;
            while (Time.time < auditEnd)
            {
                await Task.Delay(100);
                for (int i = 0; i < all.Count; i++)
                {
                    travel[i] += Vector3.Distance(previous[i], all[i].position);
                    previous[i] = all[i].position;
                }
                if (motion.LastSupportIndex >= 0) supportedSamples++;
            }
            float minTravel = float.PositiveInfinity;
            foreach (float distance in travel) minTravel = Mathf.Min(minTravel, distance);
            float riderDrift = Vector3.Distance(relativeStart, player.transform.position - last.position);
            if (minTravel < .025f) throw new Exception($"At least one marshmallow did not move: minimum travel {minTravel:F3}.");
            if (!body.isGrounded || supportedSamples < 3 || riderDrift > .35f)
                throw new Exception($"Standing on the moving marshmallow was unstable: grounded={body.isGrounded}, support samples={supportedSamples}, relative drift={riderDrift:F3}.");
            if (recovery && recovery.RespawnCount != initialRespawns)
                throw new Exception("A normal jump caused an unwanted course recovery.");
            Result += $"33 move (minimum travel {minTravel:F2}); rider follows (drift {riderDrift:F2})";
            Status = "passed";
        }
        catch (Exception e) { Status = "failed"; Result += "FAILED: " + e; }
        finally
        {
            pressed = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject;
            if (keyboard != null) InputState.Change(keyboard, new KeyboardState());
        }
        File.WriteAllText("Temp/MarshmallowHopReview-" + (normalGravity ? "normal" : "low") +
            (leftBranch ? "-left" : "-right") + ".txt", Status + "\n" + Result);
        Debug.Log("Marshmallow hop review: " + Status + " " + Result);
    }
}
