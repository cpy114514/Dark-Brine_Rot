using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Mavis;
using Mavis.VisualStyles;

// Owned runtime fixture: real first-island terrain, eight authored enemies and
// a protected capsule target. It does not run narrative progression or saves.
public static class WorldFlock021
{
    const string Root = ".codex/flock021";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Chicken = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";
    const string Duck = "Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab";

    public static object Prepare()
    {
        if (Application.isPlaying || File.Exists(Root + "/workspace-before.json") || File.Exists(Root + "/running")) throw new Exception("Fresh stopped workspace required");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(s => SceneManager.GetSceneByPath(s.path).isDirty)) throw new Exception("Preserve dirty scenes");
        Directory.CreateDirectory(Root + "/save-before");
        foreach (string n in new[] { "dark-brine-rot-save.json", "dark-brine-rot-save.json.bak" })
            if (File.Exists(Path.Combine(Application.persistentDataPath, n))) File.Copy(Path.Combine(Application.persistentDataPath, n), Root + "/save-before/" + n, false);
        File.WriteAllText(Root + "/workspace-before.json", Newtonsoft.Json.JsonConvert.SerializeObject(setup));
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity", OpenSceneMode.Single);
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity", OpenSceneMode.Additive);
        return State();
    }

    public static object State() => new { playing = Application.isPlaying, running = File.Exists(Root + "/running"), scenes = EditorSceneManager.GetSceneManagerSetup(), savesUnchanged = Directory.GetFiles(Root + "/save-before").All(p => File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath, Path.GetFileName(p))))) };
    public static object Restore()
    {
        if (Application.isPlaying || File.Exists(Root + "/running")) throw new Exception("Stop owned test first");
        foreach (string p in Directory.GetFiles(Root + "/save-before")) if (!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath, Path.GetFileName(p))))) throw new Exception("Save changed: preserve current user progress");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/workspace-before.json")));
        return State();
    }
    public static object Finish()
    {
        if (Application.isPlaying || File.Exists(Root + "/running")) throw new Exception("Stop owned test first");
        var expected = Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root + "/workspace-before.json"));
        var actual = EditorSceneManager.GetSceneManagerSetup();
        bool clean = actual.All(s => !SceneManager.GetSceneByPath(s.path).isDirty);
        bool match = actual.Length == expected.Length && expected.Zip(actual, (a, b) => a.path == b.path && a.isActive == b.isActive && a.isLoaded == b.isLoaded).All(v => v);
        foreach (string p in Directory.GetFiles(Root + "/save-before")) if (!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath, Path.GetFileName(p))))) throw new Exception("Save changed; preserve newer progress");
        if (!clean || !match) throw new Exception("Original clean scene setup not restored");
        var report = new { stopped = true, markerClear = true, clean, sceneMatch = match, savesUnchanged = true, scenes = actual };
        File.WriteAllText(Root + "/final-native-state.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented)); return report;
    }
    static async Task Frame()
    {
        int frame = Time.frameCount; var limit = DateTime.UtcNow.AddSeconds(45);
        while (Time.frameCount == frame) { if (!Application.isPlaying || DateTime.UtcNow > limit) throw new Exception("Native frame stalled"); await Task.Delay(5); }
    }
    public static object AllocationControl(string label = "qualified-allocation-control")
    {
        if (Application.isPlaying || File.Exists(Root + "/running") || File.Exists(Root + "/" + label + ".json")) throw new Exception("Fresh stopped counter control required");
        NavMeshData data = null; NavMeshDataInstance instance = default;
        try
        {
            var sources = new List<NavMeshBuildSource> { new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(12, 1, 12), transform = Matrix4x4.TRS(Vector3.down * .5f, Quaternion.identity, Vector3.one), area = 0 } };
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources, new Bounds(Vector3.zero, new Vector3(14, 8, 14)), Vector3.zero, Quaternion.identity);
            if (!data) throw new Exception("Native control mesh unavailable");
            var offset = new Vector3(-1000, 500, -1000); instance = NavMesh.AddNavMeshData(data, offset, Quaternion.identity);
            var path = new NavMeshPath(); var from = offset + Vector3.left * 3; var to = offset + Vector3.right * 3;
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) throw new Exception("Native control path failed");
            var first = path.corners; var second = path.corners; bool sameReference = ReferenceEquals(first, second);
            for (int i = 0; i < 8; i++) GC.GetAllocatedBytesForCurrentThread();
            long before = GC.GetAllocatedBytesForCurrentThread(); int length = 0;
            for (int i = 0; i < 256; i++) length += path.corners.Length;
            long cachedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path)) throw new Exception("Native replan control failed");
            before = GC.GetAllocatedBytesForCurrentThread(); var fresh = path.corners;
            long freshBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            bool invalidated = !ReferenceEquals(first, fresh);
            var buffers = new byte[256][];
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < buffers.Length; i++) buffers[i] = new byte[64];
            long managedPositiveBytes = GC.GetAllocatedBytesForCurrentThread() - before; GC.KeepAlive(buffers);
            bool cachePassed = sameReference && invalidated && fresh.Length >= 2;
            bool counterQualified = managedPositiveBytes > 0 && freshBytes > 0;
            var result = new { cachePassed, counterQualified, sameReference, invalidated, cachedBytes, freshBytes, managedPositiveBytes, calls = 256, corners = fresh.Length, fields = typeof(NavMeshPath).GetFields(Private).Select(f => new { f.Name, type = f.FieldType.Name }).ToArray(), scope = "Same Editor Mono/native NavMesh API, isolated in-memory box at(-1000,500,-1000); no scene or asset write. Identity/invalidation establish the native array cache. A zero counter without successful native/managed positive controls cannot establish allocation-free operation." };
            File.WriteAllText(Root + "/" + label + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            if (!cachePassed) throw new Exception("Native cache identity control failed"); return result;
        }
        finally { if (instance.valid) instance.Remove(); if (data) UnityEngine.Object.DestroyImmediate(data); }
    }
    public static object Start(string label)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 2 || !SceneManager.GetSceneByName("Environment").isLoaded || !SceneManager.GetSceneByName("Lighting").isLoaded || File.Exists(Root + "/running") || File.Exists(Root + "/" + label + ".json")) throw new Exception("Fresh isolated environment Play required");
        File.WriteAllText(Root + "/running", label); _ = Run(label); return new { started = true };
    }
    static async Task Capture(Camera camera, string path)
    {
        bool done = false;
        Action<ScriptableRenderContext, Camera> handler = (context, c) =>
        {
            if (c != camera || done) return; done = true;
            var old = RenderTexture.active; var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try { RenderTexture.active = camera.targetTexture; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(image); }
        };
        RenderPipelineManager.endCameraRendering += handler;
        try { for (int i = 0; i < 30 && !done; i++) await Frame(); if (!done) throw new Exception("No actual camera render"); }
        finally { RenderPipelineManager.endCameraRendering -= handler; }
    }
    static Vector3 Ground(MeshCollider terrain, Vector3 at)
    {
        if (!terrain.Raycast(new Ray(new Vector3(at.x, terrain.bounds.max.y + 50, at.z), Vector3.down), out var hit, 2000)) throw new Exception("Outside real island terrain");
        return hit.point;
    }
    static bool ClearSite(MeshCollider terrain, Vector3 at, float sea)
    {
        if (!terrain.Raycast(new Ray(new Vector3(at.x, terrain.bounds.max.y + 50, at.z), Vector3.down), out var hit, 2000) || hit.point.y < sea + 3 || hit.normal.y < .94f) return false;
        return !Physics.OverlapCapsule(hit.point + Vector3.up * 1.1f, hit.point + Vector3.up * 4.8f, 1f, ~0, QueryTriggerInteraction.Ignore).Any(c => c != terrain && !CombatBodyCollision.IsActor(c));
    }
    static ChickenBroAI Spawn(string path, Transform parent, Vector3 at, ChickenBroFlock flock, Vector3 center)
    {
        var holder = new GameObject("Excluded runtime enemy"); holder.transform.SetParent(parent); holder.SetActive(false);
        var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), holder.transform); actor.AddComponent<GameSaveExcluded>();
        var ai = actor.GetComponent<ChickenBroAI>(); ai.Activate(at + Vector3.up * .08f, Quaternion.LookRotation(Vector3.ProjectOnPlane(center - at, Vector3.up)), flock); holder.SetActive(true); return ai;
    }
    static object AllocationProbe(ChickenBroAI[] actors, MeshCollider terrain, Vector3 site)
    {
        // Raw API readings only. They are unusable as allocation-free claims
        // until AllocationControl passes real native AND managed positive controls.
        var rows = new List<object>();
        foreach (var ai in actors)
        {
            var method = typeof(ChickenBroAI).GetMethod("Steer", Private);
            var steer = (Func<Vector3, Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3, Vector3>), ai, method);
            var repath = (Func<Vector3, bool>)Delegate.CreateDelegate(typeof(Func<Vector3, bool>), ai, typeof(ChickenBroAI).GetMethod("Repath", Private));
            Vector3 goal = Ground(terrain, site + new Vector3(16, 0, 12));
            bool valid = repath(goal); typeof(ChickenBroAI).GetField("hasPath", Private).SetValue(ai, valid);
            var path = (NavMeshPath)typeof(ChickenBroAI).GetField("path", Private).GetValue(ai); var nativeCorners = path.corners;
            if (!valid || nativeCorners.Length < 2) { rows.Add(new { actor = ai.name, valid = false }); continue; }
            typeof(ChickenBroAI).GetField("nextPath", Private).SetValue(ai, Time.time + 1000);
            for (int i = 0; i < 8; i++) steer(goal);
            const int count = 256; Vector3 direction = Vector3.zero;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < count; i++) direction = steer(goal);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            int corner = (int)typeof(ChickenBroAI).GetField("corner", Private).GetValue(ai);
            Vector3 expected = Vector3.ProjectOnPlane(nativeCorners[corner] - ai.transform.position, Vector3.up).normalized;
            if ((direction - expected).sqrMagnitude > .000001f) throw new Exception("Steering differs from actual native path corner");
            // Native getter positive control and empty-loop measurement are outside the production interval.
            before = GC.GetAllocatedBytesForCurrentThread(); int length = 0;
            for (int i = 0; i < count; i++) length += path.corners.Length;
            long getterBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < count; i++) length += i;
            long emptyBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            rows.Add(new { actor = ai.name, valid = true, calls = count, corners = nativeCorners.Length, allocatedBytes = allocated, getterBytes, emptyBytes, direction = direction.ToString(), nativeDirectionVerified = true });
            typeof(ChickenBroAI).GetField("nextPath", Private).SetValue(ai, 0f);
        }
        return rows;
    }
    static async Task Run(string label)
    {
        GameObject root = null; RenderTexture target = null; UniversalRenderPipelineAsset pipeline = null; ChickenBroFlock flock = null;
        var priorPipeline = QualitySettings.renderPipeline; var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        bool oldPixel = Pixel360Rendering.IsActive; float oldTime = Time.timeScale; var random = UnityEngine.Random.state;
        var styles = VisualStyleManager.Instance; string priorStyle = styles.Current.id; float intensity = styles.Intensity;
        var phases = new List<object>(); var errors = new List<string>();
        Application.LogCallback callback = (text, stack, type) => { if (type == LogType.Exception) errors.Add(text + "\n" + stack); };
        Application.logMessageReceived += callback;
        try
        {
            if (ChickenBroFlock.Active != null) throw new Exception("Another native flock owns the stage");
            UnityEngine.Random.InitState(212021); Time.timeScale = 1;
            Pixel360Rendering.ReleaseCurrent(); pipeline = UnityEngine.Object.Instantiate(source); pipeline.renderScale = 1; QualitySettings.renderPipeline = pipeline;
            root = new GameObject("Owned world flock021"); root.AddComponent<GameSaveExcluded>();
            var cameraGo = new GameObject("Native flock021 camera", typeof(Camera)); cameraGo.transform.SetParent(root.transform); cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>(); camera.fieldOfView = 58; camera.farClipPlane = 1200; camera.nearClipPlane = .15f; camera.transform.position = new Vector3(85, 51, 632); camera.transform.LookAt(new Vector3(85, 43, 615)); camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
            var deadline = DateTime.UtcNow.AddSeconds(150); WorldPerformanceManager performance;
            while ((performance = Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().FirstOrDefault(w => w.gameObject.scene.IsValid())) == null || !performance.IsReady) { if (DateTime.UtcNow > deadline) throw new Exception("World manager not ready"); await Frame(); }
            var terrain = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).OrderByDescending(i => i.GetComponent<MeshCollider>().bounds.size.sqrMagnitude).First().GetComponent<MeshCollider>();
            var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>(); float sea = ocean ? ocean.oceanHeight : 0;
            Vector3 site = Vector3.zero; bool found = false;
            for (int r = 20; r <= 120 && !found; r += 10) for (int a = 0; a < 24 && !found; a++)
            {
                var at = terrain.bounds.center + Quaternion.Euler(0, a * 15, 0) * Vector3.forward * r;
                if (!ClearSite(terrain, at, sea)) continue;
                bool clear = true; for (int k = 0; k < 8; k++) if (!ClearSite(terrain, at + Quaternion.Euler(0, k * 45, 0) * Vector3.forward * 8, sea)) { clear = false; break; }
                if (clear) { site = Ground(terrain, at); found = true; }
            }
            if (!found) throw new Exception("No valid dry real-terrain flock site; preserve vegetation");
            root.transform.position = site;
            camera.transform.position = site + new Vector3(16, 11, 18); camera.transform.LookAt(site + Vector3.up * 2);
            var surface = root.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Volume; surface.center = Vector3.up * 8; surface.size = new Vector3(72, 36, 72); surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
            var capsule = new GameObject("Protected diagnostic target"); capsule.transform.SetParent(root.transform); capsule.transform.position = site + Vector3.up * .06f;
            var cc = capsule.AddComponent<CharacterController>(); cc.height = 3.7f; cc.center = Vector3.up * 1.85f; cc.radius = .55f;
            var health = capsule.AddComponent<PlayerHealth>(); capsule.AddComponent<GameSaveExcluded>(); health.GrantProtection(3600);
            flock = new ChickenBroFlock { maxAttackers = 2 }; flock.Open();
            var actors = Enumerable.Range(0, 8).Select(k => Spawn(k < 5 ? Chicken : Duck, root.transform, Ground(terrain, site + Quaternion.Euler(0, k * 45, 0) * Vector3.forward * 8), flock, site)).ToArray();
            if (flock.Members.Count != 8) throw new Exception("Flock membership mismatch");
            for (int i = 0; i < 90; i++) await Frame();
            var allocations = AllocationProbe(actors, terrain, site);
            int maxAttackers = 0, violations = 0, belowTerrain = 0, missingGround = 0; float maxGap = 0, minGap = float.PositiveInfinity; int frames = 0;
            foreach (string style in new[] { "realistic", "dark-fairytale", "origami", "monochrome" })
            {
                styles.Select(style, false); styles.SetIntensity(.85f, false);
                Vector3 playerAt = Ground(terrain, site + (style == "origami" ? Vector3.right * 3 : style == "monochrome" ? Vector3.left * 3 : Vector3.zero));
                cc.enabled = false; capsule.transform.position = playerAt + Vector3.up * .06f; cc.enabled = true;
                for (int i = 0; i < 90; i++)
                {
                    float angle = (frames * .25f) * Mathf.Deg2Rad; camera.transform.position = site + new Vector3(Mathf.Sin(angle) * 21, 11, Mathf.Cos(angle) * 21); camera.transform.LookAt(site + Vector3.up * 2.2f);
                    await Frame(); frames++; int attackers = actors.Count(a => a.HoldsAttack); maxAttackers = Math.Max(maxAttackers, attackers); if (attackers > 2) violations++;
                    if (i % 10 == 0) foreach (var a in actors)
                    {
                        var motor = a.GetComponent<CharacterController>(); float sole = motor.bounds.min.y;
                        var hits = Physics.RaycastAll(new Vector3(a.transform.position.x, sole + .4f, a.transform.position.z), Vector3.down, 12, ~0, QueryTriggerInteraction.Ignore).Where(h => !CombatBodyCollision.IsActor(h.collider) && h.normal.y > .55f).OrderBy(h => h.distance).ToArray();
                        if (hits.Length == 0) { missingGround++; continue; }
                        float gap = sole - hits[0].point.y; maxGap = Mathf.Max(maxGap, gap); minGap = Mathf.Min(minGap, gap); if (gap < -.35f) belowTerrain++;
                    }
                }
                await Capture(camera, Root + "/" + label + "-" + style + ".png");
                phases.Add(new { style, positions = actors.Select(a => new { a.name, mode = a.Mode.ToString(), position = a.transform.position.ToString(), grounded = a.GetComponent<CharacterController>().isGrounded }).ToArray() });
            }
            bool passed = maxAttackers > 0 && violations == 0 && belowTerrain == 0 && missingGround == 0 && actors.All(a => a.IsAlive) && flock.Members.Count == 8 && errors.Count == 0;
            File.WriteAllText(Root + "/" + label + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new { completed = true, passed, site = site.ToString(), terrain = terrain.name, allocations, frames, maxAttackers, violations, belowTerrain, missingGround, minGap, maxGap, phases, errors, scope = "Real Environment/Lighting terrain and scenery, matching native encounter NavMeshSurface geometry/volume, actual5 chicken+3 enlarged duck prefabs, protected3.7m diagnostic capsule target,90 native frames/style with orbit camera. Not narrative encounter/Sahur input/full combat or FPS acceptance." }, Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception e) { File.WriteAllText(Root + "/" + label + "-error.txt", e.ToString()); }
        finally
        {
            Application.logMessageReceived -= callback; flock?.Close(); if (root) UnityEngine.Object.DestroyImmediate(root);
            Pixel360Rendering.ReleaseCurrent(); QualitySettings.renderPipeline = priorPipeline; if (oldPixel && source) Pixel360Rendering.Configure(source, true);
            styles.Select(priorStyle, false); styles.SetIntensity(intensity, false); Time.timeScale = oldTime; UnityEngine.Random.state = random;
            if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); } if (pipeline) UnityEngine.Object.DestroyImmediate(pipeline);
            File.Delete(Root + "/running"); EditorApplication.isPlaying = false;
        }
    }
}
