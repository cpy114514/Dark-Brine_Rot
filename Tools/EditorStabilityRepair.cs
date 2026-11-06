using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Mavis;
using Object = UnityEngine.Object;

// Execute through Unity Pipeline run_script, outside Assets so no persistent editor hook is installed.
public static class EditorStabilityRepair
{
    const string Evidence = ".codex/editor-stability-20261102";
    static void Write(string name, object value)
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));
    }
    public static object Snapshot(string label)
    {
        using (var process = Process.GetCurrentProcess())
        {
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i))
                .Select(s => new { s.path, s.isDirty, s.isLoaded, roots = s.rootCount, active = s == SceneManager.GetActiveScene() }).ToArray();
            // Mono's Process counters return zero here; sample real OS counters from PowerShell instead.
            var result = new { label, utc = DateTime.UtcNow, pid = process.Id,
                graphics = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName,
                graphicsVersion = SystemInfo.graphicsDeviceVersion, playMode = EditorApplication.isPlaying,
                frame = Time.frameCount, realtime = Time.realtimeSinceStartupAsDouble,
                autoGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64),
                graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Select(g => g.ToString()).ToArray(), scenes };
            Write(label, result); return result;
        }
    }
    public static object Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11) throw new InvalidOperationException("Start once using -force-d3d11 first");
        Snapshot("before-api-setting");
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        return Snapshot("after-api-setting");
    }
    public static object ProtectSave()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        string path = (string)typeof(GameSaveManager).GetProperty("SavePath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        string destination = Evidence + "/protected-save.json";
        if (File.Exists(destination)) throw new InvalidOperationException("Save snapshot already exists; preserve it");
        var files = new[] { path, path + ".bak", path + ".tmp" }.Select(p => new
        { path = p, bytes = File.Exists(p) ? Convert.ToBase64String(File.ReadAllBytes(p)) : null }).ToArray();
        Write("protected-save", files);
        Write("save-override", new { value = typeof(GameSaveManager).GetField("editorSavePath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) });
        return new { protectedFiles = files.Length };
    }
    public static object Smoke(string label)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11) throw new InvalidOperationException("DX11 was not retained");
        typeof(GameSaveManager).GetField("editorSavePath", BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, Path.GetFullPath(Evidence + "/isolated-save.json"));
        var player = Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var camera = Camera.main;
        if (!player || !camera || !camera.isActiveAndEnabled) throw new Exception("Live player/camera unavailable");
        var map = player.GetComponent<IslandMapUI>();
        if (!map) throw new Exception("Live map unavailable");
        bool wasOpen = map.PanelVisible;
        try
        {
            if (!wasOpen && !map.SetOpen(true)) throw new Exception("Map refused opening");
            if (!map.MapGraphic || !map.PanelVisible || Time.timeScale != 0) throw new Exception("Map/pausing failed");
            Write(label + "-smoke", new { passed = true, graphics = SystemInfo.graphicsDeviceType.ToString(),
                frame = Time.frameCount, mapIslands = map.MapGraphic.IslandCount, camera = camera.name, player = player.name });
        }
        finally { if (!wasOpen && map.PanelVisible) map.SetOpen(false); }
        return Snapshot(label);
    }
    public static object RestoreSave()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        if (File.Exists(Evidence + "/save-restored.json")) throw new InvalidOperationException("Save already restored; do not overwrite newer progress");
        var files = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Evidence + "/protected-save.json"));
        foreach (var entry in files)
        {
            string path = (string)entry["path"], content = (string)entry["bytes"];
            if (content != null) File.WriteAllBytes(path, Convert.FromBase64String(content));
            else if (File.Exists(path)) File.Delete(path);
        }
        var original = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Evidence + "/save-override.json"));
        typeof(GameSaveManager).GetField("editorSavePath", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, (string)original["value"]);
        Write("save-restored", new { restored = true, files = files.Count });
        return new { restored = true };
    }
    public static object CheckRestartSafe()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt);
        if (scenes.Any(s => s.isDirty)) throw new InvalidOperationException("An open scene has unsaved changes; refusing to quit");
        Snapshot("before-verification-restart");
        // After this check use the CLI's graceful `unity close`, then `unity open`.
        return new { restartSafe = true };
    }
}
