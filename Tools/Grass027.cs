using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;

// Ephemeral native render probe; no AssetDatabase writes or preference changes.
public static class Grass027
{
 const string Root=".codex/grass027";
 static async Task Frames(int n=3){int f=Time.frameCount+n;var end=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount<f){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frames stalled");await Task.Delay(5);}}
 static long Triangles(Mesh m)=>Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3);
 static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(m=>m.gameObject.scene.IsValid());
 static async Task Ready(){var end=DateTime.UtcNow.AddSeconds(30);await Frames(2);while(!Manager.IsReady){if(DateTime.UtcNow>end)throw new Exception("Foliage cache timeout");await Frames(1);}}
 static LODGroup[] Fixtures()=>Enumerable.Range(0,3).Select(i=>EditorUtility.EntityIdToObject(SessionState.GetInt("Grass027.Group"+i,0)) as LODGroup).ToArray();
 public static object PrepareIsland()
 {
  if(Application.isPlaying||UnityEditor.BuildPipeline.isBuildingPlayer||File.Exists(Root+"/build/build-running"))throw new Exception("Stopped idle Editor required");
  if(UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
  return new{isolated=UnityEngine.SceneManagement.SceneManager.sceneCount==2,noSavablePlayer=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>()==null};
 }
 public static async Task<object> IslandReady()
 {
  if(!Application.isPlaying||UnityEngine.SceneManagement.SceneManager.sceneCount!=2||UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>())throw new Exception("Only environment/lighting expected");
  if(!Camera.main){var cam=new GameObject("Owned grass027 island camera",typeof(Camera)).GetComponent<Camera>();cam.tag="MainCamera";cam.transform.SetPositionAndRotation(new Vector3(74.6542053f,63.6699448f,585.2928f),new Quaternion(.004188633f,-.9458663f,.0122160912f,.3242997f));cam.fieldOfView=60.4064941f;cam.farClipPlane=1000;}
  await Ready();var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.name.StartsWith("Grass infill")||g.name.StartsWith("Grass patch")).ToArray();
  var report=new{groups=groups.Length,crossFade=groups.Count(g=>g.fadeMode==LODFadeMode.CrossFade&&!g.animateCrossFading&&g.GetLODs().All(x=>Mathf.Abs(x.fadeTransitionWidth-.2f)<.0001f)),foliage=((System.Collections.ICollection)typeof(WorldPerformanceManager).GetField("foliage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Manager)).Count,ready=Manager.IsReady,hasSavablePlayer=GameSaveManager.HasSavableGame};File.WriteAllText(Root+"/island-policy.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> StyleSweep()
 {
  var style=Mavis.VisualStyles.VisualStyleManager.Instance;string original=style.Current.id;var results=new List<object>();
  try{foreach(var preset in style.Presets){style.Select(preset.id,false);await Frames(3);results.Add(await SceneSweep("production-"+preset.id,false));}}
  finally{style.Select(original,false);}
  File.WriteAllText(Root+"/styles.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));return new{styles=results.Count,frames=results.Count*48,preferenceWritten=false};
 }
 public static async Task<object> PixelSmoke()
 {
  if(!Application.isPlaying||Pixel360Rendering.IsActive)throw new Exception("Native-scale live fixture expected");
  var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;float scale=pipeline.renderScale;string path=Root+"/pixel.png";if(File.Exists(path))throw new Exception("Preserve image");
  try
  {
   EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();Pixel360Rendering.Configure(pipeline,true);await Frames(10);
   ScreenCapture.CaptureScreenshot(Path.GetFullPath(path));for(int i=0;i<60&&!File.Exists(path);i++)await Frames(1);
   var report=new{active=Pixel360Rendering.IsActive,scale=pipeline.renderScale,height=Screen.height,nativeImage=File.Exists(path),grassMode=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).First(g=>g.name.StartsWith("Grass infill")).fadeMode.ToString(),savedPreferenceUntouched=true};File.WriteAllText(Root+"/pixel.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{Pixel360Rendering.ReleaseCurrent();pipeline.renderScale=scale;}
 }
 public static async Task<object> PrepareLods()
 {
  if(!Application.isPlaying||UnityEngine.SceneManagement.SceneManager.sceneCount!=1||!string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))throw new Exception("Isolated Play required");
  var root=GameObject.Find("AUTO_QA Foliage reload");if(!root||Fixtures().Any(g=>g))throw new Exception("Fresh core fixture required");
  var materials=new[]{"0","1"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass_"+n+".mat")).ToArray();
  for(int i=0;i<3;i++)
  {
   var go=new GameObject("Grass027 fixture "+i,typeof(LODGroup));go.transform.SetParent(root.transform);go.transform.position=new Vector3(20+i*15,0,0);var g=go.GetComponent<LODGroup>();var levels=new List<LOD>();
   for(int tier=0;tier<3;tier++){var child=new GameObject("Grass027 level"+tier,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(go.transform,false);child.GetComponent<MeshFilter>().sharedMesh=i==2?Resources.GetBuiltinResource<Mesh>("Cube.fbx"):AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass"+new[]{"Near","Far","Low"}[tier]+".asset");var r=child.GetComponent<MeshRenderer>();r.sharedMaterials=materials;levels.Add(new LOD(new[]{.6f,.22f,.09f}[tier],new Renderer[]{r}){fadeTransitionWidth=.07f+tier*.01f});}
   g.SetLODs(levels.ToArray());g.RecalculateBounds();g.animateCrossFading=true;g.fadeMode=i==1?LODFadeMode.CrossFade:LODFadeMode.None;SessionState.SetInt("Grass027.Group"+i,g.GetInstanceID());
  }
  SessionState.SetInt("AUTO_QA.Foliage016.ExpectedCount",45);SessionState.SetString("Grass027.PrepareAssembly",typeof(Grass027).Assembly.GetName().Name);Manager.enabled=false;Manager.enabled=true;await Ready();return await CheckLods("lod-before-reload",false);
 }
 public static async Task<object> CheckLods(string label,bool afterReload=true)
 {
  var groups=Fixtures();if(groups.Any(g=>!g))throw new Exception("Missing LOD fixture");await Ready();var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
  if(afterReload)check(!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name==SessionState.GetString("Grass027.PrepareAssembly","")),"Preparation assembly actually unloaded");
  for(int i=0;i<3;i++)
  {
   var g=groups[i];var a=g.GetLODs();check(g.fadeMode==(i==0||i==1?LODFadeMode.CrossFade:LODFadeMode.None),"Fade ownership scope "+i);
   check(g.animateCrossFading==(i!=0),"Only owned grass uses reversible distance fade "+i);
   check(a.Select(x=>x.screenRelativeTransitionHeight).SequenceEqual(new[]{.6f,.22f,.09f}),"Native distances unchanged "+i);
   check(a.Select(x=>x.fadeTransitionWidth).Select((w,j)=>Mathf.Abs(w-(i==0?.2f:.07f+j*.01f))<.0001f).All(x=>x),"Owned or authored fade widths correct "+i);
  }
  for(int cycle=0;cycle<2;cycle++)
  {
   Manager.enabled=false;for(int i=0;i<3;i++){var g=groups[i];check(g.fadeMode==(i==1?LODFadeMode.CrossFade:LODFadeMode.None)&&g.animateCrossFading,"Disable restores original native mode "+i+" cycle"+cycle);check(g.GetLODs().Select((x,j)=>Mathf.Abs(x.fadeTransitionWidth-(.07f+j*.01f))<.0001f).All(x=>x),"Disable restores original widths "+i+" cycle"+cycle);}Manager.enabled=true;await Ready();
  }
  var report=new{label,passed=rows.All(r=>(bool)r.GetType().GetProperty("ok").GetValue(r)),checks=rows.Count,rows};File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> RemoveLods()
 {
  Manager.enabled=false;foreach(var g in Fixtures())if(g)UnityEngine.Object.DestroyImmediate(g.gameObject);for(int i=0;i<3;i++)SessionState.EraseInt("Grass027.Group"+i);SessionState.EraseString("Grass027.PrepareAssembly");SessionState.SetInt("AUTO_QA.Foliage016.ExpectedCount",36);Manager.enabled=true;await Ready();return new{removed=true};
 }
 public static async Task<object> SceneSweep(string label,bool candidate=false)
 {
  if(!Application.isPlaying||EditorApplication.isPaused)throw new Exception("Live unpaused Editor required");
  string dir=Root+"/"+label;if(Directory.Exists(dir))throw new Exception("Preserve evidence");Directory.CreateDirectory(dir);
  var original=Camera.main;var pos=original.transform.position;var rot=original.transform.rotation;var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);
  var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.name.StartsWith("Grass infill")||g.name.StartsWith("Grass patch")).ToArray();
  var states=groups.Select(g=>new{g,g.fadeMode,g.animateCrossFading,lods=g.GetLODs()}).ToArray();
  var pipeline=QualitySettings.renderPipeline;var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool pixel=Pixel360Rendering.IsActive;float time=Time.timeScale;
  var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(m=>m.gameObject.scene.IsValid());var field=typeof(WorldPerformanceManager).GetField("mainCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var oldCamera=field.GetValue(manager);
  Camera own=null;UniversalRenderPipelineAsset clone=null;RenderTexture rt=null;Texture2D tex=null;var active=RenderTexture.active;int callbacks=0;Action<ScriptableRenderContext,Camera> fence=(ctx,c)=>{if(c==own)callbacks++;};
  try
  {
   if(candidate)foreach(var s in states){var a=s.g.GetLODs();s.g.fadeMode=LODFadeMode.CrossFade;s.g.animateCrossFading=false;for(int i=0;i<a.Length;i++)a[i].fadeTransitionWidth=.2f;s.g.SetLODs(a);}
   Time.timeScale=0;Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(source);clone.renderScale=1;QualitySettings.renderPipeline=clone;
   foreach(var c in cameras.Keys)if(c)c.enabled=false;
   own=new GameObject("Owned grass027 scene camera",typeof(Camera)).GetComponent<Camera>();own.CopyFrom(original);own.targetTexture=null;own.tag="MainCamera";own.enabled=true;field.SetValue(manager,own);
   rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);own.targetTexture=rt;tex=new Texture2D(960,540,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=fence;
   var poses=new List<object>();
   for(int i=0;i<48;i++)
   {
    float phase=i/47f;float along=18*Mathf.Sin(phase*Mathf.PI*2);float yaw=phase<.5f?Mathf.Sin(phase*4*Mathf.PI)*22:0;
    own.transform.SetPositionAndRotation(pos+rot*Vector3.forward*along,rot*Quaternion.Euler(0,yaw,0));int before=callbacks;await Frames(3);if(callbacks<=before)throw new Exception("No native render fence");
    RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(dir+"/frame-"+i.ToString("D3")+".png",tex.EncodeToPNG());poses.Add(new{i,along,yaw,p=own.transform.position.ToString("R"),q=own.transform.rotation.ToString("R")});
   }
   var report=new{label,candidate,frames=48,callbacks,groups=groups.Length,width=960,height=540,renderScale=clone.renderScale,scaledTimeFrozen=true,poses};File.WriteAllText(dir+"/report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{label,candidate,frames=48,callbacks,groups=groups.Length};
  }
  finally
  {
   RenderPipelineManager.endCameraRendering-=fence;field.SetValue(manager,oldCamera);if(own)UnityEngine.Object.DestroyImmediate(own.gameObject);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);QualitySettings.renderPipeline=pipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(source,true);foreach(var c in cameras)if(c.Key)c.Key.enabled=c.Value;Time.timeScale=time;foreach(var s in states)if(s.g){s.g.SetLODs(s.lods);s.g.fadeMode=s.fadeMode;s.g.animateCrossFading=s.animateCrossFading;}
  }
 }
 public static async Task<object> Probe(string label,bool candidate=false,bool stabilityOnly=false)
 {
  if(!Application.isPlaying||EditorApplication.isPaused)throw new Exception("Live unpaused Editor required; scaled time is preserved");
  string dir=Root+"/"+label;if(Directory.Exists(dir))throw new Exception("Preserve existing evidence");Directory.CreateDirectory(dir);
  var group=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).First(g=>g.name.StartsWith("Grass infill"));
  var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);var pipeline=QualitySettings.renderPipeline;var pixel=Pixel360Rendering.IsActive;
  UniversalRenderPipelineAsset clone=null;GameObject stage=null;RenderTexture rt=null;Texture2D tex=null;var active=RenderTexture.active;Dictionary<Material,Material> ownedMaterials=null;
  var rows=new List<object>();var samples=new Dictionary<string,Color32[]>();float scale=Time.timeScale;
  try
  {
   Time.timeScale=0;Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);clone.renderScale=1;QualitySettings.renderPipeline=clone;
   stage=new GameObject("Owned grass027 render probe");stage.AddComponent<GameSaveExcluded>();
   var patch=UnityEngine.Object.Instantiate(group.gameObject,stage.transform);patch.name="Owned grass027 patch";patch.transform.SetPositionAndRotation(new Vector3(0,2000,0),Quaternion.identity);
   foreach(var tr in patch.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=31;
   var lod=patch.GetComponent<LODGroup>();var levels=lod.GetLODs();if(candidate){lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=false;for(int i=0;i<levels.Length;i++)levels[i].fadeTransitionWidth=.2f;lod.SetLODs(levels);}
   var bounds=levels[0].renderers[0].bounds;var materials=patch.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
   // Freeze material wind for exact image comparisons, without touching assets.
   ownedMaterials=materials.ToDictionary(m=>m,m=>new Material(m));foreach(var r in patch.GetComponentsInChildren<MeshRenderer>()){r.forceRenderingOff=false;r.sharedMaterials=r.sharedMaterials.Select(m=>ownedMaterials[m]).ToArray();}
   foreach(var m in ownedMaterials.Values){m.SetFloat("_WindBend",0);m.SetFloat("_PlayerPushStrength",0);}
   foreach(var c in cameras.Keys)if(c)c.enabled=false;
   var camera=new GameObject("Owned grass027 camera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(stage.transform);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.23f,.27f);camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=100;
   camera.transform.position=bounds.center+new Vector3(0,3.5f,17);camera.transform.LookAt(bounds.center);
   rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;tex=new Texture2D(960,540,TextureFormat.RGB24,false);
   float distance=Vector3.Distance(camera.transform.position,lod.transform.TransformPoint(lod.localReferencePoint));float factor=2*distance*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/QualitySettings.lodBias;
   async Task<Color32[]> Capture(string name,float relative,bool write){lod.size=relative*factor;await Frames(3);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();var p=tex.GetPixels32();samples[name]=p;if(write)File.WriteAllBytes(dir+"/"+name+".png",tex.EncodeToPNG());return p;}
   Color32[] previous=null;double maximum=0;int changedMax=0;
   // A size-only LOD sweep reproduces a distance/FOV threshold at exactly the
   // same projected geometry, removing camera reprojection from the comparison.
   for(int tier=0;!stabilityOnly&&tier<levels.Length;tier++)
   {
    float threshold=levels[tier].screenRelativeTransitionHeight;
    previous=null;double max=0;int changed=0;var differences=new List<double>();
    for(int step=0;step<=12;step++)
    {
     float h=threshold*Mathf.Lerp(1.24f,.98f,step/12f);var p=await Capture("tier"+tier+"-"+step,h,step==0||step==6||step==11||step==12);
     if(previous!=null){long sum=0;int count=0;for(int i=0;i<p.Length;i++){int delta=Math.Max(Math.Abs(p[i].r-previous[i].r),Math.Max(Math.Abs(p[i].g-previous[i].g),Math.Abs(p[i].b-previous[i].b)));sum+=delta;if(delta>3)count++;}double mean=sum/(double)p.Length;differences.Add(mean);max=Math.Max(max,mean);changed=Math.Max(changed,count);}previous=p;
    }
    maximum=Math.Max(maximum,max);changedMax=Math.Max(changedMax,changed);rows.Add(new{tier,threshold,maxMeanPixelDelta=max,maxChangedPixels=changed,differences});
   }
   float hold=levels[0].screenRelativeTransitionHeight*1.1f;var held=await Capture("hold-a",hold,false);await Frames(20);var heldAgain=await Capture("hold-b",hold,false);
   int Changed(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>x.Equals(y)?0:1).Sum();
   var rotation=camera.transform.rotation;var position=camera.transform.position;
   camera.transform.rotation=rotation*Quaternion.Euler(0,18,0);await Frames();camera.transform.rotation=rotation*Quaternion.Euler(0,-18,0);await Frames();camera.transform.rotation=rotation;
   var turnReturn=await Capture("turn-return",hold,false);
   camera.transform.position=position+rotation*Vector3.forward*4;await Frames();camera.transform.position=position-rotation*Vector3.forward*4;await Frames();camera.transform.position=position;
   var moveReturn=await Capture("move-return",hold,false);
   var stability=new{heldChangedPixels=Changed(held,heldAgain),turnRoundtripChangedPixels=Changed(held,turnReturn),moveRoundtripChangedPixels=Changed(held,moveReturn)};
   var full=await Capture("full",1.4f,true);lod.ForceLOD(levels.Length-1);await Frames();var low=await Capture("low",1.4f,true);lod.ForceLOD(-1);
   foreach(var r in patch.GetComponentsInChildren<Renderer>())r.enabled=false;await Frames();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();var empty=tex.GetPixels32();File.WriteAllBytes(dir+"/empty.png",tex.EncodeToPNG());
   int visible=full.Zip(empty,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>8?1:0).Sum();
   var report=new{label,candidate,stabilityOnly,stability,qualified=visible>1000,visiblePixels=visible,maximum,changedMax,mode=lod.fadeMode.ToString(),levels=levels.Select(x=>new{x.screenRelativeTransitionHeight,x.fadeTransitionWidth,triangles=x.renderers.Sum(r=>Triangles(r.GetComponent<MeshFilter>().sharedMesh))}).ToArray(),rows};
   File.WriteAllText(dir+"/report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
   return report;
  }
  finally
  {
   if(stage)UnityEngine.Object.DestroyImmediate(stage);if(ownedMaterials!=null)foreach(var m in ownedMaterials.Values)if(m)UnityEngine.Object.DestroyImmediate(m);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);QualitySettings.renderPipeline=pipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(pipeline as UniversalRenderPipelineAsset,true);foreach(var p in cameras)if(p.Key)p.Key.enabled=p.Value;Time.timeScale=scale;
  }
 }
}
