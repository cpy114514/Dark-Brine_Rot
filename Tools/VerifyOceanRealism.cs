using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Read-only live diagnostics and disposable camera captures. Does not tune the scene.</summary>
public static class VerifyOceanRealism
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static string Run()
    {
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        Check(ocean != null, "Ocean is missing.");
        Check(ocean.GetComponent<MeshRenderer>().receiveShadows, "Water does not receive shadows.");
        foreach (string name in new[] { "DarkBrine/Procedural Ocean", "DarkBrine/Procedural Shore Foam" })
        {
            var shader = Shader.Find(name);
            Check(shader != null && shader.isSupported && !ShaderUtil.GetShaderMessages(shader)
                .Any(message => message.severity.ToString() == "Error"), "Invalid shader: " + name);
        }
        float waveExtent = (ocean.wave1.amplitude + ocean.wave2.amplitude + ocean.wave3.amplitude + ocean.wave4.amplitude) * 1.6f;
        Check(ocean.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y >= waveExtent - 0.001f,
            "Wave peaks exceed the CPU culling volume.");
        var create = typeof(OceanWorld).GetMethod("CreateOceanMesh", BindingFlags.Static | BindingFlags.NonPublic);
        var report = new StringBuilder();
        float size = (ocean.oceanSize + Mathf.Max(300f, ocean.oceanSize * 0.2f)) * 2f;
        foreach (int resolution in new[] { 80, 160, 300 })
        foreach (int coverage in new[] { 180, 360 })
        {
            Mesh mesh = null;
            try
            {
                mesh = (Mesh)create.Invoke(null, new object[] { resolution, size, coverage });
                var vertices = mesh.vertices;
                int width = coverage == 360 ? resolution : Mathf.Max(48, Mathf.RoundToInt(resolution * 0.85f));
                Check(vertices.Length == (resolution + 1) * (width + 1), "Mesh vertex budget changed.");
                Check(mesh.triangles.Length == resolution * width * 6, "Mesh triangle budget changed.");
                var uniqueZ = vertices.Select(vertex => vertex.z).Distinct().OrderBy(z => z).ToArray();
                int origin = Array.FindIndex(uniqueZ, z => Mathf.Abs(z) < 0.001f);
                Check(origin >= 1 && origin < uniqueZ.Length - 1, "Grid is not concentrated on the camera.");
                float cell = Mathf.Max(uniqueZ[origin + 1] - uniqueZ[origin], uniqueZ[origin] - uniqueZ[origin - 1]);
                Check(cell < 2f, "Near-camera grid is too coarse for short waves.");
                report.AppendLine($"{resolution}/{coverage}deg: {vertices.Length} vertices; near-camera Z cell {cell:F4}m; unchanged triangle budget.");
            }
            finally { if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh); }
        }
        ocean.GetShaderDetailDistances(out float nearDistance, out float midDistance);
        foreach (var island in UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
        {
            var foam = island.transform.Find("Procedural Shore Foam");
            Check(foam != null, "Shore ribbon is missing.");
            var renderer = foam.GetComponent<MeshRenderer>();
            Check(renderer.receiveShadows, "Shore foam does not receive shadows.");
            var material = renderer.sharedMaterial;
            Check(Mathf.Approximately(material.GetFloat("_WaveIrregularity"), ocean.waveIrregularity) &&
                Mathf.Approximately(material.GetFloat("_NearDetailDistance"), nearDistance) &&
                Mathf.Approximately(material.GetFloat("_MidDetailDistance"), midDistance), "Shore/water wave LOD mismatch.");
            Bounds bounds = renderer.bounds;
            Check(bounds.min.y < ocean.oceanHeight - waveExtent && bounds.max.y > ocean.oceanHeight + waveExtent,
                "Shore bounds do not contain the moving waterline.");
            Mesh ribbon = foam.GetComponent<MeshFilter>().sharedMesh;
            int columns = ribbon.vertexCount / 13;
            float spacing = ocean.effectsQuality == OceanWorld.EffectsQuality.Low ? 8f :
                ocean.effectsQuality == OceanWorld.EffectsQuality.Medium ? 4f : 2f;
            int cap = ocean.effectsQuality == OceanWorld.EffectsQuality.Low ? 512 :
                ocean.effectsQuality == OceanWorld.EffectsQuality.Medium ? 1024 : 2048;
            int expectedSegments = Mathf.Clamp(Mathf.CeilToInt(island.shorelineRadius * 1.2f * Mathf.PI * 2f / spacing),
                island.radialSegments, Mathf.Max(island.radialSegments, cap));
            Check(columns == expectedSegments + 1, "Shore ribbon did not rebuild at the selected quality.");
            Check(island.GetComponent<MeshFilter>().sharedMesh.vertexCount == 1 + island.radialSegments * island.rings,
                "Shore refinement changed the island terrain.");
            report.AppendLine($"Shore ribbon: {ribbon.vertexCount} vertices, independent {columns - 1} shoreline segments; terrain topology unchanged.");
        }
        if (Application.isPlaying)
        {
            var cycle = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
            var field = typeof(DayNightCycle).GetField("oceanReflectionProbe", BindingFlags.Instance | BindingFlags.NonPublic);
            var probe = cycle == null ? null : (ReflectionProbe)field.GetValue(cycle);
            Check(probe != null, "Realtime ocean reflection probe is missing.");
            var sky = ocean.transform.Find("Procedural Sky");
            Check(sky != null && (probe.cullingMask & (1 << sky.gameObject.layer)) != 0,
                "The reflection probe excludes the procedural sky layer.");
            float sphereRadius = sky.lossyScale.x * 0.5f;
            float offset = Vector3.Distance(sky.position, probe.transform.position);
            Check(probe.farClipPlane >= sphereRadius + offset,
                "Reflection probe cannot see the entire procedural sky.");
            Check(probe.size.x >= ocean.oceanSize * 2f && probe.size.z >= ocean.oceanSize * 2f,
                "Ocean pixels lie outside the reflection influence volume.");
            report.AppendLine($"Sky-reflection coverage passed: radius={sphereRadius:F1}, offset={offset:F1}, far={probe.farClipPlane:F1}, probe size={probe.size:F1}.");
        }
        report.AppendLine("Water/shore shaders, shadow receiving, displacement bounds and shared wave/LOD parameters passed.");
        return report.ToString();
    }

    public static async Task<string> CaptureDayNight()
    {
        Check(Application.isPlaying, "Use Play Mode for day/night sampling.");
        var cycle = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        Check(cycle != null, "Day/night cycle is missing.");
        var elapsed = typeof(DayNightCycle).GetField("elapsedGameHours", BindingFlags.Instance | BindingFlags.NonPublic);
        var apply = typeof(DayNightCycle).GetMethod("ApplyTimeOfDay", BindingFlags.Instance | BindingFlags.NonPublic);
        var refresh = typeof(DayNightCycle).GetMethod("RefreshOceanReflections", BindingFlags.Instance | BindingFlags.NonPublic);
        float savedHour = cycle.startHour;
        float savedElapsed = (float)elapsed.GetValue(cycle);
        bool savedClock = cycle.clockRuns;
        var report = new StringBuilder();
        try
        {
            cycle.clockRuns = false;
            elapsed.SetValue(cycle, 0f);
            foreach (var item in new[] { ("daylight", 12f), ("sunset", 18f), ("moonlight", 0f) })
            {
                cycle.startHour = item.Item2;
                apply.Invoke(cycle, null);
                refresh.Invoke(cycle, null);
                await Task.Delay(1900);
                report.AppendLine(Capture(item.Item1));
                var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
                var material = ocean.GetComponent<MeshRenderer>().sharedMaterial;
                Check(Mathf.Approximately(material.GetFloat("_SunGlitterStrength"), ocean.sunGlitterStrength),
                    "Moonlight specular has been incorrectly suppressed by the daylight factor.");
                report.AppendLine($"{item.Item1}: shallow tint={material.GetColor("_ShallowColor"):F3}; reflection tint={material.GetColor("_WaterReflectionTint"):F3}; specular strength={material.GetFloat("_SunGlitterStrength"):F3}");
            }
            report.AppendLine(Run());
            return report.ToString();
        }
        finally
        {
            cycle.startHour = savedHour;
            elapsed.SetValue(cycle, savedElapsed);
            cycle.clockRuns = savedClock;
            apply.Invoke(cycle, null);
            refresh.Invoke(cycle, null);
        }
    }

    public static string TestQualityChanges()
    {
        Check(!Application.isPlaying, "Run reversible quality tests in Edit Mode.");
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var islands = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None);
        var build = typeof(OceanWorld).GetMethod("BuildOcean", BindingFlags.Instance | BindingFlags.NonPublic);
        var update = typeof(ProceduralIsland).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var savedQuality = ocean.effectsQuality;
        var report = new StringBuilder();
        try
        {
            foreach (var quality in new[] { OceanWorld.EffectsQuality.Low, OceanWorld.EffectsQuality.High, OceanWorld.EffectsQuality.Medium })
            {
                ocean.effectsQuality = quality;
                build.Invoke(ocean, null);
                foreach (var island in islands) update.Invoke(island, null);
                report.AppendLine("Quality " + quality + ": " + Run());
            }
            return report.ToString();
        }
        finally
        {
            ocean.effectsQuality = savedQuality;
            build.Invoke(ocean, null);
            foreach (var island in islands) update.Invoke(island, null);
        }
    }

    public static string TestShadows()
    {
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var main = Camera.main;
        var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(light => light.isActiveAndEnabled && light.type == LightType.Directional);
        Check(ocean != null && main != null && sun != null && sun.shadows != LightShadows.None,
            "Use the daylight scene for the shadow render test.");
        GameObject cameraObject = null, cube = null;
        Material cubeMaterial = null;
        RenderTexture render = null;
        Texture2D texture = null;
        var savedActive = RenderTexture.active;
        try
        {
            Vector3 target = ocean.transform.TransformPoint(new Vector3(350f, 0f, 950f));
            target.y = ocean.oceanHeight;
            cameraObject = new GameObject("Temporary water shadow verification camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(main);
            camera.enabled = false;
            camera.aspect = 16f / 9f;
            Vector3 horizontal = Vector3.ProjectOnPlane(sun.transform.forward, Vector3.up).normalized;
            Vector3 cameraPosition = target + horizontal * 35f + Vector3.up * 18f;
            camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(target - cameraPosition));
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.requiresDepthTexture = true;
            data.requiresColorTexture = true;
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Temporary water shadow caster";
            cube.hideFlags = HideFlags.HideAndDontSave;
            cube.transform.localScale = new Vector3(24f, 8f, 24f);
            cube.transform.position = target - sun.transform.forward * (10f / Mathf.Max(0.1f, -sun.transform.forward.y));
            var renderer = cube.GetComponent<MeshRenderer>();
            cubeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.HideAndDontSave };
            renderer.sharedMaterial = cubeMaterial;
            render = new RenderTexture(640, 360, 24, RenderTextureFormat.Default) { hideFlags = HideFlags.HideAndDontSave };
            render.Create();
            texture = new Texture2D(640, 360, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            camera.targetTexture = render;
            float Sample(ShadowCastingMode mode, string label)
            {
                renderer.shadowCastingMode = mode;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "ocean-realism-shadow-" + label + ".png"), texture.EncodeToPNG());
                Vector3 pixel = camera.WorldToScreenPoint(target);
                float total = 0f;
                for (int y = -4; y <= 4; y++)
                for (int x = -4; x <= 4; x++)
                {
                    var color = texture.GetPixel(Mathf.Clamp(Mathf.RoundToInt(pixel.x) + x, 0, 639),
                        Mathf.Clamp(Mathf.RoundToInt(pixel.y) + y, 0, 359));
                    total += color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                }
                return total / 81f;
            }
            float shadowed = Sample(ShadowCastingMode.On, "on");
            float illuminated = Sample(ShadowCastingMode.Off, "off");
            Check(illuminated - shadowed > 0.001f, $"Water failed the actual shadow image comparison: shadow={shadowed:F5}, lit={illuminated:F5}.");
            return $"Actual rendered water shadow passed: luminance with caster {shadowed:F5}, without caster {illuminated:F5}; temporary camera/cube/resources removed.";
        }
        finally
        {
            RenderTexture.active = savedActive;
            if (cameraObject != null) { cameraObject.GetComponent<Camera>().targetTexture = null; UnityEngine.Object.DestroyImmediate(cameraObject); }
            if (cube != null) UnityEngine.Object.DestroyImmediate(cube);
            if (cubeMaterial != null) UnityEngine.Object.DestroyImmediate(cubeMaterial);
            if (render != null) { render.Release(); UnityEngine.Object.DestroyImmediate(render); }
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    public static string Inspect()
    {
        var text = new StringBuilder();
        var oceans = UnityEngine.Object.FindObjectsByType<OceanWorld>(FindObjectsSortMode.None);
        var main = Camera.main;
        text.AppendLine($"Playing={Application.isPlaying}; time={Time.time:F3}; quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}; colorSpace={QualitySettings.activeColorSpace}; GPU={SystemInfo.graphicsDeviceName}");
        if (main != null)
            text.AppendLine($"Main camera {main.name}: position={main.transform.position:F3}, forward={main.transform.forward:F3}, FOV={main.fieldOfView:F2}, near={main.nearClipPlane:F3}, far={main.farClipPlane:F1}, mask={main.cullingMask}, enabled={main.enabled}");
        else text.AppendLine("No MainCamera found.");
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline != null)
            text.AppendLine($"URP={pipeline.name}; renderScale={pipeline.renderScale:F2}; HDR={pipeline.supportsHDR}; MSAA={pipeline.msaaSampleCount}; depth={pipeline.supportsCameraDepthTexture}; opaque={pipeline.supportsCameraOpaqueTexture}; shadowDistance={pipeline.shadowDistance:F1}; cascades={pipeline.shadowCascadeCount}; cascade4={pipeline.cascade4Split:F3}");
        foreach (var ocean in oceans)
        {
            var renderer = ocean.GetComponent<MeshRenderer>();
            var mesh = ocean.GetComponent<MeshFilter>().sharedMesh;
            var material = renderer.sharedMaterial;
            text.AppendLine($"Ocean {ocean.name} in {ocean.gameObject.scene.path}: position={ocean.transform.position:F2}; Euler={ocean.transform.eulerAngles:F2}; quality={ocean.effectsQuality}; resolution={ocean.resolution}; range={ocean.oceanSize:F1}; near/mid={ocean.nearDetailDistance:F1}/{ocean.midDetailDistance:F1}; motion={ocean.waveMotionSpeed:F2}; irregularity={ocean.waveIrregularity:F2}; follow={ocean.followCamera}");
            text.AppendLine($"Colors shallow={ocean.shallowColor:F3}; mid={ocean.midColor:F3}; deep={ocean.deepColor:F3}; foam={ocean.foamColor:F3}");
            foreach (var pair in new[] { ("1", ocean.wave1), ("2", ocean.wave2), ("3", ocean.wave3), ("4", ocean.wave4) })
                text.AppendLine($"Wave {pair.Item1}: direction={pair.Item2.direction:F3}; amplitude={pair.Item2.amplitude:F3}; wavelength={pair.Item2.wavelength:F3}; speed={pair.Item2.speed:F3}; steepness={pair.Item2.steepness:F3}");
            if (mesh != null)
                text.AppendLine($"Mesh {mesh.name}: vertices={mesh.vertexCount}; triangles={mesh.triangles.Length / 3}; bounds={mesh.bounds}; rendererBounds={renderer.bounds}; shadows={renderer.shadowCastingMode}/{renderer.receiveShadows}");
            if (material != null)
            {
                text.AppendLine($"Material={material.name}; shader={material.shader.name}; supported={material.shader.isSupported}; queue={material.renderQueue}");
                foreach (string property in new[] { "_NearDetailDistance", "_MidDetailDistance", "_ViewDistance", "_Smoothness", "_ReflectionStrength", "_SpecularStrength", "_FresnelStrength", "_SunGlitterStrength", "_WhitecapStrength", "_FoamStrength", "_WaveIrregularity" })
                    if (material.HasProperty(property)) text.AppendLine($"  {property}={material.GetFloat(property):F3}");
            }
        }
        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled && l.type == LightType.Directional))
            text.AppendLine($"Directional light {light.name}: forward={light.transform.forward:F3}; color={light.color:F3}; intensity={light.intensity:F3}; shadows={light.shadows}; strength={light.shadowStrength:F2}");
        foreach (var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            text.AppendLine($"Reflection {probe.name}: position={probe.transform.position:F2}; size={probe.size:F1}; near/far={probe.nearClipPlane:F2}/{probe.farClipPlane:F1}; resolution={probe.resolution}; mode={probe.mode}; slicing={probe.timeSlicingMode}; mask={probe.cullingMask}");
        foreach (var island in UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
            text.AppendLine($"Island {island.name}: position={island.transform.position:F3}; shoreRadius={island.shorelineRadius:F3}; scale={island.transform.lossyScale:F3}");
        foreach (string name in new[] { "DarkBrine/Procedural Ocean", "DarkBrine/Procedural Shore Foam" })
        {
            var shader = Shader.Find(name);
            if (shader == null) { text.AppendLine("MISSING SHADER: " + name); continue; }
            var messages = ShaderUtil.GetShaderMessages(shader);
            text.AppendLine($"Shader {name}: supported={shader.isSupported}; errors={messages.Count(m => m.severity.ToString() == "Error")}; warnings={messages.Count(m => m.severity.ToString() == "Warning")}");
            foreach (var message in messages.Take(8)) text.AppendLine($"  {message.severity} {message.file}:{message.line} {message.message}");
        }
        return text.ToString();
    }

    public static string Capture(string label)
    {
        var main = Camera.main;
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var island = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None)
            .OrderBy(i => main == null ? 0 : (i.transform.position - main.transform.position).sqrMagnitude).FirstOrDefault();
        if (main == null || ocean == null) throw new InvalidOperationException("Load Gameplay with a MainCamera and OceanWorld before capturing.");
        label = new string((label ?? "capture").Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
        var paths = new List<string>();
        var poses = new List<(string name, Vector3 position, Quaternion rotation)> { ("game", main.transform.position, main.transform.rotation) };
        if (island != null)
        {
            // Select a shore segment inside the actual ocean patch, preferring the current view.
            // This avoids moving Camera.main (which would recenter/reorient the sea and sky).
            Vector3 shore = island.transform.position;
            Vector3 outward = Vector3.right;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < 72; i++)
            {
                float angle = i * Mathf.PI * 2f / 72f;
                var candidate = island.transform.TransformPoint(new Vector3(Mathf.Cos(angle) * island.shorelineRadius, 0, Mathf.Sin(angle) * island.shorelineRadius));
                candidate.y = ocean.oceanHeight;
                if (!InsideOceanPatch(ocean, candidate)) continue;
                Vector3 viewport = main.WorldToViewportPoint(candidate);
                float behindPenalty = viewport.z <= 0 ? 100000f : 0f;
                float edgePenalty = Mathf.Max(0, Mathf.Abs(viewport.x - 0.5f) - 0.5f) + Mathf.Max(0, Mathf.Abs(viewport.y - 0.5f) - 0.5f);
                float score = behindPenalty + edgePenalty * 1000f + Vector3.Distance(candidate, main.transform.position);
                if (score >= bestScore) continue;
                bestScore = score;
                shore = candidate;
                outward = candidate - island.transform.position;
                outward.y = 0;
                outward.Normalize();
            }
            if (!float.IsInfinity(bestScore))
            {
                Vector3 shorePosition = shore + outward * 18f + Vector3.up * 6f;
                Vector3 shoreTarget = shore - outward * 12f + Vector3.up * 0.4f;
                if (InsideOceanPatch(ocean, shorePosition))
                    poses.Add(("shore", shorePosition, Quaternion.LookRotation(shoreTarget - shorePosition)));
                Vector3 side = Vector3.Cross(Vector3.up, outward);
                Vector3 widePosition = shore + outward * 35f + side * 18f + Vector3.up * 14f;
                Vector3 wideTarget = shore - outward * 6f + side * 48f;
                if (InsideOceanPatch(ocean, widePosition))
                    poses.Add(("wide", widePosition, Quaternion.LookRotation(wideTarget - widePosition)));
            }
        }
        GameObject temporaryObject = null;
        RenderTexture render = null;
        Texture2D texture = null;
        RenderTexture originalActive = RenderTexture.active;
        try
        {
            temporaryObject = new GameObject("Temporary ocean verification camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = temporaryObject.AddComponent<Camera>();
            camera.CopyFrom(main);
            camera.enabled = false;
            camera.aspect = 16f / 9f;
            var mainData = main.GetComponent<UniversalAdditionalCameraData>();
            var cameraData = temporaryObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.requiresDepthTexture = true;
            cameraData.requiresColorTexture = true;
            if (mainData != null)
            {
                cameraData.renderPostProcessing = mainData.renderPostProcessing;
                cameraData.volumeLayerMask = mainData.volumeLayerMask;
                cameraData.volumeTrigger = mainData.volumeTrigger != null ? mainData.volumeTrigger : main.transform;
                cameraData.antialiasing = mainData.antialiasing;
                cameraData.antialiasingQuality = mainData.antialiasingQuality;
            }
            render = new RenderTexture(1280, 720, 24, RenderTextureFormat.Default) { name = "Temporary ocean verification RT", hideFlags = HideFlags.HideAndDontSave };
            render.Create();
            texture = new Texture2D(1280, 720, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            camera.targetTexture = render;
            foreach (var pose in poses)
            {
                camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                string path = Path.Combine(Path.GetTempPath(), $"ocean-realism-{label}-{pose.name}.png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                paths.Add($"{pose.name}: {path} (position={pose.position:F3}, Euler={pose.rotation.eulerAngles:F3})");
            }
            return $"Captured at time={Time.time:F3}. No scene camera, ocean, or daylight settings changed. " + string.Join(" | ", paths);
        }
        finally
        {
            RenderTexture.active = originalActive;
            if (temporaryObject != null)
            {
                var camera = temporaryObject.GetComponent<Camera>();
                if (camera != null) camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(temporaryObject);
            }
            if (render != null) { render.Release(); UnityEngine.Object.DestroyImmediate(render); }
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static bool InsideOceanPatch(OceanWorld ocean, Vector3 worldPoint)
    {
        var mesh = ocean.GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null) return false;
        Vector3 local = ocean.transform.InverseTransformPoint(worldPoint);
        var vertices = mesh.vertices;
        // Both current shapes are convex rectangles/trapezoids. Test their bounds first,
        // then triangle inclusion so samples don't land outside the forward patch corners.
        if (local.x < mesh.bounds.min.x || local.x > mesh.bounds.max.x || local.z < mesh.bounds.min.z || local.z > mesh.bounds.max.z) return false;
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
            float ab = Cross(b - a, local - a), bc = Cross(c - b, local - b), ca = Cross(a - c, local - c);
            if ((ab >= -0.001f && bc >= -0.001f && ca >= -0.001f) || (ab <= 0.001f && bc <= 0.001f && ca <= 0.001f)) return true;
        }
        return false;
    }

    static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;
}
