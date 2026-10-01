using System;
using System.Collections.Generic;
using Mavis;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>A plain black-and-white pause/settings menu adapted to this game's systems.</summary>
public sealed class PauseSettingsMenu : MonoBehaviour
{
    const string Prefix = "DarkBrine.Settings.";
    static readonly int[] FrameLimits = { 30, 60, 120, 144, -1 };
    static readonly int[] ShadowResolutions = { 1024, 2048, 4096 };
    static readonly string[] SectionNames =
    {
        "GAMEPLAY", "CONTROLS", "AUDIO", "DISPLAY", "GRAPHICS"
    };

    [Serializable]
    public sealed class OptionControl
    {
        public string key;
        public Button button;
        public Slider slider;
        public TextMeshProUGUI value;
    }

    public static bool IsOpen { get; private set; }

    [Header("Screen")]
    [Tooltip("Use this instance only as the settings overlay on the title screen.")]
    public bool mainMenuSettingsOnly;
    public CanvasGroup overlay;
    public GameObject homePanel;
    public GameObject settingsPanel;
    public TextMeshProUGUI sectionTitle;
    public TextMeshProUGUI footerHint;
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;
    public TextMeshProUGUI quitLabel;
    public Button backButton;
    public Button applyButton;
    public Button defaultsButton;
    public Button[] sectionTabs;
    public TextMeshProUGUI[] sectionTabLabels;
    public GameObject[] sectionPanels;
    public List<OptionControl> options = new();

    struct Settings
    {
        public float masterVolume;
        public float sensitivity;
        public float fieldOfView;
        public float cameraDistance;
        public float renderScale;
        public float shadowDistance;
        public float lodBias;
        public bool invertY;
        public bool fullscreen;
        public bool vsync;
        public bool postProcessing;
        public bool shadows;
        public bool bloom;
        public bool vignette;
        public bool motionBlur;
        public int resolutionIndex;
        public int textureMipmapLimit;
        public int frameLimitIndex;
        public int antialiasing;
        public int shadowResolutionIndex;
        public int anisotropicFiltering;
        public int difficulty;
    }

    readonly List<Resolution> resolutions = new();
    readonly Dictionary<Light, LightShadows> originalLightShadows = new();
    readonly Dictionary<string, OptionControl> controlsByKey = new();
    Settings saved;
    Settings draft;
    Settings factoryDefaults;
    Key[] savedKeys;
    Key[] draftKeys;
    ThirdPersonPlayerController player;
    NailongHealth bossHealth;
    NailongAI bossAI;
    SahurAttack playerAttack;
    float baseBossHealth;
    float baseBossSpeed;
    float basePlayerDamage;
    int section;
    bool showingSettings;
    bool confirmQuit;
    string pendingKey;
    float previousTimeScale = 1f;
    RenderPipelineAsset originalPipelineAsset;
    UniversalRenderPipelineAsset runtimePipelineAsset;

    void Awake()
    {
        LocalizedGameText.BindTree(transform);
        player = FindFirstObjectByType<ThirdPersonPlayerController>();
        foreach (Resolution resolution in Screen.resolutions)
            if (!resolutions.Exists(r => r.width == resolution.width && r.height == resolution.height))
                resolutions.Add(resolution);
        if (!resolutions.Exists(r => r.width == Screen.width && r.height == Screen.height))
            resolutions.Add(new Resolution { width = Screen.width, height = Screen.height });

        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            originalLightShadows[light] = light.shadows;
        foreach (OptionControl option in options)
        {
            if (option.key == "shadowDistance" && option.slider != null)
                option.slider.maxValue = 800f;
            controlsByKey[option.key] = option;
        }

        CacheDifficultyTargets();
        ConfigureScrollHitAreas();
        factoryDefaults = CaptureDefaults();
        InitializeRuntimePipeline();
        saved = ReadSettings();
        draft = saved;
        savedKeys = ReadKeys();
        draftKeys = (Key[])savedKeys.Clone();
        Bind();
        GameLocalization.Changed += Refresh;
        ApplySettings(saved);
        Refresh();
        HideImmediate();
    }

