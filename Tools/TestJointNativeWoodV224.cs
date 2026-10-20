using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
public static class TestJointNativeWoodV224
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();if(b==null||!b.RedesignActive)throw new Exception("Use current opening");
  var t=typeof(Story1WreckBattle);var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  object Call(string name,params object[] args)=>t.GetMethod(name,flags).Invoke(b,args);
  var skin=(SkinnedMeshRenderer)Call("RedContactSkin");Call("RedTutorialBody");
  var source=b.Boards.Where(p=>p!=null&&!p.IsBroken&&p.Collision.Active&&p.Dimensions.y<1.2f&&Mathf.Min(p.Dimensions.x,p.Dimensions.z)>7).OrderByDescending(p=>Mathf.Min(p.Dimensions.x,p.Dimensions.z)).First();
  var go=new GameObject("Joint native timber fixture v224",typeof(MeshFilter),typeof(Rigidbody),typeof(Story1WreckMeshCollision));
  go.GetComponent<Rigidbody>().isKinematic=true;go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
  go.transform.localScale=source.transform.lossyScale;var shape=go.GetComponent<Story1WreckMeshCollision>();shape.Initialize(go.GetComponent<MeshFilter>().sharedMesh,source.FractureSource?.splinters);
  foreach(var c in shape.Parts)c.isTrigger=true;
  var hero=(ThirdPersonPlayerController)t.GetField("player",flags).GetValue(b);var chest=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Chest).position;
  var root=(Transform)t.GetField("shark",flags).GetValue(b);Vector3 originalRoot=root.position;
  var rows=new List<object>();int overlaps=0,joint=0;float maximum=0;
  try
  {
   // A reversible fixture brings the actual posed head beside the hero so
   // the test exercises simultaneous constraints, even at a distant gate.
   var tree=t.GetField("redSolidSharkTree",flags).GetValue(b);if(tree==null)throw new Exception("Wait for a solved opening frame");
   object[] closest={skin.transform.InverseTransformPoint(chest),0,Vector3.zero,Vector3.zero};tree.GetType().GetMethod("Closest").Invoke(tree,closest);
   Vector3 surface=skin.transform.TransformPoint((Vector3)closest[3]);root.position+=chest+Vector3.right*.35f-surface;
   for(int i=0;i<20;i++)
   {
    var centre=chest+Vector3.right*(i%5)*.1f;
    go.transform.SetPositionAndRotation(centre+new Vector3(i%2*.4f,0,(i%3-1)*.3f),Quaternion.Euler(i*17,i*47,i*13));Physics.SyncTransforms();
    bool shark=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});
    bool body=(bool)Call("RedSolidWoodHeroOverlap",new object[]{shape,Vector3.zero,Vector3.zero});Vector3 offset=Vector3.zero;
    if(shark&&body)joint++;
    if(shark)offset=(Vector3)Call("RedSolidSeparation",shape,skin,(shape.WorldBounds.center-skin.bounds.center).normalized);
    else if(body)offset=(Vector3)t.GetMethod("RedSolidBoundsSeparation",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{shape.WorldBounds,Call("RedSolidBodyBounds")});
    if(shark||body)
    {
     overlaps++;offset=(Vector3)Call("RedSolidKeepWoodOffHero",shape,skin,offset);go.transform.position+=offset;Physics.SyncTransforms();
     bool sharkAfter=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});
     bool heroAfter=(bool)Call("RedSolidWoodHeroOverlap",new object[]{shape,Vector3.zero,Vector3.zero});
     if(sharkAfter||heroAfter)throw new Exception("Residual joint overlap "+i+": shark="+sharkAfter+" hero="+heroAfter);
     maximum=Mathf.Max(maximum,offset.magnitude);if(maximum>150)throw new Exception("World-origin dependent displacement "+maximum);
    }
    rows.Add(new{i,shark,body,travel=offset.magnitude});
   }
   if(overlaps<12||joint<4)throw new Exception("Insufficient native joint overlap cases: "+overlaps+" simultaneous="+joint);
   var result=new{passed=true,cases=rows.Count,overlaps,joint,maximum,rows};File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/encounter-redesign-20261017/contact-v224/joint-native-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{root.position=originalRoot;UnityEngine.Object.DestroyImmediate(go);Physics.SyncTransforms();}
 }
}
