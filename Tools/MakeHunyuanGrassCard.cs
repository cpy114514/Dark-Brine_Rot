using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class MakeHunyuanGrassCard {
 public static object Run(){
  const string dir="Assets/Game/Prefabs/Environment/HunyuanCandidates";string texPath=dir+"/grass-card.png";
  if(!File.Exists(texPath))File.Copy("ArtSource/Environment/Hunyuan-20261027-r2/grass-card.png",texPath);
  AssetDatabase.ImportAsset(texPath,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(texPath);ti.alphaIsTransparency=true;ti.maxTextureSize=512;ti.SaveAndReimport();
  string meshPath=dir+"/Hunyuan Grass Card.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
  if(!mesh){mesh=new Mesh{name="Hunyuan Grass Card"};mesh.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(-.5f,1,0),new Vector3(.5f,1,0),new Vector3(0,0,-.5f),new Vector3(0,0,.5f),new Vector3(0,1,-.5f),new Vector3(0,1,.5f)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1),new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};mesh.triangles=new[]{0,2,1,1,2,3,4,6,5,5,6,7};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
  string matPath=dir+"/Hunyuan Grass Card.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
  if(!mat){mat=new Material(Shader.Find("Mavis/FoliageWind")){name="Hunyuan Grass Card"};AssetDatabase.CreateAsset(mat,matPath);}
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);mat.SetTexture("_MainTex",tex);mat.SetTexture("_BaseMap",tex);mat.SetFloat("_Cutoff",.35f);mat.SetFloat("_WindBend",.8f);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
  string prefabPath=dir+"/Hunyuan Grass Card.prefab";if(!File.Exists(prefabPath)){var go=new GameObject("Hunyuan Grass Card");try{go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;PrefabUtility.SaveAsPrefabAsset(go,prefabPath);}finally{UnityEngine.Object.DestroyImmediate(go);}}
  return new{prefab=prefabPath,triangles=mesh.triangles.Length/3,texture=tex.width+"x"+tex.height,shader=mat.shader.name};
 }
}
