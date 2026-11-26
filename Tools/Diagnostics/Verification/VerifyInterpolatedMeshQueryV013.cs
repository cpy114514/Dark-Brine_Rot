using System;
using System.Threading.Tasks;
using UnityEngine;
public static class VerifyInterpolatedMeshQueryV013
{
 public static async Task<object> Verify()
 {
  if(!Application.isPlaying)throw new Exception("Play mode required");
  var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);
  try
  {
   fixture.name="Isolated interpolated mesh query fixture";fixture.transform.position=new Vector3(3000,50,3000);fixture.transform.localScale=Vector3.one*2;fixture.GetComponent<Renderer>().enabled=false;
   UnityEngine.Object.Destroy(fixture.GetComponent<BoxCollider>());
   var body=fixture.AddComponent<Rigidbody>();body.useGravity=false;body.linearDamping=0;body.interpolation=RigidbodyInterpolation.Interpolate;body.linearVelocity=new Vector3(3,2,0);
   var shape=fixture.AddComponent<Story1WreckMeshCollision>();shape.Initialize(fixture.GetComponent<MeshFilter>().sharedMesh);
   await Task.Delay(150);
   float raw=0,mapped=0,poseDelta=0;int count=0;
   for(int i=0;i<20;i++)
   {
    await Task.Delay(17);var ray=new Ray(fixture.transform.position+Vector3.up*4,Vector3.down);
    if(!shape.Parts[0].Raycast(ray,out var a,8) || !shape.Raycast(ray,out var b,8))throw new Exception("Known cube surface missing");
    float expected=fixture.transform.position.y+1;
    raw=Mathf.Max(raw,Mathf.Abs(a.point.y-expected));mapped=Mathf.Max(mapped,Mathf.Abs(b.point.y-expected));poseDelta=Mathf.Max(poseDelta,Vector3.Distance(body.position,fixture.transform.position));count++;
   }
   if(mapped>.001f)throw new Exception("Mapped interpolated mesh support disagrees with visible cube: "+mapped+" raw="+raw+" poseDelta="+poseDelta);
   return new{samples=count,maximumNativeError=raw,maximumMappedError=mapped,maximumPhysicsRenderSeparation=poseDelta};
  }
  finally{UnityEngine.Object.Destroy(fixture);}
 }
}
