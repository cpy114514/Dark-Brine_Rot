using UnityEngine;

namespace Mavis
{
    /// <summary>Terrain-following combat for Capri, including islands without a baked NavMesh.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(CappuccinoEnemy), typeof(CharacterController))]
    public sealed class CappuccinoAI : MonoBehaviour
    {
        public enum State { Idle, Chase, Attack, Recover, Retreat, Dead }
        [Header("Target / aggro")]
        public Transform target;
        [Min(1f)] public float sightRange = 32f;
        [Min(1f)] public float giveUpRange = 55f;
        [Header("Movement")]
        [Min(0f)] public float chaseSpeed = 4.5f;
        [Min(0f)] public float retreatSpeed = 5.5f;
        [Min(1f)] public float turnSpeed = 480f;
        [Min(0.1f)] public float maxGroundDrop = 0.8f;
        public LayerMask collisionMask = ~0;
        [Header("Sword attack")]
        [Min(0.1f)] public float attackRange = 5.6f;
        [Min(0.1f)] public float attackReach = 5.9f;
        [Min(0f)] public float damage = 18f;
        [Range(10f, 180f)] public float attackArc = 130f;
        [Range(0f, 1f)] public float aimLockTime = 0.16f;
        [Range(0f, 1f)] public float hitWindowStart = 0.28f;
        [Range(0f, 1f)] public float hitWindowEnd = 0.62f;
        [Min(0f)] public float recoverySeconds = 0.55f;
        [Header("Retreat then re-engage")]
        [Min(0f)] public float retreatSeconds = 1.2f;
        [Min(0f)] public float retreatDistance = 9f;
        [Min(0f)] public float retreatCooldown = 3.5f;
        [Range(0f, 1f)] public float lowHealthRatio = 0.3f;
        [Min(0f)] public float crowdedDistance = 2.5f;
        public State CurrentState { get; private set; }

        Health health;
        CappuccinoEnemy presentation;
        CharacterController motor;
        Animator animator;
        CappuccinoUltimate ultimate;
        OceanWorld ocean;
        PlayerHealth targetHealth;
        bool engaged, hitResolved, pendingRetreat;
        float acquireAt, actionAt, retreatAt, attackStartedAt, previousAttackTime;
        float verticalSpeed, locomotion, avoidanceSide = 1f;
        readonly RaycastHit[] castHits = new RaycastHit[48];
        readonly Collider[] overlapHits = new Collider[48];
        static readonly float[] AvoidanceAngles = { 0f, 35f, 70f, -35f, -70f, 105f };
        static readonly int MoveParameter = Animator.StringToHash("Move");
        static readonly int AttackTrigger = Animator.StringToHash("Attack");

        void Awake()
        {
            health = GetComponent<Health>();
            presentation = GetComponent<CappuccinoEnemy>();
            motor = GetComponent<CharacterController>();
            animator = presentation.animator;
            ultimate = GetComponent<CappuccinoUltimate>();
            ocean = FindFirstObjectByType<OceanWorld>();
            health.OnDamaged ??= new UnityEngine.Events.UnityEvent<float>();
            health.OnDeath ??= new UnityEngine.Events.UnityEvent();
            // The AI decides when to counterattack; a hit must not bypass range or interrupt its own swing.
            presentation.attackOnHit = false;
            health.OnDamaged.AddListener(OnDamaged);
            health.OnDeath.AddListener(OnDeath);
            if (target != null) BindTarget(target);
            if (health.IsDead) OnDeath();
        }

        void Start()
        {
            if (FindGround(transform.position, 12f, 30f, out var ground))
            {
                motor.enabled = false;
                transform.position = new Vector3(transform.position.x, ground.point.y + 0.06f - SoleOffset, transform.position.z);
                motor.enabled = true;
                SetGroundNormal(ground.normal);
            }
        }

        void Update() => Tick(Time.deltaTime, Time.time);

