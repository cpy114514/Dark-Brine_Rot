using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ConfigureIslandDayNight
{
    const string EnvironmentPath = "Assets/Scenes/First Island/Environment.unity";
    const string LightingPath = "Assets/Scenes/First Island/Lighting.unity";

    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before updating the saved island lighting scene.");

        Scene originalActive = SceneManager.GetActiveScene();
        bool openedEnvironment = !SceneManager.GetSceneByPath(EnvironmentPath).isLoaded;
        bool openedLighting = !SceneManager.GetSceneByPath(LightingPath).isLoaded;
        Scene environment = SceneManager.GetSceneByPath(EnvironmentPath);
        Scene lighting = SceneManager.GetSceneByPath(LightingPath);
        if (openedEnvironment) environment = EditorSceneManager.OpenScene(EnvironmentPath, OpenSceneMode.Additive);
        if (openedLighting) lighting = EditorSceneManager.OpenScene(LightingPath, OpenSceneMode.Additive);

        try
        {
            var ocean = environment.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OceanWorld>(true)).FirstOrDefault();
            if (ocean == null) throw new InvalidOperationException("OceanWorld was not found in the environment scene.");

            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer < 0) throw new InvalidOperationException("The project Water layer is missing.");
            ocean.gameObject.layer = waterLayer;
            EditorSceneManager.MarkSceneDirty(environment);

            var sun = lighting.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .FirstOrDefault(light => light.type == LightType.Directional);
            if (sun == null) throw new InvalidOperationException("The island directional sun was not found.");

            var cycle = sun.GetComponent<DayNightCycle>();
            if (cycle == null) cycle = Undo.AddComponent<DayNightCycle>(sun.gameObject);

            GameObject probeObject = lighting.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Ocean Reflection Probe");
            if (probeObject == null)
            {
                probeObject = new GameObject("Ocean Reflection Probe");
                SceneManager.MoveGameObjectToScene(probeObject, lighting);
                Undo.RegisterCreatedObjectUndo(probeObject, "Create realtime ocean reflection probe");
            }

            var probe = probeObject.GetComponent<ReflectionProbe>();
            if (probe == null) probe = Undo.AddComponent<ReflectionProbe>(probeObject);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 256;
            probe.hdr = true;
            probe.boxProjection = false;
            probe.intensity = 0.8f;
            probe.importance = 20;
            probe.shadowDistance = 120f;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 2200f;
            probe.size = new Vector3(1900f, 900f, 2100f);
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.cullingMask = ~LayerMask.GetMask("Water", "UI", "Ignore Raycast");

            var islands = environment.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ProceduralIsland>(true))
                .Select(island => island.GetComponent<Renderer>())
                .Where(renderer => renderer != null).ToArray();
            Vector3 focus = islands.Length == 0
                ? new Vector3(0f, ocean.oceanHeight + 5f, ocean.transform.position.z)
                : islands.Select(renderer => renderer.bounds.center).Aggregate(Vector3.zero, (sum, point) => sum + point) / islands.Length;
            float shoreline = islands.Length == 0 ? 0f : islands.Max(renderer =>
                renderer.GetComponent<ProceduralIsland>().shorelineRadius) + 100f;
            focus += Vector3.back * shoreline;
            focus.y = ocean.oceanHeight + 5f;
            probeObject.transform.position = focus;
            probe.center = Vector3.zero;

            cycle.ConfigureOceanReflectionProbe(probe);
            cycle.fullDayMinutes = 24f;
            cycle.startHour = 8f;
            cycle.noonElevation = 70f;
            cycle.noonIntensity = 1.55f;
            cycle.reflectionRefreshSeconds = 20f;
            cycle.reflectionResolution = 256;
            EditorUtility.SetDirty(cycle);
            EditorUtility.SetDirty(probe);
            EditorSceneManager.MarkSceneDirty(lighting);

            if (!EditorSceneManager.SaveScene(environment))
                throw new InvalidOperationException("Failed to save the environment scene.");
            if (!EditorSceneManager.SaveScene(lighting))
                throw new InvalidOperationException("Failed to save the lighting scene.");

            return $"Added the 24-minute animated solar cycle and HDR realtime ocean reflection probe at {focus}; ocean render layer is {LayerMask.LayerToName(waterLayer)}.";
        }
        finally
        {
            if (originalActive.IsValid() && originalActive.isLoaded)
                SceneManager.SetActiveScene(originalActive);
            if (openedLighting && lighting.IsValid() && lighting.isLoaded)
                EditorSceneManager.CloseScene(lighting, true);
            if (openedEnvironment && environment.IsValid() && environment.isLoaded)
                EditorSceneManager.CloseScene(environment, true);
        }
    }
}
