using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class PreparePressureV256
{
 public static object StopForCompile()
 {
  const string folder=".codex/pressure-vfx-v256";Directory.CreateDirectory(folder);
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
  if(File.Exists(folder+"/workspace-before.json"))throw new Exception("Preserve earlier snapshot");
  if(File.Exists(save))File.Copy(save,folder+"/disk-save-before.json");
  bool saved=Application.isPlaying&&GameSaveManager.SaveCurrentGame();
  if(File.Exists(save))File.Copy(save,folder+"/human-save-before.json");
  var snapshot=new {playing=Application.isPlaying,saved,campaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new {path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(folder+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(snapshot,Newtonsoft.Json.Formatting.Indented));
  EditorApplication.isPlaying=false;return snapshot;
 }
}
