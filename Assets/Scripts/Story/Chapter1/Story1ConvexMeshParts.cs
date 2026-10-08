using System.Collections.Generic;
using UnityEngine;

/// <summary>Small convex pieces of the real surface, within PhysX's hull limit.</summary>
public static class Story1ConvexMeshParts
{
    static readonly Dictionary<Mesh,Mesh[]> cache=new Dictionary<Mesh,Mesh[]>();
    static readonly List<Mesh> generated=new List<Mesh>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        foreach(var mesh in generated)if(mesh!=null)Object.Destroy(mesh);
        generated.Clear();cache.Clear();
    }

    public static Mesh[] Get(Mesh source)
    {
        if(cache.TryGetValue(source,out var ready))return ready;
        var vertices=source.vertices;var indices=source.triangles;
        var points=new List<Vector3>();var weld=new Dictionary<Vector3,int>();
        var remap=new int[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            if(!weld.TryGetValue(vertices[i],out int at)){at=points.Count;weld.Add(vertices[i],at);points.Add(vertices[i]);}
            remap[i]=at;
        }
        var triangles=new List<int[]>();
        for(int i=0;i<indices.Length;i+=3)
        {
            int a=remap[indices[i]],b=remap[indices[i+1]],c=remap[indices[i+2]];
            if(a!=b && b!=c && a!=c && Vector3.Cross(points[b]-points[a],points[c]-points[a]).sqrMagnitude>1e-16f)
                triangles.Add(new[]{a,b,c});
        }
        var result=new List<Mesh>();Split(source,points,triangles,result);
        ready=result.ToArray();cache.Add(source,ready);return ready;
    }

    static bool Planar(List<Vector3> points,out Vector3 normal)
    {
        normal=Vector3.up;if(points.Count<3)return true;
        for(int i=1;i<points.Count-1;i++)
        {
            var cross=Vector3.Cross(points[i]-points[0],points[i+1]-points[0]);
            if(cross.sqrMagnitude>1e-12f){normal=cross.normalized;break;}
        }
        foreach(var point in points)if(Mathf.Abs(Vector3.Dot(point-points[0],normal))>1e-5f)return false;
        return true;
    }

    static void Split(Mesh source,List<Vector3> vertices,List<int[]> triangles,List<Mesh> output)
    {
        if(triangles.Count==0)return;
        var used=new HashSet<int>();foreach(var triangle in triangles)foreach(int index in triangle)used.Add(index);
        var points=new List<Vector3>();foreach(int index in used)points.Add(vertices[index]);
        bool planar=Planar(points,out var normal);
        // A 120-vertex 3D hull has at most 236 triangular faces. Flat sheets
        // get a 0.4mm local thickness, so use 60 points before doubling them.
        int limit=planar ? 60 : 120;
        if(used.Count>limit && triangles.Count>1)
        {
            var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
            int axis=bounds.size.x>=bounds.size.y && bounds.size.x>=bounds.size.z ? 0 : bounds.size.y>=bounds.size.z ? 1 : 2;
            triangles.Sort((a,b)=>((vertices[a[0]][axis]+vertices[a[1]][axis]+vertices[a[2]][axis])/3).CompareTo((vertices[b[0]][axis]+vertices[b[1]][axis]+vertices[b[2]][axis])/3));
            int half=triangles.Count/2;
            Split(source,vertices,triangles.GetRange(0,half),output);
            Split(source,vertices,triangles.GetRange(half,triangles.Count-half),output);return;
        }
        if(!planar && used.Count==vertices.Count){output.Add(source);return;}
        var remap=new Dictionary<int,int>();var actual=new List<Vector3>();
        foreach(int index in used){remap[index]=actual.Count;actual.Add(vertices[index]-(planar ? normal*.0002f : Vector3.zero));}
        int count=actual.Count;if(planar)foreach(int index in used)actual.Add(vertices[index]+normal*.0002f);
        var indices=new List<int>();
        foreach(var triangle in triangles)
        {
            int a=remap[triangle[0]],b=remap[triangle[1]],c=remap[triangle[2]];
            indices.Add(a);indices.Add(b);indices.Add(c);
            if(planar){indices.Add(c+count);indices.Add(b+count);indices.Add(a+count);}
        }
        var mesh=new Mesh{name=source.name+" collision "+output.Count,hideFlags=HideFlags.DontSave};
        mesh.SetVertices(actual);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        Physics.BakeMesh(mesh.GetEntityId(),true);generated.Add(mesh);output.Add(mesh);
    }
}
