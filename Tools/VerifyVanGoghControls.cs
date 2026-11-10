using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;
using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Mavis;using Mavis.VisualStyles;
public static class VerifyVanGoghControls
{
 static async Task Frame(){int f=Time.frameCount;while(f==Time.frameCount){if(!Application.isPlaying)throw new Exception("Play interrupted");await Task.Delay(5);}}
 static Color32[] Capture(Camera camera,RenderTexture rt)
 {
  RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
  try{image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();return image.GetPixels32();}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
 }
 public static async Task<object> Run()
 {
  if(!Application.isPlaying||AdditiveSceneBootstrap.IsLoading)throw new Exception("Isolated loaded Play required");
  var p=VanGoghProfile.Default;string snapshot=JsonUtility.ToJson(p);float time=Time.timeScale,renderScale=UniversalRenderPipeline.asset.renderScale;
  var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.enabled)).ToArray();GameObject go=null;RenderTexture rt=null;var checks=new List<object>();
  try
  {
   Time.timeScale=0;UniversalRenderPipeline.asset.renderScale=1;go=new GameObject("Excluded brush tuning review",typeof(Camera));go.AddComponent<GameSaveExcluded>();var camera=go.GetComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;foreach(var c in cameras)if(c.c)c.c.enabled=false;
   var at=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>().transform.position+Vector3.up*3;camera.transform.position=at+new Vector3(-8,3,-12);camera.transform.LookAt(at);rt=new RenderTexture(960,540,24);rt.Create();
   VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.Select("van-gogh",false);VisualStyleManager.Instance.SetIntensity(.9f,false);await Frame();var baseline=Capture(camera,rt);
   foreach(string name in new[]{"brushSize","density","directionStrength","curvature","colorAbstraction","colorContrast","impasto","outline","temporalStability","intensity"})
   {
    JsonUtility.FromJsonOverwrite(snapshot,p);var field=typeof(VanGoghProfile).GetField(name);field.SetValue(p,name=="brushSize"?.7f:(float)field.GetValue(p)>.5f?.05f:.95f);
    // Inspector path: OnValidate increments the revision; the manager refreshes on LateUpdate.
    typeof(VanGoghProfile).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p,null);await Frame();await Frame();var changed=Capture(camera,rt);int different=0,maximum=0;
    for(int i=0;i<baseline.Length;i++){int d=Math.Max(Math.Abs(baseline[i].r-changed[i].r),Math.Max(Math.Abs(baseline[i].g-changed[i].g),Math.Abs(baseline[i].b-changed[i].b)));if(d>1)different++;maximum=Math.Max(d,maximum);}
    checks.Add(new{name,differentPixels=different,maximumChannelDifference=maximum,effective=different>0});
   }
   JsonUtility.FromJsonOverwrite(snapshot,p);var cycles=new List<int>();
   for(int i=0;i<10;i++){VisualStyleManager.Instance.Select("van-gogh",false);await Frame();Capture(camera,rt);VisualStyleManager.Instance.Select("realistic",false);await Frame();Capture(camera,rt);await Frame();await Frame();cycles.Add(Resources.FindObjectsOfTypeAll<Material>().Count(m=>m.shader&&m.shader.name=="Hidden/DarkBrine/VanGoghSkinAnchor"));}
   File.WriteAllText(".codex/van-gogh-2/controls.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,skinAnchorMaterialsAfterEachExit=cycles,allEffective=checks.All(c=>(bool)c.GetType().GetProperty("effective").GetValue(c))},Newtonsoft.Json.Formatting.Indented));return new{checks,cycles};
  }
  finally{JsonUtility.FromJsonOverwrite(snapshot,p);VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.RestorePreference();Time.timeScale=time;UniversalRenderPipeline.asset.renderScale=renderScale;foreach(var c in cameras)if(c.c)c.c.enabled=c.enabled;if(go)UnityEngine.Object.DestroyImmediate(go);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
 }
}
