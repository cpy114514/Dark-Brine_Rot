using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Scatters CC0 Kenney plants and rocks over the inland meadow. The beach stays clear.</summary>
public static class DressMeadow
{
    const string ModelFolder = "Assets/Game/Prefabs/Environment/MeadowDressing/Models";
    const string MeshFolder = "Assets/Generated/MeadowDressing";
    const string ScenePath = "Assets/Scenes/First Island/Enemies.unity";
    const string RootName = "Meadow Dressing";
    const string ProgressPath = "Temp/meadow-dress.json";

    [System.Serializable] sealed class Progress { public string phase = "import"; public float z; public int plants, pebbles, medium, large, chunks; }

    public static string RemoveFlowersAndRocks()
    {
        if (Application.isPlaying) return "playing";
        var scene = Loaded(ScenePath);
        ClearOld(scene);
        if (AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder);
        if (File.Exists(ProgressPath)) File.Delete(ProgressPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "removed";
    }

    public static string Step()
    {
        if (Application.isPlaying) return "playing";
        var state = File.Exists(ProgressPath) ? JsonUtility.FromJson<Progress>(File.ReadAllText(ProgressPath)) : new Progress();
        if (state.phase == "import")
        {
            if (AssetDatabase.FindAssets("t:Model", new[] { ModelFolder }).Length == 0) AssetDatabase.Refresh();
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.isReadable && !importer.importAnimation) continue;
                Reimport(path);
                if (++count >= 6) break;
            }
            if (count == 0) state.phase = "prepare";
            SaveProgress(state);
            return state.phase == "prepare" ? "imported" : "importing " + count;
        }
        if (state.phase == "prepare")
        {
            Material("Meadow Plants", Color.white, .85f);
            Material("Meadow Rocks", new Color(.92f, .9f, .86f), 0f);
            var enemies = Loaded(ScenePath);
            ClearOld(enemies);
            if (AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder);
            Directory.CreateDirectory(MeshFolder);
            AssetDatabase.Refresh();
            state.z = IslandCollider().bounds.min.z;
            state.phase = "scatter";
            SaveProgress(state);
            EditorSceneManager.SaveScene(enemies);
            return "prepared";
        }
        if (state.phase == "done") return "done plants=" + state.plants + " pebbles=" + state.pebbles + " medium=" + state.medium + " large=" + state.large + " chunks=" + state.chunks;
        var collider = IslandCollider();
        var ocean = Object.FindFirstObjectByType<OceanWorld>();
        float beachTop = (ocean ? ocean.oceanHeight : 0f) + collider.GetComponent<ProceduralIsland>().beachHeight + 2.5f;
        var kinds = LoadKinds();
        var enemiesScene = Loaded(ScenePath);
        var parent = Root(enemiesScene).transform;
        var rockParent = parent.Find("Rocks") ?? new GameObject("Rocks").transform;
        rockParent.SetParent(parent, false);
        var propParent = parent.Find("Plants and pebbles") ?? new GameObject("Plants and pebbles").transform;
        propParent.SetParent(parent, false);
        var chunks = new Dictionary<Vector2Int, Chunk>();
        float end = Mathf.Min(state.z + 64f, collider.bounds.max.z + 8f);
        float top = collider.bounds.max.y + 40f;
        var plantMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/MeadowDressing/Meadow Plants.mat");
        for (float z = state.z; z < end; z += 10f)
        for (float x = collider.bounds.min.x; x <= collider.bounds.max.x; x += 10f)
        {
            int hash = Hash(x, z);
            if (!Cast(collider, x, z, top, out var hit) || hit.point.y < beachTop || hit.normal.y < .62f) continue;
            var roll = new System.Random(hash);
            if (hash % 81 == 0 && state.large < 60) { DropRock(rockParent, kinds.large[roll.Next(kinds.large.Count)], hit, 2.2f + (float)roll.NextDouble() * 1.5f, false); state.large++; continue; }
            if (hash % 31 == 0 && state.medium < 180) { DropRock(rockParent, kinds.medium[roll.Next(kinds.medium.Count)], hit, .9f + (float)roll.NextDouble() * .65f, true); state.medium++; }
            if (hash % 4 == 0 && state.pebbles < 2800)
                for (int i = 0; i < 4 && state.pebbles < 2800; i++)
                {
                    Vector3 at = hit.point + new Vector3((float)roll.NextDouble() - .5f, 0f, (float)roll.NextDouble() - .5f) * 3.4f;
                    if (!Cast(collider, at.x, at.z, top, out var pebble) || pebble.point.y < beachTop) continue;
                    AddProp(chunks, kinds.pebble[roll.Next(kinds.pebble.Count)], pebble, .16f + (float)roll.NextDouble() * .2f, (float)roll.NextDouble() * 360f);
                    state.pebbles++;
                }
            if (hash % 2 != 0 || state.plants >= 5500) continue;
            int choice = roll.Next(100);
            var prop = choice < 46 ? kinds.grass[roll.Next(kinds.grass.Count)] : choice < 70 ? kinds.flower[roll.Next(kinds.flower.Count)] : choice < 90 ? kinds.bush[roll.Next(kinds.bush.Count)] : kinds.mushroom[roll.Next(kinds.mushroom.Count)];
            float height = prop.name.Contains("bush") ? .8f + (float)roll.NextDouble() * .5f : prop.name.Contains("flower") ? .4f + (float)roll.NextDouble() * .25f : .48f + (float)roll.NextDouble() * .4f;
            AddProp(chunks, prop, hit, height, (float)roll.NextDouble() * 360f);
            state.plants++;
        }
        foreach (var pair in chunks)
        {
            var mesh = new Mesh { name = "Meadow " + pair.Key.x + "_" + pair.Key.y + "_" + Mathf.RoundToInt(state.z), indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(pair.Value.vertices); mesh.SetNormals(pair.Value.normals); mesh.SetColors(pair.Value.colors); mesh.SetTriangles(pair.Value.triangles, 0); mesh.RecalculateBounds();
            string asset = MeshFolder + "/Meadow_" + pair.Key.x + "_" + pair.Key.y + "_" + Mathf.RoundToInt(state.z) + ".asset";
            AssetDatabase.CreateAsset(mesh, asset);
            var go = new GameObject(mesh.name);
            go.transform.SetParent(propParent, false);
            go.transform.position = new Vector3(pair.Key.x * 48f, 0f, pair.Key.y * 48f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = plantMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            state.chunks++;
        }
        state.z = end;
        if (state.z > collider.bounds.max.z) state.phase = "done";
        EditorSceneManager.MarkSceneDirty(enemiesScene);
        EditorSceneManager.SaveScene(enemiesScene);
        SaveProgress(state);
        AssetDatabase.SaveAssets();
        return state.phase + " z=" + Mathf.RoundToInt(state.z) + " plants=" + state.plants + " pebbles=" + state.pebbles + " medium=" + state.medium + " large=" + state.large;
    }

    static void Reimport(string path)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.globalScale = 1f;
        importer.isReadable = true;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
    }

