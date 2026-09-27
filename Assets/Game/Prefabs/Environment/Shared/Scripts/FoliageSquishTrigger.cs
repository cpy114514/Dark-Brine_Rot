// FoliageSquishTrigger.cs
// Sits on every foliage root that already has a trigger SphereCollider.
// Tracks the player and tells the shader to ramp squish up while inside.
using UnityEngine;

namespace Mavis
{
    [RequireComponent(typeof(Collider))]
    public class FoliageSquishTrigger : MonoBehaviour
    {
        public string playerTag = "Player";
        public float squishRadius = 2.0f;
        public float maxInfluence = 1.0f;

        Transform player;
        bool playerInside;

        void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (player != null) return;
            if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag) || other.attachedRigidbody != null)
            {
                player = other.transform.root;
                playerInside = true;
                Shader.SetGlobalVector("_MavisPlayerPos",
                    new Vector4(player.position.x, player.position.y, player.position.z, squishRadius));
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (player != null && other.transform.root == player)
            {
                playerInside = false;
                Shader.SetGlobalVector("_MavisPlayerPos", new Vector4(0, -999, 0, 0.0001f));
            }
        }

        void Update()
        {
            if (!playerInside || player == null) return;
            Shader.SetGlobalVector("_MavisPlayerPos",
                new Vector4(player.position.x, player.position.y, player.position.z, squishRadius));
        }
    }
}