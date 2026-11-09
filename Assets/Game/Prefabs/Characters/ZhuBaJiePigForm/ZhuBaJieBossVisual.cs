using System.Linq;
using UnityEngine;

/// <summary>Plays supplied boar clips and poses the unanimated human skeleton and held rake.</summary>
[DisallowMultipleComponent]
public sealed class ZhuBaJieBossVisual : MonoBehaviour
{
    ZhuBaJieBoss boss; Animator boarAnimator; Transform human,upper,lower,hand,spine,leftThigh,rightThigh,leftToe,rightToe;
    Quaternion upperRest,lowerRest,handRest,spineRest,leftRest,rightRest;
    readonly Transform[] tail=new Transform[6];
    readonly Quaternion[] tailRest=new Quaternion[6];
    Vector3 lastPosition; float movement;
    readonly RaycastHit[] groundHits=new RaycastHit[24];
    void Awake()
    {
        boss=GetComponent<ZhuBaJieBoss>();
        if(boss.boarForm)boarAnimator=boss.boarForm.GetComponentInChildren<Animator>(true);
        human=boss.humanForm?boss.humanForm.transform:null;
        if(human)
        {
            var bones=human.GetComponentsInChildren<Transform>(true);
            upper=Find(bones,"upperarm_r");lower=Find(bones,"lowerarm_r");hand=Find(bones,"hand_r");spine=Find(bones,"spine_03");
            leftThigh=Find(bones,"thigh_l");rightThigh=Find(bones,"thigh_r");
            leftToe=Find(bones,"ball_l");rightToe=Find(bones,"ball_r");
            if(upper)upperRest=upper.localRotation;if(lower)lowerRest=lower.localRotation;if(hand)handRest=hand.localRotation;
            if(spine)spineRest=spine.localRotation;if(leftThigh)leftRest=leftThigh.localRotation;if(rightThigh)rightRest=rightThigh.localRotation;
        }
        if(boss.boarForm)
        {
            var bones=boss.boarForm.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<tail.Length;i++)
            {
                tail[i]=Find(bones,"tail_0"+(i+1));
                if(tail[i])tailRest[i]=tail[i].localRotation;
            }
        }
        lastPosition=transform.position;
    }
    static Transform Find(Transform[] bones,string name)=>bones.FirstOrDefault(t=>t.name==name);
    public void BeginAttack(ZhuBaJieBoss.MoveKind move)
    {
        if(boarAnimator && !boss.HumanPhase)
        {
            string clip=move==ZhuBaJieBoss.MoveKind.TailSweep?"atk_02":"atk_01";
            // The source clips contain long sequences with large root-bone
            // turns. Keep their early anticipation aligned with our shorter,
            // collision-driven charge and tail-sweep windows.
            boarAnimator.speed=.55f;boarAnimator.Play(clip,0,0);
        }
    }
    public void BeginTransform(){if(boarAnimator)boarAnimator.Play("Idle",0,0);}
    public void SetHuman(){if(boarAnimator)boarAnimator.enabled=false;}
    public void Die(){if(boarAnimator && !boss.HumanPhase)boarAnimator.Play("dead",0,0);}
    void LateUpdate()
    {
        if(!boss || boss.CurrentState==ZhuBaJieBoss.State.Dead)return;
        float dt=Mathf.Max(.001f,Time.deltaTime);float speed=Vector3.ProjectOnPlane(transform.position-lastPosition,Vector3.up).magnitude/dt;
        lastPosition=transform.position;movement=Mathf.MoveTowards(movement,Mathf.Clamp01(speed/5),dt*5);
        if(!boss.HumanPhase)
        {
            if(boarAnimator && boss.CurrentState!=ZhuBaJieBoss.State.Windup && boss.CurrentState!=ZhuBaJieBoss.State.Attack && boss.CurrentState!=ZhuBaJieBoss.State.Recover)
            {
                var state=boarAnimator.GetCurrentAnimatorStateInfo(0);if(!state.IsName("Idle")){boarAnimator.speed=1;boarAnimator.Play("Idle",0,0);}
            }
            if(tail[0] && boss.CurrentMove==ZhuBaJieBoss.MoveKind.TailSweep)PoseTailSweep();
            return;
        }
        if(upper)upper.localRotation=upperRest;if(lower)lower.localRotation=lowerRest;if(hand)hand.localRotation=handRest;
        if(spine)spine.localRotation=spineRest;if(leftThigh)leftThigh.localRotation=leftRest;if(rightThigh)rightThigh.localRotation=rightRest;
        float step=Mathf.Sin(Time.time*6f)*20f*movement;
        Rotate(leftThigh,transform.right,step);Rotate(rightThigh,transform.right,-step);
        float yaw=0,raise=0,twist=0;
        if(boss.CurrentState==ZhuBaJieBoss.State.Windup){float t=boss.ActionProgress;yaw=-55*t;raise=35*t;twist=-18*t;}
        else if(boss.CurrentState==ZhuBaJieBoss.State.Attack)
        {
            float t=boss.ActionProgress;
            yaw=t<.48f?Mathf.Lerp(-55,65,t/.48f):Mathf.Lerp(65,-70,(t-.48f)/.52f);
            raise=35+20*Mathf.Sin(t*Mathf.PI*2);twist=yaw*.3f;
        }
        else if(boss.CurrentState==ZhuBaJieBoss.State.Recover){yaw=-55*(1-boss.ActionProgress);raise=25*(1-boss.ActionProgress);}
        else if(boss.CurrentState==ZhuBaJieBoss.State.Dodge){twist=20*Mathf.Sin(boss.ActionProgress*Mathf.PI);}
        Rotate(spine,transform.up,twist);Rotate(upper,transform.up,yaw);Rotate(upper,transform.right,-raise);
        Rotate(lower,transform.right,-raise*.3f);Rotate(hand,transform.right,35+raise*.5f);
        AdjustFooting(dt);
    }
    void PoseTailSweep()
    {
        float aim=boss.TailAimYaw;
        float side=Mathf.Sign(aim);
        float preparation=-48f*side;
        float followThrough=Mathf.Clamp(aim+40f*side,-125f,125f);
        // A continuous timeline lets the tip lag through the state boundaries.
        float progress=boss.ActionProgress;
        switch(boss.CurrentState)
        {
            case ZhuBaJieBoss.State.Windup:
                progress-=1f;
                break;
            case ZhuBaJieBoss.State.Attack:
                break;
            case ZhuBaJieBoss.State.Recover:
                progress+=1f;
                break;
            default:
                return;
        }
        float yaw=SweepYaw(progress,aim,preparation,followThrough);
        Vector3 up=transform.up;
        tail[0].rotation=Quaternion.AngleAxis(yaw,up)*(tail[0].parent.rotation*tailRest[0]);
        float settle=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(1.65f,2f,progress));
        float previousYaw=yaw;
        for(int i=1;i<tail.Length && tail[i];i++)
        {
            float delayed=progress-i*.12f;
            float delayedYaw=SweepYaw(delayed,aim,preparation,followThrough);
            float bend=Mathf.Clamp(delayedYaw-previousYaw,-24f,24f)*settle;
            float lift=7f*Mathf.SmoothStep(0f,1f,delayed+1f)*(1f-Mathf.SmoothStep(0f,1f,delayed))*settle;
            var joint=tail[i];
            Vector3 pitch=Vector3.Cross(joint.position-joint.parent.position,up).normalized;
            joint.rotation=Quaternion.AngleAxis(bend,up)*Quaternion.AngleAxis(lift,pitch)*(joint.parent.rotation*tailRest[i]);
            previousYaw=delayedYaw;
        }
    }
    static float SweepYaw(float progress,float aim,float preparation,float followThrough)
    {
        if(progress<0f)return Mathf.SmoothStep(0f,preparation,progress+1f);
        if(progress<ZhuBaJieBoss.TailHitPhase)return Mathf.Lerp(preparation,aim,progress/ZhuBaJieBoss.TailHitPhase);
        if(progress<1f)return Mathf.Lerp(aim,followThrough,Mathf.InverseLerp(ZhuBaJieBoss.TailHitPhase,1f,progress));
        return Mathf.SmoothStep(followThrough,0f,progress-1f);
    }
    void AdjustFooting(float dt)
    {
        float gap=Mathf.Min(ToeGap(leftToe),ToeGap(rightToe));
        if(float.IsPositiveInfinity(gap))return;
        var at=human.localPosition;float goal=Mathf.Clamp(at.y+.08f-gap,-.65f,.45f);
        at.y=Mathf.MoveTowards(at.y,goal,2f*dt);human.localPosition=at;
    }
    float ToeGap(Transform toe)
    {
        if(!toe)return float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(toe.position+Vector3.up*5,Vector3.down,groundHits,12,~0,QueryTriggerInteraction.Ignore);
        float nearest=float.PositiveInfinity,ground=0;
        for(int i=0;i<count;i++)
        {
            var hit=groundHits[i];if(!hit.collider || hit.transform.IsChildOf(transform) || Mavis.CombatBodyCollision.IsActor(hit.collider) || hit.normal.y<.6f || hit.distance>=nearest)continue;
            nearest=hit.distance;ground=hit.point.y;
        }
        return nearest<float.PositiveInfinity?toe.position.y-ground:float.PositiveInfinity;
    }
    static void Rotate(Transform bone,Vector3 axis,float degrees)
    {if(bone && Mathf.Abs(degrees)>.001f)bone.rotation=Quaternion.AngleAxis(degrees,axis)*bone.rotation;}
}
