using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectSceneGlow
{
 public static object Audit()
 {
  string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
  string Vec(Vector3 v)=>v.ToString("F2");
  var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(l=>new{path=Path(l.transform),active=l.isActiveAndEnabled,position=Vec(l.transform.position),type=l.type.ToString(),l.intensity,l.range,color=l.color.ToString(),scene=l.gameObject.scene.name}).ToArray();
  var glow=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.sharedMaterials.Any(m=>m&&((m.HasProperty("_EmissionColor")&&m.GetColor("_EmissionColor").maxColorComponent>.1f)||(m.HasProperty("_BotanicalEmission")&&m.GetColor("_BotanicalEmission").maxColorComponent>.1f)||m.name.IndexOf("glow",StringComparison.OrdinalIgnoreCase)>=0||m.name.IndexOf("halo",StringComparison.OrdinalIgnoreCase)>=0))).Select(r=>new{path=Path(r.transform),active=r.enabled&&r.gameObject.activeInHierarchy,position=Vec(r.transform.position),size=Vec(r.bounds.size),materials=r.sharedMaterials.Select(m=>m?m.name:"null").ToArray()}).ToArray();
  var particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.isPlaying).Select(p=>new{path=Path(p.transform),position=Vec(p.transform.position),p.particleCount}).ToArray();
  var result=new{lights,glow,particles,camera=Camera.main?Vec(Camera.main.transform.position):"none",selection=Selection.activeGameObject?Path(Selection.activeGameObject.transform):"none"};
  Directory.CreateDirectory(".codex/light-investigation");File.WriteAllText(".codex/light-investigation/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
