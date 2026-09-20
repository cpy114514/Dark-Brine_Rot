// EnemyAttack.cs
// Mirror of SahurAttack for the enemy side: drives a trigger hitbox
// during the Animator's Attack state window and applies damage to
// anything tagged "Player" carrying IDamageable.
using UnityEngine;

namespace Mavis
{
    public class EnemyAttack : MonoBehaviour
    {
        [Header("Hitbox")]
        [Tooltip("Trigger collider covering the attack arc; auto-disabled outside the attack window.")]
        public Collider attackHitbox;
        public LayerMask hitMask = ~0;

        [Header("Damage")]
        public float damage = 10f;
        public string playerTag = "Player";

        [Header("Animator Window")]
        public Animator animator;
        public string attackStateName = "Attack";
        [Range(0f, 1f)] public float windowStart = 0.10f;
        [Range(0f, 1f)] public float windowEnd = 0.55f;

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        void Awake()
        {
            if (attackHitbox != null) attackHitbox.enabled = false;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null || attackHitbox == null) return;

            var st = animator.GetCurrentAnimatorStateInfo(0);
            bool inAttack = st.IsName(attackStateName);
            float t = st.normalizedTime;
            bool inWindow = inAttack &&
                            t >= windowStart &&
                            t <= windowEnd &&
                            !animator.IsInTransition(0);

            attackHitbox.enabled = inWindow;
        }

        void OnTriggerEnter(Collider other)
        {
            if (attackHitbox == null || !attackHitbox.enabled) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null) dmg.ApplyDamage(damage, transform.position);
        }
    }
}