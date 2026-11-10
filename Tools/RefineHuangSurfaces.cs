using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static class RefineHuangSurfaces
{
    public static readonly string[] Slots = { "Head01", "Body01", "ZhuangBei01", "FoXiang02", "Cloth02", "FoXiang03", "Cloth01" };
    const string Folder = "Assets/Game/Prefabs/Characters/HuangMei/Materials/";
    const string Art = "Assets/Game/Art/ModelRefinement/";
    const string Evidence = ".codex/model-refinement/huang";

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        Directory.CreateDirectory(Evidence);
        var inputs = Slots.Select(slot =>
        {
            string path = Art + "Huang" + slot + "Surface.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing Huang surface " + slot);
            if (importer.sRGBTexture || importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.sRGBTexture = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return (slot, texture: AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                material: AssetDatabase.LoadAssetAtPath<Material>(Folder + slot + ".mat"));
        }).ToArray();
        foreach (var item in inputs)
        {
            if (!item.material) throw new InvalidOperationException("Missing production material " + item.slot);
            string backup = Evidence + "/" + item.slot + ".mat.before";
            if (!File.Exists(backup)) File.Copy(AssetDatabase.GetAssetPath(item.material), backup);
        }
        foreach (var item in inputs)
        {
            item.material.SetTexture("_MetallicGlossMap", item.texture);
            item.material.SetFloat("_Smoothness", 1);
            item.material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(item.material);
            AssetDatabase.SaveAssetIfDirty(item.material);
        }
        var result = inputs.Select(item => new { item.slot, item.texture.width, format = item.texture.graphicsFormat.ToString(),
            bytes = Profiler.GetRuntimeMemorySizeLong(item.texture) }).ToArray();
        File.WriteAllText(Evidence + "/unity-install.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
