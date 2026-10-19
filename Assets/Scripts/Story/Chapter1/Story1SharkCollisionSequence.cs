using UnityEngine;

/// <summary>A live shark strikes the ship, leaving a playable wreck encounter on the ocean.</summary>
[DefaultExecutionOrder(200), DisallowMultipleComponent]
public sealed class Story1SharkCollisionSequence : MonoBehaviour
{
    [Header("Scene references")]
    public ShipSailingMotion ship;
    public ThirdPersonPlayerController sahur;
    public Story1OceanAwakening awakening;
    public Transform swimmer;
    public Camera storyCamera;

    [Header("Encounter")]
    [Tooltip("Opening voyage duration relative to the original timing. Ship speed and attack animations are unchanged.")]
    [Range(.1f,1f)] public float voyageDurationScale = .25f;
    [Min(1f)] public float battleAfterSeconds = 20f;
    [Min(1f)] public float impactAfterSeconds = 30f;
    [Min(0f)] public float bowDistance = 50f;
    [Min(0f)] public float sharkNoseDistance = 37f;
    // Legacy scene data; current waterline comes from model bounds rather than a root offset.
    [HideInInspector] public float swimSubmergeDepth = 18f;
    [Min(0f)] public float swimBobHeight = 0.55f;
    [Min(1f)] public float maximumSwimSpeed = 14f;
    [Min(1f)] public float escapePursuitSpeed = 36f;
    [Min(.1f)] public float swimAcceleration = 7f;
    [Min(20f)] public float huntingStandOffDistance = 140f;
    [Min(1f)] public float pursuitBoostSeconds = 8f;
    [Min(1f)] public float swimTurnDegreesPerSecond = 65f;
    [Min(0.1f)] public float contactTolerance = 2f;
    [Tooltip("Stop the approach before the nose touches the hull, then turn for a tail strike.")]
    [Min(5f)] public float tailSetupDistance = 26f;
    [Tooltip("Sideways hunting sweeps fade out before the final bow approach.")]
    [Min(0f)] public float huntingSweepWidth = 18f;
    [Min(0f)] public float huntingDiveDepth = 4f;
    [Range(0f, 15f)] public float turnBankDegrees = 7f;

    [Header("Voyage patrol")]
    [Min(10f)] public float minimumPatrolSeconds = 30f;
    [Min(1f)] public float patrolSwimSpeed = 22f;
    [Min(5f)] public float patrolHullClearance = 12f;

    Transform shipTransform;
    TralaleroSwimAnimator swimAnimator;
    OceanWorld ocean;
    Collider[] shipColliders;
    Renderer[] voyageOccluders;
    Vector3 heading, side, swimPosition, swimDirection, swimVelocity;
    Vector3 previousContactDestination, contactDestinationVelocity, impactSharkCenter, bowContactPoint;
    Quaternion swimRotation;
    bool hasPreviousContactDestination, impacted;
    float baseTailBeatFrequency, swimBank, oceanHeight, voyageTime;
    Story1WreckBattle wreckBattle;
    public bool HasImpacted => impacted;
    public Vector3 HullImpactPoint => bowContactPoint;
    public float VoyageTime => voyageTime;
    public float CurrentSwimSpeed { get; private set; }
    public bool IsPatrolling => !impacted && (voyageTime < patrolEndsAt || voyageBreaching);
    public bool IsVoyageBreaching => voyageBreaching;
    public int VoyageBreachCount { get; private set; }
    public float PatrolDuration => patrolEndsAt;
    public float PatrolRadius => patrolRadius;
    System.Random voyageRandom;
    float patrolRadius, patrolAngle, patrolDirection, patrolEndsAt, nextVoyageBreach, voyageSwimClock,approachLeadSeconds,patrolTrim;
    Vector3 patrolBody, previousShipPosition;
    bool voyageBreaching, breachLaunched, voyageOwnsAnimation;
    float voyageBreachAt, flightDuration, launchY, launchSpeed, recoveryAt, recoveryVelocity, breachVisibilityAt;
    float VoyageScale => Mathf.Clamp(voyageDurationScale,.1f,1f);
    float ApproachDeadline => Mathf.Max(battleAfterSeconds*VoyageScale,patrolEndsAt+approachLeadSeconds);

