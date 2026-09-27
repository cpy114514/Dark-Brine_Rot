using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Third-person camera for the long-axis (+X) story ship.</summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class ShipFollowCamera : MonoBehaviour
{
    public Transform target;

    [Header("Framing")]
    [Min(20f)] public float followDistance = 110f;
    [Min(0f)] public float height = 45f;
    public float sideOffset = 35f;
    [Min(0f)] public float lookHeight = 22f;

    [Header("Follow")]
    [Min(0.1f)] public float positionSharpness = 3f;
    [Min(0.1f)] public float rotationSharpness = 5f;

    [Header("View controls")]
    [Min(0.01f)] public float mouseOrbitSensitivity = 0.18f;
    [Min(1f)] public float keyboardOrbitSpeed = 75f;
    [Min(0.01f)] public float zoomSensitivity = 0.1f;
    [Min(0.001f)] public float panSensitivity = 0.03f;
    [Min(1f)] public float minimumDistance = 60f;
    [Min(1f)] public float maximumDistance = 220f;
    [Min(0f)] public float maximumPanDistance = 40f;

    float orbitYaw;
    float orbitPitch;
    float distance;
    Vector3 focusOffset;

    void Start()
    {
        if (target != null)
        {
            ResetView();
            PlaceCameraImmediately();
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        ReadViewInput();
        GetDesiredPose(out Vector3 position, out Quaternion rotation);
        float positionBlend = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, position, positionBlend),
            Quaternion.Slerp(transform.rotation, rotation, rotationBlend));
    }

    void ResetView()
    {
        float horizontalDistance = Mathf.Sqrt(followDistance * followDistance + sideOffset * sideOffset);
        float verticalDistance = height - lookHeight;
        orbitYaw = Mathf.Atan2(sideOffset, followDistance) * Mathf.Rad2Deg;
        orbitPitch = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg;
        distance = Mathf.Clamp(Mathf.Sqrt(horizontalDistance * horizontalDistance +
                                          verticalDistance * verticalDistance),
                               minimumDistance, maximumDistance);
        focusOffset = Vector3.zero;
    }

    void ReadViewInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            if (mouse.leftButton.isPressed)
            {
                orbitYaw += delta.x * mouseOrbitSensitivity;
                orbitPitch -= delta.y * mouseOrbitSensitivity;
            }

            if (mouse.middleButton.isPressed)
            {
                float scale = panSensitivity * distance / 100f;
                focusOffset -= (transform.right * delta.x + transform.up * delta.y) * scale;
                focusOffset = Vector3.ClampMagnitude(focusOffset, maximumPanDistance);
            }

            distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * zoomSensitivity,
                                   minimumDistance, maximumDistance);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame)
                ResetView();

            float keyStep = keyboardOrbitSpeed * Time.deltaTime;
            if (keyboard.leftArrowKey.isPressed) orbitYaw -= keyStep;
            if (keyboard.rightArrowKey.isPressed) orbitYaw += keyStep;
            if (keyboard.upArrowKey.isPressed) orbitPitch += keyStep;
            if (keyboard.downArrowKey.isPressed) orbitPitch -= keyStep;
        }

        orbitPitch = Mathf.Clamp(orbitPitch, 5f, 75f);
    }

    void PlaceCameraImmediately()
    {
        GetDesiredPose(out Vector3 position, out Quaternion rotation);
        transform.SetPositionAndRotation(position, rotation);
    }

    void GetDesiredPose(out Vector3 position, out Quaternion rotation)
    {
        // Ignore the boat's pitch and roll so the horizon stays steady.
        Vector3 heading = Vector3.ProjectOnPlane(target.right, Vector3.up).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, heading);
        float yaw = orbitYaw * Mathf.Deg2Rad;
        float pitch = orbitPitch * Mathf.Deg2Rad;
        Vector3 horizontal = -heading * Mathf.Cos(yaw) + side * Mathf.Sin(yaw);
        Vector3 lookPoint = target.position + Vector3.up * lookHeight + focusOffset;
        position = lookPoint + horizontal * (Mathf.Cos(pitch) * distance) +
                   Vector3.up * (Mathf.Sin(pitch) * distance);
        rotation = Quaternion.LookRotation(lookPoint - position, Vector3.up);
    }
}
