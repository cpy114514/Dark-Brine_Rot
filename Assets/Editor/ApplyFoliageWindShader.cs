// ApplyFoliageWindShader.cs
// Walks the active scene's foliage renderers and re-points each unique material
// at Mavis/FoliageWind while copying over base color/map/smoothness/metallic.
// Also drops a FoliageWindDriver into the scene if missing.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class ApplyFoliageWindShader
    {
        const string LogPath = "Temp/foliage_wind.txt";
        static readonly Shader WindShader = Shader.Find("Mavis/FoliageWind");

        [MenuItem("Mavis/Foliage/Apply Wind Shader")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("start " + System.DateTime.Now.ToString("O"));
            sb.AppendLine("wind shader found: " + (WindShader != null));

            if (WindShader == null)
            {
                File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
                Debug.LogError("[Mavis] Mavis/FoliageWind shader not found - did Unity compile the .shader file?");
                return;
            }

            try
            {
                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();

                // 收集唯一的 material -> 多次引用只处理一次
                var seenMats = new HashSet<Material>();
                int rendererCount = 0;
                int slotSwapped = 0;

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
                            if (m.shader != null && m.shader.name == "Mavis/FoliageWind") continue;
                            if (!seenMats.Add(m)) continue;
                            SwapMaterial(m);
                            slotSwapped++;
                            dirty = true;
                        }
                        if (dirty) EditorUtility.SetDirty(mr);
                    }
                }
                sb.AppendLine("foliage renderers: " + rendererCount);
                sb.AppendLine("unique materials swapped: " + slotSwapped);

                // 加 wind driver
                bool driverExists = false;
                foreach (var r in roots)
                {
                    if (r.GetComponentInChildren<FoliageWindDriver>(true) != null) { driverExists = true; break; }
                }
                if (!driverExists)
                {
                    var go = new GameObject("FoliageWindDriver");
                    var d = go.AddComponent<FoliageWindDriver>();
                    // 尝试自动绑定场景中的第一个 WindZone
                    var zones = Object.FindObjectsByType<WindZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (zones != null && zones.Length > 0) d.windZone = zones[0];
                    sb.AppendLine("created FoliageWindDriver (bound zone: " + (d.windZone != null ? d.windZone.name : "<none>") + ")");
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                else
                {
                    sb.AppendLine("FoliageWindDriver already present");
                }

                // 添加 WindZone（如果没有）
                if (Object.FindObjectsByType<WindZone>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
                {
                    var go = new GameObject("WindZone");
                    go.transform.position = Vector3.zero;
                    go.transform.rotation = Quaternion.Euler(0, 50f, 0); // 主风向
                    var z = go.AddComponent<WindZone>();
                    z.mode = WindZoneMode.Directional;
                    z.windMain = 0.8f;
                    z.windTurbulence = 0.3f;
                    z.windPulseFrequency = 0.5f;
                    z.windPulseMagnitude = 0.4f;
                    z.radius = 500f;
                    sb.AppendLine("created WindZone (heading=50deg)");
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                // 保存
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

        static void SwapMaterial(Material m)
        {
            // 保留原 URP/Lit 上的常见属性
            var oldShader = m.shader;
            var baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            if (baseMap == null) baseMap = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            var baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            if (baseColor == Color.white && m.HasProperty("_Color")) baseColor = m.GetColor("_Color");
            float smoothness = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0.1f;
            float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0.0f;
            float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.4f;

            Undo.RecordObject(m, "Swap Foliage Shader");
            m.shader = WindShader;
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Cutoff", cutoff);
            if (baseMap != null) m.SetTexture("_MainTex", baseMap);
            // 叶子和草给予较大 wind bend，树干小一些
            if (m.name.Contains("leaves") || m.name.Contains("rostlinka") || m.name.Contains("Forest001"))
            {
                m.SetFloat("_WindBend", 1.4f);
                m.SetFloat("_WindTrunkStiffness", 0.0f);
                m.SetFloat("_WindFrequency", 1.5f);
            }
            else if (m.name.Contains("branches"))
            {
                m.SetFloat("_WindBend", 0.6f);
                m.SetFloat("_WindTrunkStiffness", 0.4f);
                m.SetFloat("_WindFrequency", 0.9f);
            }
            else if (m.name.Contains("geometry_nodes"))
            {
                m.SetFloat("_WindBend", 0.15f);
                m.SetFloat("_WindTrunkStiffness", 0.7f);
                m.SetFloat("_WindFrequency", 0.6f);
            }
            else
            {
                m.SetFloat("_WindBend", 0.4f);
                m.SetFloat("_WindTrunkStiffness", 0.5f);
                m.SetFloat("_WindFrequency", 1.0f);
            }
            m.SetFloat("_WindGust", 0.6f);
            m.SetFloat("_LocalWindScale", 1.0f);

            EditorUtility.SetDirty(m);
        }
    }
}