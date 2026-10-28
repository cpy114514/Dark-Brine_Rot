using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class WorkspaceJumpV266 {
 const string R=".codex/jump-diagonal-v266";
 static string Save=>Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 public static object Backup(){
 if(Application.isPlaying)throw new Exception("Protect live play session first");
 var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
 if(scenes.Any(s=>s.isDirty))throw new Exception("Preserve unsaved scene work");
 File.WriteAllText(R+"/scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));if(File.Exists(Save))File.Copy(Save,R+"/save.json",false);
 foreach(var path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})File.Copy(path,R+"/"+Path.GetFileName(path),false);
 return new{scenes=scenes.Select(s=>s.path).ToArray()};
 }
 public static object Open(){if(Application.isPlaying)throw new Exception("Stop first");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;return new{starting=true};}
 public static object Restore(){
 if(Application.isPlaying)throw new Exception("Stop review first");
 var paths=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(R+"/scenes.json"));for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
 bool same=!File.Exists(R+"/save.json")||File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(R+"/save.json"));if(!same){File.Copy(Save,R+"/test-save.json",true);File.Copy(R+"/save.json",Save,true);}
 var result=new{scenes=paths,saveWasUnchanged=same};File.WriteAllText(R+"/preservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
 }
}
