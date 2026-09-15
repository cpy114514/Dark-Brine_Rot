using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Diag
{
    [InitializeOnLoad]
    public static class DiagOnLoad
    {
        const string LogPath = "Temp/diag_3d_models.txt";

        static DiagOnLoad()
        {
            // 在编辑器下次 idle 跑一次，避免域重载时 EditorSceneManager 还没就绪。
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("== Assets/3d model inventory ==");

                string[] folder = { "Assets/3d model" };
                var guids = AssetDatabase.FindAssets(string.Empty, folder);
                int meshCount = 0, missingMatCount = 0;
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var ext = Path.GetExtension(path).ToLower();
                    if (ext != ".blend" && ext != ".fbx") continue;

                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (imp == null) { sb.AppendLine("  [NO-IMPORTER] " + path); continue; }
                    meshCount++;

                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    string meshName = "<none>";
                    if (go != null)
                    {
                        var mf0 = go.GetComponentInChildren<MeshFilter>();
                        if (mf0 != null && mf0.sharedMesh != null) meshName = mf0.sharedMesh.name;
                    }

                    int matCount = 0;
                    int nullMat = 0;
                    if (go != null)
                    {
                        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        {
                            foreach (var m in r.sharedMaterials)
                            {
                                matCount++;
                                if (m == null) nullMat++;
                            }
                        }
                    }

                    var deps = AssetDatabase.GetDependencies(path, true);
                    int texN = 0, missingN = 0;
                    foreach (var d in deps)
                    {
                        if (d.EndsWith(".jpg", true, null) || d.EndsWith(".png", true, null) ||
                            d.EndsWith(".exr", true, null) || d.EndsWith(".tga", true, null))
                        {
                            texN++;
                            if (AssetDatabase.LoadAssetAtPath<Texture>(d) == null) missingN++;
                        }
                    }

                    sb.AppendLine(string.Format(
                        "  {0,-50} mat={1} nullMat={2} tex={3} missTex={4} mesh={5}",
                        Path.GetFileName(path), matCount, nullMat, texN, missingN, meshName));

                    if (matCount == 0) missingMatCount++;
                }
                sb.AppendLine("total mesh assets: " + meshCount + "  with-0-mat: " + missingMatCount);

                sb.AppendLine();
                sb.AppendLine("== MainScene contents ==");
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
                int goCount = 0;
                foreach (var r in scene.GetRootGameObjects())
                {
                    foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    {
                        goCount++;
                        var mf = t.GetComponent<MeshFilter>();
                        var mr = t.GetComponent<MeshRenderer>();
                        string meshName = "-";
                        bool hasModelMesh = false;
                        if (mf != null && mf.sharedMesh != null)
                        {
                            meshName = mf.sharedMesh.name;
                            var nm = meshName;
                            if (!string.IsNullOrEmpty(nm) && nm != "Cube" && nm != "Sphere" && nm != "Plane")
                                hasModelMesh = true;
                        }
                        string flag = hasModelMesh ? " [HAS-MODEL-MESH]" : "";
                        sb.AppendLine(string.Format("  {0} pos={1} mesh={2}{3}",
                            GetPath(t), t.position.ToString("F2"), meshName, flag));
                    }
                }
                sb.AppendLine("total transforms: " + goCount);

                sb.AppendLine();
                sb.AppendLine("== Materials in project ==");
                var matGuids = AssetDatabase.FindAssets("t:Material");
                sb.AppendLine("count: " + matGuids.Length);
                foreach (var g in matGuids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    string shader = m != null && m.shader != null ? m.shader.name : "<null>";
                    sb.AppendLine(string.Format("  {0,-70} shader={1}", p, shader));
                }

                File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
                Debug.Log("[Diag] wrote " + LogPath);
            }
            catch (System.Exception e)
            {
                File.WriteAllText(LogPath, "ERROR: " + e, new UTF8Encoding(false));
            }
        }

        static string GetPath(Transform t)
        {
            var sb = new StringBuilder(t.name);
            var p = t.parent;
            while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }
    }
}