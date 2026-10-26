using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class ReviewLiveDrawV254
{
 const string Root=".codex/draw-retrieve-v254";
 public static object Open()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve open work");
  var state=new{scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray(),campaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining};
  File.WriteAllText(Root+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(state,Newtonsoft.Json.Formatting.Indented));
  EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Start(string label)
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required");string path=Root+"/"+label;if(Directory.Exists(path))throw new Exception("Preserve take");Directory.CreateDirectory(path);
  var go=new GameObject("Temporary live draw review");go.AddComponent<LiveDrawObserverV254>().folder=path;EditorApplication.isPaused=false;return new{started=true};
 }
}
[DefaultExecutionOrder(31900)] public sealed class LiveDrawObserverV254:MonoBehaviour
{
 public string folder;Story1SharkCollisionSequence seq;Story1WreckBattle battle;SahurSwimmingWeapon stow;Animator animator;Mavis.SahurAttack attack;
 Keyboard keyboard,oldKeyboard;Mouse mouse,oldMouse;Key[] keys=Array.Empty<Key>();bool left,right,done,capture;
 float oldDelta;int oldRate,oldSync,frame,gateFrames,image;Story1WreckBattle.Lesson previous=(Story1WreckBattle.Lesson)(-1);bool previousWaiting;
 RenderTexture rt,oldTarget;Texture2D pixels;Mesh baked;SkinnedMeshRenderer skin;int[] triangles;object tree;Type treeType;
 readonly List<object> rows=new List<object>();float worst=float.PositiveInfinity,oldWorst=float.PositiveInfinity;int bad,oldBad;bool sawClimb,returned;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string name)=>typeof(Story1WreckBattle).GetField(name,F).GetValue(battle);
 void Start()
 {
  try
  {
   seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=seq.GetComponent<Story1WreckBattle>();stow=seq.sahur.GetComponent<SahurSwimmingWeapon>();animator=seq.sahur.CharacterAnimator;attack=seq.sahur.GetComponent<SahurAttack>();
   oldDelta=Time.captureDeltaTime;oldRate=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;Time.captureDeltaTime=1f/30;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
   oldKeyboard=Keyboard.current;oldMouse=Mouse.current;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;
   rt=new RenderTexture(960,540,24);rt.Create();oldTarget=seq.storyCamera.targetTexture;seq.storyCamera.targetTexture=rt;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=Rendered;
   baked=new Mesh();
   var group=new HashSet<Transform>();foreach(var b in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg}){var t=animator.GetBoneTransform(b);if(t)group.Add(t);}
   skin=seq.sahur.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.bones.Any(group.Contains)).OrderByDescending(s=>s.sharedMesh.vertexCount).First();
   // Exact CPU metadata from an isolated readable clone prepared in Edit Mode;
   // the production mesh's memory optimization and source assets stay intact.
   BoneWeight[] weights;int[] all;
   using(var reader=new BinaryReader(File.OpenRead(".codex/draw-retrieve-v254/finger-topology.bin")))
   {
    weights=new BoneWeight[reader.ReadInt32()];for(int i=0;i<weights.Length;i++)weights[i]=new BoneWeight{boneIndex0=reader.ReadInt32(),weight0=reader.ReadSingle(),boneIndex1=reader.ReadInt32(),weight1=reader.ReadSingle(),boneIndex2=reader.ReadInt32(),weight2=reader.ReadSingle(),boneIndex3=reader.ReadInt32(),weight3=reader.ReadSingle()};
    all=new int[reader.ReadInt32()];for(int i=0;i<all.Length;i++)all[i]=reader.ReadInt32();
   }
   if(weights.Length!=skin.sharedMesh.vertexCount||skin.sharedMesh.name!="SahurFingerSkinnedMesh")throw new Exception("Native topology correspondence required");
   File.WriteAllText(folder+"/skin.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {skin=skin.name,mesh=skin.sharedMesh.name,skin.sharedMesh.isReadable,vertices=skin.sharedMesh.vertexCount,bones=skin.bones.Select(x=>x.name).ToArray(),human=group.Select(x=>x.name).ToArray()}));
   var membership=weights.Select(w=>{float W(int i,float a)=>i>=0&&i<skin.bones.Length&&group.Contains(skin.bones[i])?a:0;return W(w.boneIndex0,w.weight0)+W(w.boneIndex1,w.weight1)+W(w.boneIndex2,w.weight2)+W(w.boneIndex3,w.weight3);}).ToArray();
   var selected=new List<int>();for(int i=0;i<all.Length;i+=3)if((membership[all[i]]+membership[all[i+1]]+membership[all[i+2]])/3>.6f){selected.Add(all[i]);selected.Add(all[i+1]);selected.Add(all[i+2]);}triangles=selected.ToArray();treeType=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
   if(triangles.Length==0)throw new Exception("The review must inspect the animated hero skin");
   if(!battle.RedesignActive)seq.BeginImpact();
  }catch(Exception e){Finish(e);}
 }
 void Inject(){if(done||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
 void LateUpdate()
 {
  if(done||!battle)return;
  try
  {
   frame++;keys=Array.Empty<Key>();left=right=false;
   if(previous!=battle.CurrentLesson||previousWaiting!=battle.TutorialWaiting)gateFrames=0;else gateFrames++;previous=battle.CurrentLesson;previousWaiting=battle.TutorialWaiting;
   if(battle.TutorialWaiting&&gateFrames>=12)
   {
    var lesson=battle.CurrentLesson;
    if(lesson==Story1WreckBattle.Lesson.Move||lesson==Story1WreckBattle.Lesson.Swim)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
    if(lesson==Story1WreckBattle.Lesson.Attack)left=(gateFrames/3)%2==0;
    if(lesson==Story1WreckBattle.Lesson.Dodge&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};
    if(lesson==Story1WreckBattle.Lesson.ChargeHold)right=true;
    if(lesson==Story1WreckBattle.Lesson.Jump&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};
   }
   else if(!battle.TutorialWaiting&&(battle.CurrentLesson==Story1WreckBattle.Lesson.Move||battle.CurrentLesson==Story1WreckBattle.Lesson.Swim))keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
   float t=(float)Get("redesignTime");
   bool climb=battle.RedesignStage==Story1WreckBattle.OpeningStage.Pursuit&&t>=8.5f;
   bool draw=battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t<=3.25f;
   if(climb)sawClimb=true;capture=climb||draw;
   if(draw&&t>=1.7f&&t<=2.92f)
   {
    skin.BakeMesh(baked,true);var points=baked.vertices.Select(skin.transform.TransformPoint).ToArray();if(tree==null)tree=Activator.CreateInstance(treeType,new object[]{points,triangles});else treeType.GetMethod("Refit").Invoke(tree,new object[]{points});
    float Gap(){Story1CapsuleContact.Segment((CapsuleCollider)attack.stickHitbox,out var a,out var b,out float radius);return (float)treeType.GetMethod("CapsuleSurfaceGapUpTo").Invoke(tree,new object[]{a,b,radius,float.PositiveInfinity});}
    Vector3 at=stow.stick.position;Quaternion rotation=stow.stick.rotation;Legacy();float before=Gap();stow.stick.SetPositionAndRotation(at,rotation);float after=Gap();oldWorst=Mathf.Min(oldWorst,before);worst=Mathf.Min(worst,after);if(before<-.005f)oldBad++;if(after<-.005f)bad++;
    float gripError=Vector3.Distance(stow.stick.localPosition,(Vector3)typeof(SahurSwimmingWeapon).GetField("gripPosition",F).GetValue(stow));
    float rotationError=Quaternion.Angle(stow.stick.localRotation,(Quaternion)typeof(SahurSwimmingWeapon).GetField("gripRotation",F).GetValue(stow));
    bool originalGrip=!stow.IsStowed&&gripError<.00001f&&rotationError<.01f;
    if(t>=2.8f&&originalGrip)returned=true;
    rows.Add(new{frame,t,weight=stow.CinematicStowWeight,before,after,stow.IsStowed,gripError,rotationError,originalGrip});
   }
   if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>=6.5f)Finish(null);
   if(frame%60==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,stage=battle.RedesignStage.ToString(),lesson=battle.CurrentLesson.ToString(),t,images=image}));
   if(frame>3900)throw new Exception("Native replay timed out at "+battle.RedesignStage+" "+battle.CurrentLesson);
  }catch(Exception e){Finish(e);}
 }
 void Legacy()
 {
  float blend=stow.CinematicStowWeight;if(blend<=0)return;var stick=stow.stick;var mesh=stick.GetComponent<MeshFilter>().sharedMesh;
  var chest=animator.GetBoneTransform(HumanBodyBones.Chest)??animator.GetBoneTransform(HumanBodyBones.Spine);var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
  Vector3 up=(chest.position-hips.position).normalized,across=Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,up).normalized,front=Vector3.Cross(across,up).normalized;
  float angle=stow.diagonalAngle*Mathf.Deg2Rad;Vector3 shaft=up*Mathf.Cos(angle)+across*Mathf.Sin(angle);var axis=(Vector3)typeof(SahurSwimmingWeapon).GetField("shaftAxis",F).GetValue(stow);var facing=(Vector3)typeof(SahurSwimmingWeapon).GetField("shaftFront",F).GetValue(stow);
  stick.rotation=Quaternion.LookRotation(front,shaft)*Quaternion.Inverse(Quaternion.LookRotation(facing,axis));stick.position=chest.position-front*stow.backOffset+up*stow.heightOffset-stick.TransformVector(mesh.bounds.center);
  if(blend<1){var grip=(Vector3)typeof(SahurSwimmingWeapon).GetField("gripPosition",F).GetValue(stow);var rotation=(Quaternion)typeof(SahurSwimmingWeapon).GetField("gripRotation",F).GetValue(stow);stick.SetPositionAndRotation(Vector3.Lerp(stick.parent.TransformPoint(grip),stick.position,blend),Quaternion.Slerp(stick.parent.rotation*rotation,stick.rotation,blend));}
 }
 void Rendered(ScriptableRenderContext context,Camera camera)
 {
  if(done||!capture||camera!=seq.storyCamera)return;var previous=RenderTexture.active;
  try{RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/frame-"+(image++).ToString("D4")+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
 }
 void Finish(Exception error)
 {
  if(done)return;done=true;
  if(error!=null)File.WriteAllText(folder+"/error.txt",error.ToString());
  float finalGripError=Vector3.Distance(stow.stick.localPosition,(Vector3)typeof(SahurSwimmingWeapon).GetField("gripPosition",F).GetValue(stow));
  var result=new{passed=error==null&&sawClimb&&rows.Count>=30&&bad==0&&returned&&battle.RedesignTutorialConfirmedHits==2&&(bool)Get("redOverheadHit"),frame,fps=30,images=image,sawClimb,samples=rows.Count,worst,oldWorst,bad,oldBad,returned,finalGripError,tutorialHits=battle.RedesignTutorialConfirmedHits,counterHit=(bool)Get("redOverheadHit"),health=seq.sahur.GetComponent<PlayerHealth>().currentHealth,rows};File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  if(seq&&seq.storyCamera)seq.storyCamera.targetTexture=oldTarget;EditorApplication.isPlaying=false;
 }
 void OnDestroy()
 {
  InputSystem.onBeforeUpdate-=Inject;RenderPipelineManager.endCameraRendering-=Rendered;if(seq&&seq.storyCamera)seq.storyCamera.targetTexture=oldTarget;
  if(rt){rt.Release();DestroyImmediate(rt);}if(pixels)DestroyImmediate(pixels);if(baked)DestroyImmediate(baked);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();Time.captureDeltaTime=oldDelta;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;
 }
}
