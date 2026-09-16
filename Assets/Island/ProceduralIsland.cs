using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralIsland : MonoBehaviour
{
    [Header("Shape")]
    [Min(8)] public int radialSegments = 120;
    [Min(4)] public int rings = 36;
    [Min(4f)] public float shorelineRadius = 55f;
    [Min(0.5f)] public float peakHeight = 7.5f;
    [Min(0.1f)] public float underwaterDepth = 1.6f;
    public int seed = 14821;

    [Header("Fallback surface colours")]
    public Color sandColor = new Color(0.82f, 0.66f, 0.38f, 1f);
    public Color grassColor = new Color(0.20f, 0.43f, 0.15f, 1f);
    public Color rockColor = new Color(0.31f, 0.31f, 0.27f, 1f);

    [Header("Supplied surface textures")]
    public Texture2D coastSand01;
    public Texture2D coastSand03;
    public Texture2D leafyGrass;
    public Texture2D sparseGrass;
    public Texture2D drySand;
    public Texture2D forestGround;
    public Texture2D rockyTerrain;
    [Min(0.001f)] public float textureTiling = 0.09f;

    private Mesh islandMesh;
    private Material coastSandMaterial;
    private Material beachSandMaterial;
    private Material sparseGrassMaterial;
    private Material forestGroundMaterial;
    private Material drySandMaterial;
    private Material rockyTerrainMaterial;
    private GameObject shoreFoamObject;
    private Mesh shoreFoamMesh;
    private Material shoreFoamMaterial;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        radialSegments = Mathf.Max(8, radialSegments);
        rings = Mathf.Max(4, rings);
        shorelineRadius = Mathf.Max(4f, shorelineRadius);
        peakHeight = Mathf.Max(0.5f, peakHeight);
        underwaterDepth = Mathf.Max(0.1f, underwaterDepth);
        textureTiling = Mathf.Max(0.001f, textureTiling);
        // Creating generated child objects during OnValidate causes Unity
        // lifecycle warnings. OnEnable handles safe editor/runtime rebuilding;
        // while playing inspector changes still update immediately.
        if (Application.isPlaying && isActiveAndEnabled)
            Rebuild();
    }

    [ContextMenu("Rebuild Island")]
    public void Rebuild()
    {
        var filter = GetComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();
        var collider = GetComponent<MeshCollider>();

        if (islandMesh == null)
        {
            islandMesh = new Mesh { name = "Procedural Island Mesh" };
            islandMesh.MarkDynamic();
        }

        var vertices = new List<Vector3>(1 + radialSegments * rings);
        var uv = new List<Vector2>(1 + radialSegments * rings);
        vertices.Add(Vector3.up * SampleHeight(0f, 0f));
        uv.Add(Vector2.zero);

        for (var ring = 1; ring <= rings; ring++)
        {
            var normalizedRadius = ring / (float)rings;
            for (var segment = 0; segment < radialSegments; segment++)
            {
                var angle = segment / (float)radialSegments * Mathf.PI * 2f;
                var edgeRadius = EdgeRadius(angle);
                var radius = normalizedRadius * edgeRadius;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, SampleHeight(normalizedRadius, angle), z));
                // World-space-like tiling keeps the supplied 4K material detail
                // consistent even when the island is hundreds of metres wide.
                uv.Add(new Vector2(x * textureTiling, z * textureTiling));
            }
        }

        var coastSand = new List<int>();
        var beachSand = new List<int>();
        var sparse = new List<int>();
        var forest = new List<int>();
        var dry = new List<int>();
        var rocky = new List<int>();
        for (var ring = 0; ring < rings; ring++)
        {
            for (var segment = 0; segment < radialSegments; segment++)
            {
                var nextSegment = (segment + 1) % radialSegments;
                var a = ring == 0 ? 0 : 1 + (ring - 1) * radialSegments + segment;
                var b = 1 + ring * radialSegments + segment;
                var c = 1 + ring * radialSegments + nextSegment;
                var d = ring == 0 ? 0 : 1 + (ring - 1) * radialSegments + nextSegment;
                AddTriangle(a, b, c, vertices, coastSand, beachSand, sparse, forest, dry, rocky);
                if (ring > 0)
                    AddTriangle(a, c, d, vertices, coastSand, beachSand, sparse, forest, dry, rocky);
            }
        }

        islandMesh.Clear();
        islandMesh.SetVertices(vertices);
        islandMesh.SetUVs(0, uv);
        islandMesh.subMeshCount = 6;
        islandMesh.SetTriangles(coastSand, 0, true);
        islandMesh.SetTriangles(beachSand, 1, true);
        islandMesh.SetTriangles(sparse, 2, true);
        islandMesh.SetTriangles(forest, 3, true);
        islandMesh.SetTriangles(dry, 4, true);
        islandMesh.SetTriangles(rocky, 5, true);
        islandMesh.RecalculateNormals();
        islandMesh.RecalculateBounds();

        filter.sharedMesh = islandMesh;
        collider.sharedMesh = null;
        collider.sharedMesh = islandMesh;
        EnsureMaterials();
        renderer.sharedMaterials = new[] { coastSandMaterial, beachSandMaterial, sparseGrassMaterial, forestGroundMaterial, drySandMaterial, rockyTerrainMaterial };
        BuildShoreFoam();
    }

    public float GetWorldSurfaceHeight(Vector3 worldPosition)
    {
        var local = transform.InverseTransformPoint(worldPosition);
        var angle = Mathf.Atan2(local.z, local.x);
        var radius = new Vector2(local.x, local.z).magnitude;
        var normalizedRadius = radius / EdgeRadius(angle);
        return transform.TransformPoint(new Vector3(local.x, SampleHeight(normalizedRadius, angle), local.z)).y;
    }

    private void AddTriangle(int a, int b, int c, List<Vector3> vertices, List<int> coastSand, List<int> beachSand, List<int> sparse, List<int> forest, List<int> dry, List<int> rocky)
    {
        var center = (vertices[a] + vertices[b] + vertices[c]) / 3f;
        var radius = new Vector2(center.x, center.z).magnitude;
        var normalizedRadius = radius / EdgeRadius(Mathf.Atan2(center.z, center.x));
        // Complete rings create natural, continuous biome bands without the
        // saw-tooth material seams produced by per-triangle selection.
        List<int> target;
        if (normalizedRadius > 0.90f) target = coastSand;
        else if (normalizedRadius > 0.78f) target = beachSand;
        else if (normalizedRadius > 0.62f) target = sparse;
        else if (normalizedRadius > 0.36f) target = forest;
        else if (normalizedRadius > 0.23f) target = dry;
        else target = rocky;
        // The ring runs counter-clockwise in XZ; reverse this order so the
        // terrain normals face the sky and the URP Lit material renders its top.
        target.Add(a);
        target.Add(c);
        target.Add(b);
    }

    private float EdgeRadius(float angle)
    {
        var a = angle + seed * 0.017f;
        // Large low-frequency changes form coves and headlands; the smaller
        // terms soften the outline so it reads as a naturally eroded coastline.
        var variation = 1f
            + Mathf.Sin(a * 2f + 0.3f) * 0.20f
            + Mathf.Sin(a * 3f - 1.1f) * 0.14f
            + Mathf.Sin(a * 5f + 0.8f) * 0.09f
            + Mathf.Sin(a * 9f - 0.4f) * 0.04f;
        return shorelineRadius * variation;
    }

    private float SampleHeight(float normalizedRadius, float angle)
    {
        normalizedRadius = Mathf.Max(0f, normalizedRadius);
        var land = 1f - SmoothStep(0.55f, 0.98f, normalizedRadius);
        var dome = Mathf.Pow(Mathf.Max(0f, 1f - normalizedRadius * normalizedRadius), 1.55f);
        var noise = (Mathf.PerlinNoise(Mathf.Cos(angle) * 1.7f + seed * 0.01f, Mathf.Sin(angle) * 1.7f + seed * 0.019f) - 0.5f) * 0.9f;
        var ridges = (Mathf.Sin(angle * 3f + seed * 0.03f) * 0.5f + 0.5f) * Mathf.Pow(Mathf.Max(0f, 1f - normalizedRadius), 1.8f) * peakHeight * 0.16f;
        var ripples = Mathf.Sin(angle * 4f + seed) * 0.26f + Mathf.Sin(angle * 7f - seed * 0.1f) * 0.14f;
        // Keep the very edge at the waterline.  A submerged outer skirt shows
        // through the transparent water as a saw-tooth seam, so it is avoided.
        return land * (0.18f + peakHeight * dome + ridges + (noise + ripples) * Mathf.SmoothStep(0f, 0.75f, normalizedRadius))
               + (1f - land) * 0.04f;
    }

    private void EnsureMaterials()
    {
        coastSandMaterial = CreateOrUpdateMaterial(coastSandMaterial, "Island Coast Sand 01", coastSand01, sandColor, 0.28f);
        beachSandMaterial = CreateOrUpdateMaterial(beachSandMaterial, "Island Coast Sand 03", coastSand03, sandColor, 0.24f);
        sparseGrassMaterial = CreateOrUpdateMaterial(sparseGrassMaterial, "Island Sparse Grass", sparseGrass, grassColor, 0.10f);
        forestGroundMaterial = CreateOrUpdateMaterial(forestGroundMaterial, "Island Forest Ground", forestGround != null ? forestGround : leafyGrass, grassColor, 0.08f);
        drySandMaterial = CreateOrUpdateMaterial(drySandMaterial, "Island Dry Sand", drySand, rockColor, 0.20f);
        rockyTerrainMaterial = CreateOrUpdateMaterial(rockyTerrainMaterial, "Island Rocky Terrain", rockyTerrain, rockColor, 0.14f);
    }

    private static Material CreateOrUpdateMaterial(Material material, string materialName, Texture2D texture, Color fallbackColor, float smoothness)
    {
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave };
        }

        // The supplied grass scans are deliberately earthy.  A restrained tint
        // preserves their natural detail while keeping grassland readable from
        // the coastline under the scene's blue outdoor lighting.
        var color = texture == null ? fallbackColor : Color.Lerp(Color.white, fallbackColor, 0.24f);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private void BuildShoreFoam()
    {
        var shader = Shader.Find("DarkBrine/Procedural Shore Foam");
        if (shader == null)
            return;

        if (shoreFoamObject == null)
        {
            var existing = transform.Find("Procedural Shore Foam");
            shoreFoamObject = existing != null ? existing.gameObject : new GameObject("Procedural Shore Foam");
            shoreFoamObject.transform.SetParent(transform, false);
            if (shoreFoamObject.GetComponent<MeshFilter>() == null)
                shoreFoamObject.AddComponent<MeshFilter>();
            if (shoreFoamObject.GetComponent<MeshRenderer>() == null)
                shoreFoamObject.AddComponent<MeshRenderer>();
        }
        if (shoreFoamMesh == null)
            shoreFoamMesh = new Mesh { name = "Procedural Shore Breakers" };

        const int rows = 5;
        int columns = radialSegments + 1;
        var vertices = new Vector3[(rows + 1) * columns];
        var uv = new Vector2[vertices.Length];
        for (int row = 0; row <= rows; row++)
        {
            float across = row / (float)rows;
            // Five metres of water-side foam and a short overlap onto the beach.
            // Island depth hides the inland part so the strip remains clean.
            float shoreOffset = Mathf.Lerp(5f, -9f, across);
            for (int column = 0; column <= radialSegments; column++)
            {
                float angle = column / (float)radialSegments * Mathf.PI * 2f;
                float radius = EdgeRadius(angle) + shoreOffset;
                int index = row * columns + column;
                vertices[index] = new Vector3(Mathf.Cos(angle) * radius, 0.055f, Mathf.Sin(angle) * radius);
                uv[index] = new Vector2(column / (float)radialSegments * 7f, across);
            }
        }
        var triangles = new int[rows * radialSegments * 6];
        int triangle = 0;
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < radialSegments; column++)
        {
            int a = row * columns + column;
            int b = a + 1;
            int c = a + columns;
            int d = c + 1;
            triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = b;
            triangles[triangle++] = b; triangles[triangle++] = c; triangles[triangle++] = d;
        }
        shoreFoamMesh.Clear();
        shoreFoamMesh.vertices = vertices;
        shoreFoamMesh.uv = uv;
        shoreFoamMesh.triangles = triangles;
        shoreFoamMesh.RecalculateNormals();
        shoreFoamMesh.RecalculateBounds();
        shoreFoamObject.GetComponent<MeshFilter>().sharedMesh = shoreFoamMesh;

        if (shoreFoamMaterial == null)
        {
            shoreFoamMaterial = new Material(shader) { name = "Procedural Shore Foam Material", hideFlags = HideFlags.DontSave };
            shoreFoamMaterial.renderQueue = 2910;
        }
        shoreFoamObject.GetComponent<MeshRenderer>().sharedMaterial = shoreFoamMaterial;
        shoreFoamObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shoreFoamObject.GetComponent<MeshRenderer>().receiveShadows = false;
    }

    private void OnDisable()
    {
        if (shoreFoamMaterial != null)
        {
            if (Application.isPlaying) Destroy(shoreFoamMaterial); else DestroyImmediate(shoreFoamMaterial);
            shoreFoamMaterial = null;
        }
        if (shoreFoamMesh != null)
        {
            if (Application.isPlaying) Destroy(shoreFoamMesh); else DestroyImmediate(shoreFoamMesh);
            shoreFoamMesh = null;
        }
        if (shoreFoamObject != null)
        {
            if (Application.isPlaying) Destroy(shoreFoamObject); else DestroyImmediate(shoreFoamObject);
            shoreFoamObject = null;
        }
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        var t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}
