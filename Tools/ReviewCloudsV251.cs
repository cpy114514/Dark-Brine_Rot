using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
public static class ReviewCloudsV251
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 public static object MotionAndQuality()
 {
  var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var sky=(GameObject)typeof(OceanWorld).GetField("skyDome",F).GetValue(ocean);
  var renderer=sky.GetComponent<Renderer>();var source=renderer.sharedMaterial;int layer=sky.layer;var originalSpeed=source.GetFloat("_CloudSpeed");
  string folder=".codex/clouds-20261024/final-motion";if(Directory.Exists(folder))throw new Exception("Preserve previous take");Directory.CreateDirectory(folder);
  int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
  var go=new GameObject("Temporary cloud motion/quality camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
  var extra=camera.GetUniversalAdditionalCameraData();extra.renderPostProcessing=Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing;
  camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.Euler(-36,15,0));camera.fieldOfView=75;camera.cullingMask=1<<31;sky.layer=31;
  var rt=new RenderTexture(640,360,24);rt.Create();camera.targetTexture=rt;var pixels=new Texture2D(640,360,TextureFormat.RGB24,false);var material=new Material(source);renderer.sharedMaterial=material;
  var cases=new List<object>();var differences=new List<float>();byte[] previous=null;
  byte[] Render(string name)
  {
   var active=RenderTexture.active;try{camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);if(name!=null)File.WriteAllBytes(folder+"/"+name+".png",pixels.EncodeToPNG());return pixels.GetRawTextureData().ToArray();}finally{RenderTexture.active=active;}
  }
  try
  {
   foreach(int quality in new[]{2,4,5})foreach(float coverage in new[]{0f,.62f,.96f})
   {
    material.SetFloat("_CloudDetail",quality);material.SetFloat("_CloudCoverage",coverage);material.SetFloat("_CloudStrength",quality==2?.42f:quality==4?.72f:.8f);
    var rgb=Render("quality-"+quality+"-"+Mathf.RoundToInt(coverage*100));double mean=rgb.Average(b=>(double)b);
    if(mean<5||mean>250)throw new Exception("Invalid daylight cloud output");cases.Add(new{quality,coverage,mean});
   }
   material.CopyPropertiesFromMaterial(source);
   for(int i=0;i<96;i++)
   {
    material.SetFloat("_CloudTimeOffset",i/12f);var rgb=Render("motion-"+i.ToString("D4"));
    if(previous!=null){double sum=0;for(int j=0;j<rgb.Length;j++)sum+=Math.Abs(rgb[j]-previous[j]);differences.Add((float)(sum/rgb.Length));}previous=rgb;
   }
  }
  finally
  {
   renderer.sharedMaterial=source;sky.layer=layer;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(go);
  }
  var checks=new{shaderCompiles=!ShaderUtil.ShaderHasError(source.shader),allNineQualityWeatherViews=cases.Count==9,moving= differences.Max()>.01f,noTemporalPop=differences.Max()<8f,sourceRestored=renderer.sharedMaterial==source&&sky.layer==layer&&source.GetFloat("_CloudSpeed")==originalSpeed,cameraCleanup=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==cameras};
  bool passed=checks.shaderCompiles&&checks.allNineQualityWeatherViews&&checks.moving&&checks.noTemporalPop&&checks.sourceRestored&&checks.cameraCleanup;
  var result=new{passed,checks,cases,maximumMeanByteChange=differences.Max(),differences,frames=96,fps=12,scope="Actual native shader renders with material-local clock advanced8s; world/save/pause state does not advance. Not a gameplay-FPS or arbitrary-sun-direction certificate."};
  File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));if(!passed)throw new Exception("Motion/quality validation failed");return new{passed,checks,folder};
 }
 public static object Prepare()
 {
  string path="Assets/Diagnostics/CloudReviewV251";
  if(!AssetDatabase.IsValidFolder("Assets/Diagnostics"))AssetDatabase.CreateFolder("Assets","Diagnostics");
  if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder("Assets/Diagnostics","CloudReviewV251");
  string file=path+"/CloudBefore.shader";
  if(!File.Exists(file))File.WriteAllText(file,File.ReadAllText(".codex/clouds-20261024/before-ProceduralSky.shader").Replace("Shader \"DarkBrine/Procedural Sky\"","Shader \"Hidden/DarkBrine/CloudBeforeV251\""));
  AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);return new{path=file};
 }
 public static object Capture(string label,bool benchmark=false)
 {
  var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();if(!ocean)throw new Exception("Current ocean missing");
  var sky=(GameObject)typeof(OceanWorld).GetField("skyDome",F).GetValue(ocean);
  var renderer=sky.GetComponent<Renderer>();var source=renderer.sharedMaterial;
  var folder=".codex/clouds-20261024/"+label;if(Directory.Exists(folder))throw new Exception("Preserve take");Directory.CreateDirectory(folder);
  var go=new GameObject("Temporary native cloud review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
  var extra=camera.GetUniversalAdditionalCameraData();var sourceExtra=Camera.main.GetUniversalAdditionalCameraData();extra.renderPostProcessing=sourceExtra.renderPostProcessing;extra.volumeLayerMask=sourceExtra.volumeLayerMask;
  camera.fieldOfView=75;camera.transform.position=Camera.main.transform.position;camera.cullingMask=1<<31;
  int oldLayer=sky.layer;sky.layer=31;var target=new RenderTexture(960,540,24);target.Create();camera.targetTexture=target;
  var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);var material=new Material(source);renderer.sharedMaterial=material;
  var rows=new List<object>();
  void Save(string name)
  {
   var active=RenderTexture.active;try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/"+name+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;}
  }
  try
  {
   var parameters=new{coverage=source.GetFloat("_CloudCoverage"),detail=source.GetFloat("_CloudDetail"),distance=source.GetFloat("_CloudRenderDistance"),speed=source.GetFloat("_CloudSpeed"),strength=source.GetFloat("_CloudStrength"),daylight=source.GetFloat("_Daylight"),time=Time.time,position=new[]{camera.transform.position.x,camera.transform.position.y,camera.transform.position.z}};
   camera.transform.rotation=Quaternion.Euler(-22,15,0);Save("current-horizon");camera.transform.rotation=Quaternion.Euler(-65,15,0);Save("current-up");
   material.SetColor("_HorizonColor",new Color(.67f,.82f,.94f));material.SetColor("_ZenithColor",new Color(.18f,.47f,.79f));material.SetColor("_CloudColor",new Color(.96f,.98f,1));material.SetColor("_SunColor",new Color(1,.96f,.87f));material.SetFloat("_Daylight",1);material.SetFloat("_NightFactor",0);material.SetVector("_SunDirection",new Vector3(.32f,.78f,.49f).normalized);
   foreach(float coverage in new[]{.62f,.85f,.96f})
   {
    material.SetFloat("_CloudCoverage",coverage);camera.transform.rotation=Quaternion.Euler(-38,15,0);Save("day-"+Mathf.RoundToInt(coverage*100));
   }
   material.SetFloat("_Daylight",.25f);material.SetFloat("_NightFactor",.15f);material.SetColor("_HorizonColor",new Color(.65f,.32f,.24f));material.SetColor("_ZenithColor",new Color(.12f,.18f,.36f));material.SetColor("_CloudColor",new Color(.95f,.57f,.38f));material.SetVector("_SunDirection",new Vector3(.5f,.12f,.85f).normalized);Save("sunset");
   material.SetFloat("_Daylight",0);material.SetFloat("_NightFactor",1);material.SetColor("_HorizonColor",new Color(.035f,.055f,.10f));material.SetColor("_ZenithColor",new Color(.008f,.015f,.04f));material.SetColor("_CloudColor",new Color(.11f,.16f,.30f));Save("night");
   if(benchmark)
   {
    var old=Shader.Find("Hidden/DarkBrine/CloudBeforeV251");if(!old)throw new Exception("Prepare comparison shader first");
    var previous=new Material(source){shader=old};
    try
    {
     foreach(float coverage in new[]{.62f,.96f})foreach(int quality in new[]{2,5})
     {
      var timings=new List<double>[] {new List<double>(),new List<double>()};var watch=new System.Diagnostics.Stopwatch();
      previous.SetFloat("_CloudCoverage",coverage);material.CopyPropertiesFromMaterial(previous);material.shader=source.shader;
      previous.SetFloat("_CloudDetail",quality);material.SetFloat("_CloudDetail",quality);camera.transform.rotation=Quaternion.Euler(-38,15,0);
      for(int n=0;n<32;n++)for(int variant=0;variant<2;variant++)
      {
       renderer.sharedMaterial=variant==0?previous:material;var active=RenderTexture.active;
       try{watch.Restart();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1,1),0,0,false);watch.Stop();if(n>=8)timings[variant].Add(watch.Elapsed.TotalMilliseconds);}finally{RenderTexture.active=active;}
      }
      rows.Add(new{coverage,quality,before=timings[0],after=timings[1],scope="Alternating synchronized960x540 sky-only camera renders plus1px GPU readback, warmed8 iterations; includes URP CPU and synchronization, not isolated GPU time or gameplay FPS."});
     }
    }
    finally{UnityEngine.Object.DestroyImmediate(previous);}
   }
   var data=new{parameters,shaderError=ShaderUtil.ShaderHasError(source.shader),measurements=rows};File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return new{folder,data};
  }
  finally
  {
   renderer.sharedMaterial=source;sky.layer=oldLayer;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(go);
  }
 }
}
