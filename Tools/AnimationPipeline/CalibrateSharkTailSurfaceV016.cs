using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class CalibrateSharkTailSurfaceV016
{
 public static object Calibrate()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
  var actor=UnityEngine.Object.Instantiate(set.rigPrefab);
  try
  {
   var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
   var marker=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");
   animator.Rebind();animator.Update(0);
   if(!TralaleroSwimAnimator.FindVisibleTailContact(skin,marker,out var bone,out var local))throw new Exception("No caudal surface point");
   animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="CH1_SharkTail_v006").SampleAnimation(animator.gameObject,.65f);
   set.tailStrikeContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
   animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="CH1_SharkShipSmash_v006").SampleAnimation(animator.gameObject,.65f);
   set.shipSmashContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
   EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
   return new{exactVisibleCaudalVertex=true,originalRigUnmodified=true,contactBone=bone.name,tailStrike=set.tailStrikeContact.ToString(),shipSmash=set.shipSmashContact.ToString()};
  }
  finally{UnityEngine.Object.DestroyImmediate(actor);}
 }
}
