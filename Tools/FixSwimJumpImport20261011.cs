using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class FixSwimJumpImport20261011
{
    const string Backup=".codex/swim-jump-fix-20261011";
    public static object Swimming()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before importing animations.");
        var result=new System.Collections.Generic.List<object>();
        foreach(string name in new[]{"PLAYER_Swim_v007","PLAYER_SwimIdle_v007","PLAYER_SwimFast_v007"})
        {
            string path=$"Assets/Resources/Encounter/v007/{name}/{name}.fbx";
            string backup=Path.Combine(Backup,path+".meta");Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))File.Copy(path+".meta",backup);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);var clips=importer.clipAnimations;
            foreach(var clip in clips)clip.rotationOffset=180;
            importer.clipAnimations=clips;importer.SaveAndReimport();
            result.Add(new{path,avatarValid=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().All(a=>a.isValid),heading=180});
        }
        return result;
    }
    public static object StageJumps()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
        const string folder="Assets/AnimationStaging/SwimJumpFix20261011";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var result=new System.Collections.Generic.List<object>();
        foreach(string name in new[]{"PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006"})
        foreach(string variant in new[]{"Body","Feet"})
        {
            string source=$"Assets/Resources/Encounter/v006/{name}/{name}.fbx";
            string path=$"{folder}/{name}_{variant}.fbx";
            if(AssetDatabase.LoadMainAssetAtPath(path)==null && !AssetDatabase.CopyAsset(source,path))throw new Exception("Copy failed: "+path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);var clips=importer.clipAnimations;
            foreach(var clip in clips)
            {
                clip.lockRootHeightY=false;clip.keepOriginalPositionY=false;clip.heightFromFeet=variant=="Feet";
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
            result.Add(path);
        }
        return result;
    }
    public static object Jumps()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
        foreach(string name in new[]{"PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006"})
        {
            string path=$"Assets/Resources/Encounter/v006/{name}/{name}.fbx";
            string backup=Path.Combine(Backup,path+".meta");Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))File.Copy(path+".meta",backup);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);var clips=importer.clipAnimations;
            foreach(var clip in clips)
            {
                // The capsule supplies ballistic travel. Extract the source's
                // vertical root motion so retargeted feet do not float on landing.
                clip.lockRootHeightY=false;clip.keepOriginalPositionY=false;clip.heightFromFeet=true;
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
        return "Jump root height extracted relative to feet; clip timing and controller references retained.";
    }
    public static object RemoveTemporaryStages()
    {
        const string relative="Assets/AnimationStaging/SwimJumpFix20261011";
        string path=Path.GetFullPath(relative),root=Path.GetFullPath(Application.dataPath+"/AnimationStaging/");
        if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Unexpected staging path");
        var expected=new System.Collections.Generic.HashSet<string>();
        foreach(string name in new[]{"PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006"})
        foreach(string variant in new[]{"Body","Feet"})
        {expected.Add(name+"_"+variant+".fbx");expected.Add(name+"_"+variant+".fbx.meta");}
        if(Directory.GetDirectories(path).Length!=0 || Directory.GetFiles(path).Any(f=>!expected.Contains(Path.GetFileName(f))))throw new Exception("Staging contains other files");
        if(!AssetDatabase.DeleteAsset(relative))throw new Exception("Could not remove temporary staging copies");
        return "Removed only the six temporary jump import comparisons; active source FBXs preserved.";
    }
}
