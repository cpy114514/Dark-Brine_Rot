using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Non-blocking objectives for the second island's existing encounters and rewards.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandObjectives : MonoBehaviour
{
    public enum Objective { Marshmallows, Wristband, ZhuBaJie, Course, Scrolls, Complete }
    public Objective Current { get; private set; }
    public bool FinishedCourse { get; private set; }
    public bool LegacyRoninDefeated { get; private set; }
    public bool RidgeCleared => FinishedCourse || LegacyRoninDefeated || zhuBaJie && zhuBaJie.Health.IsDead;
    public void RestoreProgress(bool finishedCourse, bool defeatedRonin)
    { FinishedCourse = finishedCourse; LegacyRoninDefeated = defeatedRonin; nextRefresh = 0; }
    public string CurrentText => label ? label.text : "";
    public bool Visible => panel && panel.activeSelf;
    public Transform CurrentTarget { get; private set; }

    ThirdPersonPlayerController player;
    PlayerHealth health;
    GravityWristbandReward wristband;
    SkyboardReward skyboard;
    MarshmallowHopRecovery recovery;
    MarshmallowChallengeGuide courseGuide;
    SecondIslandRoninCamp ridgeCamp;
    DriftingPathShortcut shortcut;
    ZhuBaJieBoss zhuBaJie;
    readonly List<Transform> rows = new List<Transform>(22);
    GameObject panel;
    Text label;
    float nextRefresh, nextDiscovery;

    void Start()
    {
        player = GetComponent<ThirdPersonPlayerController>();
        health = GetComponent<PlayerHealth>();
        var canvas = new GameObject("Second Island objectives", typeof(RectTransform), typeof(Canvas));
        canvas.transform.SetParent(transform, false);
        var screen = canvas.GetComponent<Canvas>(); screen.renderMode = RenderMode.ScreenSpaceOverlay; screen.sortingOrder = 70;
        GameUITheme.Scale(screen);
        panel = canvas;
        var card = GameUITheme.Rect(canvas.transform, "Objective card", new Vector2(0, 1), new Vector2(485, 150));
        card.pivot = new Vector2(0, 1); card.anchoredPosition = new Vector2(28, -165);
        GameUITheme.Image(card, new Color(.035f, .04f, .055f, .86f));
        label = GameUITheme.Label(card, "Current objective", 20, new Vector2(18, -75), new Vector2(449, 140), TextAnchor.MiddleLeft);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
        label.rectTransform.pivot = new Vector2(0, .5f);
        panel.SetActive(false);
        Discover();
    }

    void Discover()
    {
        nextDiscovery = Time.unscaledTime + 1f;
        if (!wristband) wristband = FindFirstObjectByType<GravityWristbandReward>();
        if (!ridgeCamp) ridgeCamp = FindFirstObjectByType<SecondIslandRoninCamp>();
        if (!courseGuide) courseGuide = FindFirstObjectByType<MarshmallowChallengeGuide>();
        if (!shortcut) shortcut = FindFirstObjectByType<DriftingPathShortcut>();
        if (!skyboard)
        {
            skyboard = FindFirstObjectByType<SkyboardReward>();
            if (skyboard)
            {
                recovery = skyboard.GetComponent<MarshmallowHopRecovery>();
                rows.Clear();
                foreach (Transform child in skyboard.transform)
                    if (child.name.StartsWith("Row ")) rows.Add(child);
            }
        }
        if (!zhuBaJie) zhuBaJie = FindFirstObjectByType<ZhuBaJieBoss>();
    }

    void Update()
    {
        if (!player || !label) return;
        if (Time.unscaledTime >= nextDiscovery) Discover();
        if (!FinishedCourse && recovery && rows.Count > 0 && recovery.CurrentRow >= rows.Count)
        { FinishedCourse = true; GameSaveManager.RequestAutosave(); }
        bool inHollow = MarshmallowHollowEntrance.Contains(player);
        if (inHollow) CurrentTarget = null;
        bool show = player.enabled && !player.ExternalControlLock && !SkyboardRingRally.IsRacing(player) && !inHollow && (!health || health.currentHealth > 0) && !GameUITheme.ModalOpen &&
            !SecondIslandAwakening.BlocksInputFor(player);
        if (panel.activeSelf != show)
        {
            panel.SetActive(show);
            if (show) nextRefresh = 0;
        }
        if (!show || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .2f;
        if (!wristband || wristband.Total <= 0)
        {
            label.text = "MARSHMALLOW ISLAND\nPreparing island objectives...";
            return;
        }

        Transform target = null;
        string description;
        if (!wristband.Collected)
        {
            Current = Objective.Wristband;
            description = "1 / 5  ·  THE GRAVITY WRISTBAND\nWalk up to the wristband beside the awakening spot.";
            target = wristband.transform;
        }
        else if (!wristband.MainCampsComplete && !RidgeCleared)
        {
            Current = Objective.Marshmallows;
            int camp = wristband.NextMainCamp;
            description = "2 / 5  ·  FAIRY'S REQUEST\nClear " + wristband.CampName(camp) + "  ·  " + wristband.MainCampsCleared + " / " + wristband.MainCampCount + " camps";
            target = wristband.CampMapAnchor(camp);
        }
        else if (!RidgeCleared)
        {
            Current = Objective.ZhuBaJie;
            bool visitCamp = ridgeCamp && !ridgeCamp.IsCheckpoint && (!zhuBaJie || !zhuBaJie.FightActive);
            description = visitCamp
                ? "3 / 5  ·  ZHU BAJIE\nVisit Ridge Rest Camp\nSet a checkpoint and try your wristband."
                : "3 / 5  ·  ZHU BAJIE\nDefeat Zhu Bajie\n" + GameControls.Prompt(GameControls.Command.Gravity) + "  Switch gravity";
            target = visitCamp ? ridgeCamp.transform : zhuBaJie ? zhuBaJie.transform : null;
        }
        else if (!FinishedCourse)
        {
            Current = Objective.Course;
            int row = recovery ? Mathf.Clamp(recovery.CurrentRow, 0, rows.Count) : 0;
            description = row == 0 && courseGuide
                ? "4 / 5  ·  THE DRIFTING PATH\nFind the fairy at the course entrance\nLow gravity helps on the moving platforms."
                : "4 / 5  ·  THE DRIFTING PATH\nReach the final moving marshmallow\nRow: " + row + " / " + rows.Count + "  ·  Low gravity helps";
            target = row == 0 && courseGuide ? courseGuide.transform : NextCourseTarget(row);
            if (row == 0 && shortcut && shortcut.Guides(player))
            {
                description = "4 / 5  ·  THE DRIFTING PATH\nTake the hillside shortcut to the course\nLook for the lantern waypost.";
                target = shortcut.transform;
            }
        }
        else if (!skyboard || !skyboard.Owned || skyboard.CollectedScrolls == 0)
        {
            Current = Objective.Scrolls;
            bool needsBoard = skyboard && !skyboard.Owned;
            description = needsBoard
                ? "5 / 5  ·  A GIFT FOR THE JOURNEY\nCollect the Skyboard at the finish"
                : "5 / 5  ·  A GIFT FOR THE JOURNEY\nCollect the 10 speed scrolls at the finish";
            int platform = needsBoard ? 0 : 1;
            target = rows.Count > 0 && rows[rows.Count - 1].childCount > platform ? rows[rows.Count - 1].GetChild(platform) : null;
        }
        else
        {
            Current = Objective.Complete;
            description = "MARSHMALLOW ISLAND\nAll island objectives complete!\nExplore, or use your Skyboard from Tools.";
        }
        CurrentTarget = target;
        if (target) description += "\n" + DirectionTo(target.position);
        if (label.text != description) label.text = description;
    }

    Transform NextCourseTarget(int currentRow)
    {
        if (rows.Count == 0) return null;
        var row = rows[Mathf.Clamp(currentRow, 0, rows.Count - 1)];
        Transform closest = null; float best = float.PositiveInfinity;
        foreach (Transform candidate in row)
        {
            float distance = (candidate.position - transform.position).sqrMagnitude;
            if (distance < best) { best = distance; closest = candidate; }
        }
        return closest;
    }

    string DirectionTo(Vector3 position)
    {
        Vector3 delta = Vector3.ProjectOnPlane(position - transform.position, Vector3.up);
        var camera = Camera.main;
        Vector3 forward = camera ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : transform.forward;
        Vector3 right = camera ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized : transform.right;
        float ahead = Vector3.Dot(forward, delta), side = Vector3.Dot(right, delta);
        string arrow = Mathf.Abs(side) > Mathf.Abs(ahead) * .45f ? (side > 0 ? "→" : "←") : ahead >= 0 ? "↑" : "↓";
        return arrow + "  " + Mathf.RoundToInt(delta.magnitude) + " m";
    }
}
