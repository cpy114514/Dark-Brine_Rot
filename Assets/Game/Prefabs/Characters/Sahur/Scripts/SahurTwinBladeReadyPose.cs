using UnityEngine;

namespace Mavis
{
    // Keep the authored arms and wrists during movement; set the blade angle only at rest.
    [DisallowMultipleComponent, DefaultExecutionOrder(-15)]
    public sealed class SahurTwinBladeReadyPose : MonoBehaviour
    {
        SahurWeaponLoadout weapons;
        SahurAttack attack;
        ThirdPersonPlayerController movement;
        SahurSwimmingWeapon swimming;
        Animator animator;
        Transform leftHand, rightHand;
        Quaternion leftCorrection = Quaternion.identity, rightCorrection = Quaternion.identity;
        [Min(.01f)] public float readyBlendIn = .12f;
        [Min(.01f)] public float readyBlendOut = .08f;
        float weight;
        void Awake()
        {
            weapons=GetComponent<SahurWeaponLoadout>();attack=GetComponent<SahurAttack>();
            movement=GetComponent<ThirdPersonPlayerController>();
            swimming=GetComponent<SahurSwimmingWeapon>();
        }
        bool Bind()
        {
            if (!attack || !attack.animator || !attack.animator.isHuman) return false;
            if (animator==attack.animator && leftHand && rightHand) return true;
            animator=attack.animator;
            leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            return leftHand && rightHand;
        }
        void LateUpdate()=>Apply(Time.deltaTime);
        public void Apply(float dt)
        {
            if (!weapons || !weapons.UsesTwinBlades || !movement || !movement.enabled || movement.Swimming ||
                movement.ExternalControlLock || attack.CinematicHandoffPending || (swimming && swimming.Climbing) || !Bind())
            {weight=0;return;}
            // Ease the existing idle correction away during startup instead of
            // dropping the wrist angle in one frame. Never retarget a moving attack.
            bool idle = !attack.IsCombatMotionActive && !SahurTwinBladeLocomotion.IsMoving(animator) &&
                animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion");
            weight=Mathf.MoveTowards(weight,idle?1:0,Mathf.Max(0,dt)/Mathf.Max(.01f,idle?readyBlendIn:readyBlendOut));
            if(weight<=0)return;
            Pose(leftHand,-1,idle,ref leftCorrection);
            Pose(rightHand,1,idle,ref rightCorrection);
        }
        void Pose(Transform hand,float side,bool updateTarget,ref Quaternion correction)
        {
            if(updateTarget)
            {
                Vector3 localShaft=SahurWeaponLoadout.GripShaft(side);
                const float elevation = 35f * Mathf.Deg2Rad;
                Vector3 readyDirection=transform.forward*Mathf.Cos(elevation)+transform.up*Mathf.Sin(elevation);
                var target=Quaternion.FromToRotation(hand.TransformDirection(localShaft),readyDirection)*hand.rotation;
                correction=Quaternion.Inverse(hand.rotation)*target;
            }
            hand.localRotation*=Quaternion.Slerp(Quaternion.identity,correction,weight);
        }
        void OnDisable(){weight=0;leftCorrection=rightCorrection=Quaternion.identity;}
    }
}
