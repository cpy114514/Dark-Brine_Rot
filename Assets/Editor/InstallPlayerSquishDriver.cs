// InstallPlayerSquishDriver.cs
// Adds a FoliageWindDriver + PlayerSquishDriver to the scene and binds the player transform.
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class InstallPlayerSquishDriver
    {
        const string LogPath = "Temp/install_squish.txt";

        [MenuItem("Mavis/Foliage/Install Player Squish Driver")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("start " + System.DateTime.Now.ToString("O"));

            try
            {
                var scene = SceneManager.GetActiveScene();
                var roots = scene.GetRootGameObjects();

                // 找 Sahur Player
                Transform player = null;
                foreach (var r in roots)
                {
                    if (r.name == "Sahur Player") { player = r.transform; break; }
                }
                sb.AppendLine("player found: " + (player != null ? player.name : "<none>"));

                if (player == null)
                {
                    File.WriteAllText(LogPath, sb.ToString() + "\nno player", new UTF8Encoding(false));
                    return;
                }

                // 装 PlayerSquishDriver
                bool playerDriverExists = false;
                PlayerSquishDriver driver = null;
                foreach (var r in roots)
                {
                    driver = r.GetComponentInChildren<PlayerSquishDriver>(true);
                    if (driver != null) { playerDriverExists = true; break; }
                }
                if (!playerDriverExists)
                {
                    var host = new GameObject("FoliagePlayerDriver");
                    driver = host.AddComponent<PlayerSquishDriver>();
                    driver.player = player;
                    driver.radius = 2.0f;
                    sb.AppendLine("created PlayerSquishDriver (radius=2.0)");
                }
                else
                {
                    driver.player = player;
                    sb.AppendLine("rebound existing PlayerSquishDriver to " + player.name);
                }

                // 给每个 foliage root 加 trigger SphereCollider
                int triggerAdded = 0;
                foreach (var r in roots)
                {
                    var name = r.name;
                    bool isFoliage = name.Contains("island_tree") || name.Contains("tree_small") ||
                                     name.Contains("rostlinka") || name == "Sunset Island";
                    if (!isFoliage) continue;
                    if (r.GetComponent<Collider>() != null) continue;
                    var col = r.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    // 让每个 foliage 的 trigger 默认覆盖整棵树 / 草地
                    if (name.Contains("island_tree")) col.radius = 3.5f;
                    else if (name.Contains("tree_small")) col.radius = 2.0f;
                    else if (name.Contains("rostlinka")) col.radius = 4.0f;
                    else if (name == "Sunset Island") continue; // island 自己不要 trigger
                    triggerAdded++;
                }
                sb.AppendLine("trigger colliders added: " + triggerAdded);

                // 给 Player 加 Rigidbody（trigger 需要 kinematic rigidbody 接收 enter）
                var rb = player.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = player.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    sb.AppendLine("added kinematic Rigidbody to player");
                }
                else
                {
                    rb.isKinematic = true;
                    sb.AppendLine("player already had Rigidbody (set kinematic)");
                }

                // 给 trigger 加 MonoBehaviour 接收 player enter/exit → 调节 shader 强度
                foreach (var r in roots)
                {
                    var col = r.GetComponent<SphereCollider>();
                    if (col == null || !col.isTrigger) continue;
                    if (col.GetComponent<FoliageSquishTrigger>() != null) continue;
                    var t = col.gameObject.AddComponent<FoliageSquishTrigger>();
                    t.squishRadius = col.radius;
                }
                sb.AppendLine("FoliageSquishTrigger attached");

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
    }
}