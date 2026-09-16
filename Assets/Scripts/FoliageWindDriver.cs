// FoliageWindDriver.cs
// Per-frame pushes global wind state to Mavis/FoliageWind shader.
// Designed to be added once to the scene; reads WindZone (if any) for direction/strength.
using UnityEngine;

namespace Mavis
{
    [ExecuteAlways]
    public class FoliageWindDriver : MonoBehaviour
    {
        public WindZone windZone;

        [Header("Wind override (used if no WindZone)")]
        public Vector3 baseDirection = new Vector3(0.7f, 0, 0.7f);
        public float baseStrength = 1.0f;
        [Range(0f, 4f)] public float frequency = 1.0f;
        [Range(0f, 2f)] public float gustiness = 0.6f;

        void Update()
        {
            Vector3 dir = baseDirection;
            float strength = baseStrength;
            float main = 1.0f;

            if (windZone != null)
            {
                dir = windZone.transform.forward;
                strength = windZone.windMain;
                main = windZone.windPulseFrequency;
            }

            dir = dir.sqrMagnitude < 1e-4f ? Vector3.right : dir.normalized;

            Shader.SetGlobalVector("_MavisWindDir",
                new Vector4(dir.x, dir.y, dir.z, Time.timeSinceLevelLoad));
            Shader.SetGlobalVector("_MavisWindParams",
                new Vector4(strength * baseStrength, main, 0f, gustiness));
        }
    }
}