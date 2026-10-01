using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ConfigureMenuTreeDecoration
{
    const string NormalPath = "Assets/MainScene/Tree_without_color.png";
    const string HoverPath = "Assets/MainScene/Tree_with_color.png";
    const string ScenePath = "Assets/Scenes/Main Menu/MainMenu.unity";

    static Sprite Import(string path, bool readable)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.isReadable = readable;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before saving the menu.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var menu = scene.GetRootGameObjects().Single(g => g.name == "Main Menu");
        var normal = Import(NormalPath, true);
        var hover = Import(HoverPath, false);
        if (normal == null || hover == null || normal.rect.size != hover.rect.size)
            throw new InvalidOperationException("The tree sprite pair must have matching dimensions.");
        var go = menu.transform.Find("Tree Decoration")?.gameObject;
        if (go == null)
        {
            go = new GameObject("Tree Decoration", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(SahurDecorationHover));
            Undo.RegisterCreatedObjectUndo(go, "Add hand-drawn menu tree");
            go.transform.SetParent(menu.transform, false);
        }
        Undo.RecordObjects(go.GetComponents<Component>(), "Configure menu tree");
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(160f, 0f);
        rect.sizeDelta = new Vector2(1080f * normal.rect.width / normal.rect.height, 1080f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        // Above the menu background, below labels/buttons and Sahur.
        rect.SetSiblingIndex(1);
        var image = go.GetComponent<Image>();
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;
        var behaviour = go.GetComponent<SahurDecorationHover>();
        behaviour.alphaThreshold = 0.1f;
        behaviour.SetSprites(normal, hover);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Menu could not be saved.");
        return Verify();
    }

    public static object Verify()
    {
        Canvas.ForceUpdateCanvases();
        var tree = UnityEngine.Object.FindObjectsByType<SahurDecorationHover>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(b => b.name == "Tree Decoration" && b.gameObject.scene.path == ScenePath);
        var image = tree.GetComponent<Image>();
        var rect = (RectTransform)tree.transform;
        var data = new PointerEventData(EventSystem.current);
        tree.OnPointerExit(data);
        bool normal = image.sprite == tree.normalSprite;
        tree.OnPointerEnter(data);
        bool hover = image.sprite == tree.hoverSprite;
        tree.OnPointerExit(data);
        bool restored = image.sprite == tree.normalSprite;
        Func<float, float, Vector2> point = (u, v) => RectTransformUtility.WorldToScreenPoint(null,
            rect.TransformPoint(new Vector3(rect.rect.xMin + u * rect.rect.width,
                rect.rect.yMin + v * rect.rect.height, 0)));
        bool transparentPasses = !tree.IsRaycastLocationValid(point(0.1f, 0.2f), null);
        bool trunkResponds = tree.IsRaycastLocationValid(point(0.9f, 0.4f), null);
        bool sameAspect = Mathf.Abs(rect.rect.width / rect.rect.height -
            tree.normalSprite.rect.width / tree.normalSprite.rect.height) < 0.001f;
        bool buttonsClear = true;
        var menu = tree.transform.parent;
        var raycaster = menu.GetComponent<GraphicRaycaster>();
        // UI graphics register for raycasting during Play Mode, not edit-time verification.
        foreach (var name in Application.isPlaying ? new[] { "New Game", "Settings", "Quit" } : Array.Empty<string>())
        {
            var button = menu.Find(name).GetComponent<Button>();
            var buttonRect = (RectTransform)button.transform;
            data.position = RectTransformUtility.WorldToScreenPoint(null,
                buttonRect.TransformPoint(buttonRect.rect.center));
            var hits = new System.Collections.Generic.List<RaycastResult>();
            raycaster.Raycast(data, hits);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
                buttonsClear = false;
        }
        if (!normal || !hover || !restored || !transparentPasses || !trunkResponds || !sameAspect || !buttonsClear)
            throw new Exception("Tree hover, alpha hit testing, or aspect ratio verification failed.");
        return new { normal, hover, restored, transparentPasses, trunkResponds, sameAspect, buttonsClear,
            buttonRaycastsTested = Application.isPlaying,
            spriteSize = tree.normalSprite.rect.size.ToString(), menuSize = rect.rect.size.ToString(),
            normalAlpha = tree.normalSprite.texture.GetPixelBilinear(0.1f, 0.2f).a };
    }
}
