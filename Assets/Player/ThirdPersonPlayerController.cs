using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Third-person controller for a model placed on the procedural ocean.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class ThirdPersonPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0.1f)] public float moveSpeed = 7f;
    [Min(1f)] public float sprintMultiplier = 1.65f;
    [Min(0.1f)] public float rotationSpeed = 14f;
    [Min(0.1f)] public float jumpHeight = 2.25f;
    [Min(0.1f)] public float gravity = 32f;
    [Min(0.1f)] public float acceleration = 26f;
    [Min(0.1f)] public float deceleration = 34f;
    [Range(0.1f, 1f)] public float backwardsSpeedMultiplier = 0.72f;
    [Min(30f)] public float turnSpeedDegrees = 720f;
    [Min(0f)] public float groundStickSpeed = 5f;
    [Range(0f, 0.3f)] public float coyoteTime = 0.12f;
    [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;
    public float seaLevel = 0f;

    [Header("Spawn")]
    [Tooltip("Keeps the authored X/Z position and places Sahur on the procedural island surface at startup.")]
    public bool snapSpawnToIslandSurface = true;
    [Tooltip("Stable scene spawn anchor. This is not affected by animation root transform curves.")]
    public Transform spawnPoint;
    [Min(0f)] public float spawnSurfaceOffset = 0.03f;

    [Header("Evasion")]
    [Min(0.15f)] public float rollDuration = 0.792793f;
    [Min(0.1f)] public float rollSpeed = 8.5f;
    [Min(0f)] public float rollCooldown = 0.18f;
    [Tooltip("Exits before the source clip's held recovery pose, so the dodge does not visibly freeze on its final frame.")]
    [Range(0.75f, 0.98f)] public float rollAnimationExitPhase = 0.90f;
    [Range(0.01f, 0.16f)] public float rollExitBlend = 0.065f;

    [Header("Water contact")]
    [Tooltip("Spawn lightweight procedural droplets while the character is moving through the sea.")]
    public bool waterSplashes = false;
    [Min(0.1f)] public float splashMinSpeed = 1.15f;
    [Range(0.05f, 0.5f)] public float splashInterval = 0.16f;

    [Header("Swimming")]
    [Min(0.1f)] public float swimSpeed = 3.4f;
    [Min(0.1f)] public float swimAcceleration = 9f;
    [Range(0.2f, 1.2f)] public float swimSubmergeDepth = 0.66f;
    [Min(0.1f)] public float swimBuoyancy = 8f;

    [Header("Underwater presentation")]
    [Tooltip("How far below the surface the third-person camera settles while Sahur is swimming.")]
    [Range(0.15f, 1.2f)] public float underwaterCameraDepth = 0.46f;
    [Range(0.15f, 1f)] public float underwaterOverlayStrength = 0.68f;
    [Tooltip("Show particle bubbles while Sahur is swimming.")]
    public bool underwaterBubbleParticles = false;
    [Range(0.08f, 0.8f)] public float underwaterBubbleInterval = 0.24f;

    [Header("Third-person camera")]
    [Min(1f)] public float cameraDistance = 4.2f;
    [Tooltip("Camera focus height above the calibrated soles, in metres.")]
    [Min(0.5f)] public float cameraHeight = 0.85f;
    [Min(0.1f)] public float cameraFollowSharpness = 20f;
    [Min(0.01f)] public float mouseSensitivity = 0.12f;
    [Range(-75f, 10f)] public float minPitch = -48f;
    [Range(10f, 85f)] public float maxPitch = 78f;

    CharacterController characterController;
    Animator animator;
    Camera playerCamera;
    float yaw;
    float pitch = 10f;
    float verticalSpeed;
    float visualBaseOffset;
    float coyoteTimer;
    float jumpBufferTimer;
    float rollTimer;
    float rollCooldownTimer;
    Vector3 planarVelocity;
    Vector3 rollDirection;
    bool cameraInitialized;
    int motionState;
    float airborneTime;
    float landingTimer;
    float splashTimer;
    float underwaterBubbleTimer;
    float underwaterBlend;
    bool wasAtSeaSurface;
    bool wasSwimming;
    ParticleSystem waterRipples;
    ParticleSystem waterDroplets;
    ParticleSystem underwaterBubbles;
    Material waterVfxMaterial;
    Material underwaterOverlayMaterial;
    Material underwaterBubbleMaterial;
    Mesh waterDropletMesh;
    Mesh waterRippleMesh;
    Renderer underwaterOverlayRenderer;

    static readonly int SpeedId = Animator.StringToHash("Speed");

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        Animator rootAnimator = GetComponent<Animator>();
        Animator visualAnimator = transform.Find("Pbr Sahur Visual")?.GetComponent<Animator>();
        if (rootAnimator != null && visualAnimator != null)
        {
            // Animate the model, not the CharacterController root. Imported FBX
            // root curves otherwise reset the player's world position each frame.
            visualAnimator.enabled = false;
            visualAnimator.runtimeAnimatorController = rootAnimator.runtimeAnimatorController;
            visualAnimator.avatar = rootAnimator.avatar;
            visualAnimator.applyRootMotion = false;
            visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rootAnimator.enabled = false;
            visualAnimator.enabled = true;
            // Assigning a controller/avatar while the visual Animator is disabled can
            // leave its playable graph stale. Rebind now so gameplay triggers such as
            // Attack are handled immediately after the hand-off.
            visualAnimator.Rebind();
            visualAnimator.Update(0f);
            animator = visualAnimator;

            Mavis.SahurAttack attack = GetComponent<Mavis.SahurAttack>();
            if (attack != null) attack.animator = animator;
        }
        else
        {
            animator = visualAnimator != null ? visualAnimator : rootAnimator;
        }
        ConfigureColliderToModel();
        Vector3 rotation = transform.eulerAngles;
        yaw = rotation.y;
        LockCursor();
    }

    void Start()
    {
        playerCamera = Camera.main;
        if (animator != null) { animator.applyRootMotion = false; animator.Play("Locomotion", 0, 0f); animator.Update(0f); }
        ConfigureColliderToModel();
        SnapSpawnToIslandSurface();
        KeepFeetOnSeaLevel();
        CreateWaterSplashEffect();
        CreateUnderwaterPresentation();
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            UnlockCursor();
        if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            LockCursor();
        bool acceptInput = Cursor.lockState == CursorLockMode.Locked;

        Vector2 look = acceptInput ? Mouse.current.delta.ReadValue() * mouseSensitivity : Vector2.zero;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        cameraDistance = Mathf.Clamp(cameraDistance - Mouse.current.scroll.ReadValue().y * 0.004f, 2.5f, 11f);

        Vector3 input = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) input.z += 1f;
        if (Keyboard.current.sKey.isPressed) input.z -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (input.sqrMagnitude > 1f)
            input.Normalize();
        if (!acceptInput) input = Vector3.zero;

        // Running is direction-agnostic: any held WASD direction can sprint.
        bool sprinting = input.sqrMagnitude > 0.01f && Keyboard.current.leftShiftKey.isPressed;
        Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
        Vector3 cameraForward = heading * Vector3.forward;
        Vector3 cameraRight = heading * Vector3.right;
        float activeMoveSpeed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 desiredVelocity = (cameraForward * input.z + cameraRight * input.x) * activeMoveSpeed;

        if (IsSwimming())
        {
            UpdateSwimming(input, desiredVelocity);
            return;
        }

        wasSwimming = false;

        bool grounded = IsGroundedOrOnSea();
        bool isRolling = rollTimer > 0f;
        rollCooldownTimer = Mathf.Max(0f, rollCooldownTimer - Time.deltaTime);
        if (acceptInput && !isRolling && grounded && rollCooldownTimer <= 0f &&
            Keyboard.current.leftCtrlKey.wasPressedThisFrame && desiredVelocity.sqrMagnitude > 0.01f)
        {
            rollDirection = desiredVelocity.normalized;
            rollTimer = rollDuration;
            rollCooldownTimer = rollDuration + rollCooldown;
            planarVelocity = rollDirection * rollSpeed;
            isRolling = true;
            jumpBufferTimer = 0f;
            transform.rotation = Quaternion.LookRotation(rollDirection, Vector3.up);
            SetMotion(1, "Roll", 0.045f);
        }

        coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        if (acceptInput && !isRolling && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);

        if (grounded && verticalSpeed < 0f)
            verticalSpeed = -groundStickSpeed;
        if (!isRolling && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalSpeed = Mathf.Sqrt(jumpHeight * 2f * gravity);
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            grounded = false;
            airborneTime = 0f;
            SetMotion(2, "Jump Start", 0.055f);
        }
        verticalSpeed -= gravity * Time.deltaTime;

        bool rollFinishedThisFrame = false;
        if (isRolling)
        {
            // Roll uses code-driven movement so the same animation works for
            // forward, backward, left and right input without root-motion drift.
            rollTimer = Mathf.Max(0f, rollTimer - Time.deltaTime);
            float phase = 1f - rollTimer / rollDuration;
            float exitPhase = Mathf.Clamp(rollAnimationExitPhase, 0.75f, 0.98f);
            // Never blend into the new input direction while the model is still
            // visibly rolling. That was the source of the sideways skating.
            float rollWeight = Mathf.Lerp(1f, 0.18f, Mathf.SmoothStep(0.45f, exitPhase, phase));
            planarVelocity = rollDirection * rollSpeed * rollWeight;

            // The imported 44-frame clip pauses in its recovery pose. Fade out
            // just before that tail, and hand both animation and movement over
            // on this same frame.
            rollFinishedThisFrame = phase >= exitPhase || rollTimer <= 0f;
            if (rollFinishedThisFrame)
            {
                rollTimer = 0f;
                planarVelocity = desiredVelocity;
            }
        }
        else
        {
            float rate = desiredVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, rate * Time.deltaTime);
        }
        float downwardSpeedBeforeMove = verticalSpeed;
        characterController.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        KeepFeetOnSeaLevel();
        UpdateWaterSplash(Mathf.Max(0f, -downwardSpeedBeforeMove));

        // The exit velocity was already blended above, before the controller
        // moved.  Only change the animation state here; changing velocity at
        // this point would create a one-frame delay after the dodge.
        if (rollFinishedThisFrame)
        {
            SetMotion(0, "Locomotion", rollExitBlend);
        }

        bool onGround = verticalSpeed <= 0f && IsGroundedOrOnSea();
        if (!onGround && !isRolling)
        {
            airborneTime += Time.deltaTime;
            if (airborneTime > 0.12f || motionState == 0)
                SetMotion(3, "Jump Loop", 0.07f);
        }
        else if (onGround && (motionState == 2 || motionState == 3))
        {
            landingTimer = 0.13f;
            SetMotion(4, "Land", 0.035f);
        }
        if (motionState == 4)
        {
            landingTimer -= Time.deltaTime;
            if (landingTimer <= 0f) SetMotion(0, "Locomotion", 0.075f);
        }

        if (animator != null)
        {
            // rollTimer is now authoritative: it avoids retaining the stale
            // pre-update isRolling value on the frame that the roll finishes.
            animator.SetFloat(SpeedId, rollTimer > 0f ? 0f : planarVelocity.magnitude, 0.04f, Time.deltaTime);
        }

        if (planarVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion desired = Quaternion.LookRotation(new Vector3(planarVelocity.x, 0f, planarVelocity.z), Vector3.up);
            float turnRate = isRolling ? turnSpeedDegrees * 1.8f : turnSpeedDegrees;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnRate * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        bool swimming = IsSwimming();
        Vector3 focus = transform.position + Vector3.up * (visualBaseOffset + cameraHeight);
        if (swimming)
            focus.y = Mathf.Min(focus.y, seaLevel - underwaterCameraDepth);
        Vector3 desiredPosition = focus - cameraRotation * Vector3.forward * cameraDistance;
        if (swimming)
            desiredPosition.y = Mathf.Min(desiredPosition.y, seaLevel - 0.12f);

        // Pull the camera forward if a solid island/prop stands between it and the player.
        float closest = cameraDistance;
        foreach (var hit in Physics.SphereCastAll(focus, 0.12f, (desiredPosition-focus).normalized,
                     cameraDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            closest = Mathf.Min(closest, Mathf.Max(0.4f, hit.distance - 0.08f));
        }
        desiredPosition = focus - cameraRotation * Vector3.forward * closest;

        if (!cameraInitialized)
        {
            playerCamera.transform.SetPositionAndRotation(desiredPosition, cameraRotation);
            cameraInitialized = true;
            UpdateUnderwaterPresentation(swimming);
            return;
        }

        float smoothFactor = 1f - Mathf.Exp(-cameraFollowSharpness * Time.deltaTime);
        playerCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(playerCamera.transform.position, desiredPosition, smoothFactor),
            Quaternion.Slerp(playerCamera.transform.rotation, cameraRotation, smoothFactor));
        UpdateUnderwaterPresentation(swimming);
    }

    void ConfigureColliderToModel()
    {
        characterController = GetComponent<CharacterController>();
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers.Length == 0)
            return;

        Bounds localBounds = new Bounds();
        bool initialized = false;
        var mesh = new Mesh();
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            renderer.BakeMesh(mesh);
            foreach(var vertex in mesh.vertices)
            {
                var point = transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                if (!initialized) { localBounds = new Bounds(point, Vector3.zero); initialized = true; }
                else localBounds.Encapsulate(point);
            }
        }
        if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        if (!initialized) return;

        characterController.height = Mathf.Max(1f, localBounds.size.y);
        characterController.radius = Mathf.Clamp(Mathf.Min(localBounds.size.x, localBounds.size.z) * 0.32f, 0.18f, characterController.height * 0.45f);
        characterController.center = new Vector3(0f, localBounds.center.y, 0f);
        visualBaseOffset = localBounds.min.y;
        characterController.skinWidth = 0.015f;
        characterController.stepOffset = 0.22f;
        characterController.minMoveDistance = 0f;
    }

    void SetMotion(int state, string name, float blend)
    {
        if (motionState == state) return;
        motionState = state;
        if (animator != null) animator.CrossFadeInFixedTime(name, blend, 0, 0f);
    }

    void KeepFeetOnSeaLevel()
    {
        float lowestFootOffset = GetLowestFootOffset();
        if (transform.position.y + lowestFootOffset >= seaLevel)
            return;
        Vector3 position = transform.position;
        position.y = seaLevel - lowestFootOffset;
        transform.position = position;
        verticalSpeed = 0f;
    }

    void SnapSpawnToIslandSurface()
    {
        if (!snapSpawnToIslandSurface)
            return;

        ProceduralIsland island = FindFirstObjectByType<ProceduralIsland>();
        if (island == null)
            return;

        Vector3 position;
        if (spawnPoint != null)
        {
            position = spawnPoint.position;
        }
        else
        {
            // Imported clips can reset the character root before Start. Without
            // an assigned anchor, choose a deterministic point safely inside the
            // island rather than trusting the animated root's current position.
            position = island.transform.TransformPoint(Vector3.forward * island.shorelineRadius * 0.45f);
        }
        float surfaceHeight = island.GetWorldSurfaceHeight(position);
        position.y = surfaceHeight - GetLowestFootOffset() + spawnSurfaceOffset;
        transform.position = position;
        verticalSpeed = 0f;
    }

    bool IsGroundedOrOnSea()
    {
        // The ocean is visual-only, so it has no physics collider.  Treat the
        // model's feet meeting the waterline as grounded while solid island
        // colliders continue to use CharacterController grounding normally.
        return characterController.isGrounded || transform.position.y + GetLowestFootOffset() <= seaLevel + 0.035f;
    }

    bool IsAtSeaSurface()
    {
        // Island colliders lift the calibrated soles above sea level.  This keeps
        // water effects off beaches, rocks and props without adding water colliders.
        return transform.position.y + GetLowestFootOffset() <= seaLevel + 0.045f;
    }

    bool IsSwimming()
    {
        // Solid island/prop colliders take priority: their shore remains a
        // normal walkable surface. Outside them the ocean becomes buoyant water.
        return IsAtSeaSurface() && characterController != null && !characterController.isGrounded;
    }

    void UpdateSwimming(Vector3 input, Vector3 desiredVelocity)
    {
        rollTimer = 0f;
        rollCooldownTimer = 0f;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        verticalSpeed = 0f;

        float movementScale = Mathf.Clamp01(input.magnitude);
        Vector3 swimVelocity = desiredVelocity.sqrMagnitude > 0.001f
            ? desiredVelocity.normalized * swimSpeed * movementScale
            : Vector3.zero;
        planarVelocity = Vector3.MoveTowards(planarVelocity, swimVelocity, swimAcceleration * Time.deltaTime);

        float targetY = seaLevel - GetLowestFootOffset() - swimSubmergeDepth;
        float buoyancyVelocity = (targetY - transform.position.y) * swimBuoyancy;
        characterController.Move((planarVelocity + Vector3.up * buoyancyVelocity) * Time.deltaTime);

        bool moving = planarVelocity.sqrMagnitude > 0.12f;
        if (waterSplashes && !wasSwimming)
        {
            // One quiet breach ripple sells the waterline far better than a
            // continuous fountain of polygon droplets around the swimmer.
            EmitWaterSplash(0.46f, 2);
            wasSwimming = true;
        }
        UpdateUnderwaterBubbles(moving);
        SetMotion(moving ? 5 : 6, moving ? "Swim Forward" : "Swim Idle", 0.12f);
        if (animator != null)
            animator.SetFloat(SpeedId, moving ? planarVelocity.magnitude : 0f, 0.08f, Time.deltaTime);
        if (moving)
        {
            Quaternion desired = Quaternion.LookRotation(new Vector3(planarVelocity.x, 0f, planarVelocity.z), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeedDegrees * Time.deltaTime);
        }
    }

    float GetLowestFootOffset()
    {
        // The baked mesh and CharacterController can differ slightly between
        // animation poses.  Taking the lower of both prevents falling through
        // the visual-only ocean after travelling far from island colliders.
        float colliderFoot = characterController != null
            ? characterController.center.y - characterController.height * 0.5f
            : visualBaseOffset;
        return Mathf.Min(visualBaseOffset, colliderFoot);
    }

    void CreateWaterSplashEffect()
    {
        if (!waterSplashes || waterRipples != null)
            return;

        Shader shader = Shader.Find("DarkBrine/Procedural Water VFX");
        if (shader == null)
            return;
        waterVfxMaterial = new Material(shader) { name = "Procedural Water VFX Material" };

        waterRippleMesh = CreateRippleMesh();
        waterDropletMesh = CreateDropletMesh();
        waterRipples = CreateWaterParticleSystem("Water Ripple Rings", waterRippleMesh, 90, 0.62f, 0f);
        waterDroplets = CreateWaterParticleSystem("Water Splash Columns", waterDropletMesh, 130, 0.44f, 1.35f);

        var size = waterRipples.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.28f, 1f, 1.9f));
        var ringColor = waterRipples.colorOverLifetime;
        ringColor.enabled = true;
        ringColor.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(
            new Color(0.72f, 0.98f, 1f, 0.78f), new Color(0.30f, 0.70f, 0.82f, 0f)));

        var dropletColor = waterDroplets.colorOverLifetime;
        dropletColor.enabled = true;
        dropletColor.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(
            new Color(0.92f, 1f, 1f, 0.92f), new Color(0.38f, 0.76f, 0.88f, 0f)));
    }

    void UpdateWaterSplash(float impactSpeed)
    {
        if (!waterSplashes || waterRipples == null || waterDroplets == null)
            return;

        bool atSeaSurface = IsAtSeaSurface();
        float horizontalSpeed = new Vector2(planarVelocity.x, planarVelocity.z).magnitude;
        if (atSeaSurface && !wasAtSeaSurface && impactSpeed > 3.2f)
            EmitWaterSplash(0.9f, 5);

        if (atSeaSurface && horizontalSpeed >= splashMinSpeed)
        {
            splashTimer -= Time.deltaTime;
            if (splashTimer <= 0f)
            {
                float intensity = Mathf.InverseLerp(splashMinSpeed, rollSpeed, horizontalSpeed);
                EmitWaterSplash(0.30f + intensity * 0.38f, rollTimer > 0f ? 4 : 2);
                splashTimer = Mathf.Lerp(splashInterval * 2.1f, splashInterval * 1.25f, intensity);
            }
        }
        else
        {
            splashTimer = 0f;
        }
        wasAtSeaSurface = atSeaSurface;
    }

    void EmitWaterSplash(float intensity, int count)
    {
        // A swimming character is intentionally below the surface, but the
        // ring and droplets must always emit just above the real water plane.
        Vector3 contact = new Vector3(transform.position.x, seaLevel + 0.025f, transform.position.z);
        var ripple = new ParticleSystem.EmitParams
        {
            position = contact,
            rotation3D = new Vector3(0f, Random.Range(0f, 360f), 0f),
            startSize = Mathf.Lerp(0.55f, 1.35f, intensity),
            startLifetime = Mathf.Lerp(0.40f, 0.78f, intensity),
            startColor = new Color(0.75f, 0.98f, 1f, Mathf.Lerp(0.42f, 0.82f, intensity))
        };
        waterRipples.Emit(ripple, intensity > 0.82f ? 2 : 1);

        for (int index = 0; index < count; index++)
        {
            float angle = (index / (float)count) * Mathf.PI * 2f + Random.Range(-0.22f, 0.22f);
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var particle = new ParticleSystem.EmitParams
            {
                position = contact + outward * Random.Range(0.06f, 0.30f),
                velocity = outward * Random.Range(0.8f, 2.8f) * intensity + Vector3.up * Random.Range(2.2f, 4.8f) * intensity,
                startSize = Random.Range(0.055f, 0.17f) * Mathf.Lerp(0.8f, 1.5f, intensity),
                startLifetime = Random.Range(0.30f, 0.56f),
                startColor = Color.Lerp(new Color(0.32f, 0.75f, 0.88f, 0.65f), new Color(0.96f, 1f, 1f, 0.95f), Random.value)
            };
            waterDroplets.Emit(particle, 1);
        }
    }

    ParticleSystem CreateWaterParticleSystem(string name, Mesh mesh, int maxParticles, float lifetime, float gravity)
    {
        var effect = new GameObject(name);
        effect.transform.SetParent(transform, false);
        var particleSystem = effect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = lifetime;
        main.startSize = 1f;
        main.gravityModifier = gravity;
        var emission = particleSystem.emission;
        emission.enabled = false;
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = waterVfxMaterial;
        renderer.enableGPUInstancing = false;
        particleSystem.Play();
        return particleSystem;
    }

    void CreateUnderwaterPresentation()
    {
        if (playerCamera == null)
            return;

        Shader overlayShader = Shader.Find("DarkBrine/Underwater Overlay");
        if (overlayShader != null)
        {
            underwaterOverlayMaterial = new Material(overlayShader) { name = "Underwater Camera Overlay" };
            var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = "Underwater Camera Overlay";
            overlay.transform.SetParent(playerCamera.transform, false);
            overlay.transform.localPosition = new Vector3(0f, 0f, 0.42f);
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = new Vector3(5f, 5f, 1f);
            Destroy(overlay.GetComponent<Collider>());
            underwaterOverlayRenderer = overlay.GetComponent<Renderer>();
            underwaterOverlayRenderer.sharedMaterial = underwaterOverlayMaterial;
            underwaterOverlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            underwaterOverlayRenderer.receiveShadows = false;
            underwaterOverlayRenderer.enabled = false;
        }

        if (!underwaterBubbleParticles)
            return;

        Shader bubbleShader = Shader.Find("DarkBrine/Underwater Bubble");
        if (bubbleShader == null)
            return;

        underwaterBubbleMaterial = new Material(bubbleShader) { name = "Underwater Bubble Material" };
        var effect = new GameObject("Underwater Bubble Trail");
        effect.transform.SetParent(transform, false);
        underwaterBubbles = effect.AddComponent<ParticleSystem>();
        var main = underwaterBubbles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.13f);
        main.gravityModifier = -0.16f;
        var emission = underwaterBubbles.emission;
        emission.enabled = false;
        var renderer = underwaterBubbles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = underwaterBubbleMaterial;
        renderer.enableGPUInstancing = false;
        underwaterBubbles.Play();
    }

    void UpdateUnderwaterPresentation(bool swimming)
    {
        float targetBlend = swimming ? underwaterOverlayStrength : 0f;
        underwaterBlend = Mathf.MoveTowards(underwaterBlend, targetBlend, Time.deltaTime * 2.8f);
        if (underwaterOverlayRenderer == null || underwaterOverlayMaterial == null)
            return;

        underwaterOverlayRenderer.enabled = underwaterBlend > 0.005f;
        underwaterOverlayMaterial.SetFloat("_Intensity", underwaterBlend);
    }

    void UpdateUnderwaterBubbles(bool moving)
    {
        if (underwaterBubbles == null)
            return;

        underwaterBubbleTimer -= Time.deltaTime;
        float interval = moving ? underwaterBubbleInterval : underwaterBubbleInterval * 2.8f;
        if (underwaterBubbleTimer > 0f)
            return;

        Vector3 trail = planarVelocity.sqrMagnitude > 0.01f ? -planarVelocity.normalized * 0.22f : Vector3.zero;
        var bubble = new ParticleSystem.EmitParams
        {
            position = transform.position + trail + Vector3.up * Random.Range(0.18f, 0.62f),
            velocity = trail * Random.Range(0.45f, 1.0f) + Vector3.up * Random.Range(0.32f, 0.72f),
            startSize = Random.Range(0.045f, moving ? 0.13f : 0.09f),
            startLifetime = Random.Range(0.82f, 1.45f),
            startColor = new Color(0.76f, 0.96f, 1f, Random.Range(0.42f, 0.72f))
        };
        underwaterBubbles.Emit(bubble, moving ? Random.Range(1, 3) : 1);
        underwaterBubbleTimer = interval;
    }

    static Gradient CreateFadeGradient(Color start, Color end)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(start.a * 0.7f, 0.28f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    static Mesh CreateRippleMesh()
    {
        const int segments = 24;
        var vertices = new Vector3[segments * 2];
        var triangles = new int[segments * 6];
        for (int index = 0; index < segments; index++)
        {
            float angle = index / (float)segments * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices[index * 2] = direction * 0.60f;
            vertices[index * 2 + 1] = direction;
            int next = (index + 1) % segments;
            int tri = index * 6;
            triangles[tri] = index * 2;
            triangles[tri + 1] = next * 2;
            triangles[tri + 2] = index * 2 + 1;
            triangles[tri + 3] = index * 2 + 1;
            triangles[tri + 4] = next * 2;
            triangles[tri + 5] = next * 2 + 1;
        }
        var mesh = new Mesh { name = "Procedural Water Ripple" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateDropletMesh()
    {
        // A tiny 20-triangle icosahedron reads as a water droplet at game
        // distance and avoids a texture, billboard, or imported particle asset.
        const float t = 1.61803398875f;
        Vector3[] vertices =
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
            new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
            new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
        };
        for (int index = 0; index < vertices.Length; index++)
            vertices[index] = vertices[index].normalized;
        int[] triangles =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };
        var mesh = new Mesh { name = "Procedural Splash Droplet" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        if (waterVfxMaterial != null) Destroy(waterVfxMaterial);
        if (underwaterOverlayMaterial != null) Destroy(underwaterOverlayMaterial);
        if (underwaterBubbleMaterial != null) Destroy(underwaterBubbleMaterial);
        if (waterDropletMesh != null) Destroy(waterDropletMesh);
        if (waterRippleMesh != null) Destroy(waterRippleMesh);
    }

    void OnDisable() => UnlockCursor();

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
