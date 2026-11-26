using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
public static class VerifyWaveBuoyancyV015
{
 public static async Task<object> Verify()
 {
  var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
  var source=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).First(p=>p.name.StartsWith("Hull board 3 ")&&!p.IsBroken);
  var go=new GameObject("Actual timber wave response fixture");
  try
  {
   go.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
   var plank=go.AddComponent<Story1WreckPlank>();plank.SetFractureSource(source.FractureSource);
   Vector3 at=source.transform.position+Vector3.right*300;at.y=ocean.SampleSurfaceHeight(at,Time.time)+.4f;
   plank.Initialize(at,Quaternion.Euler(8,35,5),source.Dimensions,ocean,Vector3.zero,Vector3.zero);
   var body=go.GetComponent<Rigidbody>();var rows=new System.Collections.Generic.List<object>();float worst=0;
   for(int i=0;i<150;i++)
   {
    await Task.Delay(100);
    float tilt=Vector3.Angle(body.rotation*Vector3.up,Vector3.up);
    Vector3 edge=plank.BoardingPoint(go.transform.position+go.transform.forward*100);
    float edgeClearance=edge.y-ocean.SampleSurfaceHeight(edge,Time.time);
    Vector3 centre=plank.BoardingPoint(body.worldCenterOfMass);
    float centreClearance=centre.y-ocean.SampleSurfaceHeight(centre,Time.time);
    if(i>30)worst=Mathf.Max(worst,tilt);
    rows.Add(new{i,tilt,edgeClearance,centreClearance,heave=body.linearVelocity.y,angular=body.angularVelocity.magnitude});
   }
   var result=new{actualWaves=true,originalMesh=true,worstSettledTilt=worst,rows};
   System.IO.File.WriteAllText(".codex/encounter-v015/wave-buoyancy-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
