using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mavis.VisualStyles
{
    public sealed class VisualStyleUIController : MonoBehaviour
    {
        static VisualStyleUIController instance;
        static int closedFrame = -1;
        public static bool IsOpen => instance && instance.panel && instance.panel.activeSelf;
        public static bool BlocksParentInput => IsOpen || closedFrame == Time.frameCount;
        public VisualStyleComparisonController Comparison { get; private set; }
        public Slider IntensitySlider { get; private set; }
        public IReadOnlyList<Button> StyleButtons => styleButtons;
        readonly List<Button> styleButtons = new List<Button>();
        readonly List<Text> captions = new List<Text>();
        readonly List<Image> borders = new List<Image>();
        GameObject panel, gallery, comparisonView;
        Text intensityText, subtitle;
        Selectable previousSelection;
        Transform owner;
        readonly Color parchment = new Color(.90f, .86f, .76f);

        public static void Open(Transform source = null)
        {
            if (!instance)
            {
                var manager = VisualStyleManager.Instance;
                if (!manager) return;
                instance = manager.gameObject.AddComponent<VisualStyleUIController>(); instance.Build();
            }
            instance.owner = source ? source : instance.transform;
            instance.previousSelection = EventSystem.current && EventSystem.current.currentSelectedGameObject ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() : null;
            instance.panel.SetActive(true); instance.ShowComparison(false); instance.Refresh();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (instance.styleButtons.Count > 0) instance.styleButtons[0].Select();
        }
        public void Close()
        {
            panel.SetActive(false); closedFrame = Time.frameCount;
            if (previousSelection && previousSelection.IsActive()) previousSelection.Select();
        }
        void Update()
        {
            if (!IsOpen) return;
            if (!owner) { Close(); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if (comparisonView.activeSelf) ShowComparison(false); else Close(); closedFrame = Time.frameCount; }
        }
        void OnDestroy() { VisualStyleManager.Changed -= Refresh; if (instance == this) instance = null; }
        static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 pos)
        { var r = GameUITheme.Rect(parent, name, new Vector2(.5f, .5f), size); r.anchoredPosition = pos; return r; }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 area, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var r = Rect(parent, text, area, pos); var label = r.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = text; label.fontSize = size;
            if (Pixel360Rendering.IsActive) label.material = PixelStyleUI.NativeTextMaterial;
            label.color = Color.white; label.alignment = align; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        static Button Button(Transform parent, string title, Vector2 pos, Vector2 size, Action action)
        {
            var r = Rect(parent, title, size, pos); var image = r.gameObject.AddComponent<Image>();
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label(r, title, 22, Vector2.zero, size - new Vector2(20, 0)); GameUITheme.StyleButton(button);
            button.onClick.AddListener(() => action()); return button;
        }
        static RawImage Picture(Transform parent, string name, Texture image, Vector2 size, Vector2 pos)
        { var r = Rect(parent, name, size, pos); var raw = r.gameObject.AddComponent<RawImage>(); raw.texture = image; raw.color = image ? Color.white : GameUITheme.Gray(.08f); raw.raycastTarget = false; return raw; }
        static Slider Slider(Transform parent, string name, Vector2 size, Vector2 pos, bool fullHeight = false)
        {
            var r = Rect(parent, name, size, pos); var hit = r.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var slider = r.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            var area = Rect(r, "Handle area", size, Vector2.zero); Stretch(area);
            var handle = Rect(area, "Divider", new Vector2(fullHeight ? 3 : 8, fullHeight ? size.y : 24), Vector2.zero);
            var image = handle.gameObject.AddComponent<Image>(); image.color = new Color(.93f, .87f, .73f);
            slider.handleRect = handle; slider.targetGraphic = image; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            handle.sizeDelta = new Vector2(fullHeight ? 3 : 8, fullHeight ? 0 : 24-size.y);
            if (!fullHeight) { var track = Rect(r, "Track", new Vector2(size.x, 2), Vector2.zero); GameUITheme.Image(track, GameUITheme.Gray(.35f)); track.SetAsFirstSibling(); }
            return slider;
        }
        void Build()
        {
            var root = new GameObject("Visual Style Gallery", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1400; GameUITheme.Scale(canvas);
            panel = root; Stretch(root.GetComponent<RectTransform>());
            var shade = root.AddComponent<Image>(); shade.color = new Color(.018f, .024f, .033f, .98f);
            var sheet = Rect(root.transform, "Visual style panel", new Vector2(1520, 960), Vector2.zero);
            Label(sheet, "VISUAL STYLE", 38, new Vector2(0, 414), new Vector2(900, 60)).color = parchment;
            subtitle = Label(sheet, "One world. Choose how you see it.", 19, new Vector2(0, 367), new Vector2(900, 32)); subtitle.color = GameUITheme.Secondary;
            Button(sheet, "CLOSE", new Vector2(660, 416), new Vector2(150, 48), Close);
            gallery = Rect(sheet, "Style cards", new Vector2(1440, 700), Vector2.zero).gameObject;
            var presets = VisualStyleManager.Instance.Presets;
            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i]; float x = (i - (presets.Count - 1) * .5f) * 700;
                var card = Rect(gallery.transform, preset.id, new Vector2(664, 546), new Vector2(x, 28));
                var border = card.gameObject.AddComponent<Image>(); border.color = GameUITheme.Gray(.32f); borders.Add(border);
                var hover = card.gameObject.AddComponent<VisualStyleCardHover>();
                hover.entered = () => border.color = VisualStyleManager.Instance.Current == preset ? parchment : GameUITheme.Gray(.58f);
                hover.exited = Refresh;
                var body = Rect(card, "Surface", new Vector2(660, 542), Vector2.zero); GameUITheme.Image(body, GameUITheme.Gray(.045f));
                Picture(body, "Actual scene preview", preset.preview, new Vector2(640, 360), new Vector2(0, 81));
                Label(body, preset.displayName, 28, new Vector2(0, -131), new Vector2(610, 42)).color = parchment;
                Label(body, preset.description, 17, new Vector2(0, -178), new Vector2(590, 54)).color = GameUITheme.Secondary;
                var button = Button(body, "SELECT", new Vector2(0, -235), new Vector2(610, 46), () => VisualStyleManager.Instance.Select(preset.id));
                styleButtons.Add(button); captions.Add(button.GetComponentInChildren<Text>());
            }
            Button(gallery.transform, "COMPARE STYLES", new Vector2(0, -307), new Vector2(340, 54), () => ShowComparison(true));
            intensityText = Label(gallery.transform, "STYLE INTENSITY", 16, new Vector2(-205, -375), new Vector2(320, 32));
            IntensitySlider = Slider(gallery.transform, "Style intensity", new Vector2(350, 34), new Vector2(115, -375));
            IntensitySlider.minValue = .4f; IntensitySlider.maxValue = 1;
            IntensitySlider.onValueChanged.AddListener(value => VisualStyleManager.Instance.SetIntensity(value));
            Label(gallery.transform, "Reference captures use 85% intensity. Pixel Style is available in Graphics.", 16, new Vector2(0, -428), new Vector2(1050, 32)).color = GameUITheme.Secondary;
            BuildComparison(sheet);
            VisualStyleManager.Changed += Refresh; panel.SetActive(false);
        }
        void BuildComparison(RectTransform sheet)
        {
            comparisonView = Rect(sheet, "Before and after", new Vector2(1440, 800), new Vector2(0, -25)).gameObject;
            var area = Rect(comparisonView.transform, "Matching scene captures", new Vector2(1248, 702), new Vector2(0, 5));
            area.gameObject.AddComponent<RectMask2D>();
            var manager = VisualStyleManager.Instance;
            VisualStylePreset realistic = null, dark = null;
            foreach (var p in manager.Presets) { if (p.id == "realistic") realistic = p; if (p.id == "dark-fairytale") dark = p; }
            var darkImage = Picture(area, "Dark Fairytale", dark ? dark.preview : null, area.sizeDelta, Vector2.zero); Stretch(darkImage.rectTransform);
            var mask = Rect(area, "Realistic reveal", area.sizeDelta, Vector2.zero); Stretch(mask); mask.gameObject.AddComponent<RectMask2D>();
            var left = Picture(mask, "Realistic", realistic ? realistic.preview : null, area.sizeDelta, Vector2.zero);
            var divider = Slider(area, "Comparison divider", area.sizeDelta, Vector2.zero, true);
            Label(divider.handleRect, "||", 21, Vector2.zero, new Vector2(40, 54)).gameObject.AddComponent<Outline>();
            Comparison = comparisonView.AddComponent<VisualStyleComparisonController>();
            Comparison.leftMask = mask; Comparison.leftImage = left.rectTransform; Comparison.imageArea = area; Comparison.divider = divider;
            divider.SetValueWithoutNotify(.5f); Comparison.Initialize();
            Label(comparisonView.transform, "REALISTIC", 18, new Vector2(-480, 378), new Vector2(300, 28));
            Label(comparisonView.transform, "DARK FAIRYTALE", 18, new Vector2(480, 378), new Vector2(300, 28));
            Button(comparisonView.transform, "BACK TO STYLES", new Vector2(0, -390), new Vector2(330, 48), () => ShowComparison(false));
            Label(comparisonView.transform, "Drag the divider, or select it and use Left / Right.", 16, new Vector2(0, -435), new Vector2(900, 28)).color = GameUITheme.Secondary;
            comparisonView.SetActive(false);
        }
        public void ShowComparison(bool show)
        {
            gallery.SetActive(!show); comparisonView.SetActive(show); subtitle.gameObject.SetActive(!show);
            if (show && Comparison) Comparison.divider.Select();
            else if (styleButtons.Count > 0 && IsOpen) styleButtons[0].Select();
        }
        void Refresh()
        {
            if (!panel) return;
            var manager = VisualStyleManager.Instance;
            for (int i = 0; i < styleButtons.Count; i++)
            {
                bool selected = manager.Presets[i] == manager.Current;
                captions[i].text = selected ? "SELECTED" : "SELECT STYLE";
                borders[i].color = selected ? parchment : GameUITheme.Gray(.24f);
                GameUITheme.StyleButton(styleButtons[i], selected);
            }
            IntensitySlider.SetValueWithoutNotify(manager.Intensity);
            IntensitySlider.interactable = manager.Current && manager.Current.illustrated;
            intensityText.text = "STYLE INTENSITY  " + Mathf.RoundToInt(manager.Intensity * 100) + "%";
        }
        public static void InstallMainMenu(MainMenuController menu)
        {
            if (menu.transform.Find("Visual Style")) return;
            var settings = menu.transform.Find("Settings"); if (!settings) return;
            var entry = Instantiate(settings.gameObject, menu.transform, false); entry.name = "Visual Style";
            foreach (var label in entry.GetComponentsInChildren<TMP_Text>(true)) label.text = "VISUAL STYLE";
            var button = entry.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(() => Open(menu.transform));
            var navigation = button.navigation; navigation.mode = Navigation.Mode.Automatic; button.navigation = navigation;
            foreach (RectTransform child in menu.transform)
            {
                if (child == entry.transform) continue;
                if (child.name == "Settings" || child.name == "Quit" || child.name == "Save Status" || child.name == "Footer" || child.name == "Hairline" && child.anchoredPosition.y < -700)
                    child.anchoredPosition += Vector2.down * 84;
            }
        }
        public static void InstallGraphicsRow(PauseSettingsMenu menu)
        {
            if (menu.options.Exists(o => o.key == "visualStyle")) return;
            var template = menu.options.Find(o => o.key == "antialiasing"); if (template?.button == null) return;
            var original = template.button.transform.parent.parent as RectTransform; var content = original.parent as RectTransform;
            var row = Instantiate(original.gameObject, content, false).GetComponent<RectTransform>(); row.name = "Visual Style row";
            foreach (RectTransform child in content) if (child != row && child.GetSiblingIndex() >= 1) child.anchoredPosition += Vector2.down * 76;
            row.SetSiblingIndex(1); row.anchoredPosition = new Vector2(0, -112); content.sizeDelta += Vector2.up * 76;
            row.Find("Label").GetComponent<TMP_Text>().text = "VISUAL STYLE";
            row.Find("Explanation").GetComponent<TMP_Text>().text = "Choose a look or compare matching in-game previews";
            var button = row.GetComponentInChildren<Button>(true); button.onClick = new Button.ButtonClickedEvent();
            menu.options.Add(new PauseSettingsMenu.OptionControl { key = "visualStyle", button = button, value = button.GetComponentInChildren<TextMeshProUGUI>(true) });
        }
    }
}
