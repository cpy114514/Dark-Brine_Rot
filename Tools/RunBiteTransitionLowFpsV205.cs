using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using Unity.Profiling;
using UnityEngine;

[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV205:MonoBehaviour
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
public static class BenchmarkOpeningV205
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.naturalTrigger=true;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV205>();observer.benchmark=benchmark;observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014";observer.label=label;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV205>(FindObjectsSortMode.None))monitor.benchmark=benchmark; foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV205>(FindObjectsSortMode.None))monitor.benchmark=benchmark; var contacts=go.AddComponent<ContactRegistrationObserverV205>();contacts.benchmark=benchmark; Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV205:MonoBehaviour
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
public static class ObserveSolidV205
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV205>().output=output;return new{started=true,readOnly=true};
    }
}

public static class RunSolidProfileV205
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveSolidV205.Start(root+"solid-stages-v205.json"); ObservePoseMotionV205.Start(root+"pose-motion-v205.json");
        var result=BenchmarkOpeningV205.Start(false,20261015,10,"v205");
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        var fixture=new GameObject("Actor-only lowFPS supported route");UnityEngine.Object.DontDestroyOnLoad(fixture);fixture.AddComponent<SupportedRouteFixtureV205>().benchmark=benchmark;
        var ending=new GameObject("Read-only lowFPS ending");UnityEngine.Object.DontDestroyOnLoad(ending);var observer=ending.AddComponent<EndingObserverV205>();observer.benchmark=benchmark;observer.output=root+"ending-v205.json";
        TraceProbeV205.Attach();return result;
    }
}


[DefaultExecutionOrder(32800)]
public sealed class PoseMotionObserverV205:MonoBehaviour
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
public static class ObservePoseMotionV205
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only actual solved pose motion v205");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<PoseMotionObserverV205>().output=output;return new{started=true,readOnly=true};
    }
}

[DefaultExecutionOrder(32900)]
public sealed class ContactRegistrationObserverV205:MonoBehaviour
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
        File.WriteAllText(".codex/animation-continuation-20261014/rider-contact-registration-v205.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,rows},Newtonsoft.Json.Formatting.Indented));
    }
}

public static class AttachReviewDiagnosticsV205
{
    public static object Run()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Existing single review required");
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveSolidV205.Start(root+"solid-stages-v205.json");
        ObservePoseMotionV205.Start(root+"pose-motion-v205.json");
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV205>(FindObjectsSortMode.None))monitor.benchmark=benchmark;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV205>(FindObjectsSortMode.None))monitor.benchmark=benchmark;
        var observer=benchmark.gameObject.AddComponent<EntryObserverV205>();observer.benchmark=benchmark;observer.folder=root.TrimEnd('/');observer.label="v205";
        benchmark.gameObject.AddComponent<ContactRegistrationObserverV205>().benchmark=benchmark;
        return new{attached=true,readOnly=true,newBenchmark=false};
    }
}
// Explicit integration fixture: only the diagnostic actor is placed, once.
// No wood transform, velocity, gravity, collider, damage or timeline is edited.
[DefaultExecutionOrder(32980)]
public sealed class SupportedRouteFixtureV205:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    bool placed;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || placed || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<8)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var actor=sequence.sahur;
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        foreach(var board in battle.Boards.Where(p=>p!=null && p.IsBoardable && p.Dimensions.y<1.2f && Mathf.Abs(p.transform.up.y)>.9f && p.Speed<4).OrderByDescending(p=>p.Dimensions.x*p.Dimensions.z))
        {
            var body=board.GetComponent<Rigidbody>();Vector3 centre=body.worldCenterOfMass;
            for(int angle=0;angle<360;angle+=15)
            {
                Vector3 feet=board.BoardingPoint(centre+Quaternion.AngleAxis(angle,Vector3.up)*board.transform.forward*100);
                feet=board.BoardingPoint(feet-board.OpenWaterDirection(feet)*1.2f);
                Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
                bool clear=true;float reach=Mathf.Max(2,board.Dimensions.y+1);
                for(int bearing=0;bearing<360 && clear;bearing+=45)
                {
                    Vector3 sole=feet+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*1.1f;
                    clear=board.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) && Vector3.Dot(hit.normal,Vector3.up)>.5f;
                }
                if(!clear || ocean.SampleSurfaceHeight(feet,Time.time)-feet.y>.8f || Mathf.Abs(body.GetPointVelocity(feet).y)>3)continue;
                actor.RestoreSavedPose(feet+Vector3.up*(.04f-actor.LowestFootWorldOffset),Quaternion.LookRotation(board.OpenWaterDirection(feet)));
                var rider=actor.GetComponent<Story1WreckRider>();rider.Initialize(actor,battle.Boards,ocean,board,battle.WreckDetails);actor.NotifyPlatformLanding();
                bool stable=rider.HasStableSupport;
                if(!stable)continue;
                placed=true;bool requested=battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit);
                File.WriteAllText(".codex/animation-continuation-20261014/supported-route-fixture-v205.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{actorOnlyFixture=true,woodMoved=false,naturalHandoffAcceptance=false,performanceEvidence=false,stable,requested,board=board.name,feet=feet.ToString("F4"),fightTime=battle.FightElapsed},Newtonsoft.Json.Formatting.Indented));
                Destroy(this);return;
            }
        }
    }
}

