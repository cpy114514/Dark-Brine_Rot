using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>
    /// Applies conservative runtime culling to the unusually dense imported foliage.
    /// It does not alter scenes or assets: authoring renderer states are preserved and
    /// only Renderer.forceRenderingOff is changed while the game is running.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class WorldPerformanceManager : MonoBehaviour
    {
        const string TargetFrameRateKey = "Mavis.TargetFrameRate";
        const string VSyncKey = "Mavis.VSync";

        const float LegacyTreeDistance = 240f;
        const float GrassDistance = 105f;
        const float UpdateInterval = 0.25f;

        readonly List<Renderer> legacyTrees = new List<Renderer>();
        readonly List<Renderer> grassRenderers = new List<Renderer>();
        readonly List<Renderer> managedRenderers = new List<Renderer>();

        Camera mainCamera;
        float nextUpdate;
        Coroutine cacheRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<WorldPerformanceManager>() != null)
                return;

            var managerObject = new GameObject("World Performance Manager");
            managerObject.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(managerObject);
            managerObject.AddComponent<WorldPerformanceManager>();
        }

        void Awake()
        {
            ConfigureWindowsRuntime();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        IEnumerator Start()
        {
            yield return null;
            CacheWorldRenderers();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreManagedRenderers();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (cacheRoutine != null)
                StopCoroutine(cacheRoutine);
            cacheRoutine = StartCoroutine(CacheAfterSceneLoad());
        }

        IEnumerator CacheAfterSceneLoad()
        {
            yield return null;
            CacheWorldRenderers();
            cacheRoutine = null;
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < nextUpdate)
                return;

            nextUpdate = Time.unscaledTime + UpdateInterval;
            if (mainCamera == null || !mainCamera.isActiveAndEnabled)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            Vector3 cameraPosition = mainCamera.transform.position;
            ApplyDistanceCulling(legacyTrees, cameraPosition, LegacyTreeDistance * LegacyTreeDistance);
            ApplyDistanceCulling(grassRenderers, cameraPosition, GrassDistance * GrassDistance);
        }

        void CacheWorldRenderers()
        {
            RestoreManagedRenderers();
            legacyTrees.Clear();
            grassRenderers.Clear();
            managedRenderers.Clear();

            foreach (MeshRenderer renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null)
                    continue;

                // These imported trees have no LODGroup and each contains 1.5M triangles.
                // Fog already hides their fine detail at this range, so avoid submitting them.
                if (renderer.name.StartsWith("20260918140331_c8541e3e"))
                {
                    AddManaged(renderer, legacyTrees);
                    renderer.allowOcclusionWhenDynamic = true;
                    continue;
                }

                // The grass pack is made from hundreds of small renderers. Unity still
                // frustum-culls them normally; this adds the missing distance cutoff.
                if (mesh.name == "Forest001" || mesh.name == "rostlinka_7c_scater")
                {
                    AddManaged(renderer, grassRenderers);
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.allowOcclusionWhenDynamic = true;
                }
            }

            // Imported geometry_nodes meshes duplicate the visible LOD0 tree geometry.
            // Keep them suppressed without changing prefab or scene serialization.
            foreach (MeshFilter filter in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!filter.name.Contains("geometry_nodes"))
                    continue;

                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.forceRenderingOff = true;
            }
        }

        void AddManaged(Renderer renderer, List<Renderer> category)
        {
            category.Add(renderer);
            managedRenderers.Add(renderer);
        }

        static void ApplyDistanceCulling(List<Renderer> renderers, Vector3 cameraPosition, float maximumDistanceSquared)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                renderer.forceRenderingOff = (renderer.bounds.center - cameraPosition).sqrMagnitude > maximumDistanceSquared;
            }
        }

        void RestoreManagedRenderers()
        {
            foreach (Renderer renderer in managedRenderers)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }
        }

        static void ConfigureWindowsRuntime()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            int targetFrameRate = Mathf.Clamp(PlayerPrefs.GetInt(TargetFrameRateKey, 60), 30, 240);
            int vSync = Mathf.Clamp(PlayerPrefs.GetInt(VSyncKey, 0), 0, 2);
            QualitySettings.vSyncCount = vSync;
            Application.targetFrameRate = vSync == 0 ? targetFrameRate : -1;
            Application.runInBackground = false;
#endif
        }
    }
}
