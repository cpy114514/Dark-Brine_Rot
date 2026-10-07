using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Buoyant wreckage transfers movement controls to the plank after boarding.</summary>
[DefaultExecutionOrder(-100), DisallowMultipleComponent]
public sealed class Story1FloatingPlank : MonoBehaviour
{
    public ThirdPersonPlayerController player;
    public BoxCollider deck;
    public Material woodMaterial;
    public float freeboard = .35f;
    [Min(0f)] public float forwardSpeed = 8.5f;
    [Min(0f)] public float reverseSpeed = 4.5f;
    [Min(.1f)] public float acceleration = 5.5f;
    [Min(.1f)] public float braking = 7.5f;
    [Min(0f)] public float turnSpeed = 65f;
    public LayerMask obstacleMask = ~0;
    [Header("Surf effects")]
    public bool surfEffects = true;
    public Material surfMaterial;
    [Min(.1f)] public float wakeLifetime = 3.5f;

    OceanWorld ocean;
    Vector3 origin, riderFootAnchor;
    Quaternion baseRotation;
    float currentSpeed, riderYaw;
    bool initialized, carryPlayer, previousMovementLock;
    bool combatWasEnabled, boomerangWasEnabled, knockbackWasEnabled;
    Mavis.SahurAttack combat;
    Mavis.SahurBoomerang boomerang;
    Mavis.CombatKnockback knockback;
    Material runtimeWood;
    readonly RaycastHit[] obstacleHits = new RaycastHit[24];
    readonly Collider[] turningHits = new Collider[24];
    readonly Vector4[] surfTrail = new Vector4[16];
    ParticleSystem surfSpray;
    Material runtimeSurfMaterial;
    Mesh sprayMesh;
    float sprayTimer, wakeTimer;
    int wakeCount;

    public float CurrentSpeed => currentSpeed;
    public bool ControlsActive => carryPlayer && player != null && initialized;
    public bool CarryPlayer
    {
        get => carryPlayer;
        set
        {
            if (value == carryPlayer) return;
            if (value) AttachRider();
            else ReleaseRider();
        }
    }

    void Awake()
    {
        RepairMaterials();
        CreateSurfEffects();
    }

