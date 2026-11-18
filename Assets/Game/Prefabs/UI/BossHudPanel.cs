using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One consistent, monochrome encounter panel, shared by the island bosses.</summary>
public sealed class BossHudPanel
{
    static readonly List<BossHudPanel> panels = new List<BossHudPanel>();
    readonly GameObject root;
    readonly Transform owner;
    static ThirdPersonPlayerController player;
    static float nextPlayerLookup;
    readonly CanvasGroup group;
    readonly Text title, numbers;
    readonly Image health, trail;
    bool requested;
    float distance;
    float delayed = 1f, damageHold;
    float previousRatio = 1f;
    string previousTitle, previousNumbers;
    int previousHp=int.MinValue, previousMaxHp=int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        panels.Clear(); player = null; nextPlayerLookup = 0;
    }

    public float DisplayedFraction => health.fillAmount;
    public bool IsVisible => group.alpha > .01f;

    // Use the same encounter requests that select the current enemy's health bar.
    // Hidden, disabled and destroyed owners must not leave the exploration HUD folded.
    public static bool HasActiveEncounter
    {
        get
        {
            foreach (var panel in panels)
                if (panel.requested && panel.root && panel.root.activeInHierarchy)
                    return true;
            return false;
        }
    }

    public BossHudPanel(Transform owner, string name)
    {
        this.owner = owner;
        root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(owner, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 160;
        GameUITheme.Scale(canvas);
        group = root.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
        var panel = GameUITheme.Rect(root.transform, "Encounter", new Vector2(.5f, 1), new Vector2(720, 64));
        panel.pivot = new Vector2(.5f, 1); panel.anchoredPosition = new Vector2(0, -22);
        GameUITheme.Image(panel, GameUITheme.Surface);
        title = GameUITheme.Label(panel, "Boss name", 24, new Vector2(-342, -23), new Vector2(545, 30), TextAnchor.MiddleLeft);
        numbers = GameUITheme.Label(panel, "Health value", 16, new Vector2(342, -23), new Vector2(132, 26), TextAnchor.MiddleRight);
        numbers.color = GameUITheme.Secondary;
        var track = GameUITheme.Rect(panel, "Health track", new Vector2(.5f,1), new Vector2(684, 7));
        track.anchoredPosition = new Vector2(0, -46); GameUITheme.Image(track, GameUITheme.Track);
        trail = GameUITheme.Fill(track, "Recent damage", GameUITheme.Muted);
        health = GameUITheme.Fill(track, "Health", GameUITheme.Foreground);
        for (int i = 1; i <= 3; i++)
            GameUITheme.Rule(track, Vector2.zero, new Vector2(2, 7), GameUITheme.Surface, new Vector2(i * .25f, .5f));
        panels.Add(this);
    }

    public void Refresh(bool visible, float cameraDistance, float ratio, float hp, float maxHp,
        string bossTitle, float dt)
    {
        requested = visible && !GameUITheme.ModalOpen;
        distance = cameraDistance;
        if (!player && Time.unscaledTime >= nextPlayerLookup)
        {
            nextPlayerLookup = Time.unscaledTime + 1f;
            player = Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        }
        // Select by the player's ground distance, independent of camera offset or aircraft altitude.
        if (player)
            foreach (var panel in panels)
                if (panel.owner)
                {
                    Vector3 offset = panel.owner.position - player.transform.position;
                    panel.distance = offset.x * offset.x + offset.z * offset.z;
                }
        BossHudPanel closest = null;
        foreach (var panel in panels)
            if (panel.root && panel.root.activeInHierarchy && panel.requested && (closest == null || panel.distance < closest.distance)) closest = panel;
        foreach (var panel in panels)
            if (panel != closest && panel.group) panel.group.alpha = 0;
        group.alpha = requested && closest == this ? Mathf.MoveTowards(group.alpha, 1, Time.unscaledDeltaTime * 6f) : 0;
        ratio = Mathf.Clamp01(ratio);
        if (ratio < previousRatio) damageHold = .28f;
        if (ratio >= delayed) delayed = ratio;
        else if (damageHold > 0) damageHold -= Mathf.Max(0, dt);
        else delayed = Mathf.MoveTowards(delayed, ratio, Mathf.Max(0, dt) * .48f);
        previousRatio = ratio; health.fillAmount = ratio; trail.fillAmount = delayed;
        Set(title, ref previousTitle, bossTitle);
        int shownHp=Mathf.CeilToInt(hp),shownMaxHp=Mathf.CeilToInt(maxHp);
        if(shownHp!=previousHp || shownMaxHp!=previousMaxHp)
        {previousHp=shownHp;previousMaxHp=shownMaxHp;Set(numbers,ref previousNumbers,shownHp+" / "+shownMaxHp);}
    }

    static void Set(Text label, ref string previous, string value)
    { if (previous == value) return; previous = value; label.text = value; }

    public void Hide() { requested = false; if (group) group.alpha = 0; }
    public void Dispose()
    {
        panels.Remove(this);
        if (!root) return;
        if (Application.isPlaying) Object.Destroy(root); else Object.DestroyImmediate(root);
    }
}
