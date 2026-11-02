using System.Collections.Generic;
using UnityEngine;

/// <summary>Bounded drifting and staggered position exchanges for the jumping course.</summary>
[DefaultExecutionOrder(-500), DisallowMultipleComponent]
public sealed class MarshmallowHopMotion : MonoBehaviour
{
    [Min(0)] public float sidewaysRange = 2.4f;
    [Min(0)] public float forwardRange = 1.5f;
    [Min(0)] public float verticalRange = .6f;
    [Min(.01f)] public float movementRate = .38f;
    [Min(.1f)] public float rampSeconds = 2f;
    [SerializeField] int seed = 18273;

    struct Platform
    {
        public Transform transform;
        public Vector3 anchor, previous, delta;
        public float phaseX, phaseY, phaseZ, speedX, speedY, speedZ;
        public int pairIndex, pairSide;
    }
    struct Pair
    {
        public int first;
        public Vector3 middle, halfSpan;
        public float angle, startAngle, targetAngle, swapStart, duration, nextSwap, arcSign;
        public bool swapping;
    }
    readonly List<Platform> platforms = new List<Platform>(33);
    readonly List<Pair> pairs = new List<Pair>(11);
    readonly Dictionary<Collider, int> platformColliders = new Dictionary<Collider, int>();
    readonly RaycastHit[] hits = new RaycastHit[32];
    Transform finish;
    Vector3 finishAnchor;
    int finishIndex = -1;
    ThirdPersonPlayerController player;
    CharacterController body;
    float elapsed, nextPlayerLookup;
    System.Random random;
    public int PlatformCount => platforms.Count;
    public int LastSupportIndex { get; private set; } = -1;
    public Vector3 LastCarryDelta { get; private set; }

    void Awake()
    {
        random = new System.Random(seed);
        foreach (Transform row in transform)
        {
            if (!row.name.StartsWith("Row ")) continue;
            int first = platforms.Count;
            foreach (Transform item in row)
            {
                int index = platforms.Count;
                var entry = new Platform
                {
                    transform = item, anchor = item.localPosition, previous = item.position,
                    phaseX = Range(10, 700), phaseY = Range(710, 1400), phaseZ = Range(1410, 2100),
                    speedX = Range(.7f, 1.35f), speedY = Range(.55f, 1.15f), speedZ = Range(.7f, 1.4f),
                    pairIndex = -1
                };
                platforms.Add(entry);
                foreach (var c in item.GetComponentsInChildren<Collider>(true))
                    if (c) platformColliders[c] = index;
            }
            if (platforms.Count - first == 2)
            {
                var left = platforms[first]; var right = platforms[first + 1];
                int pairIndex = pairs.Count;
                left.pairIndex = right.pairIndex = pairIndex;
                left.pairSide = 1; right.pairSide = -1;
                platforms[first] = left; platforms[first + 1] = right;
                pairs.Add(new Pair
                {
                    first = first, middle = (left.anchor + right.anchor) * .5f,
                    halfSpan = (left.anchor - right.anchor) * .5f,
                    nextSwap = Range(3f, 9f), arcSign = Range(0f, 1f) < .5f ? -1f : 1f
                });
            }
        }
        finish = transform.Find("Finish sign");
        if (finish) finishAnchor = finish.localPosition;
        if (platforms.Count > 0) finishIndex = platforms.Count - 1;
    }

    float Range(float min, float max) => min + (max - min) * (float)random.NextDouble();

