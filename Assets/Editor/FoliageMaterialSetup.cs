using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class FoliageMaterialSetup
    {
        const string Folder = "Assets/Foliage/Materials";
        const string Models = "Assets/3d model";

        [MenuItem("Mavis/Foliage/Create Persistent Materials")]
        public static void CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Foliage")) AssetDatabase.CreateFolder("Assets", "Foliage");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Foliage", "Materials");

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader wind = Shader.Find("Mavis/FoliageWind");
            if (lit == null || wind == null || !wind.isSupported)
            {
                Debug.LogError("[Mavis] URP Lit or foliage wind shader is unavailable; materials were not created.");
                return;
            }

            string island = Models + "/island_tree_01_4k/textures/island_tree_01_";
            string small = Models + "/tree_small_02_4k/textures/tree_small_02_";
            string grass = Models + "/simple-grass-chunks/";
            string dead = Models + "/dead_tree_trunk_02_4k/textures/dead_tree_trunk_02_";

            Save("Island_Bark", lit, island + "diff_4k.jpg");
            Save("Island_Branches", lit, island + "branches_diff_4k.png");
            Save("Island_Leaves", wind, island + "leaves_diff_4k.png", island + "leaves_alpha_4k.png", 0.38f);
            Save("Small_Bark", lit, small + "diff_4k.jpg");
            Save("Small_Branches", lit, small + "branch_diff_4k.png");
            Save("Small_Leaves", wind, small + "leaves_diff_4k.png", small + "leaves_alpha_4k.png", 0.38f);
            Save("Dead_Bark", lit, dead + "diff_4k.jpg");
            Save("Grass_Stems", wind, grass + "rostlinka_07c_diffuse.jpg", grass + "rostlinka_07c_alfa.jpg", 0.36f);
            Save("Grass_12", wind, grass + "rostlinka12_2k_difuse.jpg", grass + "rostlinka12_2k_alfa.jpg", 0.36f);
            Save("Grass_Ground", lit, grass + "rostlinka_07_ground_albedo.jpg");
            Save("Ground_Close", lit, grass + "ground_close_04_basecolor.jpg");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mavis] Created or updated 11 persistent foliage materials in " + Folder);
        }

        [MenuItem("Mavis/Foliage/Repair Scene Material Slots")]
        public static void RepairSceneMaterials()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Environment.unity")
            {
                Debug.LogWarning("[Mavis] Material repair runs only in Environment.");
                return;
            }

            var replacements = new Dictionary<string, string>
            {
                ["island_tree_01"] = "Island_Bark",
                ["island_tree_01_branches"] = "Island_Branches",
                ["island_tree_01_leaves"] = "Island_Leaves",
                ["tree_small_02_trunk"] = "Small_Bark",
                ["tree_small_02_branches"] = "Small_Branches",
                ["tree_small_02_leaves"] = "Small_Leaves",
                ["rostlinka12_2k"] = "Grass_12",
                ["rostlinka_07c"] = "Grass_Stems",
                ["rostlinka_07_ground"] = "Grass_Ground",
                ["ground_close_04"] = "Ground_Close",
                ["dead_tree_trunk_02"] = "Dead_Bark"
            };
            var materials = new Dictionary<string, Material>();
            foreach (string name in replacements.Values.Distinct())
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + name + ".mat");
                if (material == null) { Debug.LogError("[Mavis] Missing material " + name); return; }
                materials[name] = material;
            }

            int renderers = 0, slots = 0, unresolved = 0;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            foreach (Transform root in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                string sourcePath = SourcePath(root.name);
                if (sourcePath == null) continue;
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) { Debug.LogError("[Mavis] Missing model " + sourcePath); continue; }
                var sourceRenderers = source.GetComponentsInChildren<MeshRenderer>(true)
                    .GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First());

                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (FindOwnerRoot(renderer.transform) != root) continue;
                    string sourceName = renderer.name.Split('(')[0].TrimEnd();
                    if (!sourceRenderers.TryGetValue(sourceName, out MeshRenderer sourceRenderer))
                    {
                        unresolved++;
                        Debug.LogWarning($"[Mavis] No source material layout for {root.name}/{renderer.name}");
                        continue;
                    }
                    Material[] sourceSlots = sourceRenderer.sharedMaterials;
                    Material[] assigned = new Material[sourceSlots.Length];
                    bool valid = true;
                    for (int i = 0; i < sourceSlots.Length; i++)
                    {
                        Material original = sourceSlots[i];
                        if (original == null || !replacements.TryGetValue(original.name, out string targetName))
                        {
                            valid = false;
                            unresolved++;
                            Debug.LogWarning($"[Mavis] Unknown source material on {root.name}/{renderer.name} slot {i}");
                            break;
                        }
                        assigned[i] = materials[targetName];
                    }
                    if (!valid) continue;
                    Undo.RecordObject(renderer, "Repair foliage material slots");
                    renderer.sharedMaterials = assigned;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                    renderers++;
                    slots += assigned.Length;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Mavis] Repaired foliage materials: renderers={renderers}, slots={slots}, unresolved={unresolved}");
        }

        static string SourcePath(string name)
        {
            if (name.StartsWith("island_tree_01_4k")) return Models + "/island_tree_01_4k/island_tree_01_4k.blend";
            if (name.StartsWith("tree_small_02_4k")) return Models + "/tree_small_02_4k/tree_small_02_4k.blend";
            if (name.StartsWith("rostlinka_07c_ske")) return Models + "/simple-grass-chunks/rostlinka_07c_ske.FBX";
            if (name.StartsWith("dead_tree_trunk_02_4k")) return Models + "/dead_tree_trunk_02_4k/dead_tree_trunk_02_4k.blend";
            return null;
        }

        static Transform FindOwnerRoot(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (SourcePath(current.name) != null) return current;
            return null;
        }

        static void Save(string name, Shader shader, string colorPath, string alphaPath = null, float cutoff = 0.4f)
        {
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            Texture2D alpha = alphaPath == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(alphaPath);
            if (color == null || (alphaPath != null && alpha == null))
            {
                Debug.LogError($"[Mavis] Missing foliage texture for {name}: {colorPath}, {alphaPath}");
                return;
            }

            string path = Folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            Undo.RecordObject(material, "Set up foliage material");
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_MainTex", color);
            material.SetFloat("_Smoothness", alpha == null ? 0.14f : 0.04f);
            material.SetFloat("_Metallic", 0f);
            if (alpha != null)
            {
                material.SetTexture("_AlphaMap", alpha);
                material.SetFloat("_UseAlphaMap", 1f);
                material.SetFloat("_Cutoff", cutoff);
                material.SetFloat("_Cull", 0f);
                material.SetFloat("_WindBend", name.StartsWith("Grass") ? 0.35f : 0.22f);
                material.SetFloat("_WindFrequency", name.StartsWith("Grass") ? 1.5f : 1f);
                material.SetFloat("_WindTrunkStiffness", 0.15f);
                material.SetFloat("_PlayerPushStrength", name.StartsWith("Grass") ? 0.5f : 0f);
            }
            else
            {
                material.enableInstancing = true;
            }
            EditorUtility.SetDirty(material);
        }
    }
}
