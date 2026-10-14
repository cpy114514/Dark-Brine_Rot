using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public static class VerifyTailBindCacheV118B
{
    public static object Check()
    {
        var set=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations");
        var skin=set.rigPrefab.GetComponentInChildren<SkinnedMeshRenderer>();var source=skin.sharedMesh;
        int tip=Array.FindIndex(skin.bones,b=>b.name=="Tail_Tip");
        var vertices=source.vertices;var weights=source.boneWeights;var selected=new List<int>();
        for(int i=0;i<vertices.Length;i++)
        {
            var w=weights[i];float amount=(w.boneIndex0==tip?w.weight0:0)+(w.boneIndex1==tip?w.weight1:0)+(w.boneIndex2==tip?w.weight2:0)+(w.boneIndex3==tip?w.weight3:0);
            if(amount>=.9999f)selected.Add(i);
        }
        if(tip<0 || selected.Count==0)throw new Exception("Actual original rigid caudal vertices required");
        var beforePoints=new Vector3[selected.Count];var afterPoints=new Vector3[selected.Count];
        var timer=System.Diagnostics.Stopwatch.StartNew();long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<selected.Count;i++)beforePoints[i]=source.bindposes[tip].MultiplyPoint3x4(vertices[selected[i]]);
        long originalBytes=GC.GetAllocatedBytesForCurrentThread()-before;double originalMs=timer.Elapsed.TotalMilliseconds;
        timer.Restart();before=GC.GetAllocatedBytesForCurrentThread();var cached=source.bindposes[tip];
        for(int i=0;i<selected.Count;i++)afterPoints[i]=cached.MultiplyPoint3x4(vertices[selected[i]]);
        long cachedBytes=GC.GetAllocatedBytesForCurrentThread()-before;double cachedMs=timer.Elapsed.TotalMilliseconds;
        float largest=0;for(int i=0;i<selected.Count;i++)largest=Mathf.Max(largest,Vector3.Distance(beforePoints[i],afterPoints[i]));
        var result=new{originalRigPrefab=true,nativeMeshGetter=true,vertices=selected.Count,largestVertexDifference=largest,originalBytes,cachedBytes,originalMs,cachedMs,
            geometryPass=largest==0,repeatedGetterCalls=selected.Count,cachedGetterCalls=1,allocationCounterObserved=originalBytes>0 || cachedBytes>0,standaloneFPSAcceptance=false};
        File.WriteAllText(".codex/opening-optimization-20261012/tail-bind-cache-v118b.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
