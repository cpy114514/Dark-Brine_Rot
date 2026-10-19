using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>Movement capsules block actors; animated anatomy remains available to weapon queries.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class CombatBodyCollision : MonoBehaviour
    {
        static readonly List<CombatBodyCollision> actors = new List<CombatBodyCollision>();
        readonly List<(Collider a, Collider b)> changedPairs = new List<(Collider, Collider)>();
        Collider[] anatomy;
        Collider motor;
        Collider[] solids = Array.Empty<Collider>();
        ThirdPersonPlayerController player;
        PlayerHealth playerHealth;
        Health health;
        static readonly Unity.Profiling.ProfilerMarker ContactMarker = new Unity.Profiling.ProfilerMarker("Combat.ResolveBodyContact");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetActors() => actors.Clear();

        public static void Ensure(GameObject actor)
        {
            var body = actor.GetComponent<CombatBodyCollision>();
            if (!body) actor.AddComponent<CombatBodyCollision>();
            else if (body.isActiveAndEnabled) body.Register();
        }

        public static bool IsAnatomy(Collider collider) => collider &&
            collider.name.StartsWith("BodyHitbox_", StringComparison.Ordinal);

        // Actors are not scenery. A moving arm must never become a camera wall or a floor.
        public static bool IsActor(Collider collider) => collider &&
            (IsAnatomy(collider) || collider.GetComponentInParent<CharacterController>() != null || collider.GetComponentInParent<NailongAI>() != null ||
             collider.GetComponentInParent<BombardinoBoss>() != null || collider.GetComponentInParent<Story1SharkHealth>() != null);

        void OnEnable() => Register();
        void Start() => Register();

        void Register()
        {
            player = GetComponent<ThirdPersonPlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
            health = GetComponent<Health>();
            motor = GetComponent<CharacterController>();
            if (!motor || !motor.enabled)
            {
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule && capsule.enabled) motor = capsule;
            }
            solids = motor && motor.enabled ? new[] { motor } : Array.FindAll(GetComponentsInChildren<Collider>(true),
                c => c && c.enabled && !c.isTrigger && !IsAnatomy(c));
            if (anatomy == null) anatomy = Array.FindAll(GetComponentsInChildren<Collider>(true), IsAnatomy);
            if (!actors.Contains(this)) actors.Add(this);
            foreach (var other in actors)
            {
                if (!other || !other.motor) continue;
                foreach (var part in anatomy) Ignore(part, other.motor);
                if (other == this) continue;
                foreach (var part in other.anatomy)
                {
                    Ignore(motor, part);
                    // Kinematic anatomical bodies need no contact solving against each other.
                    // IgnoreCollision does not remove them from overlap/raycast weapon queries.
                    foreach (var own in anatomy) Ignore(own, part);
                }
            }
        }

        void LateUpdate() => ResolveActorContacts();

        // A rotated or directly moved boss can enter a controller after that
        // controller's Move has already finished. Resolve only stable body
        // shapes, before the follow camera; animated arms remain hit queries.
        public void ResolveActorContacts()
        {
            var controller = motor as CharacterController;
            if (!player || !controller || !controller.enabled || !playerHealth || playerHealth.currentHealth <= 0 ||
                Time.timeScale <= 0 || player.ExternalControlLock || player.ExternalMovementLock) return;
            using var sample = ContactMarker.Auto();
            const float clearance = .006f;
            bool synchronized = false;
            for (int pass = 0; pass < 4; pass++)
            {
                bool moved = false;
                Bounds playerBounds = WorldBounds(controller);
                foreach (var other in actors)
                {
                    if (!other || other == this || !other.isActiveAndEnabled || other.player || (other.health && other.health.IsDead)) continue;
                    foreach (var body in other.solids)
                    {
                        if (!body || !body.enabled || !body.gameObject.activeInHierarchy || body.isTrigger ||
                            Physics.GetIgnoreLayerCollision(gameObject.layer, body.gameObject.layer) || Physics.GetIgnoreCollision(controller, body)) continue;
                        if (!playerBounds.Intersects(WorldBounds(body))) continue;
                        Vector3 direction; float depth;
                        bool overlapping = body is CharacterController otherController
                            ? CapsuleContact(controller, otherController, out direction, out depth)
                            : Physics.ComputePenetration(controller, transform.position, transform.rotation,
                                body, body.transform.position, body.transform.rotation, out direction, out depth);
                        if (!overlapping || depth <= clearance) continue;
                        // Floors, landing on a body and overhead contact stay with
                        // the controller's vertical solver. Never lift Sahur here.
                        var planar = Vector3.ProjectOnPlane(direction, Vector3.up);
                        float length = planar.magnitude;
                        if (length < .5f) continue;
                        Vector3 before = transform.position;
                        Vector3 correction = planar / length * (depth / length + clearance);
                        // Transform-driven aircraft boxes can still have their
                        // previous fixed-step pose in PhysX. Sync only on a real
                        // contact, so the controller does not hit a stale shape.
                        if (!synchronized && (!(body is CharacterController) ||
                            (body.bounds.center - WorldBounds(body).center).sqrMagnitude > .0001f))
                        { Physics.SyncTransforms(); synchronized = true; }
                        controller.Move(correction);
                        playerBounds = WorldBounds(controller);
                        // If scenery pins the player, yield the moving boss's
                        // capsule instead of forcing Sahur through a wall.
                        Vector3 unresolved = correction - Vector3.ProjectOnPlane(transform.position - before, Vector3.up);
                        if (unresolved.sqrMagnitude > .0001f && body is CharacterController bossMotor && bossMotor.enabled)
                            bossMotor.Move(-unresolved);
                        moved |= (transform.position - before).sqrMagnitude > .000001f;
                    }
                }
                if (!moved) break;
            }
        }

        // CharacterController pairs are not a supported ComputePenetration
        // shape pair. Their upright capsule segments have an exact cheap solve.
        static bool CapsuleContact(CharacterController a, CharacterController b, out Vector3 direction, out float depth)
        {
            Vector3 sa = Abs(a.transform.lossyScale), sb = Abs(b.transform.lossyScale);
            float ra = a.radius * Mathf.Max(sa.x, sa.z), rb = b.radius * Mathf.Max(sb.x, sb.z);
            float halfA = Mathf.Max(0, a.height * sa.y * .5f - ra), halfB = Mathf.Max(0, b.height * sb.y * .5f - rb);
            Vector3 delta = a.transform.TransformPoint(a.center) - b.transform.TransformPoint(b.center);
            delta.y = Mathf.Sign(delta.y) * Mathf.Max(0, Mathf.Abs(delta.y) - halfA - halfB);
            float distance = delta.magnitude;
            depth = ra + rb - distance;
            direction = distance > .0001f ? delta / distance : -b.transform.forward;
            return depth > 0;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        static Bounds WorldBounds(Collider body)
        {
            var at = body.transform;
            if (body is CharacterController capsule)
            {
                Vector3 scale = Abs(at.lossyScale);
                float radius = capsule.radius * Mathf.Max(scale.x, scale.z);
                float half = Mathf.Max(0, capsule.height * scale.y * .5f - radius);
                return new Bounds(at.TransformPoint(capsule.center), (Abs(at.up) * half + Vector3.one * radius) * 2);
            }
            if (body is BoxCollider box)
            {
                Vector3 half = box.size * .5f;
                Vector3 extent = Abs(at.TransformVector(Vector3.right * half.x)) +
                    Abs(at.TransformVector(Vector3.up * half.y)) + Abs(at.TransformVector(Vector3.forward * half.z));
                return new Bounds(at.TransformPoint(box.center), extent * 2);
            }
            return body.bounds;
        }

        void Ignore(Collider a, Collider b)
        {
            if (!a || !b || a == b || Physics.GetIgnoreCollision(a,b)) return;
            Physics.IgnoreCollision(a,b,true);
            changedPairs.Add((a,b));
        }

        void ReleaseContacts(Transform leaving)
        {
            for (int i=changedPairs.Count-1;i>=0;i--)
            {
                var pair=changedPairs[i];
                if (pair.a && pair.b && !pair.a.transform.IsChildOf(leaving) && !pair.b.transform.IsChildOf(leaving)) continue;
                if (pair.a && pair.b) Physics.IgnoreCollision(pair.a,pair.b,false);
                changedPairs.RemoveAt(i);
            }
        }

        void OnDisable()
        {
            foreach(var actor in actors) if(actor) actor.ReleaseContacts(transform);
            actors.Remove(this);
        }
    }
}
