using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class IslandRespawnPoint : MonoBehaviour
    {
        public string pointName = "Island checkpoint";
        public bool isDefault = true;
        public float activationRadius = 3f;
        PlayerDeathRespawn player;
        CharacterController playerBody;
        float nextSearch;
        void Update()
        {
            if(player==null && Time.unscaledTime>=nextSearch)
            {player=FindFirstObjectByType<PlayerDeathRespawn>();nextSearch=Time.unscaledTime+.5f;}
            if (player == null || player.IsDead) return;
            if (playerBody == null) playerBody = player.GetComponent<CharacterController>();
            Vector3 position = player.transform.position;
            if (playerBody != null && playerBody.enabled)
            {
                // Checkpoints are on the floor; imported model roots can sit above the feet.
                var bounds = playerBody.bounds;
                position = bounds.center - Vector3.up * bounds.extents.y;
            }
            if ((position - transform.position).sqrMagnitude < activationRadius * activationRadius)
                if(player.Checkpoint!=this)player.SetCheckpoint(this);
        }
        public Vector3 GroundPosition => transform.position;
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.95f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }
    }
}
