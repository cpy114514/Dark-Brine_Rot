using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BakeIslandRealism
{
    const string ScenePath = "Assets/Scenes/First Island/Environment.unity";
    const string Output = "Assets/Generated/Island1Realism";
    const string Previous = "Assets/Generated/IslandEcology074";
    const string Candidates = "Assets/Generated/NaturalGrassScatter068/Scatter.asset";
    const string Report = ".codex/island1-realism";
    const string SettingsPath = Output + "/Bake Settings.asset";

    [Serializable]
    public sealed class Options
    {
        public int seed = 218911;
        public float meadowRetention = .76f;
        public float gladeRetention = .05f;
        public float grassPatchMetres = 27f;
        public float minGrassNormalY = .76f;
        public float wornTrailHalfWidth = 1.6f;
        public float detailSpacing = 14f;
        public int uncannyPocketLimit = 19;
        public float coastlineBandMetres = 32f;
    }

    sealed class Habitat
    {
        public const int Size = 512;
        public readonly MeshCollider ground;
        public readonly Vector4 bounds;
        public readonly float sea;
        public readonly float beach;
        public readonly Options options;
        public readonly IslandEcologySettings protection;
        public readonly Color32[] pixels = new Color32[Size * Size];
        readonly float[] heights = new float[Size * Size];
        readonly float[] shoreDistances = new float[Size * Size];
        readonly Vector3[] trees; // world XZ and measured canopy radius
        readonly Vector2[] rocks;

        public Habitat(ProceduralIsland island, IslandEcologySettings protection, Options options)
        {
            ground = island.GetComponent<MeshCollider>();
            var b = ground.bounds;
            bounds = new Vector4(b.min.x, b.min.z, b.size.x, b.size.z);
            var material = island.GetComponent<Renderer>().sharedMaterial;
            sea = material.GetFloat("_SeaLevel");
            beach = material.GetFloat("_BeachEnabled") > .5f ? island.beachHeight : 4.6f;
            this.options = options;
            this.protection = protection;
            var scene = island.gameObject.scene;
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.scene == scene && r.TryGetComponent<MeshFilter>(out var f) && f.sharedMesh);
            var stands = new List<Vector3>();
            foreach (var renderer in renderers.Where(r => r.bounds.size.y > 6 && IsNamed(r, "tree")))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter.sharedMesh;
                // Imported trees often have offset pivots and asymmetric crowns.
                // Derive the trunk from the lowest geometry rather than the canopy centre.
                if (filter.name.Contains("geometry_nodes") || filter.name.Contains("LOD1") ||
                    filter.name.Contains("Leaves") || filter.name.Contains("Branches")) continue;
                var bottom = mesh.vertices.Where(v => v.y < mesh.bounds.min.y + mesh.bounds.size.y * .045f).ToArray();
                if (bottom.Length == 0) continue;
                var local = bottom.Aggregate(Vector3.zero, (a, b) => a + b) / bottom.Length;
                var root = filter.transform.TransformPoint(local);
                if (!ground.Raycast(new Ray(new Vector3(root.x, b.max.y + 2, root.z), Vector3.down), out var hit, b.size.y + 4) ||
                    hit.point.y < sea + beach || Mathf.Abs(root.y - hit.point.y) > 7) continue;
                var xy = Xz(root);
                var group = renderer.GetComponentInParent<LODGroup>();
                float radius = Mathf.Clamp(Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.z) * .76f, 4, 35);
                if (group)
                {
                    var crown = group.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                    if (crown.Length > 0) radius = Mathf.Clamp(crown.Max(r => Mathf.Max(r.bounds.extents.x, r.bounds.extents.z)) * .76f, 4, 35);
                }
                if (!stands.Any(t => Vector2.Distance(new Vector2(t.x, t.y), xy) < 4))
                    stands.Add(new Vector3(xy.x, xy.y, radius));
            }
            trees = stands.ToArray();
            rocks = renderers.Where(r => r.bounds.size.y > .6f && (IsNamed(r, "rock") || IsNamed(r, "boulder")))
                .Select(r => Xz(r.bounds.center)).Distinct().ToArray();

            var normals = new float[pixels.Length];
            for (int z = 0; z < Size; z++)
            for (int x = 0; x < Size; x++)
            {
                int i = z * Size + x;
                if (Surface(World(x, z), out var hit))
                {
                    heights[i] = hit.point.y;
                    normals[i] = hit.normal.y;
                }
                else heights[i] = sea - 1;
                shoreDistances[i] = heights[i] <= sea + .65f ? 0 : float.PositiveInfinity;
            }
            Sweep(false);
            Sweep(true);

            for (int z = 0; z < Size; z++)
            for (int x = 0; x < Size; x++)
            {
                int i = z * Size + x;
                if (heights[i] <= sea) continue;
                Vector2 p = World(x, z);
                float moisture = Moisture(p);
                float habitat = 0.69f * Noise(p, options.grassPatchMetres, 0) + 0.31f * Noise(p, 7f, 1);
                float shelter = TreeShelter(p);
                float rock = RockShelter(p);
                float exposure = 1 - Ramp(options.coastlineBandMetres * .4f, options.coastlineBandMetres * 2.8f, shoreDistances[i]);
                float turf = Ramp(.31f, .68f, habitat + (moisture - .5f) * .26f - shelter * .11f - exposure * .15f);
                turf *= Ramp(.73f, .92f, normals[i]) * Ramp(beach - 2, beach + 6, heights[i] - sea);
                turf *= 1 - rock * .56f;
                float centre = TrailDistance(p, out float shoulder);
                float wear = (1 - Ramp(options.wornTrailHalfWidth * .35f, options.wornTrailHalfWidth + .65f,
                    centre + (Noise(p, 6, 3) - .5f) * 1.2f)) * .91f;
                wear = Mathf.Max(wear, (1 - Ramp(0, 1, (shoulder + 3.4f) / 4.2f)) * .18f);
                float seams = Ramp(.49f, .81f, .57f * Noise(p, 31, 7) + .43f * Noise(p, 6.5f, 8));
                float stone = Mathf.Max((1 - Ramp(.69f, .92f, normals[i])) * (.46f + .54f * seams),
                    rock * (.26f + .55f * seams));
                stone = Mathf.Max(stone, exposure * Ramp(.61f, .85f, Noise(p, 17, 9)) * .64f);
                stone *= 1 - wear * .7f;
                float damp = (1 - Ramp(1.0f, 4.2f + moisture * 2.1f, heights[i] - sea)) *
                    (1 - Ramp(2, options.coastlineBandMetres, shoreDistances[i])) * (1 - stone * .6f);
                pixels[i] = new Color(turf, wear, stone, damp);
            }
        }

        // Two-pass chamfer distance, in world metres. Sea level seeds the mask
        // even where the terrain mesh continues underwater.
        void Sweep(bool reverse)
        {
            float dx = bounds.z / Size, dz = bounds.w / Size;
            float diagonal = Mathf.Sqrt(dx * dx + dz * dz);
            int step = reverse ? -1 : 1;
            for (int row = 0; row < Size; row++)
            for (int col = 0; col < Size; col++)
            {
                int x = reverse ? Size - 1 - col : col;
                int z = reverse ? Size - 1 - row : row;
                int i = z * Size + x;
                float best = shoreDistances[i];
                if (col > 0) best = Mathf.Min(best, shoreDistances[i - step] + dx);
                if (row > 0)
                {
                    best = Mathf.Min(best, shoreDistances[i - step * Size] + dz);
                    if (x > 0) best = Mathf.Min(best, shoreDistances[i - step * Size - 1] + diagonal);
                    if (x < Size - 1) best = Mathf.Min(best, shoreDistances[i - step * Size + 1] + diagonal);
                }
                shoreDistances[i] = best;
            }
        }

        public float TrailDistance(Vector2 point, out float shoulder)
        {
            shoulder = float.PositiveInfinity;
            float centre = float.PositiveInfinity;
            foreach (var trail in protection.corridors)
            for (int i = 1; i < trail.points.Length; i++)
            {
                Vector2 from = trail.points[i - 1], line = trail.points[i] - from;
                float lengthSquared = line.sqrMagnitude;
                if (lengthSquared < .001f) continue;
                float d = Vector2.Distance(point, from + line * Mathf.Clamp01(Vector2.Dot(point - from, line) / lengthSquared));
                centre = Mathf.Min(centre, d);
                shoulder = Mathf.Min(shoulder, d - trail.halfWidth);
            }
            return centre;
        }

        public bool Protected(Vector2 p, float margin)
        {
            TrailDistance(p, out float shoulder);
            return shoulder < margin || protection.clearings.Any(c => (p - c.center).sqrMagnitude < (c.radius + margin) * (c.radius + margin));
        }

        public bool Surface(Vector2 p, out RaycastHit hit)
        {
            var b = ground.bounds;
            return ground.Raycast(new Ray(new Vector3(p.x, b.max.y + 2, p.y), Vector3.down), out hit, b.size.y + 4);
        }

        Vector2 World(int x, int z) => new Vector2(bounds.x + (x + .5f) * bounds.z / Size, bounds.y + (z + .5f) * bounds.w / Size);
        int Texel(Vector2 p) => Mathf.Clamp(Mathf.FloorToInt((p.y - bounds.y) * Size / bounds.w), 0, Size - 1) * Size +
            Mathf.Clamp(Mathf.FloorToInt((p.x - bounds.x) * Size / bounds.z), 0, Size - 1);
        public Color Mask(Vector2 p) => pixels[Texel(p)];
        public float CoastDistance(Vector2 p) => shoreDistances[Texel(p)];
        public float Moisture(Vector2 p) => Noise(p, 38, 4) * .67f + Noise(p, 11, 5) * .33f;
        public float TreeShelter(Vector2 p)
        {
            float shelter = 0;
            foreach (var tree in trees)
                shelter = Mathf.Max(shelter, 1 - Ramp(tree.z * .23f, tree.z * 1.22f,
                    Vector2.Distance(p, new Vector2(tree.x, tree.y))));
            return shelter;
        }
        public float TrunkDistance(Vector2 p)
        {
            float distance = float.PositiveInfinity;
            foreach (var tree in trees) distance = Mathf.Min(distance, Vector2.Distance(p, new Vector2(tree.x, tree.y)));
            return distance;
        }
        public IEnumerable<Vector2> WoodlandCandidates(int species)
        {
            var rng = new System.Random(options.seed + species * 701);
            foreach (var tree in trees)
            for (int i = 0; i < 16; i++)
            {
                float angle = R(rng, 0, 2 * Mathf.PI);
                float distance = R(rng, Mathf.Max(3, tree.z * .26f), tree.z * .95f);
                yield return new Vector2(tree.x + Mathf.Cos(angle) * distance, tree.y + Mathf.Sin(angle) * distance);
            }
        }
        public float RockShelter(Vector2 p) => Proximity(p, rocks, 1.5f, 9);

        public float Noise(Vector2 p, float scale, int band)
        {
            float offsetX = (options.seed & 2047) * .043f + band * 21.17f;
            float offsetY = ((options.seed >> 11) & 2047) * .067f - band * 34.19f;
            return Mathf.PerlinNoise(p.x / scale + offsetX, p.y / scale + offsetY);
        }

        static float Proximity(Vector2 p, Vector2[] positions, float near, float far)
        {
            float squared = far * far;
            foreach (var at in positions) squared = Mathf.Min(squared, (at - p).sqrMagnitude);
            return 1 - Ramp(near, far, Mathf.Sqrt(squared));
        }

        static bool IsNamed(MeshRenderer r, string name)
        {
            if (r.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                r.GetComponent<MeshFilter>().sharedMesh.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            for (var parent = r.transform.parent; parent; parent = parent.parent)
                if (parent.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }

    static Vector2 Xz(Vector3 p) => new Vector2(p.x, p.z);
    static float Ramp(float a, float b, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));
    static float R(System.Random rng, float a = 0, float b = 1) => Mathf.Lerp(a, b, (float)rng.NextDouble());
    static Vector3 Direction(float angle) => new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

    static T Asset<T>(T value, string name) where T : Object
    {
        var path = Output + "/" + name;
        value.name = Path.GetFileNameWithoutExtension(name);
        var old = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!old)
        {
            AssetDatabase.CreateAsset(value, path);
            return value;
        }
        EditorUtility.CopySerialized(value, old);
        EditorUtility.SetDirty(old);
        Object.DestroyImmediate(value);
        return old;
    }

    static Material GrassMaterial(Material source)
    {
        var material = new Material(source) { enableInstancing = true };
        material.SetTexture("_MainTex", Texture2D.whiteTexture);
        material.SetColor("_BaseColor", new Color(.96f, .95f, .87f));
        material.SetFloat("_UseAlphaMap", 0);
        material.SetFloat("_ClipBelowSea", 0);
        material.SetFloat("_BotanicalSurface", 0);
        material.SetFloat("_AuthoredGrass", 1);
        material.SetFloat("_WindBend", .48f);
        material.SetFloat("_SunDesaturation", .35f);
        material.SetFloat("_WindFrequency", 1.25f);
        material.SetFloat("_Cutoff", .1f);
        material.SetFloat("_Cull", 0);
        material.SetVector("_EcologyDistance", new Vector4(177, 225, 0, 0));
        return Asset(material, "Grass - meadow, fescue and sedge.mat");
    }

    sealed class Geometry
    {
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        public int Count => points.Count;
        public float Height(int index) => points[index].y;
        public void Add(Vector3 position, Vector2 texture, Color color)
        {
            points.Add(position);
            uv.Add(texture);
            colors.Add(color);
        }

        public void Triangle(int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(b, d, c);
        }

        public Mesh ToMesh()
        {
            var mesh = new Mesh();
            mesh.SetVertices(points);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    static Mesh Tuft(int type, int lod)
    {
        var rng = new System.Random(95171 + type * 43);
        var g = new Geometry();
        Color[] greens =
        {
            new Color(.228f, .305f, .104f), new Color(.287f, .342f, .127f),
            new Color(.347f, .317f, .147f), new Color(.248f, .297f, .137f)
        };
        Color straw = new Color(.483f, .387f, .192f);
        int blades = lod == 0 ? 20 : lod == 1 ? 9 : 4;
        int segments = lod == 0 ? 4 : lod == 1 ? 3 : 2;
        for (int blade = 0; blade < blades; blade++)
        {
            float angle = blade * 2.399963f + R(rng, -.5f, .5f);
            var outward = Direction(angle);
            var across = Direction(angle + Mathf.PI / 2);
            bool outer = blade % 3 == 1;
            float length = R(rng, .38f, 1.03f) * (type == 0 ? 1.04f : type == 1 ? .64f : type == 2 ? .49f : .56f);
            if (outer) length *= .73f;
            float reach = length * R(rng, .28f, .76f) * (outer ? 1.65f : 1f);
            float width = R(rng, .025f, .065f) * (type == 0 ? 1.10f : type == 3 ? 1.6f : .86f);
            var root = outward * R(rng, .03f, .40f);
            float dry = R(rng, 0, type == 2 ? .71f : .23f);
            if (blade % 9 == 6) dry = .84f;
            var green = Color.Lerp(greens[type] * R(rng, .75f, 1.32f), straw, dry);
            int start = g.Count;
            for (int ring = 0; ring < segments; ring++)
            {
                float t = ring / (float)segments;
                var centre = root + outward * (reach * t * t) + Vector3.up *
                    (length * (t + (outer ? .44f : .13f) * Mathf.Sin(t * Mathf.PI)) * (1 - (outer ? .41f : .12f) * t));
                float radius = width * Mathf.Lerp(.40f, 1, Mathf.Sin(t * Mathf.PI)) * (1 - t * .71f);
                var color = green * Mathf.Lerp(.45f, 1.24f, t);
                color.a = t * Mathf.Lerp(.43f, 1, Mathf.Clamp01(length));
                g.Add(centre - across * radius, new Vector2(0, t), color * new Color(.84f, .88f, .81f, 1));
                g.Add(centre + across * radius, new Vector2(1, t), color);
                if (ring > 0) g.Quad(start + ring * 2 - 2, start + ring * 2 - 1, start + ring * 2, start + ring * 2 + 1);
            }
            var tip = green * 1.30f;
            tip.a = Mathf.Lerp(.43f, 1, Mathf.Clamp01(length));
            g.Add(root + outward * reach + Vector3.up * (length * (outer ? .59f : .88f)), Vector2.one, tip);
            g.Triangle(g.Count - 3, g.Count - 2, g.Count - 1);
        }
        return g.ToMesh();
    }

    static NaturalGrassScatterData Grass(Habitat h, NaturalGrassScatterData source, Material material)
    {
        var data = ScriptableObject.CreateInstance<NaturalGrassScatterData>();
        data.material = material;
        data.meshes = new Mesh[4];
        data.mediumMeshes = new Mesh[4];
        data.farMeshes = new Mesh[4];
        data.mediumTransition = new Vector2(31, 43);
        data.farTransition = new Vector2(63, 83);
        for (int i = 0; i < 4; i++)
        {
            data.meshes[i] = Asset(Tuft(i, 0), $"Tuft {i} - near.asset");
            data.mediumMeshes[i] = Asset(Tuft(i, 1), $"Tuft {i} - middle.asset");
            data.farMeshes[i] = Asset(Tuft(i, 2), $"Tuft {i} - far.asset");
        }
        var roots = data.meshes.Select(m => m.vertices.Where(v => Mathf.Abs(v.y) < .001f).ToArray()).ToArray();
        var rng = new System.Random(h.options.seed);
        var cells = new List<NaturalGrassScatterData.Cell>();
        foreach (var cell in source.cells)
        {
            var positions = new List<Vector4>();
            var normals = new List<Vector3>();
            var variants = new List<byte>();
            var scales = new List<float>();
            for (int i = 0; i < cell.positions.Length; i++)
            {
                var candidate = cell.positions[i];
                var p = new Vector2(candidate.x, candidate.z);
                float chance = R(rng), type = R(rng), yaw = R(rng, 0, 360), size = R(rng, .66f, 1.34f);
                var mask = h.Mask(p);
                float coast = h.CoastDistance(p);
                float distance = h.TrailDistance(p, out float shoulder);
                float density = Mathf.Lerp(h.options.gladeRetention, h.options.meadowRetention, Ramp(.14f, .74f, mask.r));
                density *= (1 - mask.b * .9f) * (1 - mask.g * .92f);
                density *= Mathf.Lerp(.54f, 1, Ramp(13, h.options.coastlineBandMetres * 2.6f, coast));
                if (distance < h.options.wornTrailHalfWidth) continue;
                if (chance > density || !h.Surface(p, out var hit) || hit.normal.y < h.options.minGrassNormalY ||
                    hit.point.y < h.sea + h.beach - .75f) continue;

                bool dry = mask.r < .43f || h.Moisture(p) < .39f;
                byte variant = coast < h.options.coastlineBandMetres * 1.55f ? (byte)3 :
                    dry ? (byte)(type < .67f ? 2 : 1) : (byte)(type < .52f ? 0 : 1);
                bool protectedGround = h.Protected(p, .5f);
                if (protectedGround)
                {
                    variant = 2;
                    size *= .52f + .12f * Ramp(0, 1, shoulder);
                }
                var normal = Vector3.Slerp(Vector3.up, hit.normal, .83f);
                var rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, yaw, 0);
                var origin = hit.point;
                float lift = 0;
                foreach (var root in roots[variant])
                {
                    Vector3 point = origin + rotation * root * size;
                    if (!h.Surface(Xz(point), out var support)) { lift = float.PositiveInfinity; break; }
                    lift = Mathf.Max(lift, point.y - support.point.y);
                }
                if (lift > .18f) continue;
                origin.y -= lift + .026f;
                positions.Add(new Vector4(origin.x, origin.y, origin.z, yaw));
                normals.Add(normal);
                variants.Add(variant);
                scales.Add(size);
            }
            if (positions.Count == 0) continue;
            var bounds = new Bounds((Vector3)positions[0], Vector3.zero);
            foreach (var p in positions) bounds.Encapsulate((Vector3)p);
            bounds.Expand(3.6f);
            cells.Add(new NaturalGrassScatterData.Cell
            {
                bounds = bounds, positions = positions.ToArray(), normals = normals.ToArray(),
                variants = variants.ToArray(), scales = scales.ToArray()
            });
        }
        data.cells = cells.ToArray();
        return Asset(data, "Meadow Scatter.asset");
    }

    static Texture2D HabitatTexture(Habitat h)
    {
        var texture = new Texture2D(Habitat.Size, Habitat.Size, TextureFormat.RGBA32, true, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear
        };
        texture.SetPixels32(h.pixels);
        texture.Apply(true, true);
        return Asset(texture, "Turf, tracks, geology and dampness.asset");
    }

    static Options LoadOptions()
    {
        const string path = "Tools/Island1RealismSettings.json";
        return JsonUtility.FromJson<Options>(File.ReadAllText(path)) ??
            throw new InvalidDataException("Island1RealismSettings.json must contain a settings object.");
    }

    static T InScene<T>(Scene scene) where T : Component =>
        Object.FindObjectsByType<T>(FindObjectsSortMode.None).Single(c => c.gameObject.scene == scene);

    // Authored detail compositions share the renderer type but own a different asset.
    static IslandEcologyRenderer FindDressing(Scene scene) =>
        Object.FindObjectsByType<IslandEcologyRenderer>(FindObjectsSortMode.None).Single(c =>
            c.gameObject.scene == scene && c.data &&
            (AssetDatabase.GetAssetPath(c.data) == Output + "/Woodland and coastal dressing.asset" ||
             AssetDatabase.GetAssetPath(c.data) == Previous + "/Ecology.asset"));

    public static object Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before baking.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded || scene.isDirty) throw new InvalidOperationException("Open a clean Island 1 Environment scene before baking.");
        var island = Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None)
            .Single(i => i.gameObject.scene == scene && i.isActiveAndEnabled);
        var scatter = InScene<NaturalGrassScatter>(scene);
        var renderer = FindDressing(scene);
        var source = AssetDatabase.LoadAssetAtPath<NaturalGrassScatterData>(Candidates);
        var baseEcology = AssetDatabase.LoadAssetAtPath<IslandEcologyData>(Previous + "/Ecology.asset");
        var protection = AssetDatabase.LoadAssetAtPath<IslandEcologySettings>(Previous + "/Settings.asset");
        if (!source || !baseEcology || !protection)
            throw new InvalidOperationException("The original meadow candidates, ecology, and gameplay protection assets are required.");
        var options = LoadOptions();
        if (options.grassPatchMetres < 4 || options.detailSpacing < 8 || options.coastlineBandMetres < 8 ||
            options.meadowRetention < 0 || options.meadowRetention > 1 || options.gladeRetention < 0 ||
            options.gladeRetention > options.meadowRetention || options.uncannyPocketLimit < 0 ||
            options.wornTrailHalfWidth < .8f || options.minGrassNormalY <= 0 || options.minGrassNormalY > 1)
            throw new InvalidOperationException("Check Island1RealismSettings.json for invalid density, spacing, slope, or trail values.");
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets/Generated", "Island1Realism");

        var habitat = new Habitat(island, protection, options);
        var texture = HabitatTexture(habitat);
        var grass = Grass(habitat, source, GrassMaterial(source.material));
        var ecology = Dress(habitat, scene, baseEcology);
        Undo.RecordObjects(new Object[] { island, scatter, renderer }, "Apply Island 1 habitat bake");
        island.SetHabitat(texture, habitat.bounds);
        scatter.data = grass;
        scatter.enabled = false;
        scatter.enabled = true;
        renderer.data = ecology;
        renderer.Rebuild();
        IslandEcologyCollisionBaker.Rebuild(renderer);
        EditorUtility.SetDirty(island);
        EditorUtility.SetDirty(scatter);
        EditorUtility.SetDirty(renderer);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        return Validate();
    }

    static IslandEcologyData.Species GroundSpecies(string name, Mesh near, Mesh far, Material material, float draw)
    {
        return new IslandEcologyData.Species
        {
            name = name, near = Asset(near, name + " - near.asset"), far = Asset(far, name + " - far.asset"),
            material = material, lodStart = draw * .42f, lodEnd = draw * .60f,
            drawDistance = draw, shadowDistance = Mathf.Min(draw * .48f, 45)
        };
    }

    static Material GroundMaterial(string name, Texture2D texture, Texture2D normal, Texture2D surface,
        Color tint, float smoothness, float distance)
    {
        var shader = Shader.Find("Mavis/FoliageWind");
        var mat = new Material(shader) { enableInstancing = true };
        mat.SetTexture("_MainTex", texture ? texture : Texture2D.whiteTexture);
        mat.SetTexture("_BotanicalNormal", normal ? normal : Texture2D.normalTexture);
        mat.SetTexture("_BotanicalSurfaceMap", surface);
        mat.SetFloat("_BotanicalSurface", 1);
        mat.SetFloat("_UseBladeHeight", 1);
        mat.SetFloat("_WindBend", 0);
        mat.SetFloat("_PlayerPushStrength", 0);
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Cutoff", .05f);
        mat.SetFloat("_Cull", 2);
        mat.SetVector("_EcologyDistance", new Vector4(distance * .82f, distance, 0, 0));
        return Asset(mat, name + ".mat");
    }

    // Offset ledges and unequal corner bevels give each stone broken bedding.
    static void Stone(Geometry g, Vector3 centre, Vector3 extents, System.Random rng, int sides, float heading, bool rounded)
    {
        int start = g.Count;
        var angles = Enumerable.Range(0, sides).Select(i => heading + (i + R(rng, -.21f, .21f)) * 2 * Mathf.PI / sides).ToArray();
        var edges = angles.Select(a => Direction(a) * R(rng, .79f, 1.13f)).ToArray();
        float grain = R(rng, .75f, 1.14f);
        int tiers = 4;
        for (int ring = 0; ring < tiers; ring++)
        for (int i = 0; i < sides; i++)
        {
            float size = ring == 0 ? .63f : ring == 1 ? 1f : ring == 2 ? .83f : rounded ? .47f : .68f;
            float y = ring == 0 ? -.13f : ring == 1 ? .15f : ring == 2 ? .68f : 1;
            var radial = edges[i] * size + Vector3.right * ring * .105f + Vector3.forward * ring * .025f;
            Vector3 p = centre + Vector3.Scale(new Vector3(radial.x, y + R(rng, -.075f, .075f), radial.z), extents);
            var tint = new Color(.86f, .87f, .84f) * (grain * (ring == 0 ? .62f : ring == 1 ? .81f : ring == 2 ? 1 : 1.12f));
            g.Add(p, new Vector2(p.x + p.y * .75f, p.z - p.y * .41f) * 1.8f, tint);
            if (ring == 0) continue;
            int a = start + (ring - 1) * sides + i, b = start + (ring - 1) * sides + (i + 1) % sides;
            g.Quad(a, a + sides, b, b + sides);
        }
        int top = start + (tiers - 1) * sides;
        for (int i = 1; i < sides - 1; i++) g.Triangle(top, top + i + 1, top + i);
    }

    static Mesh Stones(int variant, bool far, bool outcrop)
    {
        var rng = new System.Random(72820 + variant * 41);
        var g = new Geometry();
        int count = outcrop ? (far ? 1 : 3) : (far ? 4 : 8);
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.39996f + R(rng, -.3f, .3f);
            float distance = i == 0 ? 0 : R(rng, .22f, .95f);
            var centre = Direction(angle) * (outcrop ? distance * 1.55f : distance);
            float size = outcrop && i == 0 ? .92f : R(rng, .10f, outcrop ? .65f : .29f);
            Stone(g, centre, new Vector3(size * R(rng, 1.1f, 1.9f), size * (outcrop ? .61f : .43f), size * .85f),
                rng, far ? 6 : 9, variant * .69f + (outcrop ? .12f * i : angle), !outcrop);
        }
        return g.ToMesh();
    }

    static void Bough(Geometry g, Vector3[] spine, float[] widths, int sides, System.Random rng)
    {
        int first = g.Count;
        float along = 0;
        var grain = Enumerable.Range(0, sides).Select(_ => R(rng, .58f, 1.12f)).ToArray();
        for (int i = 0; i < spine.Length; i++)
        {
            if (i > 0) along += Vector3.Distance(spine[i], spine[i - 1]);
            var axis = (spine[Mathf.Min(i + 1, spine.Length - 1)] - spine[Mathf.Max(i - 1, 0)]).normalized;
            var side = Vector3.Cross(Vector3.up, axis).normalized;
            var vertical = Vector3.Cross(axis, side);
            for (int j = 0; j <= sides; j++)
            {
                float arc = j * (2 * Mathf.PI / sides);
                float size = widths[i] * (0.90f + grain[j % sides] * .16f);
                Vector3 p = spine[i] + (side * Mathf.Cos(arc) + vertical * Mathf.Sin(arc)) * size;
                var tint = new Color(.93f, .92f, .84f) * (grain[j % sides] * (i == 0 ? .82f : 1));
                g.Add(p, new Vector2(j / (float)sides, along * .94f), tint);
                if (i == 0 || j == sides) continue;
                int a = first + (i - 1) * (sides + 1) + j;
                g.Quad(a, a + 1, a + sides + 1, a + sides + 2);
            }
        }
        for (int cap = 0; cap < 2; cap++)
        {
            int ring = cap == 0 ? first : first + (spine.Length - 1) * (sides + 1);
            int center = g.Count;
            g.Add(spine[cap == 0 ? 0 : spine.Length - 1], new Vector2(.5f, .5f), new Color(.55f, .42f, .28f));
            for (int j = 0; j < sides; j++)
                if (cap == 0) g.Triangle(center, ring + j + 1, ring + j);
                else g.Triangle(center, ring + j, ring + j + 1);
        }
    }

    static Mesh Driftwood(bool far)
    {
        var g = new Geometry();
        var rng = new System.Random(99019);
        Vector3 A(float x, float y, float z) => new Vector3(x, y, z);
        Bough(g, new[] { A(-1.48f,.19f,.08f), A(-.92f,.21f,-.13f), A(-.31f,.23f,-.12f),
            A(.30f,.15f,.06f), A(.87f,.15f,.16f), A(1.54f,.23f,.36f) },
            new[] { .17f,.16f,.14f,.11f,.076f,.017f }, far ? 6 : 11, rng);
        Bough(g, new[] { A(-.97f,.23f,-.14f), A(-.67f,.27f,-.43f), A(-.34f,.32f,-.71f), A(.03f,.27f,-.94f) },
            new[] { .10f,.085f,.054f,.014f }, far ? 5 : 8, rng);
        if (!far)
        {
            Bough(g, new[] { A(-1.27f,.17f,.04f), A(-1.45f,.20f,.30f), A(-1.26f,.25f,.49f), A(-1.16f,.18f,.57f) },
                new[] { .105f,.074f,.044f,.012f }, 7, rng);
            Bough(g, new[] { A(-.10f,.20f,-.10f), A(.15f,.21f,-.32f), A(.34f,.34f,-.46f) },
                new[] { .078f,.041f,.014f }, 6, rng);
        }
        return g.ToMesh();
    }

    static Mesh Shells(bool far)
    {
        var g = new Geometry();
        var rng = new System.Random(14137);
        int ribs = far ? 8 : 18, rings = far ? 3 : 6;
        for (int shell = 0; shell < (far ? 2 : 4); shell++)
        {
            Vector3 center = Direction(shell * 2.39996f) * (shell * .24f);
            float width = R(rng, .075f, .145f);
            int first = g.Count;
            float yaw = R(rng, 0, Mathf.PI * 2);
            var cream = Color.Lerp(new Color(.57f,.47f,.34f), new Color(.85f,.78f,.63f), R(rng));
            for (int i = 0; i <= rings; i++)
            for (int j = 0; j <= ribs; j++)
            {
                float t = i / (float)rings, u = j / (float)ribs;
                float spread = (u - .5f) * 2.48f;
                float rib = Mathf.Cos(u * 15 * Mathf.PI);
                var axis = Direction(yaw + spread);
                var p = center + axis * (width * Mathf.Lerp(.09f, 1, t) * (1 + rib * .055f * t));
                p.y = width * (.41f * Mathf.Sin(t * Mathf.PI * .93f) * Mathf.Cos(spread * .6f) + rib * .065f * t);
                g.Add(p, new Vector2(u,t), cream * (1 + rib * .14f + .09f * Mathf.Sin(t * 17)));
                if (i > 0 && j < ribs)
                {
                    int a = first + (i - 1) * (ribs + 1) + j;
                    g.Quad(a, a + 1, a + ribs + 1, a + ribs + 2);
                }
            }
        }
        return g.ToMesh();
    }

    static Mesh CoppercoilFern(bool far)
    {
        var g = new Geometry();
        var rng = new System.Random(71191);
        int fronds = far ? 4 : 7;
        int sections = far ? 8 : 15;
        for (int i = 0; i < fronds; i++)
        {
            float yaw = i * 2.39996f + R(rng, -.3f, .3f);
            Vector3 outward = Direction(yaw);
            Vector3 sideways = Direction(yaw + Mathf.PI / 2);
            float height = R(rng, .49f, 1.15f);
            Vector3 Stalk(float t)
            {
                float turn = Mathf.Clamp01((t - .63f) / .37f) * Mathf.PI * 1.38f;
                return outward * (.23f * t + .17f * Mathf.Sin(turn)) +
                    sideways * (.06f * t * t) +
                    Vector3.up * (height * Mathf.Min(t, .63f) + .16f * (1 - Mathf.Cos(turn)));
            }
            var spine = Enumerable.Range(0, sections + 1).Select(j => Stalk(j / (float)sections)).ToArray();
            Bough(g, spine, spine.Select((p, j) => Mathf.Lerp(.025f, .006f, j / (float)sections)).ToArray(), far ? 4 : 6, rng);
            int pairs = far ? 4 : 7;
            for (int j = 0; j < pairs; j++)
            for (int side = -1; side <= 1; side += 2)
            {
                float t = .21f + .61f * j / pairs;
                var root = Stalk(t);
                var direction = (sideways * side + outward * .56f + Vector3.up * .41f).normalized;
                float length = R(rng, .22f, .43f) * Mathf.Sin(t * Mathf.PI) * height;
                var width = Vector3.Cross(direction, Vector3.up).normalized * length * .25f;
                var leaf = Color.Lerp(new Color(.22f,.29f,.118f), new Color(.41f,.27f,.13f), Mathf.Pow(t, 1.8f));
                int first = g.Count;
                g.Add(root, new Vector2(.5f,0), leaf * .73f);
                g.Add(root + direction * length * .45f + width, new Vector2(0,.45f), leaf);
                g.Add(root + direction * length * .47f + Vector3.up * length * .15f, new Vector2(.5f,.47f), leaf * 1.12f);
                g.Add(root + direction * length * .45f - width, new Vector2(1,.45f), leaf * .81f);
                g.Add(root + direction * length - Vector3.up * length * .25f, new Vector2(.5f,1), leaf * 1.14f);
                g.Triangle(first, first + 1, first + 2);
                g.Triangle(first, first + 2, first + 3);
                g.Triangle(first + 1, first + 4, first + 2);
                g.Triangle(first + 2, first + 4, first + 3);
            }
        }
        var mesh = g.ToMesh();
        mesh.colors = mesh.colors.Select((color, i) =>
        {
            color.a = Mathf.Clamp01(g.Height(i) * .90f);
            return color;
        }).ToArray();
        return mesh;
    }

    struct Placement
    {
        public Vector4 position;
        public Vector4 normal;
        public Placement(Vector3 p, float yaw, Vector3 up, float scale)
        {
            position = new Vector4(p.x, p.y, p.z, yaw);
            normal = new Vector4(up.x, up.y, up.z, scale);
        }
    }

    static Matrix4x4 Matrix(Placement item) => Matrix4x4.TRS(item.position,
        Quaternion.FromToRotation(Vector3.up, (Vector3)item.normal) * Quaternion.Euler(0, item.position.w, 0),
        Vector3.one * item.normal.w);

    static IslandEcologyData.Cell[] DetailCells(IslandEcologyData.Species species, List<Placement> placements)
    {
        var meshBounds = species.near.bounds;
        meshBounds.Encapsulate(species.far.bounds);
        return placements.GroupBy(p => new Vector2Int(Mathf.FloorToInt(p.position.x / 32), Mathf.FloorToInt(p.position.z / 32)))
            .OrderBy(g => g.Key.x).ThenBy(g => g.Key.y).Select(group =>
            {
                var bounds = new Bounds((Vector3)group.First().position, Vector3.zero);
                foreach (var item in group)
                {
                    var matrix = Matrix(item);
                    Vector3 extents = meshBounds.extents;
                    Vector3 x = matrix.MultiplyVector(Vector3.right * extents.x);
                    Vector3 y = matrix.MultiplyVector(Vector3.up * extents.y);
                    Vector3 z = matrix.MultiplyVector(Vector3.forward * extents.z);
                    var box = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                        Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                    bounds.Encapsulate(new Bounds(matrix.MultiplyPoint3x4(meshBounds.center), box * 2));
                }
                bounds.Expand(.35f);
                return new IslandEcologyData.Cell { bounds = bounds,
                    positions = group.Select(p => p.position).ToArray(), normals = group.Select(p => p.normal).ToArray() };
            }).ToArray();
    }

    static List<Placement> ExistingPlants(Habitat h, IslandEcologyData.Species species, int speciesIndex)
    {
        var plants = species.cells.SelectMany(c => c.positions.Select((p, i) =>
            new Placement(p, p.w, c.normals[i], c.normals[i].w))).ToList();
        if (species.uncanny)
        {
            // Favor forest-edge discoveries, spaced independently of the source cells.
            var pockets = new List<Placement>();
            var ordered = plants.Where(p => h.TreeShelter(new Vector2(p.position.x, p.position.z)) > .14f ||
                h.RockShelter(new Vector2(p.position.x, p.position.z)) > .13f).OrderByDescending(p =>
                    h.Noise(new Vector2(p.position.x, p.position.z), 30, 24 + speciesIndex));
            foreach (var plant in ordered)
            {
                if (pockets.Count >= (h.options.uncannyPocketLimit + 19 - speciesIndex) / 4) break;
                if (pockets.Any(other => ((Vector3)(other.position - plant.position)).sqrMagnitude < 36 * 36)) continue;
                pockets.Add(plant);
            }
            return plants.Where(p => pockets.Any(center => ((Vector3)(center.position - p.position)).sqrMagnitude <= 4.4f * 4.4f))
                .OrderBy(p => h.Noise(new Vector2(p.position.x, p.position.z), 5, 44)).Take(pockets.Count * 4).ToList();
        }
        if (speciesIndex < 9 || speciesIndex > 11) return plants;
        // Quiet the meadow flowers in exposed turf. Retain the denser woodland and lee-side groups.
        return plants.Where(p =>
        {
            var xy = new Vector2(p.position.x, p.position.z);
            var mask = h.Mask(xy);
            return h.Noise(xy, 8, speciesIndex + 39) > Mathf.Lerp(.63f, .39f, mask.r);
        }).ToList();
    }

    static Texture2D RockTexture(string file, TextureImporterType type, bool srgb)
    {
        string path = "Assets/ThirdParty/AmbientCG/Rock030/" + file;
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            throw new FileNotFoundException("Rock 030 map is missing.", path);
        if (importer.textureType != type || importer.sRGBTexture != srgb || importer.maxTextureSize != 1024)
        {
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static List<Placement> HabitatPlants(Habitat h, IslandEcologyData.Species species, int speciesIndex,
        List<(Vector2 point, float radius)> occupied)
    {
        var result = new List<Placement>();
        var rootVertices = species.near.vertices.Where(v => Mathf.Abs(v.y) < .025f).ToArray();
        int target = speciesIndex switch
        {
            2 => 85, 3 => 60, 4 => 145, 6 => 32, 11 => 48, 12 => 43, 13 => 90, 14 => 105, 15 => 70,
            _ => int.MaxValue
        };
        IEnumerable<Placement> source = ExistingPlants(h, species, speciesIndex);
        if (target != int.MaxValue)
        {
            var relocated = h.WoodlandCandidates(speciesIndex).Select(xy => new Placement(new Vector3(xy.x, 0, xy.y),
                h.Noise(xy, 2.1f, 16 + speciesIndex) * 360, Vector3.up,
                .85f + h.Noise(xy, 4.5f, speciesIndex + 52) * .43f));
            source = source.Concat(relocated);
        }
        source = source.OrderBy(p => h.Noise(new Vector2(p.position.x, p.position.z), 4.5f, 91 + speciesIndex));
        foreach (var original in source)
        {
            if (result.Count >= target) break;
            var xy = new Vector2(original.position.x, original.position.z);
            if (!h.Surface(xy, out var hit) || hit.normal.y < .87f) continue;
            float elevation = hit.point.y - h.sea;
            float shade = h.TreeShelter(xy), moisture = h.Moisture(xy);
            float coast = h.CoastDistance(xy), trunk = h.TrunkDistance(xy);
            Color mask = h.Mask(xy);
            if (trunk < 1.8f || mask.g > .12f || mask.b > .47f) continue;
            bool habitat = speciesIndex switch
            {
                0 => coast < 85 && elevation < h.beach + 14 && shade < .52f,
                1 => shade < .64f && mask.r > .12f,
                2 => shade > .2f && moisture > .38f && coast > 26,
                3 => shade > .43f && moisture > .43f && coast > 35,
                4 => shade > .28f && moisture > .34f && coast > 30,
                5 => shade < .42f && coast < 155 && mask.r > .13f,
                6 => shade > .15f && shade < .73f && coast > 38,
                7 or 8 => shade < .48f && coast > 40,
                9 or 10 => shade < .28f && mask.r > .32f && coast > 30,
                11 => shade > .17f && shade < .56f && mask.r > .18f,
                12 => shade > .43f && moisture > .42f && coast > 45,
                13 => shade > .38f && moisture > .39f || h.RockShelter(xy) > .4f && shade > .15f,
                14 => shade > .38f && coast > 35,
                15 => shade > .26f && trunk < 24,
                _ => shade > .22f && coast > 45 && moisture > .38f
            };
            if (!habitat) continue;
            float size = original.normal.w * (speciesIndex < 2 ? .66f : speciesIndex is 3 or 4 ? .72f :
                speciesIndex is >= 9 and <= 12 ? .62f : speciesIndex is 7 or 8 ? .84f : .83f);
            float footprint = (speciesIndex is 7 or 8 ? 2.3f : speciesIndex is 5 or 6 ? 1.2f :
                speciesIndex is 3 or 4 or 16 ? 1.1f : .65f) * size;
            if (h.Protected(xy, footprint + .8f)) continue;
            if (occupied.Any(other => Vector2.Distance(other.point, xy) < other.radius + footprint)) continue;
            var normal = Vector3.Slerp(Vector3.up, hit.normal, speciesIndex is 7 or 8 ? .12f : .85f);
            var rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, original.position.w, 0);
            var position = hit.point;
            float sink = 0;
            bool supported = true;
            foreach (var root in rootVertices)
            {
                var point = hit.point + rotation * root * size;
                if (!h.Surface(Xz(point), out var support)) { supported = false; break; }
                sink = Mathf.Max(sink, point.y - support.point.y);
            }
            if (!supported || sink > .16f) continue;
            position.y -= sink + .02f;
            result.Add(new Placement(position, original.position.w, normal, size));
            occupied.Add((xy, footprint));
        }
        return result;
    }

    static IslandEcologyData Dress(Habitat h, Scene scene, IslandEcologyData previous)
    {
        var data = Object.Instantiate(previous);
        data.seed = h.options.seed;
        data.scope = "Island 1 coast, ridge and woodland habitat";
        var occupied = new List<(Vector2 point, float radius)>();
        for (int i = 0; i < data.species.Length; i++)
            data.species[i].cells = DetailCells(data.species[i], HabitatPlants(h, data.species[i], i, occupied));
        for (int i = 0; i < 2; i++)
        {
            var grass = new Material(data.species[i].material);
            grass.SetColor("_BaseColor", i == 0 ? new Color(.90f,.83f,.66f) : new Color(.97f,.80f,.57f));
            data.species[i].material = Asset(grass, data.species[i].name + " - dune palette.mat");
        }
        var surfaceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        surfaceTexture.SetPixels(Enumerable.Repeat(new Color(.53f, 1f, 0f, 0f), 4).ToArray());
        surfaceTexture.Apply(false, true);
        var surface = Asset(surfaceTexture, "Matte prop surface.asset");
        var stoneColor = RockTexture("Rock030_1K-JPG_Color.jpg", TextureImporterType.Default, true);
        var stoneNormal = RockTexture("Rock030_1K-JPG_NormalGL.jpg", TextureImporterType.NormalMap, false);
        var stoneSurface = RockTexture("Rock030_RoughnessOcclusion.png", TextureImporterType.Default, false);
        var stone = GroundMaterial("Weathered coastal stone", stoneColor, stoneNormal, stoneSurface,
            new Color(1.24f,1.19f,1.08f), .44f, 137);
        var pebble = GroundMaterial("Washed shingle", stoneColor, stoneNormal, stoneSurface,
            new Color(1.19f,1.14f,1.08f), .50f, 62);
        var bark = AssetDatabase.LoadAssetAtPath<Material>(Previous + "/Weathered_Bark.mat");
        var wood = GroundMaterial("Salt-bleached wood", bark.GetTexture("_MainTex") as Texture2D,
            bark.GetTexture("_BotanicalNormal") as Texture2D, surface, new Color(1.02f,.93f,.77f), .25f, 100);
        var shell = GroundMaterial("Worn shell", null, null, surface, new Color(.80f,.72f,.62f), .32f, 37);
        var fern = new Material(data.species[4].material);
        fern.SetFloat("_WindBend", .27f);
        fern.SetFloat("_WindFrequency", .58f);
        fern.SetColor("_BotanicalEmission", Color.black);
        fern.SetVector("_EcologyDistance", new Vector4(35, 48, 0, 0));
        fern = Asset(fern, "Coppercoil fern.mat");
        var detail = new[]
        {
            GroundSpecies("Shingle cluster A", Stones(0,false,false), Stones(0,true,false), pebble, 62),
            GroundSpecies("Shingle cluster B", Stones(1,false,false), Stones(1,true,false), pebble, 62),
            GroundSpecies("Fractured ledge A", Stones(2,false,true), Stones(2,true,true), stone, 137),
            GroundSpecies("Fractured ledge B", Stones(3,false,true), Stones(3,true,true), stone, 137),
            GroundSpecies("Stranded branch", Driftwood(false), Driftwood(true), wood, 100),
            GroundSpecies("Shell wrack", Shells(false), Shells(true), shell, 37),
            GroundSpecies("Coppercoil fern", CoppercoilFern(false), CoppercoilFern(true), fern, 48)
        };
        var placements = detail.Select(_ => new List<Placement>()).ToArray();
        var obstacles = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => c != h.ground && c.gameObject.scene == scene && c.enabled &&
                !c.GetComponentInParent<ProceduralIsland>() && c.bounds.size.y < 100)
            .Select(c => c.bounds).ToArray();
        var rng = new System.Random(h.options.seed ^ 392189);
        float spacing = h.options.detailSpacing;
        for (float z = h.bounds.y; z < h.bounds.y + h.bounds.w; z += spacing)
        for (float x = h.bounds.x; x < h.bounds.x + h.bounds.z; x += spacing)
        {
            Vector2 p = new Vector2(x + R(rng, 0, spacing), z + R(rng, 0, spacing));
            float pick = R(rng);
            if (!h.Surface(p, out var hit) || hit.normal.y < .71f || h.Protected(p, 2.2f)) continue;
            float elevation = hit.point.y - h.sea;
            float coast = h.CoastDistance(p);
            if (elevation < 1.8f) continue;
            var mask = h.Mask(p);
            float vein = h.Noise(p, 24, 74);
            bool strand = coast < h.options.coastlineBandMetres * 1.32f && elevation < h.beach * .76f;
            bool bedrock = mask.b > .30f || h.RockShelter(p) > .27f;
            if (!strand && !bedrock && vein < .69f) continue;
            float yaw = R(rng, 0, 360);
            if (strand)
            {
                // Lie along the local shore contour with slight yaw variation.
                var along = Vector3.Cross(Vector3.up, hit.normal);
                if (along.sqrMagnitude > .0001f) yaw = Mathf.Atan2(-along.z, along.x) * Mathf.Rad2Deg + R(rng, -22, 22);
                if (pick < .14f && elevation > 3.2f) Put(4, p, yaw, R(rng, .74f, 1.23f));
                else if (pick < .42f) Put(5, p, yaw, R(rng, .72f, 1.25f));
                else if (vein > .39f) Put(pick < .69f ? 0 : 1, p, yaw, R(rng, .65f, 1.47f));
                if (vein > .57f && elevation > 3 && pick > .35f)
                    Put(pick < .62f ? 2 : 3, p + new Vector2(2.5f, -2), yaw, R(rng, .85f, 1.7f));
            }
            else if (bedrock || (vein > .74f && mask.r < .37f))
            {
                if (pick < .64f) Put(pick < .34f ? 2 : 3, p, yaw, R(rng, .69f, 1.65f));
                if (vein > .47f) Put(pick < .5f ? 0 : 1, p + new Vector2(-2.4f, 2), yaw + 19, R(rng, .7f, 1.45f));
            }
        }

        void Put(int index, Vector2 p, float yaw, float scale)
        {
            if (!h.Surface(p, out var hit) || hit.normal.y < .7f || hit.point.y < h.sea + 1.65f) return;
            var mesh = detail[index].near;
            float footprint = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z) * scale + .5f;
            if (h.Protected(p, footprint) || obstacles.Any(b =>
                (Xz(b.ClosestPoint(hit.point)) - p).sqrMagnitude < footprint * footprint &&
                b.max.y > hit.point.y - .2f && b.min.y < hit.point.y + 2)) return;
            var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, yaw, 0);
            var origin = hit.point;
            float sink = 0;
            var bounds = mesh.bounds;
            foreach (var corner in new[] { new Vector3(bounds.min.x, 0, bounds.min.z), new Vector3(bounds.max.x, 0, bounds.min.z),
                new Vector3(bounds.min.x, 0, bounds.max.z), new Vector3(bounds.max.x, 0, bounds.max.z) })
            {
                Vector3 edge = origin + rotation * corner * scale;
                if (!h.Surface(Xz(edge), out var ground) || ground.point.y < h.sea + 1.30f) return;
                sink = Mathf.Max(sink, edge.y - ground.point.y);
            }
            if (sink > (index == 5 ? .035f : .17f)) return;
            // Corners alone miss local curvature on newly graded slopes.
            // Seat the actual contact vertices as well as the footprint bounds.
            foreach (var vertex in mesh.vertices.Where(v => Mathf.Abs(v.y) < .025f))
            {
                Vector3 edge = origin + rotation * vertex * scale;
                if (!h.Surface(Xz(edge), out var support)) return;
                sink = Mathf.Max(sink, edge.y - support.point.y);
            }
            origin.y -= sink + (index == 5 ? 0 : .017f);
            placements[index].Add(new Placement(origin, yaw, hit.normal, scale));
        }

        for (int i = 0; i < detail.Length; i++) detail[i].cells = DetailCells(detail[i], placements[i]);
        // Pick shaded bracken habitat beside the approach without occupying it.
        // Sorted, well-separated spots give this plant a few discoverable landmarks.
        var fernSpecies = detail[6];
        fernSpecies.uncanny = true;
        var fernSites = data.species[4].cells.SelectMany(c => c.positions).Where(p =>
        {
            var xy = new Vector2(p.x, p.z);
            h.TrailDistance(xy, out float shoulder);
            return shoulder > 4 && shoulder < 43 && h.TreeShelter(xy) > .13f;
        }).OrderByDescending(p => h.Noise(new Vector2(p.x, p.z), 22, 78));
        foreach (var p in fernSites)
        {
            if (placements[6].Count >= Mathf.Min(h.options.uncannyPocketLimit, 11)) break;
            var xy = new Vector2(p.x + 1.1f, p.z - 1.1f);
            if (placements[6].Any(other => Vector2.Distance(xy, new Vector2(other.position.x, other.position.z)) < 25)) continue;
            Put(6, xy, p.w, .92f);
        }
        fernSpecies.cells = DetailCells(fernSpecies, placements[6]);
        data.species = data.species.Concat(detail).ToArray();
        return Asset(data, "Woodland and coastal dressing.asset");
    }

    public static object Validate()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var island = Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None)
            .Single(i => i.gameObject.scene == scene && i.isActiveAndEnabled);
        var grass = InScene<NaturalGrassScatter>(scene);
        var ecology = FindDressing(scene);
        if (!island.habitatMap || !grass.data || !ecology.data ||
            AssetDatabase.GetAssetPath(grass.data) != Output + "/Meadow Scatter.asset" ||
            AssetDatabase.GetAssetPath(ecology.data) != Output + "/Woodland and coastal dressing.asset")
            throw new InvalidOperationException("Load the baked Island 1 habitat, grass, and dressing before validating.");
        var ground = island.GetComponent<MeshCollider>();
        var terrainMaterial = island.GetComponent<Renderer>().sharedMaterial;
        var bounds = ground.bounds;
        var settings = AssetDatabase.LoadAssetAtPath<IslandEcologySettings>(Previous + "/Settings.asset");
        int missing = 0, floaters = 0, routeIntrusions = 0, grassSamples = 0, missingMaterials = 0, ecologySamples = 0;
        float maximumGap = float.NegativeInfinity;
        var roots = grass.data.meshes.Select(m => m.vertices.Where(v => Mathf.Abs(v.y) < .001f).ToArray()).ToArray();

        bool Support(Vector3 point, out RaycastHit hit) => ground.Raycast(
            new Ray(new Vector3(point.x, bounds.max.y + 2, point.z), Vector3.down), out hit, bounds.size.y + 4);
        foreach (var cell in grass.data.cells)
        for (int i = 0; i < cell.positions.Length; i++)
        {
            var p = cell.positions[i];
            if (!Support(p, out var hit) || hit.point.y < terrainMaterial.GetFloat("_SeaLevel") + island.beachHeight - .75f) missing++;
            if (i % 13 != 0) continue;
            var transform = Matrix(new Placement(p, p.w, cell.normals[i], cell.scales[i]));
            foreach (var root in roots[cell.variants[i]])
            {
                grassSamples++;
                var point = transform.MultiplyPoint3x4(root);
                if (!Support(point, out var support)) { missing++; continue; }
                float gap = point.y - support.point.y;
                maximumGap = Mathf.Max(gap, maximumGap);
                if (gap > .012f) floaters++;
            }
        }
        foreach (var species in ecology.data.species)
        {
            if (!species.material || !species.material.enableInstancing || !species.near || !species.far) missingMaterials++;
            foreach (var cell in species.cells)
            foreach (var p in cell.positions)
            {
                ecologySamples++;
                var xy = new Vector2(p.x, p.z);
                if (!Support(p, out _)) missing++;
                if (settings.clearings.Any(c => Vector2.Distance(xy, c.center) < c.radius)) routeIntrusions++;
                foreach (var trail in settings.corridors)
                for (int i = 1; i < trail.points.Length; i++)
                {
                    var a = trail.points[i - 1];
                    var d = trail.points[i] - a;
                    if (d.sqrMagnitude < .001f) continue;
                    if (Vector2.Distance(xy, a + d * Mathf.Clamp01(Vector2.Dot(xy - a, d) / d.sqrMagnitude)) < trail.halfWidth)
                        routeIntrusions++;
                }
            }
        }
        var shaderErrors = new[] { terrainMaterial.shader, grass.data.material.shader }.Distinct()
            .SelectMany(ShaderUtil.GetShaderMessages).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
        var result = new
        {
            passed = missing == 0 && floaters == 0 && routeIntrusions == 0 && missingMaterials == 0 &&
                shaderErrors.Length == 0 && grass.IsReady && ecology.IsReady && island.habitatMap,
            grass = grass.InstanceCount, ecology = ecology.InstanceCount, grassSamples, ecologySamples,
            missing, floaters, maximumGap, routeIntrusions, missingMaterials, shaderErrors,
            colliders = scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Collider>(true).Length),
            species = ecology.data.species.Select(s => new { s.name, s.uncanny, count = s.cells.Sum(c => c.positions.Length) })
        };
        Directory.CreateDirectory(Report);
        File.WriteAllText(Report + "/validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

}

