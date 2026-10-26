using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Walking, jumping and climbing on freely floating wreckage.</summary>
[DisallowMultipleComponent,DefaultExecutionOrder(-100)]
public sealed class Story1WreckRider : MonoBehaviour
{
    public bool IsSurfing => false; // Compatibility; vehicle control has been retired.
    public bool IsClimbing { get; private set; }
    public Story1WreckPlank DrivenBoard => null;
    public Story1WreckPlank NearbyBoard { get; private set; }
    public string Hint { get; private set; } = "";

    ThirdPersonPlayerController player;
    CharacterController capsule;
    SahurAttack attack;
    SahurBoomerang boomerang;
    SahurSwimmingWeapon weapon;
    IReadOnlyList<Story1WreckPlank> boards;
    Collider[] bodyHitboxes;
    readonly Collider[] climbOverlaps=new Collider[64];
    readonly Dictionary<Story1WreckMeshCollision,MeshCollider[]> registeredContacts=new Dictionary<Story1WreckMeshCollision,MeshCollider[]>();
    public int RegisteredContactShapeCount=>registeredContacts.Count;
    Story1WreckPlank climbBoard,combatBoard,standingBoard,blockedBoard;
    Story1WreckPlank intentBoard;
    OceanWorld ocean;
    Vector3 climbStart, climbEnd,leftGrip,rightGrip;
    float riderYaw, climbAge;
    bool attackWasEnabled, boomerangWasEnabled;
    float autoClimbIntent,blockedBoardUntil;
    public float PaddlePhase=>0;
    public bool IsPaddling=>false;
    public float PaddleWaterHeight(Vector3 point)=>ocean!=null ? ocean.SampleSurfaceHeight(point,Time.time) : player.seaLevel;
    readonly Vector4[] trail = new Vector4[16];
    const float ClimbSeconds = 1.15f;

    public void Initialize(ThirdPersonPlayerController actor, IReadOnlyList<Story1WreckPlank> fragments, OceanWorld water,Story1WreckPlank initialSupport=null,IReadOnlyList<Story1WreckDebris> debris=null)
    {
        using var measurement=new Unity.Profiling.ProfilerMarker("Wreck.RegisterRiderContacts").Auto();
        player = actor; boards = fragments; ocean = water;
        if(ocean!=null)ocean.SetShipWake(Vector4.zero,Vector4.zero,trail,0);
        PrepareCollisionOwner(actor); attack = GetComponent<SahurAttack>();
        boomerang = GetComponent<SahurBoomerang>(); weapon = GetComponent<SahurSwimmingWeapon>();
        // Detailed animated hurtboxes still receive weapon queries, but their
        // kinematic feet must not pin floating wood. The controller owns locomotion.
        foreach(var board in boards)if(board!=null)RegisterCollision(board.Collision);
        if(debris!=null)foreach(var piece in debris)RegisterCollision(piece.Collision);
        enabled = true;
        if(initialSupport!=null){SetStandingBoard(initialSupport);player.OnFloatingWreckDeck=true;}
    }
    // Geometry warming can prepare only physical ownership. It must not
    // claim movement, support or input before the real playable handoff.
    public void PrepareCollisionOwner(ThirdPersonPlayerController actor)
    {
        capsule=actor.GetComponent<CharacterController>();
        var current=System.Array.FindAll(actor.GetComponentsInChildren<Collider>(true),c=>c!=capsule && !c.isTrigger);
        bool same=bodyHitboxes!=null && bodyHitboxes.Length==current.Length;
        if(same)for(int i=0;i<current.Length;i++)if(bodyHitboxes[i]!=current[i]){same=false;break;}
        if(same)return;
        bodyHitboxes=current;registeredContacts.Clear();
    }
    public void RegisterBoard(Story1WreckPlank board)
    {
        RegisterCollision(board.Collision);
    }
    public void RegisterCollision(Story1WreckMeshCollision shape)
    {
        if(bodyHitboxes==null || shape==null || shape.Parts==null)return;
        if(registeredContacts.TryGetValue(shape,out var previous) && ReferenceEquals(previous,shape.Parts))return;
        foreach(var hitbox in bodyHitboxes)if(hitbox!=null)shape.Ignore(hitbox,true);
        registeredContacts[shape]=shape.Parts;
    }

