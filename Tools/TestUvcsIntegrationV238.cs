using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class TestUvcsIntegrationV238
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Edit Mode only.");
  var original=SceneManager.GetActiveScene();bool dirty=original.isDirty;
  const string path="Assets/Scenes/First Island/Environment.unity";
  var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
  var results=new List<object>();
  try {
   if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   foreach(var test in new[]{"ExperienceRegression","PacingExplorationRegression","PriorityFixRegression"})
   {
    try {
     Type type=null;foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){type=assembly.GetType(test);if(type!=null)break;}
     if(type==null)throw new Exception("Missing regression: "+test);
     var output=type.GetMethod("Run").Invoke(null,null);
     results.Add(new{test,passed=true,output});
    }catch(Exception e){results.Add(new{test,passed=false,error=(e.InnerException??e).ToString()});}
   }
  } finally {
   SceneManager.SetActiveScene(original);
   if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
   if(original.isDirty!=dirty)throw new Exception("Original scene dirty state changed.");
  }
  var json=Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented);
  File.WriteAllText(".codex/uvcs-repair-20261022/integration-tests.json",json);return results;
 }
}
