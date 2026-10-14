using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using Unity.Profiling;
using UnityEngine;

[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV200:MonoBehaviour
{
    public string folder,label; public OpeningBenchmark benchmark;
    DateTime startedUtc;
    readonly System.Collections.Generic.List<object> samples=new System.Collections.Generic.List<object>();
    double started,next;
    Story1WreckFinale film;
    void Awake(){started=Time.realtimeSinceStartupAsDouble;next=started;startedUtc=DateTime.UtcNow;}
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle : null;
        if(battle!=null && battle.Finale!=null)film=battle.Finale;
        if(Time.realtimeSinceStartupAsDouble>=next)
        {
            next=Time.realtimeSinceStartupAsDouble+1;
            int cacheCount=0;
            if(film!=null)cacheCount=((System.Collections.IDictionary)typeof(Story1WreckFinale).GetField("plannedFeet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film)).Count;
            samples.Add(new{wall=Time.realtimeSinceStartupAsDouble-started,phase=battle!=null?battle.CurrentPhase.ToString():null,clock=film!=null?film.CinematicTime:-999,cacheCount,queries=film!=null?film.EntryPlanningQueries:0,progress=film!=null?film.EntryPlanningProgress:null,entryEndpointFacing=film!=null && film.EntryEndpointFacingUsed,entryFacingQueries=film!=null?film.EntryFacingQueries:0,tailReplans=film!=null?film.TailRouteReplans:0,biteReplans=film!=null?film.BiteRouteReplans:0});
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
public static class BenchmarkOpeningV200
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.naturalTrigger=true;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV200>();observer.benchmark=benchmark;observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014";observer.label=label;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV200>(FindObjectsSortMode.None))monitor.benchmark=benchmark; foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV200>(FindObjectsSortMode.None))monitor.benchmark=benchmark; var contacts=go.AddComponent<ContactRegistrationObserverV200>();contacts.benchmark=benchmark; Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV200:MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    readonly string[] names={"SolidContact","SolidSweep","SolidEscape","ClearSearch","RetainedPlacement","UpdateVolumes","SolidOverlap","SolvedDamage","ResidualPenetration","ShapeSnapshot","AdaptivePose"};
    ProfilerRecorder[] costs;
    readonly List<object> samples=new List<object>();
    bool enteredFilm;
    void Awake(){costs=names.Select(n=>ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck."+n)).ToArray();}
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle : null;
        if(battle==null)return;
        var film=battle.Finale;var solid=battle.WreckContact;
        if(film!=null)enteredFilm=true;
        samples.Add(new{frame=Time.frameCount,clock=film!=null?film.CinematicTime:-999,
            phase=battle.CurrentPhase.ToString(),ms=costs.Select(c=>c.Valid?c.LastValue/1000000d:0).ToArray(),
            budgetYields=film!=null?film.FacingBudgetYields:0,recenters=solid!=null?solid.CurrentPoseRecenterCount:0,partialRoots=solid!=null?solid.PartialRootAdvanceCount:0, geometryCreated=solid!=null?solid.PlanningGeometryCreated:0, geometryReuses=solid!=null?solid.PlanningGeometryReuses:0, correction=solid!=null?solid.LastCorrection.magnitude:0,
            holds=solid!=null?solid.ConstraintHoldCount:0,escapes=solid!=null?solid.LargeEscapeCount:0});
        if(enteredFilm && battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        foreach(var cost in costs)if(cost.Valid)cost.Dispose();
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{readOnly=true,editorDiagnostic=true,names,samples},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObserveSolidV200
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV200>().output=output;return new{started=true,readOnly=true};
    }
}

public static class RunSolidProfileV200
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveSolidV200.Start(root+"solid-stages-v200.json"); ObservePoseMotionV200.Start(root+"pose-motion-v200.json");
        return BenchmarkOpeningV200.Start(true,20261016,30,"v200");
    }
}


[DefaultExecutionOrder(32800)]
public sealed class PoseMotionObserverV200:MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    readonly List<object> rows=new List<object>();
    bool sampled;Vector3 last;Quaternion heading;
    int frames,travelOverruns;float maximumPenetration,maximumTravel,maximumOverrun;
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle : null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0)return;
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        var solid=battle.WreckContact;Vector3 at=shark.transform.position;
        float penetration=solid.CurrentPenetration(),distance=sampled?Vector3.Distance(last,at):0;
        float overrun=Mathf.Max(0,distance-(24*Mathf.Max(.01f,Time.deltaTime)+.30f));
        if(sampled && overrun>.01f)travelOverruns++;
        maximumPenetration=Mathf.Max(maximumPenetration,penetration);maximumTravel=Mathf.Max(maximumTravel,distance);maximumOverrun=Mathf.Max(maximumOverrun,overrun);
        rows.Add(new{clock=film.CinematicTime,penetration,distance,overrun,headingStep=sampled?Quaternion.Angle(heading,shark.transform.rotation):0,
            solid.PoseAdvanceCount,solid.PartialPoseAdvanceCount,solid.ConstraintHoldCount,solid.LargeEscapeCount,solid.PartialRootAdvanceCount});
        last=at;heading=shark.transform.rotation;sampled=true;frames++;
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,
            frames,maximumPenetration,maximumTravel,maximumOverrun,travelOverruns,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObservePoseMotionV200
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only actual solved pose motion v200");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<PoseMotionObserverV200>().output=output;return new{started=true,readOnly=true};
    }
}

[DefaultExecutionOrder(32900)]
public sealed class ContactRegistrationObserverV200:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    readonly List<object> rows=new List<object>();
    readonly HashSet<string> phases=new HashSet<string>();
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle:null;if(battle==null)return;
        string phase=battle.CurrentPhase.ToString();
        if(phase!="Waiting" && phase!="Fighting")return;
        if(phases.Contains(phase))return;phases.Add(phase);
        var actor=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
        var controller=actor.GetComponent<CharacterController>();
        var bodies=actor.GetComponentsInChildren<Collider>(true).Where(c=>c!=controller && !c.isTrigger).ToArray();
        var root=(GameObject)typeof(Story1WreckBattle).GetField("debrisRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
        int tested=0,ignored=0,activeShapes=0,shapes=0;var failures=new List<string>();
        foreach(var shape in root.GetComponentsInChildren<Story1WreckMeshCollision>(true))
        {
            if(shape.Parts==null)continue;
            bool active=shape.gameObject.activeInHierarchy;if(phase=="Fighting" && !active)continue;
            shapes++;if(active)activeShapes++;
            foreach(var part in shape.Parts)foreach(var body in bodies)
            {
                tested++;
                if(Physics.GetIgnoreCollision(part,body))ignored++;
                else if(failures.Count<12)failures.Add(shape.name+" / "+part.name+" / "+body.name);
            }
        }
        rows.Add(new{phase,shapes,activeShapes,bodies=bodies.Length,tested,ignored,
            cacheShapes=actor.GetComponent<Story1WreckRider>().RegisteredContactShapeCount,passed=tested>0 && tested==ignored,failures});
        File.WriteAllText(".codex/animation-continuation-20261014/rider-contact-registration-v200.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,rows},Newtonsoft.Json.Formatting.Indented));
    }
}

public static class AttachReviewDiagnosticsV200
{
    public static object Run()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Existing single review required");
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveSolidV200.Start(root+"solid-stages-v200.json");
        ObservePoseMotionV200.Start(root+"pose-motion-v200.json");
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV200>(FindObjectsSortMode.None))monitor.benchmark=benchmark;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV200>(FindObjectsSortMode.None))monitor.benchmark=benchmark;
        var observer=benchmark.gameObject.AddComponent<EntryObserverV200>();observer.benchmark=benchmark;observer.folder=root.TrimEnd('/');observer.label="v200";
        benchmark.gameObject.AddComponent<ContactRegistrationObserverV200>().benchmark=benchmark;
        return new{attached=true,readOnly=true,newBenchmark=false};
    }
}