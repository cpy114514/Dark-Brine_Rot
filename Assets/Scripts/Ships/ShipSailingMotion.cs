using UnityEngine;

/// <summary>
/// Drives the story ship while letting its long hull follow the ocean swell.
/// The imported ship_g model points along its local +X axis.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShipSailingMotion : MonoBehaviour
{
    [Header("Sailing")]
    [Tooltip("Sail forward continuously; W/S do not stop or reverse a sailboat.")]
    public bool autoSail = true;
    [Min(0f)] public float forwardSpeed = 8f;
    [Min(1f)] public float hullLength = 96f;
    [Min(1f)] public float hullBeam = 37.5f;

    [Header("Helm")]
    [Min(0f)] public float fullSpeed = 6f;
    [Min(0f)] public float reverseSpeed = 4f;
    [Min(0.1f)] public float speedChangePerSecond = 2f;
    [Min(0f)] public float turnDegreesPerSecond = 14f;
    [Min(0.1f)] public float steeringResponse = 2.5f;

    [Header("Wave motion")]
    [Range(0f, 1f)] public float heaveStrength = 1f;
    [Range(0f, 10f)] public float maxPitchDegrees = 4f;
    [Range(0f, 10f)] public float maxRollDegrees = 5f;
    [Min(0.1f)] public float responseSpeed = 6f;

    OceanWorld ocean;
    Quaternion baseRotation;
    Vector3 heading;
    Vector3 beamDirection;
    float waterlineOffset;
    float currentSpeed;
    float steering;

    public float CurrentSpeed => currentSpeed;

    void Start()
    {
        ocean = FindFirstObjectByType<OceanWorld>();
        baseRotation = transform.rotation;
        heading = Vector3.ProjectOnPlane(baseRotation * Vector3.right, Vector3.up).normalized;
        beamDirection = Vector3.ProjectOnPlane(baseRotation * Vector3.forward, Vector3.up).normalized;
        waterlineOffset = transform.position.y - (ocean != null ? ocean.oceanHeight : 0f);
        currentSpeed = autoSail ? Mathf.Max(.1f, forwardSpeed) : forwardSpeed;
        if (GetComponent<ShipWakeEffects>() == null) gameObject.AddComponent<ShipWakeEffects>();
    }

    void Update()
    {
        float helm = (GameInputSettings.Pressed(GameInputSettings.Action.Right) ? 1f : 0f) -
                     (GameInputSettings.Pressed(GameInputSettings.Action.Left) ? 1f : 0f);
        Simulate(Time.deltaTime, helm, GameInputSettings.Pressed(GameInputSettings.Action.Forward),
            GameInputSettings.Pressed(GameInputSettings.Action.Back));
    }

    void Simulate(float deltaTime, float helm, bool forward, bool reverse)
    {
        if (PauseSettingsMenu.IsOpen || Mavis.SahurLoadoutUI.BlocksInput) return;
        deltaTime = Mathf.Max(0f, deltaTime);
        steering = Mathf.MoveTowards(steering, helm, steeringResponse * deltaTime);
        float targetSpeed = autoSail ? Mathf.Max(.1f, forwardSpeed) :
            forward && reverse ? 0f : reverse ? -reverseSpeed :
            forward ? Mathf.Max(fullSpeed, forwardSpeed) : forwardSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
            speedChangePerSecond * deltaTime);
        baseRotation = Quaternion.AngleAxis(steering * turnDegreesPerSecond * deltaTime,
            Vector3.up) * baseRotation;
        heading = Vector3.ProjectOnPlane(baseRotation * Vector3.right, Vector3.up).normalized;
        beamDirection = Vector3.ProjectOnPlane(baseRotation * Vector3.forward, Vector3.up).normalized;
        Vector3 position = transform.position;
        position += heading * (currentSpeed * deltaTime);

        if (ocean == null)
        {
            transform.SetPositionAndRotation(position, baseRotation);
            return;
        }

        float halfLength = hullLength * 0.5f;
        float halfBeam = hullBeam * 0.5f;
        float center = ocean.SampleSurfaceHeight(position, Time.time) - ocean.oceanHeight;
        float bow = ocean.SampleSurfaceHeight(position + heading * halfLength, Time.time);
        float stern = ocean.SampleSurfaceHeight(position - heading * halfLength, Time.time);
        float starboard = ocean.SampleSurfaceHeight(position + beamDirection * halfBeam, Time.time);
        float port = ocean.SampleSurfaceHeight(position - beamDirection * halfBeam, Time.time);

        float pitch = Mathf.Clamp(Mathf.Atan2(bow - stern, hullLength) * Mathf.Rad2Deg,
            -maxPitchDegrees, maxPitchDegrees);
        float roll = Mathf.Clamp(Mathf.Atan2(starboard - port, hullBeam) * Mathf.Rad2Deg,
            -maxRollDegrees, maxRollDegrees);
        // ship_g's imported root is flipped 180 degrees around X. Apply tilt
        // about world hull axes so a rising bow really lifts the bow.
        Quaternion targetRotation = Quaternion.AngleAxis(roll, heading) *
            Quaternion.AngleAxis(pitch, Vector3.Cross(heading, Vector3.up)) * baseRotation;

        float blend = 1f - Mathf.Exp(-responseSpeed * deltaTime);
        position.y = Mathf.Lerp(position.y,
            ocean.oceanHeight + waterlineOffset + center * heaveStrength, blend);
        transform.SetPositionAndRotation(position, Quaternion.Slerp(transform.rotation, targetRotation, blend));
    }

}
