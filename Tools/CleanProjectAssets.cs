using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class CleanProjectAssets
{
    sealed class Move {public string source,target,guid;}
    static readonly string[] backups={"Assets/_Recovery","Assets/ProjectRepairBackups","Assets/Temp"};
    static readonly string[] diagnostics={"Assets/Editor/PlasticList.cs","Assets/Editor/PlasticIncoming.cs","Assets/Editor/PlasticMenuProbe.cs"};
    static bool Under(string path,string root)=>path==root || path.StartsWith(root+"/",StringComparison.Ordinal);
    public static object Run(string archive,bool apply=false)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before cleaning project assets.");
        string project=Directory.GetParent(Application.dataPath).FullName;
        archive=Path.GetFullPath(archive);
        if(archive.StartsWith(project+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || archive==project)
            throw new InvalidOperationException("The recovery archive must be outside this project.");
        var removed=backups.Concat(diagnostics).Where(p=>File.Exists(p) || Directory.Exists(p)).ToArray();
        var roots=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p) && !removed.Any(r=>Under(p,r))).ToArray();
        var references=AssetDatabase.GetDependencies(roots,true).Where(p=>removed.Any(r=>Under(p,r))).ToArray();
        if(references.Length!=0)throw new InvalidOperationException("Cleanup targets are referenced: "+string.Join(", ",references));
        var open=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).Where(p=>removed.Any(r=>Under(p,r))).ToArray();
        if(open.Length!=0)throw new InvalidOperationException("Close recovery scenes before archiving: "+string.Join(", ",open));
        var moves=new List<Move>();
        foreach(string source in Directory.GetFiles("Assets/Scripts","*.cs"))
        {
            string name=Path.GetFileName(source),folder;
            if(name.StartsWith("Story1") || name.StartsWith("Tralalero"))folder="Story/Chapter1";
            else if(name=="FirstIslandArrival.cs")folder="Story/FirstIsland";
            else if(name.StartsWith("Ship"))folder="Ships";
            else if(name.StartsWith("Game"))folder="Systems";
            else if(name=="AdditiveSceneBootstrap.cs")folder="World";
            else if(name=="NaturalParticleEffects.cs")folder="Effects";
            else if(name=="FoliageMeshLibrary.cs" || name=="WorldPerformanceManager.cs")folder="Performance";
            else continue;
            string normalized=source.Replace('\\','/');moves.Add(new Move {source=normalized,target="Assets/Scripts/"+folder+"/"+name,guid=AssetDatabase.AssetPathToGUID(normalized)});
        }
        const string rootModel="Assets/d2516b4184714e31ebd1b11148104a37.fbx";
        if(File.Exists(rootModel))moves.Add(new Move {source=rootModel,target="Assets/Game/Shared/Models/Unclassified/"+Path.GetFileName(rootModel),guid=AssetDatabase.AssetPathToGUID(rootModel)});
        foreach(var move in moves)
            if(File.Exists(move.target))throw new InvalidOperationException("Target already exists: "+move.target);
        var plan=new {archive,apply,removed,moves};
        if(!apply)return plan;
        if(removed.Length==0 && moves.Count==0)return new {success=true,archive,movedAssets=0,archivedAssetTargets=0,preservedGuids=true};
        Directory.CreateDirectory(archive);
        // Copy and hash-check every asset before any removal or move.
        foreach(string path in removed.Concat(moves.Select(m=>m.source)))Backup(path,archive);
        foreach(string path in diagnostics.Where(File.Exists))
        {
            string destination="Tools/Diagnostics/VersionControl/"+Path.GetFileName(path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if(File.Exists(destination))
            {if(File.ReadAllText(destination)!=File.ReadAllText(path))throw new InvalidOperationException("Diagnostic destination has different content: "+destination);}
            else File.Copy(path,destination);
        }
        File.WriteAllText(Path.Combine(archive,"asset-cleanup-plan.json"),Newtonsoft.Json.JsonConvert.SerializeObject(plan,Newtonsoft.Json.Formatting.Indented));
        // Folder imports must finish before the batched moves can target them.
        foreach(var move in moves)EnsureFolder(Path.GetDirectoryName(move.target).Replace('\\','/'));
        EditorApplication.LockReloadAssemblies();AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var move in moves)
            {
                string error=AssetDatabase.MoveAsset(move.source,move.target);
                if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            }
            foreach(string path in removed)
                if(!AssetDatabase.DeleteAsset(path))throw new InvalidOperationException("Unity could not remove archived asset: "+path);
        }
        finally{AssetDatabase.StopAssetEditing();EditorApplication.UnlockReloadAssemblies();}
        foreach(var move in moves)
            if(AssetDatabase.AssetPathToGUID(move.target)!=move.guid)throw new InvalidOperationException("Asset GUID changed: "+move.target);
        return new {success=true,archive,movedAssets=moves.Count,archivedAssetTargets=removed.Length,preservedGuids=true};
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
        if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(path))))throw new IOException("Cannot create asset folder: "+path);
    }
    static void Backup(string path,string archive)
    {
        if(Directory.Exists(path))foreach(string file in Directory.GetFiles(path,"*",SearchOption.AllDirectories))CopyChecked(file,archive);
        else if(File.Exists(path))CopyChecked(path,archive);
        if(File.Exists(path+".meta"))CopyChecked(path+".meta",archive);
    }
    static void CopyChecked(string source,string archive)
    {
        string relative=source.Replace('\\','/');string destination=Path.GetFullPath(Path.Combine(archive,relative));
        if(!destination.StartsWith(archive+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Archive path escaped its root.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));if(!File.Exists(destination))File.Copy(source,destination,false);
        using(var hash=SHA256.Create())using(var a=File.OpenRead(source))using(var b=File.OpenRead(destination))
            if(!hash.ComputeHash(a).SequenceEqual(hash.ComputeHash(b)))throw new IOException("Backup verification failed: "+source);
    }
}
