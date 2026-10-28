using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using UnityEngine.Rendering;using Mavis;
public static class AutoQaFoliageTrace
{
 const string K="AUTO_QA.Foliage017.Trace";const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public sealed class Row {public int renderer,filter,mesh;public int[] materials;public bool forced,occlusion;public int shadows;public string[] paths;}
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static object Value(object o,string n)=>o.GetType().GetField(n).GetValue(o);
 static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m=>m.gameObject.scene.IsValid());
 static T Resolve<T>(int id) where T:UnityEngine.Object=>EditorUtility.EntityIdToObject(id) as T;
 static object Write(string suffix,object value){File.WriteAllText("AUTO_QA/Evidence/"+SessionState.GetString(K+".Take","")+suffix+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));return value;}
 public static object Arm(string take)
 {
  if(!Application.isPlaying||!string.IsNullOrEmpty(SessionState.GetString(K+".Take","")))throw new Exception("Unowned Play required");
  SessionState.SetString(K+".Take",take);AssemblyReloadEvents.beforeAssemblyReload+=Capture;
  return new{armed=true};
 }
 static void Capture()
 {
  var entries=((IList)Get(Manager,"foliage")).Cast<object>();
  var rows=entries.Select(e=>new Row{renderer=((MeshRenderer)Value(e,"renderer")).GetInstanceID(),filter=((MeshFilter)Value(e,"filter")).GetInstanceID(),mesh=((Mesh)Value(e,"original")).GetInstanceID(),materials=((Material[])Value(e,"materials")).Select(m=>m?m.GetInstanceID():0).ToArray(),paths=((Material[])Value(e,"materials")).Select(m=>m?AssetDatabase.GetAssetPath(m):"NULL").ToArray(),forced=(bool)Value(e,"forced"),occlusion=(bool)Value(e,"occlusion"),shadows=(int)(ShadowCastingMode)Value(e,"shadows")}).ToArray();
  SessionState.SetString(K+".Rows",Newtonsoft.Json.JsonConvert.SerializeObject(rows));Write("-captured",new{rows});
  AssemblyReloadEvents.beforeAssemblyReload-=Capture;
 }
 static async Task Frames(int n){int end=Time.frameCount+n;var limit=DateTime.UtcNow.AddSeconds(12);while(Time.frameCount<end){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Frame timeout");await Task.Delay(5);}}
 public static async Task<object> Recover()
 {
  var snapshot=Newtonsoft.Json.JsonConvert.DeserializeObject<Row[]>(SessionState.GetString(K+".Rows",""));
  var manager=Manager;manager.enabled=false;
  var before=snapshot.Select(e=>new {e.renderer,rendererResolves=Resolve<MeshRenderer>(e.renderer)!=null,materialResolves=e.materials.All(id=>Resolve<Material>(id)!=null),original=e.forced,current=Resolve<MeshRenderer>(e.renderer)?.forceRenderingOff}).ToArray();
  foreach(var e in snapshot){var r=Resolve<MeshRenderer>(e.renderer);var filter=Resolve<MeshFilter>(e.filter);if(!r||!filter)throw new Exception("Native original ID missing");r.forceRenderingOff=e.forced;r.allowOcclusionWhenDynamic=e.occlusion;r.shadowCastingMode=(ShadowCastingMode)e.shadows;r.sharedMaterials=e.materials.Select(Resolve<Material>).ToArray();filter.sharedMesh=Resolve<Mesh>(e.mesh);}
  var details=(IDictionary)Get(manager,"details");var meshes=(IDictionary)Get(manager,"renderMeshes");details.Clear();meshes.Clear();
  foreach(var e in Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library").entries)if(e.source){details[e.source]=e;if(e.renderSource)meshes[e.source]=e.renderSource;}
  manager.GetType().GetField("visibleGrass",F).SetValue(manager,Activator.CreateInstance(manager.GetType().GetNestedType("GrassBatch",BindingFlags.NonPublic)));
  manager.enabled=true;await Frames(30);
  var entries=((IList)Get(manager,"foliage")).Cast<object>().ToArray();var bad=entries.Where(e=>(bool)Value(e,"forced")||((Material[])Value(e,"materials")).Any(m=>!m||m.name.Contains("Runtime Instanced"))).Count();
  var recovered=new{before,entries=entries.Length,bad,details=details.Count,meshes=meshes.Count,batches=((ICollection)Get(manager,"activeGrassBatches")).Count,submitted=Get(manager,"submittedGrassBatches")};
  var result=Write("-recovered",recovered);SessionState.EraseString(K+".Rows");SessionState.EraseString(K+".Take");return result;
 }
}
