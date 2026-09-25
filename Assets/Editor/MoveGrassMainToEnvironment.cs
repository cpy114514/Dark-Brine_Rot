// MoveGrassMainToEnvironment.cs
// One-shot Editor tool: move every rostlinka (grass) prefab instance from
// Assets/Scenes/MainScene.unity into Assets/Scenes/Environment.unity.
//
// Why two scenes? The additive-scene pattern in this project loads MainScene's
// content via AdditiveSceneBootstrap, but Environment is the canonical home
// for world geometry (trees, grass, ocean, etc.). Keeping grass in MainScene
// causes it to leak into Editor preview but disappear from the build, which
// is the bug this script fixes.
//
// Two menu items:
//   Tools > Move Grass/Preview Main → Environment  — dry run, lists candidates
//   Tools > Move Grass/Move Main → Environment    — actually moves them
//
// Safe to re-run; bails when there is nothing left to move. World transforms
// are preserved (SceneManager.MoveGameObjectToScene defaults to
// worldPositionStays=true). Both scenes are saved at the end.
//
// To revert: use Plastic / git history rather than the .unity.bak files
// (those get overwritten on every save).

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MoveGrassMainToEnvironment
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string EnvironmentScenePath = "Assets/Scenes/Environment.unity";

    // Asset path substrings used to detect rostlinka grass prefab variants.
    // Rostlinka lives under Assets/3d model/simple-grass-chunks/ and the FBX +
    // extracted per-blade prefabs all carry "rostlinka_07c_ske" in their name.
    // Matching on name (not GUID) keeps this tool working across re-imports.
    private static readonly string[] GrassPathMarkers =
    {
        "3d model/simple-grass-chunks",
        "rostlinka_07c_ske",
    };

    private const string PreviewMenu = "Tools/Move Grass/Preview Main → Environment";
    private const string MoveMenu = "Tools/Move Grass/Move Main → Environment";

    // -------- Entry points --------

    [MenuItem(PreviewMenu, true)]
    private static bool PreviewValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(PreviewMenu)]
    public static void Preview()
    {
        if (!EnsureScenesLoaded(out var mainScene, out _))
            return;

        var candidates = FindGrassRoots(mainScene);
        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No grass found",
                $"No rostlinka grass prefab instances remain in {MainScenePath}.",
                "OK");
            return;
        }

        var lines = candidates.Select((g, i) =>
            $"  {i + 1,2}. {g.name,-32}  pos {g.transform.position}");

        EditorUtility.DisplayDialog(
            $"Preview — {candidates.Count} grass instance(s) would move",
            string.Join("\n", lines) + "\n\n" +
            $"Source: {MainScenePath}\n" +
            $"Target: {EnvironmentScenePath}\n\n" +
            "Use 'Tools > Move Grass/Move Main → Environment' to actually move them.",
            "OK");
    }

    [MenuItem(MoveMenu, true)]
    private static bool MoveValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(MoveMenu)]
    public static void Move()
    {
        if (!EnsureScenesLoaded(out var mainScene, out var envScene))
            return;

        var candidates = FindGrassRoots(mainScene);
        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Nothing to move",
                $"No rostlinka grass prefab instances found in {MainScenePath}.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Move grass to Environment?",
                $"This will MOVE {candidates.Count} grass prefab instance(s) from\n" +
                $"  {MainScenePath}\ninto\n  {EnvironmentScenePath}.\n\n" +
                "World transforms preserved. Both scenes will be saved.\n" +
                "Re-runnable; revert via Plastic / git history.\n\n" +
                "Continue?",
                "Move", "Cancel"))
            return;

        int movedCount = 0;
        var failed = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var go in candidates)
            {
                string goName = go.name;
                try
                {
                    // worldPositionStays: true (default) preserves the grass's
                    // world position when it becomes a root of the new scene.
                    SceneManager.MoveGameObjectToScene(go, envScene);
                    movedCount++;
                }
                catch (System.Exception ex)
                {
                    failed.Add($"{goName}: {ex.Message}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.MarkSceneDirty(envScene);

        bool mainSaved = EditorSceneManager.SaveScene(mainScene);
        bool envSaved = EditorSceneManager.SaveScene(envScene);

        string result =
            $"Moved: {movedCount} / {candidates.Count}\n" +
            $"Main saved:    {mainSaved}\n" +
            $"Environment saved: {envSaved}\n" +
            (failed.Count > 0 ? $"\nFailures:\n  - {string.Join("\n  - ", failed)}" : "");

        Debug.Log($"[MoveGrass] {result.Replace("\n", " | ")}");
        EditorUtility.DisplayDialog(
            (mainSaved && envSaved) ? "Move complete" : "Move partial — check Console",
            result,
            "OK");
    }

    // -------- Scene setup --------

    private static bool EnsureScenesLoaded(out Scene mainScene, out Scene envScene)
    {
        mainScene = default;
        envScene = default;

        // Environment must be open. If not, ask the user before forcing a
        // single-mode open (which closes everything else).
        envScene = FindLoadedScene(EnvironmentScenePath);
        if (!envScene.IsValid())
        {
            if (!EditorUtility.DisplayDialog(
                    "Open Environment.unity?",
                    $"Environment.unity is not currently loaded.\n" +
                    "Open it in Single mode (will close other open scenes)?",
                    "Open", "Cancel"))
                return false;
            envScene = EditorSceneManager.OpenScene(EnvironmentScenePath, OpenSceneMode.Single);
        }

        // MainScene loaded additively — does not disrupt the user's workspace.
        mainScene = FindLoadedScene(MainScenePath);
        if (!mainScene.IsValid())
            mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

        if (!mainScene.IsValid() || !envScene.IsValid())
        {
            EditorUtility.DisplayDialog(
                "Scene load failed",
                $"Could not load one of:\n  Main: {MainScenePath}\n  Env:  {EnvironmentScenePath}",
                "OK");
            return false;
        }

        return true;
    }

    private static Scene FindLoadedScene(string assetPath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.path == assetPath) return s;
        }
        return default;
    }

    // -------- Grass detection --------

    private static List<GameObject> FindGrassRoots(Scene scene)
    {
        var roots = new List<GameObject>();
        var seen = new HashSet<int>();

        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            // The prefab instance root may be the scene root itself, or
            // nested under a wrapper like "Celestial Cloudscape". Walk the
            // whole subtree to find every prefab instance, then dedupe by
            // outermost instance root.
            foreach (var t in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                if (!PrefabUtility.IsPartOfPrefabInstance(go)) continue;

                var outerRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (outerRoot == null) continue;
                if (!seen.Add(outerRoot.GetInstanceID())) continue;

                if (IsGrassInstance(outerRoot))
                    roots.Add(outerRoot);
            }
        }

        // Stable sort for predictable preview output.
        roots.Sort((a, b) =>
        {
            int byName = string.Compare(a.name, b.name, System.StringComparison.Ordinal);
            if (byName != 0) return byName;
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        return roots;
    }

    private static bool IsGrassInstance(GameObject go)
    {
        // GetCorrespondingObjectFromOriginalSource walks the prefab chain to
        // the deepest source asset (e.g. the rostlinka_07c_ske.FBX even when
        // the scene holds a rostlinka_07c_ske (7).prefab wrapper).
        var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
        if (source == null) return false;
        var path = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(path)) return false;
        return GrassPathMarkers.Any(m =>
            path.IndexOf(m, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }
}