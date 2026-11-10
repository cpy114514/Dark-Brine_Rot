using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public static class RefineSahurStick
{
    const string Prefab = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const string Original = "Assets/Game/Prefabs/Characters/Sahur/Models/stick.fbx";
    const string MaterialPath = "Assets/Game/Prefabs/Characters/Sahur/Materials/SahurStickMaterial.mat";
    const string Art = "Assets/Game/Art/ModelRefinement";
    const string Folder = ".codex/model-refinement/stick";

    public static object ExportOriginal()
    {
        Directory.CreateDirectory(Folder);
        var mesh = AssetDatabase.LoadAllAssetsAtPath(Original).OfType<Mesh>().Single(m => m.name == "node_0");
        using var data = MeshUtility.AcquireReadOnlyMeshData(mesh);
        using var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
        using var uv = new NativeArray<Vector2>(mesh.vertexCount, Allocator.Temp);
        using var triangles = new NativeArray<int>((int)mesh.GetIndexCount(0), Allocator.Temp);
        data[0].GetVertices(vertices);
        data[0].GetUVs(0, uv);
        data[0].GetIndices(triangles, 0);
        using var writer = new BinaryWriter(File.Create(Folder + "/native-source.bin"));
        writer.Write(vertices.Length);
        writer.Write(triangles.Length);
        foreach (var v in vertices) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
        foreach (var t in uv) { writer.Write(t.x); writer.Write(t.y); }
        foreach (int i in triangles) writer.Write(i);
        return new { mesh.vertexCount, triangles = triangles.Length / 3, bounds = mesh.bounds.ToString(), folder = Folder };
    }

    public static object Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stopped editor required");
        var original = AssetDatabase.LoadAllAssetsAtPath(Original).OfType<Mesh>().Single(m => m.name == "node_0");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var importedColour = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/SahurStickColour.png");
        var importedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/SahurStickNormal.png");
        if (!importedColour || !importedNormal) throw new InvalidOperationException("Missing imported wood maps");
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(Art + "/SahurStickNormal.png");
        if (normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        using var reader = new BinaryReader(File.OpenRead(Folder + "/native-refined.bin"));
        int vertexCount = reader.ReadInt32();
        int indexCount = reader.ReadInt32();
        if (vertexCount < 100 || vertexCount > 20000 || indexCount % 3 != 0 || indexCount >= original.GetIndexCount(0))
            throw new InvalidDataException("Unexpected refinement topology");
        var vertices = new Vector3[vertexCount];
        var uv = new Vector2[vertexCount];
        var normals = new Vector3[vertexCount];
        var indices = new int[indexCount];
        for (int i = 0; i < vertexCount; i++) vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        for (int i = 0; i < vertexCount; i++) uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        for (int i = 0; i < vertexCount; i++) normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        for (int i = 0; i < indexCount; i++) indices[i] = reader.ReadInt32();
        var mesh = new Mesh { name = "Sahur Stick - worked wood", vertices = vertices, uv = uv, normals = normals, triangles = indices };
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        float boundError = Mathf.Max(Vector3.Distance(mesh.bounds.min, original.bounds.min), Vector3.Distance(mesh.bounds.max, original.bounds.max));
        if (boundError > original.bounds.size.magnitude * .012f) throw new InvalidDataException("Stick bounds drift");
        string path = Art + "/Sahur Stick Worked Wood.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing)
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existing;
        }
        else AssetDatabase.CreateAsset(mesh, path);
        foreach (var file in new[] { Prefab, MaterialPath })
        {
            string backup = Folder + "/" + Path.GetFileName(file) + ".before";
            if (!File.Exists(backup)) File.Copy(file, backup);
        }
        var prefab = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var holder = prefab.GetComponentsInChildren<MeshFilter>(true).Single(filter => filter.name == "Sahur Stick");
            var position = holder.transform.localPosition;
            var rotation = holder.transform.localRotation;
            var scale = holder.transform.localScale;
            holder.sharedMesh = mesh;
            if (!PrefabUtility.SaveAsPrefabAsset(prefab, Prefab)) throw new IOException("Failed to update player visual");
            var result = new { vertices = mesh.vertexCount, triangles = indexCount / 3, boundError,
                position = position.ToString(), rotation = rotation.ToString(), scale = scale.ToString() };
            File.WriteAllText(Folder + "/unity-install.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", importedColour);
        material.SetTexture("_MainTex", importedColour);
        material.SetTexture("_BumpMap", importedNormal);
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_BumpScale", .66f);
        material.SetFloat("_Smoothness", .27f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        AssetDatabase.SaveAssetIfDirty(mesh);
        return new { trianglesBefore = original.GetIndexCount(0) / 3, trianglesAfter = indexCount / 3, boundError };
    }
}
