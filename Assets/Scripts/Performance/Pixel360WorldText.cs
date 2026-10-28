using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Keep existing floating UI captions readable above a pixel-rendered world.</summary>
    [RequireComponent(typeof(Text)), DefaultExecutionOrder(31000)]
    public sealed class Pixel360WorldText : MonoBehaviour
    {
        Text source, overlay;
        Canvas worldCanvas;
        CanvasGroup[] groups;
        bool substituted, previousEnabled, occluded;
        float nextOcclusion;
        readonly Vector3[] corners = new Vector3[4];
        readonly RaycastHit[] hits = new RaycastHit[8];
        void Awake()
        {
            source = GetComponent<Text>(); worldCanvas = GetComponentInParent<Canvas>();
            groups = GetComponentsInParent<CanvasGroup>(true);
        }
        void LateUpdate()
        {
            if (!Pixel360Rendering.IsActive || !worldCanvas || worldCanvas.renderMode != RenderMode.WorldSpace)
            { Restore(); return; }
            if (!substituted) { previousEnabled = source.enabled; source.enabled = false; substituted = true; }
            if (!overlay)
            {
                var go = new GameObject("Native caption - " + name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(Pixel360Rendering.OverlayRoot, false);
                overlay = go.GetComponent<Text>(); overlay.raycastTarget = false; overlay.material = PixelStyleUI.NativeTextMaterial;
                var outline = source.GetComponent<Outline>();
                if (outline) { var copy = go.AddComponent<Outline>(); copy.effectColor = outline.effectColor; copy.effectDistance = outline.effectDistance; }
            }
            var camera = worldCanvas.worldCamera ? worldCanvas.worldCamera : Camera.main;
            if (!camera || !previousEnabled) { overlay.enabled = false; return; }
            source.rectTransform.GetWorldCorners(corners);
            Vector3 a = camera.WorldToScreenPoint(corners[0]), b = camera.WorldToScreenPoint(corners[1]), c = camera.WorldToScreenPoint(corners[3]);
            float alpha = source.color.a;
            foreach (var group in groups) if (group) alpha *= group.alpha;
            if (Time.unscaledTime >= nextOcclusion)
            {
                nextOcclusion = Time.unscaledTime + .15f; occluded = false;
                Vector3 anchor = (corners[0] + corners[2]) * .5f, ray = anchor - camera.transform.position;
                int count = Physics.RaycastNonAlloc(camera.transform.position, ray.normalized, hits,
                    Mathf.Max(0, ray.magnitude - .15f), camera.cullingMask & Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                    if (!worldCanvas.transform.parent || !hits[i].transform.IsChildOf(worldCanvas.transform.parent)) { occluded = true; break; }
            }
            overlay.enabled = a.z > camera.nearClipPlane && alpha > .001f && !occluded;
            var rect = overlay.rectTransform; Vector2 size = source.rectTransform.rect.size;
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = (Vector2)(b + c) * .5f; rect.sizeDelta = size;
            rect.localScale = new Vector3(Vector2.Distance(a, c) / Mathf.Max(.001f, size.x), Vector2.Distance(a, b) / Mathf.Max(.001f, size.y), 1);
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(c.y - a.y, c.x - a.x) * Mathf.Rad2Deg);
            overlay.font = source.font; overlay.fontSize = source.fontSize; overlay.fontStyle = source.fontStyle;
            overlay.alignment = source.alignment; overlay.supportRichText = source.supportRichText;
            overlay.horizontalOverflow = source.horizontalOverflow; overlay.verticalOverflow = source.verticalOverflow;
            overlay.lineSpacing = source.lineSpacing; overlay.text = source.text;
            var color = source.color; color.a = alpha; overlay.color = color;
        }
        void Restore()
        {
            if (substituted) { source.enabled = previousEnabled; substituted = false; }
            if (overlay) { Destroy(overlay.gameObject); overlay = null; }
        }
        void OnDisable() { Restore(); }
    }
}
