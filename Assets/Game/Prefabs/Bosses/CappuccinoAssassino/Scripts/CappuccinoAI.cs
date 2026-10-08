using UnityEngine;

namespace Mavis
{
    /// <summary>Terrain-following combat for Capri, including islands without a baked NavMesh.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(CappuccinoEnemy), typeof(CharacterController))]
    public sealed class CappuccinoAI : MonoBehaviour
    {
        public enum State { Idle, Chase, Attack, Recover, Retreat, Dead, Windup, Orbit, Staggered, Returning }
        public enum AttackKind { TwinSlash, DashThrust, Whirlwind }
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
        [Header("Boss encounter")]
        [Min(10f)] public float arenaRadius = 42f;
        [Min(.2f)] public float openingSeconds = 1.1f;
        [Min(.3f)] public float telegraphSeconds = .65f;
        [Min(1f)] public float dashReach = 13f;
        [Min(1f)] public float dashSpeed = 17f;
        [Min(1f)] public float dashCooldown = 6f;
        [Min(1f)] public float staggerThreshold = 85f;
        [Min(.2f)] public float staggerSeconds = 1.5f;
        public bool FightActive { get; private set; }
        public int FightPhase { get; private set; } = 1;
        public AttackKind CurrentAttack { get; private set; }
        public float ActionProgress { get; private set; }
        public float PoiseRatio => Mathf.Clamp01(poise / Mathf.Max(1f, staggerThreshold));
        public bool DamageWindowActive { get; private set; }
        public int SwingIndex => swingIndex;
        public bool IsExposed => CurrentState == State.Recover || CurrentState == State.Staggered;
        public string AttackName => CurrentAttack == AttackKind.DashThrust ? "ASSASSIN RUSH" :
            CurrentAttack == AttackKind.Whirlwind ? "BLADE CYCLONE" : FightPhase == 2 ? "TRIPLE CUT" : "TWIN CUT";

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
        CappuccinoBossPresentation bossPresentation;
        Vector3 home, committedDirection, attackStartPosition, movementHeading;
        Quaternion committedRotation;
        float lastTickTime, stateStartedAt, windupDuration, poise, poiseHitAt, staggerReadyAt, dashReadyAt, whirlReadyAt;
        int attackCounter, swingIndex, swingCount;
        float swingSeconds;
        readonly RaycastHit[] castHits = new RaycastHit[48];
        readonly Collider[] overlapHits = new Collider[48];
        static readonly float[] AvoidanceAngles = { 0f, 35f, 70f, -35f, -70f, 105f };
        static readonly int MoveParameter = Animator.StringToHash("Move");
        static readonly int AttackTrigger = Animator.StringToHash("Attack");

        void Awake()
        {
            BossCombatVfx.PrewarmImpacts();
            health = GetComponent<Health>();
            presentation = GetComponent<CappuccinoEnemy>();
            motor = GetComponent<CharacterController>();
            animator = presentation.animator;
            ultimate = GetComponent<CappuccinoUltimate>();
            bossPresentation = GetComponent<CappuccinoBossPresentation>();
            if (bossPresentation == null) bossPresentation = gameObject.AddComponent<CappuccinoBossPresentation>();
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
            home = transform.position;
        }

        void Update() => Tick(Time.deltaTime, Time.time);

