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
        // The later overhead windup intersects Sahur's tall head with this grip.
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
        [Range(0f, 1f)] public float swingWindowStart = 0.10f;
        [Range(0f, 1f)] public float swingWindowEnd = 0.55f;
        [Range(0f, 1f)] public float heavySwingWindowStart = 0.25f;
        [Range(0f, 1f)] public float heavySwingWindowEnd = 0.69f;
        [Min(0f)] public float cooldown = 0.5f;

        [Header("Right mouse charge")]
        [Min(0.2f)] public float fullChargeTime = 1.25f;
        [Min(1f)] public float minChargeDamageMultiplier = 1.25f;
        [Min(1f)] public float maxChargeDamageMultiplier = 2.2f;

        [Header("Swing audio")]
        public AudioSource swingAudio;
        public AudioClip[] comboSwishes = new AudioClip[3];
        public AudioClip heavySwish;

        public bool IsCombatMotionActive => charging || attacking;
        public bool IsGroundComboActive => attacking && comboStep >= 0;
        public bool UsesAnimationRootMotion => attacking && activeAttackHash != jumpSlashStateHash;
        public int CurrentComboStage => IsGroundComboActive ? comboStep : -1;
        public bool IsCharging => charging;
        public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeTime) : 0f;

        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        readonly Collider[] nearbyColliders = new Collider[64];
        readonly int[] comboStateHashes = new int[3];
        ThirdPersonPlayerController controller;
        bool attackQueued;
        bool comboContinueQueued;
        bool charging;
        bool attacking;
        bool activeAttackEntered;
        bool airAttackUsed;
        bool airImpactArmed;
        int comboStep = -1;
        int lastAttackInputFrame = -1;
        float queueExpiresAt;
        float chargeStartedAt;
        float attackStartedAt;
        float lastFireTime = -10f;
        float swingDamage;
        int lightStateHash;
        int heavyStateHash;
        int chargeStateHash;
        int chargeTimeHash;
        int jumpSlashStateHash;
        int activeAttackHash;

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
            lightStateHash = Animator.StringToHash("Base Layer." + attackTrigger);
            heavyStateHash = Animator.StringToHash("Base Layer." + heavyAttackState);
            chargeStateHash = Animator.StringToHash("Base Layer." + chargeState);
            chargeTimeHash = Animator.StringToHash(chargeTimeParameter);
            jumpSlashStateHash = Animator.StringToHash("Base Layer." + jumpSlashState);
            comboStateHashes[0] = Animator.StringToHash("Base Layer." + comboOneState);
            comboStateHashes[1] = Animator.StringToHash("Base Layer." + comboTwoState);
            comboStateHashes[2] = Animator.StringToHash("Base Layer." + comboThreeState);
            if (swingAudio == null) swingAudio = GetComponent<AudioSource>();
            if (stickHitbox != null) stickHitbox.enabled = false;
        }

        void Update()
        {
            if (PauseSettingsMenu.IsOpen)
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
            if (PauseSettingsMenu.IsOpen || charging || Cursor.lockState != CursorLockMode.Locked)
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
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (animator.IsInTransition(0) &&
                        animator.GetNextAnimatorStateInfo(0).fullPathHash == activeAttackHash)
                        state = animator.GetNextAnimatorStateInfo(0);
                    if (state.fullPathHash == activeAttackHash &&
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
            else
                PlayAttack(false, damage); // Existing scenes still work until their controller is upgraded.
        }

        void StartJumpSlash()
        {
            if (animator == null || !animator.HasState(0, jumpSlashStateHash))
            {
                Debug.LogError("Sahur Jump Slash state is missing from the Animator controller.", this);
                return;
            }

            airAttackUsed = true;
            airImpactArmed = true;
            attackQueued = false;
            comboStep = -1;
            comboContinueQueued = false;
            BeginAttack(jumpSlashStateHash, damage * jumpSlashDamageMultiplier, 0f, 0.08f);
            if (swingAudio != null && heavySwish != null)
                swingAudio.PlayOneShot(heavySwish);
        }

        void StartComboStage(int stage)
        {
            comboStep = stage;
            comboContinueQueued = false;
            float multiplier = stage == 1 ? comboTwoDamageMultiplier :
                stage == 2 ? comboThreeDamageMultiplier : 1f;
            BeginAttack(comboStateHashes[stage], damage * multiplier, 0f,
                stage == 0 ? 0.08f : comboBlendTime);
            if (swingAudio != null && comboSwishes != null &&
                stage < comboSwishes.Length && comboSwishes[stage] != null)
                swingAudio.PlayOneShot(comboSwishes[stage]);
        }

        void StartCharge()
        {
            if (animator == null || !animator.HasState(0, chargeStateHash) ||
                !animator.HasState(0, heavyStateHash))
            {
                Debug.LogError("Sahur charge states are missing from the Animator controller.", this);
                return;
            }

            attackQueued = false;
            comboContinueQueued = false;
            controller?.BeginAttackFacing();
            charging = true;
            chargeStartedAt = Time.time;
            if (stickHitbox != null) stickHitbox.enabled = false;
            // Play the real hand draw-back from the source clip across the
            // charging period, then stop at its prepared striking pose.
            UpdateChargePose();
            animator.CrossFadeInFixedTime(chargeStateHash, 0.10f, 0, 0f);
        }

        void ReleaseCharge()
        {
            // Preserve the sampled clip phase when handing charge over to heavy.
            float releaseTime = animator.GetFloat(chargeTimeHash) *
                (chargeClip != null ? chargeClip.length : 0f);
            charging = false;
            float multiplier = Mathf.Lerp(minChargeDamageMultiplier, maxChargeDamageMultiplier,
                Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeTime));
            PlayAttack(true, damage * multiplier, releaseTime);
            if (swingAudio != null && heavySwish != null)
                swingAudio.PlayOneShot(heavySwish);
        }

        void CancelCharge()
        {
            charging = false;
            controller?.EndAttackFacing();
            animator.SetFloat(chargeTimeHash, 0f);
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null)
                animator.CrossFadeInFixedTime("Locomotion", 0.14f, 0, 0f);
        }

        void UpdateChargePose()
        {
            if (animator == null || chargeClip == null) return;
            // Clamp old scene overrides too; never hold the staff across the head.
            animator.SetFloat(chargeTimeHash,
                Mathf.Clamp(chargePoseTime, 0f, SafeChargePoseTime) * Charge01);
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
                heavy ? 0.09f : 0.11f);
        }

        void BeginAttack(int stateHash, float amount, float offset, float blend)
        {
            controller?.BeginAttackFacing();
            animator.ResetTrigger(attackTrigger);
            activeAttackHash = stateHash;
            swingDamage = amount;
            attacking = true;
            activeAttackEntered = false;
            attackStartedAt = Time.time;
            lastFireTime = Time.time;
            hitThisSwing.Clear();
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (stateHash == heavyStateHash && offset > 0f && chargeClip != null)
            {
                // A fixed-time offset is affected by the destination state's
                // playback speed. A clip-normalized offset keeps the same hand
                // pose even though Heavy Attack plays at 0.9x speed.
                float sourceDuration = Mathf.Max(0.01f, animator.GetCurrentAnimatorStateInfo(0).length);
                animator.CrossFade(stateHash, blend / sourceDuration, 0, offset / chargeClip.length);
            }
            else animator.CrossFadeInFixedTime(stateHash, blend, 0, offset);
        }

        void UpdateAttackProgress()
        {
            if (!attacking || animator == null || Time.time - attackStartedAt < 0.08f)
                return;

            var current = animator.GetCurrentAnimatorStateInfo(0);
            bool inActiveAttack = current.fullPathHash == activeAttackHash;
            bool transitioningToAttack = animator.IsInTransition(0) &&
                                         animator.GetNextAnimatorStateInfo(0).fullPathHash == activeAttackHash;
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
            if (comboStep >= 0 && comboContinueQueued && inActiveAttack &&
                current.normalizedTime >= comboChainPoint)
            {
                StartComboStage(comboStep + 1);
                return;
            }

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
            activeAttackEntered = false;
            comboStep = -1;
            comboContinueQueued = false;
            controller?.EndAttackFacing();
        }

        void UpdateHitbox()
        {
            if (stickHitbox == null || animator == null || !attacking)
            {
                if (stickHitbox != null) stickHitbox.enabled = false;
                return;
            }

            var info = animator.GetCurrentAnimatorStateInfo(0);
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
                start = activeAttackHash == heavyStateHash ? heavySwingWindowStart : swingWindowStart;
                end = activeAttackHash == heavyStateHash ? heavySwingWindowEnd : swingWindowEnd;
            }
            stickHitbox.enabled = info.fullPathHash == activeAttackHash &&
                                  info.normalizedTime >= start && info.normalizedTime <= end &&
                                  !animator.IsInTransition(0);
            if (stickHitbox.enabled)
                ScanHits();
        }

        void ScanHits()
        {
            // A trigger enabled mid-swing can already overlap an enemy and never
            // receive OnTriggerEnter. Scan the stick's actual volume each frame.
            Bounds bounds = stickHitbox.bounds;
            int count = Physics.OverlapSphereNonAlloc(bounds.center, bounds.extents.magnitude,
                nearbyColliders, hitMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                TryHit(nearbyColliders[i]);
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
                    target.ApplyDamage(damage * jumpSlashDamageMultiplier, center);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (stickHitbox == null || !stickHitbox.enabled) return;
            TryHit(other);
        }

        void TryHit(Collider other)
        {
            if (other == null || other == stickHitbox || other.transform.IsChildOf(transform)) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;
            var targetComponent = target as Component;
            if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag) &&
                (targetComponent == null ||
                 (!targetComponent.CompareTag(enemyTag) &&
                  !targetComponent.transform.root.CompareTag(enemyTag)))) return;
            if (!Physics.ComputePenetration(stickHitbox, stickHitbox.transform.position,
                    stickHitbox.transform.rotation, other, other.transform.position,
                    other.transform.rotation, out _, out _)) return;
            if (hitThisSwing.Add(target))
                target.ApplyDamage(swingDamage, stickHitbox.bounds.center);
        }

        void OnDisable()
        {
            controller?.EndAttackFacing(true);
            charging = false;
            attacking = false;
            activeAttackEntered = false;
            attackQueued = false;
            comboContinueQueued = false;
            airImpactArmed = false;
            airAttackUsed = false;
            comboStep = -1;
            if (stickHitbox != null) stickHitbox.enabled = false;
            if (animator != null) animator.SetFloat(chargeTimeHash, 0f);
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(float amount, Vector3 hitPoint);
    }
}
