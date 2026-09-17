// FoliageColliders.cs — adds proper colliders to the foliage on Sunset Island.
// Idempotent: skips objects that already carry any Collider component.
// Saves the scene after the run.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class FoliageColliders
    {
        const string LogPath = "Temp/foliage_colliders.txt";

        public static void Run()
        {
            int added = 0, skipped = 0, noMesh = 0;
            var sb = new StringBuilder();
            sb.AppendLine("start " + System.DateTime.Now.ToString("O"));

            try
            {
                // 用当前 active scene，避免 OpenScene 异步加载阻塞
                var scene = SceneManager.GetActiveScene();
                sb.AppendLine("active scene: " + scene.path + " loaded=" + scene.isLoaded);
                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
                    sb.AppendLine("opened " + scene.path);
                }

                var roots = scene.GetRootGameObjects();
                sb.AppendLine("roots: " + roots.Length);

                var allTransforms = new List<Transform>(512);
                foreach (var r in roots)
                {
                    if (r == null) continue;
                    CollectTransforms(r.transform, allTransforms);
                }
                sb.AppendLine("total transforms: " + allTransforms.Count);

                foreach (var t in allTransforms)
                {
                    if (t == null) { skipped++; continue; }
                    if (t.GetComponent<Collider>() != null) { skipped++; continue; }
                    var name = t.name;
                    var mf = t.GetComponent<MeshFilter>();
                    var mesh = mf != null ? mf.sharedMesh : null;
                    if (mesh == null) { noMesh++; continue; }

                    // 分类
                    if (name.StartsWith("boulder"))
                    {
                        AddMesh(t, mesh, convex: true);
                        added++;
                        sb.AppendLine("+ MeshCollider[convex](boulder) " + GetPath(t));
                    }
                    else if (name.Contains("geometry_nodes"))
                    {
                        AddCapsule(t, mesh);
                        added++;
                        sb.AppendLine("+ CapsuleCollider(trunk) " + GetPath(t));
                    }
                    else if (name.Contains("branches_high_poly") || name.Contains("branches"))
                    {
                        AddCapsule(t, mesh);
                        added++;
                        sb.AppendLine("+ CapsuleCollider(branches) " + GetPath(t));
                    }
                    else if (name.Contains("leaves_high_poly") || name.Contains("leaves"))
                    {
                        AddSphereTrigger(t, mesh);
                        added++;
                        sb.AppendLine("+ SphereCollider[trigger](leaves) " + GetPath(t));
                    }
                    else if (name.StartsWith("island_tree_01_LOD") || name.StartsWith("tree_small_02_LOD") || name == "LOD0" || name == "LOD1" || name == "LOD2" || name == "LOD3")
                    {
                        AddBox(t, mesh);
                        added++;
                        sb.AppendLine("+ BoxCollider(LOD) " + GetPath(t));
                    }
                    else if (name == "Forest001" || name.StartsWith("rostlinka_7c_scater"))
                    {
                        AddMesh(t, mesh, convex: true);
                        added++;
                        sb.AppendLine("+ MeshCollider[convex](grass) " + GetPath(t));
                    }
                    else if (name == "ground_close_04")
                    {
                        AddMesh(t, mesh, convex: true);
                        added++;
                        sb.AppendLine("+ MeshCollider[convex](ground) " + GetPath(t));
                    }
                    else
                    {
                        noMesh++;
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sb.AppendLine("---");
                sb.AppendLine(string.Format("done: added={0} skipped={1} noMeshOrUnrecognized={2}", added, skipped, noMesh));
            }
            catch (System.Exception e)
            {
                sb.AppendLine("EX: " + e);
            }

            File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Mavis] wrote " + LogPath);
        }

        static void AddMesh(Transform t, Mesh mesh, bool convex)
        {
            var c = t.gameObject.AddComponent<MeshCollider>();
            c.sharedMesh = mesh;
            c.convex = convex;
        }

        static void AddBox(Transform t, Mesh mesh)
        {
            var b = mesh.bounds;
            var c = t.gameObject.AddComponent<BoxCollider>();
            c.center = b.center;
            c.size = b.size;
        }

        static void AddCapsule(Transform t, Mesh mesh)
        {
            var b = mesh.bounds;
            var size = b.size;
            float radius, height;
            int dir;
            if (size.y >= size.x && size.y >= size.z)
            {
                height = size.y;
                radius = Mathf.Max(size.x, size.z) * 0.5f;
                dir = 0;
            }
            else if (size.x >= size.z)
            {
                height = size.x;
                radius = Mathf.Max(size.y, size.z) * 0.5f;
                dir = 1;
            }
            else
            {
                height = size.z;
                radius = Mathf.Max(size.x, size.y) * 0.5f;
                dir = 2;
            }
            var c = t.gameObject.AddComponent<CapsuleCollider>();
            c.center = b.center;
            c.radius = Mathf.Max(radius, 0.05f);
            c.height = Mathf.Max(height, c.radius * 2f);
            c.direction = dir;
        }

        static void AddSphereTrigger(Transform t, Mesh mesh)
        {
            var b = mesh.bounds;
            var c = t.gameObject.AddComponent<SphereCollider>();
            c.center = b.center;
            c.radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
            c.isTrigger = true;
        }

        static string GetPath(Transform t)
        {
            var sb = new StringBuilder(t.name);
            var p = t.parent;
            while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }

        static void CollectTransforms(Transform t, List<Transform> outList)
        {
            outList.Add(t);
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (c != null) CollectTransforms(c, outList);
            }
        }
    }
}
