using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public sealed partial class Story1WreckBattle
{
    struct RedWaterCapsule
    {
        public Vector3 a,b;
        public float radius;
        public RedWaterCapsule(Vector3 a,Vector3 b,float radius){this.a=a;this.b=b;this.radius=radius;}
    }
    sealed class RedWaterShape
    {
        public Story1WreckMeshCollision mesh;
        public Rigidbody body;
        public Bounds bounds;
        public Vector3 velocity,spin,centre;
    }
    readonly List<RedWaterShape> redWaterShapes=new List<RedWaterShape>();
    readonly List<RedWaterCapsule> redWaterCapsules=new List<RedWaterCapsule>();
    readonly List<RedWaterCapsule> redWaterPreview=new List<RedWaterCapsule>();
    CapsuleCollider redWaterQuery;
    Vector3 redWaterExitVelocity,redWaterDriftOffset,redWaterDriftVelocity,redWaterDriftTarget;
    float redWaterNextSearch,redWaterPreviewHeadY;
    float[] redWaterBodyRadii;
    bool redWaterExitPlanned,redLostStickStarted,redLostStickFloating;
    float redLostStickStartTime,redLostStickPreviousAge,redLostStickVerticalVelocity;
    Vector3 redLostStickFloatPosition,redLostStickFloatVelocity;
    Quaternion redLostStickFloatRotation;
    public float RedesignWaterPredictionCost { get; private set; }
    public float RedesignWaterBodyPenetration { get; private set; }
    public Vector3 RedesignWaterExitVelocity=>redWaterExitVelocity;
    public bool RedesignLostStickFloating=>redLostStickFloating;

    void RedWaterCollectShapes()
    {
        redWaterShapes.Clear();
        foreach(var board in planks)if(board!=null&&!board.IsBroken)RedWaterAddShape(board.Collision);
        foreach(var piece in detailPieces)if(piece!=null&&!piece.IsBroken)RedWaterAddShape(piece.Collision);
        if(redWaterQuery!=null)return;
        var go=new GameObject("Opening water clearance query"){hideFlags=HideFlags.HideInHierarchy};
        go.transform.position=new Vector3(0,-100000,0);
        redWaterQuery=go.AddComponent<CapsuleCollider>();redWaterQuery.enabled=false;redWaterQuery.isTrigger=true;redWaterQuery.direction=1;
    }
    void RedWaterAddShape(Story1WreckMeshCollision mesh)
    {
        if(mesh==null||!mesh.Active||mesh.Parts==null)return;
        var body=mesh.Primary.attachedRigidbody;
        redWaterShapes.Add(new RedWaterShape{mesh=mesh,body=body,bounds=mesh.WorldBounds,
            velocity=body!=null?body.linearVelocity:Vector3.zero,spin=body!=null?body.angularVelocity:Vector3.zero,
            centre=body!=null?body.worldCenterOfMass:mesh.transform.position});
    }
    void RedWaterAddBoneCapsule(HumanBodyBones from,HumanBodyBones to,float radius)
    {
        var a=redAnimator.GetBoneTransform(from);var b=redAnimator.GetBoneTransform(to);
        if(a!=null&&b!=null)redWaterCapsules.Add(new RedWaterCapsule(a.position,b.position,radius*redHeroHeight/5));
    }
    void RedWaterReadBody()
    {
        redWaterCapsules.Clear();
        // The whole posed body is represented, including hands, feet and skull.
        // Native compound mesh parts remain the other side of every query.
        RedWaterAddBoneCapsule(HumanBodyBones.Hips,HumanBodyBones.Chest,.68f);
        RedWaterAddBoneCapsule(HumanBodyBones.Chest,HumanBodyBones.Head,.52f);
        RedWaterAddBoneCapsule(HumanBodyBones.Head,HumanBodyBones.Head,.67f);
        RedWaterAddBoneCapsule(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,.32f);
        RedWaterAddBoneCapsule(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,.30f);
        RedWaterAddBoneCapsule(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,.32f);
        RedWaterAddBoneCapsule(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,.30f);
        RedWaterAddBoneCapsule(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,.40f);
        RedWaterAddBoneCapsule(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,.34f);
        RedWaterAddBoneCapsule(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,.40f);
        RedWaterAddBoneCapsule(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,.34f);
        RedWaterAddBoneCapsule(HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,.30f);
        RedWaterAddBoneCapsule(HumanBodyBones.RightFoot,HumanBodyBones.RightToes,.30f);
        if(redWaterBodyRadii!=null&&redWaterBodyRadii.Length==redWaterCapsules.Count)
            for(int i=0;i<redWaterCapsules.Count;i++)
            {var capsule=redWaterCapsules[i];capsule.radius=redWaterBodyRadii[i];redWaterCapsules[i]=capsule;}
    }
    void RedWaterMeasureNativeEnvelope()
    {
        redWaterBodyRadii=new float[redWaterCapsules.Count];
        for(int i=0;i<redWaterCapsules.Count;i++)redWaterBodyRadii[i]=redWaterCapsules[i].radius;
        var mesh=new Mesh();
        foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            skin.BakeMesh(mesh,true);
            foreach(var vertex in mesh.vertices)
            {
                Vector3 point=skin.transform.TransformPoint(vertex);int nearest=-1;float best=float.PositiveInfinity;
                for(int i=0;i<redWaterCapsules.Count;i++)
                {
                    var capsule=redWaterCapsules[i];Vector3 line=capsule.b-capsule.a;
                    Vector3 closest=capsule.a+line*Mathf.Clamp01(Vector3.Dot(point-capsule.a,line)/Mathf.Max(.000001f,line.sqrMagnitude));
                    float distance=(point-closest).sqrMagnitude;
                    if(distance<best){best=distance;nearest=i;}
                }
                if(nearest>=0)redWaterBodyRadii[nearest]=Mathf.Max(redWaterBodyRadii[nearest],Mathf.Sqrt(best)+.06f);
            }
            mesh.Clear();
        }
        Destroy(mesh);RedWaterReadBody();
    }
    static Vector3 RedWaterForecastTranslation(RedWaterShape shape,float seconds)
    {
        // Water damps vertical velocity quickly. Preserve measured horizontal
        // drift and retain the initial vertical momentum over its first .3 s.
        return Vector3.ProjectOnPlane(shape.velocity,Vector3.up)*seconds+
            Vector3.up*shape.velocity.y*(.3f*(1-Mathf.Exp(-seconds/.3f)));
    }
    float RedWaterCapsuleCost(RedWaterCapsule capsule,Vector3 offset,float seconds,float margin,out Vector3 avoidance)
    {
        avoidance=Vector3.zero;float cost=0;
        Vector3 a=capsule.a+offset,b=capsule.b+offset;float radius=capsule.radius+margin;
        var capsuleBounds=new Bounds((a+b)*.5f,Vector3.zero);capsuleBounds.Encapsulate(a);capsuleBounds.Encapsulate(b);capsuleBounds.Expand(radius*2);
        redWaterQuery.radius=radius;redWaterQuery.height=(b-a).magnitude+radius*2;
        Quaternion capsuleRotation=(b-a).sqrMagnitude>.00001f?Quaternion.FromToRotation(Vector3.up,b-a):Quaternion.identity;
        redWaterQuery.enabled=true;
        try
        {
        foreach(var shape in redWaterShapes)
        {
            Vector3 translation=RedWaterForecastTranslation(shape,seconds);
            Quaternion rotation=shape.spin.sqrMagnitude>.0001f?Quaternion.AngleAxis(shape.spin.magnitude*Mathf.Rad2Deg*Mathf.Min(seconds,.7f),shape.spin.normalized):Quaternion.identity;
            // Rotation-aware broad phase; exact checks below use real cut parts.
            float reach=shape.bounds.extents.magnitude+Vector3.Distance(shape.bounds.center,shape.centre);
            Bounds future=new Bounds(shape.centre+translation,Vector3.one*(reach*2));
            if(!future.Intersects(capsuleBounds))continue;
            for(int i=0;i<shape.mesh.Parts.Length;i++)
            {
                var part=shape.mesh.Parts[i];
                if(part==null||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                Vector3 point=shape.centre+translation+rotation*(shape.mesh.PartPosition(i)-shape.centre);
                Quaternion turn=rotation*shape.mesh.PartRotation(i);
                Bounds present=shape.mesh.PartBounds(i);Vector3 e=present.extents;
                Vector3 x=rotation*(Vector3.right*e.x),y=rotation*(Vector3.up*e.y),z=rotation*(Vector3.forward*e.z);
                Vector3 extents=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
                Bounds partBounds=new Bounds(shape.centre+translation+rotation*(present.center-shape.centre),extents*2);
                partBounds.Expand(radius*2);
                if(!partBounds.Intersects(capsuleBounds))continue;
                if(!Physics.ComputePenetration(redWaterQuery,(a+b)*.5f,capsuleRotation,part,point,turn,out var direction,out float depth))continue;
                cost+=depth*depth+.01f;
                Vector3 planar=Vector3.ProjectOnPlane(direction,Vector3.up);
                if(planar.sqrMagnitude<.04f)planar=Vector3.ProjectOnPlane((a+b)*.5f-(shape.centre+translation),Vector3.up);
                if(planar.sqrMagnitude>.0001f)avoidance+=planar.normalized*depth;
            }
        }
        return cost;
        }
        finally{redWaterQuery.enabled=false;}
    }
    float RedWaterBodyCost(List<RedWaterCapsule> body,Vector3 offset,float seconds,float margin,out Vector3 avoidance)
    {
        avoidance=Vector3.zero;float cost=0;
        foreach(var capsule in body){cost+=RedWaterCapsuleCost(capsule,offset,seconds,margin,out var push);avoidance+=push;}
        return cost;
    }

    /// <summary>Call once when the tail contact is confirmed, after redFloatStart is captured.</summary>
    void RedPlanWaterExit()
    {
        RedWaterCollectShapes();redWaterExitPlanned=true;redWaterExitVelocity=redRight*12;
        redWaterDriftOffset=redWaterDriftVelocity=redWaterDriftTarget=Vector3.zero;redWaterNextSearch=0;
        // Preview the actual unconscious pose on the same rig, then restore the
        // current sample. No proxy character, re-scaling or renderer changes.
        string previous=redClip.name;float sample=(float)redPose.GetTime();
        Vector3 root=player.transform.position;Quaternion rotation=player.transform.rotation;
        RedPose("PLAYER_Fall_v006",2.2f,false);player.transform.rotation=Quaternion.LookRotation(redForward);redGraph.Evaluate(0);
        redWaterPreviewHeadY=RedHead.y-player.transform.position.y;RedWaterReadBody();RedWaterMeasureNativeEnvelope();redWaterPreview.Clear();
        foreach(var capsule in redWaterCapsules)redWaterPreview.Add(new RedWaterCapsule(capsule.a-player.transform.position,capsule.b-player.transform.position,capsule.radius));
        player.transform.SetPositionAndRotation(root,rotation);RedPose(previous,sample,false);
        float best=float.PositiveInfinity;float bestContact=0;
        float[] bearings={0,15,-15,30,-30,45,-45,60,-60};float[] speeds={12,15,18,21};
        foreach(float speed in speeds)foreach(float bearing in bearings)
        {
            Vector3 velocity=Quaternion.AngleAxis(bearing,Vector3.up)*redRight*speed;
            float contact=0;
            // A range of possible splash times covers pose-dependent head and
            // chest height. Follow the landing with the entire drift interval.
            for(int sampleIndex=0;sampleIndex<7;sampleIndex++)
            {
                float flight=sampleIndex<4?1.3f+sampleIndex*.35f:1.65f;
                float drift=sampleIndex<4?0:(sampleIndex-3)*1.8f;
                Vector3 at=redFloatStart+velocity*flight+redForward*(drift*.35f);
                at.y=RedWater(at)+.32f-redWaterPreviewHeadY;
                contact+=RedWaterBodyCost(redWaterPreview,at,flight+drift,.65f+Mathf.Min(drift,.6f)*.3f,out _);
            }
            float preference=Mathf.Abs(bearing)*.0015f+(speed-12)*.015f;
            float score=contact*100+preference;
            if(score<best){best=score;bestContact=contact;redWaterExitVelocity=velocity;}
            if(contact==0&&preference==0)break;
        }
        RedesignWaterPredictionCost=bestContact;
        RedEvent("Water exit planned; velocity="+redWaterExitVelocity.ToString("F2")+"; predicted mesh cost="+bestContact.ToString("F3"));
    }
    Vector3 RedFlightPosition(float age)=>redFloatStart+(redWaterExitPlanned?redWaterExitVelocity:redRight*12)*age+Vector3.up*(9*age-4.9f*age*age);

    /// <summary>Call after placing and vertically floating the real unconscious pose, before its shot.</summary>
    void RedAvoidDrift(float dt)
    {
        RedWaterCollectShapes();player.transform.position+=redWaterDriftOffset;RedWaterReadBody();
        if(Time.time>=redWaterNextSearch)
        {
            redWaterNextSearch=Time.time+.15f;
            float cost=RedWaterDriftCost(redWaterDriftVelocity,out var push);
            redWaterDriftTarget=redWaterDriftVelocity;
            float stationary=RedWaterDriftCost(Vector3.zero,out var stationaryPush);
            if(stationary<=cost){cost=stationary;push=stationaryPush;redWaterDriftTarget=Vector3.zero;}
            if(cost>0)
            {
                Vector3 preferred=push.sqrMagnitude>.001f?push.normalized:redRight;
                float best=cost;
                for(int i=0;i<12;i++)
                {
                    Vector3 candidate=Quaternion.AngleAxis((i%2==0?1:-1)*((i+1)/2)*30,Vector3.up)*preferred*2.5f;
                    float candidateCost=RedWaterDriftCost(candidate,out _);
                    candidateCost+=Vector3.Distance(candidate,redWaterDriftVelocity)*.002f;
                    if(candidateCost<best){best=candidateCost;redWaterDriftTarget=candidate;}
                }
            }
        }
        // Bounded acceleration and displacement keep the entire body continuous.
        // The initial landing search does the large relocation while airborne.
        redWaterDriftVelocity=Vector3.MoveTowards(redWaterDriftVelocity,redWaterDriftTarget,dt*4);
        Vector3 delta=redWaterDriftVelocity*dt;redWaterDriftOffset+=delta;player.transform.position+=delta;
        RedWaterReadBody();RedesignWaterBodyPenetration=RedWaterBodyCost(redWaterCapsules,Vector3.zero,0,0,out _);
    }
    float RedWaterDriftCost(Vector3 velocity,out Vector3 avoidance)
    {
        float cost=0;avoidance=Vector3.zero;
        // Compare the same clearance margin along both paths, including the
        // authored drift, rather than testing only a distant endpoint.
        for(int i=1;i<=3;i++)
        {
            float seconds=i*.5f;
            cost+=RedWaterBodyCost(redWaterCapsules,(velocity+redFloatForward*.35f)*seconds,seconds,1.6f,out var push);
            avoidance+=push;
        }
        return cost;
    }

    /// <summary>Call after creating the released stick and setting redLostVelocity at the final hit.</summary>
    void RedBeginLostStick()
    {
        redLostStickStarted=true;redLostStickFloating=false;redLostStickStartTime=Time.time;redLostStickPreviousAge=0;
    }
    /// <summary>Call every Duel flight and every Drift frame; this uses the shared scaled clock.</summary>
    void RedContinueLostStick(float dt)
    {
        if(!redLostStickStarted||redProjectile==null)return;
        float age=Time.time-redLostStickStartTime;
        if(!redLostStickFloating)
        {
            redProjectile.SetPositionAndRotation(redThrowStart+redLostVelocity*age+Vector3.down*4.9f*age*age,
                redStickRotation*Quaternion.AngleAxis(age*450,Vector3.forward));
            float gap=RedLostStickSurfaceGap();
            if(age>.25f&&redLostVelocity.y-9.8f*age<0&&gap<=0)
            {
                // Locate the first surface crossing inside this frame. Keep the
                // same shaft mesh and world scale when changing to buoyancy.
                float lo=redLostStickPreviousAge,hi=age;
                for(int i=0;i<7;i++)
                {
                    float mid=(lo+hi)*.5f;redProjectile.SetPositionAndRotation(redThrowStart+redLostVelocity*mid+Vector3.down*4.9f*mid*mid,redStickRotation*Quaternion.AngleAxis(mid*450,Vector3.forward));
                    if(RedLostStickSurfaceGap()>0)lo=mid;else hi=mid;
                }
                redLostStickFloating=true;redLostStickFloatPosition=redProjectile.position;redLostStickFloatRotation=redProjectile.rotation;
                redLostStickFloatVelocity=Vector3.ProjectOnPlane(redLostVelocity,Vector3.up)*.16f;redLostStickVerticalVelocity=0;
                RedEvent("Released stick entered water; native shaft continues floating");
            }
            redLostStickPreviousAge=age;
            if(!redLostStickFloating)return;
        }
        Vector3 shaft=redProjectile.TransformVector(redProjectileB-redProjectileA).normalized;
        Vector3 flat=Vector3.ProjectOnPlane(shaft,Vector3.up);
        if(flat.sqrMagnitude>.001f)redLostStickFloatRotation=Quaternion.RotateTowards(redLostStickFloatRotation,Quaternion.FromToRotation(shaft,flat.normalized)*redProjectile.rotation,dt*100);
        redLostStickFloatPosition+=redLostStickFloatVelocity*dt;redLostStickFloatVelocity*=Mathf.Exp(-dt*1.4f);
        redProjectile.SetPositionAndRotation(redLostStickFloatPosition,redLostStickFloatRotation);
        float targetY=redLostStickFloatPosition.y-RedLostStickSurfaceGap()-redProjectileRadius*.25f;
        // Critically damped flotation is integrated in bounded substeps so low
        // frame rates preserve continuous placement rather than snapping up.
        float remaining=dt;
        while(remaining>0)
        {
            float step=Mathf.Min(.025f,remaining);remaining-=step;
            redLostStickVerticalVelocity+=(25*(targetY-redLostStickFloatPosition.y)-10*redLostStickVerticalVelocity)*step;
            redLostStickFloatPosition.y+=redLostStickVerticalVelocity*step;
        }
        redProjectile.position=redLostStickFloatPosition;
    }
    float RedLostStickSurfaceGap()
    {
        Vector3 a=redProjectile.TransformPoint(redProjectileA),b=redProjectile.TransformPoint(redProjectileB);
        return Mathf.Min(a.y-RedWater(a),b.y-RedWater(b))-redProjectileRadius;
    }
    void RedWaterDispose()
    {
        if(redWaterQuery!=null)Destroy(redWaterQuery.gameObject);redWaterQuery=null;
        redWaterShapes.Clear();redWaterCapsules.Clear();redWaterPreview.Clear();
    }
}
