using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
using Object=UnityEngine.Object;

public static class VerifyGameplayPolishRoute
{
 const string W=".codex/gameplay-polish/route";
 static readonly List<object> checks=new List<object>();
 static readonly List<string> errors=new List<string>();
 static string phase;
 static void Check(bool ok,string name,object evidence=null){checks.Add(new{ok,name,evidence});if(!ok)throw new Exception(name);}
 static void Phase(string value){phase=value;File.WriteAllText(W+"/test-status.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{phase,checks=checks.Count}));}
 static async Task Wait(Func<bool> predicate,float seconds){var until=DateTime.UtcNow.AddSeconds(seconds);while(!predicate()){if(!Application.isPlaying||DateTime.UtcNow>until)throw new Exception("Timeout in "+phase);await Task.Delay(20);}}
 static async Task Seconds(float value){float end=Time.unscaledTime+value;await Wait(()=>Time.unscaledTime>=end,Mathf.Max(20,value*3));}
 static void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);}
 public static object Start(){
  if(!Application.isPlaying||SceneManager.GetSceneByName("Gameplay").isLoaded||SceneManager.GetSceneByName("Enemies").isLoaded)throw new Exception("Isolated Environment + Lighting Play required");
  Directory.CreateDirectory(W);_ = Run();return "Started isolated story-route verification";
 }
 static async Task Capture(string name,Vector3 at){
  var go=new GameObject("Disposable route review camera",typeof(Camera));var c=go.GetComponent<Camera>();c.fieldOfView=58;c.farClipPlane=1400;c.nearClipPlane=.2f;
  c.transform.SetPositionAndRotation(at+new Vector3(33,23,-39),Quaternion.LookRotation(new Vector3(-33,-21,39)));
  c.GetUniversalAdditionalCameraData().renderPostProcessing=true;var rt=new RenderTexture(1600,900,24);rt.Create();c.targetTexture=rt;
  var previous=RenderTexture.active;var old=QualitySettings.renderPipeline;var pipeline=Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);
  try{Pixel360Rendering.ReleaseCurrent();pipeline.renderScale=1;QualitySettings.renderPipeline=pipeline;await Seconds(.5f);RenderTexture.active=rt;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(W+"/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);}
  finally{RenderTexture.active=previous;QualitySettings.renderPipeline=old;c.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(go);Object.Destroy(pipeline);}
 }
 static async Task Run(){
  typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Path.GetFullPath(W+"/fixture-save.json"));
  Application.logMessageReceived+=Log;
  try{
   Phase("loading disposable gameplay");
   foreach(var name in new[]{"Gameplay","Enemies"}){var op=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/"+name+".unity",LoadSceneMode.Additive);await Wait(()=>op.isDone,45);}
   var player=Object.FindFirstObjectByType<ThirdPersonPlayerController>();player.gameObject.AddComponent<GameSaveExcluded>();
   var arrival=player.GetComponent<FirstIslandArrival>();if(arrival){arrival.StopAllCoroutines();arrival.RestoreProgress(true,true);arrival.enabled=false;}
   player.ExternalControlLock=player.ExternalMovementLock=false;
   var body=player.GetComponent<CharacterController>();var health=player.GetComponent<PlayerHealth>();health.NarrativeDefeatHandler=null;health.GrantProtection(300);
   var respawn=player.GetComponent<PlayerDeathRespawn>();var stamina=player.GetComponent<PlayerStamina>();
   var cap=Object.FindFirstObjectByType<CappuccinoAI>();var flock=Object.FindFirstObjectByType<ChickenBroEncounter>();var nail=Object.FindFirstObjectByType<NailongAI>();var plane=Object.FindFirstObjectByType<BombardinoBoss>();
   var capHealth=cap.GetComponent<Health>();var nailHealth=nail.GetComponent<NailongHealth>();var planeHealth=plane.GetComponent<Health>();
   var points=Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None).OrderBy(p=>p.transform.position.z).ToArray();
   float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
   var island=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled);var ground=island.GetComponent<MeshCollider>();
   Vector3 Ground(Vector3 p){if(!ground.Raycast(new Ray(new Vector3(p.x,200,p.z),Vector3.down),out var h,400))throw new Exception("Off island");return h.point;}
   void Move(Vector3 p){player.ExternalControlLock=player.ExternalMovementLock=false;player.RestoreSavedPose(Ground(p)+Vector3.up*(.08f-sole),Quaternion.identity);}
   Phase("checkpoint and unchanged roster verification");
   Check(Object.FindObjectsByType<ChickenBroAI>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"No added patrol enemies at scene start");
   Check(Object.FindObjectsByType<CappuccinoAI>(FindObjectsSortMode.None).Length==1&&Object.FindObjectsByType<NailongAI>(FindObjectsSortMode.None).Length==1&&Object.FindObjectsByType<BombardinoBoss>(FindObjectsSortMode.None).Length==1,"Original three boss roster");
   Check(flock.waveCounts.SequenceEqual(new[]{4,6,8})&&flock.maxAlive==8&&flock.goosePerWave==1,"Original flock counts and mix retained");
   await Wait(()=>flock.NavigationReady,60);
   Check(Vector2.Distance(new Vector2(flock.transform.position.x,flock.transform.position.z),new Vector2(142,565))<.1f,"Flock keeps its authored site after runtime initialization");
   Check(points.Length==4&&points.Count(p=>p.isDefault)==1,"Four visited checkpoints, one default");
   foreach(var p in points){
    Move(p.GroundPosition);await Seconds(.3f);Check(respawn.Checkpoint==p,"Checkpoint activates",p.pointName);
    var snapshot=GameWorldProgress.Capture(player);Check(snapshot.checkpoint==p.GetComponent<WorldSaveIdentity>().id,"Checkpoint captured in save",p.pointName);
    health.currentHealth=0;respawn.Die();Check(respawn.IsDead&&respawn.Respawn(),"Death and respawn succeeds",p.pointName);await Seconds(.2f);health.GrantProtection(300);
    var feet=body.bounds.center-Vector3.up*body.bounds.extents.y;Check(Vector3.Distance(feet,p.GroundPosition)<.6f,"Respawn returns to safe ground",p.pointName);
    Check(stamina.Current==stamina.Maximum&&health.currentHealth==health.maxHealth,"Vitals restored");
    respawn.SetCheckpoint(points.First(x=>x!=p));GameWorldProgress.Restore(snapshot,player);Check(respawn.Checkpoint==p,"Checkpoint restored from save",p.pointName);
   }
   Check(!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Nailong and aircraft initially locked");
   float nh=nailHealth.currentHealth,bh=planeHealth.currentHealth;nailHealth.ApplyDamage(1,nail.transform.position);planeHealth.ApplyDamage(1,plane.transform.position);
   Check(nailHealth.currentHealth==nh&&planeHealth.currentHealth==bh,"Locked bosses reject damage");
   Phase("Cappuccino then original flock waves");Move(cap.transform.position+Vector3.back*10);await Seconds(.4f);await Capture("cappuccino-clearing",Ground(cap.transform.position));capHealth.Kill();
   Check(FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Capri)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Cappuccino victory still requires flock before Nailong");
   Move(flock.transform.position+Vector3.back*18);await Wait(()=>flock.Started&&flock.Alive>0,20);await Capture("flock-clearing",flock.transform.position);
   var seen=new HashSet<int>();var breeds=new Dictionary<string,int>();var deadline=DateTime.UtcNow.AddSeconds(65);
   while(!flock.Completed){
    if(DateTime.UtcNow>deadline)throw new Exception("Original flock waves did not complete");
    foreach(var bird in Object.FindObjectsByType<ChickenBroAI>(FindObjectsSortMode.None))if(bird.IsAlive&&seen.Add(bird.GetInstanceID())){
     string type=bird.name.Split('(')[0];breeds[type]=breeds.TryGetValue(type,out int n)?n+1:1;bird.GetComponent<Health>().Kill();
    }
    await Seconds(.12f);
   }
   Check(seen.Count==18,"All original 18 wave enemies, no extras",breeds);
   Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Flock unlocks Nailong only");
   Phase("Nailong and rifle reward route");Move(nail.transform.position+Vector3.back*15);await Seconds(.3f);await Capture("nailong-clearing",Ground(nail.transform.position));nailHealth.ApplyDamage(nailHealth.maxHealth+1,nail.transform.position);
   Check(FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Nailong)&&FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Nailong unlocks aircraft");
   var rifle=Object.FindObjectsByType<PlacedEquipmentPickup>(FindObjectsSortMode.None).Single(p=>p.name=="M4A1");
   Check(FirstIslandBossProgression.CanObtainRifle(rifle)&&Vector2.Distance(new Vector2(rifle.transform.position.x,rifle.transform.position.z),new Vector2(20,710))<30,"Existing rifle lies beside defeated Nailong");
   Phase("aircraft defeat and capture");Move(new Vector3(-80,0,800));await Wait(()=>plane.FightActive,15);await Capture("aircraft-clearing",new Vector3(-114,32.94f,826));planeHealth.Kill();
   Check(plane.AbductionActive&&BombardinoAbduction.Playing,"Aircraft defeat starts existing capture sequence");
   var capture=plane.GetComponent<BombardinoAbduction>();await Wait(()=>capture&&capture.Phase=="climb",10);
   Check(player.ExternalControlLock,"Capture owns player movement at the relocated fight");
   Phase("capture transit to second island");
   await Wait(()=>SceneManager.GetSceneByPath("Assets/Scenes/Second Island/Second Island.unity").isLoaded,90);
   await Wait(()=>!BombardinoAbduction.Playing,45);
   Check(Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None).Any(p=>p.gameObject.scene.path=="Assets/Scenes/Second Island/Second Island.unity"),"Existing capture delivers player to second island");
   Check(errors.Count==0,"No runtime errors",errors.ToArray());Phase("complete");
  }catch(Exception e){errors.Add(e.ToString());Phase("failed");}
  finally{Application.logMessageReceived-=Log;File.WriteAllText(W+"/story-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,phase,checks,errors,scope="Scripted defeats validate placement, original spawn counts and progression/capture; not manual combat balance."},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;}
 }
}
