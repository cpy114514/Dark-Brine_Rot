using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class RefineShipWreckEdges
{
    const string Evidence = ".codex/model-refinement/wreck";

    static JArray Rows() => JArray.Parse(File.ReadAllText(Evidence + "/edge-surfaces.json"));

    static Dictionary<string, Mesh> ProductionMeshes()
    {
        var asset = Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        return asset.decks.Concat(asset.hulls)
            .SelectMany(piece => new[] { piece.mesh }.Concat(piece.splinters.Select(s => s.mesh)))
            .Distinct().ToDictionary(mesh => mesh.name);
    }

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        var meshes = ProductionMeshes();
        var work = Rows().Select(row =>
        {
            var mesh = meshes[(string)row["name"]];
            var vertices = Vectors(row["vertices"]);
            var indices = row["triangles"].ToObject<int[]>();
            var uv = Uvs(row["uv"]);
            var normals = Vectors(row["normals"]);
            if (!mesh.vertices.SequenceEqual(vertices) || !mesh.triangles.SequenceEqual(indices))
                throw new InvalidDataException("Production wreck geometry differs: " + mesh.name);
            bool original = mesh.uv.SequenceEqual(Uvs(row["originalUv"])) && mesh.normals.SequenceEqual(Vectors(row["originalNormals"]));
            bool installed = mesh.uv.SequenceEqual(uv) && mesh.normals.SequenceEqual(normals);
            if (!original && !installed) throw new InvalidDataException("Wreck surface has separate edits: " + mesh.name);
            return (mesh, uv, normals, installed, triangles: indices.Length / 3, cuts: ((JArray)row["cutFaces"]).Count);
        }).ToArray();

        string backupFolder = Evidence + "/mesh-backups";
        Directory.CreateDirectory(backupFolder);
        foreach (var item in work.Where(item => !item.installed))
        {
            string path = AssetDatabase.GetAssetPath(item.mesh);
            string backup = backupFolder + "/" + Path.GetFileName(path);
            if (!File.Exists(backup)) File.Copy(path, backup);
        }
        long bytes = work.Sum(item => Profiler.GetRuntimeMemorySizeLong(item.mesh));
        foreach (var item in work.Where(item => !item.installed))
        {
            item.mesh.uv = item.uv;
            item.mesh.normals = item.normals;
            item.mesh.RecalculateTangents();
            EditorUtility.SetDirty(item.mesh);
            AssetDatabase.SaveAssetIfDirty(item.mesh);
        }
        var result = new { meshes = work.Length, totalTriangles = work.Sum(item => item.triangles),
            correctedFaces = work.Sum(item => item.cuts), originalBytes = bytes,
            refinedBytes = work.Sum(item => Profiler.GetRuntimeMemorySizeLong(item.mesh)),
            geometryIdentical = true, materialsAndAssetReferencesPreserved = true };
        File.WriteAllText(Evidence + "/unity-install.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static async Task<object> Review()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Isolated Play required");
        GameSaveManager.CancelPendingContinue();
        var scene = SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity", LoadSceneMode.Single);
        while (!scene.isDone) await Task.Delay(40);
        await Task.Delay(400);
        var sequence = UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        var battle = sequence.GetComponent<Story1WreckBattle>();
        sequence.BeginImpact();
        var deadline = DateTime.UtcNow.AddSeconds(28);
        while (battle.CurrentPhase != Story1WreckBattle.Phase.Fighting && DateTime.UtcNow < deadline)
            await Task.Delay(40);
        if (battle.CurrentPhase != Story1WreckBattle.Phase.Fighting || !battle.EntryDeck || sequence.ship.gameObject.activeInHierarchy)
            throw new InvalidOperationException("Live story wreck did not open");

        var rows = Rows().ToDictionary(row => (string)row["name"]);
        var filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(filter => filter.sharedMesh && rows.ContainsKey(filter.sharedMesh.name)).ToArray();
        var entry = battle.EntryDeck.GetComponent<MeshFilter>();
        if (!entry || !rows.ContainsKey(entry.sharedMesh.name)) throw new InvalidOperationException("No refined entry deck in live wreck");
        var hull = filters.Where(filter => filter.sharedMesh.name.StartsWith("hull_"))
            .OrderBy(filter => Vector3.Distance(filter.transform.position, entry.transform.position)).First();
        var objects = new[] { (name: "deck", filter: entry), (name: "hull", filter: hull) };
        var probe = new GameObject("Isolated live wreck surface review", typeof(Camera));
        var camera = probe.GetComponent<Camera>();
        camera.CopyFrom(sequence.storyCamera);
        camera.enabled = false;
        camera.fieldOfView = 42;
        var target = new RenderTexture(1280, 720, 24);
        var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var priorTarget = RenderTexture.active;
        var style = Mavis.VisualStyles.VisualStyleManager.Instance;
        float previousTime = Time.timeScale;
        Time.timeScale = 0;
        style.Select("realistic", false);
        var samples = new List<object>();
        try
        {
            foreach (var (name, filter) in objects)
            {
                var production = filter.sharedMesh;
                var row = rows[production.name];
                if (!production.uv.SequenceEqual(Uvs(row["uv"])))
                    throw new InvalidOperationException("Refined UV missing in live scene");
                var before = UnityEngine.Object.Instantiate(production);
                try
                {
                    before.uv = Uvs(row["originalUv"]);
                    before.normals = Vectors(row["originalNormals"]);
                    before.RecalculateTangents();
                    var renderer = filter.GetComponent<Renderer>();
                    var bounds = renderer.bounds;
                    var direction = (filter.transform.right * .9f + filter.transform.forward * .45f + Vector3.up * .34f).normalized;
                    camera.transform.position = bounds.center + direction * bounds.size.magnitude * 1.23f;
                    camera.transform.LookAt(bounds.center);
                    foreach (bool refined in new[] { false, true })
                    {
                        filter.sharedMesh = refined ? production : before;
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                        RenderTexture.active = target;
                        shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                        shot.Apply();
                        File.WriteAllBytes(Evidence + "/" + name + (refined ? "-after" : "-before") + ".png", shot.EncodeToPNG());
                    }
                    var collision = filter.GetComponent<Story1WreckMeshCollision>();
                    if (!collision || collision.Parts.Length == 0) throw new InvalidOperationException("Live wreck collision missing");
                    samples.Add(new { name, mesh = production.name, material = renderer.sharedMaterial.name, collisionParts = collision.Parts.Length });
                }
                finally
                {
                    filter.sharedMesh = production;
                    UnityEngine.Object.DestroyImmediate(before);
                }
            }
            var result = new { phase = battle.CurrentPhase.ToString(), battle.ShipFragmentCount, battle.PlankCount,
                liveRefinedMeshes = filters.Length, samples };
            File.WriteAllText(Evidence + "/unity-story-review.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            RenderTexture.active = priorTarget;
            target.Release();
            foreach (var obj in new UnityEngine.Object[] { target, shot, probe }) UnityEngine.Object.DestroyImmediate(obj);
            Time.timeScale = previousTime;
            style.RestorePreference();
        }
    }

    public static object Closeups()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Isolated Play required");
        var asset = Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        var rows = Rows().ToDictionary(row => (string)row["name"]);
        var sources = new[]
        {
            asset.decks.Select(piece => (piece.mesh, piece.material, size: piece.sourceSize)),
            asset.hulls.Select(piece => (piece.mesh, piece.material, size: piece.sourceSize)),
            asset.decks.SelectMany(piece => piece.splinters.Select(shard =>
                (shard.mesh, piece.material, size: Vector3.Scale(piece.sourceSize, shard.localSize))))
        };
        var names = new[] { "detail-deck", "detail-hull", "detail-splinter" };
        var probe = new GameObject("Wreck wood detail camera", typeof(Camera));
        var camera = probe.GetComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.11f, .13f, .16f);
        camera.fieldOfView = 35;
        var target = new RenderTexture(1000, 650, 24);
        var shot = new Texture2D(1000, 650, TextureFormat.RGB24, false);
        var prior = RenderTexture.active;
        var report = new List<object>();
        var style = Mavis.VisualStyles.VisualStyleManager.Instance;
        style.Select("realistic", false);
        try
        {
            for (int view = 0; view < sources.Length; view++)
            {
                var chosen = sources[view].Where(source => rows.ContainsKey(source.mesh.name))
                    .Select(source =>
                    {
                        var row = rows[source.mesh.name];
                        var points = source.mesh.vertices;
                        var face = row["cutFaces"].ToObject<int[]>().Select(i =>
                        {
                            var a = Vector3.Scale(points[3 * i], source.size);
                            var b = Vector3.Scale(points[3 * i + 1], source.size);
                            var c = Vector3.Scale(points[3 * i + 2], source.size);
                            return (center: (a + b + c) / 3, areaNormal: Vector3.Cross(b - a, c - a));
                        }).OrderByDescending(face => face.areaNormal.sqrMagnitude).First();
                        return (source, row, face);
                    }).OrderByDescending(item => item.face.areaNormal.sqrMagnitude).First();
                var model = new GameObject("Existing wreck detail", typeof(MeshFilter), typeof(MeshRenderer));
                var before = UnityEngine.Object.Instantiate(chosen.source.mesh);
                try
                {
                    var filter = model.GetComponent<MeshFilter>();
                    model.transform.SetPositionAndRotation(new Vector3(4500, 1100, -4200), Quaternion.identity);
                    model.transform.localScale = chosen.source.size;
                    model.GetComponent<MeshRenderer>().sharedMaterial = chosen.source.material;
                    before.uv = Uvs(chosen.row["originalUv"]);
                    before.normals = Vectors(chosen.row["originalNormals"]);
                    before.RecalculateTangents();
                    var direction = (chosen.face.areaNormal.normalized + Vector3.up * .37f + Vector3.forward * .16f).normalized;
                    var center = model.transform.position + chosen.face.center * .55f;
                    camera.transform.position = center + direction * chosen.source.size.magnitude * 1.15f;
                    camera.transform.LookAt(center);
                    foreach (bool refined in new[] { false, true })
                    {
                        filter.sharedMesh = refined ? chosen.source.mesh : before;
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                        RenderTexture.active = target;
                        shot.ReadPixels(new Rect(0, 0, 1000, 650), 0, 0);
                        shot.Apply();
                        File.WriteAllBytes(Evidence + "/" + names[view] + (refined ? "-after" : "-before") + ".png", shot.EncodeToPNG());
                    }
                    report.Add(new { view = names[view], mesh = chosen.source.mesh.name, material = chosen.source.material.name });
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(model);
                    UnityEngine.Object.DestroyImmediate(before);
                }
            }
            File.WriteAllText(Evidence + "/unity-closeups.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            RenderTexture.active = prior;
            target.Release();
            foreach (var obj in new UnityEngine.Object[] { target, shot, probe }) UnityEngine.Object.DestroyImmediate(obj);
            style.RestorePreference();
        }
    }

    static Vector3[] Vectors(JToken values)
    {
        var components = values.ToObject<float[]>();
        var result = new Vector3[components.Length / 3];
        for (int i = 0; i < result.Length; i++)
            result[i] = new Vector3(components[i * 3], components[i * 3 + 1], components[i * 3 + 2]);
        return result;
    }

    static Vector2[] Uvs(JToken values)
    {
        var components = values.ToObject<float[]>();
        var result = new Vector2[components.Length / 2];
        for (int i = 0; i < result.Length; i++) result[i] = new Vector2(components[i * 2], components[i * 2 + 1]);
        return result;
    }
}
