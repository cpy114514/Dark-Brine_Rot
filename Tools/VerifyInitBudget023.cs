using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;

public static class VerifyInitBudget023
{
    const string Root = ".codex/performance023";
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly FieldInfo Fixed = typeof(WorldPerformanceManager).GetField("ForceFixedInitializationBudget", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo Budget = typeof(WorldPerformanceManager).GetMethod("InitializationBudget", BindingFlags.Static | BindingFlags.NonPublic);
    static double Read(bool initial, float seconds) => (double)Budget.Invoke(null, new object[] { initial, seconds });
    static async Task Frame() { int n = Time.frameCount; var end = DateTime.UtcNow.AddSeconds(30); while (n == Time.frameCount) { if (!Application.isPlaying || DateTime.UtcNow > end) throw new Exception("Native frame timeout"); await Task.Delay(5); } }
    public static object PrepareScene()
    {
        if (Application.isPlaying || File.Exists(Root + "/native-running")) throw new Exception("Stopped owned workspace required");
        if (EditorSceneManager.GetSceneManagerSetup().Any(s => SceneManager.GetSceneByPath(s.path).isDirty)) throw new Exception("Preserve dirty scenes");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); return new { isolated = true };
    }
    public static object Policy()
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Isolated empty Play required");
        bool previous = (bool)Fixed.GetValue(null); var rows = new List<object>();
        Action<bool, string> check = (ok, label) => { rows.Add(new { ok, label }); if (!ok) throw new Exception(label); };
        try
        {
            Fixed.SetValue(null, false);
            check(Math.Abs(Read(true, 1f / 60) - .002) < 1e-8, "60fps initial work retains its original2ms cap");
            check(Math.Abs(Read(true, .13f) - .008) < 1e-8, "Slow initial frames use at most8ms");
            check(Read(true, 1f / 15) > .004 && Read(true, 1f / 15) < .0043, "Intermediate frame rates receive a bounded fraction");
            foreach (float seconds in new[] { 0f, .004f, .016f, .033f, .066f, .13f, .3f, 1f })
            {
                check(Read(true, seconds) >= .002 - 1e-8 && Read(true, seconds) <= .008 + 1e-8, "Initial budget bounds at" + seconds);
                check(Math.Abs(Read(false, seconds) - .002) < 1e-8, "Interactive recache remains2ms at" + seconds);
            }
            Fixed.SetValue(null, true); check(Math.Abs(Read(true, .13f) - .002) < 1e-8, "Fixed-budget A/B control exactly retains the old policy");
            var result = new { passed = true, rows }; File.WriteAllText(Root + "/budget-verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented)); return result;
        }
        finally { Fixed.SetValue(null, previous); }
    }
    public static object EditorPolicy()
    {
        if (Application.isPlaying) throw new Exception("Stopped Editor required");
        bool ok = Math.Abs(Read(true, .13f) - .002) < 1e-8;
        if (!ok) throw new Exception("Editor policy changed"); return new { editorBudgetUnchanged = ok };
    }
    static float[] Snapshot(GameObject root)
    {
        var values = new List<float>(); var block = new MaterialPropertyBlock();
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>().OrderBy(r => r.GetInstanceID()))
        {
            for (int index = -1; index < renderer.sharedMaterials.Length; index++)
            {
                block.Clear(); if (index < 0) renderer.GetPropertyBlock(block); else renderer.GetPropertyBlock(block, index);
                foreach (string name in new[] { "_MavisWindAnchorY", "_MavisWindInvHeight", "_MavisWindResponse", "_WindTrunkStiffness", "_PlayerPushStrength" }) values.Add(block.GetFloat(name));
            }
        }
        return values.ToArray();
    }
    public static object Wind()
    {
        if (!Application.isPlaying || File.Exists(Root + "/wind-running") || File.Exists(Root + "/wind-equivalence.json")) throw new Exception("Fresh owned wind fixture required");
        File.WriteAllText(Root + "/wind-running", "native"); _ = WindRun(); return new { started = true };
    }
    public static object ResumeOwnedFixture()
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path) || File.Exists(Root + "/wind-running")) throw new Exception("Stopped owned wind test required");
        var root = EditorUtility.EntityIdToObject(SessionState.GetInt("AUTO_QA.Foliage016.Root", 0)) as GameObject;
        if (!root) throw new Exception("Owned foliage root missing");
        foreach (Transform child in root.transform.Cast<Transform>().Where(t => t.name.StartsWith("Owned wind policy ", StringComparison.Ordinal)).ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m => m.gameObject.scene.IsValid()).enabled = true;
        return new { fixtureResumed = true };
    }
    static async Task WindRun()
    {
        var root = EditorUtility.EntityIdToObject(SessionState.GetInt("AUTO_QA.Foliage016.Root", 0)) as GameObject;
        var manager = Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m => m.gameObject.scene.IsValid());
        bool enabled = manager.enabled, previous = (bool)Fixed.GetValue(null); FoliageWindDriver driver = null;
        var clones = new List<Material>(); var extras = new List<GameObject>(); var direction = Shader.GetGlobalVector("_MavisWindDir"); var wind = Shader.GetGlobalVector("_MavisWindParams");
        try
        {
            if (!root) throw new Exception("Authored foliage fixture missing"); manager.enabled = false;
            var source = root.GetComponentInChildren<MeshRenderer>(); var material = source.sharedMaterial;
            if (!material || material.shader.name != "Mavis/FoliageWind") throw new Exception("Actual foliage shader required");
            foreach (string name in new[] { "bark", "branch", "leaves", "grass", "mixed" })
            {
                var obj = new GameObject("Owned wind policy " + name, typeof(MeshFilter), typeof(MeshRenderer)); extras.Add(obj); obj.transform.SetParent(root.transform); obj.transform.localPosition = Vector3.right * clones.Count;
                obj.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                string[] names = name == "mixed" ? new[] { "bark", "branch", "leaves" } : new[] { name };
                var materials = names.Select(n => { var m = new Material(material) { name = "Owned wind " + n, hideFlags = HideFlags.DontSave }; clones.Add(m); return m; }).ToArray();
                obj.GetComponent<MeshRenderer>().sharedMaterials = materials;
            }
            driver = root.AddComponent<FoliageWindDriver>(); driver.enabled = false;
            var workMethod = typeof(FoliageWindDriver).GetMethod("CacheRendererProfilesWork", Instance); var initial = typeof(FoliageWindDriver).GetField("profilesInitialized", Instance);
            Fixed.SetValue(null, true); initial.SetValue(driver, false); var work = (IEnumerator)workMethod.Invoke(driver, null);
            while (work.MoveNext()) await Frame(); var before = Snapshot(root);
            Fixed.SetValue(null, false); initial.SetValue(driver, false); work = (IEnumerator)workMethod.Invoke(driver, null);
            while (work.MoveNext()) await Frame(); var after = Snapshot(root);
            if (!before.SequenceEqual(after) || !(bool)initial.GetValue(driver)) throw new Exception("Wind profiles differ between budget policies");
            File.WriteAllText(Root + "/wind-equivalence.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, comparedFloats = before.Length, renderers = root.GetComponentsInChildren<MeshRenderer>().Length, initialized = true, scope = "Actual authored grass/tree meshes plus owned bark/branch/leaf/grass/mixed material cases; fixed/adaptive profile values identical, original assets untouched" }, Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception e) { File.WriteAllText(Root + "/wind-error.txt", e.ToString()); }
        finally
        {
            if (driver) UnityEngine.Object.DestroyImmediate(driver); foreach (var extra in extras) if (extra) UnityEngine.Object.DestroyImmediate(extra); foreach (var clone in clones) if (clone) UnityEngine.Object.DestroyImmediate(clone);
            Shader.SetGlobalVector("_MavisWindDir", direction); Shader.SetGlobalVector("_MavisWindParams", wind); Fixed.SetValue(null, previous);
            manager.enabled = enabled; File.Delete(Root + "/wind-running");
        }
    }
}
