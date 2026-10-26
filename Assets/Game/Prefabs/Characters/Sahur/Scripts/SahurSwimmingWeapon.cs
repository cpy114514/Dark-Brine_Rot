using UnityEngine;

/// <summary>Stows the original stick over the animated back without changing its grip or rig hierarchy.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class SahurSwimmingWeapon : MonoBehaviour
{
    public Transform stick;
    [Min(0f)] public float backOffset = 0.55f;
    [Min(0f)] public float bodyClearance = 0.12f;
    [Min(0f)] public float drawSideOffset = 0.6f;
    [Min(0f)] public float drawForwardOffset = 0.6f;
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
    Vector3 shaftGripLocal;
    Collider[] colliders;
    bool[] colliderEnabled;
    bool stowed;

    public bool IsStowed => stowed;
    public bool Climbing { get; set; }
    public bool CinematicTraversal { get; set; }
    // Negative uses normal swimming ownership. A cinematic may blend between
    // the existing hand grip and back pose without reparenting the weapon.
    public float CinematicStowWeight { get; set; } = -1;

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
                Story1CapsuleContact.Segment(capsule,out var a,out var b,out _);
                Vector3 hand=stick.parent!=null?stick.parent.position:stick.position;
                shaftGripLocal=stick.InverseTransformPoint((a-hand).sqrMagnitude<(b-hand).sqrMagnitude?a:b);
            }
            else shaftGripLocal=mesh.bounds.ClosestPoint(stick.parent!=null?stick.InverseTransformPoint(stick.parent.position):Vector3.zero);
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
        float blend=CinematicStowWeight>=0?Mathf.Clamp01(CinematicStowWeight):1;
        bool swimming = CinematicStowWeight>=0?blend>0:CinematicTraversal || controller.enabled && (controller.Swimming || Climbing);
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
        Vector3 heldGrip=Vector3.zero;
        Quaternion heldRotation=stick.parent.rotation*gripRotation;
        if(blend<1)
        {
            stick.SetPositionAndRotation(stick.parent.TransformPoint(gripPosition),heldRotation);
            heldGrip=stick.TransformPoint(shaftGripLocal);
        }
        // Give the shaft a stable roll as well as its diagonal direction.
        stick.rotation = Quaternion.LookRotation(front, shaft) *
            Quaternion.Inverse(Quaternion.LookRotation(shaftFront, shaftAxis));
        Vector3 centre = chest.position - front * (backOffset+bodyClearance) + up * heightOffset;
        stick.position = centre - stick.TransformVector(mesh.bounds.center);
        if(blend<1)
        {
            // The imported origin is far from the grip. Blending that origin
            // while rotating sweeps the shaft through the torso. Transport the
            // real grip instead, then reconstruct the unchanged mesh transform.
            Vector3 backGrip=stick.TransformPoint(shaftGripLocal);
            float draw=1-blend;
            Vector3 outsideAxis=-across;
            Quaternion outside=Quaternion.LookRotation(front,outsideAxis)*
                Quaternion.Inverse(Quaternion.LookRotation(shaftFront,shaftAxis));
            stick.rotation=draw<.5f
                ?Quaternion.Slerp(stick.rotation,outside,Mathf.SmoothStep(0,1,draw*2))
                :Quaternion.Slerp(outside,heldRotation,Mathf.SmoothStep(0,1,(draw-.5f)*2));
            // Quaternion roll can bow the shaft inward even when both key poses
            // are clear. Constrain its physical axis to the outboard swing.
            Vector3 desiredAxis=draw<.4f
                ?Vector3.Slerp(shaft,outsideAxis,Mathf.SmoothStep(0,1,draw/.4f))
                :draw<.75f
                    ?Vector3.Slerp(outsideAxis,-front,Mathf.SmoothStep(0,1,(draw-.4f)/.35f))
                    :Vector3.Slerp(-front,heldRotation*shaftAxis,Mathf.SmoothStep(0,1,(draw-.75f)/.25f));
            stick.rotation=Quaternion.FromToRotation(stick.rotation*shaftAxis,desiredAxis)*stick.rotation;
            // Keep the grip outboard until the long end has cleared the torso;
            // fading the arc halfway through the wrist turn cut the hips.
            float sideWeight=Mathf.SmoothStep(0,1,draw/.4f)*(1-Mathf.SmoothStep(0,1,(draw-.92f)/.08f));
            float forwardWeight=Mathf.SmoothStep(0,1,(draw-.35f)/.3f)*(1-Mathf.SmoothStep(0,1,(draw-.92f)/.08f));
            Vector3 grip=Vector3.Lerp(heldGrip,backGrip,blend)+across*(sideWeight*drawSideOffset)+front*(forwardWeight*drawForwardOffset);
            stick.position=grip-stick.TransformVector(shaftGripLocal);
        }
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

    void OnDisable() { CinematicStowWeight=-1;RestoreGrip(); }
}
