using System;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEngine;

public static class VerifyExistingBroadphaseV184
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");
        var holder=new GameObject("Original native oriented-bound rejection v184");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.waterEntry=true;benchmark.captureScreenshots=false;benchmark.scatterSeed=20261015;
        holder.AddComponent<ExistingBroadphaseObserverV184>().benchmark=benchmark;Time.captureFramerate=30;
        return new{started=true,readOnlyHypotheticalQueries=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32900)]
public sealed class ExistingBroadphaseObserverV184:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly System.Random random=new System.Random(167);
    readonly System.Collections.Generic.List<object> failures=new System.Collections.Generic.List<object>();
    int pairs,hits,falseRejects;float worstRejectedDepth;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        var contact=battle.WreckContact;Type type=contact.GetType();
        type.GetMethod("SnapshotPlanningShapes",Private).Invoke(contact,null);
        var parts=(IList)type.GetField("planningParts",Private).GetValue(contact);
        var volumes=(Collider[])type.GetField("volumes",Private).GetValue(contact);
        var queryType=type.GetNestedType("CapsuleBoundsQuery",BindingFlags.NonPublic);

        var constructor=queryType.GetConstructor(Private,null,new[]{typeof(Collider),typeof(Vector3),typeof(Quaternion)},null);
        var oldMay=queryType.GetMethod("MayTouch",Private,null,new[]{typeof(Bounds)},null);

        for(int sample=0;sample<80 && pairs<4000;sample++,pairs++)
        {
            object part=parts[(pairs*37)%parts.Count];Type partType=part.GetType();
            var bounds=(Bounds)partType.GetField("bounds").GetValue(part);

            var volume=volumes[pairs%4];
            Vector3 randomOffset=new Vector3((float)random.NextDouble()*3-1.5f,(float)random.NextDouble()*3-1.5f,(float)random.NextDouble()*3-1.5f);
            Vector3 position=bounds.center+Vector3.Scale(bounds.extents+Vector3.one*.3f,randomOffset);
            Quaternion rotation=Quaternion.Euler((float)random.NextDouble()*360,(float)random.NextDouble()*360,(float)random.NextDouble()*360);
            object query=constructor.Invoke(new object[]{volume,position,rotation});
            bool previous=(bool)oldMay.Invoke(query,new object[]{bounds}),current=previous;
            bool native=Physics.ComputePenetration(volume,position,rotation,(MeshCollider)partType.GetField("collider").GetValue(part),
                (Vector3)partType.GetField("position").GetValue(part),(Quaternion)partType.GetField("rotation").GetValue(part),out _,out float depth);
            if(native)hits++;
            if(native && !current){falseRejects++;worstRejectedDepth=Mathf.Max(worstRejectedDepth,depth);failures.Add(new{pairs,depth,mesh=((MeshCollider)partType.GetField("collider").GetValue(part)).sharedMesh.name,position=position.ToString("F6"),rotation=rotation.ToString("F6"),bounds=bounds.ToString("F6"),radius=((CapsuleCollider)volume).radius,height=((CapsuleCollider)volume).height,scale=volume.transform.lossyScale.ToString("F6")});}
        }
        if(pairs>=4000)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/existing-broadphase-v184.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalConvexMeshes=true,actorsMoved=false,woodMoved=false,performanceEvidence=false,
                pairs,hits,falseRejects,worstRejectedDepth,failures,
                passed=hits>0 && falseRejects==0},Newtonsoft.Json.Formatting.Indented));
            Destroy(benchmark.gameObject);Destroy(this);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
