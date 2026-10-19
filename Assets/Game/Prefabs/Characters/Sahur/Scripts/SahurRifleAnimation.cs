using UnityEngine;

namespace Mavis
{
    // Layers reuse the existing Animator graph, so rolls, swimming and melee keep their original states.
    [DisallowMultipleComponent, DefaultExecutionOrder(1200)]
    public sealed class SahurRifleAnimation : MonoBehaviour
    {
        static readonly int Moving = Animator.StringToHash("RifleMoving"), X = Animator.StringToHash("RifleX"), Y = Animator.StringToHash("RifleY"),
            Playback = Animator.StringToHash("RiflePlayback"), Forward = Animator.StringToHash("RifleWasForward"), Dead = Animator.StringToHash("RifleDead");
        SahurWeaponLoadout loadout;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        SahurRifleCombat rifleCombat;
        CharacterController capsule;
        Animator animator;
        Transform rightHand, leftHand, rightUpperArm, rightLowerArm, leftUpperArm, leftLowerArm, spine, headBone;
        bool gripReady, hasPoseSnapshot;
        Vector3 lastGripOffset;
        Transform[] poseBones;
        Vector3[] posePositions;
        Quaternion[] poseRotations;
        Vector3 frozenGunPosition;
        Quaternion frozenGunRotation;
        static readonly Vector3 RightPalm = new Vector3(-.015f, .075f, -.01f), LeftPalm = new Vector3(.015f, .075f, -.01f);
        static readonly Vector3 PistolGrip = new Vector3(.17f, -.085f, 0);
        [SerializeField] Vector3 foregrip = new Vector3(-.08f, -.035f, 0);
        [SerializeField] float stanceAngle = 55, carrySide = .55f, carryForward = .04f;
        [SerializeField] Vector3 gripOffset = new Vector3(.57f, -.24f, .24f);
        int holding = -1;
        int locomotion = -1, fire = -1;
        float deathTime, deathLength, nearAimBlend;
        bool movingLastFrame, wasForward, firingLastFrame;
        public bool DeathPoseActive { get; private set; }
        public bool DeathAnimationPlaying => DeathPoseActive && deathTime < deathLength;

