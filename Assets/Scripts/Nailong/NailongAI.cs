using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    // Temporary animation handoff: no attacks or procedural bone posing.
    // The boss only notices Sahur, follows, and stops at a comfortable distance.
    [RequireComponent(typeof(NailongHealth))]
    public sealed class NailongAI : MonoBehaviour
    {
        public enum State { Idle, Chase, Dead }

        [Header("Target")]
        public string targetTag = "Player";
        [Min(1f)] public float sightRange = 12f;
        [Min(1f)] public float giveUpRange = 35f;
        [Min(0.5f)] public float followStopDistance = 2.4f;

        [Header("Movement")]
        [Min(0.5f)] public float chaseSpeed = 4.2f;
        [Min(90f)] public float turnSpeed = 600f;

        [Header("Visual grounding")]
        [Range(0f, 0.5f)] public float visualGroundDrop = 0.325f;
        public bool keepVisualGroundOffset = true;

        [Header("References")]
        public Animator animator;
        public NailongHealth health;

        public State CurrentState => state;

        State state;
        NavMeshAgent agent;
        ProceduralIsland island;
        Transform target;
        Transform visualArmature;
        float surfaceOffset;
        bool engaged;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            visualArmature = transform.Find("Armature");
            if (health == null) health = GetComponent<NailongHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            health.OnDeath.AddListener(HandleDeath);
            health.Damaged += HandleDamaged;
        }

        void Start()
        {
            island = FindFirstObjectByType<ProceduralIsland>();
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null && capsule.direction == 1)
                surfaceOffset = (capsule.height * 0.5f - capsule.center.y) * Mathf.Abs(transform.lossyScale.y);

            // The first island has no baked NavMesh. Direct movement remains
            // available without leaving an invalid agent active.
            if (agent != null && agent.enabled && !agent.isOnNavMesh)
                agent.enabled = false;
            if (agent != null && agent.enabled)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = followStopDistance;
                agent.isStopped = true;
            }
            SnapToSurface();
            Play("Idle");
        }

        void Update()
        {
            if (state == State.Dead) return;
            if (target == null) AcquireTarget();
            if (target == null)
            {
                engaged = false;
                EnterIdle();
                return;
            }

            float distance = FlatDistance(target.position, transform.position);
            if (distance > giveUpRange)
            {
                engaged = false;
                EnterIdle();
                return;
            }
            if (distance <= sightRange) engaged = true;
            if (!engaged) return;

            FaceTarget();
            // A small hysteresis stops the walk animation flickering at the edge.
            float resumeDistance = followStopDistance + 0.4f;
            if (distance <= followStopDistance || (state == State.Idle && distance <= resumeDistance))
                EnterIdle();
            else
            {
                EnterChase();
                MoveTowardsTarget();
            }
        }

        void LateUpdate()
        {
            // Existing placeholder clips reset Armature.localPosition each frame.
            // This can be switched off once an authored clip owns the root pose.
            if (!keepVisualGroundOffset || visualArmature == null) return;
            Vector3 position = visualArmature.localPosition;
            position.y = -visualGroundDrop;
            visualArmature.localPosition = position;
        }

        void OnDestroy()
        {
            if (health == null) return;
            health.OnDeath.RemoveListener(HandleDeath);
            health.Damaged -= HandleDamaged;
        }

        void AcquireTarget()
        {
            var sahur = FindFirstObjectByType<PlayerHealth>();
            if (sahur != null) { target = sahur.transform; return; }
            if (string.IsNullOrEmpty(targetTag)) return;
            var tagged = GameObject.FindGameObjectWithTag(targetTag);
            if (tagged != null) target = tagged.transform;
        }

        void EnterIdle()
        {
            if (state == State.Idle) return;
            state = State.Idle;
            if (agent != null && agent.enabled) agent.isStopped = true;
            Play("Idle");
        }

        void EnterChase()
        {
            if (state == State.Chase) return;
            state = State.Chase;
            if (agent != null && agent.enabled) agent.isStopped = false;
            Play("Walk");
        }

        void MoveTowardsTarget()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
                return;
            }

            Vector3 goal = target.position;
            goal.y = transform.position.y;
            Vector3 next = Vector3.MoveTowards(transform.position, goal, chaseSpeed * Time.deltaTime);
            if (island != null) next.y = island.GetWorldSurfaceHeight(next) + surfaceOffset;
            transform.position = next;
        }

        void FaceTarget()
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        void SnapToSurface()
        {
            if (island == null || (agent != null && agent.enabled && agent.isOnNavMesh)) return;
            Vector3 position = transform.position;
            position.y = island.GetWorldSurfaceHeight(position) + surfaceOffset;
            transform.position = position;
        }

        void Play(string stateName)
        {
            if (animator == null) return;
            int hash = Animator.StringToHash("Base Layer." + stateName);
            if (animator.HasState(0, hash))
                animator.CrossFadeInFixedTime(hash, 0.12f, 0, 0f);
        }

        void HandleDamaged(float amount)
        {
            if (state == State.Dead) return;
            AcquireTarget();
            engaged = target != null;
        }

        void HandleDeath()
        {
            state = State.Dead;
            if (agent != null && agent.enabled) agent.enabled = false;
            Play("Death");
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
