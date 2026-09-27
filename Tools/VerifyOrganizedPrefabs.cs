using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyOrganizedPrefabs
{
    public static string Run()
    {
        const string root = "Assets/Game/Prefabs/";
        var b = new StringBuilder();
        string[] paths = AssetDatabase.GetAllAssetPaths().Where(p =>
            p.StartsWith(root, StringComparison.Ordinal) &&
            p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).ToArray();
        int invalidAssets = 0;
        int missingScriptsInPrefabs = 0;
        foreach (string path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { b.AppendLine("INVALID PREFAB " + path); invalidAssets++; continue; }
            missingScriptsInPrefabs += prefab.GetComponentsInChildren<Transform>(true)
                .Sum(t => t.GetComponents<Component>().Count(c => c == null));
        }
        b.AppendLine("Prefabs=" + paths.Length + " invalid=" + invalidAssets + " missingScripts=" + missingScriptsInPrefabs);

        int missingScenePrefabs = 0;
        int missingSceneScripts = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            foreach (Transform t in rootObject.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.MissingAsset)
                    missingScenePrefabs++;
                missingSceneScripts += t.GetComponents<Component>().Count(c => c == null);
            }
            b.AppendLine("Scene " + scene.name + " loaded");
        }
        b.AppendLine("Scene missingPrefabObjects=" + missingScenePrefabs + " missingScripts=" + missingSceneScripts);

        var sahur = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Characters/Sahur/SahurPlayer.prefab");
        var nailong = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Bosses/Nailong/Nailong.prefab");
        var menu = AssetDatabase.LoadAssetAtPath<GameObject>(root + "UI/SettingsMenu/PauseSettingsMenu.prefab");
        b.AppendLine("Sahur=" + (sahur != null) + " Nailong=" + (nailong != null) + " SettingsMenu=" + (menu != null));
        if (paths.Length < 131 || invalidAssets != 0 || missingScriptsInPrefabs != 0 ||
            missingScenePrefabs != 0 || missingSceneScripts != 0 || sahur == null || nailong == null || menu == null)
            throw new InvalidOperationException(b.ToString());
        return b.ToString();
    }
}
