using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class VerifyTutorialContactsV242
{
 public static object Start(string label)
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required.");
  var go=new GameObject("Temporary independent rendered contact audit");go.AddComponent<TutorialNativeAuditV242>().folder=".codex/opening-charge-20261023/"+label;
  return new{started=true};
 }
}
[DefaultExecutionOrder(31510)]public sealed class TutorialNativeAuditV242:MonoBehaviour
{
 public string folder;int count;Story1WreckBattle battle;readonly List<object> rows=new List<object>();
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Story1WreckBattle).GetField(n,Flags).GetValue(battle);
 object Call(string n,params object[] args)=>typeof(Story1WreckBattle).GetMethod(n,Flags).Invoke(battle,args);
 void LateUpdate()
 {
  if(!battle)battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
  if(!battle||battle.RedesignTutorialConfirmedHits<=count)return;count=battle.RedesignTutorialConfirmedHits;
  if(!(bool)Get("lessonImpact") || (float)Get("lessonTime")-(float)Get("redTutorialHitTime")>.001f)
  {rows.Add(new{frame=Time.frameCount,count,skipped="Audit attached after this contact; not a contact measurement."});File.WriteAllText(folder+"/independent-native-contacts.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return;}
  Mesh mesh=null;
  try{
   var skin=(SkinnedMeshRenderer)Call("RedContactSkin");mesh=new Mesh();skin.BakeMesh(mesh,true);
   var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=skin.transform.TransformPoint(vertices[i]);
   var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
   var tree=Activator.CreateInstance(type,new object[]{vertices,mesh.triangles});
   Story1CapsuleContact.Segment((CapsuleCollider)Get("redWeapon"),out var a,out var b,out float radius);
   float weaponGap=(float)type.GetMethod("CapsuleGap").Invoke(tree,new object[]{a,b,radius});
   Call("RedTutorialBody");float bodyGap=float.PositiveInfinity;
   foreach(var capsule in (IList)Get("redWaterCapsules"))
   {
    var c=capsule.GetType();float gap=(float)type.GetMethod("CapsuleGapUpTo").Invoke(tree,new object[]{c.GetField("a").GetValue(capsule),c.GetField("b").GetValue(capsule),c.GetField("radius").GetValue(capsule),(object).5f});bodyGap=Mathf.Min(bodyGap,gap);
   }
   var wood=new List<string>();Bounds bounds=skin.bounds;
   foreach(var shape in UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None))
   {
    if(!shape.Active||!shape.WorldBounds.Intersects(bounds))continue;
    var filter=shape.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
    var points=filter.sharedMesh.vertices;for(int i=0;i<points.Length;i++)points[i]=shape.transform.TransformPoint(points[i]);
    object[] query={points,filter.sharedMesh.triangles,Vector3.zero};
    if((bool)type.GetMethod("Touch",new[]{typeof(Vector3[]),typeof(int[]),typeof(Vector3).MakeByRefType()}).Invoke(tree,query))wood.Add(shape.name);
   }
   var actor=((ThirdPersonPlayerController)Get("player")).CharacterAnimator;
   var board=(Story1WreckPlank)Get("redCombatBoard");Vector3 ls=default,rs=default;SahurSupportPose.MeasureSoles(actor,ref ls,ref rs);
   var soleGaps=new List<float>();bool feetSupported=true;
   foreach(var foot in new[]{(HumanBodyBones.LeftFoot,ls),(HumanBodyBones.RightFoot,rs)})
   {
    Vector3 point=actor.GetBoneTransform(foot.Item1).TransformPoint(foot.Item2);
    bool supported=board.Collision.Raycast(new Ray(point+Vector3.up,-Vector3.up),out var hit,2);
    float gap=supported?point.y-hit.point.y:-99;soleGaps.Add(gap);feetSupported&=supported&&gap>=-.02f&&gap<.2f;
   }
   bool passed=weaponGap<=.055f&&weaponGap>=-.15f&&bodyGap>=.17f&&wood.Count==0&&feetSupported;
   rows.Add(new{frame=Time.frameCount,lesson=battle.CurrentLesson.ToString(),weaponGap,bodyGap,wood,soleGaps,feetSupported,battle.RedesignTutorialRenderedHitGap,scale=new[]{((Transform)Get("shark")).localScale.x,((Transform)Get("shark")).localScale.y,((Transform)Get("shark")).localScale.z},passed});
   File.WriteAllText(folder+"/independent-native-contacts.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
   if(!passed)Debug.LogError("Tutorial independent native contact failed; see evidence.");
  }catch(Exception e){File.WriteAllText(folder+"/native-audit-error.txt",e.ToString());}
  finally{if(mesh)Destroy(mesh);}
 }
}
