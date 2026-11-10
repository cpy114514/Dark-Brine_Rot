using System;using System.Linq;using UnityEngine;using UnityEditor;using Mavis.VisualStyles;
public static class BuildVanGogh2
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stopped editor required");
  const string path="Assets/Resources/VisualStyles/VanGoghPainting.asset";
  var p=AssetDatabase.LoadAssetAtPath<VanGoghProfile>(path);
  if(!p){p=ScriptableObject.CreateInstance<VanGoghProfile>();AssetDatabase.CreateAsset(p,path);}
  AssetDatabase.SaveAssetIfDirty(p);
  var shaders=new[]{"Assets/Resources/VisualStyles/VanGoghPaint.shader","Assets/Resources/VisualStyles/VanGoghSkinAnchor.shader","Assets/Ocean/ProceduralOcean.shader","Assets/Ocean/ProceduralSky.shader"};
  return new{profile=path,shaders=shaders.Select(s=>new{path=s,messages=ShaderUtil.GetShaderMessages(AssetDatabase.LoadAssetAtPath<Shader>(s)).Select(m=>new{m.message,m.line,severity=m.severity.ToString()})})};
 }
}
