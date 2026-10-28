using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class CombatTestArenaBuilder
{
    const string ScenePath = "Assets/Scenes/Combat Test/CombatTest.unity";

    [MenuItem("Tools/Combat/Open Combat Test Arena")]
    public static void Open() => Build();

    public static string Build()
    {
        if (Application.isPlaying) return "playing";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Combat Test"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Combat Test");
        bool dirty = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) dirty = true;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var root = new GameObject("Combat Test Arena");
        SceneManager.MoveGameObjectToScene(root, scene);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0f, -.5f, 0f);
        floor.transform.localScale = new Vector3(140f, 1f, 140f);
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Combat test floor", color = new Color(.16f, .18f, .16f) };
        AssetDatabase.CreateAsset(material, "Assets/Scenes/Combat Test/CombatTestFloor.mat");
        floor.GetComponent<Renderer>().sharedMaterial = material;
        var sunObject = new GameObject("Arena sun");
        sunObject.transform.SetParent(root.transform, false);
        sunObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.position = new Vector3(0f, 3f, -8f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 400f;
        cameraObject.AddComponent<AudioListener>();
        var surfaceObject = new GameObject("Arena navigation");
        surfaceObject.transform.SetParent(root.transform, false);
        var surface = surfaceObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0f, 2f, 0f);
        surface.size = new Vector3(130f, 12f, 130f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        var player = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), scene);
        player.name = "Sahur";
        var motor = player.GetComponent<CharacterController>();
        float sole = motor.center.y - motor.height * .5f;
        player.transform.SetPositionAndRotation(new Vector3(0f, .05f - sole, -8f), Quaternion.identity);
        var arena = root.AddComponent<Mavis.CombatTestArena>();
        arena.player = player.GetComponent<ThirdPersonPlayerController>();
        arena.cappuccino = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab");
        arena.nailong = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        arena.bombardino = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
        arena.chicken = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab");
        arena.duck = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab");
        EditorSceneManager.SaveScene(scene, ScenePath);
        if (!dirty) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return ScenePath;
    }

    public static string Check()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        foreach (var root in scene.GetRootGameObjects())
        {
            var arena = root.GetComponent<Mavis.CombatTestArena>();
            if (!arena) continue;
            return "cappuccino=" + (arena.cappuccino ? arena.cappuccino.name : "missing") +
                " nailong=" + (arena.nailong ? arena.nailong.name : "missing") +
                " bombardino=" + (arena.bombardino ? arena.bombardino.name : "missing") +
                " chicken=" + (arena.chicken ? arena.chicken.name : "missing") +
                " duck=" + (arena.duck ? arena.duck.name : "missing") +
                " player=" + (arena.player ? arena.player.name : "missing");
        }
        return "arena component missing";
    }
}
