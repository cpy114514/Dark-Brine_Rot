using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public static class AutoQaFootprintProfilerRegression
{
    static object heldControl;
    public static async Task<object> Run(string take)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        string path = "AUTO_QA/Evidence/" + take + ".json";
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$") || File.Exists(path)) throw new Exception("Preserve prior evidence");
        bool oldProfiler = ProfilerDriver.enabled, oldCpu = ProfilerDriver.IsAreaEnabled(ProfilerArea.CPU);
        var go = new GameObject("AUTO_QA footprint profiler");
        var component = go.AddComponent<SandFootprints>(); component.enabled = false;
        var renderers = component.GetComponentsInChildren<MeshRenderer>(true);
        var method = typeof(SandFootprints).GetMethod("SetAlpha", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        var alpha = (Action<MeshRenderer, float>)(method.IsStatic ? Delegate.CreateDelegate(typeof(Action<MeshRenderer, float>), method) : Delegate.CreateDelegate(typeof(Action<MeshRenderer, float>), component, method));
        var rows = new List<object>(); string error = null;
        long controlBytes = 0, footprintBytes = 0; int controlSamples = 0, footprintSamples = 0;
        double footprintMs = 0;
        try
        {
            for (int i = 0; i < 100; i++) alpha(renderers[i % renderers.Length], .5f);
            ProfilerDriver.enabled = true; ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, true);
            int beginFrame = ProfilerDriver.lastFrameIndex;
            for (int sample = 0; sample < 16; sample++)
            {
                int frame = Time.frameCount; var limit = DateTime.UtcNow.AddSeconds(10);
                while (Time.frameCount == frame) { if (!Application.isPlaying || DateTime.UtcNow > limit) throw new Exception("Profiler frame stalled"); await Task.Delay(5); }
                Profiler.BeginSample("AUTO_QA.Alloc.Control"); heldControl = new byte[1024 * 1024]; Profiler.EndSample();
                Profiler.BeginSample("AUTO_QA.Alloc.Footprint"); for (int i = 0; i < 1000; i++) alpha(renderers[i % renderers.Length], .5f); Profiler.EndSample();
            }
            await Task.Delay(100);
            for (int frame = Math.Max(beginFrame, ProfilerDriver.firstFrameIndex); frame <= ProfilerDriver.lastFrameIndex - 1; frame++)
            using (var raw = ProfilerDriver.GetRawFrameDataView(frame, 0))
            {
                if (!raw.valid) continue;
                var parents = new Stack<(int end, string name)>(); long cb = 0, fb = 0;
                for (int i = 0; i < raw.sampleCount; i++)
                {
                    while (parents.Count > 0 && parents.Peek().end < i) parents.Pop();
                    string name = raw.GetSampleName(i); int children = raw.GetSampleChildrenCountRecursive(i);
                    if (name == "AUTO_QA.Alloc.Control") controlSamples++;
                    if (name == "AUTO_QA.Alloc.Footprint") { footprintSamples++; footprintMs += raw.GetSampleTimeMs(i); }
                    if (name == "GC.Alloc" && raw.GetSampleMetadataCount(i) > 0)
                    {
                        long bytes = raw.GetSampleMetadataAsLong(i, 0);
                        if (parents.Any(p => p.name == "AUTO_QA.Alloc.Control")) cb += bytes;
                        if (parents.Any(p => p.name == "AUTO_QA.Alloc.Footprint")) fb += bytes;
                    }
                    if (children > 0) parents.Push((i + children, name));
                }
                if (cb > 0 || fb > 0) rows.Add(new { frame, controlBytes = cb, footprintBytes = fb });
                controlBytes += cb; footprintBytes += fb;
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            heldControl = null; ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, oldCpu); ProfilerDriver.enabled = oldProfiler;
            UnityEngine.Object.DestroyImmediate(go);
        }
        bool valid = error == null && controlSamples >= 10 && footprintSamples >= 10 && controlBytes >= controlSamples * 1024L * 1024;
        var result = new { validMeasurement = valid, error, controlSamples, footprintSamples, controlBytes, footprintBytes,
            bytesPer1000Calls = footprintSamples > 0 ? (double)footprintBytes / footprintSamples : -1,
            meanMsPer1000Calls = footprintSamples > 0 ? footprintMs / footprintSamples : -1, rows,
            originalProfilerRestored = ProfilerDriver.enabled == oldProfiler && ProfilerDriver.IsAreaEnabled(ProfilerArea.CPU) == oldCpu };
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented)); return result;
    }
}
