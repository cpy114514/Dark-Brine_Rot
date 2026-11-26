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

public static class VerifyProfile025
{
    const string Root = ".codex/performance025";
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
        var checks = new List<string>(); Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks.Add(name);};
        var camera=Camera.main;int mask=camera.cullingMask;float scale=Time.timeScale;
        var pipeline=QualitySettings.renderPipeline;int fps=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
        GameObject holder=null;IslandPerformanceBaseline baseline=null;bool passed=false;
        try
        {
            holder=new GameObject("Owned forest024 verifier");baseline=holder.AddComponent<IslandPerformanceBaseline>();baseline.enabled=false;
            check(!(bool)Get(baseline,"forestAttribution") && Get(baseline,"forestWindow")==null,"Normal diagnostic session does not enable forest controls");
            check(((IslandPerformanceBaseline.Report)Get(baseline,"report")).forest==null,"Normal report has no forest measurements");
            bool profiler=UnityEngine.Profiling.Profiler.enabled,binary=UnityEngine.Profiling.Profiler.enableBinaryLog;string log=UnityEngine.Profiling.Profiler.logFile;
            Call(baseline,"BeginForestProfiler");
            check(!(bool)Get(baseline,"forestOwnsProfiler") && UnityEngine.Profiling.Profiler.enabled==profiler && UnityEngine.Profiling.Profiler.enableBinaryLog==binary && UnityEngine.Profiling.Profiler.logFile==log,"Unflagged diagnostic preserves existing profiler recording state");
            Time.timeScale=.37f;Call(baseline,"BeginForestState",camera);
            check(Time.timeScale==0,"Owned paired experiment freezes scaled simulation");
            Call(baseline,"BeginForestState",camera);camera.cullingMask=0;
            Call(baseline,"RestoreForestState");
            check(Time.timeScale==.37f && camera.cullingMask==mask,"Repeated acquisition retains original timescale and camera mask");
            Time.timeScale=.53f;Call(baseline,"RestoreForestState");
            check(Time.timeScale==.53f,"Idempotent teardown does not overwrite later external state");
            var validation=typeof(IslandPerformanceBaseline).GetMethod("ForestTimingUsable",BindingFlags.Static|BindingFlags.NonPublic);
            Func<FrameTiming,bool> usable=t=>(bool)validation.Invoke(null,new object[]{t});
            var timing=new FrameTiming{frameStartTimestamp=1,cpuFrameTime=10,cpuMainThreadFrameTime=4,cpuRenderThreadFrameTime=2,cpuMainThreadPresentWaitTime=0,gpuFrameTime=8};
            check(usable(timing),"Finite native-unit timing shape accepts zero Present wait");
            timing.gpuFrameTime=0;check(usable(timing),"CPU timing remains usable when GPU timing is missing");
            timing.gpuFrameTime=double.NaN;check(usable(timing),"Unusable GPU timing does not discard independent CPU samples");
            timing.gpuFrameTime=8;timing.frameStartTimestamp=0;check(!usable(timing),"Missing frame identity rejected");
            timing.frameStartTimestamp=1;timing.cpuMainThreadFrameTime=double.PositiveInfinity;check(!usable(timing),"Nonfinite CPU counter rejected");
            timing.cpuMainThreadFrameTime=-1;check(!usable(timing),"Negative CPU counter rejected");
            Set(baseline,"nativeThroughput",true);Call(baseline,"ApplyDiagnosticOverrides");
            check(QualitySettings.renderPipeline!=pipeline && Application.targetFrameRate==-1 && QualitySettings.vSyncCount==0,"Owned native configuration uses a cloned pipeline and uncapped policy");
            Set(baseline,"forestWindow",new IslandPerformanceBaseline.ForestWindow{scale=.5f});
            check(!(bool)Call(baseline,"ForestConfigurationValid"),"Unowned or mismatched forest configuration fails closed");
            Time.timeScale=.37f;Call(baseline,"BeginForestState",camera);camera.cullingMask=0;
            UnityEngine.Object.DestroyImmediate(baseline);baseline=null;
            check(Time.timeScale==.37f && camera.cullingMask==mask,"Component destruction restores frozen state even during a control");
            check(QualitySettings.renderPipeline==pipeline && Application.targetFrameRate==fps && QualitySettings.vSyncCount==vsync,"Component destruction restores original native frame and render policy");
            holder=new GameObject("Owned qualification024 replay");baseline=holder.AddComponent<IslandPerformanceBaseline>();baseline.enabled=false;
            var source=JsonUtility.FromJson<IslandPerformanceBaseline.Report>(File.ReadAllText(".codex/performance024/runs/a-paired/player-report/report.json"));
            foreach(var window in source.forest.windows)foreach(var sample in window.samples){sample.rawGpuMs=sample.gpuMs;sample.gpuMs=-1;}
            var current=(IslandPerformanceBaseline.Report)Get(baseline,"report");current.forest=source.forest;
            int samples=source.forest.windows.Sum(w=>w.samples.Count);
            Call(baseline,"QualifyForestCounters");
            check(current.forest.cpuCounterQualified,"Actual8ms CPU positive control qualifies independent CPU readings");
            check(!current.forest.gpuCounterQualified && current.forest.windows.SelectMany(w=>w.samples).All(s=>s.gpuMs==-1),"Nonresponsive actual GPU counter publishes unavailable rather than misleading finite values");
            check(samples==current.forest.windows.Sum(w=>w.samples.Count),"Counter qualification preserves all rendered frame samples");
            File.WriteAllText(Root+"/captured-qualification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{current.forest.cpuCounterQualified,current.forest.gpuCounterQualified,current.forest.cpuPositiveDeltaMs,current.forest.fullGpuMedianMs,current.forest.emptyGpuMedianMs,samples},Newtonsoft.Json.Formatting.Indented));
            foreach(var window in current.forest.windows)foreach(var sample in window.samples)if(sample.timestamp>0)sample.rawGpuMs=window.empty?.5:8;
            Call(baseline,"QualifyForestCounters");
            check(current.forest.gpuCounterQualified && current.forest.windows.SelectMany(w=>w.samples).Any(s=>s.gpuMs==8),"Synthetic finite GPU response control exercises qualified publication");
            current.forest.windows.Single(w=>w.name=="cpu-control").samples.Clear();Call(baseline,"QualifyForestCounters");
            check(!current.forest.cpuCounterQualified && current.forest.windows.SelectMany(w=>w.samples).All(s=>s.cpuMs==-1),"Missing CPU positive control fails closed independently");
            for(int i=0;i<5;i++)await Frame();passed=true;
        }
        catch(Exception error){File.WriteAllText(Root+"/native-error.txt",error.ToString());}
        finally
        {
            if(baseline)UnityEngine.Object.DestroyImmediate(baseline);if(holder)UnityEngine.Object.DestroyImmediate(holder);
            Time.timeScale=scale;camera.cullingMask=mask;
            File.WriteAllText(Root+"/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,scope="Native optional forest ownership/control validation; timing positive controls and paired performance require standalone"},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Root+"/native-running");EditorApplication.isPlaying=false;
        }
    }
}
