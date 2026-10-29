using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Mavis.VisualStyles
{
    /// <summary>Aligned static captures of any two presets. No additional scene camera.</summary>
    public sealed class VisualStyleComparisonController : MonoBehaviour
    {
        public RectTransform leftMask, leftImage, imageArea;
        public Slider divider;
        public Dropdown leftStyle, rightStyle;
        public RawImage leftPicture, rightPicture;
        IReadOnlyList<VisualStylePreset> presets;
        public float Split { get => divider.value; set => divider.value = Mathf.Clamp01(value); }
        public string LeftId => presets != null && leftStyle ? presets[leftStyle.value].id : "";
        public string RightId => presets != null && rightStyle ? presets[rightStyle.value].id : "";
        void OnEnable() { if (divider && leftStyle) Initialize(); }
        public void Initialize()
        {
            presets = VisualStyleManager.Instance.Presets;
            divider.onValueChanged.RemoveListener(Refresh); divider.onValueChanged.AddListener(Refresh);
            leftStyle.onValueChanged.RemoveListener(LeftChanged); leftStyle.onValueChanged.AddListener(LeftChanged);
            rightStyle.onValueChanged.RemoveListener(RightChanged); rightStyle.onValueChanged.AddListener(RightChanged);
            var names = new List<string>();
            foreach (var preset in presets) names.Add(preset.displayName);
            int left = leftStyle.value, right = rightStyle.value;
            leftStyle.ClearOptions(); rightStyle.ClearOptions();
            leftStyle.AddOptions(names); rightStyle.AddOptions(names);
            leftStyle.SetValueWithoutNotify(Mathf.Clamp(left, 0, presets.Count - 1));
            rightStyle.SetValueWithoutNotify(Mathf.Clamp(right, 0, presets.Count - 1));
            LeftChanged(leftStyle.value); RightChanged(rightStyle.value); Refresh(divider.value);
        }
        public bool SetStyles(string left, string right)
        {
            int a = -1, b = -1;
            for (int i = 0; i < presets.Count; i++) { if (presets[i].id == left) a = i; if (presets[i].id == right) b = i; }
            if (a < 0 || b < 0) return false;
            leftStyle.SetValueWithoutNotify(a); rightStyle.SetValueWithoutNotify(b);
            LeftChanged(a); RightChanged(b); return true;
        }
        void LeftChanged(int index) { if (leftPicture) leftPicture.texture = presets[index].preview; }
        void RightChanged(int index) { if (rightPicture) rightPicture.texture = presets[index].preview; }
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
