using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One consistent, monochrome encounter panel, shared by the island bosses.</summary>
public sealed class BossHudPanel
{
    static readonly List<BossHudPanel> panels = new List<BossHudPanel>();
    readonly GameObject root;
    readonly CanvasGroup group;
    readonly Text title, numbers;
    readonly Image health, trail;
    bool requested;
    float distance;
    float delayed = 1f, damageHold;
    float previousRatio = 1f;
    string previousTitle, previousNumbers;
    int previousHp=int.MinValue, previousMaxHp=int.MinValue;

    public float DisplayedFraction => health.fillAmount;
    public bool IsVisible => group.alpha > .01f;

    public BossHudPanel(Transform owner, string name)
    {
        root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(owner, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 160;
        GameUITheme.Scale(canvas);
        group = root.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
        var panel = GameUITheme.Rect(root.transform, "Encounter", new Vector2(.5f, 1), new Vector2(820, 86));
        panel.pivot = new Vector2(.5f, 1); panel.anchoredPosition = new Vector2(0, -30);
        GameUITheme.Image(panel, GameUITheme.Surface);
        GameUITheme.Rule(panel, new Vector2(0, -1), new Vector2(820, 1), GameUITheme.Muted, new Vector2(.5f, 1));
        title = GameUITheme.Label(panel, "Boss name", 26, new Vector2(-386, -27), new Vector2(580, 34), TextAnchor.MiddleLeft);
        numbers = GameUITheme.Label(panel, "Health value", 16, new Vector2(386, -27), new Vector2(170, 28), TextAnchor.MiddleRight);
        var track = GameUITheme.Rect(panel, "Health track", new Vector2(.5f,1), new Vector2(772, 8));
        track.anchoredPosition = new Vector2(0, -60); GameUITheme.Image(track, GameUITheme.Track);
        trail = GameUITheme.Fill(track, "Recent damage", GameUITheme.Muted);
        health = GameUITheme.Fill(track, "Health", GameUITheme.Foreground);
        panels.Add(this);
    }

    public void Refresh(bool visible, float cameraDistance, float ratio, float hp, float maxHp,
        string bossTitle, float dt)
    {
        requested = visible && !GameUITheme.ModalOpen;
        distance = cameraDistance;
        BossHudPanel closest = null;
        foreach (var panel in panels)
            if (panel.root && panel.requested && (closest == null || panel.distance < closest.distance)) closest = panel;
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
