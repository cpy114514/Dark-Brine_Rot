using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class AuditProjectIntegrityV223
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stop the test before integrity audit");
        var before=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/cleanup/dependencies-20261018-before.json"));
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var dependencies=AssetDatabase.GetDependencies(scenes,true).Select(p=>AssetDatabase.AssetPathToGUID(p)).OrderBy(g=>g).ToArray();
        var oldDependencies=before["sceneDependencies"].Select(p=>(string)p["guid"]).OrderBy(g=>g).ToArray();
        if(!dependencies.SequenceEqual(oldDependencies))throw new Exception("Build-scene dependency GUIDs changed");
        if(!scenes.SequenceEqual(before["scenes"].Select(p=>(string)p)))throw new Exception("Build scenes changed");
        bool Archived(string path)=>path.StartsWith("Assets/Resources/Encounter/v214/CH1_SahurGetUp_v214/")||path.StartsWith("Assets/Resources/Encounter/v214/CH1_SahurOverhead_v214/")||path.StartsWith("Assets/Resources/Encounter/v214/CH1_SahurProneDodge_v214/");
        var oldResources=before["runtimeResources"].Where(p=>!Archived((string)p["path"])).Select(p=>(string)p["path"]+"|"+(string)p["guid"]).OrderBy(p=>p).ToArray();
        var resources=paths.Where(p=>p.StartsWith("Assets/Resources/")).Select(p=>p+"|"+AssetDatabase.AssetPathToGUID(p)).OrderBy(p=>p).ToArray();
        if(!resources.SequenceEqual(oldResources))throw new Exception("A retained runtime resource path/GUID changed");
        var guids=new Dictionary<string,string>();var duplicates=new List<string>();var orphans=new List<string>();
        foreach(string meta in Directory.GetFiles("Assets","*.meta",SearchOption.AllDirectories))
        {
            string source=meta.Substring(0,meta.Length-5);
            if(!File.Exists(source)&&!Directory.Exists(source))orphans.Add(meta);
            string line=File.ReadLines(meta).FirstOrDefault(l=>l.StartsWith("guid: "));
            if(line==null)continue;string guid=line.Substring(6).Trim();
            if(guids.TryGetValue(guid,out string previous))duplicates.Add(previous+" / "+meta);else guids.Add(guid,meta);
        }
        if(duplicates.Count>0||orphans.Count>0)throw new Exception("Duplicate GUID or orphan metadata: "+string.Join(",",duplicates.Concat(orphans)));
        var result=new{passed=true,buildScenes=scenes.Length,sceneDependencyGuids=dependencies.Length,retainedRuntimeResources=resources.Length,archivedRuntimeResourceFiles=before["runtimeResources"].Count()-resources.Length,duplicateGuids=duplicates.Count,orphanMeta=orphans.Count,guidsChecked=guids.Count,sourceGuidsPreserved=true};
        File.WriteAllText(".codex/project-cleanup-20261018/asset-integrity.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
