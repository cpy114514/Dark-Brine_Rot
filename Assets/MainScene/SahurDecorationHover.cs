using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class SahurDecorationHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
{
    public Sprite normalSprite;
    public Sprite hoverSprite;
    [Range(0f, 1f)] public float alphaThreshold = 0.1f;

    Image image;

    public void SetSprites(Sprite normal, Sprite hover)
    {
        normalSprite = normal;
        hoverSprite = hover;
        ShowNormal();
    }

    void OnEnable() => ShowNormal();
    void OnDisable() => ShowNormal();
    void OnApplicationFocus(bool focused)
    {
        if (!focused) ShowNormal();
    }

    void ShowNormal()
    {
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.sprite = normalSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image == null) image = GetComponent<Image>();
        if (hoverSprite != null) image.sprite = hoverSprite;
    }

    public void OnPointerExit(PointerEventData eventData) => ShowNormal();

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // Always use the same silhouette so the two drawings' edge differences
        // cannot repeatedly toggle hover when the pointer sits on an outline.
        if (normalSprite == null || !normalSprite.texture.isReadable) return true;
        var rectTransform = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out var localPoint)) return false;
        var rect = rectTransform.rect;
        if (!rect.Contains(localPoint) || rect.width <= 0f || rect.height <= 0f) return false;
        var spriteRect = normalSprite.rect;
        var texture = normalSprite.texture;
        float u = (spriteRect.x + (localPoint.x - rect.xMin) / rect.width * spriteRect.width) / texture.width;
        float v = (spriteRect.y + (localPoint.y - rect.yMin) / rect.height * spriteRect.height) / texture.height;
        return texture.GetPixelBilinear(u, v).a >= alphaThreshold;
    }
}
