using UnityEngine;

/// <summary>Second Island's soft, low-gravity traversal. The player motor owns all movement.</summary>
[DefaultExecutionOrder(1500), DisallowMultipleComponent, RequireComponent(typeof(ThirdPersonPlayerController))]
public sealed class MarshmallowTraversal : MonoBehaviour
{
    [Header("Moonlike lift (world metres)")]
    [Min(.1f)] public float gravity = 9.6f;
    [Min(.1f)] public float jumpHeight = 4.8f;
    [Min(.1f)] public float walkingBoundHeight = 1.15f;
    [Min(.1f)] public float sprintingBoundHeight = 1.85f;
    [Range(.04f, .3f)] public float groundCompressionTime = .16f;
    [Range(.1f, 1f)] public float airControl = .45f;
    [Range(0f, .2f)] public float landingSquash = .085f;

    public bool Active => isActiveAndEnabled && gameObject.scene.path == "Assets/Scenes/Second Island.unity";
    public bool NormalGravity { get; private set; }
    public bool LowGravityActive => Active && !NormalGravity;
    public void SetNormalGravity(bool enabled)
    {
        if (NormalGravity == enabled) return;
        NormalGravity = enabled; contactTime = 0f;
        if (enabled) CancelSpacewalk();
    }
    float contactTime, landingAge = 1f;
    Transform visual;
    Vector3 restScale;
    readonly RaycastHit[] footing = new RaycastHit[16];
    MarshmallowIslandExperience experience;
    MarshmallowSpacewalk spacewalk;
    ThirdPersonPlayerController motor;
    CharacterController body;
    Mavis.SahurAttack combat;
    Animator soleAnimator;
    Vector3 leftSole, rightSole;
    float soleLift;
    public float FootPenetrationBefore { get; private set; }
    public float FootPenetrationAfter { get; private set; }
    public int CorrectedFeet { get; private set; }
    public void BeginSpacewalk(float speed)
    {
        if(!spacewalk)spacewalk=GetComponent<MarshmallowSpacewalk>();
        if(spacewalk)spacewalk.BeginBound(speed);
    }
    public void CancelSpacewalk()
    {
        if(!spacewalk)spacewalk=GetComponent<MarshmallowSpacewalk>();
        if(spacewalk)spacewalk.Cancel();
    }
    float impactScale = 1f;
    public float CameraOffset => Active && experience && experience.isActiveAndEnabled ? experience.CameraOffset : 0f;
    public float CameraPullback => Active && experience && experience.isActiveAndEnabled ? experience.CameraPullback : 0f;

    public void TickFeedback()
    {
        if (!experience) experience = GetComponent<MarshmallowIslandExperience>();
        if (experience && experience.isActiveAndEnabled)
        {
            var motor = GetComponent<ThirdPersonPlayerController>();
            experience.Tick(!motor.Swimming && !motor.ExternalControlLock && !motor.ExternalMovementLock);
        }
    }

    public float UphillLift(CharacterController body, Vector3 velocity)
    {
        // Match the floor's upward travel at takeoff. Otherwise a sprint into
        // a soft hill catches the rising floor before the bound has time to float.
        var bounds = body.bounds;
        var origin = new Vector3(bounds.center.x, bounds.min.y + .6f, bounds.center.z);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, footing, 1.6f, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.PositiveInfinity, lift = 0f;
        for (int i = 0; i < count; i++)
        {
            var hit = footing[i];
            if (hit.collider.transform.IsChildOf(transform) || Mavis.CombatBodyCollision.IsActor(hit.collider) ||
                Vector3.Angle(hit.normal, Vector3.up) > body.slopeLimit || hit.distance >= closest) continue;
            closest = hit.distance;
            lift = Mathf.Max(0f, -Vector3.Dot(hit.normal, velocity) / Mathf.Max(.3f, hit.normal.y));
        }
        return Mathf.Min(lift, Mathf.Sqrt(2f * gravity * sprintingBoundHeight));
    }

    public bool WantsBound(bool grounded, bool moving, bool allowed, float deltaTime)
    {
        if (!Active || !grounded || !moving || !allowed)
        {
            contactTime = 0f;
            return false;
        }
        contactTime += deltaTime;
        if (contactTime < groundCompressionTime) return false;
        contactTime = 0f;
        return true;
    }