        void Tick(float deltaTime, float now)
        {
            if (deltaTime <= 0f || PauseSettingsMenu.IsOpen || health == null) return;
            lastTickTime = now;
            if (health.IsDead) { OnDeath(); return; }
            if (ultimate != null && ultimate.isActiveAndEnabled) ultimate.Simulate(now);
            ApplyGravity(deltaTime);
            if (now - poiseHitAt > 2.5f) poise = Mathf.MoveTowards(poise, 0f, deltaTime * 20f);
            if (CurrentState == State.Returning) { TickReturn(deltaTime); return; }
            if ((target == null || !target.gameObject.activeInHierarchy) && now >= acquireAt)
            {
                acquireAt = now + 0.5f;
                AcquireTarget();
            }
            if (target == null || (targetHealth != null && targetHealth.currentHealth <= 0f))
            { if (FightActive) BeginReturn(); else CancelCombat(); SetMovement(0f, deltaTime); return; }
            if (targetHealth == null || targetHealth.transform != target) BindTarget(target);
            Vector3 toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
            float distance = toTarget.magnitude;
            if (FightActive && (distance > giveUpRange ||
                Vector3.ProjectOnPlane(target.position - home, Vector3.up).magnitude > arenaRadius ||
                Vector3.ProjectOnPlane(transform.position - home, Vector3.up).magnitude > arenaRadius + 2f))
            { BeginReturn(); return; }
            if (distance <= sightRange && HasSight()) engaged = true;
            if (!engaged) { SetMovement(0f, deltaTime); return; }
            if (!FightActive) BeginEncounter(now);
            if (CurrentState == State.Staggered && now < actionAt)
            { DamageWindowActive = false; SetMovement(0f, deltaTime); return; }
            if (ultimate != null && ultimate.isActiveAndEnabled && ultimate.TryActivate(now))
            {
                FightPhase = 2;
                FinishAttack(now, .2f);
                bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Surge);
            }
            if (ultimate != null && ultimate.AttackLocked)
            {
                bool resumePose = CurrentState != State.Idle || animator.speed == 0f;
                DamageWindowActive = false;
                hitResolved = true;
                CurrentState = State.Idle; SetMovement(0f, deltaTime);
                if (ultimate.CurrentPhase == CappuccinoUltimate.Phase.Charging)
                {
                    presentation.SetBossPose(.14f);
                    Face(toTarget, deltaTime * .25f);
                }
                else if (resumePose) presentation.ResumeLocomotion();
                return;
            }
            bool movementLocked = ultimate != null && ultimate.MovementLocked;
            if (movementLocked && CurrentAttack == AttackKind.DashThrust &&
                (CurrentState == State.Windup || CurrentState == State.Attack))
                FinishAttack(now,.35f);
            if (CurrentState == State.Windup)
            { TickWindup(toTarget, deltaTime, now); return; }
            if (CurrentState == State.Attack)
            {
                SetMovement(0f, deltaTime);
                TickAttack(deltaTime, now);
                return;
            }
            DamageWindowActive = false;
            if (CurrentState != State.Recover && CurrentState != State.Staggered && CurrentState != State.Orbit)
                Face(toTarget, deltaTime);
            if (movementLocked && CurrentState == State.Retreat) CurrentState = State.Idle;
            if (CurrentState == State.Recover && now < actionAt)
            { SetMovement(0f, deltaTime); return; }
            if (CurrentState == State.Recover || CurrentState == State.Staggered)
            {
                CurrentState = State.Orbit;
                actionAt = now + (FightPhase == 2 ? .45f : .7f);
                presentation.ResumeLocomotion();
            }
            if (CurrentState == State.Orbit && now < actionAt && !movementLocked)
            {
                Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized) * avoidanceSide;
                Vector3 spacing = toTarget.normalized * Mathf.Clamp((distance - attackRange - 1f) * .4f, -.8f, .8f);
                Move((lateral + spacing).normalized, chaseSpeed * .65f, deltaTime, 1f);
                Face(movementHeading,deltaTime); return;
            }
            if (CurrentState == State.Retreat)
            {
                if (now < actionAt && distance < retreatDistance)
                { Move(-toTarget.normalized, retreatSpeed, deltaTime, -1f); return; }
                CurrentState = State.Chase;
            }
            if (!movementLocked && now >= retreatAt && pendingRetreat && distance < crowdedDistance + 1f)
            {
                pendingRetreat = false;
                CurrentState = State.Retreat;
                actionAt = now + retreatSeconds;
                retreatAt = now + retreatCooldown;
                Move(-toTarget.normalized, retreatSpeed, deltaTime, -1f);
                return;
            }
            bool visible = HasSight();
            if (!movementLocked && visible && distance > attackRange + 1f && distance <= dashReach && now >= dashReadyAt)
            { BeginWindup(AttackKind.DashThrust, now); return; }
            if (distance <= attackRange && visible &&
                Vector3.Angle(transform.forward, toTarget) <= 18f)
            {
                SetMovement(0f, deltaTime);
                if (now >= actionAt)
                    BeginWindup(now >= whirlReadyAt && (distance < crowdedDistance + 1f || attackCounter % 3 == 2)
                        ? AttackKind.Whirlwind : AttackKind.TwinSlash, now);
                return;
            }
            if (movementLocked)
            { CurrentState = State.Idle; SetMovement(0f, deltaTime); return; }
            CurrentState = State.Chase;
            // Approach only as far as the sword range. Do not run into Sahur's capsule.
            float speed = visible ? Mathf.Min(chaseSpeed, Mathf.Max(0f, distance - attackRange + 0.15f) / deltaTime) : chaseSpeed;
            Move(toTarget.normalized, speed, deltaTime, 1f);
        }

        void BeginEncounter(float now)
        {
            FightActive = true; engaged = true; actionAt = now + openingSeconds;
            CurrentState = State.Recover;
            bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Intro);
        }

        void BeginWindup(AttackKind kind, float now)
        {
            CurrentAttack = kind; CurrentState = State.Windup;
            stateStartedAt = now; windupDuration = telegraphSeconds + (kind == AttackKind.TwinSlash ? 0f : .25f);
            if (FightPhase == 2) windupDuration *= .9f;
            committedDirection = transform.forward; committedRotation = transform.rotation;
            ActionProgress = 0f; DamageWindowActive = false;
            presentation.SetBossPose(.10f);
            bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Windup);
        }

        void TickWindup(Vector3 toTarget, float dt, float now)
        {
            SetMovement(0f, dt);
            ActionProgress = Mathf.Clamp01((now - stateStartedAt) / windupDuration);
            if (ActionProgress < .62f) Face(toTarget, dt * .6f);
            committedDirection = transform.forward; committedRotation = transform.rotation;
            presentation.SetBossPose(Mathf.Lerp(.10f, .22f, Mathf.SmoothStep(0f,1f,ActionProgress)));
            if (ActionProgress < 1f) return;
            CurrentState = State.Attack; attackStartedAt = now;
            attackStartPosition = transform.position;
            swingSeconds = CurrentAttack == AttackKind.Whirlwind ? 1.05f : CurrentAttack == AttackKind.DashThrust ? .78f : .72f;
            swingCount = CurrentAttack == AttackKind.TwinSlash ? (FightPhase == 2 ? 3 : 2) : 1;
            swingIndex = 0; previousAttackTime = .22f; hitResolved = false;
            attackCounter++;
            if (CurrentAttack == AttackKind.DashThrust) dashReadyAt = now + dashCooldown;
            if (CurrentAttack == AttackKind.Whirlwind) whirlReadyAt = now + 5f;
            bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Swing);
        }

        void TickAttack(float dt, float now)
        {
            float elapsed = Mathf.Max(0f, now - attackStartedAt);
            float cycle = swingSeconds + .18f;
            int index = Mathf.Min(swingCount - 1, Mathf.FloorToInt(elapsed / cycle));
            if (index != swingIndex)
            {
                swingIndex = index; hitResolved = false; previousAttackTime = .22f;
                bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Swing);
            }
            float stageElapsed = elapsed - index * cycle;
            ActionProgress = Mathf.Clamp01(stageElapsed / swingSeconds);
            if (index < swingCount - 1 && stageElapsed >= swingSeconds)
            {
                DamageWindowActive = false;
                presentation.SetBossPose(Mathf.Lerp(.12f,.22f,Mathf.Clamp01((stageElapsed-swingSeconds)/.18f)), (index+1)%2==1);
                return;
            }
            // Anticipation, fast cut, then a longer follow-through, all on the
            // imported blade clip and the same authored damage window.
            float normalized = ActionProgress<.16f?Mathf.Lerp(.22f,.28f,Mathf.SmoothStep(0,1,ActionProgress/.16f)):
                ActionProgress<.52f?Mathf.Lerp(.28f,.62f,(ActionProgress-.16f)/.36f):
                Mathf.Lerp(.62f,1f,Mathf.SmoothStep(0,1,(ActionProgress-.52f)/.48f));
            presentation.SetBossPose(normalized, CurrentAttack == AttackKind.TwinSlash && swingIndex % 2 == 1);
            if(CurrentAttack==AttackKind.TwinSlash && ActionProgress>=.16f && ActionProgress<.52f &&
                target!=null && !(ultimate!=null && ultimate.MovementLocked))
            {
                float gap=Vector3.ProjectOnPlane(target.position-transform.position,Vector3.up).magnitude;
                float room=Mathf.Max(0,gap-attackReach*.55f);
                Move(committedDirection,Mathf.Min(2.1f,room/Mathf.Max(.001f,dt)),dt,1f,false);
            }
            if (CurrentAttack == AttackKind.DashThrust && ActionProgress < .68f && !(ultimate != null && ultimate.MovementLocked))
            {
                float remaining = Mathf.Max(0f,dashReach - attackReach + .5f -
                    Vector3.Dot(transform.position - attackStartPosition,committedDirection));
                Move(committedDirection, Mathf.Min(dashSpeed,remaining/Mathf.Max(.001f,dt)), dt, 1f, false);
            }
            if (CurrentAttack == AttackKind.Whirlwind)
                transform.rotation = Quaternion.AngleAxis(540f * Mathf.SmoothStep(0f, 1f, ActionProgress), Vector3.up) * committedRotation;
            DamageWindowActive = normalized >= hitWindowStart && normalized <= hitWindowEnd;
            if (!hitResolved && normalized >= hitWindowStart && previousAttackTime <= hitWindowEnd)
            {
                // A late frame outside the visible blade window must not deliver a delayed hit.
                if (DamageWindowActive) hitResolved = TryHitTarget();
                else hitResolved = true;
            }
            previousAttackTime = normalized;
            if (elapsed >= swingSeconds * swingCount + (swingCount - 1) * .18f)
                FinishAttack(now, Mathf.Max(.8f, recoverySeconds + (CurrentAttack == AttackKind.TwinSlash ? .3f : .7f)));
        }

        void FinishAttack(float now, float recovery)
        {
            DamageWindowActive = false; hitResolved = true;
            CurrentState = State.Recover; actionAt = now + recovery;
            presentation.ResumeLocomotion();
        }

        bool TryHitTarget()
        {
            if (target == null || targetHealth == null || targetHealth.currentHealth <= 0f || targetHealth.IsProtected) return false;
            Vector3 origin = Chest;
            float reach = CurrentAttack == AttackKind.Whirlwind ? attackReach * 1.05f : attackReach;
            float arc = CurrentAttack == AttackKind.Whirlwind ? 360f : CurrentAttack == AttackKind.DashThrust ? 65f : attackArc;
            int count = Physics.OverlapSphereNonAlloc(origin, reach, overlapHits, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = overlapHits[i];
                if (collider.GetComponentInParent<PlayerHealth>() != targetHealth) continue;
                Vector3 point = collider.ClosestPoint(origin);
                Vector3 direction = Vector3.ProjectOnPlane(collider.bounds.center - origin, Vector3.up);
                if (Vector3.Angle(transform.forward, direction) > arc * 0.5f || IsBlocked(origin, point)) continue;
                float multiplier = CurrentAttack == AttackKind.TwinSlash ? .75f : CurrentAttack == AttackKind.DashThrust ? 1.15f : 1.25f;
                targetHealth.ApplyDamage(damage * multiplier, point);
                // Collider count does not multiply damage. Both swords belong to this one attack.
                target.GetComponent<CombatKnockback>()?.Push(direction.normalized * (CurrentAttack == AttackKind.TwinSlash ? .65f : 1.4f));
                if (Camera.main != null) Camera.main.GetComponent<CombatCameraShake>()?.Pulse(0.025f, 0.12f);
                bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Hit);
                return true;
            }
            return false;
        }

        void BeginReturn()
        {
            CancelCombat(); FightActive = false; CurrentState = State.Returning;
            presentation.ResumeLocomotion();
        }

        void TickReturn(float dt)
        {
            Vector3 delta = Vector3.ProjectOnPlane(home - transform.position, Vector3.up);
            Face(delta, dt);
            if (delta.magnitude > .5f) { Move(delta.normalized, chaseSpeed, dt, 1f); return; }
            ultimate?.ResetEncounter();
            health.Heal(health.maxHealth - health.currentHealth);
            poise = 0f; FightPhase = 1; attackCounter = 0; actionAt = lastTickTime + openingSeconds;
            dashReadyAt = whirlReadyAt = lastTickTime + 1f;
            CurrentState = State.Idle; SetMovement(0f,dt);
        }

        internal bool SampleGround(Vector3 at, out Vector3 point)
        {
            if (FindGround(at, 4f, 9f, out var hit)) { point = hit.point; return true; }
            point = at; return false;
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

        void Move(Vector3 direction, float speed, float dt, float animationDirection, bool allowAvoidance = true)
        {
            if (!motor.enabled || speed <= 0f) { SetMovement(0f, dt); return; }
            Vector3 chosen = Vector3.zero;
            // Retain a side preference so Capri does not alternate left/right against the same obstacle.
            foreach (float baseAngle in AvoidanceAngles)
            {
                if (!allowAvoidance && baseAngle != 0f) continue;
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
            movementHeading = actualSpeed > .02f ? Vector3.ProjectOnPlane(transform.position-before,Vector3.up).normalized : Vector3.zero;
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
            if (health.IsDead || CurrentState == State.Returning) return;
            if (target == null) AcquireTarget();
            engaged = target != null;
            float now = Application.isPlaying ? Time.time : lastTickTime;
            if (engaged && !FightActive) BeginEncounter(now);
            poiseHitAt = now;
            poise += amount * (IsExposed ? 1.35f : 1f);
            if (poise >= staggerThreshold && now >= staggerReadyAt && !(ultimate != null && (ultimate.IsEmpowered || ultimate.AttackLocked)))
            {
                poise = 0f; staggerReadyAt = now + staggerSeconds + 3f;
                FinishAttack(now, staggerSeconds);
                CurrentState = State.Staggered;
                presentation.SetBossPose(.12f);
                bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Stagger);
            }
            pendingRetreat = amount >= 40f;
        }

        void OnDeath()
        {
            if (CurrentState == State.Dead) return;
            CurrentState = State.Dead;
            FightActive = false; DamageWindowActive = false;
            hitResolved = true;
            if (motor != null) motor.enabled = false;
            if (animator != null) animator.SetFloat(MoveParameter, 0f);
            bossPresentation?.Cue(CappuccinoBossPresentation.CueKind.Defeated);
        }

        void CancelCombat()
        {
            engaged = false;
            pendingRetreat = false;
            hitResolved = true;
            DamageWindowActive = false;
            if (CurrentState == State.Attack && animator != null)
            { animator.ResetTrigger(AttackTrigger); animator.CrossFadeInFixedTime("Idle", 0.12f); }
            CurrentState = State.Idle;
        }

        void OnDisable() { DamageWindowActive = false; if (animator != null) { animator.speed = 1f; animator.SetFloat(MoveParameter, 0f); } }
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
