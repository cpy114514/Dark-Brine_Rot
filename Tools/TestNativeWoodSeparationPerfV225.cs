using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
public static class TestNativeWoodSeparationPerfV225
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();if(b==null||!b.RedesignActive)throw new Exception("Run during the existing opening");
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var t=typeof(Story1WreckBattle);
  object Call(string name,params object[] args)=>t.GetMethod(name,flags).Invoke(b,args);
  var skin=(SkinnedMeshRenderer)Call("RedContactSkin");
  if(t.GetField("redSolidSharkTree",flags).GetValue(b)==null)throw new Exception("Wait until the tutorial begins");
  var source=b.Boards.First(p=>p!=null&&!p.IsBroken&&p.Collision.Active);
  var go=new GameObject("Native timber contact fixture v224",typeof(MeshFilter),typeof(Rigidbody),typeof(Story1WreckMeshCollision));
  var body=go.GetComponent<Rigidbody>();body.isKinematic=true;
  go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
  go.transform.localScale=source.transform.lossyScale;
  var shape=go.GetComponent<Story1WreckMeshCollision>();shape.Initialize(go.GetComponent<MeshFilter>().sharedMesh,source.FractureSource?.splinters);
  foreach(var c in shape.Parts)c.isTrigger=true;
  var rows=new List<object>();int intersected=0;float biggest=0;var native=new Mesh();
  try
  {
   for(int i=0;i<24;i++)
   {
    go.transform.SetPositionAndRotation(skin.bounds.center+Quaternion.AngleAxis(i*60,Vector3.up)*Vector3.forward*(i%4)*skin.bounds.extents.x*.3f,Quaternion.Euler(i*13,i*47,i*7));
    Physics.SyncTransforms();object[] query={shape,skin,Vector3.zero,Vector3.zero};
    bool touch=(bool)Call("RedSolidTouch",query);float travel=0;bool remaining=false;
    // Independent full native triangle query, without the production broad
    // phase: contained timber must never be rejected as a distant shape.
    var matrix=skin.transform.worldToLocalMatrix*go.transform.localToWorldMatrix;
    var points=go.GetComponent<MeshFilter>().sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
    var tree=t.GetField("redSolidSharkTree",flags).GetValue(b);var treeType=tree.GetType();
    var woodTree=Activator.CreateInstance(treeType,new object[]{points,go.GetComponent<MeshFilter>().sharedMesh.triangles});
    var exact=treeType.GetMethod("Touch",new[]{treeType,typeof(Vector3).MakeByRefType()});
    bool reference=(bool)exact.Invoke(tree,new[]{woodTree,(object)Vector3.zero});
    if(touch!=reference)throw new Exception("Broad phase changed actual native contact for fixture "+i+": production="+touch+" reference="+reference);
    if(touch)
    {
     intersected++;var offset=(Vector3)Call("RedSolidSeparation",shape,skin,(shape.WorldBounds.center-skin.bounds.center).normalized);
     travel=offset.magnitude;biggest=Mathf.Max(biggest,travel);go.transform.position+=offset;Physics.SyncTransforms();
     remaining=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});
     if(remaining)throw new Exception("Native timber remains in posed skin for fixture "+i);
    }
    rows.Add(new{i,touch,travel,remaining});
   }
   if(intersected<8)throw new Exception("Too few real native intersection cases: "+intersected);
   var result=new{passed=true,cases=rows.Count,intersected,biggest,rows};
   File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-performance-20261019/native-separation-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
   return result;
  }
  finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(native);}
 }
}
