using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
public static class VerifyStaminaPixel
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 const string Root=".codex/stamina-pixel";
 static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
 static object Field(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static async Task Frame(){int n=Time.frameCount;var t=DateTime.UtcNow.AddSeconds(15);while(Time.frameCount==n){if(DateTime.UtcNow>t)throw new Exception("Frame stalled");await Task.Delay(5);}}
 public static object Start(){Directory.CreateDirectory(Root);_=Run();return "started";}
 static async Task Run()
 {
  var checks=new System.Collections.Generic.List<string>();bool passed=false;string error=null;
  void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add(s);}
  var stamina=UnityEngine.Object.FindFirstObjectByType<PlayerStamina>();float previous=stamina.currentStamina;
  bool pixel=PlayerPrefs.GetInt("DarkBrine.Settings.Pixel360",0)==1; Pixel360Rendering.ReleaseCurrent(); typeof(Graphic).GetField("s_DefaultUI",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Canvas.GetDefaultCanvasMaterial()); Shader.SetGlobalFloat("_PixelStyleUI",0);
  var menu=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();var asset=(UniversalRenderPipelineAsset)Field(menu,"runtimePipelineAsset");
  GameObject fixture=null;
  try
  {
   // Rebuild the existing runtime HUD so the fix is visible in the user's current paused session too.
   var live=UnityEngine.Object.FindFirstObjectByType<PlayerVitalsHUD>();
   if(live){var child=live.transform.Find("Sahur Vitals HUD");if(child)UnityEngine.Object.DestroyImmediate(child.gameObject);var old=Field(live,"solidSprite") as Sprite;if(old)UnityEngine.Object.DestroyImmediate(old);Call(live,"BuildHud");live.GetType().GetField("shownHp",F).SetValue(live,-1);live.GetType().GetField("shownPacks",F).SetValue(live,-1);Call(live,"RefreshBars",true);Call(live,"RefreshHealing");}
   fixture=new GameObject("Temporary stamina render verification",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
   var canvas=fixture.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32767;GameUITheme.Scale(canvas);
   var back=GameUITheme.Rect(fixture.transform,"Background",Vector2.zero,Vector2.zero);back.anchorMax=Vector2.one;back.offsetMin=back.offsetMax=Vector2.zero;GameUITheme.Image(back,Color.black);
   var hud=fixture.AddComponent<PlayerVitalsHUD>();hud.enabled=false;
   var group=hud.GetComponentInChildren<CanvasGroup>();group.alpha=1;
   var fill=hud.GetComponentsInChildren<Image>().First(x=>x.name=="Stamina Fill");var track=hud.GetComponentsInChildren<Image>().First(x=>x.name=="Stamina Track");
   Check(track.rectTransform.rect.height==8,"Stamina track increased from 3 to 8 units");
   Check(fill.sprite.uv.Max(v=>v.x)==1&&fill.sprite.uv.Max(v=>v.y)==1,"Solid sprite spans full UV range");
   EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
   foreach(bool enabled in new[]{false,true})
   {
    Pixel360Rendering.Configure(asset,enabled);for(int i=0;i<4;i++)await Frame();
    foreach(float value in new[]{100f,50f,1f,0f})
    {
     stamina.currentStamina=value;Call(hud,"RefreshBars",true);Canvas.ForceUpdateCanvases();await Frame();
     Check(fill.enabled==(value>0),$"{enabled}: fill visibility at {value}%");
     Check(Mathf.Abs(fill.rectTransform.rect.width-track.rectTransform.rect.width*value/100)<.01f,$"{enabled}: fill tracks stamina at {value}%");
     string path=Root+"/"+(enabled?"pixel":"normal")+"-"+value+".png";if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);
     for(int i=0;i<20;i++){await Frame();if(File.Exists(path))break;}Check(File.Exists(path),"Captured "+path);
    }
    Check(hud.GetComponentsInChildren<Text>().All(x=>x.material.shader.name=="UI/Default"),$"{enabled}: HUD text remains native");
   }
   passed=true;
  }
  catch(Exception e){error=e.ToString();Debug.LogException(e);}
  finally
  {
   stamina.currentStamina=previous;if(fixture)UnityEngine.Object.DestroyImmediate(fixture);Pixel360Rendering.Configure(asset,pixel);
   File.WriteAllText(Root+"/native.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,checks,restoredPixel=pixel,screen=new{Screen.width,Screen.height}},Newtonsoft.Json.Formatting.Indented));
  }
 }
}
