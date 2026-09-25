using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    // Uses the same damage contract as Sahur's stick attack.
    public sealed class NailongHealth : MonoBehaviour, IDamageable
    {
        [Min(1f)] public float maxHealth = 160f;
        public float currentHealth = 160f;
        public UnityEvent OnDeath = new UnityEvent();

        public event Action<float> Damaged;
        public bool IsDead => currentHealth <= 0f;
        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Damaged?.Invoke(amount);
            if (IsDead) OnDeath.Invoke();
        }
    }
}
