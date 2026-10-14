using UnityEngine;
/// <summary>Small inertial responses layered over authored locomotion, never another movement owner.</summary>
[DefaultExecutionOrder(1100),DisallowMultipleComponent]
public sealed class SahurMovementPose : MonoBehaviour
{
    ThirdPersonPlayerController player;Animator animator;Story1WreckRider rider;
    Vector3 previousVelocity;float previousYaw,lean,bank;
    public float PaddleWaterEngagement=>0; // Historical integration compatibility.
    void Awake(){player=GetComponent<ThirdPersonPlayerController>();previousYaw=transform.eulerAngles.y;}
    void LateUpdate()
    {
        if(player==null || !player.enabled || player.ExternalControlLock || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        animator=player.CharacterAnimator;if(animator==null || !animator.isHuman)return;
        if(rider==null)rider=GetComponent<Story1WreckRider>();
        if(rider!=null && rider.IsClimbing){rider.ApplyClimbSupportPose(animator);return;}
        Vector3 velocity=player.WorldVelocity;
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
    }
}
