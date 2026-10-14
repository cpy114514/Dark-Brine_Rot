using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProbeTraceStoreV200
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
        File.WriteAllText(".codex/animation-continuation-20261014/probe-trace-v200.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            readOnly=true,actorOrWoodMoved=false,sampledBeforeAndAfterNativeSolver=true,rows,performanceEvidence=false},Newtonsoft.Json.Formatting.Indented));
        UnityEngine.Object.Destroy(holder);
    }
}
[DefaultExecutionOrder(1190)]
public sealed class ProbeTraceBeforeV200:MonoBehaviour
{
    public ProbeTraceStoreV200 store;
    void LateUpdate()
    {
        var battle=store.benchmark!=null?store.benchmark.Battle:null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0 || store.done)return;
        if(film.CinematicTime>1.8f){store.Finish(gameObject);return;}
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        store.rawRoot=shark.transform.position;store.rawNose=shark.NoseWorldPoint;
        store.previousRoot=ProbeTraceStoreV200.Read<Vector3>(battle.WreckContact,"previousRoot");
        store.aim=ProbeTraceStoreV200.Read<Vector3>(film,"attackAim");
        store.rawHead=ProbeTraceStoreV200.Read<Transform>(battle.WreckContact,"head").localRotation;
        store.rawBody=ProbeTraceStoreV200.Read<Transform>(battle.WreckContact,"body").localRotation;
        store.valid=true;
    }
}
[DefaultExecutionOrder(33110)]
public sealed class ProbeTraceAfterV200:MonoBehaviour
{
    public ProbeTraceStoreV200 store;
    void LateUpdate()
    {
        if(!store.valid || store.done)return;store.valid=false;
        var battle=store.benchmark!=null?store.benchmark.Battle:null;var film=battle!=null?battle.Finale:null;if(film==null)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
        var contact=battle.WreckContact;var head=ProbeTraceStoreV200.Read<Transform>(contact,"head");var body=ProbeTraceStoreV200.Read<Transform>(contact,"body");
        var board=ProbeTraceStoreV200.Read<Story1WreckPlank>(film,"board");
        store.rows.Add(new{clock=film.CinematicTime,dt=Time.deltaTime,
            rawRoot=ProbeTraceStoreV200.V(store.rawRoot),root=ProbeTraceStoreV200.V(shark.transform.position),
            rawNose=ProbeTraceStoreV200.V(store.rawNose),nose=ProbeTraceStoreV200.V(shark.NoseWorldPoint),
            previousRoot=ProbeTraceStoreV200.V(store.previousRoot),aim=ProbeTraceStoreV200.V(store.aim),
            rootCorrection=Vector3.Distance(store.rawRoot,shark.transform.position),
            noseCorrection=Vector3.Distance(store.rawNose,shark.NoseWorldPoint),
            requestedTravel=Vector3.Distance(store.rawRoot,store.previousRoot),actualTravel=Vector3.Distance(shark.transform.position,store.previousRoot),
            headPoseCorrectionDegrees=Quaternion.Angle(store.rawHead,head.localRotation),bodyPoseCorrectionDegrees=Quaternion.Angle(store.rawBody,body.localRotation),
            board=board!=null?board.name:null,actorRoot=ProbeTraceStoreV200.V(sequence.sahur.transform.position),
            route=ProbeTraceStoreV200.Read<float>(film,"attackRouteCost"),
            contact.ConstraintHoldCount,contact.PartialRootAdvanceCount,contact.LargeEscapeCount,
            obstacles=contact.LastObstacles.ToArray(),gaps=(float[])film.ContactAttemptGaps.Clone()});
    }
}
public static class TraceProbeV200
{
    public static object Attach()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Single existing native run required");
        var holder=new GameObject("Read-only first probe before and after solving");UnityEngine.Object.DontDestroyOnLoad(holder);
        var store=new ProbeTraceStoreV200{benchmark=benchmark};
        holder.AddComponent<ProbeTraceBeforeV200>().store=store;holder.AddComponent<ProbeTraceAfterV200>().store=store;
        return new{attached=true,readOnly=true};
    }
}
