using UnityEngine;
/// <summary>Small inertial responses layered over authored locomotion, never another movement owner.</summary>
[DefaultExecutionOrder(1100),DisallowMultipleComponent]
public sealed class SahurMovementPose : MonoBehaviour
{
    ThirdPersonPlayerController player;Animator animator;Story1WreckRider rider;
    Vector3 previousVelocity,leftSole,rightSole;float previousYaw,lean,bank;
    bool solesReady;
    public float PaddleWaterEngagement { get; private set; }
    void Awake(){player=GetComponent<ThirdPersonPlayerController>();previousYaw=transform.eulerAngles.y;}
    void LateUpdate()
    {
        PaddleWaterEngagement=0;
        if(player==null || !player.enabled || player.ExternalControlLock || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        animator=player.CharacterAnimator;if(animator==null || !animator.isHuman)return;
        if(rider==null)rider=GetComponent<Story1WreckRider>();
        if(rider!=null && rider.IsClimbing){rider.ApplyClimbSupportPose(animator);return;}
        Vector3 velocity=rider!=null && rider.IsSurfing ? rider.DrivenBoard.Velocity : player.WorldVelocity;
        float dt=Mathf.Max(.001f,Time.deltaTime);
        Vector3 acceleration=(velocity-previousVelocity)/dt;float turn=Mathf.DeltaAngle(previousYaw,transform.eulerAngles.y)/dt;
        previousVelocity=velocity;previousYaw=transform.eulerAngles.y;
        lean=Mathf.Lerp(lean,Mathf.Clamp(Vector3.Dot(acceleration,transform.forward)*-.12f,-6,5),1-Mathf.Exp(-8*dt));
        bank=Mathf.Lerp(bank,Mathf.Clamp(-turn*.025f,-7,7),1-Mathf.Exp(-7*dt));
        // Keep the user's original walking/running/rolling presentation.
        bool ordinary=player.Motion.State==SahurControlState.Swimming;
        if(ordinary)
        {
            var chest=animator.GetBoneTransform(HumanBodyBones.Chest);var neck=animator.GetBoneTransform(HumanBodyBones.Neck);
            chest.localRotation*=Quaternion.Euler(lean,0,bank);
            neck.localRotation*=Quaternion.Euler(-lean*.35f,0,-bank*.45f);
        }
        // Original locomotion playback; attack and charge clocks remain independent.
        if(animator.runtimeAnimatorController!=null && animator.runtimeAnimatorController.name=="SahurGameplay")
        {
            animator.SetFloat("LocomotionRate",1);
        }
        if(rider==null || !rider.IsSurfing)return;
        var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if(!solesReady)
        {
            SahurSupportPose.MeasureSoles(animator,ref leftSole,ref rightSole);solesReady=true;
        }
        if(rider.IsPaddling)
        {
            // Preserve the water-stroking leg. Only the left supporting sole
            // is constrained to the real board; two-foot IK erased paddling.
            float supportGap=SahurSupportPose.SupportingFoot(animator,rider.DrivenBoard,leftSole);
            Vector3 waterFoot=right.TransformPoint(rightSole);
            // The authored outward sweep owns the arc. Correct only a small
            // surface-height mismatch; never pull a leg through solid timber
            // or propel the board while the foot is still sweeping air.
            if(rider.DrivenBoard.EdgeDistance(waterFoot)>.08f)
            {
                float phase=rider.PaddlePhase;
                float stroke=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.34f,phase))*
                    (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.61f,.72f,phase)));
                Vector3 contact=waterFoot;contact.y=rider.PaddleWaterHeight(contact)-.08f;
                SahurSupportPose.Anchor(animator,false,rightSole,contact,.35f*stroke);
                waterFoot=right.TransformPoint(rightSole);
                float depth=rider.PaddleWaterHeight(waterFoot)-waterFoot.y;
                if(supportGap<=.03f && rider.DrivenBoard.EdgeDistance(waterFoot)>.08f && depth>0)
                    PaddleWaterEngagement=Mathf.Clamp01(depth/.08f);
            }
        }
        else SahurSupportPose.Plant(animator,rider.DrivenBoard,leftSole,rightSole);
    }
}
