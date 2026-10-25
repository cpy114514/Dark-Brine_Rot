using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using Mavis;
public static class TestLoadingScreenV250
{
 public static object Start()
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
  var outPath=".codex/loading-bar-20261024/native";
  if(Directory.Exists(outPath))throw new Exception("Preserve previous take");Directory.CreateDirectory(outPath);
  var go=new GameObject("Temporary native loading screen verification");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<LoadingScreenReviewV250>().folder=outPath;
  EditorApplication.isPaused=false;return new{started=true,folder=outPath};
 }
}
public sealed class LoadingScreenReviewV250:MonoBehaviour
{
 public string folder;readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<object> rows=new List<object>();
 const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;int frame;bool done;Exception failure;
 void Check(string name,bool condition){checks.Add(name,condition);if(!condition)throw new Exception(name);}
 SceneLoadingScreen ScreenObject()=>UnityEngine.Object.FindFirstObjectByType<SceneLoadingScreen>();
 float Value(SceneLoadingScreen screen)=>(float)typeof(SceneLoadingScreen).GetField("progress",Flags).GetValue(screen);
 IEnumerator Start()
 {
  Time.timeScale=0;
  var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0)
  {
   object value=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){failure=e;}
   if(failure!=null)break;
   if(!next){stack.Pop();continue;}
   if(value is IEnumerator nested){stack.Push(nested);continue;}
   yield return value;
  }
  Finish();
 }
 IEnumerator Capture(string name)
 {
  yield return new WaitForEndOfFrame();
  var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());Destroy(image);
 }
 IEnumerator Run()
 {
  Check("Invalid scene does not create an overlay",!SceneLoadingScreen.Load("MissingLoadingTestScene")&&!SceneLoadingScreen.IsLoading);
  var create=typeof(SceneLoadingScreen).GetMethod("Create",BindingFlags.NonPublic|BindingFlags.Static);
  var screen=(SceneLoadingScreen)create.Invoke(null,null);var report=typeof(SceneLoadingScreen).GetMethod("Report",Flags);
  report.Invoke(screen,new object[]{.42f});
  var images=screen.GetComponentsInChildren<Image>();
  Check("Only black background and white fill images",images.Length==2&&images.Any(x=>x.color==Color.black)&&images.Any(x=>x.color==Color.white));
  Check("No labels or buttons added",screen.GetComponentsInChildren<TMPro.TMP_Text>().Length==0&&screen.GetComponentsInChildren<Selectable>().Length==0);
  Check("Overlay survives scene transitions",screen.gameObject.scene.name=="DontDestroyOnLoad");
  Check("Overlay draws above gameplay",screen.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay&&screen.GetComponent<Canvas>().sortingOrder==32760);
  var fill=screen.transform.Find("Progress line/White fill").GetComponent<RectTransform>();
  Check("White line follows progress",Mathf.Abs(fill.anchorMax.x-.42f)<.0001f);
  report.Invoke(screen,new object[]{.2f});Check("Displayed progress never retreats",Mathf.Abs(Value(screen)-.42f)<.0001f);
  var scaler=screen.GetComponent<CanvasScaler>();Check("Screen scaling configured",scaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize&&scaler.referenceResolution==new Vector2(1920,1080));
  yield return Capture("design-42-percent");
  typeof(SceneLoadingScreen).GetMethod("Close",Flags).Invoke(screen,null);yield return null;
  Check("Closed overlay releases singleton",!SceneLoadingScreen.IsLoading&&!ScreenObject());
  Check("Story load starts",SceneLoadingScreen.Load("Assets/Scenes/First story/Story1.unity"));
  Check("Duplicate scene requests rejected",!SceneLoadingScreen.Load("Assets/Scenes/First story/Story1.unity"));
  yield return Observe("story");
  Check("Story activated and overlay removed",SceneManager.GetActiveScene().name=="Story1"&&!ScreenObject());
  bool hasSave=GameSaveManager.HasSave;
  Check("Existing save queued without rewriting it",hasSave&&GameSaveManager.QueueContinue());
  Check("Continue world load starts",SceneLoadingScreen.Load("Assets/Scenes/First Island/Main.unity"));
  yield return Observe("continue");
  Check("All four world scenes loaded",new[]{"Environment","Gameplay","Lighting","Enemies"}.All(n=>SceneManager.GetSceneByName(n).isLoaded));
  Check("Overlay waits through additive loading and save restore",!AdditiveSceneBootstrap.IsLoading&&!GameSaveManager.IsContinuePending&&!SceneLoadingScreen.IsLoading&&!ScreenObject());
  var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var save=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/loading-bar-20261024/before-dark-brine-rot-save.json"));
  var p=save["playerPosition"];var position=new Vector3((float)p["x"],(float)p["y"],(float)p["z"]);
  Check("Continue restores the saved player location",player!=null&&Vector3.Distance(player.transform.position,position)<.15f);
 }
 IEnumerator Observe(string label)
 {
  float last=0;bool additive=false,pending=false,full=false,captured=false;double deadline=Time.realtimeSinceStartupAsDouble+180;
  while(SceneLoadingScreen.IsLoading)
  {
   if(Time.realtimeSinceStartupAsDouble>deadline)throw new Exception(label+" loading timeout");
   var screen=ScreenObject();float value=Value(screen);
   if(value+.00001f<last)throw new Exception("Progress moved backwards");last=value;
   additive|=AdditiveSceneBootstrap.IsLoading;pending|=GameSaveManager.IsContinuePending;full|=value>=1;
   rows.Add(new{frame,label,progress=value,world=AdditiveSceneBootstrap.IsLoading,worldProgress=AdditiveSceneBootstrap.Progress,restoring=GameSaveManager.IsContinuePending,scene=SceneManager.GetActiveScene().name});frame++;
   if(!captured&&value>=.1f&&value<1){captured=true;yield return Capture(label+"-loading");}
   yield return null;
  }
  yield return null;
  Check(label+" progress reaches completion before disappearing",full);
  if(label=="continue"){Check("Observed real additive loading",additive);Check("Observed pending continue restoration",pending);}
 }
 void Finish()
 {
  if(done)return;done=true;
  File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=failure==null,checks,error=failure?.ToString(),rows,scope="Existing Editor; actual single and additive scene transitions with saved-game restoration. Design capture is an explicit42% fixture, other traces use real operations. No source scene save or user preference changes."},Newtonsoft.Json.Formatting.Indented));
  Time.timeScale=1;EditorApplication.isPlaying=false;
 }
}