    static void SaveProgress(Progress state)
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(ProgressPath, JsonUtility.ToJson(state));
    }

    static MeshCollider IslandCollider()
    {
        var environment = Loaded("Assets/Scenes/First Island/Environment.unity");
        ProceduralIsland island = null;
        foreach (var candidate in Object.FindObjectsByType<ProceduralIsland>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (candidate && candidate.gameObject.scene == environment && candidate.isActiveAndEnabled &&
                (!island || candidate.shorelineRadius > island.shorelineRadius)) island = candidate;
        if (!island) throw new System.InvalidOperationException("Sunset Island was not found.");
        var ground = island.GetComponent<MeshCollider>();
        if (!ground || !ground.sharedMesh) throw new System.InvalidOperationException("The island has no mesh collider.");
        return ground;
    }

    static Scene Loaded(string path)
    {
        var scene = SceneManager.GetSceneByPath(path);
        return scene.isLoaded ? scene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }

    static GameObject Root(Scene scene)
    {
        foreach (var existing in scene.GetRootGameObjects())
            if (existing.name == RootName) return existing;
        var created = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(created, scene);
        return created;
    }

    public static string Place()
    {
        if (Application.isPlaying) return "Stop Play Mode first.";
        ImportModels();
        var plant = Material("Meadow Plants", new Color(1f, 1f, 1f), .85f);
        var rock = Material("Meadow Rocks", new Color(.92f, .9f, .86f), 0f);
        var kinds = LoadKinds();
        var environment = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if (!environment.isLoaded) environment = EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity", OpenSceneMode.Additive);
        ProceduralIsland island = null;
        foreach (var candidate in Object.FindObjectsByType<ProceduralIsland>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (candidate && candidate.gameObject.scene == environment && candidate.isActiveAndEnabled &&
                (!island || candidate.shorelineRadius > island.shorelineRadius)) island = candidate;
        if (!island) return "Sunset Island was not found.";
        var ground = island.GetComponent<MeshCollider>();
        if (!ground || !ground.sharedMesh) return "The island has no mesh collider.";
        var ocean = Object.FindFirstObjectByType<OceanWorld>();
        float beachTop = (ocean ? ocean.oceanHeight : 0f) + island.beachHeight + 2.5f;
        var enemies = SceneManager.GetSceneByPath(ScenePath);
        if (!enemies.isLoaded) enemies = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        ClearOld(enemies);
        if (AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder);
        Directory.CreateDirectory(MeshFolder);
        AssetDatabase.Refresh();

        var chunks = new Dictionary<Vector2Int, Chunk>();
        int plants = 0, pebbles = 0, medium = 0, large = 0;
        var parent = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(parent, enemies);
        var rocks = new GameObject("Rocks");
        rocks.transform.SetParent(parent.transform, false);
        Bounds area = ground.bounds;
        float top = area.max.y + 40f;
        for (float z = area.min.z; z <= area.max.z; z += 8f)
        for (float x = area.min.x; x <= area.max.x; x += 8f)
        {
            int hash = Hash(x, z) & int.MaxValue;
            if (!Cast(ground, x, z, top, out var hit) || hit.point.y < beachTop || hit.normal.y < .62f) continue;
            var roll = new System.Random(hash);
            if (hash % 73 == 0 && large < 70)
            {
                DropRock(rocks.transform, kinds.large[roll.Next(kinds.large.Count)], hit, 2.1f + (float)roll.NextDouble() * 1.6f, false);
                large++;
                continue;
            }
            if (hash % 29 == 0 && medium < 220)
            {
                DropRock(rocks.transform, kinds.medium[roll.Next(kinds.medium.Count)], hit, .85f + (float)roll.NextDouble() * .7f, true);
                medium++;
            }
            if (hash % 5 == 0 && pebbles < 3500)
            {
                int count = 3 + roll.Next(4);
                for (int i = 0; i < count && pebbles < 3500; i++)
                {
                    Vector3 at = hit.point + new Vector3((float)roll.NextDouble() - .5f, 0f, (float)roll.NextDouble() - .5f) * 3.2f;
                    if (!Cast(ground, at.x, at.z, top, out var pebble) || pebble.point.y < beachTop) continue;
                    AddProp(chunks, kinds.pebble[roll.Next(kinds.pebble.Count)], pebble, .18f + (float)roll.NextDouble() * .22f, (float)roll.NextDouble() * 360f);
                    pebbles++;
                }
            }
            if (hash % 3 != 0 || plants >= 7000) continue;
            var choice = roll.Next(100);
            var prop = choice < 48 ? kinds.grass[roll.Next(kinds.grass.Count)] :
                choice < 72 ? kinds.flower[roll.Next(kinds.flower.Count)] :
                choice < 90 ? kinds.bush[roll.Next(kinds.bush.Count)] :
                kinds.mushroom[roll.Next(kinds.mushroom.Count)];
            float height = prop.name.Contains("large") || prop.name.Contains("bush") ? .85f + (float)roll.NextDouble() * .45f :
                prop.name.Contains("flower") ? .42f + (float)roll.NextDouble() * .22f : .5f + (float)roll.NextDouble() * .35f;
            AddProp(chunks, prop, hit, height, (float)roll.NextDouble() * 360f);
            plants++;
        }
        int written = 0;
        var propRoot = new GameObject("Plants and pebbles");
        propRoot.transform.SetParent(parent.transform, false);
        foreach (var pair in chunks)
        {
            if (pair.Value.vertices.Count == 0) continue;
            var mesh = new Mesh { name = "Meadow " + pair.Key.x + " " + pair.Key.y, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(pair.Value.vertices);
            mesh.SetNormals(pair.Value.normals);
            mesh.SetColors(pair.Value.colors);
            mesh.SetTriangles(pair.Value.triangles, 0);
            mesh.RecalculateBounds();
            string path = MeshFolder + "/Meadow_" + pair.Key.x + "_" + pair.Key.y + ".asset";
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(mesh.name);
            go.transform.SetParent(propRoot.transform, false);
            go.transform.position = new Vector3(pair.Key.x * 48f, 0f, pair.Key.y * 48f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = plant;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            written++;
        }
        EditorSceneManager.MarkSceneDirty(enemies);
        EditorSceneManager.SaveScene(enemies);
        AssetDatabase.SaveAssets();
        return "plants=" + plants + " pebbles=" + pebbles + " medium=" + medium + " large=" + large + " chunks=" + written;
    }

    sealed class Chunk
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<int> triangles = new List<int>();
    }
    sealed class Prop
    {
        public string name;
        public Mesh mesh;
        public Color tint;
    }
    sealed class Library
    {
        public List<Prop> grass, flower, bush, mushroom, pebble, medium, large;
    }

    static void ImportModels()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelFolder }))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.globalScale = 1f;
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }
    }

    static Material Material(string name, Color tint, float wind)
    {
        var shader = Shader.Find("Mavis/MeadowProp");
        string path = "Assets/Game/Prefabs/Environment/MeadowDressing/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_Color", tint);
        material.SetFloat("_Wind", wind);
        return material;
    }

    static Library LoadKinds()
    {
        var library = new Library
        {
            grass = new List<Prop>(), flower = new List<Prop>(), bush = new List<Prop>(),
            mushroom = new List<Prop>(), pebble = new List<Prop>(), medium = new List<Prop>(), large = new List<Prop>()
        };
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelFolder }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            var mesh = asset.GetComponentInChildren<MeshFilter>()?.sharedMesh;
            if (!mesh) continue;
            var prop = new Prop { name = asset.name, mesh = mesh, tint = Tint(asset.name) };
            if (asset.name.StartsWith("grass")) library.grass.Add(prop);
            else if (asset.name.StartsWith("flower")) library.flower.Add(prop);
            else if (asset.name.StartsWith("plant")) library.bush.Add(prop);
            else if (asset.name.StartsWith("mushroom")) library.mushroom.Add(prop);
            else if (asset.name.Contains("small")) library.pebble.Add(prop);
            else if (asset.name.Contains("large")) library.large.Add(prop);
            else library.medium.Add(prop);
        }
        if (library.grass.Count == 0 || library.large.Count == 0) throw new System.InvalidOperationException("Meadow models did not import.");
        return library;
    }

    static Color Tint(string name)
    {
        if (name.Contains("flower_red")) return new Color(.82f, .22f, .16f);
        if (name.Contains("flower_yellow")) return new Color(.95f, .78f, .18f);
        if (name.Contains("flower_purple")) return new Color(.62f, .32f, .78f);
        if (name.Contains("mushroom_red")) return new Color(.78f, .18f, .14f);
        if (name.Contains("mushroom")) return new Color(.72f, .58f, .34f);
        if (name.Contains("rock") || name.Contains("stone")) return new Color(.55f, .52f, .47f);
        if (name.Contains("bush")) return new Color(.22f, .42f, .16f);
        return new Color(.34f, .58f, .2f);
    }

    static void AddProp(Dictionary<Vector2Int, Chunk> chunks, Prop prop, RaycastHit hit, float height, float yaw)
    {
        var mesh = prop.mesh;
        var sourceVertices = mesh.vertices;
        var sourceNormals = mesh.normals;
        var sourceColors = mesh.colors;
        var sourceTriangles = mesh.triangles;
        bool painted = sourceColors != null && sourceColors.Length == sourceVertices.Length;
        float scale = height / Mathf.Max(.01f, mesh.bounds.size.y);
        var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, yaw, 0f);
        Vector3 position = hit.point + hit.normal * (-mesh.bounds.min.y * scale);
        var matrix = Matrix4x4.TRS(position, rotation, Vector3.one * scale);
        var cell = new Vector2Int(Mathf.FloorToInt(position.x / 48f), Mathf.FloorToInt(position.z / 48f));
        if (!chunks.TryGetValue(cell, out var chunk)) chunks[cell] = chunk = new Chunk();
        var origin = new Vector3(cell.x * 48f, 0f, cell.y * 48f);
        int offset = chunk.vertices.Count;
        for (int i = 0; i < sourceVertices.Length; i++)
        {
            chunk.vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[i]) - origin);
            chunk.normals.Add(matrix.MultiplyVector(sourceNormals[i]).normalized);
            chunk.colors.Add(painted ? sourceColors[i] : prop.tint);
        }
        foreach (int index in sourceTriangles) chunk.triangles.Add(offset + index);
    }

    static void DropRock(Transform parent, Prop prop, RaycastHit hit, float height, bool convex)
    {
        float scale = height / Mathf.Max(.01f, prop.mesh.bounds.size.y);
        var go = new GameObject(prop.name);
        go.transform.SetParent(parent, false);
        var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, Hash(hit.point.x, hit.point.z) % 360, 0f);
        go.transform.SetPositionAndRotation(hit.point + hit.normal * (-prop.mesh.bounds.min.y * scale), rotation);
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = prop.mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/MeadowDressing/Meadow Rocks.mat");
        renderer.shadowCastingMode = ShadowCastingMode.On;
        var collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = prop.mesh;
        collider.convex = convex;
    }

    static bool Cast(MeshCollider ground, float x, float z, float top, out RaycastHit hit)
    {
        return ground.Raycast(new Ray(new Vector3(x, top, z), Vector3.down), out hit, top - ground.bounds.min.y + 20f);
    }

    static int Hash(float x, float z)
    {
        int ix = Mathf.RoundToInt(x * 10f);
        int iz = Mathf.RoundToInt(z * 10f);
        unchecked { return ((ix * 73856093) ^ (iz * 19349663)) & int.MaxValue; }
    }

    static void ClearOld(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == RootName) Object.DestroyImmediate(root);
    }
}
