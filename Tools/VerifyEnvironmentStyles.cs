using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;
public static class VerifyEnvironmentStyles
{
 const string Root=".codex/environment-styles";
 public static object Run()
 {
  Directory.CreateDirectory(Root);var checks=new List<string>();void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add(s);}
  var manager=VisualStyleManager.Instance;string oldStyle=manager.Current.id;float oldIntensity=manager.Intensity;
  var quality=QualitySettings.renderPipeline;var original=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
  var pipeline=UnityEngine.Object.Instantiate(original);pipeline.renderScale=1;pipeline.msaaSampleCount=1;pipeline.upscalingFilter=UpscalingFilterSelection.Point;
  var pixelFlag=Shader.GetGlobalFloat("_PixelStyleUI");bool desiredPixel=PlayerPrefs.GetInt("DarkBrine.Settings.Pixel360",0)==1;
  GameObject root=null;Material sky=null,water=null;Mesh mesh=null;RenderTexture target=null;bool passed=false;
  var differences=new Dictionary<string,double>();
  try
  {
   Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=pipeline;root=new GameObject("Temporary sky and sea style verification");root.AddComponent<GameSaveExcluded>();
   var go=new GameObject("Environment verification camera",typeof(Camera));go.transform.SetParent(root.transform);var camera=go.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;camera.farClipPlane=26000;camera.fieldOfView=68;camera.transform.position=new Vector3(2000,18,2000);camera.transform.rotation=Quaternion.Euler(-3,25,0);camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   var sourceSky=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).First(m=>m&&m.shader.name=="DarkBrine/Procedural Sky");sky=new Material(sourceSky);sky.SetFloat("_CloudCoverage",.72f);sky.SetFloat("_CloudStrength",.8f);sky.SetFloat("_Daylight",1);sky.SetFloat("_NightFactor",0);sky.SetVector("_SunDirection",new Vector4(.4f,.8f,.3f,0));
   var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.transform.SetParent(root.transform);sphere.transform.position=camera.transform.position;sphere.transform.localScale=Vector3.one*40000;sphere.layer=30;sphere.GetComponent<Renderer>().sharedMaterial=sky;
   var sourceWater=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).First(m=>m&&m.shader.name=="DarkBrine/Procedural Ocean");water=new Material(sourceWater);water.SetFloat("_ShipHullMaskEnabled",0);water.SetInt("_WakeCount",0);
   var sea=new GameObject("Ocean shader fixture",typeof(MeshFilter),typeof(MeshRenderer));sea.transform.SetParent(root.transform);sea.transform.position=new Vector3(2000,0,2000);sea.layer=30;sea.GetComponent<Renderer>().sharedMaterial=water;
   int side=101;var vertices=new Vector3[side*side];var triangles=new int[(side-1)*(side-1)*6];for(int z=0;z<side;z++)for(int x=0;x<side;x++)vertices[z*side+x]=new Vector3((x/100f-.5f)*5000,0,(z/100f-.5f)*5000);int t=0;for(int z=0;z<100;z++)for(int x=0;x<100;x++){int a=z*side+x;triangles[t++]=a;triangles[t++]=a+side;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=a+side;triangles[t++]=a+side+1;}mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();mesh.RecalculateNormals();sea.GetComponent<MeshFilter>().sharedMesh=mesh;
   target=new RenderTexture(960,540,24);target.Create();camera.targetTexture=target;
   Color32[] Capture(string name)
   {
    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.StandardRequest{destination=target});var prev=RenderTexture.active;var read=new Texture2D(960,540,TextureFormat.RGB24,false);try{RenderTexture.active=target;read.ReadPixels(new Rect(0,0,960,540),0,0);read.Apply();File.WriteAllBytes(Root+"/"+name+".png",read.EncodeToPNG());return read.GetPixels32();}finally{RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(read);}
   }
   double Difference(Color32[] a,Color32[] b,int y0,int y1){double sum=0;for(int y=y0;y<y1;y++)for(int x=40;x<920;x++){int i=y*960+x;sum+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);}return sum/((y1-y0)*880*3);}
   foreach(bool pixel in new[]{false,true})
   {
    Pixel360Rendering.Configure(pipeline,pixel);pipeline.renderScale=pixel?(180.001f/540):1;
    manager.SetIntensity(.85f,false);manager.Select("realistic",false);var realistic=Capture((pixel?"pixel":"native")+"-realistic");
    manager.Select("dark-fairytale",false);var dark=Capture((pixel?"pixel":"native")+"-dark-fairytale");
    foreach(var range in new[]{(name:"clouds",min:340,max:510),(name:"sea",min:30,max:170)}){double d=Difference(realistic,dark,range.min,range.max);differences[(pixel?"pixel":"native")+"-"+range.name]=d;Check(d>1,"Observable style change: "+pixel+" "+range.name);}
    manager.SetIntensity(.4f,false);var light=Capture((pixel?"pixel":"native")+"-intensity-40");Check(Difference(light,dark,30,510)>.2,"Intensity adjusts both environment shaders: "+pixel);
    manager.Select("realistic",false);var restored=Capture((pixel?"pixel":"native")+"-restored");Check(Difference(restored,realistic,0,540)<.05,"Realistic restores the original rendered palette: "+pixel);
   }
   sky.SetFloat("_Daylight",.08f);sky.SetFloat("_NightFactor",.95f);sky.SetColor("_HorizonColor",new Color(.035f,.055f,.09f));sky.SetColor("_ZenithColor",new Color(.008f,.018f,.04f));sky.SetColor("_CloudColor",new Color(.08f,.11f,.17f));manager.Select("realistic",false);var nightReal=Capture("pixel-night-realistic");manager.Select("dark-fairytale",false);manager.SetIntensity(.85f,false);var nightDark=Capture("pixel-night-dark-fairytale");Check(Difference(nightReal,nightDark,340,510)>.1,"Night cloud palette responds to Dark Fairytale"); Check(Difference(nightReal,nightDark,30,170)>.1,"Night sea responds to Dark Fairytale"); Check(!ShaderUtil.ShaderHasError(sky.shader)&&!ShaderUtil.ShaderHasError(water.shader),"Both environment shaders compile");
   passed=true;return new{passed,checks,differences};
  }
  finally
  {
   Pixel360Rendering.ReleaseCurrent();Shader.SetGlobalFloat("_PixelStyleUI",pixelFlag);manager.SetIntensity(oldIntensity,false);manager.Select(oldStyle,false);QualitySettings.renderPipeline=quality;
   if(root)UnityEngine.Object.DestroyImmediate(root);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(mesh)UnityEngine.Object.DestroyImmediate(mesh);if(water)UnityEngine.Object.DestroyImmediate(water);if(sky)UnityEngine.Object.DestroyImmediate(sky);UnityEngine.Object.DestroyImmediate(pipeline);
   // Reconnect the selected pixel mode after the Editor's script reload, without changing saved settings.
   if(desiredPixel&&!Pixel360Rendering.IsActive){typeof(UnityEngine.UI.Graphic).GetField("s_DefaultUI",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,Canvas.GetDefaultCanvasMaterial());Pixel360Rendering.Configure(original,true);}
   File.WriteAllText(Root+"/native.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,differences},Newtonsoft.Json.Formatting.Indented));
  }
 }
}
