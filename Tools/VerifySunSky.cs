using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class VerifySunSky
{
 const string W=".codex/sun-sky-fix";
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stopped verification required");
  var sun=UnityEngine.Object.FindFirstObjectByType<ProceduralSun>();if(!sun)throw new Exception("Authored sun missing");
  var oldDisk=Shader.GetGlobalVector("_BrineSolarDisk");var oldColor=Shader.GetGlobalColor("_BrineSolarColor");
  GameObject sky=null,wall=null,view=null;Material skyMat=null,wallMat=null;RenderTexture rt=null;var active=RenderTexture.active;
  try
  {
   view=new GameObject("Owned solar sky camera",typeof(Camera));view.hideFlags=HideFlags.HideAndDontSave;var c=view.GetComponent<Camera>();c.enabled=false;c.cullingMask=1<<30;c.fieldOfView=50;c.nearClipPlane=.1f;c.farClipPlane=2500;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;c.transform.position=new Vector3(5000,1000,5000);
   rt=RenderTexture.GetTemporary(640,360,24);c.targetTexture=rt;
   sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);sky.name="Owned sky regression dome";sky.hideFlags=HideFlags.HideAndDontSave;sky.layer=30;sky.transform.position=c.transform.position;sky.transform.localScale=Vector3.one*2000;sky.GetComponent<Collider>().enabled=false;
   skyMat=new Material(Shader.Find("DarkBrine/Procedural Sky"));skyMat.SetVector("_SunDirection",Vector3.forward);skyMat.SetFloat("_CloudStrength",0);skyMat.SetFloat("_CloudCoverage",0);skyMat.SetFloat("_SunGlow",0);skyMat.SetColor("_HorizonColor",new Color(.1f,.15f,.2f));skyMat.SetColor("_ZenithColor",new Color(.05f,.1f,.15f));sky.GetComponent<Renderer>().sharedMaterial=skyMat;
   Shader.SetGlobalColor("_BrineSolarColor",Color.white);
   Color32[] Capture(string name)
   {
    c.Render();RenderTexture.active=rt;var image=new Texture2D(640,360,TextureFormat.RGB24,false);try{image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();File.WriteAllBytes(W+"/"+name+".png",image.EncodeToPNG());return image.GetPixels32();}finally{UnityEngine.Object.DestroyImmediate(image);}
   }
   int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Math.Abs(x.r-y.r)+Math.Abs(x.g-y.g)+Math.Abs(x.b-y.b)).Count(x=>x>10);
   Shader.SetGlobalVector("_BrineSolarDisk",Vector4.zero);var off=Capture("sky-off");Shader.SetGlobalVector("_BrineSolarDisk",new Vector4(.0065f,1.2f,1,0));var on=Capture("sky-on");int visiblePixels=Difference(off,on);
   c.transform.position+=Vector3.right*180;Shader.SetGlobalVector("_BrineSolarDisk",Vector4.zero);var offSecond=Capture("second-view-off");Shader.SetGlobalVector("_BrineSolarDisk",new Vector4(.0065f,1.2f,1,0));var onSecond=Capture("second-view-on");int secondPixels=Difference(offSecond,onSecond);
   wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.hideFlags=HideFlags.HideAndDontSave;wall.layer=30;wall.transform.position=c.transform.position+Vector3.forward*12;wall.transform.localScale=new Vector3(4,4,.5f);wall.GetComponent<Collider>().enabled=false;wallMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));wallMat.SetColor("_BaseColor",new Color(.15f,.2f,.1f));wall.GetComponent<Renderer>().sharedMaterial=wallMat;
   Shader.SetGlobalVector("_BrineSolarDisk",Vector4.zero);var blockedOff=Capture("terrain-off");Shader.SetGlobalVector("_BrineSolarDisk",new Vector4(.0065f,1.2f,1,0));var blockedOn=Capture("terrain-on");int occludedPixels=Difference(blockedOff,blockedOn);
   wall.SetActive(false);Shader.SetGlobalVector("_BrineSolarDisk",new Vector4(.0065f,1.2f,0,0));var night=Capture("hidden-at-night");int hiddenPixels=Difference(offSecond,night);
   var shaderErrors=ShaderUtil.GetShaderMessages(skyMat.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
   var result=new{passed=visiblePixels>5&&secondPixels>5&&occludedPixels==0&&hiddenPixels==0&&!sun.GetComponent<Renderer>().enabled&&!sun.GetComponent<Collider>().enabled&&shaderErrors.Length==0,visiblePixels,secondPixels,occludedPixels,hiddenPixels,worldSphereEnabled=sun.GetComponent<Renderer>().enabled,worldColliderEnabled=sun.GetComponent<Collider>().enabled,shaderErrors};File.WriteAllText(W+"/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally
  {
   Shader.SetGlobalVector("_BrineSolarDisk",oldDisk);Shader.SetGlobalColor("_BrineSolarColor",oldColor);RenderTexture.active=active;if(sky)UnityEngine.Object.DestroyImmediate(sky);if(wall)UnityEngine.Object.DestroyImmediate(wall);if(view)UnityEngine.Object.DestroyImmediate(view);if(skyMat)UnityEngine.Object.DestroyImmediate(skyMat);if(wallMat)UnityEngine.Object.DestroyImmediate(wallMat);if(rt)RenderTexture.ReleaseTemporary(rt);
  }
 }
 public static object CaptureIsland()
 {
  var main=Camera.main;if(!main)throw new Exception("Main camera required");var go=new GameObject("Owned solar island camera",typeof(Camera));go.hideFlags=HideFlags.HideAndDontSave;var c=go.GetComponent<Camera>();c.CopyFrom(main);c.enabled=false;c.transform.position=new Vector3(247,45,442);c.transform.LookAt(new Vector3(234,42,478));var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24);c.targetTexture=rt;
  try{c.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(W+"/island-after.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);return W+"/island-after.png";}
  finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
}
