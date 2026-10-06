using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Runtime foliage detail and batching; authoring objects and source assets stay intact.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class WorldPerformanceManager : MonoBehaviour
    {
        sealed class Foliage
        {
            public MeshRenderer renderer;
            public MeshFilter filter;
            public Mesh original;
            public Material[] materials;
            public FoliageMeshLibrary.Entry detail;
            public Bounds bounds;
            public ShadowCastingMode shadows;
            public bool forced, occlusion, grass, duplicate, legacy;
            public bool batchable;
            public Material drawMaterial;
            public float anchor, inverseHeight, response, stiffness;
        }
        sealed class GrassBatch
        {
            public const int Capacity = 256;
            public Mesh mesh;
            public Material material;
            public int count;
            public readonly Matrix4x4[] matrices = new Matrix4x4[Capacity];
            public readonly float[] anchors = new float[Capacity], heights = new float[Capacity], responses = new float[Capacity], stiffness = new float[Capacity];
            public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        }
        readonly List<Foliage> foliage = new List<Foliage>();
        readonly Dictionary<(Mesh, Material, Vector2Int), List<GrassBatch>> grassBatches = new Dictionary<(Mesh, Material, Vector2Int), List<GrassBatch>>();
        readonly Dictionary<Material, Material> instancedMaterials = new Dictionary<Material, Material>();
        readonly Dictionary<Mesh, FoliageMeshLibrary.Entry> details = new Dictionary<Mesh, FoliageMeshLibrary.Entry>();
        Camera mainCamera;
        float nextUpdate;
        Coroutine cacheRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<WorldPerformanceManager>() != null) return;
            var holder = new GameObject("World Performance Manager");
            holder.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(holder);
            holder.AddComponent<WorldPerformanceManager>();
        }
        void Awake()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            int rate = Mathf.Clamp(PlayerPrefs.GetInt("Mavis.TargetFrameRate", 60), 30, 240);
            int sync = Mathf.Clamp(PlayerPrefs.GetInt("Mavis.VSync", 0), 0, 2);
            QualitySettings.vSyncCount = sync;
            Application.targetFrameRate = sync == 0 ? rate : -1;
            Application.runInBackground = false;
