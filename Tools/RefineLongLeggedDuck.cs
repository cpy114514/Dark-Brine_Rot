using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RefineLongLeggedDuck
{
    const string Prefab = "Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab";
    const string Evidence = ".codex/model-refinement/duck";
    const string MeshPath = "Assets/Game/Art/ModelRefinement/Long-legged duck distance mesh.asset";
    const string LodName = "Long-legged duck distance skin";

    public static object BuildMesh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        var skin = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponentInChildren<SkinnedMeshRenderer>();
        var original = skin.sharedMesh;
        using var input = new BinaryReader(File.OpenRead(Evidence + "/distance.bin"));
        int count = input.ReadInt32();
        int indexCount = input.ReadInt32();
        int boneCount = input.ReadInt32();
        if (count is < 1000 or > 65535 || indexCount % 3 != 0 ||
            indexCount >= original.GetIndexCount(0) / 4 || boneCount != skin.bones.Length)
            throw new InvalidDataException("Unexpected distance mesh format");
        var vertices = new Vector3[count];
        var normals = new Vector3[count];
        var uv = new Vector2[count];
        var indices = new int[indexCount];
        var influenceCounts = new byte[count];
        using var weights = new NativeList<BoneWeight1>(count * 2, Allocator.Temp);
        for (int i = 0; i < count; i++)
        {
            vertices[i] = new Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
            uv[i] = new Vector2(input.ReadSingle(), input.ReadSingle());
            normals[i] = new Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
            byte influences = input.ReadByte();
            if (influences is < 1 or > 4) throw new InvalidDataException("Invalid influence count");
            influenceCounts[i] = influences;
            float total = 0;
            for (int j = 0; j < influences; j++)
            {
                int bone = input.ReadByte();
                float weight = input.ReadSingle();
                if (bone >= boneCount || !float.IsFinite(weight) || weight <= 0) throw new InvalidDataException("Invalid duck weight");
                weights.Add(new BoneWeight1 { boneIndex = bone, weight = weight });
                total += weight;
            }
            if (Mathf.Abs(total - 1) > .001f) throw new InvalidDataException("Unnormalized duck weights");
        }
        for (int i = 0; i < indexCount; i++) indices[i] = input.ReadInt32();
        using var counts = new NativeArray<byte>(influenceCounts, Allocator.Temp);
        var mesh = new Mesh { name = "Long-legged duck distance mesh", vertices = vertices, normals = normals,
            uv = uv, triangles = indices, bindposes = original.bindposes };
        mesh.SetBoneWeights(counts, weights.AsArray());
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        float extentError = Mathf.Max(Vector3.Distance(mesh.bounds.min, original.bounds.min),
            Vector3.Distance(mesh.bounds.max, original.bounds.max)) / original.bounds.size.magnitude;
        if (extentError > .014f) throw new InvalidDataException("Duck silhouette extent drift");
        mesh.UploadMeshData(false);
        var asset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (asset)
        {
            asset.Clear();
            asset.vertices = vertices;
            asset.normals = normals;
            asset.uv = uv;
            asset.triangles = indices;
            asset.bindposes = original.bindposes;
            asset.SetBoneWeights(counts, weights.AsArray());
            asset.bounds = mesh.bounds;
            asset.tangents = mesh.tangents;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = asset;
        }
        else AssetDatabase.CreateAsset(mesh, MeshPath);
        var result = new { triangles = indexCount / 3, vertices = count, boneCount, extentError,
            originalBytes = Profiler.GetRuntimeMemorySizeLong(original), distanceBytes = Profiler.GetRuntimeMemorySizeLong(mesh) };
        File.WriteAllText(Evidence + "/unity-mesh.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stopped editor required");
        var distanceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (!distanceMesh) throw new InvalidOperationException("Build the distance mesh first");
        var backup = Evidence + "/LongLeggedDuck.prefab.before";
        if (!File.Exists(backup)) File.Copy(Prefab, backup);
        var prefab = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var near = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(renderer => renderer.name != LodName);
            var previous = near.transform.Find(LodName);
            var far = previous ? previous.GetComponent<SkinnedMeshRenderer>() : new GameObject(LodName).AddComponent<SkinnedMeshRenderer>();
            far.transform.SetParent(near.transform, false);
            far.gameObject.layer = near.gameObject.layer;
            far.sharedMesh = distanceMesh;
            far.sharedMaterials = near.sharedMaterials;
            far.bones = near.bones;
            far.rootBone = near.rootBone;
            far.localBounds = near.localBounds;
            far.quality = near.quality;
            far.shadowCastingMode = near.shadowCastingMode;
            far.receiveShadows = near.receiveShadows;
            far.lightProbeUsage = near.lightProbeUsage;
            far.reflectionProbeUsage = near.reflectionProbeUsage;
            far.probeAnchor = near.probeAnchor;
            far.updateWhenOffscreen = near.updateWhenOffscreen;
            var lod = prefab.GetComponent<LODGroup>();
            if (!lod) lod = prefab.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.11f, new Renderer[] { near }), new LOD(0, new Renderer[] { far }) });
            lod.RecalculateBounds();
            if (!PrefabUtility.SaveAsPrefabAsset(prefab, Prefab)) throw new IOException("Failed to save duck LOD");
            return new { nearMesh = near.sharedMesh.name, distanceMesh = far.sharedMesh.name,
                bones = near.bones.Length, lod.size, switchHeight = .11f };
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    public static async Task<object> Review()
    {
        if (!Application.isPlaying || AdditiveSceneBootstrap.IsLoading)
            throw new InvalidOperationException("Load First Island in isolated Play");
        var collider = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<Collider>();
        if (!collider.Raycast(new Ray(new Vector3(288, 150, 346), Vector3.down), out var ground, 400))
            throw new InvalidOperationException("Duck review ground missing");
        var duck = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), ground.point, Quaternion.identity);
        duck.AddComponent<GameSaveExcluded>();
        var brain = duck.GetComponent<ChickenBroAI>();
        brain.enabled = false;
        var skin = duck.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(renderer => renderer.name != LodName);
        var animator = duck.GetComponentInChildren<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        skin.updateWhenOffscreen = true;
        var original = skin.sharedMesh;
        var reduced = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        var comparisonCamera = new GameObject("Duck comparison camera", typeof(Camera));
        var camera = comparisonCamera.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 38;
        var target = new RenderTexture(900, 900, 24);
        var screenshot = new Texture2D(900, 900, TextureFormat.RGB24, false);
        var baked = new Mesh();
        var savedTarget = RenderTexture.active;
        var manager = Mavis.VisualStyles.VisualStyleManager.Instance;
        var lod = duck.GetComponent<LODGroup>();
        var lods = lod ? lod.GetLODs() : Array.Empty<LOD>();
        var distanceSkin = lods.Length == 2 ? lods[1].renderers[0] as SkinnedMeshRenderer : null;
        if (lod && (!distanceSkin || lods[0].renderers[0] != skin || distanceSkin.sharedMesh != reduced ||
            !distanceSkin.bones.SequenceEqual(skin.bones) || distanceSkin.rootBone != skin.rootBone))
            throw new InvalidOperationException("Production LOD reference mismatch");
        var samples = new List<object>();
        Matrix4x4[] idlePose = null;
        float time = Time.timeScale;
        Time.timeScale = 0;
        manager.Select("realistic", false);
        try
        {
            foreach (var (view, state, normalizedTime, offset) in new[]
            {
                ("front", "Idle", .27f, new Vector3(0,.13f,1)),
                ("side", "Idle", .27f, new Vector3(1,.14f,0)),
                ("back", "Idle", .27f, new Vector3(0,.13f,-1)),
                ("walk", "Walk", .38f, new Vector3(1,.16f,1)),
                ("run", "Run", .72f, new Vector3(1,.16f,1)),
                ("peck", "Strike", .49f, new Vector3(1,.16f,1)),
                ("lunge", "Lunge", .43f, new Vector3(1,.16f,1)),
                ("knockdown", "Knockdown", .63f, new Vector3(1,.25f,1))
            })
            {
                skin.sharedMesh = original;
                animator.Play("Base Layer." + state, 0, normalizedTime);
                animator.Update(.001f);
                await Frame();
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + state))
                    throw new InvalidOperationException("Duck animation did not enter " + state);
                var world = skin.bounds;
                camera.transform.position = world.center + offset.normalized * world.size.magnitude * 1.65f;
                camera.transform.LookAt(world.center);
                var pose = skin.bones.Select(bone => bone.localToWorldMatrix).ToArray();
                if (view == "front") idlePose = pose;
                if (state != "Idle" && idlePose.SequenceEqual(pose))
                    throw new InvalidOperationException("Duck joints did not move for " + state);
                var extents = new Bounds[2];
                for (int version = 0; version < 2; version++)
                {
                    if (lod) lod.ForceLOD(version);
                    else skin.sharedMesh = version == 0 ? original : reduced;
                    await Frame();
                    (version == 1 && distanceSkin ? distanceSkin : skin).BakeMesh(baked);
                    baked.RecalculateBounds();
                    extents[version] = baked.bounds;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    screenshot.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
                    screenshot.Apply();
                    File.WriteAllBytes(Evidence + "/" + view + (version == 0 ? "-before" : "-after") + ".png", screenshot.EncodeToPNG());
                }
                float error = Mathf.Max(Vector3.Distance(extents[0].min, extents[1].min),
                    Vector3.Distance(extents[0].max, extents[1].max)) / extents[0].size.magnitude;
                if (error > .025f || !pose.SequenceEqual(skin.bones.Select(bone => bone.localToWorldMatrix)))
                    throw new InvalidOperationException("Duck pose diverged at " + view + " with extent error " + error);
                samples.Add(new { view, state, normalizedTime, relativeExtentError = error });
            }
            skin.sharedMesh = original;
            if (!BirdHurtboxes.Ensure(duck) || duck.GetComponentsInChildren<BoxCollider>().Count(box => box.name.StartsWith("BodyHitbox_Bird_")) < 2)
                throw new InvalidOperationException("Original duck hurtboxes missing");
            var result = new { samples, avatarValid = animator.avatar && animator.avatar.isValid,
                bones = skin.bones.Length, prefabLodVerified = lods.Length == 2, hurtboxes = true };
            File.WriteAllText(Evidence + "/unity-poses.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            RenderTexture.active = savedTarget;
            target.Release();
            foreach (var obj in new UnityEngine.Object[] { baked, target, screenshot, comparisonCamera, duck })
                UnityEngine.Object.DestroyImmediate(obj);
            Time.timeScale = time;
            manager.RestorePreference();
        }
    }

    static async Task Frame()
    {
        int frame = Time.frameCount;
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (frame == Time.frameCount)
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline)
                throw new InvalidOperationException("Duck review was interrupted");
            await Task.Delay(5);
        }
    }

    public static object Inspect()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var skin = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = skin.sharedMesh;
        using var data = MeshUtility.AcquireReadOnlyMeshData(mesh);
        return new
        {
            mesh.name, mesh.vertexCount, mesh.isReadable, mesh.blendShapeCount,
            triangles = mesh.GetIndexCount(0) / 3, mesh.subMeshCount,
            bounds = mesh.bounds.ToString(), localBounds = skin.localBounds.ToString(),
            vertices = data[0].vertexCount,
            bones = skin.bones.Select(bone => bone.name).ToArray(),
            clips = prefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Select(clip => clip.name).ToArray(),
            weights = mesh.GetAllBoneWeights().Length, weightsPerVertex = mesh.GetBonesPerVertex().Length,
            attributes = mesh.GetVertexAttributes().Select(a => a.ToString()).ToArray()
        };
    }

    public static object Export()
    {
        var skin = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = skin.sharedMesh;
        if (mesh.subMeshCount != 1 || mesh.blendShapeCount != 0 || mesh.bindposes.Length != skin.bones.Length)
            throw new InvalidOperationException("Duck source format changed");
        using var data = MeshUtility.AcquireReadOnlyMeshData(mesh);
        using var vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
        using var normals = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
        using var uv = new NativeArray<Vector2>(mesh.vertexCount, Allocator.Temp);
        using var triangles = new NativeArray<int>((int)mesh.GetIndexCount(0), Allocator.Temp);
        data[0].GetVertices(vertices);
        data[0].GetNormals(normals);
        data[0].GetUVs(0, uv);
        data[0].GetIndices(triangles, 0);
        var counts = mesh.GetBonesPerVertex();
        var weights = mesh.GetAllBoneWeights();
        Directory.CreateDirectory(Evidence);
        using var writer = new BinaryWriter(File.Create(Evidence + "/production.bin"));
        writer.Write(mesh.vertexCount);
        writer.Write(triangles.Length);
        writer.Write(skin.bones.Length);
        foreach (var bone in skin.bones) writer.Write(bone.name);
        foreach (var v in vertices) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
        foreach (var n in normals) { writer.Write(n.x); writer.Write(n.y); writer.Write(n.z); }
        foreach (var t in uv) { writer.Write(t.x); writer.Write(t.y); }
        foreach (int i in triangles) writer.Write(i);
        int offset = 0;
        foreach (int count in counts)
        {
            if (count is < 1 or > 4) throw new InvalidDataException("Unexpected source influence count");
            writer.Write((byte)count);
            for (int i = 0; i < count; i++)
            {
                var weight = weights[offset++];
                writer.Write((byte)weight.boneIndex);
                writer.Write(weight.weight);
            }
        }
        return new { mesh.vertexCount, mesh.bindposes.Length, weights = weights.Length };
    }
}
