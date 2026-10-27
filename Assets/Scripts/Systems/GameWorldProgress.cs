using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>First-island persistent world changes. Restoring never replays death/reward events.</summary>
    public static class GameWorldProgress
    {
        [Serializable] public sealed class Entry { public string id; public int value; }
        [Serializable] public sealed class ItemStack { public string id; public int count; }
        [Serializable] public sealed class Drop { public string item, scene; public Vector3 position; }
        [Serializable] public sealed class Snapshot
        {
            public List<Entry> changes = new List<Entry>();
            public List<ItemStack> inventory = new List<ItemStack>();
            public List<Drop> drops = new List<Drop>();
            public string checkpoint;
            public FirstIslandObjectives.Progress tutorial;
        }
        static readonly Dictionary<string, int> changes = new Dictionary<string, int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => changes.Clear();
        public static bool IsDefeated(string id) => !string.IsNullOrEmpty(id) && changes.TryGetValue(id, out int value) && value == 0;
        public static bool IsDefeated(Component component) => IsDefeated(Key(component));

        public static string Key(Component component)
        {
            if (!component || !component.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/", StringComparison.Ordinal)) return null;
            var identity = component.GetComponent<WorldSaveIdentity>();
            return identity && !string.IsNullOrEmpty(identity.id) ? identity.id : null;
        }
        public static void Record(Component component, int value)
        {
            var key = Key(component);
            if (key != null) changes[key] = value;
        }
        public static Snapshot Capture(ThirdPersonPlayerController player)
        {
            var result = new Snapshot();
            result.tutorial = player.GetComponent<FirstIslandObjectives>()?.Capture();
            foreach (var cache in UnityEngine.Object.FindObjectsByType<IslandSupplyCache>(FindObjectsSortMode.None))
                Record(cache, -Mathf.CeilToInt(cache.Remaining) - 1);
            foreach (var pair in changes) result.changes.Add(new Entry { id = pair.Key, value = pair.Value });
            var inventory = player.GetComponent<EquipmentInventory>();
            if (inventory != null)
                foreach (var pair in inventory.Items)
                    if (pair.Key && !string.IsNullOrEmpty(pair.Key.id)) result.inventory.Add(new ItemStack { id = pair.Key.id, count = pair.Value });
            foreach (var pickup in UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None))
                if (!pickup.IsCollected && pickup.Item && pickup.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/", StringComparison.Ordinal))
                    result.drops.Add(new Drop { item = pickup.Item.id, scene = pickup.gameObject.scene.path, position = pickup.transform.position });
            var respawn = player.GetComponent<PlayerDeathRespawn>();
            if (respawn && respawn.Checkpoint) result.checkpoint = Key(respawn.Checkpoint);
            return result;
        }
        public static void Restore(Snapshot snapshot, ThirdPersonPlayerController player)
        {
            Reset();
            if (snapshot == null) return; // Version 1 saves have no world snapshot.
            player.GetComponent<FirstIslandObjectives>()?.Restore(snapshot.tutorial);
            if (snapshot.changes != null)
                foreach (var entry in snapshot.changes)
                    if (entry != null && !string.IsNullOrEmpty(entry.id)) changes[entry.id] = entry.value;
            // Earlier saves can carry a confirmed boss defeat in the objective
            // snapshot. Preserve it without replaying a death or dropping loot.
            if (snapshot.tutorial?.bosses != null)
                foreach (var boss in snapshot.tutorial.bosses)
                    if (boss != null && boss.defeated && !string.IsNullOrEmpty(boss.bossId) && (int)boss.kind >= 0 && (int)boss.kind < 3)
                        changes[boss.bossId] = 0;
            var items = new Dictionary<string, EquipmentItem>();
            foreach (var item in Resources.LoadAll<EquipmentItem>("Equipment"))
                if (!string.IsNullOrEmpty(item.id)) items[item.id] = item;
            var inventory = player.GetComponent<EquipmentInventory>();
            if (inventory != null && snapshot.inventory != null)
            {
                inventory.ClearForRestore();
                foreach (var stack in snapshot.inventory)
                    if (stack != null && stack.count > 0 && stack.count <= 1000 && stack.id != null && items.TryGetValue(stack.id, out var item))
                        inventory.RestoreStack(item, stack.count);
            }
            foreach (var identity in UnityEngine.Object.FindObjectsByType<WorldSaveIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (Key(identity) == null) continue;
                if (changes.TryGetValue(identity.id, out int value))
                {
                    var ammo = identity.GetComponent<RifleAmmoPickup>();
                    var cache = identity.GetComponent<IslandSupplyCache>();
                    if (cache) cache.RestoreRemaining(-value - 1);
                    else if (ammo) ammo.RestoreRemaining(value);
                    else identity.gameObject.SetActive(false);
                }
                if (!string.IsNullOrEmpty(snapshot.checkpoint) && identity.id == snapshot.checkpoint)
                    player.GetComponent<PlayerDeathRespawn>()?.SetCheckpoint(identity.GetComponent<IslandRespawnPoint>());
            }
            foreach (var pickup in UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None))
                if (pickup.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/", StringComparison.Ordinal))
                {
                    pickup.gameObject.SetActive(false);
                    if (Application.isPlaying) UnityEngine.Object.Destroy(pickup.gameObject);
                    else UnityEngine.Object.DestroyImmediate(pickup.gameObject);
                }
            if (snapshot.drops == null) return;
            foreach (var drop in snapshot.drops)
            {
                if (drop == null || drop.item == null || !items.TryGetValue(drop.item, out var item) || !item.worldModel ||
                    !Finite(drop.position.x) || !Finite(drop.position.y) || !Finite(drop.position.z)) continue;
                var scene = SceneManager.GetSceneByPath(drop.scene);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var go = new GameObject("Loot - " + item.displayName);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = drop.position;
                go.AddComponent<EquipmentPickup>().Initialize(item);
            }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
