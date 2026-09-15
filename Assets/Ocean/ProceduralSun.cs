using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public sealed class ProceduralSun : MonoBehaviour
{
    [Header("High Sky Position")]
    [Range(0.35f, 0.95f)] public float elevation = 0.78f;
    [Range(0.001f, 0.03f)] public float apparentSize = 0.012f;
    public float maximumDistance = 1400f;

    Material generatedMaterial;

    void OnEnable()
    {
        var renderer = GetComponent<MeshRenderer>();
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

    void LateUpdate() => PositionHighSun();

    void PositionHighSun()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        // Keep the visual disc well above the horizon and inside the current camera range.
        // This also lets free-flight cameras move without leaving the sun behind in world space.
        Vector3 directionToSun = new Vector3(-0.32f, elevation, 0.78f).normalized;
        float distance = Mathf.Min(maximumDistance, camera.farClipPlane * 0.72f);
        distance = Mathf.Max(distance, 80f);
        transform.position = camera.transform.position + directionToSun * distance;
        transform.localScale = Vector3.one * (distance * apparentSize);

        Light directional = FindFirstObjectByType<Light>();
        if (directional != null && directional.type == LightType.Directional)
            directional.transform.rotation = Quaternion.LookRotation(-directionToSun, Vector3.up);
    }

    void OnDisable()
    {
        if (generatedMaterial == null) return;
        if (Application.isPlaying) Destroy(generatedMaterial); else DestroyImmediate(generatedMaterial);
        generatedMaterial = null;
    }
}
