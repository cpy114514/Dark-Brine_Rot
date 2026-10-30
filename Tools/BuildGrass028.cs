using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildGrass028
{
    const string Root=".codex/grass028/build";
    static Task pending;
    public static object Start()
    {
        if(Application.isPlaying||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer||File.Exists(Root+"/build-running"))throw new Exception("Ready unowned Editor required");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve dirty scene");
        if(File.Exists(Root+"/player/DarkBrineQA028.exe"))throw new Exception("Keep existing versioned build");
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Do not switch the user's build platform");
        File.WriteAllText(Root+"/build-running",DateTime.UtcNow.ToString("o"));pending=Build();return new{started=true,sameEditor=true};
    }
    static async Task Build()
    {
        await Task.Delay(1000);
        bool development=EditorUserBuildSettings.development,debug=EditorUserBuildSettings.allowDebugging,profiler=EditorUserBuildSettings.connectProfiler;
        var preloaded=PlayerSettings.GetPreloadedAssets();
        // Some build callbacks temporarily serialize extra preload references to disk.
        // Restore original bytes after restoring the native preload list; no YAML edits.
        var settingsBytes=File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");
        Directory.CreateDirectory(Root+"/build-before");File.WriteAllBytes(Root+"/build-before/ProjectSettings.asset",settingsBytes);
        try
        {
            string folder=Path.GetFullPath(Root+"/player");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName=folder+"/DarkBrineQA028.exe",options=BuildOptions.Development});
            var summary=report.summary;
            File.WriteAllText(Root+"/build-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result=summary.result.ToString(),seconds=summary.totalTime.TotalSeconds,bytes=summary.totalSize,warnings=summary.totalWarnings,errors=summary.totalErrors,output=summary.outputPath,development=true,backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(),sameEditor=true,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),errorDetails=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Exception).Select(m=>m.content).ToArray()},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception error){File.WriteAllText(Root+"/build-error.txt",error.ToString());}
        finally{EditorUserBuildSettings.development=development;EditorUserBuildSettings.allowDebugging=debug;EditorUserBuildSettings.connectProfiler=profiler;PlayerSettings.SetPreloadedAssets(preloaded);File.WriteAllBytes("ProjectSettings/ProjectSettings.asset",settingsBytes);File.Delete(Root+"/build-running");}
    }
}
