using UnityEditor;
using UnityEngine;

/// <summary>Avoid Inspector processing thousands of selected objects while the game runs.</summary>
[InitializeOnLoad]
static class PlayModePerformanceGuard
{
    static PlayModePerformanceGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode || Selection.objects.Length < 256) return;
        Selection.objects = System.Array.Empty<Object>();
    }
}
