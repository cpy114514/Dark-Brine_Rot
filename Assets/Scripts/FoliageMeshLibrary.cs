using UnityEngine;

namespace Mavis
{
    public sealed class FoliageMeshLibrary : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            public Mesh source, near, far;
            public bool grass;
        }
        public Entry[] entries;
    }
}
