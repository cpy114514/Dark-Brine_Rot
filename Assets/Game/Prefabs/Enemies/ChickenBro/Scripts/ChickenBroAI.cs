using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    /// <summary>Weak melee chicken. Two swings, a short lunge, and reactions through the shared hit pipeline.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public sealed class ChickenBroAI : MonoBehaviour, ICombatHitResolver
    {
        public enum Mood { Idle, Patrol, Chase, Strike, Lunge, Flinch, Launch, Knockdown, Dead }

        [Header("Awareness")]
        [Min(2f)] public float sightRange = 28f;
        [Min(4f)] public float giveUpRange = 46f;
        [Min(.5f)] public float meleeRange = 1.7f;
        [Min(1f)] public float lungeMin = 2.2f;
        [Min(1f)] public float lungeMax = 4.2f;
        [Header("Movement")]
        [Min(.5f)] public float walkSpeed = 2.1f;
        [Min(1f)] public float runSpeed = 4.8f;
        [Min(1f)] public float lungeSpeed = 9f;
        [Min(1f)] public float turnSpeed = 540f;
        [Min(.2f)] public float surroundDistance = 3.5f;
        [Min(.4f)] public float separationRadius = 1.35f;
        [Header("Animation")]
        public bool matchLocomotionToMovement;
        [Header("Attacks")]
        [Min(1f)] public float strikeDamage = 3f;
        [Min(1f)] public float lungeDamage = 5f;
        [Min(.2f)] public float strikeDuration = .95f;
        [Min(.2f)] public float lungeDuration = 1.15f;
        [Min(0f)] public float attackCooldown = 1.15f;
        [Range(0f, 1f)] public float strikeWindowStart = .32f;
        [Range(0f, 1f)] public float strikeWindowEnd = .52f;
        [Range(0f, 1f)] public float lungeWindowStart = .4f;
        [Range(0f, 1f)] public float lungeWindowEnd = .68f;
        [Min(.2f)] public float attackRadius = .85f;
        [Header("Reactions")]
        [Min(0f)] public float lightSlide = 3.2f;
        [Min(0f)] public float heavySlide = 6.5f;
        [Min(0f)] public float launchSlide = 9f;
        [Min(0f)] public float launchHop = 7.5f;
        [Min(.2f)] public float flinchSeconds = .28f;
        [Min(.2f)] public float knockdownSeconds = .85f;
        [Min(0f)] public float removeAfterDeath = 4f;
        [Min(0f)] public float courtesySeconds = .4f;

        CharacterController motor;
        Health health;
        Animator animator;
        MarshmallowEnemyVisual marshmallow;
        ChickenBroFlock flock;
        OceanWorld ocean;
        PlayerHealth player;
        NavMeshPath path;
        readonly Collider[] overlaps = new Collider[16];
        readonly RaycastHit[] casts = new RaycastHit[16];
        readonly HashSet<PlayerHealth> struck = new HashSet<PlayerHealth>();
        Vector3 home, patrol, knock, committed;
        float verticalSpeed = -2f, actionTime, nextAttack, nextPath, nextPatrol, nextAcquire, lostSightAt, removeAt = -1f, repathStride, locomotionSpeed;
        int corner;
        bool hasPath, returningHome, reclaiming, subscribed, animatorReady;
        static readonly float[] AvoidAngles = { 0f, 28f, -28f, 56f, -56f, 84f, -84f };
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int MotionHash = Animator.StringToHash("Motion");

        public Mood Mode { get; private set; } = Mood.Idle;
        public float ActionPhase { get; private set; }
        public bool HoldsAttack => Mode == Mood.Strike || Mode == Mood.Lunge;
        public bool IsAlive => health && !health.IsDead && Mode != Mood.Dead;
        public float MovementMultiplier => marshmallow && marshmallow.IsAlert ? Mathf.Max(1f, marshmallow.alertSpeedMultiplier) : 1f;

        void Awake()
        {
            motor = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            marshmallow = GetComponent<MarshmallowEnemyVisual>();
            animator = GetComponentInChildren<Animator>();
            if (animator && !AnimatorHasClip(animator)) animator.enabled = false;
            path = new NavMeshPath();
            home = patrol = transform.position;
            repathStride = (Mathf.Abs(GetInstanceID()) % 8) * .05f;
            CombatBodyCollision.Ensure(gameObject);
            if (!GetComponent<CombatImpactResponse>())
            {
                var response = gameObject.AddComponent<CombatImpactResponse>();
                response.material = CombatMaterial.Flesh;
                response.knockbackMultiplier = 0f;
            }
        }

        static bool AnimatorHasClip(Animator source)
        {
            var controller = source.runtimeAnimatorController;
            if (!controller) return false;
            foreach (var clip in controller.animationClips)
                if (clip) return true;
            return false;
        }

        void OnEnable()
        {
            flock = ChickenBroFlock.Active;
            flock?.Add(this);
            if (health && !subscribed) { health.OnDeath.AddListener(Die); subscribed = true; }
            if (ocean == null) ocean = FindFirstObjectByType<OceanWorld>();
        }

        void OnDisable()
        {
            flock?.Remove(this);
        }

        void OnDestroy()
        {
            if (subscribed && health) health.OnDeath.RemoveListener(Die);
        }

        public void Activate(Vector3 position, Quaternion rotation, ChickenBroFlock group)
        {
            flock = group;
            home = patrol = position;
            knock = Vector3.zero;
            verticalSpeed = -2f;
            actionTime = 0f;
            locomotionSpeed = 0f;
            nextAttack = Time.time + .4f;
            nextPath = 0f;
            nextPatrol = 0f;
            returningHome = false;
            hasPath = false;
            reclaiming = false;
            removeAt = -1f;
            Mode = Mood.Idle;
            ActionPhase = 0f;
            if (health) health.currentHealth = health.maxHealth;
            if (motor) motor.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (motor) motor.enabled = true;
            gameObject.SetActive(true);
        }

        public CombatHitResult ResolveCombatHit(GameObject attacker, float damage, Vector3 point, CombatHitKind kind)
        {
            if (!health || health.IsDead || damage <= 0f) return CombatHitResult.Unflinching;
            Vector3 direction = Vector3.ProjectOnPlane(transform.position - (attacker ? attacker.transform.position : transform.position), Vector3.up);
            if (direction.sqrMagnitude < .01f) direction = -transform.forward;
            direction.Normalize();
            bool launch = kind == CombatHitKind.ChargedHeavy || kind == CombatHitKind.JumpSlash;
            bool heavy = launch || kind == CombatHitKind.ComboFinisher || kind == CombatHitKind.Spin;
            health.ApplyDamage(damage, point);
            bool killed = health.IsDead;
            if (!killed) React(direction, kind, launch, heavy);
            else CarryDeath(direction, launch || heavy);
            if (!killed && kind != CombatHitKind.Rifle) flock?.Courtesy(courtesySeconds);
            return heavy ? CombatHitResult.HeavyHit : CombatHitResult.Hit;
        }

        void React(Vector3 direction, CombatHitKind kind, bool launch, bool heavy)
        {
            committed = direction;
            flock?.ReleaseAttack(this);
            if (launch)
            {
                Mode = Mood.Launch;
                knock = direction * launchSlide;
                verticalSpeed = launchHop;
                actionTime = 0f;
            }
            else if (heavy)
            {
                Mode = kind == CombatHitKind.Spin ? Mood.Flinch : Mood.Knockdown;
                knock = direction * (kind == CombatHitKind.Spin ? heavySlide : heavySlide * .85f);
                actionTime = 0f;
            }
            else
            {
                // Rifle shots stay visible without resetting a longer melee flinch every bullet.
                if (kind == CombatHitKind.Rifle && Mode == Mood.Flinch) actionTime = Mathf.Min(actionTime, flinchSeconds * .35f);
                else { Mode = Mood.Flinch; actionTime = 0f; }
                knock = direction * (kind == CombatHitKind.Rifle ? lightSlide * .45f : lightSlide);
            }
        }

        void CarryDeath(Vector3 direction, bool hard)
        {
            knock = direction * (hard ? launchSlide : lightSlide);
            if (hard) verticalSpeed = Mathf.Max(verticalSpeed, launchHop * .65f);
        }

        void Die()
        {
            Mode = Mood.Dead;
            ActionPhase = 0f;
            actionTime = 0f;
            flock?.Remove(this);
            removeAt = Time.time + removeAfterDeath;
            if (animator) animator.SetInteger(MotionHash, 7);
        }

        void Update()
        {
            if (!motor) return;
            float dt = Time.deltaTime;
            if (dt <= 0f || PauseSettingsMenu.IsOpen) return;
            locomotionSpeed = 0f;
            if (Mode == Mood.Dead) { Integrate(dt); SettleCorpse(dt); Present(); return; }
            if (!IsAlive) return;
            Acquire();
            float distance = player ? Flat(player.transform.position) : float.PositiveInfinity;
            if (marshmallow && !marshmallow.IsAlert && HoldsAttack)
            {
                flock?.ReleaseAttack(this);
                Mode = Mood.Idle; ActionPhase = 0f;
            }
            switch (Mode)
            {
                case Mood.Launch:
                case Mood.Knockdown:
                case Mood.Flinch:
                    actionTime += dt;
                    Integrate(dt);
                    if (Recovered()) Mode = Mood.Idle;
                    break;
                case Mood.Strike:
                case Mood.Lunge:
                    TickAttack(dt, distance);
                    break;
                default:
                    Decide(distance, dt);
                    break;
            }
            Present();
        }

        void Decide(float distance, float dt)
        {
            bool engaged;
            if (marshmallow) engaged = marshmallow.IsAlert && player;
            else
            {
                bool seen = CanSee(distance);
                if (!seen) lostSightAt = lostSightAt <= 0f ? Time.time : lostSightAt;
                else lostSightAt = 0f;
                engaged = (seen || (player && lostSightAt > 0f && Time.time - lostSightAt < 2.5f && distance < giveUpRange)) && distance <= giveUpRange;
            }
            if (!engaged)
            {
                flock?.ReleaseAttack(this);
                Roam(dt);
                return;
            }
            Vector3 target = player.transform.position;
            bool token = flock == null || flock.TryAttack(this);
            if (!token)
            {
                flock?.ReleaseAttack(this);
                MoveToward(flock != null ? flock.SurroundPoint(this, target, surroundDistance) : target, walkSpeed, dt, false);
                Mode = Mood.Chase;
                return;
            }
            if (distance <= meleeRange && TryBegin(Mood.Strike)) return;
            if (distance >= lungeMin && distance <= lungeMax && Time.time >= nextAttack && Random.value < .45f && TryBegin(Mood.Lunge)) return;
            if (distance > meleeRange) MoveToward(target, runSpeed, dt, false);
            else
            {
                if (player) Face(FlatDirection(target), dt);
                Integrate(dt);
            }
            Mode = distance > meleeRange ? Mood.Chase : Mood.Idle;
        }

        bool TryBegin(Mood attack)
        {
            if (Time.time < nextAttack || (flock != null && flock.AttacksPaused)) return false;
            Mode = attack;
            actionTime = 0f;
            struck.Clear();
            committed = FlatDirection(player ? player.transform.position : transform.position + transform.forward);
            Face(committed, 1f);
            return true;
        }

        void TickAttack(float dt, float distance)
        {
            actionTime += dt;
            float duration = Mode == Mood.Lunge ? lungeDuration : strikeDuration;
            ActionPhase = Mathf.Clamp01(actionTime / Mathf.Max(.01f, duration));
            float windowStart = Mode == Mood.Lunge ? lungeWindowStart : strikeWindowStart;
            float windowEnd = Mode == Mood.Lunge ? lungeWindowEnd : strikeWindowEnd;
            if (ActionPhase < windowStart && player)
                committed = Vector3.Slerp(committed, FlatDirection(player.transform.position), 1f - Mathf.Exp(-8f * dt)).normalized;
            Face(committed, dt);
            if (Mode == Mood.Lunge && ActionPhase >= windowStart && ActionPhase <= windowEnd)
            {
                if (GroundAhead(committed)) motor.Move(committed * lungeSpeed * MovementMultiplier * dt);
            }
            if (ActionPhase >= windowStart && ActionPhase <= windowEnd) Strike();
            if (ActionPhase < 1f)
            {
                verticalSpeed = motor.isGrounded ? -2f : verticalSpeed - 28f * dt;
                motor.Move(Vector3.up * verticalSpeed * dt + Separation() * dt);
                return;
            }
            nextAttack = Time.time + attackCooldown;
            flock?.ReleaseAttack(this);
            Mode = marshmallow ? (marshmallow.IsAlert ? Mood.Chase : Mood.Idle) : (distance <= giveUpRange ? Mood.Chase : Mood.Idle);
        }

        void Strike()
        {
            Vector3 origin = transform.position + Vector3.up * (motor.height * .55f) + committed * (motor.radius + .35f);
            int count = Physics.OverlapSphereNonAlloc(origin, attackRadius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = overlaps[i];
                if (!hit || hit.transform.IsChildOf(transform)) continue;
                var victim = hit.GetComponentInParent<PlayerHealth>();
                if (!victim || !struck.Add(victim)) continue;
                Vector3 point = hit.ClosestPoint(origin);
                if (Blocked(origin, point)) continue;
                float amount = Mode == Mood.Lunge ? lungeDamage : strikeDamage;
                victim.ApplyMeleeDamage(amount, point, transform.position);
            }
        }

        void Roam(float dt)
        {
            // Wander goals occupy a 4.5m circle. A smaller home threshold reverses
            // movement every frame at its boundary; return with a separate leash.
            if (!returningHome && Flat(home) > 6f)
            {
                returningHome = true;
                nextPath = 0f;
            }
            if (returningHome)
            {
                if (Flat(home) > .8f) { MoveToward(home, walkSpeed, dt, true); Mode = Mood.Patrol; return; }
                returningHome = false;
                nextPatrol = nextPath = 0f;
            }
            if (Flat(patrol) < .6f || Time.time >= nextPatrol)
            {
                Vector2 ring = Random.insideUnitCircle * 4.5f;
                patrol = Sample(home + new Vector3(ring.x, 0f, ring.y));
                nextPatrol = Time.time + Random.Range(2.5f, 4.5f);
                nextPath = 0f;
            }
            MoveToward(patrol, walkSpeed, dt, true);
            Mode = Flat(patrol) > .6f ? Mood.Patrol : Mood.Idle;
        }

        void MoveToward(Vector3 goal, float speed, float dt, bool walk)
        {
            speed *= MovementMultiplier;
            Vector3 before = transform.position;
            locomotionSpeed = 0f;
            Vector3 direction = Steer(goal);
            direction += Separation();
            if (direction.sqrMagnitude > .01f) direction.Normalize();
            if (direction.sqrMagnitude < .01f) { Mode = walk ? Mood.Idle : Mode; Integrate(dt); return; }
            Face(direction, dt);
            Vector3 chosen = Avoid(direction, speed * dt + motor.radius);
            if (chosen.sqrMagnitude < .01f) { Integrate(dt); return; }
            motor.Move(chosen * speed * dt);
            verticalSpeed = motor.isGrounded ? -2f : verticalSpeed - 28f * dt;
            motor.Move(Vector3.up * verticalSpeed * dt);
            if (ocean && transform.position.y < ocean.oceanHeight + .08f)
                motor.Move(Vector3.up * (ocean.oceanHeight + .16f - transform.position.y));
            if (matchLocomotionToMovement && dt > 0f)
            {
                Vector3 moved = transform.position - before; moved.y = 0f;
                locomotionSpeed = moved.magnitude / dt;
            }
        }

        Vector3 Steer(Vector3 goal)
        {
            if (Time.time >= nextPath) { nextPath = Time.time + .4f + repathStride; hasPath = Repath(goal); }
            if (!hasPath || path.corners == null || path.corners.Length < 2) return FlatDirection(goal);
            if (corner >= path.corners.Length) corner = path.corners.Length - 1;
            if (Flat(path.corners[corner]) < .55f && corner < path.corners.Length - 1) corner++;
            return FlatDirection(path.corners[corner]);
        }

        bool Repath(Vector3 goal)
        {
            if (!NavMesh.SamplePosition(transform.position, out var from, 2.5f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(goal, out var to, 2.5f, NavMesh.AllAreas)) return false;
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)) return false;
            if (path.status == NavMeshPathStatus.PathInvalid || path.corners == null || path.corners.Length < 2) return false;
            corner = 1;
            return true;
        }

        Vector3 Avoid(Vector3 direction, float distance)
        {
            foreach (float angle in AvoidAngles)
            {
                Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                if (!Clear(candidate, distance)) continue;
                if (!GroundAhead(candidate)) continue;
                return candidate;
            }
            return Vector3.zero;
        }

        bool Clear(Vector3 direction, float distance)
        {
            Vector3 center = transform.TransformPoint(motor.center);
            float radius = motor.radius * .9f;
            float half = Mathf.Max(0f, motor.height * .5f - radius);
            Vector3 bottom = center - Vector3.up * Mathf.Max(0f, half - motor.stepOffset);
            int count = Physics.CapsuleCastNonAlloc(bottom, center + Vector3.up * half, radius, direction, casts, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = casts[i].collider;
                if (!hit || hit.transform.IsChildOf(transform) || CombatBodyCollision.IsActor(hit)) continue;
                return false;
            }
            return true;
        }

        bool GroundAhead(Vector3 direction)
        {
            Vector3 at = transform.position + direction * Mathf.Max(.6f, motor.radius);
            return FindGround(at, out _);
        }

        void Integrate(float dt)
        {
            if (!motor.enabled) return;
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-4.5f * dt));
            if (!motor.isGrounded) verticalSpeed -= 28f * dt;
            else if (verticalSpeed < 0f) verticalSpeed = -2f;
            motor.Move((knock + Vector3.up * verticalSpeed) * dt);
        }

        bool Recovered()
        {
            if (Mode == Mood.Flinch) return actionTime >= flinchSeconds && knock.sqrMagnitude < .15f;
            if (!motor.isGrounded && actionTime < 1.6f) return false;
            float needed = Mode == Mood.Launch ? .45f : knockdownSeconds;
            return actionTime >= needed && knock.sqrMagnitude < .2f;
        }

        void SettleCorpse(float dt)
        {
            ActionPhase = Mathf.Clamp01(actionTime / .55f);
            actionTime += dt;
            if (motor.enabled && motor.isGrounded && actionTime > .55f) motor.enabled = false;
            if (!reclaiming && removeAt >= 0f && Time.time >= removeAt)
            {
                reclaiming = true;
                ChickenBroEncounter.Active?.Reclaim(this);
                if (gameObject.activeSelf) gameObject.SetActive(false);
            }
        }

        void Present()
        {
            if (!animator) return;
            if (!animatorReady)
            {
                foreach (var parameter in animator.parameters)
                    if (parameter.nameHash == MotionHash) animatorReady = true;
                if (!animatorReady) { animator = null; return; }
            }
            float speed = Mode == Mood.Chase ? 1f : Mode == Mood.Patrol ? .45f : 0f;
            if (matchLocomotionToMovement)
            {
                speed = Mode == Mood.Chase || Mode == Mood.Patrol ? Mathf.Clamp01(locomotionSpeed / Mathf.Max(.01f, runSpeed)) : 0f;
                if (speed <= .001f) animator.SetFloat(SpeedHash, 0f);
                else animator.SetFloat(SpeedHash, speed, .12f, Time.deltaTime);
                speed = animator.GetFloat(SpeedHash);
            }
            else animator.SetFloat(SpeedHash, speed);
            animator.SetInteger(MotionHash, Mode == Mood.Dead ? 7 : Mode == Mood.Knockdown || Mode == Mood.Launch ? 6 :
                Mode == Mood.Flinch ? 5 : Mode == Mood.Lunge ? 4 : Mode == Mood.Strike ? 3 : speed > .7f ? 2 : speed > .1f ? 1 : 0);
        }

        void Acquire()
        {
            if (player && player.currentHealth > 0f) return;
            if (Time.time < nextAcquire) return;
            nextAcquire = Time.time + .4f;
            player = null;
            float best = float.PositiveInfinity;
            foreach (var candidate in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (!candidate || candidate.currentHealth <= 0f) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; player = candidate; }
            }
        }

        bool CanSee(float distance)
        {
            if (!player || distance > sightRange) return false;
            if (distance < meleeRange + 1.2f) return true;
            Vector3 from = transform.position + Vector3.up * motor.height * .7f;
            Vector3 to = player.transform.position + Vector3.up * 1f;
            return !Blocked(from, to);
        }

        bool Blocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .001f) return false;
            int count = Physics.RaycastNonAlloc(from, delta.normalized, casts, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = casts[i].collider;
                if (!hit || hit.transform.IsChildOf(transform) || (player && hit.transform.IsChildOf(player.transform)) || CombatBodyCollision.IsActor(hit)) continue;
                return true;
            }
            return false;
        }

        Vector3 Separation()
        {
            Vector3 push = flock != null ? flock.Separation(this, separationRadius) : Vector3.zero;
            return Vector3.ClampMagnitude(push, 1.2f);
        }

        void Face(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude < .0001f) return;
            Quaternion want = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * dt);
        }

        float Flat(Vector3 point)
        {
            point -= transform.position;
            point.y = 0f;
            return point.magnitude;
        }

        Vector3 FlatDirection(Vector3 point)
        {
            point -= transform.position;
            point.y = 0f;
            return point.sqrMagnitude > .0001f ? point.normalized : transform.forward;
        }

        Vector3 Sample(Vector3 desired)
        {
            if (NavMesh.SamplePosition(desired, out var hit, 3f, NavMesh.AllAreas)) desired = hit.position;
            return FindGround(desired, out var ground) ? ground : desired;
        }

        bool FindGround(Vector3 at, out Vector3 point)
        {
            point = at;
            Vector3 origin = at + Vector3.up * 3f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, casts, 8f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = casts[i];
                if (!hit.collider || CombatBodyCollision.IsActor(hit.collider) || hit.normal.y < .55f) continue;
                if (ocean && hit.point.y < ocean.oceanHeight + .05f) continue;
                if (hit.distance < best) { best = hit.distance; point = hit.point; found = true; }
            }
            return found;
        }
    }
}