// Observe this already-running original encounter, without replacing the scene.
[DefaultExecutionOrder(32700)]
public sealed class EndingObserverV205 : MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    Story1WreckFinale film;
    Story1WreckBattle battle;
    ThirdPersonPlayerController hero;
    OceanWorld ocean;
    double completedAt=-1,pausedAt=-1;
    float completedUnscaledTime;
    string exitReason;
    int heroCounters,sharkCounters;
    int frames,uprightFrames,pausedFrames;
    float endClock,maxClockDrift,minChest=float.PositiveInfinity,maxChest=float.NegativeInfinity,
        minimumHeadClearance=float.PositiveInfinity,maxRock,maxPausedMotion;
    Quaternion firstHeading,lastHeading;
    Vector3 lastChest;
    bool wasPaused,restored,lostStick;
    void LateUpdate()
    {
        var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        if(completedAt>=0 && (arrival!=null || Time.realtimeSinceStartupAsDouble-completedAt>12))
        {Finish(arrival!=null && !arrival.HasStick && !arrival.IsAwake);return;}
        if(film==null && completedAt<0)
        {
            battle=benchmark!=null ? benchmark.Battle:null;
            if(battle!=null)film=battle.Finale;
            if(film!=null)
            {
                hero=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
                ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            }
        }
        if(film==null || !film.IsComplete)return;
        var animator=hero.CharacterAnimator;
        Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position,
            hips=animator.GetBoneTransform(HumanBodyBones.Hips).position,
            chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Quaternion heading=hero.transform.rotation;
        if(completedAt<0){completedAt=Time.realtimeSinceStartupAsDouble;completedUnscaledTime=Time.unscaledTime;endClock=film.Elapsed;firstHeading=heading;}
        exitReason=film.ExitReason;heroCounters=film.HeroCounterCount;sharkCounters=film.SharkCounterCount;
        frames++;
        if(Mathf.Abs(Vector3.Dot((head-hips).normalized,Vector3.up))>.55f)uprightFrames++;
        maxClockDrift=Mathf.Max(maxClockDrift,Mathf.Abs(film.Elapsed-endClock));
        lostStick|=film.HasLostStick;
        if(Time.timeScale==0)
        {
            pausedFrames++;
            if(wasPaused)maxPausedMotion=Mathf.Max(maxPausedMotion,Mathf.Max(Vector3.Distance(chest,lastChest),Quaternion.Angle(heading,lastHeading)));
        }
        else
        {
            minChest=Mathf.Min(minChest,chest.y);maxChest=Mathf.Max(maxChest,chest.y);
            maxRock=Mathf.Max(maxRock,Quaternion.Angle(firstHeading,heading));
            minimumHeadClearance=Mathf.Min(minimumHeadClearance,head.y-ocean.SampleSurfaceHeight(head,Time.time));
        }
        wasPaused=Time.timeScale==0;lastChest=chest;lastHeading=heading;
        double since=Time.unscaledTime-completedUnscaledTime;
        if(pausedAt<0 && since>=1.5){pausedAt=Time.realtimeSinceStartupAsDouble;Time.timeScale=0;}
        if(!restored && pausedAt>=0 && Time.realtimeSinceStartupAsDouble-pausedAt>=.4){Time.timeScale=1;restored=true;}
    }
    void Finish(bool handoff)
    {
            var result=new{frames,uprightFrames,pausedFrames,maxClockDrift,lostStick,chestHeightRange=maxChest-minChest,maxRock,minimumHeadClearance,maxPausedMotion,unconsciousUnarmedIsland=handoff,exitReason,heroCounters,sharkCounters,
                posePassed=frames>=60 && uprightFrames==0 && maxClockDrift==0 && pausedFrames>=5 && maxPausedMotion<=.001f && lostStick,
                wavesPassed=maxChest-minChest>=.015f && maxRock>=.02f && minimumHeadClearance>=.025f,
                standalonePerformanceEvidence=false};
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);
    }
    void OnDestroy(){if(pausedAt>=0 && !restored)Time.timeScale=1;}
}
public static class ObserveEndingV205
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires actual original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<EndingObserverV205>()!=null)throw new Exception("Ending observer already running");
        var go=new GameObject("Observe existing finale ending v074");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EndingObserverV205>().output=output;
        return new{started=true,output,continuesExistingEncounter=true};
    }
}



