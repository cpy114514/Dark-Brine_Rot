using UnityEngine;

namespace Mavis
{
    /// <summary>Leaves a short trail of prints while Sahur walks on the sandy beach.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1300)]
    public sealed class SandFootprints : MonoBehaviour
    {
        const int PoolSize = 28;
        [Min(.2f)] public float stepDistance = .62f;
        [Min(1f)] public float lifetime = 16f;

        OceanWorld ocean;
        ProceduralIsland island;
        ThirdPersonPlayerController player;
        struct Foot
        {
            public Transform bone, toe;
            public Vector3 sole, previous, lastPrint;
            public bool tracked, planted, printed;
            public float lastTime;
        }
        Foot left, right;
        Animator boundAnimator;
        readonly RaycastHit[] groundHits = new RaycastHit[32];
        Mesh oval;
        Material material;
        MaterialPropertyBlock stampProperties;
        Stamp[] stamps;
        int cursor;
        float nextSearch;

        [System.Serializable]
        struct Stamp
        {
            public Transform transform;
            public MeshRenderer renderer;
            public float born;
            public float duration, strength;
        }

        void Awake()
        {
            oval = BuildOval();
            var shader = Shader.Find("Mavis/SandFootprint");
            material = new Material(shader ? shader : Shader.Find("Sprites/Default"));
            material.color = new Color(.40f, .38f, .34f, .68f);
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

        void OnDisable()
        {
            if (stamps != null) foreach (var stamp in stamps)
                if (stamp.transform) stamp.transform.gameObject.SetActive(false);
            left.tracked = right.tracked = left.planted = right.planted = bodyReady = false;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying || Time.deltaTime <= 0f || PauseSettingsMenu.IsOpen) return;
            Fade();
            if (Time.time >= nextSearch) { nextSearch = Time.time + .5f; Bind(); }
            if (!player || player.Swimming || player.ExternalControlLock || player.ExternalMovementLock ||
                !player.CanUseGroundAttack || player.OnFloatingWreckDeck || !island || !ocean)
            { left.planted = right.planted = false; return; }
            if (left.bone) Press(ref left, true);
            if (right.bone) Press(ref right, false);
            if (!left.bone && !right.bone) PressBody();
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
            if (!animator || !animator.isHuman || animator == boundAnimator) return;
            boundAnimator = animator;
            left = new Foot { bone = animator.GetBoneTransform(HumanBodyBones.LeftFoot), toe = animator.GetBoneTransform(HumanBodyBones.LeftToes) };
            right = new Foot { bone = animator.GetBoneTransform(HumanBodyBones.RightFoot), toe = animator.GetBoneTransform(HumanBodyBones.RightToes) };
            SahurSupportPose.MeasureSolesFast(animator, ref left.sole, ref right.sole);
        }

        void Press(ref Foot foot, bool isLeft)
        {
            Vector3 at = foot.bone.TransformPoint(foot.sole);
            if (!foot.tracked) { foot.previous = at; foot.tracked = true; return; }
            bool teleport = (at - foot.previous).sqrMagnitude > 16f;
            foot.previous = at;
            RaycastHit ground = default;
            bool contact = !teleport && Ground(at + Vector3.up * .35f, .65f, out ground) &&
                Mathf.Abs(Vector3.Dot(at - ground.point, ground.normal)) < .16f;
            bool landing = contact && !foot.planted;
            foot.planted = contact;
            if (!landing || Vector3.ProjectOnPlane(player.WorldVelocity, Vector3.up).sqrMagnitude < .16f || Time.time - foot.lastTime < .22f) return;
            if (foot.printed && Vector3.ProjectOnPlane(at - foot.lastPrint, Vector3.up).sqrMagnitude < stepDistance * stepDistance) return;
            Vector3 forward = foot.toe ? foot.toe.position - foot.bone.position : player.transform.forward;
            float length = foot.toe ? Mathf.Clamp(forward.magnitude * 1.7f, .65f, 1.25f) : .8f;
            if (TryStamp(ground, forward, isLeft, length))
            { foot.lastPrint = at; foot.lastTime = Time.time; foot.printed = true; }
        }

