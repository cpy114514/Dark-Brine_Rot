using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class FixSettingsButtonContrast
{
    public static object InspectLive()
    {
        return UnityEngine.Object.FindObjectsByType<PauseSettingsMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Select(m=>new{m.name,scene=m.gameObject.scene.name,buttons=m.GetComponentsInChildren<Button>(true).Select(b=>new{b.name,target=b.targetGraphic?.GetType().Name,
                background=b.colors.normalColor.ToString(),labels=b.GetComponentsInChildren<TMP_Text>(true).Select(t=>new{t.text,color=t.color.ToString(),face=t.fontSharedMaterial.HasProperty("_FaceColor")?t.fontSharedMaterial.GetColor("_FaceColor").ToString():"none"}).ToArray()}).ToArray()}).ToArray();
    }
    public static object Inspect()
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab");
        return root.GetComponentsInChildren<Button>(true).Select(b=>new {b.name,background=(b.image.color*b.colors.normalColor).ToString(),
            captions=b.GetComponentsInChildren<TMP_Text>(true).Select(t=>new {t.name,text=t.text,color=t.color.ToString()}).ToArray()}).ToArray();
    }
    static int Apply(PauseSettingsMenu menu)
    {
        int count=0;
        foreach(var button in menu.GetComponentsInChildren<Button>(true))
        {
            Undo.RecordObjects(button.GetComponents<Component>(),"Fix settings button contrast");
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)) Undo.RecordObject(label,"Fix settings text contrast");
            int tab=Array.IndexOf(menu.sectionTabs,button);
            GameUITheme.StyleButton(button,tab==0);
            if(PrefabUtility.IsPartOfPrefabInstance(button))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                if(button.image) PrefabUtility.RecordPrefabInstancePropertyModifications(button.image);
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
            count++;
        }
        return count;
    }
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before saving.");
        const string path="Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab";
        var root=PrefabUtility.LoadPrefabContents(path); int prefabButtons;
        try {prefabButtons=Apply(root.GetComponent<PauseSettingsMenu>());PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally {PrefabUtility.UnloadPrefabContents(root);}
        int sceneButtons=0;
        foreach(var menu in UnityEngine.Object.FindObjectsByType<PauseSettingsMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(!menu.gameObject.scene.IsValid() || EditorSceneManager.IsPreviewScene(menu.gameObject.scene)) continue;
            sceneButtons+=Apply(menu); EditorSceneManager.MarkSceneDirty(menu.gameObject.scene); EditorSceneManager.SaveScene(menu.gameObject.scene);
        }
        AssetDatabase.SaveAssets(); return new {prefabButtons,sceneButtons};
    }
}
