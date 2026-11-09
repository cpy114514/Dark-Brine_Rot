using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEngine;

public static class AnalyzeSystemPerformanceProfile
{
    sealed class Stats
    {
        public int frames;
        public readonly Dictionary<string, double> self = new Dictionary<string, double>();
        public readonly Dictionary<string, double> inclusive = new Dictionary<string, double>();
    }

    static void Accumulate(Dictionary<string, double> values, string name, double milliseconds)
    {
        values.TryGetValue(name, out double previous);
        values[name] = previous + milliseconds;
    }

    public static void Run()
    {
        if (!Application.isBatchMode || !Directory.GetCurrentDirectory().EndsWith("build-workspace", StringComparison.Ordinal))
            throw new InvalidOperationException("Load captures only in the isolated batch workspace.");
        string path = Environment.GetEnvironmentVariable("BRINE_PERFORMANCE_PROFILE");
        if (!File.Exists(path) || !ProfilerDriver.LoadProfile(path, false))
            throw new IOException("Cannot load native Player profile.");
        var threads = new Dictionary<string, Stats>();
        for (int frame = ProfilerDriver.firstFrameIndex; frame <= ProfilerDriver.lastFrameIndex; frame++)
        {
            for (int thread = 0; thread < 128; thread++)
            {
                using var data = ProfilerDriver.GetRawFrameDataView(frame, thread);
                if (!data.valid) break;
                string name = string.IsNullOrEmpty(data.threadName) ? "Unnamed thread" : data.threadName;
                if (!threads.TryGetValue(name, out var stats)) threads.Add(name, stats = new Stats());
                stats.frames++;
                for (int sample = 0; sample < data.sampleCount; sample++)
                {
                    string marker = data.GetSampleName(sample) ?? "<unnamed>";
                    double total = data.GetSampleTimeNs(sample) / 1e6;
                    double self = total;
                    int end = sample + data.GetSampleChildrenCountRecursive(sample);
                    for (int child = sample + 1; child <= end; child += data.GetSampleChildrenCountRecursive(child) + 1)
                        self -= data.GetSampleTimeNs(child) / 1e6;
                    Accumulate(stats.self, marker, Math.Max(0, self));
                    Accumulate(stats.inclusive, marker, total);
                }
            }
        }
        var rows = threads.Select(entry => new
        {
            thread = entry.Key, samples = entry.Value.frames,
            self = entry.Value.self.OrderByDescending(value => value.Value).Take(25)
                .Select(value => new { name = value.Key, ms = value.Value / entry.Value.frames }),
            inclusive = entry.Value.inclusive.OrderByDescending(value => value.Value).Take(35)
                .Select(value => new { name = value.Key, ms = value.Value / entry.Value.frames })
        });
        File.WriteAllText(path + ".analysis.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex, rows
        }, Newtonsoft.Json.Formatting.Indented));
    }
}
