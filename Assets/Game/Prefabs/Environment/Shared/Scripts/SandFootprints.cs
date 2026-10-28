using UnityEngine;

namespace Mavis
{
    /// <summary>Leaves a short trail of prints while Sahur walks on the sandy beach.</summary>
    [DisallowMultipleComponent]
    public sealed class SandFootprints : MonoBehaviour
    {
        const int PoolSize = 28;
        [Min(.2f)] public float stepDistance = .62f;
        [Min(1f)] public float lifetime = 16f;

        OceanWorld ocean;
        ProceduralIsland island;
        ThirdPersonPlayerController player;
        Transform leftFoot, rightFoot;
        Vector3 leftLast, rightLast;
        bool leftReady, rightReady;
        Mesh oval;
        Material material;
        Stamp[] stamps;
        int cursor;
        float nextSearch;

        [System.Serializable]
        struct Stamp
        {
            public Transform transform;
            public MeshRenderer renderer;
            public float born;
        }

        void Awake()
        {
            oval = BuildOval();
            var shader = Shader.Find("Mavis/SandFootprint");
            material = new Material(shader ? shader : Shader.Find("Sprites/Default"));
            material.color = new Color(.33f, .22f, .11f, .62f);
            stamps = new Stamp[PoolSize];
            for (int i = 0; i < stamps.Length; i++)
            {
                var go = new GameObject("Sand footprint");
                go.transform.SetParent(transform, false);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = oval;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                go.SetActive(false);
                stamps[i] = new Stamp { transform = go.transform, renderer = renderer };
            }
        }

        void OnDestroy()
        {
            if (material) Destroy(material);
            if (oval) Destroy(oval);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying || Time.deltaTime <= 0f || PauseSettingsMenu.IsOpen) return;
            Fade();
            if (Time.time >= nextSearch) { nextSearch = Time.time + .5f; Bind(); }
            if (!player || player.Swimming || player.ExternalControlLock || !island || !ocean) return;
            if (leftFoot) Press(leftFoot, ref leftLast, ref leftReady);
            if (rightFoot) Press(rightFoot, ref rightLast, ref rightReady);
            if (!leftFoot && !rightFoot) PressBody();
        }

        void Bind()
        {
            if (!ocean) ocean = FindFirstObjectByType<OceanWorld>();
            if (!island)
                foreach (var candidate in FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
                    if (candidate && candidate.isActiveAndEnabled && candidate.beachSand) { island = candidate; break; }
            if (!player) player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!player) return;
            var animator = player.CharacterAnimator;
            if (!animator || !animator.isHuman) return;
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        void Press(Transform foot, ref Vector3 last, ref bool ready)
        {
            Vector3 at = foot.position;
            if (!ready) { last = at; ready = true; return; }
            if ((at - last).sqrMagnitude < stepDistance * stepDistance) return;
            if (TryStamp(at + Vector3.up * .8f, foot.forward)) last = at;
        }

        Vector3 bodyLast;
        bool bodyReady;
        void PressBody()
        {
            Vector3 at = player.transform.position;
            if (!bodyReady) { bodyLast = at; bodyReady = true; return; }
            Vector3 step = at - bodyLast;
            step.y = 0f;
            if (step.sqrMagnitude < stepDistance * stepDistance) return;
            Vector3 side = Vector3.Cross(Vector3.up, step.normalized) * (cursor % 2 == 0 ? .16f : -.16f);
            if (TryStamp(at + side + Vector3.up * .4f, step)) bodyLast = at;
        }

        bool TryStamp(Vector3 origin, Vector3 forward)
        {
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 2.2f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < .62f || CombatBodyCollision.IsActor(hit.collider)) return false;
            if (!hit.collider.GetComponentInParent<ProceduralIsland>()) return false;
            if (hit.point.y - ocean.oceanHeight >= island.beachHeight) return false;
            var stamp = stamps[cursor];
            cursor = (cursor + 1) % stamps.Length;
            Vector3 facing = Vector3.ProjectOnPlane(forward, hit.normal);
            if (facing.sqrMagnitude < .0001f) facing = Vector3.ProjectOnPlane(player.transform.forward, hit.normal);
            if (facing.sqrMagnitude < .0001f) facing = Vector3.forward;
            stamp.transform.SetPositionAndRotation(hit.point + hit.normal * .025f, Quaternion.LookRotation(facing.normalized, hit.normal));
            stamp.transform.gameObject.SetActive(true);
            stamp.born = Time.time;
            SetAlpha(stamp.renderer, 1f);
            stamps[(cursor + PoolSize - 1) % PoolSize] = stamp;
            return true;
        }

        void Fade()
        {
            for (int i = 0; i < stamps.Length; i++)
            {
                if (!stamps[i].transform || !stamps[i].transform.gameObject.activeSelf) continue;
                float age = Time.time - stamps[i].born;
                if (age >= lifetime) { stamps[i].transform.gameObject.SetActive(false); continue; }
                float fade = age / lifetime;
                SetAlpha(stamps[i].renderer, fade < .72f ? 1f : 1f - (fade - .72f) / .28f);
            }
        }

        static void SetAlpha(MeshRenderer renderer, float alpha)
        {
            if (!renderer) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(.33f, .22f, .11f, .62f * Mathf.Clamp01(alpha)));
            renderer.SetPropertyBlock(block);
        }

        static Mesh BuildOval()
        {
            var mesh = new Mesh { name = "Sand footprint" };
            mesh.vertices = new[]
            {
                new Vector3(-.16f, 0f, -.28f), new Vector3(.16f, 0f, -.28f),
                new Vector3(-.16f, 0f, .28f), new Vector3(.16f, 0f, .28f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
