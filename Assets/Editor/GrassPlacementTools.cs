using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class GrassPlacementTools
{
    const string ScenePath = "Assets/Scenes/First Island/Environment.unity";
    const string ReportFolder = "ProjectRepairBackups/GrassPlacement-20260930";

    [Serializable] internal class GrassRecord
    {
        public string id, name, prefab;
        public bool active, onIsland;
        public Vector3 position, rotation, scale, center, size, surfacePoint, surfaceNormal;
        public float bottom, groundGap, slope;
    }
    [Serializable] internal class IslandRecord
    {
        public string name;
        public bool active;
        public Vector3 position, scale, center, size;
    }
    [Serializable] internal class AuditReport
    {
        public int grassCount, activeGrass, grounded, missingSurface;
        public bool sceneDirty;
        public IslandRecord[] islands;
        public GrassRecord[] grass;
    }

    internal static Scene GetScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before adjusting grass.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("First Island Environment must already be loaded.");
        return scene;
    }
    internal static List<Transform> FindGrass(Scene scene)
    {
        var found = new HashSet<Transform>();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            GameObject instance = PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
            if (instance == null) continue;
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
            if (!path.Replace('\\', '/').Contains("/Prefabs/Environment/Grass/")) continue;
            Transform candidate = instance.transform;
            while (candidate.parent != null)
            {
                var parentInstance = PrefabUtility.GetNearestPrefabInstanceRoot(candidate.parent.gameObject);
                if (parentInstance == null || parentInstance == instance) break;
                string parentPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(parentInstance);
                if (!parentPath.Replace('\\', '/').Contains("/Prefabs/Environment/Grass/")) break;
                candidate = parentInstance.transform; instance = parentInstance;
            }
            found.Add(candidate);
        }
        return found.OrderBy(t => GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(), StringComparer.Ordinal).ToList();
    }
    internal static Bounds GetBounds(Transform t)
    {
        var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
    internal static bool Surface(Vector3 position, MeshCollider[] colliders, out RaycastHit best)
    {
        best = default; bool found = false;
        Ray ray = new Ray(new Vector3(position.x, 1000f, position.z), Vector3.down);
        foreach (var collider in colliders)
        {
            if (!collider.Raycast(ray, out var hit, 2000f)) continue;
            if (found && hit.point.y <= best.point.y) continue;
            best = hit; found = true;
        }
        return found;
    }
    [MenuItem("Tools/Grass Placement/Inspect Existing Grass")]
    internal static void Inspect()
    {
        Scene scene = GetScene();
        var islands = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ProceduralIsland>(true)).ToArray();
        var colliders = islands.Where(i => i.isActiveAndEnabled).Select(i => i.GetComponent<MeshCollider>()).Where(c => c != null).ToArray();
        Physics.SyncTransforms();
        var grass = FindGrass(scene); var records = new List<GrassRecord>();
        foreach (var t in grass)
        {
            var bounds = GetBounds(t); bool ground = Surface(bounds.center, colliders, out var hit);
            records.Add(new GrassRecord { id = GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(), name = t.name,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject), active = t.gameObject.activeInHierarchy,
                position = t.position, rotation = t.eulerAngles, scale = t.localScale, center = bounds.center, size = bounds.size,
                bottom = bounds.min.y, onIsland = ground, surfacePoint = hit.point, surfaceNormal = hit.normal,
                groundGap = ground ? bounds.min.y - hit.point.y : 0f, slope = ground ? Vector3.Angle(Vector3.up, hit.normal) : 0f });
        }
        var report = new AuditReport { grassCount = records.Count, activeGrass = records.Count(r => r.active), grounded = records.Count(r => r.onIsland),
            missingSurface = records.Count(r => !r.onIsland), sceneDirty = scene.isDirty, grass = records.ToArray(),
            islands = islands.Select(i => { var b = i.GetComponent<MeshRenderer>().bounds; return new IslandRecord { name = i.name, active = i.isActiveAndEnabled,
                position = i.transform.position, scale = i.transform.lossyScale, center = b.center, size = b.size }; }).ToArray() };
        Directory.CreateDirectory(ReportFolder);
        File.WriteAllText(ReportFolder + "/Inspect.json", JsonUtility.ToJson(report, true));
        Debug.Log("[Grass Placement] Inspected " + records.Count + " existing grass roots, " + report.missingSurface + " outside island mesh. Report: " + ReportFolder + "/Inspect.json");
    }
}
