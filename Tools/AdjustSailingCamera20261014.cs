using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AdjustSailingCamera20261014
{
    const string ScenePath="Assets/Scenes/First story/Story1.unity";
    const string Folder=".codex/sailing-camera-20261014";
    public static object ApplyLive()
    {
        if(!Application.isPlaying)throw new Exception("Live sailing view required");
        var camera=UnityEngine.Object.FindFirstObjectByType<ShipFollowCamera>();
        if(camera==null || !camera.enabled || camera.target==null)throw new Exception("Not currently sailing");
        if(Mathf.Abs(camera.followDistance-110)>.01f)throw new Exception("Protect another live revision");
        const float ratio=140f/110f;
        var field=typeof(ShipFollowCamera).GetField("distance",BindingFlags.Instance|BindingFlags.NonPublic);
        float before=(float)field.GetValue(camera);
        camera.followDistance=140;
        camera.height=camera.lookHeight+(camera.height-camera.lookHeight)*ratio;
        camera.sideOffset*=ratio;
        field.SetValue(camera,Mathf.Clamp(before*ratio,camera.minimumDistance,camera.maximumDistance));
        var result=new{live=true,oldDistance=before,distance=(float)field.GetValue(camera),camera.followDistance,camera.height,camera.sideOffset,camera.lookHeight,orbitAndFocusPreserved=true};
        File.WriteAllText(Folder+"/live.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    public static object SaveSceneSetting()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop before saving scene parameters");
        var original=File.ReadAllBytes(ScenePath);
        if(!original.SequenceEqual(File.ReadAllBytes(Folder+"/Story1-before.unity")))throw new Exception("Protect concurrently changed scene");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        bool opened=!scene.IsValid() || !scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShipFollowCamera>(true)).Single();
            if(Mathf.Abs(camera.followDistance-110)>.01f)throw new Exception("Protect an unexpected camera revision");
            const float ratio=140f/110f;
            camera.followDistance=140;
            camera.height=camera.lookHeight+(camera.height-camera.lookHeight)*ratio;
            camera.sideOffset*=ratio;
            EditorUtility.SetDirty(camera);EditorSceneManager.MarkSceneDirty(scene);
            bool saved=EditorSceneManager.SaveScene(scene);
            if(!saved)throw new Exception("Native scene save failed");
            return new{saved,playing=EditorApplication.isPlaying,camera.followDistance,camera.height,camera.sideOffset,camera.lookHeight};
        }
        finally
        {
            if(active.IsValid() && active.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
    }
    public static object Persist()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop before saving native scene");
        var result=SaveSceneSetting();
        return result;
    }
}
