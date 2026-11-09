using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SystemPerformanceBuild
{
    public static void Run()
    {
        if (!Application.isBatchMode || !Directory.GetCurrentDirectory().EndsWith("build-workspace", StringComparison.Ordinal))
            throw new InvalidOperationException("Run only in the isolated performance build workspace.");
        string output = Environment.GetEnvironmentVariable("BRINE_PERFORMANCE_BUILD");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set the output path.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        PlayerSettings.productName = "DarkBrineRot_PerformanceQA";
        PlayerSettings.enableFrameTimingStats = true;
        PlayerSettings.meshDeformation = Environment.GetEnvironmentVariable("BRINE_PERFORMANCE_GPU_SKINNING") == "1"
            ? MeshDeformation.GPUBatched : MeshDeformation.CPU;
        bool development = Environment.GetEnvironmentVariable("BRINE_PERFORMANCE_RELEASE") != "1";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
        var summary = report.summary;
        File.WriteAllText(output + ".build.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            result = summary.result.ToString(), seconds = summary.totalTime.TotalSeconds,
            summary.totalErrors, summary.totalWarnings, summary.totalSize,
            skinning = PlayerSettings.meshDeformation.ToString(), development,
            packedTextures = report.packedAssets.SelectMany(asset => asset.contents)
                .Where(asset => asset.type == typeof(Texture2D)).OrderByDescending(asset => asset.packedSize)
                .Take(30).Select(asset => new { asset.sourceAssetPath, asset.packedSize })
        }));
        if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed.");
    }
}
