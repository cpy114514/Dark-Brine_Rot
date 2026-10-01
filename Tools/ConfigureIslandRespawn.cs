using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mavis;

public static class ConfigureIslandRespawn
{
    public static object Configure()
    {
        if (Application.isPlaying) throw new Exception("Stop play before saving a respawn point.");
        var islands = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None);
        ProceduralIsland island = null;
        foreach (var candidate in islands)
            if (candidate.isActiveAndEnabled && candidate.gameObject.scene.name == "Environment") { island = candidate; break; }
        if (island == null) throw new Exception("Loaded Environment island not found.");
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        float water = ocean != null ? ocean.oceanHeight : 0f;
        IslandRespawnPoint point = null;
        foreach (var existing in UnityEngine.Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None))
            if (existing.gameObject.scene == island.gameObject.scene) { point = existing; break; }
        bool created = point == null;
        bool Surface(Vector3 seed, out Vector3 ground)
        {
            ground = seed;
            var hits = Physics.RaycastAll(seed + Vector3.up * 30f, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
                if (hit.collider.GetComponentInParent<ProceduralIsland>() == island && hit.collider is MeshCollider)
                { ground = hit.point; return true; }
            return false;
        }
        if (created)
        {
            Vector3 chosen = Vector3.zero;
            bool found = false;
            Physics.SyncTransforms();
            for (int r = 1; r <= 6 && !found; r++)
                for (int a = 0; a < 24 && !found; a++)
                {
                    float angle = a * Mathf.PI * 2f / 24;
                    var position = island.transform.TransformPoint(new Vector3(Mathf.Cos(angle) * r * 3f, 0f, Mathf.Sin(angle) * r * 3f));
                    position.y = island.GetWorldSurfaceHeight(position);
                    if (!Surface(position, out position)) continue;
                    if (position.y <= water + 1.5f) continue;
                    if (Physics.CheckCapsule(position + Vector3.up * .65f, position + Vector3.up * 2.5f, .5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    chosen = position; found = true;
                }
            if (!found) throw new Exception("No dry, unobstructed respawn position; no scene changes made.");
            var obj = new GameObject("Island Respawn Point");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, island.gameObject.scene);
            obj.transform.position = chosen;
            point = obj.AddComponent<IslandRespawnPoint>();
        }
        if (Surface(point.transform.position, out var aligned)) point.transform.position = aligned;
        EditorUtility.SetDirty(point);
        EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
        if (!EditorSceneManager.SaveScene(island.gameObject.scene)) throw new Exception("Scene save failed.");
        return new { created, scene = island.gameObject.scene.path, position = point.transform.position.ToString(), heightAboveWater = point.transform.position.y - water };
    }
}
