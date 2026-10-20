using System;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PacingExplorationRegression
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static void Close(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < .01f, message + ": " + actual);
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Run isolated regressions in Edit Mode.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        var actor = new GameObject("Pacing regression actor"); SceneManager.MoveGameObjectToScene(actor, scene);
        var box = new GameObject("Pacing regression cache"); SceneManager.MoveGameObjectToScene(box, scene);
        actor.transform.position = new Vector3(10000, 1, 10000); box.transform.position = new Vector3(10000, 0, 10001);
        var reports = new System.Collections.Generic.List<string>();
        try
        {
            var movement = actor.AddComponent<ThirdPersonPlayerController>();
            var stamina = actor.AddComponent<PlayerStamina>();
            Close(stamina.SprintCost(1, 4), .6f, "Exploration sprint cost");
            stamina.NotifyCombat();
            Close(stamina.SprintCost(1, 4), 4, "Combat sprint cost");
            Close(stamina.ActiveRegenerationRate, 22, "Combat regeneration rate");
            Close(stamina.ActiveRegenerationDelay, .85f, "Combat regeneration delay");
            stamina.currentStamina = 100;
            for (int i = 0; i < 6; i++) Check(stamina.TrySpend(16), "Valid dodge cost denied.");
            Check(!stamina.TrySpend(16) && stamina.Current == 4, "Insufficient stamina spent or went negative.");
            reports.Add("PASS exploration/combat sprint, recovery tuning and insufficient-stamina protection");

            Close(SahurAttack.BalancedDamage(25, false), 25, "Stick damage");
            Close(SahurAttack.BalancedDamage(25, true), 20, "Twin-blade per-hit damage");
            Close(SahurRifleCombat.DamageAtDistance(10, false, false, false), 20, "Near ground damage");
            Close(SahurRifleCombat.DamageAtDistance(100, false, false, false), 8, "Far ground damage");
            Close(SahurRifleCombat.DamageAtDistance(90, true, false, false), 20, "Anti-air damage");
            Close(SahurRifleCombat.DamageAtDistance(90, true, true, true), 37.5f, "Counter/engine reward");
            Check(SahurRifleCombat.DamageAtDistance(100, false, true, true) == 8, "Aircraft bonus leaked into ground combat.");
            reports.Add("PASS stick/blade distinction, rifle falloff and aircraft-only counter/engine bonuses");

            var inventory = actor.AddComponent<EquipmentInventory>();
            var health = actor.AddComponent<PlayerHealth>();
            var loadout = actor.AddComponent<SahurWeaponLoadout>();
            var rifle = actor.AddComponent<SahurRifleCombat>();
            Check(rifle.ReserveRounds == 90, "Starting reserve incorrect.");
            var item = Resources.Load<EquipmentItem>(SahurWeaponLoadout.M4ItemPath);
            inventory.Collect(item); Set(loadout, "inventory", inventory); Set(loadout, "rifleItem", item);
            var cache = box.AddComponent<IslandSupplyCache>();
            box.AddComponent<WorldSaveIdentity>().id = "pacing-cache-" + Guid.NewGuid().ToString("N");
            rifle.RestoreAmmo(0, 0); Physics.SyncTransforms();
            Check(cache.TryRefill(movement) && rifle.ReserveRounds == 45, "Empty-ammo recovery failed.");
            Check(!cache.TryRefill(movement) && rifle.ReserveRounds == 45, "Cache could be farmed during cooldown.");
            cache.RestoreRemaining(17);
            var snapshot = JsonUtility.FromJson<GameWorldProgress.Snapshot>(JsonUtility.ToJson(GameWorldProgress.Capture(movement)));
            cache.RestoreRemaining(0);
            GameWorldProgress.Restore(snapshot, movement);
            Check(cache.Remaining > 16 && cache.Remaining <= 17, "Cache cooldown lost on restore.");
            reports.Add("PASS empty-ammo recovery, repeat-claim prevention and cooldown save round-trip");
            cache.RestoreRemaining(0); rifle.RestoreAmmo(30, SahurRifleCombat.ReserveCapacity);
            Check(!cache.TryRefill(movement) && cache.IsAvailable, "Full inventory consumed supply cooldown.");
            rifle.RestoreAmmo(0, 0); health.currentHealth = 0;
            Check(!cache.TryRefill(movement), "Dead actor received supplies.");
            health.currentHealth = 100; actor.transform.position += Vector3.right * 20; Physics.SyncTransforms();
            Check(!cache.TryRefill(movement), "Remote actor received supplies.");
            reports.Add("PASS reserve capacity, full-inventory, death and distance restrictions");
            Check(FirstIslandObjectives.Bearing(Vector3.forward, true) == "N" &&
                FirstIslandObjectives.Bearing(Vector3.right, false) == "E" &&
                FirstIslandObjectives.Bearing(new Vector3(-1, 0, -1), true) == "SW", "Map bearings incorrect.");
            Check(UnityEngine.Object.FindObjectsByType<IslandLandmark>(FindObjectsSortMode.None).Length >= 3,
                "Authored trail signs missing.");
            reports.Add("PASS English-first compass directions and authored trail landmarks");
            return string.Join("\n", reports);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(box);
            GameWorldProgress.Reset(); Physics.SyncTransforms();
        }
    }
}
