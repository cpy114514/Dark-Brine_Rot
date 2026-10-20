using UnityEngine;

namespace Mavis
{
    /// <summary>Stable scene identity; assigned once in the editor, independent of hierarchy order.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldSaveIdentity : MonoBehaviour
    {
        public string id;
    }
}
