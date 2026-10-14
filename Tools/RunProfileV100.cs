using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using Unity.Profiling;
using UnityEngine;

[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV100:MonoBehaviour
{
    public string folder,label;
    DateTime startedUtc;
    readonly System.Collections.Generic.List<object> samples=new System.Collections.Generic.List<object>();
    double started,next;
    Story1WreckFinale film;
    void Awake(){started=Time.realtimeSinceStartupAsDouble;next=started;startedUtc=DateTime.UtcNow;}
    void LateUpdate()
    {
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if(battle!=null && battle.Finale!=null)film=battle.Finale;
        if(Time.realtimeSinceStartupAsDouble>=next)
        {
            next=Time.realtimeSinceStartupAsDouble+1;
            int cacheCount=0;
            if(film!=null)cacheCount=((System.Collections.IDictionary)typeof(Story1WreckFinale).GetField("plannedFeet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film)).Count;
            samples.Add(new{wall=Time.realtimeSinceStartupAsDouble-started,phase=battle!=null?battle.CurrentPhase.ToString():null,clock=film!=null?film.CinematicTime:-999,cacheCount,queries=film!=null?film.EntryPlanningQueries:0,progress=film!=null?film.EntryPlanningProgress:null,tailReplans=film!=null?film.TailRouteReplans:0,biteReplans=film!=null?film.BiteRouteReplans:0});
            File.WriteAllText(folder+"/entry-observer-"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));
        }
    }
    void OnDestroy()
    {
        string original=Path.Combine(Application.persistentDataPath,"opening-benchmark.json");
        if(File.Exists(original) && File.GetLastWriteTimeUtc(original)>=startedUtc)File.Copy(original,folder+"/native-profile-"+label+".json",true);
        Time.captureFramerate=0;
    }
}
public static class BenchmarkOpeningV100
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV100>();observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";observer.label=label;
        Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV100:MonoBehaviour
{
    public string output;
    readonly string[] names={"SolidContact","SolidSweep","SolidEscape","ClearSearch","RetainedPlacement","UpdateVolumes","SolidOverlap","SolvedDamage","ResidualPenetration","ShapeSnapshot"};
    ProfilerRecorder[] costs;
    readonly List<object> samples=new List<object>();
    bool enteredFilm;
    void Awake(){costs=names.Select(n=>ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck."+n)).ToArray();}
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();
        if(battle==null)return;
        var film=battle.Finale;var solid=battle.WreckContact;
        if(film!=null)enteredFilm=true;
        samples.Add(new{frame=Time.frameCount,clock=film!=null?film.CinematicTime:-999,
            phase=battle.CurrentPhase.ToString(),ms=costs.Select(c=>c.Valid?c.LastValue/1000000d:0).ToArray(),
            correction=solid!=null?solid.LastCorrection.magnitude:0,
            holds=solid!=null?solid.ConstraintHoldCount:0,escapes=solid!=null?solid.LargeEscapeCount:0});
        if(enteredFilm && battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        foreach(var cost in costs)if(cost.Valid)cost.Dispose();
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{readOnly=true,editorDiagnostic=true,names,samples},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObserveSolidV100
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV100>().output=output;return new{started=true,readOnly=true};
    }
}

[DefaultExecutionOrder(32800)]
public sealed class BoundedFacingObserverV100:MonoBehaviour
{
    public string output;
    readonly float[] cues={.9f,2.2f,7f,14.4f,17.8f};
    readonly List<object> rows=new List<object>();
    int next,tests,clear,blocked,mismatches;float maximumClearDifference;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || next>=cues.Length || film.CinematicTime<cues[next])return;
        var contact=battle.WreckContact;var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        var pose=contact.CaptureAnatomy();int before=tests,startMismatch=mismatches;
        foreach(bool tail in new[]{false,true})
        foreach(var offset in new[]{Vector3.zero,Vector3.down*2,Vector3.left*3,Vector3.forward*5})
        foreach(float yaw in new[]{0f,45,90,135,180,225,270,315})
        {
            Vector3 marker=(tail?shark.TailWorldPoint:shark.NoseWorldPoint)+offset;
            Quaternion rotation=Quaternion.Euler(-16,yaw,0);
            float full=contact.PoseObstruction(pose,marker,rotation,tail);
            float bounded=contact.PoseObstructionBounded(pose,marker,rotation,tail,.0004f);
            bool isClear=full<=.0004f;if(isClear)clear++;else blocked++;
            if(isClear)maximumClearDifference=Mathf.Max(maximumClearDifference,Mathf.Abs(full-bounded));
            if(isClear!=(bounded<=.0004f) || isClear && Mathf.Abs(full-bounded)>.000001f)mismatches++;
            tests++;
        }
        rows.Add(new{clock=film.CinematicTime,tests=tests-before,mismatches=mismatches-startMismatch});next++;
        if(next>=cues.Length)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{nativeGeometry=true,readOnly=true,performanceEvidence=false,
            poses=next,tests,clear,blocked,mismatches,maximumClearDifference,passed=next==cues.Length && mismatches==0 && clear>0 && blocked>0,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class VerifyBoundedFacingV100
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Native bounded endpoint classification v100");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<BoundedFacingObserverV100>().output=output;return new{started=true,readOnly=true};
    }
}

public static class RunProfileV100
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveSolidV100.Start(root+"solid-stages-v100.json");
        VerifyBoundedFacingV100.Start(root+"bounded-endpoints-v100.json");
        return BenchmarkOpeningV100.Start(true,20261014,30,"v100");
    }
}
