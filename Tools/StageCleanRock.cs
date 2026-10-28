using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class StageCleanRock {
 public static object Run(){const string p="Assets/Game/Prefabs/Environment/HunyuanCandidates/rock-clean.fbx";if(!File.Exists(p))File.Copy("ArtSource/Environment/Hunyuan-20261027-clean/rock-clean.fbx",p);AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var i=(ModelImporter)AssetImporter.GetAtPath(p);i.isReadable=true;i.importAnimation=false;i.SaveAndReimport();var m=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().First();return new{m.name,triangles=m.triangles.Length/3,vertices=m.vertexCount};}
}
