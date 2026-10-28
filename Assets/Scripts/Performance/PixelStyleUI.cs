using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Pixelates non-text uGUI in place. No duplicate UI cameras, render targets or input proxies.</summary>
    public sealed class PixelStyleUI : MonoBehaviour
    {
        // uGUI explicitly exposes its shared default to derived Graphics. Keep the builtin material intact.
        abstract class GraphicDefaults : Graphic { public static void Set(Material material) { s_DefaultUI = material; } }
        static PixelStyleUI instance;
        Material original, pixel;
        readonly Dictionary<Material, Material> variants = new Dictionary<Material, Material>();
        readonly Dictionary<Graphic, Material> bindings = new Dictionary<Graphic, Material>();
        Canvas[] canvases;
        float refreshAt;
        int frame = -1;
        bool active;
        int displayWidth, displayHeight;
        public static Material NativeTextMaterial => instance && instance.original ? instance.original : Graphic.defaultGraphicMaterial;
        void OnEnable()
        {
            var shader = Resources.Load<Shader>("Rendering/PixelStyleUI");
            if (!shader) return;
            instance = this; original = Graphic.defaultGraphicMaterial;
            pixel = new Material(shader) { name = "Pixel Style UI (runtime)", hideFlags = HideFlags.HideAndDontSave };
            pixel.CopyPropertiesFromMaterial(original);
            GraphicDefaults.Set(pixel); active = true;
            Shader.SetGlobalFloat("_PixelStyleUI", 1);
            Shader.SetGlobalFloat("_PixelStyleTargetHeight", Pixel360Rendering.TargetHeight);
            Shader.SetGlobalFloat("_PixelStyleMinScale", UnityEngine.Rendering.Universal.UniversalRenderPipeline.minRenderScale);
            DisplaySize();
            RefreshCanvases(true);
            Canvas.preWillRenderCanvases += Prepare;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        void SceneLoaded(Scene scene, LoadSceneMode mode) { refreshAt = 0; }
        void RefreshCanvases(bool rebuild = false)
        {
            canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            refreshAt = Time.unscaledTime + .25f;
            foreach (var canvas in canvases)
                if (canvas) foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
                { PrepareGraphic(graphic); if (rebuild) graphic.SetMaterialDirty(); }
        }
        void DisplaySize()
        {
            if (displayWidth == Screen.width && displayHeight == Screen.height) return;
            displayWidth = Screen.width; displayHeight = Screen.height;
            Shader.SetGlobalVector("_PixelStyleDisplay", new Vector4(displayWidth, displayHeight, 0, 0));
        }
        void Prepare()
        {
            if (!active || frame == Time.frameCount) return;
            frame = Time.frameCount; DisplaySize();
            if (Time.unscaledTime >= refreshAt) RefreshCanvases();
            foreach (var canvas in canvases)
            {
                if (!canvas) continue;
                var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
                for (int i = 0; i < graphics.Count; i++) PrepareGraphic(graphics[i]);
            }
        }
        void PrepareGraphic(Graphic graphic)
        {
            if (!graphic) return;
            if (graphic is Text)
            {
                // Legacy Text shares uGUI's default; explicitly retain its original native shader.
                if (graphic.material == pixel) graphic.material = original;
                return;
            }
            if (graphic is TMP_Text || graphic is TMP_SubMeshUI) return;
            var material = graphic.material;
            if (!material || material == pixel || material.shader.name != "UI/Default") return;
            // Preserve explicitly assigned UI/Default variants as well as the shared-default path.
            if (!variants.TryGetValue(material, out var replacement))
            {
                replacement = new Material(pixel) { name = material.name + " (Pixel Style)", hideFlags = HideFlags.HideAndDontSave };
                replacement.CopyPropertiesFromMaterial(material); variants.Add(material, replacement);
            }
            bindings[graphic] = material;
            graphic.material = replacement;
        }
        public void Restore()
        {
            if (!active) return; active = false;
            Canvas.preWillRenderCanvases -= Prepare; SceneManager.sceneLoaded -= SceneLoaded;
            Shader.SetGlobalFloat("_PixelStyleUI", 0);
            GraphicDefaults.Set(original);
            foreach (var binding in bindings)
                if (binding.Key && variants.TryGetValue(binding.Value, out var owned) && binding.Key.material == owned) binding.Key.material = binding.Value;
            if (canvases != null) foreach (var canvas in canvases)
                if (canvas) foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true)) if (graphic) graphic.SetMaterialDirty();
            foreach (var material in variants.Values) if (material) Destroy(material);
            variants.Clear(); bindings.Clear();
            if (pixel) Destroy(pixel);
            if (instance == this) instance = null;
        }
        void OnDisable() { Restore(); }
        void OnDestroy() { Restore(); }
    }
}
