using System;
using System.Linq;
using UnityEngine;
public static class AuditBoatGripV246
{
 public static object Curves()
 {
  var clips=new[]{"CH1_SahurStickIdle_v246","CH1_SahurGuard_v050"};
  var rows=clips.Select(name=>new{clip=name,curves=UnityEditor.AnimationUtility.GetCurveBindings(Story1EncounterClipCache.Find(name)).Where(b=>b.propertyName.Contains("Right")&&(b.propertyName.Contains("Hand")||b.propertyName.Contains("Arm"))).Select(b=>{var c=UnityEditor.AnimationUtility.GetEditorCurve(Story1EncounterClipCache.Find(name),b);return new{b.propertyName,min=c.keys.Min(k=>k.value),max=c.keys.Max(k=>k.value),start=c.Evaluate(0),before=c.Evaluate(2.18f),after=c.Evaluate(2.24f),keys=c.keys.Select(k=>new{k.time,k.value})};})}).ToArray();
  var json=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(".codex/boat-fidget-20261024/muscle-audit.json",json);return "Saved muscle audit";
 }
 public static object Run()
 {
  var seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
  var actor=seq.sahur;var a=actor.CharacterAnimator;var hand=a.GetBoneTransform(HumanBodyBones.RightHand);
  var c=(CapsuleCollider)actor.GetComponent<Mavis.SahurAttack>().stickHitbox;
  var axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;
  var p=c.transform.TransformPoint(c.center-axis*(c.height*.5f-c.radius));var q=c.transform.TransformPoint(c.center+axis*(c.height*.5f-c.radius));
  float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
  var result=new{nearHand=V(hand.InverseTransformPoint(p)),farHand=V(hand.InverseTransformPoint(q)),handScale=V(hand.lossyScale),stick=c.transform.name,parent=c.transform.parent.name,stickLocalPosition=V(c.transform.localPosition),stickLocalEuler=V(c.transform.localEulerAngles),c.radius,c.height,center=V(c.center),handName=hand.name,head=V(a.GetBoneTransform(HumanBodyBones.Head).position),actor=V(actor.transform.position),gap=Story1WreckBattle.MissingRedesignClips()};
  string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(".codex/boat-fidget-20261024/grip-audit.json",json);return json;
 }
}