        void Awake()
        {
            loadout = GetComponent<SahurWeaponLoadout>(); movement = GetComponent<ThirdPersonPlayerController>();
            health = GetComponent<PlayerHealth>(); capsule = GetComponent<CharacterController>();
        }
        bool Prepare()
        {
            if (locomotion >= 0) return animator;
            animator = movement ? movement.CharacterAnimator : null;
            if (!animator || !animator.isHuman) return false;
            var controller = Resources.Load<RuntimeAnimatorController>("Equipment/M4A1/SahurRifle");
            if (!controller) return false;
            animator.runtimeAnimatorController = controller;
            locomotion = animator.GetLayerIndex("Rifle Locomotion"); fire = animator.GetLayerIndex("Rifle Fire Upper Body");
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand); leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm); rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm); leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine); headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            holding = animator.GetLayerIndex("Rifle Holding Upper Body");
            poseBones = new[] { spine, headBone, rightUpperArm, rightLowerArm, rightHand, leftUpperArm, leftLowerArm, leftHand };
            posePositions = new Vector3[poseBones.Length]; poseRotations = new Quaternion[poseBones.Length];
            var clip = Resources.Load<AnimationClip>("Equipment/M4A1/Animations/RifleDeath"); deathLength = clip ? clip.length : 0;
            animator.SetFloat(Playback, 1);
            return locomotion >= 0 && fire >= 0;
        }
        void Update()
        {
            if (DeathPoseActive)
            {
                if (health && health.currentHealth > 0) ResetDeath();
                else
                {
                    if (DeathAnimationPlaying)
                    {
                        float dt = Mathf.Min(Time.unscaledDeltaTime, deathLength - deathTime);
                        animator.Update(dt); deathTime += dt;
                        if (!DeathAnimationPlaying) animator.speed = 0;
                    }
                    return;
                }
            }
            if (!loadout || (!loadout.UsesRifle && locomotion < 0) || !Prepare()) return;
            if (Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) return;
            bool active = loadout.UsesRifle && movement.RifleLocomotionAllowed && (!health || health.currentHealth > 0);
            float weight = Mathf.MoveTowards(animator.GetLayerWeight(locomotion), active ? 1 : 0, Time.deltaTime * 12);
            animator.SetLayerWeight(locomotion, weight);
            if (holding >= 0) animator.SetLayerWeight(holding, weight);
            Vector3 velocity = capsule && capsule.enabled ? capsule.velocity : Vector3.zero;
            velocity.y = 0;
            bool moving = active && movement.RequestedMoveDirection.sqrMagnitude > .01f && velocity.sqrMagnitude > .09f;
            Vector3 local = transform.InverseTransformDirection(velocity.normalized);
            if (moving) wasForward = local.z > .6f && Mathf.Abs(local.x) < .65f;
            if (moving && !movingLastFrame && weight < .1f) animator.Play("Rifle Move", locomotion, 0);
            animator.SetBool(Moving, moving); animator.SetBool(Forward, wasForward);
            animator.SetFloat(X, moving ? local.x : 0, .09f, Time.deltaTime);
            animator.SetFloat(Y, moving ? local.z : 0, .09f, Time.deltaTime);
            animator.SetFloat(Playback, Mathf.Clamp(velocity.magnitude / Mathf.Max(1, movement.moveSpeed), .75f, 1.5f));
            movingLastFrame = moving;
            if (!rifleCombat) rifleCombat = GetComponent<SahurRifleCombat>();
            bool firing = active && rifleCombat && rifleCombat.FirePoseActive;
            if (firing && !firingLastFrame) animator.Play("Rifle Fire", fire, 0);
            animator.SetLayerWeight(fire, Mathf.MoveTowards(animator.GetLayerWeight(fire), firing ? .22f : 0, Time.deltaTime * 5));
            firingLastFrame = firing;
        }
        public bool TryBeginDeath()
        {
            if (!loadout || !loadout.UsesRifle || !Prepare() || deathLength <= 0) return false;
            gripReady = hasPoseSnapshot = false;
            if (holding >= 0) animator.SetLayerWeight(holding, 0);
            DeathPoseActive = true; deathTime = 0;
            animator.speed = 1; animator.SetLayerWeight(locomotion, 1); animator.SetLayerWeight(fire, 0);
            animator.SetBool(Dead, true); animator.Play("Rifle Death", locomotion, 0); animator.Update(0);
            return true;
        }
        public void ResetDeath()
        {
            if (!DeathPoseActive) return;
            DeathPoseActive = false; deathTime = 0; movingLastFrame = firingLastFrame = false;
            animator.SetBool(Dead, false); animator.SetLayerWeight(fire, 0); animator.SetLayerWeight(locomotion, 0);
            if (holding >= 0) animator.SetLayerWeight(holding, 0);
            animator.Play("Rifle Idle", locomotion, 0);
        }
        void CapturePose()
        {
            for (int i = 0; i < poseBones.Length; i++)
                if (poseBones[i]) { posePositions[i] = poseBones[i].localPosition; poseRotations[i] = poseBones[i].localRotation; }
            frozenGunPosition = loadout.HeldRifle.localPosition; frozenGunRotation = loadout.HeldRifle.localRotation;
            hasPoseSnapshot = true;
        }
        void RestorePausedPose()
        {
            if (!hasPoseSnapshot) return;
            for (int i = 0; i < poseBones.Length; i++)
                if (poseBones[i]) { poseBones[i].localPosition = posePositions[i]; poseBones[i].localRotation = poseRotations[i]; }
            loadout.HeldRifle.localPosition = frozenGunPosition; loadout.HeldRifle.localRotation = frozenGunRotation;
        }
        void LateUpdate()
        {
            if (loadout && !loadout.UsesRifle) { gripReady = hasPoseSnapshot = false; return; }
            if (DeathPoseActive || !loadout || !loadout.HeldRifle || !rightHand || !leftHand || locomotion < 0) return;
            // Animator can resample its authored pose at zero delta, so replay the corrected bones while paused.
            if (Time.timeScale <= 0 || Time.deltaTime <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) { RestorePausedPose(); return; }
            if (!movement.RifleLocomotionAllowed) { gripReady = hasPoseSnapshot = false; return; }
            if (!rifleCombat) rifleCombat = GetComponent<SahurRifleCombat>();
            if (rifleCombat) AlignToAim(rifleCombat.GetAimPoint());
        }
        Quaternion GunRotation(Vector3 direction)
        {
            Vector3 up = Vector3.ProjectOnPlane(transform.up, direction);
            if (up.sqrMagnitude < .001f) up = Vector3.ProjectOnPlane(transform.forward, direction);
            // The imported barrel points along model -X, with model +Y facing up.
            return Quaternion.LookRotation(direction, up.normalized) * Quaternion.Euler(0, 90, 0);
        }
        void AlignToAim(Vector3 target)
        {
            if (!rightUpperArm || !rightLowerArm || !leftUpperArm || !leftLowerArm) return;
            var rifle = loadout.HeldRifle;
            float scale = Mathf.Abs(rifle.lossyScale.x), bodyScale = Mathf.Abs(transform.lossyScale.y) / 3;
            Vector3 up = transform.up;
            Vector3 center = (rightUpperArm.position + leftUpperArm.position) * .5f;
            float targetDistance = Vector3.Distance(target, center);
            Vector3 direction = (target - center).normalized;
            Vector3 cameraDirection = rifleCombat.AimRay.direction;
            float closest = scale * .64f + .25f;
            float wantedNear = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(closest + 1.2f * bodyScale, closest, targetDistance));
            nearAimBlend = gripReady && Time.deltaTime > 0 ? Mathf.MoveTowards(nearAimBlend, wantedNear, 5 * Time.deltaTime) : wantedNear;
            if (nearAimBlend > 0)
            {
                direction = Vector3.Slerp(direction, cameraDirection, nearAimBlend).normalized;
                target = center + direction * Mathf.Lerp(targetDistance, 80 * bodyScale, nearAimBlend);
            }
            // Carry orientation follows the camera, not the changing distance of a nearby ray hit.
            Vector3 forward = Vector3.ProjectOnPlane(cameraDirection, up).normalized;
            if (forward.sqrMagnitude < .01f) forward = transform.forward;
            Vector3 side = Vector3.Cross(up, forward).normalized;
            // Turn the shoulder line so the support hand can reach the handguard around the broad torso.
            // Only rotate bones: never stretch the arm segments or teleport either wrist.
            if (spine && headBone)
            {
                Vector3 axis = headBone.position - spine.position;
                // Retain a small forward lean instead of straightening the torso like a rigid pole.
                Vector3 postureUp = (up + forward * .055f).normalized;
                if (axis.sqrMagnitude > .001f) spine.rotation = Quaternion.FromToRotation(axis, postureUp) * spine.rotation;
                Vector3 bodyRight = Vector3.ProjectOnPlane(rightUpperArm.position - leftUpperArm.position, up).normalized;
                Vector3 bodyForward = Vector3.Cross(bodyRight, up);
                Vector3 stanceForward = Quaternion.AngleAxis(stanceAngle, up) * forward;
                spine.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(bodyForward, stanceForward, up), up) * spine.rotation;
                Vector3 look = Vector3.RotateTowards(forward, direction, 30 * Mathf.Deg2Rad, 0);
                headBone.rotation = Quaternion.FromToRotation(headBone.right, look) * headBone.rotation;
            }
            center = (rightUpperArm.position + leftUpperArm.position) * .5f;
            // Lowering the muzzle raises the stock toward the face. Ease it outboard before contact.
            float lowerClearance = .14f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.72f, .94f, Vector3.Dot(cameraDirection, -up)));
            Vector3 preferred = new Vector3(gripOffset.x + lowerClearance, gripOffset.y, gripOffset.z) * bodyScale;
            if (!gripReady || Time.deltaTime <= 0) lastGripOffset = preferred;
            else lastGripOffset = Vector3.Lerp(lastGripOffset, preferred, 1 - Mathf.Exp(-14 * Time.deltaTime));
            Vector3 grip = center + side * lastGripOffset.x + up * lastGripOffset.y + forward * lastGripOffset.z;
            float rightReach = Vector3.Distance(rightUpperArm.position, rightLowerArm.position) + Vector3.Distance(rightLowerArm.position, rightHand.position);
            float leftReach = Vector3.Distance(leftUpperArm.position, leftLowerArm.position) + Vector3.Distance(leftLowerArm.position, leftHand.position);
            Quaternion rotation = GunRotation(direction), rightRotation = Quaternion.identity, leftRotation = Quaternion.identity;
            Vector3 rightOffset = Vector3.zero, leftOffset = Vector3.zero;
            Vector3 muzzleOffset = (SahurRifleCombat.MuzzleLocal - PistolGrip) * scale;
            for (int pass = 0; pass < 24; pass++)
            {
                rightRotation = Quaternion.LookRotation(-(rotation * Vector3.up), rotation * Vector3.left);
                // Support palm faces up; the knuckle row follows the barrel, rather than an imaginary vertical grip.
                leftRotation = Quaternion.LookRotation(rotation * Vector3.right, rotation * Vector3.forward);
                rightOffset = -(rightRotation * Vector3.Scale(RightPalm, rightHand.lossyScale));
                leftOffset = rotation * ((foregrip - PistolGrip) * scale) - leftRotation * Vector3.Scale(LeftPalm, leftHand.lossyScale);
                // Intersect two reach spheres with a fixed outboard carry plane. These convex
                // constraints cannot jump between opposite sides of the body like surface pushes.
                for (int fit = 0; fit < 5; fit++)
                {
                    Vector3 anchor = rightUpperArm.position - rightOffset;
                    grip = anchor + Vector3.ClampMagnitude(grip - anchor, rightReach * .97f);
                    anchor = leftUpperArm.position - leftOffset;
                    grip = anchor + Vector3.ClampMagnitude(grip - anchor, leftReach * .97f);
                    grip += side * Mathf.Max(0, (carrySide + lowerClearance) * bodyScale - Vector3.Dot(grip - center, side));
                    grip += forward * Mathf.Max(0, carryForward * bodyScale - Vector3.Dot(grip - center, forward));
                }
                if (pass < 23) rotation = GunRotation((target - (grip + rotation * muzzleOffset)).normalized);
            }
            gripReady = true;
            SolveArm(rightUpperArm, rightLowerArm, rightHand, grip + rightOffset, rightRotation,
                rightUpperArm.position + side * rightReach * .55f + forward * rightReach * .25f - up * rightReach * .8f);
            SolveArm(leftUpperArm, leftLowerArm, leftHand, grip + leftOffset, leftRotation,
                leftUpperArm.position - side * leftReach * .65f + forward * leftReach * .35f - up * leftReach * .75f);
            rifle.SetPositionAndRotation(grip - rotation * (PistolGrip * scale), rotation);
            CapturePose();
        }
        static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Quaternion rotation, Vector3 hint)
        {
            Vector3 shoulder = upper.position, toTarget = target - shoulder;
            float upperLength = Vector3.Distance(shoulder, lower.position), lowerLength = Vector3.Distance(lower.position, hand.position);
            float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLength - lowerLength) + .001f, upperLength + lowerLength - .001f);
            Vector3 direction = toTarget.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(hint - shoulder, direction).normalized;
            if (bend.sqrMagnitude < .001f) bend = Vector3.ProjectOnPlane(lower.position - shoulder, direction).normalized;
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
            float height = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            Vector3 elbow = shoulder + direction * along + bend * height;
            upper.rotation = Quaternion.FromToRotation(lower.position - shoulder, elbow - shoulder) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            hand.rotation = rotation;
        }
    }
}
