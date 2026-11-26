using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class VerifyFiniteEscapeV164
{
    public static object Start()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Own native benchmark must already be running");
        var holder=new GameObject("Hypothetical finite escape coverage v164");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<FiniteEscapeObserverV164>().benchmark=benchmark;
        return new{started=true,queryVolumesOnly=true,actorsMoved=false,woodMoved=false,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32900)]
public sealed class FiniteEscapeObserverV164:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Static;
    delegate bool Search(float radius,out Vector3 result);
    int poses,oldMissNewClear,mismatches,accepted;float maximumExcess;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        var contact=battle.WreckContact;Type type=contact.GetType();
        type.GetMethod("SnapshotPlanningShapes",Private).Invoke(contact,null);
        var volumes=(Collider[])type.GetField("volumes",Private).GetValue(contact);
        var bounds=(Dictionary<Collider,Bounds>)type.GetField("anatomyBounds",Private).GetValue(contact);
        var directions=(Vector3[])type.GetField("escapeDirections",Private).GetValue(null);
        var distances=(float[])type.GetField("escapeDistances",Private).GetValue(null);
        var clear=(Func<Vector3,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,bool>),contact,type.GetMethod("ClearAt",Private));
        var search=(Search)Delegate.CreateDelegate(typeof(Search),contact,type.GetMethod("NearestClear",Private));
        bool OldSearch(float maximum,out Vector3 found)
        {
            found=Vector3.zero;if(clear(found))return true;
            foreach(float distance in distances){if(distance>maximum)break;foreach(var direction in directions){var at=direction*distance;if(clear(at)){found=at;return true;}}}
            return false;
        }
        var positions=new Vector3[volumes.Length];var originalBounds=new Bounds[volumes.Length];
        for(int i=0;i<volumes.Length;i++){positions[i]=volumes[i].transform.position;originalBounds[i]=bounds[volumes[i]];}
        try
        {
            var board=battle.Boards[poses%battle.Boards.Count];
            Vector3 shift=board.Collision.WorldBounds.center-positions[poses%volumes.Length];
            for(int i=0;i<volumes.Length;i++){volumes[i].transform.position=positions[i]+shift;var box=originalBounds[i];box.center+=shift;bounds[volumes[i]]=box;}
            if(!clear(Vector3.zero) && OldSearch(float.PositiveInfinity,out var firstClear) && firstClear.magnitude>.0002f)
            {
                // Locate a real clearance between the old fixed radii.
                // The actual query never accepts this bisection result;
                // it must prove clearance independently at its allowed radius.
                Vector3 direction=firstClear.normalized;float low=0,high=firstClear.magnitude;
                for(int i=0;i<12;i++){float mid=(low+high)*.5f;if(clear(direction*mid))high=mid;else low=mid;}
                float radius=(high+firstClear.magnitude)*.5f;
                bool old=OldSearch(radius,out var previous),current=search(radius,out var result);
                if(old && (!current || Vector3.Distance(previous,result)>1e-5f))mismatches++;
                if(current)
                {
                    accepted++;float excess=result.magnitude-radius;maximumExcess=Mathf.Max(maximumExcess,excess);
                    if(!clear(result) || excess>1e-5f)mismatches++;
                    if(!old)oldMissNewClear++;
                }
            }
        }
        finally
        {
            for(int i=0;i<volumes.Length;i++){volumes[i].transform.position=positions[i];bounds[volumes[i]]=originalBounds[i];}
        }
        if(++poses>=24)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/finite-escape-v164.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalReleasedMeshes=true,queryVolumesOnly=true,actorsMoved=false,woodMoved=false,performanceEvidence=false,
                poses,accepted,oldMissNewClear,mismatches,maximumExcess,contact.BoundaryClearCount,contact.RetainedFailureReuses,
                passed=oldMissNewClear>0 && mismatches==0 && maximumExcess<=1e-5f},Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);
        }
    }
}
