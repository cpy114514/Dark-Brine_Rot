using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CanonicalizeRecoveredEnvironment
{
    public static object Fix()
    {
        if (Application.isPlaying) throw new Exception("Save repaired scene outside Play Mode.");
        const string path = "Assets/Scenes/First Island/Environment.unity";
        var scene = SceneManager.GetSceneByPath(path);
        if (!scene.isLoaded || scene.rootCount < 1000) throw new Exception("Recovered scene is not fully loaded; refuse to save.");
        var roots = scene.GetRootGameObjects();
        var preserved = roots.Select(r => new { id = GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(), position = r.transform.position, rotation = r.transform.rotation }).ToArray();
        var islands = roots.SelectMany(r => r.GetComponentsInChildren<ProceduralIsland>(true)).ToArray();
        var buildFoam = typeof(ProceduralIsland).GetMethod("BuildShoreFoam", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var island in islands) buildFoam.Invoke(island, null);
        foreach (var root in roots)
        {
            var before = preserved.First(x => x.id == GlobalObjectId.GetGlobalObjectIdSlow(root).ToString());
            if (Vector3.Distance(before.position, root.transform.position) > .001f || Quaternion.Angle(before.rotation, root.transform.rotation) > .001f)
                throw new Exception("Root transform changed unexpectedly.");
        }
        // Let Unity write its own object identifiers and drop stale, unresolved PPtrs.
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception("Scene save failed.");
        var reloaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var after = reloaded.GetRootGameObjects();
        var ids = after.Select(r => GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).ToHashSet();
        if (preserved.Any(r => !ids.Contains(r.id))) throw new Exception("A root object was lost during reload.");
        return new { savedByUnity = true, rootObjectsPreserved = preserved.Length,
            reloadedRoots = after.Length, islandCount = islands.Length,
            foam = after.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "Procedural Shore Foam")
                .Select(t => new { parent = t.parent.name, material = t.GetComponent<MeshRenderer>()?.sharedMaterial?.shader.name }).ToArray() };
    }
}
