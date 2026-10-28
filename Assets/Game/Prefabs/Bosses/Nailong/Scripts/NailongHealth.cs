using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    // Uses the same damage contract as Sahur's stick attack.
    public sealed class NailongHealth : MonoBehaviour, IDamageable
    {
        [Min(1f)] public float maxHealth = 500f;
        public float currentHealth = 500f;
        public UnityEvent OnDeath = new UnityEvent();

        bool deathNotified;
        public event Action<float> Damaged;
        public bool IsDead => currentHealth <= 0f;
        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        void Awake()
        {
            FirstIslandBossProgression.Register(this);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            if (GetComponent<NailongHealthBar>() == null) gameObject.AddComponent<NailongHealthBar>();
            if (GetComponent<NailongLoot>() == null) gameObject.AddComponent<NailongLoot>();
            if (GetComponent<NailongHitReaction>() == null) gameObject.AddComponent<NailongHitReaction>();
            if (Application.isPlaying && !GetComponent<CombatDamageFlash>())gameObject.AddComponent<CombatDamageFlash>();
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            ApplyCombatDamage(amount, hitPoint, CombatHitKind.ComboOne);
        }

        public void ApplyCombatDamage(float amount, Vector3 hitPoint, CombatHitKind kind)
        {
            if (IsDead || amount <= 0f || !FirstIslandBossProgression.CanChallenge(this, FirstIslandObjectives.BossKind.Nailong)) return;
            amount=Mathf.Max(1f,Mathf.Round(amount));
            deathNotified=false;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Damaged?.Invoke(amount);
            if (!IsDead)
            {
                GetComponent<NailongHitReaction>()?.Hit(hitPoint, kind);
                if (kind == CombatHitKind.ChargedHeavy || kind == CombatHitKind.JumpSlash)
                    GetComponent<NailongAI>()?.Interrupt(0.6f);
            }
            if (IsDead && !deathNotified)
            {
                deathNotified=true;
                GameWorldProgress.Record(this, 0);
                GetComponent<NailongLoot>()?.Drop();
                OnDeath.Invoke();
            }
        }
    }
}
