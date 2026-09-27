using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyIslandNormals
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "Stop Play mode before applying island textures.";

        var sand = ImportNormal("Assets/Island/Materials/coast_sand_03_4k/textures/coast_sand_03_nor_gl_4k.exr");
        var grass = ImportNormal("Assets/Island/Materials/sparse_grass_4k/textures/sparse_grass_nor_gl_4k.exr");
        var rock = ImportNormal("Assets/Island/Materials/rocky_terrain_4k/textures/rocky_terrain_nor_gl_4k.exr");
        if (sand == null || grass == null || rock == null)
            return "One or more island normal maps could not be imported.";

        var report = new StringBuilder();
        foreach (var island in Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(island, "Apply island surface normal maps");
            island.coastSandNormal = sand;
            island.grassNormal = grass;
            island.rockyNormal = rock;
            island.Rebuild();
            EditorUtility.SetDirty(island);
            EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
            report.AppendLine($"Applied to {island.name} in {island.gameObject.scene.path}");
        }

        if (report.Length == 0)
            return "No loaded island found.";

        EditorSceneManager.SaveOpenScenes();
        return report.ToString();
    }

    private static Texture2D ImportNormal(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Missing island texture importer: {path}");
            return null;
        }

        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            Debug.LogError($"Could not load island normal map: {path}");
        return texture;
    }
}
