using System.Linq;
using System.Reflection;
using UnityEngine;
public static class AuditBuoyancyPressureV016
{
 public static object Inspect()
 {
  var ocean=Object.FindFirstObjectByType<OceanWorld>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var rows=Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).Where(p=>!p.IsBroken).Select(p=>
  {
   var f=p.GetComponent<Story1WreckFloatBody>();var b=f.Body;
   var points=(Vector3[])typeof(Story1WreckFloatBody).GetField("pressureLocal",flags).GetValue(f);
   var weights=(float[])typeof(Story1WreckFloatBody).GetField("pressureWeights",flags).GetValue(f);
   var pressure=(float[])typeof(Story1WreckFloatBody).GetField("pressures",flags).GetValue(f);
   var heights=(float[])typeof(Story1WreckFloatBody).GetField("pointSurface",flags).GetValue(f);
   float mean=0,lift=0;for(int i=0;i<4;i++){mean+=heights[i]*weights[i];lift+=pressure[i]*weights[i];}
   Vector3 deck=p.BoardingPoint(b.worldCenterOfMass);
   return new{p.name,mass=b.mass,tilt=Vector3.Angle(p.transform.up,Vector3.up),centre=p.transform.position.ToString(),centreHeight=ocean.SampleSurfaceHeight(deck,Time.time),actualDeck=deck.y,meanCornerHeight=mean,heave=b.linearVelocity.y,pressures=pressure.ToArray(),weights=weights.ToArray(),rawWeightedLift=lift};
  }).ToArray();
  System.IO.File.WriteAllText(".codex/encounter-v015/corner-pressure-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
 }
}
