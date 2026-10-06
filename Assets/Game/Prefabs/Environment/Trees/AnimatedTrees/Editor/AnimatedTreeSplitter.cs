using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AnimatedTreeSplitter
{
    public const string Folder="Assets/Game/Prefabs/Environment/Trees/AnimatedTrees";
    public const string SourcePath=Folder+"/tree_animate.glb";
    public static Vector3[] Pivots { get; private set; }
    public static readonly Dictionary<string,int[][]> VertexMaps=new Dictionary<string,int[][]>();

    [MenuItem("Tools/Trees/Create Individual Animated Trees")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run outside Play Mode.");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if(source==null)throw new InvalidOperationException("tree_animate model not found.");
        var bark=source.GetComponentInChildren<MeshFilter>();
        Pivots=FindTrunks(source.transform,bark);
        if(Pivots.Length!=3)throw new InvalidOperationException("Expected three separate trunks; found "+Pivots.Length);
        EnsureFolder(Folder+"/SingleTrees");EnsureFolder(Folder+"/SingleTrees/Meshes");
        VertexMaps.Clear();
        var sourceRenderers=source.GetComponentsInChildren<Renderer>(true);
        var split=new Dictionary<string,Mesh[]>();
        foreach(var renderer in sourceRenderers)
        {
            var skin=renderer as SkinnedMeshRenderer;
            var mesh=skin!=null?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
            if(skin!=null&&skin.bones.Length!=0)throw new InvalidOperationException("Bone animation requires a separate rig split.");
            var path=AnimationUtility.CalculateTransformPath(renderer.transform,source.transform);
            int[][] maps;
            var parts=SplitMesh(mesh,source.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix,out maps);
            VertexMaps[path]=maps;
            for(int i=0;i<parts.Length;i++)
            {
                string assetPath=Folder+"/SingleTrees/Meshes/Tree"+(i+1)+"_"+renderer.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if(existing!=null){EditorUtility.CopySerialized(parts[i],existing);UnityEngine.Object.DestroyImmediate(parts[i]);parts[i]=existing;EditorUtility.SetDirty(existing);}
                else AssetDatabase.CreateAsset(parts[i],assetPath);
            }
            split[path]=parts;
        }
        for(int i=0;i<Pivots.Length;i++)
        {
            var tree=UnityEngine.Object.Instantiate(source);
            try
            {
                if(PrefabUtility.IsPartOfPrefabInstance(tree))PrefabUtility.UnpackPrefabInstance(tree,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                tree.name="Animated Tree "+(i+1);
                tree.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                foreach(var pair in split)
                {
                    var part=tree.transform.Find(pair.Key);
                    var skin=part.GetComponent<SkinnedMeshRenderer>();
                    if(skin!=null){skin.sharedMesh=pair.Value[i];skin.localBounds=MorphBounds(pair.Value[i]);}
                    else part.GetComponent<MeshFilter>().sharedMesh=pair.Value[i];
                }
                // Keep the imported paths for MorphBake, but place the editable root
                // at this tree's trunk base instead of the combined model's origin.
                foreach(Transform child in tree.transform)child.localPosition-=Pivots[i];
                PrefabUtility.SaveAsPrefabAsset(tree,PrefabPath(i));
            }
            finally{UnityEngine.Object.DestroyImmediate(tree);}
        }
        AssetDatabase.SaveAssets();
    }

    public static string PrefabPath(int index)=>Folder+"/SingleTrees/AnimatedTree"+(index+1)+".prefab";
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        int slash=path.LastIndexOf('/');EnsureFolder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
    }

    static Vector3[] FindTrunks(Transform root,MeshFilter filter)
    {
        var mesh=filter.sharedMesh;var vertices=mesh.vertices;
        var parent=Enumerable.Range(0,vertices.Length).ToArray();
        int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
        void Union(int a,int b){a=Find(a);b=Find(b);if(a!=b)parent[b]=a;}
        var welded=new Dictionary<Vector3Int,int>();
        for(int i=0;i<vertices.Length;i++){var key=Vector3Int.RoundToInt(vertices[i]*1000);if(welded.TryGetValue(key,out int j))Union(i,j);else welded[key]=i;}
        var triangles=mesh.triangles;
        for(int i=0;i<triangles.Length;i+=3){Union(triangles[i],triangles[i+1]);Union(triangles[i],triangles[i+2]);}
        var matrix=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
        var points=vertices.Select(matrix.MultiplyPoint3x4).ToArray();float lowest=points.Min(v=>v.y),highest=points.Max(v=>v.y);
        var trunks=new List<Vector3>();
        foreach(var group in Enumerable.Range(0,vertices.Length).GroupBy(Find))
        {
            var ids=group.ToArray();float min=ids.Min(i=>points[i].y),max=ids.Max(i=>points[i].y);
            if(min>lowest+.5f||max-min<(highest-lowest)*.5f)continue;
            var basePoints=ids.Where(i=>points[i].y<min+.5f).Select(i=>points[i]).ToArray();
            trunks.Add(new Vector3(basePoints.Average(v=>v.x),min,basePoints.Average(v=>v.z)));
        }
        return trunks.OrderBy(p=>p.x).ToArray();
    }

    static Mesh[] SplitMesh(Mesh source,Matrix4x4 toRoot,out int[][] maps)
    {
        var vertices=source.vertices;
        int Tree(Vector3 p)
        {
            int best=0;float distance=float.PositiveInfinity;
            for(int i=0;i<Pivots.Length;i++){var d=p-Pivots[i];d.y=0;if(d.sqrMagnitude<distance){best=i;distance=d.sqrMagnitude;}}
            return best;
        }
        // Classify complete connected pieces. A leaf card can extend across a
        // midpoint between trunks; classifying its individual vertices would tear it.
        var parent=Enumerable.Range(0,vertices.Length).ToArray();
        int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
        void Union(int a,int b){a=Find(a);b=Find(b);if(a!=b)parent[b]=a;}
        var welded=new Dictionary<Vector3Int,int>();
        for(int i=0;i<vertices.Length;i++){var key=Vector3Int.RoundToInt(vertices[i]*1000);if(welded.TryGetValue(key,out int j))Union(i,j);else welded[key]=i;}
        var allTriangles=source.triangles;
        for(int j=0;j<allTriangles.Length;j+=3){Union(allTriangles[j],allTriangles[j+1]);Union(allTriangles[j],allTriangles[j+2]);}
        var owners=new int[vertices.Length];
        foreach(var group in Enumerable.Range(0,vertices.Length).GroupBy(Find))
        {
            var ids=group.ToArray();Vector3 center=Vector3.zero;
            foreach(int id in ids)center+=toRoot.MultiplyPoint3x4(vertices[id]);
            int tree=Tree(center/ids.Length);foreach(int id in ids)owners[id]=tree;
        }
        var triangles=new List<int>[Pivots.Length,source.subMeshCount];
        for(int t=0;t<Pivots.Length;t++)for(int s=0;s<source.subMeshCount;s++)triangles[t,s]=new List<int>();
        for(int s=0;s<source.subMeshCount;s++)
        {
            var indices=source.GetTriangles(s);
            for(int j=0;j<indices.Length;j+=3)
            {
                int a=owners[indices[j]];
                int b=owners[indices[j+1]];
                int c=owners[indices[j+2]];
                if(a!=b||a!=c)throw new InvalidOperationException(source.name+" triangle crosses tree boundaries: "+toRoot.MultiplyPoint3x4(vertices[indices[j]])+", "+toRoot.MultiplyPoint3x4(vertices[indices[j+1]])+", "+toRoot.MultiplyPoint3x4(vertices[indices[j+2]]));
                triangles[a,s].AddRange(new[]{indices[j],indices[j+1],indices[j+2]});
            }
        }
        var parts=new Mesh[Pivots.Length];maps=new int[Pivots.Length][];
        for(int t=0;t<parts.Length;t++)
        {
            var ids=Enumerable.Range(0,source.subMeshCount).SelectMany(s=>triangles[t,s]).Distinct().OrderBy(i=>i).ToArray();maps[t]=ids;
            var remap=new Dictionary<int,int>();for(int i=0;i<ids.Length;i++)remap[ids[i]]=i;
            var mesh=new Mesh{name="AnimatedTree"+(t+1)+"_"+source.name,indexFormat=ids.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=ids.Select(i=>vertices[i]).ToArray();
            var normals=source.normals;if(normals.Length==vertices.Length)mesh.normals=ids.Select(i=>normals[i]).ToArray();
            var tangents=source.tangents;if(tangents.Length==vertices.Length)mesh.tangents=ids.Select(i=>tangents[i]).ToArray();
            var colors=source.colors32;if(colors.Length==vertices.Length)mesh.colors32=ids.Select(i=>colors[i]).ToArray();
            for(int channel=0;channel<8;channel++)
            {
                var uv=new List<Vector4>();source.GetUVs(channel,uv);if(uv.Count!=vertices.Length)continue;
                int dimension=source.GetVertexAttributeDimension((VertexAttribute)((int)VertexAttribute.TexCoord0+channel));
                if(dimension==2)mesh.SetUVs(channel,ids.Select(i=>new Vector2(uv[i].x,uv[i].y)).ToList());
                else if(dimension==3)mesh.SetUVs(channel,ids.Select(i=>new Vector3(uv[i].x,uv[i].y,uv[i].z)).ToList());
                else mesh.SetUVs(channel,ids.Select(i=>uv[i]).ToList());
            }
            mesh.subMeshCount=source.subMeshCount;
            for(int s=0;s<source.subMeshCount;s++)mesh.SetTriangles(triangles[t,s].Select(i=>remap[i]).ToArray(),s,false);
            var dv=new Vector3[vertices.Length];var dn=new Vector3[vertices.Length];var dt=new Vector3[vertices.Length];
            for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
            {
                source.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),ids.Select(i=>dv[i]).ToArray(),ids.Select(i=>dn[i]).ToArray(),ids.Select(i=>dt[i]).ToArray());
            }
            mesh.RecalculateBounds();parts[t]=mesh;
        }
        return parts;
    }

    static Bounds MorphBounds(Mesh mesh)
    {
        var bounds=mesh.bounds;var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];Vector3 margin=Vector3.zero;
        for(int shape=0;shape<mesh.blendShapeCount;shape++)
        {
            Vector3 shapeMargin=Vector3.zero;
            for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
            {
                mesh.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
                foreach(var d in dv)shapeMargin=Vector3.Max(shapeMargin,new Vector3(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z)));
            }
            margin+=shapeMargin;
        }
        bounds.Expand(margin*2);return bounds;
    }
}
