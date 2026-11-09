#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mavis
{
    // Explicit editor bake only. Static collision is independent of visual LOD/residency.
    public static class IslandEcologyCollisionBaker
    {
        const string Prefix = "Solid decoration | ";

        public static int Rebuild(IslandEcologyRenderer output)
        {
            if (Application.isPlaying || !output || !output.data)
                throw new InvalidOperationException("Stopped ecology authoring output is required.");
            var solid = output.data.species.Where(s => Kind(s.name) != null).ToArray();
            var parentScale = output.transform.lossyScale;
            if (parentScale.x <= 0 || Mathf.Abs(parentScale.x - parentScale.y) > .001f || Mathf.Abs(parentScale.x - parentScale.z) > .001f)
                throw new InvalidOperationException("Use positive uniform output scale before baking collision.");
            if (solid.Any(s => !s.near || s.cells == null || s.cells.Any(c => c.positions == null || c.normals == null ||
                c.positions.Length != c.normals.Length || c.normals.Any(n => n.w <= 0 || float.IsNaN(n.w)))))
                throw new InvalidOperationException("Solid ecology data is incomplete.");
            // Only owned direct children. Never touch tree colliders, user shapes or other layers.
            var previous = output.transform.Cast<Transform>().Where(t => t.GetComponent<Collider>() &&
                new[] { "Rock", "Timber", "Branch", "Sapling" }.Any(kind =>
                    t.name.StartsWith(Prefix + kind + " | ", StringComparison.Ordinal))).ToArray();
            foreach (var t in previous) Undo.DestroyObjectImmediate(t.gameObject);
            int count = 0;
            foreach (var species in solid)
            {
                int index = 0;
                string kind = Kind(species.name);
                foreach (var cell in species.cells)
                for (int i = 0; i < cell.positions.Length; i++, index++)
                {
                    var p = cell.positions[i]; var n = cell.normals[i];
                    var position = new Vector3(p.x, p.y, p.z);
                    var rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(n.x, n.y, n.z)) * Quaternion.Euler(0, p.w, 0);
                    var go = new GameObject(Prefix + kind + " | " + species.name + " " + index);
                    Undo.RegisterCreatedObjectUndo(go, "Bake solid ecology collision");
                    go.transform.SetParent(output.transform, false);
                    go.layer = output.gameObject.layer; go.isStatic = true;
                    if (kind == "Sapling")
                    {
                        float height = species.near.bounds.size.y * n.w;
                        position += rotation * (Vector3.up * (height * .29f));
                        go.transform.SetPositionAndRotation(position, rotation);
                        go.transform.localScale = InverseScale(output.transform.lossyScale);
                        var capsule = go.AddComponent<CapsuleCollider>();
                        capsule.radius = Mathf.Max(.07f, .12f * n.w);
                        capsule.height = Mathf.Max(.4f, height * .60f);
                    }
                    else
                    {
                        go.transform.SetPositionAndRotation(position, rotation);
                        go.transform.localScale = InverseScale(output.transform.lossyScale) * n.w;
                        go.AddComponent<MeshCollider>().sharedMesh = species.near;
                    }
                    count++;
                }
            }
            EditorSceneManager.MarkSceneDirty(output.gameObject.scene);
            Physics.SyncTransforms();
            return count;
        }

        static Vector3 InverseScale(Vector3 s)
        {
            if (s.x <= 0 || s.y <= 0 || s.z <= 0) throw new InvalidOperationException("Positive output scale is required.");
            return new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
        }
        static string Kind(string name) => name.StartsWith("Fractured ledge") ? "Rock" :
            name.StartsWith("Ship timber") ? "Timber" : name == "Stranded branch" ? "Branch" :
            name == "Rowan sapling" || name == "Wind bent alder sapling" ? "Sapling" : null;
    }
}
#endif
