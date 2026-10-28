using System;
using System.IO;
using System.Linq;
using Mavis.VisualStyles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class VisualStyleSetup
{
    const string Folder = "Assets/Resources/VisualStyles/Presets";
    [MenuItem("Dark Brine/Visual Styles/Install or repair V1 assets")]
    public static void Install()
    {
        EnsureFolder(Folder);
        Preset("realistic", "Realistic", "Natural cinematic rendering with realistic lighting and materials.", false);
        Preset("dark-fairytale", "Dark Fairytale", "A mysterious hand-drawn world of ink, shadows, and storybook colors.", true);
        var shader = Shader.Find("Hidden/DarkBrine/DarkFairytale"); if (!shader) throw new Exception("Illustration shader is missing");
        Directory.CreateDirectory(".codex/visual-style-v1/renderer-before");
        foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
        {
            string backup = ".codex/visual-style-v1/renderer-before/" + Path.GetFileName(path);
            if (!File.Exists(backup)) File.Copy(path, backup);
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var feature = data.rendererFeatures.OfType<VisualStyleRendererFeature>().FirstOrDefault();
            if (!feature)
            {
                feature = ScriptableObject.CreateInstance<VisualStyleRendererFeature>(); feature.name = "Universal Visual Style";
                AssetDatabase.AddObjectToAsset(feature, data); data.rendererFeatures.Add(feature);
            }
            feature.illustrationShader = shader; feature.SetActive(true); feature.Create();
            var serialized = new SerializedObject(data); var map = serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize = data.rendererFeatures.Count;
            for (int i = 0; i < data.rendererFeatures.Count; i++)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i], out string guid, out long localId)) map.GetArrayElementAtIndex(i).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature); EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
        }

    }
    static void Preset(string id, string title, string description, bool illustrated)
    {
        string path = Folder + "/" + id + ".asset";
        if (AssetDatabase.LoadAssetAtPath<VisualStylePreset>(path)) return;
        var preset = ScriptableObject.CreateInstance<VisualStylePreset>(); preset.id = id; preset.displayName = title;
        preset.description = description; preset.illustrated = illustrated; AssetDatabase.CreateAsset(preset, path);
    }
    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
    [MenuItem("Dark Brine/Visual Styles/Assign regenerated scene previews")]
    public static void AssignPreviews()
    {
        foreach (string id in new[] { "realistic", "dark-fairytale" })
        {
            string path = "Assets/Resources/VisualStyles/Previews/" + id + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter; if (!importer) throw new Exception("Capture the matching scene previews first");
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var preset = AssetDatabase.LoadAssetAtPath<VisualStylePreset>(Folder + "/" + id + ".asset");
            preset.preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path); EditorUtility.SetDirty(preset); AssetDatabase.SaveAssetIfDirty(preset);
        }
    }
}
