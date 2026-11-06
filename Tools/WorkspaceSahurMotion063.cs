using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class WorkspaceSahur063
{
 const string Dir=".codex/sahur-polish063";
 public static object Protect()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stopped editor required");
  if(File.Exists(Dir+"/workspace.json"))throw new Exception("Preserve earlier snapshot");
  Directory.CreateDirectory(Dir+"/save-before");
  foreach(var p in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save*"))File.Copy(p,Dir+"/save-before/"+Path.GetFileName(p));
  string campaign=PlayerPrefs.GetString("DarkBrine.HealingPacks.Campaign","legacy"),key="DarkBrine.HealingPacks.Remaining."+campaign;
  var state=new{saveDirectory=Application.persistentDataPath,scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup(),campaign,campaignExists=PlayerPrefs.HasKey("DarkBrine.HealingPacks.Campaign"),count=PlayerPrefs.GetInt(key,3),countExists=PlayerPrefs.HasKey(key),cursor=Cursor.lockState.ToString(),visible=Cursor.visible};
  File.WriteAllText(Dir+"/workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(state,Newtonsoft.Json.Formatting.Indented));
  return "Protected human save, scene setup and healing preferences";
 }
 public static object Restore()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop editor first");
  if(File.Exists(Dir+"/restored.json"))throw new Exception("Snapshot already restored; do not replay an old save snapshot");
  var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Dir+"/workspace.json"));string dir=(string)state["saveDirectory"];
  var original=Directory.GetFiles(Dir+"/save-before");foreach(var p in Directory.GetFiles(dir,"dark-brine-rot-save*"))if(!original.Any(o=>Path.GetFileName(o)==Path.GetFileName(p)))File.Delete(p);
  foreach(var p in original)File.Copy(p,Path.Combine(dir,Path.GetFileName(p)),true);
  string campaign=(string)state["campaign"],key="DarkBrine.HealingPacks.Remaining."+campaign;
  if((bool)state["campaignExists"])PlayerPrefs.SetString("DarkBrine.HealingPacks.Campaign",campaign);else PlayerPrefs.DeleteKey("DarkBrine.HealingPacks.Campaign");
  if((bool)state["countExists"])PlayerPrefs.SetInt(key,(int)state["count"]);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
  var current=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var before=state["scenes"].ToObject<UnityEditor.SceneManagement.SceneSetup[]>();
  if(!current.Select(x=>x.path).SequenceEqual(before.Select(x=>x.path)))throw new Exception("Scene layout changed");
  var result=new{restored=true,saveBytesVerified=original.All(p=>File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(dir,Path.GetFileName(p))))),scenesUnchanged=true,inputs=UnityEngine.InputSystem.InputSystem.devices.Select(d=>new{d.name,d.enabled})};
  File.WriteAllText(Dir+"/restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
