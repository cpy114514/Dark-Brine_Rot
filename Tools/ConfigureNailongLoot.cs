using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class ConfigureNailongLoot
{
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Configure outside Play Mode.");
        const string folder = "Assets/Game/Equipment/Nailong";
        string[] ids = { "20260930130250_547090e7", "20260930130258_d69f77fb", "20260930131057_77c279ab" };
        string[] names = { "奶龙洞洞鞋", "奶龙下装", "奶龙圆框眼镜" };
        var slots = new[] { SahurLoadoutUI.EquipmentSlot.Feet, SahurLoadoutUI.EquipmentSlot.Legs, SahurLoadoutUI.EquipmentSlot.Head };
        const string resources = "Assets/Game/Equipment/Resources/Equipment/Nailong";
        Directory.CreateDirectory(resources);
        Directory.CreateDirectory(folder + "/Materials");
        Directory.CreateDirectory(folder + "/WorldModels");
        AssetDatabase.Refresh();
        var report = new List<object>();
        for (int i = 0; i < 3; i++)
        {
            string textureFolder = folder + "/Textures/" + ids[i] + "/texture_pbr_20250901";
            foreach (string suffix in new[] { "", "_normal", "_metallic", "_roughness" })
            {
                var textureImporter = AssetImporter.GetAtPath(textureFolder + suffix + ".png") as TextureImporter;
                if (textureImporter == null) continue;
                textureImporter.maxTextureSize = 2048;
                textureImporter.textureType = suffix == "_normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                textureImporter.sRGBTexture = suffix.Length == 0;
                textureImporter.SaveAndReimport();
            }
            string matPath = folder + "/Materials/" + ids[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, matPath); }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + ".png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "_normal.png"));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "_metallic.png"));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", .42f);
            EditorUtility.SetDirty(material);
            var importer = (ModelImporter)AssetImporter.GetAtPath(folder + "/" + ids[i] + ".fbx");
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material"), material);
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + ids[i] + ".fbx");
            var root = new GameObject(names[i]);
            try
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(model);
                child.transform.SetParent(root.transform, false);
                var renderer = child.GetComponentInChildren<Renderer>();
                Bounds bounds = renderer.bounds;
                child.transform.localScale *= .65f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                child.transform.position -= renderer.bounds.center;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                string modelPath = folder + "/WorldModels/" + ids[i] + ".prefab";
                var worldModel = PrefabUtility.SaveAsPrefabAsset(root, modelPath);
                string path = resources + "/" + ids[i] + ".asset";
                var item = AssetDatabase.LoadAssetAtPath<EquipmentItem>(path);
                if (item == null) { item = ScriptableObject.CreateInstance<EquipmentItem>(); AssetDatabase.CreateAsset(item, path); }
                item.id = "nailong-" + ids[i]; item.displayName = names[i]; item.slot = slots[i]; item.worldModel = worldModel;
                EditorUtility.SetDirty(item);
                report.Add(new { name = names[i], slot = slots[i].ToString(), model = modelPath, textured = material.GetTexture("_BaseMap") != null });
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
        return report;
    }
}
