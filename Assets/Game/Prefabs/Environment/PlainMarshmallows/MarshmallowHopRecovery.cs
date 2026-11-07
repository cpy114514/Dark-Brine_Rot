using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Returns a fallen course participant to the appropriate moving row.</summary>
[DefaultExecutionOrder(500), DisallowMultipleComponent, RequireComponent(typeof(MarshmallowHopMotion))]
public sealed class MarshmallowHopRecovery : MonoBehaviour
{
    [Min(1f)] public float fallDepth = 5f;
    [Min(.1f)] public float safeGroundExitDelay = 1f;
    readonly List<BoxCollider[]> rows = new List<BoxCollider[]>();
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    Collider islandGround;
    BoxCollider lastPlatform;
    float nextLookup, nextScan, safeGroundSince = -1, ignoreUntil;
    float returnHold;
    bool armed;
    int highestRow, consecutiveFalls, assistUntilRow, starMask;
    public bool AssistanceActive { get; private set; }
    public int HighestRow => highestRow;
    public int ConsecutiveFalls => consecutiveFalls;
    public int AssistUntilRow => assistUntilRow;
    public int StarMask => starMask;
    public int RescueRow => Mathf.Max(1, highestRow - 2);
    public int StarCount
    {
        get { int count=0; for(int i=0;i<5;i++) if((starMask & (1 << i)) != 0) count++; return count; }
    }
    public static readonly int[] StarRows = { 5, 10, 15, 20, 22 };
    // Paired rows put the star on only one branch, making it optional.
    public static readonly int[] StarSides = { 0, 1, 0, 0, 1 };
    public int CurrentRow { get; private set; }
    public int LastRespawnRow { get; private set; }
    public int RespawnCount { get; private set; }
    public bool RecoveryArmed => armed;
    public void RestoreChallengeProgress(int best, int falls, int stars, bool assist, int untilRow)
    {
        highestRow = Mathf.Clamp(best, 0, rows.Count);
        consecutiveFalls = Mathf.Max(0, falls);
        starMask = stars & 31;
        AssistanceActive = assist && highestRow > 0 && highestRow < rows.Count;
        assistUntilRow = AssistanceActive ? Mathf.Clamp(untilRow, highestRow, rows.Count) : 0;
    }
    public int SaveRow
    {
        get
        {
            if (!player || !lastPlatform || CurrentRow < 1) return 0;
            Vector3 delta=Vector3.ProjectOnPlane(lastPlatform.bounds.center-player.transform.position,Vector3.up);
            return delta.sqrMagnitude<=18f*18f ? CurrentRow : 0;
        }
    }
    public void RestoreRow(int row, bool wasArmed)
    {
        if(!player)player=FindFirstObjectByType<ThirdPersonPlayerController>();
        if(player && !body)body=player.GetComponent<CharacterController>();
        if(row>0 && row<=rows.Count && player && body){ReturnToRow(row);armed=wasArmed;}
    }

    // A deliberate flight out of the course must not be treated as a missed jump.
    public void ClearTracking()
    {
        armed = false; CurrentRow = 0; lastPlatform = null; safeGroundSince = -1;
    }

    void Awake()
    {
        foreach (Transform row in transform)
        {
            if (!row.name.StartsWith("Row ")) continue;
            var platforms = new List<BoxCollider>();
            foreach (Transform item in row)
            {
                var box = item.GetComponentInChildren<BoxCollider>();
                if (box) platforms.Add(box);
            }
            rows.Add(platforms.ToArray());
        }
    }

    void Start()
    {
        if (!GetComponent<MarshmallowHopPresentation>()) gameObject.AddComponent<MarshmallowHopPresentation>();
    }

