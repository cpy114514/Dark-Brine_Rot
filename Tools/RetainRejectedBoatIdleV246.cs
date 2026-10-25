using System;
using UnityEditor;
using UnityEngine;
public static class RetainRejectedBoatIdleV246
{
 public static object Apply()
 {
  if(Application.isPlaying)throw new Exception("Edit Mode required");
  const string source="Assets/Resources/Encounter/v246/CH1_SahurStickIdle_v246";
  const string target="Assets/AnimationStaging/Rejected_SahurStickIdle_v246";
  if(AssetDatabase.IsValidFolder(source))
  {
   var error=AssetDatabase.MoveAsset(source,target);if(!string.IsNullOrEmpty(error))throw new Exception(error);
  }
  return new {preserved=target,missing=Story1WreckBattle.MissingRedesignClips()};
 }
}
