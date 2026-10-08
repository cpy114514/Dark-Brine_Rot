using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Boarding and surfing leave the existing jump, swimming and combat controls intact.</summary>
[DisallowMultipleComponent,DefaultExecutionOrder(-100)]
public sealed class Story1WreckRider : MonoBehaviour
{
    public bool IsSurfing => DrivenBoard != null;
    public bool IsClimbing { get; private set; }
    public Story1WreckPlank DrivenBoard { get; private set; }
    public Story1WreckPlank NearbyBoard { get; private set; }
    public string Hint { get; private set; } = "";

    ThirdPersonPlayerController player;
    CharacterController capsule;
    SahurAttack attack;
    SahurBoomerang boomerang;
    SahurSwimmingWeapon weapon;
    IReadOnlyList<Story1WreckPlank> boards;
    Collider[] bodyHitboxes;
    Story1WreckPlank climbBoard,combatBoard,standingBoard,blockedBoard;
    OceanWorld ocean;
    Vector3 anchor, climbStart, climbEnd,leftGrip,rightGrip;
    float riderYaw, climbAge, wakeTimer;
    bool attackWasEnabled, boomerangWasEnabled;
    float autoClimbIntent,blockedBoardUntil;
    readonly Vector4[] trail = new Vector4[16];
    int trailCount;
    const float ClimbSeconds = 1.15f;

    public void Initialize(ThirdPersonPlayerController actor, IReadOnlyList<Story1WreckPlank> fragments, OceanWorld water,Story1WreckPlank initialSupport=null,IReadOnlyList<Story1WreckDebris> debris=null)
    {
        player = actor; boards = fragments; ocean = water;
        capsule = GetComponent<CharacterController>(); attack = GetComponent<SahurAttack>();
        boomerang = GetComponent<SahurBoomerang>(); weapon = GetComponent<SahurSwimmingWeapon>();
        var all=GetComponentsInChildren<Collider>(true);
        bodyHitboxes=System.Array.FindAll(all,c=>c!=capsule && !c.isTrigger);
        // Detailed animated hurtboxes still receive weapon queries, but their
        // kinematic feet must not pin floating wood. The controller owns locomotion.
        foreach(var board in boards)foreach(var hitbox in bodyHitboxes)
            if(board!=null && hitbox!=null)board.Collision.Ignore(hitbox,true);
        if(debris!=null)foreach(var piece in debris)RegisterCollision(piece.Collision);
        enabled = true;
        if(initialSupport!=null){SetStandingBoard(initialSupport);player.OnFloatingWreckDeck=true;}
    }
    public void RegisterBoard(Story1WreckPlank board)
    {
        RegisterCollision(board.Collision);
    }
    public void RegisterCollision(Story1WreckMeshCollision shape)
    {
        if(bodyHitboxes==null)return;
        foreach(var hitbox in bodyHitboxes)if(hitbox!=null)shape.Ignore(hitbox,true);
    }

    public Story1WreckPlank Support()
    {
        if (player == null || boards == null || IsClimbing || player.Swimming) return null;
        Vector3 feet = transform.position + Vector3.up * player.LowestFootWorldOffset;
        if(standingBoard!=null && standingBoard.IsBoardable && standingBoard.ContainsFootprint(feet) &&
            Mathf.Abs(standingBoard.BoardingPoint(feet).y-feet.y)<1.1f)return standingBoard;
        foreach (var board in boards) if (board != null && board.Supports(feet)) return board;
        return null;
    }

