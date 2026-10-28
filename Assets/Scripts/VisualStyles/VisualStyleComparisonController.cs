using UnityEngine;
using UnityEngine.UI;

namespace Mavis.VisualStyles
{
    /// <summary>Two aligned static captures, one mask and a navigable UI slider. No preview camera.</summary>
    public sealed class VisualStyleComparisonController : MonoBehaviour
    {
        public RectTransform leftMask;
        public RectTransform leftImage;
        public RectTransform imageArea;
        public Slider divider;
        public float Split { get => divider.value; set => divider.value = Mathf.Clamp01(value); }
        public void Initialize() { divider.onValueChanged.AddListener(Refresh); Refresh(divider.value); }
        void OnRectTransformDimensionsChange() { if (divider && leftMask) Refresh(divider.value); }
        void Refresh(float value)
        {
            leftMask.anchorMax = new Vector2(value, 1);
            leftMask.offsetMin = leftMask.offsetMax = Vector2.zero;
            leftImage.anchorMin = new Vector2(0, 0); leftImage.anchorMax = new Vector2(0, 1);
            leftImage.pivot = new Vector2(0, .5f); leftImage.anchoredPosition = Vector2.zero;
            leftImage.sizeDelta = new Vector2(imageArea.rect.width, 0);
        }
    }
}
