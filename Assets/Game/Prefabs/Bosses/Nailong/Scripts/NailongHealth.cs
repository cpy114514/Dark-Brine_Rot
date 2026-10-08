using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    // Uses the same damage contract as Sahur's stick attack.
    public sealed class NailongHealth : MonoBehaviour, IDamageable
    {
        [Min(1f)] public float maxHealth = 300f;
        public float currentHealth = 300f;
        public UnityEvent OnDeath = new UnityEvent();

        public event Action<float> Damaged;
        public bool IsDead => currentHealth <= 0f;
        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            if (GetComponent<NailongHealthBar>() == null) gameObject.AddComponent<NailongHealthBar>();
            if (GetComponent<NailongLoot>() == null) gameObject.AddComponent<NailongLoot>();
            if (GetComponent<NailongHitReaction>() == null) gameObject.AddComponent<NailongHitReaction>();
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            ApplyCombatDamage(amount, hitPoint, CombatHitKind.ComboOne);
        }

        public void ApplyCombatDamage(float amount, Vector3 hitPoint, CombatHitKind kind)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Damaged?.Invoke(amount);
            if (!IsDead)
            {
                GetComponent<NailongHitReaction>()?.Hit(hitPoint, kind);
                if (kind == CombatHitKind.ChargedHeavy || kind == CombatHitKind.JumpSlash)
                    GetComponent<NailongAI>()?.Interrupt(0.6f);
            }
            if (IsDead)
            {
                GetComponent<NailongLoot>()?.Drop();
                OnDeath.Invoke();
            }
        }
    }
}
