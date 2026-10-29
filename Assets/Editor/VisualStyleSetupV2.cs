using System;
using System.Linq;
using Mavis.VisualStyles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class VisualStyleSetupV2
{
    const string Folder = "Assets/Resources/VisualStyles/Presets/";
    [MenuItem("Dark Brine/Visual Styles/Install or repair V2 assets")]
    public static void Install()
    {
        foreach (string id in new[] { "realistic", "dark-fairytale" })
            if (!AssetDatabase.LoadAssetAtPath<VisualStylePreset>(Folder + id + ".asset")) throw new Exception("Install the existing V1 system first");
        VisualStyleSetup.EnsureFolder("Assets/Resources/VisualStyles/Profiles");
        string path = "Assets/Resources/VisualStyles/Profiles/Paper.asset";
        var paper = AssetDatabase.LoadAssetAtPath<PaperMaterialProfile>(path);
        if (!paper) { paper = ScriptableObject.CreateInstance<PaperMaterialProfile>(); AssetDatabase.CreateAsset(paper, path); }
        var origami = Create("origami", "Origami", "A colorful world crafted from folded paper.", VisualStyleMode.Origami);
        if (origami)
        {
            origami.paper = paper; origami.outlinePixels = 1; origami.outlineStrength = .28f; origami.normalEdges = .12f;
            origami.shadows = new Color(.84f,.97f,.96f); origami.highlights = new Color(1.08f,1.02f,.88f);
            origami.atmosphereInfluence = .12f; origami.fogTint = new Color(.66f,.79f,.78f);
            EditorUtility.SetDirty(origami); AssetDatabase.SaveAssetIfDirty(origami);
        }
        Create("monochrome", "Monochrome", "Experience the world through dramatic black-and-white cinematography.", VisualStyleMode.Monochrome);
        foreach (string renderer in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(renderer);
            if (data.rendererFeatures.OfType<VisualStyleRendererFeature>().Count() != 1) throw new Exception("Expected the existing single visual feature: " + renderer);
        }
        AssetDatabase.SaveAssets();
    }
    static VisualStylePreset Create(string id, string title, string description, VisualStyleMode mode)
    {
        string path = Folder + id + ".asset";
        if (AssetDatabase.LoadAssetAtPath<VisualStylePreset>(path)) return null;
        var preset = ScriptableObject.CreateInstance<VisualStylePreset>(); preset.id = id; preset.displayName = title;
        preset.description = description; preset.renderingMode = mode; preset.illustrated = true;
        AssetDatabase.CreateAsset(preset, path); return preset;
    }
    [MenuItem("Dark Brine/Visual Styles/Assign V2 matching previews")]
    public static void AssignPreviews()
    {
        foreach (string id in new[] { "realistic", "dark-fairytale", "origami", "monochrome" })
        {
            string path = "Assets/Resources/VisualStyles/Previews/" + id + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new Exception("Actual capture missing: " + path);
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var preset = AssetDatabase.LoadAssetAtPath<VisualStylePreset>(Folder + id + ".asset");
            preset.preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path); EditorUtility.SetDirty(preset); AssetDatabase.SaveAssetIfDirty(preset);
        }
    }
}
