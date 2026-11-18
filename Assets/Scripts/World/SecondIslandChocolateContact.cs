using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shallow, viscous contact with Island 2's rendered lake and river.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandChocolateContact : MonoBehaviour
{
    [SerializeField] MeshRenderer lake;
    [SerializeField] MeshRenderer river;
    [SerializeField] SecondIslandChocolateWaterway waterway;
    [SerializeField] SkinnedMeshRenderer sahurSkin;
    [SerializeField] Material chocolateSkin;
    [SerializeField] Material dropletMaterial;
    [SerializeField] Mesh dropletMesh;

    const int RiverColumns = 21;
    const int ImpactCount = 6;
    static readonly int ImpactsId = Shader.PropertyToID("_ChocolateImpacts");
    static readonly int CurrentsId = Shader.PropertyToID("_ChocolateImpactCurrents");
    static readonly int CoatingId = Shader.PropertyToID("_ChocolateCoating");
    readonly Vector4[] impacts = new Vector4[ImpactCount];
    readonly Vector4[] currents = new Vector4[ImpactCount];
    MaterialPropertyBlock surfaceProperties;
    Material dryMaterial, coatedMaterial;
    CharacterController capsule;
    ParticleSystem droplets;
    LineRenderer strand;
    Vector3[] riverEdges;
    Vector3 lakeCenter, lastFeet, lastStep, strandStart;
    Bounds riverBounds;
    Vector2 lakeRadii;
    float nextStep, stain, spatter, dryTime, strandEnd, dripTime;
    bool hasLastFeet, wasWet, wasGrounded, wet;
    int impactIndex;

    public float Depth { get; private set; }
    public float Coating => stain;
    public float Spatter => spatter;
    public bool Stretching => strand && strand.enabled;
    public int Splashes { get; private set; }
    public int ActiveParticles => droplets ? droplets.particleCount : 0;
    public Vector3 Current { get; private set; }
    public float SpeedFactor => Mathf.Lerp(1, .63f, Mathf.InverseLerp(.04f, 1.18f, Depth));

    void Awake()
    {
        if ((!waterway && gameObject.scene.path != SecondIslandAudio.ScenePath) || !lake || !river ||
            !sahurSkin || !chocolateSkin || !dropletMaterial || !dropletMesh)
        {
            enabled = false;
            return;
        }

        capsule = GetComponent<CharacterController>();
        surfaceProperties = new MaterialPropertyBlock();
        if (!waterway) CacheLegacyGeometry();
        dryMaterial = sahurSkin.sharedMaterial;
        coatedMaterial = new Material(chocolateSkin) { name = "Sahur runtime chocolate coating" };
    }

    void CacheLegacyGeometry()
    {
        var lakeMesh = lake.GetComponent<MeshFilter>().sharedMesh;
        lakeCenter = lake.transform.TransformPoint(lakeMesh.vertices[0]);
        // The authored shoreline uses the same irregular ellipse as the sound field.
        lakeRadii = new Vector2(85, 62);
        var ribbon = river.GetComponent<MeshFilter>().sharedMesh.vertices;
        riverBounds = river.bounds;
        riverEdges = new Vector3[ribbon.Length / RiverColumns * 2];
        for (int i = 0; i < riverEdges.Length / 2; i++)
        {
            riverEdges[2 * i] = river.transform.TransformPoint(ribbon[i * RiverColumns]);
            riverEdges[2 * i + 1] = river.transform.TransformPoint(ribbon[i * RiverColumns + RiverColumns - 1]);
        }

    }

    public bool TrySample(Vector3 point, out Vector3 surface, out Vector3 current)
    {
        surface = point;
        current = Vector3.zero;
        if (!isActiveAndEnabled || !lake || !lake.gameObject.activeInHierarchy)
            return false;
        if (waterway) return waterway.TrySample(point, out surface, out current);
        if (riverEdges == null) return false;

        bool found = false;
        var offset = new Vector2(point.x - lakeCenter.x, point.z - lakeCenter.z);
        var normalized = offset / lakeRadii;
        float angle = Mathf.Atan2(normalized.y, normalized.x);
        float edge = 1 + .06f * Mathf.Sin(angle * 3 + .5f) + .035f * Mathf.Cos(angle * 7);
        if (normalized.sqrMagnitude <= edge * edge)
        {
            surface.y = lakeCenter.y;
            current = new Vector3(offset.y, 0, -offset.x).normalized * .09f;
            found = true;
        }

        Vector2 at = new Vector2(point.x, point.z);
        if (point.x < riverBounds.min.x || point.x > riverBounds.max.x ||
            point.z < riverBounds.min.z || point.z > riverBounds.max.z)
            return found;

        // Query the actual ribbon cross sections so bends and shallow banks match the picture.
        for (int i = 2; i < riverEdges.Length; i += 2)
        {
            Vector3 a = riverEdges[i - 2], b = riverEdges[i - 1];
            Vector3 c = riverEdges[i], d = riverEdges[i + 1];
            if (at.x < Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)) ||
                at.x > Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)) ||
                at.y < Mathf.Min(Mathf.Min(a.z, b.z), Mathf.Min(c.z, d.z)) ||
                at.y > Mathf.Max(Mathf.Max(a.z, b.z), Mathf.Max(c.z, d.z)) ||
                point.y < Mathf.Min(a.y, c.y) - 2.5f || point.y > Mathf.Max(a.y, c.y) + 5)
                continue;
            if (!HeightInTriangle(at, a, b, c, out float y) &&
                !HeightInTriangle(at, b, d, c, out y))
                continue;
            if (found && surface.y > y) continue;
            surface.y = y;
            var downstream = (c + d - a - b) * .5f;
            float drop = -downstream.y;
            downstream.y = 0;
            float grade = drop / Mathf.Max(.05f, downstream.magnitude);
            current = downstream.normalized * Mathf.Lerp(.26f, .68f, Mathf.Clamp01(grade * 3));
            found = true;
        }
        return found;
    }

    public Vector3 CorrectSavedPosition(Vector3 position)
    {
        if (!waterway || !waterway.IsInChangedGround(position)) return position;
        var experience = GetComponent<MarshmallowIslandExperience>();
        if (!experience || !experience.surface || !experience.surface.TryGetComponent(out MeshCollider ground))
            return position;
        if (!ground.Raycast(new Ray(new Vector3(position.x, ground.bounds.max.y + 2f, position.z),
            Vector3.down), out var hit, ground.bounds.size.y + 4f)) return position;
        var body = GetComponent<CharacterController>();
        float sole = (body.center.y - body.height * .5f) * Mathf.Abs(transform.lossyScale.y);
        float clearance = position.y + sole - hit.point.y;
        if (clearance < -.15f && clearance > -90f)
            position.y = hit.point.y - sole + .09f;
        return position;
    }

    static bool HeightInTriangle(Vector2 p, Vector3 a, Vector3 b, Vector3 c, out float y)
    {
        var ab = new Vector2(b.x - a.x, b.z - a.z);
        var ac = new Vector2(c.x - a.x, c.z - a.z);
        var ap = p - new Vector2(a.x, a.z);
        float det = ab.x * ac.y - ab.y * ac.x;
        y = a.y;
        if (Mathf.Abs(det) < .00001f) return false;
        float u = (ap.x * ac.y - ap.y * ac.x) / det;
        float v = (ab.x * ap.y - ab.y * ap.x) / det;
        y += (b.y - a.y) * u + (c.y - a.y) * v;
        return u >= -.0001f && v >= -.0001f && u + v <= 1.0001f;
    }

    Vector3 Feet => new Vector3(transform.position.x, capsule.bounds.min.y, transform.position.z);

    public void SampleBeforeMovement()
    {
        Vector3 feet = Feet;
        wet = TrySample(feet, out var surface, out var current) &&
            surface.y - feet.y is > .025f and < 2.5f;
        Depth = wet ? surface.y - feet.y : 0;
        Current = wet ? current : Vector3.zero;
    }

    public void AfterMovement(bool grounded, float impactSpeed)
    {
        if (!isActiveAndEnabled || Time.deltaTime <= 0) return;
        Vector3 feet = Feet;
        bool relocated = !hasLastFeet || (feet - lastFeet).sqrMagnitude > 225;
        if (relocated)
        {
            ResetContact();
            lastStep = feet;
        }

        wet = TrySample(feet, out var surface, out var flow) && surface.y - feet.y is > .025f and < 2.5f;
        Depth = wet ? surface.y - feet.y : 0;
        Current = wet ? flow : Vector3.zero;
        float horizontalTravel = Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(lastStep.x, lastStep.z));
        bool landing = !relocated && wet && grounded && !wasGrounded && impactSpeed > 5.8f;
        if (wet)
        {
            dryTime = 0;
            float coverage = Mathf.InverseLerp(.04f, 1.25f, Depth);
            stain = Mathf.MoveTowards(stain, Mathf.Max(stain, Mathf.Lerp(.28f, 1, coverage)), Time.deltaTime * 1.9f);
            if (landing) spatter = Mathf.Max(spatter, Mathf.Lerp(.45f, 1, Mathf.InverseLerp(6, 15, impactSpeed)));
            if (!relocated && (landing || !wasWet || (grounded && horizontalTravel > 1.5f && Time.time >= nextStep)))
            {
                Splash(surface, flow, landing ? 1 : .34f + coverage * .25f);
                lastStep = feet;
                nextStep = Time.time + .19f;
            }
        }
        else
        {
            if (wasWet && !relocated && feet.y > lastFeet.y + .035f && !grounded &&
                TrySample(lastFeet, out var departure, out var departureFlow))
            {
                BeginStrand(departure);
                Splash(departure, departureFlow, .38f);
            }
            dryTime += Time.deltaTime;
            float runoff = dryTime < 4 ? .024f : .075f;
            stain = Mathf.MoveTowards(stain, 0, runoff * Time.deltaTime);
            spatter = Mathf.MoveTowards(spatter, 0, runoff * .9f * Time.deltaTime);
            if (stain > .23f && dryTime < 7 && Time.time >= dripTime)
            {
                EnsureEffects();
                EmitDrop(feet + transform.right * Random.Range(-.48f, .48f) + Vector3.up * .55f,
                    Vector3.down * 1.1f, .085f, .48f);
                dripTime = Time.time + .23f;
            }
        }

        UpdateCoating();
        UpdateStrand(feet);
        lastFeet = feet;
        hasLastFeet = true;
        wasWet = wet;
        wasGrounded = grounded;
    }

    void UpdateCoating()
    {
        if (!sahurSkin) return;
        float amount = Mathf.Max(stain, spatter);
        if (amount > .001f)
        {
            if (sahurSkin.sharedMaterial != coatedMaterial) sahurSkin.sharedMaterial = coatedMaterial;
            coatedMaterial.SetVector(CoatingId, new Vector4(stain, spatter, 0, 0));
        }
        else if (sahurSkin.sharedMaterial == coatedMaterial)
            sahurSkin.sharedMaterial = dryMaterial;
    }

    void Splash(Vector3 surface, Vector3 flow, float energy)
    {
        EnsureEffects();
        int slot = impactIndex++ % ImpactCount;
        impacts[slot] = new Vector4(surface.x, surface.y, surface.z, Time.time);
        currents[slot] = new Vector4(flow.x, flow.z, energy, 0);
        UploadImpacts(lake);
        UploadImpacts(river);
        for (int i = 0; i < Mathf.RoundToInt(4 + energy * 11); i++)
        {
            Vector2 offset = Random.insideUnitCircle;
            Vector3 outward = new Vector3(offset.x, 0, offset.y);
            Vector3 velocity = outward * (1.2f + 3.1f * energy) + flow * 2 +
                Vector3.up * Random.Range(1.25f, 2.1f + 3.6f * energy);
            EmitDrop(surface + outward * .45f + Vector3.up * .035f, velocity,
                Random.Range(.085f, .22f) * (1 + energy * .4f), Random.Range(.4f, .75f));
        }
        Splashes++;
    }

    void UploadImpacts(Renderer target)
    {
        if (!target) return;
        target.GetPropertyBlock(surfaceProperties);
        surfaceProperties.SetVectorArray(ImpactsId, impacts);
        surfaceProperties.SetVectorArray(CurrentsId, currents);
        target.SetPropertyBlock(surfaceProperties);
    }

    void EnsureEffects()
    {
        if (droplets) return;
        var effects = new GameObject("Chocolate splashes and runoff");
        effects.transform.SetParent(transform, false);
        droplets = effects.AddComponent<ParticleSystem>();
        droplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = droplets.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = 80;
        main.gravityModifier = 1.9f;
        var emission = droplets.emission;
        emission.enabled = false;
        var shape = droplets.shape;
        shape.enabled = false;
        var drag = droplets.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 9;
        drag.dampen = .17f;
        drag.drag = .65f;
        var shrink = droplets.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
            new Keyframe(0, .83f), new Keyframe(.22f, 1), new Keyframe(.79f, .88f), new Keyframe(1, 0)));
        var renderer = droplets.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = dropletMesh;
        renderer.sharedMaterial = dropletMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        droplets.Play();

        var line = new GameObject("Stretching chocolate thread");
        line.transform.SetParent(effects.transform, false);
        strand = line.AddComponent<LineRenderer>();
        strand.enabled = false;
        strand.sharedMaterial = dropletMaterial;
        strand.positionCount = 3;
        strand.numCapVertices = 3;
        strand.widthCurve = AnimationCurve.Linear(0, .95f, 1, .3f);
        strand.shadowCastingMode = ShadowCastingMode.Off;
        strand.generateLightingData = true;
    }

    void EmitDrop(Vector3 at, Vector3 velocity, float size, float lifetime)
    {
        droplets.Emit(new ParticleSystem.EmitParams
        {
            position = at, velocity = velocity, startSize = size, startLifetime = lifetime,
            rotation = Random.Range(-80, 80)
        }, 1);
    }

    void BeginStrand(Vector3 departure)
    {
        EnsureEffects();
        strandStart = departure;
        strandEnd = Time.time + .21f;
        strand.enabled = true;
    }

    void UpdateStrand(Vector3 feet)
    {
        if (!strand || !strand.enabled) return;
        float remaining = Mathf.Clamp01((strandEnd - Time.time) / .21f);
        if (remaining <= 0 || (feet - strandStart).sqrMagnitude > 11)
        {
            strand.enabled = false;
            return;
        }
        strand.widthMultiplier = .23f * remaining;
        strand.SetPosition(0, strandStart);
        strand.SetPosition(1, Vector3.Lerp(strandStart, feet, .55f) - Vector3.up * .16f);
        strand.SetPosition(2, feet + Vector3.up * .08f);
    }

    void ResetContact()
    {
        stain = spatter = dryTime = Depth = 0;
        wet = wasWet = wasGrounded = false;
        Current = Vector3.zero;
        System.Array.Clear(currents, 0, ImpactCount);
        UploadImpacts(lake);
        UploadImpacts(river);
        if (strand) strand.enabled = false;
        if (droplets) droplets.Clear();
        UpdateCoating();
    }

    void OnDisable()
    {
        hasLastFeet = false;
        if (coatedMaterial) ResetContact();
    }

    void OnDestroy()
    {
        if (coatedMaterial) Destroy(coatedMaterial);
    }
}
