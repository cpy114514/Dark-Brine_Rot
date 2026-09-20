// Health.cs
// Generic HP component. Implements IDamageable so it works with the
// existing SahurAttack.cs hitbox pipeline (which checks "Enemy" tag
// and then GetComponentInParent<IDamageable>()).
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Current health at scene start. Clamped to max on Awake.")]
        public float currentHealth = 100f;

        [Tooltip("Optional VFX/SFX hook fired on each hit (after damage applied).")]
        public UnityEvent<float> OnDamaged;
        [Tooltip("Fires once when health reaches 0.")]
        public UnityEvent OnDeath;
        [Tooltip("(current, max) — useful for health bars.")]
        public UnityEvent<float, float> OnHealthChanged;

        public bool IsDead => currentHealth <= 0f;
        public float Ratio => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnDamaged?.Invoke(amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0f) OnDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Kill()
        {
            ApplyDamage(currentHealth + 1f, transform.position);
        }
    }
}