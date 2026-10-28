using UnityEngine;
namespace Mavis.VisualStyles
{
    public static class VisualStyleSettings
    {
        public const string StyleKey = "DarkBrine.VisualStyle.Id";
        public const string IntensityKey = "DarkBrine.VisualStyle.Intensity";
        public static string SelectedId => PlayerPrefs.GetString(StyleKey, "realistic");
        public static float Intensity => Mathf.Clamp(PlayerPrefs.GetFloat(IntensityKey, .85f), .4f, 1);
        public static void Save(string id, float intensity)
        {
            PlayerPrefs.SetString(StyleKey, id);
            PlayerPrefs.SetFloat(IntensityKey, Mathf.Clamp(intensity, .4f, 1));
            PlayerPrefs.Save();
        }
    }
}
