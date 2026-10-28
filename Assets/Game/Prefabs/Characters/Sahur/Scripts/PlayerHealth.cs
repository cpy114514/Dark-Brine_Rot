// PlayerHealth.cs
// Hostile attacks share balance, recovery protection and confirmed received-hit
// feedback. Raw narrative/hazard damage retains the original authored values.
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
        float recoveryUntil;
        ThirdPersonPlayerController movement;
        SahurAttack combat;
        SahurTwinBladeMotion rush;
        public bool IsProtected
        {
            get
            {
                // Loadout adds rush motion after Awake; resolve it once when it becomes available.
                if (!rush) TryGetComponent(out rush);
                return Time.time < protectedUntil || !NarrativeDamageOwner && Time.time < recoveryUntil || rush && rush.IsDamageImmune;
            }
        }
        public void GrantProtection(float seconds) => protectedUntil = Mathf.Max(protectedUntil, Time.time + Mathf.Max(0f, seconds));
        public bool NarrativeDamageOwner => NarrativeHealthFloor>0 || NarrativeDefeatHandler!=null || movement && movement.ExternalControlLock;
        public bool CanReceiveCombatDamage => currentHealth>0 && !IsProtected && !(movement && movement.DodgeInvulnerable);
        public float RecoveryRemaining => Mathf.Max(0,recoveryUntil-Time.time);

        void Awake()
        {
            movement = GetComponent<ThirdPersonPlayerController>();
            if (currentHealth <= 0f) currentHealth = maxHealth;
            combat = GetComponent<SahurAttack>();
            rush = GetComponent<SahurTwinBladeMotion>();
            if(Application.isPlaying && !GetComponent<CombatReceivedReaction>())gameObject.AddComponent<CombatReceivedReaction>();
            if(Application.isPlaying && !GetComponent<CombatDamageFlash>())gameObject.AddComponent<CombatDamageFlash>();
        }

        public bool ApplyMeleeDamage(float amount,Vector3 hitPoint,Vector3 attackerPosition) =>
            ApplyAttackDamage(amount,hitPoint,attackerPosition);

        // All hostile damage supplies an incoming origin. Generic hazards and narrative damage
        // retain ApplyDamage so their contact point cannot accidentally become an attack direction.
        public bool ApplyAttackDamage(float amount,Vector3 hitPoint,Vector3 incomingOrigin)
        {
            return ReceiveAttack(amount,hitPoint,incomingOrigin,true,true);
        }

        bool ReceiveAttack(float amount,Vector3 hitPoint,Vector3 incomingOrigin,bool emitFeedback,bool block)
        {
            if(!CanReceiveCombatDamage || amount<=0 || float.IsNaN(amount) || float.IsInfinity(amount))return false;
            if(!combat)TryGetComponent(out combat);
            if(block && combat && combat.TryBlockIncoming(amount,hitPoint,incomingOrigin))return false;
            bool ordinary=!NarrativeDamageOwner;
            var profile=CombatFeelProfile.Default;
            if(ordinary)amount=Mathf.Min(amount*profile.incomingDamageMultiplier,maxHealth*profile.maximumHitHealthFraction);
            // Displayed combat damage and the actual health deduction use the
            // same whole-number result after mitigation, rather than 5.6 HP hits.
            if(ordinary)amount=Mathf.Max(1f,Mathf.Round(amount));
            float before=currentHealth;
            ApplyDamage(amount,hitPoint);
            if(currentHealth<before && ordinary)
            {
                recoveryUntil=Mathf.Max(recoveryUntil,Time.time+profile.damageRecoverySeconds);
                if(emitFeedback && Application.isPlaying)CombatHitFeedback.Received(this,before-currentHealth,hitPoint,incomingOrigin);
            }
            return currentHealth<before;
        }

        public CombatHitResult ResolveCombatHit(GameObject attacker,float amount,Vector3 point,CombatHitKind kind)
        {
            if(!combat)TryGetComponent(out combat);
            // CombatHitFeedback owns this path's single feedback event; avoid a second block event.
            if(combat && attacker && combat.TryBlockIncoming(amount,point,attacker.transform.position,false))
                return CombatHitResult.Blocked;
            ReceiveAttack(amount,point,attacker ? attacker.transform.position : point,false,false);
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
