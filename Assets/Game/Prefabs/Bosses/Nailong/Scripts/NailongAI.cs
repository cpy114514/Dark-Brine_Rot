using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    // Chases Sahur and drives Nailong's combat actions.
    [RequireComponent(typeof(NailongHealth), typeof(NailongAttack), typeof(NailongAttackMotion))]
    public sealed class NailongAI : MonoBehaviour
    {
        public enum State { Idle, Chase, Attack, Dead, Staggered }
        enum AttackPattern { None, Slap, CryingBurst, ShamelessCharge, UnreasonableTantrum, ClawFlurry, Roar, ScoldingSpit }

        [Header("乱抓连击")]
        [Min(0.1f)] public float flurryWindup = 0.45f;
        [Min(0.1f)] public float flurryInterval = 0.28f;
        [Range(2, 8)] public int flurryCount = 4;
        [Min(0f)] public float flurryDamageMultiplier = 0.55f;
        [Min(0.1f)] public float flurryRecovery = 0.6f;

        [Header("咆哮震退")]
        [Min(0.1f)] public float roarWindup = 1f;
        [Min(1f)] public float roarRadius = 6f;
        [Min(0f)] public float roarPushDistance = 3.2f;
        [Min(0f)] public float roarDamageMultiplier = 0.7f;
        [Min(0.1f)] public float roarCooldown = 7f;

        [Header("指人骂街 / 口水弹")]
        [Min(1f)] public float spitRange = 14f;
        [Min(0.1f)] public float spitWindup = 0.9f;
        [Min(0.1f)] public float spitInterval = 0.28f;
        [Range(1, 6)] public int spitCount = 3;
        [Min(1f)] public float spitSpeed = 16f;
        [Min(0f)] public float spitDamageMultiplier = 0.8f;
        [Min(0.1f)] public float spitCooldown = 5f;
        [Min(0.1f)] public float skillRecovery = 0.7f;

        [Header("Target")]
        public string targetTag = "Player";
        [Min(1f)] public float sightRange = 12f;
        [Min(1f)] public float giveUpRange = 35f;
        [Min(0.5f)] public float followStopDistance = 2.4f;

        [Header("Movement")]
        [Min(0.5f)] public float chaseSpeed = 4.2f;
        [Min(90f)] public float turnSpeed = 600f;

        [Header("Basic attack")]
        [Min(0.5f)] public float attackRange = 3.05f;
        [Min(0f)] public float attackReach = 3.35f;
        [Min(0.1f)] public float attackWindup = 0.34f;
        [Min(0.1f)] public float attackRecovery = 0.5f;
        [Min(0.1f)] public float attackCooldown = 1.2f;

        [Header("哭闹震波")]
        [Min(2f)] public float cryingRadius = 5.6f;
        [Min(0f)] public float cryingDamageMultiplier = 0.9f;
        [Min(0f)] public float cryingStaminaDrain = 8f;
        [Min(0.1f)] public float cryingWindup = 1f;
        [Min(0.1f)] public float cryingCooldown = 8f;

        [Header("耍赖冲撞")]
        [Min(2f)] public float skillRange = 7.7f;
        [Min(0.1f)] public float shamelessChargeSpeed = 9.5f;
        [Min(0.1f)] public float shamelessChargeDuration = 0.62f;
        [Min(0f)] public float shamelessChargeDamageMultiplier = 1.5f;
        [Min(0.1f)] public float shamelessChargeCooldown = 7f;

        [Header("无理取闹三连跺")]
        [Min(2f)] public float tantrumRadius = 5.2f;
        [Min(0f)] public float tantrumDamageMultiplier = 0.58f;
        [Min(0f)] public float tantrumStaminaDrainPerPulse = 2.5f;
        [Min(0.1f)] public float tantrumWindup = 0.85f;
        [Min(0.1f)] public float tantrumPulseInterval = 0.48f;
        [Min(1)] public int tantrumPulseCount = 3;
        [Min(0.1f)] public float tantrumCooldown = 11f;

        [Header("Visual grounding")]
        [Range(0f, 0.5f)] public float visualGroundDrop = 0.325f;
        public bool keepVisualGroundOffset = true;

        [Header("References")]
        public Animator animator;
        public NailongHealth health;
        public NailongAttack attack;
        public NailongAttackMotion attackMotion;

        public State CurrentState => state;

        State state;
        float staggerUntil;
        public bool IsStaggered => state == State.Staggered;

        public void Interrupt(float seconds)
        {
            if (health == null || health.IsDead || state == State.Dead) return;
            staggerUntil = Mathf.Max(staggerUntil, Time.time + seconds);
            state = State.Staggered;
            activeAttack = AttackPattern.None;
            attackResolved = true;
            nextActionTime = Mathf.Max(nextActionTime, staggerUntil + 0.4f);
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            HideWarning();
            attackMotion?.Stop();
            // Already launched projectiles remain; only the unfinished attack is cancelled.
            effects?.StopPresentation();
            Play("Idle");
        }
        NavMeshAgent agent;
        ProceduralIsland island;
        Transform target;
        Transform visualArmature;
        float surfaceOffset;
        float attackStartedAt;
        float nextAttackTime;
        bool attackResolved;
        AttackPattern activeAttack;
        float nextCryingTime;
        float nextChargeTime;
        float nextTantrumTime;
        int tantrumPulsesDone;
        Vector3 chargeDirection;
        LineRenderer warningRing;
        Material warningMaterial;
        bool engaged;
        float nextRoarTime;
        float nextSpitTime;
        float nextActionTime;
        int strikesDone;
        NailongCombatEffects effects;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            visualArmature = transform.Find("Armature");
            if (health == null) health = GetComponent<NailongHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (attack == null) attack = GetComponent<NailongAttack>();
            if (attackMotion == null) attackMotion = GetComponent<NailongAttackMotion>();
            if (attack != null) attack.enabled = true;
            if (attackMotion != null) attackMotion.enabled = true;
            if (animator != null) animator.applyRootMotion = false;
            effects = GetComponent<NailongCombatEffects>();
            if (effects == null) effects = gameObject.AddComponent<NailongCombatEffects>();
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
            if (agent != null)
            {
                if (agent.enabled && !agent.isOnNavMesh) agent.enabled = false;
                if (!agent.enabled && UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var navHit, 2f, agent.areaMask))
                {
                    transform.position = navHit.position;
                    agent.enabled = true;
                }
            }
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = followStopDistance;
                agent.isStopped = true;
            }
            CreateWarningRing();
            SnapToSurface();
            Play("Idle");
        }

        void Update()
        {
            if (PauseSettingsMenu.IsOpen) return;
            if (state == State.Dead) return;
            if (state == State.Staggered)
            {
                if (Time.time < staggerUntil) return;
                state = State.Idle;
            }
            if (target == null) AcquireTarget();
            if (target == null)
            {
                engaged = false;
                activeAttack = AttackPattern.None;
                EnterIdle();
                return;
            }
            var targetHealth = target.GetComponentInParent<PlayerHealth>();
            if (targetHealth != null && targetHealth.currentHealth <= 0f)
            {
                engaged = false;
                EnterIdle();
                return;
            }

            float distance = FlatDistance(target.position, transform.position);
            if (distance > giveUpRange)
            {
                engaged = false;
                activeAttack = AttackPattern.None;
                EnterIdle();
                return;
            }
            if (distance <= sightRange) engaged = true;
            if (!engaged) return;

            if (state != State.Attack || !attackResolved || activeAttack == AttackPattern.ScoldingSpit) FaceTarget();
            if (state == State.Attack)
            {
                TickAttack();
                return;
            }
            AttackPattern selectedAttack = ChooseAttack(distance);
            if (selectedAttack != AttackPattern.None)
            {
                BeginAttack(selectedAttack);
                return;
            }

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
            if (health != null)
            {
                health.OnDeath.RemoveListener(HandleDeath);
                health.Damaged -= HandleDamaged;
            }
            if (warningRing != null) Destroy(warningRing.gameObject);
            if (warningMaterial != null) Destroy(warningMaterial);
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
            if (state == State.Attack)
            {
                activeAttack = AttackPattern.None;
                HideWarning();
                if (attackMotion != null) attackMotion.Stop();
                effects?.HideTaunt();
            }
            state = State.Idle;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            Play("Idle");
        }

        void EnterChase()
        {
            if (state == State.Chase) return;
            state = State.Chase;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
            Play("Walk");
        }

        AttackPattern ChooseAttack(float distance)
        {
            if (Time.time < nextActionTime) return AttackPattern.None;
            bool claw = distance <= attackRange && Time.time >= nextAttackTime;
            bool roar = distance <= roarRadius && Time.time >= nextRoarTime;
            bool spit = distance <= spitRange && Time.time >= nextSpitTime;
            float roll = Random.value;
            if (roar && roll < 0.3f) return AttackPattern.Roar;
            if (spit && (!claw || roll > 0.75f)) return AttackPattern.ScoldingSpit;
            if (claw) return AttackPattern.ClawFlurry;
            if (roar) return AttackPattern.Roar;
            if (spit) return AttackPattern.ScoldingSpit;
            return AttackPattern.None;
        }

        void BeginAttack(AttackPattern pattern)
        {
            if (state == State.Dead || state == State.Staggered) return;
            activeAttack = pattern;
            state = State.Attack;
            attackStartedAt = Time.time;
            attackResolved = false;
            tantrumPulsesDone = 0;
            strikesDone = 0;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;

            float duration = AttackDuration(pattern);
            if (attackMotion != null)
                attackMotion.Begin(MotionFor(pattern), duration);
            Play("Idle");
            if (pattern == AttackPattern.ClawFlurry)
            {
                nextAttackTime = Time.time + AttackDuration(pattern) + attackCooldown;
                attackMotion?.ConfigureFlurry(flurryWindup, flurryInterval, flurryCount);
            }
            else if (pattern == AttackPattern.Roar)
            {
                nextRoarTime = Time.time + roarCooldown;
                attackMotion?.ConfigureCue(roarWindup);
                ShowWarning(transform.position, roarRadius, new Color(1f, 0.65f, 0.12f));
            }
            else if (pattern == AttackPattern.ScoldingSpit)
            {
                nextSpitTime = Time.time + spitCooldown;
                attackMotion?.ConfigureCue(spitWindup);
                effects?.ShowTaunt(AttackDuration(pattern));
            }

            if (pattern == AttackPattern.Slap)
                nextAttackTime = Time.time + attackCooldown;
            else if (pattern == AttackPattern.CryingBurst)
                nextCryingTime = Time.time + cryingCooldown;
            else if (pattern == AttackPattern.ShamelessCharge)
            {
                nextChargeTime = Time.time + shamelessChargeCooldown;
                chargeDirection = FlatDirection(target.position - transform.position);
            }
            else if (pattern == AttackPattern.UnreasonableTantrum)
                nextTantrumTime = Time.time + tantrumCooldown;

            if (pattern == AttackPattern.CryingBurst)
                ShowWarning(transform.position, cryingRadius, new Color(0.48f, 0.83f, 1f, 0.92f));
            else if (pattern == AttackPattern.UnreasonableTantrum)
                ShowWarning(transform.position, tantrumRadius, new Color(1f, 0.48f, 0.18f, 0.95f));
            else if (pattern == AttackPattern.ShamelessCharge && target != null)
                ShowWarning(target.position, 1.8f, new Color(1f, 0.76f, 0.22f, 0.95f));
        }

        void TickAttack()
        {
            if (state != State.Attack) return;
            float elapsed = Time.time - attackStartedAt;
            switch (activeAttack)
            {
                case AttackPattern.ClawFlurry:
                    // Each individual swipe owns one hit, not damage every frame.
                    while (strikesDone < flurryCount && elapsed >= flurryWindup + strikesDone * flurryInterval)
                    {
                        strikesDone++;
                        attackResolved = true;
                        TryHit(attackReach, flurryDamageMultiplier, 0.16f);
                    }
                    if (elapsed >= AttackDuration(activeAttack)) FinishAttack();
                    break;

                case AttackPattern.Roar:
                    if (!attackResolved && elapsed >= roarWindup)
                    {
                        attackResolved = true;
                        HideWarning();
                        attack?.Roar(roarRadius, roarDamageMultiplier, roarPushDistance, target);
                        effects?.Roar(roarRadius);
                    }
                    if (elapsed >= AttackDuration(activeAttack)) FinishAttack();
                    break;

                case AttackPattern.ScoldingSpit:
                    while (strikesDone < spitCount && elapsed >= spitWindup + strikesDone * spitInterval)
                    {
                        strikesDone++;
                        attackResolved = true;
                        if (target != null) effects?.Spit(target, spitSpeed, attack.damage * spitDamageMultiplier);
                    }
                    if (elapsed >= AttackDuration(activeAttack)) FinishAttack();
                    break;

                case AttackPattern.Slap:
                    if (!attackResolved && elapsed >= attackWindup)
                    {
                        attackResolved = true;
                        TryHit(attackReach, 1f, 0.55f);
                    }
                    if (elapsed >= attackWindup + attackRecovery) FinishAttack();
                    break;

                case AttackPattern.CryingBurst:
                    if (elapsed < cryingWindup)
                    {
                        ShowWarning(transform.position, cryingRadius, WarningColor(0.48f, 0.83f, 1f, 0.92f));
                        break;
                    }
                    if (!attackResolved)
                    {
                        attackResolved = true;
                        HideWarning();
                        if (TryHit(cryingRadius, cryingDamageMultiplier, 2.1f, true))
                            DrainTargetStamina(cryingStaminaDrain);
                    }
                    if (elapsed >= cryingWindup + 0.65f) FinishAttack();
                    break;

                case AttackPattern.ShamelessCharge:
                    TickShamelessCharge(elapsed);
                    break;

                case AttackPattern.UnreasonableTantrum:
                    TickTantrum(elapsed);
                    break;

                default:
                    FinishAttack();
                    break;
            }
        }

        void TickShamelessCharge(float elapsed)
        {
            const float windup = 0.55f;
            if (elapsed < windup)
            {
                if (target != null) ShowWarning(target.position, 1.8f, WarningColor(1f, 0.76f, 0.22f, 0.95f));
                return;
            }

            float chargeElapsed = elapsed - windup;
            if (chargeElapsed < shamelessChargeDuration)
            {
                HideWarning();
                if (chargeDirection.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(chargeDirection), turnSpeed * Time.deltaTime);
                Vector3 previousPosition = transform.position;
                MoveBy(chargeDirection * shamelessChargeSpeed * Time.deltaTime);
                if (!attackResolved && target != null && attack != null &&
                    DistanceToFlatSegment(target.position, previousPosition, transform.position) <= 1.9f)
                {
                    attackResolved = attack.TryHit(target, 2.2f,
                        shamelessChargeDamageMultiplier, 2.4f, true);
                }
                return;
            }

            if (elapsed >= windup + shamelessChargeDuration + 0.42f)
                FinishAttack();
        }

        void TickTantrum(float elapsed)
        {
            if (elapsed < tantrumWindup)
            {
                ShowWarning(transform.position, tantrumRadius, WarningColor(1f, 0.48f, 0.18f, 0.95f));
                return;
            }

            float pulseRadius = tantrumRadius * (0.55f + 0.225f * tantrumPulsesDone);
            float nextPulseTime = tantrumWindup + tantrumPulseInterval * tantrumPulsesDone;
            if (tantrumPulsesDone < tantrumPulseCount && elapsed >= nextPulseTime)
            {
                tantrumPulsesDone++;
                ShowWarning(transform.position, pulseRadius, WarningColor(1f, 0.64f, 0.2f, 1f));
                if (TryHit(pulseRadius, tantrumDamageMultiplier, 0.55f, true))
                    DrainTargetStamina(tantrumStaminaDrainPerPulse);
            }
            else if (elapsed > nextPulseTime + 0.18f)
            {
                HideWarning();
            }

            if (elapsed >= tantrumWindup + tantrumPulseInterval * tantrumPulseCount + 0.45f)
                FinishAttack();
        }

        bool TryHit(float reach, float multiplier, float push, bool ignoreFacing = false)
        {
            return attack != null && target != null &&
                attack.TryHit(target, reach, multiplier, push, ignoreFacing);
        }

        void DrainTargetStamina(float amount)
        {
            PlayerStamina stamina = target != null ? target.GetComponentInParent<PlayerStamina>() : null;
            if (stamina != null) stamina.TrySpend(amount);
        }

        void FinishAttack()
        {
            nextActionTime = Time.time + 0.4f;
            effects?.HideTaunt();
            activeAttack = AttackPattern.None;
            HideWarning();
            if (attackMotion != null) attackMotion.Stop();
            EnterIdle();
        }

        float AttackDuration(AttackPattern pattern)
        {
            switch (pattern)
            {
                case AttackPattern.ClawFlurry: return flurryWindup + flurryInterval * (flurryCount - 1) + flurryRecovery;
                case AttackPattern.Roar: return roarWindup + skillRecovery;
                case AttackPattern.ScoldingSpit: return spitWindup + spitInterval * (spitCount - 1) + skillRecovery;
                case AttackPattern.Slap: return attackWindup + attackRecovery;
                case AttackPattern.CryingBurst: return cryingWindup + 0.65f;
                case AttackPattern.ShamelessCharge: return 0.55f + shamelessChargeDuration + 0.42f;
                case AttackPattern.UnreasonableTantrum:
                    return tantrumWindup + tantrumPulseInterval * tantrumPulseCount + 0.45f;
                default: return 0.5f;
            }
        }

        static NailongAttackMotion.Style MotionFor(AttackPattern pattern)
        {
            switch (pattern)
            {
                case AttackPattern.ClawFlurry: return NailongAttackMotion.Style.Flurry;
                case AttackPattern.Roar: return NailongAttackMotion.Style.Roar;
                case AttackPattern.ScoldingSpit: return NailongAttackMotion.Style.PointAndSpit;
                case AttackPattern.CryingBurst: return NailongAttackMotion.Style.Cry;
                case AttackPattern.ShamelessCharge: return NailongAttackMotion.Style.ShoulderBump;
                case AttackPattern.UnreasonableTantrum: return NailongAttackMotion.Style.Tantrum;
                default: return NailongAttackMotion.Style.DoubleClaw;
            }
        }

        void CreateWarningRing()
        {
            GameObject ringObject = new GameObject("Nailong Combat Telegraph");
            warningRing = ringObject.AddComponent<LineRenderer>();
            warningRing.useWorldSpace = true;
            warningRing.loop = true;
            warningRing.positionCount = 65;
            warningRing.widthMultiplier = 0.075f;
            warningRing.numCapVertices = 2;
            warningRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            warningRing.receiveShadows = false;
            warningRing.enabled = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                warningMaterial = new Material(shader);
                warningMaterial.name = "Nailong Combat Telegraph (Runtime)";
                if (warningMaterial.HasProperty("_Surface")) warningMaterial.SetFloat("_Surface", 1f);
                if (warningMaterial.HasProperty("_BaseColor")) warningMaterial.SetColor("_BaseColor", Color.white);
                if (warningMaterial.HasProperty("_Color")) warningMaterial.SetColor("_Color", Color.white);
                warningMaterial.renderQueue = 3000;
                warningRing.material = warningMaterial;
            }
        }

        void ShowWarning(Vector3 center, float radius, Color color)
        {
            if (warningRing == null) return;
            warningRing.enabled = true;
            warningRing.startColor = color;
            warningRing.endColor = color;
            float groundY = island != null ? island.GetWorldSurfaceHeight(center) + 0.08f : center.y + 0.08f;
            center.y = groundY;
            int count = warningRing.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / (count - 1);
                warningRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f,
                    Mathf.Sin(angle) * radius));
            }
        }

        void HideWarning()
        {
            if (warningRing != null) warningRing.enabled = false;
        }

        static Color WarningColor(float r, float g, float b, float a) => new Color(r, g, b, a);

        void MoveBy(Vector3 delta)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(delta);
                return;
            }

            Vector3 next = transform.position + delta;
            if (island != null) next.y = island.GetWorldSurfaceHeight(next) + surfaceOffset;
            transform.position = next;
        }

        static Vector3 FlatDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        static float DistanceToFlatSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            point.y = start.y = end.y = 0f;
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f) return Vector3.Distance(point, start);
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return Vector3.Distance(point, start + segment * t);
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
                Quaternion.LookRotation(direction),
                (state == State.Attack && activeAttack == AttackPattern.ScoldingSpit ? Mathf.Min(turnSpeed, 120f) : turnSpeed) * Time.deltaTime);
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
            activeAttack = AttackPattern.None;
            HideWarning();
            if (attackMotion != null) attackMotion.Stop();
            effects?.Cancel();
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

        void OnDisable()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            activeAttack = AttackPattern.None;
            HideWarning();
            attackMotion?.Stop();
            effects?.Cancel();
            if (state != State.Dead) state = State.Idle;
        }
    }
}
