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
    public Texture2D coastSandNormal;
    public Texture2D grassNormal;
    public Texture2D rockyNormal;
    [Min(0.001f)] public float textureTiling = 0.09f;

    [Header("Yellow shoreline beach")]
    public Texture2D beachSand;
    public Texture2D beachSandNormal;
    public Color beachTint = new Color(1.18f, 1.08f, 0.72f, 1f);
    [Tooltip("Sand coverage height above sea level in world metres. Keeps inland ground unchanged.")]
    [Min(0.5f)] public float beachHeight = 8f;
    [Min(0.1f)] public float beachBlendWidth = 3.5f;
    [Min(0.001f)] public float beachTextureTiling = 0.075f;

    private Mesh islandMesh;
    private Material terrainMaterial;
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
        var biomeUv = new List<Vector2>(1 + radialSegments * rings);
        vertices.Add(Vector3.up * SampleHeight(0f, 0f));
        uv.Add(Vector2.zero);
        biomeUv.Add(Vector2.zero);

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
                biomeUv.Add(new Vector2(normalizedRadius, 0f));
            }
        }

        var triangles = new List<int>(radialSegments * rings * 6);
        for (var ring = 0; ring < rings; ring++)
        {
            for (var segment = 0; segment < radialSegments; segment++)
            {
                var nextSegment = (segment + 1) % radialSegments;
                var a = ring == 0 ? 0 : 1 + (ring - 1) * radialSegments + segment;
                var b = 1 + ring * radialSegments + segment;
                var c = 1 + ring * radialSegments + nextSegment;
                var d = ring == 0 ? 0 : 1 + (ring - 1) * radialSegments + nextSegment;
                AddTriangle(a, b, c, triangles);
                if (ring > 0)
                    AddTriangle(a, c, d, triangles);
            }
        }

        islandMesh.Clear();
        islandMesh.SetVertices(vertices);
        islandMesh.SetUVs(0, uv);
        islandMesh.SetUVs(1, biomeUv);
        islandMesh.subMeshCount = 1;
        islandMesh.SetTriangles(triangles, 0, true);
        islandMesh.RecalculateNormals();
        islandMesh.RecalculateBounds();

        filter.sharedMesh = islandMesh;
        collider.sharedMesh = null;
        collider.sharedMesh = islandMesh;
        EnsureMaterial();
        renderer.sharedMaterials = new[] { terrainMaterial };
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

    private static void AddTriangle(int a, int b, int c, List<int> triangles)
    {
        // XZ rings run counter-clockwise; reverse winding to face the sky.
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(b);
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

    // The island mesh intentionally continues below the water. Find the real
    // sea-level contour so shore effects do not follow that submerged skirt.
    private float FindWaterlineRadius(float angle, float localSeaLevel)
    {
        float edgeRadius = EdgeRadius(angle);
        if (SampleHeight(0f, angle) <= localSeaLevel)
            return 0f;
        if (SampleHeight(1f, angle) >= localSeaLevel)
            return edgeRadius;

        float land = 0f;
        float water = 1f;
        for (int iteration = 0; iteration < 14; iteration++)
        {
            float middle = (land + water) * 0.5f;
            if (SampleHeight(middle, angle) > localSeaLevel)
                land = middle;
            else
                water = middle;
        }

        // Keep the land side behind the island depth buffer, so foam cannot
        // spill onto dry sand as the animated waterline rises and falls.
        return edgeRadius * Mathf.Min(1f, water + 0.0002f);
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

    private void EnsureMaterial()
    {
        if (terrainMaterial == null)
        {
            var shader = Shader.Find("DarkBrine/Island Blended Terrain");
            if (shader == null)
            {
                Debug.LogError("Island Blended Terrain shader is missing.", this);
                return;
            }
            terrainMaterial = new Material(shader) { name = "Island Blended Terrain Material", hideFlags = HideFlags.DontSave };
        }

        terrainMaterial.SetTexture("_SandTex", coastSand03 != null ? coastSand03 : coastSand01);
        terrainMaterial.SetTexture("_SandAltTex", coastSand01 != null ? coastSand01 : coastSand03);
        terrainMaterial.SetTexture("_DryTex", drySand != null ? drySand : coastSand01);
        terrainMaterial.SetTexture("_GrassTex", sparseGrass != null ? sparseGrass : leafyGrass);
        terrainMaterial.SetTexture("_LeafyGrassTex", leafyGrass != null ? leafyGrass : sparseGrass);
        terrainMaterial.SetTexture("_ForestTex", forestGround != null ? forestGround : leafyGrass);
        terrainMaterial.SetTexture("_RockTex", rockyTerrain != null ? rockyTerrain : drySand);
        terrainMaterial.SetTexture("_SandNormal", coastSandNormal != null ? coastSandNormal : Texture2D.normalTexture);
        terrainMaterial.SetTexture("_GrassNormal", grassNormal != null ? grassNormal : Texture2D.normalTexture);
        terrainMaterial.SetTexture("_RockNormal", rockyNormal != null ? rockyNormal : Texture2D.normalTexture);
        terrainMaterial.SetColor("_SandTint", Color.Lerp(Color.white, sandColor, 0.10f));
        terrainMaterial.SetColor("_GrassTint", Color.Lerp(Color.white, grassColor, 0.32f));
        terrainMaterial.SetColor("_RockTint", Color.Lerp(Color.white, rockColor, 0.07f));
        terrainMaterial.SetFloat("_BeachEnabled", beachSand != null ? 1f : 0f);
        terrainMaterial.SetTexture("_BeachTex", beachSand != null ? beachSand : Texture2D.whiteTexture);
        terrainMaterial.SetTexture("_BeachNormal", beachSandNormal != null ? beachSandNormal : Texture2D.normalTexture);
        terrainMaterial.SetColor("_BeachTint", beachTint);
        terrainMaterial.SetFloat("_BeachHeight", beachHeight);
        terrainMaterial.SetFloat("_BeachBlendWidth", Mathf.Min(beachBlendWidth, beachHeight));
        terrainMaterial.SetFloat("_BeachTiling", beachTextureTiling);
        var ocean = FindFirstObjectByType<OceanWorld>();
        terrainMaterial.SetFloat("_SeaLevel", ocean != null ? ocean.oceanHeight : 0f);
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

        OceanWorld ocean = FindFirstObjectByType<OceanWorld>();
        float seaLevel = ocean != null ? ocean.oceanHeight : 0f;
        float localSeaLevel = transform.InverseTransformPoint(new Vector3(transform.position.x, seaLevel, transform.position.z)).y;

        const int rows = 12;
        int columns = radialSegments + 1;
        var vertices = new Vector3[(rows + 1) * columns];
        var uv = new Vector2[vertices.Length];
        for (int row = 0; row <= rows; row++)
        {
            float across = row / (float)rows;
            // Keep breakers close to the actual waterline. The previous wide
            // band covered the bay like a foam decal instead of shore wash.
            float shoreOffset = Mathf.Lerp(9f, -2.5f, across);
            for (int column = 0; column <= radialSegments; column++)
            {
                float angle = column / (float)radialSegments * Mathf.PI * 2f;
                float radius = FindWaterlineRadius(angle, localSeaLevel) + shoreOffset;
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
        ApplyShoreWaveSettings();
        shoreFoamObject.GetComponent<MeshRenderer>().sharedMaterial = shoreFoamMaterial;
        shoreFoamObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shoreFoamObject.GetComponent<MeshRenderer>().receiveShadows = false;
    }

    // The shore mesh uses the same wave phase as the ocean surface. This keeps a
    // breaker locked to its crest instead of leaving a flat, floating foam decal.
    private void ApplyShoreWaveSettings()
    {
        if (shoreFoamMaterial == null)
            return;

        OceanWorld ocean = FindFirstObjectByType<OceanWorld>();
        if (ocean == null)
            return;

        shoreFoamMaterial.SetFloat("_SeaLevel", ocean.oceanHeight);
        shoreFoamMaterial.SetFloat("_OceanMotionSpeed", ocean.waveMotionSpeed);
        ApplyShoreWave("_Wave1", ocean.wave1);
        ApplyShoreWave("_Wave2", ocean.wave2);
        ApplyShoreWave("_Wave3", ocean.wave3);
        ApplyShoreWave("_Wave4", ocean.wave4);
    }

    private void ApplyShoreWave(string propertyName, OceanGerstnerWave wave)
    {
        Vector2 direction = wave.direction.sqrMagnitude < 0.0001f ? Vector2.right : wave.direction.normalized;
        shoreFoamMaterial.SetVector(propertyName, new Vector4(direction.x, direction.y, wave.amplitude, wave.wavelength));
        shoreFoamMaterial.SetVector(propertyName + "Motion", new Vector4(wave.speed, wave.steepness, 0f, 0f));
    }

    private void OnDisable()
    {
        if (terrainMaterial != null)
        {
            if (Application.isPlaying) Destroy(terrainMaterial); else DestroyImmediate(terrainMaterial);
            terrainMaterial = null;
        }
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
