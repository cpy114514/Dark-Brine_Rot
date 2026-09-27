using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildMainMenu
{
    public const string ScenePath = "Assets/Scenes/Main Menu/MainMenu.unity";
    const string SettingsPath = "Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab";
    static TMP_FontAsset font;

    public static string Run()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Main menu scene already exists; refusing to replace it.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("LiberationSans TMP font is missing.");
        var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPath);
        if (settingsPrefab == null) throw new InvalidOperationException("Existing settings prefab is missing.");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Main Menu"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Main Menu");

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var cameraObject = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;

            var canvasObject = new GameObject("Main Menu", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            Stretch(root);

            var background = new GameObject("Black Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root, false);
            Stretch(background.GetComponent<RectTransform>());
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = Color.black;
            backgroundImage.raycastTarget = false;

            Text(root, "Game Title", "DARK BRINE: ROT", 96, -92, 1050, 108, 76, Color.white, true);
            Text(root, "Menu Label", "MAIN MENU", 101, -202, 480, 42, 23, Color.white, false);
            var divider = new GameObject("White Rule", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(root, false);
            TopLeft(divider.GetComponent<RectTransform>(), 100, -258, 440, 2);
            divider.GetComponent<Image>().color = Color.white;

            var menu = canvasObject.GetComponent<MainMenuController>();
            Button newGame = Button(root, "New Game", "NEW GAME", -314, true);
            Button continueGame = Button(root, "Continue", "CONTINUE", -398, false);
            Button settings = Button(root, "Settings", "SETTINGS", -482, true);
            Button quit = Button(root, "Quit", "QUIT", -566, true);
            menu.continueButton = continueGame;
            Text(root, "Save Status", "NO SAVE DATA", 566, -404, 300, 64, 17,
                new Color(1f, 1f, 1f, 0.47f), false);
            Text(root, "Footer", "FIRST ISLAND", 100, -994, 440, 30, 16,
                new Color(1f, 1f, 1f, 0.6f), false);

            UnityEventTools.AddPersistentListener(newGame.onClick, menu.NewGame);
            UnityEventTools.AddPersistentListener(settings.onClick, menu.Settings);
            UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);

            var settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, scene);
            settingsInstance.name = "Settings Overlay";
            var settingsMenu = settingsInstance.GetComponent<PauseSettingsMenu>();
            settingsMenu.mainMenuSettingsOnly = true;
            settingsMenu.overlay.alpha = 0f;
            settingsMenu.overlay.interactable = false;
            settingsMenu.overlay.blocksRaycasts = false;
            settingsMenu.homePanel.SetActive(false);
            settingsMenu.settingsPanel.SetActive(false);
            menu.settingsMenu = settingsMenu;

            new GameObject("Menu Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the main menu scene.");

            var buildScenes = EditorBuildSettings.scenes
                .Where(item => item.path != ScenePath).ToList();
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            return $"Created {ScenePath}; build scene 0 is MainMenu, scene 1 is First Island/Main.";
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
                SceneManager.SetActiveScene(previous);
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    static Button Button(RectTransform root, string name, string label, float y, bool enabled)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        obj.transform.SetParent(root, false);
        TopLeft(obj.GetComponent<RectTransform>(), 100, y, 440, 66);
        var image = obj.GetComponent<Image>();
        image.color = new Color(0.07f, 0.07f, 0.07f, 1f);
        var outline = obj.GetComponent<Outline>();
        outline.effectColor = enabled ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        outline.effectDistance = new Vector2(1f, -1f);
        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        colors.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
        colors.selectedColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        button.colors = colors;
        button.interactable = enabled;
        var text = Text(obj.GetComponent<RectTransform>(), name + " Label", label,
            24, 0, 392, 66, 27, enabled ? Color.white : new Color(1f, 1f, 1f, 0.42f), false);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return button;
    }

    static TextMeshProUGUI Text(RectTransform parent, string name, string value,
        float x, float y, float width, float height, float size, Color color, bool bold)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TopLeft(obj.GetComponent<RectTransform>(), x, y, width, height);
        var label = obj.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = value;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void TopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
