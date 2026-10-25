using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class VerifyOceanMeshBudgetsV244
{
    public static object Run()
    {
        var build=typeof(OceanWorld).GetMethod("CreateOceanMesh",BindingFlags.Static|BindingFlags.NonPublic);
        var rows=new List<object>();
        foreach(int resolution in new[]{80,160,300})foreach(int coverage in new[]{120,360})
        {
            var mesh=(Mesh)build.Invoke(null,new object[]{resolution,7200f,coverage});
            try
            {
                var v=mesh.vertices;var t=mesh.triangles;
                bool valid=v.All(p=>!float.IsNaN(p.x)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.z))&&t.All(i=>i>=0&&i<v.Length);
                int expected=coverage==360?resolution*resolution*2:resolution*Mathf.Max(48,Mathf.RoundToInt(resolution*.85f))*2;
                bool sameBudget=t.Length/3==expected;
                if(!valid||!sameBudget)throw new Exception("Invalid geometry or increased triangle budget");
                rows.Add(new{resolution,coverage,vertices=v.Length,triangles=t.Length/3,valid,sameBudget,indexFormat=mesh.indexFormat.ToString()});
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        File.WriteAllText(".codex/ocean-buoyancy-20261023/mesh-budgets.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        return rows;
    }
}
