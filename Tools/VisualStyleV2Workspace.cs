using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using Mavis.VisualStyles;
public static class VisualStyleV2Workspace
{
 const string Root=".codex/visual-style-v2";
 public static object Prepare()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Stopped clean workspace required");
  if(File.Exists(Root+"/verification-running"))throw new Exception("Verification already active");
  Directory.CreateDirectory(Root);
  File.WriteAllText(Root+"/verification-workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
  var prefs=new {hasStyle=PlayerPrefs.HasKey(VisualStyleSettings.StyleKey),hasIntensity=PlayerPrefs.HasKey(VisualStyleSettings.IntensityKey),style=VisualStyleSettings.SelectedId,intensity=PlayerPrefs.GetFloat(VisualStyleSettings.IntensityKey,.85f)};
  File.WriteAllText(Root+"/preferences-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(prefs));
  VisualStyleSettings.Save("origami",.72f);
  EditorSceneManager.OpenScene("Assets/Scenes/Combat Test/CombatTest.unity",OpenSceneMode.Single);
  return new {prepared=true,scene=SceneManager.GetActiveScene().path,saveIsolation="GameSaveManager accepts only Gameplay; CombatTestArena also adds GameSaveExcluded"};
 }
 public static object Restore()
 {
  if(Application.isPlaying)throw new Exception("Stop test Play first");
  var p=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/preferences-before.json"));
  if((bool)p["hasStyle"])PlayerPrefs.SetString(VisualStyleSettings.StyleKey,(string)p["style"]);else PlayerPrefs.DeleteKey(VisualStyleSettings.StyleKey);
  if((bool)p["hasIntensity"])PlayerPrefs.SetFloat(VisualStyleSettings.IntensityKey,(float)p["intensity"]);else PlayerPrefs.DeleteKey(VisualStyleSettings.IntensityKey);PlayerPrefs.Save();
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/verification-workspace-before.json")));
  return new{restored=true};
 }
}