    static float Wander(float time, float phase, float speed)
    {
        // Two different smooth noise fields avoid a repeated back-and-forth cycle.
        float a = Mathf.PerlinNoise(phase + time * speed, phase * .371f) * 2f - 1f;
        float b = Mathf.PerlinNoise(phase * .613f + time * speed * .57f, phase * .217f + 19f) * 2f - 1f;
        return a * .72f + b * .28f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        LastSupportIndex = -1; LastCarryDelta = Vector3.zero;
        if (dt <= 0 || PauseSettingsMenu.IsOpen || platforms.Count == 0) return;
        if ((!player || !body) && Time.time >= nextPlayerLookup)
        {
            nextPlayerLookup = Time.time + 1f;
            player = FindFirstObjectByType<ThirdPersonPlayerController>();
            body = player ? player.GetComponent<CharacterController>() : null;
        }
        int support = FindSupport();
        elapsed += dt;
        float t = elapsed * movementRate;
        float ramp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.1f, rampSeconds)));
        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            if (!pair.swapping && elapsed >= pair.nextSwap)
            {
                pair.swapping = true; pair.swapStart = elapsed;
                pair.duration = Range(7f, 10f); pair.startAngle = pair.angle;
                pair.targetAngle = pair.angle < Mathf.PI * .5f ? Mathf.PI : 0f;
            }
            if (pair.swapping)
            {
                float progress = Mathf.Clamp01((elapsed - pair.swapStart) / pair.duration);
                pair.angle = Mathf.Lerp(pair.startAngle, pair.targetAngle, Mathf.SmoothStep(0, 1, progress));
                if (progress >= 1f) { pair.swapping = false; pair.nextSwap = elapsed + Range(5f, 10f); }
            }
            pairs[i] = pair;
        }
        bool playerNearby = false;
        for (int i = 0; i < platforms.Count; i++)
        {
            var entry = platforms[i];
            if (!entry.transform) continue;
            // Paired marshmallows share their drifting centre, keeping them
            // separated while they pass one another on opposite arcs.
            var drift = entry.pairIndex >= 0 ? platforms[pairs[entry.pairIndex].first] : entry;
            Vector3 offset = new Vector3(
                Wander(t, drift.phaseX, drift.speedX) * sidewaysRange,
                Wander(t, drift.phaseY, drift.speedY) * verticalRange,
                Wander(t, drift.phaseZ, drift.speedZ) * forwardRange) * ramp;
            if (i == 0) offset *= .35f;
            Vector3 destination = entry.anchor;
            if (entry.pairIndex >= 0)
            {
                var pair = pairs[entry.pairIndex];
                Vector3 arc = pair.halfSpan * Mathf.Cos(pair.angle);
                arc.z += Mathf.Sin(pair.angle) * 2.4f * pair.arcSign;
                destination = pair.middle + arc * entry.pairSide;
            }
            entry.transform.localPosition = destination + offset;
            entry.delta = entry.transform.position - entry.previous;
            entry.previous = entry.transform.position;
            if (body && (entry.previous - body.transform.position).sqrMagnitude < 50f * 50f) playerNearby = true;
            platforms[i] = entry;
        }
        if (finish && finishIndex >= 0 && platforms[finishIndex].transform)
            finish.localPosition = finishAnchor + (platforms[finishIndex].transform.localPosition - platforms[finishIndex].anchor);
        // Update physics once for the whole course while a player can interact
        // with it, rather than synchronizing each of the 33 platforms.
        if (playerNearby) Physics.SyncTransforms();
        if (support >= 0 && support < platforms.Count && body && body.enabled)
        {
            Vector3 delta = platforms[support].delta;
            // A purely horizontal CharacterController.Move clears isGrounded.
            // Keep a small downward contact so jump input remains available.
            body.Move(delta + Vector3.down * .03f);
            LastSupportIndex = support; LastCarryDelta = delta;
        }
    }

    int FindSupport()
    {
        if (!body || !body.enabled || !player || player.ExternalControlLock || player.ExternalMovementLock ||
            player.WorldVelocity.y > .5f) return -1;
        Vector3 feet = new Vector3(body.bounds.center.x, body.bounds.min.y, body.bounds.center.z);
        int count = Physics.RaycastNonAlloc(feet + Vector3.up * .22f, Vector3.down, hits, .55f, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.PositiveInfinity;
        int index = -1;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(player.transform) || hit.normal.y < .5f ||
                hit.distance >= closest) continue;
            closest = hit.distance;
            index = platformColliders.TryGetValue(hit.collider, out int found) ? found : -1;
        }
        return index;
    }
}
