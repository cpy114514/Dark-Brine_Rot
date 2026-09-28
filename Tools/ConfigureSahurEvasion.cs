using System;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ConfigureSahurEvasion
{
    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Configure in Edit Mode.");
        const string path = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var report = new StringBuilder();
        try
        {
            var controller = root.GetComponent<ThirdPersonPlayerController>();
            controller.airFlipExtraHeight = 1.25f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            report.AppendLine("Sahur prefab: extra flip height = 1.25m; existing roll/animation settings preserved.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var controller in UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            report.AppendLine($"{controller.gameObject.scene.path}/{controller.name}: move={controller.moveSpeed}, roll={controller.rollSpeed}, flip duration={controller.airFlipDuration}, extra height={controller.airFlipExtraHeight}");
        }
        return report.ToString();
    }
}
