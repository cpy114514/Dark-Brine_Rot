using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class InstallSixWayVfx050
{
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Author assets outside Play Mode");
        AssetDatabase.Refresh();
        foreach(var name in new[]{"GroundExplosion_P","GroundExplosion_N"})
        {
            string path="Assets/ThirdParty/UnitySixWaySmoke/"+name+".tga";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;
            importer.maxTextureSize=2048;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=true;platform.maxTextureSize=2048;platform.format=TextureImporterFormat.BC7;
            importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
        }
        const string materialPath="Assets/Resources/Combat/BrineSixWaySmoke.mat";
        var shader=Shader.Find("DarkBrine/Brine Six Way Smoke");if(!shader)throw new Exception("Import six-way shader first");
        var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,materialPath);}
        mat.SetTexture("_Positive",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/UnitySixWaySmoke/GroundExplosion_P.tga"));
        mat.SetTexture("_Negative",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/UnitySixWaySmoke/GroundExplosion_N.tga"));
        var style=BrineVfxStyle.Load();style.smokeDetail=mat;EditorUtility.SetDirty(style);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        return new{shaderError=ShaderUtil.ShaderHasError(shader),messages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,m.line,m.severity}).ToArray(),materialPath};
    }
}