    void Update()
    {
        if (Time.deltaTime <= 0 || PauseSettingsMenu.IsOpen || Time.time < ignoreUntil) return;
        if ((!player || !body || !health) && Time.time >= nextLookup)
        {
            nextLookup = Time.time + 1f;
            player = FindFirstObjectByType<ThirdPersonPlayerController>();
            body = player ? player.GetComponent<CharacterController>() : null;
            health = player ? player.GetComponent<PlayerHealth>() : null;
            var experience = player ? player.GetComponent<MarshmallowIslandExperience>() : null;
            islandGround = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        }
        if (!player || !body || !body.enabled || !health || health.currentHealth <= 0 ||
            player.ExternalControlLock || player.ExternalMovementLock || PlayerDeathRespawn.IsOpen) return;
        if (rows.Count == 0) return;

        // A deliberate return gives players a way to take a break without
        // losing collected stars or requiring another fall.
        bool returning=armed && CurrentRow>1 && !GameUITheme.ModalOpen && Cursor.lockState==CursorLockMode.Locked && Keyboard.current!=null && Keyboard.current.backspaceKey.isPressed;
        returnHold=returning?returnHold+Time.deltaTime:0;
        if(returnHold>=.8f)
        {
            returnHold=0;ReturnToRow(1);GetComponent<MarshmallowHopPresentation>()?.ShowReturn();GameSaveManager.RequestAutosave();return;
        }

        float feet = body.bounds.min.y;
        // Walking back onto the island is an exit, not a missed jump.
        bool onIsland = armed && body.isGrounded && islandGround &&
            islandGround.Raycast(new Ray(new Vector3(body.bounds.center.x, feet + .16f, body.bounds.center.z), Vector3.down), out _, .32f);
        if (onIsland)
        {
            if (safeGroundSince < 0) safeGroundSince = Time.time;
            if (Time.time - safeGroundSince >= safeGroundExitDelay) ClearTracking();
            return;
        }
        if (armed && lastPlatform && (feet < lastPlatform.bounds.max.y - fallDepth ||
            body.isGrounded && feet < lastPlatform.bounds.max.y - 1.5f))
        {
            consecutiveFalls++;
            if (consecutiveFalls >= 3 && !AssistanceActive)
            {
                AssistanceActive = true;
                assistUntilRow = Mathf.Min(rows.Count, highestRow + 3);
            }
            int normalRow = CurrentRow >= 10 ? CurrentRow - 5 : 1;
            ReturnToRow(AssistanceActive ? Mathf.Max(normalRow, RescueRow) : normalRow);
            GetComponent<MarshmallowHopPresentation>()?.ShowFall(LastRespawnRow, AssistanceActive);
            GameSaveManager.RequestAutosave();
            return;
        }
        if (Time.time < nextScan) return;
        nextScan = Time.time + .05f;
        BoxCollider standing = FindStandingPlatform(feet, out int rowNumber);
        if (standing)
        {
            if (rowNumber != CurrentRow && (rowNumber == 1 || rowNumber % 5 == 0 || rowNumber == rows.Count))
                GameSaveManager.RequestAutosave();
            CurrentRow = rowNumber;
            lastPlatform = standing;
            armed = true;
            if (rowNumber > highestRow)
            {
                highestRow = rowNumber;
                if (!AssistanceActive) consecutiveFalls = 0;
                if (AssistanceActive && rowNumber >= assistUntilRow)
                { AssistanceActive = false; consecutiveFalls = 0; assistUntilRow = 0; }
            }
            for (int i=0;i<StarRows.Length;i++)
                if (rowNumber == StarRows[i] && (starMask & (1 << i)) == 0 &&
                    rows[rowNumber-1].Length > StarSides[i] && standing == rows[rowNumber-1][StarSides[i]])
                {
                    starMask |= 1 << i;
                    GetComponent<MarshmallowHopPresentation>()?.ShowStar(StarCount);
                    GameSaveManager.RequestAutosave();
                }
            safeGroundSince = -1;
        }
        else if (armed && body.isGrounded && lastPlatform && feet >= lastPlatform.bounds.max.y - 1.5f)
        {
            // Leaving the course onto safe land should not affect exploration.
            if (safeGroundSince < 0) safeGroundSince = Time.time;
            if (Time.time - safeGroundSince >= safeGroundExitDelay)
            { armed = false; CurrentRow = 0; lastPlatform = null; safeGroundSince = -1; }
        }
        else safeGroundSince = -1;
    }

    BoxCollider FindStandingPlatform(float feet, out int rowNumber)
    {
        rowNumber = 0;
        if (!body.isGrounded || player.WorldVelocity.y > .5f) return null;
        Vector3 center = body.bounds.center;
        float margin = Mathf.Min(body.bounds.extents.x, body.bounds.extents.z) * .75f;
        float best = .4f;
        BoxCollider match = null;
        for (int r = 0; r < rows.Count; r++)
            foreach (var box in rows[r])
            {
                if (!box || !box.enabled || !box.gameObject.activeInHierarchy) continue;
                Bounds bounds = box.bounds;
                float difference = Mathf.Abs(feet - bounds.max.y);
                if (difference >= best || Mathf.Abs(center.x - bounds.center.x) > bounds.extents.x + margin ||
                    Mathf.Abs(center.z - bounds.center.z) > bounds.extents.z + margin) continue;
                best = difference; match = box; rowNumber = r + 1;
            }
        return match;
    }

    void ReturnToRow(int rowNumber)
    {
        if (rowNumber < 1 || rowNumber > rows.Count) return;
        BoxCollider destination = null;
        float nearest = float.PositiveInfinity;
        foreach (var box in rows[rowNumber - 1])
        {
            if (!box || !box.enabled || !box.gameObject.activeInHierarchy) continue;
            Vector3 delta = Vector3.ProjectOnPlane(box.bounds.center - player.transform.position, Vector3.up);
            if (delta.sqrMagnitude >= nearest) continue;
            nearest = delta.sqrMagnitude; destination = box;
        }
        if (!destination) return;
        Bounds target = destination.bounds;
        float footOffset = -(body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
        Vector3 position = new Vector3(target.center.x, target.max.y + footOffset + .06f, target.center.z);
        Vector3 direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        player.RestoreSavedPose(position, direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : Quaternion.identity);
        CurrentRow = LastRespawnRow = rowNumber;
        RespawnCount++;
        lastPlatform = destination;
        // Even the first two jumps get a safety net after recovery.
        armed = true;
        safeGroundSince = -1;
        ignoreUntil = Time.time + .5f;
    }
}