    void RepairMaterials()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>(true);
        if (woodMaterial == null && renderers.Length > 0) woodMaterial = renderers[0].sharedMaterial;
        Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ?
            "Universal Render Pipeline/Lit" : "Standard");
        if (shader == null || woodMaterial == null) return;
        Material material = woodMaterial;
        if (material.shader != shader || !material.shader.isSupported)
        {
            Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
            Color tint = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            runtimeWood = new Material(shader) { name = "Floating Wreckage Wood (runtime)" };
            if (runtimeWood.HasProperty("_BaseMap")) runtimeWood.SetTexture("_BaseMap", texture);
            if (runtimeWood.HasProperty("_MainTex")) runtimeWood.SetTexture("_MainTex", texture);
            if (runtimeWood.HasProperty("_BaseColor")) runtimeWood.SetColor("_BaseColor", tint);
            if (runtimeWood.HasProperty("_Color")) runtimeWood.SetColor("_Color", tint);
            if (runtimeWood.HasProperty("_Smoothness")) runtimeWood.SetFloat("_Smoothness", .12f);
            if (runtimeWood.HasProperty("_Glossiness")) runtimeWood.SetFloat("_Glossiness", .12f);
            material = runtimeWood;
        }
        foreach (var renderer in renderers) renderer.sharedMaterial = material;
    }

    public void Place(Vector3 position, Quaternion rotation, OceanWorld water)
    {
        CarryPlayer = false;
        currentSpeed = 0f;
        ocean = water;
        origin = position;
        baseRotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        transform.SetPositionAndRotation(position, baseRotation);
        initialized = true;
        Float(0f, 0f, true);
        wakeCount = 0;
        sprayTimer = wakeTimer = 0f;
        if (surfSpray != null) surfSpray.Clear();
    }

    void AttachRider()
    {
        if (player == null || deck == null) return;
        carryPlayer = true;
        previousMovementLock = player.ExternalMovementLock;
        player.ExternalMovementLock = true;
        riderFootAnchor = deck.transform.InverseTransformPoint(player.transform.position);
        riderFootAnchor.x = Mathf.Clamp(riderFootAnchor.x, deck.center.x - deck.size.x * .35f,
            deck.center.x + deck.size.x * .35f);
        riderFootAnchor.z = Mathf.Clamp(riderFootAnchor.z, deck.center.z - deck.size.z * .35f,
            deck.center.z + deck.size.z * .35f);
        riderFootAnchor.y = deck.center.y + deck.size.y * .5f;
        riderYaw = Mathf.DeltaAngle(transform.eulerAngles.y, player.transform.eulerAngles.y);
        combat = player.GetComponent<Mavis.SahurAttack>();
        boomerang = player.GetComponent<Mavis.SahurBoomerang>();
        knockback = player.GetComponent<Mavis.CombatKnockback>();
        combatWasEnabled = combat != null && combat.enabled;
        boomerangWasEnabled = boomerang != null && boomerang.enabled;
        knockbackWasEnabled = knockback != null && knockback.enabled;
        if (combat != null) { combat.SuspendForSwimming(); combat.enabled = false; }
        if (boomerang != null) boomerang.enabled = false;
        if (knockback != null) knockback.enabled = false;
        player.GetComponent<Mavis.EnemyLockOn>()?.Clear();
        if (player.CharacterAnimator != null)
        {
            player.CharacterAnimator.CrossFadeInFixedTime("Locomotion", .12f);
            player.CharacterAnimator.SetFloat("Speed", 0f);
        }
        PositionRider();
    }

    void ReleaseRider()
    {
        if (!carryPlayer) return;
        carryPlayer = false;
        currentSpeed = 0f;
        if (player == null) return;
        player.ExternalMovementLock = previousMovementLock;
        if (combat != null) combat.enabled = combatWasEnabled;
        if (boomerang != null) boomerang.enabled = boomerangWasEnabled;
        if (knockback != null) knockback.enabled = knockbackWasEnabled;
    }

    void Update()
    {
        if (!initialized || PauseSettingsMenu.IsOpen || Time.deltaTime <= 0f) return;
        bool accept = ControlsActive && Keyboard.current != null && Cursor.lockState == CursorLockMode.Locked &&
            !player.ExternalControlLock && !Mavis.SahurLoadoutUI.BlocksInput && !Mavis.PlayerDeathRespawn.BlocksInput;
        float throttle = accept ? (GameInputSettings.Pressed(GameInputSettings.Action.Forward) ? 1f : 0f) -
            (GameInputSettings.Pressed(GameInputSettings.Action.Back) ? 1f : 0f) : 0f;
        float steer = accept ? (GameInputSettings.Pressed(GameInputSettings.Action.Right) ? 1f : 0f) -
            (GameInputSettings.Pressed(GameInputSettings.Action.Left) ? 1f : 0f) : 0f;
        Simulate(Time.deltaTime, Time.time, throttle, steer);
    }

    void Simulate(float deltaTime, float time, float throttle, float steer)
    {
        if (!initialized || PauseSettingsMenu.IsOpen || deltaTime <= 0f) return;
        if (ControlsActive)
        {
            float targetSpeed = throttle > 0f ? forwardSpeed : throttle < 0f ? -reverseSpeed : 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
                (Mathf.Abs(throttle) < .01f ? braking : acceleration) * deltaTime);
            Quaternion heading = Quaternion.AngleAxis(Mathf.Clamp(steer, -1f, 1f) * turnSpeed * deltaTime,
                Vector3.up) * baseRotation;
            heading = heading.normalized;
            if (Mathf.Abs(steer) > .01f && !TurnBlocked(heading)) baseRotation = heading;
            Vector3 motion = baseRotation * Vector3.forward * (currentSpeed * deltaTime);
            if (MoveBlocked(motion)) currentSpeed = 0f;
            else origin += motion;
        }
        else currentSpeed = 0f;
        Float(time, deltaTime, false);
        Physics.SyncTransforms();
        if (carryPlayer) PositionRider();
        UpdateSurfEffects(deltaTime, time);
    }

    void CreateSurfEffects()
    {
        if (!surfEffects || surfSpray != null) return;
        Shader shader = surfMaterial != null ? surfMaterial.shader : Shader.Find("DarkBrine/Procedural Water VFX");
        if (shader == null) return;
        runtimeSurfMaterial = surfMaterial != null ? new Material(surfMaterial) : new Material(shader);
        runtimeSurfMaterial.name = "Plank Surf Spray (runtime)";
        runtimeSurfMaterial.SetColor("_Tint",new Color(.9f,.94f,.96f));
        sprayMesh = new Mesh { name = "Plank Water Droplet" };
        sprayMesh.vertices = new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        sprayMesh.triangles = new[] { 0,2,4, 0,4,3, 0,3,5, 0,5,2, 1,4,2, 1,3,4, 1,5,3, 1,2,5 };
        sprayMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white, Color.white, Color.white };
        sprayMesh.RecalculateNormals();
        sprayMesh.RecalculateBounds();
        var effect = new GameObject("Plank Surf Spray");
        effect.transform.SetParent(transform, false);
        surfSpray = effect.AddComponent<ParticleSystem>();
        surfSpray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = surfSpray.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.startLifetime = .65f;
        main.startSpeed = 0f;
        main.gravityModifier = .8f;
        var emission = surfSpray.emission;
        emission.enabled = false;
        var shape = surfSpray.shape;
        shape.enabled = false;
        var color = surfSpray.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(.72f, .79f, .82f), 1f) },
            new[] { new GradientAlphaKey(.55f, 0f), new GradientAlphaKey(.3f, .4f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = surfSpray.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, .2f));
        var renderer = surfSpray.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = sprayMesh;
        renderer.sharedMaterial = runtimeSurfMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        surfSpray.Play();
    }

    void UpdateSurfEffects(float deltaTime, float time)
    {
        if (!surfEffects) return;
        float speed = ControlsActive ? Mathf.Abs(currentSpeed) : 0f;
        float strength = Mathf.Clamp01(speed / Mathf.Max(1f, forwardSpeed));
        Vector3 direction = baseRotation * Vector3.forward * (currentSpeed < 0f ? -1f : 1f);
        Vector3 across = baseRotation * Vector3.right;
        float halfLength = deck != null ? deck.size.z * Mathf.Abs(deck.transform.lossyScale.z) * .5f : 4f;
        float halfWidth = deck != null ? deck.size.x * Mathf.Abs(deck.transform.lossyScale.x) * .5f : 2.1f;
        wakeTimer += deltaTime;
        if (speed > .4f && (wakeCount == 0 || wakeTimer >= .16f))
        {
            wakeTimer = 0f;
            wakeCount = Mathf.Min(wakeCount + 1, surfTrail.Length);
            for (int i = wakeCount - 1; i > 0; i--) surfTrail[i] = surfTrail[i - 1];
            Vector3 stern = transform.position - direction * halfLength;
            surfTrail[0] = new Vector4(stern.x, stern.z, time, halfWidth * .45f);
        }
        while (wakeCount > 0 && time - surfTrail[wakeCount - 1].z > wakeLifetime) wakeCount--;
        if (ocean != null)
            ocean.SetShipWake(new Vector4(transform.position.x, transform.position.z, halfLength, halfWidth),
                new Vector4(direction.x, direction.z, strength, wakeLifetime), surfTrail, wakeCount);
        if (surfSpray == null || speed <= .4f) { sprayTimer = 0f; return; }
        sprayTimer -= deltaTime;
        if (sprayTimer > 0f) return;
        sprayTimer = Mathf.Lerp(.20f, .11f, strength);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 contact = transform.position + direction * (halfLength * .8f) + across * (side * halfWidth);
            contact.y = ocean != null ? ocean.SampleSurfaceHeight(contact, time) + .1f : transform.position.y - freeboard + .1f;
            for (int i = 0; i < 2; i++)
            {
                var droplet = new ParticleSystem.EmitParams
                {
                    position = contact + across * Random.Range(-.15f, .15f),
                    velocity = across * (side * Random.Range(.6f, 1.6f) * strength) - direction * (speed * .25f) +
                        Vector3.up * Random.Range(.8f, 1.7f) * (.4f + strength),
                    startSize = Random.Range(.025f, .07f) * (.65f + strength),
                    startLifetime = Random.Range(.4f, .75f),
                    startColor = new Color(.9f, .94f, .96f, .55f)
                };
                surfSpray.Emit(droplet, 1);
            }
        }
    }

    void PositionRider()
    {
        if (player == null) { carryPlayer = false; return; }
        Vector3 feet = deck.transform.TransformPoint(riderFootAnchor);
        Vector3 position = feet + Vector3.up * (.06f - player.LowestFootWorldOffset);
        var capsule = player.GetComponent<CharacterController>();
        bool enabled = capsule != null && capsule.enabled;
        if (enabled) capsule.enabled = false;
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, transform.eulerAngles.y + riderYaw, 0f));
        if (enabled) capsule.enabled = true;
    }

    Vector3 DeckHalfExtents()
    {
        Vector3 scale = deck.transform.lossyScale;
        return Vector3.Scale(deck.size * .49f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
    }

    bool IsObstacle(Collider other) => other != null && !other.transform.IsChildOf(transform) &&
        (player == null || !other.transform.IsChildOf(player.transform));

    bool MoveBlocked(Vector3 motion)
    {
        if (motion.sqrMagnitude < .000001f || deck == null) return false;
        int count = Physics.BoxCastNonAlloc(deck.transform.TransformPoint(deck.center), DeckHalfExtents(),
            motion.normalized, obstacleHits, deck.transform.rotation, motion.magnitude + .04f,
            obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) if (IsObstacle(obstacleHits[i].collider)) return true;
        return count == obstacleHits.Length;
    }

    bool TurnBlocked(Quaternion heading)
    {
        if (deck == null) return false;
        Quaternion rotation = heading * Quaternion.Inverse(baseRotation) * deck.transform.rotation;
        int count = Physics.OverlapBoxNonAlloc(deck.transform.TransformPoint(deck.center), DeckHalfExtents(),
            turningHits, rotation, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) if (IsObstacle(turningHits[i])) return true;
        return count == turningHits.Length;
    }

    void Float(float time, float deltaTime, bool snap)
    {
        Vector3 position = origin + new Vector3(Mathf.Sin(time * .12f) * .22f, 0f, Mathf.Sin(time * .17f) * .18f);
        float Surface(Vector3 point) => ocean != null ? ocean.SampleSurfaceHeight(point, time) : origin.y;
        Vector3 across = baseRotation * Vector3.right * 2f;
        Vector3 along = baseRotation * Vector3.forward * 4f;
        float highest = Mathf.Max(Surface(position), Mathf.Max(Mathf.Max(Surface(position + across), Surface(position - across)),
            Mathf.Max(Surface(position + along), Surface(position - along))));
        position.y = highest + freeboard;
        Quaternion rotation = baseRotation * Quaternion.Euler(Mathf.Sin(time * .7f) * 1.2f, 0f, Mathf.Sin(time * .85f) * 1.4f);
        float blend = snap ? 1f : 1f - Mathf.Exp(-6f * deltaTime);
        position.y = Mathf.Lerp(transform.position.y, position.y, blend);
        transform.SetPositionAndRotation(position, Quaternion.Slerp(transform.rotation, rotation, blend));
    }

    void OnEnable() { if (surfSpray != null) surfSpray.Play(); }
    void OnDisable()
    {
        ReleaseRider();
        if (surfSpray != null) surfSpray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (initialized && ocean != null) ocean.ClearShipWake();
        wakeCount = 0;
    }
    void OnDestroy()
    {
        ReleaseRider();
        DestroyResource(runtimeWood);
        DestroyResource(runtimeSurfMaterial);
        DestroyResource(sprayMesh);
    }
    void DestroyResource(Object resource)
    {
        if (resource == null) return;
        if (Application.isPlaying) Destroy(resource);
        else DestroyImmediate(resource);
    }
}
