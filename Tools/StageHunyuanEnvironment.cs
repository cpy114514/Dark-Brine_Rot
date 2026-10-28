using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class StageHunyuanEnvironment {
 public static object Run(){
  const string dest="Assets/Game/Prefabs/Environment/HunyuanCandidates";
  if(!AssetDatabase.IsValidFolder(dest))AssetDatabase.CreateFolder("Assets/Game/Prefabs/Environment","HunyuanCandidates");
  var results=new System.Collections.Generic.List<object>();
  foreach(var kind in new[]{"rock","grass"}){
   string path=dest+"/"+kind+"-optimized.fbx";
   if(!File.Exists(path))File.Copy("ArtSource/Environment/Hunyuan-20261027-r2/"+kind+"-optimized.fbx",path);
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();
   var objects=AssetDatabase.LoadAllAssetsAtPath(path);
   results.Add(new{path,meshes=objects.OfType<Mesh>().Select(m=>new{m.name,vertices=m.vertexCount,triangles=m.triangles.Length/3}).ToArray(),materials=objects.OfType<Material>().Select(m=>new{m.name,shader=m.shader.name,texture=m.mainTexture?m.mainTexture.name:null}).ToArray(),textures=objects.OfType<Texture2D>().Select(t=>new{t.name,t.width,t.height}).ToArray()});
  }
  return results;
 }
}
