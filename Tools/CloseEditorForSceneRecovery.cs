using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CloseEditorForSceneRecovery
{
    public static object Close()
    {
        if (Application.isPlaying) throw new Exception("Do not close a playing Editor.");
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        if (scenes.Any(s => s.isDirty)) throw new Exception("Unsaved scenes: close/save them manually before recovery.");
        // The failed Environment load is empty; never save it over the real scene.
        var failed = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if (!failed.IsValid() || failed.rootCount != 0) throw new Exception("Scene state changed; refusing automatic close.");
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
        return new { exiting = true, noUnsavedScenes = true };
    }
}
