using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public sealed partial class Story1WreckBattle
{
    Vector3 redDodgeStartLocal,redDodgeEndLocal,redDodgeAttackLocal,redDodgeSharkStartLocal;
    Vector3 redDodgeDirection;
    Vector3 redSpecialStartLocal;
    bool redSpecialStartCaptured;
    Quaternion redDodgeFacingStart;
    float redSwimControlSeconds;
    bool redTutorialEnvelopeReady;
    IEnumerator<object> redTutorialEnvelopeWork;
    Vector3[] redTutorialSharkVertices;
    int redTutorialVerticesFrame=-1,redTutorialVerticesVersion=-1;
    bool redTutorialVerticesLocal;
    Matrix4x4 redTutorialVerticesMatrix;
    Bounds redTutorialSharkBounds;
    readonly List<RedWaterCapsule> redTutorialBodySpace=new List<RedWaterCapsule>(13);
    public float RedesignTutorialHeroGap { get; private set; }=float.PositiveInfinity;
    public int RedesignDodgeCandidates { get; private set; }
    public float RedesignDodgeTravel { get; private set; }
    public float RedesignSwimControlSeconds=>redSwimControlSeconds;
    public double RedesignTutorialClearanceMs { get; private set; }
    public double RedesignDodgePlanMs { get; private set; }
    public double RedesignTutorialPreparationMs { get; private set; }

    // Board-relative staging keeps the committed bite out over open water. Never chase
    // every intermediate club pose; the authored contact pose defines one destination.
    Vector3 redTutorialStepStart,redTutorialStepEnd,redTutorialStrikeNose,redTutorialAttackStart,redTutorialContactOffset;
    bool redTutorialStaging,redTutorialStrikeReady,redTutorialBlendStarted;
    float redTutorialHitTime=-1;
    Quaternion redTutorialFacingStart;
    float redTutorialTurnInSeconds,redTutorialWalkSeconds,redTutorialTurnOutSeconds;
    Vector3 redTutorialLeftSole,redTutorialRightSole;
    const float RedLightContact=.585f;
    // Safe practice reuses the duel's authored throw and held weapon mesh.
    void RedTutorialSpecial(float seconds)
    {
        RedOnBoard("PLAYER_Throw_v064",Mathf.Min(seconds,2));
        Vector3 feet=RedBoardPoint(redCombatBoard);
        if(!redSpecialStartCaptured)
        {
            redSpecialStartLocal=redCombatBoard.transform.InverseTransformPoint(sharkAnimator.NoseWorldPoint);
            redSpecialStartCaptured=true;
        }
        // Keep the shark visible in open water and move from its real previous pose.
        Vector3 destination=feet+redForward*14;destination.y=RedWater(destination)+1.2f;
        Vector3 start=redCombatBoard.transform.TransformPoint(redSpecialStartLocal);
        Vector3 nose=Vector3.Lerp(start,destination,REase(seconds,0,.6f));
        RedTutorialShark("Swim",redesignTime,nose,-redForward);RedKeepTutorialSharkClear();
        if(seconds>=.55f&&!redReleased)
        {
            RedThrow();redReleased=true;redThrowStart=redStick.position;
            redThrowTarget=redThrowStart+redForward*12;RedEvent("Special skill: club released");
        }
        if(redProjectile!=null)
        {
            float outT=R01(seconds,.55f,1.3f),back=R01(seconds,1.3f,2.3f);
            redProjectile.rotation=redStickRotation*Quaternion.AngleAxis((seconds-.55f)*1100,Vector3.up);
            redProjectile.position=seconds<1.3f?Vector3.Lerp(redThrowStart,redThrowTarget,outT)+Vector3.up*Mathf.Sin(outT*Mathf.PI):
                Vector3.Lerp(redThrowTarget,redStick.position,back)+redRight*Mathf.Sin(back*Mathf.PI)*2;
            if(seconds>2.05f)redProjectile.rotation=Quaternion.Slerp(redProjectile.rotation,redStick.rotation,REase(seconds,2.05f,2.3f));
            if(seconds>=2.3f)
            {
                redProjectile.GetComponent<Renderer>().enabled=false;Destroy(redProjectile.gameObject);redProjectile=null;
                RedShowStick(true);RedEvent("Special skill: club caught");
            }
        }
        if(seconds>=2.65f)
        {
            redReleased=false;redSpecialStartCaptured=false;lessonExecuting=false;lessonTime=0;stowReady=false;CurrentLesson=Lesson.Jump;
        }
    }
    float RedHeavyContact=>redGameplayContactSeconds;
    public float RedesignTutorialRenderedHitGap { get; private set; }=-1;
    public int RedesignTutorialConfirmedHits { get; private set; }
    void RedTutorialShark(string clip,float seconds,Vector3 nose,Vector3 forward)
    {
        if(!redTutorialBlendStarted){sharkAnimator.ReleaseCinematicAnimation();redTutorialBlendStarted=true;}
        RedShark(clip,seconds,nose,forward);
        sharkAnimator.CommitCinematicPose(RedesignClock);
        shark.position+=nose-sharkAnimator.NoseWorldPoint;
    }
    void RedStartTutorialStaging()
    {
        // The free walk may end anywhere on the deck. Start from the real feet.
        Vector3 feet=player.transform.position+Vector3.up*player.LowestFootWorldOffset;
        redTutorialStepStart=redCombatBoard.transform.InverseTransformPoint(feet);
        redTutorialFacingStart=player.transform.rotation;
        Vector3 edge=feet;
        // Walk along the current facing to the actual cut surface boundary.
        for(int i=1;i<=96;i++)
        {
            Vector3 probe=feet+redForward*(i*.25f);
            if(!redCombatBoard.ContainsFootprint(probe))break;
            edge=probe;
        }
        redForward=redCombatBoard.OpenWaterDirection(edge);redRight=Vector3.Cross(Vector3.up,redForward).normalized;
        Vector3 stance=redCombatBoard.BoardingPoint(edge-redForward*.7f)+Vector3.up*.08f;
        if(!redCombatBoard.ContainsFootprint(stance-redRight*.55f)||!redCombatBoard.ContainsFootprint(stance+redRight*.55f))stance=feet;
        redTutorialStepEnd=redCombatBoard.transform.InverseTransformPoint(stance);
        redTutorialStaging=true;redTutorialStrikeReady=false;redTutorialHitTime=-1;
    }
    void RedPrepareTutorialStrike(float seconds,bool heavy)
    {
        if(!redTutorialStaging)RedStartTutorialStaging();
        Vector3 start=redCombatBoard.transform.TransformPoint(redTutorialStepStart),end=redCombatBoard.transform.TransformPoint(redTutorialStepEnd);
        if(!redTutorialStrikeReady)
        {
            // Sample and restore the existing complete action, before anything renders.
            // The original clips/rig are retained; no arbitrary bone rotation substitutes.
            Vector3 savedStance=redStanceLocal;bool hadStance=redHasStance;RedStoreTutorialStance(end);
            string strikeClip=heavy?"CH1_SahurGuard_v050":"CH1_SahurSlash_v006";float strikeTime=heavy?0:RedLightContact;
            if(heavy)RedGameplayChargeOnBoard(redGameplayContactPhase,0);
            else{RedPose(strikeClip,strikeTime,false);RedOnBoard(strikeClip,strikeTime);}
            // The imported strike changes toe articulation. Cache its actual skinned
            // soles once, then reserve space for that pose at the real cut edge.
            SahurSupportPose.MeasureSoles(redAnimator,ref redTutorialLeftSole,ref redTutorialRightSole);
            for(int attempt=0;attempt<8;attempt++)
            {
                RedStoreTutorialStance(end);
                if(heavy)RedGameplayChargeOnBoard(redGameplayContactPhase,0);
                else RedOnBoard(strikeClip,strikeTime);
                bool supported=true;
                foreach(var foot in new[]{(HumanBodyBones.LeftFoot,redTutorialLeftSole),(HumanBodyBones.RightFoot,redTutorialRightSole)})
                {
                    Vector3 sole=redAnimator.GetBoneTransform(foot.Item1).TransformPoint(foot.Item2);
                    supported&=redCombatBoard.ContainsFootprint(sole)&&redCombatBoard.ContainsFootprint(sole+redForward*.2f)&&
                        redCombatBoard.ContainsFootprint(sole+redRight*.2f)&&redCombatBoard.ContainsFootprint(sole-redRight*.2f);
                }
                if(supported)break;
                end=redCombatBoard.BoardingPoint(end-redForward*.25f)+Vector3.up*.08f;
            }
            redTutorialStepEnd=redCombatBoard.transform.InverseTransformPoint(end);
            SahurSupportPose.Plant(redAnimator,redCombatBoard,redTutorialLeftSole,redTutorialRightSole,.5f);
            Vector3 nose=RedTutorialNose(end,6,3);
            RedShark("Bite_Lunge",.5f,nose,-redForward);
            RedFitTutorialFrontToWeapon(1);
            redTutorialStrikeNose=redCombatBoard.transform.InverseTransformPoint(sharkAnimator.NoseWorldPoint);
            redTutorialContactOffset=sharkAnimator.NoseWorldPoint-RedWeaponPoint;
            // Commit phase durations once. Wave-driven deck yaw must not flip
            // an angle threshold midway through a step and restart its turn.
            Vector3 path=Vector3.ProjectOnPlane(end-start,Vector3.up);
            bool hasTravel=path.magnitude>.15f;
            Quaternion walkHeading=Quaternion.LookRotation(hasTravel?path:redForward);
            redTutorialTurnInSeconds=Quaternion.Angle(redTutorialFacingStart,walkHeading)>10?.22f:0;
            redTutorialWalkSeconds=hasTravel?Mathf.Max(.35f,path.magnitude/3.5f):0;
            redTutorialTurnOutSeconds=hasTravel&&Quaternion.Angle(walkHeading,Quaternion.LookRotation(redForward))>10?.25f:0;
            redTutorialStrikeReady=true;redStanceLocal=savedStance;redHasStance=hadStance;
            RedPose("CH1_SahurGuard_v050",0,false);RedOnBoard("CH1_SahurGuard_v050",0);
        }
        // The safe strike stance can lie behind the current actor. Walking
        // while already facing the shark played a forward gait backwards.
        // Turn in place, walk along the actual board-relative displacement,
        // then turn to the unchanged strike heading once the feet have stopped.
        Vector3 travel=Vector3.ProjectOnPlane(end-start,Vector3.up);
        bool walking=redTutorialWalkSeconds>0;
        Quaternion strikeFacing=Quaternion.LookRotation(redForward);
        Quaternion walkFacing=walking?Quaternion.LookRotation(travel):strikeFacing;
        float turnIn=redTutorialTurnInSeconds;
        float walkSeconds=redTutorialWalkSeconds;
        float walkEnd=turnIn+walkSeconds;
        float turnOut=redTutorialTurnOutSeconds;
        float settled=walkEnd+turnOut;
        float walk=walking?REase(seconds,turnIn,walkEnd):1;
        RedStoreTutorialStance(Vector3.Lerp(start,end,walk));
        Quaternion facing=seconds<turnIn
            ?Quaternion.Slerp(redTutorialFacingStart,walkFacing,REase(seconds,0,turnIn))
            :Quaternion.Slerp(walkFacing,strikeFacing,REase(seconds,walkEnd,settled));
        if(walking&&seconds>=turnIn&&seconds<walkEnd)
            RedOnBoard("PLAYER_Walk_v241",seconds-turnIn,facing);
        else RedOnBoard("Rig|Idle_Loop",Mathf.Max(0,seconds-walkEnd),facing);
        Vector3 feet=RedBoardPoint(redCombatBoard);
        Vector3 target=redTutorialStrikeReady?redCombatBoard.transform.TransformPoint(redTutorialStrikeNose):RedTutorialNose(feet,7,3);
        float approach=REase(seconds,settled,settled+1.4f);
        Vector3 at=target+redForward*Mathf.Lerp(15,heavy?1.2f:2f,approach);
        RedTutorialShark(approach>.8f?"Bite_Lunge":"Swim",approach>.8f?0:redesignTime,at,-redForward);
        RedHeroShot(10,new Vector3(9,5,-13),55);
        if(seconds>=settled+1.4f&&redTutorialStrikeReady)
        {
            redTutorialStaging=false;RedGate(heavy?Lesson.ChargeHold:Lesson.Attack);
        }
    }
    Vector3 RedPredictTutorialWeapon(bool heavy,string clip,float actionTime)
    {
        // Preview the authored contact on the LIVE deck. Restore mixer weights, time,
        // root and pose before collision/rendering; no temporary pose is committed.
        double time=redPose.GetTime();float w0=redMixer.GetInputWeight(0),w1=redMixer.GetInputWeight(1),w2=redMixer.GetInputWeight(2);
        double chargeTime=redGameplayChargePose.GetTime();
        Vector3 root=player.transform.position;Quaternion rotation=player.transform.rotation;
        redPose.SetTime(heavy?0:RedLightContact);
        if(heavy)redGameplayChargePose.SetTime(redGameplayContactPhase*redGameplayChargeClip.length);
        redMixer.SetInputWeight(0,1);redMixer.SetInputWeight(1,0);redMixer.SetInputWeight(2,0);
        RedFeet(RedBoardPoint(redCombatBoard),Quaternion.LookRotation(redForward));
        SahurSupportPose.Plant(redAnimator,redCombatBoard,redTutorialLeftSole,redTutorialRightSole,.5f);
        Vector3 result=RedWeaponPoint;
        redPose.SetTime(time);redGameplayChargePose.SetTime(chargeTime);redMixer.SetInputWeight(0,w0);redMixer.SetInputWeight(1,w1);redMixer.SetInputWeight(2,w2);
        player.transform.SetPositionAndRotation(root,rotation);
        if(heavy)RedGameplayChargeOnBoard(RedGameplayReleasePhase(actionTime),actionTime,RedGameplayReleaseWeight(actionTime));
        else RedOnBoard(clip,actionTime);
        return result;
    }
    void RedTutorialStrike(float seconds,bool heavy)
    {
        float contact=heavy?RedHeavyContact:RedLightContact;
        float actionTime=lessonImpact?contact+Mathf.Max(0,seconds-redTutorialHitTime):Mathf.Min(seconds,contact);
        string clip=heavy?"CH1_SahurGuard_v050":"CH1_SahurSlash_v006";
        float clipTime=actionTime;
        if(heavy)RedGameplayChargeOnBoard(RedGameplayReleasePhase(actionTime),actionTime,RedGameplayReleaseWeight(actionTime));
        else RedOnBoard(clip,clipTime);
        Vector3 start=redCombatBoard.transform.TransformPoint(redTutorialAttackStart);
        Vector3 target=lessonImpact?redCombatBoard.transform.TransformPoint(redTutorialStrikeNose):RedPredictTutorialWeapon(heavy,clip,clipTime)+redTutorialContactOffset;
        float age=lessonImpact?Mathf.Max(0,seconds-redTutorialHitTime):0;
        float soleWeight=lessonImpact?1-REase(age,.05f,.25f):REase(seconds,Mathf.Max(0,contact-.2f),contact);
        SahurSupportPose.Plant(redAnimator,redCombatBoard,Vector3.Lerp(redLeftSole,redTutorialLeftSole,soleWeight),Vector3.Lerp(redRightSole,redTutorialRightSole,soleWeight),.5f);
        Vector3 nose=lessonImpact?target+redForward*(12*REase(age,.08f,1.4f)):
            Vector3.Lerp(start,target,REase(seconds,0,contact));
        RedTutorialShark(lessonImpact?"Hit_Recoil":"Bite_Lunge",lessonImpact?age:Mathf.Lerp(0,.5f,R01(seconds,0,contact)),nose,-redForward);
        if(!lessonImpact&&seconds>=contact)
        {
            // Correct only the terminal contact for the live floating deck; the approach
            // remains one committed path, independent of anticipation/follow-through arcs.
            Vector3 before=shark.position;RedFitTutorialFrontToWeapon(1);
            shark.position=before+Vector3.ClampMagnitude(shark.position-before,6*Time.deltaTime);
        }
        // Native solids run once after this pose. Confirmation happens after that pass.
        if(lessonImpact&&age>=1.5f)
        {
            RedOnBoard("CH1_SahurGuard_v050",0);
            if(heavy){RedGate(Lesson.Special);}
            else RedGate(Lesson.Dodge);
        }
    }
    internal void RedConfirmTutorialStrike()
    {
        if(!RedesignActive||RedesignStage!=OpeningStage.Tutorial||TutorialWaiting||!lessonExecuting||lessonImpact)return;
        bool heavy=CurrentLesson==Lesson.ChargeRelease;
        if(!heavy&&CurrentLesson!=Lesson.Attack)return;
        float cue=heavy?RedHeavyContact:RedLightContact;
        if(lessonTime<cue-.001f)return;
        float gap=RedHeldHeadGap(out var contact);
        RedesignTutorialRenderedHitGap=gap;
        if(gap>.055f)return;
        lessonImpact=true;redTutorialHitTime=lessonTime;RedesignTutorialConfirmedHits++;
        redTutorialStrikeNose=redCombatBoard.transform.InverseTransformPoint(sharkAnimator.NoseWorldPoint);
        Effects.Impact(contact,heavy?Vector3.down:-redForward,heavy?1.5f:.8f);RedImpactShake(heavy?1.4f:.7f,heavy?.4f:.25f);
        RedEvent((heavy?"Charge success":"Attack success")+"; final rendered head/shaft gap="+gap.ToString("F3"));
    }

    void RedPrepareTutorialBody()
    {
        if(redTutorialEnvelopeReady)return;
        if(redTutorialEnvelopeWork==null)redTutorialEnvelopeWork=RedMeasureTutorialBody().GetEnumerator();
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        do{if(!redTutorialEnvelopeWork.MoveNext()){redTutorialEnvelopeWork.Dispose();redTutorialEnvelopeWork=null;break;}}
        while(RedRiggingElapsed(started)<1.5);
        RedesignTutorialPreparationMs=System.Math.Max(RedesignTutorialPreparationMs,RedRiggingElapsed(started));
    }
    IEnumerable<object> RedMeasureTutorialBody()
    {
        RedWaterReadBody();var body=redWaterCapsules.ToArray();var radii=new float[body.Length];
        for(int i=0;i<body.Length;i++)radii[i]=body[i].radius;
        var skins=player.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(var skin in skins)
        {
            RedWaterReadBody();body=redWaterCapsules.ToArray();
            // Keep the bake and the bone envelope in the same sampled pose.
            var mesh=new Mesh();skin.BakeMesh(mesh,true);var matrix=skin.transform.localToWorldMatrix;var vertices=mesh.vertices;Destroy(mesh);
            yield return null;
            for(int vertex=0;vertex<vertices.Length;vertex++)
            {
                Vector3 point=matrix.MultiplyPoint3x4(vertices[vertex]);int nearest=-1;float best=float.PositiveInfinity;
                for(int i=0;i<body.Length;i++)
                {
                    Vector3 axis=body[i].b-body[i].a;
                    Vector3 closest=body[i].a+axis*Mathf.Clamp01(Vector3.Dot(point-body[i].a,axis)/Mathf.Max(.000001f,axis.sqrMagnitude));
                    float distance=(point-closest).sqrMagnitude;if(distance<best){best=distance;nearest=i;}
                }
                if(nearest>=0)radii[nearest]=Mathf.Max(radii[nearest],Mathf.Sqrt(best)+.06f);
                if((vertex&255)==255)yield return null;
            }
        }
        redWaterBodyRadii=radii;redTutorialEnvelopeReady=true;
    }

    void RedTutorialBody()
    {
        RedWaterReadBody();
        if(!redTutorialEnvelopeReady){RedWaterMeasureNativeEnvelope();redTutorialEnvelopeReady=true;RedWaterReadBody();}
    }
    void RedKeepTutorialSharkClear(float margin=.3f)
    {
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        try{RedSeparateTutorialShark(margin);}
        finally{RedesignTutorialClearanceMs=RedRiggingElapsed(started);}
    }
    static readonly Unity.Profiling.ProfilerMarker RedSharkHeroMarker=new Unity.Profiling.ProfilerMarker("Opening.SharkHero");
    void RedSeparateTutorialShark(float margin,Vector3? escape=null,bool preciseContact=false)
    {
        using var measured=RedSharkHeroMarker.Auto();
        var skin=RedContactSkin();if(skin==null||redRiggingSkinTree==null)return;
        RedTutorialBody();
        if(redTutorialSharkVertices==null||redTutorialSharkVertices.Length!=redPosedVertices.Count)
            redTutorialSharkVertices=new Vector3[redPosedVertices.Count];
        var matrix=skin.transform.localToWorldMatrix;var inverse=skin.transform.worldToLocalMatrix;
        Vector3 x=matrix.MultiplyVector(Vector3.right),y=matrix.MultiplyVector(Vector3.up),z=matrix.MultiplyVector(Vector3.forward);
        float scale=x.magnitude;
        bool local=scale>.00001f&&Mathf.Abs(y.magnitude-scale)<scale*.00001f&&Mathf.Abs(z.magnitude-scale)<scale*.00001f&&
            Mathf.Abs(Vector3.Dot(x,y))<scale*scale*.00001f&&Mathf.Abs(Vector3.Dot(x,z))<scale*scale*.00001f&&Mathf.Abs(Vector3.Dot(y,z))<scale*scale*.00001f;
        // A uniform rigid transform preserves all capsule/triangle distances.
        // Keeping the tree local avoids rebuilding it for root travel/turns.
        if(!local){scale=1;inverse=Matrix4x4.identity;}
        margin/=scale;
        redTutorialBodySpace.Clear();
        foreach(var capsule in redWaterCapsules)redTutorialBodySpace.Add(new RedWaterCapsule(inverse.MultiplyPoint3x4(capsule.a),inverse.MultiplyPoint3x4(capsule.b),capsule.radius/scale));
        // Root fitting cannot change local skinned vertices. Rebuild this exact
        // snapshot only after a native bake; nonuniform/world fallback also
        // tracks the complete renderer matrix. No padded/approximate substitute.
        bool sameVertices=redTutorialVerticesFrame==redContactBakedFrame&&redTutorialVerticesVersion==redContactBakedVersion&&
            redTutorialVerticesLocal==local&&(local||redTutorialVerticesMatrix.Equals(matrix));
        if(!sameVertices)
        {
            redTutorialSharkBounds=new Bounds(local?redPosedVertices[0]:matrix.MultiplyPoint3x4(redPosedVertices[0]),Vector3.zero);
            for(int i=0;i<redPosedVertices.Count;i++){redTutorialSharkVertices[i]=local?redPosedVertices[i]:matrix.MultiplyPoint3x4(redPosedVertices[i]);redTutorialSharkBounds.Encapsulate(redTutorialSharkVertices[i]);}
            redTutorialVerticesFrame=redContactBakedFrame;redTutorialVerticesVersion=redContactBakedVersion;
            redTutorialVerticesLocal=local;redTutorialVerticesMatrix=matrix;
        }
        Bounds bounds=redTutorialSharkBounds;
        // A conservative bound can prove separation without refitting and
        // traversing the full triangle tree while the shark is retreating.
        float lowerGap=float.PositiveInfinity;
        foreach(var capsule in redTutorialBodySpace)
        {
            Vector3 lo=Vector3.Min(capsule.a,capsule.b),hi=Vector3.Max(capsule.a,capsule.b);
            Vector3 delta=Vector3.Max(Vector3.zero,Vector3.Max(lo-bounds.max,bounds.min-hi));
            lowerGap=Mathf.Min(lowerGap,delta.magnitude-capsule.radius);
        }
        if(lowerGap>=margin){RedesignTutorialHeroGap=lowerGap*scale;return;}
        RedRiggingTree tree;
        if(local)tree=RedSolidIndex(skin);
        else
        {
            tree=redRiggingSkinTree;tree.RefitArticulated(redTutorialSharkVertices,.15f);
            redSolidSkinFrame=-1; // The nonuniform-scale fallback uses world space.
        }
        Vector3 offset=Vector3.zero,forward=inverse.MultiplyVector(shark.forward).normalized;
        Vector3 away=escape.HasValue&&escape.Value.sqrMagnitude>.01f?inverse.MultiplyVector(escape.Value).normalized:-forward;
        // The shark can continue its committed attack only while the actual
        // posed skin stays clear of the hero's measured full-body envelope.
        // Query translations against this one posed tree, avoiding extra bakes.
        Vector3 blockedOffset=Vector3.zero;bool hasBlockedOffset=false;
        float GapAt(Vector3 candidate)
        {
            float result=float.PositiveInfinity;
            foreach(var capsule in redTutorialBodySpace)
                result=Mathf.Min(result,tree.CapsuleGapUpTo(capsule.a-candidate,capsule.b-candidate,capsule.radius,margin));
            return result;
        }
        // Past 32 short steps the old fallback threw the whole animal a body
        // length behind the hero for one frame. Keep escaping the same way,
        // with growing steps, before resorting to it.
        bool cleared=false;
        for(int attempt=0;attempt<64;attempt++)
        {
            float gap=GapAt(offset);
            RedesignTutorialHeroGap=gap*scale;
            if(gap>=margin){cleared=true;break;}
            blockedOffset=offset;hasBlockedOffset=true;
            offset+=away*Mathf.Clamp(margin-gap+.03f/scale,.15f/scale,3/scale)*(attempt<32?1:2);
        }
        // Re-testing the rescaled gap against the rescaled margin disagreed
        // with the loop by float rounding, so an already clear pose took the
        // fallback and was thrown a full body length (57 m) for one frame.
        if(!cleared)
        {
            float front=float.NegativeInfinity,near=float.PositiveInfinity;
            foreach(var point in redTutorialSharkVertices)front=Mathf.Max(front,Vector3.Dot(point,forward));
            foreach(var capsule in redTutorialBodySpace)near=Mathf.Min(near,Mathf.Min(Vector3.Dot(capsule.a,forward),Vector3.Dot(capsule.b,forward))-capsule.radius);
            offset-=forward*Mathf.Max(0,front+Vector3.Dot(offset,forward)-near+margin+.03f/scale);
            RedesignTutorialHeroGap=float.PositiveInfinity;
            foreach(var capsule in redTutorialBodySpace)RedesignTutorialHeroGap=Mathf.Min(RedesignTutorialHeroGap,tree.CapsuleGap(capsule.a-offset,capsule.b-offset,capsule.radius)*scale);
        }
        // Coarse clearance steps intentionally leave a safety pad during a
        // near miss. A scripted contact instead needs the last clear position
        // on the exact posed skin: its 15 cm minimum step can overshoot the
        // 3.5 cm hit window and strand the finale forever. Refine only the
        // bracket already proved blocked/clear, retaining full-body separation.
        if(preciseContact&&hasBlockedOffset&&(cleared||RedesignTutorialHeroGap>=margin*scale))
        {
            Vector3 clearOffset=offset;
            for(int i=0;i<14&&(clearOffset-blockedOffset).sqrMagnitude>.000001f/(scale*scale);i++)
            {
                Vector3 middle=(blockedOffset+clearOffset)*.5f;
                if(GapAt(middle)>=margin)clearOffset=middle;else blockedOffset=middle;
            }
            offset=clearOffset;RedesignTutorialHeroGap=GapAt(offset)*scale;
        }
        shark.position+=local?matrix.MultiplyVector(offset):offset;
    }
    void RedStoreTutorialStance(Vector3 feet)
    {
        redStanceLocal=redCombatBoard.transform.InverseTransformPoint(feet);redHasStance=true;
    }
    void RedPlanTutorialDodge()
    {
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        redDodgeFacingStart=player.transform.rotation;
        Vector3 feet=RedBoardPoint(redCombatBoard);RedTutorialBody();RedWaterCollectShapes();
        redWaterShapes.RemoveAll(s=>s.mesh==redCombatBoard.Collision);
        redDodgeStartLocal=redCombatBoard.transform.InverseTransformPoint(feet);
        redDodgeSharkStartLocal=redCombatBoard.transform.InverseTransformPoint(sharkAnimator.NoseWorldPoint);
        redDodgeAttackLocal=redDodgeStartLocal;redDodgeEndLocal=redDodgeStartLocal;
        RedesignDodgeCandidates=0;RedesignDodgeTravel=0;
        var candidates=new List<(Vector3 point,float score)>();
        // Destinations and the complete travel segment come from the current
        // real deck and surrounding native compound timber, not a fixed side.
        for(int ring=0;ring<3;ring++)for(int bearing=0;bearing<16;bearing++)
        {
            Vector3 direction=Quaternion.AngleAxis(bearing*22.5f,Vector3.up)*redForward;
            Vector3 candidate=redCombatBoard.BoardingPoint(feet+direction*(2.5f+ring*1.25f))+Vector3.up*.08f;
            Vector3 delta=candidate-feet;float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
            if(distance<1.5f)continue;
            float score=Mathf.Abs(Vector3.Dot(delta,redRight))*2+distance*.3f;
            bool duplicate=false;foreach(var previous in candidates)if((previous.point-candidate).sqrMagnitude<.01f){duplicate=true;break;}
            if(!duplicate)candidates.Add((candidate,score));
        }
        candidates.Sort((a,b)=>b.score.CompareTo(a.score));
        // Check the most useful open direction first. Stop at the first fully
        // validated path rather than solving every other usable destination.
        foreach(var option in candidates)
        {
            RedesignDodgeCandidates++;Vector3 candidate=option.point,delta=candidate-feet;
            float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;bool clear=true;
            for(int sample=1;sample<=12&&clear;sample++)
            {
                float u=sample/12f;Vector3 point=Vector3.Lerp(feet,candidate,u);
                // Cover the rolling silhouette and the gap between probes.
                // A legal standing position near the cut edge need not fit
                // the full rolling silhouette before the roll has moved
                // inboard. Grow support coverage over the entry quarter.
                float radius=Mathf.Lerp(.35f,.8f+distance/24,REase(u,0,.25f));
                for(int edge=0;edge<8;edge++)
                    if(!redCombatBoard.ContainsFootprint(point+Quaternion.AngleAxis(edge*45,Vector3.up)*Vector3.forward*radius)){clear=false;break;}
                if(clear&&RedWaterBodyCost(redWaterCapsules,delta*u,u*.8f,.12f,out _)>0)clear=false;
            }
            if(!clear)continue;
            redDodgeEndLocal=redCombatBoard.transform.InverseTransformPoint(candidate);redDodgeDirection=delta.normalized;RedesignDodgeTravel=distance;
            break;
        }
        if(RedesignDodgeTravel<.1f)redDodgeDirection=-redRight;
        RedEvent("Adaptive dodge; candidates="+RedesignDodgeCandidates+"; travel="+RedesignDodgeTravel.ToString("F2")+"; local target="+redDodgeEndLocal.ToString("F2"));
        RedesignDodgePlanMs=RedRiggingElapsed(started);
    }
    void RedTutorialDodge(float seconds)
    {
        Vector3 start=redCombatBoard.transform.TransformPoint(redDodgeStartLocal),end=redCombatBoard.transform.TransformPoint(redDodgeEndLocal);
        float u=REase(seconds,.05f,.95f);
        Vector3 point=Vector3.Lerp(start,end,u);
        float rollLength=RedClip("Rig|Roll").length;
        Quaternion rollFacing=Quaternion.LookRotation(redDodgeDirection);
        Quaternion facing=Quaternion.Slerp(redDodgeFacingStart,rollFacing,REase(seconds,0,.12f));
        facing=Quaternion.Slerp(facing,Quaternion.LookRotation(redForward),REase(seconds,1.02f,rollLength));
        // Finish the authored gather/stand instead of cutting off the recovery
        // and snapping the root back towards the shark at 1.05 seconds.
        if(seconds<rollLength){RedPose("Rig|Roll",seconds);RedFeet(point,facing);RedSolidSupport();}
        else{RedStoreTutorialStance(end);RedOnBoard("CH1_SahurGuard_v050",seconds-rollLength);}
        Vector3 attack=redCombatBoard.transform.TransformPoint(redDodgeAttackLocal)+Vector3.up*(redHeroHeight*.45f);
        // The bite remains aimed at the position committed before the roll.
        float run=seconds<.9f?Mathf.Lerp(10,-1,REase(seconds,0,.9f)):Mathf.Lerp(-1,18,REase(seconds,.9f,2.2f));
        Vector3 nose=seconds<.9f?Vector3.Lerp(redCombatBoard.transform.TransformPoint(redDodgeSharkStartLocal),attack-redForward,REase(seconds,0,.9f)):attack+redForward*run;
        RedTutorialShark(seconds<1.1f?"Bite_Lunge":"Swim",seconds<1.1f?seconds:seconds-1.1f,nose,-redForward);
        RedKeepTutorialSharkClear(.4f);
        if(seconds>=2.3f&&RedesignTutorialHeroGap>=.39f)
        {RedOnBoard("CH1_SahurGuard_v050",0);lessonExecuting=false;lessonTime=0;CurrentLesson=Lesson.ChargeHold;RedStartTutorialStaging();}
    }
    Vector3 RedTutorialNose(Vector3 feet,float distance,float height=2.25f)
    {
        Vector3 nose=feet+redForward*distance;nose.y=RedWater(nose)+height;return nose;
    }
    void RedFitTutorialFrontToWeapon(float weight)
    {
        var skin=RedContactSkin();if(skin==null)return;
        float front=float.NegativeInfinity;var matrix=skin.transform.localToWorldMatrix;
        foreach(int i in redHeadVertices)front=Mathf.Max(front,Vector3.Dot(matrix.MultiplyPoint3x4(redPosedVertices[i]),shark.forward));
        Vector3 target=RedWeaponPoint,surface=sharkAnimator.NoseWorldPoint;float best=float.PositiveInfinity;
        foreach(int i in redHeadVertices)
        {
            Vector3 p=matrix.MultiplyPoint3x4(redPosedVertices[i]);
            if(Vector3.Dot(p,shark.forward)<front-.6f)continue;
            float distance=(p-target).sqrMagnitude;if(distance<best){best=distance;surface=p;}
        }
        Story1CapsuleContact.Segment(redWeapon,out _,out _,out float radius);
        Vector3 approach=(target-surface).normalized;
        shark.position+=(target-approach*radius-surface)*weight;
    }
}
