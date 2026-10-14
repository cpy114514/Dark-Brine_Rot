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
            public Mesh currentMesh;
            public Material[] materials;
            public FoliageMeshLibrary.Entry detail;
            public Bounds bounds;
            public ShadowCastingMode shadows;
            public ShadowCastingMode currentShadows;
            public bool forced, occlusion, grass, duplicate, legacy;
            public bool batchable;
            public bool renderOff;
            public Material drawMaterial;
            public float anchor, inverseHeight, response, stiffness;
            public Vector2Int batchCell;
            public Matrix4x4 staticMatrix;
            public bool staticTransform;
            public Mesh groupMesh;
            public GrassGroup group;
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
            public readonly Foliage[] occupants = new Foliage[Capacity];
            public bool propertiesDirty;
        }
        sealed class GrassGroup
        {
            public readonly List<GrassBatch> batches = new List<GrassBatch>();
            public int cursor;
        }
        sealed class FoliageCell
        {
            public readonly List<Foliage> items = new List<Foliage>();
            public Bounds bounds;
            public float cutoff;
            public bool culled;
        }
        readonly List<Foliage> foliage = new List<Foliage>();
        readonly List<FoliageCell> foliageCells = new List<FoliageCell>();
        readonly Dictionary<(Mesh, Material, Vector2Int), GrassGroup> grassBatches = new Dictionary<(Mesh, Material, Vector2Int), GrassGroup>();
        readonly Dictionary<Material, Material> instancedMaterials = new Dictionary<Material, Material>();
        readonly Dictionary<Mesh, FoliageMeshLibrary.Entry> details = new Dictionary<Mesh, FoliageMeshLibrary.Entry>();
        readonly List<GrassBatch> activeGrassBatches = new List<GrassBatch>();
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
        IEnumerator CacheAfterSceneLoad()
        {
            yield return null;
            while(AdditiveSceneBootstrap.IsLoading)yield return null;
            // Batching must read completed per-renderer wind profiles.
            var windDrivers=FindObjectsByType<FoliageWindDriver>(FindObjectsSortMode.None);
            bool windReady;
            do
            {
                yield return null;windReady=true;
                foreach(var driver in windDrivers)if(driver!=null && driver.isActiveAndEnabled && !driver.ProfilesReady){windReady=false;break;}
            }while(!windReady);
            yield return CacheWorldRenderers(); cacheRoutine = null;
        }
        void LateUpdate()
        {
            if(AdditiveSceneBootstrap.IsLoading || IslandMapUI.IsOpen)return;
            if (mainCamera == null || !mainCamera.isActiveAndEnabled) mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (Time.unscaledTime >= nextUpdate)
            {
                nextUpdate = Time.unscaledTime + .2f;
                RefreshFoliage(mainCamera.transform.position);
            }
            foreach (var batch in activeGrassBatches)
                if (batch.count > 0)
                    Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, batch.matrices, batch.count, batch.properties,
                        ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off);
        }
        void RefreshFoliage(Vector3 cameraPosition)
        {
            foreach (var batch in activeGrassBatches) batch.count = 0;
            foreach (var group in grassBatches.Values) group.cursor = 0;
            activeGrassBatches.Clear();
            foreach (var cell in foliageCells)
            {
                // Distant grass cells stay hidden until their nearest edge returns.
                // Avoid visiting thousands of already culled renderers every refresh.
                if (SquaredDistance(cameraPosition, cell.bounds) > cell.cutoff * cell.cutoff)
                {
                    if (!cell.culled)
                        foreach (var item in cell.items)
                            if (!item.renderOff && item.renderer != null)
                            { item.renderer.forceRenderingOff = true; item.renderOff = true; }
                    cell.culled = true;
                    continue;
                }
                cell.culled = false;
                foreach (var item in cell.items)
                {
                // Distance to bounds keeps large patches visible until their nearest edge is far away.
                float squared = SquaredDistance(cameraPosition, item.bounds);
                float cutoff = item.grass ? 110f : item.legacy ? 260f : 550f;
                bool hidden = item.forced || item.duplicate || squared > cutoff * cutoff;
                bool forced = hidden || item.batchable;
                if (item.renderOff != forced && item.renderer != null)
                {item.renderer.forceRenderingOff=forced;item.renderOff=forced;}
                if (hidden) continue;
                if (item.renderer == null) continue;
                if (item.filter == null) continue;
                if (item.detail != null)
                {
                    float transition = item.grass ? 28f : 65f;
                    Mesh mesh = item.detail.fullDetailDistance>0 && squared < item.detail.fullDetailDistance*item.detail.fullDetailDistance
                        ? item.detail.close != null ? item.detail.close : item.original
                        : squared > transition * transition ? item.detail.far : item.detail.near;
                    if (mesh != null && item.currentMesh != mesh)
                    { item.filter.sharedMesh = mesh; item.currentMesh = mesh; }
                }
                if (item.batchable && item.renderer.enabled && item.renderer.gameObject.activeInHierarchy) AddToBatch(item);
                // Trees retain nearby shadows; distant crowns skip extra shadow passes.
                if (!item.grass)
                {
                    var shadows = squared > 120f * 120f ? ShadowCastingMode.Off : item.shadows;
                    if (item.currentShadows != shadows)
                    { item.renderer.shadowCastingMode = shadows; item.currentShadows = shadows; }
                }
                }
            }
            foreach (var batch in activeGrassBatches)
            {
                if (batch.count == 0 || !batch.propertiesDirty) continue;
                batch.properties.SetFloatArray("_MavisWindAnchorY", batch.anchors);
                batch.properties.SetFloatArray("_MavisWindInvHeight", batch.heights);
                batch.properties.SetFloatArray("_MavisWindResponse", batch.responses);
                batch.properties.SetFloatArray("_WindTrunkStiffness", batch.stiffness);
                batch.propertiesDirty = false;
            }
        }
        static float SquaredDistance(Vector3 position, Bounds bounds)
        {
            var center = bounds.center; var extent = bounds.extents;
            float x = Mathf.Max(0, Mathf.Abs(position.x - center.x) - extent.x);
            float y = Mathf.Max(0, Mathf.Abs(position.y - center.y) - extent.y);
            float z = Mathf.Max(0, Mathf.Abs(position.z - center.z) - extent.z);
            return x * x + y * y + z * z;
        }
        void AddToBatch(Foliage item)
        {
            if (item.group == null || item.groupMesh != item.currentMesh)
            {
                var key = (item.currentMesh, item.drawMaterial, item.batchCell);
                if (!grassBatches.TryGetValue(key, out var group)) grassBatches[key] = group = new GrassGroup();
                item.group = group; item.groupMesh = item.currentMesh;
            }
            var groups = item.group;
            if (groups.cursor < groups.batches.Count && groups.batches[groups.cursor].count == GrassBatch.Capacity) groups.cursor++;
            if (groups.cursor == groups.batches.Count)
                groups.batches.Add(new GrassBatch { mesh=item.currentMesh,material=item.drawMaterial });
            var batch = groups.batches[groups.cursor];
            if(batch.count==0)activeGrassBatches.Add(batch);
            int index = batch.count++;
            if (batch.occupants[index] != item)
            {
                batch.occupants[index] = item;
                batch.matrices[index] = item.staticTransform ? item.staticMatrix : item.renderer.localToWorldMatrix;
                batch.anchors[index] = item.anchor; batch.heights[index] = item.inverseHeight;
                batch.responses[index] = item.response; batch.stiffness[index] = item.stiffness;
                batch.propertiesDirty = true;
            }
            else if (!item.staticTransform) batch.matrices[index] = item.renderer.localToWorldMatrix;
        }
        IEnumerator CacheWorldRenderers()
        {
            RestoreManagedRenderers();
            var grassCells = new Dictionary<Vector2Int, FoliageCell>();
            var block=new MaterialPropertyBlock();
            double deadline=Time.realtimeSinceStartupAsDouble+.002;
            int visited=0;
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                // Bound initialization work so entering the island does not
                // process every plant and material in one rendered frame.
                if(++visited%32==0 && Time.realtimeSinceStartupAsDouble>=deadline)
                {yield return null;deadline=Time.realtimeSinceStartupAsDouble+.002;}
                if(renderer==null)continue;
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
                var item = new Foliage { renderer=renderer,filter=filter,original=mesh,currentMesh=mesh,detail=detail,grass=grass,
                    duplicate=duplicate,legacy=legacy,bounds=renderer.bounds,materials=renderer.sharedMaterials,
                    shadows=renderer.shadowCastingMode,currentShadows=renderer.shadowCastingMode,forced=renderer.forceRenderingOff,renderOff=renderer.forceRenderingOff,occlusion=renderer.allowOcclusionWhenDynamic,
                    staticTransform=renderer.gameObject.isStatic,staticMatrix=renderer.localToWorldMatrix,
                    batchCell=new Vector2Int(Mathf.FloorToInt(renderer.bounds.center.x / 32f),Mathf.FloorToInt(renderer.bounds.center.z / 32f)) };
                foliage.Add(item);
                FoliageCell cell;
                if (grass)
                {
                    var center = item.bounds.center;
                    var key = new Vector2Int(Mathf.FloorToInt(center.x / 32f), Mathf.FloorToInt(center.z / 32f));
                    if (!grassCells.TryGetValue(key, out cell))
                    {
                        cell = new FoliageCell { bounds = item.bounds, cutoff = 110f };
                        grassCells.Add(key, cell); foliageCells.Add(cell);
                    }
                    else cell.bounds.Encapsulate(item.bounds);
                }
                else
                {
                    cell = new FoliageCell { bounds = item.bounds, cutoff = legacy ? 260f : 550f };
                    foliageCells.Add(cell);
                }
                cell.items.Add(item);
                renderer.allowOcclusionWhenDynamic = true;
                if (grass) { renderer.shadowCastingMode = ShadowCastingMode.Off; item.currentShadows = ShadowCastingMode.Off; }
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
                    block.Clear(); renderer.GetPropertyBlock(block);
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
            foliageCells.Clear();
            grassBatches.Clear();
            activeGrassBatches.Clear();
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
