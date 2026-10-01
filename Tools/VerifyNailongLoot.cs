using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyNailongLoot
{
    public static object Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0, 499.5f, 0);
        ground.transform.localScale = new Vector3(80, 1, 80);
        var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), new Vector3(20, 500, 20), Quaternion.identity);
        var boss = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"), new Vector3(0, 500, 0), Quaternion.identity);
        GameObject wall = null;
        EquipmentPickup[] pickups = null;
        try
        {
            boss.GetComponent<NailongAI>().enabled = false;
            var loot = boss.GetComponent<NailongLoot>();
            var health = boss.GetComponent<NailongHealth>();
            if (loot.drops.Length != 3 || loot.Drop() != 0) throw new Exception("Invalid drop table or drops before death.");
            var before = UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None);
            Physics.SyncTransforms();
            health.ApplyDamage(10, boss.transform.position);
            if (loot.HasDropped) throw new Exception("Nonlethal damage dropped items.");
            health.ApplyDamage(10000, boss.transform.position);
            pickups = UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None).Except(before).ToArray();
            if (pickups.Length != 3 || pickups.Select(p => p.Item.id).Distinct().Count() != 3)
                throw new Exception("Death did not drop all three unique items.");
            health.ApplyDamage(10000, boss.transform.position);
            if (loot.Drop() != 0) throw new Exception("Death dropped twice.");
            foreach (var pickup in pickups)
            {
                if (Mathf.Abs(pickup.transform.position.y - 500.5f) > .01f) throw new Exception("Loot did not land above terrain.");
                var renderer = pickup.GetComponentsInChildren<MeshRenderer>().First();
                if (!renderer.sharedMaterial.shader.isSupported || renderer.sharedMaterial.GetTexture("_BaseMap") == null ||
                    renderer.bounds.size.magnitude > 1.5f || renderer.bounds.size.magnitude < .1f)
                    throw new Exception("Invalid loot appearance or scale.");
            }
            UnityEngine.Object.DestroyImmediate(boss); boss = null;
            if (pickups.Any(p => p == null)) throw new Exception("Destroying boss removed the drops.");
            var inventory = hero.GetComponent<EquipmentInventory>();
            var cc = hero.GetComponent<CharacterController>();
            var playerHp = hero.GetComponent<PlayerHealth>() ?? hero.AddComponent<PlayerHealth>();
            foreach (var pickup in pickups)
            {
                hero.transform.position = new Vector3(20, 500, 20);
                Physics.SyncTransforms();
                if (pickup.TryCollect(inventory)) throw new Exception("Remote pickup was accepted.");
                hero.transform.position = new Vector3(pickup.transform.position.x + .9f, 500, pickup.transform.position.z);
                Physics.SyncTransforms();
                hero.transform.position += Vector3.up * (500 - cc.bounds.min.y);
                Physics.SyncTransforms();
                playerHp.currentHealth = 0;
                if (pickup.TryCollect(inventory)) throw new Exception("Dead player collected an item.");
                playerHp.currentHealth = 100;
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = pickup.transform.position + new Vector3(.45f, .2f, 0);
                wall.transform.localScale = new Vector3(.05f, 1.2f, 1);
                Physics.SyncTransforms();
                if (pickup.TryCollect(inventory)) throw new Exception("Collected through a wall.");
                UnityEngine.Object.DestroyImmediate(wall); wall = null;
                Physics.SyncTransforms();
                if (!pickup.TryCollect(inventory) || inventory.Count(pickup.Item) != 1 || pickup.TryCollect(inventory))
                    throw new Exception("Pickup failed or duplicated inventory.");
            }
            if (inventory.Items.Count != 3) throw new Exception("Inventory did not retain all items.");
            return new { threeGuaranteedDrops = true, onlyOnDeath = true, noDuplicateDeathDrops = true,
                survivesBossRemoval = true, groundedAndTextured = true, closeRangePickup = true,
                deadPlayerAndWallsBlocked = true, collectedOnce = true,
                inventory = inventory.Items.Select(p => new { name = p.Key.displayName, slot = p.Key.slot.ToString(), count = p.Value }).ToArray() };
        }
        finally
        {
            if (pickups != null) foreach (var pickup in pickups) if (pickup != null) UnityEngine.Object.DestroyImmediate(pickup.gameObject);
            if (wall != null) UnityEngine.Object.DestroyImmediate(wall);
            if (boss != null) UnityEngine.Object.DestroyImmediate(boss);
            UnityEngine.Object.DestroyImmediate(hero); UnityEngine.Object.DestroyImmediate(ground);
        }
    }
}
