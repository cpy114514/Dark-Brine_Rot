using UnityEditor;
using UnityEngine;
using System;
using System.IO;

namespace Diag
{
    /// <summary>
    /// Tell Unity where the local Blender lives, then reimport any .blend
    /// whose importer has no mesh (sign that Blender wasn't on PATH the
    /// first time Unity ran).  Idempotent — safe to run every editor start.
    /// </summary>
    [InitializeOnLoad]
    public static class BlenderReimporter
    {
        const string LogPath = "Temp/blender_reimport.txt";

        static BlenderReimporter()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            try
            {
                var log = new System.Text.StringBuilder();

                // 1) Find a working blender.exe and store the parent dir in
                //    EditorUserSettings so ModelImporter picks it up.
                string blenderDir = FindBlenderDir(log);
                if (string.IsNullOrEmpty(blenderDir))
                {
                    File.WriteAllText(LogPath, "no blender.exe found:\n" + log, new System.Text.UTF8Encoding(false));
                    return;
                }

                EditorUserSettings.SetConfigValue("blenderPathExe", blenderDir + "\\blender.exe");
                log.AppendLine("EditorUserSettings.blenderPathExe = " + blenderDir + "\\blender.exe");

                // Also export the directory so a future Unity process that
                // hasn't yet loaded EditorUserSettings can still find Blender.
                Environment.SetEnvironmentVariable("PATH", blenderDir + ";" + Environment.GetEnvironmentVariable("PATH"));
                log.AppendLine("PATH updated for this session");

                // 2) Reimport every .blend whose model is missing or empty.
                var blendGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/3d model" });
                int reimported = 0;
                foreach (var g in blendGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (!path.EndsWith(".blend", System.StringComparison.OrdinalIgnoreCase)) continue;

                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (imp == null) continue;

                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    bool needsReimport = go == null ||
                                         go.GetComponentInChildren<MeshFilter>() == null ||
                                         go.GetComponentInChildren<MeshFilter>().sharedMesh == null;

                    if (needsReimport)
                    {
                        log.AppendLine("  reimport: " + path);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        reimported++;
                    }
                }

                log.AppendLine("reimported " + reimported + " .blend asset(s)");
                File.WriteAllText(LogPath, log.ToString(), new System.Text.UTF8Encoding(false));
                Debug.Log("[BlenderReimporter] wrote " + LogPath);
            }
            catch (System.Exception e)
            {
                File.WriteAllText(LogPath, "ERROR: " + e, new System.Text.UTF8Encoding(false));
            }
        }

        static string FindBlenderDir(System.Text.StringBuilder log)
        {
            // 1) Env override
            var env = Environment.GetEnvironmentVariable("BLENDER_EXE");
            if (!string.IsNullOrEmpty(env) && File.Exists(env))
            {
                log.AppendLine("using BLENDER_EXE env: " + env);
                return Path.GetDirectoryName(env);
            }

            // 2) Repo-local copy
            string local = Path.Combine(Application.dataPath, "..", "Tools", "blender", "blender.exe");
            local = Path.GetFullPath(local);
            if (File.Exists(local))
            {
                log.AppendLine("using local: " + local);
                return Path.GetDirectoryName(local);
            }

            // 3) PATH lookup (System.Diagnostics.Process resolution)
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "blender",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                var p = System.Diagnostics.Process.Start(psi);
                p.WaitForExit(5000);
                if (p.ExitCode == 0)
                {
                    string exe = null;
                    try { exe = p.MainModule.FileName; } catch { }
                    if (!string.IsNullOrEmpty(exe))
                    {
                        log.AppendLine("using PATH: " + exe);
                        return Path.GetDirectoryName(exe);
                    }
                }
            }
            catch (System.Exception e) { log.AppendLine("PATH lookup failed: " + e.Message); }

            // 4) Microsoft Store install (may be locked to TrustedInstaller)
            string[] candidates =
            {
                @"C:\Program Files\WindowsApps\BlenderFoundation.Blender_5.2.1.0_x64__ppwjx1n5r4v9t\Blender\blender.exe",
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    log.AppendLine("found candidate: " + c);
                    return Path.GetDirectoryName(c);
                }
            }
            return null;
        }
    }
}
