using UnityEngine;

/// <summary>Ground clearance for the short perfect step; never moves the collision root.</summary>
[DisallowMultipleComponent,DefaultExecutionOrder(1250)]
public sealed class SahurPerfectStepSupport:MonoBehaviour
{
    ThirdPersonPlayerController motor;
    CharacterController capsule;
    Animator animator;
    Transform[] legs;
    readonly Quaternion[] corrected=new Quaternion[6];
    readonly RaycastHit[] hits=new RaycastHit[16];
    bool hasCorrection;
    static readonly int Step=Animator.StringToHash("Base Layer.Perfect Step");
    void Awake(){motor=GetComponent<ThirdPersonPlayerController>();capsule=GetComponent<CharacterController>();}
    void LateUpdate()
    {
        if(!motor||!motor.enabled||motor.ExternalControlLock||motor.ExternalMovementLock){hasCorrection=false;return;}
        animator=motor.CharacterAnimator;if(!animator||!animator.isHuman)return;
        bool active=motor.IsPerfectStepping || animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).fullPathHash==Step;
        if(!active){hasCorrection=false;return;}
        if(legs==null)legs=new[]{animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
        if(Time.deltaTime<=0||PauseSettingsMenu.IsOpen)
        {
            if(hasCorrection)for(int i=0;i<legs.Length;i++)if(legs[i])legs[i].localRotation=corrected[i];
            return;
        }
        if(!capsule||!capsule.enabled)return;
        Vector3 left=Vector3.zero,right=Vector3.zero;SahurSupportPose.MeasureSolesFast(animator,ref left,ref right);
        Contact(true,left);Contact(false,right);
        for(int i=0;i<legs.Length;i++)if(legs[i])corrected[i]=legs[i].localRotation;hasCorrection=true;
    }
    void Contact(bool left,Vector3 localSole)
    {
        var foot=legs[left?2:5];if(!foot)return;Vector3 sole=foot.TransformPoint(localSole);
        float limit=Mathf.Clamp(capsule.height*Mathf.Abs(transform.lossyScale.y)*.22f,.25f,1.25f);
        int count=Physics.RaycastNonAlloc(sole+Vector3.up*(limit+.25f),Vector3.down,hits,limit+.55f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        float best=float.PositiveInfinity;Vector3 target=default;bool found=false;
        for(int i=0;i<count;i++)
        {
            var hit=hits[i];if(!hit.collider||hit.transform.IsChildOf(transform)||Mavis.CombatBodyCollision.IsActor(hit.collider)||hit.normal.y<Mathf.Cos(capsule.slopeLimit*Mathf.Deg2Rad))continue;
            if(Mathf.Abs(hit.point.y-capsule.bounds.min.y)>Mathf.Max(.6f,capsule.stepOffset*Mathf.Abs(transform.lossyScale.y)+.1f)||hit.distance>=best)continue;
            target=hit.point+Vector3.up*.025f;best=hit.distance;found=true;
        }
        if(!found||sole.y>=target.y)return;
        SahurSupportPose.Anchor(animator,left,localSole,target,limit);
    }
    void OnDisable(){hasCorrection=false;}
}

