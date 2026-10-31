using UnityEngine;

namespace Mavis.VisualStyles
{
    // Numeric values and legacy alias retain existing assets, saves and external tools.
    public enum VisualStyleMode { Realistic = 0, DarkFairytale = 1, ColorComic = 2, Monochrome = 3, Origami = ColorComic }

    [CreateAssetMenu(menuName = "Dark Brine/Visual Style Preset")]
    public sealed class VisualStylePreset : ScriptableObject
    {
        public int Revision { get; private set; }
        void OnValidate() { Revision++; }
        public string id = "realistic";
        public string displayName = "Realistic";
        [TextArea] public string description;
        public Texture2D preview;
        public bool illustrated;
        public VisualStyleMode renderingMode;
        // Existing V1 assets retain their original illustrated flag and tuning.
        public VisualStyleMode Mode => renderingMode == VisualStyleMode.Realistic && illustrated ? VisualStyleMode.DarkFairytale : renderingMode;
        public bool Stylized => Mode != VisualStyleMode.Realistic;
        public PaperMaterialProfile paper; // Retained legacy reference; comics do not use paper folds.
        [Tooltip("Ink weight, tone contrast, screentone, shadow ink")]
        public Vector4 comicDetails = new Vector4(.65f, .7f, .4f, .55f);
        public bool paperEnvironment = true;
        [Range(.7f, 1.6f)] public float monochromeContrast = 1.15f;
        [Range(0, .035f)] public float filmGrain = .006f;
        [Range(0, .4f)] public float vignette = .12f;
        [Range(0, 1)] public float shadowIntensity = .45f;
        [Range(0, .15f)] public float highlightBloom = .025f;
        [Range(0, .02f)] public float monochromeDither;
        public Vector4 DefaultDetails
        {
            get
            {
                if (Mode == VisualStyleMode.ColorComic || Mode == VisualStyleMode.Monochrome) return comicDetails;
                return new Vector4((monochromeContrast - .7f) / .9f, filmGrain / .035f, vignette / .4f, shadowIntensity);
            }
        }
        [Range(.5f, 3)] public float outlinePixels = 1.6f;
        [Range(0, 1)] public float outlineStrength = .85f;
        [Range(10, 200)] public float outlineDistance = 65;
        [Range(0, 1)] public float normalEdges = .32f;
        public Color ink = new Color(.055f, .065f, .085f);
        public Color shadows = new Color(.78f, .85f, 1);
        public Color highlights = new Color(1, .94f, .79f);
        [Range(0, 1)] public float saturation = .65f;
        [Range(3, 16)] public float tonalBands = 7;
        [Range(0, .08f)] public float paperGrain = .025f;
        [Range(0, .15f)] public float shadowInk = .045f;
        [Range(0, .5f)] public float atmosphereInfluence = .18f;
        public Color fogTint = new Color(.42f, .48f, .55f);
    }
}
