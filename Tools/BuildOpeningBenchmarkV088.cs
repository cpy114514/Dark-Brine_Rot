using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Threading.Tasks;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BuildOpeningBenchmarkV088
{
    static Task pending;
    public static object Start()
    {
        if(BuildPipeline.isBuildingPlayer)throw new Exception("A build is already running");
        if(EditorApplication.delayCall!=null)
            foreach(EditorApplication.CallbackFunction callback in EditorApplication.delayCall.GetInvocationList())
                if(callback.Method.DeclaringType?.FullName.StartsWith("BuildOpeningBenchmarkV088")==true)EditorApplication.delayCall-=callback;
        pending=RunBuild();return new{started=true,nonBlockingBuild=true};
    }
    static async Task RunBuild()
    {
        await Task.Delay(500);
        try{Build();}
        catch(Exception error){File.WriteAllText(".codex/opening-optimization-20261012/build-error-v088.txt",error.ToString());}
    }
    public static object Build()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready original Edit Mode required");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Preserve user dirty scene; do not save it for this build");
        const string root=".codex/opening-optimization-20261012";
        string folder=Path.GetFullPath(root+"/standalone-v088");
        if(File.Exists(folder+"/DarkBrineOpening.exe"))throw new Exception("Existing versioned build must not be overwritten");
        Directory.CreateDirectory(folder);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,target=BuildTarget.StandaloneWindows64,
            locationPathName=folder+"/DarkBrineOpening.exe",options=BuildOptions.Development});
        var result=new{result=report.summary.result.ToString(),size=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,
            errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,path=folder+"/DarkBrineOpening.exe",
            biteRevision="v078",resolvedPoseJoinRevision="v079",cameraRevision="v076",biteApproachRevision="v088",hopPlanningRevision="v083",hopClipRevision="v086",opensAnotherEditor=false};
        File.WriteAllText(root+"/build-report-v088.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
