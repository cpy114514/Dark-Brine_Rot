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
    public float WorstPlantTime { get; private set; }
    public string WorstPlantDetails { get; private set; }
    public float LargestTailSkinAdjustment { get; private set; }
    public float LargestTailSkinRotation { get; private set; }
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
    Transform[] retainedHeroBones;
    Vector3[] retainedHeroPositions;
    Quaternion[] retainedHeroRotations;
    Vector3[] entryHeroPositions;
    Quaternion[] entryHeroRotations;
    Vector3 retainedHeroRoot;
    Quaternion retainedHeroHeading;
    bool hasRetainedHeroPose;
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
    string slashPlanningStage;
    Story1WreckPlank slashSampleBoard,parrySampleBoard;
    Vector3 slashSampleNormal,parrySampleNormal;
    Vector3 parryContactOffset;
    Vector3 guardWeaponOffset;
    Story1WreckPlank guardWeaponBoard;
    Vector3 guardWeaponNormal;
    bool guardWeaponMeasured;
    bool parryContactMeasured;
    float parryShaftFraction=.45f;
    float reachableShaftCost;
    float chosenStancePoseCost;
    float chosenStanceWoodPenetration=float.PositiveInfinity;
    float reachableShaftPenetration;
    float supportFootprintRadius=.75f;
    int stanceCandidateCount;
    float attackEndpointCost,attackRouteCost;
    Vector3 attackAim;
    Vector3 attackContactMarkerLocal;
    Vector3 attackRouteOffset;
    float slashShaftFraction;
    Story1SharkWreckContact.AnatomyPose tailContactAnatomy,shipContactAnatomy;
    Quaternion attackFacing;
    string attackFacingKey;
    float attackFacingSampleTime=-100;
    Vector3 probeRaisedNose;
    Vector3[] entrySharkRoute;
    float entrySharkRouteLength;
    float entrySharkApproachSeconds;
    Vector3 entrySharkExitRootLocal;
    Quaternion entrySharkHeadingLocal;
    Vector3 attackStartRoot;
    Quaternion attackStartRotation;
    Vector3 leftSole,rightSole;
    Quaternion cameraRotation,projectileRotation,stageRotation;
    Quaternion initialHeroRotation;
    float cameraFov,swimSeconds,soleOffset;
    bool fromWater;
    bool entryPullStarted;
    float entrySupportWaitStarted=-1;
    Vector3 entryPullStartLocal,entryLeftGrip,entryRightGrip;
    Vector3 entryLandingLocal,entryOutwardLocal;
    Vector3 entryStartFeetLocal;
    Vector3 entryGripEdgeLocal;
    float entryWalkSeconds;
    SahurSwimmingWeapon swimmingWeapon;
    Rigidbody supportBody;
    float originalDamping;
    Vector3 fallVelocity;
    float defeatContactTime=-1,stickLossTime=-1;
    Transform contactTailBone;
    bool floating;
    bool initialized;
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
    bool releasedControl;
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
    public bool ProtectsBoard(Story1WreckPlank candidate)
    {
        bool airborne=activeAnimation!=null && activeAnimation.StartsWith("Jump airborne");
        return (defeatContactTime<0 && candidate==board && !airborne) || (candidate==hopTarget && airborne);
    }
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
        if(shark.BoneAnimator!=null)contactTailBone=shark.BoneAnimator.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Tail_Tip");
        if(swimmingWeapon!=null)swimmingWeapon.CinematicTraversal=fromWater;
        supportBody=board.GetComponent<Rigidbody>();originalDamping=supportBody.angularDamping;supportBody.angularDamping=6;
        board.Drive(0,0,null);
        animator=hero.CharacterAnimator;
        retainedHeroBones=animator.GetComponentsInChildren<Transform>(true);
        retainedHeroPositions=new Vector3[retainedHeroBones.Length];retainedHeroRotations=new Quaternion[retainedHeroBones.Length];
        entryHeroPositions=retainedHeroBones.Select(b=>b.localPosition).ToArray();
        entryHeroRotations=retainedHeroBones.Select(b=>b.localRotation).ToArray();
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
        entryStartFeetLocal=board.transform.InverseTransformPoint(startFeet);
        Vector3 landing=board.BoardingPoint(startFeet);
        entryLandingLocal=board.transform.InverseTransformPoint(landing+Vector3.up*.04f);
        entryGripEdgeLocal=board.transform.InverseTransformPoint(landing);
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
        else{walkSeconds=Vector3.ProjectOnPlane(startFeet-Deck(-.4f),Vector3.up).magnitude/3;LeadInSeconds=Mathf.Max(.6f,walkSeconds+.25f);}
        var definition=Resources.Load<Story1EncounterDefinition>("Encounter/v006/EncounterDefinition");
        beats=definition!=null ? definition.beats : Story1EncounterDefinition.ApprovedBeats();
        contacts=definition!=null && definition.contacts!=null && definition.contacts.Length==6 ? definition.contacts : Story1EncounterDefinition.ApprovedContacts();
        contactTimes=contacts.Select(c=>c.time).ToArray();
        AnimationClip Revised(string name)=>Resources.LoadAll<AnimationClip>("Encounter/"+name.Substring(name.LastIndexOf('_')+1)+"/"+name+"/"+name).First(c=>!c.name.StartsWith("__preview__"));
        foreach(var beat in beats)if(!revisedClips.ContainsKey(beat.clip))revisedClips.Add(beat.clip,Revised(beat.clip));
        slash=Revised(beats.First(b=>b.action==Story1EncounterAction.Slash).clip);guard=Revised(beats.First(b=>b.action==Story1EncounterAction.Guard).clip);block=Revised(beats.First(b=>b.action==Story1EncounterAction.Brace).clip);
        throwClip=Revised("PLAYER_Throw_v006");stagger=Revised("PLAYER_Stagger_v006");knockdown=Revised("PLAYER_Fall_v006");
        // Older project copies may not contain the v010 export yet. Keep the
        // newest retained Humanoid climb usable instead of aborting the finale.
        climb=new[]{"PLAYER_Climb_v010","PLAYER_Climb_v009","PLAYER_Climb_v006"}
            .Select(name=>Resources.LoadAll<AnimationClip>("Encounter/"+name.Substring(name.LastIndexOf('_')+1)+"/"+name+"/"+name)
                .FirstOrDefault(c=>!c.name.StartsWith("__preview__")))
            .FirstOrDefault(c=>c!=null);
        if(climb==null)throw new System.InvalidOperationException("Wreck finale requires a retained PLAYER_Climb animation.");
        // The user restored legacy walk/run/roll, including supported entry
        // and same-board footwork. Use the same actual controller walk clip.
        walk=library.FirstOrDefault(c=>c.name.Contains("Walk_Loop")) ?? Revised("PLAYER_Walk_v006");
        jumpStart=Revised("PLAYER_JumpStart_v006");jumpLoop=Revised("PLAYER_JumpAir_v006");jumpLand=Revised("PLAYER_JumpLand_v006");boardJump=null;
        // The live Generic Animator includes the retained prefab bind transforms.
        // Measure its authored contact pose once; FBX SampleAnimation metadata can differ from controller evaluation.
        shark.SampleCinematicAction("Tail_Strike",TralaleroSwimAnimator.TailContactTime);
        tailContactAnatomy=battle.WreckContact.CaptureAnatomy();
        filmTailLocal=shark.transform.InverseTransformPoint(shark.TailWorldPoint);
        shark.SampleCinematicAction("Ship_Smash",TralaleroSwimAnimator.TailContactTime);
        shipContactAnatomy=battle.WreckContact.CaptureAnatomy();
        filmShipTailLocal=shark.transform.InverseTransformPoint(shark.TailWorldPoint);
        shark.SampleCinematicAction("Swim",0);
        // Choose a stance using the actual paired poses after the graph and
        // retained rig are available. Empty space beside a deck is not enough
        // to establish that the whole shark can meet the authored weapon.
        Animate("Entry planning",guard,0);EvaluatePose();
        SahurSupportPose.MeasureSoles(animator,ref leftSole,ref rightSole);
        supportFootprintRadius=Mathf.Max(.75f,Mathf.Max(Vector3.ProjectOnPlane(leftFoot.TransformPoint(leftSole)-hero.transform.position,Vector3.up).magnitude,
            Vector3.ProjectOnPlane(rightFoot.TransformPoint(rightSole)-hero.transform.position,Vector3.up).magnitude)+.12f);
        // A nearby surfboard can carry the swimmer's hand without having
        // room for both authored combat feet. Choose another real reachable
        // surface before the swim/climb lead-in; never move a fragment.
        Vector3? selectedEntryEdge=null;
        if(fromWater)
        {
            FindEntryEdge(board,landing);
            if(stanceCandidateCount==0 && SelectWaterSupport(startFeet,out var replacementLanding,out var replacementEdge))
            {
                landing=replacementLanding;
                selectedEntryEdge=replacementEdge;
                entryStartFeetLocal=board.transform.InverseTransformPoint(startFeet);
                entryGripEdgeLocal=board.transform.InverseTransformPoint(landing);
                entryOutwardLocal=board.transform.InverseTransformDirection(board.OpenWaterDirection(landing));
            }
        }
        if(fromWater)
        {
            Vector3 outward=board.transform.TransformDirection(entryOutwardLocal);
            landing=board.BoardingPoint(landing-outward*(supportFootprintRadius+.3f));
            entryLandingLocal=board.transform.InverseTransformPoint(landing+Vector3.up*.04f);
        }
        edge=selectedEntryEdge ?? FindEntryEdge(fromWater ? landing : startFeet);
        localEdge=board.transform.InverseTransformPoint(edge);
        deckOffset=new Vector2(localEdge.x*board.Dimensions.x,localEdge.z*board.Dimensions.z);FaceOpenWater(edge);
        slashContactMeasured=parryContactMeasured=false;
        if(fromWater)
        {
            Vector3 swimEntry=board.transform.TransformPoint(entryLandingLocal)+board.transform.TransformDirection(entryOutwardLocal)*.8f;
            swimSeconds=Mathf.Max(.2f,Vector3.ProjectOnPlane(startFeet-swimEntry,Vector3.up).magnitude/(hero.swimSpeed*hero.fastSwimMultiplier));
            entryWalkSeconds=Vector3.ProjectOnPlane(board.transform.TransformPoint(entryLandingLocal)-Deck(-.4f),Vector3.up).magnitude/3;
            LeadInSeconds=swimSeconds+1.15f+entryWalkSeconds+.4f;
        }
        else{walkSeconds=Vector3.ProjectOnPlane(startFeet-Deck(-.4f),Vector3.up).magnitude/3;LeadInSeconds=Mathf.Max(.6f,walkSeconds+.25f);}
        PlanSharkEntry();
        LeadInSeconds=Mathf.Max(LeadInSeconds,entrySharkApproachSeconds);
        var post=gameObject.AddComponent<Story1FinalePostProcess>();post.film=this;
        RenderActors();
        initialized=true;
    }
    bool SelectWaterSupport(Vector3 swimmer,out Vector3 landing,out Vector3 combatEdge)
    {
        landing=combatEdge=default;
        var alternatives=FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None)
            .Where(p=>p!=board && p.IsBoardable && p.Dimensions.y<1.2f && Mathf.Abs(p.transform.up.y)>.82f && p.Speed<5)
            .Where(p=>Mathf.Min(p.Dimensions.x,p.Dimensions.z)>supportFootprintRadius*2+.2f && p.EdgeDistance(swimmer)<60)
            .OrderBy(p=>p.EdgeDistance(swimmer)).Take(8);
        foreach(var candidate in alternatives)
        {
            Vector3 at=candidate.BoardingPoint(swimmer);
            if(Water(at)-at.y>.6f || at.y-Water(at)>2.5f)continue;
            Vector3 edge=FindEntryEdge(candidate,at);
            if(stanceCandidateCount==0)edge=FindEntryEdge(candidate,candidate.BoardingPoint(candidate.GetComponent<Rigidbody>().worldCenterOfMass));
            if(stanceCandidateCount==0 || chosenStanceWoodPenetration>.0015f || float.IsInfinity(chosenStancePoseCost))continue;
            Vector3 afterClimb=candidate.BoardingPoint(at-candidate.OpenWaterDirection(at)*(supportFootprintRadius+.3f));
            if(!SupportedEntryWalk(candidate,afterClimb,edge))continue;
            if(supportBody!=null)supportBody.angularDamping=originalDamping;
            board=candidate;supportBody=board.GetComponent<Rigidbody>();originalDamping=supportBody.angularDamping;supportBody.angularDamping=6;
            board.Drive(0,0,null);landing=at;combatEdge=edge;
            return true;
        }
        return false;
    }
    bool SupportedEntryWalk(Story1WreckPlank support,Vector3 from,Vector3 to)
    {
        Vector3 normal=support.transform.up;if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,support.Dimensions.y+1);
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/.3f));
        for(int i=0;i<=steps;i++)for(int bearing=0;bearing<360;bearing+=90)
        {
            float u=(float)i/steps;
            Vector3 sole=Vector3.Lerp(from,to,u)+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*Mathf.Lerp(.25f,supportFootprintRadius,u);
            if(!support.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) || Vector3.Dot(hit.normal,Vector3.up)<=.5f)return false;
        }
        return true;
    }
    void PlanSharkEntry()
    {
        Vector3 futureFeet=Deck(-.4f,-Mathf.Sin(Ease(1.184f/1.8f)*Mathf.PI)*.3f);
        Vector3 contact=futureFeet+hero.transform.TransformVector(PredictParryTip(futureFeet));
        Quaternion heading=ChooseAttackFacing("Entry preparation",contact,false);
        Vector3 contactRoot=contact-heading*Vector3.Scale(attackContactMarkerLocal,shark.transform.lossyScale);
        Vector3 outward=Vector3.ProjectOnPlane(contact-contactRoot,Vector3.up).normalized;
        Vector3 exitRoot=contactRoot-outward*2;
        entrySharkExitRootLocal=board.transform.InverseTransformPoint(exitRoot);
        entrySharkHeadingLocal=Quaternion.Inverse(board.transform.rotation)*heading;
        attackFacingKey=null;attackFacingSampleTime=-100;
        shark.SampleCinematicAction("Swim",0);
        Vector3 swimNose=shark.transform.InverseTransformPoint(shark.NoseWorldPoint);
        Vector3 end=exitRoot+heading*Vector3.Scale(swimNose,shark.transform.lossyScale);
        var anatomy=battle.WreckContact.CaptureAnatomy();float best=float.PositiveInfinity;
        foreach(float depth in new[]{0f,3f,7f,12f})foreach(float lateral in new[]{0f,-8f,8f,-16f,16f,-24f,24f})
        {
            var route=new[]{initialNose,Vector3.Lerp(initialNose,end,.3f)+Side*lateral,Vector3.Lerp(initialNose,end,.75f)+Side*lateral,end};
            route[1].y=Mathf.Min(route[1].y,Water(route[1])-depth-2);
            route[2].y=Mathf.Min(route[2].y,Water(route[2])-depth-2);
            float length=0,cost=0,rootLength=0,covered=0;
            for(int i=1;i<route.Length;i++)length+=Vector3.Distance(route[i-1],route[i]);
            Vector3 previousRoot=shark.transform.position;
            // Test the whole swimming anatomy along the route. Wreck sails
            // and masts can extend deep enough to obstruct a surface-only
            // straight approach even when the final attack pose is clear.
            for(int segment=0;segment<3;segment++)
            {
                float segmentLength=Vector3.Distance(route[segment],route[segment+1]);
                for(int sample=1;sample<=4;sample++)
                {
                    float fraction=sample*.25f,progress=(covered+fraction*segmentLength)/Mathf.Max(.001f,length);
                    Vector3 at=Vector3.Lerp(route[segment],route[segment+1],fraction);
                    Quaternion rotation=Quaternion.Slerp(stageRotation,heading,Ease(progress/.6f));
                    cost+=battle.WreckContact.PoseObstruction(anatomy,at,rotation,false)*100;
                    Vector3 root=at-rotation*Vector3.Scale(anatomy.nose,shark.transform.lossyScale);
                    rootLength+=Vector3.Distance(previousRoot,root);previousRoot=root;
                }
                covered+=segmentLength;
            }
            cost+=length*.003f+depth*.002f;
            if(cost<best)
            {
                best=cost;entrySharkRoute=route;entrySharkRouteLength=length;
                entrySharkApproachSeconds=Mathf.Max(rootLength/18,Quaternion.Angle(stageRotation,heading)/220)+.5f;
            }
        }
    }
    void RenderSharkEntry()
    {
        float progress=Mathf.Clamp01(Elapsed/Mathf.Max(.01f,LeadInSeconds-.15f));
        float travelled=progress*entrySharkRouteLength;
        Vector3 nose=entrySharkRoute[entrySharkRoute.Length-1];
        for(int i=1;i<entrySharkRoute.Length;i++)
        {
            float length=Vector3.Distance(entrySharkRoute[i-1],entrySharkRoute[i]);
            if(travelled<=length){nose=Vector3.Lerp(entrySharkRoute[i-1],entrySharkRoute[i],travelled/Mathf.Max(.001f,length));break;}
            travelled-=length;
        }
        Quaternion heading=board.transform.rotation*entrySharkHeadingLocal;
        Vector3 swimNose=shark.transform.InverseTransformPoint(shark.NoseWorldPoint);
        Vector3 goal=board.transform.TransformPoint(entrySharkExitRootLocal)+heading*Vector3.Scale(swimNose,shark.transform.lossyScale);
        nose+=Vector3.Lerp(Vector3.zero,goal-entrySharkRoute[entrySharkRoute.Length-1],Ease(progress));
        NoseAt(nose,Quaternion.Slerp(stageRotation,heading,Ease(progress/.6f)));
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
        // Clip phase follows the shared authored clock, but a transition can
        // only begin when this pose is actually selected. A late low-rate
        // frame must not pretend its crossfade already happened off-screen.
        currentClip=clip;poseStart=start;poseSpeed=speed;poseOffset=offset;blendStart=Elapsed;blendSeconds=fade;
        pose=AnimationClipPlayable.Create(poseGraph,clip);pose.SetSpeed(0);mixer.ConnectInput(1,pose,0);
    }
    Vector3 FindEntryEdge(Vector3 feet)=>FindEntryEdge(board,feet);
    Vector3 FindEntryEdge(Story1WreckPlank support,Vector3 feet,Vector3? jumpFrom=null,float maximumTravel=12)
    {
        Vector3 best=support.BoardingPoint(feet);float bestScore=float.NegativeInfinity;
        chosenStancePoseCost=float.PositiveInfinity;
        chosenStanceWoodPenetration=float.PositiveInfinity;
        var candidates=new System.Collections.Generic.List<(Vector3 point,float score,bool sampledSupport)>();
        foreach(float footprint in poseGraph.IsValid() && pose.IsValid() ? new[]{supportFootprintRadius,.6f} : new[]{supportFootprintRadius})
        for(int angle=0;angle<360;angle+=15)
        {
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*support.transform.forward;
            Vector3 candidate=support.BoardingPoint(feet+direction*100);
            candidate=support.BoardingPoint(candidate-support.OpenWaterDirection(candidate)*(footprint+.3f));
            if(jumpFrom.HasValue && (Vector3.ProjectOnPlane(candidate-jumpFrom.Value,Vector3.up).magnitude>maximumTravel || Mathf.Abs(candidate.y-jumpFrom.Value.y)>1.5f))continue;
            float length=Vector3.Distance(feet,candidate);
            bool safe=true;Vector3 normal=support.transform.up;if(normal.y<0)normal=-normal;
            int steps=Mathf.Max(1,Mathf.CeilToInt(length/.3f));
            for(int i=0;i<=steps && safe;i++)
            {
                Vector3 at=Vector3.Lerp(feet,candidate,(float)i/steps);float reach=Mathf.Max(2,support.Dimensions.y+1);
                safe=support.Collision.Raycast(new Ray(at+normal*reach,-normal),out var hit,reach*2) && Vector3.Dot(hit.normal,Vector3.up)>.5f;
                for(int bearing=0;bearing<360 && safe;bearing+=90)
                {
                    // The live/climbing starting foot can be near an edge.
                    // Widen the verified corridor towards the full standing
                    // footprint instead of rejecting every interior stance
                    // because a circle around that starting foot overhangs.
                    float corridorRadius=Mathf.Lerp(.25f,footprint,(float)i/steps);
                    Vector3 step=at+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*corridorRadius;
                    safe=support.Collision.Raycast(new Ray(step+normal*reach,-normal),out var stepHit,reach*2) && Vector3.Dot(stepHit.normal,Vector3.up)>.5f;
                }
            }
            if(!safe)continue;
            // A centre ray can hit a narrow triangle while both authored feet
            // miss the mesh. Require a real stance-sized surface around it.
            for(int bearing=0;bearing<360 && safe;bearing+=45)
            {
                Vector3 at=candidate+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*footprint;
                float reach=Mathf.Max(2,support.Dimensions.y+1);
                safe=support.Collision.Raycast(new Ray(at+normal*reach,-normal),out var stanceHit,reach*2) && Vector3.Dot(stanceHit.normal,Vector3.up)>.5f;
            }
            if(!safe)continue;
            Vector3 outward=support.OpenWaterDirection(candidate);float clearance=ApproachClearance(candidate,outward);
            Vector3 centre=support.GetComponent<Rigidbody>().worldCenterOfMass;
            float relativeCrest=0;
            for(int forecast=0;ocean!=null && forecast<=20;forecast+=4)
                relativeCrest=Mathf.Max(relativeCrest,ocean.SampleSurfaceHeight(candidate,Time.time+forecast)-ocean.SampleSurfaceHeight(centre,Time.time+forecast)-(candidate.y-centre.y));
            float score=clearance*2-length*.08f-Mathf.Max(0,Water(candidate)+.1f-candidate.y)*30
                -Mathf.Max(0,relativeCrest-1.2f)*30;
            candidates.Add((candidate,score,footprint<supportFootprintRadius));
            if(footprint>=supportFootprintRadius && score>bestScore){bestScore=score;best=candidate;}
        }
        stanceCandidateCount=candidates.Count;
        if(poseGraph.IsValid() && pose.IsValid() && guard!=null && candidates.Count>0)
        {
            var oldBoard=board;Vector2 oldOffset=deckOffset;Vector3 oldForward=localForward;
            Vector3 position=hero.transform.position;Quaternion rotation=hero.transform.rotation;
            board=support;bestScore=float.NegativeInfinity;int accepted=0;
            foreach(var candidate in candidates.OrderByDescending(c=>c.score))
            {
                Vector3 local=support.transform.InverseTransformPoint(candidate.point);
                deckOffset=new Vector2(local.x*support.Dimensions.x,local.z*support.Dimensions.z);FaceOpenWater(candidate.point);
                if(candidate.sampledSupport && !SupportsContinuousStances())continue;
                accepted++;
                slashContactMeasured=parryContactMeasured=false;
                PredictParryTip(Deck(-.4f));float parryCost=reachableShaftCost;
                float parryWood=reachableShaftPenetration;
                PredictSlashTip(Deck(-.4f));float slashCost=reachableShaftCost;
                float slashWood=reachableShaftPenetration;
                float poseCost=Mathf.Max(Mathf.Max(parryCost,slashCost),GradeTailStance(Deck(-.3f),out float tailWood));
                float penetration=Mathf.Max(Mathf.Max(parryWood,slashWood),tailWood);
                float score=candidate.score-penetration*100000-poseCost*.2f;
                if(score>bestScore){bestScore=score;best=candidate.point;chosenStancePoseCost=poseCost;chosenStanceWoodPenetration=penetration;}
            }
            stanceCandidateCount=accepted;
            board=oldBoard;deckOffset=oldOffset;localForward=oldForward;
            hero.transform.SetPositionAndRotation(position,rotation);EvaluatePose();
            slashContactMeasured=parryContactMeasured=false;
        }
        return best;
    }
    bool SupportsContinuousStances()
    {
        Vector3 position=hero.transform.position;Quaternion rotation=hero.transform.rotation;
        var parry=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Parry).clip];
        bool supported=true;
        // Check the complete supported source curves, including their load
        // and recovery. A contact-frame-only check missed planted-foot drift
        // in the rejected v024 edge experiment.
        foreach(var row in new[]{(guard,-.4f),(parry,-.4f),(slash,.15f),(block,-.3f),(throwClip,.15f)})
        {
            Vector3 feet=Deck(row.Item2),normal=board.transform.up;if(normal.y<0)normal=-normal;
            float reach=Mathf.Max(2,board.Dimensions.y+1);
            var sample=AnimationClipPlayable.Create(poseGraph,row.Item1);sample.SetSpeed(0);
            mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);
            Vector3 anchor=default;bool hasAnchor=false,plantLeft=false;
            try
            {
                int frames=Mathf.CeilToInt(row.Item1.length*60);
                for(int frame=0;frame<=frames;frame++)
                {
                    float time=Mathf.Min(row.Item1.length-.00001f,frame/60f);sample.SetTime(time);
                    hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));
                    poseGraph.Evaluate(0);PlantFeet(feet,false);
                    bool OnMesh(Vector3 sole,out Vector3 point,out float height)
                    {
                        point=sole;height=float.PositiveInfinity;
                        if(!board.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) || Vector3.Dot(hit.normal,Vector3.up)<=.5f)return false;
                        point=hit.point;height=Vector3.Dot(sole-hit.point,normal)/Mathf.Max(.2f,normal.y);return height>=-.03f;
                    }
                    bool left=OnMesh(leftFoot.TransformPoint(leftSole),out var a,out float leftHeight),right=OnMesh(rightFoot.TransformPoint(rightSole),out var b,out float rightHeight);
                    // The recovering foot can lift. It must remain over real
                    // timber, and the actual supporting sole keeps the same
                    // 3 cm limit used by the native runtime acceptance check.
                    if(!left || !right || Mathf.Min(Mathf.Abs(leftHeight),Mathf.Abs(rightHeight))>.03f){supported=false;break;}
                    if(time>.14f && row.Item1!=slash && row.Item1!=throwClip)
                    {
                        if(!hasAnchor){plantLeft=Mathf.Abs(leftHeight)<=Mathf.Abs(rightHeight);anchor=plantLeft ? a : b;hasAnchor=true;}
                        if(SahurSupportPose.Anchor(animator,plantLeft,plantLeft ? leftSole : rightSole,anchor)>.05f){supported=false;break;}
                    }
                }
            }
            finally{mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);}
            if(!supported)break;
        }
        mixer.ConnectInput(1,pose,0);hero.transform.SetPositionAndRotation(position,rotation);EvaluatePose();
        return supported;
    }
    float GradeTailStance(Vector3 feet,out float woodPenetration)
    {
        woodPenetration=0;
        Vector3 position=hero.transform.position;Quaternion heading=hero.transform.rotation;
        var clip=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Brace).clip];
        var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);sample.SetTime(.572f);
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);
        hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));poseGraph.Evaluate(0);PlantFeet(feet,false);
        Vector3 chestTarget=TailContactTarget,guardTarget=GuardContactPoint;float worst=0;var surface=OceanSurfaceSampler.Capture(ocean,shotCamera,Time.time);
        foreach(bool powerful in new[]{false,true})
        {
            Vector3 target=powerful ? chestTarget : guardTarget;
            var anatomy=powerful ? shipContactAnatomy : tailContactAnatomy;float best=float.PositiveInfinity,selectedWood=float.PositiveInfinity;bool clearFound=false;
            foreach(int side in new[]{1,-1})foreach(float yaw in new[]{0f,-40f,40f,-80f,80f,-120f,120f,180f})foreach(float pitch in new[]{-25f,-16f,0f,12f,25f})
            {
                Quaternion rotation=Quaternion.LookRotation((Side*side+Forward*.75f).normalized)*Quaternion.Euler(pitch,yaw,powerful ? -4 : 0);
                Vector3 bulk=target-rotation*Vector3.Scale(anatomy.tailMarker-anatomy.body,shark.transform.lossyScale);
                float exposure=Mathf.Max(0,bulk.y-surface.Height(bulk)+1.4f),bias=exposure*exposure*.2f;
                float penetration=battle.WreckContact.PoseObstruction(anatomy,target,rotation,true);
                bool clear=penetration<=.0004f;if(clearFound && !clear)continue;
                float cost=penetration*10000+bias;
                if(clear && !clearFound || cost<best){best=cost;selectedWood=penetration;clearFound=clear;}
            }
            worst=Mathf.Max(worst,best);
            woodPenetration=Mathf.Max(woodPenetration,selectedWood);
        }
        mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);
        hero.transform.SetPositionAndRotation(position,heading);EvaluatePose();return worst;
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
        if(Elapsed<.18f)
        {
            float blend=Ease(Elapsed/.18f);
            for(int i=0;i<retainedHeroBones.Length;i++)
            {
                var bone=retainedHeroBones[i];if(bone==animator.transform || bone==hero.transform)continue;
                bone.localPosition=Vector3.Lerp(entryHeroPositions[i],bone.localPosition,blend);
                bone.localRotation=Quaternion.Slerp(entryHeroRotations[i],bone.localRotation,blend);
            }
        }
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
        bool fixedSupport=activeAction==Story1EncounterAction.Guard || activeAction==Story1EncounterAction.Parry || activeAction==Story1EncounterAction.Brace || activeAction==Story1EncounterAction.Surf || activeAnimation!=null && activeAnimation.StartsWith("Same board guard");
        // A low-rate frame can enter the guard well after its planned start.
        // Wait for the ACTUAL crossfade to settle before recording its planted
        // sole; otherwise this freezes the old walking foot, then drags the
        // new guard leg towards a different pose's anchor for the whole hold.
        if(measure && FilmTime>=0 && fixedSupport && Elapsed-poseStart>.14f && Elapsed-blendStart>Mathf.Max(.14f,blendSeconds))
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
            if(slide>LargestPlantSlide)
            {
                LargestPlantSlide=slide;WorstPlantTime=FilmTime;
                WorstPlantDetails="animation="+activeAnimation+" action="+activeAction+" poseAge="+(Elapsed-poseStart)+" blendAge="+(Elapsed-blendStart)+" target="+target+" root="+hero.transform.position+" board="+board.name;
            }
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
        string planningStage=FilmTime>=1.8f && FilmTime<3.2f ? "First counter" : FilmTime>=4.55f && FilmTime<7.8f ? "Landing counter" : "Stance";
        if(slashContactMeasured && slashPlanningStage==planningStage && slashSampleBoard==board && Vector3.Angle(normal,slashSampleNormal)<.5f)return slashContactOffset;
        slashPlanningStage=planningStage;
        Vector3 savedPosition=hero.transform.position;Quaternion savedRotation=hero.transform.rotation;
        hero.transform.SetPositionAndRotation(targetFeet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));
        var sample=AnimationClipPlayable.Create(poseGraph,slash);sample.SetSpeed(0);sample.SetTime(.63f);
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);poseGraph.Evaluate(0);
        PlantFeet(targetFeet,false);
        // The descending tip can finish below the deck. Meet the real shaft
        // above the supporting timber; driving a nose at the buried tip asks
        // a solid shark to pass through that same board.
        Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
        bool counterLocked=attackFacingKey=="First counter" && FilmTime>=1.8f && FilmTime<3.2f ||
            attackFacingKey=="Landing counter" && FilmTime>=4.55f && FilmTime<7.8f;
        float along=counterLocked ? slashShaftFraction : ReachableShaftFraction(a,b,targetFeet);
        slashShaftFraction=along;
        Vector3 offset=hero.transform.InverseTransformVector(Vector3.Lerp(a,b,along)-targetFeet);
        mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);EvaluatePose();
        hero.transform.SetPositionAndRotation(savedPosition,savedRotation);
        PlantFeet(savedPosition+Vector3.up*hero.LowestFootWorldOffset,false);
        slashContactOffset=offset;slashContactMeasured=true;slashSampleBoard=board;slashSampleNormal=normal;return offset;
    }
    Vector3 GuardContactPoint
    {
        get
        {
            if(weapon==null)return GripCentre;
            Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
            // Meet the elevated, genuinely held shaft instead of the end of
            // a downward guard buried beneath the support. No hitbox growth.
            return Vector3.Lerp(a,b,a.y>b.y ? .15f : .85f);
        }
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
        if(attackFacingKey!="Probing bite" || FilmTime>=1.8f)parryShaftFraction=ReachableShaftFraction(a,b,targetFeet);
        parryContactOffset=hero.transform.InverseTransformVector(ParryContactAim-targetFeet);
        mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);EvaluatePose();hero.transform.SetPositionAndRotation(at,rotation);
        PlantFeet(at+Vector3.up*hero.LowestFootWorldOffset,false);parryContactMeasured=true;parrySampleBoard=board;parrySampleNormal=normal;return parryContactOffset;
    }
    Vector3 PredictGuardWeaponTarget(Vector3 targetFeet,Story1WreckPlank targetSupport=null,Vector3? targetFacing=null)
    {
        targetSupport=targetSupport ?? board;
        Quaternion futureHeading=Quaternion.LookRotation(targetFacing ?? Forward);
        Vector3 normal=Quaternion.Inverse(futureHeading)*targetSupport.transform.up;
        if(guardWeaponMeasured && guardWeaponBoard==targetSupport && Vector3.Angle(normal,guardWeaponNormal)<.5f)
            return targetFeet+futureHeading*guardWeaponOffset;
        Vector3 position=hero.transform.position;Quaternion rotation=hero.transform.rotation;
        var previousSupport=board;board=targetSupport;
        var clip=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Brace).clip];
        var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);sample.SetTime(.572f);
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);
        hero.transform.SetPositionAndRotation(targetFeet-Vector3.up*hero.LowestFootWorldOffset,futureHeading);poseGraph.Evaluate(0);PlantFeet(targetFeet,false);
        guardWeaponOffset=Quaternion.Inverse(futureHeading)*(GuardContactPoint-targetFeet);
        mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);
        board=previousSupport;hero.transform.SetPositionAndRotation(position,rotation);EvaluatePose();
        if(activeAnimation==null || !activeAnimation.StartsWith("Jump airborne"))PlantFeet(position+Vector3.up*hero.LowestFootWorldOffset,false);
        guardWeaponMeasured=true;guardWeaponBoard=targetSupport;guardWeaponNormal=normal;
        return targetFeet+futureHeading*guardWeaponOffset;
    }
    float ReachableShaftFraction(Vector3 a,Vector3 b,Vector3 feet)
    {
        string action=shark.CinematicAction;float seconds=shark.CinematicActionSeconds;
        shark.SampleCinematicAction("Bite_Lunge",.52f);
        var anatomy=battle.WreckContact.CaptureAnatomy();
        Vector3 noseLocal=shark.transform.InverseTransformPoint(shark.NoseWorldPoint),bulkLocal=shark.transform.InverseTransformPoint(battle.WreckContact.BulkBodyPoint);
        Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
        var surface=OceanSurfaceSampler.Capture(ocean,shotCamera,Time.time);
        float best=float.PositiveInfinity,fraction=a.y>b.y ? 0 : 1;
        bool clearFound=false;
        bool liveCounter=FilmTime>=1.8f && FilmTime<3.2f || FilmTime>=4.55f && FilmTime<7.8f;
        float counterSeconds=FilmTime>=4.55f && FilmTime<7.8f ? 2.48f : .63f;
        var routes=new System.Collections.Generic.List<(float score,float fraction,Quaternion facing,Vector3 marker,float penetration)>();
        Vector3 startRoot=shark.transform.position;Quaternion startHeading=shark.transform.rotation;
        reachableShaftPenetration=float.PositiveInfinity;
        // A descending stick has many genuine contact points. Choose one
        // whose complete shark anatomy can reach it without entering wood,
        // keeping the bulk submerged; never extend the weapon's hitbox.
        for(int i=0;i<=8;i++)
        {
            float along=i/8f;Vector3 marker=Vector3.Lerp(a,b,along);
            float height=Vector3.Dot(marker-feet,normal);if(height<1.15f)continue;
            foreach(float yaw in new[]{0f,-40f,40f,-80f,80f,-120f,120f,180f})foreach(float pitch in new[]{-35f,-25f,-16f,0f,12f,25f,35f})
            {
                Quaternion facing=Quaternion.LookRotation(-Forward)*Quaternion.Euler(pitch,yaw,0);
                Vector3 bulk=marker-facing*Vector3.Scale(noseLocal-bulkLocal,shark.transform.lossyScale);
                float exposure=Mathf.Max(0,bulk.y-surface.Height(bulk)+1.4f);
                float bias=exposure*exposure*.2f+height*.0002f+Mathf.Abs(yaw)*.00005f+Mathf.Abs(pitch+16)*.00005f;
                float penetration=battle.WreckContact.PoseObstruction(anatomy,marker,facing,false);
                bool clear=penetration<=.0004f;if(clearFound && !clear)continue;
                float cost=penetration*10000+bias;
                if(clear && !clearFound || cost<best){best=cost;fraction=along;reachableShaftPenetration=penetration;clearFound=clear;}
                if(liveCounter && clear)
                {
                    Vector3 endRoot=marker-facing*Vector3.Scale(noseLocal,shark.transform.lossyScale);
                    float turn=Quaternion.Angle(startHeading,facing),excess=Mathf.Max(0,Vector3.Distance(startRoot,endRoot)-24*counterSeconds);
                    routes.Add((cost+turn*turn*.001f+excess*excess*100,along,facing,marker,penetration));
                }
            }
        }
        if(liveCounter && routes.Count>0)
        {
            // Select a real point on the shaft jointly with the route of the
            // whole animal. An isolated clear endpoint could previously ask
            // the next counter to cross its occupied deck during the wind-up.
            float[] phases={.25f,.5f,.75f};
            var samples=new Story1SharkWreckContact.AnatomyPose[3];
            for(int i=0;i<3;i++){shark.SampleCinematicAction("Bite_Lunge",Mathf.Max(0,phases[i]*counterSeconds-(counterSeconds-.52f)));samples[i]=battle.WreckContact.CaptureAnatomy();}
            best=float.PositiveInfinity;
            foreach(var route in routes.GroupBy(r=>r.fraction).SelectMany(g=>g.OrderBy(r=>r.score).Take(3)))
            {
                float score=route.score;
                Vector3 endRoot=route.marker-route.facing*Vector3.Scale(noseLocal,shark.transform.lossyScale);
                for(int i=0;i<3;i++)
                {
                    Quaternion heading=Quaternion.RotateTowards(startHeading,route.facing,300*(phases[i]*counterSeconds)+3);
                    Vector3 root=Vector3.Lerp(startRoot,endRoot,phases[i]);
                    Vector3 nose=root+heading*Vector3.Scale(samples[i].nose,shark.transform.lossyScale);
                    score+=battle.WreckContact.PoseObstruction(samples[i],nose,heading,false)*2000;
                }
                if(score<best){best=score;fraction=route.fraction;reachableShaftPenetration=route.penetration;}
            }
        }
        reachableShaftCost=best;
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
        attackAim=marker;
        bool same=attackFacingKey==key;
        if(!same){attackStartRoot=shark.transform.position;attackStartRotation=shark.transform.rotation;}
        float planningInterval=sampledAge>(tail ? .45f : .3f) ? .04f : .12f;
        if(same && FilmTime-attackFacingSampleTime<planningInterval)return attackFacing;
        float planningDelta=FilmTime-attackFacingSampleTime;
        string sampledAction=shark.CinematicAction;float sampledSeconds=shark.CinematicActionSeconds;
        string action=tail ? powerful ? "Ship_Smash" : "Tail_Strike" : "Bite_Lunge";
        shark.SampleCinematicAction(action,tail ? .65f : .52f);
        var contactAnatomy=battle.WreckContact.CaptureAnatomy();
        float previousCost=same ? battle.WreckContact.PoseObstruction(contactAnatomy,marker,attackFacing,tail) : float.PositiveInfinity;
        Vector3 localMarker=shark.transform.InverseTransformPoint(tail ? shark.TailWorldPoint : shark.NoseWorldPoint);
        attackContactMarkerLocal=localMarker;
        Vector3 localBulk=shark.transform.InverseTransformPoint(battle.WreckContact.BulkBodyPoint);
        float[] pathPhases={.1f,.25f,.5f,.75f,.9f};
        var pathPoses=new Story1SharkWreckContact.AnatomyPose[pathPhases.Length];
        for(int sample=0;sample<pathPhases.Length;sample++)
        {
            float phase=pathPhases[sample];
            float biteSeconds=key=="Probing bite" ? 1.184f : key=="Landing counter" ? 2.48f : .63f;
            float seconds=tail ? Mathf.Max(0,phase*(powerful ? 2.05f : 1.25f)-(powerful ? 1.4f : .6f)) : Mathf.Max(0,phase*biteSeconds-(biteSeconds-.52f));
            shark.SampleCinematicAction(action,seconds);pathPoses[sample]=battle.WreckContact.CaptureAnatomy();
        }
        shark.SampleCinematicAction(action,tail ? .65f : .52f);
        float RouteObstruction(Quaternion rotation,float endpointCost,out Vector3 bestOffset)
        {
            bestOffset=Vector3.zero;float minimum=float.PositiveInfinity;
            var offsets=!powerful ? new[]{Vector3.zero,Vector3.down*2,Vector3.down*4,
                Vector3.down*3+Forward*3,Vector3.down*3-Forward*3,Vector3.down*3+Side*3,Vector3.down*3-Side*3} : new[]{Vector3.zero};
            foreach(var offset in offsets)
            {
            // Clear solid geometry takes precedence over exposure and style.
            // A shallow intersecting pose must not win because its bulk is
            // better submerged; the solver would move it away at impact.
            float cost=endpointCost*10000;
            // A clear endpoint is insufficient when the approach cuts a
            // girder or the occupied deck. Score actual intervening anatomy
            // as well, before selecting the wind-up's facing.
            for(int sample=0;sample<pathPhases.Length;sample++)
            {
                // Test the anatomy in its actual winding-up pose. Reusing
                // the fully coiled contact pose for the whole approach hid
                // collisions made by the earlier straight spine and tail.
                float phase=pathPhases[sample];var anatomy=pathPoses[sample];
                Quaternion facing=Quaternion.Slerp(attackStartRotation,rotation,Ease(phase/(tail ? .55f : .45f)));
                Vector3 at;
                if(tail)
                {
                    Vector3 end=marker-rotation*Vector3.Scale(localMarker-shark.RootMeshBounds.center,shark.transform.lossyScale);
                    Vector3 startBody=attackStartRoot+attackStartRotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
                    Vector3 body=Vector3.Lerp(startBody,end,Ease(phase))+offset*Mathf.Sin(Mathf.PI*Ease(phase));
                    Vector3 sampledMarker=anatomy.tailMarker;
                    at=body+facing*Vector3.Scale(sampledMarker-shark.RootMeshBounds.center,shark.transform.lossyScale);
                }
                else
                {
                    Vector3 endRoot=marker-rotation*Vector3.Scale(localMarker,shark.transform.lossyScale);
                    Vector3 root=Vector3.Lerp(attackStartRoot,endRoot,phase)+offset*Mathf.Sin(Mathf.PI*phase);
                    facing=Quaternion.RotateTowards(attackStartRotation,rotation,300*(phase*(key=="Probing bite" ? 1.184f : key=="Landing counter" ? 2.48f : .63f))+3);
                    at=root+facing*Vector3.Scale(anatomy.nose,shark.transform.lossyScale);
                }
                cost+=battle.WreckContact.PoseObstruction(anatomy,at,facing,tail)*2000;
            }
            cost+=offset.sqrMagnitude*.015f;
            if(cost<minimum){minimum=cost;bestOffset=offset;}
            }
            return minimum;
        }
        var waterSnapshot=OceanSurfaceSampler.Capture(ocean,shotCamera,Time.time);
        float Exposure(Quaternion rotation)
        {
            Vector3 bulk=marker-rotation*Vector3.Scale(localMarker-localBulk,shark.transform.lossyScale);
            return Mathf.Max(0,bulk.y-(ocean!=null ? waterSnapshot.Height(bulk) : hero.seaLevel)+1.4f);
        }
        if(same && previousCost<.0004f && Exposure(attackFacing)<.2f && RouteObstruction(attackFacing,previousCost,out _) <.001f){shark.SampleCinematicAction(sampledAction,sampledSeconds);attackFacingSampleTime=FilmTime;return attackFacing;}
        attackFacingKey=key;attackFacingSampleTime=FilmTime;float best=float.PositiveInfinity;bool clearFound=false;Quaternion chosen=Quaternion.LookRotation(-Forward)*Quaternion.Euler(-16,0,0);
        Vector3 chosenOffset=Vector3.zero;
        // The new authored head rise supplies bite reach. Reserve upright
        // whole-body attitudes for the explicit breach, not ordinary counters.
        foreach(int side in tail ? new[]{1,-1} : new[]{1})foreach(float yaw in new[]{0f,-20f,20f,-40f,40f,-65f,65f,-90f,90f,-120f,120f,-150f,150f,180f})foreach(float pitch in tail ? new[]{-25f,-16f,0f,12f,25f} : new[]{-35f,-25f,-16f,0f,12f,25f,35f})
        {
            Quaternion facing=tail ? Quaternion.LookRotation((Side*side+Forward*.75f).normalized)*Quaternion.Euler(pitch,yaw,powerful ? -4 : 0) : Quaternion.LookRotation(-Forward)*Quaternion.Euler(pitch,yaw,0);
            // Grade the orientation that can actually be reached this frame.
            // Scoring a clear target and only then limiting its rotation hid
            // collisions in the intermediate, still-turning body at impact.
            if(same)
            {
                float remaining=Mathf.Max(0,(tail ? .65f : .52f)-sampledAge);
                facing=Quaternion.RotateTowards(shark.transform.rotation,facing,300*(remaining+Mathf.Max(.01f,Time.deltaTime))+3);
            }
            float exposure=Exposure(facing);
            float endpointCost=battle.WreckContact.PoseObstruction(contactAnatomy,marker,facing,tail);
            bool clear=endpointCost<=.0004f;if(clearFound && !clear)continue;
            float turn=Quaternion.Angle(attackStartRotation,facing);
            float bias=exposure*exposure*.2f+turn*turn*.001f+Mathf.Abs(yaw)*.00005f+Mathf.Abs(pitch+16)*.00005f;
            if((!clear || clearFound) && endpointCost*10000+bias>=best)continue;
            float cost=RouteObstruction(facing,endpointCost,out var routeOffset)+bias;
            if(clear && !clearFound || cost<best){best=cost;chosen=facing;clearFound=clear;chosenOffset=routeOffset;}
            if(cost<.0015f && exposure<.05f)goto FacingChosen;
        }
        FacingChosen:
        attackEndpointCost=battle.WreckContact.PoseObstruction(contactAnatomy,marker,chosen,tail);
        attackRouteCost=best;
        attackRouteOffset=same ? Vector3.MoveTowards(attackRouteOffset,chosenOffset,3*Mathf.Max(0,planningDelta)) : chosenOffset;
        // Reach planning samples the contact pose temporarily. Restore the
        // timeline's actual recoil/recovery instead of replacing it with
        // another bite every time the approach is evaluated.
        shark.SampleCinematicAction(sampledAction,sampledSeconds);
        attackFacing=chosen;return attackFacing;
    }
    void CounterApproach(float age,float hit,Vector3 aim,float strength)
    {
        Quaternion facing=ChooseAttackFacing(FilmTime<4 ? "First counter" : "Landing counter",aim,false,false,age-.11f);
        PlaceBiteRoot(age,hit,aim,facing,strength);
    }
    void PlaceBiteRoot(float age,float hit,Vector3 aim,Quaternion facing,float strength)
    {
        float approach=Mathf.Clamp01(age/hit),retreat=Ease((age-hit)/.65f);
        Quaternion rotation=Quaternion.RotateTowards(shark.transform.rotation,facing,300*Mathf.Max(.01f,Time.deltaTime)+3);
        Vector3 endRoot=aim-facing*Vector3.Scale(attackContactMarkerLocal,shark.transform.lossyScale);
        // Rotate about the actor's own root. Pinning its nose and turning a
        // twelve-metre body around that point requested an enormous lateral
        // root arc, which the solid movement budget could not follow.
        // A clear head-contact endpoint can still require the straight body
        // to cross the occupied deck. Follow the same submerged arc graded
        // by the anatomical route planner, returning continuously to the
        // actual weapon at contact. Native solid solving remains authoritative.
        Vector3 root=Vector3.Lerp(attackStartRoot,endRoot,approach)+
            attackRouteOffset*Mathf.Sin(Mathf.PI*approach)+Forward*(retreat*strength);
        if(age>hit+.15f)root.y=Mathf.Lerp(root.y,shark.SurfaceRootY(Water(root),rotation),retreat*.8f);
        battle.PoseCinematicShark(root+rotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale),rotation,absoluteHeight:true);
    }
    void TailApproach(float age,Vector3 target,bool powerful)
    {
        Quaternion facing=ChooseAttackFacing(powerful ? "Defeating tail" : "Reversal tail",target,true,powerful,age);
        float anticipation=powerful ? 1.4f : .6f;
        float advance=Ease((age+anticipation)/(TralaleroSwimAnimator.TailContactTime+anticipation)),retreat=Ease((age-.74f)/.9f);
        Vector3 local=powerful ? filmShipTailLocal : filmTailLocal;
        Vector3 end=target-facing*Vector3.Scale(local-shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 startBody=attackStartRoot+attackStartRotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 body=Vector3.Lerp(startBody,end,advance)+attackRouteOffset*Mathf.Sin(Mathf.PI*advance)-Side*(retreat*4);
        Quaternion rotation=Quaternion.Slerp(attackStartRotation,facing,Ease((age+anticipation)/Mathf.Max(.3f,(TralaleroSwimAnimator.TailContactTime+anticipation)*.55f)));
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
                if(!entryPullStarted && Elapsed>=swimSeconds)
                {
                    Vector3 support=board.transform.TransformPoint(entryLandingLocal);
                    float freeboard=support.y-Water(support);
                    float waveVelocity=ocean!=null ? (ocean.SampleSurfaceHeight(support,Time.time+.05f)-ocean.SampleSurfaceHeight(support,Time.time-.05f))/.1f : 0;
                    if(freeboard<-.6f || freeboard>2.5f || Mathf.Abs(supportBody.GetPointVelocity(support).y-waveVelocity)>3)
                    {
                        // Keep swimming while real released timber is still
                        // falling/recovering. Extend only the physical entry;
                        // the approved main film remains exactly 20.8s.
                        if(entrySupportWaitStarted<0)entrySupportWaitStarted=Elapsed;
                        float wait=Elapsed+.05f-swimSeconds;
                        swimSeconds+=wait;LeadInSeconds+=wait;
                    }
                }
                bool pulling=Elapsed>=swimSeconds;float pullAge=Elapsed-swimSeconds;
                bool walking=pullAge>=1.15f && pullAge<1.15f+entryWalkSeconds,turning=pullAge>=1.15f+entryWalkSeconds;
                Animate(turning ? "Entry turn" : walking ? "Entry clear-water walk" : pulling ? "Entry climb" : "Entry swim",turning ? guard : walking ? walk : pulling ? climb : swim,turning ? swimSeconds+1.15f+entryWalkSeconds : walking ? swimSeconds+1.15f : pulling ? swimSeconds : 0,walking ? 3/3.59f : pulling && !turning ? climb.length/1.15f : 1);
                Vector3 landing=board.transform.TransformPoint(entryLandingLocal),outward=Vector3.ProjectOnPlane(board.transform.TransformDirection(entryOutwardLocal),Vector3.up).normalized;
                Vector3 entry=landing+outward*.8f;entry.y=Water(entry)-2;
                if(pulling && !entryPullStarted)
                {
                    entryPullStarted=true;entryPullStartLocal=board.transform.InverseTransformPoint(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset);
                    Vector3 grip=board.transform.TransformPoint(entryGripEdgeLocal)+outward*.45f,normal=board.transform.up;if(normal.y<0)normal=-normal;
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
                // Track the real moving support throughout the entry.
                HeroAt(Vector3.Lerp(board.transform.TransformPoint(entryStartFeetLocal),Deck(-.4f),Mathf.Clamp01(Elapsed/Mathf.Max(.01f,walkSeconds))));
            }
            shark.SampleCinematicAction("Swim",Elapsed);
            RenderSharkEntry();
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
        bool holdReversalGuard=!verifiedContacts.Contains(4) && t>=14.572f && t<=14.572f+contacts[4].window;
        if(holdReversalGuard)
            beat=new Story1EncounterBeat("Finish the right-hand guard",14,14.572f+contacts[4].window,Story1EncounterAction.Brace,beats.First(b=>b.action==Story1EncounterAction.Brace).clip);
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
        Animate(beat.label,clip,Clock(beat.action==Story1EncounterAction.Fall ? defeatContactTime : beat.start),holdReversalGuard ? 0 : beat.action==Story1EncounterAction.Fall ? clip.length/2.2f : 1,holdReversalGuard ? .572f : 0,holdReversalGuard ? 0 : beat.action==Story1EncounterAction.Stagger ? .06f : .12f);
        float stance=t<4 ? -.4f : t<4.55f ? Mathf.Lerp(-.4f,.15f,Ease((t-4)/.55f)) :
            t<6.6f ? .15f*Ease((t-6.4f)/.2f) : t<14 ? .15f : -.3f*Ease((t-14)/.3f);
        Vector3 feet=Deck(stance,t<1.8f ? -Mathf.Sin(Ease(t/1.8f)*Mathf.PI)*.3f : 0);
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
                PlaceBiteRoot(t,1.184f,t<=1.184f ? futureTip : target,facing,3);
            }
            else
            {
                // The crossed contact cue is sampled before hit verification.
                // Keep its actual bite pose until contact is resolved, rather
                // than replacing it with Swim exactly at the planned impact.
                bool biting=t<2.43f || !verifiedContacts.Contains(1) && t<=2.43f+contacts[1].window;
                shark.SampleCinematicAction(biting ? "Bite_Lunge" : verifiedContacts.Contains(1) ? "Hit_Recoil" : "Swim",biting ? Mathf.Clamp(t-1.91f,0,.52f) : t-2.43f);
                Vector3 target=Deck(-.4f)+hero.transform.TransformVector(PredictSlashTip(Deck(-.4f)));
                CounterApproach(t-1.8f,.63f,target,3);
            }
        }
        else if(t<7.8f)
        {
            bool biting=t>=6.4f && (t<7.03f || !verifiedContacts.Contains(2) && t<=7.03f+contacts[2].window);
            shark.SampleCinematicAction(t<6.4f ? "Swim" : biting ? "Bite_Lunge" : verifiedContacts.Contains(2) ? "Hit_Recoil" : "Swim",t<6.4f ? age : biting ? Mathf.Clamp(t-6.51f,0,.52f) : t-7.03f);
            if(t<6.4f){board.Drive(.35f,-.2f,hero.transform);NoseAt(SurfaceNose(Deck()+Forward*6-Side*2,Quaternion.LookRotation(-Forward)),Quaternion.LookRotation(-Forward));}
            else{board.Drive(0,0,null);CounterApproach(t-4.55f,2.48f,Deck(.15f)+hero.transform.TransformVector(PredictSlashTip(Deck(.15f))),4);}
        }
        else if(t<10)
        {
            bool swimming=t<8.94f || !verifiedContacts.Contains(3);
            shark.SampleCinematicAction(swimming ? "Swim" : "Hit_Recoil",swimming ? age : t-8.94f);
            Vector3 nose=SurfaceNose(Deck()+Forward*(6+Ease((t-8.94f)/.8f)*3),Quaternion.LookRotation(-Forward));NoseAt(nose,Quaternion.LookRotation(-Forward));
            if(projectile!=null && !HasLostStick)
            {
                Vector3 target=t<8.94f ? ProjectileHeadAim(t-8.32f) : throwTarget;
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
            float tailAge=!verifiedContacts.Contains(4) && t>=14.572f && t<=14.572f+contacts[4].window ? .65f : Mathf.Max(0,t-13.922f);
            shark.SampleCinematicAction("Tail_Strike",tailAge);
            Vector3 guardTarget=PredictGuardWeaponTarget(Deck(-.3f));
            TailApproach(tailAge,t<=14.572f || !verifiedContacts.Contains(4) ? guardTarget : board.transform.TransformPoint(tailAnchor),false);
        }
        else
        {
            // Begin positioning during the last counter. Waiting until the
            // .65-second tail swing left no physical time to turn a full body
            // around real adjacent timber; keep the authored active window.
            {
                shark.SampleCinematicAction("Ship_Smash",defeatContactTime<0 && t>=18.05f ? .65f : Mathf.Max(0,t-17.4f));
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
            // Exposure is an artistic preference, not a collision. Keep the
            // original squared-overlap cost separately from the style score.
            if(chosenStanceWoodPenetration>.0015f || Water(edge)-edge.y>.6f)continue;
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
        float duration=index==0 ? 1.85f : 2.4f;
        Vector3 landing=FindEntryEdge(hopTarget,hopTarget.BoardingPoint(feet),feet,hopTarget==board ? 3*(duration-.5f) : 12);
        // Preparation cannot carry the actor across a whole deck in a few
        // frames. Limit the supported push-off to an actual short step.
        Vector3 takeoff=board.BoardingPoint(Vector3.MoveTowards(feet,board.BoardingPoint(landing),3*duration*.2f));
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
            // Use the real safe stance selected on this board. Previously
            // the search found it but the fallback never moved there.
            Vector3 initial=hopFrom.BoardingPoint(hopFrom.transform.TransformPoint(hopStartLocal))+Vector3.up*.04f;
            float distance=Vector3.Distance(landing,initial),walking=distance/3+.2f;
            bool moving=localAge<walking && distance>.08f;
            float travelled;
            if(distance<.6f)travelled=distance*Ease(localAge/walking);
            else if(localAge<.2f)travelled=7.5f*localAge*localAge;
            else if(localAge<walking-.2f)travelled=.3f+3*(localAge-.2f);
            else travelled=distance-7.5f*Mathf.Pow(Mathf.Max(0,walking-localAge),2);
            Animate((moving ? "Same board evade " : "Same board guard ")+index,moving ? walk : block,moving ? start : start+walking,moving ? 0 : 1,moving ? travelled/3.59f : 0,.1f);
            // Match gait phase to actual travel through the acceleration and
            // braking, including low-rate frames, rather than fixed cadence.
            if(moving)poseOffset=travelled/3.59f;
            Vector3 outward=hopTarget.OpenWaterDirection(landing),direction=Vector3.ProjectOnPlane(landing-initial,Vector3.up).normalized;
            Vector3 facing=direction.sqrMagnitude>.1f ? Vector3.Slerp(direction,outward,Ease((localAge-Mathf.Max(0,walking-.25f))/.25f)) : outward;
            facing=Quaternion.Slerp(hopFacing,Quaternion.LookRotation(facing),Ease(localAge/.25f))*Vector3.forward;
            HeroAt(Vector3.Lerp(initial,landing,Mathf.Clamp01(travelled/Mathf.Max(.001f,distance))),true,facing);
            if(!moving)LandHop();
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
        if(reversal && ChoreoTime>=13.322f)
        {
            // The tail wind-up starts during the landing, rather than being
            // skipped by the traversal branch until only .572 seconds remain.
            shark.SampleCinematicAction("Tail_Strike",Mathf.Max(0,ChoreoTime-13.922f));
            Vector3 outward=hopTarget.OpenWaterDirection(landing);
            Vector3 futureFeet=hopTarget.BoardingPoint(landing-outward*.3f)+Vector3.up*.04f;
            TailApproach(ChoreoTime-13.922f,PredictGuardWeaponTarget(futureFeet,hopTarget,outward),false);
        }
        else if(reversal)RenderBreach(age*(3.2f/1.722f));
        else
        {
            // Turn and dive towards the actual forthcoming landing strike
            // during the escape. Waiting until the .63-second slash forced
            // the entire animal to pivot through intervening hull timber.
            // This is the same physical approach and unchanged 7.03s contact,
            // with 1.85 seconds of readable preparation rather than a snap.
            shark.SampleCinematicAction("Bite_Lunge",Mathf.Max(0,ChoreoTime-6.51f));
            CounterApproach(ChoreoTime-4.55f,2.48f,LandingSlashTarget(),4);
        }
    }
    Vector3 LandingSlashTarget()
    {
        var previousBoard=board;Vector2 previousOffset=deckOffset;Vector3 previousForward=localForward;
        Vector3 position=hero.transform.position;Quaternion rotation=hero.transform.rotation;
        Vector3 target;
        try
        {
            board=hopTarget;
            deckOffset=new Vector2(hopLandingLocal.x*board.Dimensions.x,hopLandingLocal.z*board.Dimensions.z);
            FaceOpenWater(board.transform.TransformPoint(hopLandingLocal));
            Vector3 feet=Deck(.15f);
            target=feet+Quaternion.LookRotation(Forward)*PredictSlashTip(feet);
        }
        finally
        {
            board=previousBoard;deckOffset=previousOffset;localForward=previousForward;
            hero.transform.SetPositionAndRotation(position,rotation);EvaluatePose();
            if(activeAnimation==null || !activeAnimation.StartsWith("Jump airborne"))PlantFeet(position+Vector3.up*hero.LowestFootWorldOffset,false);
        }
        return target;
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
        else if(t==8.94f)throwTarget=ProjectileHeadAim(t-8.32f);
        else if(t==9.8f)CatchStick();
        else if(t==14.572f)tailAnchor=board.transform.InverseTransformPoint(PredictGuardWeaponTarget(Deck(-.3f)));
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
        if(projectile!=null && !HasLostStick && t>=8.32f && t<=8.94f)
        {
            // Flight samples follow the final solved head, after timber
            // correction, rather than a speculative pre-collision target.
            SpinProjectile(Vector3.Lerp(throwStart,ProjectileHeadAim(t-8.32f),Ease((t-8.32f)/.62f)),t-8.32f);
        }
        for(int i=0;i<contactTimes.Length;i++)
        {
            if(verifiedContacts.Contains(i) || Mathf.Abs(t-contactTimes[i])>Mathf.Max(.01f,contacts[i].window))continue;
            bool counter=i<4;
            Vector3 a=counter ? i==3 && projectile!=null ? projectile.TransformPoint(stickMesh.bounds.center) : WeaponTip : i==4 ? GuardContactPoint : animator.GetBoneTransform(HumanBodyBones.Chest).position;
            Vector3 b=counter ? shark.NoseWorldPoint : shark.TailWorldPoint;
            Vector3 contactPoint=(a+b)*.5f;
            // Relative swept markers allow a fast real crossing between rendered samples.
            Vector3 relative=a-b,previous=lastContactA[i]-lastContactB[i];
            float gap=relative.magnitude;
            if(counter && battle.WreckContact.HeadVolume!=null && (i==3 ? projectileShape : weapon)!=null)
                gap=Story1CapsuleContact.Gap(i==3 ? projectileShape : weapon,battle.WreckContact.HeadVolume,out contactPoint);
            else if(!counter && (i==4 ? weapon : torso)!=null)
                gap=Story1CapsuleContact.PointGap(b,i==4 ? weapon : torso,out contactPoint);
            if(lastContactTime[i]>0 && t-lastContactTime[i]<=.12f && Vector3.Distance(a,lastContactA[i])<3 && Vector3.Distance(b,lastContactB[i])<3)
            {
                Vector3 step=relative-previous;float u=step.sqrMagnitude>.000001f ? Mathf.Clamp01(-Vector3.Dot(previous,step)/step.sqrMagnitude) : 0;
                gap=Mathf.Min(gap,(previous+step*u).magnitude);
            }
            lastContactA[i]=a;lastContactB[i]=b;lastContactTime[i]=t;
            if(gap<ContactAttemptGaps[i])ContactObstructions[i]="correction="+battle.WreckContact.LastCorrection+" hero="+a+" shark="+b+" aim="+attackAim+" endpoint="+attackEndpointCost+" route="+attackRouteCost+" heading="+attackFacing.eulerAngles+" shaft="+slashShaftFraction+" footprint="+supportFootprintRadius+" stance="+chosenStancePoseCost+" candidates="+stanceCandidateCount+" obstacles="+string.Join(";",battle.WreckContact.LastObstacles);
            ContactAttemptGaps[i]=Mathf.Min(ContactAttemptGaps[i],gap);
            if(gap>.05f)continue;
            verifiedContacts.Add(i);ContactGaps.Add(gap);LargestContactGap=Mathf.Max(LargestContactGap,gap);
            if(counter){HeroCounterCount++;if(i==3)throwTarget=ProjectileHeadAim(t-8.32f);}
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
            battle.WreckContact.ResolvePose();AlignRightHandContact();VerifyFinalContact();
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
            Story1CapsuleContact.Segment(torso,out var a,out var b,out _);
            // Strike the upper chest inside the actual hurtbox, leaving the
            // tail's thickness clear of the support mesh underneath it.
            return a.y>b.y ? a : b;
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
    Vector3 ProjectileHeadAim(float age)
    {
        Vector3 aim=shark.NoseWorldPoint;
        if(battle.WreckContact.HeadVolume!=null)
        {
            Story1CapsuleContact.Segment(battle.WreckContact.HeadVolume,out var a,out var b,out float radius);
            Vector3 axis=b-a,nearest=a+axis*Mathf.Clamp01(Vector3.Dot(aim-a,axis)/Mathf.Max(.000001f,axis.sqrMagnitude));
            aim=nearest+Vector3.ClampMagnitude(aim-nearest,radius);
        }
        if(projectileShape==null || projectile==null)return aim;
        Vector3 localCentre=projectile.InverseTransformPoint(projectileShape.transform.TransformPoint(projectileShape.center));
        Quaternion rotation=Quaternion.AngleAxis(age*620,Side)*projectileRotation;
        return aim+rotation*Vector3.Scale(stickMesh.bounds.center-localCentre,projectile.lossyScale);
    }
    void AlignRightHandContact()
    {
        AlignTailSkinContact();
        if(weapon==null || battle.WreckContact.HeadVolume==null || HasLostStick || projectile!=null)return;
        float time=FilmTime;
        // Let the held right-hand guard receive the actually solved tail,
        // including a small moving-deck mismatch. The arm reaches within
        // its real two-bone length; feet, shark and hitboxes are unchanged.
        // A farther miss still produces no contact or stagger.
        float guardAge=time-contactTimes[4];
        if(guardAge>=-.2f && guardAge<=.16f)
        {
            float guardGap=Story1CapsuleContact.PointSeparation(shark.TailWorldPoint,weapon,out var guardDelta);
            if(guardGap>.001f && guardGap<=.4f)
            {
                float blend=guardAge<=0 ? Ease((guardAge+.2f)/.2f) : 1-Ease(guardAge/.16f);
                var guardHand=animator.GetBoneTransform(HumanBodyBones.RightHand);Quaternion guardRotation=guardHand.rotation;
                SahurSupportPose.Hand(animator,false,guardHand.position+guardDelta*blend,1);
                guardHand.rotation=guardRotation;
            }
            return;
        }
        int index=time<1.8f ? 0 : time<4 ? 1 : time<7.8f ? 2 : -1;
        if(index<0)return;
        float distance=time-contactTimes[index];
        if(distance<-.2f || distance>.16f)return;
        float gap=Story1CapsuleContact.Separation(weapon,battle.WreckContact.HeadVolume,out var delta);
        if(gap<.001f || gap>.25f)return;
        float weight=distance<=0 ? Ease((distance+.2f)/.2f) : 1-Ease(distance/.16f);
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);Quaternion rotation=hand.rotation;
        Vector3 target=hand.position+Vector3.ClampMagnitude(delta*weight,.25f);
        SahurSupportPose.Hand(animator,false,target,1);hand.rotation=rotation;
    }
    void AlignTailSkinContact()
    {
        if(contactTailBone==null || torso==null || defeatContactTime>=0)return;
        float age=FilmTime-contactTimes[5];
        if(age<-.2f || age>.16f)return;
        Vector3 point=shark.TailWorldPoint;
        float gap=Story1CapsuleContact.PointSeparation(point,torso,out var separation);
        if(gap<=.001f || gap>.15f)return;
        float weight=age<=0 ? Ease((age+.2f)/.2f) : 1-Ease(age/.16f);
        Vector3 target=point-separation*weight;
        Vector3 from=point-contactTailBone.position,to=target-contactTailBone.position;
        if(from.sqrMagnitude<.001f || to.sqrMagnitude<.001f)return;
        Quaternion previous=contactTailBone.rotation;
        Quaternion rotation=Quaternion.FromToRotation(from,to)*previous;
        contactTailBone.rotation=Quaternion.RotateTowards(previous,rotation,5*weight);
        Vector3 corrected=shark.TailWorldPoint,along=corrected-point;
        bool clear=along.magnitude<=.15f;
        // Flex the real caudal skin through a small residual moving-deck
        // mismatch. Preserve the solved root and body, and reject any fin
        // path crossing solid timber; a farther miss still cannot defeat him.
        bool Intersects(Story1WreckMeshCollision shape)
        {
            if(shape==null || !shape.Active || shape.WorldBounds.SqrDistance((point+corrected)*.5f)>Mathf.Pow(along.magnitude*.5f+.02f,2))return false;
            return along.sqrMagnitude>.000001f && shape.Raycast(new Ray(point,along.normalized),out _,along.magnitude+.01f) ||
                Vector3.Distance(shape.ClosestPoint(corrected),corrected)<.02f;
        }
        foreach(var plank in battle.Boards)if(!plank.IsBroken && Intersects(plank.Collision)){clear=false;break;}
        if(clear)foreach(var detail in battle.WreckDetails)if(!detail.IsBroken && Intersects(detail.Collision)){clear=false;break;}
        if(!clear)contactTailBone.rotation=previous;
        else
        {
            LargestTailSkinAdjustment=Mathf.Max(LargestTailSkinAdjustment,along.magnitude);
            LargestTailSkinRotation=Mathf.Max(LargestTailSkinRotation,Quaternion.Angle(previous,contactTailBone.rotation));
        }
    }
    void CatchStick(){if(projectile!=null)Destroy(projectile.gameObject);projectile=null;if(held!=null && !HasLostStick)for(int i=0;i<held.Length;i++)if(held[i]!=null)held[i].enabled=heldEnabled[i];}
    void LateUpdate()
    {
        if(!initialized || battle==null)return;
        // Keep the final fallen pose through blackout and scene loading. The
        // Animator still evaluates each frame and otherwise restores locomotion.
        // Keep the pose frozen while buoyancy follows the actual moving water.
        if(IsComplete){EvaluatePose();FloatOnWaves();return;}
        if(PauseSettingsMenu.IsOpen || Time.timeScale<=0 || Time.deltaTime<=0)
        {
            // Physics interpolation may settle on a different endpoint in
            // the first paused frame. Preserve the last final, supported
            // pose rather than solving traversal/retargeting again against
            // that new endpoint while the animation clock is frozen.
            if(hasRetainedHeroPose)
            {
                hero.transform.SetPositionAndRotation(retainedHeroRoot,retainedHeroHeading);
                for(int i=0;i<retainedHeroBones.Length;i++)
                {
                    retainedHeroBones[i].localPosition=retainedHeroPositions[i];
                    retainedHeroBones[i].localRotation=retainedHeroRotations[i];
                }
            }
            else RenderActors();
            return;
        }
        // Animator's engine evaluation happens after Update. Sample the manual graph after it.
        Advance();
        FloatOnWaves();
        if(IsComplete)return;
    }
    public void ResolveContactsAndCamera()
    {
        if(!initialized || battle==null || IsComplete)return;
        if(PauseSettingsMenu.IsOpen || Time.timeScale<=0 || Time.deltaTime<=0)return;
        if(!PauseSettingsMenu.IsOpen && Time.deltaTime>0){AlignRightHandContact();VerifyFinalContact();}
        retainedHeroRoot=hero.transform.position;retainedHeroHeading=hero.transform.rotation;
        for(int i=0;i<retainedHeroBones.Length;i++)
        {
            retainedHeroPositions[i]=retainedHeroBones[i].localPosition;
            retainedHeroRotations[i]=retainedHeroBones[i].localRotation;
        }
        hasRetainedHeroPose=true;
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