        void Tick(float deltaTime, float now)
        {
            if (deltaTime <= 0f || PauseSettingsMenu.IsOpen || health == null) return;
            if (health.IsDead) { OnDeath(); return; }
            if (ultimate != null && ultimate.isActiveAndEnabled) ultimate.Simulate(now);
            ApplyGravity(deltaTime);
            if ((target == null || !target.gameObject.activeInHierarchy) && now >= acquireAt)
            {
                acquireAt = now + 0.5f;
                AcquireTarget();
            }
            if (target == null || (targetHealth != null && targetHealth.currentHealth <= 0f))
            { CancelCombat(); SetMovement(0f, deltaTime); return; }
            if (targetHealth == null || targetHealth.transform != target) BindTarget(target);
            Vector3 toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
            float distance = toTarget.magnitude;
            if (distance > giveUpRange) { CancelCombat(); SetMovement(0f, deltaTime); return; }
            if (distance <= sightRange && HasSight()) engaged = true;
            if (!engaged) { SetMovement(0f, deltaTime); return; }
            if (ultimate != null && ultimate.isActiveAndEnabled) ultimate.TryActivate(now);
            bool movementLocked = ultimate != null && ultimate.MovementLocked;

            if (CurrentState == State.Attack)
            {
                SetMovement(0f, deltaTime);
                // Track Sahur during the windup; commit the direction before the damaging slash.
                if (previousAttackTime < aimLockTime) Face(toTarget, deltaTime);
                TickAttack(now);
                return;
            }
            Face(toTarget, deltaTime);
            if (movementLocked && CurrentState == State.Retreat) CurrentState = State.Idle;
            if (CurrentState == State.Recover && now < actionAt)
            { SetMovement(0f, deltaTime); return; }
            if (CurrentState == State.Retreat)
            {
                if (now < actionAt && distance < retreatDistance)
                { Move(-toTarget.normalized, retreatSpeed, deltaTime, -1f); return; }
                CurrentState = State.Chase;
            }
            if (!movementLocked && now >= retreatAt && (pendingRetreat || distance < crowdedDistance ||
                (health.Ratio <= lowHealthRatio && distance < attackRange + 1f)))
            {
                pendingRetreat = false;
                CurrentState = State.Retreat;
                actionAt = now + retreatSeconds;
                retreatAt = now + retreatCooldown;
                Move(-toTarget.normalized, retreatSpeed, deltaTime, -1f);
                return;
            }
            bool visible = HasSight();
            if (distance <= attackRange && visible &&
                Vector3.Angle(transform.forward, toTarget) <= 18f)
            {
                SetMovement(0f, deltaTime);
                if (now >= actionAt && presentation.TryPlayAttack(now))
                {
                    CurrentState = State.Attack;
                    attackStartedAt = now;
                    previousAttackTime = -1f;
                    hitResolved = false;
                }
                return;
            }
            if (movementLocked)
            { CurrentState = State.Idle; SetMovement(0f, deltaTime); return; }
            CurrentState = State.Chase;
            // Approach only as far as the sword range. Do not run into Sahur's capsule.
            float speed = visible ? Mathf.Min(chaseSpeed, Mathf.Max(0f, distance - attackRange + 0.15f) / deltaTime) : chaseSpeed;
            Move(toTarget.normalized, speed, deltaTime, 1f);
        }

        void TickAttack(float now)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Attack"))
            {
                float t = state.normalizedTime;
                // Crossing the window also resolves a swing during a long frame, once per attack.
                if (!hitResolved && t >= hitWindowStart && previousAttackTime <= hitWindowEnd)
                {
                    hitResolved = true;
                    TryHitTarget();
                }
                previousAttackTime = t;
                if (t < 1f) return;
            }
            // Allow the entry transition to reach Attack before considering it finished.
            if (now - attackStartedAt < 0.25f) return;
            CurrentState = State.Recover;
            actionAt = now + recoverySeconds;
        }

        void TryHitTarget()
        {
            if (target == null || targetHealth == null || targetHealth.currentHealth <= 0f || targetHealth.IsProtected) return;
            Vector3 origin = Chest;
            int count = Physics.OverlapSphereNonAlloc(origin, attackReach, overlapHits, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = overlapHits[i];
                if (collider.GetComponentInParent<PlayerHealth>() != targetHealth) continue;
                Vector3 point = collider.ClosestPoint(origin);
                Vector3 direction = Vector3.ProjectOnPlane(collider.bounds.center - origin, Vector3.up);
                if (Vector3.Angle(transform.forward, direction) > attackArc * 0.5f || IsBlocked(origin, point)) continue;
                targetHealth.ApplyDamage(damage, point);
                // Collider count does not multiply damage. Both swords belong to this one attack.
                target.GetComponent<CombatKnockback>()?.Push(direction.normalized * 0.65f);
                if (Camera.main != null) Camera.main.GetComponent<CombatCameraShake>()?.Pulse(0.025f, 0.12f);
                return;
            }
        }

        void AcquireTarget()
        {
            PlayerHealth nearest = null;
            float best = float.PositiveInfinity;
            foreach (var player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (player.currentHealth <= 0f) continue;
                float distance = (player.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = player; }
            }
            if (nearest != null) BindTarget(nearest.transform);
            else { target = null; targetHealth = null; }
        }

        void BindTarget(Transform value)
        {
            targetHealth = value.GetComponentInParent<PlayerHealth>();
            target = targetHealth != null ? targetHealth.transform : value;
        }

        bool HasSight()
        {
            if (target == null) return false;
            var controller = target.GetComponent<CharacterController>();
            Vector3 point = controller != null ? controller.bounds.center : target.position + Vector3.up * 2f;
            return !IsBlocked(Chest, point);
        }

        bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            if (direction.sqrMagnitude < 0.001f) return false;
            int count = Physics.RaycastNonAlloc(from, direction.normalized, castHits, direction.magnitude, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = castHits[i].transform;
                if (hit.IsChildOf(transform) || (target != null && hit.IsChildOf(target))) continue;
                return true;
            }
            return false;
        }

        Vector3 Chest => transform.TransformPoint(motor.center);
        float SoleOffset => (motor.center.y - motor.height * 0.5f) * Mathf.Abs(transform.lossyScale.y);

        void Move(Vector3 direction, float speed, float dt, float animationDirection)
        {
            if (!motor.enabled || speed <= 0f) { SetMovement(0f, dt); return; }
            Vector3 chosen = Vector3.zero;
            // Retain a side preference so Capri does not alternate left/right against the same obstacle.
            foreach (float baseAngle in AvoidanceAngles)
            {
                float angle = baseAngle * avoidanceSide;
                Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                Vector3 next = transform.position + candidate * Mathf.Max(speed * dt, motor.radius * 0.8f);
                if (!FindGround(next, 3f, 7f, out var ground)) continue;
                float soleY = transform.position.y + SoleOffset;
                if (ground.point.y < soleY - maxGroundDrop || ground.point.y > soleY + motor.stepOffset + 1.2f) continue;
                if (ocean != null && ground.point.y < ocean.oceanHeight + 0.12f) continue;
                if (HasObstacle(candidate, Mathf.Max(speed * dt + 0.12f, 0.75f))) continue;
                chosen = candidate;
                if (baseAngle < 0f) avoidanceSide = -avoidanceSide;
                SetGroundNormal(ground.normal);
                break;
            }
            Vector3 before = transform.position;
            if (chosen.sqrMagnitude > 0.01f) motor.Move(chosen * speed * dt);
            float actualSpeed = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude / Mathf.Max(0.001f, dt);
            SetMovement(animationDirection * Mathf.Clamp01(actualSpeed / Mathf.Max(0.1f, speed)), dt);
        }

        bool HasObstacle(Vector3 direction, float distance)
        {
            Vector3 center = transform.TransformPoint(motor.center);
            float scale = Mathf.Abs(transform.lossyScale.y);
            float radius = motor.radius * scale * 0.92f;
            float half = motor.height * scale * 0.5f - radius;
            // Raise the bottom of the probe above climbable steps; CharacterController resolves actual contact.
            int count = Physics.CapsuleCastNonAlloc(center - Vector3.up * Mathf.Max(0f, half - motor.stepOffset - 0.12f),
                center + Vector3.up * half, radius, direction, castHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (castHits[i].transform.IsChildOf(transform)) continue;
                if (Vector3.Angle(castHits[i].normal, Vector3.up) <= motor.slopeLimit) continue;
                return true;
            }
            return false;
        }

        bool FindGround(Vector3 at, float up, float length, out RaycastHit ground)
        {
            ground = default;
            float nearest = float.PositiveInfinity;
            int count = Physics.RaycastNonAlloc(at + Vector3.up * up, Vector3.down, castHits, length, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = castHits[i];
                if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<IDamageable>() != null) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > motor.slopeLimit || hit.distance >= nearest) continue;
                nearest = hit.distance;
                ground = hit;
            }
            return nearest < float.PositiveInfinity;
        }

        void ApplyGravity(float dt)
        {
            if (!motor.enabled) return;
            verticalSpeed = motor.isGrounded ? -2f : Mathf.Max(-30f, verticalSpeed - 25f * dt);
            motor.Move(Vector3.up * verticalSpeed * dt);
            if (FindGround(transform.position, 1f, 3f, out var ground)) SetGroundNormal(ground.normal);
        }

        void SetGroundNormal(Vector3 normal)
        { if (presentation.motionRig != null) presentation.motionRig.groundNormal = normal; }

        void Face(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * dt);
        }

        void SetMovement(float value, float dt)
        {
            locomotion = Mathf.MoveTowards(locomotion, value, dt * 8f);
            if (animator != null) animator.SetFloat(MoveParameter, locomotion);
        }

        void OnDamaged(float amount)
        {
            if (health.IsDead) return;
            if (target == null) AcquireTarget();
            engaged = target != null;
            pendingRetreat = true;
        }

        void OnDeath()
        {
            CurrentState = State.Dead;
            hitResolved = true;
            if (motor != null) motor.enabled = false;
            if (animator != null) animator.SetFloat(MoveParameter, 0f);
        }

        void CancelCombat()
        {
            engaged = false;
            pendingRetreat = false;
            hitResolved = true;
            if (CurrentState == State.Attack && animator != null)
            { animator.ResetTrigger(AttackTrigger); animator.CrossFadeInFixedTime("Idle", 0.12f); }
            CurrentState = State.Idle;
        }

        void OnDisable() { if (animator != null) animator.SetFloat(MoveParameter, 0f); }
        void OnDestroy()
        {
            if (health == null) return;
            health.OnDamaged?.RemoveListener(OnDamaged);
            health.OnDeath?.RemoveListener(OnDeath);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, sightRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.7f, attackRange);
        }
    }
}
