using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using Unity.Profiling;
public static class ReviewTutorialContactV234
{
 public static object Start(string label,int fps=30,bool images=true)
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required.");
  var folder=".codex/tutorial-contact-20261021/"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous take.");Directory.CreateDirectory(folder);
  var go=new GameObject("Temporary tutorial contact review v234");var observer=go.AddComponent<TutorialContactObserverV234>();observer.folder=folder;observer.fps=fps;observer.images=images;
  EditorApplication.isPaused=false;return new{folder,started=true};
 }
}
[DefaultExecutionOrder(31500)] public sealed class TutorialContactObserverV234:MonoBehaviour
{
 public string folder;public int fps;public bool images;
 const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
 Story1WreckBattle battle;Story1SharkCollisionSequence sequence;RenderTexture rt,oldTarget;Texture2D pixels;
 int frame,gateFrames,imageIndex;bool done,capture;float oldDelta;double began;int oldRate,oldSync;
 Keyboard keyboard,previousKeyboard;Mouse mouse,previousMouse;bool left,right;Key[] keys=Array.Empty<Key>();
 Story1WreckBattle.Lesson previousLesson=(Story1WreckBattle.Lesson)(-1);bool previousWaiting;
 readonly List<object> rows=new List<object>(); readonly List<object> frameMap=new List<object>();
 readonly string[] names={"Main Thread","Opening.NativeSolids","Opening.SharkHero","Opening.SharkWood","Opening.HeroWood","Opening.SkinIndex"};
 readonly List<ProfilerRecorder> profilers=new List<ProfilerRecorder>();
 object Get(string n)=>typeof(Story1WreckBattle).GetField(n,Flags).GetValue(battle);
 object Call(string n,params object[] args)=>typeof(Story1WreckBattle).GetMethod(n,Flags).Invoke(battle,args);
 float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
 float BlendWeight(){var mixer=(UnityEngine.Animations.AnimationMixerPlayable)Get("redMixer");return mixer.IsValid()?mixer.GetInputWeight(0):-1;}
 void Start()
 {
  try {
   sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();battle.DiagnosticScatterSeed=223;
   oldDelta=Time.captureDeltaTime;oldRate=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;
   Time.captureDeltaTime=1f/fps;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;began=Time.realtimeSinceStartupAsDouble;
   previousKeyboard=Keyboard.current;previousMouse=Mouse.current;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;
   if(images){rt=new RenderTexture(960,540,24);rt.Create();oldTarget=sequence.storyCamera.targetTexture;sequence.storyCamera.targetTexture=rt;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=Rendered;}
   foreach(var name in names)profilers.Add(ProfilerRecorder.StartNew(name=="Main Thread"?ProfilerCategory.Internal:ProfilerCategory.Scripts,name,1));
   if(!battle.RedesignActive)sequence.BeginImpact();
  }catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());Finish();}
 }
 void Inject(){if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
 void LateUpdate()
 {
  if(done||!battle)return;
  try {
   var stage=battle.RedesignStage;float lessonTime=(float)Get("lessonTime");
   bool tutorial=stage==Story1WreckBattle.OpeningStage.Tutorial;
   float heldGap=-1,meshGap=-1;Vector3 a=default,b=default;float radius=0;
   if(tutorial&&battle.CurrentLesson!=Story1WreckBattle.Lesson.Move&&battle.CurrentLesson!=Story1WreckBattle.Lesson.Swim&&battle.CurrentLesson!=Story1WreckBattle.Lesson.Jump)
   {
    var weapon=(CapsuleCollider)Get("redWeapon");Story1CapsuleContact.Segment(weapon,out a,out b,out radius);
    heldGap=(float)Call("RedHeldHeadGap",new object[]{null});
    var skin=(SkinnedMeshRenderer)Call("RedContactSkin");var tree=Call("RedSolidIndex",skin);float scale=skin.transform.lossyScale.x;
    meshGap=(float)tree.GetType().GetMethod("CapsuleGap").Invoke(tree,new object[]{skin.transform.InverseTransformPoint(a),skin.transform.InverseTransformPoint(b),radius/scale})*scale;
   }
   var values=profilers.Select(p=>p.Valid?p.LastValue:-1).ToArray();
   if(tutorial)rows.Add(new{frame,unityFrame=Time.frameCount,stage=stage.ToString(),lesson=battle.CurrentLesson.ToString(),battle.TutorialWaiting,lessonTime,executing=(bool)Get("lessonExecuting"),chargeHold=(float)Get("holdTime"),blendWeight=BlendWeight(),impact=(bool)Get("lessonImpact"),heldGap,meshGap,a=V(a),b=V(b),radius,hero=V(sequence.sahur.transform.position),nose=V((Vector3)Get("sharkAnimator").GetType().GetProperty("NoseWorldPoint").GetValue(Get("sharkAnimator"))),shark=V(sequence.swimmer.position),scale=V(sequence.swimmer.localScale),battle.RedesignTutorialHeroGap,battle.RedesignTutorialRenderedHitGap,battle.RedesignTutorialConfirmedHits,battle.RedesignSolidMs,battle.RedesignSolidWoodContacts,battle.RedesignSolidUnresolved,battle.RedesignSolidFractures,values});
   capture=tutorial;
   keys=Array.Empty<Key>();left=right=false;
   if(tutorial)
   {
    if(previousLesson!=battle.CurrentLesson||previousWaiting!=battle.TutorialWaiting){gateFrames=0;previousLesson=battle.CurrentLesson;previousWaiting=battle.TutorialWaiting;}
    gateFrames++;
    if(battle.TutorialWaiting&&gateFrames>=fps/2)
    {
     if(battle.CurrentLesson==Story1WreckBattle.Lesson.Move||battle.CurrentLesson==Story1WreckBattle.Lesson.Swim)keys=new[]{GameInputSettings.Get(battle.CurrentLesson==Story1WreckBattle.Lesson.Move&&fps<=10?GameInputSettings.Action.Left:GameInputSettings.Action.Forward)};
     if(battle.CurrentLesson==Story1WreckBattle.Lesson.Attack)left=(gateFrames/3)%2==0;
     if(battle.CurrentLesson==Story1WreckBattle.Lesson.Dodge&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};
     if(battle.CurrentLesson==Story1WreckBattle.Lesson.ChargeHold)right=true;
     if(battle.CurrentLesson==Story1WreckBattle.Lesson.Jump&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};
    }
    else if(!battle.TutorialWaiting&&(battle.CurrentLesson==Story1WreckBattle.Lesson.Move||battle.CurrentLesson==Story1WreckBattle.Lesson.Swim))keys=new[]{GameInputSettings.Get(battle.CurrentLesson==Story1WreckBattle.Lesson.Move&&fps<=10?GameInputSettings.Action.Left:GameInputSettings.Action.Forward)};
   }
   frame++;if(frame%fps==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,stage=stage.ToString(),lesson=battle.CurrentLesson.ToString(),lessonTime,wall=Time.realtimeSinceStartupAsDouble-began}));
   if(stage==Story1WreckBattle.OpeningStage.Pursuit||Time.realtimeSinceStartupAsDouble-began>240||frame>fps*150)Finish();
  }catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());Finish();}
 }
 void Rendered(ScriptableRenderContext context,Camera camera)
 {
  if(done||!capture||camera!=sequence.storyCamera)return;capture=false;var previous=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply(false);RenderTexture.active=previous;
  File.WriteAllBytes(folder+"/frame-"+imageIndex.ToString("D4")+".png",pixels.EncodeToPNG());frameMap.Add(new{imageIndex,frame=frame-1});imageIndex++;
 }
 void Finish(){if(done)return;done=true;File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{fps,frame,names,health=sequence.sahur.GetComponent<Mavis.PlayerHealth>().currentHealth,events=battle?.RedesignEvents,rows,frameMap,scope="Native existing Editor; exact tutorial geometry after the final solid pass. Deterministic simulation rate is not gameplay FPS. Marker values refer to prior completed frame; image capture is diagnostic."},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;}
 void OnDestroy(){if(!done&&folder!=null)Finish();InputSystem.onBeforeUpdate-=Inject;RenderPipelineManager.endCameraRendering-=Rendered;foreach(var p in profilers)p.Dispose();if(sequence&&sequence.storyCamera)sequence.storyCamera.targetTexture=oldTarget;if(rt){rt.Release();Destroy(rt);}if(pixels)Destroy(pixels);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();Time.captureDeltaTime=oldDelta;Time.timeScale=1;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;}
}
