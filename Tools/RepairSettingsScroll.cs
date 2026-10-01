using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class RepairSettingsScroll
{
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play Mode first.");
        const string path="Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(path);
        int count=0;
        try { count+=Fix(prefab); PrefabUtility.SaveAsPrefabAsset(prefab,path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        const string scenePath="Assets/Scenes/Main Menu/MainMenu.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath); bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
        foreach(var menu in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PauseSettingsMenu>(true))) count+=Fix(menu.gameObject);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if(opened) EditorSceneManager.CloseScene(scene,true);
        return new { configuredScrollRects=count };
    }
    static int Fix(GameObject root)
    {
        int count=0;
        foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))
        {
            if(!scroll.viewport) continue;
            var image=scroll.viewport.GetComponent<Image>();
            if(!image) image=Undo.AddComponent<Image>(scroll.viewport.gameObject);
            Undo.RecordObject(image,"Repair settings scroll hit area"); image.color=Color.clear; image.raycastTarget=true;
            Undo.RecordObject(scroll,"Repair settings scrolling"); scroll.horizontal=false; scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=40;
            if(PrefabUtility.IsPartOfPrefabInstance(scroll))
            { PrefabUtility.RecordPrefabInstancePropertyModifications(scroll); PrefabUtility.RecordPrefabInstancePropertyModifications(image); }
            count++;
        }
        return count;
    }
}
