using System;using System.Threading.Tasks;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEditor;using Mavis;using Mavis.VisualStyles;
public static class VisualStyleV2OverlayProbe
{
 const string K="VisualStyleV2.Overlay";
 static async Task Frames(int n){int f=Time.frameCount+n;var limit=DateTime.UtcNow.AddSeconds(15);while(Time.frameCount<f){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Frame timeout");await Task.Delay(5);}}
 public static async Task<object> Open()
 {
  if(!Application.isPlaying||SessionState.GetBool(K,false))throw new Exception("Unowned Play required");
  SessionState.SetBool(K,true);var manager=VisualStyleManager.Instance;
  SessionState.SetString(K+".Style",manager.Current.id);SessionState.SetFloat(K+".Intensity",manager.Intensity);SessionState.SetFloat(K+".Time",Time.timeScale);
  SessionState.SetInt(K+".Cursor",(int)Cursor.lockState);SessionState.SetBool(K+".Visible",Cursor.visible);SessionState.SetBool(K+".Pixel",Pixel360Rendering.IsActive);
  var previous=QualitySettings.renderPipeline;SessionState.SetInt(K+".Pipeline",previous?previous.GetInstanceID():0);
  var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;SessionState.SetInt(K+".Source",source.GetInstanceID());Pixel360Rendering.ReleaseCurrent();
  var pipeline=UnityEngine.Object.Instantiate(source);pipeline.renderScale=1;pipeline.msaaSampleCount=1;QualitySettings.renderPipeline=pipeline;SessionState.SetInt(K+".Clone",pipeline.GetInstanceID());
  manager.Select("monochrome",false);manager.SetIntensity(.85f,false);Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  VisualStyleUIController.Open(manager.transform);
  var go=new GameObject("Native UI color proof",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));SessionState.SetInt(K+".Root",go.GetInstanceID());
  var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1700;GameUITheme.Scale(canvas);
  var patch=GameUITheme.Rect(go.transform,"Unchanged red indicator",Vector2.zero,new Vector2(80,44));patch.anchoredPosition=new Vector2(100,60);GameUITheme.Image(patch,Color.red);
  var label=GameUITheme.Label(go.transform,"Native UI color and text",24,new Vector2(170,60),new Vector2(480,44),TextAnchor.MiddleLeft);label.text="NATIVE UI / COLOR TEST";label.rectTransform.anchorMin=label.rectTransform.anchorMax=Vector2.zero;
  EditorApplication.ExecuteMenuItem("Window/General/Game");await Frames(10);
  return new {ready=true,style=manager.Current.id,cards=UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>().StyleButtons.Count,overlay=canvas.renderMode.ToString()};
 }
 public static async Task<object> Pixel(bool enabled)
 {
  if(!SessionState.GetBool(K,false))throw new Exception("Probe missing");var pipeline=EditorUtility.EntityIdToObject(SessionState.GetInt(K+".Clone",0)) as UniversalRenderPipelineAsset;
  Pixel360Rendering.Configure(pipeline,enabled);await Frames(8);return new{pixel=Pixel360Rendering.IsActive,style=VisualStyleManager.Instance.Current.id};
 }
 public static object HideProof()
 {
  var root=EditorUtility.EntityIdToObject(SessionState.GetInt(K+".Root",0)) as GameObject;if(root)root.SetActive(false);return new{hidden=true};
 }
 public static object Close()
 {
  if(!SessionState.GetBool(K,false))throw new Exception("Probe missing");var ui=UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>();if(ui)ui.Close();
  Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=EditorUtility.EntityIdToObject(SessionState.GetInt(K+".Pipeline",0)) as RenderPipelineAsset;
  var source=EditorUtility.EntityIdToObject(SessionState.GetInt(K+".Source",0)) as UniversalRenderPipelineAsset;if(SessionState.GetBool(K+".Pixel",false)&&source)Pixel360Rendering.Configure(source,true);
  var manager=VisualStyleManager.Instance;manager.Select(SessionState.GetString(K+".Style","realistic"),false);manager.SetIntensity(SessionState.GetFloat(K+".Intensity",.85f),false);
  Time.timeScale=SessionState.GetFloat(K+".Time",1);Cursor.lockState=(CursorLockMode)SessionState.GetInt(K+".Cursor",0);Cursor.visible=SessionState.GetBool(K+".Visible",true);
  foreach(var n in new[]{"Root","Clone"}){var obj=EditorUtility.EntityIdToObject(SessionState.GetInt(K+"."+n,0));if(obj)UnityEngine.Object.DestroyImmediate(obj);}
  foreach(var n in new[]{"Root","Clone","Pipeline","Source","Cursor"})SessionState.EraseInt(K+"."+n);
  foreach(var n in new[]{"Time","Intensity"})SessionState.EraseFloat(K+"."+n);SessionState.EraseString(K+".Style");SessionState.EraseBool(K+".Pixel");SessionState.EraseBool(K+".Visible");SessionState.EraseBool(K);
  return new{closed=true};
 }
}
