using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class VerifyOceanMeshV244
{
    public static object Run()
    {
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var camera=Camera.main;
        if(!ocean||!camera)throw new Exception("Ocean and fixed review camera required");
        var mesh=ocean.GetComponent<MeshFilter>().sharedMesh;
        if(mesh.name!="Visible Ocean Patch")throw new Exception("Forward-patch verification required");
        var vertices=mesh.vertices;var old=vertices.ToArray();int[] triangles=mesh.triangles;
        float radius=ocean.oceanSize+Mathf.Max(300,ocean.oceanSize*.2f);
        for(int i=0;i<old.Length;i++)
        {
            float z=vertices[i].z;
            float newWidth=Mathf.Min(radius*1.10f,90+Mathf.Abs(z)*2.2f);
            float oldZ=z<=0?z:radius*Mathf.Pow(Mathf.Log(1+z/12f)/Mathf.Log(1+radius/12f),2.2f);
            float oldWidth=Mathf.Lerp(radius*.4f,radius*1.10f,Mathf.Clamp01(oldZ/radius));
            old[i].x*=oldWidth/newWidth;old[i].z=oldZ;
        }
        var snapshot=OceanSurfaceSampler.Capture(ocean,camera,27.2f);
        var displace=typeof(OceanSurfaceSampler.Snapshot).GetMethod("Displacement",BindingFlags.Instance|BindingFlags.NonPublic);
        object Measure(Vector3[] points)
        {
            var errors=new List<double>();var cache=new Dictionary<int,Vector3>();
            Vector3 Vertex(int i)
            {
                if(cache.TryGetValue(i,out var v))return v;
                Vector3 p=ocean.transform.TransformPoint(points[i]);
                object[] args={p.x,p.z,true,0f,0f,0f};displace.Invoke(snapshot,args);
                v=new Vector3(p.x+(float)args[3],ocean.oceanHeight+(float)args[4],p.z+(float)args[5]);cache.Add(i,v);return v;
            }
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 basePoint=ocean.transform.TransformPoint((points[triangles[i]]+points[triangles[i+1]]+points[triangles[i+2]])/3);
                Vector3 offset=basePoint-camera.transform.position;offset.y=0;
                var view=camera.WorldToViewportPoint(basePoint);
                if(offset.sqrMagnitude>3600||view.z<=0||view.x<0||view.x>1)continue;
                Vector3 p=(Vertex(triangles[i])+Vertex(triangles[i+1])+Vertex(triangles[i+2]))/3;
                errors.Add(Math.Abs(snapshot.Height(p)-p.y));
            }
            var sorted=errors.OrderBy(x=>x).ToArray();
            return new{samples=errors.Count,mean=errors.Count>0?errors.Average():-1,p95=errors.Count>0?sorted[(int)((sorted.Length-1)*.95)]:-1,max=errors.Count>0?errors.Max():-1};
        }
        var result=new{vertices=vertices.Length,triangles=triangles.Length/3,range=ocean.oceanSize,nearHalfWidthOld=radius*.4f,nearHalfWidthNew=90,before=Measure(old),after=Measure(vertices),scope="Same clock, camera, vertex and triangle count. Barycentric displaced triangles compared with the same analytic wave surface; geometry metric, not a hardware FPS benchmark."};
        File.WriteAllText(".codex/ocean-buoyancy-20261023/mesh-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
