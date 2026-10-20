using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Opt-in deterministic standalone capture. Never runs in a normal game session.</summary>
    [DefaultExecutionOrder(20000)]
    public sealed class IslandPerformanceBaseline : MonoBehaviour
    {
        [Serializable] public sealed class Sample { public string phase; public float ms, engineMs; public long gcBytes; }
        [Serializable] public sealed class Summary
        { public string phase; public int frames, spikes50ms; public float averageFps, averageMs, p95Ms, p99Ms, maxMs, onePercentLowFps; public long maxGcBytes; }
        [Serializable] public sealed class Report
        {
            public string capturedUtc, unity, cpu, gpu, os, quality, scene, status;
            public bool editor, developmentBuild, batchmode;
            public float renderScale, shadowDistance, lodBias;
            public int width, height, targetFps, vsync, errors, memoryMb, graphicsMemoryMb;
            public double loadSeconds;
            public List<Summary> summaries = new List<Summary>();
            public List<Sample> samples = new List<Sample>();
        }
        readonly Report report = new Report();
        ProfilerRecorder allocations;
        string phase, output;
        bool measuring, finished, story;
        Vector3 viewTarget;
        bool overrideCamera;
        float seconds = 10;
        ThirdPersonPlayerController player;
        long previousFrameTick;
        RenderTexture captureTarget;
        Texture2D frameFence;
        Camera captureCamera;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-sahur-baseline") < 0 || FindAnyObjectByType<IslandPerformanceBaseline>()) return;
            var go = new GameObject("Optional island baseline"); DontDestroyOnLoad(go); go.AddComponent<IslandPerformanceBaseline>();
        }
        static string Argument(string prefix)
        { foreach (string arg in Environment.GetCommandLineArgs()) if (arg.StartsWith(prefix, StringComparison.Ordinal)) return arg.Substring(prefix.Length); return null; }
        void Awake()
        {
            output = Argument("-baseline-report=") ?? Path.Combine(Application.persistentDataPath, "island-baseline.json");
            if (float.TryParse(Argument("-baseline-seconds="), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float requested)) seconds = Mathf.Clamp(requested, 3, 60);
            story = Array.IndexOf(Environment.GetCommandLineArgs(), "-baseline-story") >= 0;
            Application.runInBackground = true;
            report.capturedUtc = DateTime.UtcNow.ToString("o"); report.unity = Application.unityVersion;
            report.cpu = SystemInfo.processorType; report.gpu = SystemInfo.graphicsDeviceName; report.os = SystemInfo.operatingSystem;
            report.editor = Application.isEditor; report.developmentBuild = Debug.isDebugBuild;
            report.batchmode = Application.isBatchMode;
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            Application.logMessageReceived += Log;
        }
        IEnumerator Start()
        {
            double start = Time.realtimeSinceStartupAsDouble;
            string scene = story ? "Assets/Scenes/First story/Story1.unity" : "Assets/Scenes/First Island/Main.unity";
            report.scene = scene;
            if (SceneManager.GetSceneByPath(scene).isLoaded == false) yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 120;
            while ((AdditiveSceneBootstrap.IsLoading || !(player = FindFirstObjectByType<ThirdPersonPlayerController>())) && Time.realtimeSinceStartup < deadline)
            { ExcludePlayers(); yield return null; }
            ExcludePlayers();
            if (!player) { Finish("player missing or loading timeout"); yield break; }
            if (!story)
            {
                var arrival = player.GetComponent<FirstIslandArrival>();
                if (arrival) { arrival.StopAllCoroutines(); arrival.enabled = false; }
                var objectives = player.GetComponent<FirstIslandObjectives>(); if (objectives) objectives.enabled = false;
                var performance = FindFirstObjectByType<WorldPerformanceManager>();
                while (performance && !performance.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
            }
            report.loadSeconds = Time.realtimeSinceStartupAsDouble - start;
            report.width = Screen.width; report.height = Screen.height; report.targetFps = Application.targetFrameRate;
            report.vsync = QualitySettings.vSyncCount; report.quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            Application.runInBackground = true;
            report.lodBias = QualitySettings.lodBias;
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (pipeline) { report.renderScale = pipeline.renderScale; report.shadowDistance = pipeline.shadowDistance; }
            report.graphicsMemoryMb = SystemInfo.graphicsMemorySize;
            // Windows may occlude hidden diagnostic windows. Render the real world camera
            // and fence one pixel, so simulation-only frames cannot masquerade as GPU performance.
            captureTarget = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            captureTarget.Create(); frameFence = new Texture2D(1, 1, TextureFormat.RGB24, false);
            if (story)
            {
                yield return new WaitForSecondsRealtime(8);
                var sequence = FindFirstObjectByType<Story1SharkCollisionSequence>();
                if (!sequence) { Finish("story encounter missing"); yield break; }
                sequence.BeginImpact();
                phase = "shipwreck"; measuring = true; yield return new WaitForSecondsRealtime(seconds * 3);
            }
            else
            {
                var arrival = player.GetComponent<FirstIslandArrival>();
                Vector3 coast = arrival && arrival.shore ? arrival.shore.position : player.transform.position;
                yield return Route("coast", coast, Vector3.forward);
                yield return Route("forest", new Vector3(91.15f, 50, 370.89f), Vector3.forward);
                var boss = FindFirstObjectByType<CappuccinoAI>();
                if (boss) yield return Route("boss", boss.transform.position - boss.transform.forward * 12, boss.transform.forward);
            }
            measuring = false; Finish("completed");
        }
        IEnumerator Route(string name, Vector3 point, Vector3 heading)
        {
            measuring = false;
            float ground = point.y;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 100, Vector3.down, 300, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<ProceduralIsland>()) ground = hit.point.y;
            var capsule = player.GetComponent<CharacterController>();
            float sole = capsule ? (capsule.center.y - capsule.height * .5f) * Mathf.Abs(player.transform.lossyScale.y) : 0;
            point.y = ground + .1f - sole;
            player.RestoreSavedPose(point, Quaternion.LookRotation(heading));
            player.GetComponent<PlayerHealth>()?.GrantProtection(120);
            viewTarget = point + Vector3.up * 3; overrideCamera = true;
            yield return new WaitForSecondsRealtime(2);
            phase = name; measuring = true;
            yield return new WaitForSecondsRealtime(seconds);
            measuring = false;
        }
        void ExcludePlayers()
        {
            foreach (var actor in FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
            {
                if (!actor.GetComponent<GameSaveExcluded>()) actor.gameObject.AddComponent<GameSaveExcluded>();
                actor.GetComponent<PlayerHealth>()?.GrantProtection(120);
            }
        }
        void LateUpdate()
        {
            long tick = System.Diagnostics.Stopwatch.GetTimestamp();
            float wallMs = previousFrameTick == 0 ? 0 : (float)((tick - previousFrameTick) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            previousFrameTick = tick;
            if (finished) return;
            if (!player) ExcludePlayers();
            if (overrideCamera && Camera.main)
            {
                var camera = Camera.main;
                camera.transform.SetPositionAndRotation(viewTarget + new Vector3(0, 5, -11), Quaternion.LookRotation(new Vector3(0, -5, 11)));
            }
            if (captureTarget && Camera.main)
            {
                // An enabled camera with a target texture renders even when its window is occluded.
                // Fence the preceding rendered frame without submitting a duplicate camera render.
                captureCamera = Camera.main;
                captureCamera.targetTexture = captureTarget;
                var previous = RenderTexture.active; RenderTexture.active = captureTarget;
                frameFence.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); frameFence.Apply(); RenderTexture.active = previous;
            }
            if (measuring && wallMs > 0) report.samples.Add(new Sample { phase = phase, ms = wallMs, engineMs = Time.unscaledDeltaTime * 1000, gcBytes = allocations.Valid ? allocations.LastValue : 0 });
        }
        public static Summary Summarize(string phase, List<Sample> source)
        {
            var frames = source.FindAll(s => s.phase == phase); frames.Sort((a, b) => a.ms.CompareTo(b.ms));
            var result = new Summary { phase = phase, frames = frames.Count };
            if (frames.Count == 0) return result;
            double sum = 0, tail = 0; int worst = Mathf.Max(1, Mathf.CeilToInt(frames.Count * .01f));
            foreach (var frame in frames) { sum += frame.ms; result.maxGcBytes = Math.Max(result.maxGcBytes, frame.gcBytes); if (frame.ms > 50) result.spikes50ms++; }
            for (int i = frames.Count - worst; i < frames.Count; i++) tail += frames[i].ms;
            result.averageMs = (float)(sum / frames.Count); result.averageFps = 1000 / Mathf.Max(.001f, result.averageMs);
            result.p95Ms = frames[Mathf.CeilToInt(frames.Count * .95f) - 1].ms;
            result.p99Ms = frames[Mathf.CeilToInt(frames.Count * .99f) - 1].ms;
            result.maxMs = frames[frames.Count - 1].ms; result.onePercentLowFps = (float)(1000 / (tail / worst));
            return result;
        }
        void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception) report.errors++; }
        void Finish(string status)
        {
            if (finished) return; finished = true; report.status = status;
            foreach (string name in new[] { "coast", "forest", "boss", "shipwreck" })
            { var summary = Summarize(name, report.samples); if (summary.frames > 0) report.summaries.Add(summary); }
            report.memoryMb = (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (!Application.isEditor) Application.Quit();
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= Log; allocations.Dispose();
            if (captureCamera) { captureCamera.targetTexture = null; captureCamera.enabled = true; }
            if (captureTarget) { captureTarget.Release(); Destroy(captureTarget); }
            if (frameFence) Destroy(frameFence);
        }
    }
}
