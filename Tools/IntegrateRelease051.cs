using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class IntegrateRelease051
{
 const string Root=".codex/release051";
 public static object Apply()
 {
  if(Application.isPlaying||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped idle Editor required");
  var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");
  var plan=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/merge-plan.json"));if(plan["conflicts"].Any())throw new Exception("Unresolved import conflicts");
  var paths=plan["apply"].Select(t=>(string)t).ToArray();
  foreach(var path in paths){if(File.Exists(path)&&File.ReadAllBytes(Root+"/candidate/"+path).SequenceEqual(File.ReadAllBytes(path)))continue;var before=Root+"/import-before/"+path;if(File.Exists(before)?!File.Exists(path)||!File.ReadAllBytes(before).SequenceEqual(File.ReadAllBytes(path)):File.Exists(path))throw new Exception("Preserve newer local file "+path);}
  if(!File.Exists(Root+"/editor-setup.json"))File.WriteAllText(Root+"/editor-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup,Newtonsoft.Json.Formatting.Indented));
  var saves=Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*");Directory.CreateDirectory(Root+"/save-before");foreach(var save in saves)if(!File.Exists(Root+"/save-before/"+Path.GetFileName(save)))File.Copy(save,Root+"/save-before/"+Path.GetFileName(save),false);
  AssetDatabase.DisallowAutoRefresh();AssetDatabase.StartAssetEditing();
  try{foreach(var path in paths){Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path)){var temp=path+".release051.tmp";File.Copy(Root+"/candidate/"+path,temp,true);File.Replace(temp,path,null);}else File.Copy(Root+"/candidate/"+path,path);}}
  finally{AssetDatabase.StopAssetEditing();AssetDatabase.AllowAutoRefresh();}
  foreach(var s in setup)if(paths.Contains(s.path))EditorSceneManager.OpenScene(s.path,OpenSceneMode.Single);
  AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
  return new{applied=paths.Length,backedUpSaves=saves.Length};
 }
}
