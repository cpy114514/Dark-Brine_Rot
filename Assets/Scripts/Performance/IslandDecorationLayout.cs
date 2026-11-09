using System;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using Mavis;

// This authoring hierarchy is tagged EditorOnly. Runtime uses the existing batched renderer.
public sealed class IslandDecorationLayout : MonoBehaviour
{
    public IslandEcologyRenderer output;
    public IslandEcologySettings protection;
    public MeshCollider ground;
    public int seed = 611060;

#if UNITY_EDITOR
    [ContextMenu("Bake edited placements")]
    public void Bake()
    {
        if (Application.isPlaying || !output || !output.data || !ground || !protection)
            throw new InvalidOperationException("Stopped authoring scene with ground, protection and output is required.");
        var next = new List<IslandEcologyData.Species>();
        var markers = GetComponentsInChildren<Transform>().Where(t => t.childCount == 0 && t != transform).ToArray();
        foreach (var marker in markers)
        {
            if (!int.TryParse(marker.name.Split('|')[0], out int index) || index < 0 || index >= output.data.species.Length)
                throw new InvalidOperationException("Invalid palette index: " + marker.name);
            Vector3 scale = marker.lossyScale;
            if (scale.x <= 0 || Mathf.Abs(scale.x - scale.y) > .001f || Mathf.Abs(scale.x - scale.z) > .001f)
                throw new InvalidOperationException("Use positive uniform scale: " + marker.name);
            var bounds = output.data.species[index].near.bounds;
            float radius = new Vector2(Mathf.Abs(bounds.center.x) + bounds.extents.x, Mathf.Abs(bounds.center.z) + bounds.extents.z).magnitude * scale.x;
            if (!Allowed(new Vector2(marker.position.x, marker.position.z), radius, protection))
                throw new InvalidOperationException("Placement enters a protected route or arena: " + marker.name);
            if (!ground.Raycast(new Ray(marker.position + Vector3.up * 20, Vector3.down), out var hit, 40) || hit.normal.y < .83f || Mathf.Abs(marker.position.y - hit.point.y) > .22f)
                throw new InvalidOperationException("Placement has lost terrain support: " + marker.name);
        }
        for (int i = 0; i < output.data.species.Length; i++)
        {
            var s = output.data.species[i];
            var cells = new List<IslandEcologyData.Cell>();
            foreach (var cell in markers.Where(t => t.name.StartsWith(i + "|")).GroupBy(t => new Vector2Int(Mathf.FloorToInt(t.position.x / 24), Mathf.FloorToInt(t.position.z / 24))))
            {
                var positions = new List<Vector4>(); var normals = new List<Vector4>(); Bounds bounds = default; bool first = true;
                foreach (var t in cell)
                {
                    Vector3 n = t.up, p = t.position; float scale = t.lossyScale.x;
                    var twist = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.up, n)) * t.rotation;
                    positions.Add(new Vector4(p.x, p.y, p.z, twist.eulerAngles.y)); normals.Add(new Vector4(n.x, n.y, n.z, scale));
                    foreach (var mesh in new[] {s.near, s.far})
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var b = mesh.bounds; var v = t.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                        if (first) {bounds = new Bounds(v, Vector3.zero); first = false;} else bounds.Encapsulate(v);
                    }
                }
                bounds.Expand(.4f);
                cells.Add(new IslandEcologyData.Cell {bounds = bounds, positions = positions.ToArray(), normals = normals.ToArray()});
            }
            next.Add(new IslandEcologyData.Species {name=s.name, uncanny=s.uncanny, near=s.near, far=s.far, material=s.material, lodStart=s.lodStart, lodEnd=s.lodEnd, drawDistance=s.drawDistance, shadowDistance=s.shadowDistance, cells=cells.ToArray()});
        }
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Bake island decoration and solid collision");
        try
        {
            Undo.RecordObject(output.data, "Bake island decoration edits");
            output.data.species = next.ToArray(); output.data.seed = seed;
            IslandEcologyCollisionBaker.Rebuild(output);
            EditorUtility.SetDirty(output.data); AssetDatabase.SaveAssetIfDirty(output.data); output.Rebuild();
            Undo.CollapseUndoOperations(undo);
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(undo);
            output.Rebuild();
            throw;
        }
        Debug.Log("Island decoration: baked " + markers.Length + " editable placements.");
    }
    public static bool Allowed(Vector2 p, float radius, IslandEcologySettings s)
    {
        if (s.clearings.Any(c => Vector2.Distance(p,c.center) < c.radius + radius + 1.5f)) return false;
        foreach (var c in s.corridors) for (int i=1;i<c.points.Length;i++)
        {
            var a=c.points[i-1]; var d=c.points[i]-a;
            if(Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude))) < c.halfWidth+radius+1.5f) return false;
        }
        return true;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.6f,.8f,.3f,.65f);
        foreach(var t in GetComponentsInChildren<Transform>().Where(t=>t.childCount==0&&t!=transform)) Gizmos.DrawWireSphere(t.position,.16f);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(IslandDecorationLayout))]
public sealed class IslandDecorationLayoutInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Move, rotate, uniformly scale or duplicate the named child markers, then bake. Keep the numeric palette prefix. Baking validates terrain and route buffers, updates baked data and solid collision together. Save the Environment scene after baking. The EditorOnly marker hierarchy is stripped from builds.",MessageType.Info);
        if(GUILayout.Button("Bake edited placements")) ((IslandDecorationLayout)target).Bake();
    }
}
#endif
