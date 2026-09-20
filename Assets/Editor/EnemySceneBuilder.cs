// EnemySceneBuilder.cs
// One-shot Editor tool: builds a placeholder Cube enemy in Assets/Scenes/Enemies.unity
//   - Adds NavMeshAgent + Health + EnemyAI + EnemyAttack
//   - Adds a child trigger collider as the attack hitbox
//   - Tags the root "Enemy"
//   - Saves the scene
// Optionally spawns a spawner pointing at the prefab.
//
// Run via menu: Tools > Build > Enemy Sample Scene
//
// Replace the cube mesh / material with your real enemy model later — the
// components will keep working unchanged.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Mavis;

public static class EnemySceneBuilder
{
    private const string MenuPath = "Tools/Build/Enemy Sample Scene";
    private const string ScenePath = "Assets/Scenes/Enemies.unity";
    private const string PrefabPath = "Assets/Resources/Enemies/EnemyPlaceholder.prefab";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        // 1) Open the Enemies scene
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[EnemySceneBuilder] Failed to open scene at {ScenePath}");
            return;
        }

        // 2) Don't double-build: if a "EnemyPlaceholder" already exists, bail
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "EnemyPlaceholder")
            {
                if (!EditorUtility.DisplayDialog(
                    "Enemy already exists",
                    $"An object named 'EnemyPlaceholder' is already in {ScenePath}.\nRebuild it (overwrite)?",
                    "Rebuild", "Cancel"))
                    return;
                Object.DestroyImmediate(root);
                break;
            }
        }

        // 3) Build the enemy hierarchy
        var rootGo = BuildEnemyHierarchy(scene);

        // 4) Save as prefab too, so spawners can reference it
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath, out bool ok);
        if (!ok)
            Debug.LogWarning("[EnemySceneBuilder] Prefab save reported failure; continuing.");

        // 5) Selection
        Selection.activeGameObject = rootGo;
        EditorGUIUtility.PingObject(rootGo);

        // 6) Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Enemy sample built",
            $"Built sample enemy at scene root.\n\n" +
            $"Scene: {ScenePath}\n" +
            $"Prefab: {PrefabPath}\n\n" +
            "Next steps:\n" +
            "  1. Bake a NavMesh on the Environment (Window > AI > Navigation).\n" +
            "  2. Replace the Cube mesh with your real enemy model.\n" +
            "  3. (Optional) Drop an EnemySpawner in the scene and assign the prefab.",
            "OK");
    }

    private static GameObject BuildEnemyHierarchy(Scene scene)
    {
        // Root + visuals
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "EnemyPlaceholder";
        SceneManager.MoveGameObjectToScene(root, scene);
        root.tag = "Enemy";
        root.transform.position = new Vector3(0f, 1f, 0f);

        // NavMeshAgent
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.speed = 3.5f;
        agent.angularSpeed = 360f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 1.5f;

        // Health
        var health = root.AddComponent<Health>();
        health.maxHealth = 50f;
        health.currentHealth = 50f;

        // EnemyAI
        var ai = root.AddComponent<EnemyAI>();
        ai.sightRange = 15f;
        ai.attackRange = 2f;
        ai.attackInterval = 1.5f;

        // EnemyAttack + child hitbox
        var attack = root.AddComponent<EnemyAttack>();
        attack.damage = 8f;

        var hitbox = new GameObject("AttackHitbox");
        SceneManager.MoveGameObjectToScene(hitbox, scene);
        hitbox.transform.SetParent(root.transform, false);
        hitbox.transform.localPosition = new Vector3(0f, 0.5f, 0.7f);
        hitbox.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        var hbCol = hitbox.AddComponent<BoxCollider>();
        hbCol.isTrigger = true;
        attack.attackHitbox = hbCol;

        return root;
    }
}