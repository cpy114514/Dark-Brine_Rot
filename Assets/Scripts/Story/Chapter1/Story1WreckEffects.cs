using UnityEngine;

/// <summary>Contact-local spray and splinters; full hull pieces use wreck physics.</summary>
public sealed class Story1WreckEffects : MonoBehaviour
{
    public int ImpactCount {get; private set;}
    ParticleSystem water, wood;
    Material waterMaterial;
    Mesh splinterMesh;
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
        woodRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        // A whole ship-hull mesh per tiny particle multiplies geometry and reads as miniature ships.
        splinterMesh=new Mesh {name="Twelve triangle wood splinter"};
        splinterMesh.vertices=new[]{new Vector3(-.08f,-.5f,-.03f),new Vector3(.08f,-.5f,-.03f),new Vector3(.08f,.5f,-.03f),new Vector3(-.08f,.5f,-.03f),
            new Vector3(-.08f,-.5f,.03f),new Vector3(.08f,-.5f,.03f),new Vector3(.08f,.5f,.03f),new Vector3(-.08f,.5f,.03f)};
        splinterMesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        splinterMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up,Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
        splinterMesh.RecalculateNormals();splinterMesh.RecalculateBounds();splinterMesh.UploadMeshData(true);
        woodRenderer.mesh=splinterMesh;woodRenderer.enableGPUInstancing=true;
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
            int count = NaturalParticleEffects.BurstBudget(at, Mathf.Clamp(Mathf.RoundToInt(14 * strength), 5, 32));
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
            startSize = Random.Range(.3f, 1.1f), startLifetime = Random.Range(.55f, 1.4f),
            rotation3D = Random.insideUnitSphere * 180, angularVelocity3D = Random.insideUnitSphere * 160
        }, 1);
    }
    public void ShipImpact(Vector3 at, Vector3 direction)
    {
        if (water == null) return;
        ImpactCount++; EmitSplinters(at, direction);
        Vector3 across = Vector3.Cross(Vector3.up, direction).normalized;
        var surface = OceanSurfaceSampler.Capture(ocean, Camera.main, Time.time);
        int count = NaturalParticleEffects.BurstBudget(at, 64);
        for (int i = 0; i < count; i++)
        {
            Vector3 contact = at + across * Random.Range(-6f, 6f);
            contact.y = ocean != null ? surface.Height(contact) + .1f : at.y;
            water.Emit(new ParticleSystem.EmitParams {
                position = contact,
                velocity = direction * Random.Range(5f, 12f) + across * Random.Range(-4f, 4f) + Vector3.up * Random.Range(3f, 7f),
                startSize = Random.Range(.045f, .18f), startLifetime = Random.Range(.4f, .95f),
                startColor = new Color(.9f, .94f, .96f, Random.Range(.35f, .6f))
            }, 1);
        }

    }
    void OnDestroy() {if (waterMaterial != null) Destroy(waterMaterial);if(splinterMesh!=null)Destroy(splinterMesh);}

    static Mesh CreateSplinterMesh(Mesh hull)
    {
        // Tiny chips should not be scaled replicas of a curved hull fragment.
        var corners = new[] {new Vector3(-.5f,-.07f,-.13f),new Vector3(.5f,-.04f,-.13f),
            new Vector3(.5f,.04f,-.13f),new Vector3(-.5f,.07f,-.13f),
            new Vector3(-.5f,-.07f,.13f),new Vector3(.5f,-.04f,.13f),
            new Vector3(.5f,.04f,.13f),new Vector3(-.5f,.07f,.13f)};
        int[] faces = {0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        var vertices=new Vector3[faces.Length];var uv=new Vector2[faces.Length];var triangles=new int[faces.Length];
        var sourceUv=hull!=null?hull.uv:null;
        for(int i=0;i<faces.Length;i++)
        {
            vertices[i]=corners[faces[i]];triangles[i]=i;
            uv[i]=sourceUv!=null&&sourceUv.Length>=3?sourceUv[i%3]:Vector2.zero;
        }
        var mesh=new Mesh{name="Tapered wood chip (12 triangles)"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Release(Object value) {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
