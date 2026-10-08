using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InstallV011
{
 public static object Install()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  const string folder="Assets/Resources/Encounter/v011",path="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
  if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v011");
  if(AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(folder+"/PreviousEncounterDefinition.asset")==null)AssetDatabase.CopyAsset(path,folder+"/PreviousEncounterDefinition.asset");
  var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);var previous=definition.beats;
  var revised=Story1EncounterDefinition.ApprovedBeats();
  for(int i=0;i<revised.Length;i++)
  {
   var matching=previous.FirstOrDefault(b=>b.action==revised[i].action && b.label==revised[i].label);
   if(string.IsNullOrEmpty(matching.clip))matching=previous.FirstOrDefault(b=>b.action==revised[i].action);
   if(!string.IsNullOrEmpty(matching.clip))revised[i].clip=matching.clip;
  }
  definition.beats=revised;EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
  return new{actualContactGatesReactions=true,reversalStaggerStart=14.65f,recoveryStart=15.4f,pairedDuration=definition.duration};
 }
}
