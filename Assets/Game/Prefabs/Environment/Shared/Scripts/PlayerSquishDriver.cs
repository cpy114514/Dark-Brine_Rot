// PlayerSquishDriver.cs
// Per-frame pushes the player's world position + push radius to the Mavis/FoliageWind shader.
// Any foliage using that shader will bend/squish away from the player automatically.
using UnityEngine;

namespace Mavis
{
    [ExecuteAlways]
    public class PlayerSquishDriver : MonoBehaviour
    {
        public Transform player;
        [Range(0.5f, 8f)] public float radius = 2.5f;
        public bool drawGizmo = true;

        void Update()
        {
            if (player == null) return;
            Shader.SetGlobalVector("_MavisPlayerPos",
                new Vector4(player.position.x, player.position.y, player.position.z, radius));
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmo || player == null) return;
            Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(player.position, radius);
        }
    }
}