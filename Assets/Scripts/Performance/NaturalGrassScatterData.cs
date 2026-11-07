using UnityEngine;

namespace Mavis
{
    public sealed class NaturalGrassScatterData : ScriptableObject
    {
        [System.Serializable]
        public sealed class Cell
        {
            public Bounds bounds;
            public Vector4[] positions; // xyz world position, w yaw in degrees
            public Vector3[] normals;
            public byte[] variants;     // optional mesh index per tuft
            public float[] scales;      // optional authored scale per tuft
        }
        public Mesh[] meshes;
        public Material material;
        public Cell[] cells;
        [Header("Optional shared distance LODs; empty arrays preserve the original meadow")]
        public Mesh[] mediumMeshes;
        public Mesh[] farMeshes;
        public Vector2 mediumTransition = new Vector2(32,48);
        public Vector2 farTransition = new Vector2(75,100);
    }
}
