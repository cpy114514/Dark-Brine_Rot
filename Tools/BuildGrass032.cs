using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildGrass032
{
    const string Root=".codex/grass032/build";
    static Task pending;
    public static object Start()
    {
        if(Application.isPlaying||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer||File.Exists(Root+"/build-running"))throw new Exception("Ready unowned Editor required");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve dirty scene");
        if(File.Exists(Root+"/player/DarkBrineQA032.exe"))throw new Exception("Keep existing versioned build");
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
        const string duckPath="Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.mat";
        var duckBytes=File.ReadAllBytes(duckPath);Material duck;
        File.WriteAllBytes(Root+"/build-before/LongLeggedDuck.mat",duckBytes);
        try
        {
            string folder=Path.GetFullPath(Root+"/player");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName=folder+"/DarkBrineQA032.exe",options=BuildOptions.Development});
            var summary=report.summary;
            File.WriteAllText(Root+"/build-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result=summary.result.ToString(),seconds=summary.totalTime.TotalSeconds,bytes=summary.totalSize,warnings=summary.totalWarnings,errors=summary.totalErrors,output=summary.outputPath,development=true,backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(),sameEditor=true,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id,scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),errorDetails=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Exception).Select(m=>m.content).ToArray()},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception error){File.WriteAllText(Root+"/build-error.txt",error.ToString());}
        finally{EditorUserBuildSettings.development=development;EditorUserBuildSettings.allowDebugging=debug;EditorUserBuildSettings.connectProfiler=profiler;PlayerSettings.SetPreloadedAssets(preloaded);File.WriteAllBytes("ProjectSettings/ProjectSettings.asset",settingsBytes);if(!File.ReadAllBytes(duckPath).SequenceEqual(duckBytes))
        {
            duck=AssetDatabase.LoadAssetAtPath<Material>(duckPath);
            if(duck.GetTexture("_MainTex")!=duck.GetTexture("_BaseMap"))throw new Exception("Preserve unexpected material mutation");
            duck.SetTexture("_MainTex",null);EditorUtility.SetDirty(duck);AssetDatabase.SaveAssetIfDirty(duck);
            if(!File.ReadAllBytes(duckPath).SequenceEqual(duckBytes))throw new Exception("Preserve additional material mutation");
        }
        File.WriteAllText(Root+"/cleanup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{buildFlagsRestored=EditorUserBuildSettings.development==development&&EditorUserBuildSettings.allowDebugging==debug&&EditorUserBuildSettings.connectProfiler==profiler,preloadedAssetsRestored=PlayerSettings.GetPreloadedAssets().SequenceEqual(preloaded),settingsBytesRestored=File.ReadAllBytes("ProjectSettings/ProjectSettings.asset").SequenceEqual(settingsBytes),duckBytesRestored=File.ReadAllBytes(duckPath).SequenceEqual(duckBytes)},Newtonsoft.Json.Formatting.Indented));
        File.Delete(Root+"/build-running");}
    }
}