    void Bind()
    {
        foreach(var button in new[]{resumeButton,settingsButton,quitButton,backButton,applyButton,defaultsButton})
            GameUITheme.StyleButton(button);
        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(ShowSettings);
        quitButton.onClick.AddListener(QuitOrConfirm);
        backButton.onClick.AddListener(BackFromSettings);
        applyButton.onClick.AddListener(ApplyDraft);
        defaultsButton.onClick.AddListener(RestoreDefaults);
        for (int i = 0; i < sectionTabs.Length; i++)
        {
            int index = i;
            sectionTabs[i].onClick.AddListener(() => SetSection(index));
        }
        foreach (OptionControl option in options)
        {
            string key = option.key;
            if (option.button != null)
            {
                GameUITheme.StyleButton(option.button);
                option.button.onClick.AddListener(() => ActivateOption(key));
            }
            if (option.slider != null) option.slider.onValueChanged.AddListener(value => SetSlider(key, value));
        }
    }

    void ConfigureScrollHitAreas()
    {
        // Labels deliberately do not consume UI events. A transparent viewport
        // still needs a Graphic so scrolling/dragging empty space reaches ScrollRect.
        foreach (var scroll in GetComponentsInChildren<ScrollRect>(true))
        {
            if (!scroll.viewport) continue;
            var hitArea = scroll.viewport.GetComponent<Image>();
            if (!hitArea) hitArea = scroll.viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 40f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    void Update()
    {
        if (SahurLoadoutUI.BlocksInput) return;
        if (Keyboard.current == null) return;
        if (pendingKey != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                pendingKey = null;
                Refresh();
                return;
            }
            foreach (KeyControl control in Keyboard.current.allKeys)
            {
                if (!control.wasPressedThisFrame) continue;
                AssignKey(pendingKey, control.keyCode);
                pendingKey = null;
                Refresh();
                return;
            }
            return;
        }
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (!IsOpen)
        {
            if (!mainMenuSettingsOnly) Open();
        }
        else if (showingSettings) BackFromSettings();
        else Resume();
    }

    public void OpenSettingsFromMainMenu()
    {
        if (!mainMenuSettingsOnly) return;
        Open();
        ShowSettings();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        saved = ReadSettings();
        draft = saved;
        savedKeys = ReadKeys();
        draftKeys = (Key[])savedKeys.Clone();
        confirmQuit = false;
        overlay.alpha = 1f;
        overlay.blocksRaycasts = true;
        overlay.interactable = true;
        ShowHome();
    }

    public void Resume()
    {
        if (!IsOpen) return;
        pendingKey = null;
        draft = saved;
        IsOpen = false;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = mainMenuSettingsOnly ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mainMenuSettingsOnly;
        HideImmediate();
    }

    void BackFromSettings()
    {
        if (mainMenuSettingsOnly) Resume();
        else ShowHome();
    }

    void HideImmediate()
    {
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        overlay.interactable = false;
        homePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    void ShowHome()
    {
        pendingKey = null;
        showingSettings = false;
        confirmQuit = false;
        quitLabel.text = "QUIT";
        homePanel.SetActive(true);
        settingsPanel.SetActive(false);
        footerHint.text = "ESC  RESUME";
    }

    void ShowSettings()
    {
        showingSettings = true;
        homePanel.SetActive(false);
        settingsPanel.SetActive(true);
        SetSection(section);
        Refresh();
    }

    void SetSection(int next)
    {
        section = Mathf.Clamp(next, 0, SectionNames.Length - 1);
        for (int i = 0; i < sectionPanels.Length; i++)
        {
            bool selected = i == section;
            sectionPanels[i].SetActive(selected);
            GameUITheme.StyleButton(sectionTabs[i],selected);
            sectionTabLabels[i].color = selected ? Color.black : Color.white;
        }
        sectionTitle.text = SectionNames[section];
        footerHint.text = "ESC  BACK     /     APPLY TO SAVE";
    }

    void QuitOrConfirm()
    {
        if (!confirmQuit)
        {
            confirmQuit = true;
            quitLabel.text = "CONFIRM QUIT";
            footerHint.text = "SELECT AGAIN TO EXIT  /  ESC TO CANCEL";
            return;
        }
        bool hasActiveGame = FindFirstObjectByType<ThirdPersonPlayerController>() != null;
        bool savedGame = GameSaveManager.SaveCurrentGame();
        if (hasActiveGame && !savedGame)
        {
            confirmQuit = false;
            quitLabel.text = "QUIT";
            footerHint.text = "SAVE FAILED  /  EXIT CANCELLED";
            return;
        }
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ActivateOption(string key)
    {
        if (key.StartsWith("key.", StringComparison.Ordinal))
        {
            pendingKey = key;
            controlsByKey[key].value.text = "PRESS A KEY...";
            footerHint.text = "PRESS A KEY  /  ESC TO CANCEL";
            return;
        }
        switch (key)
        {
            case "language":
                GameLocalization.SetLanguage(GameLocalization.Language == GameLanguage.English ? GameLanguage.SimplifiedChinese : GameLanguage.English);
                break;
            case "difficulty": draft.difficulty = (draft.difficulty + 1) % 3; break;
            case "invertY": draft.invertY = !draft.invertY; break;
            case "resolution": draft.resolutionIndex = (draft.resolutionIndex + 1) % resolutions.Count; break;
            case "fullscreen": draft.fullscreen = !draft.fullscreen; break;
            case "vsync": draft.vsync = !draft.vsync; break;
            case "frameLimit": draft.frameLimitIndex = (draft.frameLimitIndex + 1) % FrameLimits.Length; break;
            case "texture": draft.textureMipmapLimit = (draft.textureMipmapLimit + 1) % 3; break;
            case "antialiasing": draft.antialiasing = (draft.antialiasing + 1) % 3; break;
            case "postProcessing": draft.postProcessing = !draft.postProcessing; break;
            case "shadows": draft.shadows = !draft.shadows; break;
            case "shadowResolution": draft.shadowResolutionIndex = (draft.shadowResolutionIndex + 1) % ShadowResolutions.Length; break;
            case "anisotropic": draft.anisotropicFiltering = (draft.anisotropicFiltering + 1) % 3; break;
            case "bloom": draft.bloom = !draft.bloom; break;
            case "vignette": draft.vignette = !draft.vignette; break;
            case "motionBlur": draft.motionBlur = !draft.motionBlur; break;
        }
        Refresh();
    }

    void SetSlider(string key, float value)
    {
        switch (key)
        {
            case "masterVolume": draft.masterVolume = value; break;
            case "sensitivity": draft.sensitivity = value; break;
            case "fieldOfView": draft.fieldOfView = value; break;
            case "cameraDistance": draft.cameraDistance = value; break;
            case "renderScale": draft.renderScale = value; break;
            case "shadowDistance": draft.shadowDistance = value; break;
            case "lodBias": draft.lodBias = value; break;
        }
        Refresh();
    }

    void AssignKey(string key, Key pressed)
    {
        if (!Enum.TryParse(key.Substring(4), out GameInputSettings.Action action)) return;
        if (pressed == Key.None || pressed == Key.Escape) return;
        int target = (int)action;
        for (int i = 0; i < draftKeys.Length; i++)
        {
            if (i == target || draftKeys[i] != pressed) continue;
            draftKeys[i] = draftKeys[target];
            break;
        }
        draftKeys[target] = pressed;
    }

    void ApplyDraft()
    {
        saved = draft;
        savedKeys = (Key[])draftKeys.Clone();
        ApplySettings(saved);
        for (int i = 0; i < savedKeys.Length; i++)
            GameInputSettings.Set((GameInputSettings.Action)i, savedKeys[i]);

        PlayerPrefs.SetFloat(Prefix + "MasterVolume", saved.masterVolume);
        PlayerPrefs.SetFloat(Prefix + "Sensitivity", saved.sensitivity);
        PlayerPrefs.SetFloat(Prefix + "FieldOfView", saved.fieldOfView);
        PlayerPrefs.SetFloat(Prefix + "CameraDistance", saved.cameraDistance);
        Put("InvertY", saved.invertY);
        Put("Fullscreen", saved.fullscreen);
        Put("VSync", saved.vsync);
        PlayerPrefs.SetInt(Prefix + "ResolutionWidth", resolutions[saved.resolutionIndex].width);
        PlayerPrefs.SetInt(Prefix + "ResolutionHeight", resolutions[saved.resolutionIndex].height);
        PlayerPrefs.SetInt(Prefix + "FrameLimit", FrameLimits[saved.frameLimitIndex]);
        SaveGraphicsPreferences(saved);
        PlayerPrefs.SetInt(Prefix + "Difficulty", saved.difficulty);
        PlayerPrefs.Save();
        footerHint.text = "SETTINGS SAVED";
        Refresh();
    }

    void RestoreDefaults()
    {
        GameLocalization.SetLanguage(GameLanguage.English);
        draft = factoryDefaults;
        draftKeys = new Key[Enum.GetValues(typeof(GameInputSettings.Action)).Length];
        for (int i = 0; i < draftKeys.Length; i++)
            draftKeys[i] = GameInputSettings.Default((GameInputSettings.Action)i);
        Refresh();
        footerHint.text = "DEFAULTS READY  /  APPLY TO SAVE";
    }

    Settings CaptureDefaults()
    {
        return new Settings
        {
            masterVolume = 1f,
            sensitivity = 0.12f,
            fieldOfView = Camera.main != null ? Camera.main.fieldOfView : 60f,
            cameraDistance = player != null ? player.cameraDistance : 4.2f,
            // Low-cost defaults, not visibility cuts. UI retains native resolution;
            // keep nearby contact shadows, color grading, and inexpensive FXAA.
            renderScale = 0.85f,
            shadowDistance = 180f,
            lodBias = 1f,
            invertY = false,
            fullscreen = Screen.fullScreen,
            vsync = true,
            postProcessing = true,
            shadows = true,
            bloom = false,
            vignette = false,
            motionBlur = false,
            resolutionIndex = FindResolution(Screen.width, Screen.height),
            textureMipmapLimit = 1,
            frameLimitIndex = 1,
            antialiasing = (int)AntialiasingMode.FastApproximateAntialiasing,
            shadowResolutionIndex = 0,
            anisotropicFiltering = (int)AnisotropicFiltering.Enable,
            difficulty = 1
        };
    }

    Settings ReadSettings()
    {
        // Migrate graphics once for existing installations. Audio, controls,
        // display mode, language, difficulty, and subsequent graphics choices survive.
        if (PlayerPrefs.GetInt(Prefix + "LowGraphicsDefaultV1", 0) == 0)
        {
            SaveGraphicsPreferences(factoryDefaults);
            PlayerPrefs.SetInt(Prefix + "LowGraphicsDefaultV1", 1);
            PlayerPrefs.Save();
        }
        Settings s = factoryDefaults;
        s.masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "MasterVolume", s.masterVolume));
        s.sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Sensitivity", s.sensitivity), 0.03f, 0.5f);
        s.fieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "FieldOfView", s.fieldOfView), 50f, 90f);
        s.cameraDistance = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "CameraDistance", s.cameraDistance), 2.5f, 11f);
        s.renderScale = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "RenderScale", s.renderScale), 0.6f, 1.2f);
        float storedShadowDistance = PlayerPrefs.GetFloat(Prefix + "ShadowDistance", s.shadowDistance);
        if (PlayerPrefs.GetInt(Prefix + "ShadowDistanceV2", 0) == 0)
        {
            // The old 50 m default was saved by earlier menu versions. Let
            // that default follow the new URP asset without overriding a
            // player's deliberate non-default distance.
            if (PlayerPrefs.HasKey(Prefix + "ShadowDistance") &&
                Mathf.Approximately(storedShadowDistance, 50f) && factoryDefaults.shadowDistance > 50f)
                storedShadowDistance = factoryDefaults.shadowDistance;
            PlayerPrefs.SetFloat(Prefix + "ShadowDistance", storedShadowDistance);
            PlayerPrefs.SetInt(Prefix + "ShadowDistanceV2", 1);
            PlayerPrefs.Save();
        }
        if (PlayerPrefs.GetInt(Prefix + "ShadowDistanceV3", 0) == 0)
        {
            // Upgrade earlier factory defaults, but retain a deliberate custom range.
            if (Mathf.Approximately(storedShadowDistance, 180f) ||
                Mathf.Approximately(storedShadowDistance, 50f))
                storedShadowDistance = factoryDefaults.shadowDistance;
            PlayerPrefs.SetFloat(Prefix + "ShadowDistance", storedShadowDistance);
            PlayerPrefs.SetInt(Prefix + "ShadowDistanceV3", 1);
            PlayerPrefs.Save();
        }
        s.shadowDistance = Mathf.Clamp(storedShadowDistance, 15f, 800f);
        s.lodBias = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "LodBias", s.lodBias), 0.5f, 3f);
        s.invertY = Get("InvertY", s.invertY);
        s.fullscreen = Get("Fullscreen", s.fullscreen);
        s.vsync = Get("VSync", s.vsync);
        s.postProcessing = Get("PostProcessing", s.postProcessing);
        s.shadows = Get("Shadows", s.shadows);
        s.bloom = Get("Bloom", s.bloom);
        s.vignette = Get("Vignette", s.vignette);
        s.motionBlur = Get("MotionBlur", s.motionBlur);
        s.resolutionIndex = FindResolution(
            PlayerPrefs.GetInt(Prefix + "ResolutionWidth", resolutions[s.resolutionIndex].width),
            PlayerPrefs.GetInt(Prefix + "ResolutionHeight", resolutions[s.resolutionIndex].height));
        s.textureMipmapLimit = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "TextureMipmapLimit", s.textureMipmapLimit), 0, 2);
        s.frameLimitIndex = Array.IndexOf(FrameLimits, PlayerPrefs.GetInt(Prefix + "FrameLimit", 60));
        if (s.frameLimitIndex < 0) s.frameLimitIndex = 1;
        s.antialiasing = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Antialiasing", s.antialiasing), 0, 2);
        s.shadowResolutionIndex = Array.IndexOf(ShadowResolutions,
            PlayerPrefs.GetInt(Prefix + "ShadowResolution", ShadowResolutions[s.shadowResolutionIndex]));
        if (s.shadowResolutionIndex < 0) s.shadowResolutionIndex = factoryDefaults.shadowResolutionIndex;
        s.anisotropicFiltering = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "AnisotropicFiltering", s.anisotropicFiltering), 0, 2);
        s.difficulty = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Difficulty", 1), 0, 2);
        return s;
    }

    static void SaveGraphicsPreferences(Settings s)
    {
        PlayerPrefs.SetFloat(Prefix + "RenderScale", s.renderScale);
        PlayerPrefs.SetFloat(Prefix + "ShadowDistance", s.shadowDistance);
        PlayerPrefs.SetFloat(Prefix + "LodBias", s.lodBias);
        PlayerPrefs.SetInt(Prefix + "TextureMipmapLimit", s.textureMipmapLimit);
        PlayerPrefs.SetInt(Prefix + "Antialiasing", s.antialiasing);
        PlayerPrefs.SetInt(Prefix + "ShadowResolution", ShadowResolutions[s.shadowResolutionIndex]);
        PlayerPrefs.SetInt(Prefix + "AnisotropicFiltering", s.anisotropicFiltering);
        Put("PostProcessing", s.postProcessing);
        Put("Shadows", s.shadows);
        Put("Bloom", s.bloom);
        Put("Vignette", s.vignette);
        Put("MotionBlur", s.motionBlur);
    }

    static void Put(string name, bool value) => PlayerPrefs.SetInt(Prefix + name, value ? 1 : 0);
    static bool Get(string name, bool fallback) => PlayerPrefs.GetInt(Prefix + name, fallback ? 1 : 0) != 0;

    static bool EffectActive<T>(bool fallback) where T : VolumeComponent
    {
        foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (volume.sharedProfile != null && volume.sharedProfile.TryGet(out T effect))
                return effect.active;
        return fallback;
    }

    void InitializeRuntimePipeline()
    {
        originalPipelineAsset = QualitySettings.renderPipeline;
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset active) return;
        runtimePipelineAsset = Instantiate(active);
        runtimePipelineAsset.name = active.name + " (Runtime Settings)";
        runtimePipelineAsset.hideFlags = HideFlags.DontSave;
        QualitySettings.renderPipeline = runtimePipelineAsset;
    }

    Key[] ReadKeys()
    {
        var keys = new Key[Enum.GetValues(typeof(GameInputSettings.Action)).Length];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = GameInputSettings.Get((GameInputSettings.Action)i);
        return keys;
    }

    int FindResolution(int width, int height)
    {
        for (int i = 0; i < resolutions.Count; i++)
            if (resolutions[i].width == width && resolutions[i].height == height) return i;
        return resolutions.Count - 1;
    }

    void CacheDifficultyTargets()
    {
        if (bossHealth == null)
        {
            bossHealth = FindFirstObjectByType<NailongHealth>();
            if (bossHealth != null) baseBossHealth = bossHealth.maxHealth;
        }
        if (bossAI == null)
        {
            bossAI = FindFirstObjectByType<NailongAI>();
            if (bossAI != null) baseBossSpeed = bossAI.chaseSpeed;
        }
        if (playerAttack == null)
        {
            playerAttack = FindFirstObjectByType<SahurAttack>();
            if (playerAttack != null) basePlayerDamage = playerAttack.damage;
        }
    }

    void ApplySettings(Settings s)
    {
        AudioListener.volume = s.masterVolume;
        if (Camera.main != null)
        {
            Camera.main.fieldOfView = s.fieldOfView;
            var urp = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            if (urp != null)
            {
                urp.antialiasing = (AntialiasingMode)s.antialiasing;
                urp.renderPostProcessing = s.postProcessing;
            }
        }
        if (player == null) player = FindFirstObjectByType<ThirdPersonPlayerController>();
        if (player != null)
        {
            player.mouseSensitivity = s.sensitivity;
            player.invertLookY = s.invertY;
            player.cameraCollision = true;
            player.cameraDistance = s.cameraDistance;
        }
        QualitySettings.globalTextureMipmapLimit = s.textureMipmapLimit;
        QualitySettings.anisotropicFiltering = (AnisotropicFiltering)s.anisotropicFiltering;
        QualitySettings.lodBias = s.lodBias;
        QualitySettings.vSyncCount = s.vsync ? 1 : 0;
        Application.targetFrameRate = FrameLimits[s.frameLimitIndex];
        if (runtimePipelineAsset != null)
        {
            runtimePipelineAsset.renderScale = s.renderScale;
            runtimePipelineAsset.shadowDistance = s.shadowDistance;
            int shadowResolution = ShadowResolutions[s.shadowResolutionIndex];
            runtimePipelineAsset.mainLightShadowmapResolution = shadowResolution;
            runtimePipelineAsset.additionalLightsShadowmapResolution = shadowResolution;
        }
        foreach (var pair in originalLightShadows)
            if (pair.Key != null) pair.Key.shadows = s.shadows ? pair.Value : LightShadows.None;
        foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            VolumeProfile profile = volume.profile;
            if (profile == null) continue;
            if (profile.TryGet(out Bloom bloom)) bloom.active = s.bloom;
            if (profile.TryGet(out Vignette vignette)) vignette.active = s.vignette;
            if (profile.TryGet(out MotionBlur motionBlur)) motionBlur.active = s.motionBlur;
        }
        CacheDifficultyTargets();
        float bossHealthFactor = s.difficulty == 0 ? 0.75f : s.difficulty == 2 ? 1.35f : 1f;
        float bossSpeedFactor = s.difficulty == 0 ? 0.85f : s.difficulty == 2 ? 1.2f : 1f;
        float playerDamageFactor = s.difficulty == 0 ? 1.2f : s.difficulty == 2 ? 0.85f : 1f;
        if (bossHealth != null)
        {
            float fraction = bossHealth.HealthFraction;
            bossHealth.maxHealth = baseBossHealth * bossHealthFactor;
            bossHealth.currentHealth = bossHealth.maxHealth * fraction;
        }
        if (bossAI != null) bossAI.chaseSpeed = baseBossSpeed * bossSpeedFactor;
        if (playerAttack != null) playerAttack.damage = basePlayerDamage * playerDamageFactor;
        Resolution resolution = resolutions[s.resolutionIndex];
        FullScreenMode mode = s.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (Screen.width != resolution.width || Screen.height != resolution.height || Screen.fullScreenMode != mode)
            Screen.SetResolution(resolution.width, resolution.height, mode);
    }

    void Refresh()
    {
        foreach (OptionControl option in options)
        {
            string value = ValueText(option.key);
            if (option.value != null) option.value.text = option.key == pendingKey ? "PRESS A KEY..." : value;
            if (option.slider == null) continue;
            float current = option.key switch
            {
                "masterVolume" => draft.masterVolume,
                "sensitivity" => draft.sensitivity,
                "fieldOfView" => draft.fieldOfView,
                "cameraDistance" => draft.cameraDistance,
                "renderScale" => draft.renderScale,
                "shadowDistance" => draft.shadowDistance,
                "lodBias" => draft.lodBias,
                _ => 0f
            };
            option.slider.SetValueWithoutNotify(current);
        }
    }

    string ValueText(string key)
    {
        if (key == "language") return GameLocalization.Language == GameLanguage.English ? "English" : "简体中文";
        if (key.StartsWith("key.", StringComparison.Ordinal) &&
            Enum.TryParse(key.Substring(4), out GameInputSettings.Action action))
            return draftKeys[(int)action].ToString().ToUpperInvariant();
        switch (key)
        {
            case "difficulty": return new[] { "EASY", "NORMAL", "HARD" }[draft.difficulty];
            case "cameraDistance": return draft.cameraDistance.ToString("0.0") + " M";
            case "masterVolume": return Mathf.RoundToInt(draft.masterVolume * 100f) + "%";
            case "sensitivity": return draft.sensitivity.ToString("0.00");
            case "fieldOfView": return Mathf.RoundToInt(draft.fieldOfView) + "°";
            case "invertY": return OnOff(draft.invertY);
            case "resolution":
                var r = resolutions[draft.resolutionIndex];
                return r.width + " × " + r.height;
            case "fullscreen": return draft.fullscreen ? "BORDERLESS" : "WINDOWED";
            case "vsync": return OnOff(draft.vsync);
            case "frameLimit": return FrameLimits[draft.frameLimitIndex] < 0 ? "UNLIMITED" : FrameLimits[draft.frameLimitIndex] + " FPS";
            case "texture": return new[] { "FULL", "HALF", "QUARTER" }[draft.textureMipmapLimit];
            case "antialiasing": return new[] { "OFF", "FXAA", "SMAA" }[draft.antialiasing];
            case "postProcessing": return OnOff(draft.postProcessing);
            case "shadows": return OnOff(draft.shadows);
            case "renderScale": return Mathf.RoundToInt(draft.renderScale * 100f) + "%";
            case "shadowDistance": return Mathf.RoundToInt(draft.shadowDistance) + " M";
            case "shadowResolution": return ShadowResolutions[draft.shadowResolutionIndex].ToString();
            case "anisotropic": return new[] { "OFF", "PER TEXTURE", "FORCED" }[draft.anisotropicFiltering];
            case "lodBias": return draft.lodBias.ToString("0.0") + "X";
            case "bloom": return OnOff(draft.bloom);
            case "vignette": return OnOff(draft.vignette);
            case "motionBlur": return OnOff(draft.motionBlur);
            default: return "";
        }
    }

    static string OnOff(bool enabled) => enabled ? "ON" : "OFF";

    void OnDestroy()
    {
        GameLocalization.Changed -= Refresh;
        if (runtimePipelineAsset != null)
        {
            QualitySettings.renderPipeline = originalPipelineAsset;
            Destroy(runtimePipelineAsset);
        }
        if (!IsOpen) return;
        IsOpen = false;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
