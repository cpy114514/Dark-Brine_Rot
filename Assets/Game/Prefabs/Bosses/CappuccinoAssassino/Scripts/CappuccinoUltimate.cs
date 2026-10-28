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
        // Retained for old serialized scenes; health and damage no longer multiply.
        [HideInInspector] public float healthMultiplier = 1f;
        [HideInInspector] public float damageMultiplier = 1f;
        [HideInInspector] public bool showWarnings;
        [Min(.3f)] public float chargeSeconds = .95f;
        [Min(0f)] public float healingAmount = 200f;
        public Phase CurrentPhase { get; private set; }
        public bool HasHealed { get; private set; }
        public bool MovementLocked => CurrentPhase == Phase.Immobilized || CurrentPhase == Phase.Charging;
        public bool AttackLocked => CurrentPhase == Phase.Charging || CurrentPhase == Phase.Immobilized;
        public bool IsEmpowered => CurrentPhase == Phase.Empowered;
        public float RemainingSeconds => Mathf.Max(0f, phaseEndsAt - lastTime);

        Health health;
        float phaseEndsAt, lastTime;
        void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath ??= new UnityEngine.Events.UnityEvent();
            health.OnDeath.AddListener(OnDeath);
        }
        internal bool TryActivate(float now)
        {
            if (!ultimateEnabled || !isActiveAndEnabled || CurrentPhase != Phase.Ready || health == null ||
                health.IsDead || health.Ratio > triggerHealthRatio) return false;
            CurrentPhase = Phase.Charging;
            phaseEndsAt = now + chargeSeconds;
            lastTime = now;
            return true;
        }
        internal void Simulate(float now)
        {
            lastTime = now;
            if (health == null || health.IsDead) return;
            if (CurrentPhase == Phase.Charging && now >= phaseEndsAt)
            {
                CurrentPhase = Phase.Empowered;
                phaseEndsAt += empoweredSeconds;
            }
            if (CurrentPhase == Phase.Empowered && now >= phaseEndsAt)
            {
                health.Heal(healingAmount);
                HasHealed = true;
                CurrentPhase = Phase.Immobilized;
                phaseEndsAt += immobilizedSeconds;
            }
            if (CurrentPhase == Phase.Immobilized && now >= phaseEndsAt)
                CurrentPhase = Phase.Spent;
        }
        void OnDeath() => CurrentPhase = Phase.Spent;
        internal void ResetEncounter()
        {
            CurrentPhase = Phase.Ready;
            HasHealed = false;
        }
        void OnDisable()
        {
            if (CurrentPhase != Phase.Ready) CurrentPhase = Phase.Spent;
        }
        void OnDestroy()
        {
            if (health != null) health.OnDeath?.RemoveListener(OnDeath);
        }
    }
}
