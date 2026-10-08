using System.Linq;
using Mavis;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Paired continuous choreography: equal exchange, advantage, reversal and defeat.</summary>
[DefaultExecutionOrder(900), DisallowMultipleComponent]
public sealed class Story1WreckFinale : MonoBehaviour
{
    public float Elapsed { get; private set; }
    public string CurrentBeat { get; private set; }
    public int HeroCounterCount { get; private set; }
    public int SharkCounterCount { get; private set; }
    public bool HasLostStick { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsFloating=>floating;
    public float LargestContactGap { get; private set; }
    public readonly System.Collections.Generic.List<float> ContactGaps=new System.Collections.Generic.List<float>();
    public float LargestContactCorrection { get; private set; }
    public float LargestFootHeightError { get; private set; }
    public float WorstFootTime { get; private set; }
    public float LargestPlantSlide { get; private set; }
    public readonly float[] ContactAttemptGaps={999,999,999,999,999,999};
    public readonly string[] ContactObstructions=new string[6];
    public const float Duration=20.8f;
    public int BoardJumpCount { get; private set; }
    public int SharkBreachCount { get; private set; }
    public float LargestLandingError { get; private set; }
    public float LargestBreachClearance { get; private set; }
    public float CinematicTime=>FilmTime;
    public float LeadInSeconds { get; private set; }=.6f;
    // Sample crossed cues at their actual sub-frame time, then render the end-of-frame pose.
    static readonly float[] Cues={8.32f,8.94f,9.8f,14.572f,18.05f,18.22f};
    Story1EncounterBeat[] beats;
    readonly System.Collections.Generic.Dictionary<string,AnimationClip> revisedClips=new System.Collections.Generic.Dictionary<string,AnimationClip>();
    readonly System.Collections.Generic.HashSet<int> verifiedContacts=new System.Collections.Generic.HashSet<int>();
    float[] contactTimes={1.184f,2.43f,7.03f,8.94f,14.572f,18.05f};
    Story1EncounterContact[] contacts;
    Story1EncounterAction activeAction;
    Story1WreckPlank anchoredBoard;
    string anchorAnimation;
    Vector3 plantedSoleLocal;
    bool leftPlanted;
    readonly Vector3[] lastContactA=new Vector3[6],lastContactB=new Vector3[6];
    readonly float[] lastContactTime=new float[6];
    Story1WreckBattle battle;
    ThirdPersonPlayerController hero;
    TralaleroSwimAnimator shark;
    Camera shotCamera;
    Story1WreckPlank board;
    OceanWorld ocean;
    Animator animator;
    Transform stick,projectile,leftFoot,rightFoot;
    Renderer[] held;
    bool[] heldEnabled,colliderEnabled;
    Collider[] actorColliders;
    SkinnedMeshRenderer[] sampledSkins;
    bool[] skinMatrixModes,skinOffscreenModes;
    Mesh stickMesh;
    CapsuleCollider weapon;
    CapsuleCollider projectileShape;
    CapsuleCollider torso;
    string stageBeat;
    AnimationClip slash,guard,block,throwClip,stagger,knockdown,swim,climb,walk;
    AnimationClip jumpStart,jumpLoop,jumpLand;
    AnimationClip boardJump;
    PlayableGraph poseGraph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable pose,previousPose;
    AnimationClip currentClip,previousClip;
    string activeAnimation;
    float poseStart,poseSpeed,poseOffset,previousStart,previousSpeed,previousOffset,blendStart,blendSeconds;
    Vector3 startFeet,initialNose,cameraStart,stageBodyStart,stageNoseStart,throwStart,throwTarget,lostStart,lostVelocity,fallStart;
    Vector3 equalAim,upperAim,tailAnchor;
    Vector3 filmTailLocal,filmShipTailLocal;
    Vector3 slashContactOffset;
    bool slashContactMeasured;
    Story1WreckPlank slashSampleBoard,parrySampleBoard;
    Vector3 slashSampleNormal,parrySampleNormal;
    Vector3 parryContactOffset;
    bool parryContactMeasured;
    float parryShaftFraction=.45f;
    Quaternion attackFacing;
    string attackFacingKey;
    float attackFacingSampleTime=-100;
    Vector3 probeRaisedNose;
    Vector3 leftSole,rightSole;
    Quaternion cameraRotation,projectileRotation,stageRotation;
    Quaternion initialHeroRotation;
    float cameraFov,swimSeconds,soleOffset;
    bool fromWater;
    bool entryPullStarted;
    Vector3 entryPullStartLocal,entryLeftGrip,entryRightGrip;
    Vector3 entryLandingLocal,entryOutwardLocal;
    float entryWalkSeconds;
    SahurSwimmingWeapon swimmingWeapon;
    Rigidbody supportBody;
    float originalDamping;
    Vector3 fallVelocity;
    float defeatContactTime=-1,stickLossTime=-1;
    bool floating;
    Vector3 floatingChest,floatDrift;
    Quaternion floatHeading,floatRotation;
    float floatHeightVelocity;
    float cameraSide=1,cameraOccludedSeconds;
    Vector2 deckOffset;
    Vector3 localForward=Vector3.forward;
    float walkSeconds;
    Story1WreckPlank hopFrom,hopTarget;
    Vector3 hopStartLocal,hopTakeoffLocal,hopLandingLocal;
    int hopIndex=-1,breachSection=-1;
    bool hopLanded;
    Quaternion hopFacing;
    Vector3 breachStart,breachTarget,breachDirection;
    Quaternion breachRotation;
    bool breachLiftSpray,breachEntrySpray;
    int stage=-1;
    float FilmTime=>Elapsed-LeadInSeconds;
    float ChoreoTime=>FilmTime;
    Vector3 Forward=>Vector3.ProjectOnPlane(board.transform.TransformDirection(localForward),Vector3.up).normalized;
    Vector3 Side=>Vector3.Cross(Vector3.up,Forward);
    Vector3 Deck(float z=0,float x=0)
    {
        float halfZ=Mathf.Max(.1f,board.Dimensions.z*.5f-.85f),halfX=Mathf.Max(0,board.Dimensions.x*.5f-.7f);
        Vector2 forward=new Vector2(localForward.x,localForward.z),across=new Vector2(localForward.z,-localForward.x);
        Vector2 point=deckOffset+forward*z+across*x;
        return board.BoardingPoint(board.transform.TransformPoint(new Vector3(Mathf.Clamp(point.x,-halfX,halfX)/board.Dimensions.x,
            board.TopLocalY,Mathf.Clamp(point.y,-halfZ,halfZ)/board.Dimensions.z)))+Vector3.up*.04f;
    }
    public bool CanBreakBoards=>activeAnimation!=null && activeAnimation.StartsWith("Jump airborne") || (FilmTime>=14.42f && FilmTime<14.9f) || (FilmTime>=17.9f && FilmTime<18.3f);
    public bool ProtectsBoard(Story1WreckPlank candidate)=>(defeatContactTime<0 && candidate==board) || (candidate==hopTarget && activeAnimation!=null && activeAnimation.StartsWith("Jump airborne"));
    void FaceOpenWater(Vector3 point)
    {
        localForward=board.transform.InverseTransformDirection(board.OpenWaterDirection(point));localForward.y=0;localForward.Normalize();
    }
    float Water(Vector3 at)=>ocean!=null ? ocean.SampleSurfaceHeight(at,Time.time) : hero.seaLevel;
    static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
    public static float TimelineForBeat(float beat)=>beat;
    float Clock(float film)=>film+LeadInSeconds;
    public void Initialize(Story1WreckBattle encounter,ThirdPersonPlayerController player,TralaleroSwimAnimator enemy,
        Camera camera,Story1WreckPlank support,OceanWorld water,bool swimming)
    {
        battle=encounter;hero=player;shark=enemy;shotCamera=camera;board=support;ocean=water;fromWater=swimming;
        swimmingWeapon=hero.GetComponent<SahurSwimmingWeapon>();
        if(swimmingWeapon!=null)swimmingWeapon.CinematicTraversal=fromWater;
        supportBody=board.GetComponent<Rigidbody>();originalDamping=supportBody.angularDamping;supportBody.angularDamping=6;
        board.Drive(0,0,null);
        animator=hero.CharacterAnimator;
        sampledSkins=hero.GetComponentsInChildren<SkinnedMeshRenderer>(true).Concat(shark.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Distinct().ToArray();
        skinMatrixModes=sampledSkins.Select(s=>s.forceMatrixRecalculationPerRender).ToArray();
        skinOffscreenModes=sampledSkins.Select(s=>s.updateWhenOffscreen).ToArray();
        // Contact planning samples several future poses within one frame.
        // Each camera must skin the final bones rather than reuse a matrix
        // cache from a temporary sampled pose.
        foreach(var skin in sampledSkins){skin.forceMatrixRecalculationPerRender=true;skin.updateWhenOffscreen=true;}
        actorColliders=hero.GetComponentsInChildren<Collider>(true);colliderEnabled=actorColliders.Select(c=>c.enabled).ToArray();
        foreach(var c in actorColliders)c.enabled=false;
        for(int i=1;i<animator.layerCount;i++)animator.SetLayerWeight(i,0);
        animator.SetFloat("Speed",0);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=0;
        var library=animator.runtimeAnimatorController.animationClips;
        AnimationClip Find(string name)=>library.First(c=>c.name==name);
        swim=Resources.LoadAll<AnimationClip>("Encounter/v007/PLAYER_SwimFast_v007/PLAYER_SwimFast_v007").FirstOrDefault(c=>!c.name.StartsWith("__preview__")) ?? Find("SahurFastFreestyle");
        leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        Vector3 landing=board.BoardingPoint(startFeet);
        entryLandingLocal=board.transform.InverseTransformPoint(landing+Vector3.up*.04f);
        entryOutwardLocal=board.transform.InverseTransformDirection(board.OpenWaterDirection(landing));
        Vector3 edge=FindEntryEdge(fromWater ? landing : startFeet);
        Vector3 localEdge=board.transform.InverseTransformPoint(edge);
        deckOffset=new Vector2(localEdge.x*board.Dimensions.x,localEdge.z*board.Dimensions.z);FaceOpenWater(edge);
        initialHeroRotation=hero.transform.rotation;
        soleOffset=Mathf.Min(leftFoot.position.y,rightFoot.position.y)-startFeet.y;
        leftSole=leftFoot.InverseTransformVector(Vector3.down*soleOffset);rightSole=rightFoot.InverseTransformVector(Vector3.down*soleOffset);
        initialNose=shark.NoseWorldPoint;stageRotation=shark.transform.rotation;cameraStart=camera.transform.position;cameraRotation=camera.transform.rotation;cameraFov=camera.fieldOfView;
        weapon=hero.GetComponent<SahurAttack>().stickHitbox as CapsuleCollider;
        torso=actorColliders.OfType<CapsuleCollider>().FirstOrDefault(c=>c.name=="BodyHitbox_Torso");
        stick=hero.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Sahur Stick");
        if(stick!=null){held=stick.GetComponentsInChildren<Renderer>(true);heldEnabled=held.Select(r=>r.enabled).ToArray();stickMesh=stick.GetComponent<MeshFilter>()?.sharedMesh;}
        poseGraph=PlayableGraph.Create("Paired wreck encounter");poseGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer=AnimationMixerPlayable.Create(poseGraph,2);AnimationPlayableOutput.Create(poseGraph,"Sahur",animator).SetSourcePlayable(mixer);poseGraph.Play();
        if(fromWater)
        {
            Vector3 entry=board.transform.TransformPoint(entryLandingLocal)+board.transform.TransformDirection(entryOutwardLocal)*.8f;
            swimSeconds=Mathf.Max(.2f,Vector3.ProjectOnPlane(startFeet-entry,Vector3.up).magnitude/(hero.swimSpeed*hero.fastSwimMultiplier));
            entryWalkSeconds=Vector3.ProjectOnPlane(board.transform.TransformPoint(entryLandingLocal)-Deck(-.4f),Vector3.up).magnitude/3;
            LeadInSeconds=swimSeconds+1.15f+entryWalkSeconds+.4f;
        }
        else{walkSeconds=Vector3.ProjectOnPlane(startFeet-Deck(-.6f),Vector3.up).magnitude/3;LeadInSeconds=Mathf.Max(.6f,walkSeconds+.25f);}
        var definition=Resources.Load<Story1EncounterDefinition>("Encounter/v006/EncounterDefinition");
        beats=definition!=null ? definition.beats : Story1EncounterDefinition.ApprovedBeats();
        contacts=definition!=null && definition.contacts!=null && definition.contacts.Length==6 ? definition.contacts : Story1EncounterDefinition.ApprovedContacts();
        contactTimes=contacts.Select(c=>c.time).ToArray();
        AnimationClip Revised(string name)=>Resources.LoadAll<AnimationClip>("Encounter/"+name.Substring(name.LastIndexOf('_')+1)+"/"+name+"/"+name).First(c=>!c.name.StartsWith("__preview__"));
        foreach(var beat in beats)if(!revisedClips.ContainsKey(beat.clip))revisedClips.Add(beat.clip,Revised(beat.clip));
        slash=Revised("CH1_SahurSlash_v006");guard=Revised(beats.First(b=>b.action==Story1EncounterAction.Guard).clip);block=Revised(beats.First(b=>b.action==Story1EncounterAction.Brace).clip);
        throwClip=Revised("PLAYER_Throw_v006");stagger=Revised("PLAYER_Stagger_v006");knockdown=Revised("PLAYER_Fall_v006");
        climb=Revised("PLAYER_Climb_v010");walk=Revised("PLAYER_Walk_v006");
        jumpStart=Revised("PLAYER_JumpStart_v006");jumpLoop=Revised("PLAYER_JumpAir_v006");jumpLand=Revised("PLAYER_JumpLand_v006");boardJump=null;
        // The live Generic Animator includes the retained prefab bind transforms.
        // Measure its authored contact pose once; FBX SampleAnimation metadata can differ from controller evaluation.
        shark.SampleCinematicAction("Tail_Strike",TralaleroSwimAnimator.TailContactTime);
        filmTailLocal=shark.transform.InverseTransformPoint(shark.TailWorldPoint);
        shark.SampleCinematicAction("Ship_Smash",TralaleroSwimAnimator.TailContactTime);
        filmShipTailLocal=shark.transform.InverseTransformPoint(shark.TailWorldPoint);
        shark.SampleCinematicAction("Swim",0);
        var post=gameObject.AddComponent<Story1FinalePostProcess>();post.film=this;
        RenderActors();
    }
    void Animate(string name,AnimationClip clip,float start,float speed=1,float offset=0,float fade=.16f)
    {
        if(activeAnimation==name && Mathf.Approximately(poseStart,start))return;
        activeAnimation=name;
        if(previousPose.IsValid()){mixer.DisconnectInput(0);poseGraph.DestroyPlayable(previousPose);}
        if(pose.IsValid())
        {
            mixer.DisconnectInput(1);previousPose=pose;previousClip=currentClip;previousStart=poseStart;previousSpeed=poseSpeed;previousOffset=poseOffset;
            mixer.ConnectInput(0,previousPose,0);
        }
        currentClip=clip;poseStart=start;poseSpeed=speed;poseOffset=offset;blendStart=start;blendSeconds=fade;
        pose=AnimationClipPlayable.Create(poseGraph,clip);pose.SetSpeed(0);mixer.ConnectInput(1,pose,0);
    }
    Vector3 FindEntryEdge(Vector3 feet)=>FindEntryEdge(board,feet);
    Vector3 FindEntryEdge(Story1WreckPlank support,Vector3 feet,Vector3? jumpFrom=null)
    {
        Vector3 best=support.BoardingPoint(feet);float bestScore=float.NegativeInfinity;
        for(int angle=0;angle<360;angle+=15)
        {
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*support.transform.forward;
            Vector3 candidate=support.BoardingPoint(feet+direction*100);
            candidate=support.BoardingPoint(candidate-support.OpenWaterDirection(candidate)*.9f);
            if(jumpFrom.HasValue && (Vector3.ProjectOnPlane(candidate-jumpFrom.Value,Vector3.up).magnitude>12 || Mathf.Abs(candidate.y-jumpFrom.Value.y)>1.5f))continue;
            float length=Vector3.Distance(feet,candidate);
            bool safe=true;Vector3 normal=support.transform.up;if(normal.y<0)normal=-normal;
            int steps=Mathf.Max(1,Mathf.CeilToInt(length/.3f));
            for(int i=0;i<=steps && safe;i++)
            {
                Vector3 at=Vector3.Lerp(feet,candidate,(float)i/steps);float reach=Mathf.Max(2,support.Dimensions.y+1);
                safe=support.Collision.Raycast(new Ray(at+normal*reach,-normal),out var hit,reach*2) && Vector3.Dot(hit.normal,Vector3.up)>.5f;
            }
            if(!safe)continue;
            // A centre ray can hit a narrow triangle while both authored feet
            // miss the mesh. Require a real stance-sized surface around it.
            for(int bearing=0;bearing<360 && safe;bearing+=45)
            {
                Vector3 at=candidate+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*.75f;
                float reach=Mathf.Max(2,support.Dimensions.y+1);
                safe=support.Collision.Raycast(new Ray(at+normal*reach,-normal),out var stanceHit,reach*2) && Vector3.Dot(stanceHit.normal,Vector3.up)>.5f;
            }
            if(!safe)continue;
            Vector3 outward=support.OpenWaterDirection(candidate);float clearance=ApproachClearance(candidate,outward);
            float score=clearance*2-length*.08f-Mathf.Max(0,Water(candidate)+.1f-candidate.y)*30;
            if(score>bestScore){bestScore=score;best=candidate;}
        }
        return best;
    }
    float ApproachClearance(Vector3 stance,Vector3 outward)
    {
        float clearance=8;
        foreach(float step in new[]{4f,7f,11f})for(int probe=0;probe<3;probe++)
        {
            Vector3 point=stance+outward*step;
            float radius=probe==0 ? 1.8f : .85f;
            point.y=probe==0 ? Water(point)-1.4f : stance.y+(probe==1 ? 1.3f : 4);
            void Check(Story1WreckMeshCollision shape)
            {
                if(shape==null || !shape.Active || shape.WorldBounds.SqrDistance(point)>(clearance+radius)*(clearance+radius))return;
                clearance=Mathf.Min(clearance,Vector3.Distance(shape.ClosestPoint(point),point)-radius);
            }
            foreach(var other in battle.Boards)if(!other.IsBroken)Check(other.Collision);
            foreach(var other in battle.WreckDetails)if(!other.IsBroken)Check(other.Collision);
            if(clearance<-.2f)return clearance;
        }
        return clearance;
    }
    static float ClipTime(AnimationClip clip,float time)=>clip.isLooping ? Mathf.Repeat(Mathf.Max(0,time),clip.length) : Mathf.Clamp(time,0,clip.length-.00001f);
    void EvaluatePose()
    {
        pose.SetTime(ClipTime(currentClip,(Elapsed-poseStart)*poseSpeed+poseOffset));
        float weight=previousPose.IsValid() ? Ease((Elapsed-blendStart)/Mathf.Max(.01f,blendSeconds)) : 1;
        if(previousPose.IsValid())previousPose.SetTime(ClipTime(previousClip,(Elapsed-previousStart)*previousSpeed+previousOffset));
        mixer.SetInputWeight(0,1-weight);mixer.SetInputWeight(1,weight);poseGraph.Evaluate(0);
    }
    void HeroAt(Vector3 feet,bool planted=true,Vector3? facing=null)
    {
        hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(facing ?? Forward));EvaluatePose();
        if(FilmTime<0)hero.transform.rotation=Quaternion.Slerp(initialHeroRotation,hero.transform.rotation,Ease(Elapsed/(fromWater ? .25f : Mathf.Max(.1f,LeadInSeconds))));
        if(planted)
        {
            PlantFeet(feet);
        }
    }
    void PlantFeet(Vector3 deck,bool measure=true)
    {
        Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
        float Height(Vector3 point)=>Vector3.Dot(point-deck,normal)/Mathf.Max(.2f,normal.y);
        float SurfaceHeight(Vector3 sole)
        {
            float reach=Mathf.Max(2,board.Dimensions.y+1);
            return board.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2)
                ? Vector3.Dot(sole-hit.point,normal)/Mathf.Max(.2f,normal.y) : float.PositiveInfinity;
        }
        for(int pass=0;pass<3;pass++)
        {
            float a=SurfaceHeight(leftFoot.TransformPoint(leftSole)),b=SurfaceHeight(rightFoot.TransformPoint(rightSole));
            // Support the lowest authored sole. Choosing the closest sole
            // could leave the other foot buried in a tilted deck.
            float error=Mathf.Min(a,b);
            if(float.IsInfinity(error))error=Height(leftFoot.TransformPoint(leftSole));
            hero.transform.position-=Vector3.up*error;
            if(Mathf.Abs(error)<.001f)break;
        }
        float remaining=SahurSupportPose.Plant(animator,board,leftSole,rightSole);
        for(int correction=0;correction<2 && remaining>.02f && !float.IsInfinity(remaining);correction++)
        {
            float a=SurfaceHeight(leftFoot.TransformPoint(leftSole)),b=SurfaceHeight(rightFoot.TransformPoint(rightSole));
            float error=Mathf.Min(a,b);
            if(float.IsInfinity(error))break;
            hero.transform.position-=Vector3.up*Mathf.Clamp(error,-.08f,.08f);
            remaining=SahurSupportPose.Plant(animator,board,leftSole,rightSole);
        }
        bool fixedSupport=activeAction==Story1EncounterAction.Guard || activeAction==Story1EncounterAction.Parry || activeAction==Story1EncounterAction.Brace || activeAction==Story1EncounterAction.Surf;
        if(measure && FilmTime>=0 && fixedSupport && Elapsed-poseStart>.14f)
        {
            if(anchorAnimation!=activeAnimation || anchoredBoard!=board)
            {
                leftPlanted=Mathf.Abs(SurfaceHeight(leftFoot.TransformPoint(leftSole)))<=Mathf.Abs(SurfaceHeight(rightFoot.TransformPoint(rightSole)));
                Vector3 sole=(leftPlanted ? leftFoot : rightFoot).TransformPoint(leftPlanted ? leftSole : rightSole);
                float reach=Mathf.Max(2,board.Dimensions.y+1);
                Vector3 surface=board.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) ? hit.point : sole;
                // Store the actual supporting mesh, never freeze a small
                // residual IK error as the planted foot's permanent height.
                plantedSoleLocal=board.transform.InverseTransformPoint(surface);
                anchorAnimation=activeAnimation;anchoredBoard=board;
            }
            Vector3 target=board.transform.TransformPoint(plantedSoleLocal);
            float slide=SahurSupportPose.Anchor(animator,leftPlanted,leftPlanted ? leftSole : rightSole,target);
            for(int correction=0;correction<4 && slide>.003f;correction++)
            {
                // Shift weight over the planted leg before asking a bounded
                // leg solver to reach farther than the authored limb allows.
                Vector3 current=(leftPlanted ? leftFoot : rightFoot).TransformPoint(leftPlanted ? leftSole : rightSole);
                hero.transform.position+=Vector3.ClampMagnitude(target-current,.08f);
                SahurSupportPose.Plant(animator,board,leftSole,rightSole);
                slide=SahurSupportPose.Anchor(animator,leftPlanted,leftPlanted ? leftSole : rightSole,target);
            }
            LargestPlantSlide=Mathf.Max(LargestPlantSlide,slide);
            remaining=Mathf.Min(Mathf.Abs(SurfaceHeight(leftFoot.TransformPoint(leftSole))),Mathf.Abs(SurfaceHeight(rightFoot.TransformPoint(rightSole))));
        }
        if(measure && remaining>LargestFootHeightError){LargestFootHeightError=remaining;WorstFootTime=FilmTime;}
    }
    Vector3 WeaponTip
    {
        get
        {
            if(weapon==null)return GripCentre;
            Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
            Vector3 hand=animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            return (a-hand).sqrMagnitude>(b-hand).sqrMagnitude ? a : b;
        }
    }
    Vector3 PredictSlashTip(Vector3 targetFeet)
    {
        Vector3 normal=Quaternion.Inverse(Quaternion.LookRotation(Forward))*board.transform.up;
        if(slashContactMeasured && slashSampleBoard==board && Vector3.Angle(normal,slashSampleNormal)<.5f)return slashContactOffset;
        Vector3 savedPosition=hero.transform.position;Quaternion savedRotation=hero.transform.rotation;
        hero.transform.SetPositionAndRotation(targetFeet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));
        var sample=AnimationClipPlayable.Create(poseGraph,slash);sample.SetSpeed(0);sample.SetTime(.63f);
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);poseGraph.Evaluate(0);
        PlantFeet(targetFeet,false);
        // The descending tip can finish below the deck. Meet the real shaft
        // above the supporting timber; driving a nose at the buried tip asks
        // a solid shark to pass through that same board.
        Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
        float along=ReachableShaftFraction(a,b,targetFeet);
        Vector3 offset=hero.transform.InverseTransformVector(Vector3.Lerp(a,b,along)-targetFeet);
        mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);EvaluatePose();
        hero.transform.SetPositionAndRotation(savedPosition,savedRotation);
        PlantFeet(savedPosition+Vector3.up*hero.LowestFootWorldOffset,false);
        slashContactOffset=offset;slashContactMeasured=true;slashSampleBoard=board;slashSampleNormal=normal;return offset;
    }
    Vector3 ParryContactAim
    {
        get
        {
            if(weapon==null)return GripCentre;
            Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
            return Vector3.Lerp(a,b,parryShaftFraction);
        }
    }
    Vector3 PredictParryTip(Vector3 targetFeet)
    {
        Vector3 normal=Quaternion.Inverse(Quaternion.LookRotation(Forward))*board.transform.up;
        if(parryContactMeasured && parrySampleBoard==board && Vector3.Angle(normal,parrySampleNormal)<.5f)return parryContactOffset;
        Vector3 at=hero.transform.position;Quaternion rotation=hero.transform.rotation;
        var clip=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Parry).clip];
        hero.transform.SetPositionAndRotation(targetFeet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));
        var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);sample.SetTime(.484f);
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);poseGraph.Evaluate(0);
        PlantFeet(targetFeet,false);
        Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
        parryShaftFraction=ReachableShaftFraction(a,b,targetFeet);
        parryContactOffset=hero.transform.InverseTransformVector(ParryContactAim-targetFeet);
        mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);EvaluatePose();hero.transform.SetPositionAndRotation(at,rotation);
        PlantFeet(at+Vector3.up*hero.LowestFootWorldOffset,false);parryContactMeasured=true;parrySampleBoard=board;parrySampleNormal=normal;return parryContactOffset;
    }
    float ReachableShaftFraction(Vector3 a,Vector3 b,Vector3 feet)
    {
        string action=shark.CinematicAction;float seconds=shark.CinematicActionSeconds;
        shark.SampleCinematicAction("Bite_Lunge",.52f);
        Vector3 noseLocal=shark.transform.InverseTransformPoint(shark.NoseWorldPoint),bulkLocal=shark.transform.InverseTransformPoint(battle.WreckContact.BulkBodyPoint);
        Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
        var surface=OceanSurfaceSampler.Capture(ocean,shotCamera,Time.time);
        float best=float.PositiveInfinity,fraction=a.y>b.y ? 0 : 1;
        // A descending stick has many genuine contact points. Choose one
        // whose complete shark anatomy can reach it without entering wood,
        // keeping the bulk submerged; never extend the weapon's hitbox.
        for(int i=0;i<=8;i++)
        {
            float along=i/8f;Vector3 marker=Vector3.Lerp(a,b,along);
            float height=Vector3.Dot(marker-feet,normal);if(height<1.15f)continue;
            foreach(float yaw in new[]{0f,-40f,40f})foreach(float pitch in new[]{-80f,-45f,-16f,25f,65f,80f})
            {
                Quaternion facing=Quaternion.LookRotation(-Forward)*Quaternion.Euler(pitch,yaw,0);
                Vector3 bulk=marker-facing*Vector3.Scale(noseLocal-bulkLocal,shark.transform.lossyScale);
                float exposure=Mathf.Max(0,bulk.y-surface.Height(bulk)+1.4f);
                float bias=exposure*exposure*.2f+height*.0002f+Mathf.Abs(yaw)*.00005f+Mathf.Abs(pitch+16)*.00005f;
                if(bias>=best)continue;
                float cost=battle.WreckContact.PoseObstruction(marker,facing,false)+bias;
                if(cost<best){best=cost;fraction=along;}
            }
        }
        shark.SampleCinematicAction(action,seconds);return fraction;
    }
    void EnterStage(int value)
    {
        if(value==stage)return;stage=value;stageBodyStart=battle.SharkBody;stageNoseStart=shark.NoseWorldPoint;stageRotation=shark.transform.rotation;
        CurrentBeat=value==0 ? "Regain footing" : value==1 ? "Equal exchange" : value==2 ? "Sahur gains advantage" : value==3 ? "Boomerang pressure" :
            value==4 ? "Bite feint and reversal" : value==5 ? "Guard broken" : "Final counter and defeat";
        if(value==1)equalAim=PredictSlashTip(Deck(-.15f,-Mathf.Sin((2-.6f)/2.5f*Mathf.PI)*.45f));if(value==2)upperAim=PredictSlashTip(Deck(.35f));
    }
    void NoseAt(Vector3 point,Quaternion rotation)=>battle.PoseSharkNoseAt(point,rotation);
    Vector3 SurfaceNose(Vector3 at,Quaternion rotation)
    {
        float rootY=shark.SurfaceRootY(Water(at),rotation);
        Vector3 offset=rotation*Vector3.Scale(shark.transform.InverseTransformPoint(shark.NoseWorldPoint),shark.transform.lossyScale);
        at.y=rootY+offset.y;return at;
    }
    Quaternion ChooseAttackFacing(string key,Vector3 marker,bool tail,bool powerful=false,float sampledAge=0)
    {
        bool same=attackFacingKey==key;
        if(same && (FilmTime-attackFacingSampleTime<.12f || sampledAge>(tail ? .6f : .48f)))return attackFacing;
        string sampledAction=shark.CinematicAction;float sampledSeconds=shark.CinematicActionSeconds;
        string action=tail ? powerful ? "Ship_Smash" : "Tail_Strike" : "Bite_Lunge";
        shark.SampleCinematicAction(action,tail ? .65f : .52f);
        float previousCost=same ? battle.WreckContact.PoseObstruction(marker,attackFacing,tail) : float.PositiveInfinity;
        Vector3 localMarker=shark.transform.InverseTransformPoint(tail ? shark.TailWorldPoint : shark.NoseWorldPoint);
        Vector3 localBulk=shark.transform.InverseTransformPoint(battle.WreckContact.BulkBodyPoint);
        float RouteObstruction(Quaternion rotation,float endpointCost)
        {
            float cost=endpointCost;
            // A clear endpoint is insufficient when the approach cuts a
            // girder or the occupied deck. Score actual intervening anatomy
            // as well, before selecting the wind-up's facing.
            foreach(float phase in new[]{.35f,.7f})
            {
                // Test the anatomy in its actual winding-up pose. Reusing
                // the fully coiled contact pose for the whole approach hid
                // collisions made by the earlier straight spine and tail.
                float actionSeconds=tail ? phase*.65f : key=="Probing bite" ? .036f+phase*.484f : Mathf.Max(0,phase*.63f-.11f);
                shark.SampleCinematicAction(action,actionSeconds);
                Quaternion facing=Quaternion.Slerp(stageRotation,rotation,phase);
                Vector3 at;
                if(tail)
                {
                    Vector3 end=marker-rotation*Vector3.Scale(localMarker-shark.RootMeshBounds.center,shark.transform.lossyScale);
                    Vector3 body=Vector3.Lerp(stageBodyStart,end,phase);
                    at=body+facing*Vector3.Scale(localMarker-shark.RootMeshBounds.center,shark.transform.lossyScale);
                }
                else at=Vector3.Lerp(key=="Probing bite" ? Deck()+Forward*6+Vector3.up*Mathf.Max(0,marker.y-Deck().y) : stageNoseStart,marker,phase);
                cost+=battle.WreckContact.PoseObstruction(at,facing,tail)*.6f;
            }
            shark.SampleCinematicAction(action,tail ? .65f : .52f);
            return cost;
        }
        var waterSnapshot=OceanSurfaceSampler.Capture(ocean,shotCamera,Time.time);
        float Exposure(Quaternion rotation)
        {
            Vector3 bulk=marker-rotation*Vector3.Scale(localMarker-localBulk,shark.transform.lossyScale);
            return Mathf.Max(0,bulk.y-(ocean!=null ? waterSnapshot.Height(bulk) : hero.seaLevel)+1.4f);
        }
        if(same && previousCost<.0004f && Exposure(attackFacing)<.2f){shark.SampleCinematicAction(sampledAction,sampledSeconds);attackFacingSampleTime=FilmTime;return attackFacing;}
        attackFacingKey=key;attackFacingSampleTime=FilmTime;float best=float.PositiveInfinity;Quaternion chosen=Quaternion.LookRotation(-Forward)*Quaternion.Euler(-16,0,0);
        foreach(int side in tail ? new[]{1,-1} : new[]{1})foreach(float yaw in new[]{0f,-20f,20f,-40f,40f,-65f,65f})foreach(float pitch in new[]{-80f,-65f,-45f,-25f,-16f,0f,12f,25f,45f,65f,80f})
        {
            Quaternion facing=tail ? Quaternion.LookRotation((Side*side+Forward*.75f).normalized)*Quaternion.Euler(pitch,yaw,powerful ? -4 : 0) : Quaternion.LookRotation(-Forward)*Quaternion.Euler(pitch,yaw,0);
            float exposure=Exposure(facing);
            float endpointCost=battle.WreckContact.PoseObstruction(marker,facing,tail);
            float bias=exposure*exposure*.2f+Mathf.Abs(yaw)*.00005f+Mathf.Abs(pitch+16)*.00005f;
            if(endpointCost+bias>=best)continue;
            float cost=RouteObstruction(facing,endpointCost)+bias;
            if(cost<best){best=cost;chosen=facing;}
            if(cost<.0015f && exposure<.05f)goto FacingChosen;
        }
        FacingChosen:
        // Reach planning samples the contact pose temporarily. Restore the
        // timeline's actual recoil/recovery instead of replacing it with
        // another bite every time the approach is evaluated.
        shark.SampleCinematicAction(sampledAction,sampledSeconds);
        attackFacing=same ? Quaternion.RotateTowards(attackFacing,chosen,30) : chosen;return attackFacing;
    }
    void CounterApproach(float age,float hit,Vector3 aim,float strength)
    {
        Quaternion facing=ChooseAttackFacing(FilmTime<4 ? "First counter" : "Landing counter",aim,false,false,age-.11f);
        float approach=Ease(age/hit),retreat=Ease((age-hit)/.65f);
        Vector3 nose=Vector3.Lerp(stageNoseStart,aim+Forward*(retreat*strength),approach);
        Quaternion rotation=Quaternion.Slerp(stageRotation,facing,approach);
        if(age>hit+.15f)nose.y=Mathf.Lerp(nose.y,SurfaceNose(nose,rotation).y,retreat*.8f);
        NoseAt(nose,rotation);
    }
    void TailApproach(float age,Vector3 target,bool powerful)
    {
        Quaternion facing=ChooseAttackFacing(powerful ? "Defeating tail" : "Reversal tail",target,true,powerful,age);
        float advance=Ease(age/TralaleroSwimAnimator.TailContactTime),retreat=Ease((age-.74f)/.9f);
        Vector3 local=powerful ? filmShipTailLocal : filmTailLocal;
        Vector3 end=target-facing*Vector3.Scale(local-shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 body=Vector3.Lerp(stageBodyStart,end,advance)-Side*(retreat*4);
        Quaternion rotation=Quaternion.Slerp(stageRotation,facing,advance);
        if(retreat>0)
        {
            float surface=shark.SurfaceRootY(Water(body),rotation)+(rotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale)).y;
            body.y=Mathf.Lerp(body.y,surface,retreat);
        }
        battle.PoseCinematicShark(body,rotation,absoluteHeight:true);
    }
    void RenderActors(float sampledChoreoTime=-1)
    {
        float t=sampledChoreoTime>=0 ? sampledChoreoTime : FilmTime;
        if(t<0)
        {
            if(fromWater)
            {
                bool pulling=Elapsed>=swimSeconds;float pullAge=Elapsed-swimSeconds;
                bool walking=pullAge>=1.15f && pullAge<1.15f+entryWalkSeconds,turning=pullAge>=1.15f+entryWalkSeconds;
                Animate(turning ? "Entry turn" : walking ? "Entry clear-water walk" : pulling ? "Entry climb" : "Entry swim",turning ? guard : walking ? walk : pulling ? climb : swim,turning ? swimSeconds+1.15f+entryWalkSeconds : walking ? swimSeconds+1.15f : pulling ? swimSeconds : 0,walking ? 3/3.59f : pulling && !turning ? climb.length/1.15f : 1);
                Vector3 landing=board.transform.TransformPoint(entryLandingLocal),outward=Vector3.ProjectOnPlane(board.transform.TransformDirection(entryOutwardLocal),Vector3.up).normalized;
                Vector3 entry=landing+outward*.8f;entry.y=Water(entry)-2;
                if(pulling && !entryPullStarted)
                {
                    entryPullStarted=true;entryPullStartLocal=board.transform.InverseTransformPoint(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset);
                    Vector3 grip=landing+outward*.45f,normal=board.transform.up;if(normal.y<0)normal=-normal;
                    if(board.Collision.Raycast(new Ray(grip+normal*4,-normal),out var hit,8))grip=hit.point;
                    Vector3 across=Vector3.Cross(Vector3.up,-outward);
                    entryLeftGrip=board.transform.InverseTransformPoint(grip-across*.35f);entryRightGrip=board.transform.InverseTransformPoint(grip+across*.35f);
                }
                Vector3 entryFeet=Vector3.Lerp(startFeet,entry,Ease(Elapsed/swimSeconds));
                if(pulling)
                {
                    Vector3 start=board.transform.TransformPoint(entryPullStartLocal),end=landing;
                    float phase=Mathf.Clamp01(pullAge/1.15f);
                    entryFeet=Vector3.Lerp(start,end,Ease(Mathf.InverseLerp(.38f,1,phase)));
                    entryFeet.y=Mathf.Lerp(start.y,end.y,Ease(phase/.65f));
                }
                if(walking || turning)entryFeet=Vector3.Lerp(landing,Deck(-.4f),Mathf.Clamp01((pullAge-1.15f)/Mathf.Max(.01f,entryWalkSeconds)));
                Vector3 walkHeading=Vector3.ProjectOnPlane(Deck(-.4f)-landing,Vector3.up).normalized;if(walkHeading.sqrMagnitude<.01f)walkHeading=-outward;
                Vector3 heading=turning ? Quaternion.Slerp(Quaternion.LookRotation(walkHeading),Quaternion.LookRotation(Forward),Ease((pullAge-1.15f-entryWalkSeconds)/.4f))*Vector3.forward : walking ? Quaternion.Slerp(Quaternion.LookRotation(-outward),Quaternion.LookRotation(walkHeading),Ease((pullAge-1.15f)/.2f))*Vector3.forward : -outward;
                HeroAt(entryFeet,pulling && pullAge>=1.15f,heading);
                if(!pulling)
                {
                    Vector3 breathing=animator.GetBoneTransform(HumanBodyBones.Head).position;
                    hero.transform.position+=Vector3.up*(Water(breathing)+hero.swimHeadFreeboard-breathing.y);
                }
                else if(!turning && !walking)
                {
                    float phase=pullAge/1.15f,weight=Ease(phase/.12f)*(1-Ease(Mathf.InverseLerp(.62f,.84f,phase)));
                    SahurSupportPose.Hand(animator,true,board.transform.TransformPoint(entryLeftGrip),weight);
                    SahurSupportPose.Hand(animator,false,board.transform.TransformPoint(entryRightGrip),weight);
                }
                if(swimmingWeapon!=null){swimmingWeapon.CinematicTraversal=!turning && !walking;swimmingWeapon.RefreshPose();}
            }
            else
            {
                Animate(walkSeconds>.1f && Elapsed<walkSeconds ? "Entry walk" : "Entry guard",walkSeconds>.1f && Elapsed<walkSeconds ? walk : guard,0);
                HeroAt(Vector3.Lerp(startFeet,Deck(-.4f),Ease(Elapsed/LeadInSeconds)));
            }
            shark.SampleCinematicAction("Swim",Elapsed);
            NoseAt(Vector3.Lerp(initialNose,SurfaceNose(Deck()+Forward*6,Quaternion.LookRotation(-Forward)),Ease(Elapsed/LeadInSeconds)),Quaternion.Slerp(stageRotation,Quaternion.LookRotation(-Forward),Ease(Elapsed/LeadInSeconds)));
            CurrentBeat="Regain footing";return;
        }
        if(swimmingWeapon!=null && swimmingWeapon.CinematicTraversal){swimmingWeapon.CinematicTraversal=false;swimmingWeapon.RefreshPose();}
        var beat=beats.Last(b=>b.start<=t);CurrentBeat=beat.label;
        if(defeatContactTime>=0)
        {
            var fallBeat=beats.First(b=>b.action==Story1EncounterAction.Fall);
            beat=new Story1EncounterBeat(fallBeat.label,defeatContactTime,Duration,Story1EncounterAction.Fall,fallBeat.clip);
            CurrentBeat=beat.label;
        }
        // The timeline describes intended exchanges. A missed tail cannot
        // cause a stagger or defeat merely because its scheduled time passed.
        if(beat.action==Story1EncounterAction.Stagger && !verifiedContacts.Contains(4))
            beat=new Story1EncounterBeat("Guard after missed tail",beat.start,beat.end,Story1EncounterAction.Guard,beats.First(b=>b.action==Story1EncounterAction.Guard).clip);
        if(beat.action==Story1EncounterAction.Fall && defeatContactTime<0)
            beat=new Story1EncounterBeat("Hold the final guard",17.4f,20.8f,Story1EncounterAction.Brace,beats.First(b=>b.action==Story1EncounterAction.Brace).clip);
        activeAction=beat.action;
        float age=t-beat.start;
        if(beat.action==Story1EncounterAction.Hop)
        {
            RenderTraversal(age,beat.start>10);return;
        }
        int group=t<4 ? 1 : t<10 ? 2 : t<16 ? 3 : 4;
        if(stageBeat!=beat.label){stageBeat=beat.label;stageNoseStart=shark.NoseWorldPoint;stageBodyStart=battle.SharkBody;stageRotation=shark.transform.rotation;}
        stage=group;
        var clip=revisedClips[beat.clip];
        Animate(beat.label,clip,Clock(beat.action==Story1EncounterAction.Fall ? defeatContactTime : beat.start),beat.action==Story1EncounterAction.Fall ? clip.length/2.2f : 1,0,beat.action==Story1EncounterAction.Stagger ? .06f : .12f);
        Vector3 feet=Deck(t<4 ? -.4f : t<14 ? .15f : -.3f,t<1.8f ? -Mathf.Sin(Ease(t/1.8f)*Mathf.PI)*.3f : 0);
        if(beat.action==Story1EncounterAction.Fall)
        {
            float fall=t-defeatContactTime;HeroAt(fallStart+fallVelocity*fall+Vector3.up*(fall*2.4f-fall*fall*4.9f),false);
            if(fall>.65f && !floating)
            {
                Vector3 headAt=animator.GetBoneTransform(HumanBodyBones.Head).position,chestAt=animator.GetBoneTransform(HumanBodyBones.Chest).position;
                float lift=Mathf.Max(Water(headAt)+.12f-headAt.y,Water(chestAt)+.04f-chestAt.y);
                if(lift>0){hero.transform.position+=Vector3.up*lift;floating=true;floatingChest=animator.GetBoneTransform(HumanBodyBones.Chest).position;floatHeading=floatRotation=hero.transform.rotation;floatDrift=Vector3.ProjectOnPlane(fallVelocity,Vector3.up).normalized*.35f;}
            }
        }
        else HeroAt(feet);
        if(t<4)
        {
            if(t<1.8f)
            {
                shark.SampleCinematicAction("Bite_Lunge",Mathf.Max(0,t-.664f));
                Vector3 target=ParryContactAim;float u=Ease(t/1.184f),retreat=Ease((t-1.184f)/.6f);
                Vector3 futureFeet=Deck(-.4f,-Mathf.Sin(Ease(1.184f/1.8f)*Mathf.PI)*.3f);
                Vector3 futureTip=futureFeet+hero.transform.TransformVector(PredictParryTip(futureFeet));
                Quaternion facing=ChooseAttackFacing("Probing bite",futureTip,false,false,t-.664f);
                if(t<.7f)
                {
                    probeRaisedNose=Deck()+Forward*6;probeRaisedNose.y=futureTip.y;
                    NoseAt(Vector3.Lerp(stageNoseStart,probeRaisedNose,Ease(t/.7f)),Quaternion.Slerp(stageRotation,facing,Ease(t/.45f)));
                }
                else NoseAt(Vector3.Lerp(probeRaisedNose,target+Forward*(retreat*3),Ease((t-.7f)/.484f)),Quaternion.Slerp(stageRotation,facing,u));
            }
            else
            {
                shark.SampleCinematicAction(t<2.43f ? "Bite_Lunge" : verifiedContacts.Contains(1) ? "Hit_Recoil" : "Swim",t<2.43f ? t-1.91f : t-2.43f);
                Vector3 target=Deck(-.4f)+hero.transform.TransformVector(PredictSlashTip(Deck(-.4f)));
                CounterApproach(t-1.8f,.63f,target,3);
            }
        }
        else if(t<7.8f)
        {
            shark.SampleCinematicAction(t<6.4f ? "Swim" : t<7.03f ? "Bite_Lunge" : verifiedContacts.Contains(2) ? "Hit_Recoil" : "Swim",t<6.4f ? age : t<7.03f ? Mathf.Max(0,t-6.51f) : t-7.03f);
            if(t<6.4f){board.Drive(.35f,-.2f,hero.transform);NoseAt(SurfaceNose(Deck()+Forward*6-Side*2,Quaternion.LookRotation(-Forward)),Quaternion.LookRotation(-Forward));}
            else{board.Drive(0,0,null);CounterApproach(t-6.4f,.63f,Deck(.15f)+hero.transform.TransformVector(PredictSlashTip(Deck(.15f))),4);}
        }
        else if(t<10)
        {
            shark.SampleCinematicAction(t<8.94f || !verifiedContacts.Contains(3) ? "Swim" : "Hit_Recoil",t<8.94f ? age : t-8.94f);
            Vector3 nose=SurfaceNose(Deck()+Forward*(6+Ease((t-8.94f)/.8f)*3),Quaternion.LookRotation(-Forward));NoseAt(nose,Quaternion.LookRotation(-Forward));
            if(projectile!=null && !HasLostStick)
            {
                Vector3 target=t<8.94f ? shark.NoseWorldPoint : throwTarget;
                Vector3 center=t<8.94f ? Vector3.Lerp(throwStart,target,Ease((t-8.32f)/.62f)) : Vector3.Lerp(target,GripCentre,Ease((t-8.94f)/.86f));
                SpinProjectile(center,t-8.32f);
            }
        }
        else if(t<11.6f)
        {
            shark.SampleCinematicAction("Swim",age);
            Vector3 at=Deck()+Forward*Mathf.Lerp(8,3,Ease(age/1.6f))+Side*Mathf.Lerp(0,7,Ease(age/1.6f));
            at=SurfaceNose(at,Quaternion.LookRotation(-Side));at.y-=Mathf.Sin(age/1.6f*Mathf.PI)*2;
            NoseAt(Vector3.Lerp(stageNoseStart,at,Ease(age/.5f)),Quaternion.Slerp(stageRotation,Quaternion.LookRotation(-Side),Ease(age/1.6f)));
        }
        else if(t<16)
        {
            shark.SampleCinematicAction("Tail_Strike",Mathf.Max(0,t-13.922f));
            Vector3 chest=TailContactTarget;
            TailApproach(t-13.922f,t<=14.572f ? chest : board.transform.TransformPoint(tailAnchor),false);
        }
        else
        {
            if(t<17.4f)
            {
                shark.SampleCinematicAction("Swim",age);
                Vector3 nose=SurfaceNose(Deck()+Forward*8+Side*3,Quaternion.LookRotation(-Side));
                NoseAt(Vector3.Lerp(stageNoseStart,nose,Ease(age/1.4f)),Quaternion.Slerp(stageRotation,Quaternion.LookRotation(-Side),Ease(age/1.4f)));
            }
            else
            {
                shark.SampleCinematicAction("Ship_Smash",defeatContactTime<0 && t>=18.05f ? .65f : t-17.4f);
                Vector3 chest=TailContactTarget;
                TailApproach(defeatContactTime<0 && t>=18.05f ? .65f : t-17.4f,defeatContactTime<0 ? chest : board.transform.TransformPoint(tailAnchor),true);
            }
            if(HasLostStick && projectile!=null)
            {
                float flight=Mathf.Max(0,t-stickLossTime);Vector3 center=lostStart+lostVelocity*flight+Vector3.down*(4.9f*flight*flight);center.y=Mathf.Max(center.y,Water(center)+.05f);SpinProjectile(center,flight);
            }
        }
    }
    void BeginHop(int index)
    {
        hopIndex=index;hopFrom=board;hopLanded=false;
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        hopStartLocal=board.transform.InverseTransformPoint(feet);
        var neighbours=FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None)
            .Where(p=>p!=board && p.IsBoardable && p.Dimensions.y<5 && p.Speed<5 && Mathf.Min(p.Dimensions.x,p.Dimensions.z)>1.6f)
            .Where(p=>Vector3.ProjectOnPlane(p.BoardingPoint(feet)-feet,Vector3.up).magnitude<12 && Mathf.Abs(p.BoardingPoint(feet).y-feet.y)<1.5f)
            .OrderBy(p=>Vector3.ProjectOnPlane(p.BoardingPoint(feet)-feet,Vector3.up).magnitude+Mathf.Abs(p.BoardingPoint(feet).y-feet.y)*.6f)
            .Take(6).ToArray();
        hopTarget=null;
        foreach(var candidate in neighbours)
        {
            Vector3 candidateLanding=candidate.BoardingPoint(feet),edge=FindEntryEdge(candidate,candidateLanding,feet);
            bool stance=true;Vector3 normal=candidate.transform.up;if(normal.y<0)normal=-normal;
            float reach=Mathf.Max(2,candidate.Dimensions.y+1);
            for(int bearing=0;bearing<360 && stance;bearing+=45)
            {
                Vector3 sole=edge+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*.75f;
                stance=candidate.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) && Vector3.Dot(hit.normal,normal)>.5f;
            }
            if(!stance)continue;
            // A landing is not safe merely because a foot fits: leave actual
            // space for the approaching head and submerged body. Crowded
            // neighbouring wreck uses the approved equally timed same-board
            // variant, rather than landing in an impossible contact corridor.
            if(ApproachClearance(edge,candidate.OpenWaterDirection(edge))<.15f)continue;
            hopTarget=candidate;break;
        }
        if(hopTarget==null)hopTarget=board;
        // Land on a reachable, real edge facing clear water. The nearest
        // interior point often left a whole hull between the two combatants.
        Vector3 landing=FindEntryEdge(hopTarget,hopTarget.BoardingPoint(feet),feet);
        Vector3 takeoff=board.BoardingPoint(landing);
        hopTakeoffLocal=board.transform.InverseTransformPoint(takeoff);
        hopLandingLocal=hopTarget.transform.InverseTransformPoint(landing);
        hopFacing=hero.transform.rotation;
    }
    void LandHop()
    {
        if(hopLanded)return;hopLanded=true;
        if(hopTarget!=board)
        {
            if(supportBody!=null)supportBody.angularDamping=originalDamping;
            board=hopTarget;supportBody=board.GetComponent<Rigidbody>();originalDamping=supportBody.angularDamping;supportBody.angularDamping=6;
            board.Drive(0,0,null);BoardJumpCount++;
        }
        deckOffset=new Vector2(hopLandingLocal.x*board.Dimensions.x,hopLandingLocal.z*board.Dimensions.z);
        FaceOpenWater(board.transform.TransformPoint(hopLandingLocal));
    }
    void RenderTraversal(float age,bool reversal)
    {
        int section=reversal ? 1 : 0;
        float duration=reversal ? 2.4f : 1.85f;
        int index=reversal ? 1 : 0;
        float localAge=age;
        float start=Elapsed-localAge;
        if(hopIndex!=index)BeginHop(index);
        if(breachSection!=section)
        {
            breachSection=section;if(reversal)SharkBreachCount++;
            breachLiftSpray=breachEntrySpray=false;
            breachStart=shark.NoseWorldPoint;breachRotation=shark.transform.rotation;
            float side=hopFrom.transform.InverseTransformPoint(hopTarget.transform.position).x>=0 ? -1 : 1;
            float clearance=Mathf.Max(4,shark.RootMeshBounds.extents.x*Mathf.Abs(shark.transform.lossyScale.x)*.7f+1.5f);
            // Breach beside the abandoned board, leaving a readable escape lane.
            breachTarget=hopFrom.transform.TransformPoint(new Vector3(side*(.5f+clearance/hopFrom.Dimensions.x),hopFrom.TopLocalY,hopTakeoffLocal.z));
            breachDirection=Vector3.ProjectOnPlane(breachTarget-breachStart,Vector3.up).normalized;
            if(breachDirection.sqrMagnitude<.1f)breachDirection=-Forward;
        }
        float takeoffTime=duration*.2f,landTime=duration*.77f;
        Vector3 departure=hopFrom.BoardingPoint(hopFrom.transform.TransformPoint(hopTakeoffLocal))+Vector3.up*.04f;
        Vector3 landing=hopTarget.BoardingPoint(hopTarget.transform.TransformPoint(hopLandingLocal))+Vector3.up*.04f;
        if(hopTarget==hopFrom)
        {
            // No safe neighbour: keep a supported duck, never manufacture a target.
            Animate("Duck "+index,block,start,block.length/duration,0,.12f);
            HeroAt(hopFrom.transform.TransformPoint(hopStartLocal));
        }
        else if(localAge<takeoffTime)
        {
            Animate("Jump prepare "+index,boardJump!=null ? boardJump : jumpStart,start,
                (boardJump!=null ? .5f : jumpStart.length)/takeoffTime,0,.1f);
            Vector3 initial=hopFrom.BoardingPoint(hopFrom.transform.TransformPoint(hopStartLocal))+Vector3.up*.04f;
            HeroAt(Vector3.Lerp(initial,departure,Ease(localAge/takeoffTime)));
        }
        else if(localAge<landTime)
        {
            Animate("Jump airborne "+index,boardJump!=null ? boardJump : jumpLoop,start+takeoffTime,
                boardJump!=null ? .83f/(landTime-takeoffTime) : .7f,boardJump!=null ? .5f : 0,.09f);
            float u=Mathf.Clamp01((localAge-takeoffTime)/(landTime-takeoffTime));
            float gap=Vector3.ProjectOnPlane(landing-departure,Vector3.up).magnitude;
            Vector3 feet=Vector3.Lerp(departure,landing,u)+Vector3.up*(4*u*(1-u)*Mathf.Clamp(1.8f+gap*.06f,1.8f,3));
            HeroAt(feet,false);
            Quaternion facing=Quaternion.Slerp(Quaternion.LookRotation(Vector3.ProjectOnPlane(landing-departure,Vector3.up)),
                Quaternion.LookRotation(hopTarget.OpenWaterDirection(landing)),Ease((u-.7f)/.3f));
            hero.transform.rotation=Quaternion.Slerp(hopFacing,facing,Ease(u/.4f));
        }
        else
        {
            LandHop();
            Animate("Jump land "+index,boardJump!=null ? boardJump : jumpLand,start+landTime,
                (boardJump!=null ? boardJump.length-1.33f : jumpLand.length)/(duration-landTime),boardJump!=null ? 1.33f : 0,.055f);
            HeroAt(landing);
            float error=SahurSupportPose.Plant(animator,board,leftSole,rightSole);
            LargestLandingError=Mathf.Max(LargestLandingError,error);
        }
        CurrentBeat=reversal ? "Leap away from breaching shark" : index==0 ? "Jump to evade the breach" : "Second board landing and counter";
        if(reversal)RenderBreach(age*(3.2f/2.4f));
        else{shark.SampleCinematicAction("Bite_Lunge",age);NoseAt(SurfaceNose(departure+Forward*6,Quaternion.LookRotation(-Forward)),Quaternion.LookRotation(-Forward));}
    }
    void RenderBreach(float age)
    {
        shark.SampleCinematicAction("Breach",age);
        Quaternion facing=Quaternion.LookRotation(breachDirection);
        Vector3 exit=breachTarget+breachDirection*10;
        Vector3 nose;Quaternion rotation;
        if(age<.5f)
        {
            rotation=Quaternion.Slerp(breachRotation,facing*Quaternion.Euler(-20,0,0),Ease(age/.5f));
            nose=Vector3.Lerp(breachStart,SurfaceNose(breachStart,rotation),Ease(age/.5f));
        }
        else if(age<2.7f)
        {
            float u=(age-.5f)/2.2f;
            rotation=facing*Quaternion.Euler(Mathf.Lerp(-20,28,u),0,Mathf.Sin(u*Mathf.PI)*-6);
            nose=Vector3.Lerp(breachStart,exit,u);
            float height=Mathf.Clamp(shark.RootMeshBounds.size.y*Mathf.Abs(shark.transform.lossyScale.y)*.85f,5,12);
            nose.y=SurfaceNose(nose,rotation).y+Mathf.Sin(u*Mathf.PI)*height;
        }
        else
        {
            rotation=Quaternion.Slerp(facing*Quaternion.Euler(28,0,0),facing,Ease((age-2.7f)/.5f));
            nose=SurfaceNose(exit,rotation);
        }
        NoseAt(nose,rotation);
        LargestBreachClearance=Mathf.Max(LargestBreachClearance,shark.NoseWorldPoint.y-Water(shark.NoseWorldPoint));
        if((age>=.5f && !breachLiftSpray)||(age>=2.65f && !breachEntrySpray))
        {
            if(!breachLiftSpray)breachLiftSpray=true;else breachEntrySpray=true;
            Vector3 spray=shark.NoseWorldPoint;spray.y=Water(spray)+.05f;
            battle.Effects.Impact(spray,breachDirection*.5f,1.2f);
        }
    }
    void Cue(float t)
    {
        if(t==8.32f){CreateProjectile();throwStart=GripCentre;}
        else if(t==8.94f)throwTarget=shark.NoseWorldPoint;
        else if(t==9.8f)CatchStick();
        else if(t==14.572f)tailAnchor=board.transform.InverseTransformPoint(TailContactTarget);
        else if(t==18.05f && defeatContactTime<0)tailAnchor=board.transform.InverseTransformPoint(TailContactTarget);
    }
    void LoseStick()
    {
        if(HasLostStick)return;
        CreateProjectile();lostStart=GripCentre;lostVelocity=Side*4-Forward*1.6f+Vector3.up*3;stickLossTime=FilmTime;HasLostStick=true;
    }
    void VerifyFinalContact()
    {
        float t=FilmTime;
        for(int i=0;i<contactTimes.Length;i++)
        {
            if(verifiedContacts.Contains(i) || Mathf.Abs(t-contactTimes[i])>Mathf.Max(.01f,contacts[i].window))continue;
            bool counter=i<4;
            Vector3 a=counter ? i==3 && projectile!=null ? projectile.TransformPoint(stickMesh.bounds.center) : WeaponTip : animator.GetBoneTransform(HumanBodyBones.Chest).position;
            Vector3 b=counter ? shark.NoseWorldPoint : shark.TailWorldPoint;
            Vector3 contactPoint=(a+b)*.5f;
            // Relative swept markers allow a fast real crossing between rendered samples.
            Vector3 relative=a-b,previous=lastContactA[i]-lastContactB[i];
            float gap=relative.magnitude;
            if(counter && battle.WreckContact.HeadVolume!=null && (i==3 ? projectileShape : weapon)!=null)
                gap=Story1CapsuleContact.Gap(i==3 ? projectileShape : weapon,battle.WreckContact.HeadVolume,out contactPoint);
            else if(!counter && torso!=null)
                gap=Story1CapsuleContact.PointGap(b,torso,out contactPoint);
            if(lastContactTime[i]>0 && t-lastContactTime[i]<=.12f && Vector3.Distance(a,lastContactA[i])<3 && Vector3.Distance(b,lastContactB[i])<3)
            {
                Vector3 step=relative-previous;float u=step.sqrMagnitude>.000001f ? Mathf.Clamp01(-Vector3.Dot(previous,step)/step.sqrMagnitude) : 0;
                gap=Mathf.Min(gap,(previous+step*u).magnitude);
            }
            lastContactA[i]=a;lastContactB[i]=b;lastContactTime[i]=t;
            if(gap<ContactAttemptGaps[i])ContactObstructions[i]="correction="+battle.WreckContact.LastCorrection+" hero="+a+" shark="+b+" obstacles="+string.Join(";",battle.WreckContact.LastObstacles);
            ContactAttemptGaps[i]=Mathf.Min(ContactAttemptGaps[i],gap);
            if(gap>.05f)continue;
            verifiedContacts.Add(i);ContactGaps.Add(gap);LargestContactGap=Mathf.Max(LargestContactGap,gap);
            if(counter){HeroCounterCount++;if(i==3)throwTarget=b;}
            else{SharkCounterCount++;tailAnchor=board.transform.InverseTransformPoint(b);}
            if(i==5)
            {
                defeatContactTime=t;
                fallStart=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
                fallVelocity=FindOpenWaterVelocity();
            }
            battle.CinematicImpact(contactPoint,counter,i==2 || i==5);
        }
    }
    void Advance()
    {
        if(battle==null || IsComplete || PauseSettingsMenu.IsOpen || Time.timeScale<=0 || Time.deltaTime<=0)return;
        float old=Elapsed,end=Elapsed+Time.deltaTime;
        foreach(float cue in Cues.Concat(contactTimes).Distinct().OrderBy(c=>c))
        {
            float at=Clock(cue);if(at<=old || at>end)continue;Elapsed=at;
            // Capture the supported pose before the fall starts consuming its launch anchor.
            if(cue==18.05f){RenderActors(cue-.0001f);Cue(cue);RenderActors(cue);}
            else{RenderActors(cue);Cue(cue);}
            // Evaluate the authored contact through solid collision even when a render frame crosses the cue.
            battle.WreckContact.ResolvePose();VerifyFinalContact();
        }
        Elapsed=end;RenderActors();
        if(defeatContactTime>=0 && FilmTime>=defeatContactTime+.17f)LoseStick();
        if(FilmTime>18.05f+contacts[5].window && defeatContactTime<0)
        {
            IsComplete=true;battle.ResumeAfterMissedFinale(this);return;
        }
        if(FilmTime>=Duration && defeatContactTime>=0){IsComplete=true;CurrentBeat="Defeated";battle.CompleteFinale();}
    }
    Vector3 FindOpenWaterVelocity()
    {
        var decks=FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None);
        Vector3 chosen=-Side*7-Forward*2;float best=float.NegativeInfinity;
        for(int radius=6;radius<=18;radius+=3)for(int angle=-70;angle<=70;angle+=20)
        {
            Vector3 offset=Quaternion.AngleAxis(angle,Vector3.up)*(-Side*radius);
            float clearance=decks.Min(p=>p.EdgeDistance(fallStart+offset));
            float score=Mathf.Min(clearance,3)-radius*.025f;
            if(score>best){best=score;chosen=offset;}
        }
        return chosen/1.25f;
    }
    void FixedUpdate()
    {
        if(supportBody==null || supportBody.isKinematic || IsComplete || FilmTime>=17.4f)return;
        // Stabilize around the filtered swell normal. Forcing world-up kept
        // long deck edges submerged while a crest rose beneath the actors.
        Vector3 up=supportBody.rotation*Vector3.up;if(up.y<0)up=-up;
        var floatingBody=supportBody.GetComponent<Story1WreckFloatBody>();
        Vector3 normal=floatingBody!=null ? floatingBody.SurfaceNormal : Vector3.up;
        supportBody.AddTorque((Vector3.Cross(up,normal)*15-Vector3.ProjectOnPlane(supportBody.angularVelocity,Vector3.up)*3),ForceMode.Acceleration);
    }
    Vector3 GripCentre=>stick!=null && stickMesh!=null ? stick.TransformPoint(stickMesh.bounds.center) : animator.GetBoneTransform(HumanBodyBones.RightHand).position;
    Vector3 TorsoPoint=>torso!=null ? torso.transform.TransformPoint(torso.center) : animator.GetBoneTransform(HumanBodyBones.Chest).position;
    Vector3 TailContactTarget
    {
        get
        {
            if(torso==null)return TorsoPoint;
            Story1CapsuleContact.Segment(torso,out var a,out var b,out float radius);
            // Strike the upper chest inside the actual hurtbox, leaving the
            // tail's thickness clear of the support mesh underneath it.
            return (a.y>b.y ? a : b)+Vector3.up*(radius*.85f);
        }
    }
    void CreateProjectile()
    {
        CatchStick();if(stick==null || stickMesh==null)return;
        var go=new GameObject("Cinematic Sahur stick",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetPositionAndRotation(stick.position,stick.rotation);go.transform.localScale=stick.lossyScale;
        go.GetComponent<MeshFilter>().sharedMesh=stickMesh;go.GetComponent<MeshRenderer>().sharedMaterials=stick.GetComponent<Renderer>().sharedMaterials;
        projectile=go.transform;projectileRotation=stick.rotation;foreach(var r in held)r.enabled=false;
        if(weapon!=null)
        {
            var shape=new GameObject("Actual thrown weapon shape");shape.transform.SetParent(projectile,false);
            shape.transform.localPosition=stick.InverseTransformPoint(weapon.transform.position);
            shape.transform.localRotation=Quaternion.Inverse(stick.rotation)*weapon.transform.rotation;
            Vector3 ws=weapon.transform.lossyScale,ss=stick.lossyScale;
            shape.transform.localScale=new Vector3(ws.x/ss.x,ws.y/ss.y,ws.z/ss.z);
            projectileShape=shape.AddComponent<CapsuleCollider>();projectileShape.direction=weapon.direction;
            projectileShape.center=weapon.center;projectileShape.radius=weapon.radius;projectileShape.height=weapon.height;
            projectileShape.enabled=false;
        }
    }
    void SpinProjectile(Vector3 centre,float age){projectile.rotation=Quaternion.AngleAxis(age*620,Side)*projectileRotation;projectile.position=centre-projectile.TransformVector(stickMesh.bounds.center);}
    void CatchStick(){if(projectile!=null)Destroy(projectile.gameObject);projectile=null;if(held!=null && !HasLostStick)for(int i=0;i<held.Length;i++)if(held[i]!=null)held[i].enabled=heldEnabled[i];}
    void LateUpdate()
    {
        if(battle==null)return;
        // Keep the final fallen pose through blackout and scene loading. The
        // Animator still evaluates each frame and otherwise restores locomotion.
        // Keep the pose frozen while buoyancy follows the actual moving water.
        if(IsComplete){EvaluatePose();FloatOnWaves();return;}
        if(PauseSettingsMenu.IsOpen || Time.timeScale<=0 || Time.deltaTime<=0){RenderActors();FloatOnWaves();return;}
        // Animator's engine evaluation happens after Update. Sample the manual graph after it.
        Advance();
        FloatOnWaves();
        if(IsComplete)return;
    }
    public void ResolveContactsAndCamera()
    {
        if(battle==null || IsComplete)return;
        if(!PauseSettingsMenu.IsOpen && Time.deltaTime>0)VerifyFinalContact();
        bool traversal=(FilmTime>=4.55f && FilmTime<6.4f)||(FilmTime>=11.6f && FilmTime<14);
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset,enemy=traversal ? Vector3.Lerp(shark.NoseWorldPoint,shark.VisualRenderer.bounds.center,.25f) : FilmTime>=14 ? shark.TailWorldPoint : shark.NoseWorldPoint;
        Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 focus=Vector3.Lerp((feet+head)*.5f,enemy,traversal ? .42f : .28f);float end=Ease((FilmTime-18.05f)/1.1f);
        float extent=Mathf.Max(Vector3.Distance(focus,feet),Mathf.Max(Vector3.Distance(focus,head),Vector3.Distance(focus,enemy)));
        if(traversal)extent=Mathf.Max(extent,Vector3.Distance(focus,enemy)+shark.VisualRenderer.bounds.extents.magnitude*.35f);
        float distance=Mathf.Clamp(extent/Mathf.Sin(47*.5f*Mathf.Deg2Rad)*1.3f,17,traversal ? 50 : 30);
        Vector3 Candidate(float sign)
        {
            Vector3 at=focus+(traversal ? Side*(sign*.95f)-Forward*.35f+Vector3.up*.65f : Side*(sign*.8f)-Forward*.45f+Vector3.up*.85f).normalized*distance;
            at.y=Mathf.Max(at.y,Water(at)+8);return at;
        }
        float ViewScore(Vector3 at)
        {
            float score=0;
            foreach(var point in new[]{head,animator.GetBoneTransform(HumanBodyBones.Chest).position,WeaponTip})
            {
                Vector3 delta=point-at;bool blocked=false;
                foreach(var hit in Physics.RaycastAll(at,delta.normalized,delta.magnitude-.1f,~0,QueryTriggerInteraction.Collide))
                    if(hit.collider.GetComponentInParent<Story1WreckMeshCollision>()!=null || hit.collider.name.StartsWith("Solid anatomy")){blocked=true;break;}
                if(!blocked)score+=point==head ? 4 : 3;
            }
            return score;
        }
        Vector3 shot=Candidate(cameraSide),other=Candidate(-cameraSide);
        cameraOccludedSeconds=ViewScore(other)>ViewScore(shot)+2 ? cameraOccludedSeconds+Time.deltaTime : 0;
        if(cameraOccludedSeconds>.35f){cameraSide=-cameraSide;cameraOccludedSeconds=0;shot=other;}
        float opening=Ease(Elapsed/Mathf.Max(.6f,LeadInSeconds));shot=Vector3.Lerp(cameraStart,shot,opening);float blend=1-Mathf.Exp(-5*Time.deltaTime);
        Vector3 cameraAt=Vector3.Lerp(shotCamera.transform.position,shot,blend);
        cameraAt.y=Mathf.Max(cameraAt.y,Water(cameraAt)+8);shotCamera.transform.position=cameraAt;
        Quaternion look=Quaternion.LookRotation(focus-shotCamera.transform.position);
        shotCamera.transform.rotation=Quaternion.Slerp(shotCamera.transform.rotation,Quaternion.Slerp(cameraRotation,look,opening),blend);
        shotCamera.fieldOfView=Mathf.Lerp(cameraFov,Mathf.Lerp(47,51,end),opening);
    }
    void FloatOnWaves()
    {
        if(!floating)return;
        float delta=PauseSettingsMenu.IsOpen ? 0 : Time.deltaTime;
        var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
        var head=animator.GetBoneTransform(HumanBodyBones.Head);
        // Preserve the authored horizontal fall into open water before settling.
        if(!IsComplete){floatingChest.x=chest.position.x;floatingChest.z=chest.position.z;}
        if(delta>0)
        {
            if(IsComplete)floatingChest+=floatDrift*delta;
            const float span=1.5f;
            float slopeX=(Water(floatingChest+Vector3.right*span)-Water(floatingChest-Vector3.right*span))/(2*span);
            float slopeZ=(Water(floatingChest+Vector3.forward*span)-Water(floatingChest-Vector3.forward*span))/(2*span);
            // A broad, partly submerged body follows filtered swell, not every ripple.
            var normal=new Vector3(-slopeX*.55f,1,-slopeZ*.55f).normalized;
            var tilt=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,normal),10);
            floatRotation=Quaternion.Slerp(floatRotation,tilt*floatHeading,1-Mathf.Exp(-2.5f*delta));
        }
        // Rotate about the chest, avoiding the distant standing rig's root pivot.
        hero.transform.rotation=floatRotation;
        hero.transform.position+=floatingChest-chest.position;
        if(delta>0)
        {
            float target=Mathf.Max(Water(chest.position)+.04f,
                Water(head.position)+.12f-(head.position.y-chest.position.y));
            floatingChest.y=Mathf.SmoothDamp(floatingChest.y,target,ref floatHeightVelocity,.22f,Mathf.Infinity,delta);
            // Preserve breathing clearance during a rising wave while retaining soft settling.
            floatingChest.y=Mathf.Max(floatingChest.y,target-.08f);
            hero.transform.position+=floatingChest-chest.position;
        }
    }
    void OnDestroy()
    {
        ReleaseActors();
    }
    public void ReleaseActors()
    {
        if(supportBody!=null)supportBody.angularDamping=originalDamping;
        CatchStick();if(animator!=null)animator.speed=1;if(poseGraph.IsValid())poseGraph.Destroy();if(shark!=null)shark.ReleaseCinematicAnimation();
        if(actorColliders!=null)for(int i=0;i<actorColliders.Length;i++)if(actorColliders[i]!=null)actorColliders[i].enabled=colliderEnabled[i];
        actorColliders=null;
        if(sampledSkins!=null)for(int i=0;i<sampledSkins.Length;i++)if(sampledSkins[i]!=null)
        {sampledSkins[i].forceMatrixRecalculationPerRender=skinMatrixModes[i];sampledSkins[i].updateWhenOffscreen=skinOffscreenModes[i];}
        sampledSkins=null;
        if(swimmingWeapon!=null)swimmingWeapon.CinematicTraversal=false;
    }
}
