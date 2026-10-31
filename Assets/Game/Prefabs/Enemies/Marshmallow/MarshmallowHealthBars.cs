using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One shared overlay keeps a large number of small enemies inexpensive.
public sealed class MarshmallowHealthBars : MonoBehaviour
{
    sealed class Entry
    {
        public MarshmallowEnemyHealth health;
        public RectTransform rect, fill;
        public Image fillImage;
    }
    static MarshmallowHealthBars instance;
    readonly List<Entry> entries = new List<Entry>();
    Canvas canvas;
    Camera viewer;

    public static void Register(MarshmallowEnemyHealth health)
    {
        if (!Application.isPlaying || !health) return;
        if (!instance)
        {
            var root = new GameObject("Marshmallow Enemy Health Bars", typeof(RectTransform), typeof(Canvas), typeof(MarshmallowHealthBars));
            instance = root.GetComponent<MarshmallowHealthBars>();
        }
        instance.Add(health);
    }
    public static void Unregister(MarshmallowEnemyHealth health)
    {
        if (!instance) return;
        for (int i = instance.entries.Count - 1; i >= 0; i--)
            if (instance.entries[i].health == health)
            {
                if (instance.entries[i].rect) Destroy(instance.entries[i].rect.gameObject);
                instance.entries.RemoveAt(i);
            }
    }
    void Awake()
    {
        canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
    }
    void OnDestroy() { if (instance == this) instance = null; }
    void Add(MarshmallowEnemyHealth health)
    {
        foreach (var item in entries) if (item.health == health) return;
        var root = new GameObject("Health - " + health.name, typeof(RectTransform), typeof(Image));
        var rect = root.GetComponent<RectTransform>(); rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(56, 8);
        var track = root.GetComponent<Image>(); track.color = new Color(.08f, .04f, .08f, .88f); track.raycastTarget = false;
        var fillObject = new GameObject("Health fill", typeof(RectTransform), typeof(Image));
        var fill = fillObject.GetComponent<RectTransform>(); fill.SetParent(rect, false);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(1.5f, 1.5f); fill.offsetMax = new Vector2(-1.5f, -1.5f);
        var fillImage = fillObject.GetComponent<Image>(); fillImage.color = new Color(.45f, .95f, .65f, 1f); fillImage.raycastTarget = false;
        root.SetActive(false);
        entries.Add(new Entry { health = health, rect = rect, fill = fill, fillImage = fillImage });
    }
    void LateUpdate()
    {
        if (!viewer || !viewer.isActiveAndEnabled) viewer = Camera.main;
        bool uiAllowed = viewer && !GameUITheme.ModalOpen;
        foreach (var item in entries)
        {
            if (!item.health || !item.rect) continue;
            var health = item.health;
            float maxDistance = Mathf.Max(1f, health.healthBarVisibleDistance);
            Vector3 anchor = health.transform.position + Vector3.up * health.healthBarHeight;
            Vector3 viewport = uiAllowed ? viewer.WorldToViewportPoint(anchor) : Vector3.zero;
            float distance = uiAllowed ? Vector3.Distance(viewer.transform.position, anchor) : float.PositiveInfinity;
            bool visible = uiAllowed && health.isActiveAndEnabled && health.gameObject.activeInHierarchy && !health.IsDead &&
                distance <= maxDistance && viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1;
            if (item.rect.gameObject.activeSelf != visible) item.rect.gameObject.SetActive(visible);
            if (!visible) continue;
            item.rect.position = viewer.WorldToScreenPoint(anchor);
            float size = Mathf.Lerp(1f, .7f, Mathf.Clamp01(distance / maxDistance));
            item.rect.localScale = Vector3.one * size;
            float ratio = health.Ratio;
            item.fill.anchorMax = new Vector2(ratio, 1f);
            item.fillImage.color = Color.Lerp(new Color(1f, .32f, .32f), new Color(.45f, .95f, .65f), ratio);
        }
    }

    // Used by the editor playthrough to verify real screen-space UI and health.
    public static bool TryGetBar(MarshmallowEnemyHealth health, out bool visible, out float ratio)
    {
        visible = false; ratio = 0f;
        if (!instance) return false;
        foreach (var item in instance.entries)
            if (item.health == health && item.rect && item.fill)
            { visible = item.rect.gameObject.activeSelf; ratio = item.fill.anchorMax.x; return true; }
        return false;
    }
}