    public void BeforePlanks(float deltaTime)
    {
        if (player == null || !enabled) return;
        Vector3 feet=transform.position+Vector3.up*player.LowestFootWorldOffset;
        player.OnFloatingWreckDeck=DrivenBoard!=null || (standingBoard!=null && standingBoard.IsBoardable &&
            standingBoard.ContainsFootprint(feet) && Mathf.Abs(standingBoard.BoardingPoint(feet).y-feet.y)<1.1f && player.VerticalSpeed<=1.5f);
        bool ascending=player.VerticalSpeed>1.5f;
        if(!ascending)foreach(var board in boards)if(board!=null && board.Supports(feet)){player.OnFloatingWreckDeck=true;break;}
        bool accept = Keyboard.current != null && Cursor.lockState == CursorLockMode.Locked &&
            !player.ExternalControlLock && !SahurLoadoutUI.BlocksInput && !PlayerDeathRespawn.BlocksInput && !IslandMapUI.IsOpen;
        if (IsClimbing) { Hint = GameLocalization.Text("Climbing onto the board..."); return; }
        var support = ascending ? null : Support();
        SetStandingBoard(support);
        if(support!=null)combatBoard=support;else if(!attack.IsCombatMotionActive)combatBoard=null;
        if (DrivenBoard != null)
        {
            // Its pose can move between physics ticks; the deck-local anchor owns mounted footing.
            if (!DrivenBoard.Deck.enabled || Mathf.Abs(DrivenBoard.transform.up.y)<.35f) { Release(); return; }
            bool jump = accept && GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump) && !attack.IsCombatMotionActive;
            if (jump)
            {
                Vector3 velocity = DrivenBoard.Velocity;blockedBoard=DrivenBoard;
                Release();
                blockedBoardUntil=Time.time+.35f;player.OnFloatingWreckDeck=false;player.LeaveMovingPlatform(velocity,true);
                return;
            }
            float throttle = accept ? Axis(GameInputSettings.Action.Forward, GameInputSettings.Action.Back) : 0;
            float steer = accept ? Axis(GameInputSettings.Action.Right, GameInputSettings.Action.Left) : 0;
            DrivenBoard.Drive(throttle, steer, transform);
            player.CharacterAnimator.SetFloat("BoardThrottle",throttle,.18f,deltaTime);
            player.CharacterAnimator.SetFloat("BoardSteer",steer,.18f,deltaTime);
            Hint = GameLocalization.Format("{0}/{1}: forward/back  {2}/{3}: steer  {4}: jump off",
                KeyName(GameInputSettings.Action.Forward), KeyName(GameInputSettings.Action.Back),
                KeyName(GameInputSettings.Action.Left), KeyName(GameInputSettings.Action.Right), KeyName(GameInputSettings.Action.Jump));
            return;
        }
        NearbyBoard = null;
        if (player.Swimming)
        {
            float best = 1.2f;
            foreach (var board in boards)
            {
                if (board == null || !board.IsBoardable) continue;
                float gap = board.EdgeDistance(transform.position);
                if (gap >= best) continue;
                best = gap; NearbyBoard = board;
            }
            Hint=NearbyBoard!=null ? GameLocalization.Text("Swim towards the board to climb") : "";
            Vector3 toward=NearbyBoard!=null ? Vector3.ProjectOnPlane(NearbyBoard.BoardingPoint(feet)-feet,Vector3.up) : Vector3.zero;
            bool intends=accept && NearbyBoard!=null && (NearbyBoard!=blockedBoard || Time.time>=blockedBoardUntil) && toward.sqrMagnitude>.0001f &&
                Vector3.Dot(player.RequestedMoveDirection,toward.normalized)>.55f && ClimbClear(NearbyBoard,feet);
            autoClimbIntent=intends ? autoClimbIntent+deltaTime : 0;
            if(autoClimbIntent>=.2f){autoClimbIntent=0;BeginClimb(NearbyBoard);}
        }
        else if (support != null && player.CanUseGroundAttack && (support!=blockedBoard || Time.time>=blockedBoardUntil))
        {
            Mount(support);
        }
        else Hint="";
    }
    void Mount(Story1WreckPlank support)
    {
        if(!player.Motion.Claim(this,SahurControlState.BoardRiding))return;
        DrivenBoard=support;
        anchor=support.transform.InverseTransformPoint(support.BoardingPoint(transform.position+Vector3.up*player.LowestFootWorldOffset));
        riderYaw=Mathf.DeltaAngle(support.transform.eulerAngles.y,transform.eulerAngles.y);
        player.ExternalMovementLock=true;player.ExternalMovementAnimation="Wreck Surf";
        player.OnFloatingWreckDeck=true;support.Drive(0,0,transform);
    }
    bool ClimbClear(Story1WreckPlank target,Vector3 feet)
    {
        Vector3 end=target.BoardingPoint(feet);
        float gap=end.y-feet.y;if(gap<-.4f || gap>3.8f)return false;
        Vector3 bottom=end+Vector3.up*(capsule.radius+.08f),top=end+Vector3.up*(capsule.height-capsule.radius);
        var overlaps=Physics.OverlapCapsule(bottom,top,capsule.radius*.8f,~0,QueryTriggerInteraction.Ignore);
        foreach(var overlap in overlaps)
            if(!overlap.transform.IsChildOf(transform) && !overlap.transform.IsChildOf(target.transform))return false;
        return true;
    }
    static float Axis(GameInputSettings.Action positive, GameInputSettings.Action negative) =>
        (GameInputSettings.Pressed(positive) ? 1 : 0) - (GameInputSettings.Pressed(negative) ? 1 : 0);
    static string KeyName(GameInputSettings.Action action) => GameInputSettings.Get(action).ToString().ToUpperInvariant();

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
                Release(); player.LeaveMovingPlatform(Vector3.zero);
                Physics.SyncTransforms(); capsule.Move(Vector3.down * .12f);
                player.NotifyPlatformLanding();
            }
        }
        else if (DrivenBoard != null)
        {
            // Preserve the same deck-space anchor through both yaw and wave motion.
            SetFeet(DrivenBoard.transform.TransformPoint(anchor) + Vector3.up * .04f,
                DrivenBoard.transform.eulerAngles.y + riderYaw, true);
        }
        UpdateWake(deltaTime);
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
        if(player==null || !player.enabled || IsSurfing || IsClimbing || !capsule.enabled ||
            player.ExternalControlLock || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
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
        if(!standingBoard.IsBoardable || !standingBoard.ContainsFootprint(soles))
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
            if(DrivenBoard!=null)DrivenBoard.Collision.Ignore(capsule,true);
            if (ground) capsule.Move(Vector3.down * .1f);
        }
    }

    void UpdateWake(float deltaTime)
    {
        if (ocean == null) return;
        float speed = DrivenBoard != null ? Mathf.Abs(DrivenBoard.Speed) : 0;
        wakeTimer += deltaTime;
        if (speed > .4f && wakeTimer >= .16f)
        {
            wakeTimer = 0; trailCount = Mathf.Min(trailCount + 1, trail.Length);
            for (int i = trailCount - 1; i > 0; i--) trail[i] = trail[i - 1];
            Vector3 stern = DrivenBoard.transform.position - DrivenBoard.Velocity.normalized * (DrivenBoard.Dimensions.z * .5f);
            trail[0] = new Vector4(stern.x, stern.z, Time.time, DrivenBoard.Dimensions.x * .22f);
        }
        while (trailCount > 0 && Time.time - trail[trailCount - 1].z > 3.2f) trailCount--;
        Vector3 at = DrivenBoard != null ? DrivenBoard.transform.position : transform.position;
        Vector3 direction = DrivenBoard != null ? DrivenBoard.Velocity.normalized : Vector3.zero;
        Vector3 dimensions = DrivenBoard != null ? DrivenBoard.Dimensions : Vector3.zero;
        ocean.SetShipWake(new Vector4(at.x, at.z, dimensions.z * .5f, dimensions.x * .5f),
            new Vector4(direction.x, direction.z, Mathf.Clamp01(speed / 10f), 3.2f), trail, trailCount);
    }

    public void Release()
    {
        player?.Motion?.Release(this);
        SetStandingBoard(null);
        if (DrivenBoard != null) DrivenBoard.Drive(0, 0, null);
        DrivenBoard = null; NearbyBoard = null; combatBoard=null; Hint = "";
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
        if(standingBoard!=board && combatBoard!=board && DrivenBoard!=board && climbBoard!=board)return;
        if(standingBoard==board)SetStandingBoard(null);
        if(combatBoard==board)combatBoard=null;
        if(DrivenBoard==board || climbBoard==board)Release();
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
        Release(); trailCount = 0;
        if (ocean != null) ocean.SetShipWake(Vector4.zero, Vector4.zero, trail, 0);
    }
}
