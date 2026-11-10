using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
public static class InspectVanGogh2
{
 public static object Run()
 {
  var value=new{gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),timings=FrameTimingManager.IsFeatureEnabled(),scenes=EditorSceneManager.GetSceneManagerSetup(),pipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new{c.name,position=c.transform.position.ToString(),rotation=c.transform.eulerAngles.ToString(),c.pixelWidth,c.pixelHeight,c.farClipPlane}),renderers=new[]{"PC","Mobile"}.Select(n=>{var d=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/"+n+"_Renderer.asset");return new{n,mode=d.renderingMode.ToString(),features=d.rendererFeatures.Select(f=>f.GetType().Name)};})};
  File.WriteAllText(".codex/van-gogh-2/inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));return value;
 }
}
