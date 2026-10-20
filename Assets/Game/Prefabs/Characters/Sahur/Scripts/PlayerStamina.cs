using UnityEngine;

namespace Mavis
{
    /// <summary>Shared stamina pool for Sahur's movement and combat actions.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina")]
        [Min(1f)] public float maxStamina = 100f;
        [Min(0f)] public float currentStamina = 100f;
        [Min(0f)] public float regenerationPerSecond = 35f;
        [Min(0f)] public float regenerationDelay = 0.4f;

        [Header("Movement costs")]
        [Min(0f)] public float sprintDrainPerSecond = 4f;
        [Min(0f)] public float sprintRestartThreshold = 4f;
        [Min(0f)] public float jumpCost = 4f;
        [Min(0f)] public float rollCost = 6f;
        [Min(0f)] public float airFlipCost = 5f;

        [Header("Attack costs")]
        [Min(0f)] public float lightAttackCost = 3f;
        [Min(0f)] public float jumpSlashCost = 5f;
        [Min(0f)] public float chargedAttackCost = 10f;

        [Header("Exploration and combat pacing")]
        [Range(0f, 1f)] public float explorationSprintMultiplier = .15f;
        [Min(0f)] public float combatRegenerationPerSecond = 22f;
        [Min(0f)] public float combatRegenerationDelay = .85f;
        float combatUntil, nextContextScan;
        SahurAttack attack;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        public bool InCombat => Time.time < combatUntil;
        public float ActiveRegenerationRate => InCombat ? combatRegenerationPerSecond : regenerationPerSecond;
        public float ActiveRegenerationDelay => InCombat ? combatRegenerationDelay : regenerationDelay;
        public void NotifyCombat(float seconds = 6f)
        {
            combatUntil = Mathf.Max(combatUntil, Time.time + seconds);
            nextRegenerationTime = Mathf.Max(nextRegenerationTime, Time.time + combatRegenerationDelay);
        }
        public float SprintCost(float deltaTime, float rate) => Mathf.Max(0f, rate) * Mathf.Max(0f, deltaTime) *
            (InCombat ? 1f : explorationSprintMultiplier);

        float nextRegenerationTime;
        bool sprinting;

        public float Current => Mathf.Clamp(currentStamina, 0f, maxStamina);
        public float Maximum => Mathf.Max(1f, maxStamina);
        public float Normalized => Current / Maximum;

        void Awake()
        {
            attack = GetComponent<SahurAttack>(); movement = GetComponent<ThirdPersonPlayerController>(); health = GetComponent<PlayerHealth>();
            maxStamina = Mathf.Max(1f, maxStamina);
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            nextRegenerationTime = Time.time;
        }

        void Update()
        {
            if (Time.time >= nextContextScan)
            {
                nextContextScan = Time.time + .5f;
                foreach (var id in FindObjectsByType<WorldSaveIdentity>(FindObjectsSortMode.None))
                {
                    bool active = (id.TryGetComponent<CappuccinoAI>(out var capri) && capri.FightActive) ||
                        (id.TryGetComponent<NailongAI>(out var nailong) && nailong.FightActive) ||
                        (id.TryGetComponent<BombardinoBoss>(out var aircraft) && aircraft.FightActive);
                    if (active && (id.transform.position - transform.position).sqrMagnitude < 170f * 170f)
                        combatUntil = Mathf.Max(combatUntil, Time.time + 1f);
                }
            }
            if (Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput ||
                (health && health.currentHealth <= 0) || (movement && (movement.ExternalControlLock || movement.IsDodging)) ||
                (attack && attack.IsCombatMotionActive) || currentStamina >= maxStamina ||
                Time.time < nextRegenerationTime)
                return;

            currentStamina = Mathf.MoveTowards(currentStamina, maxStamina,
                ActiveRegenerationRate * Time.deltaTime);
        }

        public bool CanSpend(float amount)
        {
            return amount <= 0f || currentStamina + 0.001f >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (!CanSpend(amount)) return false;

            currentStamina = Mathf.Max(0f, currentStamina - amount);
            nextRegenerationTime = Time.time + ActiveRegenerationDelay;
            return true;
        }

        /// <summary>Drains stamina while sprinting and resumes only after a small refill.</summary>
        public bool TickSprint(float deltaTime)
        {
            return TickSprint(deltaTime, sprintDrainPerSecond);
        }

        public bool TickSprint(float deltaTime, float drainPerSecond)
        {
            if (!sprinting)
            {
                if (currentStamina < sprintRestartThreshold) return false;
                sprinting = true;
            }

            float cost = SprintCost(deltaTime, drainPerSecond);
            if (currentStamina <= cost)
            {
                if (currentStamina > 0f)
                {
                    currentStamina = 0f;
                    nextRegenerationTime = Time.time + ActiveRegenerationDelay;
                }
                sprinting = false;
                return false;
            }

            currentStamina -= cost;
            nextRegenerationTime = Time.time + ActiveRegenerationDelay;
            return true;
        }

        public void StopSprinting()
        {
            sprinting = false;
        }
    }
}
