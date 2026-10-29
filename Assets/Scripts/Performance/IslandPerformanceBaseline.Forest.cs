using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace Mavis
{
    public sealed partial class IslandPerformanceBaseline
    {
        [Serializable] public sealed class ForestSample
        {
            public int frame;
            public float wallMs;
            public ulong timestamp;
            public double cpuMs, mainMs, renderMs, presentWaitMs, rawGpuMs, gpuMs;
        }
        [Serializable] public sealed class ForestWindow
        {
            public string name;
            public float scale, spinMs;
            public bool empty;
            public int missingRender, missingTiming, duplicateTiming, rejectedTiming;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public List<ForestSample> samples = new List<ForestSample>();
        }
        [Serializable] public sealed class ForestReport
        {
            public bool timingFeatureEnabled;
            public bool cpuCounterQualified, gpuCounterQualified;
            public bool binaryProfileCaptured, binaryProfileRestored;
            public double cpuPositiveDeltaMs, fullGpuMedianMs, emptyGpuMedianMs;
            public float originalTimeScale, frozenGameTime, endGameTime;
            public string style;
            public List<ForestWindow> windows = new List<ForestWindow>();
        }
        bool forestAttribution, forestOwnsState, forestMeasuring;
        float forestPreviousTimeScale, forestSpinMs;
        int forestOriginalMask;
        Camera forestCamera;
        ForestWindow forestWindow;
        readonly FrameTiming[] forestTiming = new FrameTiming[1];
        ulong forestLastTimestamp;
        ProfilerMarker forestPhaseMarker;
        bool forestOwnsProfiler, forestPreviousProfiler, forestPreviousBinary;
        string forestPreviousLog;

        void BeginForestProfiler()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-baseline-forest-profiler") < 0) return;
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(output)), "forest.raw");
            if (System.IO.File.Exists(path)) throw new InvalidOperationException("Preserve existing forest profile");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            forestPreviousProfiler = Profiler.enabled; forestPreviousBinary = Profiler.enableBinaryLog;
            forestPreviousLog = Profiler.logFile; forestOwnsProfiler = true;
            Profiler.logFile = path; Profiler.enableBinaryLog = true; Profiler.enabled = true;
        }
        void EndForestProfiler()
        {
            if (!forestOwnsProfiler) return;
            Profiler.enabled = false; Profiler.enableBinaryLog = false;
            Profiler.logFile = forestPreviousLog; Profiler.enableBinaryLog = forestPreviousBinary;
            Profiler.enabled = forestPreviousProfiler; forestOwnsProfiler = false;
            if (report.forest != null)
            {
                report.forest.binaryProfileCaptured = true;
                report.forest.binaryProfileRestored = Profiler.enabled == forestPreviousProfiler &&
                    Profiler.enableBinaryLog == forestPreviousBinary && Profiler.logFile == forestPreviousLog;
            }
        }

        void BeginForestState(Camera camera)
        {
            if (forestOwnsState) return;
            forestPreviousTimeScale = Time.timeScale;
            forestCamera = camera; forestOriginalMask = camera.cullingMask;
            forestOwnsState = true; Time.timeScale = 0;
        }
        void RestoreForestState()
        {
            forestMeasuring = false; forestSpinMs = 0;
            EndForestProfiler();
            if (!forestOwnsState) return;
            if (forestCamera) forestCamera.cullingMask = forestOriginalMask;
            Time.timeScale = forestPreviousTimeScale; forestOwnsState = false;
        }
        void ForestControlWork()
        {
            if (!forestOwnsState || finished) return;
            if (!forestMeasuring) { ForestSpin(); return; }
            using (forestPhaseMarker.Auto())
            {
                ForestSpin();
            }
        }
        void ForestSpin()
        {
            if (forestSpinMs <= 0) return;
            long end = System.Diagnostics.Stopwatch.GetTimestamp() +
                (long)(forestSpinMs * System.Diagnostics.Stopwatch.Frequency / 1000.0);
            while (System.Diagnostics.Stopwatch.GetTimestamp() < end) { }
        }
        static bool ForestTimingUsable(FrameTiming value)
        {
            return value.frameStartTimestamp != 0 && FiniteTiming(value.cpuFrameTime, false) &&
                FiniteTiming(value.cpuMainThreadFrameTime, true) && FiniteTiming(value.cpuRenderThreadFrameTime, true) &&
                FiniteTiming(value.cpuMainThreadPresentWaitTime, true);
        }
        static bool FiniteTiming(double value, bool zeroAllowed) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && (zeroAllowed || value > 0) && value < 1000;
        bool ForestConfigurationValid()
        {
            return forestOwnsState && forestCamera && forestWindow != null && diagnosticPipeline &&
                Screen.width == 1920 && Screen.height == 1080 &&
                Mathf.Abs(diagnosticPipeline.renderScale - forestWindow.scale) < .0001f &&
                UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == diagnosticPipeline &&
                Application.targetFrameRate == -1 && QualitySettings.vSyncCount == 0 &&
                !Pixel360Rendering.IsActive && Time.timeScale == 0 &&
                forestCamera.cullingMask == (forestWindow.empty ? 0 : forestOriginalMask) &&
                Vector3.SqrMagnitude(forestCamera.transform.position - forestWindow.cameraPosition) < .000001f &&
                Quaternion.Angle(forestCamera.transform.rotation, forestWindow.cameraRotation) < .001f;
        }
        void CollectForestTiming(float wallMs)
        {
            if (!forestOwnsState || finished) return;
            if (forestMeasuring)
            {
                if (!ForestConfigurationValid()) { Finish("forest attribution configuration changed"); return; }
                if (lastRenderedFrame != Time.frameCount - 1) forestWindow.missingRender++;
                else if (wallMs > 0)
                {
                    var sample = new ForestSample { frame = Time.frameCount, wallMs = wallMs,
                        cpuMs = -1, mainMs = -1, renderMs = -1, presentWaitMs = -1, rawGpuMs = -1, gpuMs = -1 };
                    if (FrameTimingManager.GetLatestTimings(1, forestTiming) == 0) forestWindow.missingTiming++;
                    else
                    {
                        var timing = forestTiming[0];
                        if (timing.frameStartTimestamp <= forestLastTimestamp) forestWindow.duplicateTiming++;
                        else
                        {
                            forestLastTimestamp = timing.frameStartTimestamp;
                            if (!ForestTimingUsable(timing)) forestWindow.rejectedTiming++;
                            else
                            {
                                sample.timestamp = timing.frameStartTimestamp; sample.cpuMs = timing.cpuFrameTime;
                                sample.mainMs = timing.cpuMainThreadFrameTime; sample.renderMs = timing.cpuRenderThreadFrameTime;
                                sample.presentWaitMs = timing.cpuMainThreadPresentWaitTime;
                                sample.rawGpuMs = FiniteTiming(timing.gpuFrameTime, false) ? timing.gpuFrameTime : -1;
                            }
                        }
                    }
                    forestWindow.samples.Add(sample);
                }
            }
            FrameTimingManager.CaptureFrameTimings();
        }
        static double ForestMedian(List<double> values)
        {
            if (values.Count == 0) return -1;
            values.Sort(); return values[values.Count / 2];
        }
        void QualifyForestCounters()
        {
            var data = report.forest;
            var baseline = new List<double>(); var positive = new List<double>();
            var fullGpu = new List<double>(); var emptyGpu = new List<double>();
            foreach (var window in data.windows)
                foreach (var sample in window.samples)
                {
                    sample.gpuMs = -1;
                    if (sample.timestamp == 0) continue;
                    if (window.name == "full-c" || window.name == "full-recovery")
                    {
                        if (sample.mainMs >= 0) baseline.Add(sample.mainMs);
                        if (sample.rawGpuMs > 0) fullGpu.Add(sample.rawGpuMs);
                    }
                    if (window.name == "cpu-control" && sample.mainMs >= 0) positive.Add(sample.mainMs);
                    if (window.name == "empty-control" && sample.rawGpuMs > 0) emptyGpu.Add(sample.rawGpuMs);
                }
            data.cpuPositiveDeltaMs = ForestMedian(positive) - ForestMedian(baseline);
            data.cpuCounterQualified = baseline.Count >= 30 && positive.Count >= 30 && data.cpuPositiveDeltaMs >= 4;
            data.fullGpuMedianMs = ForestMedian(fullGpu); data.emptyGpuMedianMs = ForestMedian(emptyGpu);
            // A finite positive number is not sufficient: this scene's real
            // geometry-to-empty control must produce a material GPU response.
            data.gpuCounterQualified = fullGpu.Count >= 30 && emptyGpu.Count >= 30 &&
                data.fullGpuMedianMs - data.emptyGpuMedianMs > Math.Max(.1, data.fullGpuMedianMs * .1);
            foreach (var window in data.windows)
                foreach (var sample in window.samples)
                {
                    if (!data.cpuCounterQualified) sample.cpuMs = sample.mainMs = sample.renderMs = sample.presentWaitMs = -1;
                    if (data.gpuCounterQualified && sample.timestamp != 0) sample.gpuMs = sample.rawGpuMs;
                }
        }
        IEnumerator RunForestAttribution()
        {
            if (!nativeThroughput || story || styleMatrix || orbitCamera)
            { Finish("forest attribution requires native1080 without story/style matrix/orbit"); yield break; }
            // Match the previous forest route, including the scaled capsule sole.
            Vector3 point = new Vector3(91.15f, 50, 370.89f); float ground = point.y;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 100, Vector3.down, 300, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<ProceduralIsland>()) ground = hit.point.y;
            var capsule = player.GetComponent<CharacterController>();
            float sole = capsule ? (capsule.center.y - capsule.height * .5f) * Mathf.Abs(player.transform.lossyScale.y) : 0;
            point.y = ground + .1f - sole; player.RestoreSavedPose(point, Quaternion.identity);
            player.GetComponent<PlayerHealth>()?.GrantProtection(120);
            viewTarget = point + Vector3.up * 3; overrideCamera = true;
            yield return new WaitForSecondsRealtime(2);
            if (!captureCamera) { Finish("forest camera missing"); yield break; }
            BeginForestState(captureCamera);
            report.forest = new ForestReport { timingFeatureEnabled = FrameTimingManager.IsFeatureEnabled(),
                originalTimeScale = forestPreviousTimeScale, frozenGameTime = Time.time, style = VisualStyles.VisualStyleManager.Instance.Current.id };
            try
            {
                BeginForestProfiler();
                yield return ForestPhase("full-a", 1, false, 0, seconds);
                if (finished) yield break;
                yield return ForestPhase("half-a", .5f, false, 0, seconds);
                if (finished) yield break;
                yield return ForestPhase("full-b", 1, false, 0, seconds);
                if (finished) yield break;
                yield return ForestPhase("half-b", .5f, false, 0, seconds);
                if (finished) yield break;
                yield return ForestPhase("full-c", 1, false, 0, seconds);
                if (finished) yield break;
                yield return ForestPhase("cpu-control", 1, false, 8, 2);
                if (finished) yield break;
                yield return ForestPhase("empty-control", 1, true, 0, 2);
                if (finished) yield break;
                yield return ForestPhase("full-recovery", 1, false, 0, seconds);
                report.forest.endGameTime = Time.time;
                QualifyForestCounters();
            }
            finally { RestoreForestState(); }
        }
        IEnumerator ForestPhase(string name, float scale, bool empty, float spinMs, float duration)
        {
            forestMeasuring = false; forestSpinMs = 0;
            diagnosticPipeline.renderScale = scale; forestCamera.cullingMask = forestOriginalMask;
            forestWindow = new ForestWindow { name = name, scale = scale, empty = empty, spinMs = spinMs,
                cameraPosition = forestCamera.transform.position, cameraRotation = forestCamera.transform.rotation };
            forestPhaseMarker = new ProfilerMarker("ForestPhase." + name);
            report.forest.windows.Add(forestWindow);
            // Finite native pixel readback remains outside all timing windows.
            if (!empty) { yield return VerifyRenderedView("forest-" + name); if (finished) yield break; }
            forestCamera.cullingMask = empty ? 0 : forestOriginalMask;
            forestSpinMs = spinMs;
            yield return new WaitForSecondsRealtime(2);
            // FrameTimingManager is delayed by four frames. Drain more than that
            // after each control switch; never mix preceding-window timings.
            for (int i = 0; i < 10; i++) yield return null;
            forestMeasuring = true; previousFrameTick = 0;
            yield return new WaitForSecondsRealtime(duration);
            forestMeasuring = false; forestSpinMs = 0;
        }
    }
}
