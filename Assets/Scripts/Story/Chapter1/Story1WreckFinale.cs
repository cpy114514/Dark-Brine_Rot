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
    public float LargestRightHandContactAdjustment { get; private set; }
    public readonly float[] ContactAttemptGaps={999,999,999,999,999,999};
    public readonly string[] ContactObstructions=new string[6];
    public readonly System.Collections.Generic.List<string> EntryAttempts=new System.Collections.Generic.List<string>();
    public int EntryPlanningQueries { get; private set; }
    public string EntryPlanningProgress { get; private set; }
    public string EntrySelectedBoard { get; private set; }
    public float EntryPoseCost { get; private set; }
    public float EntryWoodCost { get; private set; }
    public float EntryPreparationSeconds { get; private set; }
    public int EntrySearchRetryCount { get; private set; }
    public bool EntryFromWater { get; private set; }
    public float EntrySharkStagingDistance { get; private set; }=2;
    public bool EntrySharkStagingClear { get; private set; }
    public float EntrySharkAdmissionDelay { get; private set; }
    double entryPreparationStarted;
    public const float Duration=20.8f;
    static readonly Unity.Profiling.ProfilerMarker EntryPlanningMarker=new Unity.Profiling.ProfilerMarker("Wreck.EntryPlanning");
    static readonly Unity.Profiling.ProfilerMarker FacingPlanningMarker=new Unity.Profiling.ProfilerMarker("Wreck.AttackFacing");
    static readonly float[] attackPathPhases={.1f,.25f,.5f,.75f,.9f};
    sealed class AttackPlanningPose
    {
        public Story1SharkWreckContact.AnatomyPose contact;
        public Story1SharkWreckContact.AnatomyPose[] path;
        public Vector3 marker,bulk;
    }
    readonly System.Collections.Generic.Dictionary<string,AttackPlanningPose> attackPlanningPoses=new System.Collections.Generic.Dictionary<string,AttackPlanningPose>();
    struct FacingCandidate{public Quaternion rotation;public float endpoint,bias;public bool clear;}
    readonly System.Collections.Generic.List<FacingCandidate> facingCandidates=new System.Collections.Generic.List<FacingCandidate>(160);
    int facingSearchCursor;
    public int FacingSearchExtensions { get; private set; }
    public int FacingBudgetYields { get; private set; }
    public int BiteTransitionPenaltyCount { get; private set; }
    public float LargestBiteTransitionExcess { get; private set; }
    public int TailTransitionPenaltyCount { get; private set; }
    public float LargestTailTransitionExcess { get; private set; }
    // Nonserialized so existing scenes cannot override the runtime slice.
    double routeAlternativeBudgetSeconds=.003;
    readonly Vector3[] routeOffsets=new Vector3[13];
    struct PlannedFeet{public Vector3 left,right;public float time;}
    readonly System.Collections.Generic.Dictionary<AnimationClip,PlannedFeet[]> plannedFeet=new System.Collections.Generic.Dictionary<AnimationClip,PlannedFeet[]>();
    static readonly int[] bothSides={1,-1},oneSide={1};
    static readonly float[] facingYaws={0,-20,20,-40,40,-65,65,-90,90,-120,120,-150,150,180};
    static readonly float[] tailPitches={-25,-16,0,12,25},bitePitches={-35,-25,-16,0,12,25,35};
    public int BoardJumpCount { get; private set; }
    public int SharkBreachCount { get; private set; }
    public float LargestLandingError { get; private set; }
    public float LargestBreachClearance { get; private set; }
    public float CinematicTime=>FilmTime;
    public float LeadInSeconds { get; private set; }=.6f;
    // Sample crossed cues at their actual sub-frame time, then render the end-of-frame pose.
    static readonly float[] Cues={8.32f,8.94f,9.8f,14.572f,18.05f,18.22f};
    Story1EncounterBeat[] beats;
    Story1EncounterShot[] shots;
    readonly System.Collections.Generic.Dictionary<string,AnimationClip> revisedClips=new System.Collections.Generic.Dictionary<string,AnimationClip>();
    readonly System.Collections.Generic.HashSet<int> verifiedContacts=new System.Collections.Generic.HashSet<int>();
    float[] contactTimes={1.184f,2.43f,7.03f,8.94f,14.572f,18.05f};
    float[] timelineCues;
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
    SkinnedMeshRenderer[] heroSkins;
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
    Vector3 startFeet,initialNose,stageBodyStart,stageNoseStart,throwStart,throwTarget,lostStart,lostVelocity,fallStart;
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
    Vector3 intendedTailRoot;
    float tailRouteStartAge;
    float lastTailReplanAge=-100;
    int tailStrokeReplans;
    bool hasIntendedTailRoot;
    public int TailRouteReplans { get; private set; }
    Vector3 intendedBiteRoot;
    float biteRouteStartAge,lastBiteReplanAge=-100;
    int biteStrokeReplans;
    bool hasIntendedBiteRoot;
    public int BiteRouteReplans { get; private set; }
    public int ClearRouteEarlyExits { get; private set; }
    public int PreferredClearRouteEarlyExits { get; private set; }
    public int ExactBlockedRoutePasses { get; private set; }
    Vector3 leftSole,rightSole;
    Quaternion projectileRotation,stageRotation;
    Quaternion initialHeroRotation;
    float swimSeconds,soleOffset;
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
    bool preparingEntry;
    Story1WreckChaseEntry entryChase;
    bool chaseDockingStarted;
    bool unconsciousExit;
    bool unconsciousExitInWater;
    float unconsciousExitStart;
    Vector3 unconsciousExitFeet,unconsciousExitVelocity;
    Quaternion unconsciousExitHeading;
    public string ExitReason { get; private set; }
    Vector3 slicedEntryPoint,heldEntryFeetLocal;
    Quaternion heldEntryHeadingLocal;
    float heldEntryWater,chosenStanceScore;
    Vector3 heldEntryRoot;
    Quaternion heldEntryHeading;
    Vector3 floatingChest,floatDrift;
    Vector3 floatingWoodOffset;
    CapsuleCollider floatingProbe;
    CapsuleCollider[] floatingShapes;
    public float FloatingWoodCorrection { get; private set; }
    Quaternion floatHeading,floatRotation;
    float floatHeightVelocity;
    Vector2 deckOffset;
    Vector3 localForward=Vector3.forward;
    float walkSeconds;
    Story1WreckPlank hopFrom,hopTarget;
    Vector3 hopStartLocal,hopTakeoffLocal,hopLandingLocal,hopLandingForwardLocal;
    int hopIndex=-1,breachSection=-1;
    bool hopLanded;
    struct HopPlan{public Story1WreckPlank target;public Vector3 landingLocal,forwardLocal;}
    readonly HopPlan[] hopPlans=new HopPlan[2];
    readonly bool[] hopPlanningStarted=new bool[2];
    readonly float[] hopRetryAfter=new float[2];
    public readonly int[] HopPlanningAttempts=new int[2];
    public readonly System.Collections.Generic.List<string> HopPlanningDiagnostics=new System.Collections.Generic.List<string>();
    bool hopPlanningBusy;
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
    public bool CanBreakBoards=>activeAnimation!=null && activeAnimation.StartsWith("Jump airborne") ||
        (FilmTime>=11.6f && FilmTime<13.322f && shark.CinematicAction=="Breach") ||
        (FilmTime>=14.42f && FilmTime<14.9f) || (FilmTime>=17.9f && FilmTime<18.3f);
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
        using var measurement=new Unity.Profiling.ProfilerMarker("Wreck.InitializeFilm").Auto();
        battle=encounter;hero=player;shark=enemy;shotCamera=camera;board=encounter.EntryDeck!=null && !encounter.EntryDeck.IsBroken ? encounter.EntryDeck : support;ocean=water;fromWater=true;
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
        heroSkins=hero.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        sampledSkins=heroSkins.Concat(shark.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Distinct().ToArray();
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
        swim=Story1EncounterClipCache.Find("PLAYER_PursuitSwim_v210") ?? Story1EncounterClipCache.Find("PLAYER_SwimFast_v007") ?? Find("SahurFastFreestyle");
        leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        heldEntryRoot=hero.transform.position;heldEntryHeading=hero.transform.rotation;heldEntryWater=Water(heldEntryRoot);
        heldEntryFeetLocal=board.transform.InverseTransformPoint(startFeet);
        heldEntryHeadingLocal=Quaternion.Inverse(board.transform.rotation)*hero.transform.rotation;
        entryStartFeetLocal=board.transform.InverseTransformPoint(startFeet);
        Vector3 landing=board.BoardingPoint(startFeet);
        entryLandingLocal=board.transform.InverseTransformPoint(landing+Vector3.up*.04f);
        entryGripEdgeLocal=board.transform.InverseTransformPoint(landing);
        entryOutwardLocal=board.transform.InverseTransformDirection(board.OpenWaterDirection(landing));
        // This is only the initial pose frame. The budgeted preparation below
        // chooses and validates the actual stance after the graph is ready.
        // Searching every edge synchronously here repeated the same mesh-ray
        // work at handoff, before it could produce a usable combat candidate.
        Vector3 edge=fromWater ? landing : startFeet;
        Vector3 localEdge=board.transform.InverseTransformPoint(edge);
        deckOffset=new Vector2(localEdge.x*board.Dimensions.x,localEdge.z*board.Dimensions.z);FaceOpenWater(edge);
        initialHeroRotation=hero.transform.rotation;
        soleOffset=Mathf.Min(leftFoot.position.y,rightFoot.position.y)-startFeet.y;
        leftSole=leftFoot.InverseTransformVector(Vector3.down*soleOffset);rightSole=rightFoot.InverseTransformVector(Vector3.down*soleOffset);
        initialNose=shark.NoseWorldPoint;stageRotation=shark.transform.rotation;
        weapon=hero.GetComponent<SahurAttack>().stickHitbox as CapsuleCollider;
        torso=actorColliders.OfType<CapsuleCollider>().FirstOrDefault(c=>c.name=="BodyHitbox_Torso");
        floatingShapes=actorColliders.OfType<CapsuleCollider>().Where(c=>c.name.StartsWith("BodyHitbox_",System.StringComparison.Ordinal)).ToArray();
        var floatQuery=new GameObject("Unconscious body clearance query");floatQuery.transform.SetParent(transform,false);
        Vector3 queryScale=transform.lossyScale;
        floatQuery.transform.localScale=new Vector3(1/queryScale.x,1/queryScale.y,1/queryScale.z);
        floatQuery.transform.position=Vector3.down*10000;floatQuery.layer=2;
        floatingProbe=floatQuery.AddComponent<CapsuleCollider>();floatingProbe.direction=2;floatingProbe.isTrigger=true;
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
        shots=definition!=null && definition.shots!=null && definition.shots.Length>0 ? definition.shots : Story1EncounterDefinition.ApprovedShots();
        beats=(Story1EncounterBeat[])(definition!=null ? definition.beats : Story1EncounterDefinition.ApprovedBeats()).Clone();
        // Historical serialized timelines may still contain the retired
        // driving pose. Keep the beat's timing as ordinary grounded defence,
        // including when loading an older definition, without mutating it.
        string standingGuard=beats.First(b=>b.action==Story1EncounterAction.Guard).clip;
        for(int i=0;i<beats.Length;i++)if(beats[i].action==Story1EncounterAction.Surf)
            beats[i]=new Story1EncounterBeat("Keep footing as the bite passes",beats[i].start,beats[i].end,Story1EncounterAction.Guard,standingGuard);
        for(int i=0;i<beats.Length;i++)if(beats[i].action==Story1EncounterAction.Throw && beats[i].clip=="PLAYER_Throw_v006")
            beats[i]=new Story1EncounterBeat(beats[i].label,beats[i].start,beats[i].end,beats[i].action,"PLAYER_Throw_v064");
        contacts=definition!=null && definition.contacts!=null && definition.contacts.Length==6 ? definition.contacts : Story1EncounterDefinition.ApprovedContacts();
        contactTimes=contacts.Select(c=>c.time).ToArray();
        // Contact windows are immutable for this encounter. Avoid rebuilding
        // a sorted LINQ/hash-set pipeline every rendered timeline frame.
        timelineCues=Cues.Concat(contactTimes).Distinct().OrderBy(c=>c).ToArray();
        AnimationClip Revised(string name)=>Story1EncounterClipCache.Find(name) ?? throw new System.InvalidOperationException("Missing paired encounter clip: "+name);
        foreach(var beat in beats)if(!revisedClips.ContainsKey(beat.clip))revisedClips.Add(beat.clip,Revised(beat.clip));
        slash=Revised(beats.First(b=>b.action==Story1EncounterAction.Slash).clip);guard=Revised(beats.First(b=>b.action==Story1EncounterAction.Guard).clip);block=Revised(beats.First(b=>b.action==Story1EncounterAction.Brace).clip);
        throwClip=Revised("PLAYER_Throw_v064");stagger=Revised("PLAYER_Stagger_v006");knockdown=Revised("PLAYER_Fall_v006");
        // Older project copies may not contain the v010 export yet. Keep the
        // newest retained Humanoid climb usable instead of aborting the finale.
        climb=new[]{"PLAYER_Climb_v010","PLAYER_Climb_v009","PLAYER_Climb_v006"}
            .Select(Story1EncounterClipCache.Find)
            .FirstOrDefault(c=>c!=null);
        if(climb==null)throw new System.InvalidOperationException("Wreck finale requires a retained PLAYER_Climb animation.");
        // The user restored legacy walk/run/roll, including supported entry
        // and same-board footwork. Use the same actual controller walk clip.
        walk=library.FirstOrDefault(c=>c.name.Contains("Walk_Loop")) ?? Revised("PLAYER_Walk_v006");
        jumpStart=Revised("PLAYER_JumpStart_v006");jumpLoop=Revised("PLAYER_JumpAir_v006");jumpLand=Revised("PLAYER_JumpLand_v006");
        boardJump=beats.Where(b=>b.action==Story1EncounterAction.Hop)
            .Select(b=>revisedClips[b.clip]).FirstOrDefault(c=>c.name.Contains("BoardHop"))
            ?? Story1EncounterClipCache.Find("CH1_SahurBoardHop_v047");
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
        // A nearby fragment can carry the swimmer's hand without having
        // room for both authored combat feet. Choose another real reachable
        // surface before the swim/climb lead-in; never move a fragment.
        preparingEntry=true;
        entryChase=gameObject.AddComponent<Story1WreckChaseEntry>();
        entryChase.Initialize(battle,hero,shark,shotCamera,ocean,!swimming,swim,boardJump);
        EntryFromWater=fromWater;entryPreparationStarted=Time.realtimeSinceStartupAsDouble;
        StartCoroutine(PrepareEntry(landing));
    }
    System.Collections.IEnumerator PrepareEntry(Vector3 landing)
    {
        Vector3 landingLocal=board.transform.InverseTransformPoint(landing);
        if(plannedFeet.Count==0)yield return CacheSupportedFeet();
        landing=board.transform.TransformPoint(landingLocal);
        Vector3? selectedEntryEdge=null;
        if(fromWater)
        {
            Vector3 afterClimb=board.BoardingPoint(landing-board.OpenWaterDirection(landing)*(supportFootprintRadius+.3f));
            yield return SearchEntryEdgeSlices(board,afterClimb,refine:false);
            if(stanceCandidateCount==0)
                yield return SearchEntryEdgeSlices(board,board.BoardingPoint(board.GetComponent<Rigidbody>().worldCenterOfMass),refine:false);
            EntryAttempts.Add(board.name+" candidates="+stanceCandidateCount+" wood="+chosenStanceWoodPenetration+" pose="+chosenStancePoseCost);
            if(stanceCandidateCount>0 && chosenStanceWoodPenetration<=.0004f)selectedEntryEdge=slicedEntryPoint;
            if(stanceCandidateCount==0 || chosenStanceWoodPenetration>.0004f)
            {
                // Search only real surfaces within a bounded swim. Each
                // candidate is spread over render frames, with the actual
                // entering pose restored between hypothetical samples.
                var candidates=battle.Boards.Where(p=>p!=board && p.IsBoardable && p.Dimensions.y<1.2f && Mathf.Abs(p.transform.up.y)>.82f && p.Speed<5)
                    .Where(p=>Mathf.Min(p.Dimensions.x,p.Dimensions.z)>supportFootprintRadius*2+.2f && p.EdgeDistance(startFeet)<60)
                    .OrderBy(p=>p.EdgeDistance(startFeet)).Take(8).ToArray();
                for(int pass=0;pass<2 && !selectedEntryEdge.HasValue;pass++)
                foreach(var candidate in pass==0 ? candidates : new[]{board}.Concat(candidates))
                {
                    Vector3 at=candidate.BoardingPoint(startFeet);
                    if(Water(at)-at.y>.6f || at.y-Water(at)>2.5f)continue;
                    Vector3 gripLocal=candidate.transform.InverseTransformPoint(at);
                    Vector3 climbed=candidate.BoardingPoint(at-candidate.OpenWaterDirection(at)*(supportFootprintRadius+.3f));
                    Vector3 climbedLocal=candidate.transform.InverseTransformPoint(climbed);
                    yield return SearchEntryEdgeSlices(candidate,climbed,refine:pass==1);
                    if(stanceCandidateCount==0)
                        yield return SearchEntryEdgeSlices(candidate,candidate.BoardingPoint(candidate.GetComponent<Rigidbody>().worldCenterOfMass),refine:pass==1);
                    EntryAttempts.Add(candidate.name+" candidates="+stanceCandidateCount+" wood="+chosenStanceWoodPenetration+" pose="+chosenStancePoseCost);
                    if(stanceCandidateCount==0 || chosenStanceWoodPenetration>.0004f)continue;
                    at=candidate.transform.TransformPoint(gripLocal);
                    if(!SupportedEntryWalk(candidate,candidate.transform.TransformPoint(climbedLocal),slicedEntryPoint))
                    {EntryAttempts.Add(candidate.name+" rejected: actual entry walk crosses a mesh edge");continue;}
                    if(supportBody!=null)supportBody.angularDamping=originalDamping;
                    board=candidate;supportBody=board.GetComponent<Rigidbody>();originalDamping=supportBody.angularDamping;supportBody.angularDamping=6;
                    board.Drive(0,0,null);landing=at;selectedEntryEdge=slicedEntryPoint;
                    landingLocal=gripLocal;
                    entryStartFeetLocal=board.transform.InverseTransformPoint(startFeet);
                    entryGripEdgeLocal=board.transform.InverseTransformPoint(landing);
                    entryOutwardLocal=board.transform.InverseTransformDirection(board.OpenWaterDirection(landing));
                    break;
                }
            }
        }
        if(fromWater)
        {
            landing=board.transform.TransformPoint(landingLocal);
            Vector3 outward=board.transform.TransformDirection(entryOutwardLocal);
            landing=board.BoardingPoint(landing-outward*(supportFootprintRadius+.3f));
            entryLandingLocal=board.transform.InverseTransformPoint(landing+Vector3.up*.04f);
        }
        if(!selectedEntryEdge.HasValue && !fromWater)yield return SearchEntryEdgeSlices(board,startFeet);
        if(fromWater && !selectedEntryEdge.HasValue || stanceCandidateCount==0 || chosenStanceWoodPenetration>.0004f || float.IsInfinity(chosenStancePoseCost))
        {
            // A transient wave/tilt or an initially distant deck must not
            // cancel the film on the first search. Keep the visible chase,
            // then retry from the swimmer's actual updated position.
            if(entryChase!=null && entryChase.Age<12 && EntrySearchRetryCount<2)
            {
                EntrySearchRetryCount++;yield return new WaitForSeconds(.25f);
                startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
                yield return PrepareEntry(board.BoardingPoint(startFeet));yield break;
            }
            BeginUnconsciousExit("No reachable supported combat surface after pursuit");yield break;
        }
        Vector3 edge=selectedEntryEdge ?? slicedEntryPoint;
        Vector3 localEdge=board.transform.InverseTransformPoint(edge);
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
        HoldEntryPose();
        startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        entryStartFeetLocal=board.transform.InverseTransformPoint(startFeet);
        EntrySelectedBoard=board.name;EntryPoseCost=chosenStancePoseCost;EntryWoodCost=chosenStanceWoodPenetration;
        entryChase.SetSupport(board);
        yield return PlanSharkEntry();
        while(entryChase.Age<1.15f)yield return null;
        LeadInSeconds=Mathf.Max(LeadInSeconds,entrySharkApproachSeconds);
        var post=gameObject.AddComponent<Story1FinalePostProcess>();post.film=this;
        // Search is visible pursuit, not a held pose. Begin the final swim
        // and moving-edge climb from the actor's ACTUAL position now.
        HoldEntryPose();entryChase.ReleaseHeroPose();
        startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        initialHeroRotation=hero.transform.rotation;
        entryHeroPositions=retainedHeroBones.Select(b=>b.localPosition).ToArray();
        entryHeroRotations=retainedHeroBones.Select(b=>b.localRotation).ToArray();
        entryStartFeetLocal=board.transform.InverseTransformPoint(startFeet);
        Vector3 chaseEntry=board.transform.TransformPoint(entryLandingLocal)+board.transform.TransformDirection(entryOutwardLocal)*.8f;
        swimSeconds=Mathf.Max(Mathf.Max(.5f,6-entryChase.Age),Vector3.ProjectOnPlane(startFeet-chaseEntry,Vector3.up).magnitude/(hero.swimSpeed*hero.fastSwimMultiplier));
        LeadInSeconds=Mathf.Max(swimSeconds+1.15f+entryWalkSeconds+.4f,entrySharkApproachSeconds);
        preparingEntry=false;RenderActors();initialized=true;
        EntryPreparationSeconds=(float)(Time.realtimeSinceStartupAsDouble-entryPreparationStarted);
    }
    System.Collections.IEnumerator CacheSupportedFeet()
    {
        var parry=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Parry).clip];
        double slice=Time.realtimeSinceStartupAsDouble;
        foreach(var clip in new[]{guard,parry,slash,block,throwClip,walk})
        {
            int frames=Mathf.CeilToInt(clip.length*60);var samples=new PlannedFeet[frames+1];
            var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);
            mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);
            for(int frame=0;frame<=frames;frame++)
            {
                float time=Mathf.Min(clip.length-.00001f,frame/60f);sample.SetTime(time);poseGraph.Evaluate(0);
                Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
                samples[frame]=new PlannedFeet{left=hero.transform.InverseTransformVector(leftFoot.TransformPoint(leftSole)-feet),right=hero.transform.InverseTransformVector(rightFoot.TransformPoint(rightSole)-feet),time=time};
                if(Time.realtimeSinceStartupAsDouble-slice>=.002)
                {
                    yield return null;
                    while(PauseSettingsMenu.IsOpen || Time.timeScale<=0)yield return null;
                    mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);slice=Time.realtimeSinceStartupAsDouble;
                }
            }
            mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);plannedFeet.Add(clip,samples);
        }
        mixer.ConnectInput(1,pose,0);EvaluatePose();
    }
    System.Collections.IEnumerator SearchEntryEdgeSlices(Story1WreckPlank support,Vector3 feet,Vector3? jumpFrom=null,float maximumTravel=12,bool refine=true,float stopAtFilm=float.PositiveInfinity)
    {
        Vector3 localFeet=support.transform.InverseTransformPoint(feet),bestLocal=localFeet;
        float best=float.NegativeInfinity,bestPose=float.PositiveInfinity,bestWood=float.PositiveInfinity;
        int accepted=0;double sliceStart=Time.realtimeSinceStartupAsDouble;
        var refinements=new System.Collections.Generic.List<(Vector3 local,float footprint,float score,float cost)>();
        var edges=new System.Collections.Generic.List<(Vector3 local,float footprint,float score)>();
        foreach(float footprint in new[]{supportFootprintRadius,.6f})
        for(int angle=0;angle<360;angle+=15)
        {
            while(PauseSettingsMenu.IsOpen || Time.timeScale<=0)yield return null;
            if(support==null || support.IsBroken || FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
            Vector3 point=FindEntryEdge(support,support.transform.TransformPoint(localFeet),jumpFrom,maximumTravel,angle,angle+15,footprint,false);
            if(stanceCandidateCount>0)edges.Add((support.transform.InverseTransformPoint(point),footprint,chosenStanceScore));
            if(Time.realtimeSinceStartupAsDouble-sliceStart>=.002)
            {
                yield return null;sliceStart=Time.realtimeSinceStartupAsDouble;
            }
        }
        // Rank all cheap real-support/open-water checks before the expensive
        // anatomical tests. The former angle-order early exit chose an inner
        // wreck seam even when a later edge offered an open swimming lane.
        foreach(var edge in edges.OrderByDescending(candidate=>candidate.score))
        {
            while(PauseSettingsMenu.IsOpen || Time.timeScale<=0)yield return null;
            if(support==null || support.IsBroken || FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
            Vector3 point=support.transform.TransformPoint(edge.local);
            yield return GradeEntryCandidateSlices(support,point,edge.footprint,edge.score,false,stopAtFilm,requireTailContacts:jumpFrom.HasValue);
            if(FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
            if(stanceCandidateCount>0 && chosenStanceWoodPenetration>.0004f)
                refinements.Add((edge.local,edge.footprint,edge.score,chosenStancePoseCost));
            if(support==null || support.IsBroken){stanceCandidateCount=0;yield break;}
            point=support.transform.TransformPoint(edge.local);
            bool candidateClear=chosenStanceWoodPenetration<=.0004f && !float.IsInfinity(chosenStancePoseCost);
            bool bestClear=bestWood<=.0004f && !float.IsInfinity(bestPose);
            // A prettier blocked edge cannot outrank a physically valid
            // edge. Weighted style scores alone could retain a blocked best
            // and report no surface despite a later clear candidate.
            if(stanceCandidateCount>0 && (candidateClear && !bestClear || candidateClear==bestClear && chosenStanceScore>best))
            {
                accepted+=stanceCandidateCount;best=chosenStanceScore;bestLocal=support.transform.InverseTransformPoint(point);
                bestPose=chosenStancePoseCost;bestWood=chosenStanceWoodPenetration;
            }
            // A clear high-ranked edge needs no further style refinement.
            if(bestWood<=.0004f && !float.IsInfinity(bestPose))
            {
                slicedEntryPoint=support.transform.TransformPoint(bestLocal);stanceCandidateCount=accepted;
                chosenStancePoseCost=bestPose;chosenStanceWoodPenetration=bestWood;chosenStanceScore=best;yield break;
            }
            if(Time.realtimeSinceStartupAsDouble-sliceStart>=.002)
            {
                yield return null;sliceStart=Time.realtimeSinceStartupAsDouble;
            }
        }
        foreach(var trial in refine ? refinements.OrderBy(t=>t.cost).ThenByDescending(t=>t.score) : Enumerable.Empty<(Vector3 local,float footprint,float score,float cost)>())
        {
            if(support==null || support.IsBroken || FilmTime>=stopAtFilm)break;
            yield return GradeEntryCandidateSlices(support,support.transform.TransformPoint(trial.local),trial.footprint,trial.score,stopAtFilm:stopAtFilm,requireTailContacts:jumpFrom.HasValue);
            if(FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
            if(stanceCandidateCount==0 || chosenStanceWoodPenetration>.0004f)continue;
            slicedEntryPoint=support.transform.TransformPoint(trial.local);
            yield break;
        }
        if(FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
        slicedEntryPoint=support!=null ? support.transform.TransformPoint(bestLocal) : feet;
        stanceCandidateCount=accepted;chosenStancePoseCost=bestPose;chosenStanceWoodPenetration=bestWood;chosenStanceScore=best;
    }
    struct EntryContactGeometry
    {
        public Vector3 first,last;
        public Story1SharkWreckContact.AnatomyPose anatomy;
        public bool tail,powerful;
    }
    System.Collections.IEnumerator GradeEntryCandidateSlices(Story1WreckPlank support,Vector3 point,float footprint,float geometricScore,bool refine=true,float stopAtFilm=float.PositiveInfinity,bool requireTailContacts=false)
    {
        if(FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
        // Admit the opening exchange on real support. The reversal/final tail
        // happen 14/18 seconds later against moving, breakable timber and are
        // planned and contact-tested at that time. Requiring their endpoint
        // poses to clear today's intact deck skipped the entire encounter.
        var previousBoard=board;Vector2 previousOffset=deckOffset;Vector3 previousForward=localForward;
        Vector3 previousPosition=hero.transform.position;Quaternion previousHeading=hero.transform.rotation;
        string sharkAction=shark.CinematicAction;float sharkSeconds=shark.CinematicActionSeconds;
        // A voluntary hop has a supported same-board alternative. Its new
        // destination must also leave space for the ensuing tail exchanges;
        // entry's lighter admission must not weaken this traversal contract.
        var geometry=new EntryContactGeometry[requireTailContacts?4:2];Quaternion localHeading;Vector3 localFeet;
        try
        {
            board=support;Vector3 local=support.transform.InverseTransformPoint(point);
            deckOffset=new Vector2(local.x*support.Dimensions.x,local.z*support.Dimensions.z);FaceOpenWater(point);
            // The standing footprint alone does not include the authored
            // stepping slash. Validate real soles at every used stance even
            // when the full-size circle happened to fit this jagged fragment.
            if(!SupportsContinuousStances()){stanceCandidateCount=0;yield break;}
            localHeading=Quaternion.Inverse(support.transform.rotation)*Quaternion.LookRotation(Forward);
            localFeet=support.transform.InverseTransformPoint(Deck(-.4f));
            for(int i=0;i<(requireTailContacts?3:2);i++)
            {
                var clip=i==0 ? revisedClips[beats.First(b=>b.action==Story1EncounterAction.Parry).clip] : i==1?slash:block;
                Vector3 feet=Deck(i==2?-.3f:-.4f);
                var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);sample.SetTime(i==0 ? .484f : i==1?.63f:.572f);
                mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);
                hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));poseGraph.Evaluate(0);SampleEntryCombatGrip();PlantFeet(feet,false);
                Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
                if(i<2)
                {
                    shark.SampleCinematicAction("Bite_Lunge",.52f);
                    geometry[i]=new EntryContactGeometry{first=support.transform.InverseTransformPoint(a),last=support.transform.InverseTransformPoint(b),anatomy=battle.WreckContact.CaptureAnatomy()};
                }
                else
                {
                    var target=support.transform.InverseTransformPoint(GuardContactPoint);
                    geometry[2]=new EntryContactGeometry{first=target,last=target,anatomy=tailContactAnatomy,tail=true};
                    target=support.transform.InverseTransformPoint(TailContactTarget);
                    geometry[3]=new EntryContactGeometry{first=target,last=target,anatomy=shipContactAnatomy,tail=true,powerful=true};
                }
                mixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);mixer.ConnectInput(1,pose,0);
            }
        }
        finally
        {
            board=previousBoard;deckOffset=previousOffset;localForward=previousForward;
            hero.transform.SetPositionAndRotation(previousPosition,previousHeading);EvaluatePose();
            shark.SampleCinematicAction(sharkAction,sharkSeconds);
        }
        double slice=Time.realtimeSinceStartupAsDouble;float worst=0,wood=0;
        foreach(var contact in geometry)
        {
            float best=float.PositiveInfinity,selectedWood=float.PositiveInfinity;bool clearFound=false;
            for(int tier=0;tier<(refine ? 2 : 1);tier++)
            {
            if(tier==1 && clearFound)break;
            EntryPlanningProgress=support.name+" "+(contact.tail ? contact.powerful ? "strong tail" : "guard tail" : "weapon")+" tier="+tier;
            int points=contact.tail ? 1 : tier==0 ? 3 : 9;
            for(int i=0;i<points;i++)foreach(int sign in contact.tail ? bothSides : oneSide)
            foreach(float yaw in tier==0 ? coarseEntryYaws : entryYaws)foreach(float pitch in tier==0 ? coarseEntryPitches : contact.tail ? entryTailPitches : bitePitches)
            {
                if(support==null || support.IsBroken || FilmTime>=stopAtFilm){stanceCandidateCount=0;yield break;}
                while(PauseSettingsMenu.IsOpen || Time.timeScale<=0)yield return null;
                Quaternion heading=support.transform.rotation*localHeading;
                Vector3 forward=Vector3.ProjectOnPlane(heading*Vector3.forward,Vector3.up).normalized,side=Vector3.Cross(Vector3.up,forward);
                Vector3 marker=support.transform.TransformPoint(Vector3.Lerp(contact.first,contact.last,points==1 ? 0 : (float)i/(points-1)));
                if(!contact.tail && Vector3.Dot(marker-support.transform.TransformPoint(localFeet),support.transform.up)<1.15f)continue;
                Quaternion facing=(contact.tail ? Quaternion.LookRotation((side*sign+forward*.75f).normalized) : Quaternion.LookRotation(-forward))*Quaternion.Euler(pitch,yaw,contact.powerful ? -4 : 0);
                Vector3 bulk=marker-facing*Vector3.Scale((contact.tail ? contact.anatomy.tailMarker : contact.anatomy.nose)-contact.anatomy.body,shark.transform.lossyScale);
                float exposure=Mathf.Max(0,bulk.y-Water(bulk)+1.4f);
                // Preserve every clear candidate and the exact best score.
                // A rejected attitude cannot improve once its partial native
                // cost already exceeds both the clearance and ranking limits.
                float limit=Mathf.Max(.0004f,(best-exposure*exposure*.2f)/10000);
                float penetration=battle.WreckContact.PoseObstructionBounded(contact.anatomy,marker,facing,contact.tail,limit);
                EntryPlanningQueries++;
                bool clear=penetration<=.0004f;
                float cost=penetration*10000+exposure*exposure*.2f;
                if((clear && !clearFound) || (clear==clearFound && cost<best)){best=cost;selectedWood=penetration;clearFound=clear;}
                if(clear)goto ContactGraded;
                if(Time.realtimeSinceStartupAsDouble-slice>=.002){yield return null;slice=Time.realtimeSinceStartupAsDouble;}
            }
            }
            ContactGraded:
            worst=Mathf.Max(worst,best);wood=Mathf.Max(wood,selectedWood);
            // Both opening contacts must be physically reachable. A later
            // missed attack does not authorize a fabricated hit or skip here.
            if(!clearFound)break;
        }
        stanceCandidateCount=1;chosenStancePoseCost=worst;chosenStanceWoodPenetration=wood;
        chosenStanceScore=geometricScore-wood*100000-worst*.2f;
    }
    static readonly float[] entryYaws={0,-40,40,-80,80,-120,120,180},entryTailPitches={-25,-16,0,12,25},coarseEntryYaws={0,-80,80,180},coarseEntryPitches={-25,0,25};
    void HoldEntryPose()
    {
        if(!preparingEntry)return;
        if(entryChase!=null)
        {
            entryChase.RenderSearching();
            heldEntryRoot=hero.transform.position;heldEntryHeading=hero.transform.rotation;heldEntryWater=Water(heldEntryRoot);
            return;
        }
        Vector3 position=fromWater ? heldEntryRoot+Vector3.up*(Water(heldEntryRoot)-heldEntryWater) :
            board.transform.TransformPoint(heldEntryFeetLocal)-Vector3.up*hero.LowestFootWorldOffset;
        hero.transform.SetPositionAndRotation(position,fromWater ? heldEntryHeading : board.transform.rotation*heldEntryHeadingLocal);
        for(int i=0;i<retainedHeroBones.Length;i++)
        {retainedHeroBones[i].localPosition=entryHeroPositions[i];retainedHeroBones[i].localRotation=entryHeroRotations[i];}
        if(swimmingWeapon!=null)swimmingWeapon.RefreshPose();
        shark.SampleCinematicAction("Swim",Time.time);
    }
    void SampleEntryCombatGrip()
    {
        if(!preparingEntry || swimmingWeapon==null)return;
        // The visible water-entry pose stows the stick on the back. Native
        // attack sampling animates the hand, but does not animate the stick's
        // local grip offset. Grade the actual right-hand combat grip, then let
        // HoldEntryPose restore the visible swimming pose between slices.
        bool traversal=swimmingWeapon.CinematicTraversal;
        swimmingWeapon.CinematicTraversal=false;
        swimmingWeapon.RefreshPose();
        swimmingWeapon.CinematicTraversal=traversal;
        foreach(var collider in actorColliders)if(collider!=null)collider.enabled=false;
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
            if(stanceCandidateCount==0 || chosenStanceWoodPenetration>.0004f || float.IsInfinity(chosenStancePoseCost))continue;
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
        if(walk!=null && plannedFeet.TryGetValue(walk,out var walkingFeet))
        {
            Vector3 direction=Vector3.ProjectOnPlane(to-from,Vector3.up);
            Quaternion heading=direction.sqrMagnitude>.001f ? Quaternion.LookRotation(direction) : Quaternion.LookRotation(support.OpenWaterDirection(to));
            float seconds=direction.magnitude/3;
            for(int i=0;i<=steps;i++)
            {
                float u=(float)i/steps,phase=Mathf.Repeat(seconds*u*(3/3.59f),walk.length);
                var sampled=walkingFeet[Mathf.Min(walkingFeet.Length-1,Mathf.RoundToInt(phase*60))];
                // Use the retained walk's actual retargeted sole positions.
                // The much wider combat guard must not reject a safe walking
                // corridor; its final stance is checked separately.
                foreach(var offset in new[]{sampled.left,sampled.right})
                {
                    Vector3 sole=Vector3.Lerp(from,to,u)+heading*Vector3.ProjectOnPlane(offset,Vector3.up);
                    if(!support.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) || Vector3.Dot(hit.normal,Vector3.up)<=.5f)return false;
                }
            }
            return true;
        }
        for(int i=0;i<=steps;i++)for(int bearing=0;bearing<360;bearing+=90)
        {
            float u=(float)i/steps;
            Vector3 sole=Vector3.Lerp(from,to,u)+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*Mathf.Lerp(.25f,supportFootprintRadius,u);
            if(!support.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) || Vector3.Dot(hit.normal,Vector3.up)<=.5f)return false;
        }
        return true;
    }
    System.Collections.IEnumerator PlanSharkEntry()
    {
        double slice=Time.realtimeSinceStartupAsDouble;
        // Solid contact can move the animal while the sliced stance search
        // runs. Its admission route must start at the actual current pose.
        initialNose=shark.NoseWorldPoint;stageRotation=shark.transform.rotation;
        Vector3 futureFeet=Deck(-.4f,-Mathf.Sin(Ease(1.184f/1.8f)*Mathf.PI)*.3f);
        // The predictor returns combat-frame coordinates, then restores the
        // swimmer's current heading. Converting with that swimming transform
        // staged the shark on the opposite side of the eventual guard.
        Vector3 contact=futureFeet+Quaternion.LookRotation(Forward)*Vector3.Scale(PredictParryTip(futureFeet),hero.transform.lossyScale);
        Quaternion heading=ChooseAttackFacing("Entry preparation",contact,false);
        Vector3 contactRoot=contact-heading*Vector3.Scale(attackContactMarkerLocal,shark.transform.lossyScale);
        Vector3 outward=Vector3.ProjectOnPlane(contact-contactRoot,Vector3.up).normalized;
        Vector3 exitRoot=contactRoot-outward*2;
        // The admission pose is swimming. Using the later raised bite's
        // root height here left the whole animal hovering beside the deck
        // throughout the opening anticipation. Grade the real submerged
        // swimming endpoint; the ensuing bite still rises to its contact.
        exitRoot.y=Mathf.Min(exitRoot.y,shark.SurfaceRootY(Water(exitRoot),heading));
        var swimPoses=new Story1SharkWreckContact.AnatomyPose[4];
        for(int i=0;i<swimPoses.Length;i++)
        {shark.SampleCinematicAction("Swim",i*.4f);swimPoses[i]=battle.WreckContact.CaptureAnatomy();}
        shark.SampleCinematicAction("Swim",0);
        foreach(float setback in new[]{2f,4,6,8,12})
        {
            Vector3 candidate=contactRoot-outward*setback;bool clear=true;
            candidate.y=Mathf.Min(candidate.y,shark.SurfaceRootY(Water(candidate),heading));
            foreach(var swimming in swimPoses)
            {
                Vector3 nose=candidate+heading*Vector3.Scale(swimming.nose,shark.transform.lossyScale);
                // Admission is a swimming pose, not the coiled bite pose used
                // to find the first weapon contact. Test the whole swim cycle
                // before choosing the closest genuinely clear staging root.
                if(battle.WreckContact.PoseObstructionBounded(swimming,nose,heading,false,.000001f)>.000001f)
                {clear=false;break;}
            }
            if(clear){exitRoot=candidate;EntrySharkStagingDistance=setback;EntrySharkStagingClear=true;break;}
        }
        Quaternion supportYaw=Quaternion.LookRotation(Vector3.ProjectOnPlane(board.transform.forward,Vector3.up));
        entrySharkExitRootLocal=Quaternion.Inverse(supportYaw)*(exitRoot-board.transform.position);
        entrySharkHeadingLocal=Quaternion.Inverse(supportYaw)*heading;
        attackFacingKey=null;attackFacingSampleTime=-100;
        shark.SampleCinematicAction("Swim",0);
        Vector3 swimNose=shark.transform.InverseTransformPoint(shark.NoseWorldPoint);
        Vector3 end=exitRoot+heading*Vector3.Scale(swimNose,shark.transform.lossyScale);
        var anatomy=battle.WreckContact.CaptureAnatomy();float best=float.PositiveInfinity;
        foreach(float depth in new[]{0f,3f,7f,12f})foreach(float lateral in new[]{0f,-8f,8f,-16f,16f,-24f,24f})
        {
            if(Time.realtimeSinceStartupAsDouble-slice>=.002)
            {
                yield return null;
                while(PauseSettingsMenu.IsOpen || Time.timeScale<=0)yield return null;
                slice=Time.realtimeSinceStartupAsDouble;
            }
            var route=new[]{initialNose,Vector3.Lerp(initialNose,end,.3f)+Side*lateral,Vector3.Lerp(initialNose,end,.75f)+Side*lateral,end};
            route[1].y=Mathf.Min(route[1].y,Water(route[1])-depth-2);
            route[2].y=Mathf.Min(route[2].y,Water(route[2])-depth-2);
            float length=0,cost=0,rootLength=0,covered=0;
            for(int i=1;i<route.Length;i++)length+=Vector3.Distance(route[i-1],route[i]);
            float preference=length*.003f+depth*.002f;
            if(preference>=best)continue;
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
                    cost+=battle.WreckContact.PoseObstructionBounded(anatomy,at,rotation,false,(best-cost-preference)/100)*100;
                    if(cost+preference>=best)goto DominatedEntryRoute;
                    Vector3 root=at-rotation*Vector3.Scale(anatomy.nose,shark.transform.lossyScale);
                    rootLength+=Vector3.Distance(previousRoot,root);previousRoot=root;
                    if(Time.realtimeSinceStartupAsDouble-slice>=.002)
                    {
                        yield return null;
                        while(PauseSettingsMenu.IsOpen || Time.timeScale<=0)yield return null;
                        slice=Time.realtimeSinceStartupAsDouble;
                    }
                }
                covered+=segmentLength;
            }
            cost+=preference;
            if(cost<best)
            {
                best=cost;entrySharkRoute=route;entrySharkRouteLength=length;
                entrySharkApproachSeconds=Mathf.Max(rootLength/18,Quaternion.Angle(stageRotation,heading)/220)+.5f;
            }
            DominatedEntryRoute:;
        }
    }
    int entrySwimWaypoint;
    void RenderSharkEntry()
    {
        if(fromWater && entryChase!=null && Elapsed<swimSeconds)
        {entryChase.Pursue(entryChase.Age+Elapsed);return;}
        if(fromWater && entryChase!=null && !chaseDockingStarted)
        {
            chaseDockingStarted=true;
            Vector3 rebase=shark.NoseWorldPoint-entrySharkRoute[0];
            // The live pursuit has already advanced during sliced planning.
            // Rebase all intermediate waypoints, preserving their detours;
            // retaining the old ones made the shark swim back toward the ship.
            entrySharkRoute[1]+=rebase*.7f;
            entrySharkRoute[2]+=rebase*.25f;
            entrySharkRoute[0]=shark.NoseWorldPoint;
            entrySharkRouteLength=0;
            for(int i=1;i<entrySharkRoute.Length;i++)entrySharkRouteLength+=Vector3.Distance(entrySharkRoute[i-1],entrySharkRoute[i]);
        }
        Quaternion supportYaw=Quaternion.LookRotation(Vector3.ProjectOnPlane(board.transform.forward,Vector3.up));
        Quaternion heading=supportYaw*entrySharkHeadingLocal;
        heading=Quaternion.LookRotation(Vector3.ProjectOnPlane(heading*Vector3.forward,Vector3.up));
        Vector3 goalRoot=board.transform.position+supportYaw*entrySharkExitRootLocal;
        goalRoot.y=Mathf.Min(goalRoot.y,shark.SurfaceRootY(Water(goalRoot),heading));
        Vector3 swimNose=shark.transform.InverseTransformPoint(shark.NoseWorldPoint);
        Vector3 goal=goalRoot+heading*Vector3.Scale(swimNose,shark.transform.lossyScale);
        Vector3 correction=goal-entrySharkRoute[entrySharkRoute.Length-1];
        if(entrySwimWaypoint<1)entrySwimWaypoint=1;
        Vector3 waypoint=entrySharkRoute[entrySwimWaypoint]+correction;
        Vector3 centre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 targetBody=waypoint-heading*(Vector3.Scale(swimNose,shark.transform.lossyScale)-centre);
        Vector3 currentBody=shark.transform.position+shark.transform.rotation*centre;
        if(entrySwimWaypoint<entrySharkRoute.Length-1 && Vector3.ProjectOnPlane(currentBody-targetBody,Vector3.up).magnitude<3)
        {entrySwimWaypoint++;waypoint=entrySharkRoute[entrySwimWaypoint]+correction;}
        bool final=entrySwimWaypoint==entrySharkRoute.Length-1;
        Vector3 root=waypoint-heading*Vector3.Scale(swimNose,shark.transform.lossyScale);
        if(entryChase!=null)entryChase.SwimToRoot(root,heading,entryChase.Age+Elapsed,final);
        else NoseAt(waypoint,heading);

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
    Vector3 FindEntryEdge(Story1WreckPlank support,Vector3 feet,Vector3? jumpFrom=null,float maximumTravel=12,int firstAngle=0,int endAngle=360,float footprintOverride=-1,bool gradePose=true)
    {
        using var measurement=EntryPlanningMarker.Auto();
        Vector3 best=support.BoardingPoint(feet);float bestScore=float.NegativeInfinity;
        chosenStancePoseCost=float.PositiveInfinity;
        chosenStanceWoodPenetration=float.PositiveInfinity;
        var candidates=new System.Collections.Generic.List<(Vector3 point,float score,bool sampledSupport)>();
        foreach(float footprint in footprintOverride>=0 ? new[]{footprintOverride} : poseGraph.IsValid() && pose.IsValid() ? new[]{supportFootprintRadius,.6f} : new[]{supportFootprintRadius})
        for(int angle=firstAngle;angle<endAngle;angle+=15)
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
            if((footprint>=supportFootprintRadius || !gradePose) && score>bestScore){bestScore=score;best=candidate;}
        }
        stanceCandidateCount=candidates.Count;
        if(gradePose && poseGraph.IsValid() && pose.IsValid() && guard!=null && candidates.Count>0)
        {
            var oldBoard=board;Vector2 oldOffset=deckOffset;Vector3 oldForward=localForward;
            Vector3 position=hero.transform.position;Quaternion rotation=hero.transform.rotation;
            board=support;bestScore=float.NegativeInfinity;int accepted=0;
            foreach(var candidate in candidates.OrderByDescending(c=>c.score))
            {
                // Remaining candidates cannot beat a verified stance even
                // with zero pose penalty. Keep the geometric contract intact.
                if(candidate.score<=bestScore)break;
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
        chosenStanceScore=bestScore;return best;
    }
    bool SupportsContinuousStances()
    {
        // Sample Humanoid curves once, not hundreds of Animator evaluations
        // and iterative IK passes for every hypothetical candidate surface.
        // Final live planting and contact still use the exact mesh/rig solver.
        var parry=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Parry).clip];
        var heading=Quaternion.LookRotation(Forward);var scale=hero.transform.lossyScale;
        Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,board.Dimensions.y+1);
        // The same slash is used at three different physical stances: first
        // counter, landing counter and the last counter. Validating only the
        // landing stance allowed a final stepping foot to leave a narrow edge.
        foreach(var row in new[]{(guard,-.4f),(parry,-.4f),(slash,-.4f),(slash,.15f),(slash,-.3f),(block,-.3f),(block,0f),(throwClip,.15f)})
        {
            if(!plannedFeet.TryGetValue(row.Item1,out var samples))return false;
            Vector3 feet=Deck(row.Item2),anchor=default;bool hasAnchor=false,plantLeft=false;
            foreach(var sample in samples)
            {
                Vector3 left=feet+heading*Vector3.Scale(sample.left,scale),right=feet+heading*Vector3.Scale(sample.right,scale);
                bool leftSupported=board.Collision.Raycast(new Ray(left+normal*reach,-normal),out var a,reach*2) && Vector3.Dot(a.normal,Vector3.up)>.5f;
                bool rightSupported=board.Collision.Raycast(new Ray(right+normal*reach,-normal),out var b,reach*2) && Vector3.Dot(b.normal,Vector3.up)>.5f;
                bool weightTransfer=row.Item1==slash || row.Item1==throwClip;
                // The live solver measures the supporting leg during a step
                // or attack. Requiring the deliberately unloaded leg to stay
                // on the same plane rejected even flat, wide real decks.
                // Fixed guard and brace still require both surface rays.
                if(weightTransfer ? !leftSupported && !rightSupported : !leftSupported || !rightSupported)return false;
                if(weightTransfer)continue;
                float leftHeight=Vector3.Dot(left-a.point,normal)/Mathf.Max(.2f,normal.y),rightHeight=Vector3.Dot(right-b.point,normal)/Mathf.Max(.2f,normal.y);
                float correction=Mathf.Min(leftHeight,rightHeight);
                if(Mathf.Abs(leftHeight-rightHeight)>.35f)return false;
                if(sample.time>.14f && row.Item1!=slash && row.Item1!=throwClip)
                {
                    if(!hasAnchor){plantLeft=leftHeight<=rightHeight;anchor=plantLeft ? a.point : b.point;hasAnchor=true;}
                    Vector3 sole=(plantLeft ? left : right)-Vector3.up*correction;
                    if(Vector3.Distance(sole,anchor)>.35f)return false;
                }
            }
        }
        return true;
    }    float GradeTailStance(Vector3 feet,out float woodPenetration)
    {
        woodPenetration=0;
        Vector3 position=hero.transform.position;Quaternion heading=hero.transform.rotation;
        var clip=revisedClips[beats.First(b=>b.action==Story1EncounterAction.Brace).clip];
        var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);sample.SetTime(.572f);
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);
        hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(Forward));poseGraph.Evaluate(0);SampleEntryCombatGrip();PlantFeet(feet,false);
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
                float penetration=battle.WreckContact.PoseObstructionBounded(anatomy,target,rotation,true,Mathf.Max(.0004f,(best-bias)/10000));
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
        mixer.SetInputWeight(0,1-weight);mixer.SetInputWeight(1,weight);poseGraph.Evaluate(0);SampleEntryCombatGrip();
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
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);poseGraph.Evaluate(0);SampleEntryCombatGrip();
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
        mixer.DisconnectInput(1);mixer.ConnectInput(1,sample,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1);poseGraph.Evaluate(0);SampleEntryCombatGrip();
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
        hero.transform.SetPositionAndRotation(targetFeet-Vector3.up*hero.LowestFootWorldOffset,futureHeading);poseGraph.Evaluate(0);SampleEntryCombatGrip();PlantFeet(targetFeet,false);
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
                float penetration=battle.WreckContact.PoseObstructionBounded(anatomy,marker,facing,false,Mathf.Max(.0004f,(best-bias)/10000));
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
                if(score>=best)continue;
                Vector3 endRoot=route.marker-route.facing*Vector3.Scale(noseLocal,shark.transform.lossyScale);
                for(int i=0;i<3;i++)
                {
                    Quaternion heading=Quaternion.RotateTowards(startHeading,route.facing,300*(phases[i]*counterSeconds)+3);
                    Vector3 root=Vector3.Lerp(startRoot,endRoot,phases[i]);
                    Vector3 nose=root+heading*Vector3.Scale(samples[i].nose,shark.transform.lossyScale);
                    score+=battle.WreckContact.PoseObstructionBounded(samples[i],nose,heading,false,(best-score)/2000)*2000;
                    if(score>=best)break;
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
        using var measurement=FacingPlanningMarker.Auto();
        attackAim=marker;
        bool same=attackFacingKey==key;
        // After the authored contact window, keep the actual strike heading
        // for follow-through. A recovery pose cannot produce another hit;
        // repeatedly searching a fresh wind-up here caused 200ms stalls.
        int contactIndex=key=="Probing bite" ? 0 : key=="First counter" ? 1 : key=="Landing counter" ? 2 : key=="Reversal tail" ? 4 : key=="Defeating tail" ? 5 : -1;
        if(same && contactIndex>=0 && sampledAge>(tail ? .65f : .52f)+contacts[contactIndex].window)return attackFacing;
        if(!same){tailStrokeReplans=biteStrokeReplans=0;lastTailReplanAge=lastBiteReplanAge=-100;facingSearchCursor=0;}
        bool rebaseTail=same && tail && hasIntendedTailRoot && tailStrokeReplans<3 && sampledAge-lastTailReplanAge>.12f && sampledAge<.5f && Vector3.Distance(shark.transform.position,intendedTailRoot)>1;
        bool rebaseBite=same && !tail && hasIntendedBiteRoot && biteStrokeReplans<3 && sampledAge-lastBiteReplanAge>.12f && sampledAge<.37f && Vector3.Distance(shark.transform.position,intendedBiteRoot)>1;
        if(!same || rebaseTail || rebaseBite)
        {
            facingSearchCursor=0;
            // A solid escape can put the animal metres away from the requested
            // breach/tail arc. Plan the remaining stroke from that ACTUAL solved
            // placement instead of repeatedly steering back through the old arc.
            attackStartRoot=shark.transform.position;attackStartRotation=shark.transform.rotation;
            if(tail)tailRouteStartAge=Mathf.Max(sampledAge,powerful ? -1.4f : -.6f);
            else biteRouteStartAge=sampledAge;
            hasIntendedTailRoot=false;
            hasIntendedBiteRoot=false;
            if(rebaseTail){attackRouteOffset=Vector3.zero;attackFacingSampleTime=-100;TailRouteReplans++;tailStrokeReplans++;lastTailReplanAge=sampledAge;}
            if(rebaseBite){attackRouteOffset=Vector3.zero;attackFacingSampleTime=-100;BiteRouteReplans++;biteStrokeReplans++;lastBiteReplanAge=sampledAge;}
        }
        float planningInterval=sampledAge>(tail ? .45f : .3f) ? .04f : .12f;
        if(same && FilmTime-attackFacingSampleTime<planningInterval)return attackFacing;
        float planningDelta=FilmTime-attackFacingSampleTime;
        string sampledAction=shark.CinematicAction;float sampledSeconds=shark.CinematicActionSeconds;
        string action=tail ? powerful ? "Ship_Smash" : "Tail_Strike" : "Bite_Lunge";
        if(!attackPlanningPoses.TryGetValue(key,out var planning))
        {
            shark.SampleCinematicAction(action,tail ? .65f : .52f);
            planning=new AttackPlanningPose{contact=battle.WreckContact.CaptureAnatomy(),
                marker=shark.transform.InverseTransformPoint(tail ? shark.TailWorldPoint : shark.NoseWorldPoint),
                bulk=shark.transform.InverseTransformPoint(battle.WreckContact.BulkBodyPoint),
                path=new Story1SharkWreckContact.AnatomyPose[attackPathPhases.Length]};
            for(int sample=0;sample<attackPathPhases.Length;sample++)
            {
                float phase=attackPathPhases[sample];
                float biteSeconds=key=="Probing bite" ? 1.184f : key=="Landing counter" ? 2.48f : .63f;
                float seconds=tail ? Mathf.Max(0,phase*(powerful ? 2.05f : 1.25f)-(powerful ? 1.4f : .6f)) : Mathf.Max(0,phase*biteSeconds-(biteSeconds-.52f));
                shark.SampleCinematicAction(action,seconds);planning.path[sample]=battle.WreckContact.CaptureAnatomy();
            }
            attackPlanningPoses.Add(key,planning);
            shark.SampleCinematicAction(sampledAction,sampledSeconds);
        }
        var contactAnatomy=planning.contact;
        if(rebaseTail || rebaseBite)
        {
            // These future poses belong to the remaining wind-up, not the
            // earlier straight spine that has already passed. Rendered pose
            // restoration below remains separate from these exact samples.
            for(int sample=0;sample<attackPathPhases.Length;sample++)
            {
                shark.SampleCinematicAction(action,Mathf.Max(0,Mathf.Lerp(tail ? tailRouteStartAge : biteRouteStartAge,tail ? .65f : .52f,attackPathPhases[sample])));
                planning.path[sample]=battle.WreckContact.CaptureAnatomy();
            }
            shark.SampleCinematicAction(sampledAction,sampledSeconds);
        }
        float previousCost=same ? battle.WreckContact.PoseObstruction(contactAnatomy,marker,attackFacing,tail) : float.PositiveInfinity;
        Vector3 localMarker=planning.marker;
        attackContactMarkerLocal=localMarker;
        Vector3 localBulk=planning.bulk;
        var pathPhases=attackPathPhases;var pathPoses=planning.path;
        routeOffsets[0]=Vector3.zero;routeOffsets[1]=Vector3.down*2;routeOffsets[2]=Vector3.down*4;
        routeOffsets[3]=Vector3.down*3+Forward*3;routeOffsets[4]=Vector3.down*3-Forward*3;
        routeOffsets[5]=Vector3.down*3+Side*3;routeOffsets[6]=Vector3.down*3-Side*3;
        // Real wreck spars can extend well below the water. A fixed four-metre
        // dive offered no route around those obstructions after the breach.
        // These are candidate swim arcs, not collision corrections: native
        // anatomy still grades every arc and the solid solver owns movement.
        routeOffsets[7]=Vector3.down*7;routeOffsets[8]=Vector3.down*12;
        routeOffsets[9]=Vector3.down*6+Side*8;routeOffsets[10]=Vector3.down*6-Side*8;
        routeOffsets[11]=Vector3.down*6+Forward*8;routeOffsets[12]=Vector3.down*6-Forward*8;
        bool limitRouteExploration=false,routeBudgetYielded=false;
        long routeExplorationStarted=0;
        bool YieldRouteExploration()
        {
            if(!limitRouteExploration || System.Diagnostics.Stopwatch.GetTimestamp()-routeExplorationStarted<
                System.Diagnostics.Stopwatch.Frequency*routeAlternativeBudgetSeconds)return false;
            if(!routeBudgetYielded){FacingBudgetYields++;routeBudgetYielded=true;}
            return true;
        }
        float RouteObstruction(Quaternion rotation,float endpointCost,out Vector3 bestOffset,bool retainedOnly=false,float maximumCost=float.PositiveInfinity,bool clearOnly=false)
        {
            bestOffset=Vector3.zero;float minimum=float.PositiveInfinity;
            // First seek an actually clear route, bounding every native test
            // by the same existing physical-clear threshold. Computing the
            // full losing penetration score of the first blocked dive used
            // to dominate a new tail wind-up. If none clears, retain the
            // original exact blocked-route ranking in a second pass.
            bool seekClear=!retainedOnly && endpointCost<=.0004f;
            for(int pass=0;pass<(seekClear && !clearOnly ? 2 : 1);pass++)
            {
            if(seekClear && pass==1)ExactBlockedRoutePasses++;
            for(int offsetIndex=0;offsetIndex<(retainedOnly ? 1 : routeOffsets.Length);offsetIndex++)
            {
            Vector3 requestedOffset=routeOffsets[offsetIndex];
            // Grade the actual blended detour, not an instantly reachable
            // target offset that runtime will only approach at three m/s.
            Vector3 offset=retainedOnly ? attackRouteOffset : same
                ? Vector3.MoveTowards(attackRouteOffset,requestedOffset,3*Mathf.Max(0,planningDelta)) : requestedOffset;
            // Clear solid geometry takes precedence over exposure and style.
            // A shallow intersecting pose must not win because its bulk is
            // better submerged; the solver would move it away at impact.
            float cost=endpointCost*10000;
            if(same && !tail && sampledAge<=.52f)
            {
                // Re-selecting a heading changes the current point on this
                // already-progressed arc. Grade entry into THAT point from
                // the actual solved root, as well as the full wind-up below.
                // A clear historical arc cannot make a six-metre late jump
                // physically reachable in one frame. This is a route penalty;
                // all native collision and real-contact checks remain intact.
                float now=Mathf.Clamp01((sampledAge-biteRouteStartAge)/Mathf.Max(.01f,.52f-biteRouteStartAge));
                Vector3 endpoint=marker-rotation*Vector3.Scale(localMarker,shark.transform.lossyScale);
                Vector3 requested=Vector3.Lerp(attackStartRoot,endpoint,now)+offset*Mathf.Sin(Mathf.PI*now);
                float excess=Mathf.Max(0,Vector3.Distance(shark.transform.position,requested)-(24*Mathf.Max(.01f,Time.deltaTime)+.15f));
                if(excess>.001f){BiteTransitionPenaltyCount++;LargestBiteTransitionExcess=Mathf.Max(LargestBiteTransitionExcess,excess);}
                cost+=excess*excess*10000;
            }
            if(same && tail && sampledAge<=.65f)
            {
                // Apply the same real-entry contract to a tail arc. A late
                // clear heading can otherwise ask the body to jump across
                // timber to the current point of its eased rotating path.
                float now=Mathf.Clamp01((sampledAge-tailRouteStartAge)/Mathf.Max(.01f,.65f-tailRouteStartAge)),advance=Ease(now);
                Vector3 centre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
                Vector3 endpoint=marker-rotation*Vector3.Scale(localMarker-shark.RootMeshBounds.center,shark.transform.lossyScale);
                Vector3 body=Vector3.Lerp(attackStartRoot+attackStartRotation*centre,endpoint,advance)+offset*Mathf.Sin(Mathf.PI*advance);
                Quaternion heading=Quaternion.Slerp(attackStartRotation,rotation,Ease(now/.55f));
                float excess=Mathf.Max(0,Vector3.Distance(shark.transform.position,body-heading*centre)-(24*Mathf.Max(.01f,Time.deltaTime)+.15f));
                if(excess>.001f){TailTransitionPenaltyCount++;LargestTailTransitionExcess=Mathf.Max(LargestTailTransitionExcess,excess);}
                cost+=excess*excess*10000;
            }
            float offsetCost=offset.sqrMagnitude*.015f;
            // A clear detour must also be followable at the solid
            // controller's speed. Tail body easing and rotation about its
            // centre can be more demanding than the linear bite root arc.
            float seconds=Mathf.Max(.01f,tail ? .65f-tailRouteStartAge : .52f-biteRouteStartAge);
            Vector3 previous=attackStartRoot;float excessDistance=0;
            Vector3 scaledCentre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
            for(int step=1;step<=12;step++)
            {
                float phase=step/12f;Vector3 root;
                if(tail)
                {
                    Vector3 endpoint=marker-rotation*Vector3.Scale(localMarker-shark.RootMeshBounds.center,shark.transform.lossyScale);
                    float advance=Ease(phase);
                    Vector3 body=Vector3.Lerp(attackStartRoot+attackStartRotation*scaledCentre,endpoint,advance)+offset*Mathf.Sin(Mathf.PI*advance);
                    Quaternion heading=Quaternion.Slerp(attackStartRotation,rotation,Ease(phase/.55f));
                    root=body-heading*scaledCentre;
                }
                else
                {
                    Vector3 endpoint=marker-rotation*Vector3.Scale(localMarker,shark.transform.lossyScale);
                    root=Vector3.Lerp(attackStartRoot,endpoint,phase)+offset*Mathf.Sin(Mathf.PI*phase);
                }
                excessDistance+=Mathf.Max(0,Vector3.Distance(previous,root)-24*seconds/12);
                previous=root;
            }
            cost+=excessDistance*excessDistance*10000;
            float limit=Mathf.Min(minimum,maximumCost);
            if(seekClear && pass==0)limit=Mathf.Min(limit,offsetCost+.0015f);
            // All remaining native penetration terms are nonnegative. Once
            // this route cannot beat an already fully evaluated route, its
            // remaining samples cannot affect the selected physical approach.
            // This bounds work without dropping geometry or accepting a miss.
            if(cost+offsetCost>=limit)continue;
            bool dominated=false;
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
                    facing=Quaternion.RotateTowards(attackStartRotation,rotation,300*(phase*(.52f-biteRouteStartAge))+3);
                    at=root+facing*Vector3.Scale(anatomy.nose,shark.transform.lossyScale);
                }
                cost+=battle.WreckContact.PoseObstructionBounded(anatomy,at,facing,tail,(limit-cost-offsetCost)/2000)*2000;
                if(cost+offsetCost>=limit){dominated=true;break;}
            }
            if(dominated)continue;
            cost+=offsetCost;
            if(cost<minimum){minimum=cost;bestOffset=offset;}
            if(seekClear && pass==0 && cost-offsetCost<.0015f){ClearRouteEarlyExits++;return minimum;}
            if(cost<.0015f)break;
            }
            }
            return minimum;
        }
        var waterSnapshot=OceanSurfaceSampler.Capture(ocean,shotCamera,Time.time);
        float Exposure(Quaternion rotation)
        {
            Vector3 bulk=marker-rotation*Vector3.Scale(localMarker-localBulk,shark.transform.lossyScale);
            return Mathf.Max(0,bulk.y-(ocean!=null ? waterSnapshot.Height(bulk) : hero.seaLevel)+1.4f);
        }
        float retainedRouteCost=float.PositiveInfinity;Vector3 retainedRouteOffset=attackRouteOffset;
        if(same && previousCost<.0004f)
        {
            float routeCost=RouteObstruction(attackFacing,previousCost,out var safeOffset,true);
            retainedRouteCost=routeCost;retainedRouteOffset=safeOffset;
            // A submerged detour's artistic length penalty is not a solid
            // collision. Reuse a genuinely clear route even when the head
            // rises to meet the weapon, without repeatedly searching it.
            if(routeCost-safeOffset.sqrMagnitude*.015f<.001f)
            {attackRouteOffset=safeOffset;shark.SampleCinematicAction(sampledAction,sampledSeconds);attackFacingSampleTime=FilmTime;return attackFacing;}
        }
        attackFacingKey=key;attackFacingSampleTime=FilmTime;float best=float.PositiveInfinity;bool clearFound=false;Quaternion chosen=Quaternion.LookRotation(-Forward)*Quaternion.Euler(-16,0,0);
        Vector3 chosenOffset=Vector3.zero;float chosenRouteSolid=float.PositiveInfinity;
        if(!float.IsPositiveInfinity(retainedRouteCost))
        {
            float exposure=Exposure(attackFacing),turn=Quaternion.Angle(attackStartRotation,attackFacing);
            best=retainedRouteCost+exposure*exposure*.2f+turn*turn*.001f;
            chosen=attackFacing;chosenOffset=retainedRouteOffset;clearFound=true;
            chosenRouteSolid=retainedRouteCost-retainedRouteOffset.sqrMagnitude*.015f;
        }
        // A new stroke often has a clear natural facing. Verify that exact
        // endpoint, the full authored intermediate anatomy and the same
        // movement budget before enumerating every cinematic attitude. An
        // obstructed probe never wins; the original broad search still runs.
        // These are existing yaw-zero attitudes, not looser collision rules.
        bool TryPreferredClear(Quaternion facing)
        {
            if(same)
            {
                float remaining=Mathf.Max(0,(tail ? .65f : .52f)-sampledAge);
                facing=Quaternion.RotateTowards(shark.transform.rotation,facing,300*(remaining+Mathf.Max(.01f,Time.deltaTime))+3);
            }
            float endpoint=battle.WreckContact.PoseObstructionBounded(contactAnatomy,marker,facing,tail,.0004f);
            if(endpoint>.0004f)return false;
            float route=RouteObstruction(facing,endpoint,out var offset,clearOnly:true);
            if(float.IsInfinity(route) || route-offset.sqrMagnitude*.015f>=.0015f)return false;
            attackFacingKey=key;attackFacingSampleTime=FilmTime;facingSearchCursor=0;
            attackFacing=facing;attackRouteOffset=offset;attackEndpointCost=endpoint;
            float exposure=Exposure(facing),turn=Quaternion.Angle(attackStartRotation,facing);
            attackRouteCost=route+exposure*exposure*.2f+turn*turn*.001f;
            PreferredClearRouteEarlyExits++;
            shark.SampleCinematicAction(sampledAction,sampledSeconds);
            return true;
        }
        if(tail)
        {
            var positive=Quaternion.LookRotation((Side+Forward*.75f).normalized)*Quaternion.Euler(25,0,powerful ? -4 : 0);
            var negative=Quaternion.LookRotation((-Side+Forward*.75f).normalized)*Quaternion.Euler(25,0,powerful ? -4 : 0);
            bool nearerPositive=Quaternion.Angle(attackStartRotation,positive)<=Quaternion.Angle(attackStartRotation,negative);
            if(TryPreferredClear(nearerPositive ? positive:negative) || TryPreferredClear(nearerPositive ? negative:positive))return attackFacing;
        }
        else if(TryPreferredClear(Quaternion.LookRotation(-Forward)*Quaternion.Euler(-16,0,0)))return attackFacing;
        facingCandidates.Clear();
        // The new authored head rise supplies bite reach. Reserve upright
        // whole-body attitudes for the explicit breach, not ordinary counters.
        // A late physical escape already consumed wind-up time. Search the
        // coarse actual attitudes throughout the remaining stroke rather
        // than blocking it with the full cinematic style search. All selected
        // endpoints/routes still receive exact native anatomy tests.
        bool urgentApproach=tail ? tailStrokeReplans>0 : biteStrokeReplans>0;
        foreach(int side in tail ? bothSides : oneSide)foreach(float yaw in urgentApproach ? coarseEntryYaws : facingYaws)foreach(float pitch in urgentApproach ? coarseEntryPitches : tail ? tailPitches : bitePitches)
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
            // First classify endpoints at the existing clear threshold. A
            // blocked pose cannot outrank ANY clear pose, so its exact losing
            // score is unnecessary unless the entire attitude set is blocked.
            float endpointCost=battle.WreckContact.PoseObstructionBounded(contactAnatomy,marker,facing,tail,.0004f);
            bool clear=endpointCost<=.0004f;
            float turn=Quaternion.Angle(attackStartRotation,facing);
            float bias=exposure*exposure*.2f+turn*turn*.001f+Mathf.Abs(yaw)*.00005f+Mathf.Abs(pitch+16)*.00005f;
            facingCandidates.Add(new FacingCandidate{rotation=facing,endpoint=endpointCost,bias=bias,clear=clear});
        }
        bool hasClearEndpoint=false;
        foreach(var candidate in facingCandidates)if(candidate.clear){hasClearEndpoint=true;break;}
        if(!hasClearEndpoint && !clearFound)
        {
            // Lower bounds must never rank blocked routes against each other.
            // Recover exact costs in this case to preserve that decision.
            for(int i=0;i<facingCandidates.Count;i++)
            {
                var candidate=facingCandidates[i];
                candidate.endpoint=battle.WreckContact.PoseObstruction(contactAnatomy,marker,candidate.rotation,tail);
                facingCandidates[i]=candidate;
            }
        }
        // First grade all endpoints cheaply. Full swept-anatomy routes are
        // evaluated for a bounded shortlist instead of hundreds of attitudes
        // times seven routes, which caused visible 100ms search frames.
        facingCandidates.Sort((a,b)=>a.clear!=b.clear ? (a.clear?-1:1) : (a.endpoint*10000+a.bias).CompareTo(b.endpoint*10000+b.bias));
        // A first wind-up still receives the original complete search. Once
        // its previous route has been freshly graded against the current
        // moving wood, alternative attitudes share a soft 3ms slice. Finish
        // each attitude's full detour search before yielding: restarting its
        // first few offsets every pass would starve deeper swimming routes.
        // Exact native tests, contact clock and final solving are unchanged.
        limitRouteExploration=!float.IsPositiveInfinity(retainedRouteCost);
        routeExplorationStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        // Keep at most four new swept routes per planning pass. If those
        // endpoints all approach through timber, continue down the ranking
        // on the next pass rather than retrying the same four indefinitely.
        // Re-grade and retain the previous clear endpoint above, so widening
        // this search cannot replace it with a worse blocked approach.
        int first=facingSearchCursor<facingCandidates.Count ? facingSearchCursor : 0,examined=0;
        if(first>0)FacingSearchExtensions++;
        for(int index=first;index<Mathf.Min(first+4,facingCandidates.Count);index++)
        {
            if(index>first && YieldRouteExploration())break;
            var candidate=facingCandidates[index];bool clear=candidate.clear;
            if(clearFound && !clear)break;
            examined++;
            float routeLimit=clear && !clearFound ? float.PositiveInfinity : best-candidate.bias;
            float routeCost=RouteObstruction(candidate.rotation,candidate.endpoint,out var routeOffset,maximumCost:routeLimit),cost=routeCost+candidate.bias;
            if(clear && !clearFound || cost<best)
            {best=cost;chosen=candidate.rotation;clearFound=clear;chosenOffset=routeOffset;chosenRouteSolid=routeCost-routeOffset.sqrMagnitude*.015f;}
            if(clear && chosenRouteSolid<.0015f)break;
        }
        facingSearchCursor=chosenRouteSolid<.0015f || examined==0 || first+examined>=facingCandidates.Count ? 0 : first+examined;
        attackEndpointCost=battle.WreckContact.PoseObstruction(contactAnatomy,marker,chosen,tail);
        attackRouteCost=best;
        attackRouteOffset=chosenOffset; // Already blended and graded by RouteObstruction.
        // Reach planning samples the contact pose temporarily. Restore the
        // timeline's actual recoil/recovery instead of replacing it with
        // another bite every time the approach is evaluated.
        shark.SampleCinematicAction(sampledAction,sampledSeconds);
        attackFacing=chosen;return attackFacing;
    }
    void CounterApproach(float age,float hit,Vector3 aim,float strength)
    {
        // The landing counter has 2.48 seconds of approach, while the first
        // counter has .63. Use the same native bite phase as PlaceBiteRoot;
        // a fixed .11 offset delayed the landing approach until its last .63s
        // and disabled remaining-stroke replanning before the actual bite.
        float sampledAge=age-(hit-.52f);
        Quaternion facing=ChooseAttackFacing(FilmTime<4 ? "First counter" : "Landing counter",aim,false,false,sampledAge);
        PlaceBiteRoot(age,hit,aim,facing,strength);
    }
    void PlaceBiteRoot(float age,float hit,Vector3 aim,Quaternion facing,float strength)
    {
        float sampledAge=age-(hit-.52f);
        float approach=Mathf.Clamp01((sampledAge-biteRouteStartAge)/Mathf.Max(.01f,.52f-biteRouteStartAge)),retreat=Ease((age-hit)/.65f);
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
        intendedBiteRoot=shark.transform.position;hasIntendedBiteRoot=true;
    }
    void TailApproach(float age,Vector3 target,bool powerful)
    {
        Quaternion facing=ChooseAttackFacing(powerful ? "Defeating tail" : "Reversal tail",target,true,powerful,age);
        float phase=Mathf.Clamp01((age-tailRouteStartAge)/Mathf.Max(.01f,TralaleroSwimAnimator.TailContactTime-tailRouteStartAge));
        float advance=Ease(phase),retreat=Ease((age-.74f)/.9f);
        Vector3 local=powerful ? filmShipTailLocal : filmTailLocal;
        Vector3 end=target-facing*Vector3.Scale(local-shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 startBody=attackStartRoot+attackStartRotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 body=Vector3.Lerp(startBody,end,advance)+attackRouteOffset*Mathf.Sin(Mathf.PI*advance)-Side*(retreat*4);
        Quaternion rotation=Quaternion.Slerp(attackStartRotation,facing,Ease(phase/.55f));
        if(retreat>0)
        {
            float surface=shark.SurfaceRootY(Water(body),rotation)+(rotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale)).y;
            body.y=Mathf.Lerp(body.y,surface,retreat);
        }
        battle.PoseCinematicShark(body,rotation,absoluteHeight:true);
        intendedTailRoot=shark.transform.position;hasIntendedTailRoot=true;
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
                    if(freeboard<-.15f || freeboard>2.5f || Mathf.Abs(supportBody.GetPointVelocity(support).y-waveVelocity)>3)
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
        if(entryChase!=null)entryChase.Finish();
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
                bool parried=verifiedContacts.Contains(0) && t>contactTimes[0]+.04f;
                shark.SampleCinematicAction(parried ? "Hit_Recoil" : "Bite_Lunge",parried ? t-contactTimes[0] : Mathf.Max(0,t-.664f));
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
            if(t<6.4f){SwimNoseAt(SurfaceNose(Deck()+Forward*6-Side*2,Quaternion.LookRotation(-Forward)),Quaternion.LookRotation(-Forward));}
            else{board.Drive(0,0,null);CounterApproach(t-4.55f,2.48f,Deck(.15f)+hero.transform.TransformVector(PredictSlashTip(Deck(.15f))),4);}
        }
        else if(t<10)
        {
            bool swimming=t<8.94f || !verifiedContacts.Contains(3);
            shark.SampleCinematicAction(swimming ? "Swim" : "Hit_Recoil",swimming ? age : t-8.94f);
            Vector3 nose=SurfaceNose(Deck()+Forward*(6+Ease((t-8.94f)/.8f)*3),Quaternion.LookRotation(-Forward));
            if(swimming)SwimNoseAt(nose,Quaternion.LookRotation(-Forward));else NoseAt(nose,Quaternion.LookRotation(-Forward));
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
            Quaternion facing=Quaternion.LookRotation(-Side);
            float clearance=Mathf.Max(4,shark.RootMeshBounds.extents.x*Mathf.Abs(shark.transform.lossyScale.x)*.7f+1.5f);
            Vector3 nose=SurfaceNose(Deck()+Forward*(clearance+3)+Side*5,facing);
            Vector3 root=nose-facing*Vector3.Scale(shark.transform.InverseTransformPoint(shark.NoseWorldPoint),shark.transform.lossyScale);
            Vector3 goalBody=root+facing*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
            // Turn the mass around its actual body centre in the open-water
            // lane. Pinning the nose and turning a long rear body around it
            // swept the tail through the support during this flank.
            Vector3 body=Vector3.Lerp(stageBodyStart,goalBody,Ease(age/1.6f));
            body.y-=Mathf.Sin(age/1.6f*Mathf.PI)*3;
            if(entryChase!=null)entryChase.SwimInFilmToRoot(body-facing*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale),facing,Elapsed);
            else battle.PoseCinematicShark(body,Quaternion.Slerp(stageRotation,facing,Ease(age/1.6f)),absoluteHeight:true);
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
    System.Collections.IEnumerator PrepareHop(int index)
    {
        hopPlanningBusy=true;hopPlanningStarted[index]=true;
        HopPlanningAttempts[index]++;
        float deadline=index==0 ? 4.45f:11.5f;
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        var neighbours=battle.Boards.Where(p=>p!=board && p.IsBoardable && p.Dimensions.y<5 && p.Speed<5 && Mathf.Min(p.Dimensions.x,p.Dimensions.z)>1.6f)
            .Where(p=>Vector3.ProjectOnPlane(p.BoardingPoint(feet)-feet,Vector3.up).magnitude<12 && Mathf.Abs(p.BoardingPoint(feet).y-feet.y)<1.5f)
            .OrderBy(p=>p.EdgeDistance(feet)).Take(3).ToArray();
        if(HopPlanningDiagnostics.Count<32)HopPlanningDiagnostics.Add("hop="+index+" attempt="+HopPlanningAttempts[index]+" time="+FilmTime.ToString("F3")+" neighbours="+neighbours.Length);
        foreach(var candidate in neighbours)
        {
            if(FilmTime>=deadline || hopIndex==index)break;
            yield return SearchEntryEdgeSlices(candidate,candidate.BoardingPoint(feet),feet,stopAtFilm:deadline);
            // A search completing after takeoff cannot replace the chosen
            // supported variant or redirect an already airborne character.
            if(FilmTime>=deadline || hopIndex==index)break;
            if(HopPlanningDiagnostics.Count<32)HopPlanningDiagnostics.Add(candidate.name+" candidates="+stanceCandidateCount+" wood="+chosenStanceWoodPenetration+" pose="+chosenStancePoseCost);
            if(stanceCandidateCount==0 || chosenStanceWoodPenetration>.0004f || Water(slicedEntryPoint)-slicedEntryPoint.y>.6f)continue;
            if(ApproachClearance(slicedEntryPoint,candidate.OpenWaterDirection(slicedEntryPoint))<.15f)continue;
            hopPlans[index]=new HopPlan{target=candidate,landingLocal=candidate.transform.InverseTransformPoint(slicedEntryPoint),forwardLocal=candidate.transform.InverseTransformDirection(candidate.OpenWaterDirection(slicedEntryPoint))};break;
        }
        hopPlanningBusy=false;
        // Floating neighbours keep moving during the preceding exchange.
        // An early unsuccessful search is not a permanent no-neighbour
        // decision. Retry within the existing sliced budget and lead-in;
        // takeoff still validates the actual mesh and has no blocking search.
        if(hopPlans[index].target==null && hopIndex!=index)
        {hopPlanningStarted[index]=false;hopRetryAfter[index]=FilmTime+.6f;}
    }
    void BeginHop(int index)
    {
        hopIndex=index;hopFrom=board;hopLanded=false;
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        hopStartLocal=board.transform.InverseTransformPoint(feet);
        var plan=hopPlans[index];hopTarget=plan.target;
        Vector3 landing=hopTarget!=null ? hopTarget.transform.TransformPoint(plan.landingLocal) : feet;
        bool valid=hopTarget!=null && hopTarget.IsBoardable && hopTarget.Speed<5 &&
            Vector3.ProjectOnPlane(landing-feet,Vector3.up).magnitude<12 && Mathf.Abs(landing.y-feet.y)<1.5f;
        if(valid)
        {
            Vector3 normal=hopTarget.transform.up;if(normal.y<0)normal=-normal;
            float reach=Mathf.Max(2,hopTarget.Dimensions.y+1);
            for(int bearing=0;bearing<360 && valid;bearing+=45)
            {
                Vector3 sole=landing+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*.75f;
                valid=hopTarget.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) && Vector3.Dot(hit.normal,normal)>.5f;
            }
        }
        if(!valid)
        {
            // Planning has a bounded lead-in. A late or broken destination
            // selects the same-duration supported exchange, without stopping
            // the render thread to search for a different board at takeoff.
            // A planted pose may have shifted the root to load one leg.
            // That IK-adjusted root is not a new authored deck stance: using
            // it here rebased every later guard/counter and accumulated slide.
            // Walk back to this support's validated nominal stance instead.
            hopTarget=board;landing=Deck();
        }
        float duration=index==0 ? 1.85f : 2.4f;        // Preparation cannot carry the actor across a whole deck in a few
        // frames. Limit the supported push-off to an actual short step.
        Vector3 takeoff=board.BoardingPoint(Vector3.MoveTowards(feet,board.BoardingPoint(landing),3*duration*.2f));
        hopTakeoffLocal=board.transform.InverseTransformPoint(takeoff);
        hopLandingLocal=hopTarget.transform.InverseTransformPoint(landing);
        // Keep the facing that was actually validated with the soles. On a
        // triangular cut, re-picking the nearest edge can turn it90/180deg
        // and invalidate both the landing stance and the predicted weapon.
        hopLandingForwardLocal=valid && plan.forwardLocal.sqrMagnitude>.01f ? plan.forwardLocal :
            hopTarget.transform.InverseTransformDirection(hopTarget==board ? Forward : hopTarget.OpenWaterDirection(landing));
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
        localForward=hopLandingForwardLocal;
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
            if(hopTarget==hopFrom)
            {
                // There is no destination board to dodge around. The old
                // local-X default could pick the opposite, obstructed side
                // of a huge hull instead of the stance's real open-water edge.
                Vector3 takeoff=hopFrom.BoardingPoint(hopFrom.transform.TransformPoint(hopTakeoffLocal));
                Vector3 outward=Vector3.ProjectOnPlane(hopFrom.transform.TransformDirection(hopLandingForwardLocal),Vector3.up).normalized;
                breachTarget=hopFrom.BoardingPoint(takeoff+outward*100)+outward*clearance;
            }
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
            Vector3 outward=Vector3.ProjectOnPlane(hopTarget.transform.TransformDirection(hopLandingForwardLocal),Vector3.up).normalized,direction=Vector3.ProjectOnPlane(landing-initial,Vector3.up).normalized;
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
                Quaternion.LookRotation(Vector3.ProjectOnPlane(hopTarget.transform.TransformDirection(hopLandingForwardLocal),Vector3.up).normalized),Ease((u-.7f)/.3f));
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
            Vector3 outward=Vector3.ProjectOnPlane(hopTarget.transform.TransformDirection(hopLandingForwardLocal),Vector3.up).normalized;
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
            localForward=hopLandingForwardLocal;
            Vector3 feet=Deck(.15f);
            target=feet+Quaternion.LookRotation(Forward)*Vector3.Scale(PredictSlashTip(feet),hero.transform.lossyScale);
        }
        finally
        {
            board=previousBoard;deckOffset=previousOffset;localForward=previousForward;
            hero.transform.SetPositionAndRotation(position,rotation);EvaluatePose();
            if(activeAnimation==null || !activeAnimation.StartsWith("Jump airborne"))PlantFeet(position+Vector3.up*hero.LowestFootWorldOffset,false);
        }
        return target;
    }
    void SwimNoseAt(Vector3 nose,Quaternion heading)
    {
        if(entryChase==null){NoseAt(nose,heading);return;}
        Vector3 local=shark.transform.InverseTransformPoint(shark.NoseWorldPoint);
        entryChase.SwimInFilmToRoot(nose-heading*Vector3.Scale(local,shark.transform.lossyScale),heading,Elapsed);
    }
    void RenderBreach(float age)
    {
        shark.SampleCinematicAction("Breach",age);
        Quaternion facing=Quaternion.LookRotation(breachDirection);
        Vector3 exit=breachTarget+breachDirection*4;
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
            if(gap<ContactAttemptGaps[i])ContactObstructions[i]="correction="+battle.WreckContact.LastCorrection+" hero="+a+" shark="+b+" aim="+attackAim+" endpoint="+attackEndpointCost+" route="+attackRouteCost+" heading="+attackFacing.eulerAngles+" shaft="+slashShaftFraction+" footprint="+supportFootprintRadius+" entryPose="+EntryPoseCost+" entryWood="+EntryWoodCost+" obstacles="+string.Join(";",battle.WreckContact.LastObstacles);
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
        if(Elapsed<LeadInSeconds && Elapsed+Time.deltaTime>=LeadInSeconds && EntrySharkAdmissionDelay<12)
        {
            Quaternion supportYaw=Quaternion.LookRotation(Vector3.ProjectOnPlane(board.transform.forward,Vector3.up));
            Vector3 goal=board.transform.position+supportYaw*entrySharkExitRootLocal;
            Quaternion heading=Quaternion.LookRotation(Vector3.ProjectOnPlane(supportYaw*entrySharkHeadingLocal*Vector3.forward,Vector3.up));
            goal.y=Mathf.Min(goal.y,shark.SurfaceRootY(Water(goal),heading));
            if(entryChase!=null)goal.y-=entryChase.SwimDepth;
            if(Vector3.Distance(shark.transform.position,goal)>1 || Quaternion.Angle(shark.transform.rotation,heading)>15)
            {
                // Let the actual approach finish through the scattered hull;
                // keep swimming/guarding on the real supports in that time.
                // Never jump the shark to the planned root to begin the film.
                float wait=Mathf.Min(Time.deltaTime,12-EntrySharkAdmissionDelay);
                EntrySharkAdmissionDelay+=wait;LeadInSeconds+=wait;
            }
        }
        float old=Elapsed,end=Elapsed+Time.deltaTime;
        foreach(float cue in timelineCues)
        {
            float at=Clock(cue);if(at<=old || at>end)continue;Elapsed=at;
            // Capture the supported pose before the fall starts consuming its launch anchor.
            if(cue==18.05f){RenderActors(cue-.0001f);Cue(cue);RenderActors(cue);}
            else{RenderActors(cue);Cue(cue);}
            // Evaluate the authored contact through solid collision even when a render frame crosses the cue.
            shark.CommitCinematicPose(Elapsed);
            battle.WreckContact.ResolvePose();shark.RecordResolvedCinematicPose();AlignRightHandContact();VerifyFinalContact();
        }
        Elapsed=end;RenderActors();shark.CommitCinematicPose(Elapsed);
        if(defeatContactTime>=0 && FilmTime>=defeatContactTime+.17f)LoseStick();
        if(FilmTime>18.05f+contacts[5].window && defeatContactTime<0)
        {
            battle.ResumeAfterMissedFinale(this);return;
        }
        if(FilmTime>=Duration && defeatContactTime>=0){IsComplete=true;CurrentBeat="Defeated";battle.CompleteFinale();}
    }
    public void BeginUnconsciousExit(string reason)
    {
        if(unconsciousExit || IsComplete)return;
        if(preparingEntry)
        {
            EntryPreparationSeconds=(float)(Time.realtimeSinceStartupAsDouble-entryPreparationStarted);
            EntrySelectedBoard=board!=null ? board.name : null;EntryPoseCost=chosenStancePoseCost;EntryWoodCost=chosenStanceWoodPenetration;
        }
        StopAllCoroutines();
        if(entryChase!=null)entryChase.Finish();
        // A blocked encounter is exhaustion, not an invented shark hit.
        // Continue from the rendered pose into the existing loss-of-stick,
        // unconscious float and shore ending, once, without restarting combat.
        ExitReason=reason;unconsciousExit=true;preparingEntry=false;initialized=true;
        unconsciousExitStart=Elapsed;
        unconsciousExitFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        unconsciousExitHeading=hero.transform.rotation;
        fallStart=unconsciousExitFeet;
        Vector3 chestAtExit=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        // Admission may have begun swimming several seconds before a real
        // climb. Exhaustion on that board still needs a gravity-driven fall.
        unconsciousExitInWater=chestAtExit.y<=Water(chestAtExit)+.25f;
        unconsciousExitVelocity=unconsciousExitInWater ? Vector3.ProjectOnPlane(battle.Handoff.velocity,Vector3.up)*.15f : FindOpenWaterVelocity();
        LeadInSeconds=Elapsed-(Duration-2.2f);
        Animate("Exhausted in the wreck",knockdown,Elapsed,knockdown.length/2.2f,0,.22f);
    }
    void RenderUnconsciousExit()
    {
        if(PauseSettingsMenu.IsOpen || Time.timeScale<=0 || Time.deltaTime<=0)return;
        Elapsed+=Time.deltaTime;float age=Elapsed-unconsciousExitStart;
        CurrentBeat="Exhausted in the wreck";
        Vector3 gravity=unconsciousExitInWater ? Vector3.zero : Vector3.up*(age*1.2f-age*age*4.9f);
        HeroAt(unconsciousExitFeet+unconsciousExitVelocity*age+gravity,false,unconsciousExitHeading*Vector3.forward);
        if(age>.2f && !HasLostStick)LoseStick();
        if(HasLostStick && projectile!=null)
        {
            float flight=Mathf.Max(0,FilmTime-stickLossTime);
            Vector3 center=lostStart+lostVelocity*flight+Vector3.down*(4.9f*flight*flight);
            center.y=Mathf.Max(center.y,Water(center)+.05f);
            SpinProjectile(center,flight);
        }
        if(age>.35f && !floating)
        {
            var chest=animator.GetBoneTransform(HumanBodyBones.Chest);var head=animator.GetBoneTransform(HumanBodyBones.Head);
            float lift=Mathf.Max(Water(head.position)+.12f-head.position.y,Water(chest.position)+.04f-chest.position.y);
            if(lift>0){hero.transform.position+=Vector3.up*lift;floating=true;floatingChest=chest.position;floatHeading=floatRotation=hero.transform.rotation;floatDrift=unconsciousExitVelocity.normalized*.25f;}
        }
        shark.SampleCinematicAction("Swim",Elapsed);FloatOnWaves();
        if(age>=2.2f){IsComplete=true;battle.CompleteFinale();}
    }
    Vector3 FindOpenWaterVelocity()
    {
        var decks=FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None)
            .Where(p=>!p.IsBroken && p.Collision!=null).ToArray();
        Vector3 chosen=-Side*7-Forward*2;float best=float.NegativeInfinity;
        if(decks.Length==0)return chosen/1.25f;
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
        void Reach(Vector3 delta,float weight,bool extended)
        {
            var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            var chest=animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            Quaternion upperBefore=upper.rotation,lowerBefore=lower.rotation,handBefore=hand.rotation;
            Quaternion chestBefore=chest!=null ? chest.rotation : Quaternion.identity;
            Vector3 before=weapon.transform.position;
            float upperLength=Vector3.Distance(upper.position,lower.position),lowerLength=Vector3.Distance(lower.position,hand.position);
            Vector3 handTarget=hand.position+delta*weight;
            SahurSupportPose.Hand(animator,false,handTarget,1);hand.rotation=handBefore;
            // An already extended elbow cannot supply the final centimetres
            // on a moving landing. Let the chest follow the reaching shoulder
            // through a small real rotation, then solve the same arm lengths.
            // Feet/root and receiving geometry remain where physics put them.
            bool torsoReach=chest!=null && delta.magnitude<=.2f && Vector3.Distance(hand.position,handTarget)>.015f;
            if(torsoReach)
            {
                Quaternion desired=Quaternion.FromToRotation(hand.position-chest.position,handTarget-chest.position)*chest.rotation;
                chest.rotation=Quaternion.RotateTowards(chestBefore,desired,3*weight);
                SahurSupportPose.Hand(animator,false,handTarget,1);hand.rotation=handBefore;
            }
            bool valid=Vector3.Distance(before,weapon.transform.position)<=.65f &&
                Mathf.Abs(Vector3.Distance(upper.position,lower.position)-upperLength)<=.001f &&
                Mathf.Abs(Vector3.Distance(lower.position,hand.position)-lowerLength)<=.001f;
            if(!valid || (extended || torsoReach) && !battle.WreckContact.CapsuleTranslationClear(weapon,before,weapon.transform.position))
            {if(chest!=null)chest.rotation=chestBefore;upper.rotation=upperBefore;lower.rotation=lowerBefore;hand.rotation=handBefore;return;}
            LargestRightHandContactAdjustment=Mathf.Max(LargestRightHandContactAdjustment,Vector3.Distance(before,weapon.transform.position));
        }
        float time=FilmTime;
        // Let the held right-hand guard receive the actually solved tail,
        // including a small moving-deck mismatch. The arm reaches within
        // its real two-bone length; feet, shark and hitboxes are unchanged.
        // A farther miss still produces no contact or stagger.
        float guardAge=time-contactTimes[4];
        if(guardAge>=-.2f && guardAge<=.16f)
        {
            float guardGap=Story1CapsuleContact.PointSeparation(shark.TailWorldPoint,weapon,out var guardDelta);
            if(guardGap>.001f && guardGap<=.6f)
            {
                float blend=guardAge<=0 ? Ease((guardAge+.2f)/.2f) : 1-Ease(guardAge/.16f);
                Reach(guardDelta,blend,guardGap>.4f);
            }
            return;
        }
        int index=time<1.8f ? 0 : time<4 ? 1 : time<7.8f ? 2 : -1;
        if(index<0)return;
        float distance=time-contactTimes[index];
        if(distance<-.2f || distance>.16f)return;
        float gap=Story1CapsuleContact.Separation(weapon,battle.WreckContact.HeadVolume,out var delta);
        if(gap<.001f || gap>.55f)return;
        float weight=distance<=0 ? Ease((distance+.2f)/.2f) : 1-Ease(distance/.16f);
        Reach(Vector3.ClampMagnitude(delta,.55f),weight,gap>.25f);
    }
    void AlignTailSkinContact()
    {
        if(contactTailBone==null || torso==null || defeatContactTime>=0)return;
        int index=FilmTime<16 ? 4 : 5;
        var receivingShape=index==4 ? weapon : torso;if(receivingShape==null || verifiedContacts.Contains(index))return;
        float age=FilmTime-contactTimes[index];
        if(age<-.2f || age>.16f)return;
        Vector3 point=shark.TailWorldPoint;
        float gap=Story1CapsuleContact.PointSeparation(point,receivingShape,out var separation);
        if(gap<=.001f || gap>.15f)return;
        float weight=age<=0 ? Ease((age+.2f)/.2f) : 1-Ease(age/.16f);
        Vector3 target=point-separation*weight;
        Vector3 from=point-contactTailBone.position,to=target-contactTailBone.position;
        if(from.sqrMagnitude<.001f || to.sqrMagnitude<.001f)return;
        float previousPenetration=battle.WreckContact.CurrentPenetration();
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
        if(clear && battle.WreckContact.CurrentPenetration()>Mathf.Max(.003f,previousPenetration+.001f))clear=false;
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
        if(preparingEntry){HoldEntryPose();return;}
        if(unconsciousExit && !IsComplete){RenderUnconsciousExit();return;}
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
        if(!hopPlanningBusy && !unconsciousExit)
        {
            // The sliced search can take several seconds in dense debris.
            // Start each destination at the preceding supported beat rather
            // than after the exchange has already consumed most of its lead-in.
            // BeginHop still revalidates the real moving mesh at takeoff.
            int next=FilmTime<6.4f ? 0 : 1;float begin=next==0 ? 0 : 6.4f;
            float deadline=next==0 ? 4.45f:11.5f;
            if(!hopPlanningStarted[next] && FilmTime>=begin && FilmTime<deadline && FilmTime>=hopRetryAfter[next])StartCoroutine(PrepareHop(next));
        }
        Advance();
        FloatOnWaves();
        if(IsComplete)return;
    }
    public void ResolveContactsAndCamera()
    {
        if(battle==null)return;
        if(PauseSettingsMenu.IsOpen || Time.timeScale<=0 || Time.deltaTime<=0)return;
        // Finalize after every pose/solid correction, including the frame
        // that finishes either the main film or its unconscious exit.
        if(IsComplete)
        {
            if(battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)
            {
                Vector3 anchor=animator.GetBoneTransform(HumanBodyBones.Chest).position;
                PlaceCinematicCamera(shotCamera,anchor,shotCamera.transform.position-anchor,anchor);
            }
            return;
        }
        if(!initialized)return;
        if(!PauseSettingsMenu.IsOpen && Time.deltaTime>0){AlignRightHandContact();VerifyFinalContact();}
        // Execution order1300 follows the solid solver at1200. Its retained
        // bone pose can differ from the raw timeline pose committed at900.
        shark.RecordResolvedCinematicPose();
        retainedHeroRoot=hero.transform.position;retainedHeroHeading=hero.transform.rotation;
        for(int i=0;i<retainedHeroBones.Length;i++)
        {
            retainedHeroPositions[i]=retainedHeroBones[i].localPosition;
            retainedHeroRotations[i]=retainedHeroBones[i].localRotation;
        }
        hasRetainedHeroPose=true;
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        var framing=shots[0];
        for(int i=0;i<shots.Length;i++)if(FilmTime>=shots[i].start)framing=shots[i];
        Vector3 enemy=FilmTime>=14 ? shark.TailWorldPoint : shark.NoseWorldPoint;
        Vector3 focus=Vector3.Lerp((feet+head)*.5f,enemy,framing.enemyWeight);
        Vector3 orbit=Side*framing.direction.x+Vector3.up*framing.direction.y+Forward*framing.direction.z;
        if(FilmTime<0 && fromWater)
        {
            focus=(head+chest)*.5f;
            Vector3 outward=Vector3.ProjectOnPlane(board.transform.TransformDirection(entryOutwardLocal),Vector3.up).normalized;
            orbit=outward*12+Vector3.Cross(Vector3.up,outward)*4+Vector3.up*8;
        }
        if(floating && FilmTime>=19)
        {
            float reveal=Ease((FilmTime-19)/.65f);
            Vector3 bodyForward=Vector3.ProjectOnPlane(floatHeading*Vector3.forward,Vector3.up).normalized;
            Vector3 bodySide=Vector3.Cross(Vector3.up,bodyForward);
            orbit=Vector3.Slerp(orbit.normalized,(bodySide*5-bodyForward*4+Vector3.up*14).normalized,reveal);
            focus=Vector3.Lerp(focus,Vector3.Lerp(chest,head,.35f),reveal);
        }
        // The cinematic follows its authored orbit even through foreground
        // wreckage. Occlusion must not change radius, height, side or lens.
        PlaceCinematicCamera(shotCamera,chest,orbit,focus);
    }
    public const float CinematicFollowDistance=18f;
    public const float CinematicFieldOfView=48f;
    public static void PlaceCinematicCamera(Camera view,Vector3 anchor,Vector3 orbit,Vector3 focus)
    {
        float blend=1-Mathf.Exp(-4*Time.deltaTime);
        Vector3 previous=view.transform.position-anchor;
        Vector3 direction=previous.sqrMagnitude>.0001f
            ? Vector3.Slerp(previous.normalized,orbit.normalized,blend).normalized : orbit.normalized;
        view.transform.position=anchor+direction*CinematicFollowDistance;
        // Keep Sahur framed when the shark dives or breaches far from him.
        // Only the aim point is constrained; the radius and lens stay fixed.
        focus=anchor+Vector3.ClampMagnitude(focus-anchor,2f);
        view.transform.rotation=Quaternion.LookRotation(focus-view.transform.position);
        view.fieldOfView=CinematicFieldOfView;
    }
    void FloatOnWaves()
    {
        if(!floating)return;
        float delta=PauseSettingsMenu.IsOpen ? 0 : Time.deltaTime;
        var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
        var head=animator.GetBoneTransform(HumanBodyBones.Head);
        // Preserve the authored horizontal fall into open water before settling.
        if(!IsComplete){floatingChest.x=chest.position.x+floatingWoodOffset.x;floatingChest.z=chest.position.z+floatingWoodOffset.z;}
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
            SeparateFloatingBody(delta);
        }
    }
    void SeparateFloatingBody(float delta)
    {
        if(floatingProbe==null || floatingShapes==null)return;
        Vector3 correction=Vector3.zero;
        void Resolve(Story1WreckMeshCollision wood)
        {
            if(wood==null || !wood.Active)return;
            foreach(var body in floatingShapes)
            {
                Story1CapsuleContact.Segment(body,out var a,out var b,out float radius);
                a+=correction;b+=correction;var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Expand(radius*2);
                if(!bounds.Intersects(wood.WorldBounds))continue;
                floatingProbe.radius=radius;floatingProbe.height=Vector3.Distance(a,b)+radius*2;
                Vector3 at=(a+b)*.5f;Quaternion rotation=(b-a).sqrMagnitude>.0001f?Quaternion.LookRotation(b-a):Quaternion.identity;
                for(int part=0;part<wood.Parts.Length;part++)
                {
                    if(!bounds.Intersects(wood.PartBounds(part)))continue;
                    if(!Physics.ComputePenetration(floatingProbe,at,rotation,wood.Parts[part],wood.PartPosition(part),wood.PartRotation(part),out var direction,out float depth))continue;
                    Vector3 outward=Vector3.ProjectOnPlane(direction,Vector3.up);
                    if(outward.sqrMagnitude<.01f)
                    {
                        var plank=wood.GetComponent<Story1WreckPlank>();
                        outward=plank!=null ? plank.OpenWaterDirection(at) : Vector3.ProjectOnPlane(at-wood.WorldBounds.center,Vector3.up);
                    }
                    if(outward.sqrMagnitude<.001f)outward=Side;
                    Vector3 step=outward.normalized*Mathf.Min(depth+.025f,2*delta);
                    Vector3 next=Vector3.ClampMagnitude(correction+step,2*delta);at+=next-correction;correction=next;
                }
            }
        }
        foreach(var piece in battle.Boards)if(piece!=null && !piece.IsBroken)Resolve(piece.Collision);
        foreach(var piece in battle.WreckDetails)if(piece!=null)Resolve(piece.Collision);
        if(correction.sqrMagnitude<.000001f)return;
        FloatingWoodCorrection+=correction.magnitude;
        hero.transform.position+=correction;floatingChest+=correction;
        if(!IsComplete)floatingWoodOffset+=correction;
    }
    void OnDestroy()
    {
        ReleaseActors();
    }
    public void ReleaseActors()
    {
        StopAllCoroutines();
        if(floatingProbe!=null){Destroy(floatingProbe.gameObject);floatingProbe=null;}
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

