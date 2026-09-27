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
    [Min(0f)] public float blackoutDelay = 2.2f;
    [Min(0.1f)] public float blackoutDuration = 1.2f;

    Transform shipTransform;
    Transform sahurTransform;
    CharacterController sahurCollider;
    ShipFollowCamera shipCamera;
    TralaleroSwimAnimator swimAnimator;
    Image blackoutImage;
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
        shipTransform.position = impactShipPosition +
            heading * (10f * flightTime) + side * (7f * flightTime) +
            Vector3.up * (38f * flightTime - 4f * flightTime * flightTime);
        shipTransform.rotation =
            Quaternion.AngleAxis(70f * flightTime, side) *
            Quaternion.AngleAxis(45f * flightTime, heading) * impactShipRotation;

        float sahurForward = 80f * (1f - Mathf.Exp(-1.5f * flightTime));
        float sahurTowardCamera = 50f * (1f - Mathf.Exp(-1.4f * flightTime));
        sahurTransform.position = impactSahurPosition +
            heading * sahurForward + side * sahurTowardCamera +
            Vector3.up * (50f * flightTime - 4f * flightTime * flightTime);
        sahurTransform.rotation =
            Quaternion.AngleAxis(540f * flightTime, Vector3.forward) * impactSahurRotation;

        Vector3 desiredCamera = shipTransform.position + heading * 25f +
                                side * 95f + Vector3.up * 45f;
        float cameraBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.7f));
        Vector3 cameraPosition = Vector3.Lerp(impactCameraPosition, desiredCamera, cameraBlend);
        float shake = (1f - Mathf.Clamp01(time / 0.9f)) * 0.8f;
        cameraPosition += storyCamera.transform.right *
                          (Mathf.PerlinNoise(time * 37f, 0f) * 2f - 1f) * shake;
        cameraPosition += Vector3.up *
                          (Mathf.PerlinNoise(0f, time * 41f) * 2f - 1f) * shake;
        Vector3 focus = shipTransform.position + heading * 25f + Vector3.up * 20f +
                        (sahurTransform.position - shipTransform.position) * 0.10f;
        Quaternion desiredRotation = Quaternion.LookRotation(focus - cameraPosition, Vector3.up);
        storyCamera.transform.SetPositionAndRotation(cameraPosition,
            Quaternion.Slerp(impactCameraRotation, desiredRotation, cameraBlend));

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
    }
}
