using UnityEngine;
namespace Mavis.VisualStyles
{
    public static class VisualStyleSettings
    {
        public const string StyleKey = "DarkBrine.VisualStyle.Id";
        public const string IntensityKey = "DarkBrine.VisualStyle.Intensity";
        public static string SelectedId => PlayerPrefs.GetString(StyleKey, "realistic");
        public static float Intensity => Mathf.Clamp(PlayerPrefs.GetFloat(IntensityKey, .85f), .4f, 1);
        public static Vector4 Details(VisualStylePreset preset)
        {
            var defaults = preset.DefaultDetails;
            for (int i = 0; i < 4; i++) defaults[i] = Mathf.Clamp01(PlayerPrefs.GetFloat("DarkBrine.VisualStyle." + preset.id + ".Detail" + i, defaults[i]));
            return defaults;
        }
        public static void SaveDetails(string id, Vector4 details)
        {
            for (int i = 0; i < 4; i++) PlayerPrefs.SetFloat("DarkBrine.VisualStyle." + id + ".Detail" + i, Mathf.Clamp01(details[i]));
            PlayerPrefs.Save();
        }
        public static void Save(string id, float intensity)
        {
            PlayerPrefs.SetString(StyleKey, id);
            PlayerPrefs.SetFloat(IntensityKey, Mathf.Clamp(intensity, .4f, 1));
            PlayerPrefs.Save();
        }
    }
}
