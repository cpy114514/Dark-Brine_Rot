using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    public partial class SahurAttack
    {
        [Header("Combination attacks")]
        public Vector2 jumpChopHitWindow = new Vector2(.34f,.67f);
        [Min(0)] public float spinStaminaPerSecond = 9f;
        [Min(0)] public float spinEntryCost = 4f;
        [Min(0)] public float spinDamageMultiplier = .8f;
        [Min(0)] public float spinStartup = .18f;
        [Range(.05f,.3f)] public float specialBlend = .16f;
        // New field deliberately replaces the former 130-degree melee-only cone.
        [Range(30,180)] public float frontalGuardArc = 180f;
        [Min(0)] public float guardEntrySeconds = .16f;
        [Min(0)] public float guardHitCost = 12f;
        [Min(0)] public float guardDrainPerSecond = 2f;
        public bool IsSpinning { get; private set; }
        public bool IsGuarding { get; private set; }
        public bool GuardReady => IsGuarding && Time.time-specialStartedAt>=guardEntrySeconds;
        public int CompletedSpinTurns { get; private set; }
        float specialStartedAt;
        bool chordLatched;
        int spinTurn;
        static readonly int ClubSpinHash=Animator.StringToHash("Base Layer.Club Spin");
        static readonly int BladeSpinHash=Animator.StringToHash("Base Layer.Blade Spin");
        static readonly int BladeGuardHash=Animator.StringToHash("Base Layer.Blade Guard");

        bool ChordHeld => Mouse.current!=null && Mouse.current.leftButton.isPressed && Mouse.current.rightButton.isPressed;
        bool SpecialAllowed => !CinematicHandoffPending && animator && isActiveAndEnabled &&
            Cursor.lockState==CursorLockMode.Locked && !PauseSettingsMenu.IsOpen && !SahurLoadoutUI.BlocksInput &&
            Time.timeScale>0 && (!controller || controller.enabled && controller.CanUseGroundAttack &&
            !controller.ExternalControlLock && !controller.ExternalMovementLock && !controller.Swimming) &&
            !(boomerang && boomerang.IsBusy) && !(GetComponent<PlayerHealth>() is PlayerHealth hp && hp.currentHealth<=0);

        // Runs before charge release: a two-button chord can never release a heavy strike.
        bool UpdateCombinations()
        {
            if(!ChordHeld)chordLatched=false;
            if(!SpecialAllowed)
            {
                if(IsSpinning || IsGuarding)CancelForMovement();
                return false;
            }
            if(IsSpinning)
            {
                if(!ChordHeld || stamina && !stamina.TrySpend(spinStaminaPerSecond*Time.deltaTime))
                { EndCombination(); chordLatched=true; }
                return true;
            }
            if(IsGuarding)
            {
                if(!UsesTwinBlades || !GameInputSettings.Pressed(GameInputSettings.Action.Guard) ||
                    stamina && !stamina.TrySpend(guardDrainPerSecond*Time.deltaTime))EndCombination();
                return true;
            }
            bool canInterrupt=!attacking || CurrentAttackPhase<.22f || CurrentAttackPhase>=CurrentWindowEnd+.04f;
            if(ChordHeld && !chordLatched && canInterrupt)
            {
                int hash=UsesTwinBlades?BladeSpinHash:ClubSpinHash;
                if(!animator.HasState(0,hash))return false;
                chordLatched=true;
                if(stamina && !stamina.TrySpend(spinEntryCost))return true;
                CancelForMovement(); IsSpinning=true; specialStartedAt=Time.time;spinTurn=0;CompletedSpinTurns=0;
                comboStep=-1; BeginAttack(hash,damage*spinDamageMultiplier,0,specialBlend);
                return true;
            }
            if(UsesTwinBlades && GameInputSettings.Pressed(GameInputSettings.Action.Guard) && canInterrupt &&
                animator.HasState(0,BladeGuardHash) && (!stamina || stamina.CanSpend(guardHitCost)))
            {
                CancelForMovement(); IsGuarding=true;specialStartedAt=Time.time;controller?.BeginAttackFacing();
                animator.CrossFadeInFixedTime(BladeGuardHash,specialBlend,0,0);return true;
            }
            // Suppress leftover LMB callbacks until both-button input is released.
            return chordLatched && ChordHeld;
        }
        void EndCombination()
        {
            CancelForMovement();
            if(animator)animator.CrossFadeInFixedTime("Base Layer.Locomotion",specialBlend,0,0);
            lastFireTime=Time.time-cooldown+.08f;
        }
        void ResetCombinations(){IsSpinning=false;IsGuarding=false;}
        bool UpdateSpinHitbox()
        {
            if(!IsSpinning)return false;
            if(!stickHitbox || !animator)return true;
            var state=animator.GetCurrentAnimatorStateInfo(0);
            if(animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash==activeAttackHash)
                state=animator.GetNextAnimatorStateInfo(0);
            bool pose=state.fullPathHash==activeAttackHash;
            int turn=Mathf.FloorToInt(state.normalizedTime);
            if(pose && turn>spinTurn)
            {CompletedSpinTurns+=turn-spinTurn;spinTurn=turn;hitThisSwing.Clear();swishPlayed=false;}
            bool active=pose && Time.time-specialStartedAt>=spinStartup;
            stickHitbox.enabled=active;if(secondBlade)secondBlade.enabled=active;
            trailActive=active;
            if(active)
            {
                Physics.SyncTransforms();ScanHits();
                if(!swishPlayed){swishPlayed=true;if(swingAudio && activeSwish)swingAudio.PlayOneShot(activeSwish);}
            }
            else hasWeaponSample=false;
            return true;
        }
        public bool TryBlockIncoming(float amount,Vector3 point,Vector3 source,bool emitFeedback=true)
        {
            if(!GuardReady || !SpecialAllowed || amount<=0)return false;
            Vector3 toSource=Vector3.ProjectOnPlane(source-transform.position,Vector3.up);
            if(toSource.sqrMagnitude<.001f || Vector3.Angle(transform.forward,toSource)>frontalGuardArc*.5f+.001f)return false;
            // Every frontal attack is absorbed, regardless of damage or attack category.
            // The last block can exhaust the guard, but the blocked hit never leaks through.
            if(stamina && !stamina.TrySpend(guardHitCost))
            {
                stamina.TrySpend(stamina.Current);
                EndCombination();
            }
            if(emitFeedback)CombatHitFeedback.Block(gameObject,point,-toSource.normalized);
            return true;
        }
    }
}
