using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class InstallPressureV256
{
 public static object Apply()
 {
  if(Application.isPlaying)throw new Exception("Install the material while stopped");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var shader=Shader.Find("DarkBrine/Combat Pressure");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Pressure shader must compile");
  const string path="Assets/Resources/Combat/PressureAir.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(shader){name="Transparent combat pressure"};AssetDatabase.CreateAsset(material,path);}
  else if(material.shader!=shader)throw new Exception("Preserve the existing material");
  material.SetColor("_Tint",new Color(.88f,.94f,1,1));material.SetFloat("_Fade",1);material.SetFloat("_DistortionPixels",2.6f);
  material.SetFloat("_RimOpacity",.24f);material.SetFloat("_BodyOpacity",.016f);material.SetFloat("_Ribbon",0);material.SetFloat("_Cull",2);
  EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
  var result=new {material=path,shader=shader.name,supported=shader.isSupported,shaderErrors=ShaderUtil.ShaderHasError(shader)};
  File.WriteAllText(".codex/pressure-vfx-v256/material.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
 }
}
