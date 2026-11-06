using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEditor;
public static class VerifyFlock066
{
 static readonly List<object> checks=new List<object>();static FlockCapture066 capture;
 static void Check(bool pass,string name){checks.Add(new{pass,name});if(!pass)throw new Exception(name);}
 static async Task Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)await Task.Delay(8);}
 public static object Start(){if(!Application.isPlaying)throw new Exception("Play required");_ = Run();return "Started actual island flock regression";}
 static async Task Run(){string error=null;try{
 var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();player.gameObject.AddComponent<GameSaveExcluded>();player.GetComponent<PlayerHealth>().GrantProtection(180);var arrival=player.GetComponent<FirstIslandArrival>();arrival.StopAllCoroutines();arrival.enabled=false;arrival.RestoreProgress(true,true);var objectives=player.GetComponent<FirstIslandObjectives>();objectives.Restore(new FirstIslandObjectives.Progress{stage=4,combatLesson=4});GameWorldProgress.Reset();
 var encounter=ChickenBroEncounter.Active;Check(encounter&&encounter.prefab&&encounter.duck&&encounter.goose,"Scene contains chicken, duck and goose production prefabs");
 capture=player.gameObject.AddComponent<FlockCapture066>();capture.encounter=encounter;
 await Wait(1);Check(!encounter.Started,"Flock cannot start before Cappuccino defeat");Check(!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Nailong waits for required encounters");
 var capri=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);capri.GetComponent<Health>().Kill();await Wait(.4f);
 Check(objectives.TryGetGuidanceTarget(out var target,out var id)&&id==encounter.completionId,"Quest guide selects the flock after Cappuccino instead of skipping to Nailong");Check(target.sqrMagnitude>1,"Flock target anchored on actual island");
 player.RestoreSavedPose(target+Vector3.back*12+Vector3.up*(-player.LowestFootWorldOffset+.1f),Quaternion.identity);Cursor.lockState=CursorLockMode.Locked;
 float deadline=Time.realtimeSinceStartup+40;while(!encounter.Started&&Time.realtimeSinceStartup<deadline)await Wait(.1f);
 Check(encounter.NavigationReady&&encounter.Started,"Asynchronous navigation ready and nearby player starts actual encounter");
 var map=player.GetComponent<IslandMapUI>();Check(map.SetOpen(true),"Island map opens");typeof(IslandMapUI).GetMethod("RebuildMarkers",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(map,null);var targets=(List<Transform>)typeof(IslandMapUI).GetField("targets",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(map);Check(targets.Contains(encounter.transform),"Map includes actual flock location");int pausedAlive=encounter.Alive;await Wait(.3f);Check(encounter.Alive==pausedAlive,"Paused map does not continue spawning flock members");map.SetOpen(false);
 var species=new HashSet<string>();var waveSeen=new HashSet<int>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;deadline=Time.realtimeSinceStartup+45;
 while(!encounter.Completed&&Time.realtimeSinceStartup<deadline){await Wait(.3f);if(encounter.Completed)break;int wave=(int)typeof(ChickenBroEncounter).GetField("wave",flags).GetValue(encounter);waveSeen.Add(wave);foreach(var bro in ChickenBroFlock.Active.Members.ToArray()){species.Add(bro.name.Replace("(Clone)",""));bro.GetComponent<Health>().Kill();}}
 Check(species.Count>=3,"All three assigned flock species actually spawn");Check(waveSeen.Count==3&&encounter.Completed,"All three production waves complete and persist the completion identity");
 await Wait(.5f);Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Flock completion unlocks Nailong");Check(!objectives.TryGetGuidanceTarget(out _,out var afterId)||afterId!=encounter.completionId,"Guide leaves cleared flock");
 await Wait(6);Check(encounter.Alive==0,"All flock members removed after death");
 }catch(Exception e){error=e.ToString();}finally{if(capture){capture.Save();UnityEngine.Object.Destroy(capture);}File.WriteAllText(".codex/freeze066/flock-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=error==null,error,checks},Newtonsoft.Json.Formatting.Indented));}}
}
[DefaultExecutionOrder(32000)] public sealed class FlockCapture066:MonoBehaviour
{
 public ChickenBroEncounter encounter;readonly List<object> rows=new List<object>();double last;
 void Update(){double now=Time.realtimeSinceStartupAsDouble;double ms=last==0?0:(now-last)*1000;last=now;rows.Add(new{frame=Time.frameCount,ms,seconds=now,alive=encounter?encounter.Alive:0,nav=encounter&&encounter.NavigationReady,nativeBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),managedBytes=GC.GetTotalMemory(false)});}
 public void Save(){File.WriteAllText(".codex/freeze066/flock-frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));}
}



