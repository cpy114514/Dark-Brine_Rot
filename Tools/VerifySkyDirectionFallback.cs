using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
public static class VerifySkyDirectionFallback
{
 public static object ImportShader(){AssetDatabase.ImportAsset("Assets/Ocean/ProceduralSky.shader",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);return "Sky shader refreshed without restarting Play";}
 public static object Run()
 {
  var main=Camera.main;var sky=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="DarkBrine/Procedural Sky").sharedMaterial;
  var sun=sky.GetVector("_SunDirection");var moon=sky.GetVector("_MoonDirection");var go=new GameObject("Owned live sky regression camera",typeof(Camera));go.hideFlags=HideFlags.HideAndDontSave;var c=go.GetComponent<Camera>();c.CopyFrom(main);c.enabled=false;c.transform.SetPositionAndRotation(main.transform.position,main.transform.rotation);c.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
  var rt=RenderTexture.GetTemporary(640,360,24,RenderTextureFormat.ARGBFloat);var old=RenderTexture.active;c.targetTexture=rt;var results=new List<object>();bool passed=true;
  try
  {
   foreach(var entry in new[]{(name:"valid",direction:sun),(name:"zero",direction:Vector4.zero),(name:"tiny",direction:new Vector4(1e-12f,0,0,0))})
   {
    sky.SetVector("_SunDirection",entry.direction);sky.SetVector("_MoonDirection",Vector4.zero);c.Render();RenderTexture.active=rt;
    var image=new Texture2D(640,360,TextureFormat.RGBAFloat,false,true);image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();var pixels=image.GetPixels();int bad=pixels.Count(p=>float.IsNaN(p.r)||float.IsInfinity(p.r)||float.IsNaN(p.g)||float.IsInfinity(p.g)||float.IsNaN(p.b)||float.IsInfinity(p.b));int blue=pixels.Skip(640*220).Count(p=>p.b>p.r+.025f);int white=pixels.Skip(640*220).Count(p=>p.r>.98f&&p.g>.98f&&p.b>.98f);
    var png=new Texture2D(640,360,TextureFormat.RGB24,false);png.SetPixels(pixels);png.Apply();File.WriteAllBytes(".codex/sun-sky-fix/direction-"+entry.name+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(image);
    bool ok=bad==0&&blue>1000;passed&=ok;results.Add(new{entry.name,passed=ok,nonFinitePixels=bad,blueSkyPixels=blue,clippedWhitePixels=white});
   }
   var errors=ShaderUtil.GetShaderMessages(sky.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();passed&=errors.Length==0;var result=new{passed,results,shaderErrors=errors,scope="Current running gameplay scene and camera pose; live materials restored; no restart or save changes"};File.WriteAllText(".codex/sun-sky-fix/direction-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{sky.SetVector("_SunDirection",sun);sky.SetVector("_MoonDirection",moon);RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
}
