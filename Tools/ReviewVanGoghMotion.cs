using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Threading.Tasks;
using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Mavis;using Mavis.VisualStyles;
public static class ReviewVanGoghMotion
{
 static async Task Frame(){int f=Time.frameCount;while(f==Time.frameCount){if(!Application.isPlaying)throw new Exception("Play interrupted");await Task.Delay(5);}}
 static void Capture(Camera camera,RenderTexture rt,string path)
 {
  RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
  try{im.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);im.Apply();File.WriteAllBytes(path,im.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(im);}
 }
 public static async Task<object> Run(string label="motion1")
 {
  if(!Application.isPlaying||AdditiveSceneBootstrap.IsLoading)throw new Exception("Isolated loaded Play required");
  string dir=".codex/van-gogh-2/"+label;if(Directory.Exists(dir))throw new Exception("Preserve earlier evidence");Directory.CreateDirectory(dir+"/film");
  var states=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.enabled)).ToArray();float scale=Time.timeScale,capture=Time.captureDeltaTime,renderScale=UniversalRenderPipeline.asset.renderScale;
  var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();bool enabled=player.enabled;var pose=player.transform.position;var rotation=player.transform.rotation;
  GameObject go=null;RenderTexture rt=null;var rows=new List<object>();var errors=new List<string>();void Log(string s,string t,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(s);}Application.logMessageReceived+=Log;
  try
  {
   UniversalRenderPipeline.asset.renderScale=1;Time.timeScale=0;Time.captureDeltaTime=1f/60;
   go=new GameObject("Excluded temporal and boss camera",typeof(Camera));go.AddComponent<GameSaveExcluded>();var camera=go.GetComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.fieldOfView=58;foreach(var c in states)if(c.c)c.c.enabled=false;
   rt=new RenderTexture(1280,720,24);rt.Create();var boss=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>();var center=boss.transform.position+Vector3.up*3;
   VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.Select("van-gogh",false);VisualStyleManager.Instance.SetIntensity(.9f,false);
   for(int frame=0;frame<180;frame++)
   {
    float angle=frame*2*Mathf.Deg2Rad;camera.transform.position=center+new Vector3(Mathf.Sin(angle)*18,5,-Mathf.Cos(angle)*18);camera.transform.LookAt(center);await Frame();
    if(frame%4==0)Capture(camera,rt,dir+"/film/"+(frame/4).ToString("D5")+".png");
   }
   player.enabled=false;player.ExternalControlLock=player.ExternalMovementLock=false;
   var at=boss.transform.position+boss.transform.forward*5;var island=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();if(island.GetComponent<Collider>().Raycast(new Ray(at+Vector3.up*40,Vector3.down),out var hit,100))at=hit.point;
   player.RestoreSavedPose(at+Vector3.up*(-player.LowestFootWorldOffset+.05f),Quaternion.LookRotation(boss.transform.position-at));player.GetComponent<PlayerHealth>().GrantProtection(45);boss.target=player.transform;
   Time.timeScale=1;bool transition=false,phaseTwoAttack=false;var attacks=new HashSet<string>();int film=45;
   for(int frame=0;frame<1200;frame++)
   {
    if(frame==240)boss.GetComponent<Health>().currentHealth=boss.GetComponent<Health>().maxHealth*.25f;
    center=boss.transform.position+Vector3.up*3;camera.transform.position=Vector3.Lerp(camera.transform.position,center+new Vector3(-12,6,-16),.12f);camera.transform.LookAt(center);
    await Frame();transition|=boss.InPhaseTransition;phaseTwoAttack|=boss.FightPhase==2&&boss.CurrentState==CappuccinoAI.State.Attack;
    if(boss.CurrentState==CappuccinoAI.State.Attack)attacks.Add(boss.CurrentAttack.ToString());
    if(frame%4==0)Capture(camera,rt,dir+"/film/"+(film++).ToString("D5")+".png");
    if(frame%15==0)rows.Add(new{frame,state=boss.CurrentState.ToString(),boss.FightPhase,attack=boss.CurrentAttack.ToString(),boss.DamageWindowActive,draws=VanGoghPainting.VisibleSkinDraws});
   }
   Time.timeScale=0;var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();float hour=day.startHour;bool runs=day.clockRuns;var field=typeof(DayNightCycle).GetField("elapsedGameHours",BindingFlags.NonPublic|BindingFlags.Instance);float elapsed=(float)field.GetValue(day);
   try{day.clockRuns=false;field.SetValue(day,0f);foreach(float h in new[]{8f,17.5f,23f}){day.startHour=h;typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(day,null);await Frame();Capture(camera,rt,dir+"/light-"+h.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png");}}
   finally{day.startHour=hour;day.clockRuns=runs;field.SetValue(day,elapsed);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(day,null);}
   var result=new{errors,transition,phaseTwoAttack,attacks,rows,filmFrames=film,scope="1280x720 native, fixed60Hz actual AI. First3s:120-degree/s frozen-world camera orbit; next20s: real boss AI and existing health threshold transition. Runtime-only protected target stands still, not a complete player-controlled fight."};File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{transition,phaseTwoAttack,attacks,errors,film};
  }
  finally{Application.logMessageReceived-=Log;Time.timeScale=scale;Time.captureDeltaTime=capture;UniversalRenderPipeline.asset.renderScale=renderScale;player.RestoreSavedPose(pose,rotation);player.enabled=enabled;VanGoghPainting.LegacyComparison=false;VisualStyleManager.Instance.RestorePreference();foreach(var s in states)if(s.c)s.c.enabled=s.enabled;if(go)UnityEngine.Object.DestroyImmediate(go);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
 }
}
