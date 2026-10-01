using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class InspectDeathRespawn
{
    public static async Task<object> Inspect()
    {
        var point = UnityEngine.Object.FindFirstObjectByType<IslandRespawnPoint>();
        if (point == null) throw new Exception("Saved island checkpoint missing");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var actor = UnityEngine.Object.Instantiate(prefab, point.transform.position + Vector3.up * 3f, Quaternion.identity);
        var camera = Camera.main;
        Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
        Quaternion cameraRotation = camera != null ? camera.transform.rotation : Quaternion.identity;
        float oldTime = Time.timeScale;
        var oldCursor = Cursor.lockState; bool visible = Cursor.visible;
        try
        {
            await Task.Delay(80);
            var life = actor.GetComponent<PlayerDeathRespawn>();
            var health = actor.GetComponent<PlayerHealth>();
            life.SetCheckpoint(point);
            health.ApplyDamage(10000f, actor.transform.position);
            life.Respawn();
            await Task.Delay(1200);
            var movement = actor.GetComponent<ThirdPersonPlayerController>();
            var capsule = actor.GetComponent<CharacterController>();
            float feet = capsule.bounds.min.y;
            if (!capsule.isGrounded || movement.Swimming || Mathf.Abs(feet - point.transform.position.y) > .6f)
                throw new Exception("Saved respawn is unsafe: grounded=" + capsule.isGrounded + " swimming=" + movement.Swimming + " feet=" + feet);
            health.GrantProtection(0f);
            health.ApplyDamage(10000f, actor.transform.position);
            await Task.Delay(250);
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            string path = Path.GetFullPath("Tools/DeathRespawnInspection.png");
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            UnityEngine.Object.Destroy(screenshot);
            life.Respawn();
            movement.enabled = false;
            return new { success = true, grounded = true, swimming = false, point = point.transform.position.ToString(), feet, screenshot = path };
        }
        finally
        {
            UnityEngine.Object.Destroy(actor);
            Time.timeScale = oldTime;
            Cursor.lockState = oldCursor; Cursor.visible = visible;
            if (camera != null) camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }
    }
}
