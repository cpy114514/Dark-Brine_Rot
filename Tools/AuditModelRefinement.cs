using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuditModelRefinement
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    const string Evidence = ".codex/model-refinement";

    public static object Run()
    {
        Directory.CreateDirectory(Evidence);
        var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        var deps = AssetDatabase.GetDependencies(enabled, true).ToHashSet();
        var candidates = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs", "Assets/Resources" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => deps.Contains(path) || path.Contains("/Enemies/") || path.Contains("/Bosses/") ||
                path.Contains("/SecondIslandHuangMei/") || path.Contains("/SecondIslandZhuBaJie/"))
            .Distinct().OrderBy(path => path);
        var prefabs = new List<object>();
        foreach (var path in candidates)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;
            var meshRows = prefab.GetComponentsInChildren<Renderer>(true).Select(renderer =>
            {
                var skin = renderer as SkinnedMeshRenderer;
                renderer.TryGetComponent<MeshFilter>(out var filter);
                var mesh = skin ? skin.sharedMesh : filter ? filter.sharedMesh : null;
                if (!mesh || mesh.subMeshCount == 0) return null;
                return new
                {
                    node = AnimationUtility.CalculateTransformPath(renderer.transform, prefab.transform),
                    mesh = mesh.name, source = AssetDatabase.GetAssetPath(mesh),
                    localId = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id) ? id : 0,
                    vertices = mesh.vertexCount, triangles = mesh.GetIndexCount(0) / 3 + Enumerable.Range(1, mesh.subMeshCount - 1).Sum(i => (long)mesh.GetIndexCount(i) / 3),
                    mesh.subMeshCount, mesh.blendShapeCount, bones = skin ? skin.bones.Length : 0,
                    uv0 = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0),
                    normals = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal),
                    tangents = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent),
                    materials = renderer.sharedMaterials.Where(m => m).Select(m => new
                    {
                        m.name, path = AssetDatabase.GetAssetPath(m), shader = m.shader.name,
                        maps = m.GetTexturePropertyNames().Select(key => (key, image: m.GetTexture(key))).Where(pair => pair.image)
                            .Select(pair => new { pair.key, path = AssetDatabase.GetAssetPath(pair.image), pair.image.width, pair.image.height }).ToArray()
                    }).ToArray()
                };
            }).Where(row => row != null).ToArray();
            if (meshRows.Length == 0) continue;
            prefabs.Add(new { path, sceneDependency = deps.Contains(path), meshes = meshRows,
                animators = prefab.GetComponentsInChildren<Animator>(true).Select(a => new { avatar = AssetDatabase.GetAssetPath(a.avatar), controller = AssetDatabase.GetAssetPath(a.runtimeAnimatorController) }).ToArray() });
        }
        File.WriteAllText(Evidence + "/inventory.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { enabled, prefabs }, Newtonsoft.Json.Formatting.Indented));
        return new { prefabs = prefabs.Count, sceneCount = enabled.Length, report = Evidence + "/inventory.json" };
    }

    public static object Island()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var roots = scene.GetRootGameObjects();
        var experience = roots.SelectMany(r => r.GetComponentsInChildren<MarshmallowIslandExperience>(true)).First();
        var ground = experience.surface.GetComponent<Collider>();
        var bounds = ground.bounds;
        var rows = new List<object>();
        for (int i = 0; i < 16; i++)
        {
            var direction = Quaternion.Euler(0, 22.5f * i, 0) * Vector3.forward;
            var hits = new List<float[]>();
            for (float range = 40; range <= 2400; range += 10)
            {
                var point = bounds.center + direction * range;
                if (ground.Raycast(new Ray(new Vector3(point.x, bounds.max.y + 25, point.z), Vector3.down), out var hit, bounds.size.y + 50))
                    hits.Add(new[] { range, hit.point.x, hit.point.y, hit.point.z, hit.normal.y });
            }
            rows.Add(new { angle = 22.5f * i, outer = hits.TakeLast(16).ToArray() });
        }
        var result = new { center = bounds.center.ToString(), min = bounds.min.ToString(), max = bounds.max.ToString(), surface = experience.surface.name,
            roots = roots.Select(r => new { r.name, pos = r.transform.position.ToString(), children = r.transform.childCount }).ToArray(), rays = rows };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/island-layout.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { result.center, result.min, result.max, result.surface, roots = roots.Length };
    }
}
