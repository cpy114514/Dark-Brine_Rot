using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class ConfigureMenuStoneDecoration
{
    const string ScenePath="Assets/Scenes/Main Menu/MainMenu.unity";
    static Sprite Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true; importer.isReadable=true;
        importer.mipmapEnabled=false; importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static object Inspect()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) throw new Exception("Main menu must be loaded.");
        var menu=scene.GetRootGameObjects().Single(g=>g.name=="Main Menu");
        Canvas.ForceUpdateCanvases();
        return menu.GetComponentsInChildren<RectTransform>().Select(r=>new {r.name,
            anchor=r.anchorMin.ToString(),position=r.anchoredPosition.ToString(),size=r.sizeDelta.ToString()}).ToArray();
    }
    public static object Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before saving the menu.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var menu=scene.GetRootGameObjects().Single(g=>g.name=="Main Menu");
        Sprite normal=Import("Assets/MainScene/Stone_without_color.png");
        Sprite hover=Import("Assets/MainScene/Stone_with_color.png");
        if(normal==null || hover==null) throw new Exception("Stone artwork import failed.");
        var go=menu.transform.Find("Stone Decoration")?.gameObject;
        if(go==null)
        {
            go=new GameObject("Stone Decoration",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(SahurDecorationHover));
            Undo.RegisterCreatedObjectUndo(go,"Add hand-drawn menu stone");
            go.transform.SetParent(menu.transform,false);
        }
        Undo.RecordObjects(go.GetComponents<Component>(),"Configure menu stone decoration");
        var rect=go.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(0,0); rect.pivot=new Vector2(0,0);
        rect.anchoredPosition=new Vector2(100,110); rect.sizeDelta=new Vector2(360,320);
        rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity; rect.SetSiblingIndex(2);
        var image=go.GetComponent<Image>();
        image.type=Image.Type.Simple; image.color=Color.white; image.preserveAspect=true; image.raycastTarget=true;
        var behaviour=go.GetComponent<SahurDecorationHover>();
        behaviour.alphaThreshold=.1f;
        behaviour.hoverMaterial=AssetDatabase.LoadAssetAtPath<Material>(behaviour.alignHoverInMaterial
            ? "Assets/MainScene/StoneAlignedContrast.mat" : "Assets/MainScene/ColoredDecorationContrast.mat");
        behaviour.SetSprites(normal,hover);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save the main menu.");
        return new { name=go.name,normal=normal.rect.size.ToString(),hover=hover.rect.size.ToString(),
            position=rect.anchoredPosition.ToString(),size=rect.sizeDelta.ToString(),alphaCorner=normal.texture.GetPixelBilinear(.01f,.01f).a };
    }
}
