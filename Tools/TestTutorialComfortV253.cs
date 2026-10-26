using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
public static class TestTutorialComfortV253
{
 public static object OpenStory()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve open work before the native test");
  EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Start(string label)
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
  string folder=".codex/comfort-v253/"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous take");Directory.CreateDirectory(folder);
  var go=new GameObject("Temporary tutorial comfort review");go.AddComponent<TutorialComfortObserverV253>().folder=folder;EditorApplication.isPaused=false;return new{started=true,folder};
 }
}
[DefaultExecutionOrder(31900)] public sealed class TutorialComfortObserverV253:MonoBehaviour
{
 public string folder;Story1WreckBattle battle;Story1SharkCollisionSequence seq;ThirdPersonPlayerController hero;
 Keyboard keyboard,oldKeyboard;Mouse mouse,oldMouse;Key[] keys=Array.Empty<Key>();bool left,right,done,capture;int frame,gateFrames,image;
 float oldDelta;int oldSync,oldRate;Story1WreckBattle.Lesson previous=(Story1WreckBattle.Lesson)(-1);bool previousWaiting;
 RenderTexture rt,oldTarget;Texture2D pixels;readonly List<object> rows=new List<object>();readonly List<string> checks=new List<string>();
 int walkSamples,swimmingSamples,outsideSamples,walkClipSamples;float maxWalking,autoTravel;Vector3 lastAuto;bool autoStarted;
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 object Field(string name)=>typeof(Story1WreckBattle).GetField(name,F).GetValue(battle);
 void Check(bool value,string text){if(!value)throw new Exception(text);checks.Add(text);}
 void Start()
 {
  try
  {
   seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=seq.GetComponent<Story1WreckBattle>();hero=seq.sahur;battle.DiagnosticScatterSeed=223;
   oldDelta=Time.captureDeltaTime;oldSync=QualitySettings.vSyncCount;oldRate=Application.targetFrameRate;Time.captureDeltaTime=1f/30;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
   oldKeyboard=Keyboard.current;oldMouse=Mouse.current;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;
   rt=new RenderTexture(960,540,24);rt.Create();oldTarget=seq.storyCamera.targetTexture;seq.storyCamera.targetTexture=rt;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=Rendered;
   if(!battle.RedesignActive)seq.BeginImpact();
  }catch(Exception e){Fail(e);}
 }
 void Inject(){if(done||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
 void LateUpdate()
 {
  if(done||!battle)return;
  try
  {
   frame++;keys=Array.Empty<Key>();left=right=false;
   var lesson=battle.CurrentLesson;bool waiting=battle.TutorialWaiting;
   if(lesson!=previous||waiting!=previousWaiting)gateFrames=0;else gateFrames++;
   previous=lesson;previousWaiting=waiting;
   var rider=hero.GetComponent<Story1WreckRider>();
   if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&lesson==Story1WreckBattle.Lesson.Move)
   {
    float t=battle.RedesignWalkControlSeconds;maxWalking=Mathf.Max(maxWalking,t);
    var dir=t<4?GameInputSettings.Action.Forward:GameInputSettings.Action.Back;
    var cross=t<4?GameInputSettings.Action.Right:GameInputSettings.Action.Left;
    var list=new List<Key>{GameInputSettings.Get(dir),GameInputSettings.Get(cross)};
    if(((int)(t*4))%2==0){list.Add(GameInputSettings.Get(GameInputSettings.Action.Jump));list.Add(GameInputSettings.Get(GameInputSettings.Action.Dodge));}
    keys=list.ToArray();
    if(rider.WalkingTutorialActive)
    {
     walkSamples++;if(hero.Swimming)swimmingSamples++;
     var deck=(Story1WreckPlank)Field("redCombatBoard");var feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
     if(!deck.ContainsFootprint(feet))outsideSamples++;
     if(frame%30==0)capture=true;
     rows.Add(new{frame,walkSeconds=t,swimming=hero.Swimming,inside=deck.ContainsFootprint(feet),position=new[]{feet.x,feet.y,feet.z}});
    }
   }
   else if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial)
   {
    if(lesson==Story1WreckBattle.Lesson.Attack&&!waiting)
    {
     var at=hero.transform.position;if(autoStarted)autoTravel+=Vector3.ProjectOnPlane(at-lastAuto,Vector3.up).magnitude;autoStarted=true;lastAuto=at;
     var clip=Field("redClip") as AnimationClip;if(clip&&clip.name=="PLAYER_Walk_v241")walkClipSamples++;
     if(frame%12==0)capture=true;
    }
    if(waiting)
    {
     if(lesson==Story1WreckBattle.Lesson.Attack)left=gateFrames==12;
     if(lesson==Story1WreckBattle.Lesson.Dodge&&gateFrames==12)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};
     if(lesson==Story1WreckBattle.Lesson.ChargeHold)right=true;
     if(lesson==Story1WreckBattle.Lesson.ChargeRelease)right=gateFrames<12;
    }
   }
   if(battle.RedesignTutorialConfirmedHits>=2){capture=true;Finish();}
   if(frame>3300)throw new Exception("Native tutorial timed out: "+battle.RedesignStage+" / "+battle.CurrentLesson+" / "+battle.TutorialWaiting);
  }catch(Exception e){Fail(e);}
 }
 void Rendered(ScriptableRenderContext context,Camera camera)
 {
  if(!capture||camera!=seq.storyCamera||pixels==null)return;capture=false;var previous=RenderTexture.active;
  try{RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/frame-"+(image++).ToString("D3")+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
 }
 async void Finish()
 {
  if(done)return;done=true;
  try
  {
   await Task.Delay(60);Time.timeScale=0;
   Check(walkSamples>=220,"Eight-second walk observed over native frames");Check(maxWalking>=battle.RedesignWalkMinimumSeconds-.08f,"Walking duration retained");
   Check(swimmingSamples==0&&outsideSamples==0,"All walking samples stayed on the native deck despite diagonal/jump/dodge input");
   Check(hero.WalkingTutorialConstraint==null&&!hero.GetComponent<Story1WreckRider>().WalkingTutorialActive,"Walking restriction released before attacks");
   Check(walkClipSamples>0&&autoTravel>.1f,"Automatic reposition used the authored walk before the attack gate");
   Check(battle.RedesignTutorialConfirmedHits==2&&battle.RedesignTutorialRenderedHitGap<=.08f,"Both tutorial strikes confirmed actual weapon contact");
   await TestHealing();
   File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks,frame,walkSamples,maxWalking,swimmingSamples,outsideSamples,walkClipSamples,autoTravel,hits=battle.RedesignTutorialConfirmedHits,hitGap=battle.RedesignTutorialRenderedHitGap,rows},Newtonsoft.Json.Formatting.Indented));
  }catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());}
  finally{if(seq&&seq.storyCamera)seq.storyCamera.targetTexture=oldTarget;EditorApplication.isPlaying=false;}
 }
 async Task TestHealing()
 {
  string id=HealingPackSupply.Campaign;int count=HealingPackSupply.Remaining;GameObject actor=null,pointObject=null;
  try
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");actor=UnityEngine.Object.Instantiate(prefab,new Vector3(1500,500,1500),Quaternion.identity);
   pointObject=new GameObject("Temporary healing respawn checkpoint");pointObject.transform.position=new Vector3(1500,500,1500);var point=pointObject.AddComponent<IslandRespawnPoint>();
   await Task.Delay(80);Time.timeScale=1;HealingPackSupply.StartNewGame();string campaign=HealingPackSupply.Campaign;
   Check(HealingPackSupply.Total==3&&HealingPackSupply.Remaining==3,"New life has three packs");
   var consume=typeof(HealingPackSupply).GetMethod("Consume",BindingFlags.Static|BindingFlags.NonPublic);consume.Invoke(null,null);consume.Invoke(null,null);consume.Invoke(null,null);
   Check(!(bool)consume.Invoke(null,null)&&HealingPackSupply.Remaining==0,"Empty supply cannot be consumed");
   HealingPackSupply.RestoreCampaign(campaign,3);Check(HealingPackSupply.Remaining==0,"Continue cannot refund consumed packs");
   var hp=actor.GetComponent<PlayerHealth>();var life=actor.GetComponent<PlayerDeathRespawn>();life.SetCheckpoint(point);hp.GrantProtection(0);hp.currentHealth=0;life.Die();
   Check(life.IsDead&&life.PanelVisible,"Real death flow entered");Check(life.Respawn(),"Real respawn completed");
   Check(HealingPackSupply.Remaining==3&&HealingPackSupply.Campaign==campaign,"Respawn refills all three without replacing the campaign");
   HealingPackSupply.RestoreCampaign(campaign,0);Check(HealingPackSupply.Remaining==3,"Old checkpoint does not undo the respawn refill");
   consume.Invoke(null,null);Check(!life.Respawn()&&HealingPackSupply.Remaining==2,"Live respawn call cannot refill packs");
   hp.GrantProtection(0);hp.currentHealth=0;life.Die();Check(life.Respawn()&&HealingPackSupply.Remaining==3,"Second death also refills all packs");
   Check(hp.currentHealth==hp.maxHealth&&actor.GetComponent<PlayerStamina>().currentStamina==actor.GetComponent<PlayerStamina>().maxStamina,"Health and stamina restoration retained");
   Check(GameLocalization.Format("healing.count","H",3,HealingPackSupply.Total).Contains("3/3"),"HUD capacity reads three");
  }
  finally{if(actor)UnityEngine.Object.DestroyImmediate(actor);if(pointObject)UnityEngine.Object.DestroyImmediate(pointObject);HealingPackSupply.RestoreCampaign(id,count);Time.timeScale=0;}
 }
 void Fail(Exception e){if(done)return;done=true;File.WriteAllText(folder+"/error.txt",e.ToString());if(seq&&seq.storyCamera)seq.storyCamera.targetTexture=oldTarget;EditorApplication.isPlaying=false;}
 void OnDestroy()
 {
  InputSystem.onBeforeUpdate-=Inject;RenderPipelineManager.endCameraRendering-=Rendered;
  if(seq&&seq.storyCamera)seq.storyCamera.targetTexture=oldTarget;if(rt){rt.Release();DestroyImmediate(rt);}if(pixels)DestroyImmediate(pixels);
  if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();Time.captureDeltaTime=oldDelta;QualitySettings.vSyncCount=oldSync;Application.targetFrameRate=oldRate;
 }
}
