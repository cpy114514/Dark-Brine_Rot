using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Exclude regenerated shoreline data from saves, including inactive island copies.</summary>
[InitializeOnLoad]
internal static class ProceduralIslandGeneratedData
{
    static ProceduralIslandGeneratedData()
    {
        EditorSceneManager.sceneSaving += BeforeSceneSave;
    }

    static void BeforeSceneSave(Scene scene, string path)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (ProceduralIsland island in root.GetComponentsInChildren<ProceduralIsland>(true))
        {
            // Inactive duplicates do not run OnEnable, which normally sets these flags.
            foreach (Transform child in island.transform)
            {
                if (child.name != "Procedural Shore Foam") continue;
                MeshFilter filter = child.GetComponent<MeshFilter>();
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null) continue;
                Mesh mesh = filter.sharedMesh;
                if (mesh != null && mesh.name != "Procedural Shore Breakers") continue;
                child.gameObject.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                // Imported model assets remain persistent; only generator-owned transient data is excluded.
                if (mesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                    mesh.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                foreach (Material material in renderer.sharedMaterials)
                    if (material != null && material.name == "Procedural Shore Foam Material" &&
                        string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
                        material.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
        }
    }
}
