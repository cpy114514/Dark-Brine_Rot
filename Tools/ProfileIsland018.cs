using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using Mavis;
using Mavis.VisualStyles;

// Native environment/UI hotspot discovery; no authored Gameplay scene or saved actors.
public static class ProfileIsland018
{
    const string Root = ".codex/performance018";
    static async Task Frame()
    {
        int frame = Time.frameCount; var end = DateTime.UtcNow.AddSeconds(45);
        while (Time.frameCount == frame) { if (!Application.isPlaying || DateTime.UtcNow > end) throw new Exception("Native frame stalled"); await Task.Delay(5); }
    }
    public static object Prepare()
    {
        if (Application.isPlaying || File.Exists(Root + "/running")) throw new Exception("Stopped unowned workspace required");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(s => SceneManager.GetSceneByPath(s.path).isDirty)) throw new Exception("Preserve dirty scenes");
        Directory.CreateDirectory(Root + "/save-before");
        foreach (string n in new[] { "dark-brine-rot-save.json", "dark-brine-rot-save.json.bak" })
            if (File.Exists(Path.Combine(Application.persistentDataPath, n))) File.Copy(Path.Combine(Application.persistentDataPath, n), Root + "/save-before/" + n, false);
        File.WriteAllText(Root + "/workspace-before.json", Newtonsoft.Json.JsonConvert.SerializeObject(setup));
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity", OpenSceneMode.Single);
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity", OpenSceneMode.Additive);
        return new { prepared = true, scenes = SceneManager.sceneCount };
    }
    public static object Restore()
    {
        if (Application.isPlaying || File.Exists(Root + "/running")) throw new Exception("Stop owned test first");
        foreach (string p in Directory.GetFiles(Root + "/save-before"))
            if (!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath, Path.GetFileName(p))))) throw new Exception("User save changed; do not overwrite");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/workspace-before.json")));
        return new { restored = true, savesUnchanged = true };
    }
    public static object Start(string label = "before")
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 2 || !SceneManager.GetSceneByName("Environment").isLoaded || !SceneManager.GetSceneByName("Lighting").isLoaded || File.Exists(Root + "/running")) throw new Exception("Prepared isolated Play required");
        if (File.Exists(Root + "/" + label + ".json")) throw new Exception("Keep existing evidence");
        File.WriteAllText(Root + "/running", label); _ = Run(label); return new { started = true, label };
    }
    static async Task<Color32[]> Capture(Camera camera, string path)
    {
        Color32[] pixels = null;
        Action<ScriptableRenderContext, Camera> handler = (context, c) =>
        {
            if (c != camera || pixels != null) return;
            var previous = RenderTexture.active; var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
            try { RenderTexture.active = camera.targetTexture; image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply(); pixels = image.GetPixels32(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
        };
        RenderPipelineManager.endCameraRendering += handler;
        try { for (int i = 0; i < 20 && pixels == null; i++) await Frame(); if (pixels == null) throw new Exception("No actual camera render"); return pixels; }
        finally { RenderPipelineManager.endCameraRendering -= handler; }
    }
    static async Task<object> Trace(string phase)
    {
        bool old = ProfilerDriver.enabled;
        try
        {
            ProfilerDriver.enabled = true;
            for (int i = 0; i < 60; i++) await Frame();
            var totals = new Dictionary<string, double>(); var own = new Dictionary<string, double>(); int used = 0;
            for (int f = Math.Max(ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex - 35); f <= ProfilerDriver.lastFrameIndex - 2; f++)
            using (var raw = ProfilerDriver.GetRawFrameDataView(f, 0))
            {
                if (!raw.valid) continue; used++;
                for (int i = 0; i < raw.sampleCount; i++)
                {
                    string n = raw.GetSampleName(i); double ms = raw.GetSampleTimeMs(i), self = ms; int end = i + raw.GetSampleChildrenCountRecursive(i);
                    for (int j = i + 1; j <= end; j += raw.GetSampleChildrenCountRecursive(j) + 1) self -= raw.GetSampleTimeMs(j);
                    totals.TryGetValue(n, out double value); totals[n] = value + ms; own.TryGetValue(n, out value); own[n] = value + Math.Max(0, self);
                }
            }
            return new { phase, frames = used, inclusive = totals.OrderByDescending(p => p.Value).Take(70).Select(p => new { name = p.Key, ms = p.Value / Math.Max(1, used) }).ToArray(), self = own.OrderByDescending(p => p.Value).Take(45).Select(p => new { name = p.Key, ms = p.Value / Math.Max(1, used) }).ToArray() };
        }
        finally { ProfilerDriver.enabled = old; }
    }
    static async Task Run(string label)
    {
        var originalPipeline = QualitySettings.renderPipeline; var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; bool wasPixel = Pixel360Rendering.IsActive;
        var manager = VisualStyleManager.Instance; string style = manager.Current.id; float intensity = manager.Intensity, clock = Time.timeScale;
        int rate = Application.targetFrameRate, sync = QualitySettings.vSyncCount;
        UniversalRenderPipelineAsset pipeline = null; GameObject root = null; RenderTexture target = null; bool passed = false;
        var traces = new List<object>();
        try
        {
            Pixel360Rendering.ReleaseCurrent(); pipeline = UnityEngine.Object.Instantiate(source); pipeline.renderScale = 1; QualitySettings.renderPipeline = pipeline;
            root = new GameObject("Owned performance018 camera", typeof(Camera)); root.tag = "MainCamera"; var camera = root.GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(85.34209f, 50.7829742f, 632.001f), new Quaternion(.0983068f, .8079295f, -.140905172f, .5636768f));
            camera.fieldOfView = 52; camera.nearClipPlane = .15f; camera.farClipPlane = 1200; camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            target = new RenderTexture(1920, 1080, 24); target.Create(); camera.targetTexture = target;
            Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            var deadline = DateTime.UtcNow.AddSeconds(150); WorldPerformanceManager world = null;
            while ((world = Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().FirstOrDefault(w => w.gameObject.scene.IsValid())) == null || !world.IsReady)
            { if (DateTime.UtcNow > deadline) throw new Exception("Foliage not ready"); await Frame(); }
            for (int i = 0; i < 30; i++) await Frame();
            var visible = await Capture(camera, Root + "/" + label + "-visible.png"); camera.cullingMask = 0; await Frame(); var empty = await Capture(camera, Root + "/" + label + "-empty.png"); camera.cullingMask = ~0;
            int changed = visible.Zip(empty, (a, b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)).Count(d => d > 15);
            if (changed < visible.Length / 20) throw new Exception("Rendered-world positive control failed");
            traces.Add(await Trace("native-world"));
            Pixel360Rendering.Configure(pipeline, true); VisualStyleUIController.Open(manager.transform);
            for (int i = 0; i < 12; i++) await Frame(); traces.Add(await Trace("pixel-world-four-card-gallery"));
            File.WriteAllText(Root + "/" + label + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, changedPixels = changed, worldReady = world.IsReady, traces, hardware = SystemInfo.graphicsDeviceName, scope = "Editor CPU discovery, actual first-island Environment/Lighting including its authored NPCs; native1920x1080 and existing pixel plus gallery; Gameplay scene and player/input setup excluded; profiler overhead included" }, Newtonsoft.Json.Formatting.Indented)); passed = true;
        }
        catch (Exception e) { File.WriteAllText(Root + "/" + label + "-error.txt", e.ToString()); }
        finally
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>(); if (ui && VisualStyleUIController.IsOpen) ui.Close();
            Pixel360Rendering.ReleaseCurrent(); QualitySettings.renderPipeline = originalPipeline; if (wasPixel && source) Pixel360Rendering.Configure(source, true);
            manager.Select(style, false); manager.SetIntensity(intensity, false); Time.timeScale = clock; Application.targetFrameRate = rate; QualitySettings.vSyncCount = sync;
            if (root) UnityEngine.Object.DestroyImmediate(root); if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); } if (pipeline) UnityEngine.Object.DestroyImmediate(pipeline);
            File.WriteAllText(Root + "/" + label + "-completed.json", "{\"passed\":" + passed.ToString().ToLowerInvariant() + "}"); File.Delete(Root + "/running"); EditorApplication.isPlaying = false;
        }
    }
}
