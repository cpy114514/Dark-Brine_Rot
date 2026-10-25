using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
public static class ReviewBoatFidgetV246
{
 public static object Start(string take)
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
  string folder=".codex/boat-fidget-20261024/"+take;
  if(Directory.Exists(folder))throw new Exception("Preserve take");
  Directory.CreateDirectory(folder);
  var go=new GameObject("Temporary boat fidget review v246");var r=go.AddComponent<BoatFidgetReviewV246>();r.folder=folder;
  Time.timeScale=1;EditorApplication.isPaused=false;return new{folder};
 }
}
[DefaultExecutionOrder(31900)] public sealed class BoatFidgetReviewV246:MonoBehaviour
{
 public string folder;
 Story1SharkCollisionSequence seq;Story1WreckBattle battle;Story1SahurPassenger passenger;Animator a;
 Camera close;RenderTexture rt;Texture2D tex;float oldDelta;int frame;readonly List<object> rows=new List<object>();
 Vector3 leftSole,rightSole;
 float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 float[] Q(Quaternion q)=>new[]{q.x,q.y,q.z,q.w};
 void Start()
 {
  seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=seq.GetComponent<Story1WreckBattle>();
  passenger=seq.sahur.GetComponent<Story1SahurPassenger>();a=seq.sahur.CharacterAnimator;
  SahurSupportPose.MeasureSoles(a,ref leftSole,ref rightSole);
  oldDelta=Time.captureDeltaTime;Time.captureDeltaTime=1f/30;
  close=new GameObject("Temporary boat pose review camera").AddComponent<Camera>();close.CopyFrom(seq.storyCamera);close.enabled=false;close.gameObject.AddComponent<UniversalAdditionalCameraData>();close.fieldOfView=38;
  rt=new RenderTexture(768,432,24);rt.Create();tex=new Texture2D(768,432,TextureFormat.RGB24,false);
  File.WriteAllText(folder+"/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{components=seq.sahur.GetComponents<MonoBehaviour>().Select(c=>new{name=c.GetType().Name,c.enabled}),animators=seq.sahur.GetComponentsInChildren<Animator>(true).Select(x=>new{x.name,x.enabled,controller=x.runtimeAnimatorController?.name,x.speed,x.applyRootMotion}),clip=Story1EncounterClipCache.Find("CH1_SahurStickIdle_v247")?.name,shipScale=V(seq.ship.transform.lossyScale)},Newtonsoft.Json.Formatting.Indented));
 }
 void Save(Camera c,string prefix)
 {
  var prev=c.targetTexture;var active=RenderTexture.active;
  try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,432),0,0,false);File.WriteAllBytes(folder+"/"+prefix+"-"+(frame/2).ToString("D4")+".png",tex.EncodeToPNG());}
  finally{c.targetTexture=prev;RenderTexture.active=active;}
 }
 void LateUpdate()
 {
  if(seq==null)return;
  var ship=seq.ship.transform;var actor=seq.sahur.transform;
  var footL=a.GetBoneTransform(HumanBodyBones.LeftFoot);var footR=a.GetBoneTransform(HumanBodyBones.RightFoot);
  var hand=a.GetBoneTransform(HumanBodyBones.RightHand);var head=a.GetBoneTransform(HumanBodyBones.Head);
  var chest=a.GetBoneTransform(HumanBodyBones.Chest);
  var weapon=seq.sahur.GetComponent<Mavis.SahurAttack>().stickHitbox as CapsuleCollider;
  var axis=weapon.direction==0?Vector3.right:weapon.direction==1?Vector3.up:Vector3.forward;
  var endA=weapon.transform.TransformPoint(weapon.center+axis*(weapon.height*.5f-weapon.radius));
  var endB=weapon.transform.TransformPoint(weapon.center-axis*(weapon.height*.5f-weapon.radius));
  var tip=(endA-hand.position).sqrMagnitude>(endB-hand.position).sqrMagnitude?endA:endB;
  rows.Add(new{frame,voyage=seq.VoyageTime,pose=(typeof(Story1WreckBattle).GetField("redClip",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(battle) as AnimationClip)?.name,active=battle.RedesignActive,clock=battle.RedesignClock,shot=battle.RedesignShot,idle=passenger.StickIdleTime,root=V(ship.InverseTransformPoint(actor.position)),worldRoot=V(actor.position),rotation=Q(Quaternion.Inverse(ship.rotation)*actor.rotation),left=V(ship.InverseTransformPoint(footL.TransformPoint(leftSole))),right=V(ship.InverseTransformPoint(footR.TransformPoint(rightSole))),hand=V(actor.InverseTransformPoint(hand.position)),tip=V(actor.InverseTransformPoint(tip)),wrist=Q(hand.localRotation),chest=Q(chest.localRotation),head=Q(head.localRotation),a.applyRootMotion,a.speed});
  // Keep a clear diagnostic three-quarter view relative to the character.
  if(frame%2==0)
  {
   Vector3 feet=(footL.position+footR.position)*.5f;
   float height=Mathf.Max(.5f,Vector3.Distance(head.position,feet));
   Vector3 look=Vector3.Lerp(feet,head.position,.50f)+actor.right*height*.2f;
   Vector3 offset=(actor.forward*2.5f+actor.right*1.6f+actor.up*.25f)*height;
   close.transform.SetPositionAndRotation(look+offset,Quaternion.LookRotation(-offset,actor.up));
   Save(close,"close");Save(seq.storyCamera,"game");
   if(frame%10==0)
   {
    offset=(actor.right*2.9f+actor.up*.25f)*height;
    close.transform.SetPositionAndRotation(look+offset,Quaternion.LookRotation(-offset,actor.up));
    Save(close,"side");
   }
  }
  frame++;
  if((battle.RedesignActive&&battle.RedesignClock>=3.5f)||frame>=2100)
  {
   File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,complete=battle.RedesignActive&&battle.RedesignClock>=3.5f,rows},Newtonsoft.Json.Formatting.Indented));
   Time.captureDeltaTime=oldDelta;enabled=false;EditorApplication.isPaused=true;
  }
 }
 void OnDestroy(){Time.captureDeltaTime=oldDelta;if(close!=null)Destroy(close.gameObject);if(rt!=null){rt.Release();Destroy(rt);}if(tex!=null)Destroy(tex);}
}
