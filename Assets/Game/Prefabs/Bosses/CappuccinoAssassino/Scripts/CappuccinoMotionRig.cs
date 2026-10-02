using System;
using UnityEngine;

namespace Mavis
{
    /// <summary>Transfers authored humanoid motion onto the cup's fitted, rigid body rig.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class CappuccinoMotionRig : MonoBehaviour
    {
        [Serializable]
        public struct BoneBinding
        {
            public HumanBodyBones sourceBone;
            public Transform target;
            public Vector3 restLocalPosition;
            public Quaternion sourceReferenceRotation;
            public Quaternion targetReferenceRotation;
        }

        [Serializable]
        public struct GroundPoint
        {
            public Transform bone;
            public Vector3 localPoint;
        }

        public Animator motionAnimator;
        public Transform visualFrame;
        public Transform rigRoot;
        public BoneBinding[] bindings;
        public GroundPoint[] groundPoints;
        public GroundPoint[] bladeGroundPoints;
        public Vector3 sourceReferenceHipPosition;
        public float hipMotionScale = 0.5f;
        [HideInInspector] public Vector3 groundNormal = Vector3.up;

        Transform[] sourceBones;
        Transform sourceHips;
        Vector3 rigRestPosition;
        float floorHeight;

        public void Initialize()
        {
            if (motionAnimator == null || !motionAnimator.isHuman || visualFrame == null || rigRoot == null) return;
            sourceBones = new Transform[bindings.Length];
            for (int i = 0; i < bindings.Length; i++) sourceBones[i] = motionAnimator.GetBoneTransform(bindings[i].sourceBone);
            sourceHips = motionAnimator.GetBoneTransform(HumanBodyBones.Hips);
            rigRestPosition = rigRoot.localPosition;
            floorHeight = visualFrame.position.y;
        }

        void Awake() => Initialize();
        void LateUpdate() => ApplyPose();

        public void SetDeathGrounding() => floorHeight = visualFrame.position.y;

        public void ApplyPose()
        {
            if (sourceBones == null) Initialize();
            if (sourceBones == null) return;
            rigRoot.localPosition = rigRestPosition;
            var sourceFrame = motionAnimator.transform;
            Vector3 hipOffset = sourceFrame.InverseTransformPoint(sourceHips.position) - sourceReferenceHipPosition;
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.target == null || sourceBones[i] == null) continue;
                binding.target.localPosition = binding.restLocalPosition;
                if (binding.sourceBone == HumanBodyBones.Hips) binding.target.localPosition += hipOffset * hipMotionScale;
                Quaternion sourceRotation = Quaternion.Inverse(sourceFrame.rotation) * sourceBones[i].rotation;
                binding.target.rotation = visualFrame.rotation * sourceRotation *
                    Quaternion.Inverse(binding.sourceReferenceRotation) * binding.targetReferenceRotation;
            }
            // The cup is much wider than a human torso. Keep its fitted body above the floor during the fall.
            floorHeight = visualFrame.position.y;
            Vector3 normal = groundNormal.sqrMagnitude > 0.5f ? groundNormal.normalized : Vector3.up;
            if (groundPoints != null && groundPoints.Length > 0)
            {
                float lowest = float.PositiveInfinity;
                foreach (var point in groundPoints)
                    if (point.bone != null) lowest = Mathf.Min(lowest,
                        Vector3.Dot(point.bone.TransformPoint(point.localPoint) - visualFrame.position, normal));
                var state = motionAnimator.GetCurrentAnimatorStateInfo(0);
                bool walking = state.IsName("Walk") || state.IsName("Retreat");
                // Different source proportions can leave both cup boots floating during a walk cycle.
                if (lowest < -0.001f || (walking && lowest > 0.025f))
                    rigRoot.position += Vector3.up * ((0.015f - lowest) / Mathf.Max(0.2f, normal.y));
            }
            KeepBladesAboveFloor();
        }

        void KeepBladesAboveFloor()
        {
            if (bladeGroundPoints == null) return;
            Vector3 normal = groundNormal.sqrMagnitude > 0.5f ? groundNormal.normalized : Vector3.up;
            // Short cup arms carry full length swords. Limit only the part of a swing that would dig into the floor.
            for (int handIndex = 0; handIndex < bindings.Length; handIndex++)
            {
                var binding = bindings[handIndex];
                if (binding.sourceBone != HumanBodyBones.LeftHand && binding.sourceBone != HumanBodyBones.RightHand) continue;
                Transform hand = binding.target;
                for (int pass = 0; pass < 4; pass++)
                {
                    Vector3 lowestPoint = default;
                    float lowest = float.PositiveInfinity;
                    foreach (var point in bladeGroundPoints)
                    {
                        if (point.bone != hand) continue;
                        Vector3 world = hand.TransformPoint(point.localPoint);
                        float height = Vector3.Dot(world - visualFrame.position, normal);
                        if (height < lowest) { lowest = height; lowestPoint = world; }
                    }
                    if (lowest >= 0.02f) break;
                    Vector3 offset = lowestPoint - hand.position;
                    float length = offset.magnitude;
                    if (length < 0.001f) break;
                    float y = Mathf.Clamp(0.025f - Vector3.Dot(hand.position - visualFrame.position, normal), -length * 0.999f, length * 0.999f);
                    Vector3 flat = Vector3.ProjectOnPlane(offset, normal).normalized;
                    if (flat.sqrMagnitude < 0.1f) flat = visualFrame.forward;
                    Vector3 corrected = flat * Mathf.Sqrt(Mathf.Max(0, length * length - y * y)) + normal * y;
                    hand.rotation = Quaternion.FromToRotation(offset, corrected) * hand.rotation;
                }
            }
        }
    }
}
