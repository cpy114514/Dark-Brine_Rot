using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
public static class VerifyGrassStability
{
 const string W=".codex/stability-20261106";
 [StructLayout(LayoutKind.Sequential)] struct MemoryCounters{public uint cb,faults;public UIntPtr peakWorking,working,peakPaged,paged,peakNonPaged,nonPaged,pageFile,peakPageFile,privateBytes;}
 [DllImport("psapi.dll",SetLastError=true)] static extern bool GetProcessMemoryInfo(IntPtr process,out MemoryCounters counters,uint size);
 [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
 static long PrivateBytes(){if(!GetProcessMemoryInfo(GetCurrentProcess(),out var memory,(uint)Marshal.SizeOf<MemoryCounters>()))throw new Exception("Process memory unavailable");return (long)memory.privateBytes.ToUInt64();}
 public static object Prepare()
 {
  if(Application.isPlaying||EditorSceneManager.GetSceneManagerSetup().Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes before isolated test");
  File.WriteAllText(W+"/workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
  Directory.CreateDirectory(W+"/saves");foreach(var f in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*"))File.Copy(f,W+"/saves/"+Path.GetFileName(f),true);
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);return "Isolated rendering stability scene";
 }
 public static object Restore()
 {
  if(Application.isPlaying)throw new Exception("Test still running");
  foreach(var f in Directory.GetFiles(W+"/saves"))if(!File.ReadAllBytes(f).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(f)))))throw new Exception("Player save changed");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"/workspace.json")));return new{restored=true,savesUnchanged=true};
 }
 public static object CycleScenes()
 {
  if(Application.isPlaying||EditorSceneManager.GetSceneManagerSetup().Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Clean stopped workspace required");
  var setup=EditorSceneManager.GetSceneManagerSetup();var counts=new List<int>();
  try{for(int i=0;i<3;i++)
   {
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
    EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
    var grass=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();if(!grass||!grass.IsReady||grass.InstanceCount<100000)throw new Exception("Grass did not recover after scene load");counts.Add(grass.InstanceCount);
   }
   var result=new{passed=true,cycles=counts.Count,grassCounts=counts};File.WriteAllText(W+"/scene-cycles.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
 static async Task Frame(){int f=Time.frameCount;var timeout=DateTime.UtcNow.AddSeconds(40);while(Time.frameCount==f){if(!Application.isPlaying||DateTime.UtcNow>timeout)throw new Exception("Frame stalled");await Task.Delay(5);}}
 public static object Start(){if(!Application.isPlaying||SceneManager.sceneCount!=2)throw new Exception("Isolated Play required");_ = Run();return "Five minute rendering and dormant-camera regression started";}
 static async Task Run()
 {
  GameObject holder=null;RenderTexture target=null;var samples=new List<object>();var checks=new List<string>();string error=null;
  int oldRate=Application.targetFrameRate,oldSync=QualitySettings.vSyncCount;Application.LogCallback handler=null;int warnings=0;
  try
  {
   handler=(message,stack,type)=>{if(message.Contains("Graphics Ring Buffer")||message.Contains("Could not allocate memory"))warnings++;};Application.logMessageReceived+=handler;
   holder=new GameObject("Owned stability camera",typeof(Camera));holder.tag="MainCamera";var camera=holder.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=57;camera.nearClipPlane=.15f;camera.farClipPlane=1200;
   camera.transform.position=new Vector3(251,47,439);camera.transform.LookAt(new Vector3(235,40,470));
   target=new RenderTexture(960,540,24);target.Create();camera.targetTexture=target;
   Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
   var grass=UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();if(!grass)throw new Exception("Actual grass missing");
   var manager=UnityEngine.Object.FindFirstObjectByType<WorldPerformanceManager>();
   for(int f=0;f<1800&&(!grass.IsReady||(manager&&!manager.IsReady));f++)await Frame();
   if(!grass.IsReady)throw new Exception("Grass never became ready");
   grass.enabled=false;grass.enabled=true;while(!grass.IsReady)await Frame();
   // This disabled camera is never rendered during the submission abuse phase.
   grass.Submit(camera);if(grass.VisibleBatches!=0)throw new Exception("Dormant camera accepted a draw");
   for(int i=0;i<20000;i++)grass.Submit(camera);
   if(grass.VisibleBatches!=0)throw new Exception("Dormant submissions accumulated");checks.Add("20000 dormant-camera submissions rejected");
   camera.Render();if(grass.VisibleBatches<=0)throw new Exception("Positive control: grass did not render");checks.Add("Actual island grass renders from camera callback");
   var watch=Stopwatch.StartNew();double nextSample=0;int cycles=0;long basePrivate=0;
   while(watch.Elapsed.TotalSeconds<300)
   {
    // Render explicitly, then leave the target dormant for updates between frames.
    if(cycles%3==0)camera.Render();
    if(cycles%240==0){grass.enabled=false;grass.enabled=true;}
    if(watch.Elapsed.TotalSeconds>=nextSample)
    {
     {long memory=PrivateBytes();if(basePrivate==0&&watch.Elapsed.TotalSeconds>=30)basePrivate=memory;
      if(basePrivate>0&&memory-basePrivate>1024L*1024*1024)throw new Exception("Private memory grew by more than 1 GiB");
      samples.Add(new{seconds=watch.Elapsed.TotalSeconds,privateBytes=memory,unityAllocated=Profiler.GetTotalAllocatedMemoryLong(),grassInstances=grass.InstanceCount,visibleBatches=grass.VisibleBatches});}
     nextSample+=10;File.WriteAllText(W+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{cycles,warnings,samples},Newtonsoft.Json.Formatting.Indented));
    }
    cycles++;await Frame();
   }
   checks.Add("Five minutes of rendered/dormant updates and repeated grass enable/disable");
  }
  catch(Exception e){error=e.ToString();}
  finally
  {
   if(handler!=null)Application.logMessageReceived-=handler;
   if(holder)UnityEngine.Object.DestroyImmediate(holder);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;
   File.WriteAllText(W+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=error==null,checks,warnings,error,samples},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;
  }
 }
}

