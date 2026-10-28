using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class RestoreCombatV265 {
 public static object Run(){
 if(Application.isPlaying)throw new Exception("Stop test Play first");
 const string root=".codex/combat-v265";var before=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(root+"/workspace-before.json"));
 string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 bool same=File.ReadAllBytes(save).SequenceEqual(File.ReadAllBytes(root+"/human-save.json"));
 if(!same){File.Copy(save,root+"/test-save-backup.json",true);File.Copy(root+"/human-save.json",save,true);}
 var paths=before["scenes"].Select(s=>(string)s["path"]).ToArray();
 if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty&& !string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)))throw new Exception("Preserve new unsaved scene work");
 for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
 EditorApplication.isPaused=false;
 var result=new{scenes=paths,saveWasUnchanged=same,savePreserved=File.ReadAllBytes(save).SequenceEqual(File.ReadAllBytes(root+"/human-save.json")),stopped=!Application.isPlaying};
 File.WriteAllText(root+"/preservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
