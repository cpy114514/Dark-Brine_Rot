using System;using System.IO;using System.Linq;using UnityEngine;using Mavis;
public static class VerifyMechanical070
{
 public static object Run()
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  var boss=UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>();if(!boss)throw new Exception("Need interrupted actual actor");
  var visual=boss.visual;var rest=visual.localRotation;bool finite=true,inside=true,attached=true;int samples=0,vertices=0;
  using(var rig=new BombardinoCaptureClaws(visual))
  {
   var source=visual.GetComponent<MeshFilter>().sharedMesh;
   // The underside mount point is intentionally buried 7cm in the hull.
   var v=source.vertices;var t=source.triangles;
   for(int i=0;i<2;i++)
   {
    var p=rig.MountLocal(i);float nearest=float.MaxValue;
    for(int j=0;j<t.Length;j+=3)
    {
     var a=v[t[j]];var b=v[t[j+1]];var c=v[t[j+2]];
     var n=Vector3.Cross(b-a,c-a).normalized;if(n.sqrMagnitude<.5f)continue;
     var q=p-n*Vector3.Dot(p-a,n);
     var c0=Vector3.Dot(Vector3.Cross(b-a,q-a),n);var c1=Vector3.Dot(Vector3.Cross(c-b,q-b),n);var c2=Vector3.Dot(Vector3.Cross(a-c,q-c),n);
     if(c0>=-.00001f&&c1>=-.00001f&&c2>=-.00001f)nearest=Mathf.Min(nearest,Vector3.Distance(q,p));
    }
    inside&=nearest<=.075f;
   }
   for(int sign=-1;sign<=1;sign+=2)foreach(float bank in new[]{-35f,0,35f})foreach(float closed in new[]{0f,.5f,1f})
   {
    visual.localRotation=rest*Quaternion.Euler(bank*.4f,0,bank);
    rig.Hold(visual.TransformPoint(rig.GripLocal),visual.right*sign,.637f,closed);
    var child=visual.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh&&f.sharedMesh.name=="Aircraft-mounted hydraulic capture rig");
    var points=child.sharedMesh.vertices;vertices=points.Length;
    finite&=points.All(p=>!float.IsNaN(p.x)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z));
    attached&=child.transform.parent==visual&&child.transform.localPosition.sqrMagnitude<1e-8f&&Quaternion.Angle(child.transform.localRotation,Quaternion.identity)<.001f;
    samples++;
   }
  }
  visual.localRotation=rest;
  var result=new{passed=finite&&inside&&attached,samples,vertices,finite,mountsWithin7point5cmOfHull=inside,attached};
  File.WriteAllText(".codex/mechanical-claws070/geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
