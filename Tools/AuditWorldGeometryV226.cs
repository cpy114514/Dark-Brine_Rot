using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mavis;
public static class AuditWorldGeometryV226
{
    public static object Run()
    {
        var library=Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");
        var meshes=library.entries.SelectMany(e=>new[]{e.source,e.close,e.near,e.far}).Where(m=>m).Distinct().ToArray();
        var rows=new List<object>();
        foreach(var mesh in meshes)
        {
            long count=0,miss=0;int[] cache=Enumerable.Repeat(-1,32).ToArray();int cursor=0;
            for(int s=0;s<mesh.subMeshCount;s++)
            foreach(var i in mesh.GetIndices(s))
            {count++;bool found=false;for(int c=0;c<32;c++)if(cache[c]==i){found=true;break;}if(!found){miss++;cache[cursor]=i;cursor=(cursor+1)%32;}}
            rows.Add(new{mesh.name,mesh.vertexCount,indices=count,cacheMisses=miss,transformsPerTriangle=count>0?3.0*miss/count:0,streams=mesh.vertexBufferCount,attributes=mesh.GetVertexAttributes().Select(x=>x.ToString()).ToArray()});
        }
        var result=new{rows};Directory.CreateDirectory(".codex/whole-game-performance-20261019");File.WriteAllText(Path.Combine(".codex/whole-game-performance-20261019","geometry-audit.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{meshes=rows.Count};
    }
}
