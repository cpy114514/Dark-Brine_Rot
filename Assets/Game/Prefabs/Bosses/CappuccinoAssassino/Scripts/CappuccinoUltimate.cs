using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent, RequireComponent(typeof(CappuccinoAI))]
    public sealed class CappuccinoUltimate : MonoBehaviour
    {
        public enum Phase { Ready, Empowered, Immobilized, Spent, Charging }
        [Header("One ultimate per encounter")]
        public bool ultimateEnabled = true;
        [Range(0f, 1f)] public float triggerHealthRatio = 0.5f;
        [Min(0.1f)] public float empoweredSeconds = 5f;
        [Min(0.1f)] public float immobilizedSeconds = 3f;
        [Min(1f)] public float healthMultiplier = 2f;
        [Min(1f)] public float damageMultiplier = 2f;
        // Legacy serialized flag. Combat state is conveyed by the boss animation.
        [HideInInspector] public bool showWarnings;
        [Min(.3f)] public float chargeSeconds = .95f;
        public Phase CurrentPhase { get; private set; }
        public bool MovementLocked => CurrentPhase == Phase.Immobilized || CurrentPhase == Phase.Charging;
        public bool AttackLocked => CurrentPhase == Phase.Charging || CurrentPhase == Phase.Immobilized;
        public bool IsEmpowered => CurrentPhase == Phase.Empowered;
        public float RemainingSeconds => Mathf.Max(0f, phaseEndsAt - lastTime);

        Health health;
        CappuccinoAI ai;
        float originalMaxHealth, originalDamage, phaseEndsAt, lastTime;
        bool attributesBoosted;
        void Awake()
        {
            health = GetComponent<Health>();
            ai = GetComponent<CappuccinoAI>();
            health.OnDeath ??= new UnityEngine.Events.UnityEvent();
            health.OnDeath.AddListener(OnDeath);
        }

        internal bool TryActivate(float now)
        {
            if (!ultimateEnabled || !isActiveAndEnabled || CurrentPhase != Phase.Ready || health == null ||
                health.IsDead || health.Ratio > triggerHealthRatio) return false;
            originalMaxHealth = health.maxHealth;
            originalDamage = ai.damage;
            CurrentPhase = Phase.Charging;
            phaseEndsAt = now + chargeSeconds;
            lastTime = now;
            return true;
        }

        void BeginEmpowerment()
        {
            health.maxHealth = originalMaxHealth * healthMultiplier;
            health.currentHealth = Mathf.Min(health.maxHealth, health.currentHealth * healthMultiplier);
            ai.damage = originalDamage * damageMultiplier;
            attributesBoosted = true;
            CurrentPhase = Phase.Empowered;
            phaseEndsAt += empoweredSeconds;
            health.OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
        }

        internal void Simulate(float now)
        {
            lastTime = now;
            if (health == null || health.IsDead) return;
            if (CurrentPhase == Phase.Charging && now >= phaseEndsAt) BeginEmpowerment();
            if (CurrentPhase == Phase.Empowered && now >= phaseEndsAt)
            {
                // Remove the temporary health multiplier without converting it into a second heal.
                RestoreAttributes(true);
                health.Heal(200f);
                CurrentPhase = Phase.Immobilized;
                phaseEndsAt += immobilizedSeconds;
            }
            if (CurrentPhase == Phase.Immobilized && now >= phaseEndsAt)
                CurrentPhase = Phase.Spent;
        }

        void RestoreAttributes(bool preserveRatio)
        {
            if (!attributesBoosted) return;
            float ratio = health.Ratio;
            health.maxHealth = originalMaxHealth;
            health.currentHealth = preserveRatio ? health.maxHealth * ratio : Mathf.Min(health.currentHealth, health.maxHealth);
            ai.damage = originalDamage;
            attributesBoosted = false;
        }

        void OnDeath()
        {
            RestoreAttributes(false);
            CurrentPhase = Phase.Spent;
        }

        internal void ResetEncounter()
        {
            RestoreAttributes(false);
            CurrentPhase = Phase.Ready;
        }

        void OnDisable()
        {
            if (attributesBoosted)
            {
                RestoreAttributes(true);
                health.OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            }
            if (CurrentPhase != Phase.Ready) CurrentPhase = Phase.Spent;
        }

        void OnDestroy()
        {
            if (health != null) health.OnDeath?.RemoveListener(OnDeath);
        }
    }
}
