using UnityEngine;

/// <summary>
/// A climb path authored in metres relative to the moving ship root.
/// The two original ladders are too steep for CharacterController steps.
/// </summary>
[RequireComponent(typeof(ShipSailingMotion))]
public sealed class ShipLadder : MonoBehaviour
{
    public string ladderName = "Ship ladder";
    public Vector3 bottomOffset;
    public Vector3 rungTopOffset;
    public Vector3 deckExitOffset;
    [Min(0.5f)] public float entryDistance = 2.5f;
    [Min(0.5f)] public float climbSpeed = 3.5f;

    public Vector3 Bottom => WorldPoint(bottomOffset);
    public Vector3 RungTop => WorldPoint(rungTopOffset);
    public Vector3 DeckExit => WorldPoint(deckExitOffset);
    public float Length => Vector3.Distance(bottomOffset, rungTopOffset) +
                           Vector3.Distance(rungTopOffset, deckExitOffset);

    public Vector3 HorizontalUp
    {
        get
        {
            Vector3 direction = Vector3.ProjectOnPlane(RungTop - Bottom, Vector3.up);
            return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        }
    }

    public Vector3 PointAt(float distance)
    {
        float rungLength = Vector3.Distance(bottomOffset, rungTopOffset);
        if (distance <= rungLength)
            return Vector3.Lerp(Bottom, RungTop,
                rungLength > 0f ? Mathf.Clamp01(distance / rungLength) : 1f);

        float exitLength = Vector3.Distance(rungTopOffset, deckExitOffset);
        return Vector3.Lerp(RungTop, DeckExit,
            exitLength > 0f ? Mathf.Clamp01((distance - rungLength) / exitLength) : 1f);
    }

    Vector3 WorldPoint(Vector3 offset)
    {
        // TransformDirection rotates the metre offsets with the ship, without
        // multiplying them by the large scale of the imported ship model.
        return transform.position + transform.TransformDirection(offset);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(Bottom, RungTop);
        Gizmos.DrawLine(RungTop, DeckExit);
        Gizmos.DrawWireSphere(Bottom, entryDistance);
        Gizmos.DrawWireSphere(DeckExit, entryDistance);
    }
}
