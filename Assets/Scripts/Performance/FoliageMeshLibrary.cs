using UnityEngine;

namespace Mavis
{
    public sealed class FoliageMeshLibrary : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            public Mesh source, near, far;
            // Render-only layout with the exact same geometry and shader inputs.
            public Mesh renderSource;
            public Mesh close;
            public bool grass;
            // Optional close mesh preserves existing detail before the lighter near mesh takes over.
            public float fullDetailDistance;
        }
        public Entry[] entries;
    }
}
