using UnityEngine;

namespace Mavis
{
    // One range check at the impact frame of each swipe. The old trigger was on
    // a child collider while OnTriggerEnter lived on the root, so hits were lost.
    public sealed class NailongAttack : MonoBehaviour
    {
        [Min(0f)] public float damage = 9f;
        [Range(20f, 180f)] public float attackArc = 130f;
        [Min(0f)] public float verticalTolerance = 2.5f;
        public string targetTag = "Player";

        public bool TryHit(Transform target, float reach, float damageMultiplier = 1f, float pushDistance = 0f)
        {
            if (target == null) return false;
            // Allow the authored Sahur scene instance, which is Untagged but
            // carries PlayerHealth, without changing the player's prefab.
            if (!string.IsNullOrEmpty(targetTag) && !target.CompareTag(targetTag) &&
                target.GetComponentInParent<PlayerHealth>() == null) return false;
            Vector3 delta = target.position - transform.position;
            if (Mathf.Abs(delta.y) > verticalTolerance) return false;
            delta.y = 0f;
            if (delta.sqrMagnitude > reach * reach) return false;
            if (delta.sqrMagnitude > 0.01f &&
                Vector3.Angle(transform.forward, delta) > attackArc * 0.5f)
                return false;

            IDamageable victim = target.GetComponentInParent<IDamageable>();
            if (victim == null) return false;
            victim.ApplyDamage(damage * damageMultiplier, transform.position + transform.forward * reach);
            if (pushDistance > 0f && delta.sqrMagnitude > 0.01f &&
                target.TryGetComponent(out CharacterController controller))
                controller.Move(delta.normalized * pushDistance);
            return true;
        }
    }
}
