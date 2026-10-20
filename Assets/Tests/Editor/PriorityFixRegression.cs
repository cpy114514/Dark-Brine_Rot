using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PriorityFixRegression
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    [MenuItem("Tools/Sahur/Run Priority Fix Regression")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Run this isolated regression in Edit Mode.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("Load the first-island scenes first.");
        var objects = new List<GameObject>();
        var reports = new List<string>();
        var previousCursor = Cursor.lockState;
        var previousVisible = Cursor.visible;
        var camera = Camera.main;
        bool hadShake = camera && camera.GetComponent<CombatCameraShake>();
        string file = Path.GetFullPath("Temp/sahur-priority-regression.json");
        GameObject Make(string name, Vector3 position)
        {
            var go = new GameObject("Regression - " + name);
            // FindObjectsByType intentionally excludes DontSave objects, so use ordinary
            // temporary scene objects and remove them synchronously in finally.
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            objects.Add(go);
            return go;
        }
        WorldSaveIdentity Identify(GameObject go)
        {
            var identity = go.AddComponent<WorldSaveIdentity>();
            identity.id = "regression-" + Guid.NewGuid().ToString("N");
            return identity;
        }
        try
        {
            var hero = Make("hero", new Vector3(10000, 0, 10000));
            var movement = hero.AddComponent<ThirdPersonPlayerController>();
            var inventory = hero.AddComponent<EquipmentInventory>();
            var health = hero.AddComponent<PlayerHealth>();
            var respawn = hero.AddComponent<PlayerDeathRespawn>();
            Call(health, "Awake");
            movement.rollDuration = 1f;
            Set(movement, "rollTimer", .75f);
            health.ApplyDamage(10, hero.transform.position);
            Check(health.currentHealth == 100, "Dodge middle window should prevent damage.");
            Set(movement, "rollTimer", .98f);
            health.ApplyDamage(10, hero.transform.position);
            Check(health.currentHealth == 90, "Dodge startup must remain vulnerable.");
            Set(movement, "rollTimer", .2f);
            health.ApplyDamage(10, hero.transform.position);
            Check(health.currentHealth == 80, "Dodge recovery must remain vulnerable.");
            health.ApplyDamage(float.NaN, hero.transform.position);
            Check(health.currentHealth == 80, "Invalid damage must not corrupt health.");
            reports.Add("PASS dodge startup / middle / recovery and invalid damage");

            var boss = Make("boss", hero.transform.position + Vector3.forward * 10);
            var bossId = Identify(boss);
            GameWorldProgress.Record(bossId, 0);
            var ammo = Make("ammo", hero.transform.position + Vector3.right * 10);
            var ammoId = Identify(ammo);
            var pickup = ammo.AddComponent<RifleAmmoPickup>();
            pickup.amount = 12;
            GameWorldProgress.Record(pickup, 12);
            var item = Resources.LoadAll<EquipmentItem>("Equipment")[0];
            inventory.RestoreStack(item, 2);
            var checkpointObject = Make("checkpoint", hero.transform.position + Vector3.right * 20);
            Identify(checkpointObject);
            var checkpoint = checkpointObject.AddComponent<IslandRespawnPoint>();
            respawn.SetCheckpoint(checkpoint);
            var snapshot = GameWorldProgress.Capture(movement);
            string json = JsonUtility.ToJson(snapshot);
            Check(json.Contains(bossId.id) && json.Contains(ammoId.id), "World snapshot lost stable IDs.");
            inventory.ClearForRestore(); pickup.amount = 30;
            GameWorldProgress.Restore(JsonUtility.FromJson<GameWorldProgress.Snapshot>(json), movement);
            Check(!boss.activeSelf, "Defeated boss returned on restore.");
            Check(pickup.amount == 12, "Partial ammo pickup did not persist.");
            Check(inventory.Count(item) == 2, "Inventory stack did not round-trip.");
            respawn.SetCheckpoint(null);
            GameWorldProgress.Restore(snapshot, movement);
            Check(respawn.Checkpoint == checkpoint, "Active checkpoint did not round-trip.");
            GameWorldProgress.Record(pickup, 0);
            GameWorldProgress.Restore(GameWorldProgress.Capture(movement), movement);
            Check(!ammo.activeSelf, "Consumed ammo pickup returned.");
            reports.Add("PASS JSON world round-trip: defeated boss, partial/consumed ammo, inventory count, checkpoint");

            var managerObject = Make("save manager", hero.transform.position);
            var manager = managerObject.AddComponent<GameSaveManager>();
            var loadCounter = typeof(AdditiveSceneBootstrap).GetField("activeLoads", BindingFlags.Static | BindingFlags.NonPublic);
            int oldCounter = (int)loadCounter.GetValue(null);
            var routine = (System.Collections.IEnumerator)Call(manager, "RestoreAfterSceneStarts");
            try
            {
                loadCounter.SetValue(null, 1);
                for (int frame = 0; frame < 5; frame++)
                    Check(routine.MoveNext(), "Restore stopped waiting before additive scenes completed.");
                loadCounter.SetValue(null, 0);
                Check(routine.MoveNext(), "Restore did not allow a final scene initialization frame.");
                Check(!routine.MoveNext(), "Restore did not finish after loading completed.");
            }
            finally { loadCounter.SetValue(null, oldCounter); (routine as IDisposable)?.Dispose(); }
            reports.Add("PASS continue waits for additive loading beyond two frames");

            var read = typeof(GameSaveManager).GetMethod("TryReadSaveFile", BindingFlags.Static | BindingFlags.NonPublic);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            foreach (int version in new[] { 1, 2 })
            {
                File.WriteAllText(file, "{\"version\":" + version + ",\"playerPosition\":{\"x\":1,\"y\":2,\"z\":3},\"playerRotation\":{\"w\":1}}");
                object[] args = { file, null };
                Check((bool)read.Invoke(null, args), "Save version compatibility failed: " + version);
            }
            File.WriteAllText(file, "{broken");
            Check(!(bool)read.Invoke(null, new object[] { file, null }), "Corrupted save accepted.");
            reports.Add("PASS version 1/2 compatibility and corrupt-save rejection (temporary file only)");

            var arrival = hero.AddComponent<FirstIslandArrival>();
            arrival.player = movement;
            Set(arrival, "capsule", hero.GetComponent<CharacterController>());
            Set(arrival, "ownsCinematic", true);
            Set(arrival, "previousMovementEnabled", true);
            Set(arrival, "previousCapsuleEnabled", true);
            movement.ExternalControlLock = true;
            movement.enabled = false;
            hero.GetComponent<CharacterController>().enabled = false;
            Call(arrival, "OnDisable");
            Check(movement.enabled && hero.GetComponent<CharacterController>().enabled && !movement.ExternalControlLock,
                "Interrupted arrival retained a control lock.");
            Check(arrival.IsAwake && arrival.HasStick, "Interrupted arrival left player unable to progress.");
            reports.Add("PASS interrupted arrival restores movement, capsule and weapon availability");

            var attack = hero.AddComponent<SahurAttack>();
            var weaponObject = Make("weapon", hero.transform.position);
            var weapon = weaponObject.AddComponent<CapsuleCollider>(); weapon.isTrigger = true;
            attack.stickHitbox = weapon; attack.enemyTag = "";
            Set(attack, "attacking", true); Set(attack, "swingDamage", 10f);
            var victim = Make("victim", hero.transform.position + new Vector3(0, 1, 3));
            var victimCollider = victim.AddComponent<SphereCollider>(); victimCollider.radius = .15f;
            var victimHealth = victim.AddComponent<Health>();
            Physics.SyncTransforms();
            Vector3 p = hero.transform.position + Vector3.forward * 3;
            Call(attack, "ScanWeaponSweep", p + new Vector3(-2, 0, 0), p + new Vector3(-2, 2, 0),
                p + new Vector3(2, 0, 0), p + new Vector3(2, 2, 0), .2f);
            Check(victimHealth.currentHealth == 90, "Swept weapon middle missed target or applied duplicate damage.");
            reports.Add("PASS full-blade frame sweep hits middle target once");

            victim.transform.position = hero.transform.position + new Vector3(0, 1, 30);
            for (int i = 0; i < 40; i++)
            {
                var wall = Make("wall " + i, hero.transform.position + new Vector3(0, .5f, 1 + i * .5f));
                wall.AddComponent<BoxCollider>().size = new Vector3(2, 4, .1f);
            }
            Physics.SyncTransforms();
            Check((bool)Call(attack, "StrikeBlocked", victimCollider), "Dense obstacles failed to block attack.");
            Check(((Array)typeof(SahurAttack).GetField("strikeObstacles", Private).GetValue(attack)).Length >= 64,
                "Saturated obstruction buffer was not expanded.");
            reports.Add("PASS dense obstacle query expands and blocks attack");

            // Recreate a reward left on the ground without invoking the defeated boss again.
            Check(UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None).Length == 0,
                "Run loot fixture without authored dynamic pickups.");
            snapshot.drops.Add(new GameWorldProgress.Drop { item = item.id, scene = scene.path,
                position = hero.transform.position + Vector3.right * 50 });
            GameWorldProgress.Restore(snapshot, movement);
            var restoredDrops = UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None);
            foreach (var drop in restoredDrops) objects.Add(drop.gameObject);
            Check(restoredDrops.Length == 1 && restoredDrops[0].Item == item &&
                Vector3.Distance(restoredDrops[0].transform.position, snapshot.drops[0].position) < .01f,
                "Uncollected boss reward was lost or duplicated.");
            reports.Add("PASS uncollected boss reward recreates once at saved location");
            return string.Join("\n", reports);
        }
        finally
        {
            foreach (var go in objects) if (go) UnityEngine.Object.DestroyImmediate(go);
            if (camera && !hadShake && camera.GetComponent<CombatCameraShake>()) UnityEngine.Object.DestroyImmediate(camera.GetComponent<CombatCameraShake>());
            GameWorldProgress.Reset();
            Cursor.lockState = previousCursor; Cursor.visible = previousVisible;
            if (File.Exists(file)) File.Delete(file);
            Physics.SyncTransforms();
        }
    }
}
