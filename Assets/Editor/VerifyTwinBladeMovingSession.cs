using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

[InitializeOnLoad]
public static class VerifyTwinBladeMovingSession
{
 const string Root=".codex/twin-blade-moving-attacks/";
 const string Pending="TwinBladeMoving.Session";
 static VerifyTwinBladeMovingSession(){EditorApplication.playModeStateChanged+=Changed;}
 public static object Prepare()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Stopped Editor required");
  var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve unsaved scenes");
  Directory.CreateDirectory(Root);Write("scenes-before.json",setup);
  Write("saves-before.json",Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p))));
  SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();return "Isolated verification Play queued";
 }
 static void Write(string name,object data)=>File.WriteAllText(Root+name,Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
 static void Changed(PlayModeStateChange state)
 {
  if(!SessionState.GetBool(Pending,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode)
  {
   typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Path.GetFullPath(Root+"isolated-save.json"));GameSaveManager.BeginEditorTestSession();
  }
  if(state==PlayModeStateChange.EnteredEditMode)
  {
   SessionState.SetBool(Pending,false);
   var saved=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Root+"saves-before.json"));
   var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"scenes-before.json"));
   Write("preservation.json",new{formalSavesUnchanged=saved.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value),scenesRestored=Newtonsoft.Json.JsonConvert.SerializeObject(setup)==Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup())});
  }
 }
}
