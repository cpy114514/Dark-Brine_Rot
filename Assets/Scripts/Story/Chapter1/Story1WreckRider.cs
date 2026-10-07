using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Boarding and surfing leave the existing jump, swimming and combat controls intact.</summary>
[DisallowMultipleComponent]
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
    Story1WreckPlank climbBoard;
    OceanWorld ocean;
    Vector3 anchor, climbStart, climbEnd;
    float riderYaw, climbAge, wakeTimer;
    bool attackWasEnabled, boomerangWasEnabled;
    readonly Vector4[] trail = new Vector4[16];
    int trailCount;
    const float ClimbSeconds = .95f;

    public void Initialize(ThirdPersonPlayerController actor, IReadOnlyList<Story1WreckPlank> fragments, OceanWorld water)
    {
        player = actor; boards = fragments; ocean = water;
        capsule = GetComponent<CharacterController>(); attack = GetComponent<SahurAttack>();
        boomerang = GetComponent<SahurBoomerang>(); weapon = GetComponent<SahurSwimmingWeapon>();
        enabled = true;
    }

    public Story1WreckPlank Support()
    {
        if (player == null || boards == null || IsClimbing || player.Swimming) return null;
        Vector3 feet = transform.position + Vector3.up * player.LowestFootWorldOffset;
        foreach (var board in boards) if (board != null && board.Supports(feet)) return board;
        return null;
    }

    public void BeforePlanks(float deltaTime)
    {
        if (player == null || !enabled) return;
        Vector3 feet=transform.position+Vector3.up*player.LowestFootWorldOffset;
        player.OnFloatingWreckDeck=DrivenBoard!=null;
        foreach(var board in boards)if(board!=null && board.Supports(feet)){player.OnFloatingWreckDeck=true;break;}
        bool accept = Keyboard.current != null && Cursor.lockState == CursorLockMode.Locked &&
            !player.ExternalControlLock && !SahurLoadoutUI.BlocksInput && !PlayerDeathRespawn.BlocksInput && !IslandMapUI.IsOpen;
        if (IsClimbing) { Hint = GameLocalization.Text("Climbing onto the board..."); return; }
        var support = Support();
        if (DrivenBoard != null)
        {
            // Its pose can move between physics ticks; the deck-local anchor owns mounted footing.
            if (!DrivenBoard.Deck.enabled || Mathf.Abs(DrivenBoard.transform.up.y)<.35f) { Release(); return; }
            bool jump = accept && GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump) && !attack.IsCombatMotionActive;
            if (accept && (Keyboard.current.fKey.wasPressedThisFrame || jump))
            {
                Vector3 velocity = DrivenBoard.Velocity;
                Release();
                if (jump) player.LeaveMovingPlatform(velocity);
                return;
            }
            float throttle = accept ? Axis(GameInputSettings.Action.Forward, GameInputSettings.Action.Back) : 0;
            float steer = accept ? Axis(GameInputSettings.Action.Right, GameInputSettings.Action.Left) : 0;
            DrivenBoard.Drive(throttle, steer, transform);
            Hint = GameLocalization.Format("{0}/{1}: forward/back  {2}/{3}: steer  {4}: jump off  F: walk on board",
                KeyName(GameInputSettings.Action.Forward), KeyName(GameInputSettings.Action.Back),
                KeyName(GameInputSettings.Action.Left), KeyName(GameInputSettings.Action.Right), KeyName(GameInputSettings.Action.Jump));
            return;
        }
        NearbyBoard = null;
        if (player.Swimming)
        {
            float best = 1.6f;
            foreach (var board in boards)
            {
                if (board == null || !board.IsBoardable) continue;
                float gap = board.EdgeDistance(transform.position);
                if (gap >= best) continue;
                best = gap; NearbyBoard = board;
            }
            Hint = NearbyBoard != null ? GameLocalization.Format("{0}: climb onto the board", KeyName(GameInputSettings.Action.Jump)) :
                GameLocalization.Format("Swim near a board, then press {0} to climb", KeyName(GameInputSettings.Action.Jump));
            if (accept && NearbyBoard != null && GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump)) BeginClimb(NearbyBoard);
        }
        else if (support != null && player.CanUseGroundAttack)
        {
            Hint = GameLocalization.Format("F: surf on this board  {0}: jump between boards", KeyName(GameInputSettings.Action.Jump));
            if (accept && Keyboard.current.fKey.wasPressedThisFrame && !attack.IsCombatMotionActive)
            {
                DrivenBoard = support;
                anchor = support.transform.InverseTransformPoint(transform.position + Vector3.up * player.LowestFootWorldOffset);
                anchor.y = support.TopLocalY;
                riderYaw = Mathf.DeltaAngle(support.transform.eulerAngles.y, transform.eulerAngles.y);
                player.ExternalMovementLock = true; player.ExternalMovementAnimation = "Wreck Surf";
                support.Drive(0, 0, transform);
            }
        }
        else Hint = "";
    }

    static float Axis(GameInputSettings.Action positive, GameInputSettings.Action negative) =>
        (GameInputSettings.Pressed(positive) ? 1 : 0) - (GameInputSettings.Pressed(negative) ? 1 : 0);
    static string KeyName(GameInputSettings.Action action) => GameInputSettings.Get(action).ToString().ToUpperInvariant();

    void BeginClimb(Story1WreckPlank board)
    {
        climbBoard = board; climbAge = 0; IsClimbing = true;
        Vector3 feet = transform.position + Vector3.up * player.LowestFootWorldOffset;
        climbStart = board.transform.InverseTransformPoint(feet);
        climbEnd = board.transform.InverseTransformPoint(board.BoardingPoint(feet));
        Vector3 toward = Vector3.ProjectOnPlane(board.transform.TransformPoint(climbEnd) - feet, Vector3.up);
        if (toward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(toward);
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
                player.CharacterAnimator.CrossFadeInFixedTime("Land", .08f);
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
        if (DrivenBoard != null) DrivenBoard.Drive(0, 0, null);
        DrivenBoard = null; NearbyBoard = null; Hint = "";
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

    void OnDisable()
    {
        if(player!=null)player.OnFloatingWreckDeck=false;
        Release(); trailCount = 0;
        if (ocean != null) ocean.SetShipWake(Vector4.zero, Vector4.zero, trail, 0);
    }
}
