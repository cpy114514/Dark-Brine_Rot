using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BuildOpeningBenchmarkV039
{
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before building");
        string folder=Path.GetFullPath(".codex/opening-optimization-20261012/standalone");Directory.CreateDirectory(folder);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,target=BuildTarget.StandaloneWindows64,locationPathName=folder+"/DarkBrineOpening.exe",options=BuildOptions.Development});
        var result=new{result=report.summary.result.ToString(),size=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,path=folder+"/DarkBrineOpening.exe"};
        File.WriteAllText(".codex/opening-optimization-20261012/build-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
