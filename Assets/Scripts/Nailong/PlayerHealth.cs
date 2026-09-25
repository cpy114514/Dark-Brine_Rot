// PlayerHealth.cs
// Sahur needs an IDamageable surface for Nailong attacks to register. Kept small:
// holds HP, fires OnDeath once. Drop the script onto Sahur Player; defaults give
// 100 HP, which is generous compared to Nailong's 6-damage swings.
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Tooltip("Max HP. Default 100 makes a couple of Nailong swings survivable but punishable.")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public UnityEvent OnDeath = new UnityEvent();

        void Awake()
        {
            if (currentHealth <= 0f) currentHealth = maxHealth;
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            if (currentHealth <= 0f) return;
            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                OnDeath.Invoke();
            }
        }
    }
}