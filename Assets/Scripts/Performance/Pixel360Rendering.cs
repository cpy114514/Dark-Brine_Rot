using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>URP scene resolution only; the display, input projection and overlay UI stay native.</summary>
    public sealed class Pixel360Rendering : MonoBehaviour
    {
        public const int TargetHeight = 360;
        static Pixel360Rendering instance;
        UniversalRenderPipelineAsset pipeline;
        float normalScale;
        int normalMsaa;
        UpscalingFilterSelection normalFilter;
        bool running, refreshVolumes;
        RectTransform overlayRoot;
        struct CameraState { public Camera camera; public UniversalAdditionalCameraData data; public AntialiasingMode aa; public bool msaa, dithering; }
        struct EffectState { public VolumeComponent effect; public bool active; }
        readonly List<CameraState> cameras = new List<CameraState>();
        readonly List<EffectState> effects = new List<EffectState>();
        public static bool IsActive => instance && instance.running;
        public static RectTransform OverlayRoot
        {
            get
            {
                if (!IsActive) return null;
                if (!instance.overlayRoot)
                {
                    var go = new GameObject("Native floating captions", typeof(RectTransform), typeof(Canvas));
                    go.transform.SetParent(instance.transform, false);
                    var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
                    instance.overlayRoot = go.GetComponent<RectTransform>();
                }
                return instance.overlayRoot;
            }
        }

        public static void Configure(UniversalRenderPipelineAsset asset, bool enabled)
        {
            ReleaseCurrent();
            if (!enabled || !asset) return;
            var go = new GameObject("360p pixel rendering");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Pixel360Rendering>();
            instance.pipeline = asset;
            instance.normalScale = asset.renderScale;
            instance.normalFilter = asset.upscalingFilter;
            instance.normalMsaa = asset.msaaSampleCount;
            instance.running = true;
            asset.upscalingFilter = UpscalingFilterSelection.Point;
            asset.msaaSampleCount = 1;
            asset.renderScale = ScaleForHeight(Screen.height);
            if (Camera.main) instance.PrepareCamera(Camera.main);
            instance.CaptureVolumes();
            RenderPipelineManager.beginCameraRendering += instance.BeginCamera;
            SceneManager.sceneLoaded += instance.SceneLoaded;
        }

        public static void Release(UniversalRenderPipelineAsset owner)
        {
            if (!instance || instance.pipeline != owner) return;
            var previous = instance; instance = null;
            previous.Restore();
            Destroy(previous.gameObject);
        }
        public static void ReleaseCurrent() { if (instance) Release(instance.pipeline); }

        public static float ScaleForHeight(int height) => height <= TargetHeight ? 1f :
            Mathf.Clamp((TargetHeight + .001f) / height, UniversalRenderPipeline.minRenderScale, 1f);

        void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!running || !pipeline || camera.cameraType != CameraType.Game) return;
            // URP reads the asset after this callback. Recompute per viewport,
            // including a resized window, cinematics and offscreen game previews.
            pipeline.renderScale = ScaleForHeight(camera.pixelHeight);
            PrepareCamera(camera);
        }
        void PrepareCamera(Camera camera)
        {
            foreach (var state in cameras) if (state.camera == camera) return;
            var data = camera.GetUniversalAdditionalCameraData();
            cameras.Add(new CameraState { camera = camera, data = data, aa = data.antialiasing, msaa = camera.allowMSAA, dithering = data.dithering });
            data.antialiasing = AntialiasingMode.None;
            data.dithering = false;
            camera.allowMSAA = false;
        }
        void SceneLoaded(Scene scene, LoadSceneMode mode) { refreshVolumes = true; }
        void LateUpdate()
        {
            if (!refreshVolumes) return;
            refreshVolumes = false; CaptureVolumes();
        }
        void CaptureVolumes()
        {
            foreach (var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                var profile = volume.profile;
                if (!profile) continue;
                if (profile.TryGet(out MotionBlur blur)) Suppress(blur);
                if (profile.TryGet(out DepthOfField focus)) Suppress(focus);
                if (profile.TryGet(out FilmGrain grain)) Suppress(grain);
            }
        }
        void Suppress(VolumeComponent effect)
        {
            foreach (var state in effects) if (state.effect == effect) return;
            effects.Add(new EffectState { effect = effect, active = effect.active });
            effect.active = false;
        }
        void Restore()
        {
            if (!running) return; running = false;
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (pipeline) { pipeline.renderScale = normalScale; pipeline.upscalingFilter = normalFilter; pipeline.msaaSampleCount = normalMsaa; }
            foreach (var state in cameras)
            {
                if (state.camera) state.camera.allowMSAA = state.msaa;
                if (state.data) { state.data.antialiasing = state.aa; state.data.dithering = state.dithering; }
            }
            foreach (var state in effects) if (state.effect) state.effect.active = state.active;
            cameras.Clear(); effects.Clear();
        }
        void OnDestroy() { Restore(); if (instance == this) instance = null; }
    }
}
