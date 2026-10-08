using System.Collections.Generic;
using System.Linq;
using Mavis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Normal Sahur combat on floating hull fragments, ending in narrative defeat.</summary>
[DefaultExecutionOrder(-150), DisallowMultipleComponent]
public sealed class Story1WreckBattle : MonoBehaviour
{
    public enum Phase { Waiting, Breaking, Fighting, Defeat, Finished, Finale }
    enum SharkPhase { Pursuit, Windup, Strike, Recovery, Recoil }
    float finaleRequestedAt=-1;
    float retryFinaleAfter;
    public SahurMotionSnapshot Handoff { get; private set; }
    public Material woodMaterial;
    [Min(1)] public float minimumFightSeconds = 20;
    [Min(1)] public float maximumFightSeconds = 35;
    [Range(.01f,.5f)] public float finalePlayerHealthRatio = .2f;
    [Min(8)] public float sharkLength = 16;
    [HideInInspector] public float finaleSharkHealthRatio = .3f;
    public Phase CurrentPhase { get; private set; }
    public int PlankCount => planks.Count;
    public int SharkAttackCount { get; private set; }
    public Story1SharkHealth SharkHealth { get; private set; }
    public bool IsTelegraphing => sharkPhase == SharkPhase.Windup;
    public int SharkRecoilCount { get; private set; }
    public Story1WreckFinale Finale { get; private set; }
    public Story1WreckEffects Effects { get; private set; }
    public Vector3 StrikePoint => strikePoint;
    public float FightElapsed { get; private set; }
    public float FinaleAfterSeconds { get; private set; }
    public int ShipFragmentCount => planks.Count + detailPieces.Count;
    public IReadOnlyList<Story1WreckPlank> Boards => planks;
    public IReadOnlyList<Story1WreckDebris> WreckDetails => detailPieces;
    public int BrokenBoardCount { get; private set; }
    public int BoardDamageCount { get; private set; }
    public Story1SharkWreckContact WreckContact { get; private set; }
    public bool BoardAttackActive=>CurrentPhase==Phase.Fighting && sharkPhase==SharkPhase.Strike && sharkAge>=.30f && sharkAge<.85f;
    public bool TailBoardAttack=>tailAttack;
    public int BoardAttackSerial=>SharkAttackCount;
    public bool BoardCarriesPlayer(Story1WreckPlank board)=>rider!=null && (rider.DrivenBoard==board || rider.Support()==board);

    Story1SharkCollisionSequence sequence;
    ThirdPersonPlayerController player;
    Story1WreckRider rider;
    PlayerHealth playerHealth;
    CharacterController capsule;
    OceanWorld ocean;
    Transform shark;
    TralaleroSwimAnimator sharkAnimator;
    BoxCollider sharkHitbox;
    readonly List<Story1WreckPlank> planks = new List<Story1WreckPlank>();
    GameObject debrisRoot, hud;
    Image blackout, healthFill;
    Text warning, bossName, controls;
    Story1ShipWreckAsset shipWreck;
    Story1ShipWreckAsset.Fragment[] deckSources;
    Matrix4x4 shipModelToWorld;
    Vector3 shipModelScale;
    readonly List<Story1WreckDebris> detailPieces = new List<Story1WreckDebris>();
    readonly Dictionary<Story1ShipWreckAsset.Fragment,GameObject> preparedPieces = new Dictionary<Story1ShipWreckAsset.Fragment,GameObject>();
    int warmIndex;
    CapsuleCollider headHitbox;
    int shownHealth=-1;
    float surfacedHeight;
    Story1WreckPlank strikeBoard;
    Vector3 strikeDeckPoint, strikeStartBody;
    Quaternion strikeStartRotation;
    Sprite fillSprite;
    Bounds sharkLocalBounds;
    Vector3 center, heading, side, passengerStart, breakSharkStart, strikePoint, defeatStart;
    Quaternion passengerRotation, breakSharkRotation;
    float phaseAge, sharkAge;
    float lastRecoilTime = -10;
    Vector3 recoilStart, recoilEnd;
    bool hullContactStarted, tailAttack;
    bool shattered, hitApplied, sharkPrepared;
    Vector3 previousAttackPoint;
    bool damageApplied;
    SharkPhase sharkPhase;
    AudioSource sound;
    AudioClip shatterSound, tidalSound;
    const float TailWindupSeconds = 1.3f;
    const float ShipTailContactTime = TailWindupSeconds + TralaleroSwimAnimator.TailContactTime;
    const float FractureDelay = .14f;
    const float BreakFlightSeconds = 2.4f;
    bool breakAirPose;
    Vector3 breakShipPosition, breakCameraPosition, breakCameraTarget, breakBodyStart, breakTailBody, breakTailWindupBody;
    Quaternion breakShipRotation, breakTailRotation;
    bool tailActionStarted, breachStarted;
    System.Random scatter;
    Story1WreckPlank landingBoard;
    public float ImpactNoseGap { get; private set; } = float.PositiveInfinity;
    public float ImpactTailGap { get; private set; } = float.PositiveInfinity;
    public float MaximumHullKickDegrees { get; private set; }

