using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AlignMenuStone
{
    const string ScenePath="Assets/Scenes/Main Menu/MainMenu.unity";
    const string MaterialPath="Assets/MainScene/StoneAlignedContrast.mat";
    // Corresponding crack junctions and foreground stone corners, in bottom-left UV space.
    static readonly Vector4[] Landmarks={
        new Vector4(.42131696f,.88453314f,.04484149f,-.03728968f),
        new Vector4(.62165179f,.83749109f,-.08967274f,.05051667f),
        new Vector4(.67745536f,.40627227f,.32655757f,-.13543279f),
        new Vector4(.27120536f,.34853885f,-.24576872f,.12633019f),
        new Vector4(.66071429f,.18032787f,-.21451899f,.08047832f),
        new Vector4(.13448661f,.15181753f,.17856139f,-.08460271f)
    };
    static SahurDecorationHover Stone()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid()||!scene.isLoaded) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SahurDecorationHover>(true))
            .Single(d=>d.name=="Stone Decoration");
    }
    public static object Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before saving.");
        AssetDatabase.ImportAsset("Assets/MainScene/HandDrawnContrast.shader",ImportAssetOptions.ForceSynchronousImport);
        var stone=Stone();
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(!material)
        {
            material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/MainScene/ColoredDecorationContrast.mat"));
            material.name="Stone Aligned Contrast"; AssetDatabase.CreateAsset(material,MaterialPath);
        }
        Undo.RecordObject(material,"Align colored stone drawing");
        material.SetFloat("_AlignmentEnabled",1);
        material.SetTexture("_AlignedTex",stone.hoverSprite.texture);
        material.SetVector("_WarpAffineX",new Vector4(.0274195f,.99678539f,-.02126816f,0));
        material.SetVector("_WarpAffineY",new Vector4(.10247648f,.07654546f,.86115996f,0));
        for(int i=0;i<Landmarks.Length;i++) material.SetVector("_Warp"+i,Landmarks[i]);
        EditorUtility.SetDirty(material);
        Undo.RecordObject(stone,"Fix stone hover alignment");
        stone.hoverMaterial=material; stone.alignHoverInMaterial=true;
        stone.OnPointerExit(null);
        EditorUtility.SetDirty(stone);
        EditorSceneManager.MarkSceneDirty(stone.gameObject.scene);
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(stone.gameObject.scene)) throw new Exception("Save failed.");
        return Verify();
    }
    public static object Verify()
    {
        var stone=Stone(); var image=stone.GetComponent<Image>();
        var position=image.rectTransform.anchoredPosition; var size=image.rectTransform.sizeDelta;
        stone.OnPointerEnter(null);
        bool aligned=stone.alignHoverInMaterial && image.sprite==stone.normalSprite && image.material==stone.hoverMaterial;
        if(!aligned) throw new Exception("Hover no longer shares the original drawing geometry.");
        if(position!=image.rectTransform.anchoredPosition||size!=image.rectTransform.sizeDelta) throw new Exception("Hover moved the stone.");
        var targets=new[]{new Vector2(728f/1695,1-163f/1500),new Vector2(1093f/1695,1-204f/1500),
            new Vector2(1266f/1695,1-776f/1500),new Vector2(417f/1695,1-828f/1500),
            new Vector2(1123f/1695,1-1029f/1500),new Vector2(277f/1695,1-1136f/1500)};
        float error=0;
        var mat=stone.hoverMaterial;
        foreach(var message in ShaderUtil.GetShaderMessages(mat.shader))
            if(message.severity.ToString()=="Error") throw new Exception(message.message);
        for(int i=0;i<Landmarks.Length;i++)
        {
            var uv=new Vector2(Landmarks[i].x,Landmarks[i].y);
            var basis=new Vector3(1,uv.x,uv.y);
            var mapped=new Vector2(Vector3.Dot(mat.GetVector("_WarpAffineX"),basis),Vector3.Dot(mat.GetVector("_WarpAffineY"),basis));
            for(int j=0;j<Landmarks.Length;j++)
            {
                var landmark=mat.GetVector("_Warp"+j);
                float r=(uv-new Vector2(landmark.x,landmark.y)).sqrMagnitude;
                mapped+=new Vector2(landmark.z,landmark.w)*r*Mathf.Log(Mathf.Max(r,.000001f));
            }
            error=Mathf.Max(error,Vector2.Distance(mapped,targets[i]));
        }
        stone.OnPointerExit(null);
        if(error>.00002f) throw new Exception("Landmark alignment failed: "+error);
        if(image.sprite!=stone.normalSprite||image.material==mat) throw new Exception("Exit did not restore uncolored drawing.");
        return new { originalImagesUnchanged=true,sharedSilhouette=true,positionUnchanged=true,landmarks=6,maxUVError=error };
    }
}
