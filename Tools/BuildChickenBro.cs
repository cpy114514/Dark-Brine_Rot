using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

/// <summary>Imports the supplied Chicken Bro FBX and builds its prefab plus the island encounter.</summary>
public static class BuildChickenBro
{
    const string ModelPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.fbx";
    const string ControllerPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.controller";
    const string PrefabPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";
    const string ScenePath = "Assets/Scenes/First Island/Enemies.unity";

    public static object Build()
    {
        if (Application.isPlaying) return new { ok = false, reason = "Exit Play Mode before building Chicken Bro." };
        if (!System.IO.File.Exists(ModelPath)) return new { ok = false, reason = "Missing " + ModelPath };

        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.globalScale = 1f;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.SaveAndReimport();

        var assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        var avatar = assets.OfType<Avatar>().FirstOrDefault();
        bool human = avatar && avatar.isValid && avatar.isHuman;
        if (!human)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.SaveAndReimport();
            assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            avatar = null;
        }
        var clips = assets.OfType<AnimationClip>().Where(clip => clip && !clip.name.StartsWith("__Preview__")).Select(clip => clip.name).Distinct().ToArray();
        var materials = assets.OfType<Material>().Select(material => material.name + " / " + (material.shader ? material.shader.name : "no shader")).Distinct().ToArray();

        var controller = BuildController();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var prefab = BuildPrefab(controller, scene, out float height, out string fit);
        var placed = PlaceEncounter(prefab, scene);
        AssetDatabase.SaveAssets();
        return new
        {
            ok = prefab,
            humanoid = human,
            avatar = avatar ? avatar.name : "generic, no avatar",
            clips,
            missing = new[] { "Idle", "Walk", "Run", "Attack", "Lunge Attack", "Hit Reaction", "Heavy Hit / Knockdown", "Death" },
            materials,
            height,
            fit,
            prefab = PrefabPath,
            scene = placed
        };
    }

    static AnimatorController BuildController()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing) AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Motion", AnimatorControllerParameterType.Int);
        var machine = controller.layers[0].stateMachine;
        string[] states = { "Idle", "Walk", "Run", "Attack", "Lunge", "Hit", "Heavy", "Death" };
        for (int i = 0; i < states.Length; i++)
        {
            var state = machine.AddState(states[i], new Vector3(300f, i * 50f, 0f));
            if (i == 0) machine.defaultState = state;
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = .08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, i, "Motion");
        }
        return controller;
    }

    static ChickenBroAI BuildPrefab(RuntimeAnimatorController controller, Scene scene, out float height, out string fit)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var root = new GameObject("ChickenBro");
        SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            try { root.tag = "Enemy"; }
            catch (UnityException) { Debug.LogError("Enemy tag is missing. Chicken Bro was left untagged."); }
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            height = FitVisual(root.transform, visual);
            fit = "Visual scaled so the body is about " + height.ToString("0.00") + " m tall, feet on the pivot.";
            var animator = visual.GetComponentInChildren<Animator>();
            if (!animator) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var motor = root.AddComponent<CharacterController>();
            motor.height = Mathf.Max(.8f, height * .92f);
            motor.radius = Mathf.Clamp(height * .22f, .22f, .42f);
            motor.center = new Vector3(0f, motor.height * .5f, 0f);
            motor.stepOffset = Mathf.Min(.35f, motor.height * .3f);
            var health = root.AddComponent<Health>();
            health.maxHealth = 48f;
            health.currentHealth = 48f;
            root.AddComponent<ChickenBroPose>();
            root.AddComponent<ChickenBroAI>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return prefab.GetComponent<ChickenBroAI>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    static float FitVisual(Transform root, GameObject visual)
    {
        Bounds bounds = WorldBounds(visual);
        float height = bounds.size.y;
        if (height < .8f || height > 2.4f)
        {
            visual.transform.localScale *= 1.5f / Mathf.Max(.01f, height);
            bounds = WorldBounds(visual);
            height = bounds.size.y;
        }
        visual.transform.position -= new Vector3(0f, bounds.min.y - root.position.y, 0f);
        return WorldBounds(visual).size.y;
    }

    static Bounds WorldBounds(GameObject visual)
    {
        Bounds bounds = new Bounds(visual.transform.position, Vector3.zero);
        bool any = false;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
        {
            if (!renderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!any) bounds = new Bounds(visual.transform.position, Vector3.one * 1.5f);
        return bounds;
    }

    static string PlaceEncounter(ChickenBroAI prefab, Scene scene)
    {
        foreach (var existing in scene.GetRootGameObjects())
            if (existing.name == "Chicken Bro Encounter")
            {
                var ready = existing.GetComponent<ChickenBroEncounter>();
                if (ready && !ready.prefab) ready.prefab = prefab;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return scene.path + " (already present)";
            }
        var go = new GameObject("Chicken Bro Encounter");
        SceneManager.MoveGameObjectToScene(go, scene);
        var encounter = go.AddComponent<ChickenBroEncounter>();
        encounter.prefab = prefab;
        var identity = go.AddComponent<WorldSaveIdentity>();
        identity.id = encounter.completionId;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.path;
    }

    public static string Inspect()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        var avatar = assets.OfType<Avatar>().FirstOrDefault();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var names = string.Join(", ", model.GetComponentsInChildren<Transform>(true).Select(t => t.name));
        var materials = string.Join(" | ", assets.OfType<Material>().Select(material => material.name + "=" + (material.shader ? material.shader.name : "none")));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var animator = prefab ? prefab.GetComponentInChildren<Animator>() : null;
        var health = prefab ? prefab.GetComponent<Health>() : null;
        return "human=" + (avatar && avatar.isValid && avatar.isHuman) +
            " tag=" + (prefab && prefab.CompareTag("Enemy")) +
            " hp=" + (health ? health.maxHealth : 0) +
            " controller=" + (animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.name : "none") +
            "\nbones: " + names +
            "\nmaterials: " + materials;
    }
}
