using UnityEngine;

/// <summary>Keeps the current island character on deck until the story releases control.</summary>
[DefaultExecutionOrder(-500), DisallowMultipleComponent]
public sealed class Story1SahurPassenger : MonoBehaviour
{
    public Transform ship;
    Vector3 deckPosition;
    Quaternion deckRotation;
    Animator visual;
    void Awake()
    {
        if (ship == null) return;
        deckPosition = ship.InverseTransformPoint(transform.position);
        deckRotation = Quaternion.Inverse(ship.rotation) * transform.rotation;
        var movement = GetComponent<ThirdPersonPlayerController>();
        if (movement != null) movement.enabled = false;
        var combat = GetComponent<Mavis.SahurAttack>();
        if (combat != null) combat.enabled = false;
        GetComponent<CharacterController>().enabled = false;
    }
    void Start()
    {
        visual = transform.Find("Pbr Sahur Visual")?.GetComponent<Animator>();
        if (visual != null)
        {
            visual.Play("Locomotion", 0, 0f);
            visual.SetFloat("Speed", 0f);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void LateUpdate()
    {
        if (ship == null) return;
        transform.SetPositionAndRotation(ship.TransformPoint(deckPosition), ship.rotation * deckRotation);
        if (visual != null) visual.SetFloat("Speed", 0f);
    }
}
