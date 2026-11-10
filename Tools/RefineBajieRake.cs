using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mavis;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RefineBajieRake
{
    const string Prefab = "Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab";
    const string Source = "Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/Human/Models/ZhuBaJie_Human.fbx";
    const string SourceName = "SK_LYS_ZhuBaJie_01B_Weapon.001";
    const string Asset = "Assets/Game/Art/ModelRefinement/Bajie Nine-Tooth Rake.asset";
    const string Evidence = ".codex/model-refinement/bajie-rake";

    static SkinnedMeshRenderer Weapon(GameObject boss) =>
        boss.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(skin => skin.name == SourceName);

    static Mesh Original() =>
        AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Mesh>().Single(mesh => mesh.name == SourceName);

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        var source = Original();
        var skin = Weapon(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        using var input = new BinaryReader(File.OpenRead(Evidence + "/refined.bin"));
        int vertexCount = input.ReadInt32();
        int boneCount = input.ReadInt32();
        int submeshCount = input.ReadInt32();
        int boneIndex = input.ReadInt32();
        var bones = Enumerable.Range(0, boneCount).Select(_ => input.ReadString()).ToArray();
        var materials = Enumerable.Range(0, submeshCount).Select(_ => input.ReadString()).ToArray();
        if (vertexCount is < 1000 or > 65535 || boneIndex != 1 ||
            !bones.SequenceEqual(skin.bones.Select(bone => bone.name)) ||
            !materials.SequenceEqual(skin.sharedMaterials.Select(mat => mat.name)))
            throw new InvalidDataException("Rake production bindings differ");
        Vector3[] ReadVectors() => Enumerable.Range(0, vertexCount)
            .Select(_ => new Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle())).ToArray();
        var mesh = new Mesh { name = "Bajie Nine-Tooth Rake", vertices = ReadVectors(), normals = ReadVectors(),
            uv = Enumerable.Range(0, vertexCount).Select(_ => new Vector2(input.ReadSingle(), input.ReadSingle())).ToArray(),
            bindposes = source.bindposes, subMeshCount = submeshCount };
        int triangleCount = 0;
        for (int sub = 0; sub < submeshCount; sub++)
        {
            int indexCount = input.ReadInt32();
            if (indexCount <= 0 || indexCount % 3 != 0 || indexCount >= source.GetIndexCount(sub))
                throw new InvalidDataException("Invalid rake submesh");
            var indices = Enumerable.Range(0, indexCount).Select(_ => input.ReadInt32()).ToArray();
            mesh.SetTriangles(indices, sub);
            triangleCount += indexCount / 3;
        }
        using var counts = new NativeArray<byte>(Enumerable.Repeat((byte)1, vertexCount).ToArray(), Allocator.Temp);
        using var weights = new NativeArray<BoneWeight1>(Enumerable.Repeat(
            new BoneWeight1 { boneIndex = boneIndex, weight = 1 }, vertexCount).ToArray(), Allocator.Temp);
        mesh.SetBoneWeights(counts, weights);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        float error = Mathf.Max(Vector3.Distance(mesh.bounds.min, source.bounds.min), Vector3.Distance(mesh.bounds.max, source.bounds.max));
        if (error > .006f || triangleCount >= 100000) throw new InvalidDataException("Rake bounds or complexity drift");
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(Asset);
        if (existing)
        {
            // Mesh native buffers require explicit assignment when updating an asset.
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.normals = mesh.normals;
            existing.uv = mesh.uv;
            existing.bindposes = mesh.bindposes;
            existing.subMeshCount = mesh.subMeshCount;
            for (int sub = 0; sub < submeshCount; sub++) existing.SetTriangles(mesh.GetTriangles(sub), sub);
            existing.SetBoneWeights(counts, weights);
            existing.tangents = mesh.tangents;
            existing.bounds = mesh.bounds;
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existing;
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
        }
        else AssetDatabase.CreateAsset(mesh, Asset);
        string backup = Evidence + "/ZhuBaJie Boss.prefab.before";
        if (!File.Exists(backup)) File.Copy(Prefab, backup);
        var prefab = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            Weapon(prefab).sharedMesh = mesh;
            if (!PrefabUtility.SaveAsPrefabAsset(prefab, Prefab)) throw new IOException("Failed to save rake reference");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (string materialName in new[] { "DingPa01", "DingPa02" })
        {
            string materialPath = "Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/Human/Materials/" + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            string mapPath = "Assets/Game/Art/ModelRefinement/Bajie" + materialName + "Surface.png";
            var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
            if (!importer) throw new IOException("Missing rake surface " + mapPath);
            if (importer.sRGBTexture || importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.sRGBTexture = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            string materialBackup = Evidence + "/" + materialName + ".mat.before";
            if (!File.Exists(materialBackup)) File.Copy(materialPath, materialBackup);
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath));
            material.SetFloat("_Smoothness", 1);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        var result = new { triangleCount, vertexCount, boneCount, boundsError = error,
            originalBytes = Profiler.GetRuntimeMemorySizeLong(source), refinedBytes = Profiler.GetRuntimeMemorySizeLong(mesh) };
        File.WriteAllText(Evidence + "/unity-install.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object Review()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Use isolated Play in Second Island");
        var boss = UnityEngine.Object.FindFirstObjectByType<ZhuBaJieBoss>();
        if (!boss) throw new InvalidOperationException("Missing production Bajie boss");
        var skin = Weapon(boss.gameObject);
        var model = boss.humanForm.transform.Find("Human Model").gameObject;
        var source = Original();
        var refined = skin.sharedMesh;
        if (refined == source || refined.bindposes.Length != skin.bones.Length) throw new InvalidOperationException("Rake not installed");
        var controllers = boss.GetComponentsInChildren<Behaviour>(true).Where(b => b is Animator || b is Animation || b is ZhuBaJieBossVisual).ToArray();
        var enabled = controllers.Select(b => b.enabled).ToArray();
        bool brain = boss.enabled, boarActive = boss.boarForm.activeSelf, humanActive = boss.humanForm.activeSelf;
        var material = skin.sharedMaterials;
        var beforeMaterial = material.Select(mat => new Material(mat)).ToArray();
        foreach (var mat in beforeMaterial)
        {
            mat.SetTexture("_MetallicGlossMap", null);
            mat.SetFloat("_Smoothness", .22f);
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        }
        var probe = new GameObject("Bajie rake review", typeof(Camera));
        var camera = probe.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 38;
        var target = new RenderTexture(1100, 900, 24);
        var image = new Texture2D(1100, 900, TextureFormat.RGB24, false);
        var oldTarget = RenderTexture.active;
        var baked = new Mesh();
        var style = Mavis.VisualStyles.VisualStyleManager.Instance;
        float time = Time.timeScale;
        var report = new List<object>();
        Time.timeScale = 0;
        style.Select("realistic", false);
        try
        {
            boss.enabled = false;
            foreach (var component in controllers) component.enabled = false;
            boss.boarForm.SetActive(false);
            boss.humanForm.SetActive(true);
            skin.updateWhenOffscreen = true;
            skin.forceMatrixRecalculationPerRender = true;
            foreach (var sample in new[]
            {
                (view: "front", clip: "BajieIdle", progress: .2f, direction: new Vector3(.08f,.12f,-1)),
                (view: "side", clip: "BajieIdle", progress: .2f, direction: new Vector3(1,.12f,0)),
                (view: "back", clip: "BajieIdle", progress: .2f, direction: new Vector3(0,.12f,1)),
                (view: "rake-first", clip: "BajieRake", progress: 1.05f, direction: new Vector3(.7f,.2f,-1)),
                (view: "rake-follow", clip: "BajieRake", progress: 1.80f, direction: new Vector3(.7f,.2f,-1)),
                (view: "dodge", clip: "BajieDodge", progress: .28f, direction: new Vector3(.7f,.2f,-1)),
                (view: "detail", clip: "BajieIdle", progress: .2f, direction: new Vector3(.2f,.3f,-1))
            })
            {
                Resources.Load<AnimationClip>("SecondIslandMotion/" + sample.clip).SampleAnimation(model, sample.progress);
                var pose = skin.bones.Select(bone => bone.localToWorldMatrix).ToArray();
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                Bounds world = skin.bounds;
                if (sample.view != "detail") foreach (var s in skins) world.Encapsulate(s.bounds);
                camera.transform.position = world.center + sample.direction.normalized * world.size.magnitude * (sample.view == "detail" ? 1.18f : 1.30f);
                camera.transform.LookAt(world.center);
                var extents = new Bounds[2];
                for (int version = 0; version < 2; version++)
                {
                    skin.sharedMesh = version == 0 ? source : refined;
                    skin.sharedMaterials = version == 0 ? beforeMaterial : material;
                    skin.BakeMesh(baked);
                    baked.RecalculateBounds();
                    extents[version] = baked.bounds;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Evidence + "/" + sample.view + (version == 0 ? "-before" : "-after") + ".png", image.EncodeToPNG());
                }
                float extentError = Mathf.Max(Vector3.Distance(extents[0].min, extents[1].min), Vector3.Distance(extents[0].max, extents[1].max));
                if (extentError > .007f || !pose.SequenceEqual(skin.bones.Select(bone => bone.localToWorldMatrix)))
                    throw new InvalidOperationException("Rake pose drift " + sample.view);
                var hand = model.GetComponentsInChildren<Transform>().First(t => t.name == "hand_r");
                var grip = skin.bones.Single(t => t.name == "weapon");
                float gripError = Vector3.Distance(hand.position, grip.position);
                if (gripError > .02f) throw new InvalidOperationException("Rake grip moved");
                report.Add(new { sample.view, sample.clip, sample.progress, extentError, gripError });
            }
            File.WriteAllText(Evidence + "/unity-poses.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            skin.sharedMesh = refined;
            skin.sharedMaterials = material;
            for (int i = 0; i < controllers.Length; i++) controllers[i].enabled = enabled[i];
            boss.boarForm.SetActive(boarActive);
            boss.humanForm.SetActive(humanActive);
            boss.enabled = brain;
            RenderTexture.active = oldTarget;
            target.Release();
            foreach (var obj in beforeMaterial.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[] { target, image, baked, probe }))
                UnityEngine.Object.DestroyImmediate(obj);
            style.RestorePreference();
            Time.timeScale = time;
        }
    }

    public static object Export()
    {
        var renderer = Weapon(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var mesh = Original();
        if (mesh.blendShapeCount != 0 || mesh.bindposes.Length != renderer.bones.Length || mesh.subMeshCount != 3)
            throw new InvalidOperationException("Unexpected production rake mesh layout");
        using var data = MeshUtility.AcquireReadOnlyMeshData(mesh);
        using var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
        using var normals = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
        using var uv = new NativeArray<Vector2>(mesh.vertexCount, Allocator.Temp);
        data[0].GetVertices(vertices);
        data[0].GetNormals(normals);
        data[0].GetUVs(0, uv);
        var counts = mesh.GetBonesPerVertex();
        var weights = mesh.GetAllBoneWeights();
        var tris = new List<int>();
        Directory.CreateDirectory(Evidence);
        using var writer = new BinaryWriter(File.Create(Evidence + "/production.bin"));
        writer.Write(mesh.vertexCount);
        writer.Write(renderer.bones.Length);
        writer.Write(mesh.subMeshCount);
        foreach (var bone in renderer.bones) writer.Write(bone.name);
        foreach (var mat in renderer.sharedMaterials) writer.Write(mat.name);
        foreach (var v in vertices) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
        foreach (var n in normals) { writer.Write(n.x); writer.Write(n.y); writer.Write(n.z); }
        foreach (var t in uv) { writer.Write(t.x); writer.Write(t.y); }
        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            using var indices = new NativeArray<int>((int)mesh.GetIndexCount(submesh), Allocator.Temp);
            data[0].GetIndices(indices, submesh);
            writer.Write(indices.Length);
            foreach (int i in indices) writer.Write(i);
            tris.Add(indices.Length / 3);
        }
        int offset = 0;
        foreach (byte count in counts)
        {
            if (count is < 1 or > 4) throw new InvalidDataException("Unexpected rake weight count");
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                var weight = weights[offset++];
                writer.Write((byte)weight.boneIndex);
                writer.Write(weight.weight);
            }
        }
        var result = new { mesh.vertexCount, tris, mesh.isReadable, bounds = mesh.bounds.ToString("F6"),
            bones = renderer.bones.Select(bone => bone.name).ToArray(), materialSlots = renderer.sharedMaterials.Select(mat => mat.name).ToArray(),
            attributes = mesh.GetVertexAttributes().Select(a => a.ToString()).ToArray(),
            weightCounts = counts.GroupBy(count => count).ToDictionary(group => group.Key, group => group.Count()) };
        File.WriteAllText(Evidence + "/unity-source.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
