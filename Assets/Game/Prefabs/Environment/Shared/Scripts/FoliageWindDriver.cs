// FoliageWindDriver.cs
// Per-frame pushes global wind state to Mavis/FoliageWind shader.
// Designed to be added once to the scene; reads WindZone (if any) for direction/strength.
using System.Collections.Generic;
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

        [Header("Natural response")]
        [Tooltip("Final wind displacement in world units. This keeps a WindZone value of 1 from moving whole trees by metres.")]
        [Range(0f, 1f)] public float globalResponse = 0.55f;

        sealed class FoliageRenderer
        {
            public Renderer renderer;
            public float anchorY;
            public float inverseHeight;
            public float response;
        }

        static readonly int AnchorYId = Shader.PropertyToID("_MavisWindAnchorY");
        static readonly int InverseHeightId = Shader.PropertyToID("_MavisWindInvHeight");
        static readonly int ResponseId = Shader.PropertyToID("_MavisWindResponse");
        static readonly int RootStiffnessId = Shader.PropertyToID("_WindTrunkStiffness");
        readonly List<FoliageRenderer> foliage = new List<FoliageRenderer>();
        MaterialPropertyBlock propertyBlock;

        void OnEnable()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (windZone == null)
                windZone = FindFirstObjectByType<WindZone>();
            CacheRendererProfiles();
        }

        void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                if (propertyBlock == null)
                    propertyBlock = new MaterialPropertyBlock();
                CacheRendererProfiles();
            }
        }

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
                new Vector4(strength * baseStrength * globalResponse, main, 0f, gustiness));
        }

        void CacheRendererProfiles()
        {
            foliage.Clear();
            // Include inactive LOD renderers so they already have their wind profile
            // when a LODGroup activates them later.
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!UsesFoliageShader(renderer))
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.size.y < 0.001f)
                    continue;

                var entry = new FoliageRenderer
                {
                    renderer = renderer,
                    anchorY = bounds.min.y,
                    inverseHeight = 1f / bounds.size.y,
                    response = GetResponse(renderer)
                };
                foliage.Add(entry);
                ApplyProfile(entry);
            }
        }

        static bool UsesFoliageShader(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && material.shader != null && material.shader.name == "Mavis/FoliageWind")
                    return true;
            return false;
        }

        static float GetResponse(Renderer renderer)
        {
            string name = renderer.name.ToLowerInvariant();
            bool hasLeaves = false;
            bool hasBranches = false;
            bool hasBark = false;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                string materialName = material.name.ToLowerInvariant();
                hasLeaves |= materialName.Contains("leaf") || materialName.Contains("leaves");
                hasBranches |= materialName.Contains("branch");
                hasBark |= materialName.Contains("bark") || materialName.Contains("trunk");
            }

            if (name.Contains("geometry_nodes") || name.Contains("trunk") || (hasBark && !hasLeaves && !hasBranches)) return 0.12f;
            if (name.Contains("leaf") || hasLeaves) return 0.86f;
            if (name.Contains("branch") || hasBranches) return 0.38f;
            if (name.Contains("rostlinka") || name.Contains("forest") || name.Contains("grass")) return 1.00f;
            if (name.Contains("tree")) return 0.70f;
            return 0.52f;
        }

        void ApplyProfile(FoliageRenderer entry)
        {
            if (entry.renderer == null)
                return;
            entry.renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(AnchorYId, entry.anchorY);
            propertyBlock.SetFloat(InverseHeightId, entry.inverseHeight);
            propertyBlock.SetFloat(ResponseId, entry.response);
            propertyBlock.SetFloat(RootStiffnessId, GetRootStiffness(entry.response));
            entry.renderer.SetPropertyBlock(propertyBlock);
        }

        static float GetRootStiffness(float response)
        {
            if (response <= 0.15f) return 0.94f;
            if (response <= 0.40f) return 0.66f;
            if (response >= 0.75f) return 0.18f;
            return 0.34f;
        }
    }
}
