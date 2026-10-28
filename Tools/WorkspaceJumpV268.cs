using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class WorkspaceJumpV268 {
 const string R=".codex/jump-meshy-v268";
 static string Save=>Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 public static object ProtectPlay(){
 Directory.CreateDirectory(R);
 if(File.Exists(R+"/save-after-stop.json"))throw new Exception("Existing test backup: inspect first");
 if(File.Exists(Save))File.Copy(Save,R+"/save-before-save.json",false);
 bool saved=Application.isPlaying&&Mavis.GameSaveManager.SaveCurrentGame();
 if(File.Exists(Save))File.Copy(Save,R+"/save-after-save.json",false);
 if(Application.isPlaying)EditorApplication.isPlaying=false;
 return new{saved,stopping=true};
 }
 public static object Backup(){
 if(Application.isPlaying)throw new Exception("Stop first");
 Directory.CreateDirectory(R);
 var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
 if(scenes.Any(s=>s.isDirty))throw new Exception("Preserve unsaved scenes");
 File.WriteAllText(R+"/scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));
 if(File.Exists(Save))File.Copy(Save,R+"/save-after-stop.json",false);
 foreach(var path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})File.Copy(path,R+"/"+Path.GetFileName(path),false);
 return new{scenes=scenes.Select(s=>s.path).ToArray()};
 }
 public static object Open(){if(Application.isPlaying)throw new Exception("Stop first");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;return new{starting=true};}
 public static object Restore(){
 if(Application.isPlaying)throw new Exception("Stop test first");
 var paths=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(R+"/scenes.json"));
 if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty&&!string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)))throw new Exception("New human scene edits must be retained");
 for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
 bool same=!File.Exists(R+"/save-after-stop.json")||File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(R+"/save-after-stop.json"));
 if(!same){File.Copy(Save,R+"/test-save.json",true);File.Copy(R+"/save-after-stop.json",Save,true);}
 EditorApplication.isPaused=false;
 var result=new{scenes=paths,saveWasUnchanged=same,savePreserved=true};File.WriteAllText(R+"/preservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
 }
}
