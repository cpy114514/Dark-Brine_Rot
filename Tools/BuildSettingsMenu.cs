using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSettingsMenu
{
    const string Root = "Assets/Game/Prefabs/UI/SettingsMenu";
    static readonly Color White = Color.white;
    static readonly Color Black = Color.black;
    static readonly Color Secondary = new Color(1f, 1f, 1f, 0.68f);
    static TMP_FontAsset font;

    public static string Run()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new Exception("TMP font is missing.");
        EnsureFolder("Assets/Game/Prefabs/UI");
        EnsureFolder(Root);
        EnsureFolder(Root + "/Scripts");
        string path = Root + "/PauseSettingsMenu.prefab";

        var rootObject = new GameObject("Pause & Settings Menu", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(CanvasGroup), typeof(PauseSettingsMenu));
        try
        {
            var canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = rootObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var menu = rootObject.GetComponent<PauseSettingsMenu>();
            menu.overlay = rootObject.GetComponent<CanvasGroup>();
            RectTransform root = rootObject.GetComponent<RectTransform>();
            Stretch(root);
            AddImage(root, "Black background", Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.91f), true);

            var home = Rect(root, "Pause home", Vector2.zero, new Vector2(1000, 650));
            menu.homePanel = home.gameObject;
            AddText(home, "Paused title", "PAUSED", new Vector2(0, 215), new Vector2(900, 90),
                64, White, FontStyles.Bold, TextAlignmentOptions.Center);
            menu.resumeButton = BorderedButton(home, "Resume", "RESUME", new Vector2(0, 75),
                new Vector2(650, 72), out _);
            menu.settingsButton = BorderedButton(home, "Settings", "SETTINGS", new Vector2(0, -25),
                new Vector2(650, 72), out _);
            menu.quitButton = BorderedButton(home, "Quit", "QUIT", new Vector2(0, -125),
                new Vector2(650, 72), out var quitLabel);
            menu.quitLabel = quitLabel;

            var settings = Rect(root, "Settings", Vector2.zero, new Vector2(1500, 840));
            menu.settingsPanel = settings.gameObject;
            AddText(settings, "Settings heading", "SETTINGS", new Vector2(-570, 375),
                new Vector2(310, 64), 38, White, FontStyles.Bold);
            menu.sectionTabs = new Button[5];
            menu.sectionTabLabels = new TextMeshProUGUI[5];
            menu.sectionPanels = new GameObject[5];
            string[] categories =
            {
                "GAMEPLAY", "CONTROLS", "AUDIO", "DISPLAY", "GRAPHICS"
            };
            for (int i = 0; i < categories.Length; i++)
                menu.sectionTabs[i] = BorderedButton(settings, categories[i] + " tab", categories[i],
                    new Vector2(-583, 150 - i * 82), new Vector2(300, 54),
                    out menu.sectionTabLabels[i], 17);

            menu.sectionTitle = AddText(settings, "Section title", "GAMEPLAY",
                new Vector2(215, 322), new Vector2(900, 58), 34, White, FontStyles.Bold);
            AddImage(settings, "Divider", new Vector2(-410, 0), new Vector2(2, 760), White);

            var sections = new List<OptionSpec>[]
            {
                new()
                {
                    Choice("difficulty", "CHALLENGE", "Nailong health and chase; Sahur attack power"),
                    Slide("cameraDistance", "CAMERA DISTANCE", "Third-person follow distance", 2.5f, 11f),
                    Slide("fieldOfView", "FIELD OF VIEW", "Camera perspective", 50f, 90f)
                },
                new()
                {
                    Choice("key.Forward", "MOVE FORWARD", "Click to rebind"),
                    Choice("key.Back", "MOVE BACK", "Click to rebind"),
                    Choice("key.Left", "MOVE LEFT", "Click to rebind"),
                    Choice("key.Right", "MOVE RIGHT", "Click to rebind"),
                    Choice("key.Sprint", "SPRINT", "Click to rebind"),
                    Choice("key.Jump", "JUMP", "Click to rebind"),
                    Choice("key.Dodge", "DODGE", "Click to rebind"),
                    Slide("sensitivity", "LOOK SENSITIVITY", "Mouse camera speed", 0.03f, 0.5f),
                    Choice("invertY", "INVERT VERTICAL LOOK", "Reverse vertical mouse look")
                },
                new()
                {
                    Slide("masterVolume", "MASTER VOLUME", "Overall game sound", 0f, 1f)
                },
                new()
                {
                    Choice("resolution", "RESOLUTION", "Screen pixel dimensions"),
                    Choice("fullscreen", "DISPLAY MODE", "Borderless or windowed"),
                    Choice("vsync", "VERTICAL SYNC", "Sync frames with the display"),
                    Choice("frameLimit", "FRAME LIMIT", "V-Sync can override this limit")
                },
                new()
                {
                    Slide("renderScale", "RENDER SCALE", "Internal resolution; lower is faster", 0.6f, 1.2f),
                    Choice("texture", "TEXTURE DETAIL", "Texture mipmap limit"),
                    Choice("anisotropic", "TEXTURE FILTERING", "Sharper textures at oblique angles"),
                    Slide("lodBias", "OBJECT DETAIL", "Distance before lower-detail models appear", 0.5f, 3f),
                    Choice("antialiasing", "ANTI-ALIASING", "Off, FXAA, or SMAA"),
                    Choice("postProcessing", "POST PROCESSING", "Camera image effects"),
                    Choice("bloom", "BLOOM", "Glow around bright parts of the image"),
                    Choice("vignette", "VIGNETTE", "Darkening around the screen edge"),
                    Choice("motionBlur", "MOTION BLUR", "Blur from camera and object movement"),
                    Choice("shadows", "SHADOWS", "Scene light shadows"),
                    Choice("shadowResolution", "SHADOW DETAIL", "Shadow map resolution"),
                    Slide("shadowDistance", "SHADOW DISTANCE", "Maximum real-time shadow range", 15f, 250f)
                }
            };

            for (int i = 0; i < sections.Length; i++)
                menu.sectionPanels[i] = BuildSection(settings, categories[i], sections[i], menu.options);

            menu.backButton = BorderedButton(settings, "Back", "BACK",
                new Vector2(-72, -364), new Vector2(195, 54), out _);
            menu.defaultsButton = BorderedButton(settings, "Defaults", "DEFAULTS",
                new Vector2(165, -364), new Vector2(225, 54), out _);
            menu.applyButton = BorderedButton(settings, "Apply", "APPLY & SAVE",
                new Vector2(432, -364), new Vector2(260, 54), out _);
            menu.footerHint = AddText(root, "Footer", "ESC  RESUME",
                new Vector2(0, -426), new Vector2(1450, 30), 15, White,
                FontStyles.Normal, TextAlignmentOptions.Right);

            PrefabUtility.SaveAsPrefabAsset(rootObject, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rootObject);
        }

        string playerPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        GameObject player = PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);
            var installer = player.GetComponent<SettingsMenuInstaller>();
            if (installer == null) installer = player.AddComponent<SettingsMenuInstaller>();
            installer.menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PauseSettingsMenu>();
            PrefabUtility.SaveAsPrefabAsset(player, playerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
        AssetDatabase.SaveAssets();
        return "Rebuilt monochrome settings menu: " + path;
    }

    sealed class OptionSpec
    {
        public string key;
        public string label;
        public string detail;
        public bool slider;
        public float min;
        public float max;
    }

    static OptionSpec Choice(string key, string label, string detail) =>
        new() { key = key, label = label, detail = detail };
    static OptionSpec Slide(string key, string label, string detail, float min, float max) =>
        new() { key = key, label = label, detail = detail, slider = true, min = min, max = max };
    static GameObject BuildSection(Transform parent, string name, List<OptionSpec> specs,
        List<PauseSettingsMenu.OptionControl> bindings)
    {
        RectTransform section = Rect(parent, name + " panel", new Vector2(205, -1), new Vector2(970, 560));
        var scroll = section.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 26f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform viewport = Rect(section, "Viewport", Vector2.zero, new Vector2(950, 560));
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect(viewport, "Content", Vector2.zero,
            new Vector2(950, Mathf.Max(560, specs.Count * 76 + 20)));
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        scroll.viewport = viewport;
        scroll.content = content;
        if (specs.Count > 7)
            AddText(section, "Scroll hint", "SCROLL FOR MORE", new Vector2(382, -300),
                new Vector2(250, 25), 13, Secondary,
                FontStyles.Normal, TextAlignmentOptions.Right);

        for (int i = 0; i < specs.Count; i++)
        {
            OptionSpec spec = specs[i];
            RectTransform row = Rect(content, spec.label + " row",
                new Vector2(0, -36 - i * 76), new Vector2(920, 70));
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            AddText(row, "Label", spec.label, new Vector2(-210, 12),
                new Vector2(460, 31), 19, White, FontStyles.Bold);
            AddText(row, "Explanation", spec.detail, new Vector2(-210, -16),
                new Vector2(460, 28), 13, Secondary);
            var binding = new PauseSettingsMenu.OptionControl { key = spec.key };
            if (spec.slider)
            {
                binding.slider = MakeSlider(row, spec.key, new Vector2(265, 0), spec.min, spec.max);
                binding.value = AddText(row, "Value", "", new Vector2(427, 0),
                    new Vector2(80, 42), 17, White, FontStyles.Bold, TextAlignmentOptions.Right);
            }
            else
            {
                binding.button = BorderedButton(row, "Control", "", new Vector2(310, 0),
                    new Vector2(245, 46), out binding.value, 17);
                binding.value.alignment = TextAlignmentOptions.Right;
                binding.value.rectTransform.sizeDelta = new Vector2(205, 42);
            }
            bindings.Add(binding);
        }
        return section.gameObject;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 pos, float min, float max)
    {
        RectTransform root = Rect(parent, name, pos, new Vector2(218, 46));
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0.01f);
        hit.raycastTarget = true;
        AddImage(root, "Track", Vector2.zero, new Vector2(212, 3), White);
        RectTransform fillArea = Rect(root, "Fill area", Vector2.zero, new Vector2(205, 5));
        RectTransform fill = AddImage(fillArea, "Fill", Vector2.zero, new Vector2(0, 5), White);
        RectTransform handleArea = Rect(root, "Handle area", Vector2.zero, new Vector2(205, 42));
        RectTransform handle = AddImage(handleArea, "Handle", Vector2.zero, new Vector2(12, 27), White);
        handle.GetComponent<Image>().raycastTarget = true;
        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = min;
        slider.maxValue = max;
        return slider;
    }

    static Button BorderedButton(Transform parent, string name, string caption, Vector2 pos,
        Vector2 size, out TextMeshProUGUI label, float fontSize = 20)
    {
        RectTransform border = AddImage(parent, name + " border", pos, size, White);
        RectTransform inner = AddImage(border, name, Vector2.zero, size - new Vector2(3, 3), Black);
        Image image = inner.GetComponent<Image>();
        image.raycastTarget = true;
        var button = inner.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = White;
        colors.highlightedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        colors.selectedColor = White;
        button.colors = colors;
        label = AddText(inner, "Text", caption, Vector2.zero,
            size - new Vector2(25, 4), fontSize, White,
            FontStyles.Bold, TextAlignmentOptions.Center);
        return button;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static RectTransform AddImage(Transform parent, string name, Vector2 pos, Vector2 size,
        Color color, bool stretch = false)
    {
        RectTransform rect = Rect(parent, name, pos, size);
        if (stretch) Stretch(rect);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static TextMeshProUGUI AddText(Transform parent, string name, string text, Vector2 pos,
        Vector2 size, float fontSize, Color color,
        FontStyles style = FontStyles.Normal,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        RectTransform rect = Rect(parent, name, pos, size);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }
}
