using UnityEngine;

namespace Mavis.VisualStyles
{
    [CreateAssetMenu(menuName="Dark Brine/Van Gogh painting")]
    public sealed class VanGoghProfile : ScriptableObject
    {
        public enum Quality { Economy, Balanced, Fine }
        public Quality quality = Quality.Balanced;
        public int Revision {get;private set;}
        void OnValidate(){Revision++;}
        [Tooltip("Approximate world-space width of the broad pigment marks, in metres.")]
        [Range(.06f,1.2f)] public float brushSize=.32f;
        [Range(0,1)] public float density=.82f;
        [Range(0,1)] public float directionStrength=.8f;
        [Range(0,1)] public float curvature=.35f;
        [Range(0,1)] public float colorAbstraction=.65f;
        [Range(0,1)] public float colorContrast=.45f;
        [Range(0,1)] public float impasto=.38f;
        [Range(0,1)] public float outline=.28f;
        [Tooltip("Favours stable surface flow over transient image gradients. No history accumulation or motion-vector ghosting.")]
        [Range(0,1)] public float temporalStability=.9f;
        [Range(0,1)] public float intensity=1;
        [System.NonSerialized] static VanGoghProfile cached;
        public static VanGoghProfile Default => cached ? cached : cached=Resources.Load<VanGoghProfile>("VisualStyles/VanGoghPainting");
    }

    public static class VanGoghPainting
    {
        public static Vector3 LastGpuMilliseconds {get;internal set;}
        public static long WorkingTextureBytes {get;internal set;}
        public static int VisibleSkinDraws {get;internal set;}
        static int appliedRevision=-1;
        public static void RefreshIfChanged(VisualStyleManager manager)
        {if(Active&&appliedRevision!=VanGoghProfile.Default.Revision)Apply(manager);}
        // Kept for controlled same-frame legacy comparisons, never saved as a player preference.
        public static bool LegacyComparison { get; set; }
        public static bool Active => Application.isPlaying && !LegacyComparison && VisualStyleManager.IsIllustrated &&
            VisualStyleManager.Instance.Current.Mode==VisualStyleMode.VanGogh && VanGoghProfile.Default;
        public static void Apply(VisualStyleManager manager)
        {
            bool active=Active;
            if(active)
            {
                var p=VanGoghProfile.Default;var d=manager.Details;
                appliedRevision=p.Revision;
                Shader.SetGlobalVector("_VGBrush",new Vector4(p.brushSize*Mathf.Lerp(.55f,1.65f,d.x),p.density,p.directionStrength,p.curvature*Mathf.Lerp(.3f,1.7f,d.y)));
                Shader.SetGlobalVector("_VGPigment",new Vector4(p.colorAbstraction,p.colorContrast,p.impasto*Mathf.Lerp(.2f,2,d.z),p.outline));
                Shader.SetGlobalVector("_VGControl",new Vector4(p.temporalStability,p.intensity*manager.Intensity,Mathf.Lerp(.55f,1.45f,d.w),(int)p.quality));
                Shader.EnableKeyword("VAN_GOGH_SURFACE");
            }
            else Shader.DisableKeyword("VAN_GOGH_SURFACE");
        }
        public static void Reset(){LegacyComparison=false;Shader.DisableKeyword("VAN_GOGH_SURFACE");}
    }
}
