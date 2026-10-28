using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Mavis;

public static class AutoQaFoliageReload
{
    const string Key = "AUTO_QA.Foliage016";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
    static void Set(object o, string n, object value) => o.GetType().GetField(n, Flags).SetValue(o, value);
    static WorldPerformanceManager Manager => Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m => m.gameObject.scene.IsValid() && m.isActiveAndEnabled);
    static object Write(string suffix, object value)
    {
        File.WriteAllText("AUTO_QA/Evidence/" + SessionState.GetString(Key + ".Take", "") + suffix + ".json",
            Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));
        return value;
    }
    static void Guard()
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            throw new Exception("Isolated Play required");
    }
    static async Task Frames(int n)
    {
        int stop = Time.frameCount + n;
        var limit = DateTime.UtcNow.AddSeconds(10);
        while (Time.frameCount < stop)
        {
            if (!Application.isPlaying || DateTime.UtcNow > limit) throw new Exception("Frame timeout");
            await Task.Delay(5);
        }
    }
    static async Task Ready()
    {
        var limit = DateTime.UtcNow.AddSeconds(12);
        while (!Manager.IsReady)
        {
            if (DateTime.UtcNow > limit) throw new Exception("Cache timeout");
            await Frames(1);
        }
        await Frames(18);
    }
    static void Check(List<object> rows, bool ok, string label) => rows.Add(new { ok, label });
    static int Count(object o, string field) => ((ICollection)Get(o, field)).Count;

    public static async Task<object> Prepare(string take)
    {
        Guard();
        if (!string.IsNullOrEmpty(SessionState.GetString(Key + ".Take", ""))) throw new Exception("Fixture already owned");
        SessionState.SetString(Key + ".Take", take);
        SessionState.SetString(Key + ".Assembly", typeof(AutoQaFoliageReload).Assembly.GetName().Name);
        var root = new GameObject("AUTO_QA Foliage reload");
        SessionState.SetInt(Key + ".Root", root.GetInstanceID());
        var library = Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");
        if (!library || library.entries == null) throw new Exception("Authored foliage library missing");
        var grass = library.entries.First(e => e.grass && e.source);
        var tree = library.entries.First(e => !e.grass && e.source);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass_0.mat");
        if (!material) throw new Exception("Authored grass material missing");
        SessionState.SetInt(Key + ".Material", material.GetInstanceID());
        for (int i = 0; i < 36; i++)
        {
            var obj = new GameObject("AUTO_QA authored foliage " + i, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(root.transform);
            obj.transform.position = new Vector3((i % 6 - 3) * 3, 0, i / 6 * 3);
            var mesh = i == 35 ? tree.source : grass.source;
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            var size = mesh.bounds.size; float scale = (i == 35 ? 4f : 2f) / Mathf.Max(size.x, size.y, size.z);
            obj.transform.localScale = Vector3.one * scale;
            obj.transform.position -= Vector3.up * mesh.bounds.min.y * scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        var view = new GameObject("AUTO_QA foliage camera", typeof(Camera));
        view.transform.SetParent(root.transform);
        view.tag = "MainCamera";
        view.transform.SetPositionAndRotation(new Vector3(0, 8, -14), Quaternion.LookRotation(new Vector3(0, -4, 16)));
        var camera = view.GetComponent<Camera>();
        camera.fieldOfView = 55; camera.farClipPlane = 100;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.02f, .03f, .12f);
        var render = new RenderTexture(128, 128, 24) { hideFlags = HideFlags.DontSave };
        render.Create(); camera.targetTexture = render;
        SessionState.SetInt(Key + ".Target", render.GetInstanceID());
        var manager = Manager;
        manager.enabled = false; manager.enabled = true;
        await Ready();
        SessionState.SetInt(Key + ".Manager", manager.GetInstanceID());
        SessionState.SetInt(Key + ".LibraryCount", library.entries.Where(e => e.source).Select(e => e.source).Distinct().Count());
        SessionState.SetInt(Key + ".Details", Count(manager, "details"));
        SessionState.SetInt(Key + ".RenderMeshes", Count(manager, "renderMeshes"));
        SessionState.SetInt(Key + ".Submission", manager.GetComponent<GrassRenderSubmission>().GetInstanceID());
        Capture("-before");
        camera.cullingMask = 0; await Frames(3); Capture("-empty-control"); camera.cullingMask = ~0; await Frames(3);
        return Write("-before", Snapshot(false));
    }

    static object Snapshot(bool afterReload)
    {
        var manager = Manager;
        var rows = new List<object>();
        if (afterReload) Check(rows, !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == SessionState.GetString(Key + ".Assembly", "")), "Actual script reload unloaded preparation assembly");
        Check(rows, Count(manager, "details") == SessionState.GetInt(Key + ".LibraryCount", -1), "Authored source-to-LOD library available");
        Check(rows, Get(manager, "visibleGrass") != null, "Partial-frustum scratch batch exists");
        Check(rows, manager.GetComponents<GrassRenderSubmission>().Length == 1, "Exactly one submission component");
        var submission = manager.GetComponent<GrassRenderSubmission>();
        Check(rows, Get(submission, "owner") as WorldPerformanceManager == manager, "Late submission owner retained");
        var entries = ((IList)Get(manager, "foliage")).Cast<object>().ToArray();
        Check(rows, entries.All(e => !(bool)e.GetType().GetField("forced").GetValue(e)), "No managed override captured as original visibility");
        Check(rows, entries.All(e => ((Material[])e.GetType().GetField("materials").GetValue(e)).All(v => v && !v.name.Contains("Runtime Instanced"))), "Original material ownership stays valid");
        Check(rows, Count(manager, "foliage") == SessionState.GetInt(Key + ".ExpectedCount", 36) && manager.IsReady, "All authored fixture renderers classified and ready");
        Check(rows, Count(manager, "activeGrassBatches") > 0, "Complete grass draw snapshot exists");
        Check(rows, (int)Get(manager, "submittedGrassBatches") > 0, "Native late update submits visible grass batches");
        if (afterReload)
        {
            Check(rows, Count(manager, "details") == SessionState.GetInt(Key + ".Details", -1), "LOD map cardinality unchanged across reload");
            Check(rows, Count(manager, "renderMeshes") == SessionState.GetInt(Key + ".RenderMeshes", -1), "Compact render layout map preserved across reload");
        }
        return new { rows, details = Count(manager, "details"), renderMeshes = Count(manager, "renderMeshes"), foliage = Count(manager, "foliage"), batches = Count(manager, "activeGrassBatches"), submitted = Get(manager, "submittedGrassBatches"), frame = Time.frameCount };
    }

    static void Capture(string suffix)
    {
        var rt = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + ".Target", 0)) as RenderTexture;
        var previous = RenderTexture.active;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        try { RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
            File.WriteAllBytes("AUTO_QA/Evidence/" + SessionState.GetString(Key + ".Take", "") + suffix + ".png", image.EncodeToPNG()); }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
    }

    public static async Task<object> AfterReload()
    {
        Guard(); await Frames(20);
        Capture("-after");
        return Write("-after", Snapshot(true));
    }

    public static async Task<object> Lifecycle()
    {
        Guard(); var manager = Manager; var rows = new List<object>();
        for (int i = 0; i < 3; i++)
        {
            manager.enabled = false;
            var root = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + ".Root", 0)) as GameObject;
            Check(rows, root.GetComponentsInChildren<MeshRenderer>().All(r => !r.forceRenderingOff), "Disable restores renderer visibility " + i);
            Check(rows, Count(manager, "activeGrassBatches") == 0, "Disable clears draw snapshots " + i);
            manager.enabled = true; await Ready();
            Check(rows, manager.GetComponents<GrassRenderSubmission>().Length == 1, "Reenable retains one submission " + i);
            Check(rows, Count(manager, "foliage") == SessionState.GetInt(Key + ".ExpectedCount", 36) && Count(manager, "activeGrassBatches") > 0, "Reenable restores complete cache " + i);
        }
        return Write("-lifecycle", new { rows });
    }

    public static object CompareCaptures()
    {
        Guard(); var take = SessionState.GetString(Key + ".Take", "");
        var images = new List<Texture2D>();
        try
        {
            foreach (var suffix in new[] { "-before", "-after", "-empty-control" })
            {
                var image = new Texture2D(2, 2); images.Add(image);
                if (!image.LoadImage(File.ReadAllBytes("AUTO_QA/Evidence/" + take + suffix + ".png"))) throw new Exception("PNG decode failed");
            }
            var a = images[0].GetPixels32(); var b = images[1].GetPixels32(); var c = images[2].GetPixels32();
            int changed = 0, foreground = 0, maxError = 0;
            for (int i = 0; i < a.Length; i++)
            {
                int error = Math.Max(Math.Abs(a[i].r-b[i].r), Math.Max(Math.Abs(a[i].g-b[i].g), Math.Abs(a[i].b-b[i].b)));
                maxError = Math.Max(maxError, error); if (error > 0) changed++;
                if (!a[i].Equals(c[i])) foreground++;
            }
            return Write("-pixels", new { ok = changed == 0 && foreground > 50, pixels = a.Length, changed, foreground, maxError });
        }
        finally { foreach (var image in images) UnityEngine.Object.DestroyImmediate(image); }
    }

    public static async Task<object> SceneLoad()
    {
        Guard(); var manager = Manager;
        var root = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + ".Root", 0)) as GameObject;
        var renderer = root.GetComponentInChildren<MeshRenderer>();
        int oldMaterial = renderer.sharedMaterial.GetInstanceID();
        var operation = SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Lighting.unity", LoadSceneMode.Additive);
        var limit = DateTime.UtcNow.AddSeconds(12);
        while (!operation.isDone) { if (DateTime.UtcNow > limit) throw new Exception("Additive load timeout"); await Frames(1); }
        await Ready();
        var rows = new List<object>();
        Check(rows, renderer.sharedMaterial.GetInstanceID() != oldMaterial, "Native additive sceneLoaded event refreshes managed renderers");
        Check(rows, manager.IsReady && Count(manager, "foliage") == SessionState.GetInt(Key + ".ExpectedCount", 36), "Additive recache retains all fixture foliage");
        Check(rows, manager.GetComponents<GrassRenderSubmission>().Length == 1, "Scene recache retains one submission");
        var entries = ((IList)Get(manager, "foliage")).Cast<object>().ToArray();
        Check(rows, entries.All(e => !(bool)e.GetType().GetField("forced").GetValue(e)), "Scene recache retains original visibility ownership");
        Check(rows, entries.All(e => ((Material[])e.GetType().GetField("materials").GetValue(e)).All(m => m && !m.name.Contains("Runtime Instanced"))), "Scene recache retains original material identities");
        operation = SceneManager.UnloadSceneAsync("Assets/Scenes/First Island/Lighting.unity");
        while (!operation.isDone) { if (DateTime.UtcNow > limit) throw new Exception("Unload timeout"); await Frames(1); }
        await Frames(3);
        Check(rows, SceneManager.sceneCount == 1, "Loaded diagnostic lighting scene removed");
        return Write("-scene-load", new { rows });
    }

    public static async Task<object> Interrupt()
    {
        Guard(); var manager = Manager;
        var root = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + ".Root", 0)) as GameObject;
        var material = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + ".Material", 0)) as Material;
        var mesh = Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library").entries.First(e => e.grass && e.source).source;
        for (int i = 0; i < 600; i++)
        {
            var obj = new GameObject("AUTO_QA interrupted grass " + i, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(root.transform); obj.transform.position = new Vector3(i % 30, 0, i / 30);
            obj.transform.localScale = Vector3.one * .01f;
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        typeof(WorldPerformanceManager).GetMethod("OnSceneLoaded", Flags).Invoke(manager, new object[] { SceneManager.GetActiveScene(), LoadSceneMode.Additive });
        var deadline = DateTime.UtcNow.AddSeconds(12); int partial = 0;
        while (DateTime.UtcNow < deadline)
        {
            partial = Count(manager, "foliage");
            if (partial > 0 && partial != 36 && partial < 636 && Get(manager, "cacheRoutine") != null) break;
            await Frames(1);
        }
        if (partial <= 0 || partial == 36 || partial >= 636) throw new Exception("Interrupted-cache positive control missed");
        manager.enabled = false; manager.enabled = true; await Ready();
        var entries = ((IList)Get(manager, "foliage")).Cast<object>().ToArray();
        var rows = new List<object>();
        Check(rows, partial > 0 && partial < 636, "Cancellation occurs during native bounded scan");
        Check(rows, entries.Length == 636, "Replacement cache owns each renderer exactly once");
        Check(rows, entries.All(e => !(bool)e.GetType().GetField("forced").GetValue(e)), "Replacement does not adopt cancelled scan culling");
        Check(rows, entries.All(e => ((Material[])e.GetType().GetField("materials").GetValue(e)).All(v => v && !v.name.Contains("Runtime Instanced"))), "Replacement retains original materials");
        manager.enabled = false;
        Check(rows, root.GetComponentsInChildren<MeshRenderer>().All(r => !r.forceRenderingOff && r.sharedMaterial == material), "Disable after interruption restores every original renderer");
        manager.enabled = true; await Ready();
        return Write("-interrupted-cache", new { partial, rows });
    }

    public static object Cleanup()
    {
        Guard(); var manager = Manager; manager.enabled = false;
        foreach (string n in new[] { "Root", "Target" })
        {
            var obj = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + "." + n, 0));
            if (obj is RenderTexture rt) rt.Release();
            if (obj) UnityEngine.Object.DestroyImmediate(obj);
        }
        manager.enabled = true;
        var result = Write("-cleanup", new { cleaned = true, fixtures = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length });
        foreach (string n in new[] { "Root", "Material", "Target", "Manager", "LibraryCount", "Details", "RenderMeshes", "Submission", "ExpectedCount" }) SessionState.EraseInt(Key + "." + n);
        SessionState.EraseString(Key + ".Assembly"); SessionState.EraseString(Key + ".Take");
        return result;
    }
}
