using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit Pipeline test helper; no code or test hooks are installed in the Player.
public static class VerifyFirstRunSettings
{
    const string Root = ".codex/far-view-settings-20261102";
    const string Prefix = "DarkBrine.Settings.";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Floats = { "MasterVolume", "Sensitivity", "FieldOfView", "CameraDistance", "RenderScale", "ShadowDistance", "LodBias" };
    static readonly string[] Ints = { "InvertY", "Fullscreen", "VSync", "ResolutionWidth", "ResolutionHeight", "FrameLimit", "Difficulty",
        "TextureMipmapLimit", "Antialiasing", "ShadowResolution", "AnisotropicFiltering", "PostProcessing", "Shadows", "Bloom", "Vignette",
        "MotionBlur", "Pixel360", "RecommendedDefaultsV1", "LowGraphicsDefaultV1", "ShadowDistanceV2", "ShadowDistanceV3" };
    public sealed class Preference { public string name; public bool floating, exists; public float real; public int integer; }
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static void Write(string name, object value)
    { Directory.CreateDirectory(Root); File.WriteAllText(Root + "/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented)); }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    static object Get(object target, string name) => target.GetType().GetField(name, Hidden | BindingFlags.Public).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden | BindingFlags.Public).SetValue(target, value);
    static Preference[] Capture() => Floats.Select(n => new Preference { name = n, floating = true, exists = PlayerPrefs.HasKey(Prefix + n), real = PlayerPrefs.GetFloat(Prefix + n) })
        .Concat(Ints.Select(n => new Preference { name = n, exists = PlayerPrefs.HasKey(Prefix + n), integer = PlayerPrefs.GetInt(Prefix + n) })).ToArray();
    static void Restore(Preference[] preferences)
    {
        foreach (var p in preferences)
            if (!p.exists) PlayerPrefs.DeleteKey(Prefix + p.name);
            else if (p.floating) PlayerPrefs.SetFloat(Prefix + p.name, p.real);
            else PlayerPrefs.SetInt(Prefix + p.name, p.integer);
        PlayerPrefs.Save();
    }
    static void Clear()
    {
        foreach (string name in Floats.Concat(Ints)) PlayerPrefs.DeleteKey(Prefix + name);
        PlayerPrefs.Save();
    }
    static PauseSettingsMenu Fixture()
    {
        var go = new GameObject("First-run settings fixture"); go.SetActive(false);
        var menu = go.AddComponent<PauseSettingsMenu>();
        Call(menu, "PopulateResolutions"); Set(menu, "factoryDefaults", Call(menu, "CaptureDefaults")); return menu;
    }
    static void Recommended(object settings)
    {
        Check(Mathf.Approximately((float)Get(settings, "renderScale"), .75f), "Recommended render scale");
        Check((int)Get(settings, "frameLimitIndex") == 1 && !(bool)Get(settings, "vsync"), "Recommended effective 60 FPS cap");
        Check((int)Get(settings, "antialiasing") == (int)AntialiasingMode.FastApproximateAntialiasing, "Recommended FXAA");
        Check((int)Get(settings, "shadowResolutionIndex") == 0 && (float)Get(settings, "shadowDistance") == 60f, "Recommended shadow policy");
        Check((bool)Get(settings, "postProcessing") && (bool)Get(settings, "shadows"), "Core rendering retained");
        Check(!(bool)Get(settings, "bloom") && !(bool)Get(settings, "vignette") && !(bool)Get(settings, "motionBlur") && !(bool)Get(settings, "pixel360"), "Optional effects disabled");
        Check((int)Get(settings, "textureMipmapLimit") == 2 && (float)Get(settings, "lodBias") == 1.5f, "Lower texture cost and longer LOD range");
    }
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        checks = 0; var original = Capture();
        Write("fixture-preferences-before", original);
        bool languageExists = PlayerPrefs.HasKey(Prefix + "Language"), jumpExists = PlayerPrefs.HasKey(Prefix + "Key.Jump");
        int language = PlayerPrefs.GetInt(Prefix + "Language"), jump = PlayerPrefs.GetInt(Prefix + "Key.Jump");
        PauseSettingsMenu menu = null, next = null;
        try
        {
            menu = Fixture(); Clear();
            object fresh = Call(menu, "ReadSettings"); Recommended(fresh);
            Check(PlayerPrefs.GetInt(Prefix + "RecommendedDefaultsV1") == 1, "First-run marker saved");
            Check(Floats.Concat(Ints.Take(17)).All(n => PlayerPrefs.HasKey(Prefix + n)), "Preferences persisted on first read");
            Check(PlayerPrefs.GetInt(Prefix + "FrameLimit") == 60 && PlayerPrefs.GetInt(Prefix + "VSync") == 0, "Persisted frame pacing");
            PlayerPrefs.SetFloat(Prefix + "RenderScale", 1.1f); PlayerPrefs.SetFloat(Prefix + "ShadowDistance", 50f);
            PlayerPrefs.SetFloat(Prefix + "MasterVolume", .37f); PlayerPrefs.SetFloat(Prefix + "Sensitivity", .2f);
            PlayerPrefs.SetInt(Prefix + "FrameLimit", 144); PlayerPrefs.SetInt(Prefix + "VSync", 1);
            PlayerPrefs.SetInt(Prefix + "Difficulty", 2); PlayerPrefs.Save();
            object changed = Call(menu, "ReadSettings");
            Check((float)Get(changed, "renderScale") == 1.1f && (float)Get(changed, "shadowDistance") == 50f, "Custom graphics retained");
            Check((int)Get(changed, "frameLimitIndex") == 3 && (bool)Get(changed, "vsync"), "Custom frame pacing retained");
            Check((float)Get(changed, "masterVolume") == .37f && (float)Get(changed, "sensitivity") == .2f && (int)Get(changed, "difficulty") == 2, "Other preferences retained");
            next = Fixture();
            object reopened = Call(next, "ReadSettings");
            Check((float)Get(reopened, "renderScale") == 1.1f, "New menu instance retains settings");
            // Removing only the initialization marker simulates upgrading an older installation.
            PlayerPrefs.DeleteKey(Prefix + "RecommendedDefaultsV1");
            object legacy = Call(next, "ReadSettings");
            Check((float)Get(legacy, "renderScale") == 1.1f && (float)Get(legacy, "shadowDistance") == 50f, "Legacy choices survive missing old migration flags");
            Check((float)Get(legacy, "masterVolume") == .37f && (int)Get(legacy, "difficulty") == 2, "Legacy non-graphics choices retained");
            PlayerPrefs.DeleteKey(Prefix + "MasterVolume");
            Call(next, "ReadSettings");
            Check(!PlayerPrefs.HasKey(Prefix + "MasterVolume"), "Subsequent reads do not initialize again");
            // Invalid old values are normalized by the existing reader before they are first persisted.
            Clear(); PlayerPrefs.SetInt(Prefix + "FrameLimit", 999); PlayerPrefs.SetFloat(Prefix + "RenderScale", 9f);
            object invalid = Call(next, "ReadSettings");
            Check((int)Get(invalid, "frameLimitIndex") == 1 && (float)Get(invalid, "renderScale") == 1.2f, "Invalid legacy values clamped");
            Check(PlayerPrefs.GetInt(Prefix + "FrameLimit") == 60 && PlayerPrefs.GetFloat(Prefix + "RenderScale") == 1.2f, "Normalized values persisted");
            Check(PlayerPrefs.HasKey(Prefix + "Language") == languageExists && PlayerPrefs.GetInt(Prefix + "Language") == language, "Language untouched");
            Check(PlayerPrefs.HasKey(Prefix + "Key.Jump") == jumpExists && PlayerPrefs.GetInt(Prefix + "Key.Jump") == jump, "Bindings untouched");
            var result = new { passed = true, checks }; Write("fixture-result", result); return result;
        }
        finally
        {
            Restore(original);
            if (menu) Object.DestroyImmediate(menu.gameObject);
            if (next) Object.DestroyImmediate(next.gameObject);
        }
    }

    public static object StageFreshLaunch(bool reuseSnapshot = false)
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Unsaved scenes");
        if (File.Exists(Root + "/live-preferences-before.json"))
        {
            if (!reuseSnapshot || File.Exists(Root + "/live-restored.json")) throw new Exception("Retain previous snapshot");
            var original = Newtonsoft.Json.JsonConvert.DeserializeObject<Preference[]>(File.ReadAllText(Root + "/live-preferences-before.json"));
            if (Newtonsoft.Json.JsonConvert.SerializeObject(Capture()) != Newtonsoft.Json.JsonConvert.SerializeObject(original)) throw new Exception("Preferences changed since snapshot");
        }
        else
        {
            Write("live-preferences-before", Capture()); Write("scene-setup-before", setup);
            string save = (string)typeof(Mavis.GameSaveManager).GetProperty("SavePath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Write("save-before", new[] { save, save + ".bak", save + ".tmp" }.Select(p => new { path = p, bytes = File.Exists(p) ? Convert.ToBase64String(File.ReadAllBytes(p)) : null }).ToArray());
        }
        Clear(); EditorSceneManager.OpenScene("Assets/Scenes/Main Menu/MainMenu.unity", OpenSceneMode.Single);
        return new { staged = true };
    }
    public static object InspectFreshLaunch()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode required");
        checks = 0; var menu = Object.FindFirstObjectByType<PauseSettingsMenu>();
        Check(menu != null && Object.FindFirstObjectByType<MainMenuController>() != null, "Real title scene/menu");
        Recommended(Get(menu, "saved"));
        var pipeline = (UniversalRenderPipelineAsset)Get(menu, "runtimePipelineAsset");
        Check(pipeline != null && Mathf.Approximately(pipeline.renderScale, .75f), "Recommended scale actually applied before opening settings");
        Check(QualitySettings.vSyncCount == 0 && Application.targetFrameRate == 60, "Actual 60 FPS cap applied");
        Check(pipeline.mainLightShadowmapResolution == 1024 && pipeline.shadowDistance == 60f, "Actual recommended shadows");
        Check(QualitySettings.lodBias == 1.5f && QualitySettings.globalTextureMipmapLimit == 2, "Actual distance-priority settings");
        var camera = Camera.main ? Camera.main : Object.FindFirstObjectByType<Camera>();
        Check(camera != null && camera.GetUniversalAdditionalCameraData().antialiasing == AntialiasingMode.FastApproximateAntialiasing, "Actual recommended FXAA on title camera");
        Check(PlayerPrefs.GetInt(Prefix + "RecommendedDefaultsV1") == 1, "Real startup wrote marker");
        Check(!PauseSettingsMenu.IsOpen, "Settings applied without opening panel");
        var result = new { passed = true, checks, targetFrameRate = Application.targetFrameRate, renderScale = pipeline.renderScale };
        Write("fresh-launch", result); return result;
    }
    public static object SaveCustomChoice()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode required");
        var menu = Object.FindFirstObjectByType<PauseSettingsMenu>(); var draft = Get(menu, "draft");
        Set(draft, "renderScale", 1.1f); Set(draft, "shadowDistance", 55f); Set(draft, "frameLimitIndex", 2);
        Set(menu, "draft", draft); Call(menu, "ApplyDraft");
        return new { saved = PlayerPrefs.GetFloat(Prefix + "RenderScale") == 1.1f && PlayerPrefs.GetInt(Prefix + "FrameLimit") == 120 };
    }
    public static object InspectReturningLaunch()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode required");
        checks = 0; var menu = Object.FindFirstObjectByType<PauseSettingsMenu>();
        var pipeline = (UniversalRenderPipelineAsset)Get(menu, "runtimePipelineAsset");
        Check(Mathf.Approximately(pipeline.renderScale, 1.1f) && pipeline.shadowDistance == 55f, "Custom graphics restored on next launch");
        Check(Application.targetFrameRate == 120 && QualitySettings.vSyncCount == 0, "Custom frame limit restored");
        Check(PlayerPrefs.GetInt(Prefix + "RecommendedDefaultsV1") == 1 && !PauseSettingsMenu.IsOpen, "No repeat setup or menu interaction");
        var result = new { passed = true, checks }; Write("returning-launch", result); return result;
    }
    public static object RestoreLiveTest()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before restoring");
        if (File.Exists(Root + "/live-restored.json")) throw new Exception("Already restored; do not replay old settings");
        Restore(Newtonsoft.Json.JsonConvert.DeserializeObject<Preference[]>(File.ReadAllText(Root + "/live-preferences-before.json")));
        var setup = Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/scene-setup-before.json"));
        EditorSceneManager.RestoreSceneManagerSetup(setup);
        var result = new { restored = true }; Write("live-restored", result); return result;
    }
    public static object VerifyRestored()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        var original = Newtonsoft.Json.JsonConvert.DeserializeObject<Preference[]>(File.ReadAllText(Root + "/live-preferences-before.json"));
        bool preferences = Newtonsoft.Json.JsonConvert.SerializeObject(Capture()) == Newtonsoft.Json.JsonConvert.SerializeObject(original);
        var setup = Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/scene-setup-before.json"));
        bool scenes = setup.Select(s => s.path + "|" + s.isLoaded + "|" + s.isActive)
            .SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(s => s.path + "|" + s.isLoaded + "|" + s.isActive));
        bool save = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Root + "/save-before.json")).All(entry =>
        {
            string path = (string)entry["path"], bytes = (string)entry["bytes"];
            return bytes == null ? !File.Exists(path) : File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(Convert.FromBase64String(bytes));
        });
        if (!preferences || !scenes || !save) throw new Exception("Test state was not restored exactly");
        var result = new { passed = true, preferences, scenes, save }; Write("state-integrity", result); return result;
    }
    public static object ApplyLocalRecommendedGraphics()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first");
        if (File.Exists(Root + "/local-graphics-applied.json")) throw new Exception("Do not replay the preset over later choices");
        var before = Capture(); Write("preferences-before-local-preset", before);
        var menu = Fixture();
        try
        {
            object defaults = Get(menu, "factoryDefaults");
            typeof(PauseSettingsMenu).GetMethod("SaveGraphicsPreferences", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { defaults });
            PlayerPrefs.SetInt(Prefix + "FrameLimit", 60); PlayerPrefs.SetInt(Prefix + "VSync", 0); PlayerPrefs.Save();
            var allowed = new HashSet<string>(new[] { "RenderScale", "ShadowDistance", "LodBias", "TextureMipmapLimit", "Antialiasing", "ShadowResolution",
                "AnisotropicFiltering", "PostProcessing", "Shadows", "Bloom", "Vignette", "MotionBlur", "Pixel360", "FrameLimit", "VSync" });
            var after = Capture();
            if (Newtonsoft.Json.JsonConvert.SerializeObject(before.Where(p => !allowed.Contains(p.name))) !=
                Newtonsoft.Json.JsonConvert.SerializeObject(after.Where(p => !allowed.Contains(p.name)))) throw new Exception("An unrelated setting changed");
            var result = new { applied = true, otherSettingsPreserved = true, preferences = after.Where(p => allowed.Contains(p.name)).ToArray() };
            Write("local-graphics-applied", result); return new { applied = true, otherSettingsPreserved = true };
        }
        finally { Object.DestroyImmediate(menu.gameObject); }
    }
}
