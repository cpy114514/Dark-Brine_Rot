using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A small underline communicates pointer and keyboard focus without moving the hit area.</summary>
[DisallowMultipleComponent]
public sealed class MinimalButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    Image line;
    Button button;
    bool hovered, selected;
    float amount;
    void Awake()
    {
        button = GetComponent<Button>();
        var rect = GameUITheme.Rect(transform, "Focus underline", new Vector2(0, 0), new Vector2(0, 1));
        rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(0, 0);
        line = GameUITheme.Image(rect, GameUITheme.Foreground);
    }
    void Update()
    {
        float target = button && button.interactable && (hovered || selected) ? 1f : 0f;
        if (Mathf.Approximately(amount, target)) return;
        amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime * 8f);
        line.rectTransform.sizeDelta = new Vector2(((RectTransform)transform).rect.width * amount, 1f);
    }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;
    void OnDisable() { hovered = selected = false; amount = 0; if (line) line.rectTransform.sizeDelta = new Vector2(0, 1); }
}
