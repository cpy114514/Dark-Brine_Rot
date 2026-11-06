// PromoCapture.cs — external-drive capture hook for the promo trailer.
//
// The promo needs real frames from the running game. Rather than fight the
// interactive editor for the project lock, this script exposes a tiny
// file-based command channel: drop a JSON command in <project>/Temp/promo_cmd.json
// and the editor answers in <project>/Temp/promo_out.json.
//
// It is deliberately conservative:
//   * it never switches away from a dirty scene (that would throw away work),
//   * every camera it spawns is HideAndDontSave, so scenes are not dirtied,
//   * it renders off-screen into a RenderTexture, so the Game View is untouched.
//
// Delete this file once the trailer is cut.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[System.Serializable]
public class PromoShot
{
    public string name;
    public string scene;      // optional scene path to load first
    public float[] camera;    // camera position
    public float[] look;      // look-at target
    public float fov = 55f;
    public int width = 1920;
    public int height = 1080;
    public bool useMainCamera;
    public float exposure = -1f;
}

[System.Serializable]
public class PromoCommand
{
    public string cmd;
    public string outDir;
    public string scene;
    public PromoShot[] shots;
    public int frames = 10;
    public float interval = 1.2f;
    public float warmup = 3.0f;
    public string prefix = "play";
}

[System.Serializable]
public class PromoPlayCfg
{
    public string outDir;
    public int frames;
    public float interval;
    public float warmup;
    public string prefix;
}

[System.Serializable]
public class PromoPlayState
{
    public int index;
    public double nextAt;
}

[InitializeOnLoad]
public static class PromoCapture
{
    const string CmdPath = "Temp/promo_cmd.json";
    const string OutPath = "Temp/promo_out.json";
    const string LogPath = "Temp/promo_log.txt";
    const string PlayCfgPath = "Temp/promo_play_cfg.json";
    const string PlayStatePath = "Temp/promo_play_state.json";
    const string PlayOutPath = "Temp/promo_play_out.txt";

    static PromoCapture()
    {
        EditorApplication.update += Tick;
    }

