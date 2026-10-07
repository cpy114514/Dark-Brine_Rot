using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AuditProjectCleanup
{
    static readonly string[] backupRoots={"Assets/_Recovery","Assets/ProjectRepairBackups","Assets/Temp"};
    static bool IsBackup(string path)=>backupRoots.Any(root=>path==root || path.StartsWith(root+"/",StringComparison.Ordinal));
    public static object Run(string label="before")
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before auditing project assets.");
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)).ToArray();
        var regular=paths.Where(p=>!IsBackup(p)).ToArray();
        var referencedBackups=AssetDatabase.GetDependencies(regular,true).Where(IsBackup).Distinct().OrderBy(p=>p).ToArray();
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var sceneDependencies=AssetDatabase.GetDependencies(scenes,true).OrderBy(p=>p).Select(p=>new{path=p,guid=AssetDatabase.AssetPathToGUID(p)}).ToArray();
        var runtimeResources=paths.Where(p=>p.StartsWith("Assets/Resources/",StringComparison.Ordinal)).OrderBy(p=>p).Select(p=>new{path=p,guid=AssetDatabase.AssetPathToGUID(p)}).ToArray();
        var rootModel=AssetDatabase.GUIDToAssetPath("12cf83c4f8df11e46b335cb8a2fcfd5f");
        var result=new {label,assetCount=paths.Length,scenes,referencedBackups,backupAssets=paths.Where(IsBackup).OrderBy(p=>p).ToArray(),
            sceneDependencies,runtimeResources,rootModel=new{guid=AssetDatabase.AssetPathToGUID(rootModel),meshes=AssetDatabase.LoadAllAssetsAtPath(rootModel).OfType<Mesh>().Select(m=>m.name).ToArray()}};
        string destination=Path.GetFullPath(".codex/cleanup/dependencies-"+label+".json");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.WriteAllText(destination,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new {label,result.assetCount,buildScenes=scenes.Length,runtimeResources=runtimeResources.Length,sceneDependencies=sceneDependencies.Length,referencedBackups,backupAssets=result.backupAssets.Length,result.rootModel};
    }
}
