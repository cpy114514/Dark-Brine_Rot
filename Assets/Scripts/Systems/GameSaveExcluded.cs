using UnityEngine;

namespace Mavis
{
    /// <summary>Marks preview or combat demo players that must never replace real game progress.</summary>
    [DisallowMultipleComponent]
    public sealed class GameSaveExcluded : MonoBehaviour { }
}
