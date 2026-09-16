// RevertFoliageShader.cs
// Reverts every foliage material from Mavis/FoliageWind back to URP/Lit,
// while preserving _BaseColor / _BaseMap / _Smoothness / _Metallic.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class RevertFoliageShader
    {
        const string LogPath = "Temp/foliage_revert.txt";
        static readonly Shader LitShader = Shader.Find("Universal Render Pipeline/Lit");

        [MenuItem("Mavis/Foliage/Revert to URP/Lit")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("start " + System.DateTime.Now.ToString("O"));
            sb.AppendLine("URP/Lit shader found: " + (LitShader != null));

            if (LitShader == null)
            {
                File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
                return;
            }

            try
            {
                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();

                var seenMats = new HashSet<Material>();
                int rendererCount = 0;
                int swapped = 0;
                int alreadyLit = 0;

                foreach (var r in roots)
                {
                    foreach (var mr in r.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (!IsFoliageName(mr.transform.name)) continue;
                        rendererCount++;
                        var mats = mr.sharedMaterials;
                        bool dirty = false;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            var m = mats[i];
                            if (m == null) continue;
                            if (m.shader != null && m.shader.name == "Universal Render Pipeline/Lit") { alreadyLit++; continue; }
                            if (!seenMats.Add(m)) continue;

                            var baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                            if (baseMap == null) baseMap = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                            var baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                            if (baseColor == Color.white && m.HasProperty("_Color")) baseColor = m.GetColor("_Color");
                            float smoothness = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0.1f;
                            float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0.0f;

                            Undo.RecordObject(m, "Revert Foliage Shader");
                            m.shader = LitShader;
                            m.SetColor("_BaseColor", baseColor);
                            m.SetFloat("_Smoothness", smoothness);
                            m.SetFloat("_Metallic", metallic);
                            if (baseMap != null) m.SetTexture("_BaseMap", baseMap);
                            EditorUtility.SetDirty(m);
                            swapped++;
                            dirty = true;
                        }
                        if (dirty) EditorUtility.SetDirty(mr);
                    }
                }

                sb.AppendLine("foliage renderers: " + rendererCount);
                sb.AppendLine("reverted to URP/Lit: " + swapped);
                sb.AppendLine("already URP/Lit: " + alreadyLit);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sb.AppendLine("saved scene");
            }
            catch (System.Exception e)
            {
                sb.AppendLine("EX: " + e);
            }

            File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Mavis] wrote " + LogPath);
        }

        static bool IsFoliageName(string n)
        {
            return n.Contains("boulder") || n.Contains("island_tree") || n.Contains("tree_small") ||
                   n.Contains("rostlinka") || n == "Forest001" || n.StartsWith("leaves") ||
                   n.Contains("leaves") || n.Contains("branches") || n.Contains("geometry_nodes") ||
                   n == "LOD0" || n == "LOD1" || n == "LOD2" || n == "LOD3";
        }
    }
}