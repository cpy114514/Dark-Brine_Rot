using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class ReviewTailV258
{
 const string Root=".codex/tail-fix-v258";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static object Open()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve open work");
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
  EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Replay(string label)
 {
  string output=Root+"/"+label+".json";if(File.Exists(output))throw new Exception("Preserve previous replay");
  new GameObject("Temporary tail replay").AddComponent<OpeningTailReview>().output=output;EditorApplication.isPaused=false;return new{started=true};
 }
 public static object RestoreScenes()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve open work");
  var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/inspect.json"));
  var paths=state["scenes"].Select(s=>(string)s["path"]).ToArray();
  for(int i=0;i<paths.Length;i++)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(paths[i],i==0?UnityEditor.SceneManagement.OpenSceneMode.Single:UnityEditor.SceneManagement.OpenSceneMode.Additive);
  return new{restored=paths};
 }
 public static object Inspect()
 {
  Directory.CreateDirectory(Root);
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
  object Field(string n)=>b?typeof(Story1WreckBattle).GetField(n,F)?.GetValue(b):null;
  var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();
  var skin=shark?shark.VisualRenderer as SkinnedMeshRenderer:null;
  var r=new{playing=Application.isPlaying,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).path,SceneManager.GetSceneAt(i).isDirty}).ToArray(),stage=b?b.RedesignStage.ToString():null,t=Field("redesignTime"),tailHit=Field("redTailHit"),gap=b?b.RedesignContactGap:0,heroGap=b?b.RedesignTutorialHeroGap:0,tailTriangles=(Field("redTailTriangles") as int[])?.Length,mesh=skin?new{skin.sharedMesh.name,skin.sharedMesh.isReadable,skin.sharedMesh.vertexCount,path=AssetDatabase.GetAssetPath(skin.sharedMesh)}:null};
  File.WriteAllText(Root+"/inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(r,Newtonsoft.Json.Formatting.Indented));return r;
 }
 public static object PreserveAndStop()
 {
  Directory.CreateDirectory(Root);Inspect();
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
  if(File.Exists(save))File.Copy(save,Root+"/human-save-before.json",false);
  var health=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();
  if(Application.isPlaying&&health)GameSaveManager.SaveCurrentGame();
  if(File.Exists(save))File.Copy(save,Root+"/human-save-preserved.json",false);
  EditorApplication.isPlaying=false;return new{stopping=true,save};
 }
}
