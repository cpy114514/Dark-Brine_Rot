using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    /// <summary>Ground combo, charged swing, and a once-per-jump aerial chop.</summary>
    public partial class SahurAttack : MonoBehaviour
    {
        [Header("Animation")]
        public Animator animator;
        public string attackTrigger = "Attack";
        public string chargeState = "Charge Windup";
        public string heavyAttackState = "Heavy Attack";
        public string chargeTimeParameter = "ChargePhase";
        public AnimationClip chargeClip;
        [Tooltip("Optional separate authored attack clip played from its start on charge release.")]
        public AnimationClip chargedStrikeClip;
        [Header("Combat feel")]
        public CombatFeelProfile feel;
        CombatFeelProfile Feel => feel ? feel : CombatFeelProfile.Default;
        CombatHitFeedback impactFeedback;
        CombatLocalHitStop localStop;
        static readonly int LightPlaybackSpeedHash=Animator.StringToHash("LightPlaybackSpeed");
        bool hasLightPlaybackSpeed;
        Vector3 swingDirection;
        bool trailActive;
        int lightLayerHandoffFrame=-1;
        float lightLayerHandoffPhase;
        float comboInputExpiresAt;
        float dodgeBufferedUntil=-1;
        static readonly Unity.Profiling.ProfilerMarker SweepMarker=new Unity.Profiling.ProfilerMarker("Sahur.WeaponSweep");
        Vector3 queryWeaponA,queryWeaponB;
        public bool TryConsumeDodgeCancel(bool pressed)
        {
            if(CinematicHandoffPending || !isActiveAndEnabled || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput)return false;
            if(pressed && (IsGuarding || IsSpinning && Time.time-specialStartedAt>=spinStartup)){EndCombination();return true;}
            if(pressed && IsCombatMotionActive)dodgeBufferedUntil=Time.unscaledTime+Feel.inputBuffer;
            if(Time.unscaledTime>dodgeBufferedUntil || (localStop && localStop.IsStopped))return false;
            if(!IsCombatMotionActive){dodgeBufferedUntil=-1;return true;}
            float phase=CurrentAttackPhase;
            if(!charging && (!attacking || phase<Mathf.Max(CurrentWindowEnd+.02f,IsHeavyAttackActive?Feel.heavyDodgeCancel:Feel.lightDodgeCancel)))return false;
            CancelForMovement();dodgeBufferedUntil=-1;return true;
        }
        const string ChargeUpperBodyLayer = "Charge Upper Body";
        const string LightUpperBodyLayer = "Light Attack Upper Body";
        const float ChargeLayerFadeIn = 0.10f;
        const float ChargeLayerFadeOut = 0.18f;
        // Legacy base-layer fallback only; the moving charge uses the extracted clip.
        public const float SafeChargePoseTime = 0.18f;
        [Range(0f, SafeChargePoseTime)] public float chargePoseTime = SafeChargePoseTime;

        [Header("Left mouse combo")]
        public string comboOneState = "Combo 1";
        public string comboTwoState = "Combo 2";
        public string comboThreeState = "Combo 3";
        [Range(0f, 1f)] public float comboQueueOpen = 0.12f;
        [Range(0f, 1f)] public float comboChainPoint = 0.98f;
        [Range(0f, 1f)] public float comboQueueClose = 0.96f;
        [Range(0.01f, 0.2f)] public float comboBlendTime = 0.06f;
        [Min(0f)] public float comboTwoDamageMultiplier = 1.2f;
        [Min(0f)] public float comboThreeDamageMultiplier = 1.6f;
        public Vector2 comboOneHitWindow = new Vector2(0.57f, 0.86f);
        public Vector2 comboTwoHitWindow = new Vector2(0.12f, 0.34f);
        public Vector2 comboThreeHitWindow = new Vector2(0.32f, 0.55f);

        [Header("Air jump slash")]
        public string jumpSlashState = "Jump Slash";
        [Min(1f)] public float jumpSlashDamageMultiplier = 1.7f;
        public Vector2 jumpSlashHitWindow = new Vector2(0.34f, 0.67f);
        [Min(0f)] public float landingImpactRadius = 1.35f;
        [Min(0f)] public float landingImpactForwardOffset = 0.55f;

        [Header("Hit detection")]
        [Tooltip("Trigger collider child that covers the swing arc; auto-disabled outside attack windows.")]
        public Collider stickHitbox;
        public LayerMask hitMask = ~0;
        public string enemyTag = "Enemy";
        public float damage = 25f;
        [Tooltip("Ground combo reach in metres at Sahur's usual scale (3).")]
        [Min(0f)] public float comboReach = 4.8f;
        [Range(30f, 180f)] public float comboArc = 150f;
        [Min(0f)] public float weaponHitPadding = .55f;
        [Range(0f, 1f)] public float swingWindowStart = 0.10f;
        [Range(0f, 1f)] public float swingWindowEnd = 0.55f;
        [Range(0f, 1f)] public float heavySwingWindowStart = 0.25f;
        [Range(0f, 1f)] public float heavySwingWindowEnd = 0.69f;
        [Min(0f)] public float cooldown = 0.5f;

        [Header("Right mouse charge")]
        [Min(0.2f)] public float fullChargeTime = 1.8f;
        [Header("Twin blade rush")]
        [Min(0.2f)] public float twinBladeMinimumCharge = 1.2f;
        [Min(0.2f)] public float twinBladeFullCharge = 2.6f;
        [Min(0.05f)] public float twinBladeRaiseTime = .9f;
        [Tooltip("Fade the held X pose into the existing rush or locomotion before the damaging cut starts.")]
        [Range(.04f,.14f)] public float twinBladePoseReleaseBlend = .12f;
        bool fadingTwinBladeCharge;
        public static readonly Vector2 TwinBladeRushWindow = new Vector2(.20f, .56f);
        public float EffectiveFullChargeTime => UsesTwinBlades ? Mathf.Max(twinBladeMinimumCharge, twinBladeFullCharge) : Mathf.Max(.2f, fullChargeTime);
        public bool ChargeReady => charging && (!UsesTwinBlades || ChargeElapsed >= twinBladeMinimumCharge);
        [Tooltip("Time to raise the weapon into its held pose. Damage keeps charging until fullChargeTime.")]
        [Min(0.05f)] public float chargeWindupTime = 0.42f;
        [Tooltip("Normalized end of the authored windup in the complete heavy clip. Holding freezes here; damage still reaches full charge.")]
        [Range(0.1f, 1f)] public float maxChargePosePhase = 0.55f;
        [Min(1f)] public float minChargeDamageMultiplier = 1.25f;
        [Min(1f)] public float maxChargeDamageMultiplier = 2.2f;

        [Header("Swing audio")]
        public AudioSource swingAudio;
        public AudioClip[] comboSwishes = new AudioClip[3];
        public AudioClip heavySwish;

        public bool IsCombatMotionActive => IsGuarding || charging || attacking || (boomerang != null && boomerang.IsThrowing);
        public bool CinematicHandoffPending { get; set; }
        public bool IsGroundComboActive => attacking && comboStep >= 0;
        public bool CanSteerGroundCombo
        {
            get
            {
                if (!IsGroundComboActive || animator == null) return false;
                int layer=AttackAnimationLayer;
                var state = animator.GetCurrentAnimatorStateInfo(layer);
                if (animator.IsInTransition(layer) && animator.GetNextAnimatorStateInfo(layer).fullPathHash == AttackAnimationHash)
                    state = animator.GetNextAnimatorStateInfo(layer);
                if (state.fullPathHash != AttackAnimationHash) return false;
                Vector2 window = comboStep == 0 ? comboOneHitWindow : comboStep == 1 ? comboTwoHitWindow : comboThreeHitWindow;
                // Steering belongs to anticipation and recovery, not the damaging arc.
                return state.normalizedTime < window.x || state.normalizedTime > window.y;
            }
        }
        public bool IsHeavyAttackActive => attacking && activeAttackHash == heavyStateHash;
        public bool CanDodgeCancel => attacking && !charging && !(localStop && localStop.IsStopped) &&
            CurrentAttackPhase >= Mathf.Max(CurrentWindowEnd+.02f,IsHeavyAttackActive?Feel.heavyDodgeCancel:Feel.lightDodgeCancel);
        public bool TryDodgeCancel() => CanDodgeCancel && TryConsumeDodgeCancel(true);
        public bool CanMoveDuringCombat => charging || usesUpperBodyLight || (IsHeavyAttackActive && usesRightArmHeavy);
        public bool UsesAnimationRootMotion => attacking && !IsSpinning && !(UsesTwinBlades && IsHeavyAttackActive) && activeAttackHash != jumpSlashStateHash && !usesRightArmHeavy && !usesUpperBodyLight;
        public bool SafeForCinematic => !IsSpinning && !IsGuarding && (!IsCombatMotionActive || (attacking && !charging && CurrentAttackPhase>=CurrentWindowEnd+.04f));
        public float CurrentAttackPhase
        {
            get
            {
                if(!animator || !attacking)return 0;
                if(lightLayerHandoffFrame==Time.frameCount)return lightLayerHandoffPhase;
                int layer=AttackAnimationLayer;
                if(animator.IsInTransition(layer))
                {
                    var incoming=animator.GetNextAnimatorStateInfo(layer);
                    if(incoming.fullPathHash==AttackAnimationHash)return incoming.normalizedTime;
                }
                var current=animator.GetCurrentAnimatorStateInfo(layer);
                // A freshly requested CrossFade may not have been evaluated yet.
                // Locomotion/charge time cannot authorize recovery cancels.
                return current.fullPathHash==AttackAnimationHash ? current.normalizedTime : 0;
            }
        }
        Vector2 CurrentDamageWindow => activeAttackHash==jumpSlashStateHash ? (UsesTwinBlades?jumpSlashHitWindow:jumpChopHitWindow) :
            comboStep==0 ? comboOneHitWindow : comboStep==1 ? comboTwoHitWindow : comboStep==2 ? comboThreeHitWindow :
            IsHeavyAttackActive ? UsesTwinBlades ? TwinBladeRushWindow : new Vector2(heavySwingWindowStart,heavySwingWindowEnd) :
            new Vector2(swingWindowStart,swingWindowEnd);
        float CurrentWindowEnd => CurrentDamageWindow.y;
        public int CurrentComboStage => IsGroundComboActive ? comboStep : -1;
        public bool IsCharging => charging;
        public int ChargedAttacksPerformed { get; private set; }
        public bool UsesTwinBlades { get; private set; }
        Collider originalStickHitbox, twinRight, twinLeft, secondBlade;
        AnimationClip originalChargeClip, originalChargedStrikeClip;
        Vector2 originalWindowOne, originalWindowTwo, originalWindowThree, originalJumpWindow;
        float originalReach, originalChargePhase, originalArc;
        public float ChargeElapsed => charging ? Mathf.Max(0f, Time.time - chargeStartedAt) : 0f;
        bool capturedWeaponSettings;

        public void SetTwinBladeMode(bool useBlades, Collider right, Collider left)
        {
            if (!capturedWeaponSettings)
            {
                originalStickHitbox = stickHitbox;
                originalChargeClip = chargeClip; originalChargedStrikeClip = chargedStrikeClip;
                originalWindowOne = comboOneHitWindow; originalWindowTwo = comboTwoHitWindow; originalWindowThree = comboThreeHitWindow;
                originalJumpWindow = jumpSlashHitWindow; originalReach = comboReach; originalChargePhase = maxChargePosePhase; originalArc = comboArc;
                capturedWeaponSettings = true;
            }
            CancelForMovement();
            if (twinLeft) twinLeft.enabled = false;
            if (twinRight) twinRight.enabled = false;
            UsesTwinBlades = useBlades;
            twinRight = right; twinLeft = left; secondBlade = useBlades ? left : null;
            stickHitbox = useBlades ? right : originalStickHitbox;
            for (int i = 0; i < 3; i++) comboStateHashes[i] = Animator.StringToHash("Base Layer." +
                (useBlades ? "Capri Combo " + (i + 1) : i == 0 ? comboOneState : i == 1 ? comboTwoState : comboThreeState));
            heavyStateHash = Animator.StringToHash("Base Layer." + (useBlades ? "Capri Heavy" : heavyAttackState));
            chargeStateHash = Animator.StringToHash("Base Layer." + (useBlades ? "Capri Charge" : chargeState));
            upperBodyChargeStateHash=Animator.StringToHash(useBlades?"Twin Blade Charge.Capri Charge":ChargeUpperBodyLayer+"."+chargeState);
            jumpSlashStateHash = Animator.StringToHash("Base Layer." + (useBlades ? "Capri Jump Slash" : jumpSlashState));
            chargeClip = useBlades ? Resources.Load<AnimationClip>("Combat/Combinations/BladeCharge") : originalChargeClip;
            chargedStrikeClip = useBlades ? Resources.Load<AnimationClip>("Combat/Combinations/BladeRushV002") : originalChargedStrikeClip;
            comboOneHitWindow = useBlades ? new Vector2(.28f,.62f) : originalWindowOne;
            comboTwoHitWindow = useBlades ? new Vector2(.28f,.62f) : originalWindowTwo;
            comboThreeHitWindow = useBlades ? new Vector2(.28f,.74f) : originalWindowThree;
            jumpSlashHitWindow = useBlades ? new Vector2(.34f,.67f) : originalJumpWindow;
            comboReach = useBlades ? 5.4f : originalReach;
            comboArc = useBlades ? 150f : originalArc;
            maxChargePosePhase = useBlades ? 1f : originalChargePhase;
            if (stickHitbox) stickHitbox.enabled = false;
        }
        public float Charge01 => charging ? Mathf.Clamp01(ChargeElapsed / EffectiveFullChargeTime) : 0f;
        public float ChargePose01 => Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(ChargeElapsed / Mathf.Max(0.05f, UsesTwinBlades ? twinBladeRaiseTime : fullChargeTime))) * Mathf.Clamp01(maxChargePosePhase);
        public float HeavyAttackPhase
        {
            get
            {
                if (!IsHeavyAttackActive || animator == null) return 0f;
                int layer = AttackAnimationLayer;
                var state = animator.GetCurrentAnimatorStateInfo(layer);
                if (animator.IsInTransition(layer) && animator.GetNextAnimatorStateInfo(layer).fullPathHash == AttackAnimationHash)
                    state = animator.GetNextAnimatorStateInfo(layer);
                return state.fullPathHash == AttackAnimationHash ? state.normalizedTime : 0f;
            }
        }

        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        Collider[] nearbyColliders = new Collider[64];
        RaycastHit[] strikeObstacles = new RaycastHit[32];
        Vector3 previousWeaponA, previousWeaponB;
        Vector3 previousSecondA, previousSecondB;
        bool hasWeaponSample;
        float previousHitPhase;
        float observedAttackPhase=-1f;
        Vector3 observedA,observedB,observedSecondA,observedSecondB;
        readonly int[] comboStateHashes = new int[3];
        ThirdPersonPlayerController controller;
        CharacterController strikeController;
        PlayerStamina stamina;
        bool attackQueued;
        bool comboContinueQueued;
        bool charging;
        bool attacking;
        bool activeAttackEntered;
        bool airAttackUsed;
        float jumpAttackBufferedUntil = -1f;
        bool airImpactArmed;
        int comboStep = -1;
        int lastAttackInputFrame = -1;
        int movementCancelFrame = -1;
        float queueExpiresAt;
        float chargeStartedAt;
        float attackStartedAt;
        float lastFireTime = -10f;
        float swingDamage;
        int lightStateHash;
        int heavyStateHash;
        int chargeStateHash;
        int upperBodyChargeStateHash;
        int rightArmHeavyStateHash;
        bool usesRightArmHeavy;
        bool usesUpperBodyLight;
        readonly int[] upperComboHashes=new int[3];
        static readonly int HeavyPlaybackSpeedHash = Animator.StringToHash("HeavyPlaybackSpeed");
        bool hasHeavyPlaybackSpeed;
        int AttackAnimationLayer => usesUpperBodyLight ? lightLayerIndex : usesRightArmHeavy ? chargeLayerIndex : 0;
        int AttackAnimationHash => usesRightArmHeavy ? rightArmHeavyStateHash : usesUpperBodyLight && comboStep>=0 ? upperComboHashes[comboStep] : activeAttackHash;
        int chargeLayerIndex = -1;
        int lightLayerIndex = -1;
        bool usesUpperBodyCharge;
        int chargeTimeHash;
        int jumpSlashStateHash;
        int activeAttackHash;
        AudioClip activeSwish;
        bool swishPlayed;
        SahurBoomerang boomerang;

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == "Sahur Stick")
                {
                    stickHitbox = child.Find("Stick Hitbox")?.GetComponent<Collider>() ??
                                  child.GetComponent<Collider>();
                    break;
                }
        }

        void Awake()
        {
            controller = GetComponent<ThirdPersonPlayerController>();
            strikeController = GetComponent<CharacterController>();
            stamina = GetComponent<PlayerStamina>();
            lightStateHash = Animator.StringToHash("Base Layer." + attackTrigger);
            heavyStateHash = Animator.StringToHash("Base Layer." + heavyAttackState);
            chargeStateHash = Animator.StringToHash("Base Layer." + chargeState);
            upperBodyChargeStateHash = Animator.StringToHash(ChargeUpperBodyLayer + "." + chargeState);
            rightArmHeavyStateHash = Animator.StringToHash(ChargeUpperBodyLayer + "." + heavyAttackState);
            chargeTimeHash = Animator.StringToHash(chargeTimeParameter);
            jumpSlashStateHash = Animator.StringToHash("Base Layer." + jumpSlashState);
            comboStateHashes[0] = Animator.StringToHash("Base Layer." + comboOneState);
            comboStateHashes[1] = Animator.StringToHash("Base Layer." + comboTwoState);
            comboStateHashes[2] = Animator.StringToHash("Base Layer." + comboThreeState);
            upperComboHashes[0]=Animator.StringToHash(LightUpperBodyLayer+"."+comboOneState);
            upperComboHashes[1]=Animator.StringToHash(LightUpperBodyLayer+"."+comboTwoState);
            upperComboHashes[2]=Animator.StringToHash(LightUpperBodyLayer+"."+comboThreeState);
            if (swingAudio == null) swingAudio = GetComponent<AudioSource>();
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null)
                foreach (var parameter in animator.parameters)
                    if (parameter.nameHash == HeavyPlaybackSpeedHash && parameter.type == AnimatorControllerParameterType.Float)
                        hasHeavyPlaybackSpeed = true;
            boomerang = GetComponent<SahurBoomerang>();
            if (boomerang == null) boomerang = gameObject.AddComponent<SahurBoomerang>();
            impactFeedback=GetComponent<CombatHitFeedback>();
            if(!impactFeedback)impactFeedback=gameObject.AddComponent<CombatHitFeedback>();
            impactFeedback.profile=Feel;
            localStop=GetComponent<CombatLocalHitStop>();
            if(animator)foreach(var p in animator.parameters)
                if(p.nameHash==LightPlaybackSpeedHash)hasLightPlaybackSpeed=true;
        }

        void Update()
        {
            if(UpdateCombinations())return;
            // Check before animation progress, charge release or damage. The
            // movement controller also checks so script order cannot delay it.
            if (TryCancelForMovementInput()) return;
            if (boomerang != null && boomerang.IsBusy)
            {
                attackQueued = comboContinueQueued = false;
                if (stickHitbox != null) stickHitbox.enabled = false;
                return;
            }
            if (controller != null && controller.Swimming)
            {
                SuspendForSwimming();
                return;
            }
            if (PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput)
            {
                jumpAttackBufferedUntil = -1f;
                attackQueued = false;
                comboContinueQueued = false;
                if (charging) CancelCharge();
                if (stickHitbox != null) stickHitbox.enabled = false;
                return;
            }

            if (jumpAttackBufferedUntil >= 0f && Time.time > jumpAttackBufferedUntil)
                jumpAttackBufferedUntil = -1f;
            if (jumpAttackBufferedUntil >= 0f && controller != null && controller.CanUseAirAttack &&
                !attacking && !charging && !airAttackUsed && !CinematicHandoffPending)
            {
                jumpAttackBufferedUntil = -1f;
                StartJumpSlash();
            }
            if (airImpactArmed && controller != null && controller.CanUseGroundAttack)
            {
                airImpactArmed = false;
                ApplyLandingImpact();
            }
            if (controller != null && controller.CanUseGroundAttack)
                airAttackUsed = false;

            UpdateAttackProgress();
            UpdateLightPlayback();
            var mouse = Mouse.current;
            bool gameHasFocus = Cursor.lockState == CursorLockMode.Locked;
            if (charging && (!gameHasFocus || mouse == null ||
                             (controller != null && !controller.CanUseGroundAttack)))
                CancelCharge();
            else if (charging && !mouse.rightButton.isPressed)
                ReleaseCharge();
            if (charging) UpdateChargePose();
            else if (!CinematicHandoffPending && !charging && !attacking && mouse != null && gameHasFocus &&
                     mouse.rightButton.wasPressedThisFrame && Time.time - lastFireTime >= cooldown &&
                     (controller == null || controller.CanUseGroundAttack))
                StartCharge();

            if (attackQueued && Time.time > queueExpiresAt)
                attackQueued = false;
            if (!CinematicHandoffPending && attackQueued && !charging && !attacking && Time.time - lastFireTime >= cooldown &&
                (controller == null || controller.CanUseGroundAttack))
            {
                attackQueued = false;
                StartLightAttack();
            }

            UpdateChargeLayerWeight();
        }

        void LateUpdate()
        {
            if (secondBlade) secondBlade.enabled = false;
            // Sample the pose Unity rendered this frame, after Animator/root motion.
            if (PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput ||
                (controller != null && controller.Swimming))
            {
                if (stickHitbox != null) stickHitbox.enabled = false;
                hasWeaponSample = false;
                observedAttackPhase=-1f;
                return;
            }
            if (TryCancelForMovementInput()) return;
            UpdateHitbox();
            if(impactFeedback && impactFeedback.Visuals && stickHitbox)
            {
                GetWeaponVolume(out var trailA,out var trailB,out _);
                impactFeedback.Visuals.Sample(trailA,trailB,attacking && trailActive,Feel.trailLifetime);
            }
        }

        // PlayerInput's existing Attack binding handles left mouse/gamepad.
        public void OnAttack(InputValue value)
        {
            if (value != null && value.isPressed)
                TriggerAttack();
        }

        public void TriggerAttack()
        {
            if (!enabled || CinematicHandoffPending || IsSpinning || IsGuarding || ChordHeld) return;
            if (TryCancelForMovementInput() || movementCancelFrame == Time.frameCount) return;
            if (boomerang != null && boomerang.IsBusy) return;
            if (PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || charging || (controller != null && controller.Swimming) || Cursor.lockState != CursorLockMode.Locked)
                return;

            // SendMessages and action callbacks can both arrive in one frame.
            if (lastAttackInputFrame == Time.frameCount) return;
            lastAttackInputFrame = Time.frameCount;

            if (attacking)
            {
                // Continue an existing combo even while its authored root motion
                // temporarily lifts Sahur off the ground.
                if (comboStep >= 0 && comboStep < 2 && animator != null)
                {
                    int layer=AttackAnimationLayer;
                    var state = animator.GetCurrentAnimatorStateInfo(layer);
                    if (animator.IsInTransition(layer) &&
                        animator.GetNextAnimatorStateInfo(layer).fullPathHash == AttackAnimationHash)
                        state = animator.GetNextAnimatorStateInfo(layer);
                    float inputPhase=lightLayerHandoffFrame==Time.frameCount?lightLayerHandoffPhase:state.normalizedTime;
                    if ((state.fullPathHash == AttackAnimationHash || lightLayerHandoffFrame==Time.frameCount) &&
                        inputPhase >= 0 && inputPhase <= comboQueueClose)
                    { comboContinueQueued = true; comboInputExpiresAt=Time.unscaledTime+Feel.inputBuffer; }
                }
                else
                {
                    attackQueued=true;queueExpiresAt=Time.time+Feel.inputBuffer;
                }
                return;
            }
            // Input callbacks can precede the movement update that launches the
            // jump. Reserve that press for the aerial move rather than combo 1.
            if (controller != null && controller.CanUseGroundAttack &&
                GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump))
            {
                jumpAttackBufferedUntil = Time.time + .15f;
                return;
            }
            if (controller != null && controller.CanUseAirAttack)
            {
                if (!attacking && !airAttackUsed)
                    StartJumpSlash();
                return;
            }
            if (controller != null && !controller.CanUseGroundAttack)
                return;

            if (Time.time - lastFireTime >= cooldown)
            {
                StartLightAttack();
                return;
            }
            attackQueued = true;
            queueExpiresAt = Time.time + Feel.inputBuffer;
        }

        void StartLightAttack()
        {
            if (animator == null) return;
            attackQueued = false;
            if (animator.HasState(0, comboStateHashes[0]) &&
                animator.HasState(0, comboStateHashes[1]) &&
                animator.HasState(0, comboStateHashes[2]))
                StartComboStage(0);
            else if (stamina == null || stamina.TrySpend(stamina.lightAttackCost))
                PlayAttack(false, damage); // Existing scenes still work until their controller is upgraded.
        }

        void StartJumpSlash()
        {
            if (animator == null || !animator.HasState(0, jumpSlashStateHash))
            {
                Debug.LogError("Sahur Jump Slash state is missing from the Animator controller.", this);
                return;
            }

            if (stamina != null && !stamina.TrySpend(stamina.jumpSlashCost)) return;

            airAttackUsed = true;
            airImpactArmed = true;
            attackQueued = false;
            comboStep = -1;
            comboContinueQueued = false;
            BeginAttack(jumpSlashStateHash, damage * jumpSlashDamageMultiplier, 0f, 0.08f);
        }

        void StartComboStage(int stage)
        {
            if (stamina != null && !stamina.TrySpend(stamina.lightAttackCost * (UsesTwinBlades ? .75f : 1f)))
            {
                comboContinueQueued = false;
                return;
            }

            comboStep = stage;
            comboContinueQueued = false;
            chargeLayerIndex=animator.GetLayerIndex(ChargeUpperBodyLayer);
            var loadout=GetComponent<SahurWeaponLoadout>();
            lightLayerIndex=animator.GetLayerIndex(LightUpperBodyLayer);
            // At rest, keep the original whole-body combo, including its steps
            // and hip drive. Walking uses those same clips on a torso/arms layer.
            bool moving=controller!=null && (controller.RequestedMoveDirection.sqrMagnitude>.01f ||
                Vector3.ProjectOnPlane(controller.WorldVelocity,Vector3.up).sqrMagnitude>.04f);
            usesUpperBodyLight=moving && (loadout==null || !loadout.UsesTwinBlades) &&
                lightLayerIndex>0 && animator.HasState(lightLayerIndex,upperComboHashes[stage]);
            if(usesUpperBodyLight)controller?.PrepareChargeLocomotion();
            else controller?.BeginComboSourceTravel(stage);
            float multiplier = stage == 1 ? comboTwoDamageMultiplier :
                stage == 2 ? comboThreeDamageMultiplier : 1f;
            BeginAttack(comboStateHashes[stage], damage * multiplier, 0f,
                stage == 0 ? 0.08f : Feel.comboTransition);
        }

        void StartCharge()
        {
            if(CinematicHandoffPending)return;
            if (animator == null || !animator.HasState(0, heavyStateHash))
            {
                Debug.LogError("Sahur charge states are missing from the Animator controller.", this);
                return;
            }

            // The gameplay controller hands the Animator from the root to the
            // visual child in Awake, so resolve the layer on the live Animator.
            chargeLayerIndex = animator.GetLayerIndex(UsesTwinBlades?"Twin Blade Charge":ChargeUpperBodyLayer);
            usesUpperBodyCharge = chargeClip != null && chargeLayerIndex > 0 &&
                                  animator.HasState(chargeLayerIndex, upperBodyChargeStateHash);
            if (!usesUpperBodyCharge && !animator.HasState(0, chargeStateHash))
            {
                Debug.LogError("Sahur charge windup is missing from the Animator controller.", this);
                return;
            }

            if (stamina != null && !stamina.CanSpend(stamina.chargedAttackCost)) return;

            attackQueued = false;
            comboContinueQueued = false;
            if (usesUpperBodyCharge) controller?.PrepareChargeLocomotion();
            controller?.BeginAttackFacing();
            charging = true;
            fadingTwinBladeCharge = false;
            chargeStartedAt = Time.time;
            if (stickHitbox != null) stickHitbox.enabled = false;
            // The single-hand windup develops throughout the full charge.
            UpdateChargePose();
            if (usesUpperBodyCharge)
            {
                animator.SetLayerWeight(chargeLayerIndex, 0f);
                animator.Play(upperBodyChargeStateHash, chargeLayerIndex, 0f);
            }
            else animator.CrossFadeInFixedTime(chargeStateHash, ChargeLayerFadeIn, 0, 0f);
        }

        void ReleaseCharge()
        {
            // An early release cancels immediately; it never queues a delayed rush or spends stamina.
            if (!ChargeReady) { CancelCharge(); return; }
            if (stamina != null && !stamina.TrySpend(stamina.chargedAttackCost))
            {
                CancelCharge();
                return;
            }

            float releasePhase = ChargePose01;
            if (ChargeElapsed >= Mathf.Min(.5f, fullChargeTime)) ChargedAttacksPerformed++;
            float releaseTime = chargedStrikeClip != null ? 0f : releasePhase * (chargeClip != null ? chargeClip.length : 0f);
            charging = false;
            float multiplier = Mathf.Lerp(minChargeDamageMultiplier, maxChargeDamageMultiplier,
                Mathf.Clamp01((Time.time - chargeStartedAt) / EffectiveFullChargeTime));
            PlayAttack(true, damage * multiplier, releaseTime);
        }

        void CancelCharge()
        {
            // Keep the last evaluated X pose while its layer fades. Clearing its
            // phase/weight immediately exposes the idle arms in a single frame.
            fadingTwinBladeCharge = UsesTwinBlades && usesUpperBodyCharge && animator && chargeLayerIndex > 0 &&
                animator.GetLayerWeight(chargeLayerIndex) > 0f;
            charging = false;
            controller?.EndAttackFacing();
            if (animator != null && !fadingTwinBladeCharge) animator.SetFloat(chargeTimeHash, 0f);
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (usesUpperBodyCharge && animator != null && chargeLayerIndex >= 0 && !fadingTwinBladeCharge)
                animator.SetLayerWeight(chargeLayerIndex, 0f);
            else if (!usesUpperBodyCharge && animator != null)
                animator.CrossFadeInFixedTime("Locomotion", 0.14f, 0, 0f);
        }

        public bool TryCancelForMovementInput()
        {
            if (IsSpinning || IsGuarding)return false;
            if (movementCancelFrame == Time.frameCount) return true;
            if (enabled && !PauseSettingsMenu.IsOpen && !SahurLoadoutUI.BlocksInput &&
                controller!=null && controller.enabled && !controller.ExternalMovementLock &&
                controller.RequestedMoveDirection.sqrMagnitude>.01f)
                BlendLightIntoLocomotion();
            // Charging and the right-arm heavy strike share locomotion with running.
            // New direction presses must not clear their pose or charge timer.
            if (!enabled || !IsCombatMotionActive || CanMoveDuringCombat || PauseSettingsMenu.IsOpen ||
                SahurLoadoutUI.BlocksInput || Cursor.lockState != CursorLockMode.Locked ||
                (controller != null && (!controller.enabled || controller.ExternalControlLock || controller.ExternalMovementLock)))
                return false;
            // A new direction press cancels even when opposing keys give zero
            // net movement. Holding a key from before the attack is allowed.
            if (!GameInputSettings.PressedThisFrame(GameInputSettings.Action.Forward) &&
                !GameInputSettings.PressedThisFrame(GameInputSettings.Action.Back) &&
                !GameInputSettings.PressedThisFrame(GameInputSettings.Action.Left) &&
                !GameInputSettings.PressedThisFrame(GameInputSettings.Action.Right)) return false;
            CancelForMovement();
            return true;
        }

        void BlendLightIntoLocomotion()
        {
            if (!IsGroundComboActive || usesUpperBodyLight || UsesTwinBlades || animator==null) return;
            lightLayerIndex=animator.GetLayerIndex(LightUpperBodyLayer);
            if (lightLayerIndex<=0 || !animator.HasState(lightLayerIndex,upperComboHashes[comboStep])) return;
            var source=animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash==activeAttackHash)
                source=animator.GetNextAnimatorStateInfo(0);
            float phase=source.fullPathHash==activeAttackHash ? Mathf.Clamp01(source.normalizedTime) : 0f;
            // Continue the strike at its existing phase; movement never restarts
            // the swing or its damage window. Only the legs return to locomotion.
            usesUpperBodyLight=true;
            lightLayerHandoffFrame=Time.frameCount;
            lightLayerHandoffPhase=phase;
            animator.Play(upperComboHashes[comboStep],lightLayerIndex,phase);
            animator.SetLayerWeight(lightLayerIndex,1f);
            animator.SetLayerWeight(chargeLayerIndex,0f);
            controller.PrepareChargeLocomotion();
        }

        void CancelForMovement()
        {
            fadingTwinBladeCharge = false;
            ResetCombinations();
            localStop?.Release();
            impactFeedback?.Visuals?.ClearTrail();
            movementCancelFrame = Time.frameCount;
            boomerang?.CancelThrowForMovement();
            charging = attacking = usesRightArmHeavy = usesUpperBodyLight = activeAttackEntered = false;
            attackQueued = comboContinueQueued = airImpactArmed = false;
            jumpAttackBufferedUntil = -1f;
            comboStep = -1;
            activeAttackHash = 0;
            swingDamage = 0f;
            activeSwish = null;
            swishPlayed = true;
            hitThisSwing.Clear();
            hasWeaponSample = false;
            observedAttackPhase = -1f;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (secondBlade != null) secondBlade.enabled = false;
            controller?.EndAttackFacing(true);
            if (animator == null) return;
            animator.ResetTrigger(attackTrigger);
            animator.SetFloat(chargeTimeHash, 0f);
            if (hasHeavyPlaybackSpeed) animator.SetFloat(HeavyPlaybackSpeedHash, 1f);
            if (hasLightPlaybackSpeed) animator.SetFloat(LightPlaybackSpeedHash,1f);
            if(chargeLayerIndex>0)animator.SetLayerWeight(chargeLayerIndex,0f);
            int layer = animator.GetLayerIndex(ChargeUpperBodyLayer);
            if (layer > 0) animator.SetLayerWeight(layer, 0f);
            if (lightLayerIndex > 0) animator.SetLayerWeight(lightLayerIndex, 0f);
            animator.CrossFadeInFixedTime("Base Layer.Locomotion", .08f, 0, 0f);
        }

        public void SuspendForSwimming()
        {
            if(IsSpinning || IsGuarding)CancelForMovement();
            if (charging) CancelCharge();
            if (attacking) FinishAttack();
            attackQueued = comboContinueQueued = airImpactArmed = false;
            jumpAttackBufferedUntil = -1f;
            airAttackUsed = false;
            hitThisSwing.Clear();
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null)
            {
                // Swimming/story ownership must never inherit an outgoing charge layer.
                if (fadingTwinBladeCharge && chargeLayerIndex > 0)
                {
                    animator.SetLayerWeight(chargeLayerIndex, 0f);
                    animator.SetFloat(chargeTimeHash, 0f);
                    fadingTwinBladeCharge = false;
                }
                animator.ResetTrigger(attackTrigger);
                int layer = animator.GetLayerIndex(ChargeUpperBodyLayer);
                if (layer >= 0) animator.SetLayerWeight(layer, 0f);
                if (lightLayerIndex > 0) animator.SetLayerWeight(lightLayerIndex, 0f);
            }
        }

        void UpdateChargeLayerWeight()
        {
            if (animator == null || chargeLayerIndex <= 0) return;
            if (hasHeavyPlaybackSpeed)
            {
                float phase = HeavyAttackPhase;
                // Release accelerates into contact instead of slowing there.
                // Keep a separate weapon's authored playback contract unchanged.
                float speed = !usesRightArmHeavy || UsesTwinBlades ? 1f : phase <= heavySwingWindowEnd
                    ? Feel.heavySpeed.Evaluate(Mathf.InverseLerp(maxChargePosePhase,heavySwingWindowEnd,phase))
                    : Mathf.Lerp(1.1f,1.85f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(heavySwingWindowEnd,1,phase)));
                animator.SetFloat(HeavyPlaybackSpeedHash, speed);
            }
            // Both windup and release belong to the right-arm layer. The base
            // keeps the idle/walk/run pose instead of taking over the whole body.
            if (IsHeavyAttackActive && !usesRightArmHeavy && !fadingTwinBladeCharge && animator.GetLayerWeight(chargeLayerIndex) > 0f)
                animator.SetFloat(chargeTimeHash, HeavyAttackPhase);
            if (lightLayerIndex>0)
                animator.SetLayerWeight(lightLayerIndex,Mathf.MoveTowards(animator.GetLayerWeight(lightLayerIndex),
                    usesUpperBodyLight ? 1f : 0f,Time.deltaTime/ChargeLayerFadeOut));
            bool ownsArm = charging || (IsHeavyAttackActive && usesRightArmHeavy);
            float target = ownsArm ? 1f : 0f;
            float duration = ownsArm ? ChargeLayerFadeIn : fadingTwinBladeCharge ? twinBladePoseReleaseBlend : ChargeLayerFadeOut;
            float weight = Mathf.MoveTowards(animator.GetLayerWeight(chargeLayerIndex),
                target, Time.deltaTime / duration);
            animator.SetLayerWeight(chargeLayerIndex, weight);
            if (!charging && weight <= 0f)
            {
                animator.SetFloat(chargeTimeHash, 0f);
                fadingTwinBladeCharge = false;
            }
        }

        void UpdateLightPlayback()
        {
            if(!hasLightPlaybackSpeed || !animator)return;
            float speed=1;
            if(IsGroundComboActive && !UsesTwinBlades)
            {
                var state=animator.GetCurrentAnimatorStateInfo(AttackAnimationLayer);
                if(animator.IsInTransition(AttackAnimationLayer))state=animator.GetNextAnimatorStateInfo(AttackAnimationLayer);
                var window=comboStep==0?comboOneHitWindow:comboStep==1?comboTwoHitWindow:comboThreeHitWindow;
                // Map the same force envelope to each existing authored contact interval.
                float phase=lightLayerHandoffFrame==Time.frameCount?lightLayerHandoffPhase:state.normalizedTime;
                float mapped=phase<window.x?Mathf.InverseLerp(0,window.x,phase)*.53f:
                    phase<window.y?Mathf.Lerp(.53f,.88f,Mathf.InverseLerp(window.x,window.y,phase)):
                    Mathf.Lerp(.88f,1,Mathf.InverseLerp(window.y,1,phase));
                speed=Feel.lightSpeed.Evaluate(mapped);
            }
            animator.SetFloat(LightPlaybackSpeedHash,speed);
        }

        void UpdateChargePose()
        {
            if (animator == null) return;
            if (chargeClip != null) animator.SetFloat(chargeTimeHash, ChargePose01);
        }

        void PlayAttack(bool heavy, float amount, float heavyStartPose = 0f)
        {
            if (animator == null) return;
            int stateHash = heavy ? heavyStateHash : lightStateHash;
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogError($"Sahur attack state '{(heavy ? heavyAttackState : attackTrigger)}' is missing.", this);
                return;
            }

            comboStep = -1;
            comboContinueQueued = false;
            BeginAttack(stateHash, amount, heavy ? heavyStartPose : 0f,
                heavy ? UsesTwinBlades ? .08f : .18f : .11f);
        }

        void BeginAttack(int stateHash, float amount, float offset, float blend)
        {
            stamina?.NotifyCombat();
            controller?.BeginAttackFacing();
            animator.ResetTrigger(attackTrigger);
            activeAttackHash = stateHash;
            usesRightArmHeavy = !UsesTwinBlades && stateHash == heavyStateHash && chargeClip != null && usesUpperBodyCharge &&
                chargeLayerIndex > 0 && animator.HasState(chargeLayerIndex, rightArmHeavyStateHash);
            fadingTwinBladeCharge = UsesTwinBlades && stateHash == heavyStateHash && usesUpperBodyCharge &&
                chargeLayerIndex > 0 && animator.GetLayerWeight(chargeLayerIndex) > 0f;
            if (!usesRightArmHeavy && !fadingTwinBladeCharge && chargeLayerIndex > 0)
                animator.SetLayerWeight(chargeLayerIndex, 0f);
            if (!usesUpperBodyLight && lightLayerIndex > 0)
                animator.SetLayerWeight(lightLayerIndex, 0f);
            swingDamage = BalancedDamage(amount, UsesTwinBlades);
            attacking = true;
            activeAttackEntered = false;
            attackStartedAt = Time.time;
            lastFireTime = Time.time;
            hitThisSwing.Clear();
            hasWeaponSample = false;
            observedAttackPhase = -1f;
            swingDirection=transform.forward;
            activeSwish = comboStep >= 0 && comboSwishes != null &&
                          comboStep < comboSwishes.Length ? comboSwishes[comboStep] :
                          stateHash == heavyStateHash || stateHash == jumpSlashStateHash ?
                          heavySwish : comboSwishes != null && comboSwishes.Length > 0 ?
                          comboSwishes[0] : null;
            swishPlayed = false;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (secondBlade != null) secondBlade.enabled = false;
            if(usesUpperBodyLight)
            {
                animator.SetLayerWeight(lightLayerIndex,1);
                animator.CrossFadeInFixedTime(AttackAnimationHash,blend,lightLayerIndex,0);
                return;
            }
            if (usesRightArmHeavy && chargeClip != null)
            {
                if (hasHeavyPlaybackSpeed) animator.SetFloat(HeavyPlaybackSpeedHash, 1f);
                if (chargedStrikeClip != null)
                {
                    // Keep the accepted windup, then blend into the independent
                    // downloaded attack; charge duration only scales damage.
                    animator.CrossFadeInFixedTime(rightArmHeavyStateHash, .08f, chargeLayerIndex, 0f);
                    return;
                }
                // Windup and strike sample the same clip, at the same phase,
                // without introducing any torso, left-arm or root motion.
                // Both states contain the same pose. A long crossfade to a
                // running destination diluted the initial downswing velocity.
                animator.Play(rightArmHeavyStateHash,chargeLayerIndex,offset/chargeClip.length);
                return;
            }
            if (stateHash == heavyStateHash && offset > 0f && chargeClip != null)
            {
                // A fixed-time offset is affected by the destination state's
                // playback speed. A clip-normalized offset keeps the same hand
                // pose independently of the heavy state's playback speed.
                float sourceDuration = Mathf.Max(0.01f, animator.GetCurrentAnimatorStateInfo(0).length);
                animator.CrossFade(stateHash, blend / sourceDuration, 0, offset / chargeClip.length);
            }
            else animator.CrossFadeInFixedTime(stateHash, blend, 0, offset);
        }

        void UpdateAttackProgress()
        {
            if (IsSpinning || !attacking || animator == null || Time.time - attackStartedAt < 0.08f || lightLayerHandoffFrame==Time.frameCount)
                return;

            int layer = AttackAnimationLayer;
            var current = animator.GetCurrentAnimatorStateInfo(layer);
            bool inActiveAttack = current.fullPathHash == AttackAnimationHash;
            bool transitioningToAttack = animator.IsInTransition(layer) &&
                                         animator.GetNextAnimatorStateInfo(layer).fullPathHash == AttackAnimationHash;
            if (usesRightArmHeavy && inActiveAttack && !transitioningToAttack && current.normalizedTime >= .98f)
            {
                FinishAttack();
                return;
            }
            if (inActiveAttack) activeAttackEntered = true;
            if (!activeAttackEntered)
            {
                // CrossFade is applied by Animator after this component's Update.
                // The state can still look like Locomotion on the first frame.
                if (Time.time - attackStartedAt > 0.75f && !transitioningToAttack)
                {
                    FinishAttack();
                }
                return;
            }
            // Leave a small recovery tail for the next authored slice to blend
            // into. Waiting until .98 can lose the race to the automatic exit
            // at 1.0 on a slow frame and briefly blend through Locomotion.
            Vector2 comboWindow = comboStep == 0 ? comboOneHitWindow : comboTwoHitWindow;
            float chainPoint = Mathf.Min(comboChainPoint, Mathf.Max(comboWindow.y+.025f,Feel.comboRecoveryChain));
            // Early taps are a short input buffer, not an attack queued a whole second ahead.
            // Once entered in the authored queue interval, retain it through recovery/hit-stop.
            if(comboContinueQueued && current.normalizedTime<comboQueueOpen && Time.unscaledTime>comboInputExpiresAt)
                comboContinueQueued=false;
            if (comboStep >= 0 && comboStep < 2 && comboContinueQueued && inActiveAttack &&
                current.normalizedTime >= chainPoint)
            {
                StartComboStage(comboStep + 1);
                return;
            }
            if(usesUpperBodyLight && inActiveAttack && current.normalizedTime>=.98f){FinishAttack();return;}

            // Keep consuming the source root delta during the outgoing blend.
            // Ending at phase 1 would cut off the clip-to-locomotion motion.
            if (!inActiveAttack && !transitioningToAttack)
            {
                FinishAttack();
            }
        }

        void FinishAttack()
        {
            if(hasLightPlaybackSpeed && animator)animator.SetFloat(LightPlaybackSpeedHash,1);
            attacking = false;
            usesRightArmHeavy = false;
            // Blend the arm back into the continuing locomotion on recovery.
            usesUpperBodyLight=false;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (secondBlade != null) secondBlade.enabled = false;
            activeAttackEntered = false;
            comboStep = -1;
            comboContinueQueued = false;
            hasWeaponSample = false;
            observedAttackPhase = -1f;
            controller?.EndAttackFacing();
        }

        void UpdateHitbox()
        {
            trailActive=false;
            if (secondBlade) secondBlade.enabled = false;
            if(localStop && localStop.IsStopped)
            { if(stickHitbox)stickHitbox.enabled=false; return; }
            if (stickHitbox == null || animator == null || !attacking)
            {
                if (stickHitbox != null) stickHitbox.enabled = false;
                hasWeaponSample = false;
                observedAttackPhase=-1f;
                return;
            }

            if(UpdateSpinHitbox())return;
            int layer = AttackAnimationLayer;
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            bool transitioning=animator.IsInTransition(layer);
            if(transitioning && animator.GetNextAnimatorStateInfo(layer).fullPathHash==AttackAnimationHash)
                info=animator.GetNextAnimatorStateInfo(layer);
            bool activePose=info.fullPathHash==AttackAnimationHash;
            Vector2 window=CurrentDamageWindow;
            float start=window.x,end=window.y;
            // Start the whoosh just before the actual strike, rather than at
            // the beginning of the windup or the start of a combo transition.
            if (!swishPlayed && info.fullPathHash == AttackAnimationHash &&
                info.normalizedTime >= Mathf.Max(0f, start - 0.08f) &&
                activePose)
            {
                swishPlayed = true;
                if (swingAudio != null && activeSwish != null)
                    swingAudio.PlayOneShot(activeSwish);
                else GameplayAudio.Play(GameplayAudio.Cue.Swing, transform.position);
            }
            float phase=info.normalizedTime;
            trailActive=activePose && phase>=Mathf.Max(0,start-.05f) && phase<=end;
            // Preserve a short strike crossed between rendered frames. Never
            // bridge a new stage, a cancellation, or a large clock jump.
            bool skippedWindow=activePose && observedAttackPhase>=0 && observedAttackPhase<start &&
                phase>end && phase<=1.05f && phase-observedAttackPhase<=.6f;
            stickHitbox.enabled = activePose && (phase>=start && phase<=end || skippedWindow);
            if (stickHitbox.enabled)
            {
                if (secondBlade) secondBlade.enabled = true;
                Physics.SyncTransforms();
                if(skippedWindow)ScanCrossedWindow(start,end,phase);
                else
                {
                    // Include the damaging part of the first frame entering the window.
                    if(!hasWeaponSample && observedAttackPhase>=0 && observedAttackPhase<start &&
                        phase>=start && phase-observedAttackPhase<=.6f)
                    {
                        float from=Mathf.InverseLerp(observedAttackPhase,phase,start);
                        ScanInterpolatedWeapon(observedA,observedB,from,1);
                        if(secondBlade)
                        {
                            var first=stickHitbox;stickHitbox=secondBlade;
                            ScanInterpolatedWeapon(observedSecondA,observedSecondB,from,1);stickHitbox=first;
                        }
                    }
                    ScanHits();
                }
            }
            else hasWeaponSample = false;
            if(activePose)
            {
                GetWeaponVolume(out observedA,out observedB,out _);
                if(secondBlade)
                {
                    var first=stickHitbox;stickHitbox=secondBlade;
                    GetWeaponVolume(out observedSecondA,out observedSecondB,out _);stickHitbox=first;
                }
                observedAttackPhase=phase;
            }
            else observedAttackPhase=-1f;
            // An interpolated crossing is a query, not a trigger left alive in recovery.
            if(skippedWindow){stickHitbox.enabled=false;if(secondBlade)secondBlade.enabled=false;hasWeaponSample=false;}
        }

        void ScanHits()
        {
            GetWeaponVolume(out Vector3 a, out Vector3 b, out float radius);
            if(hasWeaponSample)swingDirection=((a+b)-(previousWeaponA+previousWeaponB))*.5f;
            ScanWeaponSegment(a, b, radius);
            var info = animator.GetCurrentAnimatorStateInfo(AttackAnimationLayer);
            if (animator.IsInTransition(AttackAnimationLayer) &&
                animator.GetNextAnimatorStateInfo(AttackAnimationLayer).fullPathHash == AttackAnimationHash)
                info = animator.GetNextAnimatorStateInfo(AttackAnimationLayer);
            float phase = info.normalizedTime;
            bool sweep = hasWeaponSample && phase >= previousHitPhase && phase - previousHitPhase <= .25f;
            if (secondBlade && secondBlade.enabled)
            {
                Collider first = stickHitbox;
                stickHitbox = secondBlade;
                try
                {
                    GetWeaponVolume(out Vector3 secondA, out Vector3 secondB, out float secondRadius);
                    ScanWeaponSegment(secondA, secondB, secondRadius);
                    if (sweep) ScanWeaponSweep(previousSecondA, previousSecondB, secondA, secondB, secondRadius);
                    previousSecondA = secondA; previousSecondB = secondB;
                }
                finally { stickHitbox = first; }
            }
            // Sweep only consecutive damaging poses. Do not bridge a cancelled
            // swing, a new combo stage, or the anticipation before the first hit.
            if (sweep)
            {
                swingDirection=((a+b)-(previousWeaponA+previousWeaponB))*.5f;
                ScanWeaponSweep(previousWeaponA, previousWeaponB, a, b, radius);
            }
            previousWeaponA = a; previousWeaponB = b; previousHitPhase = phase;
            hasWeaponSample = true;
            ScanComboArc();
        }

        void ScanCrossedWindow(float start,float end,float phase)
        {
            float from=Mathf.InverseLerp(observedAttackPhase,phase,start);
            float to=Mathf.InverseLerp(observedAttackPhase,phase,end);
            ScanInterpolatedWeapon(observedA,observedB,from,to);
            if(secondBlade)
            {
                var first=stickHitbox;stickHitbox=secondBlade;
                ScanInterpolatedWeapon(observedSecondA,observedSecondB,from,to);stickHitbox=first;
            }
            ScanComboArc();
        }
        void ScanInterpolatedWeapon(Vector3 oldA,Vector3 oldB,float from,float to)
        {
            GetWeaponVolume(out var a,out var b,out float radius);
            Vector3 startA=Vector3.Lerp(oldA,a,from),startB=Vector3.Lerp(oldB,b,from);
            Vector3 endA=Vector3.Lerp(oldA,a,to),endB=Vector3.Lerp(oldB,b,to);
            swingDirection=((endA+endB)-(startA+startB))*.5f;
            ScanWeaponSweep(startA,startB,endA,endB,radius);
        }
        void ScanWeaponSweep(Vector3 oldA, Vector3 oldB, Vector3 a, Vector3 b, float radius)
        {
            using(var sample=SweepMarker.Auto())
            {
            // Sample the whole blade between frames; tip/handle trails alone
            // leave a hole where the middle of a fast slash crosses a target.
            float travel = Mathf.Max(Vector3.Distance(oldA,a), Vector3.Distance(oldB,b));
            int steps = Mathf.Clamp(Mathf.CeilToInt(travel / Mathf.Max(.02f,radius)),1,64);
            for (int i=0; i<=steps; i++)
            {
                float t=(float)i/steps;
                ScanWeaponSegment(Vector3.Lerp(oldA,a,t),Vector3.Lerp(oldB,b,t),radius);
            }
            ScanWeaponSegment(oldA,a,radius);ScanWeaponSegment(oldB,b,radius);
            }
        }

        float StrikeScale => Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.y) / 3f);
        void ScanComboArc()
        {
            if (comboStep < 0 || comboReach <= 0f) return;
            int count = Physics.OverlapSphereNonAlloc(StrikeOrigin, comboReach * StrikeScale,
                nearbyColliders, hitMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                TryHit(nearbyColliders[i], false);
        }
        Vector3 StrikeOrigin => strikeController != null ? strikeController.bounds.center : transform.position;

        void GetWeaponVolume(out Vector3 a, out Vector3 b, out float radius)
        {
            if (stickHitbox is CapsuleCollider capsule)
            {
                Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                Vector3 scale = capsule.transform.lossyScale;
                float axialScale = Mathf.Abs(capsule.direction == 0 ? scale.x : capsule.direction == 1 ? scale.y : scale.z);
                float radialScale = capsule.direction == 0 ? Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)) :
                    capsule.direction == 1 ? Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) : Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                radius = capsule.radius * radialScale + weaponHitPadding * StrikeScale;
                float half = Mathf.Max(0f, capsule.height * axialScale * .5f - capsule.radius * radialScale);
                Vector3 center = capsule.transform.TransformPoint(capsule.center);
                Vector3 extent = capsule.transform.TransformDirection(axis).normalized * half;
                a = center - extent; b = center + extent;
            }
            else
            {
                a = b = stickHitbox.bounds.center;
                radius = stickHitbox.bounds.extents.magnitude + weaponHitPadding * StrikeScale;
            }
        }

        void ScanWeaponSegment(Vector3 a, Vector3 b, float radius)
        {
            queryWeaponA=a;queryWeaponB=b;
            int count;
            while ((count = Physics.OverlapCapsuleNonAlloc(a, b, radius, nearbyColliders, hitMask, QueryTriggerInteraction.Collide)) == nearbyColliders.Length)
                System.Array.Resize(ref nearbyColliders, nearbyColliders.Length * 2);
            // Resolve a contacted weak point before the enclosing body. Both
            // resolve to the same health component and still count as one hit.
            for (int i = 0; i < count; i++)
                if (nearbyColliders[i].GetComponent<BossWeakPoint>() != null) TryHit(nearbyColliders[i], true);
            for (int i = 0; i < count; i++)
                if (nearbyColliders[i].GetComponent<BossWeakPoint>() == null) TryHit(nearbyColliders[i], true);
        }

        bool StrikeBlocked(Collider target)
        {
            Vector3 from = StrikeOrigin;
            Vector3 to = target.ClosestPoint(from);
            Vector3 step = to - from;
            if (step.sqrMagnitude < .001f) return false;
            int count;
            while ((count = Physics.RaycastNonAlloc(from, step.normalized, strikeObstacles, step.magnitude, ~0, QueryTriggerInteraction.Ignore)) == strikeObstacles.Length)
                System.Array.Resize(ref strikeObstacles, strikeObstacles.Length * 2);
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = strikeObstacles[i].collider;
                if (obstacle == null || obstacle == target || obstacle.transform.IsChildOf(transform) ||
                    obstacle.GetComponentInParent<IDamageable>() != null) continue;
                return true;
            }
            return false;
        }

        void ApplyLandingImpact()
        {
            // Landing cannot invent a radial hit: the same live weapon volume
            // and per-swing victim set remain authoritative for the chop.
            if(attacking && activeAttackHash==jumpSlashStateHash && stickHitbox && stickHitbox.enabled &&
                !(localStop && localStop.IsStopped))ScanHits();
        }

        void OnTriggerEnter(Collider other)
        {
            if (stickHitbox == null || !stickHitbox.enabled) return;
            TryHit(other, false);
        }
        public static float BalancedDamage(float amount, bool twinBlades) => amount * (twinBlades ? .8f : 1f);

        void TryHit(Collider other, bool weaponContact)
        {
            if (!attacking || stickHitbox == null || !stickHitbox.enabled) return;
            if (other == null || other == stickHitbox || other.transform.IsChildOf(transform)) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;
            var targetComponent = target as Component;
            if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag) &&
                (targetComponent == null ||
                 (!targetComponent.CompareTag(enemyTag) &&
                  !targetComponent.transform.root.CompareTag(enemyTag)))) return;
            Vector3 point = other.ClosestPoint(StrikeOrigin);
            Vector3 direction = Vector3.ProjectOnPlane(other.bounds.center - StrikeOrigin, Vector3.up);
            if (!weaponContact && !Physics.ComputePenetration(stickHitbox, stickHitbox.transform.position,
                    stickHitbox.transform.rotation, other, other.transform.position,
                    other.transform.rotation, out _, out _)) return;
            if (StrikeBlocked(other)) return;
            if (hitThisSwing.Add(target))
            {
                Vector3 origin=stickHitbox.bounds.center;
                if(weaponContact)
                {
                    Vector3 axis=queryWeaponB-queryWeaponA;
                    float along=axis.sqrMagnitude>.00001f?Mathf.Clamp01(Vector3.Dot(other.bounds.center-queryWeaponA,axis)/axis.sqrMagnitude):0;
                    origin=queryWeaponA+axis*along;
                }
                CombatHitFeedback.Apply(gameObject, target, swingDamage, other.ClosestPoint(origin),
                    IsSpinning ? CombatHitKind.Spin : activeAttackHash == jumpSlashStateHash ? CombatHitKind.JumpSlash
                    : activeAttackHash == heavyStateHash ? CombatHitKind.ChargedHeavy
                    : comboStep == 2 ? CombatHitKind.ComboFinisher : comboStep == 1 ? CombatHitKind.ComboTwo : CombatHitKind.ComboOne,swingDirection);
            }
        }

        void OnDisable()
        {
            fadingTwinBladeCharge = false;
            ResetCombinations();chordLatched=false;
            dodgeBufferedUntil=-1;
            localStop?.Release();
            impactFeedback?.Visuals?.ClearTrail();
            if(hasLightPlaybackSpeed && animator)animator.SetFloat(LightPlaybackSpeedHash,1);
            controller?.EndAttackFacing(true);
            charging = false;
            attacking = false;
            usesRightArmHeavy = false;
            usesUpperBodyLight = false;
            activeAttackEntered = false;
            attackQueued = false;
            comboContinueQueued = false;
            airImpactArmed = false;
            jumpAttackBufferedUntil = -1f;
            airAttackUsed = false;
            comboStep = -1;
            hasWeaponSample = false;
            observedAttackPhase = -1f;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (secondBlade != null) secondBlade.enabled = false;
            if (animator != null)
            {
                animator.SetFloat(chargeTimeHash, 0f);
                if (chargeLayerIndex >= 0) animator.SetLayerWeight(chargeLayerIndex, 0f);
                if (lightLayerIndex > 0) animator.SetLayerWeight(lightLayerIndex, 0f);
            }
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint);
    }
}
