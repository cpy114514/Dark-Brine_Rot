using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PrepareCombatV265 {
 public static object Open(){
 if(Application.isPlaying)throw new Exception("Stop first");
 const string r=".codex/combat-v265";string p=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 if(!File.Exists(r+"/human-save.json")&&File.Exists(p))File.Copy(p,r+"/human-save.json");
 if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve scenes");
 EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;return new{starting=true};
 }
}
