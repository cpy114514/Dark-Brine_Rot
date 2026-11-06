using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mavis;
using Object = UnityEngine.Object;

// Run via Pipeline run_script. The baseline is captured from Git HEAD; never added to Assets.
public static class VerifyProjectMaintenance
{
    const string Evidence = ".codex/project-maintenance-20261103";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    static Action Method(object target, string name) => (Action)Delegate.CreateDelegate(typeof(Action), target, target.GetType().GetMethod(name, Fields));
    static int checks;
    static void Check(bool passed, string reason) { checks++; if (!passed) throw new Exception(reason); }

    public static object PreparePlay()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first");
        string path = (string)typeof(GameSaveManager).GetProperty("SavePath", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var files = new[] { path, path + ".bak", path + ".tmp" }.Select(p => new
        { path = p, bytes = File.Exists(p) ? Convert.ToBase64String(File.ReadAllBytes(p)) : null }).ToArray();
        string destination = Evidence + "/protected-save.json";
        if (File.Exists(destination)) throw new Exception("Keep previous save snapshot");
        File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(files));
        return new { protectedFiles = files.Length };
    }

    public static object SmokePlay()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode required");
        typeof(GameSaveManager).GetField("editorSavePath", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, Path.GetFullPath(Evidence + "/isolated-play-save.json"));
        var player = Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        if (!player) throw new Exception("No live player");
        var map = player.GetComponent<IslandMapUI>();
        if (!map) throw new Exception("No live map");
        bool wasOpen = map.PanelVisible;
        try
        {
            if (!wasOpen && !map.SetOpen(true)) throw new Exception("Live map refused opening");
            Method(map, "RefreshMarkers")();
            var markers = (IList)Get(map, "markers");
            if (!map.PanelVisible || map.MapGraphic == null || markers.Count == 0) throw new Exception("Live map not populated");
            var result = new { passed = true, playMode = true, mapMarkers = markers.Count, islandCount = map.MapGraphic.IslandCount,
                guidancePresent = player.GetComponent<MainQuestGuidance>() != null, mapPaused = Time.timeScale == 0 };
            File.WriteAllText(Evidence + "/play-smoke.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally { if (!wasOpen && map.PanelVisible) map.SetOpen(false); }
    }

    public static object RestorePlay()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before restoring save");
        var files = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Evidence + "/protected-save.json"));
        foreach (var file in files)
        {
            string path = (string)file["path"], bytes = (string)file["bytes"];
            if (bytes == null) { if (File.Exists(path)) File.Delete(path); }
            else File.WriteAllBytes(path, Convert.FromBase64String(bytes));
        }
        return new { restored = files.Count };
    }

    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required");
        checks = 0;
        var active = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var roots = new List<GameObject>();
        GameObject Root(string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene); roots.Add(go); return go;
        }
        float timeScale = Time.timeScale;
        var cursor = Cursor.lockState; bool cursorVisible = Cursor.visible;
        try
        {
            var referenceType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Mavis.MaintenanceReferenceGuidance")).FirstOrDefault(t => t != null);
            if (referenceType == null) throw new Exception("Compile retained baseline alongside this verifier");
            var before = Root("Reference guidance").AddComponent(referenceType);
            var after = Root("Optimized guidance").AddComponent<MainQuestGuidance>();
            Method(before, "Ensure")(); Method(after, "Ensure")();
            var referenceDraw = Method(before, "Draw"); var optimizedDraw = Method(after, "Draw");
            var poses = new[] { Vector3.one, new Vector3(2, .7f, 1.3f), new Vector3(.3f, .3f, .3f) };
            foreach (var scale in poses)
            foreach (int count in new[] { 2, 19, 257, 1024 })
            {
                foreach (Component target in new[] { before, after })
                {
                    target.transform.position = new Vector3(5, 2, -3);
                    target.transform.rotation = Quaternion.Euler(7, 37, 11);
                    target.transform.localScale = scale;
                    var route = (Vector3[])Get(target, "route"); var lengths = (float[])Get(target, "lengths");
                    for (int i = 0; i < count; i++)
                    {
                        route[i] = target.transform.position + new Vector3(Mathf.Sin(i * .1f) * 3, Mathf.Cos(i * .04f), i * 1.2f);
                        lengths[i] = i == 0 ? 0 : lengths[i - 1] + Vector3.Distance(route[i - 1], route[i]);
                    }
                    Set(target, "routeCount", count); Set(target, "fade", .75f);
                }
                referenceDraw(); optimizedDraw();
                var a = (Mesh)Get(before, "mesh"); var b = (Mesh)Get(after, "mesh");
                Check(a.vertices.SequenceEqual(b.vertices), "Vertex mismatch");
                Check(a.normals.SequenceEqual(b.normals), "Normal mismatch");
                Check(a.uv.SequenceEqual(b.uv), "UV mismatch");
                Check(a.colors.SequenceEqual(b.colors), "Color mismatch");
                Check(a.triangles.SequenceEqual(b.triangles), "Topology mismatch");
                Check(a.bounds == b.bounds, "Bounds mismatch");
            }
            var timings = new List<object>();
            // Alternating reference/optimized windows measure this method only, not whole-game FPS.
            for (int round = 0; round < 4; round++)
            {
                foreach (bool optimized in new[] { round % 2 != 0, round % 2 == 0 })
                {
                    Action draw = optimized ? optimizedDraw : referenceDraw;
                    for (int i = 0; i < 10; i++) draw();
                    var watch = Stopwatch.StartNew();
                    for (int i = 0; i < 150; i++) draw();
                    watch.Stop(); timings.Add(new { optimized, round, iterations = 150, meanMs = watch.Elapsed.TotalMilliseconds / 150 });
                }
            }

            // Exercise the actual map refresh with live UI and mutable target state.
            var map = Root("Map fixture").AddComponent<IslandMapUI>();
            Method(map, "Build")();
            var graphic = map.MapGraphic;
            var generic = Root("Generic enemy").AddComponent<Health>();
            var supply = Root("Supply").AddComponent<IslandSupplyCache>();
            generic.transform.position = new Vector3(10, 0, 10);
            supply.transform.position = new Vector3(-10, 0, 5);
            var add = typeof(IslandMapUI).GetMethod("AddMarker", Fields);
            add.Invoke(map, new object[] { generic.transform, "C", Color.white, 28 });
            add.Invoke(map, new object[] { supply.transform, "R", Color.white, 28 });
            var labels = (IList)Get(map, "markers");
            var first = (Text)labels[0]; var second = (Text)labels[1];
            var refresh = Method(map, "RefreshMarkers");
            refresh();
            Check(first.gameObject.activeSelf, "Living target hidden");
            Check(first.rectTransform.anchoredPosition == graphic.WorldToMap(generic.transform.position), "Map marker misplaced");
            generic.currentHealth = 0; refresh(); Check(!first.gameObject.activeSelf, "Dead target visible");
            generic.currentHealth = 100; refresh(); Check(first.gameObject.activeSelf, "Restored target hidden");
            generic.gameObject.SetActive(false); refresh(); Check(!first.gameObject.activeSelf, "Inactive target visible");
            supply.RestoreRemaining(30); refresh(); Check(second.color == GameUITheme.Muted, "Unavailable supply color wrong");
            supply.RestoreRemaining(0); refresh(); Check(second.color == new Color(1, .83f, .2f), "Available supply color wrong");
            Object.DestroyImmediate(generic.gameObject); refresh(); Check(!first.gameObject.activeSelf, "Destroyed target visible");
            // A cached reward is allowed to disappear while the map is open.
            var reward = Root("Reward").AddComponent<GravityWristbandReward>();
            reward.enemyRoot = Root("Camps").transform;
            var camp = Root("Camp").transform; camp.SetParent(reward.enemyRoot, false);
            var anchor = Root("Camp map anchor").transform; anchor.SetParent(camp, false);
            Set(reward, "campRemaining", new[] { 1 }); Set(map, "wristband", reward);
            add.Invoke(map, new object[] { anchor, "C", Color.white, 28 });
            var campLabel = (Text)labels[2]; refresh();
            Check(campLabel.color == new Color(1f, .7f, .25f), "Uncleared camp color wrong");
            Set(reward, "campRemaining", new[] { 0 }); refresh();
            Check(campLabel.color == GameUITheme.Muted, "Cleared camp color wrong");
            Object.DestroyImmediate(reward.gameObject); refresh();
            Check(labels.Count == 3, "Map refresh rebuilt markers");
            // Runtime marker disposal uses Destroy. Dispose fixture objects immediately in Edit Mode.
            foreach (Text label in labels) if (label) Object.DestroyImmediate(label.gameObject);
            labels.Clear(); ((IList)Get(map, "targets")).Clear();
            Method(map, "RebuildMarkers")();
            Check(!(GravityWristbandReward)Get(map, "wristband"), "Map rebuild kept stale reward");
            var result = new { passed = true, checks, geometryCases = 12, guidanceTimings = timings, mode = "Editor native mesh and UI fixtures; no Player/FPS claim" };
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            foreach (var go in roots)
            {
                if (!go) continue;
                foreach (var component in go.GetComponents<MonoBehaviour>())
                {
                    string name = component.GetType().Name;
                    if (name == "MainQuestGuidance" || name == "MaintenanceReferenceGuidance")
                        component.GetType().GetMethod("Dispose", Fields).Invoke(component, new object[] { true });
                    if (component is IslandMapUI)
                    {
                        var canvas = (GameObject)Get(component, "root");
                        if (canvas) Object.DestroyImmediate(canvas);
                        if ((bool)Get(component, "ownsFont"))
                        {
                            var font = (Font)Get(component, "font");
                            if (font) Object.DestroyImmediate(font);
                        }
                        Set(component, "ownsFont", false);
                    }
                }
                Object.DestroyImmediate(go);
            }
            EditorSceneManager.CloseScene(scene, true);
            if (active.IsValid()) SceneManager.SetActiveScene(active);
            Time.timeScale = timeScale; Cursor.lockState = cursor; Cursor.visible = cursorVisible;
        }
    }
}
