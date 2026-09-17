// WireMixamoSlash.cs
// Configures the imported Mixamo FBX as Humanoid, then attaches its
// AnimationClip to the "Attack" state of SahurGrounded.controller, and
// tunes SahurAttack's swing window to the clip length.
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    public static class WireMixamoSlash
    {
        const string LogPath = "Temp/wire_mixamo.txt";
        const string FbxPath = "Assets/Player/Animations/GreatSwordSlash.fbx";
        const string ControllerPath = "Assets/Player/SahurGrounded.controller";

        [MenuItem("Mavis/Sahur/Wire Mixamo Slash")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("start " + System.DateTime.Now.ToString("O"));
            try
            {
                // 1. Force Humanoid on the .fbx
                var imp = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
                if (imp == null) { sb.AppendLine("FBX not imported"); return; }
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.materialLocation = ModelImporterMaterialLocation.External;
                imp.SaveAndReimport();
                sb.AppendLine("fbx configured as Humanoid");

                // 2. Locate the clip
                var subs = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
                AnimationClip clip = null;
                foreach (var s in subs)
                {
                    if (s is AnimationClip c)
                    {
                        if (clip == null || c.name.IndexOf("mixamo", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            c.name.IndexOf("slash", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            clip = c;
                    }
                }
                if (clip == null) { sb.AppendLine("no clip found"); File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false)); return; }
                sb.AppendLine("clip: " + clip.name + " length=" + clip.length + "s");

                // 3. Patch controller — set motion on Attack state
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                var sm = ctrl.layers[0].stateMachine;
                AnimatorState attackState = null;
                foreach (var s in sm.states) if (s.state.name == "Attack") { attackState = s.state; break; }
                if (attackState == null) { sb.AppendLine("Attack state missing"); File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false)); return; }

                attackState.motion = clip;
                // Slightly faster so it feels snappy
                attackState.speed = 1.4f;
                EditorUtility.SetDirty(ctrl);
                AssetDatabase.SaveAssets();
                sb.AppendLine("controller patched: Attack.motion = " + clip.name);

                // 4. Tune swing window
                var sahur = GameObject.Find("Sahur Player");
                if (sahur != null)
                {
                    var sa = sahur.GetComponent<SahurAttack>();
                    if (sa != null)
                    {
                        // Mixamo's clip usually places the strike mid-way
                        sa.swingWindowStart = 0.20f;
                        sa.swingWindowEnd = 0.65f;
                        EditorUtility.SetDirty(sa);
                        sb.AppendLine("SahurAttack swing window tuned to 0.20..0.65");
                    }
                }
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            catch (System.Exception e) { sb.AppendLine("EX: " + e); }
            File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Mavis] wrote " + LogPath);
        }
    }
}