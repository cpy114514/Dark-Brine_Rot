using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class OceanWorld : MonoBehaviour
{
    public enum EffectsQuality { Low, Medium, High }

    [Header("Ocean")]
    [Min(16)] public int resolution = 360;
    [Min(40f)] public float renderDistance = 260f;
    [Min(10f)] public float nearDetailDistance = 65f;
    [Min(20f)] public float midDetailDistance = 150f;
    [Header("Performance")]
    [Tooltip("Controls water mesh density and procedural sky-cloud detail. Choose Low for slower computers.")]
    public EffectsQuality effectsQuality = EffectsQuality.Medium;
    [Header("Dynamic Sky")]
    [Range(0f, 1f)] public float cloudiness = 0.62f;
    [Range(0f, 1f)] public float cloudMotion = 0.85f;
    [Range(0f, 1f)] public float sunlightIntensity = 0.72f;

    Mesh generatedMesh;
    Mesh forwardMesh;
    Mesh surroundMesh;
    Material generatedMaterial;
    GameObject skyDome;
    Material skyMaterial;
    int generatedResolution;
    float generatedSize;
    int generatedCoverageAngle;
    Light cachedSun;
    float appliedNearDistance = float.NaN;
    float appliedMidDistance = float.NaN;
    float appliedViewDistance = float.NaN;

    static readonly int NearDetailDistanceId = Shader.PropertyToID("_NearDetailDistance");
    static readonly int MidDetailDistanceId = Shader.PropertyToID("_MidDetailDistance");
    static readonly int ViewDistanceId = Shader.PropertyToID("_ViewDistance");
    static readonly int SunDirectionId = Shader.PropertyToID("_SunDirection");

    void OnEnable() => BuildOcean();

    void OnValidate()
    {
        if (isActiveAndEnabled)
            BuildOcean();
    }

    void BuildOcean()
    {
        resolution = Mathf.Clamp(resolution, 16, 420);
        renderDistance = Mathf.Max(renderDistance, 40f);
        nearDetailDistance = Mathf.Clamp(nearDetailDistance, 10f, renderDistance - 20f);
        midDetailDistance = Mathf.Clamp(midDetailDistance, nearDetailDistance + 10f, renderDistance - 5f);
        int effectiveResolution = effectsQuality switch
        {
            EffectsQuality.Low => Mathf.Min(resolution, 80),
            EffectsQuality.Medium => Mathf.Min(resolution, 160),
            _ => Mathf.Min(resolution, 300)
        };
        int coverageAngle = GetVisibleCoverageAngle();
        int coverageMode = coverageAngle >= 360 ? 360 : 1;
        // Keep the square mesh edge beyond the camera's far clip. The shader fogs the extra band.
        // Keep a generous band beyond the camera range.  The same mesh is used at every
        // altitude, so looking down from the sky cannot reveal a square water boundary.
        float meshSize = (renderDistance + Mathf.Max(300f, renderDistance * 0.2f)) * 2f;
        var filter = GetComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();

        if (generatedResolution != effectiveResolution || !Mathf.Approximately(generatedSize, meshSize))
        {
            DestroyGeneratedMesh();
            generatedResolution = effectiveResolution;
            generatedSize = meshSize;
        }

        if (generatedMesh == null || generatedCoverageAngle != coverageMode)
        {
            // Each view shape is generated once per resolution/range. Looking up and down
            // then only swaps a mesh reference, avoiding repeated allocations and uploads.
            if (coverageMode == 360)
            {
                if (surroundMesh == null)
                    surroundMesh = CreateOceanMesh(effectiveResolution, meshSize, coverageAngle);
                generatedMesh = surroundMesh;
            }
            else
            {
                if (forwardMesh == null)
                    forwardMesh = CreateOceanMesh(effectiveResolution, meshSize, coverageAngle);
                generatedMesh = forwardMesh;
            }
            filter.sharedMesh = generatedMesh;
            generatedCoverageAngle = coverageMode;
        }

        if (generatedMaterial == null)
        {
            var shader = Shader.Find("DarkBrine/Procedural Ocean");
            if (shader == null)
                return;
            generatedMaterial = new Material(shader) { name = "Procedural Ocean Material" };
            generatedMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            appliedNearDistance = appliedMidDistance = appliedViewDistance = float.NaN;
        }
        renderer.sharedMaterial = generatedMaterial;
        // Draw after normal opaque props. An island's depth is therefore written first and
        // rejects hidden water pixels automatically, with no per-island setup required.
        generatedMaterial.renderQueue = 2900;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        UpdateWaterDistances();
        generatedMaterial.SetColor("_DeepColor", new Color(0.004f, 0.032f, 0.060f, 1f));
        generatedMaterial.SetColor("_ShallowColor", new Color(0.018f, 0.135f, 0.190f, 1f));
        generatedMaterial.SetColor("_CrestColor", new Color(0.78f, 0.93f, 0.92f, 1f));
        BuildSky();
    }

    int GetVisibleCoverageAngle()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return 360;

        // A downward-looking camera can see water around itself. In all other cases a wide
        // forward sector contains the visible frustum plus turning safety margin.
        float downwardLook = Mathf.Clamp01(-camera.transform.forward.y);
        if (downwardLook > 0.58f)
            return 360;

        float horizontalFov = Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect);
        float coverage = horizontalFov + Mathf.Lerp(58f, 145f, downwardLook);
        return Mathf.Clamp(Mathf.RoundToInt(coverage / 15f) * 15, 120, 255);
    }

    static Mesh CreateOceanMesh(int meshResolution, float meshSize, int coverageAngle)
    {
        var mesh = new Mesh { name = coverageAngle >= 360 ? "Procedural Ocean Mesh" : "Visible Ocean Patch" };
        // Small/medium grids fit 16-bit indices; high quality still uses 32-bit indices.
        mesh.indexFormat = (meshResolution + 1) * (meshResolution + 1) <= 65535
            ? UnityEngine.Rendering.IndexFormat.UInt16
            : UnityEngine.Rendering.IndexFormat.UInt32;

        if (coverageAngle >= 360)
        {
            int side = meshResolution + 1;
            var vertices = new Vector3[side * side];
            var triangles = new int[meshResolution * meshResolution * 6];
            float halfSize = meshSize * 0.5f;
            for (int z = 0; z <= meshResolution; z++)
            for (int x = 0; x <= meshResolution; x++)
            {
                int index = z * side + x;
                vertices[index] = new Vector3(x / (float)meshResolution * meshSize - halfSize, 0f, z / (float)meshResolution * meshSize - halfSize);
            }
            int triangle = 0;
            for (int z = 0; z < meshResolution; z++)
            for (int x = 0; x < meshResolution; x++)
            {
                int a = z * side + x; int b = a + 1; int c = a + side; int d = c + 1;
                triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = b;
                triangles[triangle++] = b; triangles[triangle++] = c; triangles[triangle++] = d;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
        }
        else
        {
            // A regular forward trapezoid uses fewer vertices than the full square while
            // avoiding the long radial triangles that make a polar fan visibly faceted.
            int depthSegments = meshResolution;
            int widthSegments = Mathf.Max(48, Mathf.RoundToInt(meshResolution * 0.85f));
            int side = widthSegments + 1;
            var vertices = new Vector3[(depthSegments + 1) * side];
            var triangles = new int[depthSegments * widthSegments * 6];
            float radius = meshSize * 0.5f;
            // Start only a little behind the player, then concentrate both axes around the
            // camera. This makes the near water smooth while keeping the far horizon cheap.
            float startDistance = -Mathf.Min(180f, radius * 0.05f);
            var columnPositions = new float[side];
            for (int column = 0; column <= widthSegments; column++)
            {
                float centeredColumn = column / (float)widthSegments * 2f - 1f;
                columnPositions[column] = Mathf.Sign(centeredColumn) * Mathf.Pow(Mathf.Abs(centeredColumn), 1.65f);
            }
            for (int row = 0; row <= depthSegments; row++)
            {
                float depth = row / (float)depthSegments;
                // Concentrate rows at the player, where wave silhouette matters, and let the
                // haze-covered far field use progressively larger cells.
                float distributedDepth = Mathf.Pow(depth, 2.2f);
                float halfWidth = Mathf.Lerp(radius * 0.40f, radius * 1.10f, distributedDepth);
                float z = Mathf.Lerp(startDistance, radius, distributedDepth);
                for (int column = 0; column <= widthSegments; column++)
                    vertices[row * side + column] = new Vector3(columnPositions[column] * halfWidth, 0f, z);
            }
            int triangle = 0;
            for (int row = 0; row < depthSegments; row++)
            for (int column = 0; column < widthSegments; column++)
            {
                int a = row * side + column; int b = a + 1; int c = a + side; int d = c + 1;
                triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = b;
                triangles[triangle++] = b; triangles[triangle++] = c; triangles[triangle++] = d;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    void UpdateWaterDistances()
    {
        if (generatedMaterial == null)
            return;
        GetShaderDetailDistances(out float nearDistance, out float midDistance);
        if (appliedNearDistance != nearDistance)
        {
            generatedMaterial.SetFloat(NearDetailDistanceId, nearDistance);
            appliedNearDistance = nearDistance;
        }
        if (appliedMidDistance != midDistance)
        {
            generatedMaterial.SetFloat(MidDetailDistanceId, midDistance);
            appliedMidDistance = midDistance;
        }
        if (appliedViewDistance != renderDistance)
        {
            generatedMaterial.SetFloat(ViewDistanceId, renderDistance);
            appliedViewDistance = renderDistance;
        }
    }

    void GetShaderDetailDistances(out float shaderNearDistance, out float shaderMidDistance)
    {
        float nearCap = effectsQuality switch
        {
            EffectsQuality.Low => 70f,
            EffectsQuality.Medium => 180f,
            _ => 340f
        };
        float midCap = effectsQuality switch
        {
            EffectsQuality.Low => 280f,
            EffectsQuality.Medium => 700f,
            _ => 1350f
        };
        shaderMidDistance = Mathf.Min(midDetailDistance, midCap);
        shaderNearDistance = Mathf.Min(nearDetailDistance, nearCap);
        shaderNearDistance = Mathf.Min(shaderNearDistance, shaderMidDistance - 20f);
    }

    void BuildSky()
    {
        if (skyDome == null)
        {
            Transform existing = transform.Find("Procedural Sky");
            if (existing != null)
                skyDome = existing.gameObject;
            else
            {
                skyDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                skyDome.name = "Procedural Sky";
                skyDome.transform.SetParent(transform, false);
                var collider = skyDome.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
                }
            }
        }
        // A Unity primitive sphere has a 0.5-unit radius.  This keeps its inside surface
        // just before the far clip, so the procedural sky is rendered instead of the camera clear colour.
        skyDome.transform.localScale = Vector3.one * (renderDistance * 2f + 16f);

        if (skyMaterial == null)
        {
            var shader = Shader.Find("DarkBrine/Procedural Sky");
            if (shader == null)
                return;
            skyMaterial = new Material(shader) { name = "Procedural Sky Material" };
            skyMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }

        int cloudDetail = effectsQuality == EffectsQuality.Low ? 2 : effectsQuality == EffectsQuality.Medium ? 4 : 5;
        skyMaterial.SetFloat("_CloudDetail", cloudDetail);
        skyMaterial.SetFloat("_CloudStrength", effectsQuality == EffectsQuality.Low ? 0.42f : effectsQuality == EffectsQuality.Medium ? 0.82f : 0.90f);
        skyMaterial.SetFloat("_CloudCoverage", cloudiness);
        // The high cloud deck crosses a visible portion of the sky within a play session;
        // the low deck remains slower, so the layers do not drift in lockstep.
        skyMaterial.SetFloat("_CloudSpeed", Mathf.Lerp(0.055f, 0.24f, cloudMotion));
        skyMaterial.SetFloat("_SunGlow", sunlightIntensity);
        skyMaterial.SetColor("_HorizonColor", new Color(0.38f, 0.62f, 0.76f, 1f));
        skyMaterial.SetColor("_ZenithColor", new Color(0.025f, 0.16f, 0.39f, 1f));
        skyMaterial.SetColor("_CloudColor", new Color(0.92f, 0.96f, 1f, 1f));
        skyDome.GetComponent<MeshRenderer>().sharedMaterial = skyMaterial;
        // The same procedural material is also the real camera skybox. This avoids relying on
        // a finite sphere and makes clouds render identically from low flight and high flight.
        RenderSettings.skybox = skyMaterial;
    }

    void LateUpdate()
    {
        if (skyDome == null || skyMaterial == null)
            return;

        Camera camera = Camera.main;
        if (camera != null)
        {
            // The ocean follows the player. Geometry outside the configured view range is never needed.
            transform.position = new Vector3(camera.transform.position.x, 0f, camera.transform.position.z);
            transform.rotation = Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f);
            if (camera.farClipPlane != renderDistance + 20f)
                camera.farClipPlane = renderDistance + 20f;
            if (camera.clearFlags != CameraClearFlags.Skybox)
                camera.clearFlags = CameraClearFlags.Skybox;
            skyDome.transform.position = camera.transform.position;
            UpdateWaterDistances();
            int coverageAngle = GetVisibleCoverageAngle();
            if ((coverageAngle >= 360 ? 360 : 1) != generatedCoverageAngle)
                BuildOcean();
        }

        // Retain live editor selection; only cache during play, reacquiring destroyed or
        // deactivated lights. This scene's sun no longer requires a scene-wide search/frame.
        if (!Application.isPlaying || cachedSun == null || !cachedSun.gameObject.activeInHierarchy)
            cachedSun = FindFirstObjectByType<Light>();
        Light sun = cachedSun;
        if (sun != null && sun.type == LightType.Directional)
            skyMaterial.SetVector(SunDirectionId, -sun.transform.forward);
    }

    void OnDisable()
    {
        DestroyGeneratedMesh();
        if (generatedMaterial != null)
        {
            if (Application.isPlaying) Destroy(generatedMaterial); else DestroyImmediate(generatedMaterial);
        }
        generatedMaterial = null;
        cachedSun = null;
        if (skyMaterial != null)
        {
            if (RenderSettings.skybox == skyMaterial)
                RenderSettings.skybox = null;
            if (Application.isPlaying) Destroy(skyMaterial); else DestroyImmediate(skyMaterial);
            skyMaterial = null;
        }
    }

    void DestroyGeneratedMesh()
    {
        if (forwardMesh != null)
        {
            if (Application.isPlaying) Destroy(forwardMesh); else DestroyImmediate(forwardMesh);
        }
        if (surroundMesh != null)
        {
            if (Application.isPlaying) Destroy(surroundMesh); else DestroyImmediate(surroundMesh);
        }
        forwardMesh = null;
        surroundMesh = null;
        generatedMesh = null;
        generatedResolution = 0;
        generatedSize = 0f;
        generatedCoverageAngle = 0;
    }

}
