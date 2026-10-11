using System;
using System.Linq;
using UnityEngine;
public static class AuditAnatomyV015
{
    public static object Inspect()
    {
        var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();
        var skin=(SkinnedMeshRenderer)shark.VisualRenderer;var mesh=new Mesh();
        string action=shark.CinematicAction;float seconds=shark.CinematicActionSeconds;
        try
        {
            var rows=new System.Collections.Generic.List<object>();
            foreach(float time in new[]{0f,.2f,.52f})
            {
                shark.SampleCinematicAction("Bite_Lunge",time);skin.BakeMesh(mesh,true);
                var vertices=mesh.vertices.Select(v=>shark.transform.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
                var bones=skin.bones.Select(b=>new {b.name,position=shark.transform.InverseTransformPoint(b.position).ToString("F3")}).ToArray();
                var groups=skin.bones.Select((bone,index)=>new {bone,index}).Select(entry=>
                {
                    var indices=skin.sharedMesh.boneWeights.Select((weight,i)=>new {weight,i}).Where(v=>v.weight.boneIndex0==entry.index && v.weight.weight0>.5f).Select(v=>v.i).ToArray();
                    if(indices.Length==0)return new {name=entry.bone.name,count=0,min="",max=""};
                    var bounds=new Bounds(vertices[indices[0]],Vector3.zero);foreach(int index in indices)bounds.Encapsulate(vertices[index]);
                    return new {name=entry.bone.name,count=indices.Length,min=bounds.min.ToString("F3"),max=bounds.max.ToString("F3")};
                }).ToArray();
                rows.Add(new {time,nose=shark.transform.InverseTransformPoint(shark.NoseWorldPoint).ToString("F3"),rootBounds=shark.RootMeshBounds.ToString(),bones,groups});
            }
            string result=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);
            System.IO.Directory.CreateDirectory(".codex/encounter-v015");System.IO.File.WriteAllText(".codex/encounter-v015/anatomy-audit.json",result);return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);shark.SampleCinematicAction(action??"Swim",seconds);}
    }
}
