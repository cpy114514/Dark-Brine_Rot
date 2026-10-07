using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;

// Measures the same sailing, wreck and island viewpoints without changing saved progress.
public static class CaptureGameplayPerformance
{
    public static async Task<object> Run(string label = "before")
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        string save = Path.Combine(Application.persistentDataPath, "dark-brine-rot-save.json");
        byte[] original = File.Exists(save) ? File.ReadAllBytes(save) : null;
        var reports = new List<object>();
        try
        {
            GameSaveManager.CancelPendingContinue();
            await Load("Assets/Scenes/First story/Story1.unity");
            await Task.Delay(1500);
            reports.Add(await Measure("sailing"));
            var sequence = UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle = sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds = battle.maximumFightSeconds = 300;
            sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(300);
            sequence.BeginImpact();
            for (int i = 0; i < 200 && battle.CurrentPhase != Story1WreckBattle.Phase.Fighting; i++) await Task.Delay(50);
            if (battle.CurrentPhase != Story1WreckBattle.Phase.Fighting) throw new Exception("Wreck did not enter combat.");
            await Task.Delay(1500);
            var pieces=UnityEngine.Object.FindObjectsByType<Story1WreckFloatBody>(FindObjectsSortMode.None);
            if (pieces.Length != 202 || pieces.Any(p=>!p.Body.useGravity || p.Body.isKinematic))
                throw new Exception("Wreck fragments lost their independent gravity/physics.");
            if (!UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).Any(p=>p.IsBoardable))
                throw new Exception("No physically floating board remains boardable.");
            reports.Add(await Measure("wreck"));
            await Load("Assets/Scenes/First Island/Main.unity");
            for (int i = 0; i < 200 && UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>() == null; i++) await Task.Delay(50);
            await Task.Delay(2500);
            ExcludeSave();
            reports.Add(await Measure("island"));
            string path = Path.GetFullPath(".codex/performance/" + label + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented));
            return new { label, path, reports };
        }
        finally
        {
            ExcludeSave();
            Time.timeScale = 1;
            await Load("Assets/Scenes/Main Menu/MainMenu.unity");
            GameSaveManager.CancelPendingContinue();
            if (original != null) File.WriteAllBytes(save, original);
            else if (File.Exists(save)) File.Delete(save);
        }
    }
    static void ExcludeSave()
    {
        var actor = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        if (actor && !actor.GetComponent<GameSaveExcluded>()) actor.gameObject.AddComponent<GameSaveExcluded>();
    }
    static async Task Load(string path)
    {
        var op = SceneManager.LoadSceneAsync(path);
        while (!op.isDone) await Task.Delay(30);
        ExcludeSave();
    }
    static async Task<object> Measure(string scene)
    {
        var holder = new GameObject("Temporary performance sampler");
        var sampler = holder.AddComponent<GameplayPerformanceSampler>();
        try
        {
            sampler.Begin();
            await Task.Delay(5500);
            return sampler.Result(scene);
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
}
public sealed class GameplayPerformanceSampler : MonoBehaviour
{
    sealed class Counter
    {
        public string name;
        public ProfilerRecorder recorder;
        public double sum, peak;
    }
    readonly List<Counter> counters = new List<Counter>();
    readonly List<double> intervals = new List<double>(1000);
    int warmup;
    double batches, triangles, gc;
    public void Begin()
    {
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        string[] names = { "Main Thread", "Render Thread", "PlayerLoop", "GC Allocated In Frame", "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate", "FixedUpdate.ScriptRunBehaviourFixedUpdate", "Physics.Simulate", "Physics.Processing", "Camera.Render", "RenderPipelineManager.DoRenderLoop_Internal" };
        foreach (var h in handles)
        {
            var description = ProfilerRecorderHandle.GetDescription(h);
            if (!names.Contains(description.Name)) continue;
            var recorder = ProfilerRecorder.StartNew(description.Category, description.Name, 1);
            if (recorder.Valid) counters.Add(new Counter { name = description.Name, recorder = recorder });
        }
    }
    void Update()
    {
        if (warmup++ < 10) return;
        intervals.Add(Time.unscaledDeltaTime * 1000);
        batches += UnityEditor.UnityStats.batches;
        triangles += UnityEditor.UnityStats.triangles;
        foreach (var c in counters)
        {
            double value = c.recorder.LastValue;
            c.sum += value; c.peak = Math.Max(c.peak, value);
            if (c.name == "GC Allocated In Frame") gc += value;
        }
    }
    public object Result(string scene)
    {
        var ordered = intervals.OrderBy(x => x).ToArray();
        int n = ordered.Length;
        if (n == 0) throw new Exception("No frames sampled.");
        return new { scene, frames = n, frameMs = ordered.Average(), p95Ms = ordered[(int)((n - 1) * .95)],
            batches = batches / n, triangles = triangles / n, gcBytesPerFrame = gc / n,
            markers = counters.Select(c => new { c.name, average = c.sum / n, peak = c.peak }).ToArray(),
            renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,
            bodies = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length,
            camera = Camera.main ? Camera.main.transform.position.ToString() : "none" };
    }
    void OnDestroy() { foreach (var c in counters) c.recorder.Dispose(); }
}
