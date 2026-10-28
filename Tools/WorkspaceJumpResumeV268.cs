using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class WorkspaceJumpResumeV268 {
 const string R=".codex/jump-meshy-v268/resume";
 static string Save=>Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 public static object Open(){
  if(Application.isPlaying)throw new Exception("Stop first");
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  if(scenes.Any(s=>s.isDirty))throw new Exception("Preserve unsaved scenes");
  Directory.CreateDirectory(R);
  if(File.Exists(R+"/scenes.json"))throw new Exception("Preserve existing resume snapshot");
  File.WriteAllText(R+"/scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));
  if(File.Exists(Save))File.Copy(Save,R+"/save.json",false);
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  SessionState.SetBool("CombatCombinationTests",false);
  EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Restore(){
  if(Application.isPlaying)throw new Exception("Stop test first");
  if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty&&!string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)))throw new Exception("Retain new human scene edits");
  var paths=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(".codex/jump-meshy-v268/scenes.json"));
  for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
  bool same=!File.Exists(R+"/save.json")||File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(R+"/save.json"));
  if(!same){File.Copy(Save,R+"/test-save.json",true);File.Copy(R+"/save.json",Save,true);}
  EditorApplication.isPaused=false;
  var result=new{scenes=paths,saveWasUnchanged=same,savePreserved=true};
  File.WriteAllText(R+"/preservation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
 }
}