    public void Land(float impactSpeed = 5f)
    {
        if(spacewalk)spacewalk.Land();
        landingAge = 0f;
        impactScale = Mathf.Lerp(.65f,1.2f,Mathf.InverseLerp(2f,15f,impactSpeed));
        if (!experience) experience = GetComponent<MarshmallowIslandExperience>();
        if (experience && experience.isActiveAndEnabled) experience.Landing(impactSpeed);
    }

    // Called after the motor restores the visual pose, before its camera follows.
    // Scale about the soles so the landing compresses into the ground without sinking the collider.
    public void ApplyVisual(Transform target, float footWorldOffset, float verticalSpeed, bool allowed)
    {
        if (target == null) return;
        if (visual == null) { visual = target; restScale = visual.localScale; }
        landingAge += Time.deltaTime;
        float squash = Active && allowed && landingAge < .32f
            ? -Mathf.Sin(landingAge / .32f * Mathf.PI) * landingSquash * impactScale : 0f;
        float stretch = Active && allowed ? Mathf.Clamp01(verticalSpeed / 10f) * .025f : 0f;
        float amount = squash + stretch;
        visual.localScale = Vector3.Scale(restScale, new Vector3(1f - amount * .5f, 1f + amount, 1f - amount * .5f));
        float footLocal = footWorldOffset / Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.y));
        var position = visual.localPosition;
        position.y -= (footLocal - position.y) * amount;
        if (Active && allowed && experience && experience.isActiveAndEnabled)
            position.y -= experience.VisualSink / Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.y));
        visual.localPosition = position;
    }

    void LateUpdate()
    {
        FootPenetrationBefore=FootPenetrationAfter=0;CorrectedFeet=0;
        if (!Active || Time.deltaTime<=0 || PauseSettingsMenu.IsOpen) { soleLift=0; return; }
        if (!motor) motor=GetComponent<ThirdPersonPlayerController>();
        if (!body) body=GetComponent<CharacterController>();
        if (!combat) combat=GetComponent<Mavis.SahurAttack>();
        if (!experience) experience=GetComponent<MarshmallowIslandExperience>();
        if (!motor || !motor.enabled || !body || !body.enabled || !experience || !experience.Ready ||
            motor.Swimming || motor.IsDodging || motor.ExternalControlLock || motor.ExternalMovementLock ||
            motor.Motion.Scripted || combat && combat.IsCombatMotionActive) { soleLift=0; return; }
        var animator=motor.CharacterAnimator;
        if (!animator || !animator.isHuman) return;
        if (soleAnimator!=animator) { soleAnimator=animator; SahurSupportPose.Prepare(animator); }
        var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (!left || !right) return;
        SahurSupportPose.MeasureSolesFast(animator,ref leftSole,ref rightSole);
        float correction=Mathf.Max(FootCorrection(left.TransformPoint(leftSole)),FootCorrection(right.TransformPoint(rightSole)));
        // Preserve the authored leg and ankle rotations. Lift immediately when
        // needed, then ease down as the feet leave the surface or terrain falls.
        soleLift=Mathf.Max(correction,Mathf.MoveTowards(soleLift,0,5f*Time.deltaTime));
        if (soleLift>0 && visual) visual.position+=Vector3.up*soleLift;
        FootPenetrationAfter=Mathf.Max(0,FootPenetrationBefore-soleLift);
    }

    float FootCorrection(Vector3 point)
    {
        if (!experience.TryVisualSurface(point,out float ground)) return 0;
        // Leave room for the sparse sole estimate and shader interpolation.
        float penetration=ground+.06f-point.y;
        if (penetration<=0 || penetration>1.5f) return 0;
        FootPenetrationBefore=Mathf.Max(FootPenetrationBefore,penetration);
        CorrectedFeet++;
        return penetration;
    }

    void OnDisable()
    {
        contactTime = 0f;
        if (visual != null) visual.localScale = restScale;
        if (!experience) experience = GetComponent<MarshmallowIslandExperience>();
        if (experience) experience.ResetFeedback();
        CancelSpacewalk();
    }
}
