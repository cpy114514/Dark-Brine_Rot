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
    [Tooltip("Optional display-only adjustment for the colored drawing; original PNGs stay untouched.")]
    public Material hoverMaterial;
    [Tooltip("Keep the original silhouette when a material aligns the colored drawing in UV space.")]
    public bool alignHoverInMaterial;
    [Range(0f, 1f)] public float alphaThreshold = 0.1f;

    Image image;
    Material normalMaterial;
    bool materialCaptured;

    void EnsureImage()
    {
        if (image == null) image = GetComponent<Image>();
        if (!materialCaptured && image != null)
        {
            normalMaterial = image.material == image.defaultMaterial ? null : image.material;
            materialCaptured = true;
        }
    }

    public void SetSprites(Sprite normal, Sprite hover)
    {
        normalSprite = normal;
        hoverSprite = hover;
        ShowNormal();
    }

    public void SetNormalMaterial(Material material)
    {
        EnsureImage();
        normalMaterial = material;
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
        EnsureImage();
        if (image != null)
        {
            image.sprite = normalSprite;
            image.material = normalMaterial;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureImage();
        if (hoverSprite != null)
        {
            image.sprite = alignHoverInMaterial && hoverMaterial != null ? normalSprite : hoverSprite;
            image.material = hoverMaterial != null ? hoverMaterial : normalMaterial;
        }
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
        EnsureImage();
        if (image != null && image.preserveAspect && normalSprite.rect.height > 0f)
        {
            // Match Image's aspect-preserving drawing area, including pivot alignment.
            // This also excludes the transparent letterbox when the two drawings differ in size.
            float aspect = normalSprite.rect.width / normalSprite.rect.height;
            Vector2 previousSize = rect.size;
            if (aspect > rect.width / Mathf.Max(.0001f, rect.height)) rect.height = rect.width / aspect;
            else rect.width = rect.height * aspect;
            rect.x += (previousSize.x - rect.width) * rectTransform.pivot.x;
            rect.y += (previousSize.y - rect.height) * rectTransform.pivot.y;
        }
        if (!rect.Contains(localPoint) || rect.width <= 0f || rect.height <= 0f) return false;
        var spriteRect = normalSprite.rect;
        var texture = normalSprite.texture;
        float u = (spriteRect.x + (localPoint.x - rect.xMin) / rect.width * spriteRect.width) / texture.width;
        float v = (spriteRect.y + (localPoint.y - rect.yMin) / rect.height * spriteRect.height) / texture.height;
        return texture.GetPixelBilinear(u, v).a >= alphaThreshold;
    }
}
