using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
using Mavis;
public static class FinalizePressureV256
{
 public static object Restore()
 {
  if(Application.isPlaying)throw new Exception("Stop the native test before restoration");
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved changes");
  const string root=".codex/pressure-vfx-v256/";var state=JObject.Parse(File.ReadAllText(root+"workspace-before.json"));
  var scenes=state["scenes"].Select(x=>(string)x["path"]).ToArray();
  for(int i=0;i<scenes.Length;i++)EditorSceneManager.OpenScene(scenes[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
  var environment=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");if(environment.IsValid())SceneManager.SetActiveScene(environment);
  HealingPackSupply.RestoreCampaign((string)state["campaign"],(int)state["packs"]);
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");File.Copy(root+"human-save-before.json",save,true);
  var result=new {stopped=!Application.isPlaying,saveRestored=File.ReadAllBytes(save).SequenceEqual(File.ReadAllBytes(root+"human-save-before.json")),campaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new {path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(root+"final-editor.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
