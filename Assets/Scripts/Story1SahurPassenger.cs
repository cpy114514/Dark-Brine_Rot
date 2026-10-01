using UnityEngine;

/// <summary>Keeps the Story1 passenger on the moving deck until the shark impact.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class Story1SahurPassenger : MonoBehaviour
{
    public Transform ship;
    Vector3 deckPosition;
    Quaternion deckRotation;

    void Start()
    {
        if (ship == null) { enabled = false; return; }
        deckPosition = ship.InverseTransformPoint(transform.position);
        deckRotation = Quaternion.Inverse(ship.rotation) * transform.rotation;
        var movement = GetComponent<ThirdPersonPlayerController>();
        if (movement != null) movement.enabled = false;
        var capsule = GetComponent<CharacterController>();
        if (capsule != null) capsule.enabled = false;
    }

    void LateUpdate()
    {
        if (ship != null)
            transform.SetPositionAndRotation(ship.TransformPoint(deckPosition), ship.rotation * deckRotation);
    }
}
