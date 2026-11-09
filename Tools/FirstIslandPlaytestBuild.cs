using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class FirstIslandPlaytestBuild
{
    const string Work = ".codex/first-island-playtest-20261107/";
    const string TestProduct = "DarkBrineRot_Playtest";
    const string PlayerSettingsPath = "ProjectSettings/ProjectSettings.asset";
    static string Hash(string p){using(var sha=SHA256.Create())using(var stream=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(stream));}
    static void Write(string name,object value)=>File.WriteAllText(Work+name,JsonConvert.SerializeObject(value,Formatting.Indented));
    static Dictionary<string,string> Saves()=>Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p)));
    public static object Snapshot()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Stopped, compiled workspace required");
        if(File.Exists(Work+"baseline.json"))throw new Exception("Preserve the existing baseline");
        Directory.CreateDirectory(Work);Directory.CreateDirectory(Work+"scene-copies");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        // Read dirty states without saving or reloading them.
        var scenes=setup.Select(s=>SceneManager.GetSceneByPath(s.path)).Where(s=>s.IsValid()).ToArray();
        foreach(var scene in scenes)if(!string.IsNullOrEmpty(scene.path))File.Copy(scene.path,Work+"scene-copies/"+Path.GetFileName(Path.GetDirectoryName(scene.path))+"-"+Path.GetFileName(scene.path));
        var prefabs=Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>g.scene.IsValid()&&g.scene.isLoaded&&PrefabUtility.IsAnyPrefabInstanceRoot(g)).Select(g=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(g).ToString(),source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g),overrides=PrefabUtility.GetPropertyModifications(g)?.Length??0}).ToArray();
        Write("workspace.json",setup);Write("saves.json",Saves());
        var baseline=new{capturedUtc=DateTime.UtcNow.ToString("o"),unity=Application.unityVersion,product=PlayerSettings.productName,company=PlayerSettings.companyName,savesPath=Application.persistentDataPath,graphics=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Select(g=>g.ToString()),automaticGraphics=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64),target=EditorUserBuildSettings.activeBuildTarget.ToString(),scenes=scenes.Select(s=>new{s.path,s.isDirty,hash=Hash(s.path)}),prefabs,settings=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset","Packages/manifest.json","Packages/packages-lock.json"}.Where(File.Exists).ToDictionary(p=>p,Hash)};
        Write("baseline.json",baseline);return new{scenes=scenes.Length,dirty=scenes.Count(s=>s.isDirty),prefabRoots=prefabs.Length,product=PlayerSettings.productName};
    }
    public static object Development()=>Build(true);
    // QA-only input transport. The shipped release and the authored settings stay disabled.
    public static object DevelopmentWithInput() => BuildWithInput("development-input");
    public static object DevelopmentFixed() => BuildWithInput("development-fixed");
    public static void BuildBatchPair()
    {
        if(!Application.isBatchMode)throw new Exception("Batch build only");
        BuildWithInput("development-final");
        Release();
    }
    static object BuildWithInput(string variant)
    {
        var original=Unity.Pipeline.Config.RuntimePipelineConfig.Load();
        var temporary=UnityEngine.ScriptableObject.CreateInstance<Unity.Pipeline.Config.RuntimePipelineConfig>();
        bool originalDevelopment=EditorUserBuildSettings.development;
        if(original!=null)EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(original),temporary);
        try
        {
            // Pipeline's build callback checks this setting separately from BuildPlayerOptions.
            EditorUserBuildSettings.development=true;
            temporary.enableInBuilds=true;
            temporary.autoStart=true;
            temporary.Save();
            return Build(true,variant);
        }
        finally
        {
            EditorUserBuildSettings.development=originalDevelopment;
            if(original!=null){original.Save();UnityEngine.Object.DestroyImmediate(original);}
            else Unity.Pipeline.Config.RuntimePipelineConfig.DeleteSettingsFileForTesting();
            UnityEngine.Object.DestroyImmediate(temporary);
        }
    }
    public static object Release()=>Build(false);
    static object Build(bool development, string variant = null)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped, idle Editor required");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Keep unsaved scenes; build only after their state is preserved");
        if(Application.unityVersion!="6000.3.23f1")throw new Exception("Keep the pinned editor version");
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Do not silently switch/import another target");
        if(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64)||!PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).SequenceEqual(new[]{GraphicsDeviceType.Direct3D11}))throw new Exception("Expected the existing DX11 configuration");
        string kind=variant??(development?"development":"release"),oldProduct=PlayerSettings.productName;
        var oldPreloaded=PlayerSettings.GetPreloadedAssets();
        var settingsBytes=File.ReadAllBytes(PlayerSettingsPath);
        string output=Path.GetFullPath(Work+"build/"+kind+"/DarkBrineRot_Playtest.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        Write("build-restore.json",new{originalProduct=oldProduct,testProduct=TestProduct,inProgress=true,kind,startedUtc=DateTime.UtcNow.ToString("o")});
        try
        {
            PlayerSettings.productName=TestProduct;
            var options=new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output,target=BuildTarget.StandaloneWindows64,options=development?BuildOptions.Development:BuildOptions.None};
            var report=BuildPipeline.BuildPlayer(options);var summary=report.summary;
            var messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning||m.type==LogType.Exception).Select(m=>new{type=m.type.ToString(),m.content}).ToArray();
            var result=new{passed=summary.result==BuildResult.Succeeded,kind,result=summary.result.ToString(),summary.totalErrors,summary.totalWarnings,seconds=summary.totalTime.TotalSeconds,bytes=summary.totalSize,output,messages};Write("build-"+kind+".json",result);return result;
        }
        finally
        {
            PlayerSettings.productName=oldProduct;
            PlayerSettings.SetPreloadedAssets(oldPreloaded);
            RestoreBuildSettingsSnapshot(settingsBytes,oldProduct,kind);
            Write("build-restore.json",new{originalProduct=oldProduct,testProduct=TestProduct,inProgress=false,
                restored=PlayerSettings.productName==oldProduct&&PlayerSettings.GetPreloadedAssets().SequenceEqual(oldPreloaded),kind});
        }
    }
    static void RestoreBuildSettingsSnapshot(byte[] original, string product, string kind)
    {
        // BuildPlayer flushes its temporary settings to disk. The native setters
        // above restore the live editor, but saving this singleton with AssetDatabase
        // does not flush it. Recover the exact pre-build bytes if the difference is
        // limited to the QA product and App UI's generated preload entry.
        var built=File.ReadAllBytes(PlayerSettingsPath);
        if(built.SequenceEqual(original))return;
        File.WriteAllBytes(Work+"settings-before-"+kind+".asset",original);
        File.WriteAllBytes(Work+"settings-build-written-"+kind+".asset",built);
        var originalText=System.Text.Encoding.UTF8.GetString(original);
        var builtText=System.Text.Encoding.UTF8.GetString(built);
        var normalized=builtText.Replace("  productName: "+TestProduct+"\n","  productName: "+product+"\n");
        if(EditorBuildSettings.TryGetConfigObject<ScriptableObject>("com.unity.dt.app-ui",out var config)&&
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(config,out string guid,out long fileId))
        {
            string preload=$"  - {{fileID: {fileId}, guid: {guid}, type: 2}}\n";
            if(!originalText.Contains(preload))normalized=normalized.Replace(preload+"  metroInputSource:","  metroInputSource:");
        }
        if(normalized!=originalText)throw new Exception("Unexpected PlayerSettings changes; saved both snapshots for review");
        File.WriteAllBytes(PlayerSettingsPath,original);
    }
    public static object VerifyProtection()
    {
        var baseline=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Work+"baseline.json"));
        var saves=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Work+"saves.json"));
        bool saveBytes=saves.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value)&&Saves().Count==saves.Count;
        var scenes=baseline["scenes"].Select(s=>
        {
            string path=(string)s["path"];
            var scene=SceneManager.GetSceneByPath(path);
            return new{path, same=Hash(path)==(string)s["hash"], loaded=scene.isLoaded, dirty=scene.isDirty};
        }).ToArray();
        var settings=((Newtonsoft.Json.Linq.JObject)baseline["settings"]).Properties()
            .ToDictionary(p=>p.Name,p=>File.Exists(p.Name)&&Hash(p.Name)==(string)p.Value);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        bool setupSame=JsonConvert.SerializeObject(setup,Formatting.Indented)==File.ReadAllText(Work+"workspace.json");
        // Scene byte identity covers the complete persisted Prefab overrides, not only their counts.
        bool stopped=!EditorApplication.isPlayingOrWillChangePlaymode;
        bool productRestored=PlayerSettings.productName==(string)baseline["product"];
        var result=new{passed=saveBytes&&scenes.All(s=>s.same&&s.loaded&&!s.dirty)&&settings.Values.All(x=>x)&&setupSame&&productRestored&&stopped,
            saveBytes,scenes,settings,setupSame,productRestored,product=PlayerSettings.productName,stopped};
        Write("protection.json",result);return result;
    }
}
