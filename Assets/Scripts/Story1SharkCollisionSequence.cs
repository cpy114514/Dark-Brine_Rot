using UnityEngine;
using UnityEngine.UI;

/// <summary>Timed Story1 encounter: a swimming shark hits the bow, throws the ship and Sahur, then fades to black.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class Story1SharkCollisionSequence : MonoBehaviour
{
    [Header("Scene references")]
    public ShipSailingMotion ship;
    public ShipboardSahurController sahur;
    public Transform swimmer;
    public Camera storyCamera;

    [Header("Encounter")]
    [Min(1f)] public float impactAfterSeconds = 30f;
    [Min(0f)] public float bowDistance = 50f;
    [Min(0f)] public float sharkNoseDistance = 37f;
    [Min(0f)] public float swimSubmergeDepth = 18f;
    [Min(0f)] public float swimBobHeight = 0.55f;

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
    Vector3 swimStartPosition;
    Vector3 swimDirection;
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

        OceanWorld ocean = FindFirstObjectByType<OceanWorld>();
        oceanHeight = ocean != null ? ocean.oceanHeight : 0f;

        // Use the shark's authored scene position as the start of its swim.
        // Its nose meets the bow when the ship reaches the 30-second point.
        impactSharkCenter = shipTransform.position + heading *
            (ship.forwardSpeed * impactAfterSeconds + bowDistance + sharkNoseDistance);
        swimStartPosition = swimmer.position;
        swimStartPosition.y = oceanHeight - swimSubmergeDepth;
        swimDirection = Vector3.ProjectOnPlane(impactSharkCenter - swimStartPosition,
            Vector3.up).normalized;
        CreateBlackoutOverlay();
        UpdateSwimmer(0f);
    }

    void Update()
    {
        voyageTime += Time.deltaTime;
        if (!impacted)
        {
            UpdateSwimmer(voyageTime);
            if (voyageTime >= impactAfterSeconds)
                BeginImpact();
            return;
        }

        UpdateImpact(Mathf.Min(voyageTime - impactTime, blackoutDelay + blackoutDuration));
    }

    void UpdateSwimmer(float time)
    {
        float progress = Mathf.Clamp01(time / impactAfterSeconds);
        float bob = Mathf.Sin(time * 3.4f) * swimBobHeight;
        Vector3 destination = new Vector3(impactSharkCenter.x,
            oceanHeight - swimSubmergeDepth, impactSharkCenter.z);
        swimmer.position = Vector3.Lerp(swimStartPosition, destination, progress) +
                           Vector3.up * bob;

        float turn = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(impactAfterSeconds * 0.6f, impactAfterSeconds, time));
        Vector3 forward = Vector3.Lerp(swimDirection, -heading, turn).normalized;
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
        swimmer.rotation = facing * Quaternion.Euler(
            Mathf.Sin(time * 2.2f) * 3f,
            Mathf.Sin(time * 2.8f) * 5f,
            Mathf.Sin(time * 3.4f) * 5f);
    }

    void BeginImpact()
    {
        impacted = true;
        impactTime = voyageTime;

        // Keep the final frame aligned with the actual bow if sailing speed
        // was changed in the Inspector during the voyage.
        swimmer.position = shipTransform.position + heading *
                           (bowDistance + sharkNoseDistance) +
                           Vector3.up * (oceanHeight - swimSubmergeDepth);
        impactSwimmerPosition = swimmer.position;
        impactSwimmerRotation = swimmer.rotation;
        swimAnimator.PlayTailSlap();
        ship.enabled = false;
        sahur.enabled = false;
        if (sahurCollider != null) sahurCollider.enabled = false;
        if (shipCamera != null) shipCamera.enabled = false;

        impactShipPosition = shipTransform.position;
        impactShipRotation = shipTransform.rotation;
        impactSahurPosition = sahurTransform.position;
        impactSahurRotation = sahurTransform.rotation;
        impactCameraPosition = storyCamera.transform.position;
        impactCameraRotation = storyCamera.transform.rotation;
        // A fixed wide shot lets the ship and Sahur visibly recede into the sky.
        launchCameraPosition = impactShipPosition - heading * 120f + side * 130f + Vector3.up * 95f;
        launchCameraFocus = impactShipPosition + heading * 60f + Vector3.up * 75f;
        UpdateImpact(0f);
    }

    void UpdateImpact(float time)
    {
        // The shark turns around its center, bringing its tail to the bow.
        // The launch begins when the tail finishes its striking sweep.
        float tailTurn = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(time / TralaleroSwimAnimator.TailContactTime));
        swimmer.rotation = Quaternion.AngleAxis(170f * tailTurn, Vector3.up) *
                           impactSwimmerRotation;
        swimmer.position = impactSwimmerPosition + Vector3.up *
            (Mathf.Sin(Mathf.Min(time, 0.75f) / 0.75f * Mathf.PI) * 3f);

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

        float cameraBlend = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((time - 0.15f) / 0.85f));
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
                        side * (20f * follow) + Vector3.up * (48f * follow);
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
