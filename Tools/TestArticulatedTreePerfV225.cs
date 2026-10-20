using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class TestArticulatedTreePerfV225
{
 public static object Run()
 {
  var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
  var update=type.GetMethod("RefitArticulated");if(update==null)throw new Exception("Compile articulated index first");
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=go.GetComponent<MeshFilter>().sharedMesh;
  var cube=mesh.vertices;var indices=mesh.triangles;UnityEngine.Object.DestroyImmediate(go);
  var source=new Vector3[cube.Length*48];var triangles=new int[indices.Length*48];
  for(int i=0;i<48;i++){var offset=new Vector3(i%4*2,(i/4)%3*2,i/12*2);for(int j=0;j<cube.Length;j++)source[i*cube.Length+j]=cube[j]+offset;for(int j=0;j<indices.Length;j++)triangles[i*indices.Length+j]=indices[j]+i*cube.Length;}
  var points=(Vector3[])source.Clone();var exact=Activator.CreateInstance(type,new object[]{points,triangles});var adaptive=Activator.CreateInstance(type,new object[]{points,triangles});
  var gap=type.GetMethod("CapsuleGapUpTo");var closest=type.GetMethod("Closest");
  var random=new System.Random(224);float Next(float a,float b)=>a+(b-a)*(float)random.NextDouble();float error=0;int checks=0;double exactMs=0,adaptiveMs=0;
  for(int pose=0;pose<16;pose++)
  {
   for(int i=0;i<points.Length;i++){var p=source[i];points[i]=p+new Vector3(Mathf.Sin(p.z+pose*.17f)*.2f,Mathf.Sin(p.x*.3f+pose*.07f)*.12f,Mathf.Cos(p.y+pose*.13f)*.18f)+(pose>=8?Vector3.up*3:Vector3.zero);}
   var watch=System.Diagnostics.Stopwatch.StartNew();type.GetMethod("Refit").Invoke(exact,new object[]{points});exactMs+=watch.Elapsed.TotalMilliseconds;
   watch.Restart();update.Invoke(adaptive,new object[]{points,1f});adaptiveMs+=watch.Elapsed.TotalMilliseconds;
   for(int query=0;query<120;query++)
   {
    Vector3 a=new Vector3(Next(-2,9),Next(-2,9),Next(-2,9)),b=a+new Vector3(Next(-2,2),Next(-2,2),Next(-2,2));
    object[] args={a,b,Next(.05f,.8f),.4f};float expected=(float)gap.Invoke(exact,args),actual=(float)gap.Invoke(adaptive,args);error=Mathf.Max(error,Mathf.Abs(actual-expected));checks++;
    object[] ca={a,0,Vector3.zero,Vector3.zero},cb={a,0,Vector3.zero,Vector3.zero};closest.Invoke(exact,ca);closest.Invoke(adaptive,cb);error=Mathf.Max(error,Vector3.Distance((Vector3)ca[3],(Vector3)cb[3]));checks++;
    if(error>.001f)throw new Exception("Articulated conservative bounds missed geometry at "+pose+"/"+query+": "+error);
   }
  }
  var result=new{passed=true,checks,maximumError=error,exactMs,adaptiveMs};File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-performance-20261019/articulated-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
