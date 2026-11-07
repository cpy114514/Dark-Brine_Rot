using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
public static class ReviewIslandDecoration
{
 const string W=".codex/island-decoration";
 public static object Prepare()
 {
  if(Application.isPlaying||EditorSceneManager.GetSceneManagerSetup().Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Clean stopped scenes required");
  File.WriteAllText(W+"/review-workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);return "Isolated visual review prepared";
 }
 public static object Restore()
 {
  if(Application.isPlaying)throw new Exception("Stop review first");
  foreach(var f in Directory.GetFiles(W+"/save-before"))if(!File.ReadAllBytes(f).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(f)))))throw new Exception("Player save changed");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"/review-workspace.json")));return new{restored=true,savesUnchanged=true};
 }
 static async Task Frame(){int f=Time.frameCount;var until=DateTime.UtcNow.AddSeconds(35);while(f==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>until)throw new Exception("Frame stalled");await Task.Delay(5);}}
 static async Task Capture(Camera c,string path)
 {
  bool done=false;
  Action<ScriptableRenderContext,Camera> handler=(context,camera)=>{if(camera!=c||done)return;var old=RenderTexture.active;var image=new Texture2D(c.targetTexture.width,c.targetTexture.height,TextureFormat.RGB24,false);try{RenderTexture.active=c.targetTexture;image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());done=true;}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}};
  RenderPipelineManager.endCameraRendering+=handler;try{for(int i=0;i<20&&!done;i++)await Frame();if(!done)throw new Exception("Camera did not render");}finally{RenderPipelineManager.endCameraRendering-=handler;}
 }
 public static object Start(){if(!Application.isPlaying||SceneManager.sceneCount!=2)throw new Exception("Isolated Play required");_ = Run();return "Running paired captures and ABBA benchmark";}
 static async Task Run()
 {
  GameObject go=null;RenderTexture target=null;var oldPipeline=QualitySettings.renderPipeline;UniversalRenderPipelineAsset pipeline=null;int sync=QualitySettings.vSyncCount,rate=Application.targetFrameRate;var runs=new List<object>();
  try
  {
   var layer=GameObject.Find("Island 1 - habitat detail compositions");if(!layer)throw new Exception("Decoration layer missing");
   pipeline=UnityEngine.Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);pipeline.renderScale=1;QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
   go=new GameObject("Owned decoration review camera",typeof(Camera));go.tag="MainCamera";var c=go.GetComponent<Camera>();c.fieldOfView=57;c.nearClipPlane=.1f;c.farClipPlane=1200;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   target=new RenderTexture(1920,1080,24);target.Create();c.targetTexture=target;
   var world=UnityEngine.Object.FindFirstObjectByType<WorldPerformanceManager>();for(int i=0;i<1200&&world&&!world.IsReady;i++)await Frame();
   var sites=Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(File.ReadAllText(W+"/sites.json"))["sites"];
   foreach(string type in new[]{"Coast","Woodland","Meadow","Scree"})
   {
    var site=sites.First(s=>(string)s["biome"]==type);var p=new Vector3((float)site["x"],(float)site["y"],(float)site["z"]);c.transform.position=p+new Vector3(9,4,-11);c.transform.LookAt(p+Vector3.up*.6f);
    foreach(bool enabled in new[]{false,true}){layer.SetActive(enabled);for(int f=0;f<25;f++)await Frame();await Capture(c,W+"/"+type+"-"+(enabled?"after":"before")+".png");}
   }
   var coast=sites.First(s=>(string)s["biome"]=="Coast");var center=new Vector3((float)coast["x"],(float)coast["y"],(float)coast["z"]);c.transform.position=center+new Vector3(9,4,-11);c.transform.LookAt(center+Vector3.up*.6f);
   foreach(bool enabled in new[]{false,true,true,false})
   {
    layer.SetActive(enabled);for(int f=0;f<120;f++)await Frame();var times=new List<double>();long draws=0,tris=0;
    for(int f=0;f<240;f++){await Frame();times.Add(Time.unscaledDeltaTime*1000);draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;}
    times.Sort();runs.Add(new{enabled,meanMs=times.Average(),p95Ms=times[(int)(times.Count*.95)],drawCalls=draws/240.0,triangles=tris/240.0,frames=times.Count});
   }
   layer.SetActive(true);File.WriteAllText(W+"/performance.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,hardware=SystemInfo.graphicsDeviceName,resolution="1920x1080",renderScale=1,warmupFrames=120,runs,scope="Unity Editor Play, isolated Environment and Lighting; matched coastal camera; no standalone GPU/VRAM measurement"},Newtonsoft.Json.Formatting.Indented));
  }
  catch(Exception e){File.WriteAllText(W+"/review-error.txt",e.ToString());}
  finally{QualitySettings.renderPipeline=oldPipeline;QualitySettings.vSyncCount=sync;Application.targetFrameRate=rate;if(go)UnityEngine.Object.DestroyImmediate(go);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(pipeline)UnityEngine.Object.DestroyImmediate(pipeline);EditorApplication.isPlaying=false;}
 }
}
