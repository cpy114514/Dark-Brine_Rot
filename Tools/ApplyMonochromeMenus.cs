using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class ApplyMonochromeMenus
{
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play Mode before saving UI assets.");
        int graphics=0,buttons=0;
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Game/Prefabs/UI"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { Apply(root,ref graphics,ref buttons); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        const string scenePath="Assets/Scenes/Main Menu/MainMenu.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath); bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
        foreach(var root in scene.GetRootGameObjects()) foreach(var canvas in root.GetComponentsInChildren<Canvas>(true))
            if(!canvas.GetComponentsInParent<Canvas>(true).Any(c=>c!=canvas)) Apply(canvas.gameObject,ref graphics,ref buttons);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if(opened) EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets();
        return new {graphics,buttons,handDrawnArtPreserved=true};
    }
    static void Apply(GameObject root,ref int graphics,ref int buttons)
    {
        foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if(graphic.GetComponentInParent<SahurDecorationHover>(true)) continue;
            Undo.RecordObject(graphic,"Monochrome UI theme"); graphic.color=GameUITheme.Neutral(graphic.color);
            if(graphic is TMP_Text text) text.enableVertexGradient=false;
            if(PrefabUtility.IsPartOfPrefabInstance(graphic)) PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);
            graphics++;
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            bool light=button.image && (button.image.color*button.colors.normalColor).grayscale>.5f;
            Undo.RecordObject(button,"Monochrome button states");
            if(button.image) Undo.RecordObject(button.image,"Monochrome button fill");
            GameUITheme.StyleButton(button,light);
            foreach(var caption in button.GetComponentsInChildren<TMP_Text>(true)) { Undo.RecordObject(caption,"Button contrast"); caption.color=light ? Color.black : Color.white; }
            if(PrefabUtility.IsPartOfPrefabInstance(button))
            { PrefabUtility.RecordPrefabInstancePropertyModifications(button); if(button.image) PrefabUtility.RecordPrefabInstancePropertyModifications(button.image); }
            buttons++;
        }
    }
}
