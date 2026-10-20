using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public static class TestNativeBvhV225
{
 public static object Run()
 {
  var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();if(sequence==null||sequence.swimmer==null)throw new Exception("Run after Start in existing Play Mode");var mesh=sequence.swimmer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
  var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);var source=mesh.vertices;var indices=mesh.triangles;var points=(Vector3[])source.Clone();
  var actual=Activator.CreateInstance(type,new object[]{(Vector3[])points.Clone(),indices});var gap=type.GetMethod("CapsuleGapUpTo");var closest=type.GetMethod("Closest");var random=new System.Random(225);int checks=0;float error=0;double rebuildMs=0,refitMs=0;
  float Next(float lo,float hi)=>lo+(hi-lo)*(float)random.NextDouble();
  for(int pose=0;pose<9;pose++)
  {
   for(int i=0;i<points.Length;i++){var p=source[i];points[i]=p+new Vector3(Mathf.Sin(p.z+pose*.13f)*.12f,Mathf.Cos(p.x+pose*.07f)*.09f,Mathf.Sin(p.y+pose*.11f)*.07f);}
   var watch=System.Diagnostics.Stopwatch.StartNew();
   if(pose==3||pose==7)type.GetMethod("RefitConservative").Invoke(actual,new object[]{points,1f});
   else if(pose==0||pose==6)type.GetMethod("Refit").Invoke(actual,new object[]{points});
   else type.GetMethod("RefitArticulated").Invoke(actual,new object[]{points,.15f});
   refitMs+=watch.Elapsed.TotalMilliseconds;watch.Restart();
   // Constructor retains the original independent triangle-bounds/build path.
   var reference=Activator.CreateInstance(type,new object[]{(Vector3[])points.Clone(),indices});rebuildMs+=watch.Elapsed.TotalMilliseconds;
   var bounds=(Bounds)type.GetProperty("Bounds").GetValue(reference);
   for(int query=0;query<100;query++)
   {
    Vector3 a=bounds.center+new Vector3(Next(-1.3f,1.3f)*bounds.extents.x,Next(-1.3f,1.3f)*bounds.extents.y,Next(-1.3f,1.3f)*bounds.extents.z);
    Vector3 b=query%3==0?a:a+new Vector3(Next(-.6f,.6f),Next(-.6f,.6f),Next(-.6f,.6f));object[] args={a,b,Next(.01f,.2f),.4f};
    float expected=(float)gap.Invoke(reference,args),value=(float)gap.Invoke(actual,args);error=Mathf.Max(error,Mathf.Abs(value-expected));checks++;
    object[] ca={a,0,Vector3.zero,Vector3.zero},cb={a,0,Vector3.zero,Vector3.zero};closest.Invoke(reference,ca);closest.Invoke(actual,cb);error=Mathf.Max(error,Vector3.Distance((Vector3)ca[3],(Vector3)cb[3]));checks++;
    if(error>.0002f)throw new Exception("Native BVH refit/cache oracle mismatch "+pose+"/"+query+": "+error);
   }
  }
  var result=new{passed=true,checks,maximumError=error,vertices=source.Length,triangles=indices.Length/3,rebuildMs,refitMs,scope="Native shark triangles, scalar exact and capped distance/closest queries, mixed full/conservative/articulated updates; independent original constructor bounds path"};
  File.WriteAllText(".codex/animation-performance-20261019/native-bvh-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
