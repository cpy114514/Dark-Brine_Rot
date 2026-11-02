using System.Collections.Generic;
using Mavis;
using UnityEngine;

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
    BoxCollider lastPlatform;
    Transform rulesSign;
    float nextLookup, nextScan, safeGroundSince = -1, ignoreUntil;
    bool armed;
    public int CurrentRow { get; private set; }
    public int LastRespawnRow { get; private set; }
    public int RespawnCount { get; private set; }
    public bool RecoveryArmed => armed;
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
        if (rows.Count>0 && rows[0].Length>0 && rows[0][0])
        {
            var parent=rows[0][0].transform;
            SkyboardReward.Label(parent, "MOVING MARSHMALLOWS\nPlatforms move and swap · low gravity helps\nRows 3-9: return to start\nRow 10+: return 5 rows");
            rulesSign=parent.Find("Pickup label");
            if(rulesSign)
            {
                Vector3 scale=parent.lossyScale;
                rulesSign.localScale=new Vector3(.012f/Mathf.Max(.001f,Mathf.Abs(scale.x)),.012f/Mathf.Max(.001f,Mathf.Abs(scale.y)),.012f/Mathf.Max(.001f,Mathf.Abs(scale.z)));
                rulesSign.localPosition=Vector3.up*(2.3f/Mathf.Max(.001f,Mathf.Abs(scale.y)));
                var label=rulesSign.GetComponentInChildren<UnityEngine.UI.Text>();if(label)label.fontSize=20;
            }
        }
    }

    void Update()
    {
        if(rulesSign && Camera.main)rulesSign.rotation=Camera.main.transform.rotation;
        if (Time.deltaTime <= 0 || PauseSettingsMenu.IsOpen || Time.time < ignoreUntil) return;
        if ((!player || !body || !health) && Time.time >= nextLookup)
        {
            nextLookup = Time.time + 1f;
            player = FindFirstObjectByType<ThirdPersonPlayerController>();
            body = player ? player.GetComponent<CharacterController>() : null;
            health = player ? player.GetComponent<PlayerHealth>() : null;
        }
        if (!player || !body || !body.enabled || !health || health.currentHealth <= 0 ||
            player.ExternalControlLock || player.ExternalMovementLock || PlayerDeathRespawn.IsOpen) return;
        if (rows.Count == 0) return;

        float feet = body.bounds.min.y;
        if (armed && lastPlatform && (feet < lastPlatform.bounds.max.y - fallDepth ||
            body.isGrounded && feet < lastPlatform.bounds.max.y - 1.5f))
        {
            ReturnToRow(CurrentRow >= 10 ? CurrentRow - 5 : 1);
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
            if (rowNumber >= 3) armed = true;
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
        // A checkpoint behind row 3 remains protected on subsequent falls.
        armed = rowNumber != 1;
        safeGroundSince = -1;
        ignoreUntil = Time.time + .5f;
    }
}
