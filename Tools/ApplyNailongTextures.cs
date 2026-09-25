using System;
using UnityEditor;
using UnityEngine;

// Run with: unity command run_script --file Tools/ApplyNailongTextures.cs --entry ApplyNailongTextures.Apply
public static class ApplyNailongTextures
{
    public static string Apply()
    {
        const string root = "Assets/Models/Nailong";
        const string modelPath = root + "/nailong.fbx";
        const string textureFolder = root + "/Textures";
        const string materialFolder = root + "/Materials";
        const string materialPath = materialFolder + "/NailongBody.mat";

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_pbr_20250901.png");
        var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_pbr_20250901_normal.png");
        var metallicMap = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_pbr_20250901_metallic.png");
        if (baseMap == null || normalMap == null || metallicMap == null)
            throw new InvalidOperationException("Nailong's embedded textures must be extracted first.");

        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normalMap));
        if (normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }

        var metallicImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(metallicMap));
        if (metallicImporter.sRGBTexture)
        {
            metallicImporter.sRGBTexture = false;
            metallicImporter.SaveAndReimport();
        }

        var roughnessPath = textureFolder + "/texture_pbr_20250901_roughness.png";
        var roughnessImporter = AssetImporter.GetAtPath(roughnessPath) as TextureImporter;
        if (roughnessImporter != null && roughnessImporter.sRGBTexture)
        {
            roughnessImporter.sRGBTexture = false;
            roughnessImporter.SaveAndReimport();
        }

        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder(root, "Materials");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP Lit shader is unavailable.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "NailongBody" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", baseMap);
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_pbr_20250901_normal.png"));
        material.SetFloat("_BumpScale", 1.0f);
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_pbr_20250901_metallic.png"));
        // The source metallic map has mid-gray values over the cloth; tone it down
        // so the hood remains readable instead of turning into dark metal.
        material.SetFloat("_Metallic", 0.2f);
        material.SetFloat("_Smoothness", 0.35f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);

        var modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (modelImporter == null)
            throw new InvalidOperationException("Nailong FBX importer was not found.");
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material"), material);
        modelImporter.SaveAndReimport();
        AssetDatabase.SaveAssets();

        return "Mapped Nailong's embedded base color, normal and metallic textures to " + materialPath;
    }
}
