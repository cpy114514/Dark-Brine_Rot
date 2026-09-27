using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplyYellowBeach
{
    const string Root = "Assets/Island/Materials/YellowBeach/";
    static readonly string[] Scenes = { "Assets/Scenes/First Island/Environment.unity", "Assets/Scenes/MainScene.unity" };

    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before saving beach settings.");
        foreach (string path in Scenes)
        {
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.isLoaded && existing.isDirty) throw new InvalidOperationException("Save existing edits in " + path + " first.");
        }
        var sand = ImportTexture(Root + "aerial_sand_diff_2k.jpg", false);
        var normal = ImportTexture(Root + "aerial_sand_nor_gl_2k.jpg", true);
        int count = 0;
        foreach (string path in Scenes)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var island in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ProceduralIsland>(true)))
                {
                    Undo.RecordObject(island, "Apply yellow coastal beach");
                    island.beachSand = sand;
                    island.beachSandNormal = normal;
                    island.beachTint = new Color(1.18f, 1.08f, 0.72f, 1f);
                    island.beachHeight = 8f;
                    island.beachBlendWidth = 3.5f;
                    island.beachTextureTiling = 0.075f;
                    typeof(ProceduralIsland).GetMethod("EnsureMaterial", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(island, null);
                    EditorUtility.SetDirty(island);
                    count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
            }
            finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
        }
        return $"Saved warm yellow sand beaches on {count} islands; original inland materials and collision retained.";
    }

    static Texture2D ImportTexture(string path, bool normal)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing downloaded texture " + path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 8;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    public static string Verify()
    {
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        if (island == null || island.beachSand == null || island.beachSandNormal == null)
            throw new InvalidOperationException("Load the island with the downloaded beach textures first.");
        var material = island.GetComponent<MeshRenderer>().sharedMaterial;
        if (material == null || material.GetFloat("_BeachEnabled") < 0.5f || material.GetTexture("_BeachTex") != island.beachSand ||
            material.GetTexture("_BeachNormal") != island.beachSandNormal || material.GetFloat("_BeachHeight") != island.beachHeight)
            throw new InvalidOperationException("The beach shader is not using the saved textures/settings.");
        if (ShaderUtil.GetShaderMessages(material.shader).Any(message => message.severity.ToString() == "Error"))
            throw new InvalidOperationException("Beach shader compilation errors.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(island.beachSandNormal));
        if (importer.textureType != TextureImporterType.NormalMap) throw new InvalidOperationException("Beach normal import is incorrect.");
        var collider = island.GetComponent<MeshCollider>();
        if (collider == null || collider.sharedMesh != island.GetComponent<MeshFilter>().sharedMesh)
            throw new InvalidOperationException("Terrain visual/collision mesh do not match.");
        return $"Beach textures active: {island.beachSand.width}px, repeat + mipmaps; sea-relative transition={island.beachHeight:F1}m; shader/collision valid.";
    }

    public static async Task<string> Capture()
    {
        for (int i = 0; i < 150 && UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>() == null && EditorApplication.isPlaying; i++)
            await Task.Delay(100);
        Verify();
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var main = Camera.main;
        Vector3 originalPosition = main.transform.position;
        Quaternion originalRotation = main.transform.rotation;
        var movement = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        bool restoreMovement = movement != null && movement.enabled;
        if (restoreMovement) movement.enabled = false;
        var go = new GameObject("Temporary beach preview");
        var camera = go.AddComponent<Camera>();
        camera.CopyFrom(main);
        camera.enabled = false;
        Vector3 shore = island.transform.TransformPoint(new Vector3(island.shorelineRadius, 0, 0));
        Vector3 target = shore + new Vector3(-48f, ocean.oceanHeight + 3f - shore.y, 0f);
        Vector3 position = shore + new Vector3(10f, ocean.oceanHeight + 46f - shore.y, -55f);
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        main.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        var render = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        try
        {
            camera.targetTexture = render;
            await Task.Delay(500);
            camera.Render();
            RenderTexture.active = render;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            string path = Path.Combine(Path.GetTempPath(), "yellow-beach-preview.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return Verify() + " | " + path;
        }
        finally
        {
            main.transform.SetPositionAndRotation(originalPosition, originalRotation);
            if (restoreMovement) movement.enabled = true;
            camera.targetTexture = null;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(render);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
