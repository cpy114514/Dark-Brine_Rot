using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Normalize only the two measured legacy clips. Original FBXs/import settings stay intact.
public static class InstallEncounterDirectionV241
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stop the existing Editor before importing revisions.");
  const string root="Assets/Resources/Encounter/v241";
  if(!AssetDatabase.IsValidFolder(root))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v241");
  var reports=new System.Collections.Generic.List<object>();
  foreach(var pair in new[]{new[]{"PLAYER_Walk_v006","PLAYER_Walk_v241"},new[]{"PLAYER_Climb_v010","PLAYER_Climb_v241"}})
  {
   var original=Story1EncounterClipCache.Find(pair[0]);if(!original)throw new Exception("Missing source: "+pair[0]);
   string source=AssetDatabase.GetAssetPath(original),folder=root+"/"+pair[1],destination=folder+"/"+pair[1]+".fbx";
   if(AssetDatabase.LoadAssetAtPath<GameObject>(destination))throw new Exception("Keep existing revision: "+destination);
   AssetDatabase.CreateFolder(root,pair[1]);if(!AssetDatabase.CopyAsset(source,destination))throw new Exception("Copy failed");
   var importer=(ModelImporter)AssetImporter.GetAtPath(destination);var takes=importer.clipAnimations;
   if(takes.Length!=1)throw new Exception("Expected one retained complete action");
   float previous=takes[0].rotationOffset;takes[0].rotationOffset=previous+180;takes[0].name=pair[1];importer.clipAnimations=takes;importer.SaveAndReimport();
   var clip=AssetDatabase.LoadAllAssetsAtPath(destination).OfType<AnimationClip>().Single(c=>c.name==pair[1]);
   if(Mathf.Abs(clip.length-original.length)>.001f||clip.frameRate!=original.frameRate||clip.isLooping!=original.isLooping)throw new Exception("Retained timing changed");
   reports.Add(new{source,destination,original=original.name,revision=clip.name,previousRotationOffset=previous,rotationOffset=takes[0].rotationOffset,clip.length,clip.frameRate,clip.isLooping});
  }
  Directory.CreateDirectory(".codex/shark-direction-20261023");File.WriteAllText(".codex/shark-direction-20261023/import-revisions.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
  return reports;
 }
}
