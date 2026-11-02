using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Exercises the opening safety gate and the board against the real island collider.</summary>
public static class VerifySecondIslandPeaceAndBoard
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
        float end = Time.time + seconds; var deadline = DateTime.UtcNow.AddSeconds(Math.Max(15, seconds * 4));
        while (Time.time < end)
        {
            if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(20);
        }
    }
    static async Task Until(Func<bool> condition, float timeout, string message)
    {
        float end = Time.time + timeout;
        while (!condition() && Time.time < end) await Task.Delay(20);
        if (!condition()) throw new Exception(message);
    }
    static void Progress(string message)
    {
        Status = message; File.WriteAllText("Temp/SecondIslandPeaceBoard.progress", message);
    }
    public static async void Run()
    {
        Status = "running"; Result = "";
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Enter play mode first.");
            await Wait(.3f);
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var hp = player ? player.GetComponent<PlayerHealth>() : null;
            var body = player ? player.GetComponent<CharacterController>() : null;
            var protection = player ? player.GetComponent<SecondIslandStartProtection>() : null;
            var opening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            var reward = UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            var surface = player ? player.GetComponent<MarshmallowIslandExperience>()?.surface : null;
            var island = surface ? surface.GetComponent<Collider>() : null;
            var group = GameObject.Find("Marshmallow Friends - Second Island");
            if (!player || !hp || !body || !protection || !opening || !reward || !island || !group)
                throw new Exception("The opening, player, protection, island, board or enemies are missing.");
            var all = group.GetComponentsInChildren<ChickenBroAI>();
            if (all.Length == 0) throw new Exception("No marshmallow enemies found.");
            var enemy = all[0];
            for (int i = 1; i < all.Length; i++) all[i].gameObject.SetActive(false);
            var boss = UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>(); if (boss) boss.gameObject.SetActive(false);
            var enemyBody = enemy.GetComponent<CharacterController>();
            float enemyFootOffset = enemy.transform.position.y - enemyBody.bounds.min.y;
            void PlaceEnemy()
            {
                Vector3 at = player.transform.position + Vector3.forward * 1.8f;
                if (!island.Raycast(new Ray(at + Vector3.up * 20, Vector3.down), out var ground, 45)) throw new Exception("No ground near player.");
                at.y = ground.point.y + enemyFootOffset + .06f;
                enemy.Activate(at, Quaternion.identity, null); Physics.SyncTransforms();
            }
            keyboard = Keyboard.current; if (keyboard == null) throw new Exception("No keyboard available.");
            InputSystem.onBeforeUpdate += Inject;
            PlaceEnemy(); float initialHealth = hp.currentHealth;
            await Until(() => opening && opening.Dialogue && opening.Dialogue.Active, 20, "Opening dialogue did not start.");
            if (!protection.WaitingForMovement || !SecondIslandStartProtection.Protects(hp)) throw new Exception("No protection during dialogue.");
            PlaceEnemy();
            float dialogueWait = Time.time + 1.7f;
            while (Time.time < dialogueWait)
            {
                if (enemy.HoldsAttack || hp.currentHealth < initialHealth) throw new Exception("Marshmallow attacked during dialogue.");
                await Task.Delay(20);
            }
            Progress("Dialogue protected; completing conversation without moving.");
            int iterations = 0;
            while (opening && opening.Dialogue && opening.Dialogue.Active && iterations++ < 30)
            {
                pressed = Array.Empty<Key>(); await Wait(.21f);
                bool last = opening.Dialogue.CurrentLine == SecondIslandDialogue.Lines.Length - 1;
                pressed = new[] { last ? Key.Space : Key.Enter }; await Wait(.10f);
                if (last)
                {
                    await Until(() => !opening || !opening.Playing, 5, "Last dialogue line did not finish.");
                    await Wait(.3f);
                    if (!protection.WaitingForMovement) throw new Exception("Holding the dialogue-confirm key released protection without movement.");
                }
                pressed = Array.Empty<Key>(); await Wait(.13f);
            }
            await Until(() => !opening || !opening.Playing, 5, "Dialogue did not finish.");
            if (!protection.WaitingForMovement) throw new Exception("Protection ended simply by dismissing dialogue.");
            PlaceEnemy();
            float idleEnd = Time.time + 2f;
            while (Time.time < idleEnd)
            {
                if (!protection.WaitingForMovement || enemy.HoldsAttack || hp.currentHealth < initialHealth)
                    throw new Exception("Marshmallow attacked while the player remained idle after dialogue.");
                await Task.Delay(20);
            }
            var start = player.transform.position;
            pressed = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward) };
            await Until(() => !protection.WaitingForMovement, 4, "First intentional movement did not release protection.");
            pressed = Array.Empty<Key>(); await Wait(.15f);
            if (Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude < .01f)
                throw new Exception("Protection released without actual movement.");
            PlaceEnemy();
            await Until(() => enemy.HoldsAttack || hp.currentHealth < initialHealth, 6, "Marshmallow did not resume attacking after movement.");
            Result = "opening and post-dialogue idle safe; first movement restores marshmallow attacks; ";
            Progress("Opening safety passed; testing skateboard against the island surface.");
            enemy.gameObject.SetActive(false);
            pressed = Array.Empty<Key>();
            if (!reward.CollectSpawnTestKit()) throw new Exception("Could not obtain test skyboard.");
            Vector3 spawn = new Vector3(-147.47322f, 2542.132f, 1610.31f);
            if (!island.Raycast(new Ray(spawn + Vector3.up * 20, Vector3.down), out var spawnHit, 45)) throw new Exception("No spawn surface.");
            float footOffset = -(body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
            spawn.y = spawnHit.point.y + footOffset + .08f;
            player.RestoreSavedPose(spawn, Quaternion.identity); await Wait(.2f);
            if (!reward.TryStartFlight()) throw new Exception("Could not start the skyboard.");
            await Wait(.5f);
            float worstPenetration = 0, nearestClearance = float.PositiveInfinity;
            void AuditBoard()
            {
                var board = GameObject.Find("Sahur skyboard");
                if (!board) throw new Exception("Ridden board missing.");
                var renderers = board.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
                {
                    var origin = board.transform.position + board.transform.right * (x * 1.4f) + board.transform.forward * (z * .5f);
                    origin.y = reward.DeckHeight + 4;
                    if (!island.Raycast(new Ray(origin, Vector3.down), out var hit, 7) || hit.normal.y < .4f || hit.point.y > reward.DeckHeight + 2.5f) continue;
                    float clearance = bounds.min.y - hit.point.y;
                    worstPenetration = Mathf.Max(worstPenetration, -clearance);
                    nearestClearance = Mathf.Min(nearestClearance, clearance);
                }
            }
            AuditBoard();
            pressed = new[] { GameInputSettings.Get(GameInputSettings.Action.Dodge) };
            float descendEnd = Time.time + 1.2f;
            while (Time.time < descendEnd) { await Task.Delay(35); AuditBoard(); }
            pressed = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward), GameInputSettings.Get(GameInputSettings.Action.Dodge) };
            float travelEnd = Time.time + 2f;
            while (Time.time < travelEnd) { await Task.Delay(35); AuditBoard(); }
            pressed = Array.Empty<Key>();
            if (float.IsPositiveInfinity(nearestClearance) || worstPenetration > .12f)
                throw new Exception($"Board intersects island: penetration={worstPenetration:F2}, minimum clearance={nearestClearance:F2}.");
            Result += $"board stays above real island while descending and moving (max penetration {worstPenetration:F3}); passed";
            Progress("passed");
        }
        catch (Exception e) { Status = "failed"; Result += "FAILED: " + e; }
        finally
        {
            pressed = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject;
            if (keyboard != null) InputState.Change(keyboard, new KeyboardState());
            File.WriteAllText("Temp/SecondIslandPeaceBoard.result", Status + "\n" + Result);
        }
    }
}
