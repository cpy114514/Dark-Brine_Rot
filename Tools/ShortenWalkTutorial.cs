using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class ShortenWalkTutorial {
 const string PathName="Assets/Scripts/Story/Chapter1/Story1WreckBattle.Redesign.cs";
 const string Old="[SerializeField, Range(4f,20f)] float walkingTutorialSeconds=8f;";
 const string New="[SerializeField, Range(4f,20f)] float walkingTutorialSeconds=4f;";
 static void Apply(PlayModeStateChange change){
  if(change!=PlayModeStateChange.EnteredEditMode)return;
  try{var source=File.ReadAllText(PathName);if(!source.Contains(Old))throw new Exception("Tutorial source changed; preserve it and inspect manually");File.WriteAllText(PathName,source.Replace(Old,New));Debug.Log("Tutorial walking duration updated to four seconds.");}
  finally{EditorApplication.playModeStateChanged-=Apply;}
 }
 public static object Run(){
  string source=File.ReadAllText(PathName);if(!source.Contains(Old))throw new Exception("Expected original value not found");
  var field=typeof(Story1WreckBattle).GetField("walkingTutorialSeconds",BindingFlags.Instance|BindingFlags.NonPublic);
  var battles=UnityEngine.Object.FindObjectsByType<Story1WreckBattle>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  foreach(var battle in battles)field.SetValue(battle,4f);
  if(EditorApplication.isPlaying){EditorApplication.playModeStateChanged-=Apply;EditorApplication.playModeStateChanged+=Apply;return new{liveInstances=battles.Length,seconds=4,sourceUpdate="scheduled when Play exits",preservesCurrentSession=true};}
  Apply(PlayModeStateChange.EnteredEditMode);return new{liveInstances=battles.Length,seconds=4,sourceUpdate="applied"};
 }
}
