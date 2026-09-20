// RebuildFoliagePhysics.cs
// Replaces the old per-LOD colliders with a simplified MeshCollider made from bark geometry.
// Grass intentionally has no physical collider: the player can walk through it and the
// existing global PlayerSquishDriver still provides the visual response.
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class RebuildFoliagePhysics
    {
        const string ScenePath = "Assets/Scenes/Environment.unity";
        const string ColliderFolder = "Assets/Foliage/Colliders";
        const int TriangleBudget = 8000;

        [MenuItem("Mavis/Foliage/Rebuild Tree Model Colliders")]
        public static void RepairFromMenu() => RepairActiveScene();

        static void RepairActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || !scene.isLoaded)
            {
                Debug.LogWarning("[Mavis] Foliage physics repair only runs in Environment.");
                return;
            }

            var treeRoots = new List<Transform>();
            var grassRoots = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
                CollectFoliageRoots(root.transform, treeRoots, grassRoots);

            // Prepare every mesh before removing the working colliders from the scene.
            var prepared = new List<(MeshRenderer renderer, Mesh collisionMesh)>();
            var generated = new Dictionary<Mesh, Mesh>();
            foreach (Transform tree in treeRoots)
            {
                if (!TryFindBarkRenderer(tree, out MeshRenderer renderer, out int barkIndex))
                {
                    Debug.LogError("[Mavis] Keeping existing colliders; no bark mesh on " + tree.name);
                    return;
                }
                Mesh source = renderer.GetComponent<MeshFilter>().sharedMesh;
                if (!generated.TryGetValue(source, out Mesh collisionMesh))
                {
                    collisionMesh = BuildCollisionMesh(source, barkIndex);
                    if (collisionMesh == null)
                    {
                        Debug.LogError("[Mavis] Keeping existing colliders; could not simplify " + source.name);
                        return;
                    }
                    generated.Add(source, collisionMesh);
                }
                prepared.Add((renderer, collisionMesh));
            }

            int removedColliders = 0;
            int removedTriggers = 0;
            for (int i = 0; i < treeRoots.Count; i++)
            {
                Transform tree = treeRoots[i];
                removedColliders += RemoveColliders(tree);
                removedTriggers += RemoveSquishTriggers(tree);
                var collider = prepared[i].renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = prepared[i].collisionMesh;
                collider.convex = false;
                collider.isTrigger = false;
            }

            foreach (Transform grass in grassRoots)
            {
                // A grass root can be contained in a tree prefab in future; do not remove a
                // newly-created tree collider in that case.
                if (FindTreeAncestor(grass) != null)
                    continue;
                removedColliders += RemoveColliders(grass);
                removedTriggers += RemoveSquishTriggers(grass);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string summary = $"trees={treeRoots.Count}, grass={grassRoots.Count}, bark mesh colliders={prepared.Count}, unique collision meshes={generated.Count}, removed colliders={removedColliders}, removed foliage triggers={removedTriggers}";
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/foliage_physics_rebuild.txt", summary + "\n", new UTF8Encoding(false));
            Debug.Log("[Mavis] Rebuilt foliage physics: " + summary);
        }

        static void CollectFoliageRoots(Transform current, List<Transform> trees, List<Transform> grass)
        {
            if (IsTreeRoot(current.name))
            {
                trees.Add(current);
                return;
            }

            if (IsGrassRoot(current.name))
            {
                grass.Add(current);
                return;
            }

            for (int i = 0; i < current.childCount; i++)
                CollectFoliageRoots(current.GetChild(i), trees, grass);
        }

        static bool IsTreeRoot(string name)
        {
            return name.StartsWith("island_tree_01_4k") ||
                   name.StartsWith("tree_small_02_4k") ||
                   name.StartsWith("dead_tree_trunk_02_4k");
        }

        static bool IsGrassRoot(string name)
        {
            return name == "Forest001" || name.StartsWith("rostlinka");
        }

        static Transform FindTreeAncestor(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (IsTreeRoot(current.name))
                    return current;
            return null;
        }

        static int RemoveColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
                Object.DestroyImmediate(collider);
            return colliders.Length;
        }

        static int RemoveSquishTriggers(Transform root)
        {
            FoliageSquishTrigger[] triggers = root.GetComponentsInChildren<FoliageSquishTrigger>(true);
            foreach (FoliageSquishTrigger trigger in triggers)
                Object.DestroyImmediate(trigger);
            return triggers.Length;
        }

        static bool TryFindBarkRenderer(Transform root, out MeshRenderer selected, out int barkIndex)
        {
            selected = null;
            barkIndex = -1;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                // The first imported island tree uses geometry_nodes as its near LOD.
                if (!renderer.name.EndsWith("_LOD0") && !renderer.name.Contains("geometry_nodes")) continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < Mathf.Min(mesh.subMeshCount, slots.Length); i++)
                {
                    if (slots[i] == null || !slots[i].name.EndsWith("_Bark")) continue;
                    if (selected == null || (renderer.name.EndsWith("_LOD0") && !selected.name.EndsWith("_LOD0")))
                    {
                        selected = renderer;
                        barkIndex = i;
                    }
                    break;
                }
            }
            return selected != null;
        }

        static Mesh BuildCollisionMesh(Mesh source, int barkIndex)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
            if (string.IsNullOrEmpty(guid)) return null;
            string path = $"{ColliderFolder}/{guid}_{localId}_BarkCollider.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            Bounds bounds = source.GetSubMesh(barkIndex).bounds;
            Mesh collisionMesh = null;
            using (Mesh.MeshDataArray dataArray = MeshUtility.AcquireReadOnlyMeshData(source))
            {
                Mesh.MeshData data = dataArray[0];
                using (var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.TempJob))
                using (var indices = new NativeArray<int>((int)source.GetIndexCount(barkIndex), Allocator.TempJob))
                {
                    data.GetVertices(vertices);
                    data.GetIndices(indices, barkIndex, true);
                    float cellSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / 45f;
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        if (collisionMesh != null) Object.DestroyImmediate(collisionMesh);
                        collisionMesh = ClusterBark(vertices, indices, bounds.min, cellSize);
                        if (collisionMesh.triangles.Length / 3 <= TriangleBudget) break;
                        cellSize *= 1.35f;
                    }
                }
            }
            if (collisionMesh == null || collisionMesh.triangles.Length < 3) return null;
            collisionMesh.name = source.name + " Bark Collision";
            Directory.CreateDirectory(ColliderFolder);
            AssetDatabase.CreateAsset(collisionMesh, path);
            Debug.Log($"[Mavis] Created bark collision mesh {path}, {collisionMesh.triangles.Length / 3} triangles");
            return collisionMesh;
        }

        static Mesh ClusterBark(NativeArray<Vector3> vertices, NativeArray<int> indices, Vector3 origin, float cellSize)
        {
            var cells = new Dictionary<Vector3Int, int>();
            var sums = new List<Vector3>();
            var counts = new List<int>();
            var triangles = new List<int>();
            var uniqueTriangles = new HashSet<(int, int, int)>();
            int Cluster(int originalIndex)
            {
                Vector3 position = vertices[originalIndex];
                var key = new Vector3Int(
                    Mathf.FloorToInt((position.x - origin.x) / cellSize),
                    Mathf.FloorToInt((position.y - origin.y) / cellSize),
                    Mathf.FloorToInt((position.z - origin.z) / cellSize));
                if (!cells.TryGetValue(key, out int clusteredIndex))
                {
                    clusteredIndex = sums.Count;
                    cells.Add(key, clusteredIndex);
                    sums.Add(Vector3.zero);
                    counts.Add(0);
                }
                sums[clusteredIndex] += position;
                counts[clusteredIndex]++;
                return clusteredIndex;
            }

            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                int a = Cluster(indices[i]);
                int b = Cluster(indices[i + 1]);
                int c = Cluster(indices[i + 2]);
                if (a == b || b == c || c == a) continue;
                int min = Mathf.Min(a, b, c);
                int max = Mathf.Max(a, b, c);
                int middle = a + b + c - min - max;
                if (!uniqueTriangles.Add((min, middle, max))) continue;
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            var positions = new Vector3[sums.Count];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = sums[i] / counts[i];
            var mesh = new Mesh { indexFormat = positions.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.vertices = positions;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
