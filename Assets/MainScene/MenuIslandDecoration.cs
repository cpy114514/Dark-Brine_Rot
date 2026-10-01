using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Rotates the drawing under its half-island viewport, independently of hover colour.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SahurDecorationHover))]
public sealed class MenuIslandDecoration : MonoBehaviour, IPointerClickHandler
{
    public float initialAngle = -90f;
    [Min(.1f)] public float rotationDuration = .65f;
    RectTransform artwork;
    float fromAngle, targetAngle, elapsed;
    bool rotating;
    public bool IsRotating => rotating;

    void OnEnable()
    {
        artwork = (RectTransform)transform;
        fromAngle = targetAngle = initialAngle;
        elapsed = 0f;
        rotating = false;
        SetAngle(initialAngle);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || rotating) return;
        fromAngle = targetAngle;
        targetAngle += 180f;
        elapsed = 0f;
        rotating = true;
    }

    void Update() { AdvanceRotation(Time.unscaledDeltaTime); }

    // Unscaled time also supports menus opened while the game is paused.
    public void AdvanceRotation(float deltaTime)
    {
        if (!rotating) return;
        elapsed += Mathf.Max(0f, deltaTime);
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(.1f, rotationDuration));
        SetAngle(Mathf.Lerp(fromAngle, targetAngle, Mathf.SmoothStep(0f, 1f, progress)));
        if (progress < 1f) return;
        targetAngle = Mathf.Repeat(targetAngle, 360f);
        fromAngle = targetAngle;
        rotating = false;
    }

    void SetAngle(float angle)
    {
        if (!artwork) artwork = (RectTransform)transform;
        artwork.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
