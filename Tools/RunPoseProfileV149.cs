using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using Unity.Profiling;
using UnityEngine;

[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV149:MonoBehaviour
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
public static class BenchmarkOpeningV149
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.naturalTrigger=true;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV149>();observer.benchmark=benchmark;observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";observer.label=label;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV149>(FindObjectsSortMode.None))monitor.benchmark=benchmark; foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV149>(FindObjectsSortMode.None))monitor.benchmark=benchmark; var contacts=go.AddComponent<ContactRegistrationObserverV149>();contacts.benchmark=benchmark; Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV149:MonoBehaviour
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
public static class ObserveSolidV149
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV149>().output=output;return new{started=true,readOnly=true};
    }
}

public static class RunSolidProfileV149
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveSolidV149.Start(root+"solid-stages-v149.json"); ObservePoseMotionV149.Start(root+"pose-motion-v149.json");
        return BenchmarkOpeningV149.Start(true,20261015,30,"v149");
    }
}


[DefaultExecutionOrder(32800)]
public sealed class PoseMotionObserverV149:MonoBehaviour
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
            solid.PoseAdvanceCount,solid.PartialPoseAdvanceCount,solid.ConstraintHoldCount,solid.LargeEscapeCount});
        last=at;heading=shark.transform.rotation;sampled=true;frames++;
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,
            frames,maximumPenetration,maximumTravel,maximumOverrun,travelOverruns,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObservePoseMotionV149
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only actual solved pose motion v149");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<PoseMotionObserverV149>().output=output;return new{started=true,readOnly=true};
    }
}

[DefaultExecutionOrder(32900)]
public sealed class ContactRegistrationObserverV149:MonoBehaviour
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
        File.WriteAllText(".codex/opening-optimization-20261012/rider-contact-registration-v149.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
