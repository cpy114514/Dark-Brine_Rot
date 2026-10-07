using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class IslandRespawnPoint : MonoBehaviour
    {
        public string pointName = "小岛复活点";
        public bool isDefault = true;
        public float activationRadius = 3f;
        PlayerDeathRespawn player;
        float nextSearch;
        void Update()
        {
            if(player==null && Time.unscaledTime>=nextSearch)
            {player=FindFirstObjectByType<PlayerDeathRespawn>();nextSearch=Time.unscaledTime+.5f;}
            if (player == null || player.IsDead) return;
            if ((player.transform.position - transform.position).sqrMagnitude < activationRadius * activationRadius)
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
