using UnityEngine;

/// <summary>Stows the original stick over the animated back without changing its grip or rig hierarchy.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class SahurSwimmingWeapon : MonoBehaviour
{
    public Transform stick;
    [Min(0f)] public float backOffset = 0.55f;
    public float heightOffset = 0.04f;
    [Range(-60f, 60f)] public float diagonalAngle = 22f;

    ThirdPersonPlayerController controller;
    Mavis.SahurAttack attack;
    Animator animator;
    Vector3 gripPosition;
    Quaternion gripRotation;
    Vector3 gripScale;
    Mesh mesh;
    Vector3 shaftAxis;
    Vector3 shaftFront;
    Collider[] colliders;
    bool[] colliderEnabled;
    bool stowed;

    public bool IsStowed => stowed;
    public bool Climbing { get; set; }
    public bool CinematicTraversal { get; set; }

    void Awake()
    {
        controller = GetComponent<ThirdPersonPlayerController>();
        attack = GetComponent<Mavis.SahurAttack>();
        if (stick == null)
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == "Sahur Stick") { stick = child; break; }
        if (stick == null) return;
        gripPosition = stick.localPosition;
        gripRotation = stick.localRotation;
        gripScale = stick.localScale;
        mesh = stick.GetComponent<MeshFilter>()?.sharedMesh;
        if (mesh != null)
        {
            var size = mesh.bounds.size;
            shaftAxis = size.x > size.y && size.x > size.z ? Vector3.right :
                size.y > size.z ? Vector3.up : Vector3.forward;
            // This FBX's shaft is diagonal inside its mesh bounds. Its fitted
            // hitbox describes the real shaft direction, unlike an AABB axis.
            if (attack != null && attack.stickHitbox is CapsuleCollider capsule)
            {
                Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                shaftAxis = stick.InverseTransformDirection(capsule.transform.TransformDirection(axis)).normalized;
            }
            shaftFront = Vector3.ProjectOnPlane(Vector3.forward, shaftAxis);
            if (shaftFront.sqrMagnitude < 0.01f) shaftFront = Vector3.ProjectOnPlane(Vector3.up, shaftAxis);
            shaftFront.Normalize();
        }
        colliders = stick.GetComponentsInChildren<Collider>(true);
        colliderEnabled = new bool[colliders.Length];
    }

    void LateUpdate()=>RefreshPose();
    public void RefreshPose()
    {
        if (controller == null || stick == null || mesh == null) return;
        bool swimming = CinematicTraversal || controller.enabled && (controller.Swimming || Climbing);
        if (!swimming) { RestoreGrip(); return; }
        animator = attack != null ? attack.animator : GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;
        var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        if (chest == null || hips == null || left == null || right == null) return;

        if (!stowed)
        {
            for (int i = 0; i < colliders.Length; i++)
                colliderEnabled[i] = colliders[i] != null && colliders[i].enabled;
            stowed = true;
        }
        // The stick cannot hurt anything or obstruct the swimming capsule.
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = false;

        Vector3 up = (chest.position - hips.position).normalized;
        Vector3 across = Vector3.ProjectOnPlane(right.position - left.position, up).normalized;
        Vector3 front = Vector3.Cross(across, up).normalized;
        if (up.sqrMagnitude < 0.5f || across.sqrMagnitude < 0.5f) return;
        float angle = diagonalAngle * Mathf.Deg2Rad;
        Vector3 shaft = up * Mathf.Cos(angle) + across * Mathf.Sin(angle);
        // Give the shaft a stable roll as well as its diagonal direction.
        stick.rotation = Quaternion.LookRotation(front, shaft) *
            Quaternion.Inverse(Quaternion.LookRotation(shaftFront, shaftAxis));
        Vector3 centre = chest.position - front * backOffset + up * heightOffset;
        stick.position = centre - stick.TransformVector(mesh.bounds.center);
    }

    void RestoreGrip()
    {
        if (!stowed || stick == null) return;
        stick.localPosition = gripPosition;
        stick.localRotation = gripRotation;
        stick.localScale = gripScale;
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null)
                colliders[i].enabled = (attack == null || colliders[i] != attack.stickHitbox) && colliderEnabled[i];
        stowed = false;
    }

    void OnDisable() => RestoreGrip();
}
