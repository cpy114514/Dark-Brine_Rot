using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

// Asset authoring only. Geometry work runs outside the editor's main thread.
public static class CoastalTreeBuilder
{
    public const string Folder = "Assets/Game/Prefabs/Environment/Trees/CoastalTrees";
    static Task<Data[][]> pending;
    static int tree;
    public static string Status { get; private set; } = "Idle";
    class Data
    {
        public Vector3[] positions, normals;
        public Vector4[] tangents;
        public Vector2[] uv;
        public int[] triangles;
    }

    public static void Start(int number)
    {
        if (pending != null) throw new InvalidOperationException("A tree is already being built.");
        tree = number;
        string path = Folder + "/Models/island_tree_0" + number + "_2k.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (!importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().First();
        var rotation = model.transform.localRotation;
        var matrix = Matrix4x4.TRS(model.transform.localPosition * 2f, rotation, model.transform.localScale * 2f);
        var positions = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        float bottom = positions.Min(v => v.y);
        for (int i = 0; i < positions.Length; i++) positions[i].y -= bottom;
        var source = new Data { positions = positions, normals = mesh.normals.Select(v => rotation * v).ToArray(), tangents = mesh.tangents.Select(v => { var t = rotation * new Vector3(v.x, v.y, v.z); return new Vector4(t.x, t.y, t.z, v.w); }).ToArray(), uv = mesh.uv };
        var indices = Enumerable.Range(0, 3).Select(mesh.GetTriangles).ToArray();
        Status = "Simplifying tree " + number;
        pending = Task.Run(() => Build(source, indices));
        EditorApplication.update += Finish;
    }

