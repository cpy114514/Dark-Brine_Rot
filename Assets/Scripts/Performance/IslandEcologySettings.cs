using UnityEngine;

namespace Mavis
{
    [CreateAssetMenu(menuName="Mavis/Island ecology settings")]
    public sealed class IslandEcologySettings : ScriptableObject
    {
        [System.Serializable] public struct Clearing {public string reason;public Vector2 center;public float radius;}
        [System.Serializable] public struct Corridor {public string reason;public Vector2[] points;public float halfWidth;}
        public int seed=740311;
        [Min(5)] public float clusterSpacing=13;
        [Range(0,1)] public float clusterDensity=.78f;
        [Range(0,1)] public float uncannyFraction=.20f;
        [Range(.5f,1)] public float minimumGroundNormalY=.83f;
        [Min(0)] public float shorelineMargin=1.2f;
        public Vector2 sampleCenter=new Vector2(251,469);
        public float sampleRadius=68;
        [Range(0,1)] public float meadowGrassRetention=.90f;
        [Range(0,1)] public float woodlandGrassRetention=.38f;
        public Clearing[] clearings;
        public Corridor[] corridors;
    }
}
