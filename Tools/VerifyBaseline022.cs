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

public static class VerifyBaseline022
{
    const string Root = ".codex/performance022";
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
        GameObject holder = null; IslandPerformanceBaseline baseline = null; RenderTexture target = null; bool passed = false; object gcControl = null;
        var camera = Camera.main; var oldTarget = camera.targetTexture; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
        var originalPipeline = QualitySettings.renderPipeline; int fps = Application.targetFrameRate, vsync = QualitySettings.vSyncCount; bool background = Application.runInBackground, pixel = Pixel360Rendering.IsActive;
        var styles = VisualStyleManager.Instance; string originalStyle = styles.Current.id; float intensity = styles.Intensity;
        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; float scale = source.renderScale;
        try
        {
            for (int i = 0; i < 5; i++) await Frame();
            holder = new GameObject("Owned throughput022 verifier"); baseline = holder.AddComponent<IslandPerformanceBaseline>(); baseline.enabled = false;
            Set(baseline, "nativeThroughput", true); Set(baseline, "styleMatrix", true); Set(baseline, "orbitCamera", true); Call(baseline, "ApplyDiagnosticOverrides");
            var temporary = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            check(temporary && temporary != source && temporary.renderScale == 1, "Native mode owns a separate full-scale pipeline");
            check(source.renderScale == scale, "Original pipeline scale is preserved");
            check(Application.targetFrameRate == -1 && QualitySettings.vSyncCount == 0, "Only explicit native mode uncaps frame policy");
            check(!Pixel360Rendering.IsActive, "Native throughput temporarily releases pixel presentation");
            Call(baseline, "ApplyDiagnosticOverrides"); check(QualitySettings.renderPipeline == temporary, "Repeated apply does not overwrite the original snapshot");
            styles.Select("origami", false); styles.SetIntensity(.3f, false); UnityEngine.Object.DestroyImmediate(baseline); baseline = null;
            check(QualitySettings.renderPipeline == originalPipeline && Application.targetFrameRate == fps && QualitySettings.vSyncCount == vsync, "Teardown restores pipeline and frame policy");
            check(styles.Current.id == originalStyle && Mathf.Abs(styles.Intensity - intensity) < .0001f, "Teardown restores style without saving preferences");
            check(Pixel360Rendering.IsActive == pixel, "Teardown restores pixel presentation");
            UnityEngine.Object.DestroyImmediate(holder); holder = new GameObject("Owned orbit022 verifier"); baseline = holder.AddComponent<IslandPerformanceBaseline>(); baseline.enabled = false;
            Set(baseline, "output", Path.GetFullPath(Root + "/native-summary.json"));
            var report = (IslandPerformanceBaseline.Report)Get(baseline, "report"); var qualification = (IEnumerator)Call(baseline, "QualifyGcCounter");
            while (qualification.MoveNext()) await Frame();
            gcControl = new { valid = report.gcCounterValid, baselineBytes = report.gcBaselineBytes, positiveBytes = report.gcPositiveControlBytes, qualified = (bool)Get(baseline, "gcQualified") };
            check(Get(baseline, "gcPositiveControl") == null, "Finite GC control releases its buffer outside measurement");
            target = new RenderTexture(640, 360, 24); target.Create(); Set(baseline, "captureTarget", target); Set(baseline, "overrideCamera", true); Set(baseline, "viewTarget", new Vector3(0, 2, 0)); Set(baseline, "orbitCamera", true); Set(baseline, "measuring", true); Set(baseline, "phase", "origami-coast");
            Set(baseline, "phaseStarted", Time.unscaledTime - 1); Call(baseline, "LateUpdate"); var first = camera.transform.position;
            Set(baseline, "phaseStarted", Time.unscaledTime - 3); Call(baseline, "LateUpdate"); var second = camera.transform.position;
            check(Vector3.Distance(first, second) > 1, "Orbit changes actual native camera position during measurement");
            check(Mathf.Abs(Vector3.Distance(first, new Vector3(0, 2, 0)) - Vector3.Distance(second, new Vector3(0, 2, 0))) < .001f, "Orbit preserves authored diagnostic camera radius");
            Set(baseline, "measuring", false); Call(baseline, "LateUpdate"); check(Vector3.Distance(camera.transform.position, new Vector3(0, 7, -11)) < .001f, "Render proof pauses orbit at a matching camera pose");
            var proof = (IEnumerator)Call(baseline, "VerifyRenderedView", "orbit-control"); while (proof.MoveNext()) await Frame();
            check(report.renderProofs.Count == 1 && report.renderProofs[0].changedPixels > 640 * 360 / 20, "Actual world/empty native pixels qualify the orbit camera target");
            Set(baseline, "gcQualified", false); Set(baseline, "measuring", true); Set(baseline, "phase", "origami-coast");
            for (int i = 0; i < 4; i++) { Call(baseline, "LateUpdate"); await Frame(); }
            Set(baseline, "measuring", false); check(report.samples.Count > 0 && report.samples.All(s => s.gcBytes == -1), "Unqualified GC samples remain unavailable rather than zero");
            report.samples.Clear(); foreach (string name in new[] { "realistic-coast", "origami-forest", "monochrome-boss" }) report.samples.Add(new IslandPerformanceBaseline.Sample { phase = name, ms = 10, gcBytes = -1 });
            Call(baseline, "Finish", "native summary control"); check(report.summaries.Count == 3 && report.summaries.All(s => s.frames == 1), "All new style/route names receive summaries");
            check(!report.gcCounterAvailable, "Report availability requires the positive control");
            UnityEngine.Object.DestroyImmediate(baseline); baseline = null; check(camera.targetTexture == oldTarget, "Teardown restores the previous camera target");
            check(Application.runInBackground == background, "Teardown restores background policy"); passed = true;
        }
        catch (Exception e) { File.WriteAllText(Root + "/native-error.txt", e.ToString()); }
        finally
        {
            if (baseline) UnityEngine.Object.DestroyImmediate(baseline); if (holder) UnityEngine.Object.DestroyImmediate(holder); if (target) UnityEngine.Object.DestroyImmediate(target);
            camera.targetTexture = oldTarget; camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
            File.WriteAllText(Root + "/verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed, checks, gcControl, scope = "Native isolated opt-in diagnostic override/camera/counter contracts; no game FPS conclusion" }, Newtonsoft.Json.Formatting.Indented));
            File.Delete(Root + "/native-running"); EditorApplication.isPlaying = false;
        }
    }
}
