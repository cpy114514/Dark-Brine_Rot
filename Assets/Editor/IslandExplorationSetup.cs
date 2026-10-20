using System;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotent authoring tool for the three supply landmarks and physical trail signs.</summary>
public static class IslandExplorationSetup
{
    static Material Material(string name, Color color)
    {
        const string directory = "Assets/Game/Materials/Exploration";
        if (!AssetDatabase.IsValidFolder("Assets/Game/Materials")) AssetDatabase.CreateFolder("Assets/Game", "Materials");
        if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/Game/Materials", "Exploration");
        string path = directory + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .12f);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
    static RaycastHit Ground(Vector3 point)
    {
        foreach (var hit in Physics.RaycastAll(new Vector3(point.x, 250, point.z), Vector3.down, 400, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider.GetComponentInParent<ProceduralIsland>()) return hit;
        throw new InvalidOperationException("No island beneath landmark: " + point);
    }
    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.isStatic = true; return go;
    }
    static IslandSupplyCache Cache(Transform parent, Vector3 point, string english, string chinese, Material wood, Material trim)
    {
        RaycastHit best = Ground(point); float score = float.PositiveInfinity;
        for (int x = -8; x <= 8; x += 4) for (int z = -8; z <= 8; z += 4)
        {
            var hit = Ground(point + new Vector3(x, 0, z));
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            bool blocked = false;
            foreach (var collider in Physics.OverlapBox(hit.point + Vector3.up * 1.2f, new Vector3(.9f, .35f, .9f)))
                if (!collider.isTrigger && !collider.GetComponentInParent<ProceduralIsland>()) { blocked = true; break; }
            float value = slope + new Vector2(x, z).magnitude * .3f;
            if (!blocked && slope < 18 && value < score) { best = hit; score = value; }
        }
        if (score == float.PositiveInfinity) throw new InvalidOperationException("No safe supply placement near " + point);
        var go = new GameObject(english); go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(best.point + best.normal * .06f, Quaternion.FromToRotation(Vector3.up, best.normal));
        var cache = go.AddComponent<IslandSupplyCache>(); cache.englishName = english; cache.chineseName = chinese;
        go.AddComponent<WorldSaveIdentity>().id = Guid.NewGuid().ToString("N");
        var crateCollider = go.AddComponent<BoxCollider>(); crateCollider.center = new Vector3(0, .5f, 0); crateCollider.size = new Vector3(1.6f, 1, 1.2f);
        Box(go.transform, "Supply crate", new Vector3(0, .5f, 0), new Vector3(1.6f, 1, 1.2f), wood);
        Box(go.transform, "Gold supply band", new Vector3(0, .51f, 0), new Vector3(.18f, 1.025f, 1.225f), trim);
        Box(go.transform, "Supply lid", new Vector3(0, 1.04f, 0), new Vector3(1.7f, .12f, 1.3f), wood);
        return cache;
    }
    static void Sign(Transform parent, Vector3 point, Transform destination, string english, string chinese, Material wood, Material trim)
    {
        var hit = Ground(point); var go = new GameObject("Trail sign - " + english); go.transform.SetParent(parent, false);
        var direction = Vector3.ProjectOnPlane(destination.position - hit.point, hit.normal).normalized;
        go.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(direction, hit.normal));
        Box(go.transform, "Timber post", new Vector3(0, 1.6f, 0), new Vector3(.2f, 3.2f, .2f), wood);
        Box(go.transform, "Destination board", new Vector3(0, 3, 0), new Vector3(3.7f, 1.1f, .18f), wood);
        Box(go.transform, "Arrow shaft", new Vector3(0, 3.85f, .05f), new Vector3(.13f, .12f, 1.4f), trim);
        var left = Box(go.transform, "Arrow head left", new Vector3(-.2f, 3.85f, .55f), new Vector3(.12f, .12f, .65f), trim);
        left.transform.localRotation = Quaternion.Euler(0, 42, 0);
        var right = Box(go.transform, "Arrow head right", new Vector3(.2f, 3.85f, .55f), new Vector3(.12f, .12f, .65f), trim);
        right.transform.localRotation = Quaternion.Euler(0, -42, 0);
        var caption = new GameObject("Destination"); caption.transform.SetParent(go.transform, false);
        caption.transform.localPosition = new Vector3(0, 3, -.11f);
        var text = caption.AddComponent<TextMesh>(); text.text = english; text.characterSize = .1f; text.fontSize = 56;
        text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(1, .88f, .5f);
        var landmark = go.AddComponent<IslandLandmark>(); landmark.englishName = english; landmark.chineseName = chinese;
        landmark.destination = destination; landmark.caption = text;
    }
    public static string Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Install in Edit Mode.");
        var environment = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if (!environment.IsValid() || !environment.isLoaded) throw new InvalidOperationException("Load Environment first.");
        foreach (var root in environment.GetRootGameObjects()) if (root.name == "Sahur Exploration Routes") return "Existing exploration layout preserved.";
        var group = new GameObject("Sahur Exploration Routes"); SceneManager.MoveGameObjectToScene(group, environment);
        Undo.RegisterCreatedObjectUndo(group, "Add island supply landmarks and trail signs");
        try
        {
            var wood = Material("Trail timber", new Color(.23f, .12f, .055f));
            var trim = Material("Supply brass", new Color(.95f, .62f, .12f));
            var south = Cache(group.transform, new Vector3(269, 0, 405), "Shore Camp", "海岸营地", wood, trim);
            var ridge = Cache(group.transform, new Vector3(215, 0, 545), "Ridge Cache", "山脊补给点", wood, trim);
            var north = Cache(group.transform, new Vector3(50, 0, 765), "North Lookout", "北岸瞭望点", wood, trim);
            Sign(group.transform, new Vector3(307, 0, 319), south.transform, "Shore Camp", "海岸营地", wood, trim);
            Sign(group.transform, new Vector3(269, 0, 415), ridge.transform, "Ridge Cache / M4A1", "山脊补给 / M4A1", wood, trim);
            Sign(group.transform, new Vector3(215, 0, 555), north.transform, "North Lookout", "北岸瞭望点", wood, trim);
            EditorSceneManager.MarkSceneDirty(environment); AssetDatabase.SaveAssets();
            return "Installed 3 persistent supply caches and 3 physical trail signs.";
        }
        catch { UnityEngine.Object.DestroyImmediate(group); throw; }
    }
    public static string FinishLayout()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        var caches = UnityEngine.Object.FindObjectsByType<IslandSupplyCache>(FindObjectsSortMode.None);
        var signs = UnityEngine.Object.FindObjectsByType<IslandLandmark>(FindObjectsSortMode.None);
        int cleared = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.activeSelf || !root.name.StartsWith("rostlinka_07c_ske", StringComparison.Ordinal)) continue;
            bool intersects = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.name.StartsWith("rostlinka", StringComparison.Ordinal)) continue;
                foreach (var cache in caches)
                {
                    Vector3 delta = renderer.bounds.ClosestPoint(cache.transform.position) - cache.transform.position; delta.y = 0;
                    if (delta.sqrMagnitude < 16) { intersects = true; break; }
                }
                if (!intersects) foreach (var sign in signs)
                {
                    Vector3 delta = renderer.bounds.ClosestPoint(sign.transform.position) - sign.transform.position; delta.y = 0;
                    if (delta.sqrMagnitude < 3.24f) { intersects = true; break; }
                }
                if (intersects) break;
            }
            if (!intersects) continue;
            Undo.RecordObject(root, "Clear small supply/sign footprints"); root.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root); cleared++;
        }
        foreach (var sign in signs)
            if (sign.caption)
            { Undo.RecordObject(sign.caption.transform, "Fit physical sign captions"); sign.caption.transform.localScale = Vector3.one * .65f; }
        EditorSceneManager.MarkSceneDirty(scene);
        return "Cleared " + cleared + " overlapping grass clumps at supply/sign footprints; originals retained inactive.";
    }
}
