using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ConfigureMenuColoredContrast
{
    const string MaterialPath = "Assets/MainScene/ColoredDecorationContrast.mat";
    const string ScenePath = "Assets/Scenes/Main Menu/MainMenu.unity";
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode before saving.");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/MainScene/HandDrawnContrast.shader");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Contrast shader is not ready.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.SetFloat("_Contrast", 1.6f);
        material.SetFloat("_Pivot", 0.65f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var drawings = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SahurDecorationHover>(true)).ToArray();
        if (drawings.Length < 2) throw new Exception("Expected the existing menu drawings.");
        foreach (var drawing in drawings)
        {
            Undo.RecordObject(drawing, "Increase colored menu drawing contrast");
            drawing.hoverMaterial = material;
            drawing.OnPointerExit(null);
            EditorUtility.SetDirty(drawing);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return Verify();
    }
    public static object Verify()
    {
        var drawings = UnityEngine.Object.FindObjectsByType<SahurDecorationHover>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(d => d.gameObject.scene.name == "MainMenu").ToArray();
        if (drawings.Length < 2) throw new Exception("Menu drawings missing.");
        foreach (var drawing in drawings)
        {
            var image = drawing.GetComponent<Image>();
            drawing.OnPointerExit(null);
            if (image.sprite != drawing.normalSprite || image.material == drawing.hoverMaterial)
                throw new Exception("Normal drawing must remain unchanged.");
            drawing.OnPointerEnter(new PointerEventData(EventSystem.current));
            if (image.sprite != drawing.hoverSprite || image.material != drawing.hoverMaterial ||
                drawing.hoverMaterial.GetFloat("_Contrast") < 1.5f || ShaderUtil.ShaderHasError(image.material.shader))
                throw new Exception("Colored contrast was not applied.");
            drawing.OnPointerExit(null);
            if (image.sprite != drawing.normalSprite || image.material == drawing.hoverMaterial)
                throw new Exception("Exit did not restore the original appearance.");
        }
        return new { drawings = drawings.Select(d => d.name).ToArray(), contrast = 1.6f,
            normalUnchanged = true, hoverMaterialSwitch = true, exitRestores = true, shaderErrors = false };
    }
    public static void PreviewColored()
    {
        foreach (var drawing in UnityEngine.Object.FindObjectsByType<SahurDecorationHover>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(d => d.gameObject.scene.name == "MainMenu")) drawing.OnPointerEnter(null);
    }
}
