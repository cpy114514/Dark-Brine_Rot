using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;

public static class Grass032
{
 const string Root=".codex/grass032";
 static async Task Frames(int n=4){int f=Time.frameCount+n;var end=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount<f){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frames stalled");await Task.Delay(5);}}
 public static object Prepare()
 {
  if(Application.isPlaying||File.Exists(Root+"/editor-setup.json"))throw new Exception("Fresh stopped workspace required");var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");File.WriteAllText(Root+"/editor-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup,Newtonsoft.Json.Formatting.Indented));
  Directory.CreateDirectory(Root+"/save-after-user-stop");foreach(var n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"}){var p=Path.Combine(Application.persistentDataPath,n);if(File.Exists(p))File.Copy(p,Root+"/save-after-user-stop/"+n,false);}
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);return new{scenes=UnityEngine.SceneManagement.SceneManager.sceneCount};
 }
 public static async Task<object> Stage()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||GameObject.Find("Owned grass032 stage"))throw new Exception("Fresh environment fixture required");
  var boss=GameObject.Find("CappuccinoAssassino");if(boss)boss.SetActive(false);
  var root=new GameObject("Owned grass032 stage");root.SetActive(false);root.AddComponent<GameSaveExcluded>();
  var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.name="Owned native Sahur";actor.AddComponent<GameSaveExcluded>();actor.transform.position=new Vector3(369.59f,3.77f,731.02f);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;root.SetActive(true);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
  var animator=actor.GetComponentInChildren<Animator>();if(animator){animator.applyRootMotion=false;animator.Play("Idle",0,.25f);animator.Update(0);animator.enabled=false;}
  var bounds=actor.GetComponentsInChildren<Renderer>().First(r=>r.bounds.size.sqrMagnitude>1).bounds;
  var cam=new GameObject("Owned grass032 fixture view",typeof(Camera)).GetComponent<Camera>();cam.transform.SetParent(root.transform);cam.gameObject.AddComponent<AudioListener>();cam.tag="MainCamera";cam.fieldOfView=48;cam.farClipPlane=1000;cam.transform.position=bounds.center+new Vector3(8,2,10);cam.transform.LookAt(bounds.center);
  var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(w=>w.gameObject.scene.IsValid());var end=DateTime.UtcNow.AddSeconds(60);await Frames(4);while(!manager.IsReady){if(DateTime.UtcNow>end)throw new Exception("Environment readiness timeout");await Frames(1);}var report=new{ready=true,actorBounds=bounds.ToString(),savable=GameSaveManager.HasSavableGame,camera=cam.transform.position.ToString()};File.WriteAllText(Root+"/stage.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> Arrange()
 {
  if(GameSaveManager.HasSavableGame)throw new Exception("Preview only");var actor=GameObject.Find("Owned native Sahur");var hits=Physics.RaycastAll(new Ray(new Vector3(250,400,500),Vector3.down),600);var hit=hits.First(h=>h.collider.GetComponentInParent<ProceduralIsland>());actor.transform.position=new Vector3(250,hit.point.y+5,500);await Frames(2);var parts=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var b=parts[0].bounds;foreach(var r in parts)b.Encapsulate(r.bounds);actor.transform.position+=Vector3.up*(hit.point.y-b.min.y+.03f);await Frames(2);b=parts[0].bounds;foreach(var r in parts)b.Encapsulate(r.bounds);Camera.main.transform.position=b.center+new Vector3(9,2,11);Camera.main.transform.LookAt(b.center);return new{ground=hit.point.ToString(),bounds=b.ToString()};
 }

 public static object Noon()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||!GameObject.Find("Owned grass032 stage"))throw new Exception("Owned fixture required");
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
  var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  clock.GetType().GetField("clockRuns",flags).SetValue(clock,false);clock.GetType().GetField("startHour",flags).SetValue(clock,12f);clock.GetType().GetField("elapsedGameHours",flags).SetValue(clock,0f);clock.GetType().GetMethod("ApplyTimeOfDay",flags).Invoke(clock,null);
  return new{frozenNoon=true};
 }
 public static object RestoreWorkspace()
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped idle Editor required");
  foreach(var p in Directory.GetFiles(Root+"/save-after-user-stop"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Preserve newer save; no rollback");
  var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(File.ReadAllText(Root+"/editor-setup.json"));
  UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DarkFairytale.shader");
  var report=new{stopped=!Application.isPlaying,scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,paths=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path).ToArray(),dirty=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty),ownedFixtures=Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.scene.IsValid()&&g.name.StartsWith("Owned grass032")),savesUnchanged=true,shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{severity=m.severity.ToString(),m.message}).ToArray(),editorPid=System.Diagnostics.Process.GetCurrentProcess().Id};
  File.WriteAllText(Root+"/final-native.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static object AuditStopped()
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer||File.Exists(Root+"/build/build-running"))throw new Exception("Stopped idle Editor required");
  foreach(var p in Directory.GetFiles(Root+"/save-after-user-stop"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Preserve newer save");
  var scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var expected=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(File.ReadAllText(Root+"/editor-setup.json"));
  bool match=scenes.Select(s=>s.path).SequenceEqual(expected.Select(s=>s.path));var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DarkFairytale.shader");
  var mat=new Material(shader);try{ShaderUtil.CompilePass(mat,0,true);mat.EnableKeyword("_GBUFFER_NORMALS_OCT");ShaderUtil.CompilePass(mat,0,true);}finally{UnityEngine.Object.DestroyImmediate(mat);}
  var report=new{stopped=true,scenes=scenes.Length,paths=scenes.Select(s=>s.path).ToArray(),setupMatches=match,dirty=scenes.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty),ownedFixtures=Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.scene.IsValid()&&g.name.StartsWith("Owned grass032")),savesUnchanged=true,timeScale=Time.timeScale,shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{severity=m.severity.ToString(),m.message}).ToArray(),editorPid=System.Diagnostics.Process.GetCurrentProcess().Id};File.WriteAllText(Root+"/final-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static object RemoveFixturePreferenceKeys()
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped required");
  var delta=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/prefs-comparison-final.json"));
  if(delta["changedNames"].Any(v=>!((string)v).StartsWith("DarkBrine.HealingPacks.Campaign_h"))||delta["newNames"].Count()<1||delta["newNames"].Count()>3)throw new Exception("Unexpected preference delta");
  var keys=delta["newNames"].Select(t=>System.Text.RegularExpressions.Regex.Match((string)t,@"^(DarkBrine\.HealingPacks\.Remaining\.[0-9a-f]{32})_h[0-9]+$")).ToArray();
  var original=File.ReadAllText(Root+"/campaign-pref-before.txt");var current=PlayerPrefs.GetString("DarkBrine.HealingPacks.Campaign");
  if(current!=original&&!keys.Any(k=>k.Success&&k.Groups[1].Value.EndsWith(current)))throw new Exception("Preserve newer user preference");
  foreach(var key in keys){if(!key.Success)throw new Exception("Unexpected key; preserve");string id=key.Groups[1].Value.Substring("DarkBrine.HealingPacks.Remaining.".Length);if(original==id||Directory.GetFiles(Root+"/save-after-user-stop").Any(p=>File.ReadAllText(p).Contains(id)))throw new Exception("Preserve user campaign");if(PlayerPrefs.GetInt(key.Groups[1].Value,-1)!=3)throw new Exception("Preserve modified campaign");}
  PlayerPrefs.SetString("DarkBrine.HealingPacks.Campaign",original);foreach(var key in keys)PlayerPrefs.DeleteKey(key.Groups[1].Value);PlayerPrefs.Save();
  return new{removedUnusedFixtureKeys=keys.Length,existingValuesUntouched=true};
 }
 static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(w=>w.gameObject.scene.IsValid());
 static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
 public static async Task<object> Ready()
 {
  if(!Application.isPlaying||!GameObject.Find("Owned grass032 stage")||GameSaveManager.HasSavableGame)throw new Exception("Owned environment fixture required");
  var until=DateTime.UtcNow.AddSeconds(90);while(!Manager.IsReady){if(DateTime.UtcNow>until)throw new Exception("Foliage cache timeout");await Frames(2);}
  return new{ready=true};
 }
 static LODGroup[] Groups()=>UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.name.StartsWith("Grass infill")||g.name.StartsWith("Grass patch")).ToArray();
 public static async Task<object> Capture(string label,string mode="realistic",bool pixelCapture=false,bool walking=false)
 {
  await Ready();if(Directory.Exists(Root+"/"+label))throw new Exception("Unique capture label required");string dir=Root+"/"+label;Directory.CreateDirectory(dir);
  var manager=Manager;var groups=Groups();var style=VisualStyleManager.Instance;string id=style.Current.id;var details=style.Details;float intensity=style.Intensity,time=Time.timeScale,oldBias=QualitySettings.lodBias;var original=Camera.main;var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);var oldPipeline=QualitySettings.renderPipeline;var current=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool pixel=Pixel360Rendering.IsActive;var active=RenderTexture.active;Camera own=null;UniversalRenderPipelineAsset clone=null;RenderTexture rt=null;Texture2D tex=null;
  var main=typeof(WorldPerformanceManager).GetField("mainCamera",Flags);var next=typeof(WorldPerformanceManager).GetField("nextUpdate",Flags);var refreshing=typeof(WorldPerformanceManager).GetField("foliageRefreshing",Flags);var refresh=typeof(WorldPerformanceManager).GetMethod("RefreshFoliage",Flags);var oldMain=main.GetValue(manager);float oldNext=(float)next.GetValue(manager);int callbacks=0;Action<ScriptableRenderContext,Camera> fence=(ctx,c)=>{if(c==own)callbacks++;};
  try
  {
   Time.timeScale=0;Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(current);clone.renderScale=1;QualitySettings.renderPipeline=clone;
   foreach(var c in cameras.Keys)c.enabled=false;own=new GameObject("Owned grass032 capture",typeof(Camera)).GetComponent<Camera>();own.CopyFrom(original);own.enabled=true;own.tag="MainCamera";main.SetValue(manager,own);next.SetValue(manager,float.PositiveInfinity);
   rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);own.targetTexture=rt;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=fence;style.Select(mode,false);if(pixelCapture){await Frames(4);Pixel360Rendering.Configure(clone,true);}
   var live=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/live-camera.json"))["result"];var liveP=ParseVector((string)live["camera"]);var qv=ParseNumbers((string)live["rotation"]);var liveQ=new Quaternion(qv[0],qv[1],qv[2],qv[3]);float fov=(float)live["fieldOfView"];QualitySettings.lodBias=(float)live["lodBias"];
   var poses=walking ? Enumerable.Range(0,24).Select(k=>new{n="move-"+k.ToString("D3"),p=liveP+liveQ*Vector3.forward*(Mathf.Sin(k/23f*Mathf.PI)*16),q=liveQ*Quaternion.Euler(0,Mathf.Sin(k/23f*Mathf.PI*2)*12,0)}).ToArray() : new[]{new{n="user",p=liveP,q=liveQ},new{n="forward",p=liveP+liveQ*Vector3.forward*12,q=liveQ},new{n="turn",p=liveP,q=liveQ*Quaternion.Euler(0,25,0)},new{n="return",p=liveP,q=liveQ}};
   var rows=new List<object>();foreach(var pose in poses)
   {
    own.fieldOfView=fov;own.transform.SetPositionAndRotation(pose.p,pose.q);var end=DateTime.UtcNow.AddSeconds(40);while((bool)refreshing.GetValue(manager)){if(DateTime.UtcNow>end)throw new Exception("Old refresh stalled");await Frames(1);}refresh.Invoke(manager,new object[]{pose.p});while((bool)refreshing.GetValue(manager)){if(DateTime.UtcNow>end)throw new Exception("Refresh stalled");await Frames(1);}int start=callbacks;await Frames(6);if(callbacks<=start)throw new Exception("No render callbacks");RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+pose.n+".png",tex.EncodeToPNG());
    int fully=0,fading=0,culled=0;double triangles=0;
    foreach(var g in groups){var l=g.GetLODs()[0];float scale=Mathf.Max(g.transform.lossyScale.x,Mathf.Max(g.transform.lossyScale.y,g.transform.lossyScale.z));float distance=Vector3.Distance(own.transform.position,g.transform.TransformPoint(g.localReferencePoint));float relative=g.size*scale*QualitySettings.lodBias/(2*Mathf.Tan(fov*Mathf.Deg2Rad*.5f)*Mathf.Max(.01f,distance));if(relative<l.screenRelativeTransitionHeight){culled++;continue;}if(relative<l.screenRelativeTransitionHeight+(1-l.screenRelativeTransitionHeight)*l.fadeTransitionWidth)fading++;else fully++;triangles+=l.renderers.Sum(r=>TriangleCount(r.GetComponent<MeshFilter>().sharedMesh));}
    rows.Add(new{pose=pose.n,camera=pose.p.ToString("R"),fov,lodBias=QualitySettings.lodBias,callbacks=callbacks-start,potentialFull=fully,potentialFading=fading,potentialCulled=culled,potentialTrianglesIgnoringFrustum=triangles});
   }
   var g0=groups[0];var level=g0.GetLODs()[0];float factor=g0.size*QualitySettings.lodBias/(2*Mathf.Tan(fov*Mathf.Deg2Rad*.5f));var report=new{label,style=mode,pixel=Pixel360Rendering.IsActive,renderScale=clone.renderScale,groups=groups.Length,cutoff=level.screenRelativeTransitionHeight,width=level.fadeTransitionWidth,estimatedEndMeters=factor/level.screenRelativeTransitionHeight,estimatedFadeStartMeters=factor/(level.screenRelativeTransitionHeight+(1-level.screenRelativeTransitionHeight)*level.fadeTransitionWidth),callbacks,rows};File.WriteAllText(dir+"/report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{RenderPipelineManager.endCameraRendering-=fence;main.SetValue(manager,oldMain);next.SetValue(manager,oldNext);if(own)UnityEngine.Object.DestroyImmediate(own.gameObject);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);QualitySettings.renderPipeline=oldPipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(current,true);foreach(var c in cameras)if(c.Key)c.Key.enabled=c.Value;style.Select(id,false);style.SetIntensity(intensity,false);for(int j=0;j<4;j++)style.SetDetail(j,details[j],false);Time.timeScale=time;QualitySettings.lodBias=oldBias;}
 }
 static float[] ParseNumbers(string text)=>text.Trim('(',')').Split(',').Select(n=>float.Parse(n,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
 static Vector3 ParseVector(string text){var p=ParseNumbers(text);return new Vector3(p[0],p[1],p[2]);}

 public static async Task<object> Boundary(string label="boundary-qualified")
 {
  await Ready();string dir=Root+"/"+label;if(Directory.Exists(dir))throw new Exception("Fresh boundary control required");Directory.CreateDirectory(dir);
  var manager=Manager;var groups=Groups();var registry=(System.Collections.IDictionary)typeof(WorldPerformanceManager).GetField("grassLods",Flags).GetValue(manager);var sample=registry.Keys.Cast<LODGroup>().First();var state=registry[sample];var heights=(float[])state.GetType().GetField("heights").GetValue(state);float oldCut=heights.Last(),oldBand=(heights[1]-oldCut)*.2f;
  var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);var oldPipeline=QualitySettings.renderPipeline;float bias=QualitySettings.lodBias,time=Time.timeScale;bool pixel=Pixel360Rendering.IsActive;var active=RenderTexture.active;GameObject stage=null;Material[] materials=null;RenderTexture rt=null;Texture2D tex=null;UniversalRenderPipelineAsset clone=null;var style=VisualStyleManager.Instance;string id=style.Current.id;var details=style.Details;var rows=new List<object>();var checks=new List<object>();Action<bool,string> check=(ok,name)=>checks.Add(new{ok,name});
  int Changed(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Math.Max(Math.Abs(x.r-y.r),Math.Max(Math.Abs(x.g-y.g),Math.Abs(x.b-y.b)))>3?1:0).Sum();
  try
  {
   Time.timeScale=0;Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);clone.renderScale=1;QualitySettings.renderPipeline=clone;QualitySettings.lodBias=.5f;style.Select("realistic",false);foreach(var c in cameras.Keys)c.enabled=false;
   stage=new GameObject("Owned grass032 boundary");var patch=UnityEngine.Object.Instantiate(sample.gameObject,stage.transform);patch.transform.SetPositionAndRotation(new Vector3(0,2000,0),Quaternion.identity);foreach(var t in patch.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   var group=patch.GetComponent<LODGroup>();var level=group.GetLODs()[0];var renderer=level.renderers[0];renderer.forceRenderingOff=false;renderer.enabled=true;var source=renderer.sharedMaterials;materials=source.Select(m=>new Material(m)).ToArray();foreach(var m in materials){m.SetFloat("_WindBend",0);m.SetFloat("_PlayerPushStrength",0);}renderer.sharedMaterials=materials;
   var cam=new GameObject("Owned grass032 boundary camera",typeof(Camera)).GetComponent<Camera>();cam.transform.SetParent(stage.transform);cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.23f,.27f);cam.fieldOfView=60.4064941f;cam.farClipPlane=100;cam.transform.position=renderer.bounds.center+new Vector3(0,3.5f,17);cam.transform.LookAt(renderer.bounds.center);
   rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;tex=new Texture2D(960,540,TextureFormat.RGB24,false);
   float fixedFactor=2*Vector3.Distance(cam.transform.position,group.transform.TransformPoint(group.localReferencePoint))*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f)/QualitySettings.lodBias;
   float liveFactor=sample.size*.5f/(2*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f));
   async Task<Color32[]> Shot(string label){await Frames(5);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+label+".png",tex.EncodeToPNG());return tex.GetPixels32();}
   renderer.enabled=false;var empty=await Shot("empty");renderer.enabled=true;
   var counts=new Dictionary<string,Dictionary<int,int>>();foreach(string policy in new[]{"before","after"})
   {
    float cutoff=policy=="before"?oldCut:oldCut*.5f;float band=policy=="before"?oldBand:cutoff/3f;group.SetLODs(new[]{new LOD(cutoff,new[]{renderer}){fadeTransitionWidth=band/(1-cutoff)}});group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=false;counts[policy]=new Dictionary<int,int>();
    foreach(int distance in new[]{20,31,39,41,50,60,70,79,81,90}){group.size=liveFactor/distance*fixedFactor;var pixels=await Shot(policy+"-"+distance);counts[policy][distance]=Changed(pixels,empty);rows.Add(new{policy,equivalentDistance=distance,visiblePixels=counts[policy][distance],cutoff,band});}
   }
   check(counts["before"][20]>1000,"Positive original patch control");check(counts["before"][41]<10,"Old boundary culls around40m");check(counts["after"][50]>1000,"Extended grass remains at50m");check(counts["after"][60]>1000,"Extended grass remains at60m");check(counts["after"][70]>100,"Extended fade remains visible at70m");check(counts["after"][81]<10,"Extended boundary culls around80m");check(counts["after"][90]<10,"Beyond new boundary remains culled");
   check(counts["after"][70]<counts["after"][60],"New distance band fades instead of hard replacement");
   foreach(var g in groups){var l=g.GetLODs();if(l.Length!=1||l[0].renderers.Any(r=>r.GetComponent<MeshFilter>().sharedMesh.name!="VerifiedGrassNear"))throw new Exception("Grass layout changed");}
   check(groups.Length==11298,"All11298 production grass groups retain complete layout");
   var report=new{passed=checks.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),checks=checks.Count,rows,assertions=checks,fixedProjection=true,windDisabledOnOwnedMaterialCopies=true};File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{if(stage)UnityEngine.Object.DestroyImmediate(stage);if(materials!=null)foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);QualitySettings.renderPipeline=oldPipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(oldPipeline as UniversalRenderPipelineAsset,true);QualitySettings.lodBias=bias;Time.timeScale=time;foreach(var c in cameras)if(c.Key)c.Key.enabled=c.Value;style.Select(id,false);for(int j=0;j<4;j++)style.SetDetail(j,details[j],false);}
 }

 public static object PolicyTests()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||File.Exists(Root+"/policy.json"))throw new Exception("Fresh owned policy tests required");
  var registry=(System.Collections.IDictionary)typeof(WorldPerformanceManager).GetField("grassLods",Flags).GetValue(Manager);var configure=typeof(WorldPerformanceManager).GetMethod("ConfigureGrassLod",Flags);var restore=typeof(WorldPerformanceManager).GetMethod("RestoreGrassLod",BindingFlags.Static|BindingFlags.NonPublic);var stage=new GameObject("Owned grass032 policy");var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});var originals=new Dictionary<LODGroup,LOD[]>();
  try
  {
   for(int variant=0;variant<3;variant++)
   {
    var go=new GameObject("Owned grass032 group"+variant,typeof(LODGroup));go.transform.SetParent(stage.transform);go.transform.position=new Vector3(0,2200,0);var group=go.GetComponent<LODGroup>();var levels=new List<LOD>();
    for(int tier=0;tier<3;tier++){var child=new GameObject("level"+tier,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(go.transform,false);child.GetComponent<MeshFilter>().sharedMesh=variant==2?Resources.GetBuiltinResource<Mesh>("Cube.fbx"):AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass"+new[]{"Near","Far","Low"}[tier]+".asset");child.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass_0.mat");levels.Add(new LOD(new[]{.6f,.22f,.09f}[tier],new[]{child.GetComponent<MeshRenderer>()}){fadeTransitionWidth=.07f+tier*.01f});}
    group.SetLODs(levels.ToArray());group.fadeMode=variant==1?LODFadeMode.CrossFade:LODFadeMode.None;group.animateCrossFading=true;originals[group]=group.GetLODs();
    for(int cycle=0;cycle<2;cycle++)
    {
     configure.Invoke(Manager,new object[]{group.GetComponentsInChildren<MeshRenderer>()[0]});var actual=group.GetLODs();
     if(variant==0){check(actual.Length==1,"Recognized grass one complete layout cycle"+cycle);check(Mathf.Abs(actual[0].screenRelativeTransitionHeight-.045f)<.000001f,"Cull threshold halves cycle"+cycle);check(Mathf.Abs(actual[0].fadeTransitionWidth-(.045f/3)/(1-.045f))<.000001f,"Fade begins at75percent end distance cycle"+cycle);check(actual[0].renderers[0].GetComponent<MeshFilter>().sharedMesh.name=="VerifiedGrassNear","Original Near mesh cycle"+cycle);check(group.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==1,"Unused layouts stay disabled cycle"+cycle);configure.Invoke(Manager,new object[]{actual[0].renderers[0]});check(group.GetLODs()[0].screenRelativeTransitionHeight==actual[0].screenRelativeTransitionHeight,"Idempotent repeated ownership cycle"+cycle);var nativeState=registry[group];restore.Invoke(null,new object[]{group,nativeState});registry.Remove(group);check(group.GetLODs().Length==3&&group.GetComponentsInChildren<MeshRenderer>().All(r=>r.enabled),"Restore membership and original enabled flags cycle"+cycle);check(group.fadeMode==LODFadeMode.None&&group.animateCrossFading,"Restore original fade mode cycle"+cycle);check(group.GetLODs().Select((l,j)=>l.screenRelativeTransitionHeight==originals[group][j].screenRelativeTransitionHeight&&l.fadeTransitionWidth==originals[group][j].fadeTransitionWidth).All(v=>v),"Restore all original thresholds and widths cycle"+cycle);}
     else{check(actual.Length==3,"Unowned family/authored crossfade unchanged variant"+variant+" cycle"+cycle);check(!registry.Contains(group),"Unowned group not captured variant"+variant+" cycle"+cycle);check(actual.Select((l,j)=>l.screenRelativeTransitionHeight==originals[group][j].screenRelativeTransitionHeight&&l.fadeTransitionWidth==originals[group][j].fadeTransitionWidth).All(v=>v),"Unowned thresholds and widths unchanged variant"+variant+" cycle"+cycle);}
    }
   }
   var report=new{passed=rows.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),checks=rows.Count,rows};File.WriteAllText(Root+"/policy.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{foreach(var group in originals.Keys)if(registry.Contains(group)){restore.Invoke(null,new object[]{group,registry[group]});registry.Remove(group);}UnityEngine.Object.DestroyImmediate(stage);}
 }
 public static object FreshStage()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||GameObject.Find("Owned grass032 stage"))throw new Exception("Fresh environment Play required");
  var boss=GameObject.Find("CappuccinoAssassino");if(boss)boss.SetActive(false);var root=new GameObject("Owned grass032 stage");root.AddComponent<GameSaveExcluded>();var go=new GameObject("Owned grass032 fresh camera",typeof(Camera),typeof(AudioListener));go.transform.SetParent(root.transform);go.tag="MainCamera";go.transform.position=new Vector3(368.744171f,12.1212826f,359.4233f);go.GetComponent<Camera>().fieldOfView=60.4064941f;return new{fresh=true};
 }
 public static async Task<object> AuditFresh()
 {
  await Ready();var groups=Groups();var registry=(System.Collections.IDictionary)typeof(WorldPerformanceManager).GetField("grassLods",Flags).GetValue(Manager);var wrong=groups.Count(g=>g.GetLODs().Length!=1||Mathf.Abs(g.GetLODs()[0].screenRelativeTransitionHeight-.04632947f)>.00001f||g.GetLODs()[0].renderers[0].GetComponent<MeshFilter>().sharedMesh.name!="VerifiedGrassNear");
  var report=new{freshPlay=true,groups=groups.Length,registered=registry.Count,wrongPolicy=wrong,savable=GameSaveManager.HasSavableGame,styles=VisualStyleManager.Instance.Presets.Count,ready=Manager.IsReady};File.WriteAllText(Root+"/fresh-play.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 static long TriangleCount(Mesh mesh){long total=0;for(int sub=0;sub<mesh.subMeshCount;sub++)total+=(long)mesh.GetIndexCount(sub)/3;return total;}

}
