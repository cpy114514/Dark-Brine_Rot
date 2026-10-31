using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlaceMarshmallowOutposts
{
    const string Folder = "Assets/Game/Prefabs/Enemies/Marshmallow/";
    static readonly string[] Colors = { "Strawberry", "Mint", "Sky", "Lemon", "Lavender" };
    sealed class Outpost
    {
        public Transform candy;
        public readonly List<Transform> existing = new List<Transform>();
        public readonly List<Vector3> additions = new List<Vector3>();
    }
    [MenuItem("Tools/Second Island Review/Place Marshmallow Outposts")]
    public static void Place()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Second Island.unity") throw new Exception("Open Second Island in edit mode.");
        bool wasDirty = scene.isDirty;
        var garden = GameObject.Find("Candy Garden - Second Island");
        var enemies = GameObject.Find("Marshmallow Friends - Second Island");
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var exp = player ? player.GetComponent<MarshmallowIslandExperience>() : null;
        var ground = exp && exp.surface ? exp.surface.GetComponent<Collider>() : null;
        if (!garden || !enemies || !ground) throw new Exception("Candy garden, existing enemies or island ground missing.");
        var anchors = garden.GetComponentsInChildren<Transform>().Where(t => t.name == "Strawberry Swirl Lollipop")
            .OrderBy(t => t.parent.name, StringComparer.Ordinal).Select(t => new Outpost { candy = t }).ToList();
        if (anchors.Count == 0) throw new Exception("No lollipop outposts found.");
        var prefabs = Colors.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(Folder + c + " Marshmallow.prefab")).ToArray();
        if (prefabs.Any(x => !x)) throw new Exception("One or more marshmallow prefabs are missing.");
        var existing = enemies.GetComponentsInChildren<MarshmallowEnemyHealth>(true);
        if (existing.Length != 5 || enemies.transform.childCount != 5) throw new Exception("The enemy group has changed; refusing to replace existing work.");
        var occupied = new List<Vector3>();
        foreach (var hp in existing)
        {
            var near = anchors.OrderBy(a => PlanarDistance(a.candy.position, hp.transform.position)).First();
            if (PlanarDistance(near.candy.position, hp.transform.position) > 25f) throw new Exception("Existing enemy is too far from every lollipop: " + hp.name);
            near.existing.Add(hp.transform); occupied.Add(hp.transform.position);
        }
        Physics.SyncTransforms();
        // Plan all placements before changing the scene, preserving the five existing enemies.
        for (int a = 0; a < anchors.Count; a++)
        {
            var post = anchors[a];
            for (int attempt = 0; attempt < 1000 && post.existing.Count + post.additions.Count < 10; attempt++)
            {
                float angle = (attempt * 2.399963f + a * .67f);
                float radius = 6.5f + (attempt % 13) * .85f;
                var flat = post.candy.position + new Vector3(Mathf.Cos(angle)*radius, 0, Mathf.Sin(angle)*radius);
                if (PlanarDistance(flat, player.transform.position) < 18f) continue;
                if (!ground.Raycast(new Ray(flat + Vector3.up*150f, Vector3.down), out var hit, 350f) || hit.normal.y < .77f) continue;
                Vector3 point = hit.point + Vector3.up * .08f;
                if (Mathf.Abs(point.y - post.candy.position.y) > 8f || occupied.Any(p => PlanarDistance(p, point) < 2.8f)) continue;
                bool blocked = false;
                foreach (var collider in Physics.OverlapCapsule(point + Vector3.up*.75f, point + Vector3.up*1.4f, .72f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!collider || collider == ground || collider.transform.IsChildOf(enemies.transform)) continue;
                    blocked = true; break;
                }
                if (blocked) continue;
                post.additions.Add(point); occupied.Add(point);
            }
            if (post.existing.Count + post.additions.Count != 10)
                throw new Exception("Could only find " + (post.existing.Count + post.additions.Count) + " safe places at " + post.candy.parent.name + ". No scene changes were made.");
        }
        Undo.IncrementCurrentGroup(); int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place marshmallow outposts");
        for (int a = 0; a < anchors.Count; a++)
        {
            var post = anchors[a];
            var group = new GameObject("Lollipop outpost " + (a+1) + " - " + post.candy.parent.name);
            Undo.RegisterCreatedObjectUndo(group, "Create marshmallow outpost");
            group.transform.SetParent(enemies.transform, false);
            foreach (var old in post.existing) Undo.SetTransformParent(old, group.transform, "Assign existing marshmallow to outpost");
            for (int i = 0; i < post.additions.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[(i + post.existing.Count + a) % prefabs.Length], scene);
                Undo.RegisterCreatedObjectUndo(instance, "Place marshmallow enemy");
                instance.transform.SetParent(group.transform, true);
                instance.transform.position = post.additions[i];
                var facing = Vector3.ProjectOnPlane(post.candy.position - instance.transform.position, Vector3.up);
                instance.transform.rotation = Quaternion.LookRotation(facing.sqrMagnitude > .01f ? facing : Vector3.forward);
            }
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = enemies;
        Directory.CreateDirectory("Temp/MarshmallowOutposts");
        File.WriteAllText("Temp/MarshmallowOutposts/placement.txt", $"outposts={anchors.Count}; enemies={occupied.Count}; existing preserved={existing.Length}; scene saved={!wasDirty}");
        Debug.Log($"Placed {occupied.Count} marshmallow enemies at {anchors.Count} lollipop outposts. Scene saved: {!wasDirty}.");
    }
    static float PlanarDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
}
