using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene-owned cinematic framing; never changes the camera's projection or aspect.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(900)]
public sealed class CinematicLetterbox : MonoBehaviour
{
    public const float Aspect = 2.39f;
    const float TransitionSeconds = .3f;
    static CinematicLetterbox instance;
    readonly List<MonoBehaviour> owners = new List<MonoBehaviour>(3);
    RectTransform top, bottom;
    Canvas canvas;
    float amount;

    public static void Set(MonoBehaviour owner, bool active)
    {
        if (owner == null) return;
        if (instance == null)
        {
            if (!active) return;
            var root = new GameObject("Cinematic letterbox", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            instance = root.AddComponent<CinematicLetterbox>();
            instance.Build();
        }
        if (active)
        {
            if (!instance.owners.Contains(owner)) instance.owners.Add(owner);
        }
        else instance.owners.Remove(owner);
    }

    // Reserve the final safe frame immediately on entry, and until the bars finish leaving.
    public static float VisibleHeight(float cameraAspect)
    {
        float blend = instance == null ? 0 : Mathf.Max(instance.amount, instance.owners.Count > 0 ? 1 : 0);
        return Mathf.Lerp(1, Mathf.Min(1, cameraAspect / Aspect), Mathf.SmoothStep(0, 1, blend));
    }

    void Build()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Below narrative blackouts/subtitles and pause menus, above the game HUD.
        canvas.sortingOrder = 450;
        top = Bar("Top"); bottom = Bar("Bottom");
        Apply(0, Screen.width, Screen.height);
    }

    RectTransform Bar(string label)
    {
        var bar = new GameObject(label, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(transform, false);
        var image = bar.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
        return (RectTransform)bar.transform;
    }

    void LateUpdate()
    {
        for (int i = owners.Count - 1; i >= 0; i--)
            if (owners[i] == null || !owners[i].isActiveAndEnabled) owners.RemoveAt(i);
        float delta = PauseSettingsMenu.IsOpen ? 0 : Time.deltaTime;
        amount = Mathf.MoveTowards(amount, owners.Count > 0 ? 1 : 0, delta / TransitionSeconds);
        Apply(amount, Screen.width, Screen.height);
    }

    void Apply(float blend, int width, int height)
    {
        float bar = .5f * (1 - Mathf.Min(1, width / (Mathf.Max(1, height) * Aspect))) * Mathf.SmoothStep(0, 1, blend);
        bottom.anchorMin = Vector2.zero; bottom.anchorMax = new Vector2(1, bar);
        top.anchorMin = new Vector2(0, 1 - bar); top.anchorMax = Vector2.one;
        top.offsetMin = top.offsetMax = bottom.offsetMin = bottom.offsetMax = Vector2.zero;
        canvas.enabled = bar > .00001f;
    }

    void OnDestroy() { if (instance == this) instance = null; }
}
