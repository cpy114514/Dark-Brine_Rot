using System.Collections.Generic;
using UnityEngine;

/// <summary>Poses the imported weighted rig with two-joint limbs and a hand-mounted staff.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(HuangMeiBoss))]
public sealed class HuangMeiRig : MonoBehaviour
{
    public SkinnedMeshRenderer skin;
    public Transform staffTip;
    public Transform LeftPalm => leftArm?.end.bone;

    sealed class Joint
    {
        public readonly Transform bone;
        readonly Quaternion rest, parentBasis;
        public Joint(Transform bone, Transform actor)
        {
            this.bone=bone; rest=bone.localRotation;
            parentBasis=Quaternion.Inverse(actor.rotation)*bone.parent.rotation;
        }
        public void Reset()=>bone.localRotation=rest;
        public void Bend(Vector3 euler)=>bone.localRotation=Quaternion.Inverse(parentBasis)*Quaternion.Euler(euler)*parentBasis*rest;
    }

    sealed class Limb
    {
        public readonly Joint upper, lower, end;
        public readonly Vector3 restEnd;
        readonly Quaternion restFacing;
        public Limb(Transform actor, Transform upper, Transform lower, Transform end)
        {
            this.upper=new Joint(upper,actor); this.lower=new Joint(lower,actor); this.end=new Joint(end,actor);
            restEnd=actor.InverseTransformPoint(end.position);
            restFacing=Quaternion.Inverse(actor.rotation)*end.rotation;
        }
        public void Reset(){upper.Reset();lower.Reset();end.Reset();}
        public void Solve(Vector3 target, Vector3 pole)
        {
            Vector3 start=upper.bone.position;
            Vector3 a=lower.bone.position-start, b=end.bone.position-lower.bone.position;
            float lengthA=a.magnitude, lengthB=b.magnitude;
            Vector3 delta=target-start;
            if(delta.sqrMagnitude<.00001f || lengthA<.001f || lengthB<.001f)return;
            Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole-start,direction).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(a,direction).normalized;
            float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(lengthA-lengthB)+.005f,lengthA+lengthB-.005f);
            float along=(lengthA*lengthA-lengthB*lengthB+reach*reach)/(2*reach);
            Vector3 knee=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,lengthA*lengthA-along*along));
            upper.bone.rotation=Quaternion.FromToRotation(a,knee-start)*upper.bone.rotation;
            lower.bone.rotation=Quaternion.FromToRotation(end.bone.position-lower.bone.position,start+direction*reach-lower.bone.position)*lower.bone.rotation;
        }
        public void LevelFoot(Transform actor)=>end.bone.rotation=actor.rotation*restFacing;
    }

    struct Pose
    {
        public Vector3 right,left,shaft,chest;
        public float crouch;
        public Pose(Vector3 right,Vector3 left,Vector3 shaft,Vector3 chest,float crouch=0)
        {this.right=right;this.left=left;this.shaft=shaft;this.chest=chest;this.crouch=crouch;}
        public static Pose Blend(Pose a,Pose b,float t)=>new Pose(Vector3.Lerp(a.right,b.right,t),Vector3.Lerp(a.left,b.left,t),
            Vector3.Slerp(a.shaft,b.shaft,t),Vector3.Lerp(a.chest,b.chest,t),Mathf.Lerp(a.crouch,b.crouch,t));
    }

    static readonly Pose Rest=new Pose(new Vector3(1.86f,4.48f,.79f),new Vector3(-1.55f,4.58f,.55f),Vector3.up,Vector3.zero,.12f);
    static readonly Pose[] Windups={
        new Pose(new Vector3(2.68f,5.30f,-.46f),new Vector3(-1.42f,5.42f,1.25f),new Vector3(.93f,.17f,-.32f),new Vector3(-5,39,-7),.26f),
        new Pose(new Vector3(1.54f,7.54f,.40f),new Vector3(-1.31f,6.42f,1.10f),new Vector3(.02f,.72f,-.70f),new Vector3(-16,-12,4),.08f),
        new Pose(new Vector3(1.93f,4.7f,.38f),new Vector3(-1.44f,5.98f,.63f),Vector3.up,new Vector3(-8,19,-5),.17f)
    };
    static readonly Pose[] Impacts={
        new Pose(new Vector3(.53f,4.83f,2.23f),new Vector3(-1.58f,4.68f,-.04f),new Vector3(-.84f,.15f,.51f),new Vector3(12,-40,9),.44f),
        new Pose(new Vector3(1.22f,3.72f,1.93f),new Vector3(-1.44f,4.3f,1.22f),new Vector3(.01f,-.76f,.65f),new Vector3(22,-4,3),.63f),
        new Pose(new Vector3(1.93f,4.40f,-.10f),new Vector3(-.83f,5.50f,2.60f),new Vector3(.16f,.96f,.12f),new Vector3(11,-18,5),.28f)
    };
    static readonly Pose Invocation=new Pose(new Vector3(2.17f,6.24f,.5f),new Vector3(-2.11f,6.44f,.73f),new Vector3(.25f,.96f,-.06f),new Vector3(-10,0,0));
    static readonly Pose SweepRecoil=new Pose(new Vector3(.68f,5.46f,2.04f),new Vector3(-1.56f,5.34f,.72f),new Vector3(-.93f,.30f,.05f),new Vector3(-6,-47,4),.22f);
    static readonly Pose SweepFollowThrough=new Pose(new Vector3(2.81f,4.73f,1.44f),new Vector3(-1.28f,4.79f,1.13f),new Vector3(.86f,.14f,.48f),new Vector3(14,44,-12),.47f);
    static readonly Pose MandalaRaised=new Pose(new Vector3(1.91f,7.51f,.67f),new Vector3(-1.46f,6.92f,1.81f),new Vector3(.14f,.96f,.25f),new Vector3(-13,-8,-6),.07f);
    static readonly Pose MandalaPlanted=new Pose(new Vector3(1.53f,4.14f,1.58f),new Vector3(-1.31f,5.38f,2.20f),new Vector3(.04f,.99f,.04f),new Vector3(21,-5,5),.62f);

    HuangMeiBoss boss;
    Joint spine, chest, head;
    Transform pelvis, art;
    Vector3 pelvisRest,artRest;
    Quaternion artFacing;
    Limb leftArm,rightArm,leftLeg,rightLeg;
    readonly List<Joint> fingers=new List<Joint>();
    readonly RaycastHit[] groundHits=new RaycastHit[12];
    float stride,death;

    void Awake()
    {
        boss=GetComponent<HuangMeiBoss>();
        if(!skin){enabled=false;return;}
        art=skin.transform;
        while(art.parent && art.parent!=transform)art=art.parent;
        artRest=art.localPosition; artFacing=art.localRotation;
        var bones=new Dictionary<string,Transform>();
        foreach(var bone in skin.bones)if(bone && !bones.ContainsKey(bone.name))bones.Add(bone.name,bone);
        pelvis=bones["pelvis"];pelvisRest=pelvis.localPosition;
        spine=new Joint(bones["spine_02"],transform);chest=new Joint(bones["spine_03"],transform);head=new Joint(bones["head"],transform);
        leftArm=new Limb(transform,bones["upperarm_l"],bones["lowerarm_l"],bones["hand_l"]);
        rightArm=new Limb(transform,bones["upperarm_r"],bones["lowerarm_r"],bones["hand_r"]);
        leftLeg=new Limb(transform,bones["thigh_l"],bones["calf_l"],bones["foot_l"]);
        rightLeg=new Limb(transform,bones["thigh_r"],bones["calf_r"],bones["foot_r"]);
        foreach(string finger in new[]{"index","middle","ring","pinky"})
            for(int part=1;part<=3;part++)fingers.Add(new Joint(bones[$"{finger}_0{part}_r"],transform));
    }

    void LateUpdate()
    {
        art.SetLocalPositionAndRotation(artRest,artFacing);
        pelvis.localPosition=pelvisRest;
        leftArm.Reset();rightArm.Reset();leftLeg.Reset();rightLeg.Reset();
        int attack=(int)boss.CurrentAttack;
        float progress=boss.Progress;
        float walk=boss.Moving;
        stride+=Time.deltaTime*walk*7.8f;
        float step=Mathf.Sin(stride)*walk;
        var idle=Rest;
        idle.left.z+=step*.54f;
        idle.right.z-=step*.18f;
        idle.chest.y+=step*6;
        idle.chest.x+=Mathf.Sin(Time.time*1.9f)*1.5f+walk*4;
        bool mandala=boss.CurrentAttack==HuangMeiBoss.Attack.GoldenMandala;
        var wind=mandala?MandalaRaised:Windups[attack];
        var finish=mandala?MandalaPlanted:boss.CurrentAttack==HuangMeiBoss.Attack.StaffSweep?SweepFollowThrough:Impacts[attack];
        Pose pose=boss.CurrentState switch {
            HuangMeiBoss.State.Windup => Pose.Blend(idle,wind,Ease(progress/.86f)),
            HuangMeiBoss.State.Strike when mandala => Pose.Blend(MandalaRaised,MandalaPlanted,HuangMeiBoss.StaffPlant(boss.ActionElapsed)),
            HuangMeiBoss.State.Strike when boss.CurrentAttack==HuangMeiBoss.Attack.StaffSweep => SweepPose(progress),
            HuangMeiBoss.State.Strike => Pose.Blend(wind,finish,Ease(progress/HuangMeiBoss.ImpactPhase(boss.CurrentAttack))),
            HuangMeiBoss.State.Recover => Pose.Blend(finish,idle,Ease(progress)),
            HuangMeiBoss.State.Invocation => Pose.Blend(idle,Invocation,Mathf.Sin(progress*Mathf.PI)),
            _ => idle
        };
        pelvis.position-=transform.up*((pose.crouch + walk*(.10f+.07f*Mathf.Cos(stride*2)))*transform.lossyScale.y);
        spine.Bend(pose.chest*.40f);chest.Bend(pose.chest*.60f);
        head.Bend(new Vector3(-pose.chest.x*.40f,-pose.chest.y*.35f,0));
        float stance=Mathf.Clamp01((pose.crouch-.13f)*2.45f);
        SetFoot(leftLeg,-step,walk,stance);SetFoot(rightLeg,step,walk,-stance);
        leftArm.Solve(transform.TransformPoint(pose.left),transform.TransformPoint(new Vector3(-3.7f,4.7f,.25f)));
        rightArm.Solve(transform.TransformPoint(pose.right),transform.TransformPoint(new Vector3(3.8f,4.8f,.05f)));
        Grip(pose.shaft);
        if(boss.CurrentAttack==HuangMeiBoss.Attack.PalmSeal || mandala)
        {
            float palmWeight=boss.CurrentState==HuangMeiBoss.State.Windup?Ease(progress):boss.CurrentState==HuangMeiBoss.State.Strike?1:
                boss.CurrentState==HuangMeiBoss.State.Recover?1-Ease(progress):0;
            PresentPalm(palmWeight);
        }
        if(boss.CurrentState==HuangMeiBoss.State.Dead)
        {
            death=Mathf.MoveTowards(death,1,Time.deltaTime*1.65f);
            float t=Ease(death);
            art.localRotation=Quaternion.Euler(-86*t,0,-8*t)*artFacing;
            art.localPosition=artRest+new Vector3(0,1.45f*t,-.5f*t);
        }
    }

    static Pose SweepPose(float t)
    {
        if(t<=HuangMeiBoss.FirstSweep)return Pose.Blend(Windups[0],Impacts[0],Ease(t/HuangMeiBoss.FirstSweep));
        if(t<.63f)return Pose.Blend(Impacts[0],SweepRecoil,Ease(Mathf.InverseLerp(HuangMeiBoss.FirstSweep,.63f,t)));
        return Pose.Blend(SweepRecoil,SweepFollowThrough,Ease(Mathf.InverseLerp(.63f,HuangMeiBoss.SecondSweep,t)));
    }

    static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
    void SetFoot(Limb leg,float swing,float movement,float stance)
    {
        var point=leg.restEnd;
        point.z+=swing*1.15f+stance*.54f;
        point.x*=1+Mathf.Abs(stance)*.21f;
        Vector3 target=transform.TransformPoint(point);
        int count=Physics.RaycastNonAlloc(target+Vector3.up*1.55f,Vector3.down,groundHits,3.1f,~0,QueryTriggerInteraction.Ignore);
        float nearest=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var hit=groundHits[i];
            if(!hit.collider || hit.collider.transform.IsChildOf(transform) || Mavis.CombatBodyCollision.IsActor(hit.collider) || hit.normal.y<.65f || hit.distance>=nearest)continue;
            nearest=hit.distance;
            target.y=hit.point.y+point.y*transform.lossyScale.y;
        }
        target+=transform.up*(Mathf.Max(0,swing)*.46f+movement*.035f)*transform.lossyScale.y;
        leg.Solve(target,transform.TransformPoint(new Vector3(point.x,2.5f,3.1f)));
        leg.LevelFoot(transform);
    }

    void PresentPalm(float weight)
    {
        if(weight<=0)return;
        var hand=leftArm.end.bone;
        var back=leftArm.lower.bone.position-hand.position;
        var axis=Vector3.ProjectOnPlane(back,transform.forward).normalized;
        if(axis.sqrMagnitude<.01f)return;
        var facing=Quaternion.LookRotation(Vector3.Cross(axis,transform.forward),transform.forward);
        hand.rotation=Quaternion.Slerp(hand.rotation,facing,weight*.85f);
    }

    void Grip(Vector3 direction)
    {
        var hand=rightArm.end.bone;
        Vector3 shaft=transform.TransformDirection(direction).normalized;
        Vector3 wrist=rightArm.lower.bone.position-hand.position;
        Vector3 axis=Vector3.ProjectOnPlane(wrist,shaft).normalized;
        if(axis.sqrMagnitude<.01f)axis=Vector3.ProjectOnPlane(transform.right,shaft).normalized;
        hand.rotation=Quaternion.LookRotation(shaft,Vector3.Cross(shaft,axis));
        for(int i=0;i<fingers.Count;i++)
        {
            var joint=fingers[i];joint.Reset();
            joint.bone.localRotation=Quaternion.AngleAxis(i%3==0?-30:-42,joint.bone.parent.InverseTransformDirection(shaft))*joint.bone.localRotation;
        }
    }
}
