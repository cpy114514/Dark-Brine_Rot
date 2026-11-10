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
    readonly RaycastHit[] groundFollowHits=new RaycastHit[16];
    Vector3 FollowWalkableGround(Vector3 delta,bool supported)
    {
        if(!supported||!TunedControls||Response.groundFollowDistance<=0||verticalSpeed>0||delta.y>0||
            motionState!=0||rollTimer>0||airFlipTimer>0||SoftTraversal||OnFloatingWreckDeck||ExternalControlLock||
            ExternalMovementLock||WalkingTutorialConstraint!=null||delta.x*delta.x+delta.z*delta.z<.000001f)return delta;
        var bounds=characterController.bounds;
        float radius=Mathf.Max(.03f,Mathf.Min(bounds.extents.x,bounds.extents.z)*.92f);
        float lift=.06f+characterController.skinWidth*Mathf.Abs(transform.lossyScale.y);
        var origin=new Vector3(bounds.center.x+delta.x,bounds.min.y+radius+lift,bounds.center.z+delta.z);
        int count=Physics.SphereCastNonAlloc(origin,radius,Vector3.down,groundFollowHits,lift+Response.groundFollowDistance,~0,QueryTriggerInteraction.Ignore);
        float minimumNormal=Mathf.Cos(characterController.slopeLimit*Mathf.Deg2Rad),nearest=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var hit=groundFollowHits[i];
            if(!hit.collider||hit.transform.IsChildOf(transform)||CombatBodyCollision.IsActor(hit.collider)||
                hit.normal.y<minimumNormal||hit.distance>=nearest)continue;
            nearest=hit.distance;
        }
        if(float.IsPositiveInfinity(nearest))return delta;
        float down=origin.y-nearest-radius-bounds.min.y-.015f;
        // One collision-resolved Move owns the whole displacement. The probe
        // only adds the missing descent; steps, walls and ascent remain native.
        if(down<delta.y&&down>=-Response.groundFollowDistance)delta.y=down;
        return delta;
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
    int bladeFootworkLayer=-1;
    RuntimeAnimatorController footworkController;
    bool hasLocomotionRate;
    float locomotionReferenceSpeed=16.8f;
    static readonly int LocomotionRateId=Animator.StringToHash("LocomotionPlaybackSpeed");
    static readonly int LocomotionReferenceId=Animator.StringToHash("LocomotionReferenceSpeed");
    bool bladeFootworkActive;
    public bool BladeFootworkActive => bladeFootworkActive&&bladeFootworkLayer>0&&animator&&animator.GetLayerWeight(bladeFootworkLayer)>.01f;
    void CacheFootworkController()
    {
        if(footworkController==animator.runtimeAnimatorController)return;
        footworkController=animator.runtimeAnimatorController;bladeFootworkLayer=animator.GetLayerIndex("Mobile Blade Legs");hasLocomotionRate=false;
        foreach(var parameter in animator.parameters)
        {
            if(parameter.nameHash==LocomotionRateId)hasLocomotionRate=true;
            if(parameter.nameHash==LocomotionReferenceId)locomotionReferenceSpeed=Mathf.Max(.1f,parameter.defaultFloat);
        }
    }
    void MatchLocomotionStride(float speed)
    {
        CacheFootworkController();
        if(hasLocomotionRate)animator.SetFloat(LocomotionRateId,TunedControls&&Response.matchOverspeedStride?
            Mathf.Clamp(speed/locomotionReferenceSpeed,1,1.8f):1);
    }
    void UpdateLockFootwork()
    {
        int layer=animator.GetLayerIndex("Lock-on Footwork");if(layer<1)return;
        CacheFootworkController();
        bool use=TunedControls&&enemyLock&&enemyLock.IsLocked&&!IsSprinting&&!Swimming&&!ExternalControlLock&&
            !ExternalMovementLock&&rollTimer<=0&&airFlipTimer<=0&&motionState==0&&!(combat&&combat.IsCombatMotionActive);
        Vector3 actual=Vector3.ProjectOnPlane(WorldVelocity,Vector3.up);
        bladeFootworkActive=TunedControls&&Response.mobileBladeFootwork&&bladeFootworkLayer>0&&combat&&combat.UsesTwinBlades&&combat.IsGroundComboActive&&
            combat.CurrentComboStage<2&&actual.sqrMagnitude>.0225f&&verticalSpeed<=0&&IsGroundedOrOnSea()&&
            !Swimming&&!ExternalControlLock&&!ExternalMovementLock&&rollTimer<=0&&airFlipTimer<=0&&motionState==0;
        Vector3 local=Quaternion.Inverse(transform.rotation)*(bladeFootworkActive?actual:planarVelocity)/Mathf.Max(.1f,moveSpeed);
        animator.SetFloat("LockMoveX",local.x,.055f,Time.deltaTime);animator.SetFloat("LockMoveY",local.z,.055f,Time.deltaTime);
        animator.SetLayerWeight(layer,Mathf.MoveTowards(animator.GetLayerWeight(layer),use?1:0,Time.deltaTime/(use?.12f:.08f)));
        if(bladeFootworkLayer>0)animator.SetLayerWeight(bladeFootworkLayer,Mathf.MoveTowards(animator.GetLayerWeight(bladeFootworkLayer),
            bladeFootworkActive?1:0,Time.deltaTime/(bladeFootworkActive?.09f:.065f)));
    }
}
