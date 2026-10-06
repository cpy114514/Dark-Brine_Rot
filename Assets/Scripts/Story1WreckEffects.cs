using UnityEngine;

/// <summary>Contact-local spray and splinters; full hull pieces use wreck physics.</summary>
public sealed class Story1WreckEffects : MonoBehaviour
{
    public int ImpactCount {get; private set;}
    ParticleSystem water, wood;
    Material waterMaterial;
    OceanWorld ocean;
    public void Initialize(Material hullMaterial, Mesh hullMesh)
    {
        ocean = FindFirstObjectByType<OceanWorld>();
        waterMaterial = NaturalParticleEffects.Material("Contact water spray", new Color(.88f, .92f, .94f));
        water = NaturalParticleEffects.Emitter(transform, "Contact water droplets", waterMaterial, 480, 1f);
        var renderer = water.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = .025f; renderer.lengthScale = .8f;
        var shrink = water.sizeOverLifetime; shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, .35f));
        wood = NaturalParticleEffects.Emitter(transform, "Small hull splinters", hullMaterial, 70, 1f);
        var woodRenderer = wood.GetComponent<ParticleSystemRenderer>();
        woodRenderer.renderMode = ParticleSystemRenderMode.Mesh; woodRenderer.mesh = hullMesh;
    }
    public void Impact(Vector3 at, Vector3 direction, float strength, bool splinters = false)
    {
        if (water == null) return;
        ImpactCount++;
        float surface = ocean != null ? ocean.SampleSurfaceHeight(at, Time.time) : at.y;
        // A dry weapon hit reads through recoil, rather than an airborne fountain.
        if (at.y <= surface + 1.4f)
        {
            at.y = surface + .08f;
            int count = Mathf.Clamp(Mathf.RoundToInt(18 * strength), 5, 45);
            for (int i = 0; i < count; i++)
            {
                Vector3 spread = Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up);
                water.Emit(new ParticleSystem.EmitParams {
                    position = at + spread * (.25f + strength * .4f),
                    velocity = spread * Random.Range(1f, 3f) + Vector3.up * Random.Range(1.5f, 4f) * Mathf.Sqrt(Mathf.Max(0, strength))
                        + direction * Mathf.Min(strength * 2f, 5f),
                    startSize = Random.Range(.035f, .12f) * Mathf.Min(1 + strength * .2f, 1.6f),
                    startLifetime = Random.Range(.3f, .7f), startColor = new Color(.9f, .94f, .96f, .62f)
                }, 1);
            }
        }
        if (splinters) EmitSplinters(at, direction);
    }
    void EmitSplinters(Vector3 at, Vector3 direction)
    {
        for (int i = 0; i < 24; i++) wood.Emit(new ParticleSystem.EmitParams {
            position = at + Random.insideUnitSphere * 1.5f,
            velocity = Random.insideUnitSphere * 5 + Vector3.up * 4 + direction * Random.Range(3f, 8f),
            startSize = Random.Range(.07f, .25f), startLifetime = Random.Range(.55f, 1.4f),
            rotation3D = Random.insideUnitSphere * 180, angularVelocity3D = Random.insideUnitSphere * 160
        }, 1);
    }
    public void ShipImpact(Vector3 at, Vector3 direction)
    {
        if (water == null) return;
        ImpactCount++; EmitSplinters(at, direction);
        Vector3 across = Vector3.Cross(Vector3.up, direction).normalized;
        float surface = ocean != null ? ocean.SampleSurfaceHeight(at, Time.time) : at.y;
        for (int i = 0; i < 100; i++)
        {
            Vector3 contact = at + across * Random.Range(-6f, 6f);
            contact.y = ocean != null ? ocean.SampleSurfaceHeight(contact, Time.time) + .1f : surface;
            water.Emit(new ParticleSystem.EmitParams {
                position = contact,
                velocity = direction * Random.Range(6f, 15f) + across * Random.Range(-4f, 4f) + Vector3.up * Random.Range(4f, 11f),
                startSize = Random.Range(.10f, .42f), startLifetime = Random.Range(.5f, 1.1f),
                startColor = new Color(.9f, .94f, .96f, Random.Range(.35f, .6f))
            }, 1);
        }
    }
    void OnDestroy() {if (waterMaterial != null) Destroy(waterMaterial);}
}
