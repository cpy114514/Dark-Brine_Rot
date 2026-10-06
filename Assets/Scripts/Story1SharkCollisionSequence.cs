using UnityEngine;

/// <summary>A live shark strikes the ship, leaving a playable wreck encounter on the ocean.</summary>
[DefaultExecutionOrder(200), DisallowMultipleComponent]
public sealed class Story1SharkCollisionSequence : MonoBehaviour
{
    [Header("Scene references")]
    public ShipSailingMotion ship;
    public ThirdPersonPlayerController sahur;
    public Story1OceanAwakening awakening;
    public Transform swimmer;
    public Camera storyCamera;

    [Header("Encounter")]
    [Min(1f)] public float battleAfterSeconds = 20f;
    [Min(1f)] public float impactAfterSeconds = 30f;
    [Min(0f)] public float bowDistance = 50f;
    [Min(0f)] public float sharkNoseDistance = 37f;
    // Legacy scene data; current waterline comes from model bounds rather than a root offset.
    [HideInInspector] public float swimSubmergeDepth = 18f;
    [Min(0f)] public float swimBobHeight = 0.55f;
    [Min(1f)] public float maximumSwimSpeed = 14f;
    [Min(1f)] public float escapePursuitSpeed = 36f;
    [Min(.1f)] public float swimAcceleration = 7f;
    [Min(20f)] public float huntingStandOffDistance = 140f;
    [Min(1f)] public float pursuitBoostSeconds = 8f;
    [Min(1f)] public float swimTurnDegreesPerSecond = 65f;
    [Min(0.1f)] public float contactTolerance = 2f;
    [Tooltip("Stop the approach before the nose touches the hull, then turn for a tail strike.")]
    [Min(5f)] public float tailSetupDistance = 26f;
    [Tooltip("Sideways hunting sweeps fade out before the final bow approach.")]
    [Min(0f)] public float huntingSweepWidth = 18f;
    [Min(0f)] public float huntingDiveDepth = 4f;
    [Range(0f, 15f)] public float turnBankDegrees = 7f;

    Transform shipTransform;
    TralaleroSwimAnimator swimAnimator;
    OceanWorld ocean;
    Collider[] shipColliders;
    Vector3 heading, side, swimPosition, swimDirection, swimVelocity;
    Vector3 previousContactDestination, contactDestinationVelocity, impactSharkCenter, bowContactPoint;
    Quaternion swimRotation;
    bool hasPreviousContactDestination, impacted;
    float baseTailBeatFrequency, swimBank, oceanHeight, voyageTime;
    Story1WreckBattle wreckBattle;
    public bool HasImpacted => impacted;
    public Vector3 HullImpactPoint => bowContactPoint;
    public float VoyageTime => voyageTime;
    public float CurrentSwimSpeed { get; private set; }

    void Start()
    {
        if (ship == null || sahur == null || swimmer == null || storyCamera == null || awakening == null)
        {
            Debug.LogError("Story1 wreck encounter is missing a scene reference.", this);
            enabled = false; return;
        }
        shipTransform = ship.transform;
        if (sahur.GetComponent<Mavis.GameSaveExcluded>() == null) sahur.gameObject.AddComponent<Mavis.GameSaveExcluded>();
        swimAnimator = swimmer.GetComponent<TralaleroSwimAnimator>();
        if (swimAnimator == null) swimAnimator = swimmer.gameObject.AddComponent<TralaleroSwimAnimator>();
        heading = Vector3.ProjectOnPlane(shipTransform.right, Vector3.up).normalized;
        side = Vector3.Cross(Vector3.up, heading).normalized;
        ocean = FindFirstObjectByType<OceanWorld>();
        oceanHeight = ocean != null ? ocean.oceanHeight : 0;
        shipColliders = shipTransform.GetComponentsInChildren<Collider>();
        swimPosition = swimmer.position; swimRotation = swimmer.rotation;
        float water=ocean!=null ? ocean.SampleSurfaceHeight(swimPosition,Time.time) : oceanHeight;
        swimPosition.y=swimAnimator.SurfaceRootY(water,swimRotation);
        swimmer.position=swimPosition;
        swimDirection = Vector3.ProjectOnPlane(swimmer.forward, Vector3.up).normalized;
        float noseReach = Vector3.ProjectOnPlane(swimAnimator.NoseWorldPoint - swimmer.position, Vector3.up).magnitude;
        if (noseReach > 1) sharkNoseDistance = noseReach;
        baseTailBeatFrequency = swimAnimator.tailBeatFrequency;
        wreckBattle = GetComponent<Story1WreckBattle>();
        if (wreckBattle == null) wreckBattle = gameObject.AddComponent<Story1WreckBattle>();
    }

