using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class FoliageAudit
    {
        [MenuItem("Mavis/Foliage/Focus Tree Preview")]
        public static void FocusTree() => Focus("island_tree_01_4k (1)");

        [MenuItem("Mavis/Foliage/Focus Grass Preview")]
        public static void FocusGrass() => Focus("rostlinka_07c_ske (5)");

        [MenuItem("Mavis/Foliage/Render Material Previews")]
        public static void RenderMaterialPreviews()
        {
            RenderPreview("island_tree_01_4k (1)", "island_tree_01_LOD0", "Temp/foliage_tree_preview.png");
            RenderPreview("rostlinka_07c_ske (5)", "rostlinka_7c_scater", "Temp/foliage_grass_preview.png");
        }

        static void RenderPreview(string rootName, string rendererName, string path)
        {
            MeshRenderer target = null;
            foreach (GameObject sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != rootName) continue;
                foreach (MeshRenderer renderer in t.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.name == rendererName) { target = renderer; break; }
                if (target != null) break;
            }
            if (target == null) { Debug.LogWarning("[Mavis] Preview renderer not found: " + rootName); return; }

            Bounds b = target.bounds;
            var cameraObject = new GameObject("Foliage Material Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.13f, 0.16f);
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = b.center + new Vector3(b.size.x * 0.7f, b.size.y * 0.15f, -b.size.z * 2.2f - 4f);
            camera.transform.LookAt(b.center);

            RenderTexture targetTexture = RenderTexture.GetTemporary(1024, 768, 24);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = targetTexture;
                camera.Render();
                RenderTexture.active = targetTexture;
                var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                Debug.Log("[Mavis] Rendered foliage preview: " + path);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(targetTexture);
                Object.DestroyImmediate(cameraObject);
            }
        }

        static void Focus(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return;
                Bounds bounds = new Bounds();
                bool hasBounds = false;
                foreach (Renderer renderer in renderers)
                {
                    if (!renderer.enabled) continue;
                    if (hasBounds) bounds.Encapsulate(renderer.bounds);
                    else { bounds = renderer.bounds; hasBounds = true; }
                }
                if (!hasBounds) return;
                Selection.activeGameObject = t.gameObject;
                view.Frame(bounds, false);
                SceneView.RepaintAll();
                Debug.Log("[Mavis] Focused foliage preview: " + name);
                return;
            }
        }

        [MenuItem("Mavis/Foliage/Write Audit Report")]
        public static void WriteReport()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Environment.unity") return;
            Physics.SyncTransforms();
            var output = new StringBuilder();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name.ToLowerInvariant();
                    if (!(n.StartsWith("island_tree_01_4k") || n.StartsWith("tree_small_02_4k") ||
                          n.StartsWith("dead_tree_trunk_02_4k") || n == "forest001" || n.StartsWith("rostlinka")))
                        continue;
                    if (t.parent != null && t.parent.name.ToLowerInvariant().StartsWith("rostlinka")) continue;
                    output.AppendLine($"ROOT {t.name} scale={t.lossyScale} position={t.position}");
                    foreach (LODGroup group in t.GetComponentsInChildren<LODGroup>(true))
                    {
                        LOD[] lods = group.GetLODs();
                        output.AppendLine($" LODGROUP {group.name} size={group.size:F2} {string.Join(";", lods.Select((l, i) => $"{i}:{l.screenRelativeTransitionHeight:F3}[{string.Join(",", l.renderers.Select(r => r ? r.name : "null"))}]"))}");
                    }
                    foreach (MeshRenderer renderer in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                        output.AppendLine($" RENDER {renderer.name} enabled={renderer.enabled} tri={TriangleCount(mesh)} bounds={renderer.bounds.size} pos={renderer.bounds.center}");
                        if (mesh != null && (renderer.name == "island_tree_01_LOD1" || renderer.name == "tree_small_02_LOD1"))
                        {
                            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                            {
                                Bounds b = mesh.GetSubMesh(submesh).bounds;
                                output.AppendLine($"  SUBMESH {submesh} local={b.center}/{b.size} worldCenter={renderer.transform.TransformPoint(b.center)}");
                            }
                        }
                        foreach (Material m in renderer.sharedMaterials)
                        {
                            if (m == null) { output.AppendLine("  MAT null"); continue; }
                            Texture baseTex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                            if (baseTex == null && m.HasProperty("_MainTex")) baseTex = m.GetTexture("_MainTex");
                            output.AppendLine($"  MAT {m.name} shader={m.shader?.name} path={AssetDatabase.GetAssetPath(m)} tex={AssetDatabase.GetAssetPath(baseTex)} queue={m.renderQueue}");
                        }
                    }
                    foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
                    {
                        string capsule = c is CapsuleCollider cap ? $" radius={cap.radius:F3} height={cap.height:F3} center={cap.center}" : "";
                        string meshInfo = c is MeshCollider meshCollider && meshCollider.sharedMesh != null
                            ? $" triangles={meshCollider.sharedMesh.GetIndexCount(0) / 3} rayHits={ProbeCollider(c)} mesh={AssetDatabase.GetAssetPath(meshCollider.sharedMesh)}"
                            : "";
                        output.AppendLine($" COLLIDER {c.GetType().Name} {c.name} enabled={c.enabled} bounds={c.bounds.size} center={c.bounds.center}{capsule}{meshInfo}");
                    }
                }
            string[] sources =
            {
                "Assets/3d model/island_tree_01_4k/island_tree_01_4k.blend",
                "Assets/3d model/tree_small_02_4k/tree_small_02_4k.blend",
                "Assets/3d model/simple-grass-chunks/rostlinka_07c_ske.FBX"
            };
            foreach (string source in sources)
            {
                output.AppendLine("SOURCE " + source);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
                if (prefab == null) continue;
                foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    output.AppendLine($" {renderer.name}: submeshes={mesh?.subMeshCount} materials=[{string.Join(",", renderer.sharedMaterials.Select(m => m ? m.name : "null"))}]");
                }
                foreach (Material material in AssetDatabase.LoadAllAssetsAtPath(source).OfType<Material>())
                    output.AppendLine(" SUBASSET " + material.name);
            }
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/foliage_audit.txt", output.ToString(), new UTF8Encoding(false));
            Debug.Log($"[Mavis] Foliage audit wrote {output.Length} characters to Temp/foliage_audit.txt");
        }

        static int ProbeCollider(Collider collider)
        {
            if (!collider.gameObject.activeInHierarchy || !collider.enabled) return 0;
            Bounds bounds = collider.bounds;
            int hits = 0;
            foreach (float fraction in new[] { 0.2f, 0.5f, 0.8f })
            {
                Vector3 center = new Vector3(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, fraction), bounds.center.z);
                float xDistance = bounds.size.x + 2f;
                float zDistance = bounds.size.z + 2f;
                if (collider.Raycast(new Ray(center + Vector3.right * (bounds.extents.x + 1f), Vector3.left), out _, xDistance)) hits++;
                if (collider.Raycast(new Ray(center + Vector3.forward * (bounds.extents.z + 1f), Vector3.back), out _, zDistance)) hits++;
            }
            return hits;
        }

        static long TriangleCount(Mesh mesh)
        {
            if (mesh == null) return 0;
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetTopology(i) == MeshTopology.Triangles)
                    count += mesh.GetIndexCount(i) / 3;
            return count;
        }
    }
}
