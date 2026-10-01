using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mavis;

public static class VerifyDeathRespawn
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task<object> Verify()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var actor = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var pointObject = new GameObject("Temporary isolated respawn point");
        pointObject.transform.position = new Vector3(30, 500, 0);
        var point = pointObject.AddComponent<IslandRespawnPoint>();
        var camera = Camera.main;
        Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
        Quaternion cameraRotation = camera != null ? camera.transform.rotation : Quaternion.identity;
        float oldTime = Time.timeScale;
        var oldCursor = Cursor.lockState;
        bool cursorVisible = Cursor.visible;
        try
        {
            await Task.Delay(50);
            var hp = actor.GetComponent<PlayerHealth>();
            var life = actor.GetComponent<PlayerDeathRespawn>();
            var movement = actor.GetComponent<ThirdPersonPlayerController>();
            var melee = actor.GetComponent<SahurAttack>();
            var boom = actor.GetComponent<SahurBoomerang>();
            var stamina = actor.GetComponent<PlayerStamina>();
            var inventory = actor.GetComponent<EquipmentInventory>();
            var item = Resources.LoadAll<EquipmentItem>("Equipment/Nailong")[0];
            inventory.Collect(item);
            life.SetCheckpoint(point);
            stamina.currentStamina = 1f;
            var push = actor.AddComponent<CombatKnockback>();
            push.Push(Vector3.right * 3f);
            hp.ApplyDamage(10000f, actor.transform.position);
            Check(life.IsDead && life.PanelVisible && PlayerDeathRespawn.IsOpen && Time.timeScale == 0f, "Death did not open/pause");
            Check(!movement.enabled && !melee.enabled && !boom.enabled && !push.IsBeingPushed, "Dead player remains active");
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible && SahurLoadoutUI.BlocksInput, "Dead UI input not unlocked/blocked");
            var overlay = GameObject.Find("Sahur Death Screen");
            var buttons = overlay.GetComponentsInChildren<Button>();
            Check(buttons.Length == 1, "Respawn button missing");
            hp.ApplyDamage(100f, Vector3.zero);
            Check(hp.currentHealth == 0f, "Dead health changed");
            buttons[0].onClick.Invoke();
            Check(!life.IsDead && !life.PanelVisible && !PlayerDeathRespawn.IsOpen && Time.timeScale > 0f, "Respawn left death overlay/pause");
            Check(movement.enabled && melee.enabled && boom.enabled, "Controls not restored");
            Check(hp.currentHealth == hp.maxHealth && stamina.currentStamina == stamina.maxStamina, "Vitals not restored");
            Check(Mathf.Abs(actor.transform.position.x - 30f) < .001f && actor.transform.position.y > 499f, "Checkpoint teleport failed");
            Check(inventory.Items.ContainsKey(item) && inventory.Items[item] == 1, "Inventory lost on death");
            Check(hp.IsProtected, "Respawn protection missing");
            hp.ApplyDamage(20f, Vector3.zero);
            Check(hp.currentHealth == hp.maxHealth, "Protection allowed damage");
            hp.GrantProtection(0f);
            hp.ApplyDamage(20f, Vector3.zero);
            Check(hp.currentHealth == hp.maxHealth - 20f, "Damage did not resume");
            Check(!life.Respawn(), "Live player can repeatedly respawn");
            hp.ApplyDamage(10000f, Vector3.zero);
            Check(life.IsDead && life.PanelVisible, "Second death failed");
            Check(life.Respawn(), "Second respawn failed");
            movement.enabled = false;
            return new { success = true, deathUI = true, mouseAndEnterSupported = true,
                controlsStopped = true, checkpointTeleport = true, fullVitals = true,
                equipmentPreserved = true, protection = true, repeatedDeaths = true };
        }
        finally
        {
            UnityEngine.Object.Destroy(actor);
            UnityEngine.Object.Destroy(pointObject);
            Time.timeScale = oldTime;
            Cursor.lockState = oldCursor; Cursor.visible = cursorVisible;
            if (camera != null) camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }
    }
}
