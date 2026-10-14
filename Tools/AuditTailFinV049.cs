using System;
using System.Linq;
using UnityEngine;
public static class AuditTailFinV049
{
    public static object Run()
    {
        var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();
        var skin=(SkinnedMeshRenderer)shark.VisualRenderer;
        var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
        int tip=Array.FindIndex(skin.bones,b=>b.name=="Tail_Tip");
        var points=Enumerable.Range(0,vertices.Length).Where(i=>{
            var w=weights[i];return (w.boneIndex0==tip?w.weight0:0)+(w.boneIndex1==tip?w.weight1:0)+(w.boneIndex2==tip?w.weight2:0)+(w.boneIndex3==tip?w.weight3:0)>.9999f;
        }).Select(i=>skin.sharedMesh.bindposes[tip].MultiplyPoint3x4(vertices[i])).ToArray();
        return new{skin=skin.name,points=points.Length,bindPoints=points.OrderBy(p=>p.y).Where((p,i)=>i%Math.Max(1,points.Length/12)==0).Select(p=>p.ToString("F6")).ToArray(),bonePosition=shark.transform.InverseTransformPoint(skin.bones[tip].position).ToString("F6"),skinScale=skin.transform.lossyScale.ToString(),sharkScale=shark.transform.lossyScale.ToString()};
    }
}
