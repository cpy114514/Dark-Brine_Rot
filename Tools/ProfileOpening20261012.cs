using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProfileOpening20261012
{
    static Task pending;
    public static object Start(string label="before")
    {
        if (!Application.isPlaying) throw new Exception("Play Mode required");
        if (pending!=null && !pending.IsCompleted) throw new Exception("Profile already running");
        pending=Run(label);
        return new {started=true,label};
    }
    static async Task Run(string label)
    {
        string directory=Path.GetFullPath(".codex/opening-optimization-20261012");
        Directory.CreateDirectory(directory);
        OpeningFrameProbe probe=null;
        int cap=Application.targetFrameRate,sync=QualitySettings.vSyncCount;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(20);
            await Task.Delay(350);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();
            sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            probe=battle.gameObject.AddComponent<OpeningFrameProbe>();probe.battle=battle;probe.Begin();
            await Task.Delay(4000);
            sequence.BeginImpact();
            float until=Time.realtimeSinceStartup+35;
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<until)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("Break did not finish");
            await Task.Delay(4000);
            typeof(Story1WreckBattle).GetMethod("BeginFinale",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,null);
            until=Time.realtimeSinceStartup+85;
            while(battle.CurrentPhase==Story1WreckBattle.Phase.Finale && Time.realtimeSinceStartup<until)await Task.Delay(50);
            File.WriteAllText(Path.Combine(directory,"profile-"+label+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {
                label,editor=true,width=Screen.width,height=Screen.height,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                hardware=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,phase=battle.CurrentPhase.ToString(),
                time=battle.Finale!=null?battle.Finale.CinematicTime:-1,frames=probe.Report()
            },Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(directory,"profile-"+label+"-error.txt"),ex.ToString());}
        finally{if(probe!=null)UnityEngine.Object.Destroy(probe);Application.targetFrameRate=cap;QualitySettings.vSyncCount=sync;}
    }
}
public sealed class OpeningFrameProbe:MonoBehaviour
{
    public Story1WreckBattle battle;
    struct Frame {public string phase;public float ms;public long script,physics,allocation,main,late,entry,facing,obstruction;}
    readonly List<Frame> frames=new List<Frame>(10000);
    ProfilerRecorder script,physics,allocation,main;
    ProfilerRecorder late,entry,facing,obstruction;
    public void Begin(){script=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"BehaviourUpdate");physics=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics.Simulate");allocation=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");late=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"PreLateUpdate.ScriptRunBehaviourLateUpdate");entry=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.EntryPlanning");facing=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.AttackFacing");obstruction=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.PoseObstruction");}
    void Update(){frames.Add(new Frame{phase=battle.CurrentPhase.ToString(),ms=Time.unscaledDeltaTime*1000,script=script.Valid?script.LastValue:0,physics=physics.Valid?physics.LastValue:0,allocation=allocation.Valid?allocation.LastValue:0,main=main.Valid?main.LastValue:0,late=late.Valid?late.LastValue:0,entry=entry.Valid?entry.LastValue:0,facing=facing.Valid?facing.LastValue:0,obstruction=obstruction.Valid?obstruction.LastValue:0});}
    public object Report()=>frames.GroupBy(f=>f.phase).Select(g=>{
        var a=g.OrderBy(f=>f.ms).ToArray();int n=a.Length;
        return new{phase=g.Key,n,meanMs=a.Average(f=>f.ms),p99Ms=a[(int)((n-1)*.99f)].ms,maxMs=a[n-1].ms,spikes50ms=a.Count(f=>f.ms>50),mainMeanMs=a.Average(f=>f.main)*1e-6,scriptMeanMs=a.Average(f=>f.script)*1e-6,physicsMeanMs=a.Average(f=>f.physics)*1e-6,gcMeanBytes=a.Average(f=>f.allocation),lateMeanMs=a.Average(f=>f.late)*1e-6,entryMaxMs=a.Max(f=>f.entry)*1e-6,facingMaxMs=a.Max(f=>f.facing)*1e-6,obstructionMeanMs=a.Average(f=>f.obstruction)*1e-6};
    }).ToArray();
    void OnDestroy(){script.Dispose();physics.Dispose();allocation.Dispose();main.Dispose();late.Dispose();entry.Dispose();facing.Dispose();obstruction.Dispose();}
}
