using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ConfigureSahurSwimming
{
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    public static object Install()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Configure(root, false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        string path = "Assets/Scenes/First Island/Gameplay.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        foreach (var player in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ThirdPersonPlayerController>(true)))
            Configure(player.gameObject, true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Swimming configured on Sahur prefab and island Gameplay scene.";
    }

    static void Configure(GameObject root, bool undo)
    {
        var player = root.GetComponent<ThirdPersonPlayerController>();
        if (player == null) return;
        if (undo) Undo.RecordObject(player, "Configure swimming");
        player.swimSpeed = 3.4f;
        player.swimAcceleration = 9f;
        player.swimStartDepth = 1.15f;
        player.swimShoreHysteresis = 0.25f;
        player.fastSwimMultiplier = 1.5f;
        player.fastSwimDrainPerSecond = 3f;
        player.wadingSpeedMultiplier = 0.7f;
        player.waterSplashes = true;
        var weapon = root.GetComponent<SahurSwimmingWeapon>();
        if (weapon == null) weapon = undo ? Undo.AddComponent<SahurSwimmingWeapon>(root) : root.AddComponent<SahurSwimmingWeapon>();
        if (undo) Undo.RecordObject(weapon, "Set swimming stick");
        weapon.stick = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Sahur Stick");
        weapon.backOffset = 0.55f;
        weapon.heightOffset = 0.04f;
        weapon.diagonalAngle = 22f;
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(weapon);
        if (undo && PrefabUtility.IsPartOfPrefabInstance(root))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(weapon);
        }
    }

    public static object OpenWorld()
    {
        foreach (var name in new[] { "Main", "Lighting", "Environment" })
        {
            string path = "Assets/Scenes/First Island/" + name + ".unity";
            if (!UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path).isLoaded)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        return true;
    }
}
