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
    readonly List<Story1WreckFloatBody> detailPieces = new List<Story1WreckFloatBody>();
    Sprite fillSprite;
    Bounds sharkLocalBounds;
    Vector3 center, heading, side, passengerStart, breakSharkStart, strikePoint, defeatStart;
    Quaternion passengerRotation, breakSharkRotation;
    float phaseAge, sharkAge;
    float lastRecoilTime = -10;
    Vector3 recoilStart, recoilEnd;
    bool hullContactStarted, tailAttack;
    bool shattered, hitApplied, sharkPrepared;
    SharkPhase sharkPhase;
    AudioSource sound;
    AudioClip shatterSound, tidalSound;
    const float TailWindupSeconds = 1.3f;
    const float ShipTailContactTime = TailWindupSeconds + TralaleroSwimAnimator.TailContactTime;
    const float FractureDelay = .14f;
    const float BreakFlightSeconds = 4.6f;
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
        breakTailRotation = Quaternion.LookRotation(-heading) * Quaternion.Euler(0,0,-6);
        breakTailBody = owner.HullImpactPoint - breakTailRotation * Vector3.Scale(
            sharkAnimator.ShipSmashContactLocalPoint-sharkLocalBounds.center,shark.lossyScale);
        breakTailWindupBody = breakTailBody + side * 12;
        float windupWater = ocean != null ? ocean.SampleSurfaceHeight(breakTailWindupBody,Time.time) : center.y;
        breakTailWindupBody.y = sharkAnimator.SurfaceRootY(windupWater,breakTailRotation) +
            (breakTailRotation * Vector3.Scale(sharkLocalBounds.center,shark.lossyScale)).y;
        breakShipPosition = owner.ship.transform.position; breakShipRotation = owner.ship.transform.rotation;
        owner.ship.enabled = false;
        var passenger = player.GetComponent<Story1SahurPassenger>(); if (passenger != null) passenger.enabled = false;
        var follow = owner.storyCamera.GetComponent<ShipFollowCamera>(); if (follow != null) follow.enabled = false;
        player.ExternalControlLock = true; player.enabled = false; capsule.enabled = false;
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

    void Update() => Tick(Time.deltaTime);
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
        if (playerLowHealth || FightElapsed >= FinaleAfterSeconds) { BeginFinale(); return; }
        TickShark(deltaTime);
        healthFill.fillAmount = SharkHealth.Ratio;
        bossName.text = "TRALALERO TRALALA  " + Mathf.CeilToInt(SharkHealth.currentHealth) + " / " + Mathf.CeilToInt(SharkHealth.maxHealth);
        warning.text = sharkPhase == SharkPhase.Windup ? "鲨鱼正在蓄力，快闪开！" :
            sharkPhase == SharkPhase.Recoil || sharkPhase == SharkPhase.Recovery ? "趁现在反击！" :
            FightElapsed < 5 ? "在木板上迎战鲨鱼。使用平时的攻击和闪避。" : "";
        if (CurrentPhase == Phase.Fighting) controls.text = rider != null ? rider.Hint : "";
    }

    void BreakShip(float deltaTime)
    {
        // Turn broadside underwater, coil the tail, then whip the authored tail marker into the hull.
        if (!tailActionStarted && phaseAge >= TailWindupSeconds)
        {
            tailActionStarted = true; sharkAnimator.PlayTailSlap(phaseAge-TailWindupSeconds,powerful:true);
        }
        if (!sharkPrepared)
        {
            float windup = Mathf.SmoothStep(0,1,Mathf.Clamp01(phaseAge/TailWindupSeconds));
            float swing = Mathf.Clamp01((phaseAge-TailWindupSeconds)/TralaleroSwimAnimator.TailContactTime);
            Quaternion coiled = breakTailRotation * Quaternion.Euler(0,-25,0);
            Vector3 body;
            if(phaseAge<TailWindupSeconds)
            {
                shark.rotation=Quaternion.Slerp(breakSharkRotation,coiled,windup);
                body=Vector3.Lerp(breakBodyStart,breakTailWindupBody,windup);
            }
            else if(phaseAge<ShipTailContactTime || !hullContactStarted)
            {
                shark.rotation=Quaternion.Slerp(coiled,breakTailRotation,swing*swing);
                body=Vector3.Lerp(breakTailWindupBody,breakTailBody,swing*swing);
            }
            else
            {
                float after=phaseAge-ShipTailContactTime;
                float followThrough=Mathf.Clamp01(after/.22f);
                float retreat=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1.8f,after));
                shark.rotation=Quaternion.Slerp(breakTailRotation*Quaternion.Euler(0,20*followThrough,0),
                    Quaternion.LookRotation(side)*Quaternion.Euler(12,0,0),retreat);
                body=breakTailBody-side*(8*followThrough)+(side*35-heading*12-Vector3.up*24)*retreat;
            }
            shark.position=body-shark.TransformVector(sharkLocalBounds.center);
        }
        if (!breachStarted && phaseAge >= TailWindupSeconds+.25f)
        {
            breachStarted = true;
            Vector3 at = sharkAnimator.TailWorldPoint; at.y = center.y;
            Effects.Impact(at, -side, 2.4f);
            if(tidalSound!=null)sound.PlayOneShot(tidalSound,.6f);
        }
        if (phaseAge < ShipTailContactTime)
            return;
        if (!hullContactStarted)
        {
            hullContactStarted = true;
            sharkAnimator.SynchronizeShipTailContact();
            shark.position += sequence.HullImpactPoint - sharkAnimator.TailWorldPoint;
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
        // The full-size shark follows through with its tail and dives before taking its combat position.
        if(!sharkPrepared && progress>=1){sharkPrepared=true;PrepareShark();}
        foreach(var piece in planks)piece.CommitPose();
        Vector3 wreckCenter=Vector3.zero;
        foreach(var piece in planks)wreckCenter+=piece.transform.position;
        wreckCenter/=planks.Count;
        float cameraBlend=1-Mathf.Exp(-2.5f*deltaTime);
        sequence.storyCamera.transform.position=Vector3.Lerp(sequence.storyCamera.transform.position,
            wreckCenter+side*140+heading*65+Vector3.up*48,cameraBlend);
        sequence.storyCamera.transform.rotation=Quaternion.Slerp(sequence.storyCamera.transform.rotation,
            Quaternion.LookRotation(wreckCenter+heading*24+Vector3.up*22-sequence.storyCamera.transform.position),cameraBlend);
        if(landingBoard==null && progress>.7f)
            landingBoard=planks.Take(20).Where(p=>p.IsBoardable && p.GetComponent<Rigidbody>().linearVelocity.y<2)
                .OrderBy(p=>(p.transform.position-passengerStart).sqrMagnitude).FirstOrDefault();
        var landing=landingBoard!=null ? landingBoard : planks[0];
        Vector3 destination = landing.BoardingPoint(landing.transform.position) + Vector3.up * (.15f - player.LowestFootWorldOffset);
        player.transform.position = Vector3.Lerp(passengerStart - side*3.5f + Vector3.up*3.5f, destination,
            Mathf.SmoothStep(0, 1, progress)) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 5);
        if (progress < 1) return;
        Physics.SyncTransforms();
        capsule.enabled = true;
        player.RestoreSavedPose(destination, Quaternion.LookRotation(heading));
        player.ExternalControlLock = player.ExternalMovementLock = false;
        player.enabled = true; player.GetComponent<SahurAttack>().enabled = true; player.GetComponent<SahurBoomerang>().enabled = true;
        playerHealth.GrantProtection(1.25f);
        player.CharacterAnimator.CrossFadeInFixedTime("Locomotion", .12f);
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        CurrentPhase = Phase.Fighting; phaseAge = 0; sharkPhase = SharkPhase.Pursuit; sharkAge = 0;
        FightElapsed = 0;
        float minimum = Mathf.Max(1, minimumFightSeconds);
        FinaleAfterSeconds = ScatterRange(minimum, Mathf.Max(minimum, maximumFightSeconds));
        rider = player.GetComponent<Story1WreckRider>();
        if (rider == null) rider = player.gameObject.AddComponent<Story1WreckRider>();
        rider.Initialize(player, planks, ocean);
    }

    void CreateWreckage()
    {
        debrisRoot = new GameObject("Shattered Ship - Floating Hull Boards"); SceneManager.MoveGameObjectToScene(debrisRoot, gameObject.scene);
        shipWreck = Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        if (shipWreck == null || shipWreck.decks.Length != 20 || shipWreck.hulls.Length != 24)
            throw new System.InvalidOperationException("The Blender-cut ship wreck asset is missing or incomplete.");
        shipModelToWorld = Story1ShipWreckAsset.ModelToWorld(sequence.ship.transform);
        shipModelScale = sequence.ship.transform.lossyScale * .01f;
        deckSources = shipWreck.decks.OrderBy(p => (shipModelToWorld.MultiplyPoint3x4(p.sourceCenter) - passengerStart).sqrMagnitude).ToArray();
        scatter = new System.Random(System.Guid.NewGuid().GetHashCode());
        foreach(var source in deckSources)AddPlank(source);
        foreach(var source in shipWreck.hulls)AddPlank(source);
        foreach(var source in shipWreck.details)AddShipDetail(source);
    }

    float ScatterRange(float min,float max) => Mathf.Lerp(min,max,(float)scatter.NextDouble());
    void AddPlank(Story1ShipWreckAsset.Fragment source)
    {
        var go=new GameObject("Hull board "+(planks.Count+1)+" - "+source.name,typeof(MeshFilter),typeof(MeshRenderer),typeof(Story1WreckPlank));
        go.transform.SetParent(debrisRoot.transform);
        go.GetComponent<MeshFilter>().sharedMesh=source.mesh;go.GetComponent<MeshRenderer>().sharedMaterial=source.material;
        Vector3 from=shipModelToWorld.MultiplyPoint3x4(source.sourceCenter);
        Vector3 size=Vector3.Scale(source.sourceSize,shipModelScale);
        var piece=go.GetComponent<Story1WreckPlank>();
        piece.Initialize(from,Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source),size,ocean,FragmentVelocity(from),FragmentSpin());
        planks.Add(piece);
    }
    Vector3 FragmentVelocity(Vector3 from)
    {
        float near=1-Mathf.Clamp01(Vector3.Distance(from,sequence.HullImpactPoint)/145);
        float lateral=Vector3.Dot(from-breakShipPosition,heading);
        float sign=Mathf.Abs(lateral)>.2f ? Mathf.Sign(lateral) : (ScatterRange(0,1)>.5f ? 1 : -1);
        return -side*(ScatterRange(7,15)+near*12)+heading*(sign*(ScatterRange(3,9)+near*6)+ScatterRange(-3,3))+
            Vector3.up*(ScatterRange(5,10)+near*4);
    }
    Vector3 FragmentSpin() => side*ScatterRange(-75,75)+heading*ScatterRange(-65,65)+Vector3.up*ScatterRange(-45,45);
    void AddShipDetail(Story1ShipWreckAsset.Fragment source)
    {
        var go=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider),typeof(Story1WreckFloatBody));
        go.transform.SetParent(debrisRoot.transform);
        go.GetComponent<MeshFilter>().sharedMesh=source.mesh;go.GetComponent<MeshRenderer>().sharedMaterial=source.material;
        Vector3 from=shipModelToWorld.MultiplyPoint3x4(source.sourceCenter);
        Vector3 size=Vector3.Scale(source.sourceSize,shipModelScale);
        go.transform.localScale=size;
        go.transform.SetPositionAndRotation(from,Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source));
        var collider=go.GetComponent<BoxCollider>();collider.size=Vector3.one;collider.center=Vector3.zero;
        bool floats=source.material.name=="acmat_1" || source.material.name=="acmat_5" || source.material.name=="acmat_7" || source.material.name=="acmat_9";
        var body=go.GetComponent<Story1WreckFloatBody>();body.Initialize(ocean,size,floats,FragmentVelocity(from),FragmentSpin());
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
        sharkHitbox.center = sharkLocalBounds.center; sharkHitbox.size = sharkLocalBounds.size * .86f;
        PlaceShark(center + heading * 15, Quaternion.LookRotation(-heading));
        ShowFightHUD();
    }

    void PlaceShark(Vector3 body, Quaternion rotation)
    {
        float water=ocean != null ? ocean.SampleSurfaceHeight(body, Time.time) : center.y;
        body.y = sharkAnimator.SurfaceRootY(water,rotation) + (rotation*Vector3.Scale(sharkLocalBounds.center,shark.lossyScale)).y;
        // Briefly surface the head for attacks and the existing counter window; cruising stays submerged.
        float surfaceLift=sharkPhase==SharkPhase.Windup ? 2.4f*Mathf.SmoothStep(0,1,sharkAge/.65f) :
            sharkPhase==SharkPhase.Recovery ? 2.4f*(1-Mathf.SmoothStep(0,1,sharkAge/1.45f)) :
            sharkPhase==SharkPhase.Strike ? 2.4f :
            sharkPhase==SharkPhase.Recoil ? 2.4f*(1-Mathf.SmoothStep(0,1,sharkAge/.8f)) : 0;
        float feetY=player.transform.position.y+player.LowestFootWorldOffset;
        float headRise=Mathf.Max(2.4f,feetY-.25f-body.y);
        body.y+=headRise*(surfaceLift/2.4f);
        shark.rotation = rotation; shark.position = body - shark.TransformVector(sharkLocalBounds.center);
    }

    void TickShark(float deltaTime)
    {
        sharkAge += deltaTime;
        Vector3 body = shark.TransformPoint(sharkLocalBounds.center);
        Vector3 feet = player.transform.position + Vector3.up * player.LowestFootWorldOffset;
        Vector3 toward = Vector3.ProjectOnPlane(feet - body, Vector3.up).normalized;
        if (toward.sqrMagnitude < .1f) toward = shark.forward;
        if (sharkPhase == SharkPhase.Pursuit)
        {
            Quaternion facing = Quaternion.RotateTowards(shark.rotation, Quaternion.LookRotation(toward), 130 * deltaTime);
            Vector3 goal = feet - toward * (sharkLength * .5f + 3.4f);
            PlaceShark(Vector3.MoveTowards(body, goal, 15 * deltaTime), facing);
            if (sharkAge > 1.3f && Vector3.ProjectOnPlane(sharkAnimator.NoseWorldPoint - feet, Vector3.up).magnitude < 7)
            { sharkPhase = SharkPhase.Windup; sharkAge = 0; strikePoint = feet; hitApplied = false; tailAttack = SharkAttackCount % 2 == 0; sharkAnimator.PlayThreat(); }
        }
        else if (sharkPhase == SharkPhase.Windup)
        {
            PlaceShark(body, shark.rotation);
            if (tailAttack) shark.rotation = Quaternion.RotateTowards(shark.rotation, Quaternion.LookRotation(toward) * Quaternion.Euler(0,90,0),120*deltaTime);
            if (sharkAge >= .95f) { sharkPhase = SharkPhase.Strike; sharkAge = 0; if(tailAttack)sharkAnimator.PlayTailSlap();else sharkAnimator.PlayBite(); }
        }
        else if (sharkPhase == SharkPhase.Strike)
        {
            if (tailAttack)
            {
                Vector3 root = strikePoint + Vector3.up * 2.4f - shark.rotation * Vector3.Scale(sharkAnimator.TailContactLocalPoint,shark.lossyScale);
                shark.position = Vector3.Lerp(shark.position,root,1-Mathf.Exp(-12*deltaTime));
            }
            else
            {
                Vector3 goal = strikePoint - toward * (sharkLength * .5f + 1.8f);
                PlaceShark(Vector3.MoveTowards(body, goal, 22 * deltaTime), Quaternion.LookRotation(toward));
            }
            if (!hitApplied && sharkAge >= (tailAttack ? TralaleroSwimAnimator.TailContactTime : .45f))
            {
                hitApplied = true; SharkAttackCount++;
                if (tidalSound != null) sound.PlayOneShot(tidalSound,.6f);
                Effects.Impact(strikePoint, toward, 1.1f);
                if (Vector3.ProjectOnPlane(feet - strikePoint, Vector3.up).sqrMagnitude <= 5.5f * 5.5f && feet.y <= strikePoint.y + 2.4f)
                {
                    playerHealth.ApplyDamage(Mathf.Min(28,Mathf.Max(0,playerHealth.currentHealth-1)), feet + Vector3.up * 2);
                    sequence.storyCamera.GetComponent<CombatCameraShake>()?.Pulse(.1f, .18f);
                }
            }
            if (sharkAge > 1.1f && CurrentPhase == Phase.Fighting) { sharkPhase = SharkPhase.Recovery; sharkAge = 0;  }
        }
        else if (sharkPhase == SharkPhase.Recovery)
        {
            Vector3 goal = feet - toward * (sharkLength * .5f + 3.4f);
            PlaceShark(Vector3.MoveTowards(body,goal,8*deltaTime),Quaternion.RotateTowards(shark.rotation,Quaternion.LookRotation(toward),130*deltaTime));
            if (sharkAge > 1.45f) { sharkPhase = SharkPhase.Pursuit; sharkAge = 0; }
        }
        else
        {
            PlaceShark(Vector3.Lerp(recoilStart,recoilEnd,Mathf.SmoothStep(0,1,sharkAge/.65f)),Quaternion.LookRotation(toward)*Quaternion.Euler(0,0,Mathf.Sin(sharkAge/.7f*Mathf.PI)*-8));
            if(sharkAge>.8f){sharkPhase=SharkPhase.Pursuit;sharkAge=.7f;}
        }
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
        if(CurrentPhase!=Phase.Fighting)return;
        var board=rider!=null ? rider.Support() : null;
        if(board==null)
            board=planks.Take(20).Where(p=>p.IsBoardable && p.Dimensions.z>=8)
                .OrderBy(p=>(p.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault()
                ?? planks.Where(p=>p.IsBoardable).OrderBy(p=>(p.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault()
                ?? planks[0];
        bool fromWater=player.Swimming || board.EdgeDistance(player.transform.position+Vector3.up*player.LowestFootWorldOffset)>2;
        if(rider!=null)rider.enabled=false;
        player.GetComponent<SahurLoadoutUI>()?.SetOpen(false);player.GetComponent<IslandMapUI>()?.SetOpen(false);
        player.GetComponent<SahurAttack>().SuspendForSwimming();player.GetComponent<SahurAttack>().enabled=false;
        player.GetComponent<SahurBoomerang>().enabled=false;
        player.GetComponent<EnemyLockOn>().Clear();
        var knockback=player.GetComponent<CombatKnockback>();if(knockback!=null)knockback.enabled=false;
        player.enabled=false;player.ExternalControlLock=true;capsule.enabled=false;
        playerHealth.GrantProtection(100);
        warning.text=controls.text="";bossName.enabled=false;healthFill.transform.parent.gameObject.SetActive(false);
        CurrentPhase=Phase.Finale;phaseAge=0;
        Finale=gameObject.AddComponent<Story1WreckFinale>();
        Finale.Initialize(this,player,sharkAnimator,sequence.storyCamera,board,ocean,fromWater);
    }
    public void CompleteFinale()
    {
        if(CurrentPhase!=Phase.Finale)return;
        playerHealth.NarrativeDefeatHandler=BeginDefeat;playerHealth.GrantProtection(0);
        playerHealth.ApplyDamage(playerHealth.maxHealth+playerHealth.currentHealth,player.transform.position);
    }

    void BeginDefeat()
    {
        if (CurrentPhase != Phase.Finale) return;
        CurrentPhase = Phase.Defeat; phaseAge = 0; defeatStart = player.transform.position;
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
