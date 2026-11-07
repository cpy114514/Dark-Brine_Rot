using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
public static class PreserveAnimationNonRifle
{
 const string W=".codex/animation-nonrifle";
 public static object Prepare(){
  if(Application.isPlaying)throw new Exception("Stop play first");
  Directory.CreateDirectory(W);var setup=EditorSceneManager.GetSceneManagerSetup();
  if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes first");
  if(!File.Exists(W+"/workspace.json"))File.WriteAllText(W+"/workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));
  Directory.CreateDirectory(W+"/save-before");foreach(var f in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*"))if(!File.Exists(W+"/save-before/"+Path.GetFileName(f)))File.Copy(f,W+"/save-before/"+Path.GetFileName(f));
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);return "Disposable native VFX review scene";
 }
 public static object Restore(){
  if(Application.isPlaying)throw new Exception("Stop test first");
  foreach(var f in Directory.GetFiles(W+"/save-before"))if(!File.ReadAllBytes(f).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(f)))))throw new Exception("Player save changed");
  EditorApplication.isPaused=false;EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"/workspace.json")));return new{restored=true,savesUnchanged=true};
 }
 public static object PrepareIsland(){
  if(Application.isPlaying)throw new Exception("Stop test first");
  if(EditorSceneManager.GetSceneManagerSetup().Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes first");
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
  return "Isolated island environment and lighting; no authored gameplay or enemies loaded";
 }
}

