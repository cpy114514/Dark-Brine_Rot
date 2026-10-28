using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis.VisualStyles
{
    /// <summary>Persistent visual-only state. Presets, original materials and gameplay state are never mutated.</summary>
    [DefaultExecutionOrder(-300)]
    public sealed class VisualStyleManager : MonoBehaviour
    {
        static VisualStyleManager instance;
        public static VisualStyleManager Instance { get { Ensure(); return instance; } }
        public static event Action Changed;
        public static bool IsIllustrated => instance && instance.Current && instance.Current.illustrated;
        public IReadOnlyList<VisualStylePreset> Presets => presets;
        public VisualStylePreset Current { get; private set; }
        public float Intensity { get; private set; }
        VisualStylePreset[] presets;
        Color fog, ambientSky, ambientEquator, ambientGround;
        bool environmentCaptured;
        int capturedScene = int.MinValue;
        public event Action<VisualStylePreset, float> EnvironmentChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; Changed = null; Shader.SetGlobalVector("_EnvironmentStyle", Vector4.zero); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Ensure()
        {
            if (instance || !Application.isPlaying) return;
            var go = new GameObject("Visual Style Manager");
            DontDestroyOnLoad(go); go.AddComponent<VisualStyleManager>();
        }
        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            presets = Resources.LoadAll<VisualStylePreset>("VisualStyles/Presets").OrderBy(p => p.illustrated).ThenBy(p => p.id).ToArray();
            Intensity = VisualStyleSettings.Intensity;
            Current = presets.FirstOrDefault(p => p.id == VisualStyleSettings.SelectedId) ?? presets.FirstOrDefault();
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }
        void Start() { if (!environmentCaptured) CaptureEnvironment(); }
        void SceneLoaded(Scene scene, LoadSceneMode mode) { if (scene == SceneManager.GetActiveScene()) CaptureEnvironment(); }
        void ActiveSceneChanged(Scene oldScene, Scene nextScene) { CaptureEnvironment(); }
        void CaptureEnvironment()
        {
            int handle = SceneManager.GetActiveScene().handle;
            if (environmentCaptured && capturedScene == handle) return;
            capturedScene = handle;
            // Each active scene supplies its own canonical atmosphere. Do not restore an outgoing scene over it.
            fog = RenderSettings.fogColor; ambientSky = RenderSettings.ambientSkyColor;
            ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
            environmentCaptured = true; ApplyEnvironment();
        }
        public bool Select(string id, bool persist = true)
        {
            var next = presets.FirstOrDefault(p => p.id == id);
            if (!next) return false;
            Current = next; ApplyEnvironment();
            if (persist) VisualStyleSettings.Save(Current.id, Intensity);
            Changed?.Invoke(); return true;
        }
        public void SetIntensity(float value, bool persist = true)
        {
            Intensity = Mathf.Clamp(value, .4f, 1); ApplyEnvironment();
            if (persist && Current) VisualStyleSettings.Save(Current.id, Intensity);
            Changed?.Invoke();
        }
        public void RestorePreference() { Intensity = VisualStyleSettings.Intensity; Select(VisualStyleSettings.SelectedId, false); }
        void ApplyEnvironment()
        {
            if (!Current) return;
            // Transparent sky/water render after the opaque illustration pass, so shade them in place.
            Shader.SetGlobalVector("_EnvironmentStyle", new Vector4(Current.illustrated ? Intensity : 0,
                Current.saturation, Current.tonalBands, Current.paperGrain));
            Shader.SetGlobalColor("_EnvironmentShadow", Current.shadows);
            Shader.SetGlobalColor("_EnvironmentHighlight", Current.highlights);
            Shader.SetGlobalColor("_EnvironmentInk", Current.ink);
            if (!environmentCaptured) return;
            float t = Current.illustrated ? Current.atmosphereInfluence * Intensity : 0;
            RenderSettings.fogColor = Color.Lerp(fog, Current.fogTint, t);
            RenderSettings.ambientSkyColor = Color.Lerp(ambientSky, ambientSky * Current.shadows, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(ambientEquator, ambientEquator * Current.highlights, t);
            RenderSettings.ambientGroundColor = Color.Lerp(ambientGround, ambientGround * Current.shadows, t);
            EnvironmentChanged?.Invoke(Current, Intensity);
        }
        void OnDestroy()
        {
            if (instance != this) return;
            Shader.SetGlobalVector("_EnvironmentStyle", Vector4.zero);
            SceneManager.sceneLoaded -= SceneLoaded; SceneManager.activeSceneChanged -= ActiveSceneChanged;
            if (environmentCaptured) { RenderSettings.fogColor = fog; RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientEquator; RenderSettings.ambientGroundColor = ambientGround; }
            instance = null;
        }
    }
}
