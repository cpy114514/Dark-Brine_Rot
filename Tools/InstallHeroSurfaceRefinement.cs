using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public static class InstallHeroSurfaceRefinement
{
    const string Art = "Assets/Game/Art/ModelRefinement/";
    const string Sahur = "Assets/Game/Prefabs/Characters/Sahur/Materials/PbrSahurMaterial.mat";
    const string Capri = "Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Materials/Capuchino.mat";
    const string CapriMesh = "Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Animations/CapriBodyRigged.asset";
    const string Nailong = "Assets/Game/Prefabs/Bosses/Nailong/Materials/NailongBody.mat";

    public static object Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stopped editor required");
        var woodMap = LoadMap("SahurWoodSmoothness.png", TextureImporterType.Default);
        var paperMap = LoadMap("CapriPaperAndSleeveNormal.png", TextureImporterType.NormalMap);
        var clothMap = LoadMap("NailongClothAndSkinSmoothness.png", TextureImporterType.Default);
        var wood = AssetDatabase.LoadAssetAtPath<Material>(Sahur);
        var paper = AssetDatabase.LoadAssetAtPath<Material>(Capri);
        var cloth = AssetDatabase.LoadAssetAtPath<Material>(Nailong);
        var folder = ".codex/model-refinement/hero-surfaces";
        Directory.CreateDirectory(folder);
        foreach (var path in new[] { Sahur, Capri, Nailong, CapriMesh })
        {
            var backup = folder + "/" + Path.GetFileName(path) + ".before";
            if (!File.Exists(backup)) File.Copy(path, backup);
        }
        var tangentAudit = EnsureCapriTangents();
        wood.SetTexture("_MetallicGlossMap", woodMap);
        wood.SetFloat("_Smoothness", 1);
        wood.EnableKeyword("_METALLICSPECGLOSSMAP");
        paper.SetTexture("_BumpMap", paperMap);
        paper.SetFloat("_BumpScale", .60f);
        paper.EnableKeyword("_NORMALMAP");
        cloth.SetTexture("_MetallicGlossMap", clothMap);
        cloth.SetFloat("_Smoothness", 1);
        cloth.EnableKeyword("_METALLICSPECGLOSSMAP");
        foreach (var material in new[] { wood, paper, cloth })
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        return new { wood = woodMap.width, paper = paperMap.width, cloth = clothMap.width,
            materialGuidsPreserved = true, tangentAudit };
    }

    static object EnsureCapriTangents()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(CapriMesh);
        if (!mesh || !mesh.isReadable) throw new InvalidOperationException("Capri body mesh is unavailable");
        if (mesh.HasVertexAttribute(VertexAttribute.Tangent)) return new { existingTangents = mesh.vertexCount };

        long originalBytes = Profiler.GetRuntimeMemorySizeLong(mesh);
        var updated = UnityEngine.Object.Instantiate(mesh);
        try
        {
            updated.name = mesh.name;
            updated.RecalculateTangents();
            if (updated.tangents.Length != mesh.vertexCount ||
                updated.tangents.Any(t => !float.IsFinite(t.sqrMagnitude) || t.sqrMagnitude < 1))
                throw new InvalidDataException("Incomplete Capri tangent frame");
            var weights = mesh.GetAllBoneWeights().ToArray();
            var updatedWeights = updated.GetAllBoneWeights().ToArray();
            if (!mesh.vertices.SequenceEqual(updated.vertices) || !mesh.triangles.SequenceEqual(updated.triangles) ||
                !mesh.normals.SequenceEqual(updated.normals) || !mesh.uv.SequenceEqual(updated.uv) ||
                !mesh.bindposes.SequenceEqual(updated.bindposes) || mesh.bounds != updated.bounds ||
                mesh.blendShapeCount != updated.blendShapeCount ||
                !mesh.GetBonesPerVertex().ToArray().SequenceEqual(updated.GetBonesPerVertex().ToArray()) ||
                !weights.SequenceEqual(updatedWeights))
                throw new InvalidDataException("Capri tangent generation changed geometry or skinning");
            mesh.tangents = updated.tangents;
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            if (!mesh.HasVertexAttribute(VertexAttribute.Tangent))
                throw new IOException("Capri tangent frame was not applied");
            var audit = new { tangents = mesh.vertexCount, weights = weights.Length,
                geometryAndSkinIdentical = true, originalBytes, refinedBytes = Profiler.GetRuntimeMemorySizeLong(mesh) };
            File.WriteAllText(".codex/model-refinement/hero-surfaces/capri-tangents.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(audit, Newtonsoft.Json.Formatting.Indented));
            return audit;
        }
        finally { UnityEngine.Object.DestroyImmediate(updated); }
    }

    static Texture2D LoadMap(string filename, TextureImporterType type)
    {
        var path = Art + filename;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Missing " + filename);
        if (importer.textureType != type || importer.sRGBTexture || importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureType = type;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
