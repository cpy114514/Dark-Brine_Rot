using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ConfigureFirstIslandArrival
{
    const string Gameplay = "Assets/Scenes/First Island/Gameplay.unity";
    const string Environment = "Assets/Scenes/First Island/Environment.unity";
    static Scene Open(string path)
    {
        var s = SceneManager.GetSceneByPath(path);
        return s.isLoaded ? s : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before installing arrival.");
        var environment = Open(Environment); var gameplay = Open(Gameplay);
        var island = environment.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ProceduralIsland>()).Single(i => i.isActiveAndEnabled);
        var ocean = environment.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<OceanWorld>()).Single();
        var player = gameplay.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ThirdPersonPlayerController>()).Single();
        Physics.SyncTransforms();
        var mesh = island.GetComponent<MeshCollider>();
        bool Ground(Vector3 at, out RaycastHit hit) => mesh.Raycast(new Ray(new Vector3(at.x, mesh.bounds.max.y + 50, at.z), Vector3.down), out hit, mesh.bounds.size.y + 150);
        Vector3 radial = Vector3.ProjectOnPlane(player.transform.position - island.transform.position, Vector3.up).normalized;
        Vector3 landing = Vector3.zero, outward = radial, drop = Vector3.zero; bool found = false;
        for (int angle = 0; angle < 24 && !found; angle++)
        {
            float turn = angle == 0 ? 0 : ((angle + 1) / 2) * 7.5f * (angle % 2 == 1 ? 1 : -1);
            Vector3 direction = Quaternion.Euler(0, turn, 0) * radial;
            float maxRadius = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);
            for (float radius = maxRadius; radius > 20; radius -= 2)
            {
                if (!Ground(island.transform.position + direction * radius, out var hit) || hit.point.y < ocean.oceanHeight + 4 || hit.point.y > ocean.oceanHeight + 6 || hit.normal.y < .95f) continue;
                Vector3 tangent = Vector3.Cross(Vector3.up, direction);
                if (!Ground(hit.point + tangent * 24 - direction * 3, out var stickHit) || stickHit.point.y < ocean.oceanHeight + 3 || stickHit.point.y > ocean.oceanHeight + 8 || stickHit.normal.y < .94f) continue;
                if (Physics.CheckCapsule(hit.point + Vector3.up * .7f, hit.point + Vector3.up * 5, .6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool blocked = false;
                for (int k = 0; k <= 12; k++)
                {
                    if (!Ground(Vector3.Lerp(hit.point, stickHit.point, k / 12f), out var step) ||
                        Physics.CheckCapsule(step.point + Vector3.up * .7f, step.point + Vector3.up * 5, .6f, ~0, QueryTriggerInteraction.Ignore)) { blocked = true; break; }
                }
                if (blocked) continue;
                landing = hit.point; drop = stickHit.point; outward = direction; found = true; break;
            }
        }
        if (!found) throw new Exception("No clear, dry beach with a reachable stick was found. Nothing saved.");
        // Keep the pickup on dry sand, but wash the survivor onto the waterline.
        Vector3 dryBeach = landing, seaEnd = landing + outward * 65;
        for (int i = 0; i < 18; i++)
        {
            Vector3 midpoint = (dryBeach + seaEnd) * .5f;
            if (Ground(midpoint, out var hit) && hit.point.y >= ocean.oceanHeight + 1f) dryBeach = hit.point;
            else seaEnd = midpoint;
        }
        if (!Ground(dryBeach, out var shoreHit) || shoreHit.normal.y < .95f)
            throw new Exception("No gentle waterline found. Nothing saved.");
        landing = shoreHit.point;
        for (int k = 0; k <= 12; k++)
            if (!Ground(Vector3.Lerp(landing, drop, k / 12f), out var step) ||
                Physics.CheckCapsule(step.point + Vector3.up * .7f, step.point + Vector3.up * 5, .6f, ~0, QueryTriggerInteraction.Ignore))
                throw new Exception("Waterline-to-stick route is obstructed. Nothing saved.");
        var old = gameplay.GetRootGameObjects().SingleOrDefault(o => o.name == "First Island Arrival Points");
        if (old == null) { old = new GameObject("First Island Arrival Points"); SceneManager.MoveGameObjectToScene(old, gameplay); Undo.RegisterCreatedObjectUndo(old, "Add arrival points"); }
        Transform Point(string name, Vector3 at, Quaternion rotation)
        {
            var point = old.transform.Find(name);
            if (point == null) { point = new GameObject(name).transform; point.SetParent(old.transform); }
            Undo.RecordObject(point, "Place arrival point"); point.SetPositionAndRotation(at, rotation); return point;
        }
        Vector3 start = landing + outward * 65; start.y = ocean.oceanHeight;
        var arrival = player.GetComponent<FirstIslandArrival>();
        if (arrival == null) arrival = Undo.AddComponent<FirstIslandArrival>(player.gameObject);
        Undo.RecordObject(arrival, "Configure shore arrival");
        arrival.player = player;
        arrival.driftStart = Point("Wave Drift Start", start, Quaternion.LookRotation(outward));
        arrival.shore = Point("Unconscious On Shore", landing, Quaternion.LookRotation(outward));
        arrival.lostStickPoint = Point("Lost Stick On Sand", drop, Quaternion.LookRotation(-outward));
        EditorUtility.SetDirty(arrival); EditorSceneManager.MarkSceneDirty(gameplay);
        if (!EditorSceneManager.SaveScene(gameplay)) throw new Exception("Could not save arrival Gameplay scene.");
        return new { landing = landing.ToString(), driftStart = start.ToString(), stick = drop.ToString(), stickDistance = Vector3.Distance(landing, drop), beachAboveSea = landing.y - ocean.oceanHeight, clearWalkableRoute = true };
    }
}