#endif
            var library = Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");
            if (library != null)
                foreach (var entry in library.entries)
                    if (entry.source != null) details[entry.source] = entry;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        void OnEnable() { if (Application.isPlaying) cacheRoutine = StartCoroutine(CacheAfterSceneLoad()); }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!isActiveAndEnabled) return;
            if (cacheRoutine != null) StopCoroutine(cacheRoutine);
            cacheRoutine = StartCoroutine(CacheAfterSceneLoad());
        }
        IEnumerator CacheAfterSceneLoad() { yield return null; CacheWorldRenderers(); cacheRoutine = null; }
        void LateUpdate()
        {
            if (mainCamera == null || !mainCamera.isActiveAndEnabled) mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (Time.unscaledTime >= nextUpdate)
            {
            nextUpdate = Time.unscaledTime + .2f;
            foreach (var groups in grassBatches.Values) foreach (var batch in groups) batch.count = 0;
            Vector3 cameraPosition = mainCamera.transform.position;
            foreach (var item in foliage)
            {
                if (item.renderer == null) continue;
                // Distance to bounds keeps large patches visible until their nearest edge is far away.
                float squared = item.bounds.SqrDistance(cameraPosition);
                float cutoff = item.grass ? 110f : item.legacy ? 260f : 550f;
                bool hidden = item.forced || item.duplicate || squared > cutoff * cutoff;
                bool forced = hidden || item.batchable;
                if (item.renderer.forceRenderingOff != forced) item.renderer.forceRenderingOff = forced;
                if (hidden || item.filter == null) continue;
                if (item.detail != null)
                {
                    float transition = item.grass ? 28f : 65f;
                    Mesh mesh = squared > transition * transition ? item.detail.far : item.detail.near;
                    if (mesh != null && item.filter.sharedMesh != mesh) item.filter.sharedMesh = mesh;
                }
                if (item.batchable && item.renderer.enabled && item.renderer.gameObject.activeInHierarchy) AddToBatch(item);
                // Trees retain nearby shadows; distant crowns skip extra shadow passes.
                if (!item.grass)
                {
                    var shadows = squared > 120f * 120f ? ShadowCastingMode.Off : item.shadows;
                    if (item.renderer.shadowCastingMode != shadows) item.renderer.shadowCastingMode = shadows;
                }
            }
            foreach (var groups in grassBatches.Values) foreach (var batch in groups)
            {
                if (batch.count == 0) continue;
                batch.properties.SetFloatArray("_MavisWindAnchorY", batch.anchors);
                batch.properties.SetFloatArray("_MavisWindInvHeight", batch.heights);
                batch.properties.SetFloatArray("_MavisWindResponse", batch.responses);
                batch.properties.SetFloatArray("_WindTrunkStiffness", batch.stiffness);
            }
            }
            foreach (var groups in grassBatches.Values) foreach (var batch in groups)
                if (batch.count > 0)
                    Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, batch.matrices, batch.count, batch.properties,
                        ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
        }
        void AddToBatch(Foliage item)
        {
            var center = item.bounds.center;
            var key = (item.filter.sharedMesh, item.drawMaterial, new Vector2Int(Mathf.FloorToInt(center.x / 32f), Mathf.FloorToInt(center.z / 32f)));
            if (!grassBatches.TryGetValue(key, out var groups)) grassBatches[key] = groups = new List<GrassBatch>();
            GrassBatch batch = null;
            foreach (var candidate in groups) if (candidate.count < GrassBatch.Capacity) { batch = candidate; break; }
            if (batch == null) { batch = new GrassBatch { mesh=key.Item1,material=key.Item2 }; groups.Add(batch); }
            int index = batch.count++;
            batch.matrices[index] = item.renderer.localToWorldMatrix;
            batch.anchors[index] = item.anchor; batch.heights[index] = item.inverseHeight;
            batch.responses[index] = item.response; batch.stiffness[index] = item.stiffness;
        }
        void CacheWorldRenderers()
        {
            RestoreManagedRenderers();
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || renderer.GetComponentInParent<ProceduralIsland>() != null ||
                    renderer.GetComponentInParent<ProceduralSeabed>() != null) continue;
                details.TryGetValue(mesh, out var detail);
                bool grass = detail != null && detail.grass;
                if (!grass)
                    for (Transform t = renderer.transform; t != null; t = t.parent)
                    {
                        string name = t.name.ToLowerInvariant();
                        if (name.Contains("grass") || name.StartsWith("rostlinka")) { grass = true; break; }
                    }
                bool duplicate = renderer.name.Contains("geometry_nodes");
                bool legacy = renderer.name.StartsWith("20260918140331_c8541e3e");
                if (!grass && detail == null && !duplicate && !legacy) continue;
                var item = new Foliage { renderer=renderer,filter=filter,original=mesh,detail=detail,grass=grass,
                    duplicate=duplicate,legacy=legacy,bounds=renderer.bounds,materials=renderer.sharedMaterials,
                    shadows=renderer.shadowCastingMode,forced=renderer.forceRenderingOff,occlusion=renderer.allowOcclusionWhenDynamic };
                foliage.Add(item);
                renderer.allowOcclusionWhenDynamic = true;
                if (grass) renderer.shadowCastingMode = ShadowCastingMode.Off;
                var materials = (Material[])item.materials.Clone();
                bool changed = false;
                for (int i=0;i<materials.Length;i++)
                {
                    var material=materials[i];
                    if(material==null || (material.shader.name!="Mavis/FoliageWind" &&
                        !(grass && material.shader.name=="Universal Render Pipeline/Lit")))continue;
                    if(!instancedMaterials.TryGetValue(material,out var instance))
                    {
                        instance=new Material(material){name=material.name+" (Runtime Instanced)",hideFlags=HideFlags.DontSave,enableInstancing=true};
                        instancedMaterials.Add(material,instance);
                    }
                    materials[i]=instance;changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
                item.batchable = grass && SystemInfo.supportsInstancing && materials.Length == 1 && materials[0] != null && materials[0].enableInstancing &&
                    (materials[0].shader.name == "Mavis/FoliageWind" || materials[0].shader.name == "Universal Render Pipeline/Lit");
                if (item.batchable)
                {
                    item.drawMaterial=materials[0];
                    var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                    item.anchor=block.GetFloat("_MavisWindAnchorY");item.inverseHeight=block.GetFloat("_MavisWindInvHeight");
                    item.response=block.GetFloat("_MavisWindResponse");item.stiffness=block.GetFloat("_WindTrunkStiffness");
                }
            }
            nextUpdate=0;
        }
        void RestoreManagedRenderers()
        {
            foreach(var item in foliage)
            {
                if(item.renderer==null)continue;
                item.renderer.forceRenderingOff=item.forced;
                item.renderer.allowOcclusionWhenDynamic=item.occlusion;
                item.renderer.shadowCastingMode=item.shadows;
                item.renderer.sharedMaterials=item.materials;
                if(item.filter!=null)item.filter.sharedMesh=item.original;
            }
            foliage.Clear();
            grassBatches.Clear();
            foreach(var material in instancedMaterials.Values)
                if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            instancedMaterials.Clear();
        }
        void OnDisable()
        {
            if (cacheRoutine != null) StopCoroutine(cacheRoutine);
            cacheRoutine = null;
            RestoreManagedRenderers();
        }
        void OnDestroy() { SceneManager.sceneLoaded-=OnSceneLoaded; RestoreManagedRenderers(); }
    }
}
