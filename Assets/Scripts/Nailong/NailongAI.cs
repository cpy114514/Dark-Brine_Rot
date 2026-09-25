using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    [RequireComponent(typeof(NailongHealth), typeof(NailongAttack))]
    public sealed class NailongAI : MonoBehaviour
    {
        public enum State { Idle, Feign, Chase, Attack, Dead }

        [Header("Sahur")]
        public string targetTag = "Player";
        [Min(1f)] public float sightRange = 12f;
        [Min(1f)] public float attackRange = 2.8f;
        [Min(0.4f)] public float attackInterval = 0.88f;
        [Min(1f)] public float giveUpRange = 35f;

        [Header("Boss behaviour")]
        [Min(0.3f)] public float feignDuration = 1.1f;
        [Min(0.5f)] public float chaseSpeed = 4.2f;
        [Min(90f)] public float turnSpeed = 600f;

        [Header("Visual grounding")]
        [Range(0.15f, 0.40f)] public float visualGroundDrop = 0.275f;

        [Header("References (auto-filled if blank)")]
        public Animator animator;
        public NailongHealth health;
        public NailongAttack attack;

        public State CurrentState => state;

        State state;
        NavMeshAgent agent;
        NailongAttackMotion motion;
        ProceduralIsland island;
        Transform target;
        Transform visualArmature;
        float surfaceOffset;
        float feignEndsAt;
        float impactAt;
        float recoverAt;
        float nextSwipeAt;
        float swipeStartedAt;
        float swipeDuration;
        bool swiping;
        int impactsRemaining;
        NailongAttackMotion.Style currentStyle;
        bool feigned;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            motion = GetComponent<NailongAttackMotion>();
            visualArmature = transform.Find("Armature");
            if (health == null) health = GetComponent<NailongHealth>();
            if (attack == null) attack = GetComponent<NailongAttack>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            health.OnDeath.AddListener(HandleDeath);
            health.Damaged += HandleDamaged;
        }

        void Start()
        {
            island = FindFirstObjectByType<ProceduralIsland>();
            bool usingNavMesh = agent != null && agent.enabled && agent.isOnNavMesh;

            // This island has no baked NavMesh. Keep the agent for a future bake,
            // but use direct, surface-following movement until it is available.
            if (agent != null && agent.enabled && !usingNavMesh)
                agent.enabled = false;
            if (island != null)
            {
                // Derive the ground offset from the collider, not the scene's initial
                // placement: otherwise a misplaced prefab keeps floating while chasing.
                CapsuleCollider capsule = GetComponent<CapsuleCollider>();
                if (capsule != null && capsule.direction == 1)
                    surfaceOffset = (capsule.height * 0.5f - capsule.center.y) * Mathf.Abs(transform.lossyScale.y);
                else
                {
                    SkinnedMeshRenderer skin = GetComponentInChildren<SkinnedMeshRenderer>();
                    surfaceOffset = skin != null ? transform.position.y - skin.bounds.min.y : 0f;
                }

                if (!usingNavMesh)
                {
                    Vector3 grounded = transform.position;
                    grounded.y = island.GetWorldSurfaceHeight(grounded) + surfaceOffset;
                    transform.position = grounded;
                }
            }
            if (agent != null && agent.enabled)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = attackRange * 0.82f;
            }
            Play("Idle", 1f);
        }

        void LateUpdate()
        {
            // Imported clips animate Armature.localPosition back to zero every
            // frame. Reapply the calibrated visual drop after animation so the
            // feet stay on the ground in Play mode, not just in the prefab view.
            if (visualArmature == null) return;
            Vector3 local = visualArmature.localPosition;
            local.y = -visualGroundDrop;
            visualArmature.localPosition = local;
        }

        void OnDestroy()
        {
            if (health == null) return;
            health.OnDeath.RemoveListener(HandleDeath);
            health.Damaged -= HandleDamaged;
        }

        void Update()
        {
            if (state == State.Dead) return;
            if (target == null) AcquireTarget();
            if (target == null) { EnterIdle(); return; }

            float distance = FlatDistance(target.position, transform.position);
            switch (state)
            {
                case State.Idle:
                    if (distance <= sightRange)
                    {
                        if (feigned) EnterChase();
                        else EnterFeign();
                    }
                    break;

                case State.Feign:
                    FaceTarget();
                    if (Time.time >= feignEndsAt) EnterChase();
                    break;

                case State.Chase:
                    if (distance <= attackRange) EnterAttack();
                    else if (distance > giveUpRange) EnterIdle();
                    else MoveTowardsTarget();
                    break;

                case State.Attack:
                    FaceTarget();
                    if (distance > attackRange * 1.35f) { EnterChase(); break; }
                    if (swiping && impactsRemaining > 0 && Time.time >= impactAt)
                    {
                        float damageScale = currentStyle == NailongAttackMotion.Style.DoubleClaw ? 0.62f :
                            currentStyle == NailongAttackMotion.Style.ShoulderBump ? 1.2f : 1f;
                        float push = currentStyle == NailongAttackMotion.Style.ShoulderBump ? 0.6f : 0f;
                        attack.TryHit(target, attackRange, damageScale, push);
                        impactsRemaining--;
                        if (impactsRemaining > 0)
                            impactAt = swipeStartedAt + swipeDuration * 0.62f;
                    }
                    if (swiping && Time.time >= recoverAt)
                    {
                        swiping = false;
                        Play("Idle", 1f);
                    }
                    if (!swiping && Time.time >= nextSwipeAt) BeginSwipe();
                    break;
            }
        }

        void AcquireTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag(targetTag);
            if (player != null) target = player.transform;
            else
            {
                // The current Sahur prefab is intentionally Untagged; the
                // Gameplay scene adds PlayerHealth to its root instance.
                PlayerHealth sahur = FindFirstObjectByType<PlayerHealth>();
                if (sahur != null) target = sahur.transform;
            }
        }

        void EnterIdle()
        {
            if (state == State.Idle) return;
            state = State.Idle;
            if (motion != null) motion.Stop();
            StopMoving();
            Play("Idle", 1f);
        }

        void EnterFeign()
        {
            feigned = true;
            state = State.Feign;
            StopMoving();
            FaceTarget();
            feignEndsAt = Time.time + feignDuration;
            Play("Stumble", 0.85f);
        }

        void EnterChase()
        {
            if (state == State.Chase) return;
            state = State.Chase;
            swiping = false;
            if (motion != null) motion.Stop();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = false;
            Play("Walk", 1.2f);
        }

        void EnterAttack()
        {
            state = State.Attack;
            StopMoving();
            FaceTarget();
            swiping = false;
            nextSwipeAt = Time.time + 0.08f;
            Play("Idle", 1f);
        }

        void BeginSwipe()
        {
            // A mix of wild claws, two-beat tantrums and an impolite body check.
            float choice = Random.value;
            currentStyle = choice < 0.30f ? NailongAttackMotion.Style.LeftClaw :
                choice < 0.60f ? NailongAttackMotion.Style.RightClaw :
                choice < 0.84f ? NailongAttackMotion.Style.DoubleClaw :
                NailongAttackMotion.Style.ShoulderBump;
            float baseDuration = currentStyle == NailongAttackMotion.Style.DoubleClaw ? 1.16f :
                currentStyle == NailongAttackMotion.Style.ShoulderBump ? 0.92f : 0.78f;
            swipeDuration = baseDuration * Random.Range(0.92f, 1.10f);
            swipeStartedAt = Time.time;
            swiping = true;
            impactsRemaining = currentStyle == NailongAttackMotion.Style.DoubleClaw ? 2 : 1;
            impactAt = swipeStartedAt + swipeDuration *
                (currentStyle == NailongAttackMotion.Style.DoubleClaw ? 0.27f : 0.46f);
            recoverAt = swipeStartedAt + swipeDuration;
            nextSwipeAt = recoverAt + Mathf.Max(0.08f, attackInterval - 0.72f) + Random.Range(0f, 0.20f);
            if (motion != null) motion.Begin(currentStyle, swipeDuration);
        }

        void MoveTowardsTarget()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
                FaceTarget();
                return;
            }

            Vector3 goal = target.position;
            goal.y = transform.position.y;
            Vector3 next = Vector3.MoveTowards(transform.position, goal, chaseSpeed * Time.deltaTime);
            if (island != null)
                next.y = island.GetWorldSurfaceHeight(next) + surfaceOffset;
            transform.position = next;
            FaceTarget();
        }

        void StopMoving()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;
        }

        void FaceTarget()
        {
            if (target == null) return;
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion desired = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);
        }

        void Play(string stateName, float speed)
        {
            if (animator == null) return;
            animator.speed = speed;
            animator.CrossFadeInFixedTime("Base Layer." + stateName, 0.09f, 0, 0f);
        }

        void HandleDamaged(float amount)
        {
            if (state == State.Dead || state == State.Chase || state == State.Attack) return;
            AcquireTarget();
            if (target != null) EnterChase();
        }

        void HandleDeath()
        {
            state = State.Dead;
            if (motion != null) motion.Stop();
            StopMoving();
            if (agent != null && agent.enabled) agent.enabled = false;
            Play("Death", 1f);
            foreach (Collider collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
            Destroy(gameObject, 5f);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
