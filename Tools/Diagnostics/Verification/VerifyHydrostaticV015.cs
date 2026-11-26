using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
public static class VerifyHydrostaticV015
{
    public static async Task<object> Verify()
    {
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var source=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).First(p=>p.name.StartsWith("Hull board 3 ") && !p.IsBroken);
        var waves=new[]{ocean.wave1,ocean.wave2,ocean.wave3,ocean.wave4};GameObject go=null;
        try
        {
            ocean.wave1.amplitude=ocean.wave2.amplitude=ocean.wave3.amplitude=ocean.wave4.amplitude=0;
            go=new GameObject("Real irregular deck hydrostatic fixture");go.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
            var plank=go.AddComponent<Story1WreckPlank>();plank.SetFractureSource(source.FractureSource);
            Vector3 at=source.transform.position+Vector3.right*300;at.y=ocean.SampleSurfaceHeight(at,Time.time)+.4f;
            plank.Initialize(at,Quaternion.Euler(8,35,5),source.Dimensions,ocean,Vector3.zero,Vector3.zero);
            await Task.Delay(10000);
            var floating=go.GetComponent<Story1WreckFloatBody>();var body=floating.Body;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var points=(Vector3[])typeof(Story1WreckFloatBody).GetField("pressureLocal",flags).GetValue(floating);
            var weights=(float[])typeof(Story1WreckFloatBody).GetField("pressureWeights",flags).GetValue(floating);
            Vector3 mean=Vector3.zero;for(int i=0;i<4;i++)mean+=points[i]*weights[i];
            Vector3 centre=Quaternion.Inverse(body.rotation)*(body.worldCenterOfMass-body.position);
            float bias=Vector3.ProjectOnPlane(mean-centre,Vector3.up).magnitude;
            float tilt=Vector3.Angle(body.rotation*Vector3.up,Vector3.up);
            var result=new{actualReleasedMesh=true,compoundParts=plank.Collision.Parts.Length,tiltDegrees=tilt,heaveSpeed=body.linearVelocity.y,angularSpeed=body.angularVelocity.magnitude,forceCentreError=bias};
            System.IO.File.WriteAllText(".codex/encounter-v015/hydrostatic-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(tilt>1.5f || Mathf.Abs(body.linearVelocity.y)>.15f || body.angularVelocity.magnitude>.12f || bias>.001f)throw new Exception("Actual timber did not settle by balanced gravity/buoyancy: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);ocean.wave1=waves[0];ocean.wave2=waves[1];ocean.wave3=waves[2];ocean.wave4=waves[3];}
    }
}
