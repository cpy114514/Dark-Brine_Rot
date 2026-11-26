using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
using Mavis.VisualStyles;

public static class VerifyStartup023
{
    const string Root = ".codex/performance023";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object value, string name) => value.GetType().GetField(name, Flags).GetValue(value);
    static void Set(object value, string name, object field) => value.GetType().GetField(name, Flags).SetValue(value, field);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Flags).Invoke(value, args);
    static async Task Frame() { int n = Time.frameCount; var end = DateTime.UtcNow.AddSeconds(30); while (n == Time.frameCount) { if (!Application.isPlaying || DateTime.UtcNow > end) throw new Exception("Native frame timeout"); await Task.Delay(5); } }
    public static object Prepare()
    {
        if (Application.isPlaying || File.Exists(Root + "/workspace-before-tests.json")) throw new Exception("Fresh stopped clean scene required");
        var setup = EditorSceneManager.GetSceneManagerSetup(); if (setup.Any(s => SceneManager.GetSceneByPath(s.path).isDirty)) throw new Exception("Preserve dirty scenes");
        Directory.CreateDirectory(Root + "/save-before-tests"); foreach (string n in new[] { "dark-brine-rot-save.json", "dark-brine-rot-save.json.bak" }) if (File.Exists(Path.Combine(Application.persistentDataPath, n))) File.Copy(Path.Combine(Application.persistentDataPath, n), Root + "/save-before-tests/" + n, false);
        File.WriteAllText(Root + "/workspace-before-tests.json", Newtonsoft.Json.JsonConvert.SerializeObject(setup)); EditorSceneManager.OpenScene("Assets/Scenes/Combat Test/CombatTest.unity", OpenSceneMode.Single); return new { prepared = true };
    }
    public static object Restore()
    {
        if (Application.isPlaying || File.Exists(Root + "/native-running")) throw new Exception("Stop fixture first");
        foreach (string p in Directory.GetFiles(Root + "/save-before-tests")) if (!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath, Path.GetFileName(p))))) throw new Exception("Save changed: preserve newer progress");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/workspace-before-tests.json"))); return State();
    }
    public static object State()
    {
        var actual = EditorSceneManager.GetSceneManagerSetup(); var expected = Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/workspace-before-tests.json"));
        return new { stopped = !Application.isPlaying, clean = actual.All(s => !SceneManager.GetSceneByPath(s.path).isDirty), sceneMatch = actual.Length == expected.Length && actual.Zip(expected, (a, b) => a.path == b.path && a.isActive == b.isActive && a.isLoaded == b.isLoaded).All(b => b), savesUnchanged = Directory.GetFiles(Root + "/save-before-tests").All(p => File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath, Path.GetFileName(p))))), scenes = actual };
    }
    public static object Finish()
    {
        if (Application.isPlaying || File.Exists(Root + "/native-running") || File.Exists(Root + "/build-running")) throw new Exception("Stop owned work first");
        var report = new { state = State(), development = EditorUserBuildSettings.development, debugging = EditorUserBuildSettings.allowDebugging, connectProfiler = EditorUserBuildSettings.connectProfiler, backend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(), preloaded = PlayerSettings.GetPreloadedAssets().Select(AssetDatabase.GetAssetPath).ToArray(), projectSettingsBytesRestored = File.ReadAllBytes("ProjectSettings/ProjectSettings.asset").SequenceEqual(File.ReadAllBytes(Root + "/build-before/ProjectSettings.asset")) };
        File.WriteAllText(Root + "/final-native-state.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented)); return report;
    }
    public static object Start()
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().name != "CombatTest" || File.Exists(Root + "/native-running") || File.Exists(Root + "/verification.json")) throw new Exception("Fresh isolated CombatTest required");
        File.WriteAllText(Root + "/native-running", "native"); _ = Run(); return new { started = true };
    }
    static async Task Run()
    {
        var checks = new List<string>(); Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); checks.Add(name); };
        GameObject holder = null; IslandPerformanceBaseline baseline = null; RenderTexture sentinel = null;
        var camera = Camera.main; var oldTarget = camera.targetTexture; bool background = Application.runInBackground;
        var pipeline = QualitySettings.renderPipeline; int fps = Application.targetFrameRate, vsync = QualitySettings.vSyncCount; bool passed = false;
        try
        {
            for (int i = 0; i < 5; i++) await Frame();
            sentinel = new RenderTexture(96, 64, 24); sentinel.Create(); camera.targetTexture = sentinel;
            holder = new GameObject("Owned startup023 verifier"); baseline = holder.AddComponent<IslandPerformanceBaseline>(); baseline.enabled = false;
            var report = (IslandPerformanceBaseline.Report)Get(baseline, "report");
            Call(baseline, "TraceLoad", "disabled-control"); check(report.loadStages.Count == 0, "Tracing is inert unless explicitly enabled");
            Call(baseline, "LateUpdate"); check(Get(baseline, "captureTarget") == null && camera.targetTexture == sentinel, "Default startup does not force a render target");
            Set(baseline, "loadTrace", true); Call(baseline, "TraceLoad", "native-start");
            check(report.loadStages.Count == 1 && report.loadStages[0].scenes.Contains("CombatTest"), "Trace records the actual loaded scene set");
            check(report.loadStages[0].frame == Time.frameCount && report.loadStages[0].seconds >= 0, "Trace uses native frame and monotonic elapsed time");
            Set(baseline, "startupRender", true); Call(baseline, "LateUpdate");
            var first = (RenderTexture)Get(baseline, "captureTarget");
            check(first && first.width == Screen.width && first.height == Screen.height && camera.targetTexture == first, "Early capture binds the actual startup camera at output resolution");
            Call(baseline, "EnsureCaptureTarget"); check((RenderTexture)Get(baseline, "captureTarget") == first, "Stable output size reuses its owned target");
            first.Release(); first.width = 32; first.height = 32; first.Create(); Call(baseline, "EnsureCaptureTarget");
            var resized = (RenderTexture)Get(baseline, "captureTarget");
            check(resized != first && resized.width == Screen.width && camera.targetTexture == resized, "Resolution change replaces the owned target and rebinds its camera");
            check(sentinel.IsCreated(), "Resize preserves the original external target");
            for (int i = 0; i < 5; i++) await Frame();
            check(report.loadingCameraCallbacks >= 2, "Actual native camera callbacks are counted during startup");
            Call(baseline, "TraceLoad", "native-after-render"); var last = report.loadStages.Last();
            check(last.cameraCallbacks >= 2 && last.seconds >= report.loadStages[0].seconds && last.frame > report.loadStages[0].frame, "Stage trace preserves rendered/frame/time progression");
            check(QualitySettings.renderPipeline == pipeline && Application.targetFrameRate == fps && QualitySettings.vSyncCount == vsync, "Early rendering does not override saved frame or quality policy");
            UnityEngine.Object.DestroyImmediate(baseline); baseline = null;
            check(camera.targetTexture == sentinel && sentinel.IsCreated(), "Teardown restores the original target after a resize");
            check(Application.runInBackground == background, "Teardown restores background policy"); passed = true;
        }
        catch (Exception e) { File.WriteAllText(Root + "/native-error.txt", e.ToString()); }
        finally
        {
            if (baseline) UnityEngine.Object.DestroyImmediate(baseline); if (holder) UnityEngine.Object.DestroyImmediate(holder);
            camera.targetTexture = oldTarget; if (sentinel) UnityEngine.Object.DestroyImmediate(sentinel);
            File.WriteAllText(Root + "/verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed, checks, scope = "Native opt-in startup trace and render-target ownership/resize contracts; no loading-speed or game FPS conclusion" }, Newtonsoft.Json.Formatting.Indented));
            File.Delete(Root + "/native-running"); EditorApplication.isPlaying = false;
        }
    }
}
