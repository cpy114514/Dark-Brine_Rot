using UnityEngine;

namespace Mavis.VisualStyles
{
    [CreateAssetMenu(menuName = "Dark Brine/Visual Style Preset")]
    public sealed class VisualStylePreset : ScriptableObject
    {
        public string id = "realistic";
        public string displayName = "Realistic";
        [TextArea] public string description;
        public Texture2D preview;
        public bool illustrated;
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
