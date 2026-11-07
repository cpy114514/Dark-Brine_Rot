using UnityEngine;

namespace Mavis
{
    // Independent of resolution, URP quality and gameplay. Loaded even before a menu exists.
    public static class CombatVfxSettings
    {
        public enum Quality { Low, Medium, High }
        const string Prefix="DarkBrine.Settings.";
        static bool loaded;
        static Quality quality;
        static int cameraMotion,screenEffects;
        public static Quality Current { get { Ensure();return quality; } }
        public static int CameraMotion { get { Ensure();return cameraMotion; } }
        public static int ScreenEffects { get { Ensure();return screenEffects; } }
        public static float CameraScale => CameraMotion==0?0:CameraMotion==1?.35f:1;
        public static float ScreenScale => ScreenEffects==0?0:ScreenEffects==1?.35f:1;
        public static int VolumeSteps(int authored) => Mathf.Min(authored,Current==Quality.Low?20:Current==Quality.Medium?28:48);
        public static int DustSteps => Current==Quality.Low?12:Current==Quality.Medium?16:20;
        public static int DustCapacity => Current==Quality.Low?1:Current==Quality.Medium?2:3;
        public static bool SecondarySmoke => Current!=Quality.Low;
        public static bool Refraction => Current==Quality.High && ScreenScale>0;
        public static bool BounceLights => Current!=Quality.Low && ScreenScale>0;
        public static float DetailScale => Current==Quality.Low?.4f:Current==Quality.Medium?.7f:1;
        public static int TrailSubsteps => Current==Quality.Low?3:Current==Quality.Medium?4:6;
        public static float TrailSpacing => Current==Quality.Low?.34f:Current==Quality.Medium?.27f:.22f;
        public static float AmbientDistance => Current==Quality.Low?30:Current==Quality.Medium?55:85;
        public static void Apply(int level,int motion,int effects)
        {quality=(Quality)Mathf.Clamp(level,0,2);cameraMotion=Mathf.Clamp(motion,0,2);screenEffects=Mathf.Clamp(effects,0,2);loaded=true;}
        static void Ensure()
        {
            if(loaded)return;
            Apply(PlayerPrefs.GetInt(Prefix+"VfxQuality",2),PlayerPrefs.GetInt(Prefix+"CameraShake",2),PlayerPrefs.GetInt(Prefix+"ScreenEffects",2));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){loaded=false;}
    }
}
