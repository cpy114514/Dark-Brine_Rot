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

    [Tooltip("Effective buoyancy span as a fraction of the hull. Avoids a full wave cancelling between bow and stern.")]
    [Range(.35f, 1f)] public float buoyancySpan = .65f;
    [Tooltip("Wave slope response, limited by the pitch and roll angle limits above.")]
    [Range(0f, 2f)] public float waveTiltStrength = 1.2f;

    [Header("Wind heel")]
    [Tooltip("Broadside lean in the same world wind direction used by the sails. Zero disables wind heel.")]
    [Range(0f, 5f)] public float windHeelDegrees = 1.8f;
    [Tooltip("Slow variations in sail pressure. Zero keeps a steady wind load.")]
    [Range(0f, 1f)] public float windGustStrength = .55f;
    [Tooltip("Time in seconds for one broad change of wind pressure; not a repeating rocking cycle.")]
    [Min(2f)] public float windGustSeconds = 6f;
    [Tooltip("Below one allows a small, heavy settling motion instead of an immediate return to level.")]
    [Range(.6f, 1f)] public float angularDamping = .78f;

    static readonly Unity.Profiling.ProfilerMarker MotionMarker = new Unity.Profiling.ProfilerMarker("Ship.SailingMotion");

    OceanWorld ocean;
    Quaternion baseRotation;
    Vector3 heading;
    Vector3 beamDirection;
    float waterlineOffset;
    float currentSpeed;
    float steering;
    float heaveVelocity,pitchVelocity,rollVelocity,filteredPitch,filteredRoll;

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
        if (GetComponent<ShipSailWind>() == null) gameObject.AddComponent<ShipSailWind>();
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
        if(deltaTime<=0)return;
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

        using (MotionMarker.Auto())
        {
            float supportLength = Mathf.Max(1f, hullLength * Mathf.Clamp(buoyancySpan, .35f, 1f));
            float supportBeam = Mathf.Max(1f, hullBeam * Mathf.Clamp(buoyancySpan, .35f, 1f));
            // Capture the shared wave/camera configuration once for all five probes.
            // The same native CPU wave sampler is used by water contact elsewhere.
            var surface = OceanSurfaceSampler.Capture(ocean, Camera.main, Time.time);
            float center = surface.Height(position);
            float bow = surface.Height(position + heading * (supportLength * .5f));
            float stern = surface.Height(position - heading * (supportLength * .5f));
            float starboard = surface.Height(position + beamDirection * (supportBeam * .5f));
            float port = surface.Height(position - beamDirection * (supportBeam * .5f));

            float pitch = Mathf.Clamp(Mathf.Atan2(bow - stern, supportLength) * Mathf.Rad2Deg * waveTiltStrength,
                -maxPitchDegrees, maxPitchDegrees);
            Vector2 windDirection = ocean.wave1.direction.normalized;
            Vector3 wind = new Vector3(windDirection.x, 0, windDirection.y);
            float crossWind = Vector3.Dot(wind, beamDirection);
            float pressure = 1f + (Mathf.PerlinNoise(Time.time / Mathf.Max(2f, windGustSeconds), 4.73f) * 2f - 1f)
                * Mathf.Clamp01(windGustStrength);
            // Wind pushes the high sails downwind; wind heel has the opposite sign
            // to a wave lifting that side of the hull. No artificial yaw or drift.
            float roll = Mathf.Clamp(Mathf.Atan2(starboard - port, supportBeam) * Mathf.Rad2Deg * waveTiltStrength
                - crossWind * windHeelDegrees * pressure,
                -maxRollDegrees, maxRollDegrees);
            // ship_g's imported root is flipped 180 degrees around X. Apply tilt
            // about world hull axes so a rising bow really lifts the bow.
            float inertia=Mathf.Clamp(4.5f/Mathf.Max(.1f,responseSpeed),.5f,2f);
            // Closed-form damped springs remain stable through low frame rates and
            // pauses, without a separate visual root or a competing physics body.
            StepTilt(ref filteredPitch, ref pitchVelocity, pitch, inertia, angularDamping, maxPitchDegrees, deltaTime);
            StepTilt(ref filteredRoll, ref rollVelocity, roll, inertia * 1.25f, angularDamping, maxRollDegrees, deltaTime);
            // Derive the roll axis from the sampled beam, rather than assuming the
            // imported root is upright. Positive roll lifts the +beam probe.
            Quaternion targetRotation = Quaternion.AngleAxis(filteredRoll, Vector3.Cross(beamDirection, Vector3.up)) *
                Quaternion.AngleAxis(filteredPitch, Vector3.Cross(heading, Vector3.up)) * baseRotation;
            float hullWater = center * .6f + (bow + stern + starboard + port) * .1f;
            position.y = Mathf.SmoothDamp(position.y,ocean.oceanHeight + waterlineOffset + (hullWater-ocean.oceanHeight) * heaveStrength,
                ref heaveVelocity,inertia,3f,deltaTime);
            transform.SetPositionAndRotation(position,targetRotation);
        }
    }

    static void StepTilt(ref float angle, ref float velocity, float target, float smoothTime,
        float damping, float limit, float deltaTime)
    {
        float frequency = 2f / Mathf.Max(.1f, smoothTime);
        float ratio = Mathf.Clamp(damping, .6f, .999f);
        float damped = frequency * Mathf.Sqrt(1f - ratio * ratio);
        float offset = angle - target;
        float decay = Mathf.Exp(-ratio * frequency * deltaTime);
        float cosine = Mathf.Cos(damped * deltaTime), sine = Mathf.Sin(damped * deltaTime);
        float coefficient = (velocity + ratio * frequency * offset) / damped;
        float nextOffset = decay * (offset * cosine + coefficient * sine);
        velocity = decay * ((-offset * damped * sine + coefficient * damped * cosine)
            - ratio * frequency * (offset * cosine + coefficient * sine));
        angle = target + nextOffset;
        limit = Mathf.Max(0f, limit);
        if (Mathf.Abs(angle) > limit)
        {
            angle = Mathf.Clamp(angle, -limit, limit);
            if (angle * velocity > 0f) velocity = 0f;
        }
    }
}
