using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class TuneOceanAndShadows
{
    static readonly string[] OceanScenes =
    {
        "Assets/Scenes/First Island/Environment.unity",
        "Assets/Scenes/MainScene.unity",
        "Assets/Scenes/First story/Story1.unity"
    };

    public static string Run()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (pipeline == null) throw new Exception("PC URP asset is missing.");
        Undo.RecordObject(pipeline, "Extend shadow range with distance cascades");
        pipeline.shadowDistance = 180f;
        pipeline.shadowCascadeCount = 4;
        pipeline.cascade4Split = new Vector3(0.075f, 0.22f, 0.48f);
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();

        int oceanCount = 0;
        int islandCount = 0;
        foreach (string path in OceanScenes)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                OceanWorld[] oceans = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<OceanWorld>(true)).ToArray();
                foreach (OceanWorld ocean in oceans)
                {
                    Undo.RecordObject(ocean, "Tune ocean waves and white surf");
                    ocean.shallowColor = new Color(0.09f, 0.22f, 0.24f, 1f);
                    ocean.midColor = new Color(0.026f, 0.09f, 0.12f, 1f);
                    ocean.deepColor = new Color(0.012f, 0.043f, 0.065f, 1f);
                    ocean.foamColor = new Color(0.93f, 0.97f, 0.98f, 1f);
                    ocean.foamWidth = 3f;
                    ocean.foamStrength = 1.05f;
                    ocean.wave1 = Wave(new Vector2(0.82f, 0.57f), 1.30f, 95f, 12.2f, 0.25f);
                    ocean.wave2 = Wave(new Vector2(-0.38f, 0.93f), 0.75f, 44f, 8.3f, 0.20f);
                    ocean.wave3 = Wave(new Vector2(0.96f, -0.29f), 0.28f, 18f, 5.3f, 0.12f);
                    ocean.wave4 = Wave(new Vector2(-0.72f, -0.69f), 0.11f, 8f, 3.5f, 0.08f);
                    typeof(OceanWorld).GetMethod("BuildOcean", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(ocean, null);
                    EditorUtility.SetDirty(ocean);
                    oceanCount++;
                }
                foreach (ProceduralIsland island in scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ProceduralIsland>(true)))
                {
                    typeof(ProceduralIsland).GetMethod("BuildShoreFoam", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(island, null);
                    islandCount++;
                }
                if (oceans.Length > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new Exception("Could not save " + path);
                }
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
        return $"Tuned {oceanCount} oceans and {islandCount} shorelines; PC shadows reach 180 m with 4 distance cascades.";
    }

    static OceanGerstnerWave Wave(Vector2 direction, float amplitude, float wavelength, float speed, float steepness) =>
        new OceanGerstnerWave
        {
            direction = direction,
            amplitude = amplitude,
            wavelength = wavelength,
            speed = speed,
            steepness = steepness
        };
}
