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
using UnityEngine.SceneManagement;
using Mavis;
public static class ReviewIslandVisualQuality
{
 const string W=".codex/island-visual-quality";
 static async Task Frame(){int frame=Time.frameCount;var until=DateTime.UtcNow.AddSeconds(40);while(Time.frameCount==frame){if(!Application.isPlaying||DateTime.UtcNow>until)throw new Exception("Frame stalled");await Task.Delay(5);}}
 static async Task Capture(Camera c,string path)
 {
  bool done=false;Action<ScriptableRenderContext,Camera> action=(context,camera)=>{if(camera!=c||done)return;var old=RenderTexture.active;var resolved=RenderTexture.GetTemporary(c.targetTexture.width,c.targetTexture.height,0);var image=new Texture2D(resolved.width,resolved.height,TextureFormat.RGB24,false);try{Graphics.Blit(c.targetTexture,resolved);RenderTexture.active=resolved;image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());done=true;}finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(resolved);UnityEngine.Object.DestroyImmediate(image);}};
  RenderPipelineManager.endCameraRendering+=action;try{for(int i=0;i<30&&!done;i++)await Frame();if(!done)throw new Exception("Capture did not render");}finally{RenderPipelineManager.endCameraRendering-=action;}
 }
 public static object Start(){if(!Application.isPlaying||SceneManager.sceneCount!=2)throw new Exception("Isolated Play required");_ = Run();return "Rendering final viewpoints, dusk/night and performance";}
 static async Task Run()
 {
  GameObject go=null;RenderTexture target=null;var samples=new List<object>();int rate=Application.targetFrameRate,sync=QualitySettings.vSyncCount,msaa=0;string error=null;
  try
  {
   var ground=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled).GetComponent<MeshCollider>();
   (string name,Vector2 xz,Vector3 offset,Vector3 look)[] views=new[]{("coast",new Vector2(267,340),new Vector3(0,1.7f,0),new Vector3(-36,-2,32)),("meadow",new Vector2(247,442),new Vector3(0,1.8f,0),new Vector3(-25,3,35)),("woodland",new Vector2(102,714),new Vector3(8,2,-10),new Vector3(0,1,0)),("ridge",new Vector2(62,640),new Vector3(0,1.8f,0),new Vector3(-90,-15,130)),("ground",new Vector2(217,423),new Vector3(0,1.4f,0),new Vector3(1,0,3)),("west-shore",new Vector2(-294,578),new Vector3(0,1.8f,0),new Vector3(-160,1,190)),("headland-view",new Vector2(130,650),new Vector3(0,1.8f,0),new Vector3(-450,-18,260)),("north-shore",new Vector2(177,865),new Vector3(0,1.8f,0),new Vector3(180,-3,420))};
   go=new GameObject("Owned final coastal review camera",typeof(Camera));go.tag="MainCamera";var c=go.GetComponent<Camera>();c.fieldOfView=58;c.nearClipPlane=.12f;c.farClipPlane=6000;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   var asset=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;msaa=asset.msaaSampleCount;target=new RenderTexture(1600,900,24){antiAliasing=msaa};target.Create();c.targetTexture=target;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
   var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.clockRuns=false;cycle.startHour=9;cycle.minimumCloudCoverage=.64f;cycle.maximumCloudCoverage=.64f;
   typeof(DayNightCycle).GetField("elapsedGameHours",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(cycle,0f);
   var manager=UnityEngine.Object.FindFirstObjectByType<WorldPerformanceManager>();for(int i=0;i<1200&&manager&&!manager.IsReady;i++)await Frame();
   void Position(int index){var v=views[index];if(!ground.Raycast(new Ray(new Vector3(v.xz.x,ground.bounds.max.y+5,v.xz.y),Vector3.down),out var h,ground.bounds.size.y+20))throw new Exception("Viewpoint lost terrain support");c.transform.position=h.point+v.offset;c.transform.LookAt(h.point+v.look);}
   for(int view=0;view<views.Length;view++){Position(view);for(int f=0;f<45;f++)await Frame();await Capture(c,W+"/"+views[view].name+"-final.png");}
   // Explicitly check the same island at dusk and night, rather than assuming the daytime tuning transfers.
   foreach(float hour in new[]{18f,22f}){cycle.startHour=hour;Position(0);for(int f=0;f<30;f++)await Frame();await Capture(c,W+"/coast-"+hour+"h.png");}cycle.startHour=9;
   c.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);target=new RenderTexture(1920,1080,24){antiAliasing=asset.msaaSampleCount};target.Create();c.targetTexture=target;
   foreach(int view in new[]{1,2,5})
   {
    Position(view);for(int f=0;f<120;f++)await Frame();var times=new List<double>();long draws=0,triangles=0;
    for(int f=0;f<240;f++){await Frame();times.Add(Time.unscaledDeltaTime*1000);draws+=UnityStats.drawCalls;triangles+=UnityStats.triangles;}times.Sort();samples.Add(new{view=views[view].name,meanMs=times.Average(),p95Ms=times[(int)(times.Count*.95)],drawCalls=draws/240.0,triangles=triangles/240.0});
   }
  }
  catch(Exception e){error=e.ToString();}
  finally
  {
   if(go)UnityEngine.Object.DestroyImmediate(go);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}Application.targetFrameRate=rate;QualitySettings.vSyncCount=sync;
   File.WriteAllText(W+"/final-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=error==null,error,samples,msaa,hardware=SystemInfo.graphicsDeviceName,scope="Editor Play, Environment + Lighting, 1920x1080, 120 warm-up and 240 measured frames per view; no standalone/GPU isolation"},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;
  }
 }
}