    void Start()
    {
        if (ship == null || sahur == null || swimmer == null || storyCamera == null || awakening == null)
        {
            Debug.LogError("Story1 wreck encounter is missing a scene reference.", this);
            enabled = false; return;
        }
        shipTransform = ship.transform;
        SahurSupportPose.Prepare(sahur.CharacterAnimator);
        if (sahur.GetComponent<Mavis.GameSaveExcluded>() == null) sahur.gameObject.AddComponent<Mavis.GameSaveExcluded>();
        swimAnimator = swimmer.GetComponent<TralaleroSwimAnimator>();
        if (swimAnimator == null) swimAnimator = swimmer.gameObject.AddComponent<TralaleroSwimAnimator>();
        heading = Vector3.ProjectOnPlane(shipTransform.right, Vector3.up).normalized;
        side = Vector3.Cross(Vector3.up, heading).normalized;
        ocean = FindFirstObjectByType<OceanWorld>();
        oceanHeight = ocean != null ? ocean.oceanHeight : 0;
        shipColliders = shipTransform.GetComponentsInChildren<Collider>();
        var sailSet=Resources.Load<ShipSailSet>("Sailing/v039/ShipSails");
        voyageOccluders=System.Array.FindAll(shipTransform.GetComponentsInChildren<Renderer>(),r=>
            (r is MeshRenderer || r is SkinnedMeshRenderer) &&
            (sailSet==null || sailSet.panels==null || System.Array.Exists(sailSet.panels,p=>p.node==r.name)));
        swimPosition = swimmer.position; swimRotation = swimmer.rotation;
        float water=ocean!=null ? ocean.SampleSurfaceHeight(swimPosition,Time.time) : oceanHeight;
        swimPosition.y=swimAnimator.SurfaceRootY(water,swimRotation);
        swimmer.position=swimPosition;
        swimDirection = Vector3.ProjectOnPlane(swimmer.forward, Vector3.up).normalized;
        float noseReach = Vector3.ProjectOnPlane(swimAnimator.NoseWorldPoint - swimmer.position, Vector3.up).magnitude;
        if (noseReach > 1) sharkNoseDistance = noseReach;
        baseTailBeatFrequency = swimAnimator.tailBeatFrequency;
        InitializeVoyagePatrol();
        wreckBattle = GetComponent<Story1WreckBattle>();
        if (wreckBattle == null) wreckBattle = gameObject.AddComponent<Story1WreckBattle>();
    }

    void Update() => Tick(Time.deltaTime);
    internal void Tick(float deltaTime)
    {
        if (impacted || PauseSettingsMenu.IsOpen || deltaTime <= 0) return;
        voyageTime += deltaTime;
        if(IsPatrolling)UpdateVoyagePatrol(deltaTime);
        else
        {
            if(voyageOwnsAnimation)
            {
                swimDirection=Vector3.ProjectOnPlane(swimmer.forward,Vector3.up).normalized;
                swimPosition=swimmer.position;hasPreviousContactDestination=false;
                swimAnimator.ReleaseCinematicAnimation();voyageOwnsAnimation=false;
            }
            UpdateSwimmer(voyageTime, deltaTime);
        }
        float gap = Vector3.ProjectOnPlane(swimAnimator.NoseWorldPoint - bowContactPoint, Vector3.up).magnitude;
        if (!IsPatrolling && voyageTime >= Mathf.Max(impactAfterSeconds*VoyageScale,ApproachDeadline) && gap <= tailSetupDistance + contactTolerance &&
            Vector3.Angle(Vector3.ProjectOnPlane(swimmer.forward, Vector3.up), -heading) < 15)
            BeginImpact();
    }