    void Update() => Tick(Time.deltaTime);
    internal void Tick(float deltaTime)
    {
        if (impacted || PauseSettingsMenu.IsOpen || deltaTime <= 0) return;
        voyageTime += deltaTime;
        UpdateSwimmer(voyageTime, deltaTime);
        float gap = Vector3.ProjectOnPlane(swimAnimator.NoseWorldPoint - bowContactPoint, Vector3.up).magnitude;
        if (voyageTime >= impactAfterSeconds && gap <= tailSetupDistance + contactTolerance &&
            Vector3.Angle(Vector3.ProjectOnPlane(swimmer.forward, Vector3.up), -heading) < 15)
            BeginImpact();
    }

    [ContextMenu("Start wreck encounter")]
    public void BeginImpact()
    {
        if (impacted || !Application.isPlaying || wreckBattle == null) return;
        impacted = true;
        float water=ocean!=null ? ocean.SampleSurfaceHeight(shipTransform.position,Time.time) : oceanHeight;
        // Attack the camera-facing hull flank so the coiling tail and contact are visible.
        Vector3 rayStart=shipTransform.position+heading*25+side*100;
        rayStart.y=water+12;
        bowContactPoint=shipTransform.position+heading*25+side*20;
        bowContactPoint.y=rayStart.y;
        float nearest=150;
        bool backfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        foreach(var collider in shipColliders)
            if(collider!=null && collider.enabled && !collider.isTrigger &&
                collider.Raycast(new Ray(rayStart,-side),out RaycastHit hit,nearest))
            {nearest=hit.distance;bowContactPoint=hit.point;}
        Physics.queriesHitBackfaces=backfaces;
        wreckBattle.Begin(this, heading);
    }
    void UpdateSwimmer(float time, float deltaTime)
    {
        if (deltaTime <= 0f) return;
        heading = Vector3.ProjectOnPlane(shipTransform.right, Vector3.up).normalized;
        side = Vector3.Cross(Vector3.up, heading).normalized;
        float approachDeadline = Mathf.Max(1f, battleAfterSeconds);
        float progress = Mathf.Clamp01(time / approachDeadline);
        bowContactPoint = FindBowContactPoint();
        float finalApproach = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(approachDeadline - pursuitBoostSeconds, approachDeadline, time));
        float standOff = Mathf.Lerp(huntingStandOffDistance,tailSetupDistance,finalApproach);
        float hunting = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.45f, .82f, progress));
        Vector3 huntingOffset = side * (huntingSweepWidth * hunting *
            (Mathf.Sin(time * .52f) + .25f * Mathf.Sin(time * 1.13f))) +
            heading * (Mathf.Sin(time * .38f) * 6f * hunting);
        Vector3 approach = Vector3.ProjectOnPlane(bowContactPoint + heading * standOff + huntingOffset -
            swimAnimator.NoseWorldPoint, Vector3.up);
        if (approach.sqrMagnitude > .0001f) swimDirection = approach.normalized;
        float faceBow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .8f, finalApproach));
        Vector3 forward = Vector3.Slerp(swimDirection, -heading, faceBow).normalized;
        Vector3 previousForward = swimRotation * Vector3.forward;
        if (forward.sqrMagnitude > .001f)
            swimRotation = Quaternion.RotateTowards(swimRotation, Quaternion.LookRotation(forward, Vector3.up),
                swimTurnDegreesPerSecond * deltaTime);
        float turnRate = Vector3.SignedAngle(previousForward,
            swimRotation * Vector3.forward, Vector3.up) / deltaTime;
        float bankTarget = Mathf.Clamp(-turnRate * .2f, -turnBankDegrees, turnBankDegrees) * hunting;
        swimBank = Mathf.Lerp(swimBank, bankTarget, 1f - Mathf.Exp(-4f * deltaTime));
        swimmer.rotation = swimRotation * Quaternion.Euler(
            Mathf.Sin(time * 1.4f) * 2.5f * hunting, 0f,
            swimBank + Mathf.Sin(time * 2.1f) * 1.5f * hunting);
        // The model's nose is offset sideways from its imported root. Aim its
        // actual nose at the bow instead of treating the root as its centreline.
        Vector3 noseOffset = swimAnimator.NoseWorldPoint - swimmer.position;
        Vector3 contactDestination = bowContactPoint - noseOffset;
        if (hasPreviousContactDestination)
        {
            Vector3 targetVelocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(
                contactDestination - previousContactDestination, Vector3.up) / deltaTime, escapePursuitSpeed);
            contactDestinationVelocity = Vector3.Lerp(contactDestinationVelocity, targetVelocity,
                1f - Mathf.Exp(-6f * deltaTime));
        }
        else contactDestinationVelocity = heading * ship.CurrentSpeed;
        previousContactDestination = contactDestination;
        hasPreviousContactDestination = true;
        impactSharkCenter = contactDestination + heading * standOff + huntingOffset;
        Vector3 towardDestination = Vector3.ProjectOnPlane(impactSharkCenter - swimPosition, Vector3.up);
        float remaining = Mathf.Max(.65f, approachDeadline - time);
        // Cruise at a distance first. Only the final approach closes on the bow.
        // Acceleration is bounded; a moving/rotating target cannot cause a speed spike.
        float pursuitLimit = Mathf.Lerp(maximumSwimSpeed,
            Mathf.Max(maximumSwimSpeed, escapePursuitSpeed), finalApproach);
        Vector3 shipVelocity = heading * ship.CurrentSpeed;
        float response = Mathf.Lerp(2.5f, remaining, finalApproach);
        Vector3 targetMotion = Vector3.Lerp(shipVelocity, contactDestinationVelocity, finalApproach);
        Vector3 desiredVelocity = Vector3.ClampMagnitude(targetMotion + towardDestination / response, pursuitLimit);
        float acceleration = Mathf.Lerp(swimAcceleration, swimAcceleration * 4f, finalApproach);
        swimVelocity = Vector3.MoveTowards(swimVelocity, desiredVelocity, acceleration * deltaTime);
        swimVelocity = Vector3.ClampMagnitude(swimVelocity, pursuitLimit);
        Vector3 step = swimVelocity * deltaTime;
        CurrentSwimSpeed = deltaTime > .0001f ? step.magnitude / deltaTime : 0f;
        float targetBeat = baseTailBeatFrequency *
            Mathf.Lerp(.75f, 1.35f, Mathf.Clamp01(CurrentSwimSpeed / escapePursuitSpeed)) *
            (1f + .08f * Mathf.Sin(time * .9f) * hunting);
        swimAnimator.tailBeatFrequency = Mathf.Lerp(swimAnimator.tailBeatFrequency, targetBeat,
            1f - Mathf.Exp(-3f * deltaTime));
        swimPosition += step;
        float water = ocean != null ? ocean.SampleSurfaceHeight(swimPosition, Time.time) : oceanHeight;
        float dive = huntingDiveDepth * hunting * (.5f + .5f * Mathf.Sin(time * .72f));
        swimPosition.y = Mathf.MoveTowards(swimPosition.y, swimAnimator.SurfaceRootY(water,swimmer.rotation) - dive,
            maximumSwimSpeed * deltaTime);
        swimmer.position = swimPosition + Vector3.up * (Mathf.Sin(time * 3.4f) * swimBobHeight);

    }

    Vector3 FindBowContactPoint(float contactHeight = float.NaN)
    {
        Vector3 origin = shipTransform.position + heading * (bowDistance + 60f);
        origin.y = float.IsNaN(contactHeight) ? swimAnimator.NoseWorldPoint.y : contactHeight;
        Vector3 contact = shipTransform.position + heading * bowDistance;
        contact.y = origin.y;
        float closest = 150f;
        bool backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
        foreach (Collider collider in shipColliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;
            if (collider.Raycast(new Ray(origin, -heading), out RaycastHit hit, closest))
            {
                closest = hit.distance;
                contact = hit.point;
            }
        }
        Physics.queriesHitBackfaces = backfaces;
        return contact;
    }

}
