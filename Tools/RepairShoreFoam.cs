using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RepairShoreFoam
{
    const string ScenePath = "Assets/Scenes/First Island/Environment.unity";
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode before saving.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        foreach (var island in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProceduralIsland>(true)))
            if (island.isActiveAndEnabled) island.Rebuild();
        foreach (var floor in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProceduralSeabed>(true)))
            if (floor.isActiveAndEnabled) floor.Rebuild();
        var result = Verify();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Environment save failed.");
        return result;
    }
    public static object Verify()
    {
        var islands = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(i => i.isActiveAndEnabled).ToArray();
        if (islands.Length == 0) throw new Exception("No active island to verify.");
        foreach (var island in islands)
        {
            var foam = island.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.name == "Procedural Shore Foam" && r.enabled).ToArray();
            if (foam.Length != 1) throw new Exception("Expected exactly one shore ribbon: " + foam.Length);
            var renderer = foam[0];
            var material = renderer.sharedMaterial;
            if (material == null || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                throw new Exception("Shore foam material is missing or invalid.");
            if ((renderer.gameObject.hideFlags & HideFlags.DontSaveInEditor) == 0)
                throw new Exception("Generated ribbon would still be serialized without its material.");
        }
        return new { activeIslands = islands.Length, ribbonsPerIsland = 1, missingFoamMaterials = 0,
            shaderErrors = 0, generatedRibbonsExcludedFromSceneSave = true };
    }
    public static object ReloadAndVerify()
    {
        if (Application.isPlaying) throw new Exception("Reload verification requires Edit Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (scene.isDirty) throw new Exception("Save environment changes before reload.");
        EditorSceneManager.CloseScene(scene, true);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        return Verify();
    }
    public static object CreateInspectionCamera()
    {
        var island = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i => i.isActiveAndEnabled);
        var mesh = island.GetComponent<MeshFilter>().sharedMesh;
        var rim = island.transform.TransformPoint(mesh.vertices[mesh.vertexCount - island.radialSegments]);
        float sea = UnityEngine.Object.FindFirstObjectByType<OceanWorld>().oceanHeight;
        var inward = island.transform.position;
        for (int i = 1; i <= 100; i++)
        {
            var point = Vector3.Lerp(island.transform.position, rim, i / 100f);
            if (island.GetWorldSurfaceHeight(point) > sea + 1f) inward = point;
        }
        inward.y = Mathf.Max(sea, island.GetWorldSurfaceHeight(inward)) + 3f;
        var go = new GameObject("Shore Repair Inspection Camera") { hideFlags = HideFlags.DontSave };
        var camera = go.AddComponent<Camera>();
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1500f;
        camera.fieldOfView = 65f;
        go.transform.position = inward;
        var target = rim + (rim - island.transform.position).normalized * 12f;
        target.y = sea;
        go.transform.LookAt(target);
        return inward.ToString();
    }
    public static void RestoreEditor()
    {
        var go = GameObject.Find("Shore Repair Inspection Camera");
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        foreach (string name in new[] { "Main", "Lighting", "Environment" })
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/First Island/" + name + ".unity");
            if (scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
