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

// Native live-scene controls; no authored scene/material writes.
public static class Grass028
{
 const string Root=".codex/grass028";
 static async Task Frames(int n=3){int f=Time.frameCount+n;var end=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount<f){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frames stalled");await Task.Delay(5);}}
 static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(m=>m.gameObject.scene.IsValid());
 static async Task Ready(){var end=DateTime.UtcNow.AddSeconds(30);await Frames(2);while(!Manager.IsReady){if(DateTime.UtcNow>end)throw new Exception("Foliage cache timeout");await Frames(1);}}
 static LODGroup[] Fixtures()=>Enumerable.Range(0,3).Select(i=>EditorUtility.EntityIdToObject(SessionState.GetInt("Grass028.Group"+i,0)) as LODGroup).ToArray();
 public static async Task<object> PrepareLods()
 {
  if(!Application.isPlaying||UnityEngine.SceneManagement.SceneManager.sceneCount!=1||!string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))throw new Exception("Isolated Play required");
  var root=GameObject.Find("AUTO_QA Foliage reload");if(!root||Fixtures().Any(g=>g))throw new Exception("Fresh core fixture required");
  var materials=new[]{"0","1"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass_"+n+".mat")).ToArray();
  for(int i=0;i<3;i++)
  {
   var go=new GameObject("Grass028 fixture "+i,typeof(LODGroup));go.transform.SetParent(root.transform);go.transform.position=new Vector3(20+i*15,0,0);var g=go.GetComponent<LODGroup>();var levels=new List<LOD>();
   for(int tier=0;tier<3;tier++){var child=new GameObject("Grass028 level"+tier,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(go.transform,false);child.GetComponent<MeshFilter>().sharedMesh=i==2?Resources.GetBuiltinResource<Mesh>("Cube.fbx"):AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass"+new[]{"Near","Far","Low"}[tier]+".asset");var r=child.GetComponent<MeshRenderer>();r.sharedMaterials=materials;levels.Add(new LOD(new[]{.6f,.22f,.09f}[tier],new Renderer[]{r}){fadeTransitionWidth=.07f+tier*.01f});}
   g.SetLODs(levels.ToArray());g.RecalculateBounds();g.animateCrossFading=true;g.fadeMode=i==1?LODFadeMode.CrossFade:LODFadeMode.None;SessionState.SetInt("Grass028.Group"+i,g.GetInstanceID());
  }
  SessionState.SetInt("AUTO_QA.Foliage016.ExpectedCount",45);SessionState.SetString("Grass028.PrepareAssembly",typeof(Grass028).Assembly.GetName().Name);Manager.enabled=false;Manager.enabled=true;await Ready();return await CheckLods("lod-before-reload",false);
 }
 public static async Task<object> CheckLods(string label,bool afterReload=true)
 {
  var groups=Fixtures();if(groups.Any(g=>!g))throw new Exception("Missing LOD fixture");await Ready();var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
  if(afterReload)check(!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name==SessionState.GetString("Grass028.PrepareAssembly","")),"Preparation assembly actually unloaded");
  var fixedGroup=groups[0];var fixedLods=fixedGroup.GetLODs();
  check(fixedLods.Length==1,"Visible grass has exactly one layout");
  check(fixedLods.Length==1&&Mathf.Abs(fixedLods[0].screenRelativeTransitionHeight-.09f)<.00001f,"Original final cull retained");
  check(fixedLods.Length==1&&fixedLods[0].renderers.Single().GetComponent<MeshFilter>().sharedMesh.name=="VerifiedGrassNear","Original full-detail mesh selected");
  check(fixedGroup.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==1,"Unused LOD renderers suppressed");
  check(fixedGroup.fadeMode==LODFadeMode.CrossFade&&!fixedGroup.animateCrossFading,"Only far cull fades by distance");
  check(Mathf.Abs(fixedLods[0].fadeTransitionWidth-(.22f-.09f)*.2f/(1-.09f))<.00001f,"Cull band preserved in screen-height units");
  for(int i=1;i<3;i++){var g=groups[i];check(g.GetLODs().Length==3,"Unowned layout count intact "+i);check(g.fadeMode==(i==1?LODFadeMode.CrossFade:LODFadeMode.None)&&g.animateCrossFading,"Authored mode intact "+i);check(g.GetLODs().Select((x,j)=>Mathf.Abs(x.fadeTransitionWidth-(.07f+j*.01f))<.0001f).All(x=>x),"Authored widths intact "+i);}
  for(int cycle=0;cycle<2;cycle++)
  {
   Manager.enabled=false;for(int i=0;i<3;i++){var g=groups[i];check(g.fadeMode==(i==1?LODFadeMode.CrossFade:LODFadeMode.None)&&g.animateCrossFading,"Disable restores original native mode "+i+" cycle"+cycle);check(g.GetLODs().Select((x,j)=>Mathf.Abs(x.fadeTransitionWidth-(.07f+j*.01f))<.0001f).All(x=>x),"Disable restores original widths "+i+" cycle"+cycle);check(g.GetLODs().Length==3&&g.GetComponentsInChildren<MeshRenderer>().All(r=>r.enabled),"Disable restores membership/enabled flags "+i+" cycle"+cycle);}Manager.enabled=true;await Ready();
  }
  var report=new{label,passed=rows.All(r=>(bool)r.GetType().GetProperty("ok").GetValue(r)),checks=rows.Count,rows};File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> RemoveLods()
 {
  Manager.enabled=false;foreach(var g in Fixtures())if(g)UnityEngine.Object.DestroyImmediate(g.gameObject);for(int i=0;i<3;i++)SessionState.EraseInt("Grass028.Group"+i);SessionState.EraseString("Grass028.PrepareAssembly");SessionState.SetInt("AUTO_QA.Foliage016.ExpectedCount",36);Manager.enabled=true;await Ready();return new{removed=true};
 }
 public static object PrepareStopped(){
  if(Application.isPlaying)throw new Exception("Stop user session normally first");var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();if(setup.Any(x=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(x.path).isDirty))throw new Exception("Preserve dirty scenes");
  File.WriteAllText(Root+"/editor-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup,Newtonsoft.Json.Formatting.Indented));Directory.CreateDirectory(Root+"/save-after-user-stop");foreach(var n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"}){var p=Path.Combine(Application.persistentDataPath,n);if(File.Exists(p))File.Copy(p,Root+"/save-after-user-stop/"+n,false);}
  UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);return new{isolated=UnityEngine.SceneManagement.SceneManager.sceneCount==1&&string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path)};
 }
 public static object Snapshot(){
  if(!Application.isPlaying||File.Exists(Root+"/live.json"))throw new Exception("Fresh live session required");
  Directory.CreateDirectory(Root+"/save-before");foreach(var n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"}){string p=Path.Combine(Application.persistentDataPath,n);if(File.Exists(p))File.Copy(p,Root+"/save-before/"+n,false);}
  var c=Camera.main;var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var report=new{camera=new{p=c.transform.position.ToString("R"),q=c.transform.rotation.ToString("R"),c.fieldOfView},player=player.transform.position.ToString("R"),time=Time.timeScale,pixel=Pixel360Rendering.IsActive,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var x=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return new{x.path,x.isDirty};}).ToArray()};File.WriteAllText(Root+"/live.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> PerformancePair(){
  var registry=(System.Collections.IDictionary)typeof(WorldPerformanceManager).GetField("grassLods",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Manager);
  var groups=registry.Keys.Cast<LODGroup>().ToArray();var fixedLevels=groups.ToDictionary(g=>g,g=>g.GetLODs());var enabled=groups.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>()).ToDictionary(r=>r,r=>r.enabled);float time=Time.timeScale;
  Action restore=()=>{foreach(var g in groups){g.SetLODs(fixedLevels[g]);g.fadeMode=LODFadeMode.CrossFade;g.animateCrossFading=false;}foreach(var r in enabled)if(r.Key)r.Key.enabled=r.Value;};
  Action legacy=()=>{foreach(var g in groups){var state=registry[g];var levels=(LOD[])((LOD[])state.GetType().GetField("levels").GetValue(state)).Clone();foreach(var level in levels)foreach(var r in level.renderers)r.enabled=true;for(int i=0;i<levels.Length;i++)levels[i].fadeTransitionWidth=.2f;g.SetLODs(levels);}};
  if(GameSaveManager.HasSavableGame)throw new Exception("Environment-only geometry control required");var boss=GameObject.Find("CappuccinoAssassino");bool activeBoss=boss&&boss.activeSelf;
  try{if(boss)boss.SetActive(false);Time.timeScale=0;legacy();await SceneSweep("paired-geometry-a",0);restore();await SceneSweep("paired-geometry-fixed",0);legacy();await SceneSweep("paired-geometry-b",0);return new{paired=true,frames=144,onlyGeometryCounter=true,proceduralBossExcluded=true};}
  finally{restore();Time.timeScale=time;if(boss)boss.SetActive(activeBoss);}
 }
 public static async Task<object> Pair(){
  var c=Camera.main;var behaviours=c.GetComponents<MonoBehaviour>().Where(b=>!(b is UniversalAdditionalCameraData)).ToDictionary(b=>b,b=>b.enabled);var p=c.transform.position;var q=c.transform.rotation;float scale=Time.timeScale;var actors=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.GetComponentInParent<Animator>()).ToDictionary(r=>r,r=>r.forceRenderingOff);
  try{Time.timeScale=0;foreach(var r in actors.Keys)r.forceRenderingOff=true;foreach(var b in behaviours.Keys)b.enabled=false;var old=await SceneSweep("qualified-before",0);var mid=await SceneSweep("qualified-fixed-layout",1);var near=await SceneSweep("qualified-full-detail-control",2);return new{completed=true};}
  finally{foreach(var r in actors)if(r.Key)r.Key.forceRenderingOff=r.Value;c.transform.SetPositionAndRotation(p,q);foreach(var b in behaviours)if(b.Key)b.Key.enabled=b.Value;Time.timeScale=scale;}
 }
 public static async Task<object> SceneSweep(string label,int candidate=0)
 {
  if(!Application.isPlaying||EditorApplication.isPaused)throw new Exception("Live unpaused Editor required");
  string dir=Root+"/"+label;if(Directory.Exists(dir))throw new Exception("Preserve evidence");Directory.CreateDirectory(dir);
  var original=Camera.main;var pos=original.transform.position;var rot=original.transform.rotation;var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);
  var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.name.StartsWith("Grass infill")||g.name.StartsWith("Grass patch")).ToArray();
  var states=groups.Select(g=>new{g,g.fadeMode,g.animateCrossFading,lods=g.GetLODs()}).ToArray();var rendererEnabled=states.SelectMany(s=>s.lods).SelectMany(l=>l.renderers).Distinct().ToDictionary(r=>r,r=>r.enabled);
  var pipeline=QualitySettings.renderPipeline;var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool pixel=Pixel360Rendering.IsActive;float time=Time.timeScale;
  var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(m=>m.gameObject.scene.IsValid());var field=typeof(WorldPerformanceManager).GetField("mainCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var oldCamera=field.GetValue(manager);
  Camera own=null;UniversalRenderPipelineAsset clone=null;RenderTexture rt=null;Texture2D tex=null;var active=RenderTexture.active;int callbacks=0;Action<ScriptableRenderContext,Camera> fence=(ctx,c)=>{if(c==own)callbacks++;};
  try
  {
   if(candidate!=0)foreach(var s in states){var a=s.g.GetLODs();float cutoff=a[a.Length-1].screenRelativeTransitionHeight;float band=(a[a.Length-2].screenRelativeTransitionHeight-cutoff)*a[a.Length-1].fadeTransitionWidth;var fixedLevel=new LOD(cutoff,a[candidate==2?0:1].renderers){fadeTransitionWidth=band/Mathf.Max(.001f,1-cutoff)};foreach(var r in a.SelectMany(l=>l.renderers).Distinct())if(!fixedLevel.renderers.Contains(r))r.enabled=false;s.g.SetLODs(new[]{fixedLevel});s.g.fadeMode=LODFadeMode.CrossFade;s.g.animateCrossFading=false;}
   Time.timeScale=0;Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(source);clone.renderScale=1;QualitySettings.renderPipeline=clone;
   foreach(var c in cameras.Keys)if(c)c.enabled=false;
   own=new GameObject("Owned grass028 scene camera",typeof(Camera)).GetComponent<Camera>();own.CopyFrom(original);own.targetTexture=null;own.tag="MainCamera";own.enabled=true;field.SetValue(manager,own);
   rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);own.targetTexture=rt;tex=new Texture2D(960,540,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=fence;
   var poses=new List<object>();var triangleSamples=new List<int>();
   for(int i=0;i<48;i++)
   {
    float phase=i/47f;float along=18*Mathf.Sin(phase*Mathf.PI*2);float yaw=phase<.5f?Mathf.Sin(phase*4*Mathf.PI)*22:0;
    own.transform.SetPositionAndRotation(pos+rot*Vector3.forward*along,rot*Quaternion.Euler(0,yaw,0));int before=callbacks;await Frames(3);triangleSamples.Add(UnityEditor.UnityStats.triangles);if(callbacks<=before)throw new Exception("No native render fence");
    RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(dir+"/frame-"+i.ToString("D3")+".png",tex.EncodeToPNG());poses.Add(new{i,along,yaw,p=own.transform.position.ToString("R"),q=own.transform.rotation.ToString("R")});
   }
   own.cullingMask=0;await Frames(4);int emptyTriangles=UnityEditor.UnityStats.triangles;var report=new{label,candidate,frames=48,callbacks,groups=groups.Length,width=960,height=540,renderScale=clone.renderScale,scaledTimeFrozen=true,emptyTriangles,medianNativeTriangleCount=triangleSamples.OrderBy(x=>x).ElementAt(triangleSamples.Count/2),triangleSamples,poses};File.WriteAllText(dir+"/report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{label,candidate,frames=48,callbacks,groups=groups.Length};
  }
  finally
  {
   RenderPipelineManager.endCameraRendering-=fence;field.SetValue(manager,oldCamera);if(own)UnityEngine.Object.DestroyImmediate(own.gameObject);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);QualitySettings.renderPipeline=pipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(source,true);foreach(var c in cameras)if(c.Key)c.Key.enabled=c.Value;Time.timeScale=time;foreach(var s in states)if(s.g){s.g.SetLODs(s.lods);s.g.fadeMode=s.fadeMode;s.g.animateCrossFading=s.animateCrossFading;}foreach(var e in rendererEnabled)if(e.Key)e.Key.enabled=e.Value;
  }
 }
 static long Triangles(Mesh m)=>Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3);
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
  var report=new{groups=groups.Length,crossFade=groups.Count(g=>g.fadeMode==LODFadeMode.CrossFade&&!g.animateCrossFading&&g.GetLODs().Length==1&&g.GetLODs()[0].renderers.Single().GetComponent<MeshFilter>().sharedMesh.name=="VerifiedGrassNear"&&g.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==1),foliage=((System.Collections.ICollection)typeof(WorldPerformanceManager).GetField("foliage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Manager)).Count,ready=Manager.IsReady,hasSavablePlayer=GameSaveManager.HasSavableGame};File.WriteAllText(Root+"/island-policy.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> StyleSweep()
 {
  var style=Mavis.VisualStyles.VisualStyleManager.Instance;string original=style.Current.id;var results=new List<object>();
  try{foreach(var preset in style.Presets){style.Select(preset.id,false);await Frames(3);results.Add(await SceneSweep("production-"+preset.id,0));}}
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

 public static async Task<object> Density(string label)
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
   var lod=patch.GetComponent<LODGroup>();var levels=lod.GetLODs();
   var registry=(System.Collections.IDictionary)typeof(WorldPerformanceManager).GetField("grassLods",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Manager);var state=registry[group];var heights=(float[])state.GetType().GetField("heights").GetValue(state);
   var renderers=patch.GetComponentsInChildren<MeshRenderer>();var originals=heights.Select((h,i)=>new LOD(h,new Renderer[]{renderers.Single(r=>r.GetComponent<MeshFilter>().sharedMesh.name==new[]{"VerifiedGrassNear","VerifiedGrassFar","VerifiedGrassLow"}[i])}){fadeTransitionWidth=.2f}).ToArray();
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
   var phaseMax=new Dictionary<string,double>();
   foreach(string phase in new[]{"legacy","fixed"}) {
   if(phase=="legacy"){foreach(var r in renderers)r.enabled=true;lod.SetLODs(originals);}else{foreach(var r in renderers)r.enabled=levels[0].renderers.Contains(r);lod.SetLODs(levels);}
   for(int tier=0;tier<2;tier++)
   {
    float threshold=heights[tier];
    previous=null;double max=0;int changed=0;var differences=new List<double>();
    for(int step=0;step<=12;step++)
    {
     float h=threshold*Mathf.Lerp(1.24f,.98f,step/12f);var p=await Capture(phase+"-tier"+tier+"-"+step,h,step==0||step==6||step==11||step==12);
     if(previous!=null){long sum=0;int count=0;for(int i=0;i<p.Length;i++){int delta=Math.Max(Math.Abs(p[i].r-previous[i].r),Math.Max(Math.Abs(p[i].g-previous[i].g),Math.Abs(p[i].b-previous[i].b)));sum+=delta;if(delta>3)count++;}double mean=sum/(double)p.Length;differences.Add(mean);max=Math.Max(max,mean);changed=Math.Max(changed,count);}previous=p;
    }
    maximum=Math.Max(maximum,max);changedMax=Math.Max(changedMax,changed);rows.Add(new{phase,tier,threshold,maxMeanPixelDelta=max,maxChangedPixels=changed,differences});phaseMax.TryGetValue(phase,out var prevMax);phaseMax[phase]=Math.Max(prevMax,max);
   }}
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
   var report=new{label,phaseMax,stability,qualified=visible>1000&&phaseMax["legacy"]>.05&&phaseMax["fixed"]==0,visiblePixels=visible,maximum,changedMax,mode=lod.fadeMode.ToString(),levels=levels.Select(x=>new{x.screenRelativeTransitionHeight,x.fadeTransitionWidth,triangles=x.renderers.Sum(r=>Triangles(r.GetComponent<MeshFilter>().sharedMesh))}).ToArray(),rows};
   File.WriteAllText(dir+"/report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
   return report;
  }
  finally
  {
   if(stage)UnityEngine.Object.DestroyImmediate(stage);if(ownedMaterials!=null)foreach(var m in ownedMaterials.Values)if(m)UnityEngine.Object.DestroyImmediate(m);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);QualitySettings.renderPipeline=pipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(pipeline as UniversalRenderPipelineAsset,true);foreach(var p in cameras)if(p.Key)p.Key.enabled=p.Value;Time.timeScale=scale;
  }
 } public static object RestoreWorkspace(){
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped idle Editor required");
  foreach(var p in Directory.GetFiles(Root+"/save-after-user-stop"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Preserve newer save; no rollback");
  UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(File.ReadAllText(Root+"/editor-setup.json")));
  var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.name.StartsWith("Grass patch")||g.name.StartsWith("Grass infill")).ToArray();
  var report=new{stopped=!Application.isPlaying,scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,dirty=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty),groups=groups.Length,originalRestored=groups.Count(g=>g.GetLODs().Length==3&&g.fadeMode==LODFadeMode.None&&g.GetLODs().All(l=>l.fadeTransitionWidth==0)&&g.GetComponentsInChildren<MeshRenderer>().All(r=>r.enabled)),ownedFixtures=Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.scene.IsValid()&&(g.name.StartsWith("Owned grass028")||g.name=="AUTO_QA Foliage reload")),savesUnchanged=true,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id};File.WriteAllText(Root+"/final-native.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }

}
