using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildSecondIslandCloudSea
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    const string RootName = "Second Island - Cloud Sea";
    const string Art = "Assets/Game/Art/SecondIslandCloudSea";
    const string Details = "Assets/Resources/SecondIslandRealism";
    const string Evidence = ".codex/model-refinement/island2";
    static readonly (Vector3 site, Vector3 direction)[] Overlooks =
    {
        (new Vector3(1010, 0, 1610), Vector3.right),
        (new Vector3(-448, 0, 2328), new Vector3(-.35f, 0, .94f)),
        (new Vector3(180, 0, 830), new Vector3(.32f, 0, -.95f))
    };

    public static object Remove()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded || scene.isDirty) throw new InvalidOperationException("Open a saved Second Island scene");
        var root = scene.GetRootGameObjects().SingleOrDefault(go => go.name == RootName);
        if (!root) return new { removed = false };
        Undo.DestroyObjectImmediate(root);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Second Island save failed");
        return new { removed = true };
    }

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded || scene.isDirty) throw new InvalidOperationException("Open a saved Second Island scene");
        Directory.CreateDirectory(Evidence);
        if (!File.Exists(Evidence + "/Second Island.before.unity"))
            File.Copy(ScenePath, Evidence + "/Second Island.before.unity");
        var ground = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MarshmallowIslandExperience>(true))
            .First().surface.GetComponent<Collider>();
        var cliff = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/SecondIslandCloudSea/ChalkBluff.glb");
        var shrub = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/SecondIslandCloudSea/HighlandShrub.glb");
        var cloudShader = Shader.Find("Dark Brine/Second Island/Cloud Sea");
        var trailShader = Shader.Find("Dark Brine/Second Island/Gravel Trail");
        if (!cliff || !shrub || !cloudShader || !trailShader || ShaderUtil.ShaderHasError(cloudShader) || ShaderUtil.ShaderHasError(trailShader))
            throw new InvalidOperationException("Resolve cliff import or cloud shader compilation");

        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Game/Art", "SecondIslandCloudSea");
        var cloud = MaterialAsset("Cloud Sea", cloudShader);
        cloud.SetTexture("_BillowField", BillowField());
        EditorUtility.SetDirty(cloud);
        var path = MaterialAsset("Worn lavender gravel", trailShader);
        path.shader = trailShader;
        path.SetColor("_BaseColor", new Color(.84f, .73f, .78f));
        path.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Island/Materials/rocky_terrain_4k/textures/rocky_terrain_diff_4k.jpg"));
        var post = MaterialAsset("Weathered trail wood", Shader.Find("Universal Render Pipeline/Lit"));
        post.SetColor("_BaseColor", new Color(.30f, .19f, .135f));
        post.SetFloat("_Smoothness", .22f);
        EditorUtility.SetDirty(path);
        EditorUtility.SetDirty(post);

        var previous = scene.GetRootGameObjects().FirstOrDefault(go => go.name == RootName);
        var root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.SetActive(false);
        try
        {
            var layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = "Cloud bed below the island";
            layer.transform.SetParent(root.transform);
            layer.transform.position = new Vector3(ground.bounds.center.x, 2122, ground.bounds.center.z);
            layer.transform.localScale = new Vector3(14200, 334, 14200);
            UnityEngine.Object.DestroyImmediate(layer.GetComponent<Collider>());
            var cloudRenderer = layer.GetComponent<MeshRenderer>();
            cloudRenderer.sharedMaterial = cloud;
            cloudRenderer.shadowCastingMode = ShadowCastingMode.Off;
            cloudRenderer.receiveShadows = false;
            cloudRenderer.lightProbeUsage = LightProbeUsage.Off;
            cloudRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            // The world-space volume belongs to the visible island, not reflection probes.
            layer.layer = LayerMask.NameToLayer("Ignore Raycast");

            var terraces = Group(root.transform, "Cliff lookouts");
            var billows = Group(root.transform, "Low drifting cloud banks");
            var cloudSource = scene.GetRootGameObjects().First(go => go.name == "Small Cotton Candy Clouds")
                .GetComponentsInChildren<MeshFilter>(true).First(filter => filter.sharedMesh);
            var bankMaterial = MaterialAsset("Sunlit cloud banks", cloudSource.GetComponent<Renderer>().sharedMaterial.shader);
            bankMaterial.CopyPropertiesFromMaterial(cloudSource.GetComponent<Renderer>().sharedMaterial);
            bankMaterial.SetColor("_Strawberry", new Color(.94f, .86f, .96f));
            bankMaterial.SetColor("_Lavender", new Color(.77f, .75f, .88f));
            EditorUtility.SetDirty(bankMaterial);
            int index = 0;
            foreach (var (point, outward) in Overlooks)
            {
                var heading = outward.normalized;
                var right = Vector3.Cross(Vector3.up, heading);
                string name = new[] { "East", "North", "South" }[index];
                var overlook = Group(terraces, name + " outlook");
                Trail(overlook, ground, path, name + " gravel approach", 2.7f,
                    point - heading * 38 + right * 8, point - heading * 20 + right * 3,
                    point - heading * 9, point + heading * 4);
                Prop(overlook, ground, "painted_wooden_bench", point + heading * 8 + right * 5.5f, heading, 1.45f);
                Prop(overlook, ground, "wooden_lantern_01", point + heading * 7 + right * 4.5f, heading, 1.38f);
                Prop(overlook, ground, "tree_stump_01", point - heading * 10 - right * 5, heading, 1.75f);
                Place(overlook, ground, shrub, point + heading * 8 - right * 5, Quaternion.Euler(0, index * 81, 0), 2.25f);
                for (int side = -1; side <= 1; side += 2)
                {
                    var rail = point + heading * 8 + right * (4.6f * side);
                    Guard(overlook, ground, post, rail - right * 1.6f, rail + right * 1.6f);
                    // Place the exposed cliffs below the walkable rim as a continuation
                    // of the terrain silhouette, with a clear channel through the middle.
                    var wall = point + heading * (30 + 16 * index) + right * (side * (34 + index * 12));
                    if (Ground(ground, wall, out var hit))
                        Cliff(overlook, cliff, hit.point, heading, side, index);
                    var bank = Group(billows, name + " rolling bank " + side);
                    bank.position = new Vector3(point.x, 2266 + side * 8, point.z) +
                        heading * (250 + index * 45 + side * 32) + right * (110 * side);
                    bank.rotation = Quaternion.Euler(0, 36 + index * 94 + side * 31, 0);
                    var mesh = cloudSource.sharedMesh;
                    bank.localScale = new Vector3(224 + index * 18, 82 + side * 12, 139 + index * 23);
                    bank.localScale = new Vector3(bank.localScale.x / mesh.bounds.size.x,
                        bank.localScale.y / mesh.bounds.size.y, bank.localScale.z / mesh.bounds.size.z);
                    bank.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var bankRenderer = bank.gameObject.AddComponent<MeshRenderer>();
                    bankRenderer.sharedMaterial = bankMaterial;
                    bankRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                index++;
            }
            billows.gameObject.AddComponent<MarshmallowCloudMotion>().driftDistance = 17;

            var cottage = Group(root.transform, "Cottage yard");
            Trail(cottage, ground, path, "Cottage lane", 3.2f,
                new Vector3(143, 0, 1979), new Vector3(132, 0, 1972),
                new Vector3(119, 0, 1965), new Vector3(114, 0, 1952), new Vector3(114, 0, 1935),
                new Vector3(107, 0, 1925), new Vector3(98.6f, 0, 1925));
            Prop(cottage, ground, "wooden_crate_01", new Vector3(115, 0, 1948), Vector3.forward, 1.38f);
            Prop(cottage, ground, "wooden_crate_01", new Vector3(116.6f, 0, 1947), Vector3.right, .84f);
            Prop(cottage, ground, "wooden_lantern_01", new Vector3(111, 0, 1957), Vector3.right, 1.55f);
            Prop(cottage, ground, "painted_wooden_bench", new Vector3(121, 0, 1953), Vector3.right, 1.28f);
            Prop(cottage, ground, "rock_moss_set_01", new Vector3(127, 0, 1958), Vector3.right, 1.02f);
            Prop(cottage, ground, "rock_moss_set_01", new Vector3(126, 0, 1968), Vector3.forward, 1.13f);
            Place(cottage, ground, shrub, new Vector3(111, 0, 1962), Quaternion.Euler(0, 28, 0), 2.6f);
            Place(cottage, ground, shrub, new Vector3(113, 0, 1941), Quaternion.Euler(0, 74, 0), 2.15f);
            Place(cottage, ground, shrub, new Vector3(104, 0, 1926), Quaternion.Euler(0, 143, 0), 1.84f);

            var camp = Group(root.transform, "Camp approach");
            Trail(camp, ground, path, "Supply path", 2.5f, new Vector3(-113, 0, 1691),
                new Vector3(-101, 0, 1696), new Vector3(-93, 0, 1703), new Vector3(-84, 0, 1710));
            Prop(camp, ground, "wooden_crate_01", new Vector3(-91, 0, 1709), Vector3.left, 1.08f);
            Prop(camp, ground, "wooden_lantern_01", new Vector3(-96, 0, 1704), Vector3.left, 1.37f);
            Prop(camp, ground, "rock_moss_set_01", new Vector3(-102, 0, 1702), Vector3.forward, 1.24f);
            Place(camp, ground, shrub, new Vector3(-105, 0, 1698), Quaternion.Euler(0, 105, 0), 1.85f);

            if (previous) UnityEngine.Object.DestroyImmediate(previous);
            root.SetActive(true);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Second Island save failed");
            return new { outlooks = Overlooks.Length, renderers = root.GetComponentsInChildren<Renderer>().Length,
                colliders = root.GetComponentsInChildren<Collider>().Length };
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(root);
            throw;
        }
    }

    static Material MaterialAsset(string name, Shader shader)
    {
        string path = Art + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Texture2D BillowField()
    {
        string path = Art + "/Billow Field.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;
        const int size = 512;
        var random = new System.Random(8623);
        int[] periods = { 13, 41, 103 };
        float[] weights = { .54f, .32f, .14f };
        var noise = new float[size * size];
        for (int layer = 0; layer < periods.Length; layer++)
        {
            int period = periods[layer];
            var values = new float[period * period];
            for (int i = 0; i < values.Length; i++) values[i] = (float)random.NextDouble();
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x * period / (float)size;
                float py = y * period / (float)size;
                int x0 = (int)px;
                int y0 = (int)py;
                int x1 = (x0 + 1) % period;
                int y1 = (y0 + 1) % period;
                float u = Mathf.SmoothStep(0, 1, px - x0);
                float v = Mathf.SmoothStep(0, 1, py - y0);
                noise[y * size + x] += weights[layer] * Mathf.Lerp(
                    Mathf.Lerp(values[y0 * period + x0], values[y0 * period + x1], u),
                    Mathf.Lerp(values[y1 * period + x0], values[y1 * period + x1], u), v);
            }
        }
        var pixels = noise.Select(value => (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255), 0, 255)).ToArray();
        var field = new Texture2D(size, size, TextureFormat.R8, false, true)
        { name = "Billow Field", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        field.SetPixelData(pixels, 0);
        field.Apply(false, true);
        AssetDatabase.CreateAsset(field, path);
        return field;
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static bool Ground(Collider ground, Vector3 site, out RaycastHit hit)
    {
        var top = new Vector3(site.x, ground.bounds.max.y + 20, site.z);
        return ground.Raycast(new Ray(top, Vector3.down), out hit, ground.bounds.size.y + 40);
    }

    static Bounds ModelBounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers.First().bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static void Prop(Transform parent, Collider ground, string id, Vector3 site, Vector3 forward, float height)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Details + "/" + id + ".prefab");
        Place(parent, ground, prefab, site, Quaternion.LookRotation(forward), height);
    }

    static void Place(Transform parent, Collider ground, GameObject prefab, Vector3 site, Quaternion rotation, float height)
    {
        if (!Ground(ground, site, out var hit) || hit.normal.y < .67f) return;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        model.transform.SetPositionAndRotation(hit.point, rotation);
        model.transform.localScale *= height / ModelBounds(model).size.y;
        var bounds = ModelBounds(model);
        model.transform.position += hit.point - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - Vector3.up * .13f;
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        var lod = model.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(.014f, renderers) });
        lod.RecalculateBounds();
    }

    static void Cliff(Transform parent, GameObject source, Vector3 top, Vector3 outward, int side, int index)
    {
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        model.name = "Chalk bluff " + (side < 0 ? "west" : "east");
        model.transform.SetPositionAndRotation(top, Quaternion.LookRotation(outward) * Quaternion.Euler(0, side * 18, 0));
        model.transform.localScale = new Vector3(5.1f + index * .8f, 5.7f + index, 5.4f + index);
        var bounds = ModelBounds(model);
        model.transform.position += top - new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) - Vector3.up * 1.3f;
        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void Guard(Transform parent, Collider ground, Material wood, Vector3 a, Vector3 b)
    {
        if (!Ground(ground, a, out var ha) || !Ground(ground, b, out var hb)) return;
        a = ha.point;
        b = hb.point;
        Beam(parent, wood, a - Vector3.up * .2f, a + Vector3.up * 1.35f, .23f);
        Beam(parent, wood, b - Vector3.up * .2f, b + Vector3.up * 1.35f, .23f);
        Beam(parent, wood, a + Vector3.up * 1.06f, b + Vector3.up * 1.06f, .18f);
    }

    static void Beam(Transform parent, Material wood, Vector3 a, Vector3 b, float width)
    {
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Weathered trail rail";
        beam.transform.SetParent(parent);
        beam.transform.SetPositionAndRotation((a + b) / 2, Quaternion.LookRotation(b - a,
            Mathf.Abs(Vector3.Dot((b - a).normalized, Vector3.up)) > .98f ? Vector3.forward : Vector3.up));
        beam.transform.localScale = new Vector3(width, width, Vector3.Distance(a, b));
        beam.GetComponent<Renderer>().sharedMaterial = wood;
        UnityEngine.Object.DestroyImmediate(beam.GetComponent<Collider>());
    }

    static void Trail(Transform parent, Collider ground, Material material, string name, float width, params Vector3[] points)
    {
        const int columns = 7;
        var path = new List<Vector3>();
        for (int leg = 0; leg < points.Length - 1; leg++)
        {
            var a = points[Mathf.Max(0, leg - 1)];
            var b = points[leg];
            var c = points[leg + 1];
            var d = points[Mathf.Min(points.Length - 1, leg + 2)];
            int samples = Mathf.CeilToInt(Vector3.Distance(b, c) * 2);
            for (int step = 0; step < samples; step++)
            {
                float t = (float)step / samples;
                path.Add(.5f * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t +
                    (-a + 3 * b - 3 * c + d) * t * t * t));
            }
        }
        path.Add(points.Last());
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var edges = new List<Vector2>();
        var triangles = new List<int>();
        var valid = new List<bool>();
        for (int row = 0; row < path.Count; row++)
        {
            var heading = path[Mathf.Min(path.Count - 1, row + 1)] - path[Mathf.Max(0, row - 1)];
            var side = Vector3.Cross(Vector3.up, heading.normalized);
            float variedWidth = width * (.86f + .14f * Mathf.Sin(row * .13f));
            for (int col = 0; col < columns; col++)
            {
                var p = path[row] + side * ((col / (columns - 1f) - .5f) * variedWidth);
                bool hasGround = Ground(ground, p, out var hit) && hit.normal.y > .55f;
                vertices.Add(hasGround ? hit.point + Vector3.up * .18f : p);
                uv.Add(new Vector2(p.x, p.z) / 3.4f);
                edges.Add(new Vector2(col / (columns - 1f), row / (path.Count - 1f)));
                valid.Add(hasGround);
                if (row == 0 || col == 0) continue;
                int i = row * columns + col;
                int a = i - columns - 1;
                int b = i - columns;
                if (valid[a] && valid[b] && valid[i - 1] && valid[i])
                    triangles.AddRange(new[] { a, i - 1, b, b, i - 1, i });
            }
        }
        if (triangles.Count == 0) throw new InvalidOperationException("No ground for " + name);
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetUVs(1, edges);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        string assetPath = Art + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (existing)
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existing;
        }
        else AssetDatabase.CreateAsset(mesh, assetPath);
        var target = Group(parent, name);
        target.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        target.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }
}
