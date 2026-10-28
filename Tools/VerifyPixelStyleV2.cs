using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;

public static class VerifyPixelStyleV2
{
 const string Root=".codex/pixel-style-v2";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static async Task Frame(){int n=Time.frameCount;var limit=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount==n){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Test frame stalled");await Task.Delay(5);}}
 static object Field(object owner,string name)=>owner.GetType().GetField(name,F).GetValue(owner);
 static void Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name,F).Invoke(owner,args);
 static void SetPixel(PauseSettingsMenu menu,bool enabled){var box=Field(menu,"saved");box.GetType().GetField("pixel360").SetValue(box,enabled);menu.GetType().GetField("draft",F).SetValue(menu,box);Call(menu,"ApplyDraft");}
 public static object Start(){Directory.CreateDirectory(Root);_=Run();return new{started=true};}
 static async Task Capture(string name)
 {
  string path=Root+"/"+name+".png";if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);for(int i=0;i<30;i++){await Frame();if(File.Exists(path))return;}throw new Exception("Screenshot not written: "+name);
 }
 static RectTransform Rect(Transform p,string name,Vector2 pos,Vector2 size){var r=GameUITheme.Rect(p,name,new Vector2(.5f,.5f),size);r.anchoredPosition=pos;return r;}
 static async Task Run()
 {
  var checks=new List<string>();void Check(bool value,string text){if(!value)throw new Exception(text);checks.Add(text);}
  bool passed=false;GameObject root=null;Texture2D texture=null;Material worldMaterial=null,artMaterial=null,customUI=null;RenderTexture target=null;Texture2D read=null;
  float clock=Time.timeScale;var oldCursor=Cursor.lockState;bool cursorVisible=Cursor.visible;
  bool pixelExists=PlayerPrefs.HasKey("DarkBrine.Settings.Pixel360");int savedPixel=PlayerPrefs.GetInt("DarkBrine.Settings.Pixel360");
  var manager=VisualStyleManager.Instance;string style=manager.Current.id;float intensity=manager.Intensity;
  PauseSettingsMenu menu=null;object menuSaved=null,menuDraft=null;
  var originals=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>!(b is VisualStyleManager)&&!(b is Pixel360Rendering)&&!(b is PixelStyleUI)).Select(b=>(b,b.enabled)).ToArray();
  var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.enabled,c.tag)).ToArray();
  var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Select(c=>(c,c.enabled)).ToArray();
  try
  {
   await Frame();menu=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();Check(menu,"Existing settings menu installed");
   menuSaved=Field(menu,"saved");menuDraft=Field(menu,"draft");
   foreach(var o in originals)if(o.b)o.b.enabled=false;
   foreach(var c in cameras)if(c.c){c.c.enabled=false;if(c.c.CompareTag("MainCamera"))c.c.tag="Untagged";}
   foreach(var c in canvases)if(c.c)c.c.enabled=false;
   SetPixel(menu,false);var pipeline=(UniversalRenderPipelineAsset)Field(menu,"runtimePipelineAsset");
   var builtin=Graphic.defaultGraphicMaterial;float normalScale=pipeline.renderScale;var normalFilter=pipeline.upscalingFilter;int normalMsaa=pipeline.msaaSampleCount;
   root=new GameObject("Temporary Pixel Style verification");root.AddComponent<GameSaveExcluded>();root.transform.position=new Vector3(2000,500,2000);
   var camObject=new GameObject("Pixel Style test camera",typeof(Camera));camObject.transform.SetParent(root.transform,false);camObject.tag="MainCamera";var camera=camObject.GetComponent<Camera>();camera.transform.localPosition=new Vector3(0,0,-12);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.14f,.20f);camera.farClipPlane=100;
   camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   texture=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=new Color(x/63f,y/63f,((x/2+y/2)%2==0)?1:.05f,1);texture.SetPixels(pixels);texture.Apply();
   worldMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));worldMaterial.SetTexture("_BaseMap",texture);
   var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(root.transform,false);cube.transform.localScale=Vector3.one*5;cube.transform.localRotation=Quaternion.Euler(14,24,8);cube.GetComponent<Renderer>().sharedMaterial=worldMaterial;
   var canvasObject=new GameObject("Pixel Style UI fixture",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(root.transform,false);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=800;GameUITheme.Scale(canvas);
   var background=Rect(canvas.transform,"Text background",new Vector2(430,290),new Vector2(760,150));GameUITheme.Image(background,new Color(.025f,.035f,.045f,1));
   var legacy=GameUITheme.Label(canvas.transform,"Native legacy text",38,new Vector2(0,-170),new Vector2(680,70),TextAnchor.MiddleCenter);legacy.text="Text stays sharp 0123456789";legacy.rectTransform.anchorMin=legacy.rectTransform.anchorMax=new Vector2(.5f,1);legacy.rectTransform.anchoredPosition=new Vector2(430,-250);
   var raw=Rect(canvas.transform,"Photo and gradient",new Vector2(-430,160),new Vector2(650,420)).gameObject.AddComponent<RawImage>();raw.texture=texture;
   var art=Rect(canvas.transform,"Authored artwork shader",new Vector2(-430,-260),new Vector2(650,280)).gameObject.AddComponent<RawImage>();art.texture=texture;artMaterial=new Material(Shader.Find("UI/Hand Drawn Contrast"));artMaterial.SetFloat("_Contrast",1);artMaterial.SetFloat("_Saturation",1);art.material=artMaterial;
   var buttonRect=Rect(canvas.transform,"Pixel button",new Vector2(430,90),new Vector2(400,64));var button=buttonRect.gameObject.AddComponent<Button>();button.targetGraphic=buttonRect.gameObject.AddComponent<Image>();int clicks=0;button.onClick.AddListener(()=>clicks++);GameUITheme.StyleButton(button);
   var label=GameUITheme.Label(buttonRect,"Button text",26,new Vector2(0,-32),new Vector2(380,58),TextAnchor.MiddleCenter);label.text="PIXEL BUTTON";
   var worldCanvasObject=new GameObject("Floating text",typeof(RectTransform),typeof(Canvas));worldCanvasObject.transform.SetParent(root.transform,false);worldCanvasObject.transform.localPosition=new Vector3(0,3.7f,0);worldCanvasObject.transform.localScale=Vector3.one*.012f;worldCanvasObject.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
   var worldText=GameUITheme.Label(worldCanvasObject.transform,"Floating native text",28,Vector2.zero,new Vector2(420,50),TextAnchor.MiddleCenter);worldText.text="Floating text stays sharp";worldText.gameObject.AddComponent<Pixel360WorldText>();
   var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Focus();
   Time.timeScale=0;for(int i=0;i<5;i++)await Frame();await Capture("normal");
   var centerRay=camera.ViewportPointToRay(new Vector3(.5f,.5f)).direction;
   SetPixel(menu,true);for(int i=0;i<5;i++)await Frame();Canvas.ForceUpdateCanvases();
   Check(Pixel360Rendering.IsActive&&Pixel360Rendering.TargetHeight==180,"Coarser world target enabled");
   Check(Graphic.defaultGraphicMaterial!=builtin&&Graphic.defaultGraphicMaterial.shader.name=="UI/DarkBrine Pixel Style","Shared non-text UI uses the pixel shader");
   Check(raw.material==Graphic.defaultGraphicMaterial,"Gallery/photos and icons inherit pixel rendering");
   Check(legacy.material.shader==builtin.shader,"Legacy text retains its native shader");
   Check(!worldText.enabled&&Pixel360Rendering.OverlayRoot.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"Floating text keeps its native overlay path");
   Check(art.material==artMaterial&&art.material.shader.name=="UI/Hand Drawn Contrast","Authored artwork material and alignment remain intact");
   Check(Shader.GetGlobalFloat("_PixelStyleUI")==1,"Authored artwork receives the same global pixel toggle");
   var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,buttonRect.position),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Check(hits.Any(h=>h.gameObject==button.gameObject),"Pixel graphics retain native UI hit areas");ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Check(clicks==1,"Pixel button remains clickable");
   var option=menu.options.Single(o=>o.key=="pixel360");var row=option.button.transform.parent.parent;
   Check(row.Find("Label").GetComponent<TMPro.TMP_Text>().text=="PIXEL STYLE","Pixel Style name replaces the resolution label");
   Check(row.Find("Explanation").GetComponent<TMPro.TMP_Text>().text.Contains("Ultra-light"),"Low rendering cost is explained");
   Check(!option.value.text.Contains("360")&&!option.value.text.Contains("180")&&option.value.text.Contains("ULTRA LIGHT"),"No internal resolution appears in the option value");
   Check(option.value.preferredWidth<option.value.rectTransform.rect.width,"New value fits its existing settings control");
   foreach(int height in new[]{720,1080,1440})Check(Mathf.FloorToInt(height*Pixel360Rendering.ScaleForHeight(height))==180,"Coarse scene grid at output height "+height);
   Check(Mathf.Abs(Pixel360Rendering.ScaleForHeight(1080)-1f/6)<.00001f,"World raster area is one quarter of the old pixel preset at the same output");
   await Capture("pixel-style");
   target=new RenderTexture(1920,1080,24);target.Create();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});read=new Texture2D(1920,1080,TextureFormat.RGB24,false);var oldRT=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,1920,1080),0,0);read.Apply();RenderTexture.active=oldRT;File.WriteAllBytes(Root+"/world-pixel.png",read.EncodeToPNG());
   Check(Vector3.Distance(centerRay,camera.ViewportPointToRay(new Vector3(.5f,.5f)).direction)<.00001f,"Projection and aim are unchanged");
   manager.Select("dark-fairytale",false);await Frame();Check(Pixel360Rendering.IsActive,"Dark Fairytale combines with Pixel Style");manager.Select("realistic",false);await Frame();Check(Pixel360Rendering.IsActive,"Realistic combines with Pixel Style");
   customUI=new Material(builtin);customUI.name="Temporary explicit UI material";button.image.material=customUI;for(int i=0;i<20;i++)await Frame();Check(button.image.material.shader.name=="UI/DarkBrine Pixel Style","Explicit UI materials join the pixel style");
   SetPixel(menu,false);for(int i=0;i<3;i++)await Frame();Check(Graphic.defaultGraphicMaterial==builtin&&Shader.GetGlobalFloat("_PixelStyleUI")==0,"Turning off restores the exact builtin UI material and artwork path");
   Check(raw.material==builtin&&worldText.enabled,"Normal UI graphics and original floating text return");
   Check(Mathf.Approximately(pipeline.renderScale,normalScale)&&pipeline.upscalingFilter==normalFilter&&pipeline.msaaSampleCount==normalMsaa,"Normal scene scale/filter/MSAA restore");
   Check(art.material==artMaterial,"Original custom UI material reference survives both toggles");Check(button.image.material==customUI,"Dynamically assigned explicit UI material restores exactly");passed=true;
  }
  catch(Exception error){File.WriteAllText(Root+"/error.txt",error.ToString());}
  finally
  {
   Pixel360Rendering.ReleaseCurrent();if(menu&&menuSaved!=null){menu.GetType().GetField("saved",F).SetValue(menu,menuSaved);menu.GetType().GetField("draft",F).SetValue(menu,menuDraft);Call(menu,"ApplySettings",menuSaved);Call(menu,"Refresh");}
   if(pixelExists)PlayerPrefs.SetInt("DarkBrine.Settings.Pixel360",savedPixel);else PlayerPrefs.DeleteKey("DarkBrine.Settings.Pixel360");PlayerPrefs.Save();
   manager.Select(style,false);manager.SetIntensity(intensity,false);Time.timeScale=clock;Cursor.lockState=oldCursor;Cursor.visible=cursorVisible;
   if(root)UnityEngine.Object.DestroyImmediate(root);if(texture)UnityEngine.Object.DestroyImmediate(texture);if(worldMaterial)UnityEngine.Object.DestroyImmediate(worldMaterial);if(artMaterial)UnityEngine.Object.DestroyImmediate(artMaterial);if(customUI)UnityEngine.Object.DestroyImmediate(customUI);if(read)UnityEngine.Object.DestroyImmediate(read);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}
   foreach(var o in originals)if(o.b)o.b.enabled=o.enabled;foreach(var c in cameras)if(c.c){c.c.enabled=c.enabled;c.c.tag=c.tag;}foreach(var c in canvases)if(c.c)c.c.enabled=c.enabled;
   File.WriteAllText(Root+"/native.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,screen=new{Screen.width,Screen.height}},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;
  }
 }
}
