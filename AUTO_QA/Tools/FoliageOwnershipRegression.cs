using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using UnityEngine.Rendering;using Mavis;
public static class AutoQaFoliageOwnership
{
 const string K="AUTO_QA.Foliage017.Ownership";const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m=>m.gameObject.scene.IsValid());
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
 static async Task Frames(int n){int end=Time.frameCount+n;var limit=DateTime.UtcNow.AddSeconds(10);while(Time.frameCount<end){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Native frames stalled");await Task.Delay(5);}}
 static async Task Ready(){var limit=DateTime.UtcNow.AddSeconds(12);while(!Manager.IsReady){if(DateTime.UtcNow>limit)throw new Exception("Foliage cache timeout");await Frames(1);}await Frames(16);}
 static void Check(List<object> rows,bool ok,string label)=>rows.Add(new{ok,label});
 static object Write(string suffix,object value){File.WriteAllText("AUTO_QA/Evidence/"+SessionState.GetString(K+".Take","")+suffix+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));return value;}
 public static object Repeat(string take)
 {
  if(!Application.isPlaying||string.IsNullOrEmpty(SessionState.GetString("AUTO_QA.Foliage016.Take","")))throw new Exception("Owned foliage fixture required");
  SessionState.SetString("AUTO_QA.Foliage016.Take",take);
  SessionState.SetString("AUTO_QA.Foliage016.Assembly",typeof(AutoQaFoliageOwnership).Assembly.GetName().Name);
  return new{repeatReady=true};
 }
 public static async Task<object> Prepare(string take)
 {
  if(!Application.isPlaying||!string.IsNullOrEmpty(SessionState.GetString(K+".Take","")))throw new Exception("Unowned ownership test required");
  SessionState.SetString(K+".Take",take);SessionState.SetString(K+".Assembly",typeof(AutoQaFoliageOwnership).Assembly.GetName().Name);
  var manager=Manager;manager.enabled=false;
  var root=EditorUtility.EntityIdToObject(SessionState.GetInt("AUTO_QA.Foliage016.Root",0)) as GameObject;
  var renderer=root.GetComponentsInChildren<MeshRenderer>().First();
  renderer.forceRenderingOff=true;renderer.allowOcclusionWhenDynamic=false;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
  SessionState.SetInt(K+".Renderer",renderer.GetInstanceID());SessionState.SetInt(K+".Mesh",renderer.GetComponent<MeshFilter>().sharedMesh.GetInstanceID());SessionState.SetInt(K+".Material",renderer.sharedMaterial.GetInstanceID());
  manager.enabled=true;await Ready();
  return Write("-before",new{originalHidden=true,originalOcclusion=false,originalShadows="TwoSided",renderer=renderer.GetInstanceID()});
 }
 public static async Task<object> AfterReload()
 {
  await Ready();var manager=Manager;var renderer=EditorUtility.EntityIdToObject(SessionState.GetInt(K+".Renderer",0)) as MeshRenderer;
  var rows=new List<object>();var entries=((IList)Get(manager,"foliage")).Cast<object>().ToArray();var owned=entries.Single(e=>(MeshRenderer)Field(e,"renderer")==renderer);
  Check(rows,!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name==SessionState.GetString(K+".Assembly","")),"Ownership fixture actually reloaded");
  Check(rows,entries.Length==SessionState.GetInt("AUTO_QA.Foliage016.ExpectedCount",36)&&entries.Count(e=>(bool)Field(e,"forced"))==1,"Exactly the original hidden renderer stays excluded");
  Check(rows,(bool)Field(owned,"forced")&&!(bool)Field(owned,"occlusion")&&(ShadowCastingMode)Field(owned,"shadows")==ShadowCastingMode.TwoSided,"Original hidden/occlusion/shadow state preserved in cache");
  Check(rows,((Material[])Field(owned,"materials"))[0].GetInstanceID()==SessionState.GetInt(K+".Material",0),"Original material identity preserved across reload");
  Check(rows,((Mesh)Field(owned,"original")).GetInstanceID()==SessionState.GetInt(K+".Mesh",0),"Original mesh identity preserved across reload");
  Check(rows,string.IsNullOrEmpty(SessionState.GetString("Mavis.WorldPerformance.Reload."+manager.GetInstanceID(),"")),"Production reload handoff consumed exactly once");
  manager.enabled=false;
  Check(rows,renderer.forceRenderingOff&&!renderer.allowOcclusionWhenDynamic&&renderer.shadowCastingMode==ShadowCastingMode.TwoSided,"Disable restores original hidden/occlusion/shadow flags");
  Check(rows,renderer.sharedMaterial.GetInstanceID()==SessionState.GetInt(K+".Material",0)&&renderer.GetComponent<MeshFilter>().sharedMesh.GetInstanceID()==SessionState.GetInt(K+".Mesh",0),"Disable restores exact original material and mesh");
  Check(rows,entries.Where(e=>e!=owned).All(e=>!((MeshRenderer)Field(e,"renderer")).forceRenderingOff),"Other original visible renderers restored");
  renderer.forceRenderingOff=false;renderer.allowOcclusionWhenDynamic=true;renderer.shadowCastingMode=ShadowCastingMode.On;
  manager.enabled=true;await Ready();
  var camera=Camera.main;int mask=camera.cullingMask;
  try{camera.cullingMask=0;await Frames(3);Check(rows,(int)Get(manager,"submittedGrassBatches")==0,"Map-style zero camera mask submits no grass");camera.cullingMask=mask;await Frames(3);Check(rows,(int)Get(manager,"submittedGrassBatches")>0,"Restored world camera resumes grass submissions");}
  finally{camera.cullingMask=mask;}
  var result=Write("-after",new{rows});
  foreach(var name in new[]{"Renderer","Mesh","Material"})SessionState.EraseInt(K+"."+name);SessionState.EraseString(K+".Assembly");SessionState.EraseString(K+".Take");return result;
 }
}
