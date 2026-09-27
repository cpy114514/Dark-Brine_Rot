using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public sealed class ProceduralSun : MonoBehaviour
{
    [Header("High Sky Position")]
    [Range(0.35f, 0.95f)] public float elevation = 0.78f;
    [Range(0.001f, 0.04f)] public float apparentSize = 0.022f;
    public float maximumDistance = 1400f;

    Material generatedMaterial;
    MeshRenderer sunRenderer;
    Light cachedLight;

    void OnEnable()
    {
        var renderer = GetComponent<MeshRenderer>();
        sunRenderer = renderer;
        var shader = Shader.Find("DarkBrine/Procedural Sun");
        if (shader == null)
            return;

        generatedMaterial = new Material(shader) { name = "Procedural Sun Material" };
        generatedMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        renderer.sharedMaterial = generatedMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        PositionHighSun();
    }

    public void SetAtmosphere(Color core, Color glow, float intensity, bool visible)
    {
        if (generatedMaterial != null)
        {
            generatedMaterial.SetColor("_CoreColor", core);
            generatedMaterial.SetColor("_GlowColor", glow);
            generatedMaterial.SetFloat("_Intensity", intensity);
        }
        if (sunRenderer != null && sunRenderer.enabled != visible)
            sunRenderer.enabled = visible;
    }

    void LateUpdate() => PositionHighSun();

    void PositionHighSun()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        if (!Application.isPlaying || cachedLight == null || !cachedLight.gameObject.activeInHierarchy)
        {
            foreach (var candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (candidate.type == LightType.Directional)
                {
                    cachedLight = candidate;
                    break;
                }
        }
        DayNightCycle cycle = cachedLight != null ? cachedLight.GetComponent<DayNightCycle>() : null;
        Vector3 directionToSun = cycle != null
            ? cycle.SunDirection
            : cachedLight != null && cachedLight.type == LightType.Directional
                ? -cachedLight.transform.forward
                : new Vector3(-0.32f, elevation, 0.78f).normalized;
        // Keep the visible sun locked to the animated directional light and camera.
        float distance = Mathf.Min(maximumDistance, camera.farClipPlane * 0.72f);
        distance = Mathf.Max(distance, 80f);
        transform.position = camera.transform.position + directionToSun * distance;
        transform.localScale = Vector3.one * (distance * apparentSize);

    }

    void OnDisable()
    {
        cachedLight = null;
        if (generatedMaterial == null) return;
        if (Application.isPlaying) Destroy(generatedMaterial); else DestroyImmediate(generatedMaterial);
        generatedMaterial = null;
    }
}
