using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static class RefineBombardinoSurfaces
{
    const string Folder = "Assets/Game/Prefabs/Bosses/BombardinoCrocodilo";
    const string Evidence = ".codex/model-refinement/bombardino";
    static readonly string[] Slots = { "Fusil", "Engine", "crocodile_v01body_blinn" };

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        Directory.CreateDirectory(Evidence);
        var inputs = Slots.Select(slot =>
        {
            string path = "Assets/Game/Art/ModelRefinement/Bombardino" + slot + "Surface.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing Bombardino surface " + slot);
            if (importer.sRGBTexture || importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.sRGBTexture = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return (slot, map: AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                material: AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/" + slot + ".mat"));
        }).ToArray();
        foreach (var input in inputs)
        {
            if (!input.material) throw new InvalidOperationException("Missing Bombardino material " + input.slot);
            string backup = Evidence + "/" + input.slot + ".mat.before";
            if (!File.Exists(backup)) File.Copy(AssetDatabase.GetAssetPath(input.material), backup);
        }
        foreach (var input in inputs)
        {
            input.material.SetTexture("_MetallicGlossMap", input.map);
            input.material.SetFloat("_Smoothness", 1);
            input.material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(input.material);
            AssetDatabase.SaveAssetIfDirty(input.material);
        }
        return inputs.Select(input => new { input.slot, input.map.width, input.map.height }).ToArray();
    }

    public static object Studio()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stopped editor required");
        var preview = new PreviewRenderUtility();
        var originalMaterials = new List<Material>();
        try
        {
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/BombardinoCrocodilo.prefab"));
            preview.AddSingleGO(actor);
            var renderer = actor.GetComponentInChildren<MeshRenderer>();
            var materials = renderer.sharedMaterials;
            var before = materials.Select(material =>
            {
                var original = new Material(material);
                if (Slots.Contains(material.name))
                {
                    original.SetTexture("_MetallicGlossMap", null);
                    original.SetFloat("_Smoothness", .3f);
                    original.DisableKeyword("_METALLICSPECGLOSSMAP");
                }
                originalMaterials.Add(original);
                return original;
            }).ToArray();
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var sub = mesh.GetIndices(8);
            var head = new Bounds(renderer.transform.TransformPoint(vertices[sub[0]]), Vector3.zero);
            foreach (int index in sub) head.Encapsulate(renderer.transform.TransformPoint(vertices[index]));
            preview.camera.fieldOfView = 36;
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.backgroundColor = new Color(.055f, .065f, .08f);
            preview.camera.nearClipPlane = .01f;
            preview.camera.farClipPlane = 200;
            preview.ambientColor = new Color(.34f, .37f, .42f);
            preview.lights[0].intensity = 1.5f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35, -35, 0);
            preview.lights[1].intensity = 1.0f;
            preview.lights[1].transform.rotation = Quaternion.Euler(18, 140, 0);
            foreach (var (view, offset) in new[]
            {
                ("front", new Vector3(.12f,.18f,1)),
                ("side", new Vector3(1,.15f,.12f)),
                ("back", new Vector3(.12f,.18f,-1)),
                ("detail", new Vector3(.7f,.20f,1))
            })
            {
                var bounds = view == "detail" ? head : renderer.bounds;
                preview.camera.transform.position = bounds.center + offset.normalized * bounds.size.magnitude * 1.25f;
                preview.camera.transform.LookAt(bounds.center);
                foreach (bool refined in new[] { false, true })
                {
                    renderer.sharedMaterials = refined ? materials : before;
                    preview.BeginStaticPreview(new Rect(0, 0, 1100, 750));
                    preview.Render(true);
                    var image = preview.EndStaticPreview();
                    File.WriteAllBytes(Evidence + "/studio-" + view + (refined ? "-after" : "-before") + ".png", image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            var result = new { triangles = Enumerable.Range(0, mesh.subMeshCount).Sum(i => mesh.GetIndexCount(i) / 3),
                materialSlots = materials.Length, sourceMesh = AssetDatabase.GetAssetPath(mesh),
                newSurfaceBytes = materials.Where(m => Slots.Contains(m.name)).Sum(m =>
                    Profiler.GetRuntimeMemorySizeLong(m.GetTexture("_MetallicGlossMap"))) };
            File.WriteAllText(Evidence + "/unity-studio.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            preview.Cleanup();
            foreach (var material in originalMaterials) UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
