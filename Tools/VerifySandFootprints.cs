using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Mavis;
public static class VerifySandFootprints
{
 public static object Run()
 {
  if(!Application.isPlaying)throw new Exception("Runtime-only temporary fixture");
  var root=new GameObject("Temporary footprint verifier");
  const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
  try
  {
   var s=root.AddComponent<SandFootprints>();var type=typeof(SandFootprints);type.GetMethod("Bind",flags).Invoke(s,null);
   var hit=Physics.RaycastAll(new Vector3(330,100,315),Vector3.down,200,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<ProceduralIsland>()).OrderBy(h=>h.distance).First();
   var method=type.GetMethod("TryStamp",flags);
   bool Stamp(RaycastHit h,bool left)=>(bool)method.Invoke(s,new object[]{h,Vector3.forward,left,.9f});
   bool beach=Stamp(hit,true);
   var array=(Array)type.GetField("stamps",flags).GetValue(s);var stampType=array.GetType().GetElementType();
   var first=array.GetValue(0);var renderer=(MeshRenderer)stampType.GetField("renderer").GetValue(first);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);bool mirror=block.GetFloat("_Mirror")==-1;
   var high=hit;high.point=new Vector3(hit.point.x,500,hit.point.z);bool noGrass=!Stamp(high,false);
   var water=hit;water.point=new Vector3(hit.point.x,-5,hit.point.z);bool noWater=!Stamp(water,false);
   for(int i=0;i<40;i++)Stamp(hit,i%2==0);
   bool bounded=root.GetComponentsInChildren<MeshRenderer>(true).Length==28;
   for(int i=0;i<array.Length;i++)
   {
    var item=array.GetValue(i);stampType.GetField("born").SetValue(item,Time.time-100);array.SetValue(item,i);
   }
   type.GetMethod("Fade",flags).Invoke(s,null);bool expired=root.GetComponentsInChildren<MeshRenderer>().Length==0;
   Stamp(hit,true);s.enabled=false;bool disabled=root.GetComponentsInChildren<MeshRenderer>().Length==0;
   var errors=UnityEditor.ShaderUtil.GetShaderMessages(Shader.Find("Mavis/SandFootprint")).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();
   var result=new{beach,mirror,noGrass,noWater,bounded,expired,disabled,shaderErrors=errors,passed=beach&&mirror&&noGrass&&noWater&&bounded&&expired&&disabled&&errors.Length==0};
   File.WriteAllText(".codex/sand-footprints-20261107/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
   if(!result.passed)throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
  }
  finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}

