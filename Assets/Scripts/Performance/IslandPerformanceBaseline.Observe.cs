using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Mavis
{
    public sealed partial class IslandPerformanceBaseline
    {
        bool observeOnly, observationEnded;
        string observationDirectory, observationPhase = "startup";
        long observationTick;
        double observationStarted, nextObservationState, nextObservationReport, nextObservationMemory;
        int observationRenderedFrame = -1, observationRenderedFrames;
        int observationErrors, observationWarnings;
        double observationRenderedSeconds, observationActiveIslandSeconds;
        int lastObservedBossCount = -1;
        bool? lastObservedFlockComplete, lastObservedArmed;
        ThirdPersonPlayerController observedPlayer;
        readonly Dictionary<string, ObservationPhase> observationPhases = new Dictionary<string, ObservationPhase>();
        readonly List<object> observationMemory = new List<object>();
        StreamWriter observationEvents;

        sealed class ObservationPhase
        {
            public string name;
            public int frames, spikes50;
            public double sum;
            public float maximum;
            readonly int[] histogram = new int[8192];
            public void Add(float ms)
            {
                frames++; sum += ms; maximum = Mathf.Max(maximum, ms);
                if (ms > 50) spikes50++;
                histogram[Mathf.Clamp(Mathf.CeilToInt(ms * 10), 0, histogram.Length - 1)]++;
            }
            float Percentile(float fraction)
            {
                int rank = Mathf.CeilToInt(frames * fraction), count = 0;
                for (int i = 0; i < histogram.Length; i++)
                {
                    count += histogram[i];
                    if (count >= rank) return i == histogram.Length - 1 ? maximum : i / 10f;
                }
                return 0;
            }
            public object Snapshot() => new { name, frames, meanMs = frames > 0 ? sum / frames : 0,
                p95Ms = Percentile(.95f), p99Ms = Percentile(.99f), maxMs = maximum, spikes50 };
        }

        void BeginObservation()
        {
            output = Argument("-baseline-report=") ?? Path.Combine(Application.persistentDataPath, "playtest-observation.json");
            observationDirectory = Path.GetDirectoryName(Path.GetFullPath(output));
            Directory.CreateDirectory(observationDirectory);
            observationEvents = new StreamWriter(Path.Combine(observationDirectory, "events.jsonl"), true) { AutoFlush = true };
            observationStarted = Time.realtimeSinceStartupAsDouble;
            observationTick = Stopwatch.GetTimestamp();
            Application.logMessageReceived += ObservationLog;
            SceneManager.sceneLoaded += ObservationSceneLoaded;
            RenderPipelineManager.endCameraRendering += ObservationCameraRendered;
            ObservationEvent("session-start", new { product = Application.productName, persistentDataPath = Application.persistentDataPath,
                unity = Application.unityVersion, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                gpu = SystemInfo.graphicsDeviceName, development = Debug.isDebugBuild, passive = true });
            WriteObservationReport("running");
        }

        void ObservationEvent(string kind, object details)
        {
            if (observationEnded || observationEvents == null) return;
            observationEvents.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(new {
                utc = DateTime.UtcNow.ToString("o"), seconds = Time.realtimeSinceStartupAsDouble - observationStarted,
                frame = Time.frameCount, kind, details }));
        }
        void ObservationLog(string message, string trace, LogType type)
        {
            if (type == LogType.Warning) observationWarnings++;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) observationErrors++;
            else return;
            ObservationEvent("log", new { type = type.ToString(), message, trace });
        }
        void ObservationSceneLoaded(Scene scene, LoadSceneMode mode) => ObservationEvent("scene-loaded", new { scene = scene.path, mode = mode.ToString() });
        void ObservationCameraRendered(ScriptableRenderContext context, Camera camera)
        {
            if (camera && camera.cameraType == CameraType.Game && observationRenderedFrame != Time.frameCount)
            { observationRenderedFrame = Time.frameCount; observationRenderedFrames++; }
        }
        string ObservedPhase()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (SceneLoadingScreen.IsLoading || AdditiveSceneBootstrap.IsLoading) return "loading:" + scene;
            if (!Application.isFocused) return "unfocused:" + scene;
            if (PauseSettingsMenu.IsOpen) return "paused:" + scene;
            if (PlayerDeathRespawn.IsOpen) return "death:" + scene;
            if (observedPlayer && observedPlayer.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/", StringComparison.Ordinal))
            {
                var arrival = observedPlayer.GetComponent<FirstIslandArrival>();
                if (arrival && !arrival.IsAwake) return "island:arrival";
                var capri = FindFirstObjectByType<CappuccinoAI>();
                if (capri && capri.FightActive) return "island:cappuccino";
                var flock = ChickenBroEncounter.Active;
                if (flock && flock.Started && !flock.Completed) return "island:flock";
                var nailong = FindFirstObjectByType<NailongAI>();
                if (nailong && nailong.FightActive) return "island:nailong";
                var aircraft = FindFirstObjectByType<BombardinoBoss>();
                if (aircraft && aircraft.FightActive) return "island:aircraft";
                return "island:exploration";
            }
            var story = FindFirstObjectByType<Story1WreckBattle>();
            if (story) return "opening:" + story.CurrentPhase + ":" + story.RedesignStage + ":" + story.CurrentLesson;
            return "scene:" + scene;
        }
        void ObserveFrame()
        {
            if (observationEnded) return;
            long tick = Stopwatch.GetTimestamp();
            float ms = (float)((tick - observationTick) * 1000.0 / Stopwatch.Frequency);
            observationTick = tick;
            if (observationRenderedFrame == Time.frameCount - 1 && ms > 0)
            {
                if (!observationPhases.TryGetValue(observationPhase, out var phase))
                    observationPhases[observationPhase] = phase = new ObservationPhase { name = observationPhase };
                phase.Add(ms);
                observationRenderedSeconds += ms / 1000.0;
                if (observationPhase.StartsWith("island:", StringComparison.Ordinal) &&
                    observationPhase != "island:arrival" && Application.isFocused && Time.timeScale > 0 &&
                    !SceneLoadingScreen.IsLoading && !AdditiveSceneBootstrap.IsLoading)
                    observationActiveIslandSeconds += ms / 1000.0;
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (now >= nextObservationState)
            {
                nextObservationState = now + 1;
                observedPlayer = FindFirstObjectByType<ThirdPersonPlayerController>();
                string phase = ObservedPhase();
                if (phase != observationPhase) { ObservationEvent("phase", new { previous = observationPhase, current = phase }); observationPhase = phase; }
                WriteObservationState();
            }
            if (now >= nextObservationMemory) { nextObservationMemory = now + 5; RecordObservationMemory(); }
            if (now >= nextObservationReport) { nextObservationReport = now + 15; WriteObservationReport("running"); }
        }
        void WriteObservationState()
        {
            var camera = Camera.main;
            var health = observedPlayer ? observedPlayer.GetComponent<PlayerHealth>() : null;
            var stamina = observedPlayer ? observedPlayer.GetComponent<PlayerStamina>() : null;
            var objectives = observedPlayer ? observedPlayer.GetComponent<FirstIslandObjectives>() : null;
            var arrival = observedPlayer ? observedPlayer.GetComponent<FirstIslandArrival>() : null;
            var story = FindFirstObjectByType<Story1WreckBattle>();
            var respawn = observedPlayer ? observedPlayer.GetComponent<PlayerDeathRespawn>() : null;
            var flock = ChickenBroEncounter.Active;
            int bossCount = objectives ? objectives.DefeatedBossCount : -1;
            bool? flockComplete = flock ? flock.Completed : null;
            bool? armed = arrival ? arrival.HasStick : null;
            if (bossCount != lastObservedBossCount || flockComplete != lastObservedFlockComplete || armed != lastObservedArmed)
            {
                ObservationEvent("island-progress", new { bossCount, flockComplete, armed });
                lastObservedBossCount = bossCount;
                lastObservedFlockComplete = flockComplete;
                lastObservedArmed = armed;
            }
            var state = new { utc = DateTime.UtcNow.ToString("o"), seconds = Time.realtimeSinceStartupAsDouble - observationStarted,
                frame = Time.frameCount, phase = observationPhase, focused = Application.isFocused, timeScale = Time.timeScale,
                scene = SceneManager.GetActiveScene().path, player = observedPlayer ? observedPlayer.name : null,
                position = observedPlayer ? new[] { observedPlayer.transform.position.x, observedPlayer.transform.position.y, observedPlayer.transform.position.z } : null,
                cameraRotation = camera ? new[] { camera.transform.eulerAngles.x, camera.transform.eulerAngles.y, camera.transform.eulerAngles.z } : null,
                health = health ? health.currentHealth : -1, stamina = stamina ? stamina.Current : -1,
                controlLocked = observedPlayer && observedPlayer.ExternalControlLock,
                checkpoint = respawn && respawn.Checkpoint ? respawn.Checkpoint.name : null,
                questStage = objectives ? objectives.Stage : -1, bossCount, flockComplete, armed,
                recommendedBossId = objectives ? objectives.RecommendedBossId : null,
                openingStage = story ? story.RedesignStage.ToString() : null, lesson = story ? story.CurrentLesson.ToString() : null,
                tutorialWaiting = story && story.TutorialWaiting,
                renderWidth = Screen.width, renderHeight = Screen.height, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                targetFps = Application.targetFrameRate, vsync = QualitySettings.vSyncCount };
            File.WriteAllText(Path.Combine(observationDirectory, "state.json"), Newtonsoft.Json.JsonConvert.SerializeObject(state, Newtonsoft.Json.Formatting.Indented));
        }
        [StructLayout(LayoutKind.Sequential)] struct ObservationMemoryCounters
        { public uint cb, faults; public UIntPtr peakWorking, working, peakPaged, paged, peakNonPaged, nonPaged, pageFile, peakPageFile, privateBytes; }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("psapi.dll")] static extern bool GetProcessMemoryInfo(IntPtr process, out ObservationMemoryCounters counters, uint size);
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
#endif
        void RecordObservationMemory()
        {
            double privateMb = -1, workingMb = -1;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (GetProcessMemoryInfo(GetCurrentProcess(), out var memory, (uint)Marshal.SizeOf<ObservationMemoryCounters>()))
            { privateMb = memory.privateBytes.ToUInt64() / 1048576.0; workingMb = memory.working.ToUInt64() / 1048576.0; }
#endif
            observationMemory.Add(new { seconds = Time.realtimeSinceStartupAsDouble - observationStarted, phase = observationPhase,
                unityAllocatedMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576.0, privateMB = privateMb, workingMB = workingMb });
        }
        void WriteObservationReport(string status)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var data = new { status, passive = true, capturedUtc = DateTime.UtcNow.ToString("o"), product = Application.productName,
                unity = Application.unityVersion, gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                editor = Application.isEditor, development = Debug.isDebugBuild, width = Screen.width, height = Screen.height,
                msaa = pipeline ? pipeline.msaaSampleCount : 0, renderScale = pipeline ? pipeline.renderScale : 0,
                elapsedSeconds = Time.realtimeSinceStartupAsDouble - observationStarted, observationRenderedFrames,
                observationRenderedSeconds, observationActiveIslandSeconds, errors = observationErrors, warnings = observationWarnings,
                phases = observationPhases.Values.Select(p => p.Snapshot()).ToArray(), memory = observationMemory.ToArray() };
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
        }
        void EndObservation()
        {
            if (observationEnded) return;
            WriteObservationReport("stopped");
            observationEnded = true;
            Application.logMessageReceived -= ObservationLog;
            SceneManager.sceneLoaded -= ObservationSceneLoaded;
            RenderPipelineManager.endCameraRendering -= ObservationCameraRendered;
            observationEvents?.Dispose(); observationEvents = null;
        }
        void OnApplicationQuit() { if (observeOnly) EndObservation(); }
    }
}
