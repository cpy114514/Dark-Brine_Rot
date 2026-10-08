using UnityEngine;

namespace Mavis
{
    public sealed class SahurHandRigData : ScriptableObject
    {
        [System.Serializable]
        public struct FingerBone
        {
            public string name;
            public int parentIndex;
            public Vector3 localPosition;
            public Quaternion closedRotation;
        }
        public Mesh sourceMesh;
        public Mesh skinnedMesh;
        public int originalBoneCount;
        public FingerBone[] fingers;
    }
}
