using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InstallBoatIdleV246
{
 public static object Apply()
 {
  if(Application.isPlaying)throw new Exception("Edit Mode required");
  const string stage="Assets/AnimationStaging/CH1_SahurStickIdle_v246";
  const string root="Assets/Resources/Encounter/v246";
  const string folder=root+"/CH1_SahurStickIdle_v246";
  if(!AssetDatabase.IsValidFolder(root))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v246");
  if(!AssetDatabase.IsValidFolder(folder))
  {
   var error=AssetDatabase.MoveAsset(stage,folder);if(!string.IsNullOrEmpty(error))throw new Exception(error);
  }
  var asset=folder+"/CH1_SahurStickIdle_v246.fbx";
  var importer=(ModelImporter)AssetImporter.GetAtPath(asset);
  var clip=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Single(x=>!x.name.StartsWith("__preview__"));
  if(!clip.isHumanMotion||!clip.isLooping||Mathf.Abs(clip.length-4.4f)>.001f)throw new Exception("Wrong clip contract");
  if(importer.sourceAvatar==null||!importer.sourceAvatar.isValid||!importer.sourceAvatar.isHuman)throw new Exception("Avatar invalid");
  var take=importer.clipAnimations.Single();
  if(!take.lockRootRotation||!take.lockRootHeightY||!take.lockRootPositionXZ)throw new Exception("Root must stay baked");
  var result=new{asset,clip.name,clip.length,clip.frameRate,clip.isHumanMotion,clip.isLooping,avatar=AssetDatabase.GetAssetPath(importer.sourceAvatar),take.firstFrame,take.lastFrame,take.lockRootRotation,take.lockRootHeightY,take.lockRootPositionXZ,curves=AnimationUtility.GetCurveBindings(clip).Length,missing=Story1WreckBattle.MissingRedesignClips()};
  File.WriteAllText(".codex/boat-fidget-20261024/import.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
