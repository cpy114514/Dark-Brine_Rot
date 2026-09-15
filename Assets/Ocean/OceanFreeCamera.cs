using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Simple free-flight camera control for exploring the ocean scene.</summary>
[DisallowMultipleComponent]
public sealed class OceanFreeCamera : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float moveSpeed = 22f;
    [SerializeField, Min(0.01f)] float mouseSensitivity = 0.12f;
    [SerializeField, Range(-89f, 0f)] float minPitch = -80f;
    [SerializeField, Range(0f, 89f)] float maxPitch = 80f;

    float pitch;
    float yaw;

    void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        pitch = NormalizeAngle(angles.x);
        yaw = angles.y;
        LockCursor();
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
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 input = Vector3.zero;
        var keyboard = Keyboard.current;
        if (keyboard.wKey.isPressed) input += Vector3.forward;
        if (keyboard.sKey.isPressed) input += Vector3.back;
        if (keyboard.aKey.isPressed) input += Vector3.left;
        if (keyboard.dKey.isPressed) input += Vector3.right;
        if (keyboard.spaceKey.isPressed) input += Vector3.up;
        if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) input += Vector3.down;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        // Horizontal movement follows the camera heading; vertical movement stays world-up.
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 movement = (forward * input.z + right * input.x + Vector3.up * input.y) * moveSpeed;
        transform.position += movement * Time.deltaTime;
    }

    void OnDisable() => UnlockCursor();

    static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;

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