    [ContextMenu("Start wreck encounter")]
    public void BeginImpact()
    {
        if (impacted || !Application.isPlaying || wreckBattle == null) return;
        impacted = true;
        voyageBreaching=false;voyageOwnsAnimation=false;swimAnimator.ReleaseCinematicAnimation();
        float water=ocean!=null ? ocean.SampleSurfaceHeight(shipTransform.position,Time.time) : oceanHeight;
        // Attack the camera-facing hull flank so the coiling tail and contact are visible.
        Vector3 rayStart=shipTransform.position+heading*25+side*100;
        rayStart.y=water+12;
        bowContactPoint=shipTransform.position+heading*25+side*20;
        bowContactPoint.y=rayStart.y;
        float nearest=150;
        bool backfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        foreach(var collider in shipColliders)
            if(collider!=null && collider.enabled && !collider.isTrigger &&
                collider.Raycast(new Ray(rayStart,-side),out RaycastHit hit,nearest))
            {nearest=hit.distance;bowContactPoint=hit.point;}
        Physics.queriesHitBackfaces=backfaces;
        wreckBattle.Begin(this, heading);
    }
    void UpdateSwimmer(float time, float deltaTime)
    {
        if (deltaTime <= 0f) return;
        heading = Vector3.ProjectOnPlane(shipTransform.right, Vector3.up).normalized;
        side = Vector3.Cross(Vector3.up, heading).normalized;
        float approachDeadline = Mathf.Max(1f, ApproachDeadline);
        float progress = Mathf.Clamp01((time+patrolTrim) / (approachDeadline+patrolTrim));
        bowContactPoint = FindBowContactPoint();
        float finalApproach = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(approachDeadline - approachLeadSeconds, approachDeadline, time));
        float standOff = Mathf.Lerp(huntingStandOffDistance,tailSetupDistance,finalApproach);
        float hunting = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.45f, .82f, progress));
        Vector3 huntingOffset = side * (huntingSweepWidth * hunting *
            (Mathf.Sin(time * .52f) + .25f * Mathf.Sin(time * 1.13f))) +
            heading * (Mathf.Sin(time * .38f) * 6f * hunting);
        Vector3 approach = Vector3.ProjectOnPlane(bowContactPoint + heading * standOff + huntingOffset -
            swimAnimator.NoseWorldPoint, Vector3.up);
        if (approach.sqrMagnitude > .0001f) swimDirection = approach.normalized;
        float faceBow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .8f, finalApproach));
        Vector3 forward = Vector3.Slerp(swimDirection, -heading, faceBow).normalized;
        Vector3 previousForward = swimRotation * Vector3.forward;
        if (forward.sqrMagnitude > .001f)
            swimRotation = Quaternion.RotateTowards(swimRotation, Quaternion.LookRotation(forward, Vector3.up),
                swimTurnDegreesPerSecond * deltaTime);
        float turnRate = Vector3.SignedAngle(previousForward,
            swimRotation * Vector3.forward, Vector3.up) / deltaTime;
        float bankTarget = Mathf.Clamp(-turnRate * .2f, -turnBankDegrees, turnBankDegrees) * hunting;
        swimBank = Mathf.Lerp(swimBank, bankTarget, 1f - Mathf.Exp(-4f * deltaTime));
        swimmer.rotation = swimRotation * Quaternion.Euler(
            Mathf.Sin(time * 1.4f) * 2.5f * hunting, 0f,
            swimBank + Mathf.Sin(time * 2.1f) * 1.5f * hunting);
        // The model's nose is offset sideways from its imported root. Aim its
        // actual nose at the bow instead of treating the root as its centreline.
        Vector3 noseOffset = swimAnimator.NoseWorldPoint - swimmer.position;
        Vector3 contactDestination = bowContactPoint - noseOffset;
        if (hasPreviousContactDestination)
        {
            Vector3 targetVelocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(
                contactDestination - previousContactDestination, Vector3.up) / deltaTime, escapePursuitSpeed);
            contactDestinationVelocity = Vector3.Lerp(contactDestinationVelocity, targetVelocity,
                1f - Mathf.Exp(-6f * deltaTime));
        }
        else contactDestinationVelocity = heading * ship.CurrentSpeed;
        previousContactDestination = contactDestination;
        hasPreviousContactDestination = true;
        impactSharkCenter = contactDestination + heading * standOff + huntingOffset;
        Vector3 towardDestination = Vector3.ProjectOnPlane(impactSharkCenter - swimPosition, Vector3.up);
        float remaining = Mathf.Max(.65f, approachDeadline - time);
        // Cruise at a distance first. Only the final approach closes on the bow.
        // Acceleration is bounded; a moving/rotating target cannot cause a speed spike.
        float pursuitLimit = Mathf.Lerp(maximumSwimSpeed,
            Mathf.Max(maximumSwimSpeed, escapePursuitSpeed), finalApproach);
        Vector3 shipVelocity = heading * ship.CurrentSpeed;
        float response = Mathf.Lerp(2.5f, remaining, finalApproach);
        Vector3 targetMotion = Vector3.Lerp(shipVelocity, contactDestinationVelocity, finalApproach);
        Vector3 desiredVelocity = Vector3.ClampMagnitude(targetMotion + towardDestination / response, pursuitLimit);
        float acceleration = Mathf.Lerp(swimAcceleration, swimAcceleration * 4f, finalApproach);
        swimVelocity = Vector3.MoveTowards(swimVelocity, desiredVelocity, acceleration * deltaTime);
        swimVelocity = Vector3.ClampMagnitude(swimVelocity, pursuitLimit);
        Vector3 step = swimVelocity * deltaTime;
        CurrentSwimSpeed = deltaTime > .0001f ? step.magnitude / deltaTime : 0f;
        float targetBeat = baseTailBeatFrequency *
            Mathf.Lerp(.75f, 1.35f, Mathf.Clamp01(CurrentSwimSpeed / escapePursuitSpeed)) *
            (1f + .08f * Mathf.Sin(time * .9f) * hunting);
        swimAnimator.tailBeatFrequency = Mathf.Lerp(swimAnimator.tailBeatFrequency, targetBeat,
            1f - Mathf.Exp(-3f * deltaTime));
        swimPosition += step;
        float water = ocean != null ? ocean.SampleSurfaceHeight(swimPosition, Time.time) : oceanHeight;
        float dive = huntingDiveDepth * hunting * (.5f + .5f * Mathf.Sin(time * .72f));
        swimPosition.y = Mathf.MoveTowards(swimPosition.y, swimAnimator.SurfaceRootY(water,swimmer.rotation) - dive,
            maximumSwimSpeed * deltaTime);
        swimmer.position = swimPosition + Vector3.up * (Mathf.Sin(time * 3.4f) * swimBobHeight);

    }

    void InitializeVoyagePatrol()
    {
        voyageRandom=new System.Random();
        float hullRadius=Mathf.Sqrt(ship.hullLength*ship.hullLength+ship.hullBeam*ship.hullBeam)*.5f;
        float hullHeight=0;
        foreach(var collider in shipColliders)
        {
            if(collider==null || !collider.enabled || collider.isTrigger)continue;
            Bounds bounds=collider.bounds;
            for(int i=0;i<8;i++)
            {
                Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 delta=corner-shipTransform.position;
                hullRadius=Mathf.Max(hullRadius,Vector3.ProjectOnPlane(delta,Vector3.up).magnitude);
                hullHeight=Mathf.Max(hullHeight,Mathf.Abs(delta.y));
            }
        }
        Vector3 half=Vector3.Scale(swimAnimator.RootMeshBounds.extents,swimmer.lossyScale);
        float animalRadius=new Vector2(half.x,half.z).magnitude;
        float sway=hullHeight*Mathf.Sin(Mathf.Min(20,ship.maxPitchDegrees+ship.maxRollDegrees)*Mathf.Deg2Rad);
        patrolRadius=hullRadius+animalRadius+Mathf.Max(5,patrolHullClearance)+sway;
        float originalPatrol=Mathf.Max(minimumPatrolSeconds,2*Mathf.PI*patrolRadius/Mathf.Max(1,patrolSwimSpeed));
        approachLeadSeconds=pursuitBoostSeconds;
        if(VoyageScale>=.999f)patrolEndsAt=originalPatrol;
        else
        {
            // Shorten the opening wait, not the physical approach or attack.
            // A full-circuit lower bound previously defeated shorter timers.
            float target=Mathf.Max(impactAfterSeconds,Mathf.Max(battleAfterSeconds,originalPatrol+pursuitBoostSeconds))*VoyageScale;
            approachLeadSeconds=Mathf.Min(pursuitBoostSeconds,Mathf.Max(.5f,target-.5f));
            float arrivalMargin=Mathf.Min(2f,target*.15f);
            patrolEndsAt=Mathf.Max(.5f,target-approachLeadSeconds-arrivalMargin);
        }
        patrolTrim=originalPatrol-patrolEndsAt;
        // Initial staging happens before the first voyage frame. Keep the
        // animal on the camera-facing quarter, outside the entire real hull.
        patrolDirection=Vector3.Dot(storyCamera.transform.position-shipTransform.position,side)>=0 ? 1 : -1;
        Vector3 viewOffset=storyCamera.transform.position-shipTransform.position;
        float viewAngle=Mathf.Atan2(Vector3.Dot(viewOffset,side),Vector3.Dot(viewOffset,heading));
        // Start farther around the original orbit before its first visible
        // frame, so the shorter patrol ends on the same approach quarter.
        // The shark keeps its normal speed and never skips position in view.
        patrolAngle=viewAngle+patrolDirection*(1.8f+patrolTrim*patrolSwimSpeed/Mathf.Max(1,patrolRadius));
        Vector3 radial=heading*Mathf.Cos(patrolAngle)+side*Mathf.Sin(patrolAngle);
        Vector3 tangent=(-heading*Mathf.Sin(patrolAngle)+side*Mathf.Cos(patrolAngle))*patrolDirection;
        swimRotation=Quaternion.LookRotation(tangent,Vector3.up);
        patrolBody=shipTransform.position+radial*patrolRadius;
        float water=WaterAt(patrolBody);
        patrolBody.y=SurfaceBodyY(water,swimRotation);
        PlaceVoyageBody(patrolBody,swimRotation);
        swimVelocity=heading*ship.CurrentSpeed+tangent*patrolSwimSpeed;
        previousShipPosition=shipTransform.position;
        nextVoyageBreach=2+(float)voyageRandom.NextDouble()*1.5f;
    }
    float WaterAt(Vector3 point)=>ocean!=null ? ocean.SampleSurfaceHeight(point,Time.time) : oceanHeight;
    float SurfaceBodyY(float water,Quaternion rotation)=>swimAnimator.SurfaceRootY(water,rotation)+
        (rotation*Vector3.Scale(swimAnimator.RootMeshBounds.center,swimmer.lossyScale)).y;
    void PlaceVoyageBody(Vector3 body,Quaternion rotation)
    {
        swimRotation=rotation;swimmer.SetPositionAndRotation(body-rotation*Vector3.Scale(swimAnimator.RootMeshBounds.center,swimmer.lossyScale),rotation);
        swimPosition=swimmer.position;
    }
    void UpdateVoyagePatrol(float deltaTime)
    {
        voyageOwnsAnimation=true;
        heading=Vector3.ProjectOnPlane(shipTransform.right,Vector3.up).normalized;
        side=Vector3.Cross(Vector3.up,heading).normalized;
        Vector3 shipStep=Vector3.ProjectOnPlane(shipTransform.position-previousShipPosition,Vector3.up);
        Vector3 shipVelocity=Vector3.ClampMagnitude(shipStep/deltaTime,escapePursuitSpeed);
        patrolAngle+=patrolDirection*patrolSwimSpeed/Mathf.Max(1,patrolRadius)*deltaTime;
        Vector3 radial=heading*Mathf.Cos(patrolAngle)+side*Mathf.Sin(patrolAngle);
        Vector3 tangent=(-heading*Mathf.Sin(patrolAngle)+side*Mathf.Cos(patrolAngle))*patrolDirection;
        Vector3 target=shipTransform.position+radial*patrolRadius;
        Vector3 correction=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(target-patrolBody,Vector3.up)*1.5f,8);
        Vector3 desired=shipVelocity+tangent*patrolSwimSpeed+correction;
        float limit=Mathf.Max(maximumSwimSpeed,patrolSwimSpeed+shipVelocity.magnitude);
        swimVelocity=Vector3.MoveTowards(swimVelocity,Vector3.ClampMagnitude(desired,limit),swimAcceleration*2*deltaTime);
        Vector3 step=Vector3.ProjectOnPlane(swimVelocity,Vector3.up)*deltaTime;
        Vector3 candidate=patrolBody+step;
        float guardRadius=patrolRadius-Mathf.Max(5,patrolHullClearance)*.5f;
        if(Vector3.ProjectOnPlane(candidate-shipTransform.position,Vector3.up).sqrMagnitude<guardRadius*guardRadius)
        {
            // A sharp helm change must not cut the circular path through the
            // ship. Carry the previous safe offset with the actual boat step;
            // never project the shark outwards with a visible position jump.
            step=shipStep;candidate=patrolBody+step;swimVelocity=shipVelocity;
        }
        patrolBody=candidate;previousShipPosition=shipTransform.position;
        CurrentSwimSpeed=step.magnitude/deltaTime;
        Vector3 facing=Vector3.ProjectOnPlane(swimVelocity-shipVelocity*.35f,Vector3.up);
        Quaternion flat=facing.sqrMagnitude>.001f ? Quaternion.LookRotation(facing,Vector3.up) : swimRotation;
        Quaternion headingRotation=Quaternion.RotateTowards(Quaternion.LookRotation(Vector3.ProjectOnPlane(swimRotation*Vector3.forward,Vector3.up),Vector3.up),flat,swimTurnDegreesPerSecond*deltaTime);
        float water=WaterAt(patrolBody);
        float normalY=SurfaceBodyY(water,headingRotation)-.35f;
        Quaternion rotation=headingRotation;
        voyageSwimClock+=deltaTime*Mathf.Clamp(CurrentSwimSpeed/Mathf.Max(1,patrolSwimSpeed),.7f,1.5f);

        if(!voyageBreaching && voyageTime>=nextVoyageBreach && voyageTime>=breachVisibilityAt && patrolEndsAt-voyageTime>6)
        {
            breachVisibilityAt=voyageTime+.25f;
            if(VoyageBreachVisible(water))
            {voyageBreaching=true;breachLaunched=false;voyageBreachAt=voyageTime;recoveryAt=-1;VoyageBreachCount++;}
        }
        if(voyageBreaching)
        {
            float age=voyageTime-voyageBreachAt;
            if(age<.45f)
            {
                rotation=headingRotation*Quaternion.Euler(-8*Mathf.SmoothStep(0,1,age/.45f),0,0);
                patrolBody.y=Mathf.MoveTowards(patrolBody.y,normalY-Mathf.Sin(age/.45f*Mathf.PI)*1.5f,maximumSwimSpeed*deltaTime);
                swimAnimator.SampleCinematicAction("Breach",age*(.5f/.45f));
            }
            else
            {
                if(!breachLaunched)
                {
                    breachLaunched=true;launchY=patrolBody.y;
                    Vector3 half=Vector3.Scale(swimAnimator.RootMeshBounds.extents,swimmer.lossyScale);
                    float apex=water+Mathf.Clamp(Mathf.Abs(half.y)*.35f,2,6);
                    float lift=Mathf.Clamp(apex-launchY,5,18);
                    launchSpeed=Mathf.Sqrt(2*Physics.gravity.magnitude*lift);
                    flightDuration=2*launchSpeed/Mathf.Max(.1f,Physics.gravity.magnitude);
                }
                float flying=age-.45f,u=Mathf.Clamp01(flying/flightDuration);
                if(recoveryAt<0 && flying<flightDuration)
                {
                    patrolBody.y=launchY+launchSpeed*flying-.5f*Physics.gravity.magnitude*flying*flying;
                    float vertical=launchSpeed-Physics.gravity.magnitude*flying;
                    float pitch=Mathf.Clamp(-Mathf.Atan2(vertical,Mathf.Max(1,patrolSwimSpeed))*Mathf.Rad2Deg,-35,32);
                    rotation=headingRotation*Quaternion.Euler(pitch,0,-6*patrolDirection*Mathf.Sin(Mathf.PI*u));
                    swimAnimator.SampleCinematicAction("Breach",.5f+2.2f*u);
                }
                else
                {
                    if(recoveryAt<0){recoveryAt=voyageTime;recoveryVelocity=-launchSpeed;}
                    float recovery=voyageTime-recoveryAt;
                    rotation=headingRotation*Quaternion.Euler(Mathf.Lerp(32,0,Mathf.SmoothStep(0,1,recovery/.65f)),0,0);
                    patrolBody.y=Mathf.SmoothDamp(patrolBody.y,normalY,ref recoveryVelocity,.3f,maximumSwimSpeed,deltaTime);
                    swimAnimator.SampleCinematicAction(recovery<.5f ? "Breach" : "Swim",recovery<.5f ? 2.7f+recovery : voyageSwimClock);
                    if(recovery>=1.4f || recovery>=.8f && Mathf.Abs(patrolBody.y-normalY)<.25f)
                    {
                        voyageBreaching=false;nextVoyageBreach=voyageTime+4+(float)voyageRandom.NextDouble()*3;
                    }
                }
            }
        }
        else
        {
            patrolBody.y=Mathf.MoveTowards(patrolBody.y,normalY,maximumSwimSpeed*deltaTime);
            swimAnimator.SampleCinematicAction("Swim",voyageSwimClock);
        }
        swimAnimator.CommitCinematicPose(voyageTime);
        PlaceVoyageBody(patrolBody,rotation);
    }
    bool VoyageBreachVisible(float water)
    {
        // Prefer a real visible opening, without steering the player's camera
        // or jumping the animal to a convenient staging point.
        bool previous=Physics.queriesHitBackfaces;
        try
        {
            Physics.queriesHitBackfaces=true;
            for(int sample=0;sample<2;sample++)
            {
                float angle=patrolAngle+(sample==0?0:2*patrolDirection*patrolSwimSpeed/Mathf.Max(1,patrolRadius));
                // Forecast relative to the moving boat/camera for the apex,
                // rather than only seeing an opening at the start of a leap.
                Vector3 aim=sample==0 ? patrolBody : shipTransform.position+
                    (heading*Mathf.Cos(angle)+side*Mathf.Sin(angle))*patrolRadius;
                aim.y=water+3;
                Vector3 screen=storyCamera.WorldToViewportPoint(aim);
                if(screen.z<=0 || screen.x<.08f || screen.x>.92f || screen.y<.04f || screen.y>.92f)return false;
                Vector3 from=storyCamera.transform.position,delta=aim-from;
                var ray=new Ray(from,delta.normalized);
                foreach(var collider in shipColliders)
                    if(collider!=null && collider.enabled && !collider.isTrigger && collider.Raycast(ray,out _,delta.magnitude))return false;
                // Sails and flags need no physical collider, but their visible
                // meshes still hide a shark. The real hull already used its
                // exact raycast above, rather than a filled whole-ship box.
                // Cloth bounds are rejection-only here;
                // an occluded chance simply waits for the next real opening.
                foreach(var renderer in voyageOccluders)
                    if(renderer!=null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                        renderer.bounds.IntersectRay(ray,out float at) && at<delta.magnitude)return false;
            }
        }
        finally{Physics.queriesHitBackfaces=previous;}
        return true;
    }
    void OnDisable()
    {
        if(swimAnimator!=null && !impacted)swimAnimator.ReleaseCinematicAnimation();
        voyageOwnsAnimation=false;
    }

    Vector3 FindBowContactPoint(float contactHeight = float.NaN)
    {
        Vector3 origin = shipTransform.position + heading * (bowDistance + 60f);
        origin.y = float.IsNaN(contactHeight) ? swimAnimator.NoseWorldPoint.y : contactHeight;
        Vector3 contact = shipTransform.position + heading * bowDistance;
        contact.y = origin.y;
        float closest = 150f;
        bool backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
        foreach (Collider collider in shipColliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;
            if (collider.Raycast(new Ray(origin, -heading), out RaycastHit hit, closest))
            {
                closest = hit.distance;
                contact = hit.point;
            }
        }
        Physics.queriesHitBackfaces = backfaces;
        return contact;
    }

}
