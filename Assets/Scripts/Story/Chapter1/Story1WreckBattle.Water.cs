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
    struct RedWaterShape
    {
        public Story1WreckMeshCollision mesh;
        public Rigidbody body;
        public Bounds bounds;
        public Vector3 velocity,spin,centre;
        public Bounds[] partBounds;
        public Vector3[] partPositions;
        public Quaternion[] partRotations;
    }
    sealed class RedWaterForecast
    {
        public int epoch=-1;
        public float seconds;
        public Bounds[] shapeBounds,partBounds;
        public Vector3[] positions;
        public Quaternion[] rotations;
        public int[] starts;
        public void Reserve(int shapes,int parts)
        {
            if(shapeBounds==null||shapeBounds.Length<shapes){int n=Mathf.NextPowerOfTwo(Mathf.Max(1,shapes));shapeBounds=new Bounds[n];starts=new int[n];}
            if(partBounds==null||partBounds.Length<parts){int n=Mathf.NextPowerOfTwo(Mathf.Max(1,parts));partBounds=new Bounds[n];positions=new Vector3[n];rotations=new Quaternion[n];}
        }
    }
    readonly List<RedWaterForecast> redWaterForecasts=new List<RedWaterForecast>(8);
    int redWaterForecastEpoch;
    RedWaterForecast RedWaterForecastAt(float seconds)
    {
        RedWaterForecast available=null;
        foreach(var item in redWaterForecasts)
        {
            if(item.epoch==redWaterForecastEpoch&&item.seconds==seconds)return item;
            if(item.epoch!=redWaterForecastEpoch&&available==null)available=item;
        }
        if(available==null){available=new RedWaterForecast();redWaterForecasts.Add(available);}
        int count=0;foreach(var shape in redWaterShapes)count+=shape.mesh.Parts.Length;
        available.Reserve(redWaterShapes.Count,count);count=0;
        for(int index=0;index<redWaterShapes.Count;index++)
        {
            var shape=redWaterShapes[index];available.starts[index]=count;
            Vector3 translation=RedWaterForecastTranslation(shape,seconds);
            Quaternion rotation=shape.spin.sqrMagnitude>.0001f?Quaternion.AngleAxis(shape.spin.magnitude*Mathf.Rad2Deg*Mathf.Min(seconds,.7f),shape.spin.normalized):Quaternion.identity;
            float reach=shape.bounds.extents.magnitude+Vector3.Distance(shape.bounds.center,shape.centre);
            available.shapeBounds[index]=new Bounds(shape.centre+translation,Vector3.one*(reach*2));
            for(int i=0;i<shape.mesh.Parts.Length;i++,count++)
            {
                available.positions[count]=shape.centre+translation+rotation*(shape.partPositions[i]-shape.centre);
                available.rotations[count]=rotation*shape.partRotations[i];
                Bounds present=shape.partBounds[i];Vector3 e=present.extents;
                Vector3 x=rotation*(Vector3.right*e.x),y=rotation*(Vector3.up*e.y),z=rotation*(Vector3.forward*e.z);
                Vector3 extents=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
                available.partBounds[count]=new Bounds(shape.centre+translation+rotation*(present.center-shape.centre),extents*2);
            }
        }
        available.epoch=redWaterForecastEpoch;available.seconds=seconds;return available;
    }
    readonly List<RedWaterShape> redWaterShapes=new List<RedWaterShape>();
    readonly List<RedWaterCapsule> redWaterCapsules=new List<RedWaterCapsule>();
    readonly List<RedWaterCapsule> redWaterPreview=new List<RedWaterCapsule>();
    IEnumerator<object> redWaterFallPreparation;
    float[] redWaterFallRadii;
    public bool RedesignWaterPreviewPrepared=>redWaterFallRadii!=null;
    void RedWarmWaterPreview()
    {
        if(redWaterFallRadii!=null||!redTutorialEnvelopeReady||!redesignManual||!redGraph.IsValid())return;
        if(redWaterFallPreparation==null)redWaterFallPreparation=RedPrepareFallEnvelope().GetEnumerator();
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        do{if(!redWaterFallPreparation.MoveNext()){redWaterFallPreparation.Dispose();redWaterFallPreparation=null;break;}}
        while(RedRiggingElapsed(started)<1.5);
    }
    IEnumerable<object> RedPrepareFallEnvelope()
    {
        // Sample the approved pose on this very rig, restoring the current graph
        // before returning to the frame. Its 279k native vertices are then measured
        // in bounded slices without retaining a second character or changing clips.
        var samples=new List<(List<Vector3> vertices,Matrix4x4 matrix)>();
        var root=player.transform;var rotation=root.rotation;var position=root.position;
        var weights=new[]{redMixer.GetInputWeight(0),redMixer.GetInputWeight(1),redMixer.GetInputWeight(2)};
        var pose=UnityEngine.Animations.AnimationClipPlayable.Create(redGraph,RedClip("PLAYER_Fall_v006"));
        var mesh=new Mesh();RedWaterCapsule[] body;
        try
        {
            redMixer.ConnectInput(1,pose,0);redMixer.SetInputWeight(0,0);redMixer.SetInputWeight(1,1);redMixer.SetInputWeight(2,0);
            pose.SetSpeed(0);pose.SetTime(2.2);root.rotation=Quaternion.LookRotation(redForward);redGraph.Evaluate(0);
            RedWaterReadBody();body=redWaterCapsules.ToArray();
            foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.BakeMesh(mesh,true);var vertices=new List<Vector3>(mesh.vertexCount);mesh.GetVertices(vertices);
                samples.Add((vertices,skin.transform.localToWorldMatrix));mesh.Clear();
            }
        }
        finally
        {
            redMixer.DisconnectInput(1);redGraph.DestroyPlayable(pose);
            for(int i=0;i<3;i++)redMixer.SetInputWeight(i,weights[i]);
            root.SetPositionAndRotation(position,rotation);redGraph.Evaluate(0);RedWaterReadBody();Destroy(mesh);
        }
        yield return null;
        var maxima=new float[body.Length];var lines=new Vector3[body.Length];var inverseLengths=new float[body.Length];var radii=new float[body.Length];
        for(int j=0;j<body.Length;j++){lines[j]=body[j].b-body[j].a;inverseLengths[j]=1/Mathf.Max(.000001f,lines[j].sqrMagnitude);radii[j]=body[j].radius;}
        foreach(var sample in samples)
        {
            for(int vertex=0;vertex<sample.vertices.Count;vertex++)
            {
                Vector3 point=sample.matrix.MultiplyPoint3x4(sample.vertices[vertex]);float best=float.PositiveInfinity;int nearest=-1;
                for(int j=0;j<body.Length;j++)
                {
                    var closest=body[j].a+lines[j]*Mathf.Clamp01(Vector3.Dot(point-body[j].a,lines[j])*inverseLengths[j]);
                    float square=(point-closest).sqrMagnitude;if(square<best){best=square;nearest=j;}
                }
                if(nearest>=0)maxima[nearest]=Mathf.Max(maxima[nearest],best);
                if((vertex&255)==255)yield return null;
            }
        }
        for(int j=0;j<radii.Length;j++)radii[j]=Mathf.Max(radii[j],Mathf.Sqrt(maxima[j])+.06f);
        redWaterFallRadii=radii;
    }
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
        if(unchecked(++redWaterForecastEpoch)==0){redWaterForecastEpoch=1;foreach(var cache in redWaterForecasts)cache.epoch=-1;}
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
        mesh.ReadWorldGeometry(out var bounds,out var boxes,out var positions,out var rotations);
        redWaterShapes.Add(new RedWaterShape{mesh=mesh,body=body,bounds=bounds,partBounds=boxes,partPositions=positions,partRotations=rotations,
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
        var forecast=RedWaterForecastAt(seconds);
        redWaterQuery.enabled=true;
        try
        {
        for(int shapeIndex=0;shapeIndex<redWaterShapes.Count;shapeIndex++)
        {
            var shape=redWaterShapes[shapeIndex];
            // The same seven forecast times are reused by every candidate and
            // body capsule. Snapshot native geometry once; retain exact narrow phase.
            if(!forecast.shapeBounds[shapeIndex].Intersects(capsuleBounds))continue;
            for(int i=0;i<shape.mesh.Parts.Length;i++)
            {
                var part=shape.mesh.Parts[i];
                if(part==null||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                int at=forecast.starts[shapeIndex]+i;
                Vector3 point=forecast.positions[at];Quaternion turn=forecast.rotations[at];
                Bounds partBounds=forecast.partBounds[at];
                partBounds.Expand(radius*2);
                if(!partBounds.Intersects(capsuleBounds))continue;
                if(!Physics.ComputePenetration(redWaterQuery,(a+b)*.5f,capsuleRotation,part,point,turn,out var direction,out float depth))continue;
                cost+=depth*depth+.01f;
                Vector3 planar=Vector3.ProjectOnPlane(direction,Vector3.up);
                if(planar.sqrMagnitude<.04f)planar=Vector3.ProjectOnPlane((a+b)*.5f-forecast.shapeBounds[shapeIndex].center,Vector3.up);
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
    static readonly Unity.Profiling.ProfilerMarker RedWaterPlanMarker=new Unity.Profiling.ProfilerMarker("Opening.WaterExitPlan");
    void RedPlanWaterExit()
    {
        using var measured=RedWaterPlanMarker.Auto();
        RedWaterCollectShapes();redWaterExitPlanned=true;redWaterExitVelocity=redRight*12;
        redWaterDriftOffset=redWaterDriftVelocity=redWaterDriftTarget=Vector3.zero;redWaterNextSearch=0;
        // Preview the actual unconscious pose on the same rig, then restore the
        // current sample. No proxy character, re-scaling or renderer changes.
        string previous=redClip.name;float sample=(float)redPose.GetTime();
        Vector3 root=player.transform.position;Quaternion rotation=player.transform.rotation;
        RedPose("PLAYER_Fall_v006",2.2f,false);player.transform.rotation=Quaternion.LookRotation(redForward);redGraph.Evaluate(0);
        redWaterPreviewHeadY=RedHead.y-player.transform.position.y;RedWaterReadBody();
        if(redWaterFallRadii!=null&&redWaterFallRadii.Length==redWaterCapsules.Count)
        {
            if(redWaterBodyRadii==null||redWaterBodyRadii.Length!=redWaterFallRadii.Length)redWaterBodyRadii=new float[redWaterFallRadii.Length];
            for(int i=0;i<redWaterFallRadii.Length;i++)redWaterBodyRadii[i]=Mathf.Max(redWaterCapsules[i].radius,redWaterFallRadii[i]);
            RedWaterReadBody();
        }
        else RedWaterMeasureNativeEnvelope();
        redWaterPreview.Clear();
        foreach(var capsule in redWaterCapsules)redWaterPreview.Add(new RedWaterCapsule(capsule.a-player.transform.position,capsule.b-player.transform.position,capsule.radius));
        player.transform.SetPositionAndRotation(root,rotation);RedPose(previous,sample,false);
        float best=float.PositiveInfinity;float bestContact=0;
        float[] bearings={0,15,-15,30,-30,45,-45,60,-60};float[] speeds={12,15,18,21};
        bool optimal=false;
        foreach(float speed in speeds)
        {
            foreach(float bearing in bearings)
            {
                Vector3 velocity=Quaternion.AngleAxis(bearing,Vector3.up)*redRight*speed;
                float contact=0;float preference=Mathf.Abs(bearing)*.0015f+(speed-12)*.015f;
                bool pruned=false;
                for(int sampleIndex=0;sampleIndex<7;sampleIndex++)
                {
                    float flight=sampleIndex<4?1.3f+sampleIndex*.35f:1.65f;
                    float drift=sampleIndex<4?0:(sampleIndex-3)*1.8f;
                    Vector3 at=redFloatStart+velocity*flight+redForward*(drift*.35f);
                    at.y=RedWater(at)+.32f-redWaterPreviewHeadY;
                    contact+=RedWaterBodyCost(redWaterPreview,at,flight+drift,.65f+Mathf.Min(drift,.6f)*.3f,out _);
                    // Every remaining cost is nonnegative; this candidate can
                    // no longer beat the current choice, including tie order.
                    if(contact*100+preference>=best){pruned=true;break;}
                }
                if(pruned)continue;
                float score=contact*100+preference;
                if(score<best){best=score;bestContact=contact;redWaterExitVelocity=velocity;}
                // Zero is the global lower bound, not just a reason to stop
                // the innermost bearing loop and evaluate 27 more candidates.
                if(best==0){optimal=true;break;}
            }
            if(optimal)break;
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
        redWaterFallPreparation?.Dispose();redWaterFallPreparation=null;redWaterFallRadii=null;
        redWaterShapes.Clear();redWaterCapsules.Clear();redWaterPreview.Clear();redWaterForecasts.Clear();
    }
}
