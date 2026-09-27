using System;
using System.Linq;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VerifySettingsMenu
{
    const string Prefix = "DarkBrine.Settings.";
    static readonly string[] FloatPrefs =
    {
        "MasterVolume", "Sensitivity", "FieldOfView", "CameraDistance",
        "RenderScale", "ShadowDistance", "LodBias"
    };
    static readonly string[] IntPrefs =
    {
        "InvertY", "Fullscreen", "VSync", "PostProcessing", "Shadows",
        "Bloom", "Vignette", "MotionBlur", "ResolutionWidth", "ResolutionHeight",
        "TextureMipmapLimit", "FrameLimit", "Antialiasing", "ShadowResolution",
        "AnisotropicFiltering", "Difficulty", "ShadowDistanceV2"
    };

    public static string Run()
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
        if (menu == null) throw new Exception("Settings menu was not instantiated.");
        if (menu.sectionPanels.Length != 5 || menu.options.Count != 29)
            throw new Exception("Unexpected section or option count.");
        if (menu.options.Any(o => o.value == null || (o.button == null && o.slider == null)))
            throw new Exception("An interactive option is missing its UI control.");
        if (menu.options.Select(o => o.key).Distinct().Count() != menu.options.Count ||
            menu.options.Any(o => new[] { "gameSpeed", "textScale", "highContrast", "cameraCollision", "followSharpness" }.Contains(o.key)))
            throw new Exception("A duplicate or removed option is still present.");
        if (menu.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text == "N/A"))
            throw new Exception("An unsupported placeholder is still visible.");
        if (UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1)
            throw new Exception("The scene must have exactly one EventSystem.");

        float?[] originalFloats = FloatPrefs.Select(n => PlayerPrefs.HasKey(Prefix + n)
            ? (float?)PlayerPrefs.GetFloat(Prefix + n) : null).ToArray();
        int?[] originalInts = IntPrefs.Select(n => PlayerPrefs.HasKey(Prefix + n)
            ? (int?)PlayerPrefs.GetInt(Prefix + n) : null).ToArray();
        var actions = (GameInputSettings.Action[])Enum.GetValues(typeof(GameInputSettings.Action));
        Key[] originalKeys = actions.Select(GameInputSettings.Get).ToArray();
        bool[] hadKeyPref = actions.Select(a => PlayerPrefs.HasKey(Prefix + "Key." + a)).ToArray();
        float originalTimeScale = Time.timeScale;
        float originalLodBias = QualitySettings.lodBias;
        var originalAnisotropic = QualitySettings.anisotropicFiltering;
        var originalPipeline = QualitySettings.renderPipeline;
        try
        {
            var boss = UnityEngine.Object.FindFirstObjectByType<NailongHealth>();
            var bossAI = UnityEngine.Object.FindFirstObjectByType<NailongAI>();
            var attack = UnityEngine.Object.FindFirstObjectByType<SahurAttack>();
            var urp = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (boss == null || bossAI == null || attack == null || urp == null || pipeline == null)
                throw new Exception("A live gameplay or URP target is missing.");
            if (UnityEditor.EditorUtility.IsPersistent(pipeline))
                throw new Exception("Graphics changes would modify the project URP asset instead of a runtime copy.");

            float oldBossHealth = boss.maxHealth;
            float oldBossSpeed = bossAI.chaseSpeed;
            float oldDamage = attack.damage;
            var oldAA = urp.antialiasing;
            float oldRenderScale = pipeline.renderScale;
            int oldShadowResolution = pipeline.mainLightShadowmapResolution;
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)
                .FirstOrDefault(v => v.sharedProfile != null && v.sharedProfile.TryGet(out Bloom _));
            if (volume == null || !volume.profile.TryGet(out Bloom bloom) ||
                !volume.profile.TryGet(out MotionBlur motionBlur))
                throw new Exception("The active scene lacks its Bloom or Motion Blur profile.");
            bool oldBloom = bloom.active;
            bool oldMotionBlur = motionBlur.active;

            menu.Open();
            menu.settingsButton.onClick.Invoke();
            Click(menu, "difficulty");
            menu.applyButton.onClick.Invoke();
            if (Mathf.Approximately(boss.maxHealth, oldBossHealth) ||
                Mathf.Approximately(bossAI.chaseSpeed, oldBossSpeed) ||
                Mathf.Approximately(attack.damage, oldDamage))
                throw new Exception("Difficulty did not affect Nailong and Sahur.");

            menu.sectionTabs[4].onClick.Invoke();
            Click(menu, "antialiasing");
            Click(menu, "shadowResolution");
            Click(menu, "anisotropic");
            Click(menu, "bloom");
            Click(menu, "motionBlur");
            Slide(menu, "renderScale", Mathf.Approximately(oldRenderScale, 0.75f) ? 0.9f : 0.75f);
            Slide(menu, "shadowDistance", 30f);
            Slide(menu, "lodBias", 1.25f);
            menu.applyButton.onClick.Invoke();
            if (urp.antialiasing == oldAA ||
                Mathf.Approximately(pipeline.renderScale, oldRenderScale) ||
                !Mathf.Approximately(pipeline.shadowDistance, 30f) ||
                pipeline.mainLightShadowmapResolution == oldShadowResolution ||
                !Mathf.Approximately(QualitySettings.lodBias, 1.25f) ||
                bloom.active == oldBloom || motionBlur.active == oldMotionBlur)
                throw new Exception("A graphics setting did not reach its live URP, quality, or volume target.");

            menu.sectionTabs[1].onClick.Invoke();
            Click(menu, "key.Forward");
            typeof(PauseSettingsMenu).GetMethod("AssignKey", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(menu, new object[] { "key.Forward", Key.T });
            typeof(PauseSettingsMenu).GetField("pendingKey", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(menu, null);
            menu.applyButton.onClick.Invoke();
            if (GameInputSettings.Get(GameInputSettings.Action.Forward) != Key.T)
                throw new Exception("Keyboard rebind did not reach Sahur input.");

            menu.defaultsButton.onClick.Invoke();
            menu.applyButton.onClick.Invoke();
            if (GameInputSettings.Get(GameInputSettings.Action.Forward) != Key.W ||
                !Mathf.Approximately(pipeline.renderScale, 1f))
                throw new Exception("Restore defaults did not restore controls and graphics.");
            menu.Resume();
            if (PauseSettingsMenu.IsOpen || !Mathf.Approximately(Time.timeScale, originalTimeScale))
                throw new Exception("Resume did not restore gameplay time.");
            return "5 sections, 29 options; gameplay, controls, URP graphics, volume effects, defaults and resume passed.";
        }
        finally
        {
            if (PauseSettingsMenu.IsOpen) menu.Resume();
            for (int i = 0; i < FloatPrefs.Length; i++)
                if (originalFloats[i].HasValue) PlayerPrefs.SetFloat(Prefix + FloatPrefs[i], originalFloats[i].Value);
                else PlayerPrefs.DeleteKey(Prefix + FloatPrefs[i]);
            for (int i = 0; i < IntPrefs.Length; i++)
                if (originalInts[i].HasValue) PlayerPrefs.SetInt(Prefix + IntPrefs[i], originalInts[i].Value);
                else PlayerPrefs.DeleteKey(Prefix + IntPrefs[i]);
            for (int i = 0; i < actions.Length; i++)
            {
                GameInputSettings.Set(actions[i], originalKeys[i]);
                if (!hadKeyPref[i]) PlayerPrefs.DeleteKey(Prefix + "Key." + actions[i]);
            }
            PlayerPrefs.Save();
            QualitySettings.lodBias = originalLodBias;
            QualitySettings.anisotropicFiltering = originalAnisotropic;
            if (QualitySettings.renderPipeline != originalPipeline)
                QualitySettings.renderPipeline = originalPipeline;
        }
    }

    static void Click(PauseSettingsMenu menu, string key) =>
        menu.options.First(o => o.key == key).button.onClick.Invoke();

    static void Slide(PauseSettingsMenu menu, string key, float value) =>
        menu.options.First(o => o.key == key).slider.value = value;
}
