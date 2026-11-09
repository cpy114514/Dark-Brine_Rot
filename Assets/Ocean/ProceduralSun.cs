using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public sealed class ProceduralSun : MonoBehaviour
{
    // Preserve existing serialized scene settings; the disk is now part of the sky.
    [Header("Sky Sun")]
    [Range(0.35f, 0.95f)] public float elevation = 0.78f;
    [Range(0.001f, 0.04f)] public float apparentSize = 0.022f;
    [Min(80f)] public float maximumDistance = 2200f;
    Vector4 skyDisk;
    static readonly int DiskId = Shader.PropertyToID("_BrineSolarDisk");
    static readonly int ColorId = Shader.PropertyToID("_BrineSolarColor");
    void OnEnable()
    {
        HideWorldSphere();
        SetAtmosphere(new Color(1f,.96f,.87f), Color.white, 3f, true);
    }
    void HideWorldSphere()
    {
        // Only sky pixels should contain the sun, never a finite HDR world sphere.
        var renderer = GetComponent<MeshRenderer>();
        if (renderer) renderer.enabled = false;
        var collider = GetComponent<Collider>();
        if (collider) collider.enabled = false;
    }
    public void SetAtmosphere(Color core, Color glow, float intensity, bool visible)
    {
        Shader.SetGlobalColor(ColorId, core);
        float radius = Mathf.Clamp(apparentSize * .5f, .001f, .0065f);
        skyDisk = new Vector4(radius, Mathf.Clamp(intensity * .4f, 0f, 1.5f), visible ? 1f : 0f, 0);
        LateUpdate();
    }
    void LateUpdate()
    {
        var camera = Camera.main;
        bool circuit = Application.isPlaying && camera && SkyboardRingRally.UsesCircuitCamera(camera);
        Shader.SetGlobalVector(DiskId, circuit ? Vector4.zero : skyDisk);
    }
    void OnDisable() => Shader.SetGlobalVector(DiskId, Vector4.zero);
}
