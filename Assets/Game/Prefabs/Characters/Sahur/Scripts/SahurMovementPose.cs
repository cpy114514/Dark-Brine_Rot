using UnityEngine;
/// <summary>Small inertial responses layered over authored locomotion, never another movement owner.</summary>
[DefaultExecutionOrder(1100),DisallowMultipleComponent]
public sealed class SahurMovementPose : MonoBehaviour
{
    ThirdPersonPlayerController player;Animator animator;Story1WreckRider rider;
    Vector3 previousVelocity,leftSole,rightSole;float previousYaw,lean,bank;
    bool solesReady;
    void Awake(){player=GetComponent<ThirdPersonPlayerController>();previousYaw=transform.eulerAngles.y;}
    void LateUpdate()
    {
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
        bool ordinary=player.Motion.State==SahurControlState.Grounded || player.Motion.State==SahurControlState.Swimming;
        if(ordinary)
        {
            var chest=animator.GetBoneTransform(HumanBodyBones.Chest);var neck=animator.GetBoneTransform(HumanBodyBones.Neck);
            chest.localRotation*=Quaternion.Euler(lean,0,bank);
            neck.localRotation*=Quaternion.Euler(-lean*.35f,0,-bank*.45f);
        }
        // Only locomotion playback changes; attack, jump and charge clocks retain their own timing.
        if(animator.runtimeAnimatorController!=null && animator.runtimeAnimatorController.name=="SahurGameplay")
        {
            float speed=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude;
            // Measured on the actual retargeted Sahur: walk 3.59m/s, run 7.44m/s backward foot travel.
            // Below the run threshold the speed BlendTree already scales the gait through its weights.
            animator.SetFloat("LocomotionRate",Mathf.Clamp(Mathf.Max(1,speed/7.44f),1,2.4f),.1f,dt);
        }
        if(rider==null || !rider.IsSurfing)return;
        var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if(!solesReady)
        {
            float offset=Mathf.Min(left.position.y,right.position.y)-(transform.position.y+player.LowestFootWorldOffset);
            leftSole=left.InverseTransformVector(Vector3.down*offset);rightSole=right.InverseTransformVector(Vector3.down*offset);solesReady=true;
        }
        SahurSupportPose.Plant(animator,rider.DrivenBoard,leftSole,rightSole);
    }
}
