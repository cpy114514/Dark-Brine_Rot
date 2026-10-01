using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureCloudWeather
{
    const string LightingPath = "Assets/Scenes/First Island/Lighting.unity";
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode before saving.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(LightingPath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(LightingPath, OpenSceneMode.Additive);
        var cycle = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DayNightCycle>(true)).Single();
        Undo.RecordObject(cycle, "Increase cloud weather variation");
        cycle.minimumCloudCoverage = 0.62f;
        cycle.maximumCloudCoverage = 0.98f;
        cycle.cloudWeatherFrequency = 0.0045f;
        EditorUtility.SetDirty(cycle);
        var result = Verify();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return result;
    }
    public static object Verify()
    {
        var cycle = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        if (cycle == null) throw new Exception("Day/night cycle missing.");
        float min = 1f, max = 0f, maxStep = 0f, previous = cycle.EvaluateCloudCoverage(0f);
        for (int seconds = 0; seconds <= 1800; seconds++)
        {
            float value = cycle.EvaluateCloudCoverage(seconds);
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
            maxStep = Mathf.Max(maxStep, Mathf.Abs(value - previous));
            previous = value;
        }
        if (min < 0.619f || max < 0.94f || maxStep > 0.004f)
            throw new Exception("Cloud range or transition smoothing failed.");
        if (Application.isPlaying)
        {
            typeof(DayNightCycle).GetMethod("ApplyTimeOfDay", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cycle, null);
            var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            var sky = (Material)typeof(OceanWorld).GetField("skyMaterial", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ocean);
            if (sky == null || ShaderUtil.ShaderHasError(sky.shader) ||
                Mathf.Abs(sky.GetFloat("_CloudCoverage") - cycle.CurrentCloudCoverage) > 0.0001f)
                throw new Exception("Weather coverage did not reach the sky material.");
        }
        return new { min, max, maxChangePerSecond = maxStep,
            frontDurationSeconds = 1f / cycle.cloudWeatherFrequency, runtimeSkyChecked = Application.isPlaying };
    }
    public static void Preview(bool dense)
    {
        if (!Application.isPlaying) throw new Exception("Preview requires Play Mode.");
        var cycle = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        cycle.minimumCloudCoverage = cycle.maximumCloudCoverage = dense ? 0.96f : 0.62f;
        typeof(DayNightCycle).GetMethod("ApplyTimeOfDay", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cycle, null);
        var go = GameObject.Find("Cloud Weather Inspection Camera");
        if (go == null)
        {
            go = new GameObject("Cloud Weather Inspection Camera") { hideFlags = HideFlags.DontSave };
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 85f;
            camera.farClipPlane = 6000f;
            go.transform.position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, 0.6f, 1f));
        }
    }
    public static void Restore()
    {
        foreach (string name in new[] { "Main", "Lighting", "Environment" })
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/First Island/" + name + ".unity");
            if (scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
