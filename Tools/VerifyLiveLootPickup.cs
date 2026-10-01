using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyLiveLootPickup
{
    public static async Task<object> Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), new Vector3(0, 500, 0), Quaternion.identity);
        var pickupObject = new GameObject("Live pickup verification");
        try
        {
            hero.GetComponent<ThirdPersonPlayerController>().enabled = false;
            hero.GetComponent<SahurAttack>().enabled = false;
            var inventory = hero.GetComponent<EquipmentInventory>();
            var cc = hero.GetComponent<CharacterController>();
            var item = Resources.LoadAll<EquipmentItem>("Equipment/Nailong").First();
            Physics.SyncTransforms();
            pickupObject.transform.position = new Vector3(hero.transform.position.x, cc.bounds.min.y + .5f, hero.transform.position.z);
            var pickup = pickupObject.AddComponent<EquipmentPickup>();
            pickup.Initialize(item);
            await Task.Delay(1200);
            if (inventory.Count(item) != 1) throw new Exception("Update did not automatically collect nearby loot.");
            if (pickupObject != null && pickupObject.activeSelf) throw new Exception("Collected loot remained in world.");
            return new { automaticPickup = true, inventoryCount = inventory.Count(item),
                worksWithoutOptionalHealthComponent = hero.GetComponent<PlayerHealth>() == null };
        }
        finally
        {
            if (pickupObject != null) UnityEngine.Object.DestroyImmediate(pickupObject);
            UnityEngine.Object.DestroyImmediate(hero);
        }
    }
}
