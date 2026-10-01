using UnityEngine;

/// <summary>Floats the wreckage on the ocean and carries a player standing on its deck.</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class Story1FloatingPlank : MonoBehaviour
{
    public ThirdPersonPlayerController player;
    public BoxCollider deck;
    public float freeboard = 0.35f;
    OceanWorld ocean;

    void OnEnable()
    {
        ocean = FindFirstObjectByType<OceanWorld>();
        if (deck == null) deck = GetComponent<BoxCollider>();
    }

    void Update()
    {
        if (ocean == null) return;
        Vector3 before = transform.position;
        Vector3 position = before;
        position.y = ocean.SampleSurfaceHeight(position, Time.time) + freeboard;
        bool carry = false;
        CharacterController capsule = player != null ? player.GetComponent<CharacterController>() : null;
        if (capsule != null && capsule.enabled && player.enabled && deck != null)
        {
            Bounds bounds = deck.bounds;
            Vector3 feet = capsule.bounds.center - Vector3.up * capsule.bounds.extents.y;
            carry = feet.x >= bounds.min.x && feet.x <= bounds.max.x &&
                    feet.z >= bounds.min.z && feet.z <= bounds.max.z &&
                    Mathf.Abs(feet.y - bounds.max.y) < 0.3f;
        }
        transform.position = position;
        if (carry) capsule.Move(position - before);
    }
}
