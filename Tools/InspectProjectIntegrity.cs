using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only inspection of the current workspace; never opens or saves scenes.
// unity command run_script --file Tools/InspectProjectIntegrity.cs --entry InspectProjectIntegrity.Run
public static class InspectProjectIntegrity
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Use a stopped, settled Editor. Existing scene dirtiness is preserved.");
        var scenes = new List<object>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var missingPrefabs = new List<string>();
            var missingScripts = new List<string>();
            var emptyMaterialSlots = new List<object>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = transform.gameObject;
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                        missingPrefabs.Add(Hierarchy(transform));
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                        missingScripts.Add(Hierarchy(transform));
                    foreach (var renderer in go.GetComponents<Renderer>())
                        if (renderer.sharedMaterials.Any(material => material == null))
                        {
                            var filter = renderer.GetComponent<MeshFilter>();
                            emptyMaterialSlots.Add(new { path = Hierarchy(transform), renderer.enabled,
                                active = go.activeInHierarchy, mesh = filter && filter.sharedMesh ? filter.sharedMesh.name : null });
                        }
                }
            scenes.Add(new { path = scene.path, dirty = scene.isDirty, roots = scene.rootCount,
                active = scene == SceneManager.GetActiveScene(), missingPrefabs, missingScripts, emptyMaterialSlots });
        }
        var paths = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path)).ToArray();
        string directory = ".codex/project-integrity/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(directory);
        var report = new { unity = Application.unityVersion, scenes,
            prefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null,
            buildScenes = EditorBuildSettings.scenes.Select(scene => new { scene.path, scene.enabled }).ToArray(),
            resources = paths.Where(path => path.Contains("/Resources/")).OrderBy(path => path).ToArray(),
            unrecognizedSerializedAssets = paths.Where(path => new[] { ".asset", ".prefab", ".unity", ".mat", ".controller", ".anim" }.Contains(Path.GetExtension(path)))
                .Where(path => { var type = AssetDatabase.GetMainAssetTypeAtPath(path); return type == null || type == typeof(DefaultAsset); }).ToArray(),
            streamingAssets = paths.Where(path => path.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal)).ToArray(),
            assetBundles = AssetDatabase.GetAllAssetBundleNames() };
        File.WriteAllText(directory + "/workspace.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return new { directory, sceneCount = scenes.Count, resourceCount = report.resources.Length, report.prefabStage };
    }

    static string Hierarchy(Transform transform)
    {
        string path = transform.name;
        while (transform.parent) { transform = transform.parent; path = transform.name + "/" + path; }
        return path;
    }
}
