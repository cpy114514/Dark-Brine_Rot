using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using Unity.Profiling;
using UnityEngine;

[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV094:MonoBehaviour
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
public static class BenchmarkOpeningV094
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV094>();observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";observer.label=label;
        Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV094:MonoBehaviour
{
    public string output;
    readonly string[] names={"SolidContact","SolidSweep","SolidEscape","ClearSearch","RetainedPlacement","UpdateVolumes","SolidOverlap","SolvedDamage","ResidualPenetration"};
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
public static class ObserveSolidV094
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV094>().output=output;return new{started=true,readOnly=true};
    }
}

[DefaultExecutionOrder(32800)]
public sealed class CapsuleBoundsObserverV094:MonoBehaviour
{
    public string output;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly float[] cues={.9f,7f,9f,14.5f,18f};
    readonly List<object> rows=new List<object>();
    int next,pairs,rejected,hits,mismatches;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || next>=cues.Length || film.CinematicTime<cues[next])return;
        var contact=battle.WreckContact;var type=typeof(Story1SharkWreckContact);
        type.GetMethod("SnapshotPlanningShapes",flags).Invoke(contact,null);
        var volumes=(Collider[])type.GetField("volumes",flags).GetValue(contact);
        var parts=(IList)type.GetField("planningParts",flags).GetValue(contact);
        var queryType=type.GetNestedType("CapsuleBoundsQuery",BindingFlags.NonPublic);
        var constructor=queryType.GetConstructor(new[]{typeof(Collider),typeof(Vector3),typeof(Quaternion)});
        var test=queryType.GetMethod("MayTouch");
        int startPairs=pairs,startRejected=rejected,startHits=hits;
        foreach(var volume in volumes)
        foreach(var offset in new[]{Vector3.zero,Vector3.down*.5f,Vector3.up*.5f,Vector3.left*2,Vector3.right*2,Vector3.forward*5,Vector3.back*5})
        foreach(var angle in new[]{Quaternion.identity,Quaternion.Euler(30,45,0),Quaternion.Euler(-25,-70,0)})
        {
            Vector3 position=volume.transform.position+offset;Quaternion heading=angle*volume.transform.rotation;
            object query=constructor.Invoke(new object[]{volume,position,heading});
            // An enclosing sphere stays valid for every test rotation. It does
            // not share the proposed segment/AABB rejection implementation.
            float reach=volume.bounds.extents.magnitude+(volume.bounds.center-volume.transform.position).magnitude+.01f;
            foreach(object part in parts)
            {
                var partType=part.GetType();Bounds bounds=(Bounds)partType.GetField("bounds").GetValue(part);
                if(bounds.SqrDistance(position)>reach*reach)continue;
                var mesh=(MeshCollider)partType.GetField("collider").GetValue(part);
                var at=(Vector3)partType.GetField("position").GetValue(part);
                var rotation=(Quaternion)partType.GetField("rotation").GetValue(part);
                bool allowed=(bool)test.Invoke(query,new object[]{bounds});
                bool actual=Physics.ComputePenetration(volume,position,heading,mesh,at,rotation,out _,out float depth);
                pairs++;if(!allowed)rejected++;if(actual && depth>0)hits++;
                if(!allowed && actual && depth>0)
                {
                    mismatches++;rows.Add(new{clock=film.CinematicTime,volume=volume.name,mesh=mesh.name,depth,offset=offset.ToString(),heading=heading.eulerAngles.ToString()});
                }
            }
        }
        rows.Add(new{clock=film.CinematicTime,pairs=pairs-startPairs,rejected=rejected-startRejected,hits=hits-startHits});next++;
        if(next>=cues.Length)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{nativeGeometry=true,readOnly=true,performanceEvidence=false,
            poses=next,pairs,rejected,hits,mismatches,passed=next==cues.Length && mismatches==0 && rejected>0 && hits>0,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class VerifyCapsuleBoundsV094
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Native capsule bounds equivalence v094");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<CapsuleBoundsObserverV094>().output=output;return new{started=true,readOnly=true};
    }
}

public static class RunSolidRegressionV094
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveSolidV094.Start(root+"solid-stages-v094.json");
        VerifyCapsuleBoundsV094.Start(root+"capsule-bounds-v094.json");
        return BenchmarkOpeningV094.Start(true,20261014,30,"v094");
    }
}
