// Editor tool: organize Environment.unity scene
// - Groups all "Cloud Entity ##" GameObjects under a new "Clouds" empty parent
// - Renames them to "Cloud_##_<n>" (deterministic per source)
// - Groups timestamp-named orphans under "_Uncategorized"
// - Marks scene dirty and saves
//
// Usage: open Environment.unity in Unity, then menu:
//   Tools > Organize > Environment Scene
//
// Re-runnable. Safe to undo via Ctrl+Z (registers Undo). To revert completely,
// restore from Assets/Scenes/Environment.unity.bak.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnvironmentOrganizer
{
    private const string MenuPath = "Tools/Organize/Environment Scene";

    private static readonly Regex CloudPattern =
        new Regex(@"^Cloud Entity (\d{2})$", RegexOptions.Compiled);

    private static readonly Regex TimestampPattern =
        new Regex(@"^\d{14}_[0-9a-f]+", RegexOptions.Compiled);

    [MenuItem(MenuPath)]
    public static void Organize()
    {
        var scene = SceneManager.GetActiveScene();

        if (scene.path != "Assets/Scenes/Environment.unity")
        {
            bool ok = EditorUtility.DisplayDialog(
                "Scene mismatch",
                $"Active scene is '{scene.path}'.\nThis tool is designed for 'Assets/Scenes/Environment.unity'.\nContinue anyway?",
                "Yes", "Cancel");
            if (!ok) return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organize Environment Scene");

        // 1) Ensure parent GameObjects exist in scene
        var cloudsParent = GetOrCreateSceneObject("Clouds", scene);
        var uncategorizedParent = GetOrCreateSceneObject("_Uncategorized", scene);

        // 2) Walk all GameObjects in scene (recursive)
        var allTransforms = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .ToList();

        // Pre-count: how many of each Cloud Entity ## to produce deterministic suffixes
        var cloudCount = new Dictionary<string, int>();
        var cloudReorder = new List<Transform>();
        var junkReorder = new List<Transform>();

        foreach (var t in allTransforms)
        {
            if (t == null) continue;
            if (t == cloudsParent.transform || t == uncategorizedParent.transform) continue;
            var n = t.gameObject.name;
            var m = CloudPattern.Match(n);
            if (m.Success)
            {
                cloudReorder.Add(t);
                string key = m.Groups[1].Value;
                if (!cloudCount.ContainsKey(key)) cloudCount[key] = 0;
                cloudCount[key]++;
            }
            else if (TimestampPattern.IsMatch(n))
            {
                junkReorder.Add(t);
            }
        }

        // 3) Re-parent + rename clouds (deterministic order: by sibling index then name)
        cloudReorder = cloudReorder
            .OrderBy(t => GetDepth(t))
            .ThenBy(t => CloudPattern.Match(t.gameObject.name).Groups[1].Value)
            .ThenBy(t => t.GetSiblingIndex())
            .ToList();

        var cloudSeen = new Dictionary<string, int>();
        int movedClouds = 0, renamedClouds = 0;

        foreach (var t in cloudReorder)
        {
            var m = CloudPattern.Match(t.gameObject.name);
            string key = m.Groups[1].Value;
            if (!cloudSeen.ContainsKey(key)) cloudSeen[key] = 0;
            int idx = ++cloudSeen[key];
            string newName = $"Cloud_{key}_{idx:00}";

            Undo.RecordObject(t, "Re-parent + rename cloud");
            Undo.SetTransformParent(t, cloudsParent.transform, "Re-parent cloud");
            t.SetParent(cloudsParent.transform, true);

            if (t.gameObject.name != newName)
            {
                Undo.RecordObject(t.gameObject, "Rename cloud");
                t.gameObject.name = newName;
                renamedClouds++;
            }
            movedClouds++;
        }

        // 4) Re-parent junk (timestamp-named) under _Uncategorized
        int movedJunk = 0;
        foreach (var t in junkReorder)
        {
            Undo.SetTransformParent(t, uncategorizedParent.transform, "Re-parent junk");
            t.SetParent(uncategorizedParent.transform, true);
            movedJunk++;
        }

        // 5) Mark dirty + save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "Done",
            $"Clouds: {movedClouds} re-parented, {renamedClouds} renamed.\n" +
            $"Junk: {movedJunk} re-parented under '_Uncategorized'.\n\n" +
            "Backup: Assets/Scenes/Environment.unity.bak",
            "OK");
    }

    private static GameObject GetOrCreateSceneObject(string name, Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static int GetDepth(Transform t)
    {
        int d = 0;
        while (t.parent != null) { d++; t = t.parent; }
        return d;
    }
}