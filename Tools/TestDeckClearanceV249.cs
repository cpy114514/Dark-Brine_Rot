using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class TestDeckClearanceV249
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Isolated Edit Mode fixture required");
  var scene=EditorSceneManager.NewPreviewScene();var checks=new Dictionary<string,bool>();
  void Check(string n,bool v){checks.Add(n,v);if(!v)throw new Exception(n);}
  GameObject root=null;
  try
  {
   root=new GameObject("Temporary deck separation contracts");SceneManager.MoveGameObjectToScene(root,scene);
   var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cube);
   Story1WreckPlank Board(string name,Vector3 at,Vector3 size)
   {
    var go=new GameObject(name);go.transform.SetParent(root.transform);go.AddComponent<MeshFilter>().sharedMesh=mesh;
    var p=go.AddComponent<Story1WreckPlank>();p.Initialize(at,Quaternion.identity,size,null,Vector3.zero,Vector3.zero);return p;
   }
   var deck=Board("Landing deck",new Vector3(10000,10,10000),new Vector3(12,.6f,12));
   var other=Board("Heavy overlapping beam",deck.transform.position+Vector3.up*.5f,new Vector3(2,2,16));
   var far=Board("Unrelated remote timber",deck.transform.position+Vector3.right*60,new Vector3(3,1,3));
   var rb=deck.GetComponent<Rigidbody>();var load=other.GetComponent<Rigidbody>();load.mass=50000;
   load.linearVelocity=new Vector3(-4,-3,2);load.angularVelocity=new Vector3(.2f,.4f,.1f);
   var original=rb.position;var originalMass=rb.mass;var spin=load.angularVelocity;var distant=far.transform.position;
   var boards=new List<Story1WreckPlank>{deck,other,far};
   var system=root.AddComponent<Story1WreckDeckClearance>();system.Track(boards,new List<Story1WreckDebris>());
   system.Reserve(deck);Physics.SyncTransforms();
   Check("Occupied deck stays at original physical position",Vector3.Distance(rb.position,original)<.0001f);
   Check("Deck remains dynamic with original mass",!rb.isKinematic&&rb.useGravity&&rb.mass==originalMass);
   bool Clear(Story1WreckPlank a,Story1WreckPlank b)
   {
    var x=a.Collision.WorldBounds;var y=b.Collision.WorldBounds;
    return x.min.x-y.max.x>=1.24f||y.min.x-x.max.x>=1.24f||x.min.z-y.max.z>=1.24f||y.min.z-x.max.z>=1.24f;
   }
   Check("Overlapping heavy timber lies outside deck margin",Clear(deck,other));
   Check("Falling speed and angular momentum retained",Mathf.Abs(load.linearVelocity.y+3)<.0001f&&Vector3.Distance(load.angularVelocity,spin)<.0001f);
   Check("Remote timber unchanged",Vector3.Distance(far.transform.position,distant)<.0001f);
   var settled=load.position;system.Enforce();Check("Repeated clear pass is idempotent",Vector3.Distance(load.position,settled)<.0001f);
   load.position=rb.position;other.transform.position=load.position;system.Enforce();Check("Returning overlap is cleared again",Clear(deck,other));
   rb.rotation=Quaternion.Euler(8,35,6);deck.transform.rotation=rb.rotation;
   load.rotation=Quaternion.Euler(10,48,12);other.transform.rotation=load.rotation;load.position=rb.position+Vector3.up*3;other.transform.position=load.position;
   system.Enforce();Check("Tilted rotating timber also clears the actual footprint",Clear(deck,other));
   var shard=Board("Later fractured timber",rb.position+Vector3.up*3,new Vector3(3,.4f,3));boards.Add(shard);system.Enforce();Check("Later shards automatically join live roster",Clear(deck,shard));
   int count=system.SeparationCount;deck.MarkBroken();system.Enforce();Check("Destroyed board releases its reservation",system.ReservedDecks==0&&system.SeparationCount==count);
   var next=Board("Next climb deck",new Vector3(10100,10,10000),new Vector3(12,.6f,12));system.Register(next.Collision);system.Reserve(next);Check("Next real platform can take ownership",system.ReservedDecks==1);
   var watch=System.Diagnostics.Stopwatch.StartNew();long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<200;i++)system.Enforce();long bytes=GC.GetAllocatedBytesForCurrentThread()-before;watch.Stop();
   Check("Warm separation loop allocates no managed objects",bytes==0);
   var result=new{passed=true,checks,iterations=200,ms=watch.Elapsed.TotalMilliseconds,managedBytes=bytes,note="Isolated retained-mesh dynamic-body contracts; native sea and full opening validated separately."};
   string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(".codex/deck-clearance-20261024/contracts.json",json);return json;
  }
  finally{if(root)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
