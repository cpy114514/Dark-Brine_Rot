using UnityEngine;

namespace Mavis
{
    [CreateAssetMenu(menuName="Mavis/Island ecology")]
    public sealed class IslandEcologyData : ScriptableObject
    {
        [System.Serializable]
        public sealed class Cell
        {
            public Bounds bounds;
            public Vector4[] positions; // xyz world root, w yaw
            public Vector4[] normals;   // xyz ground normal, w uniform scale
        }
        [System.Serializable]
        public sealed class Species
        {
            public string name;
            public bool uncanny;
            public Mesh near, far;
            public Material material;
            [Min(5)] public float lodStart=38, lodEnd=52, drawDistance=150, shadowDistance=45;
            public Cell[] cells;
        }
        public int seed=740311;
        public string scope;
        public Species[] species;
    }
}
