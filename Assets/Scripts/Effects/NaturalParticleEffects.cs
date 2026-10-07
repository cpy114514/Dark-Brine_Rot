using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Neutral, alpha-blended particles that respond to scene lighting and depth.</summary>
public static class NaturalParticleEffects
{
    static Texture2D smokeMask;
    static Material smokeTemplate;

    public static Material SmokeMaterial(string name, Color tint)
    {
        // The Resources material also keeps the textured shader variant in player builds.
        if (smokeTemplate == null) smokeTemplate = Resources.Load<Material>("NaturalVfx/SmokeMaterial");
        var material = smokeTemplate != null ? new Material(smokeTemplate) {name = name} : Material(name, tint);
        material.SetColor("_Tint", tint);
        if (smokeMask == null) smokeMask = Resources.Load<Texture2D>("NaturalVfx/SmokeMask");
        if (smokeMask != null)
        {
            material.SetTexture("_MaskTex", smokeMask);
            material.EnableKeyword("_PARTICLE_MASK");
        }
        return material;
    }

    // Cosmetic bursts can thin out with distance; damage and encounter events remain independent.
    public static int BurstBudget(Vector3 position, int count)
    {
        var camera = Camera.main;
        if (camera == null) return count;
        float distance = Vector3.Distance(camera.transform.position, position);
        if (distance >= 140f) return 0;
        return Mathf.CeilToInt(count * Mathf.Lerp(1f, .2f, Mathf.InverseLerp(35f, 100f, distance)));
    }
    public static Material Material(string name, Color tint, bool soft = true)
    {
        var shader = Shader.Find("DarkBrine/Procedural Water VFX");
        var material = new Material(shader) {name = name};
        material.SetColor("_Tint", tint);
        material.SetFloat("_Softness", soft ? 1f : 0f);
        return material;
    }

    public static ParticleSystem Emitter(Transform parent, string name, Material material, int budget, float gravity)
    {
        var holder = new GameObject(name); holder.transform.SetParent(parent, false);
        var particles = holder.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        // Manual bursts may occur long after initialization; keep simulation ready.
        main.playOnAwake = false; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = budget; main.gravityModifier = gravity; main.startSpeed = 0f;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.enabled = false;
        var color = particles.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] {new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1)},
            new[] {new GradientAlphaKey(.65f, 0), new GradientAlphaKey(.7f, .08f), new GradientAlphaKey(.35f, .5f), new GradientAlphaKey(0, 1)});
        color.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false; renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        particles.Play();
        return particles;
    }
}
