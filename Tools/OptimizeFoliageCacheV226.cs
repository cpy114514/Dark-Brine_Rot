using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class OptimizeFoliageCacheV226
{
    const string Root=".codex/whole-game-performance-20261019";
    public static object Run(bool apply=false)
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var library=Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");
        var meshes=library.entries.SelectMany(e=>new[]{e.close,e.near,e.far}).Where(m=>m).Distinct().Where(m=>m.name.Contains("Optimized")||m.name.Contains("Balanced")).ToArray();
        var report=new List<object>();int changed=0;
        foreach(var original in meshes)
        {
            var copy=Weld(original);copy.name=original.name;
            try
            {
                long oldMiss=Misses(original);var signature=Triangles(original);
                MeshUtility.Optimize(copy);long newMiss=Misses(copy);
                var after=Triangles(copy);
                if(original.bounds!=copy.bounds||signature.Length!=after.Length||signature.Where((s,i)=>!s.SequenceEqual(after[i])).Any())throw new Exception("Triangle/attribute mismatch: "+original.name);
                bool keep=newMiss<oldMiss*.98;
                if(keep&&apply)
                {
                    string path=AssetDatabase.GetAssetPath(original);string backup=Path.Combine(Root,"before",path);Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                    EditorUtility.CopySerialized(copy,original);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);changed++;
                }
                report.Add(new{original.name,original.vertexCount,newVertices=copy.vertexCount,oldMiss,newMiss,keep,verified=true});
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        File.WriteAllText(Path.Combine(Root,apply?"geometry-optimized.json":"geometry-experiment.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{apply,changed,report},Newtonsoft.Json.Formatting.Indented));return new{apply,changed,report};
    }
    static long Misses(Mesh m){int[] cache=Enumerable.Repeat(-1,32).ToArray();int cursor=0;long miss=0;for(int s=0;s<m.subMeshCount;s++)foreach(int i in m.GetIndices(s)){bool found=false;for(int c=0;c<32;c++)if(cache[c]==i){found=true;break;}if(!found){miss++;cache[cursor]=i;cursor=(cursor+1)%32;}}return miss;}
    static ulong Mix(ulong h,ulong v){unchecked{return (h^v)*1099511628211UL;}}
    static Mesh Weld(Mesh mesh)
    {
        if(mesh.blendShapeCount!=0||mesh.bindposes.Length!=0)throw new Exception("Static foliage only");
        var streams=new byte[mesh.vertexBufferCount][];var strides=new int[streams.Length];
        using(var data=Mesh.AcquireReadOnlyMeshData(mesh))for(int s=0;s<streams.Length;s++){streams[s]=data[0].GetVertexData<byte>(s).ToArray();strides[s]=mesh.GetVertexBufferStride(s);}
        var hashes=new Dictionary<ulong,int>(mesh.vertexCount);var unique=new List<int>(mesh.vertexCount);var remap=new int[mesh.vertexCount];
        for(int i=0;i<mesh.vertexCount;i++)
        {
            ulong h=1469598103934665603UL;for(int s=0;s<streams.Length;s++)for(int b=0;b<strides[s];b++)h=Mix(h,streams[s][i*strides[s]+b]);
            if(hashes.TryGetValue(h,out int at))
            {
                int prior=unique[at];for(int s=0;s<streams.Length;s++)for(int b=0;b<strides[s];b++)if(streams[s][prior*strides[s]+b]!=streams[s][i*strides[s]+b])throw new Exception("Hash collision; preserve original mesh");remap[i]=at;
            }
            else{remap[i]=unique.Count;hashes[h]=unique.Count;unique.Add(i);}
        }
        var copy=new Mesh{indexFormat=mesh.indexFormat};copy.SetVertexBufferParams(unique.Count,mesh.GetVertexAttributes());
        for(int s=0;s<streams.Length;s++){var bytes=new byte[unique.Count*strides[s]];for(int i=0;i<unique.Count;i++)Buffer.BlockCopy(streams[s],unique[i]*strides[s],bytes,i*strides[s],strides[s]);copy.SetVertexBufferData(bytes,0,0,bytes.Length,s,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);}
        copy.subMeshCount=mesh.subMeshCount;for(int s=0;s<mesh.subMeshCount;s++){var ix=mesh.GetIndices(s);for(int i=0;i<ix.Length;i++)ix[i]=remap[ix[i]];copy.SetIndices(ix,mesh.GetTopology(s),s,false);}copy.bounds=mesh.bounds;return copy;
    }
    // Hash every byte of every vertex stream, then compare sorted, winding-preserving
    // triangle signatures per material slot. No attribute or polygon can be removed.
    static ulong[][] Triangles(Mesh mesh)
    {
        var vertex=new ulong[mesh.vertexCount];for(int i=0;i<vertex.Length;i++)vertex[i]=1469598103934665603UL;
        using(var data=Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var m=data[0];
            for(int s=0;s<mesh.vertexBufferCount;s++){var bytes=m.GetVertexData<byte>(s);int stride=mesh.GetVertexBufferStride(s);for(int i=0;i<vertex.Length;i++)for(int b=0;b<stride;b++)vertex[i]=Mix(vertex[i],bytes[i*stride+b]);}
        }
        var result=new ulong[mesh.subMeshCount][];
        for(int s=0;s<result.Length;s++)
        {
            var ix=mesh.GetIndices(s);if(mesh.GetTopology(s)!=UnityEngine.MeshTopology.Triangles)throw new Exception("Non-triangle mesh");
            var hashes=new ulong[ix.Length/3];for(int i=0;i<ix.Length;i+=3){ulong a=vertex[ix[i]],b=vertex[ix[i+1]],c=vertex[ix[i+2]];ulong h1=Mix(Mix(a,b),c),h2=Mix(Mix(b,c),a),h3=Mix(Mix(c,a),b);hashes[i/3]=Math.Min(h1,Math.Min(h2,h3));}Array.Sort(hashes);result[s]=hashes;
        }
        return result;
    }
}
