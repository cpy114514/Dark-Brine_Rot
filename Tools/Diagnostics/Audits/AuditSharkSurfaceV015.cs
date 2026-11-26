using System;
using System.Linq;
using UnityEngine;
public static class AuditSharkSurfaceV015
{
 static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
 {
  Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return a;
  Vector3 bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
  float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
  Vector3 cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
  float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
  float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
  float den=1/(va+vb+vc);return a+ab*(vb*den)+ac*(vc*den);
 }
 public static object Inspect()
 {
  var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();var skin=(SkinnedMeshRenderer)shark.VisualRenderer;
  string old=shark.CinematicAction;float oldTime=shark.CinematicActionSeconds;var mesh=new Mesh();var rows=new System.Collections.Generic.List<object>();
  try
  {
   foreach(var sample in new[]{("Swim",0f),("Bite_Lunge",.52f),("Tail_Strike",.2f),("Tail_Strike",.65f),("Ship_Smash",.65f)})
   {
    shark.SampleCinematicAction(sample.Item1,sample.Item2);skin.BakeMesh(mesh,true);var vs=mesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();var ts=mesh.triangles;
    float Gap(Vector3 marker){float best=float.PositiveInfinity;for(int i=0;i<ts.Length;i+=3)best=Mathf.Min(best,(Closest(marker,vs[ts[i]],vs[ts[i+1]],vs[ts[i+2]])-marker).sqrMagnitude);return Mathf.Sqrt(best);}
    rows.Add(new{action=sample.Item1,time=sample.Item2,noseSkinGap=Gap(shark.NoseWorldPoint),tailSkinGap=Gap(shark.TailWorldPoint)});
   }
   System.IO.File.WriteAllText(".codex/encounter-v015/shark-surface-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
  }
  finally{UnityEngine.Object.DestroyImmediate(mesh);shark.SampleCinematicAction(old??"Swim",oldTime);}
 }
}
