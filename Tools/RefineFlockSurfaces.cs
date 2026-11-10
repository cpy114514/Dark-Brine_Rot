using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RefineFlockSurfaces
{
    const string Enemies = "Assets/Game/Prefabs/Enemies/";
    const string Art = "Assets/Game/Art/ModelRefinement/";
    const string Evidence = ".codex/model-refinement/flock";
    static readonly string[] Birds = { "ChickenBro", "BullyGoose" };

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        Directory.CreateDirectory(Evidence);
        var maps = Birds.Select(bird => (colour: ImportMap(bird + "Colour.png", true),
            surface: ImportMap(bird + "Surface.png", false))).ToArray();
        var result = new List<object>();
        for (int i = 0; i < Birds.Length; i++)
        {
            string bird = Birds[i];
            string path = Enemies + bird + "/" + bird + ".mat";
            string backup = Evidence + "/" + bird + ".mat.before";
            if (!File.Exists(backup)) File.Copy(path, backup);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Apply(material, maps[i].colour, maps[i].surface);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            result.Add(new { bird, surface = maps[i].surface.name, colour = maps[i].colour.name });
        }
        File.WriteAllText(Evidence + "/unity-install.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object Measure()
    {
        var rows = new List<object>();
        foreach (var bird in Birds)
        {
            string texture = Enemies + bird + "/Textures/texture_pbr_20250901";
            bool chicken = bird == "ChickenBro";
            var before = new[]
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(texture + (chicken ? "_albedo.png" : ".png")),
                chicken ? AssetDatabase.LoadAssetAtPath<Texture2D>(texture + "_metallicSmoothness.png") : null
            };
            var after = new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(Art + bird + "Colour.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Art + bird + "Surface.png") };
            rows.Add(new { bird, beforeBytes = before.Where(map => map).Sum(map => Profiler.GetRuntimeMemorySizeLong(map)),
                afterBytes = after.Sum(map => Profiler.GetRuntimeMemorySizeLong(map)),
                maps = after.Select(map => new { map.name, map.width, format = map.graphicsFormat.ToString() }).ToArray() });
        }
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/unity-memory.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
        return rows;
    }

    public static async Task<object> Review(bool compare = false)
    {
        if (!Application.isPlaying || AdditiveSceneBootstrap.IsLoading)
            throw new InvalidOperationException("Load First Island in isolated Play");
        Directory.CreateDirectory(Evidence);
        var terrain = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<Collider>();
        if (!terrain.Raycast(new Ray(new Vector3(288, 150, 346), Vector3.down), out var ground, 400))
            throw new InvalidOperationException("Flock review ground missing");
        var probe = new GameObject("Flock material review", typeof(Camera));
        var camera = probe.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 38;
        var target = new RenderTexture(960, 960, 24);
        var screenshot = new Texture2D(960, 960, TextureFormat.RGB24, false);
        var savedTarget = RenderTexture.active;
        var manager = Mavis.VisualStyles.VisualStyleManager.Instance;
        var report = new List<object>();
        float time = Time.timeScale;
        Time.timeScale = 0;
        manager.Select("realistic", false);
        try
        {
            foreach (string bird in Birds)
            {
                bool chicken = bird == "ChickenBro";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + bird + "/" + bird + ".prefab");
                var actor = UnityEngine.Object.Instantiate(prefab, ground.point, Quaternion.identity);
                actor.AddComponent<GameSaveExcluded>();
                actor.GetComponent<ChickenBroAI>().enabled = false;
                var skin = actor.GetComponentInChildren<SkinnedMeshRenderer>();
                var animator = actor.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                skin.updateWhenOffscreen = true;
                var production = skin.sharedMaterial;
                var before = new Material(production);
                var after = compare ? new Material(production) : null;
                try
                {
                    RestoreBaseline(before, bird, chicken);
                    if (compare)
                    {
                        var surface = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + bird + "Surface.png");
                        var colour = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + bird + "Colour.png");
                        if (!surface || !colour) throw new InvalidOperationException("Missing comparison map for " + bird);
                        Apply(after, colour, surface);
                    }
                    Matrix4x4[] idlePose = null;
                    foreach (var (view, state, progress, offset) in new[]
                    {
                        ("front", "Idle", .20f, new Vector3(0, .14f, 1)),
                        ("side", "Idle", .20f, new Vector3(1, .18f, .09f)),
                        ("back", "Idle", .20f, new Vector3(.15f, .14f, -1)),
                        ("run", "Run", .34f, new Vector3(.76f, .19f, 1)),
                        ("strike", chicken ? "Attack" : "Strike", .44f, new Vector3(.76f, .19f, 1)),
                        ("heavy", chicken ? "Heavy" : "Wing", .52f, new Vector3(.76f, .19f, 1))
                    })
                    {
                        string actualState = !chicken && (state == "Idle" || state == "Run") ? "Locomotion" : state;
                        if (!chicken) animator.SetFloat("Speed", state == "Run" ? 1 : 0);
                        animator.Play("Base Layer." + actualState, 0, progress);
                        animator.Update(.001f);
                        await Frame();
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + actualState))
                            throw new InvalidOperationException("Missing " + bird + " animation " + state);
                        var world = skin.bounds;
                        camera.transform.position = world.center + offset.normalized * (world.size.magnitude * 1.56f);
                        camera.transform.LookAt(world.center);
                        var pose = skin.bones.Select(bone => bone.localToWorldMatrix).ToArray();
                        if (view == "front") idlePose = pose;
                        if (state != "Idle" && idlePose.SequenceEqual(pose))
                            throw new InvalidOperationException("Bird did not pose for " + state);
                        for (int version = 0; version < (compare ? 2 : 1); version++)
                        {
                            skin.sharedMaterial = version == 0 ? before : after;
                            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                            RenderTexture.active = target;
                            screenshot.ReadPixels(new Rect(0, 0, 960, 960), 0, 0);
                            screenshot.Apply();
                            string name = bird + "-" + view + (version == 0 ? "-before" : "-after");
                            File.WriteAllBytes(Evidence + "/" + name + ".png", screenshot.EncodeToPNG());
                        }
                        if (!pose.SequenceEqual(skin.bones.Select(bone => bone.localToWorldMatrix)))
                            throw new InvalidOperationException("Material comparison changed a bone pose");
                    }
                    bool hurtboxes = BirdHurtboxes.Ensure(actor) &&
                        actor.GetComponentsInChildren<BoxCollider>().Count(box => box.name.StartsWith("BodyHitbox_Bird_")) >= 2;
                    if (!hurtboxes) throw new InvalidOperationException("Original bird hurtboxes missing");
                    report.Add(new { bird, mesh = skin.sharedMesh.name, bones = skin.bones.Length,
                        triangles = skin.sharedMesh.GetIndexCount(0) / 3, avatarValid = animator.avatar && animator.avatar.isValid,
                        hurtboxes, poseMatricesIdentical = true, materialInstalled = compare &&
                            production.GetTexture("_MetallicGlossMap") == after.GetTexture("_MetallicGlossMap") &&
                            production.GetTexture("_BaseMap") == after.GetTexture("_BaseMap") });
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(actor);
                    UnityEngine.Object.DestroyImmediate(before);
                    if (after) UnityEngine.Object.DestroyImmediate(after);
                }
            }
            File.WriteAllText(Evidence + "/unity-comparison.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            RenderTexture.active = savedTarget;
            target.Release();
            foreach (var obj in new UnityEngine.Object[] { target, screenshot, probe }) UnityEngine.Object.DestroyImmediate(obj);
            Time.timeScale = time;
            manager.RestorePreference();
        }
    }

    static Texture2D ImportMap(string name, bool srgb)
    {
        string path = Art + name;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Missing " + path);
        var quality = srgb ? TextureImporterCompression.Compressed : TextureImporterCompression.CompressedHQ;
        if (importer.sRGBTexture != srgb || importer.textureCompression != quality)
        {
            importer.sRGBTexture = srgb;
            importer.textureCompression = quality;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Apply(Material material, Texture2D colour, Texture2D map)
    {
        material.SetTexture("_BaseMap", colour);
        material.SetTexture("_MainTex", colour);
        material.SetTexture("_MetallicGlossMap", map);
        material.SetFloat("_Smoothness", 1);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
    }

    static void RestoreBaseline(Material material, string bird, bool chicken)
    {
        var colour = AssetDatabase.LoadAssetAtPath<Texture2D>(Enemies + bird +
            "/Textures/texture_pbr_20250901" + (chicken ? "_albedo.png" : ".png"));
        material.SetTexture("_BaseMap", colour);
        material.SetTexture("_MainTex", colour);
        var map = chicken ? AssetDatabase.LoadAssetAtPath<Texture2D>(
            Enemies + bird + "/Textures/texture_pbr_20250901_metallicSmoothness.png") : null;
        material.SetTexture("_MetallicGlossMap", map);
        material.SetFloat("_Smoothness", chicken ? .45f : .42f);
        if (chicken) material.EnableKeyword("_METALLICSPECGLOSSMAP");
        else material.DisableKeyword("_METALLICSPECGLOSSMAP");
    }

    static async Task Frame()
    {
        int frame = Time.frameCount;
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (frame == Time.frameCount)
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new InvalidOperationException("Flock review interrupted");
            await Task.Delay(5);
        }
    }
}
