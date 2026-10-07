using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;
using Mavis;

// Build reusable geometry on a worker; never change scene objects or source FBX files.
public static class FoliagePerformanceBuilder
{
    const string Folder = "Assets/Resources/FoliagePerformance";
    sealed class Data
    {
        public Vector3[] positions, normals;
        public Vector4[] tangents;
        public Vector2[] uv;
        public int[][] indices;
    }
    static readonly Queue<Mesh> queue = new Queue<Mesh>();
    static readonly List<FoliageMeshLibrary.Entry> entries = new List<FoliageMeshLibrary.Entry>();
    static Task<Data[]> pending;
    static Mesh current;
    static bool grass;
    public static string Status { get; private set; } = "Idle";
    public static void Start()
    {
        if (pending != null || queue.Count != 0) throw new InvalidOperationException("Build already running");
        System.IO.Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        entries.Clear();
        foreach (var path in new[] {
            "Assets/Game/Prefabs/Environment/Grass/SimpleGrassChunks/Shared/rostlinka_07c_ske.FBX",
            "Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/island_tree_01_4k.fbx",
            "Assets/Game/Prefabs/Environment/Trees/SmallTree02/tree_small_02_4k.fbx",
            "Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/20260918140331_c8541e3e.fbx" })
        foreach (var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
        {
            if (mesh.name.Contains("geometry_nodes")) continue;
            long triangles = Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i) / 3);
            if (triangles > 1000) queue.Enqueue(mesh);
        }
        EditorApplication.update += Pump;
        Next();
    }
    public static object ExtendCoastalLibrary()
    {
        var library=AssetDatabase.LoadAssetAtPath<FoliageMeshLibrary>(Folder+"/Library.asset");
        if(library==null)throw new InvalidOperationException("Build the base foliage library first.");
        var combined=new List<FoliageMeshLibrary.Entry>(library.entries);
        int original=combined.Count;AddCoastalLevels(combined);
        library.entries=combined.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        return new {added=combined.Count-original,total=combined.Count,closeDetailPreserved=true};
    }
    public static async Task<object> OptimizeHeavyTrees()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before building foliage assets.");
        if(pending!=null || queue.Count!=0)throw new InvalidOperationException("Build already running");
        var library=AssetDatabase.LoadAssetAtPath<FoliageMeshLibrary>(Folder+"/Library.asset");
        var report=new List<object>();
        foreach(var entry in library.entries)
        {
            if(entry.grass || entry.near==null)continue;
            var input=entry.close!=null?entry.close:entry.near;
            long before=Enumerable.Range(0,input.subMeshCount).Sum(i=>(long)input.GetIndexCount(i)/3);
            if(before<150000)continue;
            string path=Folder+"/"+entry.source.name+"_BalancedNear.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var source=new Data {positions=input.vertices,normals=input.normals,tangents=input.tangents,uv=input.uv,
                    indices=Enumerable.Range(0,input.subMeshCount).Select(i=>input.GetTriangles(i)).ToArray()};
                // Keep every disconnected leaf/branch, simplifying each piece rather than
                // deleting whole leaves to meet a global polygon budget.
                var levels=await Task.Run(()=>Build(source,false,2));var d=levels[0];
                mesh=new Mesh {name=entry.source.name+"_BalancedNear",indexFormat=d.positions.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.vertices=d.positions;mesh.normals=d.normals;
                if(d.uv.Length==d.positions.Length)mesh.uv=d.uv;
                if(d.tangents.Length==d.positions.Length)mesh.tangents=d.tangents;
                mesh.subMeshCount=d.indices.Length;for(int s=0;s<d.indices.Length;s++)mesh.SetTriangles(d.indices[s],s,false);
                mesh.bounds=input.bounds;AssetDatabase.CreateAsset(mesh,path);
            }
            entry.close=input;entry.near=mesh;entry.fullDetailDistance=18f;
            report.Add(new {source=entry.source.name,before,after=Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(long)mesh.GetIndexCount(i)/3)});
        }
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return report;
    }
    static void AddCoastalLevels(List<FoliageMeshLibrary.Entry> target)
    {
        const string root="Assets/Game/Prefabs/Environment/Trees/CoastalTrees/Meshes/";
        foreach(int tree in new[]{2,3})foreach(string part in new[]{"Branches","Leaves","Trunk"})
        {
            Mesh[] lod=new Mesh[3];
            for(int i=0;i<3;i++)lod[i]=AssetDatabase.LoadAssetAtPath<Mesh>(root+"IslandTree0"+tree+"_"+part+"_LOD"+i+".asset");
            if(lod.Any(m=>m==null))continue;
            for(int i=0;i<3;i++)
                if(!target.Any(e=>e.source==lod[i]))target.Add(new FoliageMeshLibrary.Entry
                {source=lod[i],near=lod[Mathf.Max(1,i)],far=lod[2],fullDetailDistance=i==0?35:0});
        }
    }
    static void Next()
    {
        if (queue.Count == 0)
        {
            var library = ScriptableObject.CreateInstance<FoliageMeshLibrary>();
            AddCoastalLevels(entries);
            library.entries = entries.ToArray();
            var existing = AssetDatabase.LoadAssetAtPath<FoliageMeshLibrary>(Folder + "/Library.asset");
            if (existing != null) { existing.entries = library.entries; EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(library); }
            else AssetDatabase.CreateAsset(library, Folder + "/Library.asset");
            AssetDatabase.SaveAssets();
            Status = "Complete: " + entries.Count + " mesh pairs";
            EditorApplication.update -= Pump;
            return;
        }
        current = queue.Dequeue(); grass = AssetDatabase.GetAssetPath(current).Contains("/Grass/");
        var data = new Data {positions=current.vertices, normals=current.normals, tangents=current.tangents, uv=current.uv,
            indices=Enumerable.Range(0,current.subMeshCount).Select(i=>current.GetTriangles(i)).ToArray()};
        Status = "Simplifying " + current.name + " (" + queue.Count + " remaining)";
        bool isGrass = grass;
        pending = Task.Run(() => Build(data, isGrass));
    }
    static Data[] Build(Data source, bool isGrass, int treeNearTriangles=8)
    {
        var levels = new Data[2];
        var parts = new List<Data>[2] {new List<Data>(),new List<Data>()};
        foreach (var indices in source.indices)
        {
            // Keep every disconnected leaf/blade in the near model. A global
            // collapse of thin foliage would erase entire cards instead of detail.
            var parent = Enumerable.Range(0,source.positions.Length).ToArray();
            Func<int,int> find = x => {while(parent[x]!=x){parent[x]=parent[parent[x]];x=parent[x];}return x;};
            // FBX splits vertices at UV/normal seams. Connect identical positions
            // for topology analysis while keeping all original UV/normal values.
            var sharedPositions = new Dictionary<Vector3,int>();
            foreach(int vertex in indices)
                if(sharedPositions.TryGetValue(source.positions[vertex],out int other))parent[find(vertex)]=find(other);
                else sharedPositions[source.positions[vertex]]=vertex;
            for(int i=0;i<indices.Length;i+=3){int root=find(indices[i]);parent[find(indices[i+1])]=root;parent[find(indices[i+2])]=root;}
            var groups = new Dictionary<int,List<int>>();
            for(int i=0;i<indices.Length;i+=3){int root=find(indices[i]);if(!groups.TryGetValue(root,out var list))groups[root]=list=new List<int>();list.Add(indices[i]);list.Add(indices[i+1]);list.Add(indices[i+2]);}
            var near = new List<Data>(); var far = new List<Data>();
            foreach(var group in groups)
            {
                var piece=Compact(source,group.Value.ToArray());
                int count=piece.indices[0].Length/3;
                if(count>256)
                {
                    var reduced=Reduce(piece,Math.Min(count,isGrass?1000:count>100000?60000:12000));
                    near.Add(reduced);far.Add(Reduce(reduced,isGrass?220:count>100000?7000:2500));
                }
                else
                {
                    near.Add(Reduce(piece,isGrass?6:treeNearTriangles));
                    uint hash=(uint)group.Key*747796405u+2891336453u;hash=((hash>>(int)((hash>>28)+4))^hash)*277803737u;
                    if(((hash>>22)^hash)/(float)uint.MaxValue < (isGrass?.22f:.45f))far.Add(Reduce(piece,2));
                }
            }
            if(far.Count==0 && near.Count>0)far.Add(near[0]);
            parts[0].Add(Combine(near));parts[1].Add(Combine(far));
        }
        for(int i=0;i<2;i++)levels[i]=Combine(parts[i],true);
        return levels;
    }
    static Data Compact(Data source,int[] indices)
    {
        var map=new Dictionary<int,int>();var original=new List<int>();var remap=new int[indices.Length];
        for(int i=0;i<indices.Length;i++){int v=indices[i];if(!map.TryGetValue(v,out int n)){n=map.Count;map[v]=n;original.Add(v);}remap[i]=n;}
        return new Data {positions=original.Select(i=>source.positions[i]).ToArray(),normals=original.Select(i=>source.normals[i]).ToArray(),
            tangents=source.tangents.Length==source.positions.Length?original.Select(i=>source.tangents[i]).ToArray():null,
            uv=source.uv.Length==source.positions.Length?original.Select(i=>source.uv[i]).ToArray():null,indices=new[]{remap}};
    }
    static Data Reduce(Data data,int target)
    {
        int count=data.indices[0].Length/3;if(count<=target)return data;
        var simplifier=new MeshSimplifier {Vertices=data.positions,Normals=data.normals,Tangents=data.tangents,UV1=data.uv};
        var options=SimplificationOptions.Default;options.EnableSmartLink=true;options.MaxIterationCount=60;
        simplifier.SimplificationOptions=options;simplifier.AddSubMeshTriangles(data.indices[0]);simplifier.SimplifyMesh(target/(float)count);
        var indices=simplifier.GetSubMeshTriangles(0);if(indices.Length<3)return data;
        return new Data{positions=simplifier.Vertices,normals=simplifier.Normals,tangents=simplifier.Tangents,uv=simplifier.UV1,indices=new[]{indices}};
    }
    static Data Combine(List<Data> pieces,bool keepSubmeshes=false)
    {
        var positions=new List<Vector3>();var normals=new List<Vector3>();var tangents=new List<Vector4>();var uv=new List<Vector2>();var merged=new List<int>();var submeshes=new List<int[]>();
        foreach(var piece in pieces){int offset=positions.Count;positions.AddRange(piece.positions);normals.AddRange(piece.normals);if(piece.tangents!=null)tangents.AddRange(piece.tangents);if(piece.uv!=null)uv.AddRange(piece.uv);
            var index=piece.indices[0].Select(i=>i+offset).ToArray();if(keepSubmeshes)submeshes.Add(index);else merged.AddRange(index);}
        return new Data{positions=positions.ToArray(),normals=normals.ToArray(),tangents=tangents.ToArray(),uv=uv.ToArray(),indices=keepSubmeshes?submeshes.ToArray():new[]{merged.ToArray()}};
    }
    static void Pump()
    {
        if(pending==null||!pending.IsCompleted)return;
        if(pending.IsFaulted){Status=pending.Exception.ToString();Debug.LogError(Status);pending=null;queue.Clear();EditorApplication.update-=Pump;return;}
        var data=pending.Result;pending=null;var meshes=new Mesh[2];
        for(int i=0;i<2;i++){
            var d=data[i];var mesh=new Mesh{name=current.name+(i==0?"_OptimizedNear":"_OptimizedFar"),indexFormat=d.positions.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=d.positions;mesh.normals=d.normals;if(d.uv.Length==d.positions.Length)mesh.uv=d.uv;if(d.tangents.Length==d.positions.Length)mesh.tangents=d.tangents;
            mesh.subMeshCount=d.indices.Length;for(int s=0;s<d.indices.Length;s++)mesh.SetTriangles(d.indices[s],s,false);
            // Preserve source coverage so wind, culling and LODGroup size agree.
            mesh.bounds=current.bounds;
            string path=Folder+"/"+entries.Count+"_"+mesh.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null){EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            else AssetDatabase.CreateAsset(mesh,path);
            meshes[i]=mesh;
        }
        entries.Add(new FoliageMeshLibrary.Entry{source=current,near=meshes[0],far=meshes[1],grass=grass});
        Next();
    }
}
