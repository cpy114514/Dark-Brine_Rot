using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
public static class InspectVisualStylesV1
{
 public static object Run(){
  Directory.CreateDirectory(".codex/visual-style-v1");
  var result=new{unity=Application.unityVersion,pipeline=GraphicsSettings.currentRenderPipeline?.name,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
   scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).path,SceneManager.GetSceneAt(i).isDirty}).ToArray(),
   renderers=new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"}.Select(p=>new{path=p,features=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(p).rendererFeatures.Select(f=>new{f.name,type=f.GetType().Name,f.isActive}).ToArray()}).ToArray(),
   volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Select(v=>new{v.name,v.isGlobal,v.priority,profile=AssetDatabase.GetAssetPath(v.sharedProfile)}).ToArray(),
   lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>new{l.name,type=l.type.ToString(),l.intensity,shadows=l.shadows.ToString()}).ToArray(),
   materials=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m).GroupBy(m=>m.shader?m.shader.name:"missing").Select(g=>new{shader=g.Key,count=g.Count()}).ToArray(),
   fog=RenderSettings.fog,sky=RenderSettings.skybox?RenderSettings.skybox.shader.name:"none",ocean=UnityEngine.Object.FindObjectsByType<OceanWorld>(FindObjectsSortMode.None).Length,
   shaderMessages=ShaderUtil.GetShaderMessages(Shader.Find("Hidden/DarkBrine/DarkFairytale")).Select(m=>new{m.message,severity=m.severity.ToString(),m.line}).ToArray()};
  File.WriteAllText(".codex/visual-style-v1/inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{inspected=true};
 }
}
