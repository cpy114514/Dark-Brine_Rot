using UnityEngine;

namespace Mavis
{
    public sealed class FoliageMeshLibrary : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            public Mesh source, near, far;
            public Mesh close;
            public bool grass;
            // Optional close mesh preserves existing detail before the lighter near mesh takes over.
            public float fullDetailDistance;
        }
        public Entry[] entries;
    }
}
