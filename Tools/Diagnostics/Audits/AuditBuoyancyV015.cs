using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class AuditBuoyancyV015
{
    public static object Inspect()
    {
        var rows=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).Where(p=>!p.IsBroken).Select(p=>
        {
            var floating=p.GetComponent<Story1WreckFloatBody>();var body=floating.Body;
            var points=(Vector3[])typeof(Story1WreckFloatBody).GetField("pressureLocal",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(floating);
            var weights=(float[])typeof(Story1WreckFloatBody).GetField("pressureWeights",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(floating);
            var normal=(Vector3)typeof(Story1WreckFloatBody).GetField("broadFaceNormal",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(floating);
            Vector3 mean=Vector3.zero;for(int i=0;i<4;i++)mean+=points[i]*weights[i];
            Vector3 centre=Quaternion.Inverse(body.rotation)*(body.worldCenterOfMass-body.position);
            float imbalance=Vector3.ProjectOnPlane(mean-centre,normal).magnitude;
            return new{p.name,imbalance,weights,pressureMean=mean.ToString("F3"),nativeCentre=centre.ToString("F3"),tilt=Vector3.Angle(p.transform.up,Vector3.up),mass=body.mass};
        }).OrderByDescending(p=>p.imbalance).ToArray();
        System.IO.File.WriteAllText(".codex/encounter-v015/buoyancy-centre-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
}
