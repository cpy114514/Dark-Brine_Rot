// EnemySpawner.cs
// Periodically instantiates enemy prefabs at NavMesh-valid points
// around spawnCenter. Tracks alive count via Health.OnDeath.
using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        public GameObject enemyPrefab;

        [Header("Cadence")]
        public float spawnInterval = 5f;
        [Tooltip("Hard cap on simultaneously alive enemies from this spawner.")]
        public int maxAlive = 5;

        [Header("Area")]
        public Transform spawnCenter;
        public float spawnRadius = 10f;
        public float navMeshSampleRadius = 5f;

        [Header("Debug")]
        public bool spawnOnEnable = false;

        float nextSpawnTime;
        int aliveCount;

        public int AliveCount => aliveCount;

        void Awake()
        {
            if (spawnCenter == null) spawnCenter = transform;
        }

        void OnEnable()
        {
            if (spawnOnEnable) TrySpawnOne();
            nextSpawnTime = Time.time + spawnInterval;
        }

        void Update()
        {
            if (enemyPrefab == null) return;
            if (aliveCount >= maxAlive) return;
            if (Time.time < nextSpawnTime) return;

            if (TrySpawnOne())
                nextSpawnTime = Time.time + spawnInterval;
            else
                nextSpawnTime = Time.time + 1f; // retry soon
        }

        bool TrySpawnOne()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = Random.insideUnitCircle * spawnRadius;
                Vector3 p = spawnCenter.position + new Vector3(r.x, 0f, r.y);
                if (!NavMesh.SamplePosition(p, out var hit, navMeshSampleRadius, NavMesh.AllAreas))
                    continue;

                var go = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                var h = go.GetComponent<Health>();
                if (h != null)
                {
                    aliveCount++;
                    h.OnDeath.AddListener(() => aliveCount--);
                }
                return true;
            }
            return false;
        }

        void OnDrawGizmosSelected()
        {
            Transform c = spawnCenter != null ? spawnCenter : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(c.position, spawnRadius);
        }
    }
}