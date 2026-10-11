using UnityEngine;

namespace Mavis
{
    /// <summary>Runtime dimensions; authored tuning stays unchanged when the model is resized.</summary>
    public static class NailongSize
    {
        // Range grows more slowly than the mesh to keep the enlarged boss dodgeable.
        public static float RangeFactor(Transform root) =>
            Mathf.Sqrt(Mathf.Max(.01f, Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z))));

        public static Vector3 Feet(Transform root)
        {
            var motor = root.GetComponent<CharacterController>();
            if (motor && motor.enabled)
                return root.TransformPoint(motor.center - Vector3.up * motor.height * .5f);
            var body = root.GetComponent<CapsuleCollider>();
            if (body && body.direction == 1)
                return root.TransformPoint(body.center - Vector3.up * body.height * .5f);
            return root.position;
        }
    }
}
