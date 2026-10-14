if(!UnityEngine.Application.isPlaying)throw new System.Exception("Requires Play Mode");
string root="ProjectRepairBackups/WreckPerformance-20261012/";System.IO.Directory.CreateDirectory(root);
string label=UnityEditor.SessionState.GetString("WreckPerf.Label","before");
System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Story1SharkWreckContact).TypeHandle);
var names=new[]{("Main Thread",Unity.Profiling.ProfilerCategory.Internal),("PlayerLoop",Unity.Profiling.ProfilerCategory.Scripts),("BehaviourUpdate",Unity.Profiling.ProfilerCategory.Scripts),("LateBehaviourUpdate",Unity.Profiling.ProfilerCategory.Scripts),("Physics.Simulate",Unity.Profiling.ProfilerCategory.Physics),("Physics.FetchResults",Unity.Profiling.ProfilerCategory.Physics),("Physics.SyncTransforms",Unity.Profiling.ProfilerCategory.Physics),("Story.WreckBuoyancy",Unity.Profiling.ProfilerCategory.Scripts),("Story.WreckContact",Unity.Profiling.ProfilerCategory.Scripts),("GC Allocated In Frame",Unity.Profiling.ProfilerCategory.Memory)};
var recorders=names.Select(n=>Unity.Profiling.ProfilerRecorder.StartNew(n.Item2,n.Item1,1)).ToArray();
var rows=new System.Collections.Generic.List<object>();int lastFrame=-1,fightingFrames=0;float started=UnityEngine.Time.realtimeSinceStartup;bool ended=false;
UnityEditor.EditorApplication.CallbackFunction capture=null;
capture=()=>
{
    if(ended)return;
    try
    {
        if(!UnityEngine.Application.isPlaying)throw new System.Exception("Play ended");
        int frame=UnityEngine.Time.frameCount;if(frame==lastFrame)return;lastFrame=frame;
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if(battle && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting)fightingFrames++;
        if(battle && battle.CurrentPhase!=Story1WreckBattle.Phase.Waiting)
            rows.Add(new{frame,phase=battle.CurrentPhase.ToString(),dtMs=UnityEngine.Time.unscaledDeltaTime*1000,elapsed=battle.FightElapsed,boards=battle.Boards.Count,pieces=battle.WreckDetails.Count,cpu=names.Select((n,i)=>new{name=n.Item1,value=recorders[i].LastValue,valid=recorders[i].Valid}).ToArray()});
        if(fightingFrames>=240 || rows.Count>=1600 || UnityEngine.Time.realtimeSinceStartup-started>65 || (rows.Count>10 && !battle))
        {
            ended=true;System.IO.File.WriteAllText(root+label+"-frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));
            UnityEditor.EditorApplication.update-=capture;foreach(var r in recorders)r.Dispose();UnityEditor.SessionState.SetBool("WreckPerf.Complete",true);
        }
    }
    catch(System.Exception error){ended=true;System.IO.File.WriteAllText(root+label+"-error.txt",error.ToString());UnityEditor.EditorApplication.update-=capture;foreach(var r in recorders)r.Dispose();UnityEditor.SessionState.SetBool("WreckPerf.Complete",true);}
};
UnityEditor.SessionState.SetBool("WreckPerf.Complete",false);UnityEditor.EditorApplication.update+=capture;
var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
if(!sequence)throw new System.Exception("Requires Story1");
UnityEngine.Application.runInBackground=true;
sequence.sahur.GetComponent<Mavis.PlayerHealth>().GrantProtection(120);
sequence.BeginImpact();UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
return "Recording wreck encounter frames asynchronously: "+label;
