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
    [Tooltip("Restore the pre-attack heading after the animation finishes, without changing its authored travel.")]
    [Range(0.05f, 0.3f)] public float attackFacingRecoveryTime = 0.14f;
      [Tooltip("Original source root-position curves, normalized to each combo slice's playback phase.")]
      [HideInInspector] public AnimationCurve[] comboSourceX;
      [HideInInspector] public AnimationCurve[] comboSourceZ;
      [Tooltip("Scales the sword animation's long lunges to suit Sahur's stick attacks.")]
      [Range(0.1f, 1f)] public float comboTravelScale = 0.65f;
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
    [Min(0.1f)] public float rollSpeed = 6.5f;
    [Min(0f)] public float rollCooldown = 0.18f;
    [Tooltip("Exits before the source clip's held recovery pose, so the dodge does not visibly freeze on its final frame.")]
    [Range(0.75f, 0.98f)] public float rollAnimationExitPhase = 0.90f;
    [Range(0.01f, 0.16f)] public float rollExitBlend = 0.065f;

    [Header("Air flip")]
    [Tooltip("Tap Dodge once anywhere in the jump to complete a forward flip. Releasing the key does not cancel it.")]
    [Min(0.2f)] public float airFlipDuration = 0.43f;
    [Tooltip("Extra upward height supplied by the air flip, added to any remaining jump momentum. Once per airborne cycle.")]
    [Min(0.1f)] public float airFlipExtraHeight = 1.25f;

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
    public bool cameraCollision = true;
    [Min(0.01f)] public float mouseSensitivity = 0.12f;
    public bool invertLookY;
    [Range(-75f, 10f)] public float minPitch = -48f;
    [Range(10f, 85f)] public float maxPitch = 78f;

    CharacterController characterController;
    Animator animator;
    Transform visualTransform;
    Vector3 visualBasePosition;
    Quaternion visualBaseRotation;
    struct ComboMotionSample { public int hash; public float phase; public bool valid; }
    ComboMotionSample comboCurrentSample;
    ComboMotionSample comboNextSample;
    Mavis.SahurAttack combat;
    Mavis.PlayerStamina playerStamina;
    Camera playerCamera;
    float yaw;
    float pitch = 10f;
    float verticalSpeed;
    float authoredAttackHeight;
    float unsupportedAttackSpeed;
    bool trackingAttackRoot;
    bool hasAttackFacing;
    bool recoveringAttackFacing;
    Quaternion attackFacing;
    Quaternion attackRecoveryStart;
    float attackRecoveryElapsed;
    readonly RaycastHit[] attackGroundHits = new RaycastHit[16];
    float visualBaseOffset;
    float coyoteTimer;
    float jumpBufferTimer;
    float rollTimer;
    float rollCooldownTimer;
    float rollInheritedSpeed;
    float airFlipTimer;
    bool airFlipUsed;
    Quaternion airFlipVisualSpin = Quaternion.identity;
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
    static readonly int LocomotionId = Animator.StringToHash("Base Layer.Locomotion");

    public bool CanUseGroundAttack => characterController != null && rollTimer <= 0f &&
                                      IsGroundedOrOnSea() && !IsSwimming();
    public bool CanUseAirAttack => characterController != null && rollTimer <= 0f && airFlipTimer <= 0f &&
                                   !IsGroundedOrOnSea() && !IsSwimming();

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        combat = GetComponent<Mavis.SahurAttack>();
        playerStamina = GetComponent<Mavis.PlayerStamina>();
        Animator rootAnimator = GetComponent<Animator>();
        visualTransform = transform.Find("Pbr Sahur Visual");
        Animator visualAnimator = visualTransform != null ? visualTransform.GetComponent<Animator>() : null;
        if (rootAnimator != null && visualAnimator != null)
        {
            // Animate the model, not the CharacterController root. Imported FBX
            // root curves otherwise reset the player's world position each frame.
            visualAnimator.enabled = false;
            visualAnimator.runtimeAnimatorController = rootAnimator.runtimeAnimatorController;
            visualAnimator.avatar = rootAnimator.avatar;
            visualAnimator.applyRootMotion = true;
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
        if (animator != null)
        {
            var rootMotion = animator.GetComponent<SahurRootMotionRelay>();
            if (rootMotion == null) rootMotion = animator.gameObject.AddComponent<SahurRootMotionRelay>();
            rootMotion.Initialize(this, combat);
            animator.applyRootMotion = true;
        }
        ConfigureColliderToModel();
        Vector3 rotation = transform.eulerAngles;
        yaw = rotation.y;
        LockCursor();
    }

    void Start()
    {
        playerCamera = Camera.main;
        if (animator != null) { animator.applyRootMotion = true; animator.Play("Locomotion", 0, 0f); animator.Update(0f); }
        ConfigureColliderToModel();
        if (visualTransform != null)
        {
            visualBasePosition = visualTransform.localPosition;
            visualBaseRotation = visualTransform.localRotation;
        }
        SnapSpawnToIslandSurface();
        KeepFeetOnSeaLevel();
        CreateWaterSplashEffect();
        CreateUnderwaterPresentation();
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        if (PauseSettingsMenu.IsOpen)
            return;
        if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            LockCursor();
        bool acceptInput = Cursor.lockState == CursorLockMode.Locked;

        Vector2 look = acceptInput ? Mouse.current.delta.ReadValue() * mouseSensitivity : Vector2.zero;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch + (invertLookY ? look.y : -look.y), minPitch, maxPitch);
        cameraDistance = Mathf.Clamp(cameraDistance - Mouse.current.scroll.ReadValue().y * 0.004f, 2.5f, 11f);

        Vector3 input = Vector3.zero;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Forward)) input.z += 1f;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Back)) input.z -= 1f;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Right)) input.x += 1f;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Left)) input.x -= 1f;
        if (input.sqrMagnitude > 1f)
            input.Normalize();
        if (!acceptInput) input = Vector3.zero;

        // Running is direction-agnostic: any held WASD direction can sprint.
        bool wantsSprint = input.sqrMagnitude > 0.01f &&
                           GameInputSettings.Pressed(GameInputSettings.Action.Sprint);
        Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
        Vector3 cameraForward = heading * Vector3.forward;
        Vector3 cameraRight = heading * Vector3.right;
        Vector3 moveDirection = cameraForward * input.z + cameraRight * input.x;
        Vector3 desiredVelocity = moveDirection * moveSpeed * (wantsSprint ? sprintMultiplier : 1f);
        bool combatLocked = combat != null && combat.IsCombatMotionActive;
        bool chargeMovementAllowed = combat != null && combat.IsCharging;
        bool movementLocked = combatLocked && !chargeMovementAllowed;
        bool attackRootMotion = combat != null && combat.UsesAnimationRootMotion;
        if (!attackRootMotion)
        {
            trackingAttackRoot = false;
            authoredAttackHeight = 0f;
            comboCurrentSample = comboNextSample = default;
        }
        if (movementLocked)
            desiredVelocity = Vector3.zero;

        UpdateAttackFacingRecovery(!movementLocked && desiredVelocity.sqrMagnitude > 0.01f);

        if (IsSwimming())
        {
            playerStamina?.StopSprinting();
            UpdateSwimming(input, desiredVelocity);
            return;
        }

        bool sprinting = wantsSprint && !movementLocked &&
                         (playerStamina == null || playerStamina.TickSprint(Time.deltaTime));
        if (!sprinting)
        {
            if (!wantsSprint || movementLocked) playerStamina?.StopSprinting();
            desiredVelocity = movementLocked ? Vector3.zero : moveDirection * moveSpeed;
        }
        bool exitedSwimming = wasSwimming;
        wasSwimming = false;
        if (exitedSwimming)
        {
            // Water movement uses its own state and velocity.  As soon as an
            // island collider takes over, return to the ground blend tree at
            // walking speed before sprint input can accelerate it again.
            Vector3 walkVelocity = (cameraForward * input.z + cameraRight * input.x) * moveSpeed;
            planarVelocity = walkVelocity;
            SetMotion(0, "Locomotion", 0.10f);
        }

        bool grounded = verticalSpeed <= 0f && IsGroundedOrOnSea();
        bool isRolling = rollTimer > 0f;
        bool dodgePressed = acceptInput && !combatLocked &&
                            GameInputSettings.PressedThisFrame(GameInputSettings.Action.Dodge);
        bool jumpPressed = acceptInput && !combatLocked &&
                           GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump);
        if (grounded) airFlipUsed = false;
        rollCooldownTimer = Mathf.Max(0f, rollCooldownTimer - Time.deltaTime);
        if (acceptInput && !combatLocked && !isRolling && grounded && rollCooldownTimer <= 0f &&
            dodgePressed && !jumpPressed &&
            (playerStamina == null || playerStamina.TrySpend(playerStamina.rollCost)))
        {
            // Without WASD, dodge forward in the direction Sahur is facing.
            // Directional dodges still follow the camera-relative input.
            Vector3 dodgeIntent = desiredVelocity.sqrMagnitude > 0.01f
                ? desiredVelocity : transform.forward;
            rollDirection = Vector3.ProjectOnPlane(dodgeIntent, Vector3.up).normalized;
            // Carry the current movement speed into a directional dodge, but
            // never turn leftover braking momentum into a stationary boost.
            rollInheritedSpeed = GetRollInheritedSpeed(planarVelocity, desiredVelocity);
            rollTimer = rollDuration;
            recoveringAttackFacing = false;
            rollCooldownTimer = rollDuration + rollCooldown;
            planarVelocity = rollDirection * (rollSpeed + rollInheritedSpeed);
            isRolling = true;
            jumpBufferTimer = 0f;
            transform.rotation = Quaternion.LookRotation(rollDirection, Vector3.up);
            SetMotion(1, "Roll", 0.075f);
        }

        coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        if (jumpPressed && !isRolling)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        if (combatLocked)
            jumpBufferTimer = 0f;

        if (grounded && verticalSpeed < 0f && !attackRootMotion)
            verticalSpeed = -groundStickSpeed;
        if (!isRolling && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            if (playerStamina != null && !playerStamina.TrySpend(playerStamina.jumpCost))
            {
                jumpBufferTimer = 0f;
            }
            else
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * 2f * gravity);
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                grounded = false;
                airborneTime = 0f;
                SetMotion(2, "Jump Start", 0.10f);
            }
        }

        // Latch a single press, including at the apex or while falling. The
        // timer owns the whole flip; holding/releasing Ctrl cannot shorten it.
        // Added lift both raises the jump and gives a late flip time to finish.
        if (acceptInput && !combatLocked && !isRolling && !grounded && !airFlipUsed &&
            airFlipTimer <= 0f && rollCooldownTimer <= 0f && dodgePressed &&
            (playerStamina == null || playerStamina.TrySpend(playerStamina.airFlipCost)))
        {
            airFlipUsed = true;
            airFlipTimer = airFlipDuration;
            rollCooldownTimer = airFlipDuration + rollCooldown;
            jumpBufferTimer = 0f;
            float upwardSpeed = Mathf.Max(0f, verticalSpeed);
            verticalSpeed = Mathf.Sqrt(upwardSpeed * upwardSpeed + 2f * gravity * airFlipExtraHeight);
            // Even a deliberately small lift must leave time for one rotation.
            verticalSpeed = Mathf.Max(verticalSpeed, gravity * (airFlipDuration + 0.08f) * 0.5f);
            SetMotion(7, "Air Flip", 0.06f);
        }
        if (attackRootMotion)
            verticalSpeed = unsupportedAttackSpeed; // Source lift plus gravity only when there is no floor.
        else
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
            float brakePhase = Mathf.InverseLerp(0.42f, exitPhase, phase);
            float rollWeight = 1f - Mathf.SmoothStep(0f, 1f, brakePhase);
            // Only the dodge boost brakes. Normal movement keeps its own
            // speed, so walking/running + rolling really is an additive sum.
            // Keep the roll heading fixed until its animation exits.
            float inheritedTarget = desiredVelocity.magnitude;
            float inheritedRate = inheritedTarget > rollInheritedSpeed ? acceleration : deceleration;
            rollInheritedSpeed = Mathf.MoveTowards(rollInheritedSpeed, inheritedTarget, inheritedRate * Time.deltaTime);
            planarVelocity = rollDirection * (rollInheritedSpeed + rollSpeed * rollWeight);

            // The imported 44-frame clip pauses in its recovery pose. Fade out
            // just before that tail, and hand both animation and movement over
            // on this same frame.
            rollFinishedThisFrame = phase >= exitPhase || rollTimer <= 0f;
            if (rollFinishedThisFrame)
            {
                rollTimer = 0f;
                rollInheritedSpeed = 0f;
                planarVelocity = desiredVelocity;
            }
        }
        else
        {
            if (attackRootMotion)
                planarVelocity = Vector3.zero;
            else
            {
                float rate = desiredVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, rate * Time.deltaTime);
            }
        }
        float downwardSpeedBeforeMove = verticalSpeed;
        if (!attackRootMotion)
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
        if (airFlipTimer > 0f)
            airFlipTimer = onGround ? 0f : Mathf.Max(0f, airFlipTimer - Time.deltaTime);
        if (!onGround && !isRolling)
        {
            airborneTime += Time.deltaTime;
            if (airFlipTimer <= 0f && (airborneTime > 0.18f || motionState == 0 || motionState == 7))
                SetMotion(3, "Jump Loop", 0.12f);
        }
        else if (onGround && (motionState == 2 || motionState == 3 || motionState == 7))
        {
            landingTimer = 0.25f;
            SetMotion(4, "Land", 0.09f);
        }
        if (motionState == 4)
        {
            landingTimer -= Time.deltaTime;
            if (landingTimer <= 0f) SetMotion(0, "Locomotion", 0.12f);
        }

        if (animator != null)
        {
            // rollTimer is now authoritative: it avoids retaining the stale
            // pre-update isRolling value on the frame that the roll finishes.
            animator.SetFloat(SpeedId, rollTimer > 0f ? 0f : planarVelocity.magnitude, 0.12f, Time.deltaTime);
        }

        if (!movementLocked && !recoveringAttackFacing && planarVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion desired = Quaternion.LookRotation(new Vector3(planarVelocity.x, 0f, planarVelocity.z), Vector3.up);
            float turnRate = isRolling ? turnSpeedDegrees * 1.8f : turnSpeedDegrees;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnRate * Time.deltaTime);
        }
        if (chargeMovementAllowed && hasAttackFacing)
            attackFacing = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    public void BeginAttackFacing()
    {
        // Combo stages share one heading. Never capture an intermediate turn
        // from a sliced animation as the starting direction of its next hit.
        if (hasAttackFacing) return;
        if (recoveringAttackFacing) transform.rotation = attackFacing;
        recoveringAttackFacing = false;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        attackFacing = forward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward.normalized, Vector3.up)
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        hasAttackFacing = true;
    }

    public void EndAttackFacing(bool immediate = false)
    {
        if (!hasAttackFacing && !recoveringAttackFacing) return;
        hasAttackFacing = false;
        trackingAttackRoot = false;
        authoredAttackHeight = 0f;
        comboCurrentSample = comboNextSample = default;
        attackRecoveryStart = transform.rotation;
        attackRecoveryElapsed = 0f;
        recoveringAttackFacing = !immediate;
        if (immediate)
        {
            transform.rotation = attackFacing;
            if (visualTransform != null) visualTransform.localRotation = visualBaseRotation;
        }
    }

    void UpdateAttackFacingRecovery(bool movementRequested)
    {
        if (!recoveringAttackFacing) return;
        // Intentional movement/dodging takes priority over recovery. Merely
        // orbiting the camera does not change the remembered attack heading.
        if (movementRequested || rollTimer > 0f)
        {
            recoveringAttackFacing = false;
            return;
        }
        attackRecoveryElapsed += Time.deltaTime;
        float phase = Mathf.Clamp01(attackRecoveryElapsed / Mathf.Max(0.05f, attackFacingRecoveryTime));
        transform.rotation = Quaternion.Slerp(attackRecoveryStart, attackFacing, Mathf.SmoothStep(0f, 1f, phase));
        if (phase >= 1f) recoveringAttackFacing = false;
    }

    public void ApplyAttackRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        if (characterController == null || combat == null || !combat.UsesAnimationRootMotion ||
            PauseSettingsMenu.IsOpen)
            return;

        // Unity supplies the imported clip's world-space root delta after
        // humanoid retargeting and transition blending. The controller only
        // resolves that delta against real collision; it never invents a step.
        if (!trackingAttackRoot)
        {
            trackingAttackRoot = true;
            authoredAttackHeight = 0f;
            unsupportedAttackSpeed = 0f;
        }
        if (combat.IsGroundComboActive && visualTransform != null)
        {
            // Separating body yaw changes X/Z root projection during blends.
            // Read the original positional curves in their original frame;
            // keep native feet-based Y for the source jump.
            if (TryGetComboSourceTravel(out Vector3 sourceTravel))
            {
                Vector3 worldTravel = (transform.rotation * visualBaseRotation) * sourceTravel;
                deltaPosition.x = worldTravel.x;
                deltaPosition.z = worldTravel.z;
            }
        }
        authoredAttackHeight += deltaPosition.y;
        characterController.Move(deltaPosition);
        // A ground combo keeps the heading chosen before its first hit. The
        // natural local spine/hips twists remain in the imported pose, but its
        // extracted trajectory yaw must not change the gameplay heading.
        if (combat.IsGroundComboActive && hasAttackFacing)
            transform.rotation = attackFacing;
        else
            transform.rotation = transform.rotation * deltaRotation;
        FollowAttackGroundSupport();
        KeepFeetOnSeaLevel();
    }

    void FollowAttackGroundSupport()
    {
        float scaleY = transform.lossyScale.y;
        float contactTolerance = characterController.skinWidth * scaleY + 0.02f;

        // Root motion is authored on a flat floor. Follow a descending physical
        // surface only while the source's feet are at floor level; keep its
        // actual jump height untouched. This changes no X/Z travel or timing.
        Vector3 foot = transform.TransformPoint(characterController.center -
            Vector3.up * (characterController.height * 0.5f));
        float sourceLift = Mathf.Max(0f, authoredAttackHeight);
        float probeHeight = characterController.stepOffset * scaleY;
        int count = Physics.RaycastNonAlloc(foot - Vector3.up * sourceLift + Vector3.up * probeHeight,
            Vector3.down, attackGroundHits, probeHeight * 2f + contactTolerance,
            ~0, QueryTriggerInteraction.Ignore);
        float supportY = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = attackGroundHits[i];
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (Vector3.Angle(hit.normal, Vector3.up) > characterController.slopeLimit) continue;
            supportY = Mathf.Max(supportY, hit.point.y);
        }

        // Keep the source jump relative to its floor, but never use root motion
        // to suspend the actor after that floor ends. Carry the falling speed
        // into normal movement if the attack ends before the actor lands.
        float drop = foot.y - sourceLift - supportY;
        if (float.IsNegativeInfinity(supportY) || drop > probeHeight)
        {
            unsupportedAttackSpeed -= gravity * Time.deltaTime;
            var flags = characterController.Move(Vector3.up * unsupportedAttackSpeed * Time.deltaTime);
            if ((flags & CollisionFlags.Below) != 0) unsupportedAttackSpeed = 0f;
            verticalSpeed = unsupportedAttackSpeed;
            return;
        }

        unsupportedAttackSpeed = 0f;
        verticalSpeed = 0f;
        if (authoredAttackHeight > contactTolerance) return;
        if (drop > characterController.skinWidth * scaleY && drop <= probeHeight)
            characterController.Move(Vector3.down * drop);
    }

    static float GetRollInheritedSpeed(Vector3 currentVelocity, Vector3 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude <= 0.01f)
            return 0f;
        return Vector3.ProjectOnPlane(currentVelocity, Vector3.up).magnitude;
    }

    bool TryGetComboSourceTravel(out Vector3 travel)
    {
        travel = Vector3.zero;
        if (animator == null || comboSourceX == null || comboSourceZ == null ||
            comboSourceX.Length != 3 || comboSourceZ.Length != 3) return false;
        bool transitioning = animator.IsInTransition(0);
        Vector3 Delta(AnimatorStateInfo state, ComboMotionSample previous, bool incoming,
            out ComboMotionSample sample)
        {
            sample = default;
            for (int stage = 0; stage < 3; stage++)
            {
                string name = stage == 0 ? combat.comboOneState : stage == 1 ? combat.comboTwoState : combat.comboThreeState;
                if (!state.IsName(name) || comboSourceX[stage] == null || comboSourceZ[stage] == null) continue;
                float phase = Mathf.Clamp01(state.normalizedTime);
                // A completed blend promotes the incoming cache even when a
                // same-state restart advances past the old outgoing phase.
                if (!incoming && (!transitioning || !previous.valid || previous.hash != state.fullPathHash || phase < previous.phase) &&
                    comboNextSample.valid && comboNextSample.hash == state.fullPathHash)
                    previous = comboNextSample;
                float from = previous.valid && previous.hash == state.fullPathHash && phase >= previous.phase
                    ? previous.phase : incoming ? 0f : phase;
                sample = new ComboMotionSample { hash = state.fullPathHash, phase = phase, valid = true };
                return new Vector3(comboSourceX[stage].Evaluate(phase) - comboSourceX[stage].Evaluate(from), 0f,
                    comboSourceZ[stage].Evaluate(phase) - comboSourceZ[stage].Evaluate(from));
            }
            return Vector3.zero;
        }
        Vector3 current = Delta(animator.GetCurrentAnimatorStateInfo(0), comboCurrentSample, false, out var currentSample);
        ComboMotionSample nextSample = default;
        travel = current;
        if (transitioning)
        {
            Vector3 next = Delta(animator.GetNextAnimatorStateInfo(0), comboNextSample, true, out nextSample);
            float weight = Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime);
            travel = Vector3.Lerp(current, next, weight);
        }
        comboCurrentSample = currentSample;
        comboNextSample = nextSample;
          travel = Vector3.Scale(travel, visualTransform.lossyScale) *
                   (animator.humanScale * comboTravelScale);
        return true;
    }

    public void BeginComboSourceTravel(int stage)
    {
        if (animator == null || combat == null) return;
        // Seed the outgoing pose and incoming clip explicitly. This avoids
        // replaying old displacement on restart or losing it on a slow frame.
        var state = animator.GetCurrentAnimatorStateInfo(0);
        comboCurrentSample = new ComboMotionSample
            { hash = state.fullPathHash, phase = Mathf.Clamp01(state.normalizedTime), valid = true };
        string destination = stage == 0 ? combat.comboOneState : stage == 1 ? combat.comboTwoState : combat.comboThreeState;
        comboNextSample = new ComboMotionSample
            { hash = Animator.StringToHash("Base Layer." + destination), phase = 0f, valid = true };
    }

    void LateUpdate()
    {
        UpdateAirFlipVisual();
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
        if (cameraCollision)
        {
            foreach (var hit in Physics.SphereCastAll(focus, 0.12f, (desiredPosition-focus).normalized,
                         cameraDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                closest = Mathf.Min(closest, Mathf.Max(0.4f, hit.distance - 0.08f));
            }
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
            // BakeMesh's default already includes the renderer scale. Asking
            // for unscaled vertices prevents TransformPoint from applying it twice.
            renderer.BakeMesh(mesh, true);
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
        // The animated bone colliders describe the limbs and torso. Keep this
        // movement capsule inside the torso so it does not block contact early.
        characterController.radius = Mathf.Clamp(Mathf.Min(localBounds.size.x, localBounds.size.z) * 0.25f, 0.15f, characterController.height * 0.45f);
        characterController.center = new Vector3(0f, localBounds.center.y, 0f);
        visualBaseOffset = localBounds.min.y;
        characterController.skinWidth = 0.015f;
        characterController.stepOffset = 0.22f;
        characterController.minMoveDistance = 0f;
    }

    void SetMotion(int state, string name, float blend)
    {
        if (combat != null && combat.IsCombatMotionActive) return;
        if (motionState == state) return;
        motionState = state;
        if (animator != null) animator.CrossFadeInFixedTime(name, blend, 0, 0f);
    }

    public void PrepareChargeLocomotion()
    {
        if (animator == null) return;
        // Charge only overrides the arms. The base layer must leave a landing
        // or roll-recovery pose so its Speed blend tree can animate the legs.
        bool transitioning = animator.IsInTransition(0);
        int activeHash = transitioning
            ? animator.GetNextAnimatorStateInfo(0).fullPathHash
            : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        if (activeHash != LocomotionId)
            animator.CrossFadeInFixedTime(LocomotionId, 0.10f, 0, 0f);
        motionState = 0;
    }

    void UpdateAirFlipVisual()
    {
        if (visualTransform == null) return;

        if (combat != null && combat.IsGroundComboActive)
        {
            airFlipVisualSpin = Quaternion.identity;
            visualTransform.localPosition = visualBasePosition;
            visualTransform.localRotation = visualBaseRotation;
            return;
        }

        // The imported roll stores its full rotation as humanoid root motion.
        // Root motion is not applied during rolls, so rotate only the
        // visual around the movement capsule's centre to show the somersault
        // without tilting or displacing the CharacterController and camera.
        if (airFlipTimer > 0f)
        {
            float phase = Mathf.Clamp01(1f - airFlipTimer / Mathf.Max(0.2f, airFlipDuration));
            // Keep almost constant angular speed. A full SmoothStep stalls at
            // both ends and makes the middle of this short flip rush past.
            float spinPhase = Mathf.Lerp(phase, Mathf.SmoothStep(0f, 1f, phase), 0.18f);
            airFlipVisualSpin = Quaternion.Euler(360f * spinPhase, 0f, 0f);
        }
        else
        {
            // A landing or collision can end the timer between frames. Blend
            // the remaining tilt back to upright rather than snapping it.
            float recovery = 1f - Mathf.Exp(-Time.deltaTime / 0.06f);
            airFlipVisualSpin = Quaternion.Slerp(airFlipVisualSpin, Quaternion.identity, recovery);
            if (Quaternion.Angle(airFlipVisualSpin, Quaternion.identity) < 0.2f)
                airFlipVisualSpin = Quaternion.identity;
        }

        Vector3 pivot = characterController.center;
        visualTransform.localPosition = pivot + airFlipVisualSpin * (visualBasePosition - pivot);
        visualTransform.localRotation = airFlipVisualSpin * visualBaseRotation;
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
        rollInheritedSpeed = 0f;
        rollCooldownTimer = 0f;
        airFlipTimer = 0f;
        airFlipUsed = false;
        airFlipVisualSpin = Quaternion.identity;
        if (visualTransform != null)
        {
            visualTransform.localPosition = visualBasePosition;
            visualTransform.localRotation = visualBaseRotation;
        }
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
        if (!wasSwimming)
        {
            if (waterSplashes)
            {
                // One quiet breach ripple sells the waterline far better than a
                // continuous fountain of polygon droplets around the swimmer.
                EmitWaterSplash(0.46f, 2);
            }
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

    void OnDisable()
    {
        EndAttackFacing(true);
        UnlockCursor();
    }

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
