using UnityEngine;
using System.Collections.Generic;

namespace Mavis
{
    // One range check at the impact frame of each swipe. The old trigger was on
    // a child collider while OnTriggerEnter lived on the root, so hits were lost.
    public sealed class NailongAttack : MonoBehaviour
    {
        [Min(0f)] public float damage = 20f;
        [Range(20f, 180f)] public float attackArc = 130f;
        [Min(0f)] public float verticalTolerance = 2.5f;
        public string targetTag = "Player";
        readonly Collider[] roarHits = new Collider[64];
        readonly HashSet<Transform> roarVictims = new HashSet<Transform>();
        readonly RaycastHit[] sightHits = new RaycastHit[32];
        NailongAttackMotion motion;

        void Awake() => motion = GetComponent<NailongAttackMotion>();

        public bool HasLineOfSight(Transform target)
        {
            if (!target) return false;
            var capsule = target.GetComponentInParent<CharacterController>();
            Vector3 from = motion ? motion.MouthPosition : transform.TransformPoint(Vector3.up * 1.8f);
            Vector3 to = capsule ? capsule.bounds.center : target.position + Vector3.up;
            Vector3 ray = to-from;
            int count=Physics.RaycastNonAlloc(from,ray.normalized,sightHits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=sightHits[i].transform;
                if(hit.IsChildOf(transform)||hit.IsChildOf(target)||target.IsChildOf(hit))continue;
                return false;
            }
            return true;
        }

        public int Roar(float radius, float multiplier, float pushDistance, Transform mainTarget)
        {
            roarVictims.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, roarHits, ~0, QueryTriggerInteraction.Collide);
            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                var victim = roarHits[i].GetComponentInParent<PlayerHealth>();
                if (victim == null || victim.transform.IsChildOf(transform) || !roarVictims.Add(victim.transform)) continue;
                if (TryHit(victim.transform, radius, multiplier, pushDistance, true)) hitCount++;
            }
            if (mainTarget != null)
            {
                var victim = mainTarget.GetComponentInParent<PlayerHealth>();
                Transform root = victim != null ? victim.transform : mainTarget;
                if (roarVictims.Add(root) && TryHit(root, radius, multiplier, pushDistance, true)) hitCount++;
            }
            return hitCount;
        }

        public bool TryHit(Transform target, float reach, float damageMultiplier = 1f,
            float pushDistance = 0f, bool ignoreFacing = false, Vector3? rangeOrigin = null)
        {
            if (target == null) return false;
            var playerHealth = target.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && (playerHealth.currentHealth <= 0f || playerHealth.IsProtected)) return false;
            // Allow the authored Sahur scene instance, which is Untagged but
            // carries PlayerHealth, without changing the player's prefab.
            if (!string.IsNullOrEmpty(targetTag) && target.tag != targetTag &&
                target.GetComponentInParent<PlayerHealth>() == null) return false;
            Vector3 origin = rangeOrigin ?? NailongSize.Feet(transform);
            Vector3 delta = NailongSize.Feet(target) - origin;
            if (Mathf.Abs(delta.y) > verticalTolerance) return false;
            delta.y = 0f;
            if (delta.sqrMagnitude > reach * reach) return false;
            if (!ignoreFacing && delta.sqrMagnitude > 0.01f &&
                Vector3.Angle(transform.forward, delta) > attackArc * 0.5f)
                return false;

            if (!HasLineOfSight(target)) return false;

            IDamageable victim = target.GetComponentInParent<IDamageable>();
            if (victim == null) return false;
            victim.ApplyDamage(damage * damageMultiplier, transform.position + transform.forward * reach);
            if (playerHealth != null && playerHealth.currentHealth <= 0f) return true;
            if (pushDistance > 0f)
            {
                CharacterController controller = target.GetComponentInParent<CharacterController>();
                if (controller != null && controller.enabled)
                {
                    Vector3 pushDirection = delta.sqrMagnitude > 0.01f ? delta.normalized : transform.forward;
                    var knockback = controller.GetComponent<CombatKnockback>();
                    if (knockback == null) knockback = controller.gameObject.AddComponent<CombatKnockback>();
                    knockback.Push(pushDirection * pushDistance);
                }
            }
            return true;
        }
    }
}
