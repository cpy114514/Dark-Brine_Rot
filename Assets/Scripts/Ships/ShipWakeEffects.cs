using UnityEngine;

/// <summary>World-space foam history: old water does not follow the boat or its cinematic launch.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(350)]
public sealed class ShipWakeEffects : MonoBehaviour
{
    const int Capacity = 16;
    public float lifetime = 9f;
    public float sampleInterval = .5f;
    readonly Vector4[] trail = new Vector4[Capacity];
    // Heading, foam strength and lateral spread belong to the water at emission.
    readonly Vector4[] trailSettings = new Vector4[Capacity];
    ShipSailingMotion sailing;
    OceanWorld ocean;
    Vector3 lastPosition;
    float sampleTimer;
    int count;

    void Start()
    {
        sailing = GetComponent<ShipSailingMotion>();
        ocean = FindFirstObjectByType<OceanWorld>();
        lastPosition = transform.position;
    }

    void LateUpdate() => Simulate(Time.deltaTime, Time.time);

    void Simulate(float deltaTime, float now)
    {
        if (ocean == null || sailing == null) return;
        Vector3 heading = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 delta = transform.position - lastPosition;
        float actualSpeed = deltaTime > 0f ? Vector3.ProjectOnPlane(delta, Vector3.up).magnitude / deltaTime : 0f;
        lastPosition = transform.position;
        // A teleport/load must not draw a ribbon through the entire map.
        if (delta.sqrMagnitude > 10000f) { count = 0; actualSpeed = 0f; sampleTimer = 0f; }
        if (Vector3.Dot(delta, heading) < -.01f) heading = -heading;
        bool moving = sailing.isActiveAndEnabled && actualSpeed > .2f && deltaTime > 0f &&
            !PauseSettingsMenu.IsOpen && !Mavis.SahurLoadoutUI.BlocksInput;
        float strength = moving ? Mathf.Clamp01(actualSpeed / 6f) : 0f;
        sampleTimer += moving ? deltaTime : 0f;
        if (moving && (count == 0 || sampleTimer >= sampleInterval))
        {
            sampleTimer = 0f;
            count = Mathf.Min(count + 1, Capacity);
            for (int i = count - 1; i > 0; i--)
            {
                trail[i] = trail[i - 1];
                trailSettings[i] = trailSettings[i - 1];
            }
            Vector3 stern = transform.position - heading * sailing.hullLength * .5f;
            float sternWakeWidth = sailing.hullLength * .195f + sailing.hullBeam * .04f;
            trail[0] = new Vector4(stern.x, stern.z, now, sternWakeWidth);
            trailSettings[0] = new Vector4(heading.x, heading.z, strength,
                Mathf.Clamp(actualSpeed * .28f, .5f, 3.5f));
        }
        while (count > 0 && now - trail[count - 1].z > lifetime) count--;
        ocean.SetShipSailingWake(new Vector4(transform.position.x, transform.position.z,
            sailing.hullLength * .5f, sailing.hullBeam * .5f),
            new Vector4(heading.x, heading.z, strength, Mathf.Max(.1f, lifetime)), trail, trailSettings, count);
    }

    void OnDisable() { if (ocean != null) ocean.ClearShipWake(); }
}
