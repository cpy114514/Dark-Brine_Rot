// Copied into the isolated Player build alongside IslandPerformanceBaseline.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mavis
{
    public sealed partial class IslandPerformanceBaseline
    {
        static readonly string[] SystemCounters =
        {
            "Main Thread", "Render Thread", "GC Allocated In Frame", "Draw Calls Count",
            "SetPass Calls Count", "Batches Count", "Triangles Count",
            "World.GrassVisibility", "World.GrassSubmit", "Physics.Simulate"
        };
        readonly ProfilerRecorder[] systemRecorders = new ProfilerRecorder[SystemCounters.Length];
        readonly List<SystemFrame> systemFrames = new List<SystemFrame>(24000);
        readonly FrameTiming[] systemTiming = new FrameTiming[1];
        ulong systemTimestamp;
        bool systemMeasuring, systemProfile, systemManualRender;
        int systemRenderStarted;
        int systemPhase;
        Vector3 systemHeading = Vector3.forward;
        CappuccinoAI systemCappuccino;
        NailongAI systemNailong;

        [Serializable] struct SystemFrame
        {
            public int phase, frame, enemies;
            public bool bossActive, focused;
            public float wall;
            public double cpu, main, render, wait, gpu;
            public long mainMarker, renderMarker, gc, draw, setPass, batches, triangles, grassVisibility, grassSubmit, physics;
        }

        IEnumerator SystemRoutes()
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            string path = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (captureTarget && asset && captureTarget.antiAliasing != asset.msaaSampleCount)
            {
                captureTarget.Release();
                captureTarget.antiAliasing = asset.msaaSampleCount;
                captureTarget.Create();
            }
            systemMeasuring = true;
            systemRenderStarted = Time.frameCount;
            var handles = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                var description = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(handle);
                int index = Array.IndexOf(SystemCounters, description.Name);
                if (index >= 0 && !systemRecorders[index].Valid)
                    systemRecorders[index] = ProfilerRecorder.StartNew(description.Category, description.Name, 1);
            }
            systemProfile = Array.IndexOf(Environment.GetCommandLineArgs(), "-system-profile") >= 0;
            if (systemProfile)
            {
                Profiler.logFile = Path.ChangeExtension(path, ".raw");
                Profiler.enableBinaryLog = true;
                Profiler.enabled = true;
            }
            var memory = new List<object>();
            try
            {
                var arrival = player.GetComponent<FirstIslandArrival>();
                var cup = systemCappuccino = FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);
                var dragon = systemNailong = FindFirstObjectByType<NailongAI>(FindObjectsInactive.Include);
                var flock = ChickenBroEncounter.Active;
                Vector3 coast = arrival && arrival.shore ? arrival.shore.position : new Vector3(252, 25, 290);
                var locations = new[]
                {
                    new Vector3(235, 25, 500), coast, new Vector3(91.15f, 50, 370.89f),
                    cup ? cup.transform.position - cup.transform.forward * 9 : new Vector3(235,25,500),
                    Vector3.zero, dragon ? dragon.transform.position - dragon.transform.forward * 10 : new Vector3(18,25,868),
                    dragon ? dragon.transform.position - dragon.transform.forward * 10 : new Vector3(18,25,868)
                };
                var names = new[] { "exploration", "coast", "forest", "cappuccino", "flock", "nailong", "boss-effects" };
                for (int i = 0; i < names.Length; i++)
                {
                    if (i == 4)
                    {
                        if (cup) cup.GetComponent<Health>().Kill();
                        if (!flock || !flock.TryGetGuidanceTarget(out locations[i]))
                        {
                            report.skippedPhases.Add(names[i]);
                            continue;
                        }
                    }
                    if (i == 5)
                    {
                        if (flock)
                        {
                            GameWorldProgress.Record((Component)flock.GetComponent<WorldSaveIdentity>() ?? flock, 0);
                            flock.enabled = false;
                            if (ChickenBroFlock.Active != null)
                            {
                                // These belong to the previous sample. Keep their skinned
                                // meshes out of the isolated Nailong window.
                                var members = ChickenBroFlock.Active.Members;
                                for (int member = members.Count - 1; member >= 0; member--)
                                    flock.Reclaim(members[member]);
                                ChickenBroFlock.Active.Close();
                            }
                            yield return null;
                        }
                    }
                    if (i >= 5 && !dragon) { report.skippedPhases.Add(names[i]); continue; }
                    systemPhase = i;
                    systemBurstAt = 0;
                    if (i == 6 && dragon) systemVfx = BossPhaseVfx.For(dragon.gameObject);
                    if (i >= 5) locations[i] = dragon.transform.position - dragon.transform.forward * 6f;
                    Transform opponent = i == 3 && cup ? cup.transform : i >= 5 ? dragon.transform : null;
                    Vector3 heading = opponent ? Vector3.ProjectOnPlane(opponent.position - locations[i], Vector3.up) : Vector3.forward;
                    systemHeading = heading.sqrMagnitude > .01f ? heading.normalized : Vector3.forward;
                    File.WriteAllText(path + ".progress", names[i]);
                    yield return Route(names[i], locations[i], systemHeading);
                    if (finished) yield break;
                    memory.Add(new
                    {
                        phase = names[i], allocated = Profiler.GetTotalAllocatedMemoryLong(),
                        reserved = Profiler.GetTotalReservedMemoryLong(), graphics = Profiler.GetAllocatedMemoryForGraphicsDriver(),
                        enemies = flock ? flock.Alive : 0, cupActive = cup && cup.FightActive,
                        nailongActive = dragon && dragon.FightActive
                    });
                }
                File.WriteAllText(path + ".counters.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    msaa = captureTarget.antiAliasing, renderer = asset.name, systemManualRender,
                    frameTiming = FrameTimingManager.IsFeatureEnabled(), memory, frames = systemFrames,
                    valid = Array.ConvertAll(systemRecorders, recorder => recorder.Valid)
                }));
            }
            finally
            {
                foreach (var recorder in systemRecorders) recorder.Dispose();
                systemMeasuring = false;
                if (systemProfile) { Profiler.enabled = false; Profiler.enableBinaryLog = false; }
            }
        }

        void CollectSystemFrame(float wall)
        {
            if (!systemMeasuring) return;
            if (measuring && wall > 0 && lastRenderedFrame == Time.frameCount - 1)
            {
                long Counter(int index) => systemRecorders[index].Valid ? systemRecorders[index].LastValue : -1;
                var sample = new SystemFrame
                {
                    phase = systemPhase, frame = Time.frameCount, wall = wall,
                    enemies = ChickenBroEncounter.Active ? ChickenBroEncounter.Active.Alive : 0,
                    bossActive = systemPhase == 3 ? systemCappuccino && systemCappuccino.FightActive :
                        systemPhase >= 5 && systemNailong && systemNailong.FightActive,
                    focused = Application.isFocused,
                    cpu = -1, main = -1, render = -1, wait = -1, gpu = -1,
                    mainMarker = Counter(0), renderMarker = Counter(1), gc = Counter(2),
                    draw = Counter(3), setPass = Counter(4), batches = Counter(5), triangles = Counter(6),
                    grassVisibility = Counter(7), grassSubmit = Counter(8), physics = Counter(9)
                };
                if (FrameTimingManager.GetLatestTimings(1, systemTiming) > 0 &&
                    systemTiming[0].frameStartTimestamp > systemTimestamp)
                {
                    var timing = systemTiming[0];
                    systemTimestamp = timing.frameStartTimestamp;
                    sample.cpu = timing.cpuFrameTime; sample.main = timing.cpuMainThreadFrameTime;
                    sample.render = timing.cpuRenderThreadFrameTime; sample.wait = timing.cpuMainThreadPresentWaitTime;
                    if (timing.gpuFrameTime > 0) sample.gpu = timing.gpuFrameTime;
                }
                systemFrames.Add(sample);
            }
            FrameTimingManager.CaptureFrameTimings();
            if (systemVfx && systemPhase == 6 && Time.unscaledTime >= systemBurstAt)
            {
                systemBurstAt = Time.unscaledTime + .25f;
                var position = viewTarget + Vector3.forward * 4;
                systemVfx.Burst(position);
                systemVfx.Burst(position, true);
            }
            if (!captureCamera || !captureTarget || Time.frameCount < systemRenderStarted + 8) return;
            if (!systemManualRender && lastRenderedFrame >= Time.frameCount - 2) return;
            if (!systemManualRender)
            {
                systemManualRender = true;
                Debug.Log("System performance: automatic render callbacks absent. Rendering owned camera explicitly.");
            }
            captureCamera.Render();
        }
        BossPhaseVfx systemVfx;
        float systemBurstAt;
    }
}
