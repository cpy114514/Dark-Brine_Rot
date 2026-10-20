// PlayerHealth.cs
// Sahur needs an IDamageable surface for Nailong attacks to register. Kept small:
// holds HP, fires OnDeath once. Drop the script onto Sahur Player; defaults give
// 100 HP, which is generous compared to Nailong's 6-damage swings.
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    public class PlayerHealth : MonoBehaviour, IDamageable, ICombatHitResolver
    {
        [Tooltip("Max HP. Default 100 makes a couple of Nailong swings survivable but punishable.")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public UnityEvent OnDeath = new UnityEvent();
        // Story encounters can hand a lethal hit to the next scene instead of respawning.
        public System.Action NarrativeDefeatHandler { get; set; }
        // Only an active narrative encounter opts in. Ordinary combat still reaches zero.
        public float NarrativeHealthFloor { get; set; }
        public event System.Action<float> Damaged;
        float protectedUntil;
        ThirdPersonPlayerController movement;
        SahurAttack combat;
        SahurTwinBladeMotion rush;
        public bool IsProtected
        {
            get
            {
                // Loadout adds rush motion after Awake; resolve it once when it becomes available.
                if (!rush) TryGetComponent(out rush);
                return Time.time < protectedUntil || rush && rush.IsDamageImmune;
            }
        }
        public void GrantProtection(float seconds) => protectedUntil = Time.time + Mathf.Max(0f, seconds);

        void Awake()
        {
            movement = GetComponent<ThirdPersonPlayerController>();
            if (currentHealth <= 0f) currentHealth = maxHealth;
            combat = GetComponent<SahurAttack>();
            rush = GetComponent<SahurTwinBladeMotion>();
        }

        public bool ApplyMeleeDamage(float amount,Vector3 hitPoint,Vector3 attackerPosition) =>
            ApplyAttackDamage(amount,hitPoint,attackerPosition);

        // All hostile damage supplies an incoming origin. Generic hazards and narrative damage
        // retain ApplyDamage so their contact point cannot accidentally become an attack direction.
        public bool ApplyAttackDamage(float amount,Vector3 hitPoint,Vector3 incomingOrigin)
        {
            if(currentHealth<=0 || amount<=0 || IsProtected)return false;
            if(combat && combat.TryBlockIncoming(amount,hitPoint,incomingOrigin))return false;
            float before=currentHealth;
            ApplyDamage(amount,hitPoint);
            return currentHealth<before;
        }

        public CombatHitResult ResolveCombatHit(GameObject attacker,float amount,Vector3 point,CombatHitKind kind)
        {
            // CombatHitFeedback owns this path's single feedback event; avoid a second block event.
            if(combat && attacker && combat.TryBlockIncoming(amount,point,attacker.transform.position,false))
                return CombatHitResult.Blocked;
            ApplyDamage(amount,point);
            return kind==CombatHitKind.ChargedHeavy || kind==CombatHitKind.ComboFinisher || kind==CombatHitKind.JumpSlash
                ? CombatHitResult.HeavyHit : CombatHitResult.Hit;
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            if (currentHealth <= 0f || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) || IsProtected ||
                (movement != null && movement.DodgeInvulnerable)) return;
            float before = currentHealth;
            currentHealth = Mathf.Max(Mathf.Clamp(NarrativeHealthFloor, 0f, maxHealth), currentHealth - amount);
            Damaged?.Invoke(before - currentHealth);
            if (currentHealth < before) GameplayAudio.Play(GameplayAudio.Cue.Hurt, transform.position);
            if (currentHealth < before) GetComponent<PlayerStamina>()?.NotifyCombat();
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                if (NarrativeDefeatHandler != null) NarrativeDefeatHandler();
                else OnDeath.Invoke();
            }
        }
    }
}
