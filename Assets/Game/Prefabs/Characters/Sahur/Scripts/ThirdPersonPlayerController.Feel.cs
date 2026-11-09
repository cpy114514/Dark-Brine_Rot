using Mavis;using UnityEngine;
public sealed partial class ThirdPersonPlayerController
{
    [Header("Control feel")]
    [Tooltip("Use the selected CombatFeelProfile experiment without overwriting the authored scene speeds.")]
    public bool useControlPreset = true;
    public CombatFeelProfile ControlFeel => combat && combat.feel ? combat.feel : CombatFeelProfile.Default;
    bool TunedControls => useControlPreset && ControlFeel.useControlTuning;
    CombatFeelProfile.ControlResponse Response => ControlFeel.Controls;

    static readonly int LocomotionState = Animator.StringToHash("Base Layer.Locomotion");

    float WalkStrideScale(float walkSpeed, float deltaTime)
    {
        if (motionState != 0 || walkSpeed <= 0f || !animator || !animator.isActiveAndEnabled ||
            animator.IsInTransition(0))
            return 1f;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != LocomotionState || state.length <= 0f)
            return 1f;

        float walkWeight = 1f - Mathf.Clamp01(4f * Mathf.Abs(animator.GetFloat(SpeedId) / walkSpeed - 1f));
        // The planted foot moves back fastest near 12% and 62% of Walk_Loop.
        // Average the two pulses over the frame so a full loop keeps its distance.
        const float radiansPerCycle = 4f * Mathf.PI;
        float phase = (Mathf.Repeat(state.normalizedTime, 1f) - .12f) * radiansPerCycle;
        float halfSpan = deltaTime * animator.speed * state.speed * state.speedMultiplier / state.length * radiansPerCycle * .5f;
        float average = Mathf.Abs(halfSpan) > .0001f ? Mathf.Sin(halfSpan) / halfSpan : 1f;
        return 1f + .10f * walkWeight * average * Mathf.Cos(phase + halfSpan);
    }

    Vector3 AdvanceLocomotion(Vector3 desired,bool grounded,float dt)
    {
        float rate=desired.sqrMagnitude>planarVelocity.sqrMagnitude ? acceleration : deceleration;
        if(TunedControls)
        {
            bool braking=desired.sqrMagnitude<.01f;
            bool redirecting=!braking && planarVelocity.sqrMagnitude>.01f && Vector3.Dot(planarVelocity.normalized,desired.normalized)<.8f;
            rate=braking ? deceleration*Response.braking : redirecting ? acceleration*Response.acceleration*Response.redirect :
                desired.sqrMagnitude>planarVelocity.sqrMagnitude ? acceleration*Response.acceleration : deceleration*Response.braking;
            if(!grounded)rate*=braking ? Response.airBraking : Response.airAcceleration;
        }
        if(SoftTraversal&&!grounded)rate*=marshmallow.airControl;
        return Vector3.MoveTowards(planarVelocity,desired,Mathf.Max(0,rate)*dt);
    }
    public float DodgeTimeRemaining => Mathf.Max(0,rollTimer-ActiveDodgeDuration*(perfectStep?0:1-Mathf.Clamp(rollAnimationExitPhase,.75f,.98f)));
    void ConfigureDodgePlayback()
    {
        if(!animator)return;
        int hash=Animator.StringToHash("RollPlaybackSpeed");
        foreach(var parameter in animator.parameters)if(parameter.nameHash==hash)
        {animator.SetFloat(hash,perfectStep?1f:rollDuration/Mathf.Max(.15f,ActiveDodgeDuration));break;}
    }
    Vector3 smoothControlFocus;
    float controlCameraDistance,manualCameraLookUntil;
    bool controlFollowValid;
    Vector3 FollowControlCamera(Vector3 focus,Quaternion rotation,float distance)
    {
        if(!controlFollowValid)
        {
            controlCameraDistance=Mathf.Min(distance,Vector3.Distance(playerCamera.transform.position,focus));
            smoothControlFocus=playerCamera.transform.position+rotation*Vector3.forward*controlCameraDistance;
            controlFollowValid=true;
        }
        float blend=1-Mathf.Exp(-cameraFollowSharpness*Response.cameraFollow*Time.deltaTime);
        smoothControlFocus=Vector3.Lerp(smoothControlFocus,focus,blend);
        // Obstructions contract immediately, and release at a separately tunable rate.
        controlCameraDistance=distance<controlCameraDistance ? distance : Mathf.Lerp(controlCameraDistance,distance,
            1-Mathf.Exp(-Response.cameraRelease*Time.deltaTime));
        // Smooth the moving anchor, not a second copy of mouse/stick rotation.
        return smoothControlFocus-rotation*Vector3.forward*controlCameraDistance;
    }
    void UpdateLockFootwork()
    {
        int layer=animator.GetLayerIndex("Lock-on Footwork");if(layer<1)return;
        bool use=TunedControls&&enemyLock&&enemyLock.IsLocked&&!IsSprinting&&!Swimming&&!ExternalControlLock&&
            !ExternalMovementLock&&rollTimer<=0&&airFlipTimer<=0&&motionState==0&&!(combat&&combat.IsCombatMotionActive);
        Vector3 local=Quaternion.Inverse(transform.rotation)*planarVelocity/Mathf.Max(.1f,moveSpeed);
        animator.SetFloat("LockMoveX",local.x,.055f,Time.deltaTime);animator.SetFloat("LockMoveY",local.z,.055f,Time.deltaTime);
        animator.SetLayerWeight(layer,Mathf.MoveTowards(animator.GetLayerWeight(layer),use?1:0,Time.deltaTime/(use?.12f:.08f)));
    }
}
