using UnityEngine;

/// <summary>
/// Moves the story ship forward while letting its long hull follow the ocean swell.
/// The imported ship_g model points along its local +X axis.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShipSailingMotion : MonoBehaviour
{
    [Header("Sailing")]
    [Min(0f)] public float forwardSpeed = 3f;
    [Min(1f)] public float hullLength = 55f;
    [Min(1f)] public float hullBeam = 22f;

    [Header("Wave motion")]
    [Range(0f, 1f)] public float heaveStrength = 0.6f;
    [Range(0f, 10f)] public float maxPitchDegrees = 4f;
    [Range(0f, 10f)] public float maxRollDegrees = 5f;
    [Min(0.1f)] public float responseSpeed = 3f;

    OceanWorld ocean;
    Quaternion baseRotation;
    Vector3 heading;
    Vector3 beamDirection;
    float waterlineOffset;

    void Start()
    {
        ocean = FindFirstObjectByType<OceanWorld>();
        baseRotation = transform.rotation;
        heading = Vector3.ProjectOnPlane(baseRotation * Vector3.right, Vector3.up).normalized;
        beamDirection = Vector3.ProjectOnPlane(baseRotation * Vector3.forward, Vector3.up).normalized;
        waterlineOffset = transform.position.y - (ocean != null ? ocean.oceanHeight : 0f);
    }

    void Update()
    {
        if (ocean == null)
            return;

        float deltaTime = Time.deltaTime;
        Vector3 position = transform.position;
        position += heading * (forwardSpeed * deltaTime);

        float time = Time.time * ocean.waveMotionSpeed;
        float halfLength = hullLength * 0.5f;
        float halfBeam = hullBeam * 0.5f;
        float center = SampleHeight(position, time);
        float bow = SampleHeight(position + heading * halfLength, time);
        float stern = SampleHeight(position - heading * halfLength, time);
        float starboard = SampleHeight(position + beamDirection * halfBeam, time);
        float port = SampleHeight(position - beamDirection * halfBeam, time);

        float pitch = Mathf.Clamp(Mathf.Atan2(bow - stern, hullLength) * Mathf.Rad2Deg,
            -maxPitchDegrees, maxPitchDegrees);
        float roll = Mathf.Clamp(Mathf.Atan2(starboard - port, hullBeam) * Mathf.Rad2Deg,
            -maxRollDegrees, maxRollDegrees);
        Quaternion targetRotation = baseRotation *
            Quaternion.AngleAxis(pitch, Vector3.forward) *
            Quaternion.AngleAxis(-roll, Vector3.right);

        float blend = 1f - Mathf.Exp(-responseSpeed * deltaTime);
        position.y = Mathf.Lerp(position.y,
            ocean.oceanHeight + waterlineOffset + center * heaveStrength, blend);
        transform.SetPositionAndRotation(position, Quaternion.Slerp(transform.rotation, targetRotation, blend));
    }

    float SampleHeight(Vector3 worldPosition, float time)
    {
        Vector2 point = new Vector2(worldPosition.x, worldPosition.z);
        // The large hull follows the two swells; the shader's short chop
        // moves across the water without pitching the whole ship.
        return SampleWave(ocean.wave1, point, time) +
               SampleWave(ocean.wave2, point, time + 1.9f);
    }

    static float SampleWave(OceanGerstnerWave wave, Vector2 point, float time)
    {
        if (wave.amplitude <= 0f || wave.wavelength <= 0f)
            return 0f;

        Vector2 direction = wave.direction.sqrMagnitude > 0.0001f
            ? wave.direction.normalized : Vector2.right;
        float phase = (Vector2.Dot(direction, point) - wave.speed * time) *
                      (Mathf.PI * 2f / wave.wavelength);
        return wave.amplitude * Mathf.Sin(phase);
    }
}
