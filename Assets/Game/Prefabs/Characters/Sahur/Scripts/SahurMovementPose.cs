using UnityEngine;
/// <summary>Small inertial responses layered over authored locomotion, never another movement owner.</summary>
[DefaultExecutionOrder(1100),DisallowMultipleComponent]
public sealed class SahurMovementPose : MonoBehaviour
{
    internal enum WaterTransition { None, Enter, WadeOut, Breach }

    ThirdPersonPlayerController player;Animator animator;Story1WreckRider rider;
    Vector3 previousVelocity;float previousYaw,lean,bank;
    WaterTransition waterTransition;
    float waterTransitionStart;
    float waterEntryImpact;

    public float PaddleWaterEngagement=>0; // Historical integration compatibility.
    void Awake(){player=GetComponent<ThirdPersonPlayerController>();previousYaw=transform.eulerAngles.y;}

    internal void BeginWaterTransition(WaterTransition transition, float entryImpact = 0f)
    {
        waterTransition = transition;
        waterTransitionStart = Time.time;
        waterEntryImpact = entryImpact;
    }

    internal void ClearWaterTransition() => waterTransition = WaterTransition.None;

    void LateUpdate()
    {
        if (player == null || !player.enabled || player.ExternalControlLock)
        {
            ClearWaterTransition();
            return;
        }
        if (PauseSettingsMenu.IsOpen || Time.deltaTime <= 0) return;
        animator=player.CharacterAnimator;if(animator==null || !animator.isHuman)return;
        if(rider==null)rider=GetComponent<Story1WreckRider>();
        if(rider!=null && rider.IsClimbing){ClearWaterTransition();rider.ApplyClimbSupportPose(animator);return;}
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
            if (chest != null) chest.localRotation *= Quaternion.Euler(lean, 0, bank);
            if (neck != null) neck.localRotation *= Quaternion.Euler(-lean * .35f, 0, -bank * .45f);
        }
        ApplyWaterTransition();
        // Original locomotion playback; attack and charge clocks remain independent.
        if(animator.runtimeAnimatorController!=null && animator.runtimeAnimatorController.name=="SahurGameplay")
        {
            animator.SetFloat("LocomotionRate",1);
        }
    }

    void ApplyWaterTransition()
    {
        if (waterTransition == WaterTransition.None) return;
        if (!player.CanPoseWaterTransition(waterTransition))
        {
            ClearWaterTransition();
            return;
        }

        float duration = waterTransition switch
        {
            WaterTransition.Enter => Mathf.Lerp(.30f, .40f, waterEntryImpact),
            WaterTransition.WadeOut => .30f,
            _ => .24f
        };
        float phase = (Time.time - waterTransitionStart) / duration;
        if (phase >= 1f)
        {
            ClearWaterTransition();
            return;
        }

        // Start on the evaluated pose and hand back to the current clip at the
        // end. Only the arms change, so head clearance and foot support stay
        // with the original locomotion and capsule.
        float weight = Mathf.SmoothStep(0f, 1f, phase / .23f) *
            (1f - Mathf.SmoothStep(0f, 1f, (phase - .45f) / .55f));
        float sweep = Mathf.SmoothStep(0f, 1f, (phase - .14f) / .66f);
        switch (waterTransition)
        {
            case WaterTransition.Enter:
                // Reach into the water, then sweep down into the first stroke.
                PoseArms(Vector3.Lerp(new Vector3(.33f, -.12f, .93f), new Vector3(.60f, -.70f, .24f), sweep),
                    Vector3.Lerp(new Vector3(.18f, .10f, 1f), new Vector3(.16f, -.82f, .38f), sweep),
                    weight * Mathf.Lerp(.52f, .78f, waterEntryImpact), .93f);
                break;
            case WaterTransition.WadeOut:
                // The free hand finishes a low pull and opens for balance as
                // the feet take over. Keep the weapon-side arm near its grip.
                PoseArms(Vector3.Lerp(new Vector3(.31f, -.68f, .64f), new Vector3(.66f, -.72f, .08f), sweep),
                    Vector3.Lerp(new Vector3(.11f, -.30f, .94f), new Vector3(.45f, -.72f, .15f), sweep),
                    weight * .70f, .34f);
                break;
            case WaterTransition.Breach:
                // Both forearms press down and back before the jump pose rises.
                PoseArms(new Vector3(.48f, -.78f, Mathf.Lerp(.45f, -.22f, sweep)),
                    new Vector3(.21f, -.87f, Mathf.Lerp(.38f, -.40f, sweep)), weight * .74f, .70f);
                break;
        }
    }

    void PoseArms(Vector3 upper, Vector3 forearm, float weight, float rightWeight)
    {
        PoseArm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            new Vector3(-upper.x, upper.y, upper.z), new Vector3(-forearm.x, forearm.y, forearm.z), weight);
        PoseArm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            upper, forearm, weight * rightWeight);
    }

    void PoseArm(HumanBodyBones shoulderBone, HumanBodyBones elbowBone, HumanBodyBones handBone,
        Vector3 upper, Vector3 forearm, float weight)
    {
        var shoulder = animator.GetBoneTransform(shoulderBone);
        var elbow = animator.GetBoneTransform(elbowBone);
        var hand = animator.GetBoneTransform(handBone);
        if (shoulder == null || elbow == null || hand == null) return;

        AimBone(shoulder, elbow, transform.TransformDirection(upper), weight);
        AimBone(elbow, hand, transform.TransformDirection(forearm), weight);
    }

    static void AimBone(Transform bone, Transform tip, Vector3 direction, float weight)
    {
        Vector3 current = tip.position - bone.position;
        if (current.sqrMagnitude < .000001f) return;
        Quaternion turn = Quaternion.RotateTowards(Quaternion.identity,
            Quaternion.FromToRotation(current, direction), 85f);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * bone.rotation;
    }
}
