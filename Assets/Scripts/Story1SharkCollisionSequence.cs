using UnityEngine;
using UnityEngine.UI;

/// <summary>Timed Story1 encounter: a swimming shark hits the bow, throws the ship and Sahur, then fades to black.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class Story1SharkCollisionSequence : MonoBehaviour
{
    [Header("Scene references")]
    public ShipSailingMotion ship;
    public ThirdPersonPlayerController sahur;
    public Story1OceanAwakening awakening;
    public Transform swimmer;
    public Camera storyCamera;

    [Header("Encounter")]
    [Min(1f)] public float impactAfterSeconds = 30f;
    [Min(0f)] public float bowDistance = 50f;
    [Min(0f)] public float sharkNoseDistance = 37f;
    [Min(0f)] public float swimSubmergeDepth = 18f;
    [Min(0f)] public float swimBobHeight = 0.55f;
    [Min(1f)] public float maximumSwimSpeed = 30f;
    [Min(1f)] public float escapePursuitSpeed = 90f;
    [Min(1f)] public float pursuitBoostSeconds = 8f;
    [Min(1f)] public float swimTurnDegreesPerSecond = 120f;
    [Min(0.1f)] public float contactTolerance = 2f;
    [Tooltip("Sideways hunting sweeps fade out before the final bow approach.")]
    [Min(0f)] public float huntingSweepWidth = 18f;
    [Min(0f)] public float huntingDiveDepth = 4f;
    [Range(0f, 15f)] public float turnBankDegrees = 7f;

    [Header("Ending")]
    [Min(0f)] public float blackoutDelay = 4.7f;
    [Min(0.1f)] public float blackoutDuration = 0.75f;

    Transform shipTransform;
    Transform sahurTransform;
    CharacterController sahurCollider;
    ShipFollowCamera shipCamera;
    TralaleroSwimAnimator swimAnimator;
    Image blackoutImage;
    Image glintHorizontal;
    Image glintVertical;
    Vector3 heading;
    Vector3 side;
    Vector3 impactSharkCenter;
    Vector3 swimPosition;
    Vector3 swimDirection;
    Quaternion swimRotation;
    Vector3 previousDestination;
    bool hasPreviousDestination;
    float baseTailBeatFrequency;
    float swimBank;
    OceanWorld ocean;
    Collider[] shipColliders;
    Vector3 bowContactPoint;
    Vector3 tailContactPosition;
    Quaternion tailContactRotation;
    Vector3 impactShipPosition;
    Vector3 impactSahurPosition;
    Vector3 impactSwimmerPosition;
    Vector3 impactCameraPosition;
    Quaternion impactShipRotation;
    Quaternion impactSahurRotation;
    Quaternion impactSwimmerRotation;
    Quaternion impactCameraRotation;
    Vector3 launchCameraPosition;
    Vector3 launchCameraFocus;
    float oceanHeight;
    float voyageTime;
    float impactTime;
    bool impacted;
    bool blackoutComplete;

    public bool HasImpacted => impacted;
    public float VoyageTime => voyageTime;
    public float CurrentSwimSpeed { get; private set; }

    void Start()
    {
        if (ship == null || sahur == null || swimmer == null || storyCamera == null)
        {
            Debug.LogError("Story1 shark collision is missing a scene reference.", this);
            enabled = false;
            return;
        }

        shipTransform = ship.transform;
        sahurTransform = sahur.transform;
        sahurCollider = sahur.GetComponent<CharacterController>();
        shipCamera = storyCamera.GetComponent<ShipFollowCamera>();
        swimAnimator = swimmer.GetComponent<TralaleroSwimAnimator>();
        if (swimAnimator == null)
            swimAnimator = swimmer.gameObject.AddComponent<TralaleroSwimAnimator>();
        heading = Vector3.ProjectOnPlane(shipTransform.right, Vector3.up).normalized;
        side = Vector3.Cross(Vector3.up, heading).normalized;

        ocean = FindFirstObjectByType<OceanWorld>();
        oceanHeight = ocean != null ? ocean.oceanHeight : 0f;
        shipColliders = shipTransform.GetComponentsInChildren<Collider>();
        swimPosition = swimmer.position;
        swimRotation = swimmer.rotation;
        swimDirection = Vector3.ProjectOnPlane(swimmer.forward, Vector3.up).normalized;
        float actualNoseReach = Vector3.ProjectOnPlane(swimAnimator.NoseWorldPoint - swimmer.position, Vector3.up).magnitude;
        if (actualNoseReach > 1f) sharkNoseDistance = actualNoseReach;
        baseTailBeatFrequency = swimAnimator.tailBeatFrequency;
        CreateBlackoutOverlay();
    }

    void Update()
    {
        voyageTime += Time.deltaTime;
        if (!impacted)
        {
            UpdateSwimmer(voyageTime);
            // The timer arms the encounter; actual bow contact starts the slap.
            // Deadline pursuit closes the gap continuously even while reversing or turning.
            float noseGap = Vector3.ProjectOnPlane(swimAnimator.NoseWorldPoint - bowContactPoint, Vector3.up).magnitude;
            if (voyageTime >= impactAfterSeconds && noseGap <= contactTolerance &&
                Vector3.Angle(Vector3.ProjectOnPlane(swimmer.forward, Vector3.up), -heading) < 10f)
                BeginImpact();
            return;
        }

        float endingTime = voyageTime - impactTime;
        float fullBlackTime = blackoutDelay + blackoutDuration;
        if (endingTime < fullBlackTime || awakening == null)
        {
            UpdateImpact(Mathf.Min(endingTime, fullBlackTime));
            return;
        }
        if (!blackoutComplete)
        {
            UpdateImpact(fullBlackTime);
            blackoutComplete = true;
            awakening.BeginBlackout(blackoutImage, impactShipPosition, heading);
        }
    }

    void UpdateSwimmer(float time)
    {
        heading = Vector3.ProjectOnPlane(shipTransform.right, Vector3.up).normalized;
        side = Vector3.Cross(Vector3.up, heading).normalized;
        float progress = Mathf.Clamp01(time / impactAfterSeconds);
        bowContactPoint = FindBowContactPoint();
        float standOff = 45f * (1f - Mathf.SmoothStep(0f, 1f, progress));
        float hunting = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.45f, .82f, progress));
        Vector3 huntingOffset = side * (huntingSweepWidth * hunting *
            (Mathf.Sin(time * .52f) + .25f * Mathf.Sin(time * 1.13f))) +
            heading * (Mathf.Sin(time * .38f) * 6f * hunting);
        Vector3 approach = Vector3.ProjectOnPlane(bowContactPoint + heading * standOff + huntingOffset -
            swimAnimator.NoseWorldPoint, Vector3.up);
        if (approach.sqrMagnitude > .0001f) swimDirection = approach.normalized;
        float faceBow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, .9f, progress));
        Vector3 forward = Vector3.Slerp(swimDirection, -heading, faceBow).normalized;
        Vector3 previousForward = swimRotation * Vector3.forward;
        if (forward.sqrMagnitude > .001f)
            swimRotation = Quaternion.RotateTowards(swimRotation, Quaternion.LookRotation(forward, Vector3.up),
                swimTurnDegreesPerSecond * Time.deltaTime);
        float turnRate = Time.deltaTime > .0001f ? Vector3.SignedAngle(previousForward,
            swimRotation * Vector3.forward, Vector3.up) / Time.deltaTime : 0f;
        float bankTarget = Mathf.Clamp(-turnRate * .2f, -turnBankDegrees, turnBankDegrees) * hunting;
        swimBank = Mathf.Lerp(swimBank, bankTarget, 1f - Mathf.Exp(-4f * Time.deltaTime));
        swimmer.rotation = swimRotation * Quaternion.Euler(
            Mathf.Sin(time * 1.4f) * 2.5f * hunting, 0f,
            swimBank + Mathf.Sin(time * 2.1f) * 1.5f * hunting);
        // The model's nose is offset sideways from its imported root. Aim its
        // actual nose at the bow instead of treating the root as its centreline.
        Vector3 noseOffset = swimAnimator.NoseWorldPoint - swimmer.position;
        impactSharkCenter = bowContactPoint - noseOffset + heading * standOff + huntingOffset;
        Vector3 towardDestination = Vector3.ProjectOnPlane(impactSharkCenter - swimPosition, Vector3.up);
        float remaining = Mathf.Max(.08f, impactAfterSeconds - time);
        float deltaTime = Time.deltaTime;
        float destinationSpeed = hasPreviousDestination && deltaTime > .0001f ?
            Vector3.ProjectOnPlane(impactSharkCenter - previousDestination, Vector3.up).magnitude / deltaTime :
            Mathf.Abs(ship.CurrentSpeed);
        previousDestination = impactSharkCenter;
        hasPreviousDestination = true;
        // Include how fast the bow target moves, not just the remaining gap.
        // A reversing or turning ship otherwise leaves a permanent chase lag.
        float urgency = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(1f - (impactAfterSeconds - time) / pursuitBoostSeconds));
        float pursuitLimit = Mathf.Lerp(maximumSwimSpeed,
            Mathf.Max(maximumSwimSpeed, escapePursuitSpeed), urgency);
        float requiredSpeed = destinationSpeed + towardDestination.magnitude / remaining + 2f;
        float speed = Mathf.Min(pursuitLimit, Mathf.Max(Mathf.Abs(ship.CurrentSpeed) + 1f, requiredSpeed));
        Vector3 step = Vector3.ClampMagnitude(towardDestination, speed * Time.deltaTime);
        CurrentSwimSpeed = deltaTime > .0001f ? step.magnitude / deltaTime : 0f;
        float targetBeat = baseTailBeatFrequency *
            Mathf.Lerp(1f, 2f, Mathf.Clamp01(CurrentSwimSpeed / maximumSwimSpeed)) *
            (1f + .08f * Mathf.Sin(time * .9f) * hunting);
        swimAnimator.tailBeatFrequency = Mathf.Lerp(swimAnimator.tailBeatFrequency, targetBeat,
            1f - Mathf.Exp(-3f * Time.deltaTime));
        swimPosition += step;
        float water = ocean != null ? ocean.SampleSurfaceHeight(swimPosition, Time.time) : oceanHeight;
        float dive = huntingDiveDepth * hunting * (.5f + .5f * Mathf.Sin(time * .72f));
        swimPosition.y = Mathf.MoveTowards(swimPosition.y, water - swimSubmergeDepth - dive,
            maximumSwimSpeed * Time.deltaTime);
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

    void BeginImpact()
    {
        impacted = true;
        impactTime = voyageTime;

        impactSwimmerPosition = swimmer.position;
        impactSwimmerRotation = swimmer.rotation;
        // Sweep above the waterline so the slap is visible, still targeting
        // the actual bow collider at that height.
        bowContactPoint = FindBowContactPoint(Mathf.Max(bowContactPoint.y, oceanHeight + 8f));
        // Turn and translate continuously so the deformed tail tip reaches the
        // actual hull point at the slap's contact frame.
        tailContactRotation = Quaternion.LookRotation(heading, Vector3.up);
        Vector3 tailOffset = Vector3.Scale(swimAnimator.TailContactLocalPoint, swimmer.lossyScale);
        tailContactPosition = bowContactPoint - tailContactRotation * tailOffset;
        swimAnimator.PlayTailSlap();
        ship.enabled = false;
        sahur.enabled = false;
        var passenger = sahur.GetComponent<Story1SahurPassenger>();
        if (passenger != null) passenger.enabled = false;
        if (sahurCollider != null) sahurCollider.enabled = false;
        if (shipCamera != null) shipCamera.enabled = false;

        impactShipPosition = shipTransform.position;
        impactShipRotation = shipTransform.rotation;
        impactSahurPosition = sahurTransform.position;
        impactSahurRotation = sahurTransform.rotation;
        impactCameraPosition = storyCamera.transform.position;
        impactCameraRotation = storyCamera.transform.rotation;
        // A fixed wide shot lets the ship and Sahur visibly recede into the sky.
        launchCameraPosition = impactShipPosition + heading * 15f + side * 150f + Vector3.up * 55f;
        launchCameraFocus = impactShipPosition + heading * 35f + Vector3.up * 20f;
        UpdateImpact(0f);
    }

    void UpdateImpact(float time)
    {
        // The shark turns around its center, bringing its tail to the bow.
        // The launch begins when the tail finishes its striking sweep.
        float tailTurn = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(time / TralaleroSwimAnimator.TailContactTime));
        swimmer.rotation = Quaternion.Slerp(impactSwimmerRotation, tailContactRotation, tailTurn);
        swimmer.position = Vector3.Lerp(impactSwimmerPosition, tailContactPosition, tailTurn) +
            Vector3.up * (Mathf.Sin(Mathf.Clamp01(time / TralaleroSwimAnimator.TailContactTime) * Mathf.PI) * 1.5f);

        float flightTime = Mathf.Max(0f, time - TralaleroSwimAnimator.TailContactTime);
        float shipBurst = 1f - Mathf.Exp(-4.5f * flightTime);
        // A quick pop after contact becomes an accelerating, upward flight.
        // Both trajectories keep rising until the blackout: neither falls into the sea.
        shipTransform.position = impactShipPosition +
            heading * (12f * shipBurst + 24f * flightTime + 18f * flightTime * flightTime) +
            side * (5f * shipBurst + 8f * flightTime + 4f * flightTime * flightTime) +
            Vector3.up * (12f * shipBurst + 20f * flightTime + 7.5f * flightTime * flightTime);
        // Keep the bow above the water during the first beat; tumble only
        // after the whole hull has cleared the surface.
        float shipTumble = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((flightTime - 0.35f) / 3.5f));
        shipTransform.rotation =
            Quaternion.AngleAxis(-115f * shipTumble, side) *
            Quaternion.AngleAxis(105f * shipTumble, heading) *
            Quaternion.AngleAxis(20f * shipTumble, Vector3.up) * impactShipRotation;

        // Sahur separates a moment later and tumbles on a wider path.
        float sahurTime = Mathf.Max(0f, flightTime - 0.13f);
        float sahurBurst = 1f - Mathf.Exp(-5f * sahurTime);
        sahurTransform.position = impactSahurPosition +
            heading * (20f * sahurBurst + 27f * sahurTime + 20f * sahurTime * sahurTime) +
            side * (10f * sahurBurst + 14f * sahurTime + 6f * sahurTime * sahurTime) +
            Vector3.up * (16f * sahurBurst + 35f * sahurTime + 9f * sahurTime * sahurTime);
        float sahurTumble = 1f - Mathf.Exp(-0.75f * sahurTime);
        sahurTransform.rotation =
            Quaternion.AngleAxis(650f * sahurTumble, impactCameraRotation * Vector3.forward) *
            Quaternion.AngleAxis(180f * sahurTumble, side) * impactSahurRotation;

        float cameraBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.45f));
        float follow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(flightTime / 4.1f));
        Vector3 cameraPosition = Vector3.Lerp(impactCameraPosition,
            launchCameraPosition + heading * (24f * follow) + Vector3.up * (18f * follow),
            cameraBlend);
        float shake = (1f - Mathf.Clamp01(Mathf.Abs(time - TralaleroSwimAnimator.TailContactTime) / 0.35f)) * 1.3f;
        cameraPosition += (impactCameraRotation * Vector3.right) *
                          (Mathf.PerlinNoise(time * 37f, 0f) * 2f - 1f) * shake;
        cameraPosition += Vector3.up *
                          (Mathf.PerlinNoise(0f, time * 41f) * 2f - 1f) * shake;
        Vector3 focus = launchCameraFocus + heading * (70f * follow) +
                        side * (20f * follow) + Vector3.up * (100f * follow);
        Quaternion desiredRotation = Quaternion.LookRotation(focus - cameraPosition, Vector3.up);
        storyCamera.transform.SetPositionAndRotation(cameraPosition,
            Quaternion.Slerp(impactCameraRotation, desiredRotation, cameraBlend));

        UpdateGlint(flightTime);

        float alpha = Mathf.Clamp01((time - blackoutDelay) / blackoutDuration);
        blackoutImage.enabled = alpha > 0f;
        blackoutImage.color = new Color(0f, 0f, 0f, alpha);
    }

    void CreateBlackoutOverlay()
    {
        GameObject canvasObject = new GameObject("Story1 Blackout", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        GameObject imageObject = new GameObject("Black Screen", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        blackoutImage = imageObject.GetComponent<Image>();
        blackoutImage.color = Color.clear;
        blackoutImage.raycastTarget = false;
        blackoutImage.enabled = false;

        glintHorizontal = CreateGlintBar(canvasObject.transform, "Distant glint horizontal", new Vector2(30f, 2f));
        glintVertical = CreateGlintBar(canvasObject.transform, "Distant glint vertical", new Vector2(2f, 30f));
    }

    static Image CreateGlintBar(Transform parent, string name, Vector2 size)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        Image image = bar.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    void UpdateGlint(float flightTime)
    {
        float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((flightTime - 3.2f) / 0.22f));
        float vanish = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((flightTime - 3.65f) / 0.35f));
        float alpha = appear * vanish;
        Vector3 point = storyCamera.WorldToViewportPoint(
            Vector3.Lerp(shipTransform.position, sahurTransform.position, 0.5f));
        bool visible = alpha > 0f && point.z > 0f &&
                       point.x > 0f && point.x < 1f && point.y > 0f && point.y < 1f;
        glintHorizontal.enabled = glintVertical.enabled = visible;
        if (!visible) return;

        Vector2 position = new Vector2((point.x - 0.5f) * Screen.width,
                                       (point.y - 0.5f) * Screen.height);
        glintHorizontal.rectTransform.anchoredPosition = position;
        glintVertical.rectTransform.anchoredPosition = position;
        float size = 0.45f + 0.55f * Mathf.Sin(Mathf.PI * appear);
        glintHorizontal.rectTransform.localScale = glintVertical.rectTransform.localScale =
            Vector3.one * size;
        glintHorizontal.color = glintVertical.color = new Color(1f, 1f, 1f, alpha);
    }
}
