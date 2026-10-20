using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mavis;
public static class AuditGrassTopologyV226
{
    public static object Run()
    {
        var rows=new List<object>();
        foreach(var m in Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library").entries.Where(e=>e.grass).Select(e=>e.source).Distinct())
        {
            var p=m.vertices;var n=m.normals;var uv=m.uv;var keys=new HashSet<(Vector3,Vector3,Vector2)>();for(int i=0;i<p.Length;i++)keys.Add((p[i],n[i],uv.Length==p.Length?uv[i]:Vector2.zero));
            int duplicate=0,triangles=0;for(int s=0;s<m.subMeshCount;s++){var seen=new HashSet<string>();var ix=m.GetIndices(s);triangles+=ix.Length/3;for(int i=0;i<ix.Length;i+=3){var k=new string[3];for(int j=0;j<3;j++){int at=ix[i+j];k[j]=p[at].x.ToString("R")+","+p[at].y.ToString("R")+","+p[at].z.ToString("R")+","+uv[at].x.ToString("R")+","+uv[at].y.ToString("R");}Array.Sort(k,StringComparer.Ordinal);if(!seen.Add(string.Join(";",k)))duplicate++;}}
            rows.Add(new{m.name,m.vertexCount,usedAttributeVertices=keys.Count,triangles,duplicate});
        }
        File.WriteAllText(".codex/whole-game-performance-20261019/grass-topology.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
}
