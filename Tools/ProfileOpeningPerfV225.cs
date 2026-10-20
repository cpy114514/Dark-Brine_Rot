using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
public static class ProfileOpeningPerfV225
{
 public static object Start(string label="before",int fps=30)
 {
  if(!Application.isPlaying)throw new Exception("Use current Editor Play Mode");
  string path=".codex/animation-performance-20261019/"+label+"-"+fps;
  if(Directory.Exists(path))throw new Exception("Preserve existing evidence");Directory.CreateDirectory(path);
  var go=new GameObject("Temporary opening performance observer v225");var probe=go.AddComponent<OpeningPerfV225>();probe.folder=path;probe.fps=fps;return new{started=true,path,fps,captures=false};
 }
}
[DefaultExecutionOrder(31500)]public sealed class OpeningPerfV225:MonoBehaviour
{
 public string folder;public int fps;
 readonly List<object> rows=new List<object>(5000);
 readonly List<ProfilerRecorder> counters=new List<ProfilerRecorder>();readonly List<string> names=new List<string>();
 readonly string[] desired={"Main Thread","Render Thread","PlayerLoop","GC Allocated In Frame","GC.Collect","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","Physics.Simulate","Animator.Update","RenderPipelineManager.DoRenderLoop_Internal","Gfx.WaitForPresentOnGfxThread","Opening.NativeSolids","Opening.ContactWarmup","Opening.Support","Opening.WaterExitPlan","Opening.TailSkin","Opening.HeroWood","Opening.SharkWood","Opening.SkinIndex","Opening.SharkHero","Story.WreckBuoyancy","Wreck.CrushCreate","Wreck.CrushWarmFlush","Wreck.CrushBodies","Wreck.RegisterRiderContacts"};
 Story1WreckBattle battle;Story1SharkCollisionSequence sequence;double started,previous;int frames,waiting;bool done,microProbed;int oldRate,oldSync;
 void Start()
 {
  sequence=FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();started=previous=Time.realtimeSinceStartupAsDouble;
  oldRate=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;Time.captureFramerate=fps;
  battle.DiagnosticScatterSeed=223;battle.RedesignNearPassDiagnostics=false;
  var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
  foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);if(desired.Contains(d.Name)){var r=ProfilerRecorder.StartNew(d.Category,d.Name,1);if(r.Valid){names.Add(d.Name);counters.Add(r);}}}
  foreach(string n in desired.Where(n=>n.StartsWith("Opening.")||n.StartsWith("Wreck.")||n.StartsWith("Story.")))if(!names.Contains(n)){var r=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,n,1);if(r.Valid){names.Add(n);counters.Add(r);}}
  if(!battle.RedesignActive)sequence.BeginImpact();
 }
 void LateUpdate()
 {
  if(done||!battle)return;double now=Time.realtimeSinceStartupAsDouble;var values=new long[counters.Count];for(int i=0;i<values.Length;i++)values[i]=counters[i].LastValue;
  rows.Add(new{frame=Time.frameCount,clock=battle.RedesignClock,stage=battle.RedesignStage.ToString(),beat=battle.RedesignBeat,lesson=battle.CurrentLesson.ToString(),battle.TutorialWaiting,wallMs=(now-previous)*1000,solidMs=battle.RedesignSolidMs,previewPrepared=battle.RedesignWaterPreviewPrepared,heroWood=battle.RedesignSolidHeroWoodDepth,unresolved=battle.RedesignSolidUnresolved,battle.RedesignSolidFractures,contacts=battle.RedesignContacts,counters=values});
  previous=now;frames++;
  if(folder.Contains("before") && !microProbed && battle.RedesignStage==Story1WreckBattle.OpeningStage.Pursuit && battle.RedesignSolidMs>100)
  {microProbed=true;MicroQueriesV225.Run();}
  if(battle.TutorialWaiting){if(++waiting>=Math.Max(1,fps/2)){battle.DiagnosticAcceptLesson();waiting=0;}}
  else {waiting=0;if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial)battle.DiagnosticCompleteMovement();}
  if(frames%Math.Max(1,fps)==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frames,stage=battle.RedesignStage.ToString(),clock=battle.RedesignClock,wallSeconds=now-started}));
  if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Finished||now-started>300||frames>Math.Max(1,fps)*150)Finish();
 }
 void Finish(){done=true;Save();EditorApplication.isPlaying=false;}
 void Save(){if(folder==null)return;File.WriteAllText(folder+"/profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{fps,frames,stage=battle?battle.RedesignStage.ToString():"stopped",names,rows,scope="Existing Editor, deterministic pose stepping, no image capture or independent geometry diagnostics. Counters refer to last completed frame; wall intervals include Editor/observer/render work. Not release FPS."},Newtonsoft.Json.Formatting.Indented));}
 void OnDestroy(){if(!done)Save();foreach(var r in counters)r.Dispose();Time.captureFramerate=0;Time.timeScale=1;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;}
}

public static class MicroQueriesV225
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
  File.WriteAllText(".codex/animation-performance-20261019/query-probe-worst-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
