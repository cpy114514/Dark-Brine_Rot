// EnemyAI.cs
// Simple FSM: Idle / Chasing / Attacking / Dead.
// Uses NavMeshAgent for pathing; flips to Attack state when in range.
// Triggers the "Attack" animator trigger; EnemyAttack handles hitbox window.
//
// Designed to play nice with Health (subscribes to OnDeath).
using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State { Idle, Chasing, Attacking, Dead }

        [Header("Target")]
        public Transform target;
        public string targetTag = "Player";
        [Tooltip("Re-acquire target each time the previous one dies / despawns.")]
        public bool retargetOnLost = true;

        [Header("Ranges")]
        public float sightRange = 15f;
        public float attackRange = 2f;
        [Tooltip("If true, line of sight is a simple distance check (no occlusion raycast).")]
        public bool ignoreObstacles = true;

        [Header("Combat")]
        public float attackInterval = 1.25f;
        public float turnSpeed = 8f;

        [Header("Animator (optional)")]
        public Animator animator;
        public string speedParam = "Speed";
        public string attackTrigger = "Attack";

        [Header("Cleanup")]
        [Tooltip("Destroy this GameObject this many seconds after death.")]
        public float destroyAfterDeath = 3f;

        NavMeshAgent agent;
        Health health;
        State state;
        float nextAttackTime;

        public State CurrentState => state;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (target == null) AcquireTarget();
            health.OnDeath.AddListener(HandleDeath);
        }

        void OnDestroy()
        {
            if (health != null) health.OnDeath.RemoveListener(HandleDeath);
        }

        void AcquireTarget()
        {
            if (string.IsNullOrEmpty(targetTag)) return;
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null) target = go.transform;
        }

        void Update()
        {
            if (state == State.Dead) return;

            if (target == null && retargetOnLost) AcquireTarget();

            if (target == null)
            {
                EnterIdle();
                return;
            }

            float dist = Vector3.Distance(transform.position, target.position);
            bool canSee = dist <= sightRange && (ignoreObstacles || HasLineOfSight(target));

            if (!canSee)
            {
                EnterIdle();
            }
            else if (dist <= attackRange)
            {
                EnterAttacking();
            }
            else
            {
                EnterChasing();
            }

            switch (state)
            {
                case State.Chasing:
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(target.position);
                    }
                    break;

                case State.Attacking:
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    FaceTarget();
                    if (Time.time >= nextAttackTime)
                    {
                        if (animator != null) animator.SetTrigger(attackTrigger);
                        nextAttackTime = Time.time + attackInterval;
                    }
                    break;

                case State.Idle:
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    break;
            }

            if (animator != null && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, agent.velocity.magnitude);
        }

        void EnterIdle()
        {
            if (state != State.Idle) state = State.Idle;
        }

        void EnterChasing()
        {
            if (state != State.Chasing) state = State.Chasing;
        }

        void EnterAttacking()
        {
            if (state != State.Attacking) state = State.Attacking;
        }

        void FaceTarget()
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        bool HasLineOfSight(Transform t)
        {
            Vector3 eye = transform.position + Vector3.up * 1.2f;
            Vector3 dst = t.position + Vector3.up * 1.0f;
            Vector3 v = dst - eye;
            if (Physics.Raycast(eye, v.normalized, out var hit, v.magnitude, ~0, QueryTriggerInteraction.Ignore))
                return hit.transform == t || hit.transform.IsChildOf(t);
            return true;
        }

        void HandleDeath()
        {
            state = State.Dead;
            if (agent != null) agent.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            if (animator != null) animator.SetTrigger("Die");
            if (destroyAfterDeath > 0f) Destroy(gameObject, destroyAfterDeath);
        }

        // Draw sight/attack ranges in editor for tuning
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sightRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}