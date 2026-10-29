using UnityEngine;

/// <summary>Alternating, long airborne strides for the Second Island's automatic bounds.</summary>
[DefaultExecutionOrder(1400), DisallowMultipleComponent, RequireComponent(typeof(MarshmallowTraversal))]
public sealed class MarshmallowSpacewalk : MonoBehaviour
{
    public AnimationClip leftStride;
    public AnimationClip rightStride;
    [Range(.05f,.3f)] public float blendTime = .14f;
    public bool LeftLeading { get; private set; }
    public bool Striding { get; private set; }
    public float Phase { get; private set; }
    public float Weight { get; private set; }
    public int Strides { get; private set; }

    public static readonly HumanBodyBones[] PoseBones = {
        HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,
        HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,
        HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,
        HumanBodyBones.LeftToes,HumanBodyBones.RightToes,HumanBodyBones.UpperChest,
        HumanBodyBones.LeftShoulder,HumanBodyBones.RightShoulder,HumanBodyBones.Neck,HumanBodyBones.Head
    };
    ThirdPersonPlayerController motor;
    MarshmallowTraversal traversal;
    CharacterController body;
    Mavis.SahurAttack combat;
    Mavis.SahurWeaponLoadout weapons;
    Animator animator;
    Transform[] bones;
    Quaternion[] sourceRotations;
    Vector3 sourceHips;
    float launchSpeed, airPhase, landedAt, landedPhase;
    bool landed, applied;
    int appliedFrame;

    void Awake()
    {
        motor=GetComponent<ThirdPersonPlayerController>();traversal=GetComponent<MarshmallowTraversal>();
        body=GetComponent<CharacterController>();combat=GetComponent<Mavis.SahurAttack>();
        weapons=GetComponent<Mavis.SahurWeaponLoadout>();
    }
    public void BeginBound(float upwardSpeed)
    {
        if(!isActiveAndEnabled || !traversal.Active)return;
        LeftLeading=!LeftLeading;Striding=true;Strides++;
        launchSpeed=Mathf.Max(1,upwardSpeed);Phase=airPhase=0;landed=false;
    }
    public void Land()
    {
        if(!Striding)return;
        // Uneven terrain can end a bound before its expected ballistic phase.
        // Continue from the pose that was actually visible rather than snapping
        // the knee forward to an assumed late-flight position.
        landed=true;landedAt=Time.time;landedPhase=Mathf.Clamp01(airPhase);
    }
    public void Cancel() { Striding=false;Weight=0; }
    bool Bind()
    {
        var next=motor.CharacterAnimator;
        if(!next || !next.isHuman)return false;
        if(animator==next && bones!=null)return true;
        animator=next;bones=new Transform[PoseBones.Length];sourceRotations=new Quaternion[PoseBones.Length];
        for(int i=0;i<bones.Length;i++)bones[i]=animator.GetBoneTransform(PoseBones[i]);
        return true;
    }
    void LateUpdate()
    {
        if(Time.deltaTime<=0 || PauseSettingsMenu.IsOpen)return;
        if(!motor || !motor.enabled || !traversal.Active || !body.enabled || !leftStride || !rightStride || !Bind())
        { Weight=0;Striding=false;return; }
        bool blocked=motor.ExternalControlLock || motor.ExternalMovementLock || motor.Swimming || motor.IsDodging ||
            motor.Motion.Scripted || combat && combat.IsCombatMotionActive;
        // Attacks, manual jumps and flips immediately retain their authored pose.
        if(blocked){Striding=false;Weight=0;return;}
        if(Striding)
        {
            if(landed)
            {
                Phase=Mathf.Lerp(landedPhase,1,Mathf.SmoothStep(0,1,(Time.time-landedAt)/Mathf.Max(.08f,traversal.groundCompressionTime)));
                if(Time.time-landedAt>traversal.groundCompressionTime+.06f)Striding=false;
            }
            else
            {
                float ballistic=Mathf.Clamp01((launchSpeed-motor.VerticalSpeed)/(2*launchSpeed));
                // One extended stride per physical bound, including a long downhill descent.
                airPhase=Mathf.Max(airPhase,Mathf.Lerp(0,.86f,ballistic));Phase=airPhase;
            }
        }
        Weight=Mathf.MoveTowards(Weight,Striding?1:0,Time.deltaTime/Mathf.Max(.05f,blendTime));
        if(Weight<=0)return;
        for(int i=0;i<bones.Length;i++)if(bones[i])sourceRotations[i]=bones[i].localRotation;
        sourceHips=bones[0].localPosition;
        (LeftLeading?leftStride:rightStride).SampleAnimation(animator.gameObject,Phase);
        bool preserveArms=weapons && weapons.UsesRifle;
        for(int i=0;i<bones.Length;i++)if(bones[i])
            bones[i].localRotation=Quaternion.Slerp(sourceRotations[i],bones[i].localRotation,
                preserveArms && (i>=9 && i<=12 || i>=15 || i==1 || i==2) ? 0:Weight);
        bones[0].localPosition=Vector3.Lerp(sourceHips,bones[0].localPosition,Weight);
        applied=true;appliedFrame=Time.frameCount;
    }
    void OnDisable()
    {
        // Avoid leaving a held generic bone pose when disabled between Animator ticks.
        if(applied && appliedFrame==Time.frameCount && bones!=null)
        {
            for(int i=0;i<bones.Length;i++)if(bones[i])bones[i].localRotation=sourceRotations[i];
            if(bones[0])bones[0].localPosition=sourceHips;
        }
        applied=false;Weight=0;Striding=false;
    }
}