public sealed class ProbeTraceStoreV205
{
    public OpeningBenchmark benchmark;
    public bool valid,done;
    public Vector3 rawRoot,rawNose,previousRoot,aim;
    public Quaternion rawHead,rawBody;
    public readonly List<object> rows=new List<object>();
    public static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    public static T Read<T>(object instance,string name)=>(T)instance.GetType().GetField(name,Flags).GetValue(instance);
    public void Finish(GameObject holder)
    {
        if(done)return;done=true;
        File.WriteAllText(".codex/animation-continuation-20261014/probe-trace-v205.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            readOnly=true,actorOrWoodMoved=false,sampledBeforeAndAfterNativeSolver=true,rows,performanceEvidence=false},Newtonsoft.Json.Formatting.Indented));
        UnityEngine.Object.Destroy(holder);
    }
}
[DefaultExecutionOrder(1190)]
public sealed class ProbeTraceBeforeV205:MonoBehaviour
{
    public ProbeTraceStoreV205 store;
    void LateUpdate()
    {
        var battle=store.benchmark!=null?store.benchmark.Battle:null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0 || store.done)return;
        if(film.CinematicTime>1.8f){store.Finish(gameObject);return;}
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        store.rawRoot=shark.transform.position;store.rawNose=shark.NoseWorldPoint;
        store.previousRoot=ProbeTraceStoreV205.Read<Vector3>(battle.WreckContact,"previousRoot");
        store.aim=ProbeTraceStoreV205.Read<Vector3>(film,"attackAim");
        store.rawHead=ProbeTraceStoreV205.Read<Transform>(battle.WreckContact,"head").localRotation;
        store.rawBody=ProbeTraceStoreV205.Read<Transform>(battle.WreckContact,"body").localRotation;
        store.valid=true;
    }
}
[DefaultExecutionOrder(33110)]
public sealed class ProbeTraceAfterV205:MonoBehaviour
{
    public ProbeTraceStoreV205 store;
    void LateUpdate()
    {
        if(!store.valid || store.done)return;store.valid=false;
        var battle=store.benchmark!=null?store.benchmark.Battle:null;var film=battle!=null?battle.Finale:null;if(film==null)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
        var contact=battle.WreckContact;var head=ProbeTraceStoreV205.Read<Transform>(contact,"head");var body=ProbeTraceStoreV205.Read<Transform>(contact,"body");
        var board=ProbeTraceStoreV205.Read<Story1WreckPlank>(film,"board");
        store.rows.Add(new{clock=film.CinematicTime,dt=Time.deltaTime,
            rawRoot=ProbeTraceStoreV205.V(store.rawRoot),root=ProbeTraceStoreV205.V(shark.transform.position),
            rawNose=ProbeTraceStoreV205.V(store.rawNose),nose=ProbeTraceStoreV205.V(shark.NoseWorldPoint),
            previousRoot=ProbeTraceStoreV205.V(store.previousRoot),aim=ProbeTraceStoreV205.V(store.aim),
            rootCorrection=Vector3.Distance(store.rawRoot,shark.transform.position),
            noseCorrection=Vector3.Distance(store.rawNose,shark.NoseWorldPoint),
            requestedTravel=Vector3.Distance(store.rawRoot,store.previousRoot),actualTravel=Vector3.Distance(shark.transform.position,store.previousRoot),
            headPoseCorrectionDegrees=Quaternion.Angle(store.rawHead,head.localRotation),bodyPoseCorrectionDegrees=Quaternion.Angle(store.rawBody,body.localRotation),
            board=board!=null?board.name:null,actorRoot=ProbeTraceStoreV205.V(sequence.sahur.transform.position),
            route=ProbeTraceStoreV205.Read<float>(film,"attackRouteCost"),
            contact.ConstraintHoldCount,contact.PartialRootAdvanceCount,contact.LargeEscapeCount,
            obstacles=contact.LastObstacles.ToArray(),gaps=(float[])film.ContactAttemptGaps.Clone()});
    }
}
public static class TraceProbeV205
{
    public static object Attach()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Single existing native run required");
        var holder=new GameObject("Read-only first probe before and after solving");UnityEngine.Object.DontDestroyOnLoad(holder);
        var store=new ProbeTraceStoreV205{benchmark=benchmark};
        holder.AddComponent<ProbeTraceBeforeV205>().store=store;holder.AddComponent<ProbeTraceAfterV205>().store=store;
        return new{attached=true,readOnly=true};
    }
}