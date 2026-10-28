using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;

public static class ReviewOpeningPolishV262
{
    const string Root=".codex/opening-polish-v262";
    static string Save=>Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");

    public static object State()
    {
        return new{playing=Application.isPlaying,paused=EditorApplication.isPaused,
            scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{s.path,s.isDirty,s.isLoaded}).ToArray(),
            save=File.Exists(Save)?Hash(Save):null};
    }
    static string Hash(string path)
    {
        using var sha=System.Security.Cryptography.SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    }
    public static object Backup()
    {
        Directory.CreateDirectory(Root);
        var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        File.WriteAllText(Root+"/editor-scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes));
        if(File.Exists(Save)&&!File.Exists(Root+"/human-save.json"))File.Copy(Save,Root+"/human-save.json");
        return new{scenes,save=File.Exists(Save)?Hash(Save):null};
    }
    // The player kept testing between passes; their newer save is the one to keep.
    public static object Rebackup()
    {
        if(Application.isPlaying)throw new Exception("Stop first");
        Directory.CreateDirectory(Root);
        var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
        File.WriteAllText(Root+"/editor-scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes));
        if(File.Exists(Save))File.Copy(Save,Root+"/human-save.json",true);
        return new{scenes,save=File.Exists(Save)?Hash(Save):null};
    }
    public static object Open()
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve open work");
        EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",OpenSceneMode.Single);
        EditorApplication.isPlaying=true;return new{starting=true};
    }
    public static object Record(string label)
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve open work");
        string output=Root+"/"+label+"/review.json";if(File.Exists(output))throw new Exception("Preserve previous replay");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText("Temp/opening-review-request.txt",output);
        EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",OpenSceneMode.Single);
        EditorApplication.isPlaying=true;return new{recording=output};
    }
    public static object Replay(string label)
    {
        if(!Application.isPlaying)throw new Exception("Enter Play Mode first");
        string output=Root+"/"+label+"/review.json";if(File.Exists(output))throw new Exception("Preserve previous replay");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var review=new GameObject("Temporary opening polish replay").AddComponent<OpeningTailReview>();
        review.output=output;review.recordAll=true;EditorApplication.isPaused=false;return new{started=output};
    }
    public static object Progress(string label)
    {
        string p=Root+"/"+label+"/review.json.progress";
        return new{done=File.Exists(Root+"/"+label+"/review.json"),progress=File.Exists(p)?File.ReadAllText(p):null};
    }
    public static object ForceFinish()
    {
        var review=UnityEngine.Object.FindFirstObjectByType<OpeningTailReview>();
        if(!review)throw new Exception("No replay");
        typeof(OpeningTailReview).GetMethod("Finish",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(review,new object[]{null});
        return new{finished=review.output};
    }
    public static object Stop(){EditorApplication.isPlaying=false;return new{stopping=true};}
    public static object RestoreEditor()
    {
        if(Application.isPlaying)throw new Exception("Stop first");
        var scenes=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Root+"/editor-scenes.json"));
        for(int i=0;i<scenes.Length;i++)EditorSceneManager.OpenScene(scenes[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
        string backup=Root+"/human-save.json";
        bool same=!File.Exists(backup)||File.Exists(Save)&&Hash(Save)==Hash(backup);
        if(!same)File.Copy(backup,Save,true);
        return new{scenes,saveRestored=!same,save=File.Exists(Save)?Hash(Save):null};
    }
}
