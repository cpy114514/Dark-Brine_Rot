using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class StageHunyuanMaterials {
 public static object Run(){
  const string dir="Assets/Game/Prefabs/Environment/HunyuanCandidates";var results=new System.Collections.Generic.List<object>();
  foreach(string kind in new[]{"rock","grass"}){
   foreach(string type in kind=="rock"?new[]{"base","normal"}:new[]{"base"}){
    string file=kind+"-"+type+".png",dest=dir+"/"+file;
    if(!File.Exists(dest))File.Copy("ArtSource/Environment/Hunyuan-20261027-r2/"+file,dest);
    AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
    var imp=(TextureImporter)AssetImporter.GetAtPath(dest);if(type=="normal")imp.textureType=TextureImporterType.NormalMap;imp.maxTextureSize=2048;imp.SaveAndReimport();
   }
   string materialPath=dir+"/Hunyuan "+(kind=="rock"?"Rock":"Grass")+".mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find(kind=="rock"?"Universal Render Pipeline/Lit":"Mavis/FoliageWind"));mat.name="Hunyuan "+(kind=="rock"?"Rock":"Grass");AssetDatabase.CreateAsset(mat,materialPath);}
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/"+kind+"-base.png");mat.SetTexture(kind=="rock"?"_BaseMap":"_MainTex",tex);
   if(kind=="rock"){mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/rock-normal.png"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Smoothness",.16f);}
   else {mat.SetTexture("_BaseMap",tex);mat.SetFloat("_WindBend",.8f);mat.SetFloat("_Cutoff",.3f);}
   EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
   results.Add(new{kind,shader=mat.shader.name,baseTexture=mat.GetTexture(kind=="rock"?"_BaseMap":"_MainTex")?.name,normal=kind=="rock"?mat.GetTexture("_BumpMap")?.name:null});
  }return results;
 }
}
