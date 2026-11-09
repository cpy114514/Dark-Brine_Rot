using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class InspectSkyRuntime
{
 public static object RepairLiveSunDirection()
 {
  var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
  var field=typeof(OceanWorld).GetField("cachedSun",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  var old=field.GetValue(ocean) as Light;
  var directional=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.isActiveAndEnabled&&l.type==LightType.Directional);
  field.SetValue(ocean,directional);
  var sky=ocean.transform.Find("Procedural Sky").GetComponent<Renderer>().sharedMaterial;
  var cycle=directional.GetComponent<DayNightCycle>();sky.SetVector("_SunDirection",cycle&&cycle.SunDirection.sqrMagnitude>.001f?cycle.SunDirection:-directional.transform.forward);
  return new{previousLight=old?old.name:"null",previousType=old?old.type.ToString():"none",correctLight=directional.name,direction=sky.GetVector("_SunDirection").ToString()};
 }
 public static object Audit()
 {
  var c=Camera.main;var sky=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="DarkBrine/Procedural Sky");
  var result=new{camera=new{position=c.transform.position.ToString(),rotation=c.transform.eulerAngles.ToString(),c.farClipPlane,c.nearClipPlane,c.cullingMask,clear=c.clearFlags.ToString(),background=c.backgroundColor.ToString()},sun=Shader.GetGlobalVector("_BrineSolarDisk").ToString(),style=Shader.GetGlobalVector("_EnvironmentStyle").ToString(),sky=sky.Select(r=>new{r.name,r.enabled,position=r.transform.position.ToString(),scale=r.transform.lossyScale.ToString(),bounds=r.bounds.ToString(),layer=r.gameObject.layer,material=r.sharedMaterial.name,colors=new[]{"_HorizonColor","_ZenithColor","_CloudColor","_SunColor"}.Select(p=>new{p,value=r.sharedMaterial.GetColor(p).ToString()}),values=new[]{"_CloudStrength","_CloudCoverage","_CloudDetail","_CloudRenderDistance","_SunGlow","_Daylight","_NightFactor"}.Select(p=>new{p,value=r.sharedMaterial.GetFloat(p)}),sunDirection=r.sharedMaterial.GetVector("_SunDirection").ToString()}),volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Select(v=>new{v.name,v.weight,profile=v.sharedProfile?v.sharedProfile.name:"none",effects=v.profile.components.Select(p=>new{type=p.GetType().Name,p.active})})};
  File.WriteAllText(".codex/sun-sky-fix/runtime-sky.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
