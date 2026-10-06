using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>One shared GPU interaction field; no per-blade colliders or Update methods.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class FoliageInteractionSystem : MonoBehaviour
    {
        public float radius = 1.4f;
        public float recoverySeconds = 1.15f;
        sealed class Body
        {
            public Transform transform;
            public Collider collider;
            public Vector3 previous, footprint;
            public bool initialized, player;
        }
        struct Trail { public Vector3 feet, direction; public float age, strength; }
        readonly List<Body> bodies = new List<Body>(4);
        readonly List<Trail> trails = new List<Trail>(8);
        readonly Dictionary<Vector2Int, List<Renderer>> grass = new Dictionary<Vector2Int, List<Renderer>>();
        readonly Vector4[] positions = new Vector4[12], motions = new Vector4[12];
        float nextScan;
        AudioSource rustle;
        AudioClip rustleClip;
        static Vector2Int Cell(Vector3 p) { return new Vector2Int(Mathf.FloorToInt(p.x / 10f), Mathf.FloorToInt(p.z / 10f)); }

        void OnEnable() { RebuildGrassIndex(); nextScan = 0f; }
        public void RebuildGrassIndex()
        {
            grass.Clear();
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool isGrass = false;
                foreach (var material in renderer.sharedMaterials)
                    if (material && material.shader.name == "Mavis/FoliageWind" && material.name.ToLowerInvariant().Contains("grass")) isGrass = true;
                if (!isGrass) continue;
                Bounds bounds = renderer.bounds;
                Vector2Int min = Cell(bounds.min), max = Cell(bounds.max);
                // Guard accidentally assigned terrain materials: never allocate a whole-island grid.
                if (max.x - min.x > 20 || max.y - min.y > 20) continue;
                for (int x = min.x; x <= max.x; x++) for (int z = min.y; z <= max.y; z++)
                {
                    var key = new Vector2Int(x, z);
                    if (!grass.TryGetValue(key, out var list)) grass[key] = list = new List<Renderer>();
                    list.Add(renderer);
                }
            }
        }

        bool NearGrass(Vector3 feet)
        {
            if (!grass.TryGetValue(Cell(feet), out var list)) return false;
            foreach (var renderer in list)
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Bounds bounds = renderer.bounds;
                if (feet.x >= bounds.min.x && feet.x <= bounds.max.x && feet.z >= bounds.min.z && feet.z <= bounds.max.z &&
                    feet.y >= bounds.min.y - .8f && feet.y <= bounds.max.y + .3f) return true;
            }
            return false;
        }

        void ScanBodies()
        {
            bodies.RemoveAll(b => !b.transform || !b.transform.gameObject.activeInHierarchy);
            foreach (var player in FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
                AddBody(player.transform, player.GetComponent<CharacterController>(), true);
            foreach (var enemy in FindObjectsByType<NailongHealth>(FindObjectsSortMode.None))
                if (!enemy.IsDead) AddBody(enemy.transform, enemy.GetComponentInChildren<Collider>(), false);
            // Moving physics props can brush through foliage as well.
            foreach (var body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                if (!body.isKinematic && !body.IsSleeping()) AddBody(body.transform, body.GetComponent<Collider>(), false);
            bodies.Sort((a,b) => b.player.CompareTo(a.player));
            if (bodies.Count > 4) bodies.RemoveRange(4, bodies.Count - 4);
        }

        void AddBody(Transform actor, Collider collider, bool player)
        {
            foreach (var b in bodies) if (b.transform == actor || actor.IsChildOf(b.transform)) return;
            if (bodies.Count >= 4 && !player) return;
            bodies.Add(new Body { transform = actor, collider = collider, player = player });
        }

        void LateUpdate()
        {
            if (Time.time >= nextScan) { ScanBodies(); nextScan = Time.time + 1.5f; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // Paused map/backpack/death keeps both wind and interaction frozen.
            for (int i = trails.Count - 1; i >= 0; i--)
            {
                var t = trails[i]; t.age += dt;
                if (t.age >= recoverySeconds) trails.RemoveAt(i); else trails[i] = t;
            }
            int count = 0;
            float soundVolume = 0f;
            foreach (var body in bodies)
            {
                if (!body.transform || !body.transform.gameObject.activeInHierarchy) continue;
                var health = body.transform.GetComponent<NailongHealth>();
                if (health && health.IsDead) continue;
                Vector3 feet = body.collider && body.collider.enabled ? new Vector3(body.collider.bounds.center.x, body.collider.bounds.min.y, body.collider.bounds.center.z) : body.transform.position;
                Vector3 delta = body.initialized ? feet - body.previous : Vector3.zero;
                delta.y = 0f;
                bool teleport = delta.sqrMagnitude > 25f;
                float speed = teleport ? 0f : Mathf.Min(delta.magnitude / dt, 10f);
                Vector3 direction = delta.sqrMagnitude > .00001f ? delta.normalized : Vector3.zero;
                float strength = .45f + Mathf.Min(speed / 7f, .65f);
                positions[count] = new Vector4(feet.x, feet.y, feet.z, radius);
                motions[count++] = new Vector4(direction.x, direction.z, strength, 0f);
                if (!body.initialized || teleport) body.footprint = feet;
                if (speed > .2f && (feet - body.footprint).sqrMagnitude > .16f)
                {
                    if (trails.Count == 8) trails.RemoveAt(0);
                    trails.Add(new Trail { feet = body.previous, direction = direction, strength = strength * .65f });
                    body.footprint = feet;
                }
                if (body.player && speed > .3f && NearGrass(feet))
                {
                    EnsureEffects();
                    rustle.transform.position = feet + Vector3.up * .6f;
                    soundVolume = Mathf.Min(.065f, speed * .009f);
                    // Passing through grass bends the stems; it does not tear off
                    // a constant stream of floating leaf fragments.
                }
                body.previous = feet; body.initialized = true;
            }
            foreach (var trail in trails)
            {
                float fade = Mathf.Clamp01(1f - trail.age / Mathf.Max(.05f, recoverySeconds));
                fade = fade * fade * (3f - 2f * fade);
                positions[count] = new Vector4(trail.feet.x, trail.feet.y, trail.feet.z, radius * .85f);
                motions[count++] = new Vector4(trail.direction.x, trail.direction.z, trail.strength * fade, 0f);
            }
            Shader.SetGlobalVectorArray("_MavisFoliageBodies", positions);
            Shader.SetGlobalVectorArray("_MavisFoliageMotion", motions);
            Shader.SetGlobalInt("_MavisFoliageBodyCount", count);
            if (rustle)
            {
                if (!rustle.isPlaying && soundVolume > 0f) rustle.Play();
                rustle.volume = Mathf.Lerp(rustle.volume, soundVolume, 1f - Mathf.Exp(-dt * 8f));
            }
        }

        void EnsureEffects()
        {
            if (rustle) return;
            var holder = new GameObject("Foliage rustle"); holder.transform.SetParent(transform, false);
            rustle = holder.AddComponent<AudioSource>(); rustle.loop = true; rustle.spatialBlend = 1f; rustle.minDistance = 2f; rustle.maxDistance = 12f; rustle.volume = 0f;
            const int samples = 22050; var noise = new float[samples]; var random = new System.Random(9271); float filtered = 0f;
            for (int i=0;i<samples;i++) { filtered = Mathf.Lerp(filtered,(float)random.NextDouble()*2f-1f,.3f); noise[i] = filtered * Mathf.Sin(Mathf.PI*i/(samples-1)); }
            rustleClip = AudioClip.Create("Soft grass rustle", samples, 1, 22050, false); rustleClip.SetData(noise,0); rustle.clip = rustleClip; rustle.Play();
        }
        void OnDisable() { Shader.SetGlobalInt("_MavisFoliageBodyCount", 0); trails.Clear(); bodies.Clear(); if (rustle) rustle.Stop(); }
        void OnDestroy()
        {
            if (rustle) Destroy(rustle.gameObject);
            if (rustleClip) Destroy(rustleClip);
        }
    }
}
