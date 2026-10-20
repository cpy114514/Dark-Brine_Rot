using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class ProbeOpeningQueriesV225
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(Story1WreckBattle);object Get(string n)=>type.GetField(n,flags).GetValue(b);
  var tree=Get("redRiggingSkinTree");var tt=tree.GetType();var points=(Vector3[])Get("redTutorialSharkVertices");var current=(Vector3[])tt.GetField("vertices",flags).GetValue(tree);var triangles=(int[])tt.GetProperty("Triangles").GetValue(tree);
  if(points==null||points.Length==0)throw new Exception("Need tutorial/duel pose");var reference=(Vector3[])tt.GetField("conservativeReference",flags).GetValue(tree);
  var exact=Activator.CreateInstance(tt,new object[]{(Vector3[])current.Clone(),triangles});var articulated=Activator.CreateInstance(tt,new object[]{(Vector3[])(reference??current).Clone(),triangles});
  tt.GetMethod("RefitArticulated").Invoke(articulated,new object[]{reference??current,1f});tt.GetMethod("RefitArticulated").Invoke(articulated,new object[]{current,1f});
  var method=tt.GetMethod("CapsuleGapUpTo");var caps=(IList)Get("redTutorialBodySpace");var queries=new List<object[]>();
  foreach(var cap in caps){var c=cap.GetType();queries.Add(new[]{c.GetField("a").GetValue(cap),c.GetField("b").GetValue(cap),c.GetField("radius").GetValue(cap),(object).06f});}
  var rows=new List<object>();var all=new List<float[]>();
  foreach(var named in new[]{("current",tree),("exact",exact),("articulated",articulated)})
  {
   var values=new float[queries.Count];var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<queries.Count;i++)values[i]=(float)method.Invoke(named.Item2,queries[i]);var ms=watch.Elapsed.TotalMilliseconds;rows.Add(new{method=named.Item1,queries=queries.Count,ms,minimum=values.Min()});all.Add(values);
  }
  float error=0;for(int i=0;i<queries.Count;i++)error=Mathf.Max(error,Mathf.Abs(all[0][i]-all[1][i]),Mathf.Abs(all[0][i]-all[2][i]));
  var result=new{stage=b.RedesignStage.ToString(),b.RedesignClock,vertices=points.Length,triangles=triangles.Length/3,padding=(float)tt.GetField("conservativePadding",flags).GetValue(tree),maximumError=error,rows};
  File.WriteAllText(".codex/animation-performance-20261019/query-probe-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
