using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class VerifyPixel360V260
{
 const string Root=".codex/pixel360-v260",Prefix="DarkBrine.Settings.";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Field(object target,string name)=>target.GetType().GetField(name,F).GetValue(target);
 static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,F).Invoke(target,args);
 public static object Open()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve current work");
  var paths=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  File.WriteAllText(Root+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(paths));
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;return new{started=true};
 }
 public static object Reopen()
 {
  if(Application.isPlaying)throw new Exception("Stopped Editor required");
  var paths=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Root+"/workspace-before.json"));
  for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
  return new{reopened=paths};
 }
 public static async Task<object> Verify()
 {
  var checks=new List<string>();var errors=new List<string>();
  void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
  void Log(string s,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception)errors.Add(s);}
  string[] floats={"MasterVolume","Sensitivity","FieldOfView","CameraDistance","RenderScale","ShadowDistance","LodBias"};
  string[] ints={"InvertY","Fullscreen","VSync","PostProcessing","Shadows","Bloom","Vignette","MotionBlur","Pixel360","ResolutionWidth","ResolutionHeight","FrameLimit","TextureMipmapLimit","Antialiasing","ShadowResolution","AnisotropicFiltering","Difficulty","LowGraphicsDefaultV1","ShadowDistanceV2","ShadowDistanceV3"};
  ints=ints.Concat(Enum.GetNames(typeof(GameInputSettings.Action)).Select(n=>"Key."+n)).ToArray();
  var oldFloats=floats.Select(k=>new{k,exists=PlayerPrefs.HasKey(Prefix+k),value=PlayerPrefs.GetFloat(Prefix+k)}).ToArray();
  var oldInts=ints.Select(k=>new{k,exists=PlayerPrefs.HasKey(Prefix+k),value=PlayerPrefs.GetInt(Prefix+k)}).ToArray();
  File.WriteAllText(Root+"/preferences-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{oldFloats,oldInts},Newtonsoft.Json.Formatting.Indented));
  PauseSettingsMenu menu=null;GameObject sceneRoot=null;Texture2D checker=null;Material material=null;RenderTexture target=null;Texture2D read=null;VolumeProfile sharedProfile=null;
  Application.logMessageReceived+=Log;
  try
  {
   PlayerPrefs.SetInt(Prefix+"Pixel360",0);
   sceneRoot=new GameObject("Temporary pixel mode verification");
   var cameraObject=new GameObject("Pixel verification camera",typeof(Camera));cameraObject.transform.SetParent(sceneRoot.transform);cameraObject.tag="MainCamera";
   var camera=cameraObject.GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-12);camera.backgroundColor=new Color(.06f,.12f,.2f);camera.clearFlags=CameraClearFlags.SolidColor;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.GetUniversalAdditionalCameraData().dithering=true;
   var volume=sceneRoot.AddComponent<Volume>();volume.isGlobal=true;sharedProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=sharedProfile;
   sharedProfile.Add<DepthOfField>(true);sharedProfile.Add<MotionBlur>(true);sharedProfile.Add<FilmGrain>(true);
   checker=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};var colors=new Color[4096];
   for(int y=0;y<64;y++)for(int x=0;x<64;x++)colors[y*64+x]=((x/2+y/2)%2==0)?new Color(.95f,.4f,.05f):new Color(.04f,.65f,.9f);checker.SetPixels(colors);checker.Apply();
   material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",checker);
   var shape=GameObject.CreatePrimitive(PrimitiveType.Cube);shape.transform.SetParent(sceneRoot.transform);shape.transform.localScale=Vector3.one*5;shape.transform.rotation=Quaternion.Euler(18,27,12);shape.GetComponent<Renderer>().sharedMaterial=material;
   var hud=new GameObject("Native test HUD",typeof(RectTransform),typeof(Canvas));hud.transform.SetParent(sceneRoot.transform);hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   var label=GameUITheme.Label(hud.transform,"Native text",32,new Vector2(30,-32),new Vector2(680,58),TextAnchor.MiddleLeft);label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(0,1);label.rectTransform.pivot=new Vector2(0,1);label.text="Native HUD text  |  HP 100 / 100";
   var world=new GameObject("World caption",typeof(RectTransform),typeof(Canvas));world.transform.SetParent(sceneRoot.transform);world.transform.position=new Vector3(0,3.6f,0);world.transform.localScale=Vector3.one*.012f;world.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
   var worldText=GameUITheme.Label(world.transform,"Floating caption",24,Vector2.zero,new Vector2(420,50),TextAnchor.MiddleCenter);worldText.text="Floating caption stays sharp";worldText.gameObject.AddComponent<Pixel360WorldText>();
   var prefab=AssetDatabase.LoadAssetAtPath<PauseSettingsMenu>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab");
   menu=UnityEngine.Object.Instantiate(prefab);
   await Task.Delay(100);
   Check(menu.options.Count(o=>o.key=="pixel360")==1,"Exactly one 360p option is installed");
   var option=menu.options.Find(o=>o.key=="pixel360");var row=option.button.transform.parent.parent as RectTransform;
   Check(row.GetSiblingIndex()==0&&row.anchoredPosition.y==-36,"Pixel mode is the first graphics row");
   var pipeline=(UniversalRenderPipelineAsset)Field(menu,"runtimePipelineAsset");var source=(UniversalRenderPipelineAsset)Field(menu,"originalPipelineAsset");float sourceScale=source.renderScale;
   float normalScale=pipeline.renderScale;var normalFilter=pipeline.upscalingFilter;int normalMsaa=pipeline.msaaSampleCount;var aa=camera.GetUniversalAdditionalCameraData().antialiasing;
   var effectProfile=volume.profile;effectProfile.TryGet(out DepthOfField dof);effectProfile.TryGet(out MotionBlur blur);effectProfile.TryGet(out FilmGrain grain);bool normalBlur=blur.active;
   Vector3 direction=camera.ViewportPointToRay(new Vector3(.5f,.5f)).direction;
   Check(!Pixel360Rendering.IsActive,"360p is opt-in for existing settings");
   menu.Open();menu.settingsButton.onClick.Invoke();menu.sectionTabs[4].onClick.Invoke();
   option.button.onClick.Invoke();
   Check(!Pixel360Rendering.IsActive&&option.value.text.Contains("360P"),"Toggle stages a draft until Apply");
   menu.applyButton.onClick.Invoke();await Task.Delay(100);
   Check(Pixel360Rendering.IsActive&&PlayerPrefs.GetInt(Prefix+"Pixel360")==1,"Apply enables and persists 360p mode");
   Check(pipeline.upscalingFilter==UpscalingFilterSelection.Point&&pipeline.msaaSampleCount==1&&camera.GetUniversalAdditionalCameraData().antialiasing==AntialiasingMode.None,"Nearest-neighbour upscale with MSAA and camera AA disabled");
   Check(!dof.active&&!blur.active&&!grain.active&&!camera.GetUniversalAdditionalCameraData().dithering,"Depth blur, motion blur, film grain and final dithering are suppressed");
   Check(!menu.options.Find(o=>o.key=="renderScale").slider.interactable&&!menu.options.Find(o=>o.key=="antialiasing").button.interactable,"Conflicting render-scale and AA controls are disabled");
   Check(hud.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay&&!worldText.enabled&&Pixel360Rendering.OverlayRoot.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"HUD and floating caption text render in native overlay UI");
   Canvas.ForceUpdateCanvases();Check(option.value.preferredWidth<option.value.rectTransform.rect.width,"Pixel mode value fits the menu button");
   var scaleLabel=menu.options.Find(o=>o.key=="renderScale").value;
   Check(scaleLabel.preferredWidth<=scaleLabel.rectTransform.rect.width,"Fixed resolution value fits beside its disabled slider");
   ScreenCapture.CaptureScreenshot(Root+"/settings.png");await Task.Delay(700);menu.Resume();
   var dimensions=new List<object>();
   foreach(int height in new[]{720,1080,1440,2160})
   {
    int width=height*16/9;target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.Render();
    int internalHeight=Mathf.FloorToInt(height*pipeline.renderScale);dimensions.Add(new{width,height,scale=pipeline.renderScale,internalHeight});
    Check(internalHeight==360,"360 internal lines at "+height+"p output");
    if(height==1080)
    {
     read=new Texture2D(width,height,TextureFormat.RGB24,false);var active=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,width,height),0,0,false);RenderTexture.active=active;
     File.WriteAllBytes(Root+"/pixel-world-1080.png",read.EncodeToPNG());var data=read.GetPixels32();int mixed=0,blocks=0,detail=0;
     for(int y=0;y<height;y+=3)for(int x=0;x<width;x+=3)
     {
      var first=data[y*width+x];bool same=true;
      for(int j=0;j<3;j++)for(int i=0;i<3;i++){var c=data[(y+j)*width+x+i];if(Mathf.Abs(c.r-first.r)>1||Mathf.Abs(c.g-first.g)>1||Mathf.Abs(c.b-first.b)>1)same=false;}
      if(!same)mixed++;blocks++;if(first.r>first.b+30)detail++;
     }
     File.WriteAllText(Root+"/pixel-blocks.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{mixed,blocks,detail}));
     Check(detail>1000&&mixed<blocks*.002f,"Actual 1080p image consists of sharp repeated 3x3 scene pixels");
     UnityEngine.Object.Destroy(read);read=null;
    }
    camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);target=null;
   }
   Check(Vector3.Distance(direction,camera.ViewportPointToRay(new Vector3(.5f,.5f)).direction)<.00001f,"Camera projection and centre aim are unchanged");
   var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Focus();await Task.Delay(200);
   ScreenCapture.CaptureScreenshot(Root+"/native-ui.png");await Task.Delay(700);
   UnityEngine.Object.Destroy(menu.gameObject);menu=null;await Task.Delay(100);
   menu=UnityEngine.Object.Instantiate(prefab);await Task.Delay(100);pipeline=(UniversalRenderPipelineAsset)Field(menu,"runtimePipelineAsset");
   Check(Pixel360Rendering.IsActive,"Recreated settings menu restores the saved pixel mode");
   option=menu.options.Find(o=>o.key=="pixel360");option.button.onClick.Invoke();menu.applyButton.onClick.Invoke();await Task.Delay(100);
   Check(!Pixel360Rendering.IsActive&&worldText.enabled&&Mathf.Approximately(pipeline.renderScale,normalScale)&&pipeline.upscalingFilter==normalFilter&&pipeline.msaaSampleCount==normalMsaa&&camera.GetUniversalAdditionalCameraData().antialiasing==aa,"Turning off restores normal scale, filtering, AA, MSAA and world text");
   Check(dof.active&&grain.active&&blur.active==normalBlur&&camera.GetUniversalAdditionalCameraData().dithering,"Turning off restores original post effects and dithering");
   Check(Mathf.Approximately(source.renderScale,sourceScale),"Shared project render-pipeline asset is unchanged");
   Check(errors.Count==0,"No rendering or runtime errors");
   var result=new{passed=true,checks,dimensions,screen=new{Screen.width,Screen.height},errors};File.WriteAllText(Root+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  catch(Exception e){File.WriteAllText(Root+"/failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error=e.ToString(),checks,errors},Newtonsoft.Json.Formatting.Indented));throw;}
  finally
  {
   Application.logMessageReceived-=Log;if(menu)UnityEngine.Object.Destroy(menu.gameObject);Pixel360Rendering.ReleaseCurrent();
   if(sceneRoot)UnityEngine.Object.Destroy(sceneRoot);if(material)UnityEngine.Object.Destroy(material);if(sharedProfile)UnityEngine.Object.Destroy(sharedProfile);if(checker)UnityEngine.Object.Destroy(checker);if(read)UnityEngine.Object.Destroy(read);if(target){target.Release();UnityEngine.Object.Destroy(target);}
   foreach(var item in oldFloats){if(item.exists)PlayerPrefs.SetFloat(Prefix+item.k,item.value);else PlayerPrefs.DeleteKey(Prefix+item.k);}
   foreach(var item in oldInts){if(item.exists)PlayerPrefs.SetInt(Prefix+item.k,item.value);else PlayerPrefs.DeleteKey(Prefix+item.k);}PlayerPrefs.Save();
   EditorApplication.isPlaying=false;
  }
 }
}