        Vector3 bodyLast;
        bool bodyReady;
        void PressBody()
        {
            var capsule = player.GetComponent<CharacterController>();
            Vector3 at = capsule ? new Vector3(capsule.bounds.center.x, capsule.bounds.min.y, capsule.bounds.center.z) : player.transform.position;
            if (!bodyReady) { bodyLast = at; bodyReady = true; return; }
            Vector3 step = at - bodyLast;
            step.y = 0f;
            if (step.sqrMagnitude < stepDistance * stepDistance) return;
            Vector3 side = Vector3.Cross(Vector3.up, step.normalized) * (cursor % 2 == 0 ? .16f : -.16f);
            if (Ground(at + side + Vector3.up * .35f, .65f, out var ground) &&
                TryStamp(ground, step, cursor % 2 == 0, .8f)) bodyLast = at;
        }

        bool Ground(Vector3 origin, float distance, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, distance, ~0, QueryTriggerInteraction.Ignore);
            if (count == groundHits.Length) return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (CombatBodyCollision.IsActor(hit.collider) || hit.distance >= best) continue;
                best = hit.distance; nearest = hit;
            }
            return best < float.PositiveInfinity;
        }

        bool TryStamp(RaycastHit hit, Vector3 forward, bool isLeft, float length)
        {
            if (hit.normal.y < .62f || CombatBodyCollision.IsActor(hit.collider)) return false;
            if (hit.collider.GetComponentInParent<ProceduralIsland>() != island) return false;
            float height = hit.point.y - ocean.oceanHeight;
            if (height >= island.beachHeight || hit.point.y <= OceanSurfaceSampler.Height(ocean, hit.point, Camera.main) + .04f) return false;
            var stamp = stamps[cursor];
            cursor = (cursor + 1) % stamps.Length;
            Vector3 facing = Vector3.ProjectOnPlane(forward, hit.normal);
            if (facing.sqrMagnitude < .0001f) facing = Vector3.ProjectOnPlane(player.transform.forward, hit.normal);
            if (facing.sqrMagnitude < .0001f) facing = Vector3.forward;
            stamp.transform.SetPositionAndRotation(hit.point + hit.normal * .008f, Quaternion.LookRotation(facing.normalized, hit.normal));
            stamp.transform.localScale = new Vector3(length * .42f / .32f, 1, length / .56f);
            stamp.transform.gameObject.SetActive(true);
            stamp.born = Time.time;
            float dry = Mathf.Clamp01(height / 2.4f);
            stamp.strength = Mathf.Lerp(1f, .72f, dry) * (1f - Mathf.SmoothStep(island.beachHeight - .6f, island.beachHeight, height));
            stamp.duration = lifetime * Mathf.Lerp(.65f, 1f, dry);
            var block = stampProperties ?? (stampProperties = new MaterialPropertyBlock());
            block.Clear(); block.SetFloat("_Mirror", isLeft ? -1f : 1f); block.SetFloat("_Seed", cursor * .137f);
            stamp.renderer.SetPropertyBlock(block);
            SetAlpha(stamp.renderer, stamp.strength);
            stamps[(cursor + PoolSize - 1) % PoolSize] = stamp;
            return true;
        }

        void Fade()
        {
            for (int i = 0; i < stamps.Length; i++)
            {
                if (!stamps[i].transform || !stamps[i].transform.gameObject.activeSelf) continue;
                float age = Time.time - stamps[i].born;
                if (age >= stamps[i].duration) { stamps[i].transform.gameObject.SetActive(false); continue; }
                float fade = age / stamps[i].duration;
                SetAlpha(stamps[i].renderer, stamps[i].strength * (1f - Mathf.SmoothStep(.55f, 1f, fade)));
            }
        }

        void SetAlpha(MeshRenderer renderer, float alpha)
        {
            if (!renderer) return;
            var block = stampProperties ?? (stampProperties = new MaterialPropertyBlock());
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(.40f, .38f, .34f, .68f * Mathf.Clamp01(alpha)));
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
