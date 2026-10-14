// EnemyAttack.cs
// Mirror of SahurAttack for the enemy side: drives a trigger hitbox
// during the Animator's Attack state window and applies damage to
// anything tagged "Player" carrying IDamageable.
using UnityEngine;
using System.Collections.Generic;

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

        readonly HashSet<IDamageable> victims=new HashSet<IDamageable>();
        bool windowActive;

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

            if(inWindow && !windowActive)victims.Clear();
            windowActive=inWindow;
            attackHitbox.enabled = inWindow;
        }

        void OnTriggerEnter(Collider other)=>TryHit(other);
        void OnTriggerStay(Collider other)=>TryHit(other);
        void OnDisable(){windowActive=false;victims.Clear();if(attackHitbox!=null)attackHitbox.enabled=false;}
        void TryHit(Collider other)
        {
            if (Time.timeScale<=0 || !isActiveAndEnabled || attackHitbox == null || !attackHitbox.enabled) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;
            var player=other.GetComponentInParent<PlayerHealth>();
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag) &&
                (player==null || !player.CompareTag(playerTag))) return;

            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null && victims.Add(dmg)) dmg.ApplyDamage(damage, transform.position);
        }
    }
}