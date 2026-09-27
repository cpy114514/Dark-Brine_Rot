using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DisableDuplicateIsland
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "Stop Play mode before editing the islands.";

        ProceduralIsland original = null;
        ProceduralIsland duplicate = null;
        foreach (var island in Object.FindObjectsByType<ProceduralIsland>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (island.name == "Sunset Island") original = island;
            if (island.name == "Sunset Island (1)") duplicate = island;
        }

        if (original == null || duplicate == null)
            return "No exact Sunset Island / Sunset Island (1) pair found.";
        if (original.gameObject.scene != duplicate.gameObject.scene ||
            original.transform.position != duplicate.transform.position ||
            original.transform.rotation != duplicate.transform.rotation ||
            original.transform.localScale != duplicate.transform.localScale ||
            original.shorelineRadius != duplicate.shorelineRadius ||
            original.rings != duplicate.rings ||
            original.radialSegments != duplicate.radialSegments ||
            original.seed != duplicate.seed)
            return "The two island meshes differ; no object was changed.";

        if (duplicate.gameObject.activeSelf)
        {
            Undo.RecordObject(duplicate.gameObject, "Disable overlapping island duplicate");
            duplicate.gameObject.SetActive(false);
            EditorUtility.SetDirty(duplicate.gameObject);
            EditorSceneManager.MarkSceneDirty(duplicate.gameObject.scene);
            EditorSceneManager.SaveScene(duplicate.gameObject.scene);
        }

        return "Disabled the overlapping Sunset Island (1). The original remains active.";
    }
}
