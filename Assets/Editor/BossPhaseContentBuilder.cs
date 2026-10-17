using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

public static class BossPhaseContentBuilder
{
    [MenuItem("Tools/Combat/Rebuild Boss Phase Assets")]
    public static void Rebuild(){ImportOnly();ExportSelected();}
    public static void ImportOnly()
    {
        const string path="Assets/ThirdParty/BossPhaseAssets/UAL2_Standard.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType=ModelImporterAnimationType.Human;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if(avatar==null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("The phase animation source must have a valid Humanoid Avatar.");
        Debug.Log("Boss phase animation library imported: "+clips.Length+" humanoid clips.");
        AssetDatabase.SaveAssets();
    }
    public static void ExportSelected()
    {
        const string source="Assets/ThirdParty/BossPhaseAssets/UAL2_Standard.fbx";
        var clips=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().ToArray();
        string[] names={"Idle_Rail_Call","Shield_Dash_RM","Zombie_Scratch"};
        string[] targets={"NailongRoar","NailongCharge","NailongRampage"};
        for(int i=0;i<names.Length;i++)
        {
            var original=clips.First(c=>c.name=="Armature|"+names[i]);
            if(!original.humanMotion)throw new InvalidOperationException("Selected clip is not humanoid: "+original.name);
            var clip=UnityEngine.Object.Instantiate(original);clip.name=targets[i];
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime=i==2;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            string path="Assets/Resources/BossPhases/"+targets[i]+".anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,path);
        }
        for(int i=0;i<2;i++)
        {
            string texturePath="Assets/Resources/BossPhases/"+(i==0?"dirt_01":"flame_01")+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=256;importer.SaveAndReimport();
            var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if(!shader)throw new InvalidOperationException("URP particle shader unavailable");
            var material=new Material(shader){name=i==0?"PhaseDust":"PhaseFire"};
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);
            material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            string path="Assets/Resources/BossPhases/"+material.name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing){EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);}else AssetDatabase.CreateAsset(material,path);
        }
        AssetDatabase.SaveAssets();
    }
}
