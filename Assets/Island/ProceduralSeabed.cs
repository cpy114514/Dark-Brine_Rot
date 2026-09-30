using UnityEngine;
using UnityEngine.Rendering;

// Runs after the island has generated its mesh, including when a scene is opened.
[ExecuteAlways, DefaultExecutionOrder(100)]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public sealed class ProceduralSeabed : MonoBehaviour
{
    public ProceduralIsland island;
    [Tooltip("World-space radius measured from the island centre.")]
    [Min(100f)] public float outerRadius = 6000f;
    [Min(1f)] public float depthBelowRim = 110f;
    [Tooltip("Distance from the island edge at which the deep floor is reached.")]
    [Min(10f)] public float slopeDistance = 1800f;
    [Range(8, 96)] public int rings = 48;
    [Range(0f, 10f)] public float relief = 3f;

    private Mesh generatedMesh;
    private Mesh sourceMesh;
    private Matrix4x4 sourceMatrix;
    private Matrix4x4 ownMatrix;
    private Bounds sourceBounds;
    private int sourceSeed;
    private bool rebuildRequested;

    private void OnEnable() => rebuildRequested = true;
    private void OnValidate() => rebuildRequested = true;

    private void LateUpdate()
    {
        var renderer = GetComponent<MeshRenderer>();
        var collider = GetComponent<MeshCollider>();
        bool available = island != null && island.isActiveAndEnabled;
        renderer.enabled = available;
        collider.enabled = available;
        if (!available) return;

        var mesh = island.GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null) return;
        if (rebuildRequested || generatedMesh == null || sourceMesh != mesh ||
            sourceMatrix != island.transform.localToWorldMatrix ||
            ownMatrix != transform.localToWorldMatrix || sourceBounds != mesh.bounds ||
            sourceSeed != island.seed || generatedMesh.vertexCount != (rings + 1) * island.radialSegments)
            Rebuild();

        // Share the island's sand material, so colour, texture scale and shore
        // patches agree across the join. Never modify the island or its material.
        renderer.sharedMaterial = island.GetComponent<MeshRenderer>().sharedMaterial;
    }

    [ContextMenu("Rebuild Seabed")]
    public void Rebuild()
    {
        if (island == null || !island.isActiveAndEnabled) return;
        var mesh = island.GetComponent<MeshFilter>().sharedMesh;
        int segments = island.radialSegments;
        if (mesh == null || mesh.vertexCount != 1 + segments * island.rings) return;

        rings = Mathf.Clamp(rings, 8, 96);
        outerRadius = Mathf.Max(100f, outerRadius);
        slopeDistance = Mathf.Max(10f, slopeDistance);
        depthBelowRim = Mathf.Max(1f, depthBelowRim);
        var sourceVertices = mesh.vertices;
        var sourceNormals = mesh.normals;
        var sourceUv = mesh.uv;
        int rimStart = sourceVertices.Length - segments;
        var centre = island.transform.position;
        var vertices = new Vector3[(rings + 1) * segments];
        var uv = new Vector2[vertices.Length];
        var biome = new Vector2[vertices.Length];
        var rimNormals = new Vector3[segments];
        var indices = new int[rings * segments * 6];

        for (int segment = 0; segment < segments; segment++)
        {
            int sourceIndex = rimStart + segment;
            var rim = island.transform.TransformPoint(sourceVertices[sourceIndex]);
            var direction = new Vector3(rim.x - centre.x, 0f, rim.z - centre.z);
            float rimRadius = direction.magnitude;
            direction.Normalize();
            // A minimum continuation still works if the island is enlarged.
            float extent = Mathf.Max(100f, outerRadius - rimRadius);
            rimNormals[segment] = transform.localToWorldMatrix.transpose.MultiplyVector(
                island.transform.worldToLocalMatrix.transpose.MultiplyVector(sourceNormals[sourceIndex])).normalized;
            for (int ring = 0; ring <= rings; ring++)
            {
                float t = ring / (float)rings;
                // Concentrate geometry at the seam; far-away floor can be coarse.
                float distance = extent * t * t;
                var world = rim + direction * distance;
                float slope = Mathf.Clamp01(distance / slopeDistance);
                float descent = Mathf.SmoothStep(0f, 1f, slope);
                float noise = Mathf.PerlinNoise(world.x * 0.006f + island.seed * 0.01f,
                    world.z * 0.006f + island.seed * 0.019f) * 2f - 1f;
                world.y = rim.y - depthBelowRim * descent + noise * Mathf.Min(relief, depthBelowRim * 0.1f) * descent;
                int index = ring * segments + segment;
                vertices[index] = transform.InverseTransformPoint(world);
                // Match the existing rim UV exactly, also with scaled islands.
                var islandLocal = island.transform.InverseTransformPoint(world);
                uv[index] = ring == 0 ? sourceUv[sourceIndex] :
                    new Vector2(islandLocal.x, islandLocal.z) * island.textureTiling;
                biome[index] = new Vector2(1f, 0f);
            }
        }

        int cursor = 0;
        for (int ring = 0; ring < rings; ring++)
        for (int segment = 0; segment < segments; segment++)
        {
            int next = (segment + 1) % segments;
            int a = ring * segments + segment;
            int b = (ring + 1) * segments + segment;
            int c = (ring + 1) * segments + next;
            int d = ring * segments + next;
            indices[cursor++] = a; indices[cursor++] = c; indices[cursor++] = b;
            indices[cursor++] = a; indices[cursor++] = d; indices[cursor++] = c;
        }

        if (generatedMesh == null)
            generatedMesh = new Mesh { name = "Island Seabed Extension", hideFlags = HideFlags.DontSave };
        var collider = GetComponent<MeshCollider>();
        collider.sharedMesh = null;
        generatedMesh.Clear();
        generatedMesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.uv2 = biome;
        generatedMesh.triangles = indices;
        generatedMesh.RecalculateNormals();
        var normals = generatedMesh.normals;
        System.Array.Copy(rimNormals, normals, segments);
        generatedMesh.normals = normals;
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        collider.sharedMesh = generatedMesh;
        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = island.GetComponent<MeshRenderer>().sharedMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        sourceMesh = mesh;
        sourceBounds = mesh.bounds;
        sourceMatrix = island.transform.localToWorldMatrix;
        ownMatrix = transform.localToWorldMatrix;
        sourceSeed = island.seed;
        rebuildRequested = false;
    }

    private void OnDisable()
    {
        GetComponent<MeshFilter>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshRenderer>().enabled = false;
        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
            generatedMesh = null;
        }
    }
}
