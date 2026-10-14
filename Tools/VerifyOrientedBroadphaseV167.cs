using System;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEngine;

public static class VerifyOrientedBroadphaseV167
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");
        var holder=new GameObject("Original native oriented-bound rejection v167");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.waterEntry=true;benchmark.captureScreenshots=false;benchmark.scatterSeed=20261015;
        holder.AddComponent<OrientedBroadphaseObserverV167>().benchmark=benchmark;Time.captureFramerate=30;
        return new{started=true,readOnlyHypotheticalQueries=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32900)]
public sealed class OrientedBroadphaseObserverV167:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly System.Random random=new System.Random(167);
    int pairs,hits,additionalRejects,falseRejects,validBoxes;float worstRejectedDepth;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        var contact=battle.WreckContact;Type type=contact.GetType();
        type.GetMethod("SnapshotPlanningShapes",Private).Invoke(contact,null);
        var parts=(IList)type.GetField("planningParts",Private).GetValue(contact);
        var volumes=(Collider[])type.GetField("volumes",Private).GetValue(contact);
        var queryType=type.GetNestedType("CapsuleBoundsQuery",BindingFlags.NonPublic);
        var boxType=type.GetNestedType("OrientedPartBounds",BindingFlags.NonPublic);
        var constructor=queryType.GetConstructor(Private,null,new[]{typeof(Collider),typeof(Vector3),typeof(Quaternion)},null);
        var oldMay=queryType.GetMethod("MayTouch",Private,null,new[]{typeof(Bounds)},null);
        var newMay=queryType.GetMethod("MayTouch",Private,null,new[]{boxType},null);
        for(int sample=0;sample<80 && pairs<4000;sample++,pairs++)
        {
            object part=parts[(pairs*37)%parts.Count];Type partType=part.GetType();
            var bounds=(Bounds)partType.GetField("bounds").GetValue(part);
            var box=partType.GetField("oriented").GetValue(part);
            if((bool)boxType.GetField("valid").GetValue(box))validBoxes++;
            var volume=volumes[pairs%4];
            Vector3 randomOffset=new Vector3((float)random.NextDouble()*3-1.5f,(float)random.NextDouble()*3-1.5f,(float)random.NextDouble()*3-1.5f);
            Vector3 position=bounds.center+Vector3.Scale(bounds.extents+Vector3.one*.3f,randomOffset);
            Quaternion rotation=Quaternion.Euler((float)random.NextDouble()*360,(float)random.NextDouble()*360,(float)random.NextDouble()*360);
            object query=constructor.Invoke(new object[]{volume,position,rotation});
            bool previous=(bool)oldMay.Invoke(query,new object[]{bounds}),current=(bool)newMay.Invoke(query,new[]{box});
            bool native=Physics.ComputePenetration(volume,position,rotation,(MeshCollider)partType.GetField("collider").GetValue(part),
                (Vector3)partType.GetField("position").GetValue(part),(Quaternion)partType.GetField("rotation").GetValue(part),out _,out float depth);
            if(native)hits++;
            if(previous && !current)additionalRejects++;
            if(native && !current){falseRejects++;worstRejectedDepth=Mathf.Max(worstRejectedDepth,depth);}
        }
        if(pairs>=4000)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/oriented-broadphase-v167.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalConvexMeshes=true,actorsMoved=false,woodMoved=false,performanceEvidence=false,
                pairs,hits,validBoxes,additionalRejects,falseRejects,worstRejectedDepth,
                passed=hits>0 && additionalRejects>0 && falseRejects==0},Newtonsoft.Json.Formatting.Indented));
            Destroy(this);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