    public bool HasStableSupport
    {
        get
        {
            // The controller deliberately ignores its supporting floating
            // mesh, so isGrounded alone cannot describe a settled passenger.
            // Actual deck contact, not the wider landing/grab grace, admits
            // a cinematic. Ascending and descending airborne actors stay live.
            if(player==null || IsClimbing || player.Swimming || capsule==null || !capsule.enabled ||
                player.VerticalSpeed>0 || !player.OnFloatingWreckDeck || standingBoard==null || !standingBoard.IsBoardable)return false;
            Vector3 feet=transform.position+Vector3.up*player.LowestFootWorldOffset;
            if(!standingBoard.ContainsFootprint(feet) || !DryEnough(standingBoard,feet))return false;
            Vector3 normal=standingBoard.transform.up;if(normal.y<0)normal=-normal;
            float reach=Mathf.Max(2,standingBoard.Dimensions.y+1);
            if(!standingBoard.Collision.Raycast(new Ray(feet+normal*reach,-normal),out var hit,reach*2) || Vector3.Dot(hit.normal,Vector3.up)<=.5f)return false;
            float gap=feet.y-hit.point.y;
            return gap>=-.03f && gap<=.06f;
        }
    }

    Story1WreckPlank tutorialWalkingDeck;
    public bool WalkingTutorialActive => tutorialWalkingDeck != null;
    public void BeginWalkingTutorial(Story1WreckPlank board)
    {
        tutorialWalkingDeck=board;
        player.WalkingTutorialConstraint=ConstrainTutorialStep;
        ResumeCinematicSupport(board);
    }
    public void EndWalkingTutorial()
    {
        if(player!=null && player.WalkingTutorialConstraint==ConstrainTutorialStep)player.WalkingTutorialConstraint=null;
        tutorialWalkingDeck=null;
    }
    Vector3 ConstrainTutorialStep(Vector3 delta)
    {
        if(tutorialWalkingDeck==null || !tutorialWalkingDeck.IsBoardable)return Vector3.zero;
        Vector3 feet=transform.position+Vector3.up*player.LowestFootWorldOffset;
        float inset=Mathf.Max(.85f,capsule.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z))+.15f);
        Vector3 safe=tutorialWalkingDeck.BoardingPoint(feet+Vector3.ProjectOnPlane(delta,Vector3.up),inset);
        return new Vector3(safe.x-feet.x,delta.y,safe.z-feet.z);
    }
    public bool ResumeCinematicSupport(Story1WreckPlank board)
    {
        if(board==null||!board.IsBoardable||player==null||capsule==null)return false;
        // OnDisable releases the cinematic's old passenger registration.
        // Restore the controller's real feet on the actual mesh, rather than
        // relying on the just-finished get-up animation's root height.
        Vector3 feet=board.BoardingPoint(transform.position+Vector3.up*player.LowestFootWorldOffset);
        SetStandingBoard(board);SetFeet(feet+Vector3.up*.04f,transform.eulerAngles.y,false);
        player.LeaveMovingPlatform(Vector3.zero);player.OnFloatingWreckDeck=true;return true;
    }

    public Story1WreckPlank Support()
    {
        if (player == null || boards == null || IsClimbing || player.Swimming) return null;
        Vector3 feet = transform.position + Vector3.up * player.LowestFootWorldOffset;
        if(standingBoard!=null && standingBoard.IsBoardable && standingBoard.ContainsFootprint(feet) &&
            Mathf.Abs(standingBoard.BoardingPoint(feet).y-feet.y)<1.1f && DryEnough(standingBoard,feet))return standingBoard;
        foreach (var board in boards) if (board != null && board.Supports(feet) && DryEnough(board,feet)) return board;
        return null;
    }
    bool DryEnough(Story1WreckPlank board,Vector3 feet,float immersion=1.1f)
    {
        Vector3 at=board.BoardingPoint(feet);
        return ocean==null || ocean.SampleSurfaceHeight(at,Time.time)-at.y<=immersion;
    }

    public void BeforePlanks(float deltaTime)
    {
        if (player == null || !enabled) return;
        Vector3 feet=transform.position+Vector3.up*player.LowestFootWorldOffset;
        player.OnFloatingWreckDeck=standingBoard!=null && DryEnough(standingBoard,feet) && standingBoard.IsBoardable &&
            standingBoard.ContainsFootprint(feet) && Mathf.Abs(standingBoard.BoardingPoint(feet).y-feet.y)<1.1f && player.VerticalSpeed<=1.5f;
        bool ascending=player.VerticalSpeed>1.5f;
        if(!ascending)foreach(var board in boards)if(board!=null && board.Supports(feet) && DryEnough(board,feet)){player.OnFloatingWreckDeck=true;break;}
        bool accept = Keyboard.current != null && Cursor.lockState == CursorLockMode.Locked &&
            !player.ExternalControlLock && !SahurLoadoutUI.BlocksInput && !PlayerDeathRespawn.BlocksInput && !IslandMapUI.IsOpen;
        if (IsClimbing) { Hint = GameLocalization.Text("Climbing onto the board..."); return; }
        var support = ascending ? null : Support();
        SetStandingBoard(support);
        if(support!=null)combatBoard=support;else if(!attack.IsCombatMotionActive)combatBoard=null;
        if (support!=null && accept && GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump) && !attack.IsCombatMotionActive)
        {
            // Normal controller jump, carrying the real support's momentum.
            // Keep the short water/grab grace period without claiming movement.
            blockedBoard=support;blockedBoardUntil=Time.time+.35f;
            SetStandingBoard(null);player.OnFloatingWreckDeck=false;
            player.LeaveMovingPlatform(support.Velocity,true);
            return;
        }
        NearbyBoard = null;
        if (player.Swimming)
        {
            float best = 1.2f;
            foreach (var board in boards)
            {
                if (board == null || !board.IsBoardable) continue;
                // Swimming feet are below the water; only horizontal proximity
                // determines whether the swimmer can reach this timber edge.
                var bounds=board.Collision.WorldBounds;
                Vector3 probe=transform.position;probe.y=bounds.center.y;
                if(bounds.SqrDistance(probe)>4)continue;
                float gap = board.EdgeDistance(transform.position);
                if (gap >= best || !DryEnough(board,feet,1.1f) || !ClimbClear(board,feet)) continue;
                best = gap; NearbyBoard = board;
            }
            Hint=NearbyBoard!=null ? GameLocalization.Text("Swim towards the board to climb") : "";
            Vector3 toward=NearbyBoard!=null ? Vector3.ProjectOnPlane(NearbyBoard.BoardingPoint(feet)-feet,Vector3.up) : Vector3.zero;
            bool intends=accept && NearbyBoard!=null && (NearbyBoard!=blockedBoard || Time.time>=blockedBoardUntil) && toward.sqrMagnitude>.0001f &&
                Vector3.Dot(player.RequestedMoveDirection,toward.normalized)>.55f;
            if(intentBoard!=NearbyBoard){intentBoard=NearbyBoard;autoClimbIntent=0;}
            autoClimbIntent=intends ? autoClimbIntent+deltaTime : 0;
            // Sustained input and current landing clearance are separate:
            // a passing crest must not repeatedly erase a held intention.
            if(autoClimbIntent>=.2f && ClimbClear(NearbyBoard,feet)){autoClimbIntent=0;BeginClimb(NearbyBoard);}
        }
        else Hint="";
    }
    bool ClimbClear(Story1WreckPlank target,Vector3 feet)
    {
        Vector3 end=target.BoardingPoint(feet);
        if(end.y-feet.y<-.4f)return false;
        // Swimming soles lie well below the surface on the actual scaled
        // Sahur. A fixed feet-to-deck cutoff rejected a reachable water edge.
        // Use the real shoulders and arm-chain reach for the grab instead.
        var animator=player.CharacterAnimator;
        var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var elbow=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        float armReach=Vector3.Distance(upper.position,elbow.position)+Vector3.Distance(elbow.position,hand.position);
        if(end.y-upper.position.y>armReach*.9f+.2f)return false;
        Vector3 scale=transform.lossyScale;
        float radius=capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
        float half=Mathf.Max(0,capsule.height*Mathf.Abs(scale.y)*.5f-radius);
        Vector3 landingRoot=end-Vector3.up*player.LowestFootWorldOffset;
        Vector3 centre=landingRoot+transform.rotation*Vector3.Scale(capsule.center,scale);
        int count=Physics.OverlapCapsuleNonAlloc(centre-Vector3.up*half,centre+Vector3.up*half,radius*.95f,climbOverlaps,~0,QueryTriggerInteraction.Ignore);
        // A full buffer cannot prove clearance.
        if(count==climbOverlaps.Length)return false;
        for(int i=0;i<count;i++)
        {
            var overlap=climbOverlaps[i];
            if(!overlap.transform.IsChildOf(transform) && !overlap.transform.IsChildOf(target.transform))return false;
        }
        return true;
    }

    void BeginClimb(Story1WreckPlank board)
    {
        if(!player.Motion.Claim(this,SahurControlState.Climbing))return;
        climbBoard = board; climbAge = 0; IsClimbing = true;
        Vector3 feet = transform.position + Vector3.up * player.LowestFootWorldOffset;
        climbStart = board.transform.InverseTransformPoint(feet);
        climbEnd = board.transform.InverseTransformPoint(board.BoardingPoint(feet));
        Vector3 toward = Vector3.ProjectOnPlane(board.transform.TransformPoint(climbEnd) - feet, Vector3.up);
        if (toward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(toward);
        Vector3 edge=board.BoardingPoint(feet)-toward.normalized*.55f;
        Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,board.Dimensions.y+1);
        if(board.Collision.Raycast(new Ray(edge+normal*reach,-normal),out var gripHit,reach*2))edge=gripHit.point;
        leftGrip=board.transform.InverseTransformPoint(edge-transform.right*.35f);
        rightGrip=board.transform.InverseTransformPoint(edge+transform.right*.35f);
        riderYaw = Mathf.DeltaAngle(board.transform.eulerAngles.y, transform.eulerAngles.y);
        attackWasEnabled = attack.enabled; boomerangWasEnabled = boomerang.enabled;
        attack.SuspendForSwimming(); attack.enabled = boomerang.enabled = false;
        capsule.enabled = false;
        player.ExternalMovementLock = true; player.ExternalMovementAnimation = "Wreck Climb";
        player.ExternalCameraFocus = player.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head);
        player.KeepCameraAboveWater = true;
        if (weapon != null) weapon.Climbing = true;
    }

    public void AfterPlanks(float deltaTime)
    {
        if (player == null || !enabled) return;
        if (IsClimbing)
        {
            if (climbBoard == null || !climbBoard.Deck.enabled) { Release(); return; }
            climbAge += deltaTime;
            float t = Mathf.Clamp01(climbAge / ClimbSeconds);
            // Pull up first; only cross the solid edge once the soles clear the deck.
            Vector3 start = climbBoard.transform.TransformPoint(climbStart), end = climbBoard.transform.TransformPoint(climbEnd);
            float crossing = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.38f, 1, t));
            Vector3 feet = Vector3.Lerp(start, end, crossing);
            feet.y = Mathf.Lerp(start.y, end.y + .05f, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .65f)));
            SetFeet(feet, climbBoard.transform.eulerAngles.y + riderYaw, false);
            if (t >= 1)
            {
                var landed=climbBoard;
                Release(); player.LeaveMovingPlatform(Vector3.zero);
                SetStandingBoard(landed);player.OnFloatingWreckDeck=true;
                Physics.SyncTransforms(); capsule.Move(Vector3.down * .12f);
                player.NotifyPlatformLanding();
            }
        }
    }

    void SetStandingBoard(Story1WreckPlank next)
    {
        // A CharacterController has no dynamic mass. Let its supporting rigidbody
        // rise with buoyancy rather than treating the passenger as an immovable lid.
        if(standingBoard!=next)
        {
            if(standingBoard!=null && capsule!=null)standingBoard.Collision.Ignore(capsule,false);
            standingBoard=next;
        }
        if(standingBoard!=null && capsule!=null)standingBoard.Collision.Ignore(capsule,true);
    }
    void LateUpdate()
    {
        if(player==null || !player.enabled || IsClimbing || !capsule.enabled ||
            player.ExternalControlLock || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        if(tutorialWalkingDeck!=null && tutorialWalkingDeck.IsBoardable)
        {
            Vector3 correction=ConstrainTutorialStep(Vector3.zero);
            if(correction.sqrMagnitude>.000001f)capsule.Move(correction);
            SetStandingBoard(tutorialWalkingDeck);player.OnFloatingWreckDeck=true;
        }
        if(standingBoard!=null && player.VerticalSpeed>1.5f)
        {
            SetStandingBoard(null);player.OnFloatingWreckDeck=false;return;
        }
        // Bound only grounded attack root motion; intentional walking and jumps are free.
        if(combatBoard!=null && attack.UsesAnimationRootMotion && combatBoard.IsBoardable)
        {
            Vector3 feet=transform.position+Vector3.up*player.LowestFootWorldOffset;
            Vector3 safe=combatBoard.BoardingPoint(feet);
            Vector3 correction=Vector3.ProjectOnPlane(safe-feet,Vector3.up);
            if((standingBoard==combatBoard || Mathf.Abs(safe.y-feet.y)<=1.1f) && correction.sqrMagnitude>.0001f && correction.sqrMagnitude<9)
                capsule.Move(correction);
        }
        if(standingBoard==null)return;
        Vector3 soles=transform.position+Vector3.up*player.LowestFootWorldOffset;
        Vector3 local=standingBoard.transform.InverseTransformPoint(soles);
        Vector3 top=standingBoard.BoardingPoint(soles);
        if(!standingBoard.IsBoardable || !standingBoard.ContainsFootprint(soles) || !DryEnough(standingBoard,soles))
        {
            SetStandingBoard(null);player.OnFloatingWreckDeck=false;return;
        }
        // Follow the real deck after normal gravity/root motion, without affecting XZ controls.
        capsule.Move(Vector3.up*(top.y+.04f-soles.y));player.OnFloatingWreckDeck=true;
    }

    void SetFeet(Vector3 feet, float yaw, bool ground)
    {
        bool active = capsule.enabled;
        if (active) capsule.enabled = false;
        transform.SetPositionAndRotation(feet - Vector3.up * player.LowestFootWorldOffset, Quaternion.Euler(0, yaw, 0));
        if (active)
        {
            capsule.enabled = true;
            if (ground) capsule.Move(Vector3.down * .1f);
        }
    }

    public void Release()
    {
        EndWalkingTutorial();
        player?.Motion?.Release(this);
        SetStandingBoard(null);
        NearbyBoard = null; combatBoard=null; Hint = "";
        if (player != null)
        {
            player.ExternalMovementLock = false; player.ExternalMovementAnimation = null;
            player.ExternalCameraFocus = null; player.KeepCameraAboveWater = false;
        }
        if (IsClimbing)
        {
            if (capsule != null) capsule.enabled = true;
            if (attack != null) attack.enabled = attackWasEnabled;
            if (boomerang != null) boomerang.enabled = boomerangWasEnabled;
        }
        IsClimbing = false; climbBoard = null;
        if (weapon != null) weapon.Climbing = false;
    }
    public void ApplyClimbSupportPose(Animator animator)
    {
        if(!IsClimbing || climbBoard==null || animator==null)return;
        float phase=climbAge/ClimbSeconds;
        float weight=Mathf.SmoothStep(0,1,phase/.12f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.84f,phase)));
        SahurSupportPose.Hand(animator,true,climbBoard.transform.TransformPoint(leftGrip),weight);
        SahurSupportPose.Hand(animator,false,climbBoard.transform.TransformPoint(rightGrip),weight);
    }
    public void OnBoardBroken(Story1WreckPlank board)
    {
        if(standingBoard!=board && combatBoard!=board && climbBoard!=board)return;
        if(standingBoard==board)SetStandingBoard(null);
        if(combatBoard==board)combatBoard=null;
        if(climbBoard==board)Release();
        player.OnFloatingWreckDeck=false;
        player.LeaveMovingPlatform(board.Velocity);
    }

    void OnDestroy()
    {
        if(boards!=null && bodyHitboxes!=null)
            foreach(var board in boards)foreach(var hitbox in bodyHitboxes)
                if(board!=null && board.Deck!=null && hitbox!=null)board.Collision.Ignore(hitbox,false);
    }
    void OnDisable()
    {
        if(player!=null)player.OnFloatingWreckDeck=false;
        Release();
        if (ocean != null) ocean.SetShipWake(Vector4.zero, Vector4.zero, trail, 0);
    }
}
