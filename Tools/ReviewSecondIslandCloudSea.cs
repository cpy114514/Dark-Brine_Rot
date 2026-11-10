using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReviewSecondIslandCloudSea
{
    public static readonly (string name, Vector3 site, Vector3 heading)[] Views =
    {
        ("east-overlook", new Vector3(1010, 0, 1610), Vector3.right),
        ("north-overlook", new Vector3(-448, 0, 2328), new Vector3(-.35f, 0, .94f)),
        ("south-overlook", new Vector3(180, 0, 830), new Vector3(.32f, 0, -.95f)),
        ("cottage", new Vector3(123, 0, 1964), new Vector3(-.75f, 0, -.7f)),
        ("camp", new Vector3(-100, 0, 1704), new Vector3(.83f, 0, .55f)),
        ("cottage-front", new Vector3(121, 0, 1910), new Vector3(-.55f, 0, .84f)),
        ("east-approach", new Vector3(992, 0, 1612), Vector3.right)
    };

    public static object Capture(string label, bool includeUpgrade = true, string style = "realistic")
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Isolated Play required");
        var folder = ".codex/model-refinement/island2/" + label;
        if (Directory.Exists(folder)) throw new InvalidOperationException("Preserve previous capture");
        Directory.CreateDirectory(folder);
        var island = UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>();
        var ground = island.surface.GetComponent<Collider>();
        var original = Camera.main;
        var manager = Mavis.VisualStyles.VisualStyleManager.Instance;
        var upgrade = original.gameObject.scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Second Island - Cloud Sea");
        if (!upgrade) upgrade = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(go => go.name == "Second Island - Cloud Sea");
        bool upgradeWasActive = upgrade && upgrade.activeSelf;
        float time = Time.timeScale;
        Time.timeScale = 0;
        if (upgrade) upgrade.SetActive(includeUpgrade);
        manager.Select(style, false);
        var obj = new GameObject("Cloud Sea review camera", typeof(Camera));
        var camera = obj.GetComponent<Camera>();
        camera.CopyFrom(original);
        camera.enabled = false;
        camera.fieldOfView = 57;
        var target = new RenderTexture(1440, 810, 24, RenderTextureFormat.ARGB32);
        var screenshot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        var oldTarget = RenderTexture.active;
        var records = new List<object>();
        try
        {
            foreach (var view in Views)
            {
                var start = new Vector3(view.site.x, ground.bounds.max.y + 20, view.site.z);
                if (!ground.Raycast(new Ray(start, Vector3.down), out var hit, ground.bounds.size.y + 40))
                    throw new InvalidOperationException("Missing ground at " + view.name);
                camera.transform.position = hit.point + Vector3.up * 6;
                bool edge = view.name.EndsWith("overlook");
                camera.transform.rotation = Quaternion.LookRotation(view.heading + Vector3.down * (edge ? .19f : .12f));
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                screenshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                screenshot.Apply();
                File.WriteAllBytes(folder + "/" + view.name + ".png", screenshot.EncodeToPNG());
                records.Add(new { view.name, ground = hit.point.ToString(), slope = Vector3.Angle(hit.normal, Vector3.up) });
            }
        }
        finally
        {
            RenderTexture.active = oldTarget;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(obj);
            if (upgrade) upgrade.SetActive(upgradeWasActive);
            Time.timeScale = time;
            manager.RestorePreference();
        }
        var result = new { captures = records, cloudSea = upgrade && includeUpgrade,
            position = original.transform.position.ToString(), style };
        File.WriteAllText(folder + "/result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static async Task<object> Measure()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Isolated Play required");
        var root = GameObject.Find("Second Island - Cloud Sea");
        var cloud = root.transform.Find("Cloud bed below the island").gameObject;
        var island = UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>();
        var ground = island.surface.GetComponent<Collider>();
        var view = Views[0];
        if (!ground.Raycast(new Ray(new Vector3(view.site.x, 2800, view.site.z), Vector3.down), out var hit, 1300))
            throw new InvalidOperationException("Missing overlook ground");
        var previous = Camera.allCameras.Select(c => (camera: c, c.enabled)).ToArray();
        var go = new GameObject("Island2 cloud GPU review", typeof(Camera));
        var camera = go.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.transform.SetPositionAndRotation(hit.point + Vector3.up * 6, Quaternion.LookRotation(view.heading + Vector3.down * .19f));
        var rt = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = rt;
        var pipeline = UniversalRenderPipeline.asset;
        float renderScale = pipeline.renderScale;
        float time = Time.timeScale;
        var records = new List<object>();
        ProfilingSampler sampler = null;
        var errors = new List<string>();
        void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }
        Application.logMessageReceived += Log;
        try
        {
            foreach (var state in previous) state.camera.enabled = false;
            Time.timeScale = 0;
            pipeline.renderScale = 1;
            Mavis.VisualStyles.VisualStyleManager.Instance.Select("realistic", false);
            await Frames(3);
            var metadata = typeof(UniversalRenderPipeline).GetNestedType("CameraMetadataCache", BindingFlags.NonPublic);
            var entry = metadata.GetMethod("GetCached", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { camera });
            sampler = (ProfilingSampler)entry.GetType().GetField("sampler").GetValue(entry);
            sampler.enableRecording = true;
            foreach (bool visible in new[] { false, true, false, true })
            {
                cloud.SetActive(visible);
                await Frames(24);
                var samples = new List<float>();
                for (int i = 0; i < 48; i++)
                {
                    await Frames(1);
                    if (sampler.gpuElapsedTime > 0) samples.Add(sampler.gpuElapsedTime);
                }
                samples.Sort();
                records.Add(new { visible, samples = samples.Count, medianGpuMs = samples.Count == 0 ? -1 : samples[samples.Count / 2],
                    graphicsBytes = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() });
            }
        }
        finally
        {
            if (sampler != null) sampler.enableRecording = false;
            cloud.SetActive(true);
            Time.timeScale = time;
            pipeline.renderScale = renderScale;
            Application.logMessageReceived -= Log;
            Mavis.VisualStyles.VisualStyleManager.Instance.RestorePreference();
            foreach (var state in previous) if (state.camera) state.camera.enabled = state.enabled;
            UnityEngine.Object.DestroyImmediate(go);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
        var result = new { records, errors, hardware = SystemInfo.graphicsDeviceName, width = 1920, height = 1080,
            upgradeRenderers = root.GetComponentsInChildren<Renderer>().Length, upgradeColliders = root.GetComponentsInChildren<Collider>().Length };
        File.WriteAllText(".codex/model-refinement/island2/performance.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static async Task<object> PlayerView()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Isolated Play required");
        var motor = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var ground = UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        var view = Views[2];
        var start = new Vector3(view.site.x, ground.bounds.max.y + 20, view.site.z);
        if (!ground.Raycast(new Ray(start, Vector3.down), out var hit, ground.bounds.size.y + 40))
            throw new InvalidOperationException("Missing overlook");
        var camera = Camera.main;
        var heading = view.heading.normalized;
        var oldPos = motor.transform.position;
        var oldRot = motor.transform.rotation;
        var oldCameraPos = camera.transform.position;
        var oldCameraRot = camera.transform.rotation;
        var awakening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
        bool awakeningEnabled = awakening && awakening.enabled;
        var target = new RenderTexture(1440, 810, 24);
        var image = new Texture2D(1440, 810, TextureFormat.RGB24, false);
        var oldRt = RenderTexture.active;
        try
        {
            if (awakening) awakening.enabled = false;
            motor.ExternalMovementLock = motor.ExternalControlLock = false;
            motor.RestoreSavedPose(hit.point + Vector3.up * (-motor.LowestFootWorldOffset + .04f), Quaternion.LookRotation(heading));
            camera.transform.SetPositionAndRotation(hit.point - heading * 10 + Vector3.up * 6,
                Quaternion.LookRotation(heading - Vector3.up * .23f));
            motor.ContinueCameraFromCurrentView(camera);
            await Frames(48);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(".codex/model-refinement/island2/player-overlook.png", image.EncodeToPNG());
            var result = new { player = motor.transform.position.ToString(), camera = camera.transform.position.ToString(),
                controllerGrounded = motor.GetComponent<CharacterController>().isGrounded, view = view.name };
            File.WriteAllText(".codex/model-refinement/island2/player-overlook.json", Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally
        {
            if (awakening) awakening.enabled = awakeningEnabled;
            motor.RestoreSavedPose(oldPos, oldRot);
            camera.transform.SetPositionAndRotation(oldCameraPos, oldCameraRot);
            motor.ContinueCameraFromCurrentView(camera);
            RenderTexture.active = oldRt;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    static async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int frame = Time.frameCount;
            while (Time.frameCount == frame)
            {
                if (!Application.isPlaying) throw new InvalidOperationException("Play interrupted");
                await Task.Delay(5);
            }
        }
    }
}
