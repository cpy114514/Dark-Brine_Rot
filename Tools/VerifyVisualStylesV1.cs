using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;

public static class VerifyVisualStylesV1
{
 const string Root=".codex/visual-style-v1";
 static readonly List<string> checks=new List<string>();
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string description){if(!ok)throw new Exception(description);checks.Add(description);}
 static async Task Frame(){int n=Time.frameCount;var deadline=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount==n){if(!Application.isPlaying||DateTime.UtcNow>deadline)throw new Exception("Frame wait interrupted");await Task.Delay(5);}}
 public static object Start(){if(!Application.isPlaying)throw new Exception("Test Play required");_=Run();return new{started=true};}
 static object Field(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
 static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
 static async Task WaitScene(AsyncOperation op){while(!op.isDone)await Frame();await Frame();}
 static async Task<object> Profile(string style)
 {
  VisualStyleManager.Instance.Select(style,false);for(int i=0;i<12;i++)await Frame();
  var frameTimes=new List<double>();var cpu=new List<double>();var gpu=new List<double>();var timings=new FrameTiming[1];
  for(int i=0;i<60;i++){FrameTimingManager.CaptureFrameTimings();await Frame();frameTimes.Add(Time.unscaledDeltaTime*1000);if(FrameTimingManager.GetLatestTimings(1,timings)>0){if(timings[0].cpuFrameTime>0)cpu.Add(timings[0].cpuFrameTime);if(timings[0].gpuFrameTime>0)gpu.Add(timings[0].gpuFrameTime);}}
  double Median(List<double> list){if(list.Count==0)return -1;list.Sort();return list[list.Count/2];}
  double ms=Median(frameTimes);return new{style,samples=frameTimes.Count,editorFrameMs=ms,editorFps=1000/ms,cpuFrameMs=Median(cpu),gpuFrameMs=Median(gpu),gpuAvailable=gpu.Count>0,width=Screen.width,height=Screen.height,renderScale=(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).renderScale,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),context="Live CombatTest, six existing Chicken Bro prefabs; Editor pacing is not a standalone benchmark"};
 }
 static Task SnapshotUI(VisualStyleUIController ui,string path)=>SnapshotCanvas(ui.GetComponentInChildren<Canvas>(true),path);
 static async Task SnapshotCanvas(Canvas canvas,string path)
 {
  // A temporary camera-space canvas renders the actual built UI into a native-size capture; restore its overlay mode immediately.
  var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
  var go=new GameObject("Temporary UI verification capture",typeof(Camera));var cam=go.GetComponent<Camera>();cam.cullingMask=1<<5;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
  var target=new RenderTexture(1600,900,24);target.Create();cam.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
  var layers=canvas.GetComponentsInChildren<Graphic>(true).Select(g=>g.gameObject).Append(canvas.gameObject).Distinct().Select(go=>(go,go.layer)).ToArray();foreach(var layer in layers)layer.go.layer=5;
  bool done=false;Action<ScriptableRenderContext,Camera> cb=(context,camera)=>{if(camera!=cam||done)return;done=true;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var rt=RenderTexture.active;try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{RenderTexture.active=rt;UnityEngine.Object.DestroyImmediate(image);}};
  RenderPipelineManager.endCameraRendering+=cb;
  try{for(int i=0;i<60&&!done;i++)await Frame();Check(done,"Native UI capture "+Path.GetFileName(path));}
  finally{RenderPipelineManager.endCameraRendering-=cb;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;foreach(var layer in layers)if(layer.go)layer.go.layer=layer.layer;UnityEngine.Object.DestroyImmediate(go);target.Release();UnityEngine.Object.DestroyImmediate(target);}
 }
 static async Task Run()
 {
  checks.Clear();bool passed=false;Scene menuScene=default;var reports=new List<object>();
  bool hasStyle=PlayerPrefs.HasKey(VisualStyleSettings.StyleKey),hasIntensity=PlayerPrefs.HasKey(VisualStyleSettings.IntensityKey);
  string savedStyle=VisualStyleSettings.SelectedId;float savedIntensity=PlayerPrefs.GetFloat(VisualStyleSettings.IntensityKey,.85f);
  var manager=VisualStyleManager.Instance;string originalStyle=manager.Current.id;float originalIntensity=manager.Intensity;
  bool wasPixel=Pixel360Rendering.IsActive;var originalPipeline=QualitySettings.renderPipeline;var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  float time=Time.timeScale;VisualStyleUIController ui=null;
  try
  {
   Check(manager.Presets.Count==2,"Two resource presets load without a scene dependency");
   var materials=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).ToDictionary(r=>r,r=>r.sharedMaterials.Select(m=>m?m.GetInstanceID():0).ToArray());
   var actors=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None).Select(p=>(p,p.transform.position,p.transform.rotation)).ToArray();
   for(int i=0;i<20;i++)Check(manager.Select(i%2==0?"dark-fairytale":"realistic",false),"Switch without scene reload "+i);
   Check(materials.All(pair=>pair.Key&&pair.Value.SequenceEqual(pair.Key.sharedMaterials.Select(m=>m?m.GetInstanceID():0))),"Switches preserve all scene material references");
   Check(actors.All(a=>a.p&&a.p.transform.position==a.position&&a.p.transform.rotation==a.rotation),"Same-frame switches do not move gameplay actors");
   Check(!manager.Select("unknown-style",false),"Unknown style is rejected without changing a valid preset");
   manager.Select("dark-fairytale");manager.SetIntensity(.7f);manager.Select("realistic",false);manager.RestorePreference();
   Check(manager.Current.id=="dark-fairytale"&&Mathf.Abs(manager.Intensity-.7f)<.001f,"Saved style and intensity restore from the single preference store");
   manager.SetIntensity(-10,false);Check(manager.Intensity==.4f,"Intensity lower bound");manager.SetIntensity(10,false);Check(manager.Intensity==1,"Intensity upper bound");
   foreach(var preset in manager.Presets)Check(preset.preview&&preset.preview.width==1600&&preset.preview.height==900,preset.displayName+" has a real matching scene capture");
   await WaitScene(SceneManager.LoadSceneAsync("Assets/Scenes/Main Menu/MainMenu.unity",LoadSceneMode.Additive));menuScene=SceneManager.GetSceneByName("MainMenu");
   var menu=UnityEngine.Object.FindObjectsByType<MainMenuController>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==menuScene);
   var entry=menu.transform.Find("Visual Style");Check(entry&&entry.GetComponent<Button>(),"Existing Main Menu gains Visual Style without replacing original controls");
   Check(entry.GetComponentInChildren<TMPro.TMP_Text>().text=="VISUAL STYLE","New Main Menu entry retains its English label");
   Check(menu.transform.Find("New Game")&&menu.transform.Find("Continue")&&menu.transform.Find("Settings")&&menu.transform.Find("Quit"),"All original Main Menu entries remain");
   await SnapshotCanvas(menu.GetComponent<Canvas>(),Root+"/main-menu.png");
   entry.GetComponent<Button>().onClick.Invoke();await Frame();ui=UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>();
   Check(VisualStyleUIController.IsOpen&&ui.StyleButtons.Count==2,"Main Menu opens the two-card gallery");
   Check(ui.GetComponentInChildren<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"Gallery and text remain native overlay UI");
   var hover=ui.GetComponentsInChildren<VisualStyleCardHover>().First(h=>h.name!=manager.Current.id);var card=hover.GetComponent<Image>();var color=card.color;hover.OnPointerEnter(new PointerEventData(EventSystem.current));Check(card.color!=color,"Full card has hover feedback");hover.OnPointerExit(new PointerEventData(EventSystem.current));
   ui.StyleButtons[0].onClick.Invoke();Check(manager.Current.id=="realistic","Realistic card selects the shared manager");
   ui.StyleButtons[1].onClick.Invoke();ui.IntensitySlider.value=.65f;Check(manager.Current.id=="dark-fairytale"&&Mathf.Abs(manager.Intensity-.65f)<.001f,"Dark card and intensity update the same saved configuration");
   await SnapshotUI(ui,Root+"/gallery.png");
   int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;ui.ShowComparison(true);Canvas.ForceUpdateCanvases();await Frame();
   var comparison=ui.Comparison;var rect=comparison.imageArea;
   foreach(float value in new[]{.2f,.8f})
   {
    var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3((value-.5f)*rect.rect.width,0,0))),button=PointerEventData.InputButton.Left};
    ExecuteEvents.Execute(comparison.divider.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(comparison.divider.gameObject,pointer,ExecuteEvents.dragHandler);
    Check(Mathf.Abs(comparison.Split-value)<.03f,"Comparison pointer drag reaches "+value);
    Check(Mathf.Abs(comparison.leftMask.anchorMax.x-comparison.Split)<.001f&&Mathf.Abs(comparison.leftImage.rect.width-rect.rect.width)<1,"Comparison reveals aligned images without stretching");
   }
   Canvas.ForceUpdateCanvases();Check(Mathf.Abs(comparison.divider.handleRect.rect.height-rect.rect.height)<1,"Divider stays within image height");
   Check(cameras==UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length,"Comparison adds no real-time preview cameras");
   comparison.Split=.5f;await SnapshotUI(ui,Root+"/comparison-ui.png");ui.Close();
   menu.Settings();await Frame();var settings=menu.settingsMenu;settings.sectionTabs[4].onClick.Invoke();
   Check(settings.options.Count(o=>o.key=="visualStyle")==1&&settings.options.Count(o=>o.key=="pixel360")==1,"One visual-style row and one existing pixel-mode row");
   var option=settings.options.Single(o=>o.key=="visualStyle");option.button.onClick.Invoke();await Frame();
   Check(VisualStyleUIController.IsOpen,"Graphics opens the same gallery");ui.StyleButtons[0].onClick.Invoke();Check(option.value.text=="Realistic","Graphics value synchronizes immediately with gallery selection");
   ui.StyleButtons[1].onClick.Invoke();Check(option.value.text=="Dark Fairytale","Graphics shares Dark Fairytale selection");ui.Close();
   settings.Resume();await Frame();await WaitScene(SceneManager.UnloadSceneAsync(menuScene));menuScene=default;
   var active=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   foreach(bool pixel in new[]{false,true})
   {
    Pixel360Rendering.Configure(active,pixel);
    foreach(string style in new[]{"realistic","dark-fairytale"}){manager.Select(style,false);await Frame();Check(manager.Current.id==style&&Pixel360Rendering.IsActive==pixel,style+" combines independently with pixel="+pixel);}
   }
   Pixel360Rendering.ReleaseCurrent();
   var arena=UnityEngine.Object.FindFirstObjectByType<CombatTestArena>();Check(arena&&arena.player,"Existing live combat arena available");
   var health=arena.player.GetComponent<PlayerHealth>();if(health)health.GrantProtection(30);
   Call(arena,"SpawnChickens",6);await Frame();Check(UnityEngine.Object.FindObjectsByType<ChickenBroAI>(FindObjectsSortMode.None).Length>=6,"Six existing Chicken Bros remain active for both style profiles");
   Time.timeScale=1;reports.Add(await Profile("realistic"));reports.Add(await Profile("dark-fairytale"));
   Check(arena.player.enabled&&arena.player.GetComponent<SahurAttack>().enabled,"Player movement and combat components remain enabled after switching");
   Call(arena,"ClearSpawned");passed=true;
  }
  catch(Exception error){File.WriteAllText(Root+"/verification-error.txt",error.ToString());}
  finally
  {
   if(ui&&VisualStyleUIController.IsOpen)ui.Close();if(menuScene.IsValid()&&menuScene.isLoaded)SceneManager.UnloadSceneAsync(menuScene);
   Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=originalPipeline;if(wasPixel&&pipeline)Pixel360Rendering.Configure(pipeline,true);
   if(hasStyle)PlayerPrefs.SetString(VisualStyleSettings.StyleKey,savedStyle);else PlayerPrefs.DeleteKey(VisualStyleSettings.StyleKey);
   if(hasIntensity)PlayerPrefs.SetFloat(VisualStyleSettings.IntensityKey,savedIntensity);else PlayerPrefs.DeleteKey(VisualStyleSettings.IntensityKey);PlayerPrefs.Save();
   manager.Select(originalStyle,false);manager.SetIntensity(originalIntensity,false);Time.timeScale=time;
   File.WriteAllText(Root+"/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,profiles=reports},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;
  }
 }
}
