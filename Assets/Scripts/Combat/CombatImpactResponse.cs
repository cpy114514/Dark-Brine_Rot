using UnityEngine;
namespace Mavis
{
    public sealed class CombatImpactResponse : MonoBehaviour
    {
        [Tooltip("Large or armored enemies retain their current action on light hits.")]
        public bool unflinching;
        public bool blocksMelee;
        public CombatMaterial material = CombatMaterial.Flesh;
        [Range(0,2)] public float stopMultiplier = 1;
        [Range(0,2)] public float reactionMultiplier = 1;
        [Range(0,2)] public float knockbackMultiplier = 1;
    }
}
