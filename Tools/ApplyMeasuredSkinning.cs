using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplyMeasuredSkinning
{
    public static object Run()
    {
        const string root = ".codex/system-performance-20261107";
        string path = root + "/apply-measured-skinning.json";
        if (File.Exists(path))
        {
            var previous = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            if (previous.Value<bool>("saved"))
                return previous;
        }
        var evidence = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(
            root + "/after-gpu-release/first-island/stability.json"));
        if (evidence.Value<bool>("passed") != true)
            throw new InvalidOperationException("The isolated GPU skinning regression must pass first.");
        var build = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(
            root + "/after-gpu-release/DarkBrineRot_PerformanceQA.exe.build.json"));
        if (build.Value<string>("skinning") != "GPUBatched" || build.Value<bool>("development"))
            throw new InvalidOperationException("Expected a tested non-Development GPU build.");
        if (PlayerSettings.meshDeformation != MeshDeformation.CPU &&
            PlayerSettings.meshDeformation != MeshDeformation.GPUBatched)
            throw new InvalidOperationException("The original CPU skinning setting has changed.");
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
        {
            var scene = SceneManager.GetSceneAt(i);
            return new { scene.path, scene.isDirty };
        }).ToArray();
        string settingsPath = "ProjectSettings/ProjectSettings.asset";
        string backup = root + "/PlayerSettings-before-GPU.asset";
        if (!File.Exists(backup)) File.Copy(settingsPath, backup, false);
        if (!File.ReadAllBytes(settingsPath).SequenceEqual(File.ReadAllBytes(backup)))
            throw new InvalidOperationException("Player settings changed after the protected snapshot.");
        PlayerSettings.meshDeformation = MeshDeformation.GPUBatched;
        var settings = AssetDatabase.LoadAllAssetsAtPath(settingsPath).OfType<PlayerSettings>().Single();
        // Persistent PlayerSettings is skipped by SaveAssetIfDirty. Serialize a
        // transient copy with Unity's native serializer to save only this file.
        var copy = UnityEngine.Object.Instantiate(settings);
        try
        {
            string staged = Path.GetFullPath(root + "/PlayerSettings-verified-GPU.asset");
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { copy }, staged, true);
            string original = File.ReadAllText(backup);
            string expected = original.Replace("  meshDeformation: 0", "  meshDeformation: 2");
            if (original == expected || File.ReadAllText(staged) != expected)
                throw new InvalidOperationException("Native serialization must change only meshDeformation.");
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(
                new UnityEngine.Object[] { copy }, Path.GetFullPath(settingsPath), true);
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
        var result = new
        {
            previous = "CPU", updated = PlayerSettings.meshDeformation.ToString(),
            playing = EditorApplication.isPlaying, product = PlayerSettings.productName, scenes,
            settingsPath, saved = File.ReadAllText(settingsPath).Contains("meshDeformation: 2")
        };
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
