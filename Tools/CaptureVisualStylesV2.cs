using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
using Mavis.VisualStyles;

/// <summary>Regenerates matching static previews from the actual First Island Environment/Lighting scenes.</summary>
public static class CaptureVisualStylesV2
{
 const string Root=".codex/visual-style-v2";
 public static object Prepare()
 {
  if(Application.isPlaying)throw new Exception("Stopped Editor required");
  var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve unsaved scenes before preparing the preview stage");
  Directory.CreateDirectory(Root);File.WriteAllText(Root+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));
  var environment=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
  var lighting=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
  SceneManager.SetActiveScene(environment);return new{prepared=true,environment=environment.path,lighting=lighting.path};
 }
 public static object Restore()
 {
  if(Application.isPlaying)throw new Exception("Stop test Play before restoring workspace");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/workspace-before.json")));
  return new{restored=true};
 }
 public static object Start()
 {
  if(!Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).name!="Environment"&&SceneManager.GetSceneAt(i).name!="Lighting"))throw new Exception("Only prepared Environment/Lighting Play is allowed");
  if(File.Exists(Root+"/capture-running"))throw new Exception("Capture already active");File.Delete(Root+"/capture-complete.json");File.Delete(Root+"/capture-error.txt");File.WriteAllText(Root+"/capture-running",DateTime.UtcNow.ToString("o"));_=Run();return new{started=true};
 }
 static async Task Frame(){int n=Time.frameCount;var limit=DateTime.UtcNow.AddSeconds(45);while(Time.frameCount==n){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Preview frame stalled");await Task.Delay(5);}}
 static async Task Render(Camera camera,string path)
 {
  bool done=false;Action<ScriptableRenderContext,Camera> callback=(context,cam)=>{if(cam!=camera||done)return;done=true;var texture=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);var before=RenderTexture.active;try{RenderTexture.active=camera.targetTexture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}finally{RenderTexture.active=before;UnityEngine.Object.DestroyImmediate(texture);}};
  RenderPipelineManager.endCameraRendering+=callback;
  try{for(int i=0;i<90&&!done;i++)await Frame();if(!done)throw new Exception("Native preview camera did not render");}
  finally{RenderPipelineManager.endCameraRendering-=callback;}
 }
 static GameObject SpawnExcluded(string path,Transform parent)
 {
  var holder=new GameObject("Excluded capture actor");holder.transform.SetParent(parent,false);holder.SetActive(false);
  var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),holder.transform);
  actor.AddComponent<GameSaveExcluded>();foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
  foreach(var agent in actor.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))agent.enabled=false;
  holder.SetActive(true);foreach(var canvas in actor.GetComponentsInChildren<Canvas>(true))canvas.enabled=false;return actor;
 }
 static async Task Run()
 {
  GameObject root=null;RenderTexture target=null;var originalPipeline=QualitySettings.renderPipeline;UniversalRenderPipelineAsset pipeline=null;
  float time=Time.timeScale;var manager=VisualStyleManager.Instance;string old=manager.Current.id;float intensity=manager.Intensity;
  var enabled=UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None).Where(b=>b is Camera||b is Canvas||b is AudioListener||(b is MonoBehaviour && b.gameObject.scene.name!="Environment"&&b.gameObject.scene.name!="Lighting"&&b!=manager)).Select(b=>(b,b.enabled)).ToArray();
  var otherRoots=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s.name!="Environment"&&s.name!="Lighting").SelectMany(s=>s.GetRootGameObjects()).Select(go=>(go,go.activeSelf)).ToArray();
  try
  {
   foreach(var entry in otherRoots)entry.go.SetActive(false);
   foreach(var entry in enabled)entry.b.enabled=false;
   Pixel360Rendering.ReleaseCurrent();
   pipeline=UnityEngine.Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);pipeline.name="Temporary full-resolution visual preview";pipeline.renderScale=1;pipeline.msaaSampleCount=1;pipeline.shadowDistance=160;QualitySettings.renderPipeline=pipeline;
   root=new GameObject("Temporary visual style capture (not saved)");root.AddComponent<GameSaveExcluded>();
   var island=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).OrderByDescending(i=>i.GetComponent<Renderer>().bounds.size.magnitude).First();
   var collider=island.GetComponent<MeshCollider>();var bounds=collider.bounds;var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();float sea=ocean?ocean.oceanHeight:0;
   // Pick an actual shoreline location with a clear seaward foreground; existing vegetation/rocks are retained.
   Vector3 center=bounds.center;float best=float.MaxValue;Vector3 shore=center;
   for(int a=0;a<24;a++)for(int r=5;r<80;r+=2)
   {
    float angle=a*Mathf.PI/12;Vector3 p=new Vector3(center.x+Mathf.Sin(angle)*r,bounds.max.y+50,center.z+Mathf.Cos(angle)*r);
    if(!collider.Raycast(new Ray(p,Vector3.down),out var hit,1000))continue;
    float score=Mathf.Abs(hit.point.y-(sea+2.5f))+Mathf.Abs(a-15)*.12f;
    if(score<best){best=score;shore=hit.point;}
   }
   Vector3 inland=Vector3.ProjectOnPlane(center-shore,Vector3.up).normalized;if(inland.sqrMagnitude<.1f)inland=Vector3.forward;Vector3 right=Vector3.Cross(Vector3.up,inland);
   float Ground(Vector3 p){return collider.Raycast(new Ray(new Vector3(p.x,bounds.max.y+40,p.z),Vector3.down),out var h,2000)?h.point.y:shore.y;}
   var actor=SpawnExcluded("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab",root.transform);
   // Exclusion exists before actor activation.foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
   var cap=actor.GetComponent<CharacterController>();Vector3 at=shore+right*2.6f+inland*2;at.y=Ground(at)-(cap.center.y-cap.height*.5f)*actor.transform.lossyScale.y+.08f;
   actor.transform.SetPositionAndRotation(at,Quaternion.LookRotation(inland*.7f+right*.4f));
   var animator=actor.GetComponentInChildren<Animator>();animator.Play("Locomotion",0,.4f);animator.Update(0);animator.speed=0;
   var enemy=SpawnExcluded("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab",root.transform);
   foreach(var script in enemy.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
   var nav=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav)nav.enabled=false;
   Vector3 enemyAt=shore-right*4+inland*5;enemyAt.y=Ground(enemyAt)+.1f;enemy.transform.SetPositionAndRotation(enemyAt,Quaternion.LookRotation(inland));
   var enemyAnimator=enemy.GetComponentInChildren<Animator>();if(enemyAnimator){enemyAnimator.Update(0);enemyAnimator.speed=0;}
   var go=new GameObject("Matching island preview camera",typeof(Camera));go.transform.SetParent(root.transform);go.tag="MainCamera";var camera=go.GetComponent<Camera>();camera.fieldOfView=52;camera.nearClipPlane=.15f;camera.farClipPlane=1200;
   Vector3 aim=shore+inland*1+Vector3.up*3.8f;Vector3 cameraAt=shore+inland*11+right*12;cameraAt.y=Ground(cameraAt)+5.5f;camera.transform.position=cameraAt;camera.transform.LookAt(aim);
   target=new RenderTexture(1600,900,24){name="Matching static visual style previews"};target.Create();camera.targetTexture=target;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   root.SetActive(true);
   var deadline=DateTime.UtcNow.AddSeconds(90);
   var performance=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().SingleOrDefault(m=>m.gameObject.scene.IsValid());
   while(performance&&!performance.IsReady){if(DateTime.UtcNow>deadline)throw new Exception("World batching did not settle");await Frame();}
   for(int i=0;i<8;i++)await Frame();Time.timeScale=0;
   Directory.CreateDirectory("Assets/Resources/VisualStyles/Previews");
   var reports=new List<object>();
   foreach(string id in new[]{"realistic","dark-fairytale","origami","monochrome"})
   {
    manager.Select(id,false);manager.SetIntensity(.85f,false);await Frame();await Render(camera,"Assets/Resources/VisualStyles/Previews/"+id+".png");
    reports.Add(new{id,position=new[]{camera.transform.position.x,camera.transform.position.y,camera.transform.position.z},rotation=new[]{camera.transform.rotation.x,camera.transform.rotation.y,camera.transform.rotation.z,camera.transform.rotation.w},time=Time.time,actor=new[]{at.x,at.y,at.z},enemy=new[]{enemyAt.x,enemyAt.y,enemyAt.z},scene=island.gameObject.scene.path});
   }
   File.WriteAllText(Root+"/preview-provenance.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
   AssetDatabase.Refresh();VisualStyleSetupV2.AssignPreviews();File.WriteAllText(Root+"/capture-complete.json","{\"passed\":true}");
  }
  catch(Exception error){File.WriteAllText(Root+"/capture-error.txt",error.ToString());}
  finally
  {
   manager.Select(old,false);manager.SetIntensity(intensity,false);Time.timeScale=time;
   QualitySettings.renderPipeline=originalPipeline;if(pipeline)UnityEngine.Object.DestroyImmediate(pipeline);
   if(root)UnityEngine.Object.DestroyImmediate(root);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}
   foreach(var entry in enabled)if(entry.b)entry.b.enabled=entry.enabled;
   foreach(var entry in otherRoots)if(entry.go)entry.go.SetActive(entry.activeSelf);
   if(File.Exists(Root+"/capture-running"))File.Delete(Root+"/capture-running");
   EditorApplication.isPlaying=false;
  }
 }
}

// V2 matched framing keeps the original character and boss readable in gallery cards.
