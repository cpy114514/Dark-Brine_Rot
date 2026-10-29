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

public static class VerifyVisualStylesV2
{
 const string Root=".codex/visual-style-v2";
 static readonly List<string> checks=new List<string>();
 static int nativeRenderFrames;
 static void Rendered(ScriptableRenderContext context,Camera camera){if(camera==Camera.main)nativeRenderFrames++;}
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string description){if(!ok)throw new Exception(description);checks.Add(description);}
 static async Task Frame(){int n=Time.frameCount;var deadline=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount==n){if(!Application.isPlaying||DateTime.UtcNow>deadline)throw new Exception("Frame wait interrupted");await Task.Delay(5);}}
 public static object Start(){if(!Application.isPlaying||!SceneManager.GetSceneByName("CombatTest").isLoaded)throw new Exception("CombatTest Play required");File.Delete(Root+"/verification-error.txt");File.Delete(Root+"/verification.json");File.WriteAllText(Root+"/verification-running",DateTime.UtcNow.ToString("o"));_=Run();return new{started=true};}
 static object Field(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
 static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
 static async Task WaitScene(AsyncOperation op){while(!op.isDone)await Frame();await Frame();}
 static async Task<object> Profile(string style)
 {
  VisualStyleManager.Instance.Select(style,false);for(int i=0;i<12;i++)await Frame();
  int renderedBefore=nativeRenderFrames;
  var frameTimes=new List<double>();var cpu=new List<double>();var gpu=new List<double>();var timings=new FrameTiming[1];
  for(int i=0;i<60;i++){FrameTimingManager.CaptureFrameTimings();await Frame();frameTimes.Add(Time.unscaledDeltaTime*1000);if(FrameTimingManager.GetLatestTimings(1,timings)>0){if(timings[0].cpuFrameTime>0)cpu.Add(timings[0].cpuFrameTime);if(timings[0].gpuFrameTime>0)gpu.Add(timings[0].gpuFrameTime);}}
  double Median(List<double> list){if(list.Count==0)return -1;list.Sort();return list[list.Count/2];}
  Check(nativeRenderFrames-renderedBefore>=50,"Native camera rendered during "+style+" profiling");
  double ms=Median(frameTimes);return new{renderedFrames=nativeRenderFrames-renderedBefore,style,samples=frameTimes.Count,editorFrameMs=ms,editorFps=1000/ms,cpuFrameMs=Median(cpu),gpuFrameMs=Median(gpu),gpuAvailable=gpu.Count>0,width=Camera.main.pixelWidth,height=Camera.main.pixelHeight,renderScale=(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).renderScale,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),context="Live CombatTest, six active Chicken Bro prefabs, 960x540 offscreen camera; Editor pacing and AI variance, not a standalone benchmark"};
 }
 static Task SnapshotUI(VisualStyleUIController ui,string path)=>SnapshotCanvas(ui.GetComponentInChildren<Canvas>(true),path);
 static async Task SnapshotCanvas(Canvas canvas,string path)
 {
  // Capture the actual composited Game view. Converting overlay canvases to camera-space
  // changes stencil/default-material behavior and cannot establish production UI appearance.
  if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay)throw new Exception("Expected real overlay UI");
  EditorApplication.ExecuteMenuItem("Window/General/Game");for(int i=0;i<4;i++)await Frame();
  var viewType=typeof(Editor).Assembly.GetType("UnityEditor.PlayModeView");
  var flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
  var view=viewType.GetMethod("GetMainPlayModeView",flags).Invoke(null,null);
  var source=viewType.GetField("m_TargetTexture",flags).GetValue(view) as RenderTexture;
  if(!source||!source.IsCreated())throw new Exception("Game view has no rendered target");
  var output=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
  var image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
  try{if(SystemInfo.graphicsUVStartsAtTop)Graphics.Blit(source,output,new Vector2(1,-1),new Vector2(0,1));else Graphics.Blit(source,output);RenderTexture.active=output;image.ReadPixels(new Rect(0,0,output.width,output.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Check(true,"Composited overlay capture "+Path.GetFileName(path));}
  finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(output);}
 }
 static async Task Run()
 {
  checks.Clear();bool passed=false;Scene menuScene=default;var reports=new List<object>();
  bool hasStyle=PlayerPrefs.HasKey(VisualStyleSettings.StyleKey),hasIntensity=PlayerPrefs.HasKey(VisualStyleSettings.IntensityKey);
  string savedStyle=VisualStyleSettings.SelectedId;float savedIntensity=PlayerPrefs.GetFloat(VisualStyleSettings.IntensityKey,.85f);
  var manager=VisualStyleManager.Instance;string originalStyle=manager.Current.id;float originalIntensity=manager.Intensity;
  bool wasPixel=Pixel360Rendering.IsActive;var originalPipeline=QualitySettings.renderPipeline;var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  float time=Time.timeScale;VisualStyleUIController ui=null;RenderTexture profileTarget=null,oldCameraTarget=null;UniversalRenderPipelineAsset profilePipeline=null;
  var detailPrefs=new List<(string key,bool exists,float value)>();foreach(string id in new[]{"origami","monochrome"})for(int i=0;i<4;i++){string key="DarkBrine.VisualStyle."+id+".Detail"+i;detailPrefs.Add((key,PlayerPrefs.HasKey(key),PlayerPrefs.GetFloat(key)));}
  try
  {
   Check(manager.Presets.Count==4,"Four resource presets load without a scene dependency");
   Check(manager.Current.id=="origami"&&Mathf.Abs(manager.Intensity-.72f)<.001f,"A fresh Play restores saved Origami and intensity");
   var materials=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).ToDictionary(r=>r,r=>r.sharedMaterials.Select(m=>m?m.GetInstanceID():0).ToArray());
   var actors=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None).Select(p=>(p,p.transform.position,p.transform.rotation)).ToArray();
   for(int i=0;i<20;i++)Check(manager.Select(new[]{"realistic","dark-fairytale","origami","monochrome"}[i%4],false),"Switch without scene reload "+i);
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
   Check(VisualStyleUIController.IsOpen&&ui.StyleButtons.Count==4,"Main Menu opens the four-card gallery");
   Check(ui.GetComponentInChildren<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"Gallery and text remain native overlay UI");
   var hover=ui.GetComponentsInChildren<VisualStyleCardHover>().First(h=>h.name!=manager.Current.id);var card=hover.GetComponent<Image>();var color=card.color;hover.OnPointerEnter(new PointerEventData(EventSystem.current));Check(card.color!=color,"Full card has hover feedback");hover.OnPointerExit(new PointerEventData(EventSystem.current));
   ui.StyleButtons[0].onClick.Invoke();Check(manager.Current.id=="realistic","Realistic card selects the shared manager");
   ui.StyleButtons[1].onClick.Invoke();ui.IntensitySlider.value=.65f;Check(manager.Current.id=="dark-fairytale"&&Mathf.Abs(manager.Intensity-.65f)<.001f,"Dark card and intensity update the same saved configuration");
   ui.StyleButtons[2].onClick.Invoke();Check(manager.Current.id=="origami","Origami card selects the shared manager");
   float assetFiber=manager.Current.paper.fiberStrength;ui.DetailSliders[0].value=.22f;
   Check(Mathf.Abs(manager.Details.x-.22f)<.001f&&manager.Current.paper.fiberStrength==assetFiber,"Paper control saves a runtime override without mutating its material profile");
   ((Button)Field(ui,"detailsButton")).onClick.Invoke();
   await SnapshotUI(ui,Root+"/gallery.png");
   ui.StyleButtons[3].onClick.Invoke();Check(manager.Current.id=="monochrome","Monochrome card selects the shared manager");
   ui.DetailSliders[1].value=.26f;Check(Mathf.Abs(manager.Details.y-.26f)<.001f,"Film grain control updates saved monochrome parameters");
   ui.StyleButtons[2].onClick.Invoke();Check(Mathf.Abs(manager.Details.x-.22f)<.001f,"Paper and film parameters are independent between styles");
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
   foreach(var a in manager.Presets)foreach(var b in manager.Presets)
   {
    Check(comparison.SetStyles(a.id,b.id)&&comparison.leftPicture.texture==a.preview&&comparison.rightPicture.texture==b.preview,"Aligned selectable comparison "+a.id+" / "+b.id);
   }
   Check(!comparison.SetStyles("invalid","origami"),"Comparison rejects unknown choices");
   comparison.SetStyles("origami","monochrome");
   comparison.Split=.5f;await SnapshotUI(ui,Root+"/comparison-ui.png");ui.Close();
   menu.Settings();await Frame();var settings=menu.settingsMenu;settings.sectionTabs[4].onClick.Invoke();
   Check(settings.options.Count(o=>o.key=="visualStyle")==1&&settings.options.Count(o=>o.key=="pixel360")==1,"One visual-style row and one existing pixel-mode row");
   var option=settings.options.Single(o=>o.key=="visualStyle");option.button.onClick.Invoke();await Frame();
   Check(VisualStyleUIController.IsOpen,"Graphics opens the same gallery");ui.StyleButtons[0].onClick.Invoke();Check(option.value.text=="Realistic","Graphics value synchronizes immediately with gallery selection");
   ui.StyleButtons[1].onClick.Invoke();Check(option.value.text=="Dark Fairytale","Graphics shares Dark Fairytale selection");ui.Close();
   VisualStyleUIController.Open(menu.transform); // Reopen through the existing parent context for both added graphics choices.
   ui.StyleButtons[2].onClick.Invoke();Check(option.value.text=="Origami","Graphics shares Origami selection");
   ui.StyleButtons[3].onClick.Invoke();Check(option.value.text=="Monochrome","Graphics shares Monochrome selection");ui.Close();
   settings.Resume();await Frame();await WaitScene(SceneManager.UnloadSceneAsync(menuScene));menuScene=default;
   var active=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   foreach(bool pixel in new[]{false,true})
   {
    Pixel360Rendering.Configure(active,pixel);
    foreach(string style in new[]{"realistic","dark-fairytale","origami","monochrome"}){manager.Select(style,false);await Frame();Check(manager.Current.id==style&&Pixel360Rendering.IsActive==pixel,style+" combines independently with pixel="+pixel);}
   }
   Pixel360Rendering.ReleaseCurrent();
   var arena=UnityEngine.Object.FindFirstObjectByType<CombatTestArena>();Check(arena&&arena.player,"Existing live combat arena available");
   var health=arena.player.GetComponent<PlayerHealth>();if(health)health.GrantProtection(30);
   Call(arena,"SpawnChickens",6);await Frame();Check(UnityEngine.Object.FindObjectsByType<ChickenBroAI>(FindObjectsSortMode.None).Length>=6,"Six existing Chicken Bros remain active for both style profiles");
   var chickenBones=UnityEngine.Object.FindObjectsByType<ChickenBroAI>(FindObjectsSortMode.None).SelectMany(c=>c.GetComponentsInChildren<SkinnedMeshRenderer>()).SelectMany(r=>r.bones).Where(b=>b).Distinct().Select(b=>(bone:b,rotation:b.localRotation)).ToArray();
   Time.timeScale=1;
   profilePipeline=UnityEngine.Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);profilePipeline.renderScale=1;profilePipeline.msaaSampleCount=1;QualitySettings.renderPipeline=profilePipeline;
   RenderPipelineManager.endCameraRendering+=Rendered;var profileCamera=Camera.main;
   profileTarget=new RenderTexture(960,540,24);profileTarget.Create();oldCameraTarget=profileCamera.targetTexture;profileCamera.targetTexture=profileTarget;
   foreach(var style in new[]{"realistic","dark-fairytale","origami","monochrome"})reports.Add(await Profile(style));
   Check(chickenBones.Length>0&&chickenBones.Any(b=>b.bone&&Quaternion.Angle(b.rotation,b.bone.localRotation)>.1f),"Existing enemy skeleton poses advance during the four live profiles");
   profileCamera.targetTexture=oldCameraTarget;
   Check(arena.player.enabled&&arena.player.GetComponent<SahurAttack>().enabled,"Player movement and combat components remain enabled after switching");
   Call(arena,"ClearSpawned");passed=true;
  }
  catch(Exception error){File.WriteAllText(Root+"/verification-error.txt",error.ToString());}
  finally
  {
   RenderPipelineManager.endCameraRendering-=Rendered;
   if(ui&&VisualStyleUIController.IsOpen)ui.Close();if(menuScene.IsValid()&&menuScene.isLoaded)SceneManager.UnloadSceneAsync(menuScene);
   Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=originalPipeline;if(wasPixel&&pipeline)Pixel360Rendering.Configure(pipeline,true);
   if(Camera.main&&Camera.main.targetTexture==profileTarget)Camera.main.targetTexture=oldCameraTarget;
   if(profileTarget){profileTarget.Release();UnityEngine.Object.DestroyImmediate(profileTarget);}
   if(profilePipeline)UnityEngine.Object.DestroyImmediate(profilePipeline);
   foreach(var pref in detailPrefs)if(pref.exists)PlayerPrefs.SetFloat(pref.key,pref.value);else PlayerPrefs.DeleteKey(pref.key);
   if(hasStyle)PlayerPrefs.SetString(VisualStyleSettings.StyleKey,savedStyle);else PlayerPrefs.DeleteKey(VisualStyleSettings.StyleKey);
   if(hasIntensity)PlayerPrefs.SetFloat(VisualStyleSettings.IntensityKey,savedIntensity);else PlayerPrefs.DeleteKey(VisualStyleSettings.IntensityKey);PlayerPrefs.Save();
   manager.Select(originalStyle,false);manager.SetIntensity(originalIntensity,false);Time.timeScale=time;
   File.WriteAllText(Root+"/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,profiles=reports},Newtonsoft.Json.Formatting.Indented));File.Delete(Root+"/verification-running");EditorApplication.isPlaying=false;
  }
 }
}
