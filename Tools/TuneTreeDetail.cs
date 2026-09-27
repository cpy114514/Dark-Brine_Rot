using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TuneTreeDetail
{
    static readonly string[] Models = {
        "Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/island_tree_01_4k.fbx",
        "Assets/Game/Prefabs/Environment/Trees/SmallTree02/tree_small_02_4k.fbx",
        "Assets/Game/Prefabs/Environment/Trees/IslandTree01/Shared/20260918140331_c8541e3e.fbx"
    };
    static readonly string[] Scenes = { "Assets/Scenes/First Island/Environment.unity", "Assets/Scenes/MainScene.unity", "Assets/Scenes/First story/Story1.unity" };

    static bool IsTree(MeshFilter filter)
    {
        string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
        if (path.Contains("/Trees/")) return true;
        for (Transform current = filter.transform; current != null; current = current.parent)
            if (current.name.StartsWith("20260918140331_c8541e3e") || current.name.StartsWith("island_tree") || current.name.StartsWith("tree_small")) return true;
        return false;
    }

    public static string Audit()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Audit saved tree scenes outside Play Mode.");
        var report = new StringBuilder();
        foreach (string path in Scenes)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var filters = roots.SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).Where(filter => filter.sharedMesh != null && IsTree(filter)).ToArray();
                report.AppendLine(path + " trees meshes=" + filters.Length);
                foreach (var set in filters.GroupBy(filter => filter.sharedMesh))
                {
                    var filter = set.First();
                    var mesh = set.Key;
                    var renderer = filter.GetComponent<MeshRenderer>();
                    var group = filter.GetComponentInParent<LODGroup>();
                    int level = group == null ? -1 : Array.FindIndex(group.GetLODs(), lod => lod.renderers.Contains(renderer));
                    report.AppendLine($" {mesh.name}: {mesh.vertexCount} verts, {Triangles(mesh)} tris, x{set.Count()}, active={filter.gameObject.activeInHierarchy}, renderer={renderer?.enabled}, LOD={level}, bounds={mesh.bounds.size}, source={AssetDatabase.GetAssetPath(mesh)}");
                }
                foreach (var set in roots.SelectMany(root => root.GetComponentsInChildren<LODGroup>(true)).Where(group => group.GetComponentsInChildren<MeshFilter>(true).Any(IsTree)).GroupBy(group => group.name))
                {
                    var group = set.First();
                    report.AppendLine($" LODGroup {group.name} x{set.Count()} enabled={group.enabled}, fade={group.fadeMode}, levels=" + string.Join("; ", group.GetLODs().Select(lod => lod.screenRelativeTransitionHeight + ": " + string.Join(",", lod.renderers.Select(renderer => renderer == null ? "null" : renderer.name)))));
                }
                report.AppendLine(" TreeGeometryLodOptimizer count=" + roots.SelectMany(root => root.GetComponentsInChildren<TreeGeometryLodOptimizer>(true)).Count());
            }
            finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
        }
        return report.ToString();
    }

    static long Triangles(Mesh mesh)
    {
        long count = 0;
        for (int i = 0; i < mesh.subMeshCount; i++) count += (long)mesh.GetIndexCount(i) / 3;
        return count;
    }

    public static string InspectApis()
    {
        var report = new StringBuilder();
        foreach (Type type in new[] { typeof(Mesh), typeof(MeshRenderer), typeof(Renderer), typeof(QualitySettings), typeof(ModelImporter) })
        {
            report.AppendLine(type.Name + ": " + string.Join("; ", type.GetMembers().Where(member => member.Name.ToLowerInvariant().Contains("lod")).Select(member => member.ToString())));
        }
        report.AppendLine("Current quality mesh threshold=" + QualitySettings.meshLodThreshold);
        foreach (string path in Models)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            report.AppendLine(path + " generated=" + importer.generateMeshLods + " limit=" + importer.maximumMeshLod + " flags=" + importer.meshLodGenerationFlags);
            foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Where(mesh => Triangles(mesh) > 100000))
                report.AppendLine(mesh.name + " native lods=" + mesh.lodCount);
        }
        return report.ToString();
    }

    public static string ImportModel(int modelIndex)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing tree LOD data.");
        string path = Models[modelIndex];
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new InvalidOperationException("Missing original tree model: " + path);
        // Keep every existing mesh/GUID, material, normal and collider. Unity stores
        // reduced index buffers inside that very same mesh, not a substitute tree.
        importer.generateMeshLods = true;
        // Foliage alpha cards lose coverage at aggressive levels. Keep one
        // conservative reduction step, approximately half the original triangles.
        importer.maximumMeshLod = 1;
        importer.meshLodGenerationFlags = 0;
        importer.SaveAndReimport();
        return VerifyModel(path);
    }

    static string VerifyModel(string path)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (!importer.generateMeshLods || importer.maximumMeshLod != 1)
            throw new InvalidOperationException("Tree import is not using the conservative same-mesh LOD limit: " + path);
        var report = new StringBuilder(path + "\n");
        foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Where(mesh => Triangles(mesh) > 100000))
        {
            if (mesh.lodCount != 2) throw new InvalidOperationException(mesh.name + " does not have exactly the original plus one reduced detail level.");
            report.AppendLine(mesh.name + ": " + string.Join(" -> ", Enumerable.Range(0, mesh.lodCount).Select(lod =>
                Enumerable.Range(0, mesh.subMeshCount).Sum(submesh => (long)mesh.GetLod(submesh, lod).indexCount) / 3)) + " triangles; same mesh asset");
        }
        return report.ToString();
    }

    public static string VerifyModels()
    {
        return string.Join("\n", Models.Select(VerifyModel));
    }

    public static async Task<string> ImportAll()
    {
        var report = new StringBuilder();
        for (int i = 0; i < Models.Length; i++)
        {
            report.AppendLine(ImportModel(i));
            await Task.Delay(100);
        }
        return report.ToString();
    }

    public static string EditorState()
    {
        return "Playing=" + EditorApplication.isPlaying + "\n" + string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
        {
            var scene = SceneManager.GetSceneAt(index);
            return scene.path + ": loaded=" + scene.isLoaded + ", dirty=" + scene.isDirty + ", active=" + (scene == SceneManager.GetActiveScene());
        }));
    }

    public static async Task<string> VerifyRuntime()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run the island for the rendering check.");
        MeshRenderer[] trees = null;
        for (int i = 0; i < 150; i++)
        {
            trees = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(renderer => renderer.GetComponent<MeshFilter>() != null &&
                    Models.Contains(AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh))).ToArray();
            if (trees.Length > 0) break;
            await Task.Delay(100);
        }
        if (trees.Length == 0) throw new InvalidOperationException("No original trees loaded.");
        foreach (var renderer in trees)
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            if (Triangles(mesh) > 100000 && mesh.lodCount < 2) throw new InvalidOperationException("Tree has no native detail levels: " + renderer.name);
            if (renderer.forceMeshLod != -1) throw new InvalidOperationException("Tree is forced to a fixed detail level: " + renderer.name);
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && (material.shader.name == "Hidden/InternalErrorShader" || ShaderUtil.GetShaderMessages(material.shader).Any(message => message.severity.ToString() == "Error")))
                    throw new InvalidOperationException("Tree material shader error: " + renderer.name);
        }

        var tree = trees.First(renderer => renderer.name == "island_tree_01_LOD0" && renderer.enabled && !renderer.forceRenderingOff);
        var filter = tree.GetComponent<MeshFilter>();
        Mesh originalMesh = filter.sharedMesh;
        var originalMaterials = tree.sharedMaterials;
        var colliderMeshes = tree.transform.root.GetComponentsInChildren<MeshCollider>(true).ToDictionary(collider => collider, collider => collider.sharedMesh);
        for (int i = 0; i < 150 && Camera.main == null; i++) await Task.Delay(100);
        var main = Camera.main;
        if (main == null) throw new InvalidOperationException("The island player camera has not finished loading.");
        if (UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(renderer => renderer.gameObject.layer == 31))
            throw new InvalidOperationException("The isolated preview layer is already in use.");
        Vector3 originalPosition = main.transform.position;
        Quaternion originalRotation = main.transform.rotation;
        var movement = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        bool restoreMovement = movement != null && movement.enabled;
        if (restoreMovement) movement.enabled = false;
        var cameraObject = new GameObject("Temporary original-tree LOD preview");
        var camera = cameraObject.AddComponent<Camera>();
        camera.CopyFrom(main);
        camera.enabled = false;
        var previewObject = new GameObject("Temporary same-mesh tree preview");
        previewObject.layer = 31;
        previewObject.AddComponent<MeshFilter>().sharedMesh = originalMesh;
        var preview = previewObject.AddComponent<MeshRenderer>();
        preview.sharedMaterials = originalMaterials;
        preview.receiveShadows = false;
        preview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.20f, 0.22f);
        var target = preview.bounds.center;
        float size = Mathf.Max(preview.bounds.size.x, preview.bounds.size.y, preview.bounds.size.z);
        var position = target + new Vector3(size * 1.5f, size * 0.25f, -size * 2.7f);
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        main.transform.SetPositionAndRotation(position, camera.transform.rotation);
        var render = new RenderTexture(960, 640, 24);
        var texture = new Texture2D(960, 640, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        try
        {
            camera.targetTexture = render;
            foreach (short lod in Enumerable.Range(0, originalMesh.lodCount).Select(level => (short)level))
            {
                preview.forceMeshLod = lod;
                await Task.Delay(350);
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 960, 640), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "original-tree-lod" + lod + ".png"), texture.EncodeToPNG());
            }
            if (filter.sharedMesh != originalMesh || !tree.sharedMaterials.SequenceEqual(originalMaterials) || colliderMeshes.Any(pair => pair.Key.sharedMesh != pair.Value))
                throw new InvalidOperationException("Tree source mesh/material/collider changed while selecting render detail.");
            return $"Checked {trees.Length} original tree renderers: native LOD selection automatic, shaders valid. LOD0/LOD{originalMesh.lodCount - 1} rendering previews captured; mesh, materials and colliders unchanged.";
        }
        finally
        {
            main.transform.SetPositionAndRotation(originalPosition, originalRotation);
            if (restoreMovement) movement.enabled = true;
            camera.targetTexture = null;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(previewObject);
            UnityEngine.Object.DestroyImmediate(render);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
