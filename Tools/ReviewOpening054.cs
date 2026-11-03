using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;

public static class ReviewOpening054
{
    const string Root = ".codex/opening054";
    static string Save => Path.Combine(Application.persistentDataPath, "dark-brine-rot-save.json");
    public static object State() => new {
        playing=Application.isPlaying,paused=EditorApplication.isPaused,
        scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{s.path,s.isDirty}).ToArray(),
        devices=InputSystem.devices.Where(d=>d is Keyboard||d is Mouse).Select(d=>new{d.deviceId,d.name,d.enabled}).ToArray(),
        savePreserved=File.Exists(Root+"/human-save.json")&&File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(Root+"/human-save.json"))
    };
    public static object Record(string label)
    {
        if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Preserve current Editor work");
        Directory.CreateDirectory(Root);
        string output = Root + "/" + label + "/review.json";
        if (Directory.Exists(Path.GetDirectoryName(output))) throw new Exception("Preserve previous take");
        if (!File.Exists(Root + "/scenes.json"))
        {
            File.WriteAllText(Root + "/scenes.json", Newtonsoft.Json.JsonConvert.SerializeObject(
                Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path).ToArray()));
            if (File.Exists(Save)) File.Copy(Save, Root + "/human-save.json");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText("Temp/opening-review-request.txt", output);
        File.WriteAllText("Temp/opening-review-helm.flag", "true");
        EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
        return new { output };
    }
    public static object Restore()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before restoring");
        string backup = Root + "/human-save.json";
        if (File.Exists(backup) && (!File.Exists(Save) || !File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(backup))))
        {
            if (File.Exists(Save)) File.Copy(Save, Root + "/test-save-" + DateTime.UtcNow.Ticks + ".json");
            File.Copy(backup, Save, true);
        }
        var paths = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Root + "/scenes.json"));
        if (!paths.SequenceEqual(Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path)))
            for (int i = 0; i < paths.Length; i++) EditorSceneManager.OpenScene(paths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
        EditorApplication.isPaused = false;
        return new { restored = paths, savePreserved = !File.Exists(backup) || File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(backup)) };
    }
}
