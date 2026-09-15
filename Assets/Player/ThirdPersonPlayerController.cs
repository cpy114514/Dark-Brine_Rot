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
    [Min(0.1f)] public float gravity = 24f;
    public float seaLevel = 0f;

    [Header("Third-person camera")]
    [Min(1f)] public float cameraDistance = 4.6f;
    [Min(0.5f)] public float cameraHeight = 2.25f;
    [Min(0.01f)] public float mouseSensitivity = 0.12f;
    [Range(-75f, 10f)] public float minPitch = -48f;
    [Range(10f, 80f)] public float maxPitch = 55f;

    CharacterController characterController;
    Animator animator;
    Camera playerCamera;
    float yaw;
    float pitch = 14f;
    float verticalSpeed;
    float visualBaseOffset;

    static readonly int MoveZId = Animator.StringToHash("MoveZ");
    static readonly int SprintingId = Animator.StringToHash("Sprinting");
    static readonly int JumpId = Animator.StringToHash("Jump");

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
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 look = Mouse.current.delta.ReadValue() * mouseSensitivity;
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

        bool sprinting = input.z > 0.01f && Keyboard.current.leftShiftKey.isPressed;
        if (animator != null)
        {
            // Sideways WASD movement uses the forward walk cycle; only S selects the
            // dedicated backwards cycle.
            animator.SetFloat(MoveZId, input.z < -0.01f ? -1f : input.sqrMagnitude);
            animator.SetBool(SprintingId, sprinting);
        }

        Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
        Vector3 cameraForward = heading * Vector3.forward;
        Vector3 cameraRight = heading * Vector3.right;
        float activeMoveSpeed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 movement = (cameraForward * input.z + cameraRight * input.x) * activeMoveSpeed;

        bool grounded = characterController.isGrounded || transform.position.y + visualBaseOffset <= seaLevel + 0.02f;
        if (grounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        if (grounded && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            verticalSpeed = Mathf.Sqrt(jumpHeight * 2f * gravity);
            if (animator != null)
                animator.SetTrigger(JumpId);
        }
        verticalSpeed -= gravity * Time.deltaTime;

        characterController.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        KeepFeetOnSeaLevel();

        if (input.sqrMagnitude > 0.001f)
        {
            Quaternion desired = Quaternion.LookRotation(new Vector3(movement.x, 0f, movement.z), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotationSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = transform.position + Vector3.up * cameraHeight;
        Vector3 desiredPosition = focus - cameraRotation * Vector3.forward * cameraDistance;

        // Pull the camera forward if a solid island/prop stands between it and the player.
        if (Physics.Linecast(focus, desiredPosition, out RaycastHit hit))
            desiredPosition = hit.point + hit.normal * 0.18f;

        playerCamera.transform.SetPositionAndRotation(desiredPosition, cameraRotation);
    }

    void ConfigureColliderToModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds localBounds = new Bounds(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            Vector3 min = transform.InverseTransformPoint(world.min);
            Vector3 max = transform.InverseTransformPoint(world.max);
            localBounds.Encapsulate(min);
            localBounds.Encapsulate(max);
        }

        characterController.height = Mathf.Max(1f, localBounds.size.y);
        characterController.radius = Mathf.Clamp(Mathf.Min(localBounds.size.x, localBounds.size.z) * 0.32f, 0.18f, characterController.height * 0.45f);
        characterController.center = new Vector3(0f, localBounds.center.y, 0f);
        visualBaseOffset = localBounds.min.y;
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
