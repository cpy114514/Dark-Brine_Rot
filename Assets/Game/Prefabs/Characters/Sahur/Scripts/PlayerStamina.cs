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
        [Min(0f)] public float regenerationPerSecond = 30f;
        [Min(0f)] public float regenerationDelay = 0.55f;

        [Header("Movement costs")]
        [Min(0f)] public float sprintDrainPerSecond = 8f;
        [Min(0f)] public float sprintRestartThreshold = 4f;
        [Min(0f)] public float jumpCost = 6f;
        [Min(0f)] public float rollCost = 10f;
        [Min(0f)] public float airFlipCost = 8f;

        [Header("Attack costs")]
        [Min(0f)] public float lightAttackCost = 5f;
        [Min(0f)] public float jumpSlashCost = 8f;
        [Min(0f)] public float chargedAttackCost = 16f;

        float nextRegenerationTime;
        bool sprinting;

        public float Current => Mathf.Clamp(currentStamina, 0f, maxStamina);
        public float Maximum => Mathf.Max(1f, maxStamina);
        public float Normalized => Current / Maximum;

        void Awake()
        {
            maxStamina = Mathf.Max(1f, maxStamina);
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            nextRegenerationTime = Time.time;
        }

        void Update()
        {
            if (PauseSettingsMenu.IsOpen || currentStamina >= maxStamina ||
                Time.time < nextRegenerationTime)
                return;

            currentStamina = Mathf.MoveTowards(currentStamina, maxStamina,
                regenerationPerSecond * Time.deltaTime);
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
            nextRegenerationTime = Time.time + regenerationDelay;
            return true;
        }

        /// <summary>Drains stamina while sprinting and resumes only after a small refill.</summary>
        public bool TickSprint(float deltaTime)
        {
            if (!sprinting)
            {
                if (currentStamina < sprintRestartThreshold) return false;
                sprinting = true;
            }

            float cost = sprintDrainPerSecond * deltaTime;
            if (currentStamina <= cost)
            {
                if (currentStamina > 0f)
                {
                    currentStamina = 0f;
                    nextRegenerationTime = Time.time + regenerationDelay;
                }
                sprinting = false;
                return false;
            }

            currentStamina -= cost;
            nextRegenerationTime = Time.time + regenerationDelay;
            return true;
        }

        public void StopSprinting()
        {
            sprinting = false;
        }
    }
}
