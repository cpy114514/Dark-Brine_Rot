using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Mavis;
public static class BuildGrassRenderMeshesV226
{
    struct Vertex {public Vector3 position,normal;public Vector2 uv;}
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string folder="Assets/Resources/FoliagePerformance/RenderLayoutsV226";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/FoliagePerformance","RenderLayoutsV226");
        var library=Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");var report=new List<object>();
        foreach(var e in library.entries.Where(x=>x.grass))
        {
            var source=e.source;if(source.blendShapeCount>0||source.bindposes.Length>0)throw new Exception("Static mesh required");
            var p=source.vertices;var n=source.normals;var uv=source.uv;if(n.Length!=p.Length||uv.Length!=p.Length)throw new Exception("Incomplete shader inputs");
            string path=folder+"/"+source.name+"_ExactWind.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                mesh=new Mesh{name=source.name+"_ExactWind",indexFormat=source.indexFormat};
                mesh.SetVertexBufferParams(p.Length,new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float32,3),new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,2));
                var data=new Vertex[p.Length];for(int i=0;i<data.Length;i++)data[i]=new Vertex{position=p[i],normal=n[i],uv=uv[i]};mesh.SetVertexBufferData(data,0,0,data.Length);mesh.subMeshCount=source.subMeshCount;
                for(int s=0;s<source.subMeshCount;s++)mesh.SetIndices(source.GetIndices(s),source.GetTopology(s),s,false);mesh.bounds=source.bounds;AssetDatabase.CreateAsset(mesh,path);
            }
            bool equal=mesh.vertices.SequenceEqual(p)&&mesh.normals.SequenceEqual(n)&&mesh.uv.SequenceEqual(uv)&&mesh.bounds==source.bounds&&mesh.subMeshCount==source.subMeshCount;
            for(int s=0;s<source.subMeshCount;s++)equal&=mesh.GetIndices(s).SequenceEqual(source.GetIndices(s))&&mesh.GetTopology(s)==source.GetTopology(s);
            if(!equal)throw new Exception("Render layout changed geometry: "+source.name);
            e.renderSource=mesh;
            report.Add(new{source.name,source.vertexCount,originalStride=source.GetVertexBufferStride(0),renderStride=mesh.GetVertexBufferStride(0),unchangedGeometryAndShaderInputs=equal});
        }
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);
        File.WriteAllText(".codex/whole-game-performance-20261019/render-layouts.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
}
