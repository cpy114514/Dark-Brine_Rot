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
    [Range(0f, 1f)] public float cloudMotion = 0.75f;
    [Range(0f, 1f)] public float sunlightIntensity = 0.72f;

    Mesh generatedMesh;
    Material generatedMaterial;
    GameObject skyDome;
    Material skyMaterial;
    int generatedResolution;
    float generatedSize;
    int generatedCoverageAngle;

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

        if (generatedMesh == null || generatedResolution != effectiveResolution || !Mathf.Approximately(generatedSize, meshSize) || generatedCoverageAngle != coverageMode)
        {
            DestroyGeneratedMesh();
            generatedMesh = CreateOceanMesh(effectiveResolution, meshSize, coverageAngle);
            filter.sharedMesh = generatedMesh;
            generatedResolution = effectiveResolution;
            generatedSize = meshSize;
            generatedCoverageAngle = coverageMode;
        }

        if (generatedMaterial == null)
        {
            var shader = Shader.Find("DarkBrine/Procedural Ocean");
            if (shader == null)
                return;
            generatedMaterial = new Material(shader) { name = "Procedural Ocean Material" };
            generatedMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }
        renderer.sharedMaterial = generatedMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        GetShaderDetailDistances(out float shaderNearDistance, out float shaderMidDistance);
        generatedMaterial.SetFloat("_NearDetailDistance", shaderNearDistance);
        generatedMaterial.SetFloat("_MidDetailDistance", shaderMidDistance);
        generatedMaterial.SetFloat("_ViewDistance", renderDistance);
        generatedMaterial.SetColor("_CrestColor", new Color(0.74f, 0.91f, 0.88f, 1f));
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
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        if (coverageAngle >= 360)
        {
            int side = meshResolution + 1;
            var vertices = new Vector3[side * side];
            var triangles = new int[meshResolution * meshResolution * 6];
            var uvs = new Vector2[vertices.Length];
            float halfSize = meshSize * 0.5f;
            for (int z = 0; z <= meshResolution; z++)
            for (int x = 0; x <= meshResolution; x++)
            {
                int index = z * side + x;
                vertices[index] = new Vector3(x / (float)meshResolution * meshSize - halfSize, 0f, z / (float)meshResolution * meshSize - halfSize);
                uvs[index] = new Vector2(x / (float)meshResolution, z / (float)meshResolution);
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
            mesh.uv = uvs;
        }
        else
        {
            // A regular forward trapezoid uses fewer vertices than the full square while
            // avoiding the long radial triangles that make a polar fan visibly faceted.
            int depthSegments = meshResolution;
            int widthSegments = Mathf.Max(48, Mathf.RoundToInt(meshResolution * 0.70f));
            int side = widthSegments + 1;
            var vertices = new Vector3[(depthSegments + 1) * side];
            var triangles = new int[depthSegments * widthSegments * 6];
            var uvs = new Vector2[vertices.Length];
            float radius = meshSize * 0.5f;
            float startDistance = -radius * 0.14f;
            for (int row = 0; row <= depthSegments; row++)
            for (int column = 0; column <= widthSegments; column++)
            {
                int index = row * side + column;
                float depth = row / (float)depthSegments;
                float halfWidth = Mathf.Lerp(radius * 0.40f, radius * 1.10f, depth);
                vertices[index] = new Vector3(Mathf.Lerp(-halfWidth, halfWidth, column / (float)widthSegments), 0f, Mathf.Lerp(startDistance, radius, depth));
                uvs[index] = new Vector2(column / (float)widthSegments, depth);
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
            mesh.uv = uvs;
        }

        mesh.RecalculateBounds();
        return mesh;
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
        skyMaterial.SetFloat("_CloudStrength", effectsQuality == EffectsQuality.Low ? 0.42f : effectsQuality == EffectsQuality.Medium ? 0.72f : 0.86f);
        skyMaterial.SetFloat("_CloudCoverage", cloudiness);
        // The high cloud deck crosses a visible portion of the sky within a play session;
        // the low deck remains slower, so the layers do not drift in lockstep.
        skyMaterial.SetFloat("_CloudSpeed", Mathf.Lerp(0.06f, 0.32f, cloudMotion));
        skyMaterial.SetFloat("_SunGlow", sunlightIntensity);
        skyMaterial.SetColor("_CloudColor", new Color(0.96f, 0.98f, 1f, 1f));
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
            camera.farClipPlane = renderDistance + 20f;
            camera.clearFlags = CameraClearFlags.Skybox;
            skyDome.transform.position = camera.transform.position;
            if (generatedMaterial != null)
            {
                GetShaderDetailDistances(out float shaderNearDistance, out float shaderMidDistance);
                generatedMaterial.SetFloat("_NearDetailDistance", shaderNearDistance);
                generatedMaterial.SetFloat("_MidDetailDistance", shaderMidDistance);
                generatedMaterial.SetFloat("_ViewDistance", renderDistance);
            }
            int coverageAngle = GetVisibleCoverageAngle();
            if ((coverageAngle >= 360 ? 360 : 1) != generatedCoverageAngle)
                BuildOcean();
        }

        Light sun = FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
            skyMaterial.SetVector("_SunDirection", -sun.transform.forward);
    }

    void OnDisable()
    {
        DestroyGeneratedMesh();
        if (generatedMaterial == null) return;
        if (Application.isPlaying) Destroy(generatedMaterial); else DestroyImmediate(generatedMaterial);
        generatedMaterial = null;
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
        if (generatedMesh == null) return;
        if (Application.isPlaying) Destroy(generatedMesh); else DestroyImmediate(generatedMesh);
        generatedMesh = null;
        generatedResolution = 0;
        generatedSize = 0f;
        generatedCoverageAngle = 0;
    }

}
