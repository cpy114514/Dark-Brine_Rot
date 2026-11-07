using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
public static class VerifySunCamera
{
 public static object Run()
 {
  var sun=UnityEngine.Object.FindFirstObjectByType<ProceduralSun>();if(!sun||!Camera.main)throw new Exception("Live sun and main camera required");
  var originalPosition=sun.transform.position;var originalScale=sun.transform.localScale;var main=Camera.main;
  Vector3 direction=(originalPosition-main.transform.position).normalized;
  void Place(Camera camera){float d=Mathf.Max(80,Mathf.Min(sun.maximumDistance,camera.farClipPlane*.72f));sun.transform.position=camera.transform.position+direction*d;sun.transform.localScale=Vector3.one*(d*sun.apparentSize);}
  var go=new GameObject("Owned sun camera regression",typeof(Camera));go.hideFlags=HideFlags.HideAndDontSave;var c=go.GetComponent<Camera>();c.enabled=false;c.fieldOfView=55;c.nearClipPlane=.1f;c.farClipPlane=6000;
  // Look directly at the old physical sun from above the island: this reproduces the floating orb.
  c.transform.position=originalPosition+new Vector3(0,170,-220);c.transform.LookAt(originalPosition);
  var rt=RenderTexture.GetTemporary(960,540,24);c.targetTexture=rt;var old=RenderTexture.active;
  void Capture(string name){c.Render();RenderTexture.active=rt;var image=new Texture2D(960,540,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(".codex/light-investigation/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
  Vector3 observed=Vector3.zero;
  Action<ScriptableRenderContext,Camera> before=(context,camera)=>{if(camera!=c)return;Place(camera);observed=sun.transform.position;};
  try
  {
   Capture("sun-before");RenderPipelineManager.beginCameraRendering+=before;Capture("sun-after");
   float alignment=Vector3.Dot((observed-c.transform.position).normalized,direction);float distance=Vector3.Distance(observed,c.transform.position);
   var result=new{passed=alignment>.99999f&&Mathf.Abs(distance-2200)<.1f,alignment,distance,source="Runtime reproduction of per-camera placement; no scene saved or player state changed"};File.WriteAllText(".codex/light-investigation/sun-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{RenderPipelineManager.beginCameraRendering-=before;sun.transform.position=originalPosition;sun.transform.localScale=originalScale;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
 public static object ApplyLive()
 {
  // Temporary bridge for this already-running Play session. The source component
  // owns the same callbacks after Unity next recompiles; domain reload drops this bridge.
  var sun=UnityEngine.Object.FindFirstObjectByType<ProceduralSun>();if(!Application.isPlaying||!sun)throw new Exception("Live sun required");
  Vector3 savedPosition=Vector3.zero,savedScale=Vector3.one;
  RenderPipelineManager.beginCameraRendering+=(context,camera)=>{
   if(!sun||camera.cameraType!=CameraType.SceneView||!Camera.main)return;
   savedPosition=sun.transform.position;savedScale=sun.transform.localScale;
   var direction=(savedPosition-Camera.main.transform.position).normalized;
   float d=Mathf.Max(80,Mathf.Min(sun.maximumDistance,camera.farClipPlane*.72f));sun.transform.position=camera.transform.position+direction*d;sun.transform.localScale=Vector3.one*(d*sun.apparentSize);
  };
  RenderPipelineManager.endCameraRendering+=(context,camera)=>{if(sun&&camera.cameraType==CameraType.SceneView){sun.transform.position=savedPosition;sun.transform.localScale=savedScale;}};
  SceneView.RepaintAll();return "Per-SceneView placement applied to current Play session without restarting gameplay";
 }
}
