using System;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ConfigureMenuIslandDecoration
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
        const string path = "Assets/Scenes/Main Menu/MainMenu.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var menu = scene.GetRootGameObjects().Single(g => g.name == "Main Menu");
        var normal = Import("Assets/MainScene/island_without_color.png");
        var coloured = Import("Assets/MainScene/island_with_color.png");
        var viewport = menu.transform.Find("Island Decoration Viewport")?.gameObject;
        if (!viewport)
        {
            viewport = new GameObject("Island Decoration Viewport", typeof(RectTransform), typeof(RectMask2D));
            Undo.RegisterCreatedObjectUndo(viewport, "Add menu island");
            viewport.transform.SetParent(menu.transform, false);
        }
        var viewRect = viewport.GetComponent<RectTransform>();
        Place(viewRect, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-10, 0), new Vector2(800, 430));
        viewRect.SetSiblingIndex(1);
        var artwork = viewport.transform.Find("Island Decoration")?.gameObject;
        if (!artwork)
        {
            artwork = new GameObject("Island Decoration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SahurDecorationHover), typeof(MenuIslandDecoration));
            artwork.transform.SetParent(viewport.transform, false);
        }
        var rect = artwork.GetComponent<RectTransform>();
        Place(rect, new Vector2(.5f, 0), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900, 900 * normal.rect.height / normal.rect.width));
        rect.localRotation = Quaternion.Euler(0, 0, -90);
        var image = artwork.GetComponent<Image>();
        image.preserveAspect = true; image.raycastTarget = true; image.color = Color.white;
        var hover = artwork.GetComponent<SahurDecorationHover>();
        hover.alignHoverInMaterial = false;
        hover.hoverMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainScene/IslandColoredContrast.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/MainScene/ColoredDecorationContrast.mat");
        var normalMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainScene/IslandNeutralPaper.mat");
        if (normalMaterial) hover.SetNormalMaterial(normalMaterial);
        hover.SetSprites(normal, coloured);
        var hint = menu.transform.Find("Island Rotation Hint")?.gameObject;
        if (!hint)
        {
            hint = new GameObject("Island Rotation Hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LocalizedGameText));
            hint.transform.SetParent(menu.transform, false);
        }
        Place(hint.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(.5f, .5f), new Vector2(-10, 453), new Vector2(360, 32));
        var label = hint.GetComponent<Text>();
        label.font = GameLocalization.Font; label.fontSize = 18; label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(.42f, .42f, .42f, 1); label.raycastTarget = false;
        label.text = "CLICK TO SWITCH TERRAIN";
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Menu save failed.");
        return new { normal = normal.rect.size.ToString(), coloured = coloured.rect.size.ToString(), alpha = normal.texture.GetPixel(0,0).a, saved = path };
    }
    static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = position; r.sizeDelta = size;
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
    }
    static Sprite Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var i = (TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType = TextureImporterType.Sprite; i.spriteImportMode = SpriteImportMode.Single;
        i.alphaIsTransparency = true; i.isReadable = true; i.mipmapEnabled = false;
        i.maxTextureSize = 4096; i.textureCompression = TextureImporterCompression.Uncompressed; i.filterMode = FilterMode.Bilinear;
        var s = new TextureImporterSettings(); i.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; i.SetTextureSettings(s);
        i.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
