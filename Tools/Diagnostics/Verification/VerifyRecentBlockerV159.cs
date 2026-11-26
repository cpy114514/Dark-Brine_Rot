using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class VerifyRecentBlockerV159
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");
        var holder=new GameObject("Native original-part rejection equivalence v159");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.waterEntry=true;benchmark.captureScreenshots=false;benchmark.scatterSeed=20261015;
        holder.AddComponent<RecentBlockerObserverV159>().benchmark=benchmark;Time.captureFramerate=30;
        return new{started=true,readOnlyHypotheticalQueries=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32900)]
public sealed class RecentBlockerObserverV159:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly System.Random random=new System.Random(159);
    readonly float[] limits={0,.0004f,.003f,.1f,float.PositiveInfinity};
    int poses,comparisons,penetrating,mismatches;float largestAcceptedDifference;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<2)return;
        var contact=battle.WreckContact;var type=contact.GetType();
        type.GetMethod("SnapshotPlanningShapes",Private).Invoke(contact,null);
        var volumes=(Collider[])type.GetField("volumes",Private).GetValue(contact);
        var maxOriginal=type.GetMethod("PlanningMaximumOriginal",Private);var maxNew=type.GetMethod("PlanningMaximum",Private);
        var costOriginal=type.GetMethod("PlanningCostBoundedOriginal",Private);var costNew=type.GetMethod("PlanningCostBounded",Private);
        // Native skin shell and actual released mesh geometry, with only
        // hypothetical query transforms. No actor or wood is repositioned.
        for(int sample=0;sample<8 && poses<320;sample++,poses++)
        {
            var volume=volumes[poses%volumes.Length];
            Vector3 position=volume.transform.position;
            if(poses%2==0 && battle.Boards.Count>0)position=battle.Boards[poses%battle.Boards.Count].transform.position;
            position+=new Vector3((float)random.NextDouble()*8-4,(float)random.NextDouble()*6-3,(float)random.NextDouble()*8-4);
            Quaternion rotation=volume.transform.rotation;
            var bounds=volume.bounds;bounds.center+=position-volume.transform.position;
            object[] arguments={volume,position,rotation,bounds,float.PositiveInfinity};
            float exactMax=(float)maxOriginal.Invoke(contact,arguments),exactCost=(float)costOriginal.Invoke(contact,arguments);
            if(exactMax>0)penetrating++;
            foreach(float limit in limits)
            {
                arguments[4]=limit;
                float maximum=(float)maxNew.Invoke(contact,arguments),cost=(float)costNew.Invoke(contact,arguments);
                comparisons+=2;
                if((maximum>limit)!=(exactMax>limit) || (cost>limit)!=(exactCost>limit))mismatches++;
                if(maximum<=limit){float difference=Mathf.Abs(maximum-exactMax);largestAcceptedDifference=Mathf.Max(largestAcceptedDifference,difference);if(difference>1e-6f)mismatches++;}
                if(cost<=limit){float difference=Mathf.Abs(cost-exactCost);largestAcceptedDifference=Mathf.Max(largestAcceptedDifference,difference);if(difference>1e-6f)mismatches++;}
            }
        }
        if(poses>=320)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/recent-blocker-v159.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalConvexMeshes=true,actorsMoved=false,woodMoved=false,performanceEvidence=false,
                poses,comparisons,penetrating,mismatches,largestAcceptedDifference,contact.RecentBlockerRejects,
                passed=penetrating>0 && mismatches==0 && largestAcceptedDifference<=1e-6f},Newtonsoft.Json.Formatting.Indented));
            Destroy(this);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