    /// <summary>
    /// Grabs real frames while the game is running. The island is generated at
    /// runtime, so nothing is visible in edit mode; this drives the play session
    /// and samples the actual screen.
    /// </summary>
    static void PlayTick()
    {
        if (!EditorApplication.isPlaying || !File.Exists(PlayCfgPath))
        {
            return;
        }

        PromoPlayCfg cfg;
        try
        {
            cfg = JsonUtility.FromJson<PromoPlayCfg>(File.ReadAllText(PlayCfgPath));
        }
        catch (Exception)
        {
            return;
        }

        PromoPlayState state;
        if (File.Exists(PlayStatePath))
        {
            try
            {
                state = JsonUtility.FromJson<PromoPlayState>(File.ReadAllText(PlayStatePath));
            }
            catch (Exception)
            {
                state = new PromoPlayState();
            }
        }
        else
        {
            state = new PromoPlayState { index = 0, nextAt = EditorApplication.timeSinceStartup + cfg.warmup };
            File.WriteAllText(PlayStatePath, JsonUtility.ToJson(state));
            return;
        }

        if (EditorApplication.timeSinceStartup < state.nextAt)
        {
            return;
        }

        Texture2D tex = null;
        try
        {
            tex = ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (Exception)
        {
            tex = null;
        }

        if (tex != null)
        {
            try
            {
                string path = Path.Combine(cfg.outDir, cfg.prefix + "_" + state.index.ToString("00") + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                File.AppendAllText(PlayOutPath, path + "\n");
            }
            catch (Exception)
            {
            }

            UnityEngine.Object.DestroyImmediate(tex);
            state.index++;
        }

        state.nextAt = EditorApplication.timeSinceStartup + cfg.interval;
        File.WriteAllText(PlayStatePath, JsonUtility.ToJson(state));

        if (state.index >= cfg.frames)
        {
            File.Delete(PlayCfgPath);
            File.Delete(PlayStatePath);
            EditorApplication.ExitPlaymode();
        }
    }

    static void Tick()
    {
        if (!File.Exists(CmdPath))
        {
            PlayTick();
            return;
        }

        string raw;
        try
        {
            raw = File.ReadAllText(CmdPath);
        }
        catch (Exception)
        {
            return;
        }

        try
        {
            File.Delete(CmdPath);
        }
        catch (Exception)
        {
        }

        var report = new List<string>();
        try
        {
            Run(raw, report);
        }
        catch (Exception e)
        {
            report.Add("FATAL " + e);
        }

        try
        {
            File.WriteAllText(OutPath, string.Join("\n", report.ToArray()));
            File.AppendAllText(LogPath, string.Join("\n", report.ToArray()) + "\n");
        }
        catch (Exception)
        {
        }

        PlayTick();
    }

    static void Run(string raw, List<string> report)
    {
        PromoCommand cmd;
        try
        {
            cmd = JsonUtility.FromJson<PromoCommand>(raw);
        }
        catch (Exception e)
        {
            report.Add("PARSE_FAIL " + e.Message);
            return;
        }

        if (cmd == null || string.IsNullOrEmpty(cmd.cmd))
        {
            report.Add("NO_CMD");
            return;
        }

        report.Add("CMD " + cmd.cmd);

        if (cmd.cmd == "ping")
        {
            report.Add("pong");
            report.Add("scene=" + EditorSceneManager.GetActiveScene().path);
            report.Add("dirty=" + EditorSceneManager.GetActiveScene().isDirty);
            report.Add("unity=" + Application.unityVersion);
            return;
        }

        if (cmd.cmd == "inspect")
        {
            if (!string.IsNullOrEmpty(cmd.scene))
            {
                var current = EditorSceneManager.GetActiveScene();
                if (current.path != cmd.scene)
                {
                    if (current.isDirty)
                    {
                        report.Add("SKIP_DIRTY cannot open " + cmd.scene);
                        return;
                    }

                    if (!File.Exists(cmd.scene))
                    {
                        report.Add("MISSING_SCENE " + cmd.scene);
                        return;
                    }

                    var opened = EditorSceneManager.OpenScene(cmd.scene, OpenSceneMode.Single);
                    if (!opened.IsValid())
                    {
                        report.Add("OPEN_FAIL " + cmd.scene);
                        return;
                    }
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            report.Add("scene=" + scene.path + " name=" + scene.name + " dirty=" + scene.isDirty);
            foreach (var go in scene.GetRootGameObjects())
            {
                report.Add("ROOT " + go.name + " pos=" + go.transform.position + " active=" + go.activeSelf);
            }

            var main = Camera.main;
            if (main != null)
            {
                report.Add("MAINCAM pos=" + main.transform.position + " rot=" + main.transform.eulerAngles + " fov=" + main.fieldOfView);
            }
            else
            {
                report.Add("MAINCAM none");
            }

            var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in all)
            {
                string n = t.name;
                bool interesting = n.Contains("Sahur") || n.Contains("Nailong") || n.Contains("Cappuccino")
                    || n.Contains("Bombardino") || n.Contains("Camera") || n.Contains("Player")
                    || n.Contains("Shark") || n.Contains("Ronin") || n.Contains("Terrain");
                if (interesting)
                {
                    report.Add("ACTOR " + n + " pos=" + t.position + " active=" + t.gameObject.activeInHierarchy);
                }
            }

            report.Add("total_transforms=" + all.Length);
            return;
        }

        if (cmd.cmd == "play_capture")
        {
            var cfg = new PromoPlayCfg
            {
                outDir = cmd.outDir,
                frames = cmd.frames,
                interval = cmd.interval,
                warmup = cmd.warmup,
                prefix = string.IsNullOrEmpty(cmd.prefix) ? "play" : cmd.prefix
            };

            Directory.CreateDirectory(cfg.outDir);
            File.WriteAllText(PlayCfgPath, JsonUtility.ToJson(cfg));
            if (File.Exists(PlayStatePath))
            {
                File.Delete(PlayStatePath);
            }

            report.Add("ENTERING_PLAY frames=" + cfg.frames + " interval=" + cfg.interval + " warmup=" + cfg.warmup);
            EditorApplication.EnterPlaymode();
            return;
        }

        if (cmd.cmd == "shots")
        {
            if (cmd.shots == null)
            {
                report.Add("NO_SHOTS");
                return;
            }

            if (!string.IsNullOrEmpty(cmd.outDir))
            {
                Directory.CreateDirectory(cmd.outDir);
            }

            foreach (var shot in cmd.shots)
            {
                report.Add(RenderShot(shot, cmd.outDir));
            }
            return;
        }

        report.Add("UNKNOWN_CMD " + cmd.cmd);
    }

    static string RenderShot(PromoShot shot, string outDir)
    {
        string label = shot != null && !string.IsNullOrEmpty(shot.name) ? shot.name : "shot";
        if (shot == null)
        {
            return label + ": NULL_SHOT";
        }

        var active = EditorSceneManager.GetActiveScene();

        if (!string.IsNullOrEmpty(shot.scene) && active.path != shot.scene)
        {
            if (active.isDirty)
            {
                return label + ": SKIPPED_DIRTY_SCENE (" + active.path + ")";
            }

            if (!File.Exists(shot.scene))
            {
                return label + ": MISSING_SCENE " + shot.scene;
            }

            var opened = EditorSceneManager.OpenScene(shot.scene, OpenSceneMode.Single);
            if (!opened.IsValid())
            {
                return label + ": OPEN_FAIL " + shot.scene;
            }
        }

        int width = shot.width > 0 ? shot.width : 1920;
        int height = shot.height > 0 ? shot.height : 1080;

        var go = new GameObject("PromoCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
        Camera cam = go.AddComponent<Camera>();
        cam.hideFlags = HideFlags.HideAndDontSave;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
        cam.allowHDR = false;
        cam.fieldOfView = shot.fov > 0 ? shot.fov : 55f;

        if (shot.useMainCamera)
        {
            var main = Camera.main;
            if (main == null)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return label + ": NO_MAIN_CAMERA";
            }

            cam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            cam.fieldOfView = main.fieldOfView;
        }
        else
        {
            if (shot.camera == null || shot.camera.Length < 3 || shot.look == null || shot.look.Length < 3)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return label + ": BAD_TRANSFORM";
            }

            cam.transform.position = new Vector3(shot.camera[0], shot.camera[1], shot.camera[2]);
            Vector3 lookAt = new Vector3(shot.look[0], shot.look[1], shot.look[2]);
            Vector3 dir = lookAt - cam.transform.position;
            if (dir.sqrMagnitude < 1e-6f)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return label + ": DEGENERATE_LOOK";
            }

            cam.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2
        };
        rt.Create();
        cam.targetTexture = rt;

        try
        {
            // First render primes lighting and shader state; the second is the keeper.
            cam.Render();
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;

            string fileName = string.IsNullOrEmpty(outDir)
                ? label + ".png"
                : Path.Combine(outDir, label + ".png");
            File.WriteAllBytes(fileName, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            return label + ": OK " + fileName;
        }
        catch (Exception e)
        {
            return label + ": RENDER_FAIL " + e.Message;
        }
        finally
        {
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
