using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
public static class ReviewDeckClearanceV249
{
 public static object Start(string label)
 {
  if(!Application.isPlaying)throw new Exception("Existing Play required");
  var folder=".codex/opening-charge-20261023/"+label;
  var go=new GameObject("Temporary independent deck clearance audit");go.AddComponent<DeckClearanceReviewV249>().folder=folder;
  return new{started=true};
 }
}
[DefaultExecutionOrder(31900)] public sealed class DeckClearanceReviewV249:MonoBehaviour
{
 public string folder;
 Story1WreckBattle battle;Story1SharkCollisionSequence seq;Story1WreckDeckClearance system;
 readonly List<object> rows=new List<object>();int frame,captured;bool done;
 RenderTexture rt;Texture2D pixels;Camera layoutCamera;ProfilerRecorder cost;
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 Bounds BoundsOf(Story1WreckMeshCollision c)
 {
  var mesh=c.GetComponent<MeshFilter>().sharedMesh;var b=mesh.bounds;var r=c.GetComponent<Rigidbody>();
  var m=r!=null?Matrix4x4.TRS(r.position,r.rotation.normalized,c.transform.lossyScale):c.transform.localToWorldMatrix;
  var x=m.MultiplyVector(Vector3.right*b.extents.x);var y=m.MultiplyVector(Vector3.up*b.extents.y);var z=m.MultiplyVector(Vector3.forward*b.extents.z);
  return new Bounds(m.MultiplyPoint3x4(b.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
 }
 void Start()
 {
  seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=seq.GetComponent<Story1WreckBattle>();
  cost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.DeckClearance",1);
  layoutCamera=new GameObject("Temporary overhead layout review camera").AddComponent<Camera>();layoutCamera.CopyFrom(seq.storyCamera);layoutCamera.enabled=false;
  rt=new RenderTexture(768,432,24);rt.Create();layoutCamera.targetTexture=rt;pixels=new Texture2D(768,432,TextureFormat.RGB24,false);
 }
 void LateUpdate()
 {
  if(done||!battle)return;
  try
  {
   if(!system)system=UnityEngine.Object.FindFirstObjectByType<Story1WreckDeckClearance>();
   var deck=typeof(Story1WreckBattle).GetField("redCombatBoard",F).GetValue(battle) as Story1WreckPlank;
   if(system&&deck&&!deck.IsBroken&&deck.Collision.Active)
   {
    var d=BoundsOf(deck.Collision);float gap=float.PositiveInfinity;int overlaps=0;string nearest=null;
    var registry=typeof(Story1WreckDeckClearance).GetField("fragments",F).GetValue(system) as System.Collections.IEnumerable;
    foreach(var entry in registry)
    {
     var c=entry.GetType().GetField("collision").GetValue(entry) as Story1WreckMeshCollision;
     if(!c||c==deck.Collision||!c.Active)continue;
     var b=BoundsOf(c);float separation=Mathf.Max(d.min.x-b.max.x,b.min.x-d.max.x,d.min.z-b.max.z,b.min.z-d.max.z);
     if(separation<gap){gap=separation;nearest=c.name;}if(separation<system.clearance-.01f)overlaps++;
    }
    var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var at=deck.BoardingPoint(d.center);float height=at.y-water.SampleSurfaceHeight(at,Time.time);
    rows.Add(new{frame,rate=Time.timeScale,stage=battle.RedesignStage.ToString(),clock=battle.RedesignClock,deck=deck.name,gap,overlaps,nearest,deckHeight=height,kinematic=deck.GetComponent<Rigidbody>().isKinematic,system.SeparationCount,costNs=cost.Valid?cost.LastValue:-1,fractures=battle.RedesignSolidFractures});
    if(frame%8==0&&captured<240&&battle.RedesignStage!=Story1WreckBattle.OpeningStage.Drift)
    {
     layoutCamera.fieldOfView=52;Vector3 look=d.center;Vector3 offset=new Vector3(0,Mathf.Max(28,d.size.magnitude*1.1f),Mathf.Max(15,d.size.magnitude*.55f));
     layoutCamera.transform.SetPositionAndRotation(look+offset,Quaternion.LookRotation(-offset));
     var active=RenderTexture.active;try{layoutCamera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,768,432),0,0,false);File.WriteAllBytes(folder+"/deck-"+captured.ToString("D4")+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;}
     captured++;
    }
   }
   frame++;
   if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Finished)Finish();
  }
  catch(Exception e){File.WriteAllText(folder+"/deck-error.txt",e.ToString());done=true;}
 }
 void Finish(){if(done)return;done=true;File.WriteAllText(folder+"/deck-clearance.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rows,captured,scope="Independent actual native physics-pose XZ envelopes after the late contact pass; no broad claim of all layouts or FPS."},Newtonsoft.Json.Formatting.Indented));}
 void OnDestroy(){if(!done&&folder!=null)Finish();cost.Dispose();if(layoutCamera)Destroy(layoutCamera.gameObject);if(rt){rt.Release();Destroy(rt);}if(pixels)Destroy(pixels);}
}
