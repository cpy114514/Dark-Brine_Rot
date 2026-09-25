// SahurAttack.cs
// Listens for the Input System "Attack" action and runs a melee swing:
//   1. SetTrigger("Attack") -> Animator plays the Attack clip (one-shot)
//   2. During the active swing window, enable the stick's trigger collider
//      so OnTriggerEnter fires when hitting something with a Damageable tag.
//   3. Optional damage event + cooldown to prevent spam-multi-hits per swing.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    public class SahurAttack : MonoBehaviour
    {
        [Header("Animation")]
        public Animator animator;
        public string attackTrigger = "Attack";

        [Header("Hit detection")]
        [Tooltip("Trigger collider child that covers the swing arc; auto-disabled outside attack window.")]
        public Collider stickHitbox;
        [Tooltip("Layers considered hittable. Default = everything.")]
        public LayerMask hitMask = ~0;
        [Tooltip("Tag the enemy must carry to receive damage.")]
        public string enemyTag = "Enemy";
        public float damage = 25f;
        public float swingWindowStart = 0.10f;
        public float swingWindowEnd = 0.55f;
        public float cooldown = 0.5f;

        bool attackQueued;
        float lastFireTime = -10f;
        int attackStateHash;
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            stickHitbox = transform.Find("Pbr Sahur Visual/Sahur Stick")?.GetComponent<Collider>();
        }

        void Awake()
        {
            attackStateHash = Animator.StringToHash("Base Layer." + attackTrigger);
            if (stickHitbox != null) stickHitbox.enabled = false;
        }

        void Update()
        {
            if (attackQueued && Time.time - lastFireTime >= cooldown)
            {
                attackQueued = false;
                DoAttack();
            }

            // Drive hitbox window via animator state info (works with any clip length)
            var info = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            bool inAttack = info.fullPathHash == attackStateHash;
            float normalized = info.normalizedTime;
            bool inWindow = inAttack &&
                            normalized >= swingWindowStart &&
                            normalized <= swingWindowEnd &&
                            !animator.IsInTransition(0);
            if (stickHitbox != null) stickHitbox.enabled = inWindow;
        }

        public void OnAttack(InputValue value)
        {
            if (value == null || !value.isPressed) return;
            TriggerAttack();
        }

        public void TriggerAttack()
        {
            attackQueued = true;
        }

        void DoAttack()
        {
            if (animator == null) return;
            if (!animator.HasState(0, attackStateHash))
            {
                Debug.LogError($"Attack state '{attackTrigger}' is missing from '{animator.runtimeAnimatorController?.name}'.", this);
                return;
            }

            animator.ResetTrigger(attackTrigger);
            hitThisSwing.Clear();
            // The visual Animator is handed off from the player root at runtime.
            // Use the full-path hash so the state survives the playable-graph hand-off,
            // while retaining a short blend instead of snapping into the first pose.
            animator.CrossFadeInFixedTime(attackStateHash, 0.05f, 0, 0f);
            lastFireTime = Time.time;
        }

        void OnTriggerEnter(Collider other)
        {
            if (stickHitbox == null || !stickHitbox.enabled) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;
            if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag)) return;
            // Body hitboxes also report trigger contacts through the player's
            // kinematic Rigidbody. Only the stick's actual overlap may deal damage.
            if (!Physics.ComputePenetration(stickHitbox, stickHitbox.transform.position,
                    stickHitbox.transform.rotation, other, other.transform.position,
                    other.transform.rotation, out _, out _)) return;
            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null && hitThisSwing.Add(dmg))
                dmg.ApplyDamage(damage, transform.position);
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint);
    }
}
