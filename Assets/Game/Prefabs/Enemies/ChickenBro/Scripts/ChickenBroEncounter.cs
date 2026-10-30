using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Three short waves after Cappuccino. Mostly Chicken Bros, with long-legged ducks mixed in.</summary>
    [DisallowMultipleComponent]
    public sealed class ChickenBroEncounter : MonoBehaviour
    {
        public ChickenBroAI prefab;
        public ChickenBroAI duck;
        [Range(0f, 1f)] public float duckRatio = 0.3f;
        public int[] waveCounts = { 4, 6, 8 };
        [Min(1)] public int maxAlive = 8;
        [Min(1)] public int maxSimultaneousAttackers = 2;
        [Min(4f)] public float triggerRadius = 38f;
        [Min(8f)] public float arenaSize = 72f;
        [Min(2f)] public float spawnRadius = 8f;
        [Min(0f)] public float waveGap = 2.4f;
        [Min(20f)] public float distanceFromCappuccino = 50f;
        public bool placeBesideCappuccino = true;
        public string completionId = "first-island-chicken-bro-flock";

        ChickenBroFlock flock;
        NavMeshSurface surface;
        CappuccinoAI cappuccino;
        ProceduralIsland island;
        ThirdPersonPlayerController player;
        WorldSaveIdentity identity;
        Text caption;
        GameObject captionRoot;
        float nextWatch, nextWaveAt, captionUntil;
        int wave = -1, spawned, ducks;
        bool anchored, started, baking, finished;
        public static ChickenBroEncounter Active { get; private set; }
        public int Alive => flock == null ? 0 : flock.Members.Count;

        void Awake()
        {
            Active = this;
            identity = GetComponent<WorldSaveIdentity>();
            if (identity && string.IsNullOrEmpty(identity.id)) identity.id = completionId;
            flock = new ChickenBroFlock { maxAttackers = maxSimultaneousAttackers };
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (captionRoot) Destroy(captionRoot);
            flock?.Close();
        }

        void Update()
        {
            if (finished || !Application.isPlaying) return;
            if (started) { RunWaves(); if (caption && Time.unscaledTime > captionUntil) captionRoot.SetActive(false); return; }
            if (Time.unscaledTime < nextWatch) return;
            nextWatch = Time.unscaledTime + .25f;
            if (GameWorldProgress.IsDefeated(identity != null ? (Component)identity : this)) { Finish(false); return; }
            if (GameSaveManager.IsContinuePending || AdditiveSceneBootstrap.IsLoading) return;
            if (!CapriDefeated()) return;
            if (!anchored && !PlaceInland()) return;
            if (!player) player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!player || player.Swimming || player.ExternalControlLock) return;
            if (Flat(player.transform.position) > triggerRadius) return;
            Begin();
        }

        bool CapriDefeated()
        {
            if (!cappuccino) cappuccino = FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);
            if (cappuccino)
            {
                var health = cappuccino.GetComponent<Health>();
                if (health && health.IsDead) return true;
            }
            return FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Capri);
        }

        bool PlaceInland()
        {
            if (!cappuccino) return false;
            if (!island)
                foreach (var candidate in FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
                    if (candidate && candidate.isActiveAndEnabled && (!island || candidate.shorelineRadius > island.shorelineRadius))
                        island = candidate;
            if (!island) return false;
            var ocean = FindFirstObjectByType<OceanWorld>();
            float inlandFloor = (ocean ? ocean.oceanHeight : 0f) + island.beachHeight + 3f;
            Vector3 towardMiddle = Vector3.ProjectOnPlane(island.transform.position - cappuccino.transform.position, Vector3.up);
            if (towardMiddle.sqrMagnitude < 4f) towardMiddle = Vector3.forward;
            towardMiddle.Normalize();
            for (float distance = 70f; distance <= 150f; distance += 12f)
            {
                Vector3 at = cappuccino.transform.position + towardMiddle * distance;
                if (!FindGround(at, out var ground) || ground.y < inlandFloor) continue;
                transform.position = ground;
                triggerRadius = 56f;
                anchored = true;
                return true;
            }
            return false;
        }

        void Begin()
        {
            if (!prefab) { Debug.LogError("Chicken Bro encounter has no prefab.", this); finished = true; return; }
            started = true;
            flock.maxAttackers = maxSimultaneousAttackers;
            flock.Open();
            Bake();
            wave = 0;
            spawned = 0;
            ducks = 0;
            SpawnBudget();
            Say("Wave 1  ·  The flock is closing in.", 4f);
        }

        void Bake()
        {
            if (baking) return;
            baking = true;
            surface = gameObject.GetComponent<NavMeshSurface>();
            if (!surface) surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = Vector3.up * 8f;
            surface.size = new Vector3(arenaSize, 36f, arenaSize);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        void RunWaves()
        {
            if (wave < 0 || finished) return;
            SpawnBudget();
            if (spawned < waveCounts[wave] || Alive > 0) return;
            if (nextWaveAt <= 0f)
            {
                if (wave >= waveCounts.Length - 1) { Finish(true); return; }
                nextWaveAt = Time.time + waveGap;
                Say("Wave " + (wave + 1) + " clear.", 1.6f);
                return;
            }
            if (Time.time < nextWaveAt) return;
            wave++;
            spawned = 0;
            ducks = 0;
            nextWaveAt = 0f;
            SpawnBudget();
            Say(wave == 1 ? "Wave 2  ·  Chicken Bros and long-legged ducks arrive." : "Wave 3  ·  The flock closes in.", 3.5f);
        }

        void SpawnBudget()
        {
            while (spawned < waveCounts[wave] && Alive < maxAlive)
            {
                bool asDuck = duck && ducks < Mathf.Round((spawned + 1) * duckRatio);
                var chosen = asDuck ? duck : prefab;
                if (!chosen) break;
                Vector3 at = Ring(spawned, waveCounts[wave]);
                var bro = Instantiate(chosen);
                bro.Activate(at, Quaternion.LookRotation(FlatDirection(at, player ? player.transform.position : transform.position), Vector3.up), flock);
                if (asDuck) ducks++;
                spawned++;
            }
        }

        public void Reclaim(ChickenBroAI bro)
        {
            if (bro) Destroy(bro.gameObject);
        }

        void Finish(bool celebrate)
        {
            if (finished) return;
            finished = true;
            flock?.Close();
            if (celebrate)
            {
                Component saved = identity != null ? (Component)identity : this;
                if (saved) GameWorldProgress.Record(saved, 0);
                GameSaveManager.SaveCurrentGame();
                Say("The flock scatters.", 4f);
            }
        }

        Vector3 Ring(int index, int count)
        {
            float angle = index / (float)Mathf.Max(1, count) * Mathf.PI * 2f + wave * .4f;
            Vector3 at = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
            return FindGround(at, out var ground) ? ground : new Vector3(at.x, transform.position.y, at.z);
        }

        bool FindGround(Vector3 at, out Vector3 point)
        {
            point = at;
            var hits = Physics.RaycastAll(at + Vector3.up * 40f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            bool found = false;
            var ocean = FindFirstObjectByType<OceanWorld>();
            foreach (var hit in hits)
            {
                if (!hit.collider || CombatBodyCollision.IsActor(hit.collider) || hit.normal.y < .6f) continue;
                if (ocean && hit.point.y < ocean.oceanHeight + .2f) continue;
                if (hit.distance < best) { best = hit.distance; point = hit.point + Vector3.up * .05f; found = true; }
            }
            return found;
        }

        void Say(string message, float seconds)
        {
            if (!captionRoot)
            {
                captionRoot = new GameObject("Chicken Bro caption", typeof(RectTransform), typeof(Canvas));
                var canvas = captionRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 40;
                GameUITheme.Scale(canvas);
                var card = GameUITheme.Rect(captionRoot.transform, "Card", new Vector2(.5f, 1f), new Vector2(740, 64));
                card.pivot = new Vector2(.5f, 1f);
                card.anchoredPosition = new Vector2(0f, -132f);
                GameUITheme.Image(card, new Color(.035f, .045f, .045f, .82f));
                caption = GameUITheme.Label(card, "Caption", 22, Vector2.zero, new Vector2(700, 48), TextAnchor.MiddleCenter);
                caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(.5f, .5f);
            }
            captionRoot.SetActive(true);
            caption.text = message;
            captionUntil = Time.unscaledTime + seconds;
        }

        float Flat(Vector3 point)
        {
            point -= transform.position;
            point.y = 0f;
            return point.magnitude;
        }

        static Vector3 FlatDirection(Vector3 from, Vector3 to)
        {
            to -= from;
            to.y = 0f;
            return to.sqrMagnitude > .01f ? to.normalized : Vector3.forward;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .75f, .2f, .8f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
