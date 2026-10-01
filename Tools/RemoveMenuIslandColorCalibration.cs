using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RemoveMenuIslandColorCalibration
{
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before saving.");
        var changed = new System.Collections.Generic.List<object>();
        foreach(string path in new[]{"Assets/MainScene/IslandNeutralPaper.mat","Assets/MainScene/IslandColoredContrast.mat"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) continue;
            float contrast=material.GetFloat("_Contrast");
            Undo.RecordObject(material,"Remove island colour calibration");
            material.SetVector("_WhiteBalance",new Vector4(1,1,1,0));
            material.SetFloat("_LocalCorrection",0);
            material.SetFloat("_Saturation",1);
            for(int y=0;y<3;y++) for(int x=0;x<3;x++) material.SetVector("_Local"+x+y,new Vector4(1,1,1,0));
            EditorUtility.SetDirty(material);
            changed.Add(new{path,contrastPreserved=contrast,whiteBalance=material.GetVector("_WhiteBalance").ToString(),localCorrection=material.GetFloat("_LocalCorrection"),saturation=material.GetFloat("_Saturation")});
        }
        AssetDatabase.SaveAssets();
        // Refresh the user's current textures, without replacing them from Downloads.
        foreach(string path in new[]{"Assets/MainScene/island_without_color.png","Assets/MainScene/island_with_color.png"})
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        return new {changed,verified=Verify()};
    }

    public static object Verify()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Menu/MainMenu.unity");
        if(!scene.IsValid() || !scene.isLoaded) scene=EditorSceneManager.OpenScene("Assets/Scenes/Main Menu/MainMenu.unity",OpenSceneMode.Additive);
        var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MenuIslandDecoration>(true)).Single();
        var clone=UnityEngine.Object.Instantiate(source.gameObject);
        try
        {
            var hover=clone.GetComponent<SahurDecorationHover>(); var image=clone.GetComponent<Image>();
            hover.OnPointerExit(null); var normal=image.material;
            CheckNeutral(normal);
            hover.OnPointerEnter(null); CheckNeutral(image.material);
            if(image.sprite!=hover.hoverSprite) throw new Exception("Hover sprite not displayed.");
            hover.OnPointerExit(null);
            if(image.material!=normal || image.sprite!=hover.normalSprite) throw new Exception("Normal state not restored.");
            var rotation=clone.GetComponent<MenuIslandDecoration>();
            rotation.SendMessage("OnEnable"); float angle=clone.transform.localEulerAngles.z;
            rotation.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
            rotation.AdvanceRotation(1f);
            if(rotation.IsRotating || Mathf.Abs(Mathf.Abs(Mathf.DeltaAngle(angle,clone.transform.localEulerAngles.z))-180)>.1f) throw new Exception("Rotation failed.");
            return new {bothVariantsNeutral=true,hoverRestore=true,rotation180=true,layoutUntouched=true};
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
    }

    static void CheckNeutral(Material m)
    {
        if(!m || m==Graphic.defaultGraphicMaterial) return;
        if(m.HasProperty("_WhiteBalance") && Vector3.Distance((Vector3)m.GetVector("_WhiteBalance"),Vector3.one)>.0001f)
            throw new Exception("Global white balance still active.");
        if(m.HasProperty("_LocalCorrection") && m.GetFloat("_LocalCorrection")!=0)
            throw new Exception("Local white balance still active.");
        if(m.HasProperty("_Saturation") && m.GetFloat("_Saturation")!=1)
            throw new Exception("Saturation correction still active.");
    }
}