    public void Begin(Story1SharkCollisionSequence owner, Vector3 travelHeading)
    {
        if (CurrentPhase != Phase.Waiting) return;
        sequence = owner; player = owner.sahur; capsule = player.GetComponent<CharacterController>();
        playerHealth = player.GetComponent<PlayerHealth>();
        ocean = FindFirstObjectByType<OceanWorld>(); shark = owner.swimmer;
        sharkAnimator = shark.GetComponent<TralaleroSwimAnimator>();
        heading = travelHeading; side = Vector3.Cross(Vector3.up, heading);
        center = player.transform.position; center.y = ocean != null ? ocean.oceanHeight : player.seaLevel;
        passengerStart = player.transform.position; passengerRotation = player.transform.rotation;
        breakSharkStart = shark.position; breakSharkRotation = shark.rotation;
        sharkLocalBounds = sharkAnimator.RootMeshBounds;
        breakBodyStart = SharkBody;
        sharkAnimator.SampleCinematicAction("Ship_Smash",TralaleroSwimAnimator.TailContactTime);
        Vector3 liveShipTail=shark.InverseTransformPoint(sharkAnimator.TailWorldPoint);
        sharkAnimator.SampleCinematicAction("Swim",0);
        breakTailRotation = Quaternion.LookRotation(-heading) * Quaternion.Euler(0,0,-6);
        breakTailBody = owner.HullImpactPoint - breakTailRotation * Vector3.Scale(
            liveShipTail-sharkLocalBounds.center,shark.lossyScale);
        breakTailWindupBody = breakTailBody + side * 4;
        float windupWater = ocean != null ? ocean.SampleSurfaceHeight(breakTailWindupBody,Time.time) : center.y;
        breakTailWindupBody.y = sharkAnimator.SurfaceRootY(windupWater,breakTailRotation) +
            (breakTailRotation * Vector3.Scale(sharkLocalBounds.center,shark.lossyScale)).y;
        breakShipPosition = owner.ship.transform.position; breakShipRotation = owner.ship.transform.rotation;
        owner.ship.enabled = false;
        var passenger = player.GetComponent<Story1SahurPassenger>(); if (passenger != null) passenger.enabled = false;
        var follow = owner.storyCamera.GetComponent<ShipFollowCamera>(); if (follow != null) follow.enabled = false;
        player.ExternalControlLock = true; player.enabled = false; capsule.enabled = false;
        player.Motion.Claim(this,SahurControlState.Cinematic);
        player.GetComponent<SahurAttack>().SuspendForSwimming(); player.GetComponent<SahurAttack>().enabled = false;
        player.GetComponent<SahurBoomerang>().enabled = false;
        if (player.GetComponent<GameSaveExcluded>() == null) player.gameObject.AddComponent<GameSaveExcluded>();
        playerHealth.NarrativeDefeatHandler = ProtectStoryFight;
        if (owner.awakening.plank != null) owner.awakening.plank.gameObject.SetActive(false);
        CreateUI(); CurrentPhase = Phase.Breaking; phaseAge = 0;
        sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0;
        shatterSound = Resources.Load<AudioClip>("StoryBattle/Audio/EncounterStart");
        tidalSound = Resources.Load<AudioClip>("StoryBattle/Audio/TidalHit");
        breakCameraTarget = breakShipPosition + heading * 24 + Vector3.up * 22;
        breakCameraPosition = breakShipPosition + side * 140 + heading * 65 + Vector3.up * 48;
        owner.storyCamera.transform.SetPositionAndRotation(breakCameraPosition,
            Quaternion.LookRotation(breakCameraTarget - breakCameraPosition));
        var asset = Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        Effects = gameObject.AddComponent<Story1WreckEffects>();Effects.Initialize(asset.hulls[0].material,asset.hulls[0].mesh);
    }

