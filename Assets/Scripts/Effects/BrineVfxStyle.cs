using UnityEngine;

namespace Mavis
{
    [CreateAssetMenu(menuName="Dark Brine/Brine VFX Style")]
    public sealed class BrineVfxStyle : ScriptableObject
    {
        public Material surface, stone, blast;
        public Material smokeDetail;
        [Range(0,1)]public float smokeDetailOpacity=.55f;
        [Range(20,48)]public int volumeSteps=36;
        public Color body = new Color(.18f,.165f,.14f,1);
        [ColorUsage(false,true)] public Color edge = new Color(.72f,.62f,.44f,1);
        public Color contact = new Color(.55f,.34f,.13f,1);
        [ColorUsage(false,true)]public Color fireCore=new Color(1.15f,.65f,.23f,1);
        [ColorUsage(false,true)]public Color fireBody=new Color(.55f,.19f,.025f,1);
        public Color smoke=new Color(.095f,.085f,.072f,1);
        [Range(.2f,1.5f)] public float visibility=.85f;
        [Range(.5f,2)] public float scale=1;
        public bool heatRefraction=true;
        [Range(0,3)] public float heatRefractionPixels=1.5f;
        [Range(30,150)] public float maximumDistance=85;
        [Range(8,32)] public int contactCapacity=16;
        [Range(2,12)] public int groundCapacity=6;
        public static BrineVfxStyle Load() => Resources.Load<BrineVfxStyle>("Combat/BrineVfxStyle");
    }
}
