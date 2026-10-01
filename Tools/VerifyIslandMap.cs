using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public static class VerifyIslandMap
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static async Task<object> Verify()
    {
        Check(Application.isPlaying, "Requires play mode");
        var point = UnityEngine.Object.FindFirstObjectByType<IslandRespawnPoint>();
        Check(point != null, "Island checkpoint not loaded");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var player = UnityEngine.Object.Instantiate(prefab, point.transform.position + Vector3.up * 2f, Quaternion.Euler(0, 90, 0));
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss = UnityEngine.Object.Instantiate(bossPrefab, point.transform.position + Vector3.right * 70f, Quaternion.identity);
        boss.GetComponent<NailongAI>().enabled = false;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        var camera = Camera.main;
        Vector3 position = camera != null ? camera.transform.position : Vector3.zero;
        Quaternion rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
        float previousScale = Time.timeScale;
        CursorLockMode cursor = Cursor.lockState; bool visible = Cursor.visible;
        async Task Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            await Task.Delay(120);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(80);
        }
        try
        {
            await Task.Delay(100);
            var map = player.GetComponent<IslandMapUI>();
            Check(map != null, "Map not auto-installed");
            await Press(Key.M);
            Check(map.PanelVisible && IslandMapUI.IsOpen && SahurLoadoutUI.BlocksInput && Time.timeScale == 0f, "M failed to open/pause/block");
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Map cursor is locked");
            Check(map.MapGraphic.IslandCount >= 1, "No island mesh on map");
            Vector2 center = map.MapGraphic.WorldToMap(new Vector3(map.MapGraphic.WorldCenter.x, 0, map.MapGraphic.WorldCenter.y));
            Check(center.magnitude < .001f && map.MapGraphic.WorldToMap(new Vector3(map.MapGraphic.WorldCenter.x + 10, 0, map.MapGraphic.WorldCenter.y)).x > 0f
                && map.MapGraphic.WorldToMap(new Vector3(map.MapGraphic.WorldCenter.x, 0, map.MapGraphic.WorldCenter.y + 10)).y > 0f, "Projection/orientation mismatch");
            var ui = GameObject.Find("Island Map Screen");
            Canvas.ForceUpdateCanvases();
            var drawnMesh = map.MapGraphic.canvasRenderer.GetMesh();
            Debug.Log("Map mesh vertices=" + drawnMesh.vertexCount + " bounds=" + drawnMesh.bounds + " cull=" + map.MapGraphic.canvasRenderer.cull + " rect=" + map.MapGraphic.rectTransform.rect);
            Check(drawnMesh.vertexCount > 0 && !map.MapGraphic.canvasRenderer.cull, "Island graphic not rendering");
            Check(ui != null && ui.GetComponentsInChildren<UnityEngine.UI.Text>().Length >= 10, "Map labels/markers missing");
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            string preview = Path.GetFullPath("Tools/IslandMapInspection.png");
            File.WriteAllBytes(preview, screenshot.EncodeToPNG()); UnityEngine.Object.Destroy(screenshot);
            await Press(Key.M);
            Check(!map.PanelVisible && !IslandMapUI.IsOpen && Time.timeScale == previousScale, "M failed to close/resume");
            await Press(Key.M);
            await Press(Key.Escape);
            Check(!IslandMapUI.IsOpen && !PauseSettingsMenu.IsOpen && Time.timeScale == previousScale, "Escape also opened pause menu");
            await Press(Key.M);
            player.GetComponent<PlayerHealth>().ApplyDamage(10000f, player.transform.position);
            Check(!IslandMapUI.IsOpen && PlayerDeathRespawn.IsOpen, "Death did not replace map");
            Check(!map.SetOpen(true), "Dead player can open map");
            player.GetComponent<PlayerDeathRespawn>().Respawn();
            await Task.Delay(100);
            await Press(Key.M);
            Check(IslandMapUI.IsOpen, "Map cannot reopen after respawn");
            map.enabled = false;
            Check(!IslandMapUI.IsOpen && Time.timeScale == previousScale, "Disabling map leaves game frozen");
            player.GetComponent<ThirdPersonPlayerController>().enabled = false;
            return new { success = true, mToggle = true, escapeNoPauseConflict = true, northUp = true,
                islandMesh = true, playerAndCheckpointAndBoss = true, deathAndRespawn = true, disableRestoresTime = true, preview };
        }
        finally
        {
            UnityEngine.Object.Destroy(player); UnityEngine.Object.Destroy(boss);
            InputSystem.RemoveDevice(keyboard);
            Time.timeScale = previousScale; Cursor.lockState = cursor; Cursor.visible = visible;
            if (camera != null) camera.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
