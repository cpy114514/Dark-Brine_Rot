using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Profiling;

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
            public Mesh alternateMesh;
            public GrassGroup alternateGroup;
            public object groupOwner;
            public bool compactWind;
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
            public Bounds bounds;
            public float maximumResponse;
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
            public bool grass;
        }
        [System.Serializable] sealed class GrassLodState
        {
            public int id;
            public LODFadeMode mode;
            public bool animated;
            public float[] widths;
            public float[] heights;
            public int[] rendererCounts, renderers;
            public bool[] enabled;
            [System.NonSerialized] public LOD[] levels;
        }
#if UNITY_EDITOR
        // Native renderer overrides can outlive the managed batching graph. Keep
        // original instance IDs outside Unity's object serialization until recache.
        [System.Serializable] sealed class ReloadRenderer
        {
            public int renderer, filter, mesh;
            public int[] materials;
            public bool forced, occlusion;
            public ShadowCastingMode shadows;
        }
        [System.Serializable] sealed class ReloadRenderers
        {
            public List<ReloadRenderer> items = new List<ReloadRenderer>();
            public List<GrassLodState> lods = new List<GrassLodState>();
        }
        string ReloadKey => "Mavis.WorldPerformance.Reload." + GetInstanceID();
        void CaptureReloadRenderers()
        {
            var snapshot = new ReloadRenderers();
            foreach (var state in grassLods.Values) snapshot.lods.Add(state);
            foreach (var item in foliage)
            {
                if (!item.renderer) continue;
                var materials = new int[item.materials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = item.materials[i] ? item.materials[i].GetInstanceID() : 0;
                snapshot.items.Add(new ReloadRenderer { renderer = item.renderer.GetInstanceID(),
                    filter = item.filter ? item.filter.GetInstanceID() : 0, mesh = item.original ? item.original.GetInstanceID() : 0,
                    materials = materials, forced = item.forced, occlusion = item.occlusion, shadows = item.shadows });
            }
            UnityEditor.SessionState.SetString(ReloadKey, JsonUtility.ToJson(snapshot));
        }
        Dictionary<int, ReloadRenderer> ReadReloadRenderers()
        {
            string json = UnityEditor.SessionState.GetString(ReloadKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            var result = new Dictionary<int, ReloadRenderer>();
            var snapshot = JsonUtility.FromJson<ReloadRenderers>(json);
            if (snapshot.lods != null)
                foreach (var state in snapshot.lods)
                {
                    if (state.heights != null)
                    {
                        state.levels = new LOD[state.heights.Length];
                        int offset=0;
                        for (int i=0;i<state.levels.Length;i++)
                        {
                            var parts=new Renderer[state.rendererCounts[i]];
                            for (int j=0;j<parts.Length;j++)
                                parts[j]=UnityEditor.EditorUtility.EntityIdToObject(state.renderers[offset++]) as Renderer;
                            state.levels[i]=new LOD(state.heights[i],parts){fadeTransitionWidth=state.widths[i]};
                        }
                    }
                    RestoreGrassLod(UnityEditor.EditorUtility.EntityIdToObject(state.id) as LODGroup, state);
                }
            foreach (var item in snapshot.items) result[item.renderer] = item;
            return result;
        }
        static void RestoreReloadRenderer(ReloadRenderer item, MeshRenderer renderer)
        {
            var materials = new Material[item.materials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = UnityEditor.EditorUtility.EntityIdToObject(item.materials[i]) as Material;
            renderer.forceRenderingOff = item.forced;
            renderer.allowOcclusionWhenDynamic = item.occlusion;
            renderer.shadowCastingMode = item.shadows;
            renderer.sharedMaterials = materials;
            var filter = UnityEditor.EditorUtility.EntityIdToObject(item.filter) as MeshFilter;
            if (filter) filter.sharedMesh = UnityEditor.EditorUtility.EntityIdToObject(item.mesh) as Mesh;
        }
#endif
        readonly List<Foliage> foliage = new List<Foliage>();
        readonly Dictionary<LODGroup, GrassLodState> grassLods = new Dictionary<LODGroup, GrassLodState>();
        readonly List<FoliageCell> foliageCells = new List<FoliageCell>();
        Dictionary<(Mesh, Material, Vector2Int), GrassGroup> grassBatches = new Dictionary<(Mesh, Material, Vector2Int), GrassGroup>();
        Dictionary<(Mesh, Material, Vector2Int), GrassGroup> renderedGrassGroups = new Dictionary<(Mesh, Material, Vector2Int), GrassGroup>();
        readonly Dictionary<Material, Material> instancedMaterials = new Dictionary<Material, Material>();
        readonly Dictionary<Mesh, FoliageMeshLibrary.Entry> details = new Dictionary<Mesh, FoliageMeshLibrary.Entry>();
        readonly Dictionary<Mesh, Mesh> renderMeshes = new Dictionary<Mesh, Mesh>();
        List<GrassBatch> activeGrassBatches = new List<GrassBatch>();
        List<GrassBatch> pendingGrassBatches = new List<GrassBatch>();
        GrassBatch visibleGrass;
        readonly Plane[] grassPlanes = new Plane[6];
        readonly Vector3[] grassPlaneAbsoluteNormals = new Vector3[6];
        static readonly int GrassAnchorId = Shader.PropertyToID("_MavisWindAnchorY");
        static readonly int GrassHeightId = Shader.PropertyToID("_MavisWindInvHeight");
        static readonly int GrassResponseId = Shader.PropertyToID("_MavisWindResponse");
        static readonly int GrassStiffnessId = Shader.PropertyToID("_WindTrunkStiffness");
        readonly List<Vector4> bodyMotion = new List<Vector4>(12);
        readonly Dictionary<Material, Vector2> grassPadding = new Dictionary<Material, Vector2>();
        static readonly ProfilerMarker GrassVisibility = new ProfilerMarker("World.GrassVisibility");
        static readonly ProfilerMarker GrassSubmit = new ProfilerMarker("World.GrassSubmit");
        // Diagnostic A/B control only; normal sessions always use the adaptive policy.
        internal static bool ForceFixedInitializationBudget;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInitializationControl() => ForceFixedInitializationBudget = false;
        internal static double InitializationBudget(bool initial, float frameSeconds)
        {
            // A 2ms slice per rendered frame takes minutes when startup is paced
            // slowly. Spend at most 1/16 of that frame, capped at 8ms, only while
            // acquiring a fresh scene. Interactive refreshes retain the 2ms cap.
            return Application.isPlaying && initial && !ForceFixedInitializationBudget
                ? Mathf.Clamp(frameSeconds * .0625f, .002f, .008f) : .002;
        }
        Camera mainCamera;
        float nextUpdate;
        Coroutine cacheRoutine;
        Coroutine foliageRoutine;
        bool foliageRefreshing;
        Camera[] grassCameras = new Camera[4];
        int submittedGrassBatches;
        public bool IsReady => cacheRoutine == null && !foliageRefreshing && foliage.Count > 0;

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
        }
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            // Draw graphs and mesh maps are managed state; recreate them after reload.
            if (visibleGrass == null) visibleGrass = new GrassBatch();
            var submission = GetComponent<GrassRenderSubmission>();
            if (submission == null) submission = gameObject.AddComponent<GrassRenderSubmission>();
            submission.owner = this;
            details.Clear(); renderMeshes.Clear();
            var library = Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");
            if (library != null)
                foreach (var entry in library.entries)
                    if (entry.source != null)
                    {
                        details[entry.source] = entry;
                        if (entry.renderSource != null) renderMeshes[entry.source] = entry.renderSource;
                    }
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= CaptureReloadRenderers;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += CaptureReloadRenderers;
#endif
            cacheRoutine = StartCoroutine(CacheAfterSceneLoad());
        }
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
            if(AdditiveSceneBootstrap.IsLoading || IslandMapUI.IsOpen || cacheRoutine != null)return;
            if (mainCamera == null || !mainCamera.isActiveAndEnabled) mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (Time.unscaledTime >= nextUpdate)
            {
                nextUpdate = Time.unscaledTime + .2f;
                RefreshFoliage(mainCamera.transform.position);
            }
        }
        void RenderGrass(ScriptableRenderContext context, Camera camera)
        {
            if (!isActiveAndEnabled || AdditiveSceneBootstrap.IsLoading || IslandMapUI.IsOpen ||
                activeGrassBatches.Count == 0 || camera == null || (camera.cullingMask & 1) == 0) return;
            // The final camera pose is available here (after camera-follow LateUpdate).
            // Submit only to this camera, so multiple cameras do not duplicate draws.
            GeometryUtility.CalculateFrustumPlanes(camera.cullingMatrix, grassPlanes);
            // Each plane is reused by every batch and intersecting blade bounds.
            // Recompute per camera so camera movement and custom culling matrices stay exact.
            for (int i = 0; i < grassPlanes.Length; i++)
            {
                var normal = grassPlanes[i].normal;
                grassPlaneAbsoluteNormals[i] = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            }
            float wind = Mathf.Abs(Shader.GetGlobalVector("_MavisWindParams").x);
            int count = Shader.GetGlobalInt("_MavisFoliageBodyCount");
            float sumStrength = 0f;
            if (count > 0)
            {
                Shader.GetGlobalVectorArray("_MavisFoliageMotion", bodyMotion);
                for (int i = 0; i < Mathf.Min(count, bodyMotion.Count); i++) sumStrength += Mathf.Abs(bodyMotion[i].z);
            }
            // The shader caps horizontal push at one, while vertical push is
            // bounded by .35 times the sum of active body/footprint strengths.
            float push = count > 0 ? .65f * Mathf.Sqrt(1f + .1225f * sumStrength * sumStrength) : 0f;
            grassPadding.Clear();
            using (GrassVisibility.Auto())
            foreach (var batch in activeGrassBatches)
            {
                if (batch.count == 0) continue;
                if (!grassPadding.TryGetValue(batch.material, out var scale))
                {
                    bool deformed = batch.material.shader.name == "Mavis/FoliageWind";
                    scale = deformed ? new Vector2(
                        Mathf.Abs(batch.material.GetFloat("_WindBend") * batch.material.GetFloat("_LocalWindScale")) * wind * 1.6f,
                        Mathf.Abs(batch.material.GetFloat("_PlayerPushStrength")) * push + .05f) : Vector2.zero;
                    grassPadding.Add(batch.material, scale);
                }
                float padding = scale.x * batch.maximumResponse + scale.y;
                int classification = GrassFrustum(batch.bounds, padding);
                if (classification < 0) continue;
                if (classification > 0) { SubmitGrass(batch, camera); continue; }
                var visible = visibleGrass;
                visible.mesh = batch.mesh; visible.material = batch.material; visible.count = 0;
                for (int i = 0; i < batch.count; i++)
                {
                    if (GrassFrustum(batch.occupants[i].bounds, padding) < 0) continue;
                    int index = visible.count++;
                    visible.matrices[index] = batch.matrices[i]; visible.anchors[index] = batch.anchors[i];
                    visible.heights[index] = batch.heights[i]; visible.responses[index] = batch.responses[i];
                    visible.stiffness[index] = batch.stiffness[i];
                }
                if (visible.count == 0) continue;
                SetGrassProperties(visible);
                SubmitGrass(visible, camera);
            }
        }
        internal void SubmitVisibleGrass()
        {
            submittedGrassBatches = 0;
            int count = Camera.allCamerasCount;
            if (grassCameras.Length < count) grassCameras = new Camera[Mathf.NextPowerOfTwo(count)];
            count = Camera.GetAllCameras(grassCameras);
            for (int i = 0; i < count; i++) RenderGrass(default, grassCameras[i]);
        }
        // -1 outside, 0 intersecting, 1 wholly inside. Include the maximum
        // shader displacement so gusts and body interaction cannot pop at edges.
        int GrassFrustum(Bounds bounds, float padding)
        {
            if (float.IsNaN(padding) || float.IsInfinity(padding)) return 1;
            var center = bounds.center; var extent = bounds.extents + Vector3.one * padding;
            bool inside = true;
            for (int i = 0; i < grassPlanes.Length; i++)
            {
                var plane = grassPlanes[i]; var normal = plane.normal;
                float distance = Vector3.Dot(normal, center) + plane.distance;
                var absoluteNormal = grassPlaneAbsoluteNormals[i];
                float radius = absoluteNormal.x * extent.x + absoluteNormal.y * extent.y + absoluteNormal.z * extent.z;
                if (distance + radius < 0f) return -1;
                if (distance - radius < 0f) inside = false;
            }
            return inside ? 1 : 0;
        }
        void SubmitGrass(GrassBatch batch, Camera camera)
        {
            submittedGrassBatches++;
            using (GrassSubmit.Auto())
                Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, batch.matrices, batch.count, batch.properties,
                    ShadowCastingMode.Off, true, 0, camera, LightProbeUsage.Off);
        }
        static void SetGrassProperties(GrassBatch batch)
        {
            batch.properties.SetFloatArray(GrassAnchorId, batch.anchors);
            batch.properties.SetFloatArray(GrassHeightId, batch.heights);
            batch.properties.SetFloatArray(GrassResponseId, batch.responses);
            batch.properties.SetFloatArray(GrassStiffnessId, batch.stiffness);
        }
        void RefreshFoliage(Vector3 cameraPosition)
        {
            if (foliageRefreshing) return;
            foliageRefreshing = true;
            foliageRoutine = StartCoroutine(RefreshFoliageWork(cameraPosition));
        }
        IEnumerator RefreshFoliageWork(Vector3 cameraPosition)
        {
            try
            {
            // Terrain textures carry the distant meadow. Drawing individual
            // blades across the entire island overwhelms even instanced batches.
            const float grassDistance = 180f;
            foreach (var batch in pendingGrassBatches) batch.count = 0;
            foreach (var group in grassBatches.Values) group.cursor = 0;
            pendingGrassBatches.Clear();
            double deadline = Time.realtimeSinceStartupAsDouble + RefreshBudget();
            int visited = 0;
            foreach (var cell in foliageCells)
            {
                // Distant grass cells stay hidden until their nearest edge returns.
                // Avoid visiting thousands of already culled renderers every refresh.
                float cellCutoff = cell.grass ? grassDistance : cell.cutoff;
                if (SquaredDistance(cameraPosition, cell.bounds) > cellCutoff * cellCutoff)
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
                if (++visited % 32 == 0 && Time.realtimeSinceStartupAsDouble >= deadline)
                { yield return null; deadline = Time.realtimeSinceStartupAsDouble + RefreshBudget(); }
                // Distance to bounds keeps large patches visible until their nearest edge is far away.
                float squared = SquaredDistance(cameraPosition, item.bounds);
                float cutoff = item.grass ? grassDistance : item.legacy ? 260f : 550f;
                bool hidden = item.forced || item.duplicate || squared > cutoff * cutoff;
                bool forced = hidden || item.batchable;
                if (item.renderOff != forced && item.renderer != null)
                {item.renderer.forceRenderingOff=forced;item.renderOff=forced;}
                if (hidden) continue;
                if (item.renderer == null) continue;
                if (item.filter == null) continue;
                if (item.grass)
                {
                    Mesh desired = item.detail != null && item.detail.far != null && squared > 35f * 35f
                        ? item.detail.far : item.original;
                    if (item.compactWind && renderMeshes.TryGetValue(desired, out var compact)) desired = compact;
                    if (item.currentMesh != desired)
                    { item.filter.sharedMesh = desired; item.currentMesh = desired; }
                }
                else if (item.detail != null)
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
            foreach (var batch in pendingGrassBatches)
            {
                if (batch.count == 0 || !batch.propertiesDirty) continue;
                SetGrassProperties(batch);
                batch.propertiesDirty = false;
            }
            // Readers keep the complete previous snapshot throughout the bounded
            // refresh; never render an empty or partially rebuilt grass field.
            var previous = activeGrassBatches; activeGrassBatches = pendingGrassBatches; pendingGrassBatches = previous;
            var previousGroups = renderedGrassGroups; renderedGrassGroups = grassBatches; grassBatches = previousGroups;
            }
            finally { foliageRefreshing = false; }
        }
        double RefreshBudget()
        {
            // Spend less background work when no camera can see this field (for
            // example open ocean). Full snapshots remain available immediately
            // when the camera turns; visible-field checks return to the fast budget.
            return activeGrassBatches.Count == 0 || submittedGrassBatches > 0 ? .0015 : .00035;
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
            if (item.groupOwner != grassBatches)
            {
                var oldGroup = item.group; var oldMesh = item.groupMesh;
                item.group = item.alternateGroup; item.groupMesh = item.alternateMesh;
                item.alternateGroup = oldGroup; item.alternateMesh = oldMesh;
                item.groupOwner = grassBatches;
            }
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
            if(batch.count==0)pendingGrassBatches.Add(batch);
            int index = batch.count++;
            if (index == 0) { batch.bounds = item.bounds; batch.maximumResponse = Mathf.Abs(item.response); }
            else { batch.bounds.Encapsulate(item.bounds); batch.maximumResponse = Mathf.Max(batch.maximumResponse, Mathf.Abs(item.response)); }
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
            // Retain originals until each renderer transfers to the new bounded
            // scan; native overrides may otherwise be read back after a yield.
            var previousRenderers = new Dictionary<MeshRenderer, Foliage>();
            foreach (var item in foliage) if (item.renderer) previousRenderers[item.renderer] = item;
            bool initial = previousRenderers.Count == 0;
            RestoreManagedRenderers();
#if UNITY_EDITOR
            var reloadRenderers = ReadReloadRenderers();
#endif
            var grassCells = new Dictionary<Vector2Int, FoliageCell>();
            var block=new MaterialPropertyBlock();
            double deadline=Time.realtimeSinceStartupAsDouble+InitializationBudget(initial,Time.unscaledDeltaTime);
            int visited=0;
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                // Bound initialization work so entering the island does not
                // process every plant and material in one rendered frame.
                if(++visited%32==0 && Time.realtimeSinceStartupAsDouble>=deadline)
                {yield return null;deadline=Time.realtimeSinceStartupAsDouble+InitializationBudget(initial,Time.unscaledDeltaTime);}
                if(renderer==null)continue;
                if (previousRenderers.TryGetValue(renderer, out var previous)) RestoreRenderer(previous);
#if UNITY_EDITOR
                // Restore immediately before taking ownership, including after
                // every bounded-scan yield; an earlier bulk restore can go stale.
                if (reloadRenderers != null && reloadRenderers.TryGetValue(renderer.GetInstanceID(), out var original))
                    RestoreReloadRenderer(original, renderer);
#endif
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
                        cell = new FoliageCell { bounds = item.bounds, grass = true };
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
                item.compactWind = materials.Length > 0;
                foreach (var material in materials)
                    if (material == null || material.shader.name != "Mavis/FoliageWind") { item.compactWind = false; break; }
                item.batchable = grass && SystemInfo.supportsInstancing && materials.Length == 1 && materials[0] != null && materials[0].enableInstancing &&
                    (materials[0].shader.name == "Mavis/FoliageWind" || materials[0].shader.name == "Universal Render Pipeline/Lit");
                if (grass && !item.batchable) ConfigureGrassLod(renderer);
                if (item.batchable)
                {
                    item.drawMaterial=materials[0];
                    block.Clear(); renderer.GetPropertyBlock(block);
                    item.anchor=block.GetFloat("_MavisWindAnchorY");item.inverseHeight=block.GetFloat("_MavisWindInvHeight");
                    item.response=block.GetFloat("_MavisWindResponse");item.stiffness=block.GetFloat("_WindTrunkStiffness");
                }
                // RefreshFoliage also runs while this coroutine builds the cache.
                // A distant cell may already be culled before these items join it;
                // subsequent refreshes intentionally skip that cell's contents.
                // Register the new renderer in the current state immediately, or
                // it remains individually visible indefinitely. Instanced items
                // must never also render through their original MeshRenderer.
                if (item.batchable || cell.culled)
                {
                    renderer.forceRenderingOff = true;
                    item.renderOff = true;
                }
            }
#if UNITY_EDITOR
            UnityEditor.SessionState.EraseString(ReloadKey);
#endif
            nextUpdate=0;
        }
        void ConfigureGrassLod(MeshRenderer renderer)
        {
            // Keep the original complete grass layout at every visible distance.
            // Cross-fading a thinned mesh still changed the meadow as the camera
            // moved. Only the final distance cull fades; tree LODs stay authored.
            var group = renderer.GetComponentInParent<LODGroup>();
            if (group == null || grassLods.ContainsKey(group) || group.fadeMode != LODFadeMode.None) return;
            var levels = group.GetLODs();
            if (levels.Length != 3) return;
            foreach (var level in levels)
                foreach (var part in level.renderers)
                {
                    if (part == null) return;
                    var filter = part.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.name.StartsWith("VerifiedGrass")) return;
                    foreach (var material in part.sharedMaterials)
                        if (material == null || (material.shader.name != "Mavis/FoliageWind" &&
                            material.shader.name != "Universal Render Pipeline/Lit")) return;
                }
            var state = new GrassLodState { id=group.GetInstanceID(),mode=group.fadeMode,
                animated=group.animateCrossFading,widths=new float[levels.Length],
                heights=new float[levels.Length],rendererCounts=new int[levels.Length],levels=levels };
            var parts=new List<int>();var enabled=new List<bool>();
            for (int i=0;i<levels.Length;i++)
            {
                state.widths[i]=levels[i].fadeTransitionWidth;state.heights[i]=levels[i].screenRelativeTransitionHeight;
                state.rendererCounts[i]=levels[i].renderers.Length;
                foreach (var part in levels[i].renderers)
                {
                    parts.Add(part.GetInstanceID());enabled.Add(part.enabled);
                    // Removed LOD members become ordinary renderers unless
                    // explicitly suppressed; do not draw all three layouts.
                    if (i>0 && System.Array.IndexOf(levels[0].renderers,part)<0) part.enabled=false;
                }
            }
            state.renderers=parts.ToArray();state.enabled=enabled.ToArray();
            grassLods.Add(group,state);
            group.fadeMode=LODFadeMode.CrossFade;
            group.animateCrossFading=false;
            float cutoff=levels[levels.Length-1].screenRelativeTransitionHeight;
            float band=(levels[levels.Length-2].screenRelativeTransitionHeight-cutoff)*.2f;
            group.SetLODs(new[]{new LOD(cutoff,levels[0].renderers){fadeTransitionWidth=band/Mathf.Max(.001f,1-cutoff)}});
        }
        static void RestoreGrassLod(LODGroup group, GrassLodState state)
        {
            if (group == null) return;
            if (state.levels != null)
            {
                group.SetLODs(state.levels);
                int offset=0;
                foreach (var level in state.levels)
                    foreach (var part in level.renderers)
                    { if (part != null) part.enabled=state.enabled[offset];offset++; }
            }
            var levels=group.GetLODs();
            if (levels.Length == state.widths.Length)
            {
                for (int i=0;i<levels.Length;i++) levels[i].fadeTransitionWidth=state.widths[i];
                group.SetLODs(levels);
            }
            group.fadeMode=state.mode;group.animateCrossFading=state.animated;
        }
        static void RestoreRenderer(Foliage item)
        {
            if (item.renderer == null) return;
            item.renderer.forceRenderingOff = item.forced;
            item.renderer.allowOcclusionWhenDynamic = item.occlusion;
            item.renderer.shadowCastingMode = item.shadows;
            item.renderer.sharedMaterials = item.materials;
            if (item.filter != null) item.filter.sharedMesh = item.original;
        }
        void RestoreManagedRenderers()
        {
            if (foliageRoutine != null) StopCoroutine(foliageRoutine);
            foliageRoutine = null; foliageRefreshing = false;
            foreach (var pair in grassLods) RestoreGrassLod(pair.Key,pair.Value);
            grassLods.Clear();
            foreach(var item in foliage)
            {
                RestoreRenderer(item);
            }
            foliage.Clear();
            foliageCells.Clear();
            grassBatches.Clear();
            renderedGrassGroups.Clear();
            activeGrassBatches.Clear();
            pendingGrassBatches.Clear();
            grassPadding.Clear();
            foreach(var material in instancedMaterials.Values)
                if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            instancedMaterials.Clear();
        }
        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= CaptureReloadRenderers;
#endif
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (cacheRoutine != null) StopCoroutine(cacheRoutine);
            cacheRoutine = null;
            RestoreManagedRenderers();
        }
        void OnDestroy() { SceneManager.sceneLoaded-=OnSceneLoaded; RestoreManagedRenderers(); }
    }
}
