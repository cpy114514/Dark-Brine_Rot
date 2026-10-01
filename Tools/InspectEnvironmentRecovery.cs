using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

public static class InspectEnvironmentRecovery
{
    public static object Inspect()
    {
        if (Application.isPlaying) throw new Exception("Exit Play Mode before scene inspection.");
        string path = "Assets/Scenes/First Island/Environment.unity";
        var original = SceneManager.GetActiveScene();
        var existing = SceneManager.GetSceneByPath(path);
        bool opened = !existing.IsValid() || !existing.isLoaded;
        try
        {
            var scene = opened ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : existing;
            var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            return new { valid = scene.IsValid(), loaded = scene.isLoaded, dirty = scene.isDirty, opened,
                objectCount = objects.Length, rootCount = scene.rootCount,
                foamCount = objects.Count(t => t.name == "Procedural Shore Foam"),
                scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => new { SceneManager.GetSceneAt(i).path, SceneManager.GetSceneAt(i).isDirty }).ToArray() };
        }
        finally
        {
            var scene = SceneManager.GetSceneByPath(path);
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
