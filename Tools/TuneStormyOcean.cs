using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Applies and verifies the faster, rougher sea without changing sky or graphics quality.</summary>
public static class TuneStormyOcean
{
    static readonly string[] OceanScenes =
    {
        "Assets/Scenes/First Island/Environment.unity",
        "Assets/Scenes/MainScene.unity",
        "Assets/Scenes/First story/Story1.unity"
    };

    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before saving ocean settings.");
        foreach (string path in OceanScenes)
        {
            var loaded = SceneManager.GetSceneByPath(path);
            if (loaded.isLoaded && loaded.isDirty)
                throw new InvalidOperationException("Save the existing edits in " + path + " before tuning the ocean.");
        }
        int count = 0;
        foreach (string path in OceanScenes)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var oceans = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OceanWorld>(true));
                foreach (var ocean in oceans)
                {
                    Undo.RecordObject(ocean, "Faster stormy ocean");
                    ocean.waveMotionSpeed = 1.8f;
                    ocean.wave1 = WithShape(ocean.wave1, 1.90f, 0.42f);
                    ocean.wave2 = WithShape(ocean.wave2, 1.10f, 0.34f);
                    ocean.wave3 = WithShape(ocean.wave3, 0.42f, 0.22f);
                    ocean.wave4 = WithShape(ocean.wave4, 0.18f, 0.14f);
                    ocean.waveIrregularity = 0.85f;
                    ocean.largeDetailStrength = 0.52f;
                    ocean.mediumDetailStrength = 0.44f;
                    ocean.rippleStrength = 0.24f;
                    ocean.whitecapStrength = 0.55f;
                    ocean.foamStrength = 1.2f;
                    ocean.foamSpeed = 0.42f;
                    ocean.foamIrregularity = 0.85f;
                    typeof(OceanWorld).GetMethod("BuildOcean", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ocean, null);
                    EditorUtility.SetDirty(ocean);
                    count++;
                }
                foreach (var island in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ProceduralIsland>(true)))
                    typeof(ProceduralIsland).GetMethod("BuildShoreFoam", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(island, null);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
            }
            finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
        }
        return $"Saved {count} oceans: 1.8x animation, higher/choppier waves, stronger irregular whitecaps; shore phase synchronized.";
    }

    static OceanGerstnerWave WithShape(OceanGerstnerWave wave, float amplitude, float steepness)
    {
        wave.amplitude = amplitude;
        wave.steepness = steepness;
        return wave;
    }

    public static string Verify()
    {
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        if (ocean == null) throw new InvalidOperationException("Load the island before verification.");
        var water = ocean.GetComponent<MeshRenderer>().sharedMaterial;
        if (!Mathf.Approximately(ocean.waveMotionSpeed, 1.8f) || ocean.wave1.amplitude < 1.8f ||
            water == null || !Mathf.Approximately(water.GetFloat("_OceanMotionSpeed"), ocean.waveMotionSpeed) ||
            !Mathf.Approximately(water.GetFloat("_WhitecapStrength"), ocean.whitecapStrength))
            throw new InvalidOperationException("Stormy ocean settings are not active.");
        int shores = 0;
        foreach (var island in UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
        {
            var foam = island.transform.Find("Procedural Shore Foam");
            if (foam == null) throw new InvalidOperationException("Shore foam missing.");
            var material = foam.GetComponent<MeshRenderer>().sharedMaterial;
            if (material == null || !Mathf.Approximately(material.GetFloat("_OceanMotionSpeed"), ocean.waveMotionSpeed))
                throw new InvalidOperationException("Shore/ocean animation speeds do not match.");
            shores++;
        }
        foreach (string name in new[] { "DarkBrine/Procedural Ocean", "DarkBrine/Procedural Shore Foam" })
        {
            var shader = Shader.Find(name);
            if (shader == null || ShaderUtil.GetShaderMessages(shader).Any(message => message.severity.ToString() == "Error"))
                throw new InvalidOperationException("Shader errors in " + name);
        }
        return $"Water/shore shaders valid; {shores} synchronized shores; speed={ocean.waveMotionSpeed:F1}x; main wave amplitude={ocean.wave1.amplitude:F2}m; timeScale={Time.timeScale:F1}.";
    }

    public static async Task<string> Capture()
    {
        for (int i = 0; i < 150 && UnityEngine.Object.FindFirstObjectByType<OceanWorld>() == null && EditorApplication.isPlaying; i++)
            await Task.Delay(100);
        Verify();
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        var go = new GameObject("Temporary rough-sea preview");
        var camera = go.AddComponent<Camera>();
        var mainCamera = Camera.main;
        var originalPosition = mainCamera.transform.position;
        var originalRotation = mainCamera.transform.rotation;
        var movement = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        bool restoreMovement = movement != null && movement.enabled;
        if (restoreMovement) movement.enabled = false;
        camera.CopyFrom(mainCamera);
        camera.enabled = false;
        Vector3 shore = island.transform.TransformPoint(new Vector3(island.shorelineRadius, 0, 0));
        Vector3 position = shore + new Vector3(25, ocean.oceanHeight + 7f - shore.y, -12);
        Vector3 target = shore + new Vector3(-8, ocean.oceanHeight + 0.2f - shore.y, 15);
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        // The ocean patch and sky follow Camera.main. Keep the preview camera
        // inside that same frustum instead of rendering outside the ocean's LOD sector.
        mainCamera.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        var render = new RenderTexture(1024, 576, 24);
        var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        var paths = new List<string>();
        try
        {
            camera.targetTexture = render;
            for (int i = 0; i < 2; i++)
            {
                await Task.Delay(400);
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                string path = Path.Combine(Path.GetTempPath(), "rough-ocean-" + i + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                paths.Add(path);
            }
            return Verify() + " | " + string.Join(" | ", paths);
        }
        finally
        {
            mainCamera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            if (restoreMovement) movement.enabled = true;
            camera.targetTexture = null;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(render);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
