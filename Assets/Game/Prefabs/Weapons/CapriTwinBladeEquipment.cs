using UnityEngine;

namespace Mavis
{
    /// <summary>Independent Capri weapon meshes and Sahur's holding animation set.</summary>
    [DisallowMultipleComponent]
    public sealed class CapriTwinBladeEquipment : MonoBehaviour
    {
        public Transform leftBlade;
        public Transform rightBlade;
        public AnimationClip holdingIdle;
        public AnimationClip holdingWalk;
        public AnimatorOverrideController holdingController;
        public Mesh handGripMesh;
        public SahurHandRigData handRig;
    }
}