    static Data[][] Build(Data source, int[][] indices)
    {
        var output = new Data[3][];
        for (int l = 0; l < 3; l++) output[l] = new Data[3];
        var trunk = Compact(source, indices[0]);
        for (int l = 0; l < 3; l++) output[l][0] = l == 0 ? trunk : Reduce(trunk, l == 1 ? 4000 : 1000, true);
        for (int part = 1; part < 3; part++)
        {
            int[] parent = Enumerable.Range(0, source.positions.Length).ToArray();
            Func<int, int> find = x => { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; };
            var tri = indices[part];
            for (int i = 0; i < tri.Length; i += 3) { int r = find(tri[i]); parent[find(tri[i + 1])] = r; parent[find(tri[i + 2])] = r; }
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < tri.Length; i += 3)
            {
                int r = find(tri[i]);
                if (!groups.TryGetValue(r, out var list)) groups[r] = list = new List<int>();
                list.Add(tri[i]); list.Add(tri[i + 1]); list.Add(tri[i + 2]);
            }
            var pieces = new List<Data>[] { new List<Data>(), new List<Data>(), new List<Data>() };
            int count = 0;
            foreach (var entry in groups)
            {
                // Stable, nested card thinning. Keep each selected leaf/card intact.
                uint hash = (uint)entry.Key * 747796405u + 2891336453u;
                hash = ((hash >> (int)((hash >> 28) + 4)) ^ hash) * 277803737u;
                float sample = ((hash >> 22) ^ hash) / (float)uint.MaxValue;
                var card = Compact(source, entry.Value.ToArray());
                pieces[0].Add(Reduce(card, part == 1 ? 6 : 16, part == 2));
                if (sample < .45f) pieces[1].Add(Reduce(card, part == 1 ? 2 : 8, part == 2));
                if (sample < .12f) pieces[2].Add(Reduce(card, part == 1 ? 2 : 6, part == 2));
                if ((++count % 2000) == 0) Status = "Tree " + tree + ": " + (part == 1 ? "leaves " : "branches ") + count + "/" + groups.Count;
            }
            for (int l = 0; l < 3; l++) output[l][part] = Combine(pieces[l]);
        }
        return output;
    }

    static Data Compact(Data source, int[] triangles)
    {
        var map = new Dictionary<int, int>();
        var original = new List<int>();
        var remap = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            int v = triangles[i];
            if (!map.TryGetValue(v, out int n)) { n = map.Count; map[v] = n; original.Add(v); }
            remap[i] = n;
        }
        return new Data { positions = original.Select(i => source.positions[i]).ToArray(), normals = original.Select(i => source.normals[i]).ToArray(), tangents = original.Select(i => source.tangents[i]).ToArray(), uv = original.Select(i => source.uv[i]).ToArray(), triangles = remap };
    }

    static Data Reduce(Data data, int target, bool preserveBorder = false)
    {
        if (data.triangles.Length <= target * 3) return data;
        var s = new MeshSimplifier { Vertices = data.positions, Normals = data.normals, Tangents = data.tangents, UV1 = data.uv };
        var options = SimplificationOptions.Default;
        options.EnableSmartLink = false;
        options.PreserveBorderEdges = preserveBorder;
        options.MaxIterationCount = 40;
        s.SimplificationOptions = options;
        s.AddSubMeshTriangles(data.triangles);
        s.SimplifyMesh(target / (float)(data.triangles.Length / 3));
        var result = new Data { positions = s.Vertices, normals = s.Normals, tangents = s.Tangents, uv = s.UV1, triangles = s.GetSubMeshTriangles(0) };
        // A separate card must never vanish from the near LOD.
        return result.triangles.Length == 0 ? data : result;
    }

    static Data Combine(List<Data> pieces)
    {
        var p = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<Vector4>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        foreach (var piece in pieces) { int offset = p.Count; p.AddRange(piece.positions); n.AddRange(piece.normals); t.AddRange(piece.tangents); uv.AddRange(piece.uv); triangles.AddRange(piece.triangles.Select(i => i + offset)); }
        return new Data { positions = p.ToArray(), normals = n.ToArray(), tangents = t.ToArray(), uv = uv.ToArray(), triangles = triangles.ToArray() };
    }

    static void Finish()
    {
        if (pending == null || !pending.IsCompleted) return;
        EditorApplication.update -= Finish;
        var task = pending; pending = null;
        if (task.IsFaulted) { Status = task.Exception.ToString(); Debug.LogError(Status); return; }
        try { Save(task.Result); Status = "Complete: IslandTree0" + tree; }
        catch (Exception e) { Status = e.ToString(); Debug.LogException(e); }
    }

    static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + name);
    static Material MaterialAsset(string name, string shader)
    {
        string path = Folder + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(material, path); }
        material.enableInstancing = true;
        return material;
    }

    static void Save(Data[][] data)
    {
        foreach (string dir in new[] { "Materials", "Meshes", "Prefabs" }) if (!AssetDatabase.IsValidFolder(Folder + "/" + dir)) AssetDatabase.CreateFolder(Folder, dir);
        var bark = MaterialAsset("IslandTree0" + tree + "_Bark", "Universal Render Pipeline/Lit");
        bark.SetTexture("_BaseMap", Texture("island_tree_0" + tree + "_Diffuse_2k.jpg"));
        bark.SetTexture("_BumpMap", Texture("island_tree_0" + tree + "_nor_gl_2k.png")); bark.EnableKeyword("_NORMALMAP");
        bark.SetFloat("_Smoothness", .08f); bark.SetFloat("_BumpScale", .8f);
        var leaves = MaterialAsset("Coastal_Leaves", "Mavis/FoliageWind");
        var branches = MaterialAsset("Coastal_Branches", "Mavis/FoliageWind");
        foreach (var m in new[] { leaves, branches })
        {
            bool leaf = m == leaves;
            var color = Texture(leaf ? "coastal_leaves_diff_2k.png" : "coastal_branches_diff_2k.png");
            m.SetTexture("_MainTex", color); m.SetTexture("_BaseMap", color);
            m.SetFloat("_UseAlphaMap", leaf ? 1f : 0f);
            if (leaf) m.SetTexture("_AlphaMap", Texture("coastal_leaves_alpha_2k.png"));
            m.SetFloat("_Cutoff", .38f); m.SetFloat("_Cull", 0); m.SetFloat("_Smoothness", .04f);
            m.SetFloat("_WindBend", .55f); m.SetFloat("_WindFrequency", .75f); m.SetFloat("_WindGust", .6f); m.SetFloat("_WindTrunkStiffness", .15f);
            m.SetFloat("_PlayerPushStrength", 0); m.SetFloat("_MavisWindInvHeight", .15f); m.SetFloat("_MavisWindResponse", leaf ? .86f : .38f);
        }
        foreach (var m in new[] { bark, leaves, branches }) { EditorUtility.SetDirty(m); AssetDatabase.SaveAssetIfDirty(m); }
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("IslandTree0" + tree); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var lods = new LOD[3];
            string[] parts = { "Trunk", "Leaves", "Branches" };
            var materials = new[] { bark, leaves, branches };
            for (int l = 0; l < 3; l++)
            {
                var level = new GameObject("LOD" + l); level.transform.SetParent(root.transform, false);
                var renderers = new Renderer[3];
                for (int p = 0; p < 3; p++)
                {
                    var d = data[l][p];
                    var mesh = new Mesh { name = "IslandTree0" + tree + "_" + parts[p] + "_LOD" + l, indexFormat = IndexFormat.UInt32 };
                    mesh.vertices = d.positions; mesh.normals = d.normals; mesh.tangents = d.tangents; mesh.uv = d.uv; mesh.triangles = d.triangles; mesh.RecalculateBounds();
                    string path = Folder + "/Meshes/" + mesh.name + ".asset";
                    var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (old != null) { old.Clear(); old.indexFormat = mesh.indexFormat; old.vertices = mesh.vertices; old.normals = mesh.normals; old.tangents = mesh.tangents; old.uv = mesh.uv; old.triangles = mesh.triangles; old.RecalculateBounds(); old.UploadMeshData(false); UnityEngine.Object.DestroyImmediate(mesh); mesh = old; EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh); }
                    else AssetDatabase.CreateAsset(mesh, path);
                    var part = new GameObject(parts[p]); part.transform.SetParent(level.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials[p]; renderers[p] = renderer;
                }
                lods[l] = new LOD(new[] { .45f, .16f, .015f }[l], renderers);
            }
            var group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
            var collision = new GameObject("TrunkCollider"); collision.transform.SetParent(root.transform, false);
            collision.AddComponent<MeshCollider>().sharedMesh = root.transform.Find("LOD1/Trunk").GetComponent<MeshFilter>().sharedMesh;
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Prefabs/IslandTree0" + tree + ".prefab");
            var report = new { tree, height = group.size, triangles = data.Select(partsData => partsData.Sum(d => d.triangles.Length / 3)).ToArray(), materials = materials.Select(m => new { m.name, shader = m.shader.name, supported = m.shader.isSupported }).ToArray() };
            System.IO.File.WriteAllText("ProjectRepairBackups/IslandTreeImport-20261001/Tree0" + tree + "-build.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
    }
}
