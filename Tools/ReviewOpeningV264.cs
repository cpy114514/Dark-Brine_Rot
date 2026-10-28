using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ReviewOpeningV264 {
 const string Root=".codex/opening-v264";
 static string Save=>Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 public static object Record(){
 if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve current Editor work");
 var paths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
 if(!File.Exists(Root+"/scenes.json"))File.WriteAllText(Root+"/scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(paths));
 if(File.Exists(Save)&&!File.Exists(Root+"/save.json"))File.Copy(Save,Root+"/save.json");
 File.WriteAllText("Temp/opening-review-request.txt",Root+"/visible/review.json");File.WriteAllText("Temp/opening-review-helm.flag","true");
 EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Restore(){
 if(Application.isPlaying)throw new Exception("Stop Play first");
 var paths=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Root+"/scenes.json"));
 for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
 bool same=!File.Exists(Root+"/save.json")||File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(Root+"/save.json"));
 if(!same){File.Copy(Save,Root+"/after-test-save.json",true);File.Copy(Root+"/save.json",Save,true);}
 return new{restored=paths,saveRestored=!same,savePreserved=File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(Root+"/save.json"))};
 }
}



