using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;
using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEngine.Profiling;using UnityEditor;using Mavis;using Mavis.VisualStyles;
public static class ReviewVanGogh2
{
 static async Task Frame(){int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(60);while(f==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Interrupted review");await Task.Delay(5);}}
 public static object Load(){if(!Application.isPlaying)throw new Exception("Use isolated Play");SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Main.unity");return "Loading actual island";}
 static Color32[] Capture(Camera camera,RenderTexture rt,string path)
 {
  RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
  var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
  try{im.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);im.Apply();if(path!=null)File.WriteAllBytes(path,im.EncodeToPNG());return im.GetPixels32();}
  finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(im);}
 }
 public static async Task<object> Static(string label="iteration1",int msaa=1)
 {
  if(!Application.isPlaying||AdditiveSceneBootstrap.IsLoading)throw new Exception("Loaded isolated Play required");
  string dir=".codex/van-gogh-2/"+label;if(Directory.Exists(dir))throw new Exception("Preserve earlier take");Directory.CreateDirectory(dir);
  var errors=new List<string>();void Log(string s,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(s);}
  Application.logMessageReceived+=Log;var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.enabled)).ToArray();
  float time=Time.timeScale,renderScale=UniversalRenderPipeline.asset.renderScale;GameObject probe=null;RenderTexture rt=null;var checks=new List<object>();
  try
  {
   var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
   if(arrival){arrival.enabled=false;arrival.RestoreProgress(true,true);var at=arrival.shore.position;var island=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();if(island.GetComponent<Collider>().Raycast(new Ray(at+Vector3.up*100,Vector3.down),out var hit,300))at=hit.point;player.RestoreSavedPose(at+Vector3.up*(-player.LowestFootWorldOffset+.05f),Quaternion.Euler(0,180,0));}
   player.ExternalControlLock=player.ExternalMovementLock=false;Time.timeScale=0;UniversalRenderPipeline.asset.renderScale=1;
   player.CharacterAnimator.speed=1;player.CharacterAnimator.Play("Base Layer.Locomotion",0,.2f);player.CharacterAnimator.Update(0);
   var center=player.GetComponent<CharacterController>().bounds.center;
   var main=Camera.main;probe=new GameObject("Excluded painting review",typeof(Camera));probe.AddComponent<GameSaveExcluded>();var camera=probe.GetComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.fieldOfView=55;foreach(var saved in cameras)if(saved.c)saved.c.enabled=false;
   rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.antiAliasing=msaa;camera.allowMSAA=msaa>1;rt.Create();
   var views=new List<(string,Vector3,Vector3)>{("coast",center+new Vector3(-8,3,-12),center+Vector3.up*.2f),("ocean",center+new Vector3(0,7,-4),center+new Vector3(0,8,-90))};
   var tree=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.name.ToLowerInvariant().Contains("tree")&&r.name.Contains("LOD0")&&r.bounds.size.y>4).OrderBy(r=>r.bounds.size.magnitude).FirstOrDefault();if(tree){var at=tree.bounds.center;float radius=tree.bounds.extents.magnitude;views.Add(("vegetation",at+new Vector3(-.8f,.2f,-1).normalized*radius*2.6f,at));}
   var ground=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();if(ground.GetComponent<Collider>().Raycast(new Ray(new Vector3(288,150,346),Vector3.down),out var grass,400))views.Add(("grass",grass.point+new Vector3(-8,4,-12),grass.point+Vector3.up*2));
   var boss=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>();if(boss){var at=boss.transform.position+Vector3.up*3;views.Add(("boss",at+new Vector3(-11,4,-16),at));}
   foreach(var v in views)
   {
    camera.transform.position=v.Item2;camera.transform.LookAt(v.Item3);
    foreach(var mode in new[]{"realistic","legacy","painting"})
    {
     VanGoghPainting.LegacyComparison=mode=="legacy";VisualStyleManager.Instance.Select(mode=="realistic"?"realistic":"van-gogh",false);VisualStyleManager.Instance.SetIntensity(.9f,false);await Frame();
     Capture(camera,rt,dir+"/"+v.Item1+"-"+mode+".png");
    }
   }
   camera.transform.position=views[0].Item2;camera.transform.LookAt(views[0].Item3);
   foreach(string mode in new[]{"realistic","dark-fairytale","origami","monochrome","ink-wash"})
   {
    VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.Select(mode,false);await Frame();var a=Capture(camera,rt,null);
    VisualStyleManager.Instance.Select("van-gogh",false);await Frame();Capture(camera,rt,null);
    VisualStyleManager.Instance.Select(mode,false);await Frame();var b=Capture(camera,rt,null);int diff=0,max=0;
    for(int i=0;i<a.Length;i++){int d=Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));if(d>0)diff++;max=Math.Max(d,max);}
    checks.Add(new{mode,differentPixels=diff,maximumChannelDifference=max,keywordDisabled=!Shader.IsKeywordEnabled("VAN_GOGH_SURFACE")});
   }
   VisualStyleManager.Instance.Select("realistic",false);await Frame();var controlA=Capture(camera,rt,null);VisualStyleManager.Instance.Select("realistic",false);await Frame();var controlB=Capture(camera,rt,null);
   int controlDifferent=0,controlMaximum=0;for(int i=0;i<controlA.Length;i++){int d=Math.Max(Math.Abs(controlA[i].r-controlB[i].r),Math.Max(Math.Abs(controlA[i].g-controlB[i].g),Math.Abs(controlA[i].b-controlB[i].b)));if(d>0)controlDifferent++;controlMaximum=Math.Max(controlMaximum,d);}checks.Add(new{mode="realistic-repeat-control",differentPixels=controlDifferent,maximumChannelDifference=controlMaximum});
   var result=new{errors,checks,views=views.Select(v=>new{name=v.Item1,position=v.Item2.ToString(),target=v.Item3.ToString()}),gpu=SystemInfo.graphicsDeviceName};File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.RestorePreference();Time.timeScale=time;UniversalRenderPipeline.asset.renderScale=renderScale;Application.logMessageReceived-=Log;foreach(var c in cameras)if(c.c)c.c.enabled=c.enabled;if(probe)UnityEngine.Object.DestroyImmediate(probe);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
 }
 public static async Task<object> Performance(string label="performance1")
 {
  if(!Application.isPlaying||AdditiveSceneBootstrap.IsLoading)throw new Exception("Isolated loaded Play required");
  string dir=".codex/van-gogh-2/"+label;if(Directory.Exists(dir))throw new Exception("Preserve earlier take");Directory.CreateDirectory(dir);
  var states=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.enabled)).ToArray();var p=VanGoghProfile.Default;var quality=p.quality;float scale=Time.timeScale,renderScale=UniversalRenderPipeline.asset.renderScale;GameObject go=null;RenderTexture rt=null;var rows=new List<object>();ProfilingSampler fullCamera=null;
  try
  {
   Time.timeScale=0;UniversalRenderPipeline.asset.renderScale=1;var main=Camera.main;go=new GameObject("Excluded 1080p GPU review",typeof(Camera));go.AddComponent<GameSaveExcluded>();var camera=go.GetComponent<Camera>();camera.CopyFrom(main);camera.fieldOfView=55;
   var at=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>().transform.position+Vector3.up*3;camera.transform.position=at+new Vector3(-11,4,-16);camera.transform.LookAt(at);
   foreach(var s in states)if(s.c)s.c.enabled=false;rt=new RenderTexture(1920,1080,24);rt.Create();camera.targetTexture=rt;camera.enabled=true;
   await Frame();
   var metadata=typeof(UniversalRenderPipeline).GetNestedType("CameraMetadataCache",System.Reflection.BindingFlags.NonPublic);
   var entry=metadata.GetMethod("GetCached",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public).Invoke(null,new object[]{camera});
   fullCamera=(ProfilingSampler)entry.GetType().GetField("sampler").GetValue(entry);fullCamera.enableRecording=true;
   foreach(var mode in new[]{"realistic","legacy","Economy","Balanced","Fine","realistic-return"})
   {
    bool isPainting=mode=="Economy"||mode=="Balanced"||mode=="Fine";
    if(isPainting)p.quality=(VanGoghProfile.Quality)Enum.Parse(typeof(VanGoghProfile.Quality),mode);
    VanGoghPainting.LegacyComparison=mode=="legacy";VisualStyleManager.Instance.Select(isPainting||mode=="legacy"?"van-gogh":"realistic",false);VisualStyleManager.Instance.SetIntensity(.9f,false);
    for(int i=0;i<45;i++)await Frame();
    var gpu=new List<float[]>();var frames=new List<object>();var timings=new FrameTiming[4];
    long graphics=Profiler.GetAllocatedMemoryForGraphicsDriver();
    for(int i=0;i<60;i++)
    {
     FrameTimingManager.CaptureFrameTimings();await Frame();var v=VanGoghPainting.LastGpuMilliseconds;gpu.Add(new[]{v.x,v.y,v.z});
     uint n=FrameTimingManager.GetLatestTimings(1,timings);frames.Add(new{cameraGpuMilliseconds=fullCamera.gpuElapsedTime,frameTimingAvailable=n>0,gpuFrameTime=n>0?timings[0].gpuFrameTime:0,cpuFrameTime=n>0?timings[0].cpuFrameTime:0});
    }
    rows.Add(new{mode,width=rt.width,height=rt.height,renderScale=UniversalRenderPipeline.asset.renderScale,msaa=UniversalRenderPipeline.asset.msaaSampleCount,keyword=Shader.IsKeywordEnabled("VAN_GOGH_SURFACE"),skinDraws=isPainting?VanGoghPainting.VisibleSkinDraws:0,workingTextureBytes=isPainting?VanGoghPainting.WorkingTextureBytes:0,graphicsDriverAllocatedBytes=graphics,gpuPassMilliseconds=isPainting?gpu:null,frameTiming=frames});
    File.WriteAllText(dir+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{mode,rows=rows.Count}));
   }
   var result=new{gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),driver=SystemInfo.graphicsDeviceVersion,scope="Editor, fixed actual boss view, live Game camera1920x1080, time frozen. Frame timings include Editor overhead. GPU pass readings exclude depth copy and in-place water/sky. Driver memory is Unity estimate, not a dedicated VRAM resident counter.",rows};File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{complete=true,dir};
  }
  finally{if(fullCamera!=null)fullCamera.enableRecording=false;p.quality=quality;VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.RestorePreference();Time.timeScale=scale;UniversalRenderPipeline.asset.renderScale=renderScale;foreach(var s in states)if(s.c)s.c.enabled=s.enabled;if(go)UnityEngine.Object.DestroyImmediate(go);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
 }
}
