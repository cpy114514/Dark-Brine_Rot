using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportNailongLoot
{
    public const string Folder = "Assets/Game/Equipment/Nailong";
    public static readonly string[] Files = { "20260930130250_547090e7.fbx", "20260930130258_d69f77fb.fbx", "20260930131057_77c279ab.fbx" };
    public static object Import()
    {
        if (Application.isPlaying) throw new Exception("Import outside Play Mode.");
        Directory.CreateDirectory(Folder);
        var report = new List<object>();
        foreach (string file in Files)
        {
            string path = Folder + "/" + file;
            if (!File.Exists(path)) File.Copy("C:/Users/pinyu/Downloads/" + file, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Directory.CreateDirectory(Folder + "/Textures/" + Path.GetFileNameWithoutExtension(file));
            importer.ExtractTextures(Folder + "/Textures/" + Path.GetFileNameWithoutExtension(file));
            importer.importAnimation = false;
            importer.SaveAndReimport();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            report.Add(new { file, nodes = Array.ConvertAll(prefab.GetComponentsInChildren<Transform>(true), x => x.name),
                meshes = Array.ConvertAll(prefab.GetComponentsInChildren<MeshFilter>(true), x => new { name = x.name, bounds = x.sharedMesh.bounds.ToString(), vertices = x.sharedMesh.vertexCount }),
                materials = Array.ConvertAll(prefab.GetComponentsInChildren<Renderer>(true), x => Array.ConvertAll(x.sharedMaterials,
                    m => m == null ? "null" : m.name + " / " + m.shader.name + " / " + (m.mainTexture == null ? "no texture" : AssetDatabase.GetAssetPath(m.mainTexture)))) });
        }
        AssetDatabase.Refresh();
        return report;
    }
}
