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
        Animator animator;
        Transform leftHand, rightHand;
        float weight;
        void Awake()
        {
            weapons=GetComponent<SahurWeaponLoadout>();attack=GetComponent<SahurAttack>();
            movement=GetComponent<ThirdPersonPlayerController>();
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
                (GetComponent<SahurSwimmingWeapon>()?.Climbing??false) || !Bind()) {weight=0;return;}
            // Sprint, dodge, air motion and attacks retain every authored arm/wrist rotation.
            if (attack.IsCombatMotionActive || SahurTwinBladeLocomotion.IsMoving(animator) ||
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")) {weight=0;return;}
            weight=Mathf.MoveTowards(weight,1,Mathf.Max(0,dt)/.12f);
            if(weight<=0)return;
            Pose(leftHand,-1);
            Pose(rightHand,1);
        }
        void Pose(Transform hand,float side)
        {
            Vector3 localShaft=SahurWeaponLoadout.GripShaft(side);
            const float elevation = 35f * Mathf.Deg2Rad;
            Vector3 readyDirection=transform.forward*Mathf.Cos(elevation)+transform.up*Mathf.Sin(elevation);
            hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.FromToRotation(hand.TransformDirection(localShaft),readyDirection)*hand.rotation,weight);
        }
    }
}
