using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    /// <summary>Ground combo, charged swing, and a once-per-jump aerial chop.</summary>
    public class SahurAttack : MonoBehaviour
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
        const string ChargeUpperBodyLayer = "Charge Upper Body";
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
        public Vector2 comboTwoHitWindow = new Vector2(0.34f, 0.75f);
        public Vector2 comboThreeHitWindow = new Vector2(0.32f, 0.55f);

        [Header("Air jump slash")]
        public string jumpSlashState = "Jump Slash";
        [Min(1f)] public float jumpSlashDamageMultiplier = 1.7f;
        public Vector2 jumpSlashHitWindow = new Vector2(0.16f, 0.82f);
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

        public bool IsCombatMotionActive => charging || attacking || (boomerang != null && boomerang.IsThrowing);
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
        public bool CanMoveDuringCombat => charging || usesUpperBodyLight || (IsHeavyAttackActive && usesRightArmHeavy);
        public bool UsesAnimationRootMotion => attacking && activeAttackHash != jumpSlashStateHash && !usesRightArmHeavy && !usesUpperBodyLight;
        public bool SafeForCinematic => !IsCombatMotionActive || (attacking && !charging && CurrentAttackPhase>=CurrentWindowEnd+.04f);
        public float CurrentAttackPhase => animator!=null && attacking ? animator.GetCurrentAnimatorStateInfo(AttackAnimationLayer).normalizedTime : 0;
        float CurrentWindowEnd => comboStep==0 ? comboOneHitWindow.y : comboStep==1 ? comboTwoHitWindow.y : comboStep==2 ? comboThreeHitWindow.y : IsHeavyAttackActive ? heavySwingWindowEnd : swingWindowEnd;
        public int CurrentComboStage => IsGroundComboActive ? comboStep : -1;
        public bool IsCharging => charging;
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
            twinRight = right; twinLeft = left; secondBlade = null;
            stickHitbox = useBlades ? right : originalStickHitbox;
            for (int i = 0; i < 3; i++) comboStateHashes[i] = Animator.StringToHash("Base Layer." +
                (useBlades ? "Capri Combo " + (i + 1) : i == 0 ? comboOneState : i == 1 ? comboTwoState : comboThreeState));
            heavyStateHash = Animator.StringToHash("Base Layer." + (useBlades ? "Capri Heavy" : heavyAttackState));
            chargeStateHash = Animator.StringToHash("Base Layer." + (useBlades ? "Capri Charge" : chargeState));
            jumpSlashStateHash = Animator.StringToHash("Base Layer." + (useBlades ? "Capri Jump Slash" : jumpSlashState));
            chargeClip = useBlades ? Resources.Load<AnimationClip>("Equipment/Capri/CapriSlash") : originalChargeClip;
            chargedStrikeClip = useBlades ? null : originalChargedStrikeClip;
            comboOneHitWindow = useBlades ? new Vector2(.28f,.62f) : originalWindowOne;
            comboTwoHitWindow = useBlades ? new Vector2(.28f,.62f) : originalWindowTwo;
            comboThreeHitWindow = useBlades ? new Vector2(.28f,.74f) : originalWindowThree;
            jumpSlashHitWindow = useBlades ? new Vector2(.28f,.62f) : originalJumpWindow;
            comboReach = useBlades ? 5.4f : originalReach;
            comboArc = useBlades ? 150f : originalArc;
            maxChargePosePhase = useBlades ? .22f : originalChargePhase;
            if (stickHitbox) stickHitbox.enabled = false;
        }
        public float Charge01 => charging ? Mathf.Clamp01(ChargeElapsed / Mathf.Max(0.2f, fullChargeTime)) : 0f;
        public float ChargePose01 => Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(ChargeElapsed / Mathf.Max(0.05f, chargeWindupTime))) * Mathf.Clamp01(maxChargePosePhase);
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
        readonly Collider[] nearbyColliders = new Collider[64];
        readonly RaycastHit[] strikeObstacles = new RaycastHit[32];
        Vector3 previousWeaponA, previousWeaponB;
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
        int AttackAnimationLayer => usesRightArmHeavy || usesUpperBodyLight ? chargeLayerIndex : 0;
        int AttackAnimationHash => usesRightArmHeavy ? rightArmHeavyStateHash : usesUpperBodyLight && comboStep>=0 ? upperComboHashes[comboStep] : activeAttackHash;
        int chargeLayerIndex = -1;
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
            upperComboHashes[0]=Animator.StringToHash(ChargeUpperBodyLayer+"."+comboOneState);
            upperComboHashes[1]=Animator.StringToHash(ChargeUpperBodyLayer+"."+comboTwoState);
            upperComboHashes[2]=Animator.StringToHash(ChargeUpperBodyLayer+"."+comboThreeState);
            if (swingAudio == null) swingAudio = GetComponent<AudioSource>();
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null)
                foreach (var parameter in animator.parameters)
                    if (parameter.nameHash == HeavyPlaybackSpeedHash && parameter.type == AnimatorControllerParameterType.Float)
                        hasHeavyPlaybackSpeed = true;
            boomerang = GetComponent<SahurBoomerang>();
            if (boomerang == null) boomerang = gameObject.AddComponent<SahurBoomerang>();
        }

        void Update()
        {
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
                attackQueued = false;
                comboContinueQueued = false;
                if (charging) CancelCharge();
                if (stickHitbox != null) stickHitbox.enabled = false;
                return;
            }

            if (airImpactArmed && controller != null && controller.CanUseGroundAttack)
            {
                airImpactArmed = false;
                ApplyLandingImpact();
            }
            if (controller != null && controller.CanUseGroundAttack)
                airAttackUsed = false;

            UpdateAttackProgress();
            var mouse = Mouse.current;
            bool gameHasFocus = Cursor.lockState == CursorLockMode.Locked;
            if (charging && (!gameHasFocus || mouse == null ||
                             (controller != null && !controller.CanUseGroundAttack)))
                CancelCharge();
            else if (charging && !mouse.rightButton.isPressed)
                ReleaseCharge();
            if (charging) UpdateChargePose();
            else if (!charging && !attacking && mouse != null && gameHasFocus &&
                     mouse.rightButton.wasPressedThisFrame && Time.time - lastFireTime >= cooldown &&
                     (controller == null || controller.CanUseGroundAttack))
                StartCharge();

            if (attackQueued && Time.time > queueExpiresAt)
                attackQueued = false;
            if (attackQueued && !charging && !attacking && Time.time - lastFireTime >= cooldown &&
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
        }

        // PlayerInput's existing Attack binding handles left mouse/gamepad.
        public void OnAttack(InputValue value)
        {
            if (value != null && value.isPressed)
                TriggerAttack();
        }

        public void TriggerAttack()
        {
            if (!enabled) return;
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
                    if (state.fullPathHash == AttackAnimationHash &&
                        state.normalizedTime >= comboQueueOpen &&
                        state.normalizedTime <= comboQueueClose)
                        comboContinueQueued = true;
                }
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
            queueExpiresAt = Time.time + 0.35f;
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
            if (stamina != null && !stamina.TrySpend(stamina.lightAttackCost))
            {
                comboContinueQueued = false;
                return;
            }

            comboStep = stage;
            comboContinueQueued = false;
            chargeLayerIndex=animator.GetLayerIndex(ChargeUpperBodyLayer);
            var loadout=GetComponent<SahurWeaponLoadout>();
            usesUpperBodyLight=(loadout==null || !loadout.UsesTwinBlades) && chargeLayerIndex>0 && animator.HasState(chargeLayerIndex,upperComboHashes[stage]);
            if(usesUpperBodyLight)controller?.PrepareChargeLocomotion();
            else controller?.BeginComboSourceTravel(stage);
            float multiplier = stage == 1 ? comboTwoDamageMultiplier :
                stage == 2 ? comboThreeDamageMultiplier : 1f;
            BeginAttack(comboStateHashes[stage], damage * multiplier, 0f,
                stage == 0 ? 0.08f : comboBlendTime);
        }

        void StartCharge()
        {
            if (animator == null || !animator.HasState(0, heavyStateHash))
            {
                Debug.LogError("Sahur charge states are missing from the Animator controller.", this);
                return;
            }

            // The gameplay controller hands the Animator from the root to the
            // visual child in Awake, so resolve the layer on the live Animator.
            chargeLayerIndex = animator.GetLayerIndex(ChargeUpperBodyLayer);
            usesUpperBodyCharge = !UsesTwinBlades && chargeClip != null && chargeLayerIndex > 0 &&
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
            chargeStartedAt = Time.time;
            if (stickHitbox != null) stickHitbox.enabled = false;
            // Raise promptly, then hold the ready pose while damage continues charging.
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
            if (stamina != null && !stamina.TrySpend(stamina.chargedAttackCost))
            {
                CancelCharge();
                return;
            }

            float releasePhase = ChargePose01;
            float releaseTime = chargedStrikeClip != null ? 0f : releasePhase * (chargeClip != null ? chargeClip.length : 0f);
            charging = false;
            float multiplier = Mathf.Lerp(minChargeDamageMultiplier, maxChargeDamageMultiplier,
                Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeTime));
            PlayAttack(true, damage * multiplier, releaseTime);
        }

        void CancelCharge()
        {
            charging = false;
            controller?.EndAttackFacing();
            if (animator != null) animator.SetFloat(chargeTimeHash, 0f);
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (usesUpperBodyCharge && animator != null && chargeLayerIndex >= 0)
                animator.SetLayerWeight(chargeLayerIndex, 0f);
            else if (animator != null)
                animator.CrossFadeInFixedTime("Locomotion", 0.14f, 0, 0f);
        }

        public bool TryCancelForMovementInput()
        {
            if (movementCancelFrame == Time.frameCount) return true;
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

        void CancelForMovement()
        {
            movementCancelFrame = Time.frameCount;
            boomerang?.CancelThrowForMovement();
            charging = attacking = usesRightArmHeavy = usesUpperBodyLight = activeAttackEntered = false;
            attackQueued = comboContinueQueued = airImpactArmed = false;
            comboStep = -1;
            activeAttackHash = 0;
            swingDamage = 0f;
            activeSwish = null;
            swishPlayed = true;
            hitThisSwing.Clear();
            hasWeaponSample = false;
            if (stickHitbox != null) stickHitbox.enabled = false;
            controller?.EndAttackFacing(true);
            if (animator == null) return;
            animator.ResetTrigger(attackTrigger);
            animator.SetFloat(chargeTimeHash, 0f);
            if (hasHeavyPlaybackSpeed) animator.SetFloat(HeavyPlaybackSpeedHash, 1f);
            int layer = animator.GetLayerIndex(ChargeUpperBodyLayer);
            if (layer > 0) animator.SetLayerWeight(layer, 0f);
            animator.CrossFadeInFixedTime("Base Layer.Locomotion", .08f, 0, 0f);
        }

        public void SuspendForSwimming()
        {
            if (charging) CancelCharge();
            if (attacking) FinishAttack();
            attackQueued = comboContinueQueued = airImpactArmed = false;
            airAttackUsed = false;
            hitThisSwing.Clear();
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null)
            {
                animator.ResetTrigger(attackTrigger);
                int layer = animator.GetLayerIndex(ChargeUpperBodyLayer);
                if (layer >= 0) animator.SetLayerWeight(layer, 0f);
            }
        }

        void UpdateChargeLayerWeight()
        {
            if ((!usesUpperBodyCharge && !usesUpperBodyLight) || animator == null || chargeLayerIndex < 0) return;
            if (hasHeavyPlaybackSpeed)
            {
                float phase = HeavyAttackPhase;
                // Give the fast imported downswing more frames while shortening
                // its long recovery. This affects only the right-arm state.
                float speed = !usesRightArmHeavy ? 1f : phase <= heavySwingWindowEnd
                    ? Mathf.Lerp(1f, .8f, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(maxChargePosePhase, heavySwingWindowStart, phase)))
                    : Mathf.Lerp(.8f, 1.6f, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(heavySwingWindowEnd, Mathf.Min(1f, heavySwingWindowEnd + .25f), phase)));
                animator.SetFloat(HeavyPlaybackSpeedHash, speed);
            }
            // Both windup and release belong to the right-arm layer. The base
            // keeps the idle/walk/run pose instead of taking over the whole body.
            if (IsHeavyAttackActive && !usesRightArmHeavy && animator.GetLayerWeight(chargeLayerIndex) > 0f)
                animator.SetFloat(chargeTimeHash, HeavyAttackPhase);
            bool ownsArm = charging || usesUpperBodyLight || (IsHeavyAttackActive && usesRightArmHeavy);
            float target = ownsArm ? 1f : 0f;
            float duration = ownsArm ? ChargeLayerFadeIn : ChargeLayerFadeOut;
            float weight = Mathf.MoveTowards(animator.GetLayerWeight(chargeLayerIndex),
                target, Time.deltaTime / duration);
            animator.SetLayerWeight(chargeLayerIndex, weight);
            if (!charging && weight <= 0f)
                animator.SetFloat(chargeTimeHash, 0f);
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
                heavy ? 0.18f : 0.11f);
        }

        void BeginAttack(int stateHash, float amount, float offset, float blend)
        {
            controller?.BeginAttackFacing();
            animator.ResetTrigger(attackTrigger);
            activeAttackHash = stateHash;
            usesRightArmHeavy = stateHash == heavyStateHash && chargeClip != null && usesUpperBodyCharge &&
                chargeLayerIndex > 0 && animator.HasState(chargeLayerIndex, rightArmHeavyStateHash);
            if (!usesRightArmHeavy && !usesUpperBodyLight && chargeLayerIndex > 0)
                animator.SetLayerWeight(chargeLayerIndex, 0f);
            swingDamage = amount;
            attacking = true;
            activeAttackEntered = false;
            attackStartedAt = Time.time;
            lastFireTime = Time.time;
            hitThisSwing.Clear();
            hasWeaponSample = false;
            activeSwish = comboStep >= 0 && comboSwishes != null &&
                          comboStep < comboSwishes.Length ? comboSwishes[comboStep] :
                          stateHash == heavyStateHash || stateHash == jumpSlashStateHash ?
                          heavySwish : comboSwishes != null && comboSwishes.Length > 0 ?
                          comboSwishes[0] : null;
            swishPlayed = false;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if(usesUpperBodyLight)
            {
                animator.SetLayerWeight(chargeLayerIndex,1);
                animator.CrossFadeInFixedTime(AttackAnimationHash,blend,chargeLayerIndex,0);
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
                float currentDuration = Mathf.Max(.01f, animator.GetCurrentAnimatorStateInfo(chargeLayerIndex).length);
                animator.CrossFade(rightArmHeavyStateHash, .09f / currentDuration,
                    chargeLayerIndex, offset / chargeClip.length);
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
            if (!attacking || animator == null || Time.time - attackStartedAt < 0.08f)
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
            float chainPoint = Mathf.Min(comboChainPoint, Mathf.Clamp(comboWindow.y + 0.06f, 0.86f, 0.94f));
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
            attacking = false;
            usesRightArmHeavy = false;
            if(usesUpperBodyLight && animator!=null && chargeLayerIndex>0)animator.SetLayerWeight(chargeLayerIndex,0);
            usesUpperBodyLight=false;
            if (stickHitbox != null) stickHitbox.enabled = false;
            activeAttackEntered = false;
            comboStep = -1;
            comboContinueQueued = false;
            hasWeaponSample = false;
            controller?.EndAttackFacing();
        }

        void UpdateHitbox()
        {
            if (secondBlade) secondBlade.enabled = false;
            if (stickHitbox == null || animator == null || !attacking)
            {
                if (stickHitbox != null) stickHitbox.enabled = false;
                hasWeaponSample = false;
                observedAttackPhase=-1f;
                return;
            }

            int layer = AttackAnimationLayer;
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            bool transitioning=animator.IsInTransition(layer);
            if(transitioning && animator.GetNextAnimatorStateInfo(layer).fullPathHash==AttackAnimationHash)
                info=animator.GetNextAnimatorStateInfo(layer);
            bool activePose=info.fullPathHash==AttackAnimationHash;
            float start;
            float end;
            if (activeAttackHash == jumpSlashStateHash)
            {
                start = jumpSlashHitWindow.x;
                end = jumpSlashHitWindow.y;
            }
            else if (comboStep >= 0)
            {
                Vector2 window = comboStep == 0 ? comboOneHitWindow :
                    comboStep == 1 ? comboTwoHitWindow : comboThreeHitWindow;
                start = window.x;
                end = window.y;
            }
            else
            {
                start = UsesTwinBlades ? .28f : activeAttackHash == heavyStateHash ? heavySwingWindowStart : swingWindowStart;
                end = UsesTwinBlades ? .62f : activeAttackHash == heavyStateHash ? heavySwingWindowEnd : swingWindowEnd;
            }
            // Start the whoosh just before the actual strike, rather than at
            // the beginning of the windup or the start of a combo transition.
            if (!swishPlayed && info.fullPathHash == AttackAnimationHash &&
                info.normalizedTime >= Mathf.Max(0f, start - 0.08f) &&
                activePose)
            {
                swishPlayed = true;
                if (swingAudio != null && activeSwish != null)
                    swingAudio.PlayOneShot(activeSwish);
            }
            float phase=info.normalizedTime;
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
                else ScanHits();
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
            ScanWeaponSegment(a, b, radius);
            if (secondBlade && secondBlade.enabled)
            {
                Collider first = stickHitbox;
                stickHitbox = secondBlade;
                GetWeaponVolume(out Vector3 secondA, out Vector3 secondB, out float secondRadius);
                stickHitbox = first;
                ScanWeaponSegment(secondA, secondB, secondRadius);
            }
            float phase = animator.GetCurrentAnimatorStateInfo(AttackAnimationLayer).normalizedTime;
            // Sweep only consecutive damaging poses. Do not bridge a cancelled
            // swing, a new combo stage, or the anticipation before the first hit.
            if (hasWeaponSample && phase >= previousHitPhase && phase - previousHitPhase <= .25f)
            {
                ScanWeaponSegment(previousWeaponA, a, radius);
                ScanWeaponSegment(previousWeaponB, b, radius);
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
            ScanWeaponSegment(startA,startB,radius);ScanWeaponSegment(endA,endB,radius);
            ScanWeaponSegment(startA,endA,radius);ScanWeaponSegment(startB,endB,radius);
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
            int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, nearbyColliders, hitMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++) TryHit(nearbyColliders[i], true);
        }

        bool StrikeBlocked(Collider target)
        {
            Vector3 from = StrikeOrigin;
            Vector3 to = target.ClosestPoint(from);
            Vector3 step = to - from;
            if (step.sqrMagnitude < .001f) return false;
            int count = Physics.RaycastNonAlloc(from, step.normalized, strikeObstacles, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
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
            if (landingImpactRadius <= 0f) return;
            Vector3 center = transform.position + transform.forward * landingImpactForwardOffset;
            var character = GetComponent<CharacterController>();
            if (character != null)
                center.y = character.bounds.min.y + Mathf.Min(landingImpactRadius * 0.5f, 0.6f);
            int count = Physics.OverlapSphereNonAlloc(center, landingImpactRadius, nearbyColliders,
                hitMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider other = nearbyColliders[i];
                if (other == null || other.transform.IsChildOf(transform)) continue;
                var target = other.GetComponentInParent<IDamageable>();
                var targetComponent = target as Component;
                if (targetComponent == null) continue;
                if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag) &&
                    !targetComponent.CompareTag(enemyTag) &&
                    !targetComponent.transform.root.CompareTag(enemyTag)) continue;
                if (hitThisSwing.Add(target))
                    CombatHitFeedback.Apply(gameObject, target, damage * jumpSlashDamageMultiplier, center, CombatHitKind.JumpSlash);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (stickHitbox == null || !stickHitbox.enabled) return;
            TryHit(other, false);
        }

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
                CombatHitFeedback.Apply(gameObject, target, swingDamage, other.ClosestPoint(stickHitbox.bounds.center),
                    activeAttackHash == jumpSlashStateHash ? CombatHitKind.JumpSlash
                    : activeAttackHash == heavyStateHash ? CombatHitKind.ChargedHeavy
                    : comboStep == 2 ? CombatHitKind.ComboFinisher : comboStep == 1 ? CombatHitKind.ComboTwo : CombatHitKind.ComboOne);
        }

        void OnDisable()
        {
            controller?.EndAttackFacing(true);
            charging = false;
            attacking = false;
            usesRightArmHeavy = false;
            usesUpperBodyLight = false;
            activeAttackEntered = false;
            attackQueued = false;
            comboContinueQueued = false;
            airImpactArmed = false;
            airAttackUsed = false;
            comboStep = -1;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null)
            {
                animator.SetFloat(chargeTimeHash, 0f);
                if (chargeLayerIndex >= 0) animator.SetLayerWeight(chargeLayerIndex, 0f);
            }
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint);
    }
}
