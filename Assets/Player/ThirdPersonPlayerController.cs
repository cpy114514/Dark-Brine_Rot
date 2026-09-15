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
    [Min(0.1f)] public float jumpHeight = 1.35f;
    [Min(0.1f)] public float gravity = 32f;
    [Min(0.1f)] public float acceleration = 26f;
    [Min(0.1f)] public float deceleration = 34f;
    [Range(0.1f, 1f)] public float backwardsSpeedMultiplier = 0.72f;
    [Min(30f)] public float turnSpeedDegrees = 720f;
    [Min(0f)] public float groundStickSpeed = 5f;
    [Range(0f, 0.3f)] public float coyoteTime = 0.12f;
    [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;
    public float seaLevel = 0f;

    [Header("Evasion")]
    [Min(0.15f)] public float rollDuration = 0.792793f;
    [Min(0.1f)] public float rollSpeed = 6.5f;
    [Min(0f)] public float rollCooldown = 0.18f;

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

    static readonly int SpeedId = Animator.StringToHash("Speed");

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
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
        KeepFeetOnSeaLevel();
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
            // Stop translation before the standing recovery at the end of the clip.
            float rollWeight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.72f, phase));
            planarVelocity = rollDirection * rollSpeed * rollWeight;
            rollFinishedThisFrame = rollTimer <= 0f;
        }
        else
        {
            float rate = desiredVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, rate * Time.deltaTime);
        }
        characterController.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        KeepFeetOnSeaLevel();

        // Do not feed the last roll velocity into normal deceleration: that
        // was responsible for a short but noticeable glide after each roll.
        if (rollFinishedThisFrame)
        {
            planarVelocity = Vector3.zero;
            SetMotion(0, "Locomotion", 0.06f);
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
            animator.SetFloat(SpeedId, isRolling ? 0f : planarVelocity.magnitude, 0.04f, Time.deltaTime);
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
        Vector3 focus = transform.position + Vector3.up * (visualBaseOffset + cameraHeight);
        Vector3 desiredPosition = focus - cameraRotation * Vector3.forward * cameraDistance;

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
            return;
        }

        float smoothFactor = 1f - Mathf.Exp(-cameraFollowSharpness * Time.deltaTime);
        playerCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(playerCamera.transform.position, desiredPosition, smoothFactor),
            Quaternion.Slerp(playerCamera.transform.rotation, cameraRotation, smoothFactor));
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
        if (transform.position.y + visualBaseOffset >= seaLevel)
            return;
        Vector3 position = transform.position;
        position.y = seaLevel - visualBaseOffset;
        transform.position = position;
        verticalSpeed = 0f;
    }

    bool IsGroundedOrOnSea()
    {
        // The ocean is visual-only, so it has no physics collider.  Treat the
        // model's feet meeting the waterline as grounded while solid island
        // colliders continue to use CharacterController grounding normally.
        return characterController.isGrounded || transform.position.y + visualBaseOffset <= seaLevel + 0.035f;
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
