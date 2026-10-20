using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
public static class ProfileOpeningContactsV224
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var t=typeof(Story1WreckBattle);var f=BindingFlags.Instance|BindingFlags.NonPublic;
  object Call(string name,params object[] args)=>t.GetMethod(name,f).Invoke(b,args);
  var watch=System.Diagnostics.Stopwatch.StartNew();var skin=(SkinnedMeshRenderer)Call("RedContactSkin");double skinMs=watch.Elapsed.TotalMilliseconds;
  var tree=t.GetField("redSolidSharkTree",f).GetValue(b);var points=t.GetField("redSolidSkin",f).GetValue(b);
  watch.Restart();tree.GetType().GetMethod("Refit").Invoke(tree,new[]{points});double treeMs=watch.Elapsed.TotalMilliseconds;
  var rows=new List<(string name,int vertices,int triangles,double ms,bool touch)>();
  foreach(var shape in UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None))
  {
   if(!shape.Active)continue;watch.Restart();bool touch=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});double ms=watch.Elapsed.TotalMilliseconds;
   var filter=shape.GetComponent<MeshFilter>();rows.Add((shape.name,filter.sharedMesh.vertexCount,(int)filter.sharedMesh.GetIndexCount(0)/3,ms,touch));
  }
  var result=new{b.RedesignStage,b.RedesignBeat,skinMs,treeMs,queries=rows.Count,totalQueriesMs=rows.Sum(r=>r.ms),slowest=rows.OrderByDescending(r=>r.ms).Take(8).Select(r=>new{r.name,r.vertices,r.triangles,r.ms,r.touch})};
  File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/encounter-redesign-20261017/contact-v224/contact-profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
