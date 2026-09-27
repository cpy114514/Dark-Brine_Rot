using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TuneDaylightSky
{
    public static string Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before saving daylight settings.");

        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        if (ocean == null)
            throw new InvalidOperationException("No OceanWorld in the loaded island.");
        var sun = RenderSettings.sun;
        if (sun == null || sun.type != LightType.Directional)
            throw new InvalidOperationException("The island's directional sun was not found.");

        Undo.RecordObject(ocean, "Tune daytime clouds");
        ocean.cloudiness = 0.56f;
        ocean.sunlightIntensity = 0.90f;
        EditorUtility.SetDirty(ocean);
        EditorSceneManager.MarkSceneDirty(ocean.gameObject.scene);

        Undo.RecordObject(sun, "Tune daytime sunlight");
        sun.color = new Color(1f, 0.96f, 0.89f, 1f);
        sun.intensity = 1.55f;
        EditorUtility.SetDirty(sun);
        EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);

        var environment = SceneManager.GetActiveScene();
        var lighting = sun.gameObject.scene;
        ApplyAmbient(environment, sun);

        var buildSky = typeof(OceanWorld).GetMethod("BuildSky", BindingFlags.Instance | BindingFlags.NonPublic);
        if (buildSky == null)
            throw new InvalidOperationException("OceanWorld.BuildSky was not found.");
        buildSky.Invoke(ocean, null);

        EditorSceneManager.SaveScene(environment);
        if (environment != lighting)
        {
            // The game bootstrap switches the active scene to Lighting at runtime.
            // RenderSettings belong to the active scene, so configure both scenes.
            SceneManager.SetActiveScene(lighting);
            ApplyAmbient(lighting, sun);
            EditorSceneManager.SaveScene(lighting);
            SceneManager.SetActiveScene(environment);
        }
        return $"Daylight saved to {environment.path} and {lighting.path}; clouds={ocean.cloudiness:0.00}, sun={sun.intensity:0.00}, ambient={RenderSettings.ambientMode}.";
    }

    static void ApplyAmbient(Scene scene, Light sun)
    {
        // The procedural sky sphere is a mesh renderer, not a skybox material.
        // A Skybox ambient source with no skybox material keeps its old dim tint.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.57f, 0.70f, 0.82f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.43f, 0.51f, 0.58f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.25f, 0.24f, 1f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.sun = sun;
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
