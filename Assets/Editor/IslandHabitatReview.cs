using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;

public sealed class IslandHabitatReview : EditorWindow
{
    const string Environment = "Assets/Scenes/First Island/Environment.unity";
    const string Protection = "Assets/Generated/IslandEcology074/Settings.asset";
    static readonly Color TrailColor = new Color(.92f, .71f, .28f);
    static readonly Color ClearingColor = new Color(.9f, .37f, .26f);
    static readonly Color DiscoveryColor = new Color(.73f, .56f, .91f);
    static readonly Color DetailColor = new Color(.33f, .76f, .71f);

    bool showRoutes = true;
    bool showDiscoveries = true;
    bool showGroundDetails;

    [MenuItem("Tools/Island 1/Habitat Review")]
    static void Open() => GetWindow<IslandHabitatReview>("Island 1 Habitat");

    void OnEnable() => SceneView.duringSceneGui += DrawScene;
    void OnDisable()
    {
        SceneView.duringSceneGui -= DrawScene;
        SceneView.RepaintAll();
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        showRoutes = EditorGUILayout.ToggleLeft("Gameplay corridors and clearings", showRoutes);
        showDiscoveries = EditorGUILayout.ToggleLeft("Rare plants", showDiscoveries);
        showGroundDetails = EditorGUILayout.ToggleLeft("Stone and shoreline dressing", showGroundDetails);
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

        var terrain = FindIsland();
        if (!terrain || !terrain.habitatMap)
        {
            EditorGUILayout.HelpBox("Open the baked Island 1 Environment scene.", MessageType.Info);
            return;
        }
        if (GUILayout.Button("Inspect habitat mask")) Selection.activeObject = terrain.habitatMap;
        if (GUILayout.Button("Edit routes and clearings")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<IslandEcologySettings>(Protection);
        if (GUILayout.Button("Edit baked plant and prop placements"))
        {
            var ecology = FindEcology();
            if (ecology) Selection.activeObject = ecology.data;
        }
        EditorGUILayout.LabelField("Mask channels", "R turf • G wear • B stone • A damp sand");
    }

    static ProceduralIsland FindIsland() => FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None)
        .FirstOrDefault(i => i.isActiveAndEnabled && i.gameObject.scene.path == Environment);

    static IslandEcologyRenderer FindEcology() => FindObjectsByType<IslandEcologyRenderer>(FindObjectsSortMode.None)
        .FirstOrDefault(i => i.gameObject.scene.path == Environment);

    void DrawScene(SceneView view)
    {
        if (Event.current.type != EventType.Repaint || EditorApplication.isPlaying || (!showRoutes && !showDiscoveries && !showGroundDetails)) return;
        var terrain = FindIsland();
        if (!terrain || !terrain.habitatMap) return;
        using (new Handles.DrawingScope(Color.white))
        {
            if (showRoutes) DrawRoutes(terrain.GetComponent<MeshCollider>());
            if (showDiscoveries || showGroundDetails) DrawPlacements(view.camera);
        }
    }

    static void DrawRoutes(MeshCollider ground)
    {
        var settings = AssetDatabase.LoadAssetAtPath<IslandEcologySettings>(Protection);
        if (!settings) return;
        var bounds = ground.bounds;
        Vector3 Surface(Vector2 p) => ground.Raycast(new Ray(new Vector3(p.x, bounds.max.y + 2, p.y), Vector3.down),
            out var hit, bounds.size.y + 4) ? hit.point + Vector3.up * .35f : new Vector3(p.x, bounds.min.y, p.y);

        Handles.color = TrailColor;
        foreach (var route in settings.corridors)
        for (int i = 1; i < route.points.Length; i++)
        {
            Vector2 a = route.points[i - 1], b = route.points[i];
            Vector2 d = b - a;
            Vector2 edge = new Vector2(-d.y, d.x).normalized * route.halfWidth;
            int steps = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / 5));
            for (int side = -1; side <= 1; side += 2)
                Handles.DrawAAPolyLine(2f, Enumerable.Range(0, steps + 1)
                    .Select(step => Surface(Vector2.Lerp(a, b, step / (float)steps) + side * edge)).ToArray());
        }
        Handles.color = ClearingColor;
        foreach (var clearing in settings.clearings)
            Handles.DrawAAPolyLine(2f, Enumerable.Range(0, 41).Select(i =>
            {
                float t = i * Mathf.PI / 20;
                return Surface(clearing.center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * clearing.radius);
            }).ToArray());
    }

    void DrawPlacements(Camera camera)
    {
        var ecology = FindEcology();
        if (!ecology || !ecology.data || !camera) return;
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        foreach (var species in ecology.data.species)
        {
            if (species.uncanny ? !showDiscoveries : !showGroundDetails || !IsGroundDetail(species.name)) continue;
            Handles.color = species.uncanny ? DiscoveryColor : DetailColor;
            foreach (var cell in species.cells)
            {
                if (!GeometryUtility.TestPlanesAABB(planes, cell.bounds)) continue;
                foreach (Vector4 p in cell.positions)
                {
                    Vector3 point = (Vector3)p + Vector3.up * .25f;
                    Handles.DotHandleCap(0, point, Quaternion.identity, HandleUtility.GetHandleSize(point) * .045f, EventType.Repaint);
                }
            }
        }
    }

    static bool IsGroundDetail(string name) => name.StartsWith("Shingle") || name.StartsWith("Fractured") ||
        name == "Stranded branch" || name == "Shell wrack";
}