    void Update()
    {
        if(CurrentPhase==Phase.Waiting){WarmWreckage(6);return;}
        Tick(Time.deltaTime);
    }
    void WarmWreckage(int budget)
    {
        if(shipWreck==null)shipWreck=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        if(shipWreck==null)return;
        if(debrisRoot==null)
        {
            debrisRoot=new GameObject("Shattered Ship - Floating Hull Boards");
            SceneManager.MoveGameObjectToScene(debrisRoot,gameObject.scene);debrisRoot.SetActive(false);
        }
        int boards=shipWreck.decks.Length;
        int hullEnd=boards+shipWreck.hulls.Length;
        int total=hullEnd+shipWreck.details.Length;
        while(budget-->0 && warmIndex<total)
        {
            int index=warmIndex++;
            var source=index<shipWreck.decks.Length ? shipWreck.decks[index] : index<hullEnd ? shipWreck.hulls[index-boards] : shipWreck.details[index-hullEnd];
            var go=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(debrisRoot.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=source.mesh;go.GetComponent<MeshRenderer>().sharedMaterial=source.material;
            if(index<boards)go.AddComponent<Story1WreckPlank>();else go.AddComponent<Story1WreckDebris>();
            preparedPieces.Add(source,go);
        }
    }
    void LateUpdate()
    {
        if(CurrentPhase==Phase.Breaking && !sharkPrepared)PoseBreakShark(phaseAge);
        if(headHitbox!=null && sharkAnimator!=null)
            headHitbox.transform.SetPositionAndRotation(sharkAnimator.NoseWorldPoint,shark.rotation);
    }
    public void ApplySharkContactDamage()
    {
        if(headHitbox!=null)headHitbox.transform.SetPositionAndRotation(sharkAnimator.NoseWorldPoint,shark.rotation);
        if(CurrentPhase==Phase.Fighting && sharkPhase==SharkPhase.Strike)
        {
            Vector3 tip=tailAttack ? sharkAnimator.TailWorldPoint : sharkAnimator.NoseWorldPoint;
            float lo=tailAttack ? .52f : .36f,hi=tailAttack ? .78f : .58f;
            if(!damageApplied && sharkAge>=lo && sharkAge<=hi)
            {
                Vector3 center=capsule.transform.TransformPoint(capsule.center);
                float radius=capsule.radius*Mathf.Abs(capsule.transform.lossyScale.x);
                float half=Mathf.Max(0,capsule.height*Mathf.Abs(capsule.transform.lossyScale.y)*.5f-radius);
                float reach=radius+(tailAttack ? 1.4f : 1.1f);
                if(SegmentDistanceSquared(previousAttackPoint,tip,center-Vector3.up*half,center+Vector3.up*half)<=reach*reach)
                {
                    damageApplied=true;
                    playerHealth.ApplyDamage(Mathf.Min(28,Mathf.Max(0,playerHealth.currentHealth-1)),tip);
                    sequence.storyCamera.GetComponent<CombatCameraShake>()?.Pulse(.1f,.18f);
                }
            }
            previousAttackPoint=tip;
        }
    }
    internal void Tick(float deltaTime)
    {
        if (CurrentPhase == Phase.Waiting || CurrentPhase == Phase.Finished || PauseSettingsMenu.IsOpen || deltaTime <= 0) return;
        phaseAge += deltaTime;
        if (CurrentPhase == Phase.Breaking) { BreakShip(deltaTime); return; }
        MovePlanks(deltaTime);
        if (CurrentPhase == Phase.Defeat) { FinishDefeat(); return; }
        if (CurrentPhase == Phase.Finale) return;
        FightElapsed += deltaTime;
        bool playerLowHealth = playerHealth.maxHealth > 0 &&
            playerHealth.currentHealth <= playerHealth.maxHealth * finalePlayerHealthRatio;
        if (Time.time>=retryFinaleAfter && (playerLowHealth || FightElapsed >= FinaleAfterSeconds))
        {
            if(finaleRequestedAt<0)finaleRequestedAt=Time.time;
            var attack=player.GetComponent<SahurAttack>();
            bool supported=player.Swimming || player.CanUseGroundAttack;
            // An airborne player keeps their physical trajectory until landing/splashing.
            if(supported && (rider==null || !rider.IsClimbing) &&
                (attack.SafeForCinematic || Time.time-finaleRequestedAt>=1)){BeginFinale();return;}
        }
        TickShark(deltaTime);
        healthFill.fillAmount = SharkHealth.Ratio;
        int hp=Mathf.CeilToInt(SharkHealth.currentHealth);
        if(hp!=shownHealth){shownHealth=hp;bossName.text="TRALALERO TRALALA  "+hp+" / "+Mathf.CeilToInt(SharkHealth.maxHealth);}
        warning.text = sharkPhase == SharkPhase.Windup ? "鲨鱼正在蓄力，快闪开！" :
            sharkPhase == SharkPhase.Recoil || sharkPhase == SharkPhase.Recovery ? "趁现在反击！" :
            FightElapsed < 5 ? "在木板上迎战鲨鱼。使用平时的攻击和闪避。" : "";
        if (CurrentPhase == Phase.Fighting) controls.text = rider != null ? rider.Hint : "";
    }

    void BreakShip(float deltaTime)
    {
        if(!sharkPrepared)PoseBreakShark(phaseAge);
        if (!breachStarted && phaseAge >= TailWindupSeconds+.25f)
        {
            breachStarted = true;
            Vector3 at = sharkAnimator.TailWorldPoint; at.y = center.y;
            Effects.Impact(at, -side, 2.4f);
            if(tidalSound!=null)sound.PlayOneShot(tidalSound,.6f);
        }
        if(!shattered)
        {
            float close=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1.65f,phaseAge));
            Vector3 focus=Vector3.Lerp(breakCameraTarget,sequence.HullImpactPoint+Vector3.up*5,close*.7f);
            Vector3 shot=Vector3.Lerp(breakCameraPosition,sequence.HullImpactPoint+side*85+heading*35+Vector3.up*30,close);
            float blend=1-Mathf.Exp(-5*deltaTime);
            sequence.storyCamera.transform.position=Vector3.Lerp(sequence.storyCamera.transform.position,shot,blend);
            sequence.storyCamera.transform.rotation=Quaternion.Slerp(sequence.storyCamera.transform.rotation,Quaternion.LookRotation(focus-sequence.storyCamera.transform.position),blend);
        }
        if (phaseAge < ShipTailContactTime)
            return;
        if (!hullContactStarted)
        {
            hullContactStarted = true;
            // Evaluate the crossed contact cue at its true time, without moving a marker into place.
            PoseBreakShark(ShipTailContactTime);
            ImpactTailGap = Vector3.Distance(sharkAnimator.TailWorldPoint,sequence.HullImpactPoint);
            ImpactNoseGap = Vector3.Distance(sharkAnimator.NoseWorldPoint,sequence.HullImpactPoint);
            player.CharacterAnimator.CrossFadeInFixedTime("Finale Stagger", .03f);
            if (shatterSound != null) sound.PlayOneShot(shatterSound,1f);
            Effects.ShipImpact(sequence.HullImpactPoint, -side);
            var shake = sequence.storyCamera.GetComponent<CombatCameraShake>();
            if (shake == null) shake = sequence.storyCamera.gameObject.AddComponent<CombatCameraShake>();
            shake.PulseStoryImpact(.6f, .7f);
        }
        if (!shattered)
        {
            float kick = Mathf.Clamp01((phaseAge - ShipTailContactTime) / FractureDelay);
            Quaternion hullKick = Quaternion.AngleAxis(9 * kick, Vector3.Cross(-side,Vector3.up)) *
                Quaternion.AngleAxis(-7 * kick, heading);
            MaximumHullKickDegrees = Quaternion.Angle(Quaternion.identity,hullKick);
            sequence.ship.transform.SetPositionAndRotation(breakShipPosition - side * (3.5f * kick) + Vector3.up * (2.2f * kick),
                hullKick * breakShipRotation);
            player.transform.position = passengerStart - side * (3.5f * kick) + Vector3.up * (3.5f * kick);
            if(kick < 1) return;
        }
        if (!shattered)
        {
            shattered = true;
            CreateWreckage();
            sequence.ship.gameObject.SetActive(false);
            if (sequence.awakening.looseShipDecoration != null) sequence.awakening.looseShipDecoration.SetActive(false);
            Physics.SyncTransforms();
        }
        float progress = Mathf.Clamp01((phaseAge - ShipTailContactTime - FractureDelay) / BreakFlightSeconds);
        if(!breakAirPose){breakAirPose=true;player.CharacterAnimator.CrossFadeInFixedTime("Jump Loop",.10f);}
        // The full-size shark follows through with its tail and dives before taking its combat position.
        if(!sharkPrepared && progress>=1){sharkPrepared=true;sharkAnimator.ReleaseCinematicAnimation();PrepareShark();}
        foreach(var piece in planks)piece.CommitPose();
        Vector3 wreckCenter=Vector3.zero;
        foreach(var piece in planks)wreckCenter+=piece.transform.position;
        wreckCenter/=planks.Count;
        float cameraBlend=1-Mathf.Exp(-2.5f*deltaTime);
        float handoff=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1,progress));
        Vector3 wreckFocus=Vector3.Lerp(wreckCenter+heading*24+Vector3.up*22,player.transform.position+Vector3.up*3,handoff);
        Vector3 wreckShot=Vector3.Lerp(wreckCenter+side*120+heading*55+Vector3.up*44,player.transform.position+side*22-heading*12+Vector3.up*12,handoff);
        sequence.storyCamera.transform.position=Vector3.Lerp(sequence.storyCamera.transform.position,wreckShot,cameraBlend);
        sequence.storyCamera.transform.rotation=Quaternion.Slerp(sequence.storyCamera.transform.rotation,
            Quaternion.LookRotation(wreckFocus-sequence.storyCamera.transform.position),cameraBlend);
        if(progress>.7f && (landingBoard==null || !landingBoard.IsBoardable))
            landingBoard=FindLandingBoard(passengerStart);
        var landing=landingBoard!=null ? landingBoard : planks[0];
        Vector3 destination = landing.BoardingPoint(landing.transform.position) + Vector3.up * (.15f - player.LowestFootWorldOffset);
        player.transform.position = Vector3.Lerp(passengerStart - side*3.5f + Vector3.up*3.5f, destination,
            progress) + Vector3.up * (.5f*9.8f*BreakFlightSeconds*BreakFlightSeconds*progress*(1-progress));
        if (progress < 1) return;
        Physics.SyncTransforms();
        capsule.enabled = true;
        player.RestoreSavedPose(destination, Quaternion.LookRotation(heading));
        player.ExternalControlLock = player.ExternalMovementLock = false;
        player.Motion.Release(this);
        player.enabled = true; player.GetComponent<SahurAttack>().enabled = true; player.GetComponent<SahurBoomerang>().enabled = true;
        playerHealth.GrantProtection(1.25f);
        player.NotifyPlatformLanding();
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        CurrentPhase = Phase.Fighting; phaseAge = 0; sharkPhase = SharkPhase.Pursuit; sharkAge = 0;
        FightElapsed = 0;
        float minimum = Mathf.Max(1, minimumFightSeconds);
        FinaleAfterSeconds = ScatterRange(minimum, Mathf.Max(minimum, maximumFightSeconds));
        rider = player.GetComponent<Story1WreckRider>();
        if (rider == null) rider = player.gameObject.AddComponent<Story1WreckRider>();
        rider.Initialize(player, planks, ocean,landing,detailPieces);
    }

    void PoseBreakShark(float time)
    {
        float age=time-TailWindupSeconds;
        sharkAnimator.SampleCinematicAction(age<0 ? "Swim" : "Ship_Smash",age<0 ? time : age);
        float windup=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/TailWindupSeconds));
        float swing=Mathf.Clamp01(age/TralaleroSwimAnimator.TailContactTime);
        Quaternion coiled=breakTailRotation*Quaternion.Euler(0,-25,0),rotation;
        Vector3 body;
        if(age<0)
        {
            rotation=Quaternion.Slerp(breakSharkRotation,coiled,windup);
            body=Vector3.Lerp(breakBodyStart,breakTailWindupBody,windup);
        }
        else if(time<=ShipTailContactTime)
        {
            rotation=Quaternion.Slerp(coiled,breakTailRotation,swing*swing);
            body=Vector3.Lerp(breakTailWindupBody,breakTailBody,swing*swing);
        }
        else
        {
            float after=time-ShipTailContactTime,followThrough=4*(1-Mathf.Exp(-after/.35f));
            float retreat=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,2.2f,after));
            rotation=Quaternion.Slerp(breakTailRotation*Quaternion.Euler(0,12*Mathf.Clamp01(after/.55f),0),Quaternion.LookRotation(side)*Quaternion.Euler(12,0,0),retreat);
            body=breakTailBody-side*followThrough+(side*35-heading*12-Vector3.up*24)*retreat;
        }
        PoseCinematicShark(body,rotation,absoluteHeight:true);
    }

    void CreateWreckage()
    {
        WarmWreckage(int.MaxValue);
        shipWreck = Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        if (shipWreck == null || shipWreck.decks.Length != 20 || shipWreck.hulls.Length != 24)
            throw new System.InvalidOperationException("The Blender-cut ship wreck asset is missing or incomplete.");
        shipModelToWorld = Story1ShipWreckAsset.ModelToWorld(sequence.ship.transform);
        shipModelScale = sequence.ship.transform.lossyScale * .01f;
        deckSources = shipWreck.decks.OrderBy(p => (shipModelToWorld.MultiplyPoint3x4(p.sourceCenter) - passengerStart).sqrMagnitude).ToArray();
        scatter = new System.Random(System.Guid.NewGuid().GetHashCode());
        foreach(var source in deckSources)AddPlank(source);
        foreach(var source in shipWreck.hulls)AddShipDetail(source);
        foreach(var source in shipWreck.details)AddShipDetail(source);
        debrisRoot.SetActive(true);
    }
    Story1WreckPlank FindLandingBoard(Vector3 from,float minimumLength=0)
    {
        Story1WreckPlank best=null;float score=float.PositiveInfinity;
        // Prefer real flat deck sections; tall hull bounds are poor footing and block swings.
        for(int i=0;i<Mathf.Min(20,planks.Count);i++)
        {
            var board=planks[i];
            if(!board.IsBoardable || board.Dimensions.z<minimumLength || Mathf.Abs(board.transform.up.y)<.82f)continue;
            var body=board.GetComponent<Rigidbody>();
            float value=board.EdgeDistance(from)+Mathf.Max(0,board.Dimensions.y-1.1f)*35+
                Mathf.Abs(body.linearVelocity.y)*5+(1-Mathf.Abs(board.transform.up.y))*25;
            Vector3 footing=board.BoardingPoint(from);
            if(ocean!=null)value+=Mathf.Max(0,ocean.SampleSurfaceHeight(footing,Time.time)-footing.y+.15f)*20;
            if(value<score){score=value;best=board;}
        }
        return best;
    }

    float ScatterRange(float min,float max) => Mathf.Lerp(min,max,(float)scatter.NextDouble());
    void AddPlank(Story1ShipWreckAsset.Fragment source)
    {
        var go=preparedPieces[source];go.name="Hull board "+(planks.Count+1)+" - "+source.name;
        Vector3 from=shipModelToWorld.MultiplyPoint3x4(source.sourceCenter);
        Vector3 size=Vector3.Scale(source.sourceSize,shipModelScale);
        var piece=go.GetComponent<Story1WreckPlank>();
        piece.SetFractureSource(source);
        piece.Initialize(from,Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source),size,ocean,FragmentVelocity(from),FragmentSpin());
        planks.Add(piece);
    }
    Vector3 FragmentVelocity(Vector3 from)
    {
        float near=1-Mathf.Clamp01(Vector3.Distance(from,sequence.HullImpactPoint)/145);
        float lateral=Vector3.Dot(from-breakShipPosition,heading);
        float sign=Mathf.Abs(lateral)>.2f ? Mathf.Sign(lateral) : (ScatterRange(0,1)>.5f ? 1 : -1);
        Vector3 launch=-side*(ScatterRange(7,15)+near*12)+heading*(sign*(ScatterRange(3,9)+near*6)+ScatterRange(-3,3));
        launch=Quaternion.AngleAxis(ScatterRange(-32,32),Vector3.up)*launch*ScatterRange(.72f,1.25f);
        return launch+Vector3.up*(ScatterRange(4,12)+near*4);
    }
    Vector3 FragmentSpin() => side*ScatterRange(-75,75)+heading*ScatterRange(-65,65)+Vector3.up*ScatterRange(-45,45);
    void AddShipDetail(Story1ShipWreckAsset.Fragment source)
    {
        var go=preparedPieces[source];
        Vector3 from=shipModelToWorld.MultiplyPoint3x4(source.sourceCenter);
        Vector3 size=Vector3.Scale(source.sourceSize,shipModelScale);
        go.transform.localScale=size;
        go.transform.SetPositionAndRotation(from,Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source));
        bool floats=source.material.name=="acmat_1" || source.material.name=="acmat_5" || source.material.name=="acmat_7" || source.material.name=="acmat_9";
        var body=go.GetComponent<Story1WreckDebris>();body.Initialize(ocean,size,floats,FragmentVelocity(from),FragmentSpin());
        body.ConfigureObstacle(source);
        detailPieces.Add(body);
    }
    void MovePlanks(float deltaTime)
    {
        Vector3 feet=player.transform.position+Vector3.up*player.LowestFootWorldOffset;
        // Footing belongs to the last rendered deck pose, before this frame's wave movement.
        if(CurrentPhase==Phase.Fighting && !player.Swimming && capsule.enabled && (rider==null || (!rider.IsSurfing && !rider.IsClimbing)))
            foreach(var piece in planks)if(piece.SupportedPreviousPose(feet)){capsule.Move(piece.CarryDisplacement(feet));break;}
        if(CurrentPhase==Phase.Fighting && rider!=null)rider.BeforePlanks(deltaTime);
        foreach(var piece in planks)piece.CommitPose();
        if(CurrentPhase==Phase.Fighting && rider!=null)rider.AfterPlanks(deltaTime);
    }

    void PrepareShark()
    {
        var mesh = shark.GetComponentInChildren<MeshFilter>();
        Bounds b = mesh.sharedMesh.bounds; sharkLocalBounds = new Bounds(); bool first = true;
        for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
        {
            Vector3 at = shark.InverseTransformPoint(mesh.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x,y,z))));
            if (first) { sharkLocalBounds = new Bounds(at, Vector3.zero); first = false; } else sharkLocalBounds.Encapsulate(at);
        }
        shark.localScale *= sharkLength / Mathf.Max(.01f, sharkLocalBounds.size.z * Mathf.Abs(shark.lossyScale.z));
        shark.gameObject.tag = "Enemy";
        SharkHealth = shark.gameObject.AddComponent<Story1SharkHealth>(); SharkHealth.maxHealth = SharkHealth.currentHealth = 360;
        SharkHealth.OnDamaged = new UnityEvent<float>(); SharkHealth.OnDamaged.AddListener(OnSharkHit);
        sharkHitbox = shark.gameObject.AddComponent<BoxCollider>(); sharkHitbox.isTrigger = true;
        sharkHitbox.center = sharkLocalBounds.center; sharkHitbox.size = sharkLocalBounds.size * .96f;
        var head=new GameObject("Shark head contact",typeof(CapsuleCollider));head.transform.SetParent(shark,false);
        Vector3 scale=shark.lossyScale;
        head.transform.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        headHitbox=head.GetComponent<CapsuleCollider>();headHitbox.isTrigger=true;headHitbox.direction=2;
        headHitbox.radius=1.1f;headHitbox.height=3.2f;headHitbox.center=Vector3.back*.65f;
        head.transform.SetPositionAndRotation(sharkAnimator.NoseWorldPoint,shark.rotation);SharkHealth.ConfigureHead(headHitbox);
        Vector3 landing=landingBoard!=null ? landingBoard.BoardingPoint(landingBoard.transform.position) : player.transform.position;
        Quaternion facing=Quaternion.LookRotation(-heading);
        float water=ocean!=null ? ocean.SampleSurfaceHeight(landing,Time.time) : center.y;
        surfacedHeight=sharkAnimator.SurfaceRootY(water,facing)+(facing*Vector3.Scale(sharkLocalBounds.center,shark.lossyScale)).y;
        PlaceShark(BodyForNose(landing+heading*9,facing),facing);
        WreckContact=gameObject.AddComponent<Story1SharkWreckContact>();WreckContact.Initialize(this,sharkAnimator);
        ShowFightHUD();
    }

    void PlaceShark(Vector3 body, Quaternion rotation)
    {
        float water=ocean != null ? ocean.SampleSurfaceHeight(body, Time.time) : center.y;
        body.y = sharkAnimator.SurfaceRootY(water,rotation) + (rotation*Vector3.Scale(sharkLocalBounds.center,shark.lossyScale)).y;
        float exposure=sharkPhase==SharkPhase.Windup ? Mathf.SmoothStep(0,1,sharkAge/.65f) :
            sharkPhase==SharkPhase.Recovery ? 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,2.4f,sharkAge)) :
            sharkPhase==SharkPhase.Strike ? 1 : sharkPhase==SharkPhase.Recoil ? 1-Mathf.SmoothStep(0,1,sharkAge/.8f) : 0;
        float feetY=player.transform.position.y+player.LowestFootWorldOffset;
        Vector3 noseOffset=rotation*Vector3.Scale(shark.InverseTransformPoint(sharkAnimator.NoseWorldPoint)-sharkLocalBounds.center,shark.lossyScale);
        float strikeHeight=capsule!=null ? Mathf.Clamp(capsule.bounds.size.y*.4f,1.35f,2.4f) : 1.8f;
        float rise=Mathf.Max(0,feetY+strikeHeight-(body.y+noseOffset.y));
        float desired=body.y+rise*exposure;
        surfacedHeight=Mathf.Lerp(surfacedHeight,desired,1-Mathf.Exp(-8*Time.deltaTime));body.y=surfacedHeight;
        shark.rotation=rotation;shark.position=body-shark.TransformVector(sharkLocalBounds.center);
    }
    Vector3 BodyForNose(Vector3 nose,Quaternion rotation)
    {
        Vector3 offset=shark.InverseTransformPoint(sharkAnimator.NoseWorldPoint)-sharkLocalBounds.center;
        return nose-rotation*Vector3.Scale(offset,shark.lossyScale);
    }
    void MoveSharkNose(Vector3 target,Quaternion rotation,float distance)
    {
        Vector3 nose=sharkAnimator.NoseWorldPoint;target.y=nose.y;
        PlaceShark(BodyForNose(Vector3.MoveTowards(nose,target,distance),rotation),rotation);
    }
    public void PoseSharkNoseAt(Vector3 nose,Quaternion rotation)
    {
        PoseCinematicShark(BodyForNose(nose,rotation),rotation,absoluteHeight:true);
    }

    void TickShark(float deltaTime)
    {
        sharkAge += deltaTime;
        Vector3 body = shark.TransformPoint(sharkLocalBounds.center);
        Vector3 feet = player.transform.position + Vector3.up * player.LowestFootWorldOffset;
        Vector3 toward = Vector3.ProjectOnPlane(feet - body, Vector3.up).normalized;
        if (toward.sqrMagnitude < .1f) toward = shark.forward;
        if((sharkPhase==SharkPhase.Windup || sharkPhase==SharkPhase.Strike) && strikeBoard!=null)
            strikePoint=strikeBoard.transform.TransformPoint(strikeDeckPoint);
        if (sharkPhase == SharkPhase.Pursuit)
        {
            Quaternion facing = Quaternion.RotateTowards(shark.rotation, Quaternion.LookRotation(toward), 130 * deltaTime);
            MoveSharkNose(feet-toward*2.2f,facing,15*deltaTime);
            if (sharkAge > 1.3f && Vector3.ProjectOnPlane(sharkAnimator.NoseWorldPoint - feet, Vector3.up).magnitude < 7)
            {
                sharkPhase=SharkPhase.Windup;sharkAge=0;strikePoint=feet;hitApplied=false;damageApplied=false;
                strikeBoard=rider!=null ? rider.Support() : null;
                if(strikeBoard!=null)strikeDeckPoint=strikeBoard.transform.InverseTransformPoint(strikeBoard.BoardingPoint(SharkBody));
                tailAttack=SharkAttackCount%2==0;sharkAnimator.PlayThreat();
            }
        }
        else if (sharkPhase == SharkPhase.Windup)
        {
            PlaceShark(body, shark.rotation);
            if (tailAttack) shark.rotation = Quaternion.RotateTowards(shark.rotation, Quaternion.LookRotation(toward) * Quaternion.Euler(0,90,0),120*deltaTime);
            if(sharkAge>=.95f)
            {
                sharkPhase=SharkPhase.Strike;sharkAge=0;strikeStartBody=SharkBody;strikeStartRotation=shark.rotation;
                if(tailAttack)sharkAnimator.PlayTailSlap();else sharkAnimator.PlayBite();
                previousAttackPoint=tailAttack ? sharkAnimator.TailWorldPoint : sharkAnimator.NoseWorldPoint;
            }
        }
        else if (sharkPhase == SharkPhase.Strike)
        {
            if (tailAttack)
            {
                Vector3 contact=TailContactBody(strikePoint+Vector3.up*(strikeBoard!=null ? .15f : 1.8f),strikeStartRotation);
                float swing=Mathf.SmoothStep(0,1,Mathf.Clamp01(sharkAge/TralaleroSwimAnimator.TailContactTime));
                PoseCinematicShark(Vector3.Lerp(strikeStartBody,contact,swing),strikeStartRotation,absoluteHeight:true);
            }
            else
            {
                Vector3 goal=BodyForNose(strikePoint-toward*.65f,Quaternion.LookRotation(toward));
                PlaceShark(Vector3.MoveTowards(body, goal, 22 * deltaTime), Quaternion.LookRotation(toward));
            }
            if (!hitApplied && sharkAge >= (tailAttack ? TralaleroSwimAnimator.TailContactTime : .45f))
            {
                hitApplied = true; SharkAttackCount++;
                if (tidalSound != null) sound.PlayOneShot(tidalSound,.6f);
                Effects.Impact(strikePoint, toward, 1.1f);
            }
            if (sharkAge > 1.1f && CurrentPhase == Phase.Fighting) { sharkPhase = SharkPhase.Recovery; sharkAge = 0;  }
        }
        else if (sharkPhase == SharkPhase.Recovery)
        {
            Quaternion facing=Quaternion.RotateTowards(shark.rotation,Quaternion.LookRotation(toward),170*deltaTime);
            MoveSharkNose(feet-toward*1.8f,facing,18*deltaTime);
            if (sharkAge > 2.4f) { sharkPhase = SharkPhase.Pursuit; sharkAge = 0; }
        }
        else
        {
            PlaceShark(Vector3.Lerp(recoilStart,recoilEnd,Mathf.SmoothStep(0,1,sharkAge/.65f)),Quaternion.LookRotation(toward)*Quaternion.Euler(0,0,Mathf.Sin(sharkAge/.7f*Mathf.PI)*-8));
            if(sharkAge>.8f){sharkPhase=SharkPhase.Pursuit;sharkAge=.7f;}
        }
    }

    // Closest distance between the swept attack marker and the player's capsule axis.
    static float SegmentDistanceSquared(Vector3 p,Vector3 q,Vector3 a,Vector3 b)
    {
        Vector3 d=q-p,e=b-a,r=p-a;float dd=Vector3.Dot(d,d),ee=Vector3.Dot(e,e),f=Vector3.Dot(e,r),s,t;
        if(dd<.000001f){s=0;t=ee>.000001f ? Mathf.Clamp01(f/ee) : 0;}
        else
        {
            float c=Vector3.Dot(d,r);
            if(ee<.000001f){t=0;s=Mathf.Clamp01(-c/dd);}
            else
            {
                float de=Vector3.Dot(d,e),den=dd*ee-de*de;
                s=den>.000001f ? Mathf.Clamp01((de*f-c*ee)/den) : 0;t=(de*s+f)/ee;
                if(t<0){t=0;s=Mathf.Clamp01(-c/dd);}else if(t>1){t=1;s=Mathf.Clamp01((de-c)/dd);}
            }
        }
        return (p+d*s-a-e*t).sqrMagnitude;
    }

    void OnSharkHit(float damage)
    {
        if (CurrentPhase == Phase.Fighting && Time.time-lastRecoilTime>1.8f)
        {
            sharkAnimator.PlayRecoil();
            lastRecoilTime=Time.time;SharkRecoilCount++;sharkPhase=SharkPhase.Recoil;sharkAge=0;
            recoilStart=SharkBody;Vector3 away=Vector3.ProjectOnPlane(recoilStart-player.transform.position,Vector3.up).normalized;
            recoilEnd=recoilStart+away*3.2f;Effects.Impact(sharkAnimator.NoseWorldPoint,away,.55f);
        }
        else if(CurrentPhase==Phase.Finale)sharkAnimator.PlayRecoil();
    }

    public Vector3 SharkBody => shark.TransformPoint(sharkLocalBounds.center);
    public bool DamageBoard(Story1WreckPlank board,float damage,Vector3 point,Vector3 impulse)
    {
        if(board==null || board.IsBroken || !board.gameObject.activeInHierarchy)return false;
        var body=board.GetComponent<Rigidbody>();
        body.AddForceAtPosition(impulse,point,ForceMode.VelocityChange);
        BoardDamageCount++;
        bool breaks=board.Damage(damage);
        Effects.Impact(point,impulse.normalized,breaks ? 1.1f : .45f,splinters:true);
        if(!breaks)return false;
        SpawnBrokenFragments(board.FractureSource,board.transform,body.GetPointVelocity,point,impulse);
        if(rider!=null)rider.OnBoardBroken(board);
        board.MarkBroken();BrokenBoardCount++;return true;
    }
    public bool DamageWreckDetail(Story1WreckDebris piece,float damage,Vector3 point,Vector3 impulse)
    {
        if(piece==null || piece.IsBroken || piece.Obstacle==null || !piece.gameObject.activeInHierarchy)return false;
        BoardDamageCount++;bool breaks=piece.Damage(damage);
        Effects.Impact(point,impulse.normalized,breaks ? 1.1f : .45f,splinters:true);
        if(!breaks)return false;
        SpawnBrokenFragments(piece.FractureSource,piece.transform,piece.PointVelocity,point,impulse);
        piece.MarkBroken();BrokenBoardCount++;return true;
    }
    void SpawnBrokenFragments(Story1ShipWreckAsset.Fragment source,Transform original,System.Func<Vector3,Vector3> pointVelocity,Vector3 point,Vector3 impulse)
    {
        var shards=source.splinters;Vector3 scale=original.lossyScale;
        Quaternion rotation=original.rotation;
        for(int i=0;i<shards.Length;i++)
        {
            var shard=shards[i];Vector3 at=original.TransformPoint(shard.localCenter);
            Vector3 size=Vector3.Scale(scale,shard.localSize);
            var go=new GameObject(original.name+" splinter "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(debrisRoot.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=shard.mesh;go.GetComponent<MeshRenderer>().sharedMaterial=source.material;
            Vector3 away=(at-point).normalized;
            Vector3 velocity=pointVelocity(at)+impulse*.45f+away*ScatterRange(2,5)+Vector3.up*ScatterRange(.5f,2.5f);
            if(Mathf.Min(size.x,size.z)>2 && Mathf.Max(size.x,size.z)>4 && size.y<2)
            {
                var piece=go.AddComponent<Story1WreckPlank>();piece.Initialize(at,rotation,size,ocean,velocity,FragmentSpin()*.3f);
                piece.SharkContactAfter=Time.time+.3f;planks.Add(piece);
                if(rider!=null)rider.RegisterBoard(piece);
            }
            else
            {
                go.transform.SetPositionAndRotation(at,rotation);go.transform.localScale=size;
                var detail=go.AddComponent<Story1WreckDebris>();detail.Initialize(ocean,size,true,velocity,FragmentSpin()*.3f);detailPieces.Add(detail);
                if(rider!=null)rider.RegisterCollision(detail.Collision);
            }
        }
    }
    public void PoseCinematicShark(Vector3 body, Quaternion rotation, float lift = 0, bool absoluteHeight = false)
    {
        if(!absoluteHeight)
        {
            float water=ocean != null ? ocean.SampleSurfaceHeight(body,Time.time) : center.y;
            body.y=sharkAnimator.SurfaceRootY(water,rotation)+(rotation*Vector3.Scale(sharkLocalBounds.center,shark.lossyScale)).y+lift;
        }
        shark.SetPositionAndRotation(body - rotation * Vector3.Scale(sharkLocalBounds.center,shark.lossyScale),rotation);
    }
    public Vector3 TailContactBody(Vector3 contact, Quaternion rotation) =>
        contact - rotation * Vector3.Scale(sharkAnimator.TailContactLocalPoint-sharkLocalBounds.center,shark.lossyScale);

    public void CinematicImpact(Vector3 point, bool heroCounter, bool heavy = false)
    {
        if(heroCounter) OnSharkHit(0);
        Effects.Impact(point,heroCounter ? shark.forward : -shark.forward,heavy ? 1.8f : .6f);
        if(tidalSound!=null)sound.PlayOneShot(tidalSound,heavy ? .85f : .35f);
        sequence.storyCamera.GetComponent<CombatCameraShake>()?.PulseStoryImpact(heavy ? .3f : .09f,heavy ? .4f : .16f);
    }
    void ProtectStoryFight()
    {
        if(CurrentPhase!=Phase.Fighting && CurrentPhase!=Phase.Breaking)return;
        playerHealth.currentHealth=1; playerHealth.GrantProtection(2);
        sharkPhase=SharkPhase.Recovery;sharkAge=0;
    }
    void BeginFinale()
    {
        if (CurrentPhase != Phase.Fighting) return;
        Handoff=player.Motion.Capture(player);
        var board = rider != null ? rider.Support() : null;
        if (board == null)
            board = FindLandingBoard(player.transform.position, 8) ?? FindLandingBoard(player.transform.position) ?? planks[0];
        bool fromWater = player.Swimming || board.EdgeDistance(player.transform.position + Vector3.up * player.LowestFootWorldOffset) > 2;
        if (rider != null) rider.enabled = false;
        player.GetComponent<SahurLoadoutUI>()?.SetOpen(false); player.GetComponent<IslandMapUI>()?.SetOpen(false);
        player.GetComponent<SahurAttack>().SuspendForSwimming(); player.GetComponent<SahurAttack>().enabled = false;
        player.GetComponent<SahurBoomerang>().enabled = false;
        player.GetComponent<EnemyLockOn>().Clear();
        var knockback = player.GetComponent<CombatKnockback>(); if (knockback != null) knockback.enabled = false;
        player.enabled = false; player.ExternalControlLock = true; capsule.enabled = false;
        playerHealth.GrantProtection(100);
        warning.text = controls.text = ""; bossName.enabled = false; healthFill.transform.parent.gameObject.SetActive(false);
        CurrentPhase = Phase.Finale; phaseAge = 0;
        Finale = gameObject.AddComponent<Story1WreckFinale>();
        player.Motion.Claim(Finale,SahurControlState.Cinematic);
        Finale.Initialize(this, player, sharkAnimator, sequence.storyCamera, board, ocean, fromWater);
    }
    public void CompleteFinale()
    {
        if(CurrentPhase!=Phase.Finale)return;
        playerHealth.NarrativeDefeatHandler=BeginDefeat;playerHealth.GrantProtection(0);
        playerHealth.ApplyDamage(playerHealth.maxHealth+playerHealth.currentHealth,player.transform.position);
    }
    public void ResumeAfterMissedFinale(Story1WreckFinale film)
    {
        if(CurrentPhase!=Phase.Finale || Finale!=film)return;
        // Dense, moving wreck can invalidate an approach. Return control at
        // the current supported pose instead of inventing a lethal contact.
        film.ReleaseActors();player.Motion.Release(film);Destroy(film);Finale=null;
        player.ExternalControlLock=player.ExternalMovementLock=false;
        player.ExternalMovementAnimation=null;player.OnFloatingWreckDeck=false;
        player.RestoreSavedPose(player.transform.position,player.transform.rotation);
        player.enabled=true;capsule.enabled=true;
        player.GetComponent<SahurAttack>().enabled=true;player.GetComponent<SahurBoomerang>().enabled=true;
        var knockback=player.GetComponent<CombatKnockback>();if(knockback!=null)knockback.enabled=true;
        if(rider!=null)rider.enabled=true;
        playerHealth.NarrativeDefeatHandler=ProtectStoryFight;playerHealth.GrantProtection(2);
        sharkPhase=SharkPhase.Recovery;sharkAge=0;
        CurrentPhase=Phase.Fighting;phaseAge=0;finaleRequestedAt=-1;retryFinaleAfter=Time.time+3;
        ShowFightHUD();
    }

    void BeginDefeat()
    {
        if (CurrentPhase != Phase.Finale) return;
        CurrentPhase = Phase.Defeat; phaseAge = 0; defeatStart = player.transform.position;
        player.Motion.Claim(Finale,SahurControlState.Unconscious);
        if (rider != null) rider.enabled = false;
        controls.text = "";
        player.ExternalControlLock = true; player.enabled = false;
        player.GetComponent<SahurAttack>().SuspendForSwimming(); player.GetComponent<SahurAttack>().enabled = false;
        player.GetComponent<SahurBoomerang>().enabled = false;
        var knockback = player.GetComponent<CombatKnockback>(); if (knockback != null) knockback.enabled = false;
        player.GetComponent<EnemyLockOn>().Clear(); capsule.enabled = false;
        player.GetComponent<SahurLoadoutUI>()?.SetOpen(false); player.GetComponent<IslandMapUI>()?.SetOpen(false);
        if (tidalSound != null) sound.PlayOneShot(tidalSound,.9f);
         warning.text = ""; healthFill.transform.parent.gameObject.SetActive(false); bossName.enabled = false;
        sequence.storyCamera.GetComponent<CombatCameraShake>()?.Pulse(.2f, .3f);
    }

    void FinishDefeat()
    {
        // The paired finale keeps the unconscious body afloat until scene unload.
        if(Finale==null)
            player.transform.position = defeatStart + heading * Mathf.Min(phaseAge * 3, 4) - Vector3.up * Mathf.Min(phaseAge * 3.5f, 4);
        blackout.enabled = true; blackout.color = new Color(0, 0, 0, Mathf.Clamp01((phaseAge - .45f) / .8f));
        if (phaseAge < 1.5f) return;
        CurrentPhase = Phase.Finished;
        sequence.awakening.BeginBlackout(blackout, center, heading);
    }

    void CreateUI()
    {
        hud = new GameObject("Wreck encounter HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(hud, gameObject.scene);
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; hud.GetComponent<Canvas>().sortingOrder = 950;
        var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
        Text Label(string name, Vector2 anchor, Vector2 at, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)); go.transform.SetParent(hud.transform, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = at; rect.sizeDelta = new Vector2(1200,70);
            var text = go.GetComponent<Text>(); text.font = GameLocalization.Font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size;
            text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            go.GetComponent<Outline>().effectColor = Color.black; LocalizedGameText.Bind(text); return text;
        }
        bossName = Label("Shark", new Vector2(.5f,1), new Vector2(0,-65), 27);
        warning = Label("Warning", new Vector2(.5f,.13f), Vector2.zero, 26);
        controls = Label("Board controls", new Vector2(.5f,.065f), Vector2.zero, 22);
        var bar = new GameObject("Shark health", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(hud.transform, false);
        var br = (RectTransform)bar.transform; br.anchorMin = br.anchorMax = new Vector2(.5f,1); br.anchoredPosition = new Vector2(0,-105); br.sizeDelta = new Vector2(600,10); bar.GetComponent<Image>().color = Color.black;
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(bar.transform,false);
        var fr = (RectTransform)fill.transform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), new Vector2(.5f,.5f));
        healthFill = fill.GetComponent<Image>(); healthFill.sprite = fillSprite; healthFill.color = Color.white; healthFill.type = Image.Type.Filled; healthFill.fillMethod = Image.FillMethod.Horizontal;
        var black = new GameObject("Blackout",typeof(RectTransform),typeof(Image)); black.transform.SetParent(hud.transform,false);
        var r = (RectTransform)black.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        blackout = black.GetComponent<Image>(); blackout.color = Color.clear; blackout.raycastTarget = false; blackout.enabled = false;
        bossName.enabled = false; bar.SetActive(false);
    }

    void ShowFightHUD()
    {
        bossName.enabled = true; healthFill.transform.parent.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        if (rider != null) rider.enabled = false;
        if (playerHealth != null && (playerHealth.NarrativeDefeatHandler == BeginDefeat || playerHealth.NarrativeDefeatHandler == ProtectStoryFight)) playerHealth.NarrativeDefeatHandler = null;
        if (hud != null) Destroy(hud); if (debrisRoot != null) Destroy(debrisRoot);
        if (fillSprite != null) Destroy(fillSprite);
    }
}
