using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class VerifyPlanningGeometryV176
{
    public static object Start()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Own benchmark required");
        var go=new GameObject("Native exact planning geometry v176");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<PlanningGeometryObserverV176>().benchmark=benchmark;return new{started=true,actorsMoved=false,woodMoved=false};
    }
}
[DefaultExecutionOrder(32960)]
public sealed class PlanningGeometryObserverV176:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    int cases,comparisons,penetrating,mismatches,cachedCases;float maximumDifference;
    double mutableMs,cachedMs;
    readonly List<object> rows=new List<object>();
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        var contact=battle.WreckContact;var type=contact.GetType();
        var volumes=(CapsuleCollider[])type.GetField("planningVolumes",Private).GetValue(contact);
        var get=(Func<int,float,float,CapsuleCollider>)Delegate.CreateDelegate(typeof(Func<int,float,float,CapsuleCollider>),contact,type.GetMethod("PlanningCapsule",Private));
        var cost=(Func<Collider,Vector3,Quaternion,Bounds,float,float>)Delegate.CreateDelegate(typeof(Func<Collider,Vector3,Quaternion,Bounds,float,float>),contact,type.GetMethod("PlanningCostBounded",Private));
        var maximum=(Func<Collider,Vector3,Quaternion,Bounds,float,float>)Delegate.CreateDelegate(typeof(Func<Collider,Vector3,Quaternion,Bounds,float,float>),contact,type.GetMethod("PlanningMaximum",Private));
        var dimensions=new Vector2[volumes.Length];for(int i=0;i<volumes.Length;i++)dimensions[i]=new Vector2(volumes[i].radius,volumes[i].height);
        try
        {
            for(int sample=0;sample<4 && cases<320;sample++,cases++)
            {
                int index=cases%4;var original=volumes[index];
                float radius=dimensions[index].x,height=Mathf.Max(2*radius,dimensions[index].y*(.95f+.005f*((cases/4)%20)));
                original.radius=radius;original.height=height;var cached=get(index,radius,height);
                if(cached!=original)cachedCases++;
                var part=battle.Boards[cases%battle.Boards.Count];var box=part.Collision.WorldBounds;
                float angle=cases*.7f;
                Vector3 at=box.center+new Vector3(Mathf.Sin(angle)*2,(cases%5-2)*.3f,Mathf.Cos(angle)*2);
                Quaternion facing=Quaternion.Euler(cases%7*9,cases%18*20,0);
                Vector3 half=facing*Vector3.forward*Mathf.Max(0,height*.5f-radius);
                var bounds=new Bounds(at,2*(new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z))+Vector3.one*radius));
                float exactCost=cost(original,at,facing,bounds,float.PositiveInfinity),exactMaximum=maximum(original,at,facing,bounds,float.PositiveInfinity);
                if(exactMaximum>0)penetrating++;
                foreach(float limit in new[]{0f,.0004f,.003f,.1f,float.PositiveInfinity})
                {
                    float c=cost(cached,at,facing,bounds,limit),m=maximum(cached,at,facing,bounds,limit);comparisons+=2;
                    if((c>limit)!=(exactCost>limit) || (m>limit)!=(exactMaximum>limit))mismatches++;
                    if(c<=limit){float difference=Mathf.Abs(c-exactCost);maximumDifference=Mathf.Max(maximumDifference,difference);if(difference>1e-6f)mismatches++;}
                    if(m<=limit){float difference=Mathf.Abs(m-exactMaximum);maximumDifference=Mathf.Max(maximumDifference,difference);if(difference>1e-6f)mismatches++;}
                }
                if(cached!=original)
                {
                    var timer=Stopwatch.StartNew();
                    for(int i=0;i<16;i++){original.height=height+.01f;original.height=height;cost(original,at,facing,bounds,float.PositiveInfinity);}
                    mutableMs+=timer.Elapsed.TotalMilliseconds;timer.Restart();
                    for(int i=0;i<16;i++){var reused=get(index,radius,height);cost(reused,at,facing,bounds,float.PositiveInfinity);}
                    cachedMs+=timer.Elapsed.TotalMilliseconds;
                }
                rows.Add(new{cases,cached=cached!=original,exactMaximum,exactCost});
            }
        }
        finally{for(int i=0;i<volumes.Length;i++){volumes[i].radius=dimensions[i].x;volumes[i].height=dimensions[i].y;}}
        if(cases>=320)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/planning-geometry-v176.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalReleasedMeshes=true,actorsMoved=false,woodMoved=false,performanceEvidence="Editor microbenchmark only; no game FPS claim",
                cases,comparisons,cachedCases,penetrating,mismatches,maximumDifference,mutableMs,cachedMs,
                contact.PlanningGeometryCreated,contact.PlanningGeometryReuses,passed=cachedCases>0 && penetrating>0 && mismatches==0,rows},Newtonsoft.Json.Formatting.Indented));Destroy(gameObject);
        }
    }
}
