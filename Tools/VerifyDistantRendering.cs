using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mavis;
using Object = UnityEngine.Object;

public static class VerifyDistantRendering
{
    const string Root = ".codex/far-view-settings-20261102";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
    static void Write(string name, object value) => File.WriteAllText(Root + "/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));

    public static object Culling()
    {
        if (Application.isPlaying) throw new Exception("Edit Mode fixture required");
        var root = new GameObject("Distant foliage fixture"); root.SetActive(false);
        var manager = root.AddComponent<WorldPerformanceManager>();
        var cells = (IList)typeof(WorldPerformanceManager).GetField("foliageCells", Hidden).GetValue(manager);
        var itemType = typeof(WorldPerformanceManager).GetNestedType("Foliage", BindingFlags.NonPublic);
        var cellType = typeof(WorldPerformanceManager).GetNestedType("FoliageCell", BindingFlags.NonPublic);
        var refresh = typeof(WorldPerformanceManager).GetMethod("RefreshFoliageWork", Hidden);
        var mesh = new Mesh(); mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }; mesh.triangles = new[] { 0, 1, 2 };
        int checks = 0;
        void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
        try
        {
            foreach (var kind in new[] { "grass", "legacy", "tree" })
            {
                float cutoff = kind == "grass" ? WorldPerformanceManager.GrassResidentDistance : kind == "legacy"
                    ? WorldPerformanceManager.LegacyFoliageResidentDistance : WorldPerformanceManager.FoliageResidentDistance;
                float before = kind == "grass" ? 190 : kind == "legacy" ? 260 : 550;
                var go = new GameObject(kind, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform, false);
                var renderer = go.GetComponent<MeshRenderer>(); var filter = go.GetComponent<MeshFilter>(); filter.sharedMesh = mesh;
                var item = Activator.CreateInstance(itemType, true); var cell = Activator.CreateInstance(cellType, true);
                Set(item, "renderer", renderer); Set(item, "filter", filter); Set(item, "original", mesh); Set(item, "currentMesh", mesh);
                Set(item, "grass", kind == "grass"); Set(item, "legacy", kind == "legacy");
                Set(cell, "grass", kind == "grass"); Set(cell, "cutoff", cutoff);
                ((IList)cellType.GetField("items").GetValue(cell)).Add(item); cells.Add(cell);
                void Refresh(float distance)
                {
                    var bounds = new Bounds(new Vector3(0, 0, distance), Vector3.one * 2);
                    Set(item, "bounds", bounds); Set(cell, "bounds", bounds);
                    var work = (IEnumerator)refresh.Invoke(manager, new object[] { Vector3.zero });
                    while (work.MoveNext()) { }
                }
                Refresh(before + 20); Check(!renderer.forceRenderingOff, kind + " visible beyond previous outer cutoff");
                Refresh(cutoff + 5); Check(renderer.forceRenderingOff, kind + " hidden outside new cutoff");
                Refresh(cutoff + .5f); Check(!renderer.forceRenderingOff, kind + " bounds edge retained");
                Refresh(10); Check(!renderer.forceRenderingOff, kind + " returns when nearby");
                Check(filter.sharedMesh == mesh, kind + " geometry unchanged");
                cells.Clear(); Object.DestroyImmediate(go);
            }
            var result = new { passed = true, checks, grass = WorldPerformanceManager.GrassResidentDistance,
                legacy = WorldPerformanceManager.LegacyFoliageResidentDistance, trees = WorldPerformanceManager.FoliageResidentDistance };
            Write("culling-fixtures", result); return result;
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(mesh); }
    }

    public static object ShaderCoverage()
    {
        if (!Application.isPlaying) throw new Exception("Play Mode required");
        Vector4 previous = Shader.GetGlobalVector("_MavisGrassFadeRange");
        Vector4 shadow = Shader.GetGlobalVector("_MavisGrassShadowRange");
        if (previous.x != 200 || previous.y != 240 || previous.w != 1) throw new Exception("Live manager did not publish the new range");
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Grass distance shader fixture"; quad.layer = 5;
        quad.transform.SetPositionAndRotation(new Vector3(5000, 1000, 5000), Quaternion.Euler(90, 0, 0)); quad.transform.localScale = Vector3.one * 20;
        var material = new Material(Shader.Find("Mavis/FoliageWind"));
        material.SetFloat("_ClipBelowSea", 1); material.SetFloat("_WindBend", 0); material.SetFloat("_LocalWindScale", 0); material.SetFloat("_PlayerPushStrength", 0);
        quad.GetComponent<MeshRenderer>().sharedMaterial = material;
        var camera = new GameObject("Grass coverage camera", typeof(Camera)).GetComponent<Camera>();
        camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 11; camera.aspect = 1;
        camera.cullingMask = 1 << 5; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.farClipPlane = 500;
        var data = camera.GetUniversalAdditionalCameraData(); data.renderShadows = false; data.renderPostProcessing = false;
        var target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
        var active = RenderTexture.active; var image = new Texture2D(256, 256, TextureFormat.RGB24, false);
        int Render(float distance, Vector4 range, string name)
        {
            Shader.SetGlobalVector("_MavisGrassFadeRange", range);
            camera.transform.SetPositionAndRotation(quad.transform.position + Vector3.up * distance, Quaternion.Euler(90, 0, 0));
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
            File.WriteAllBytes(Root + "/" + name + ".png", image.EncodeToPNG());
            return image.GetPixels32().Count(p => p.r > 4 || p.g > 4 || p.b > 4);
        }
        try
        {
            int before = Render(180, new Vector4(140, 176, 1f / 36, 1), "grass-old-180m");
            int after = Render(180, previous, "grass-new-180m");
            int fading = Render(220, previous, "grass-new-220m");
            int beyond = Render(245, previous, "grass-new-245m");
            if (before != 0 || after < 2000 || fading <= 100 || fading >= after || beyond != 0)
                throw new Exception("Native grass coverage failed: " + string.Join(",", before, after, fading, beyond));
            if (Shader.GetGlobalVector("_MavisGrassShadowRange") != shadow) throw new Exception("Shadow range changed");
            var result = new { passed = true, checks = 5, old180Pixels = before, new180Pixels = after, new220Pixels = fading, new245Pixels = beyond,
                description = "Fixed orthographic projection using the actual grass shader, independent of scene content/FPS" };
            Write("shader-coverage", result); return result;
        }
        finally
        {
            Shader.SetGlobalVector("_MavisGrassFadeRange", previous); RenderTexture.active = active; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image); Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(quad); Object.DestroyImmediate(material);
        }
    }
}
