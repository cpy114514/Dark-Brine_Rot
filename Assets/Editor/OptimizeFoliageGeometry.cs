using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class OptimizeFoliageGeometry
    {
        [MenuItem("Mavis/Foliage/Optimize Scene Geometry")]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity")
            {
                Debug.LogWarning("[Mavis] Foliage optimization runs only in MainScene.");
                return;
            }

            int trees = 0, removedDuplicates = 0, repairedLodSlots = 0, grassRenderers = 0, groundSlabs = 0;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            foreach (Transform root in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (IsTree(root.name))
                {
                    LODGroup group = root.GetComponent<LODGroup>();
                    if (group == null) continue;
                    LOD[] lods = group.GetLODs();
                    if (lods.Length < 2) continue;
                    var lodRenderers = new HashSet<Renderer>();
                    foreach (LOD level in lods)
                        foreach (Renderer renderer in level.renderers)
                            if (renderer != null) lodRenderers.Add(renderer);
                    bool hasMainLod0 = System.Array.Exists(lods[0].renderers, renderer => renderer != null &&
                        (renderer.name == "island_tree_01_LOD0" || renderer.name == "tree_small_02_LOD0"));

                    foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (!renderer.name.Contains("geometry_nodes")) continue;
                        if (hasMainLod0)
                        {
                            Renderer[] levelRenderers = lods[0].renderers;
                            for (int i = 0; i < levelRenderers.Length; i++)
                                if (levelRenderers[i] == renderer) levelRenderers[i] = null;
                            lods[0].renderers = levelRenderers;
                            if (renderer.enabled)
                            {
                                Undo.RecordObject(renderer, "Disable duplicate tree renderer");
                                renderer.enabled = false;
                                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                                removedDuplicates++;
                            }
                            continue;
                        }
                        if (lodRenderers.Contains(renderer)) continue;
                        int missing = System.Array.FindIndex(lods[0].renderers, r => r == null);
                        if (missing >= 0)
                        {
                            Renderer[] levelRenderers = lods[0].renderers;
                            levelRenderers[missing] = renderer;
                            lods[0].renderers = levelRenderers;
                            lodRenderers.Add(renderer);
                            repairedLodSlots++;
                        }
                        else
                        {
                            Undo.RecordObject(renderer, "Disable duplicate tree renderer");
                            renderer.enabled = false;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                            removedDuplicates++;
                        }
                    }

                    lods[0].screenRelativeTransitionHeight = 0.42f;
                    lods[1].screenRelativeTransitionHeight = 0.025f;
                    Undo.RecordObject(group, "Tune tree LOD distances");
                    group.SetLODs(lods);
                    group.RecalculateBounds();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                    trees++;
                }
                else if (root.name.StartsWith("rostlinka_07c_ske"))
                {
                    foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Undo.RecordObject(renderer, "Reduce grass lighting cost");
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                        if (renderer.name == "ground_close_04" || renderer.name == "rostlinka_07_ground")
                        {
                            if (renderer.enabled) groundSlabs++;
                            renderer.enabled = false;
                        }
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        grassRenderers++;
                    }
                }
            }

            foreach (TreeGeometryLodOptimizer oldOptimizer in Object.FindObjectsByType<TreeGeometryLodOptimizer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(oldOptimizer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Mavis] Optimized foliage: trees={trees}, duplicate renderers disabled={removedDuplicates}, LOD slots restored={repairedLodSlots}, grass renderers without shadows={grassRenderers}, ground slabs disabled={groundSlabs}");
        }

        static bool IsTree(string name) => name.StartsWith("island_tree_01_4k") ||
            name.StartsWith("tree_small_02_4k") || name.StartsWith("dead_tree_trunk_02_4k");
    }
}
