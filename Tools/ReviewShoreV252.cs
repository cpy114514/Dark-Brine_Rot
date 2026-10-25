using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class ReviewShoreV252
{
 const string Root=".codex/shore-foam-20261025";
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 public static object Prepare()
 {
  const string dir="Assets/Diagnostics/ShoreReviewV252";
  if(!AssetDatabase.IsValidFolder("Assets/Diagnostics"))AssetDatabase.CreateFolder("Assets","Diagnostics");
  if(!AssetDatabase.IsValidFolder(dir))AssetDatabase.CreateFolder("Assets/Diagnostics","ShoreReviewV252");
  foreach(string version in new[]{"before","after"})foreach(string kind in new[]{"shore","ocean"})
  {
   string file=version=="before"?Root+"/before-"+kind+".shader":kind=="shore"?"Assets/Island/ProceduralShoreFoam.shader":"Assets/Ocean/ProceduralOcean.shader";
   string source=File.ReadAllText(file).Replace("\r\n","\n");int end=source.IndexOf('\"',8);
   source="Shader \"Hidden/ShoreV252/"+version+"-"+kind+"\""+source.Substring(end+1);
   source=source.Replace("    Properties\n    {","    Properties\n    {\n        _ReviewTime (\"Review time\", Float) = 104");
   source=source.Replace("CBUFFER_START(UnityPerMaterial)","CBUFFER_START(UnityPerMaterial)\n                float _ReviewTime;").Replace("_Time.y","_ReviewTime");
   string dest=dir+"/"+version+"-"+kind+".shader";File.WriteAllText(dest,source);AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
   if(ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Shader>(dest)))throw new Exception(dest+" did not compile");
  }
  return new{prepared=true};
 }
 public static object ResetRenderer(){var method=typeof(RenderPipelineManager).GetMethod("CleanupRenderPipeline",BindingFlags.Static|BindingFlags.NonPublic);if(method==null)throw new Exception("No cleanup API");method.Invoke(null,null);return new{reset=true};}
 public static async System.Threading.Tasks.Task<object> RefreshEditor()
 {
  ResetRenderer();SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();await System.Threading.Tasks.Task.Delay(500);
  return new{scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray(),probes=Resources.FindObjectsOfTypeAll<ReflectionProbe>().Select(p=>new{name=p.name,scene=p.gameObject.scene.name,active=p.isActiveAndEnabled}).ToArray(),previewScenes=EditorSceneManager.previewSceneCount};
 }
 public static object ReloadCleanScene()
 {
  var scene=SceneManager.GetActiveScene();if(scene.isDirty||SceneManager.sceneCount!=1||Application.isPlaying)throw new Exception("Retain current work");
  string path=scene.path;ResetRenderer();EditorSceneManager.OpenScene(path,OpenSceneMode.Single);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();return new{reloaded=path};
 }
 public static object ReloadDomain(){UnityEditor.EditorUtility.RequestScriptReload();return new{requested=true};}
 public static async System.Threading.Tasks.Task<object> FinalCheck()
 {
  AssetDatabase.ImportAsset("Assets/Island/ProceduralShoreFoam.shader",ImportAssetOptions.ForceSynchronousImport);
  AssetDatabase.ImportAsset("Assets/Ocean/ProceduralOcean.shader",ImportAssetOptions.ForceSynchronousImport);
  SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();await System.Threading.Tasks.Task.Delay(1200);
  var result=new{productionShadersCompile=!ShaderUtil.ShaderHasError(Shader.Find("DarkBrine/Procedural Shore Foam"))&&!ShaderUtil.ShaderHasError(Shader.Find("DarkBrine/Procedural Ocean")),temporaryShadersRemoved=!AssetDatabase.IsValidFolder("Assets/Diagnostics/ShoreReviewV252"),playing=Application.isPlaying,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(Root+"/final-editor-check.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object Cleanup(){return new{deleted=AssetDatabase.DeleteAsset("Assets/Diagnostics/ShoreReviewV252")};}
 public static object Capture(string label,string version="after",bool variants=false)
 {
  if(Application.isPlaying||SceneManager.sceneCount!=1||SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity").isLoaded||SceneManager.GetSceneByPath("Assets/Scenes/First Island/Lighting.unity").isLoaded)throw new Exception("Use only the stopped single-scene review workspace; preserve any open work.");
  string folder=Root+"/"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous take");Directory.CreateDirectory(folder);
  AssetDatabase.ImportAsset("Assets/Island/ProceduralShoreFoam.shader",ImportAssetOptions.ForceSynchronousImport);
  AssetDatabase.ImportAsset("Assets/Ocean/ProceduralOcean.shader",ImportAssetOptions.ForceSynchronousImport);
  ResetRenderer();
  var originalScene=SceneManager.GetActiveScene();
  var originalWater=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
  var originalPosition=originalWater.transform.position;var originalRotation=originalWater.transform.rotation;
  var originalSky=(GameObject)typeof(OceanWorld).GetField("skyDome",Flags).GetValue(originalWater);
  var originalSkyPosition=originalSky?originalSky.transform.position:Vector3.zero;
  var originalRenderer=originalWater.GetComponent<Renderer>();bool originalEnabled=originalRenderer.enabled;originalRenderer.enabled=false;
  var environment=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Additive);
  var lighting=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
  SceneManager.SetActiveScene(lighting);
  GameObject go=null;RenderTexture rt=null;Texture2D pixels=null;OceanWorld water=null;bool fixture=false;
  var shoreMaterials=new List<Material>();Material oceanMaterial=null;
  try
  {
   var roots=environment.GetRootGameObjects();var islands=roots.SelectMany(r=>r.GetComponentsInChildren<ProceduralIsland>()).ToArray();
   water=roots.SelectMany(r=>r.GetComponentsInChildren<OceanWorld>()).FirstOrDefault();
   if(!water){var w=new GameObject("Temporary water");SceneManager.MoveGameObjectToScene(w,environment);water=w.AddComponent<OceanWorld>();fixture=true;}
   typeof(OceanWorld).GetMethod("BuildOcean",Flags).Invoke(water,null);
   foreach(var island in islands){island.Rebuild();typeof(ProceduralIsland).GetField("shoreOcean",Flags).SetValue(island,water);typeof(ProceduralIsland).GetMethod("ApplyShoreWaveSettings",Flags).Invoke(island,null);}
   go=new GameObject("Temporary shore camera");SceneManager.MoveGameObjectToScene(go,environment);var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.1f;camera.farClipPlane=3020;camera.fieldOfView=60;camera.aspect=16f/9;
   camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(environment)|EditorSceneManager.GetSceneCullingMask(lighting);
   var data=camera.GetUniversalAdditionalCameraData();data.requiresDepthTexture=true;data.requiresColorTexture=true;data.renderPostProcessing=true;
   rt=new RenderTexture(1280,720,24);rt.Create();camera.targetTexture=rt;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
   // Review shader copies change only the clock, so both versions see the
   // same real coast, native waves, terrain depth, lighting and camera.
   var oceanRenderer=water.GetComponent<Renderer>();oceanMaterial=new Material(oceanRenderer.sharedMaterial);
   if(version!="live")oceanMaterial.shader=Shader.Find("Hidden/ShoreV252/"+version+"-ocean");oceanRenderer.sharedMaterial=oceanMaterial;
   foreach(var island in islands)
   {
    var shore=(GameObject)typeof(ProceduralIsland).GetField("shoreFoamObject",Flags).GetValue(island);
    var renderer=shore.GetComponent<Renderer>();var material=new Material(renderer.sharedMaterial);
    if(version!="live")material.shader=Shader.Find("Hidden/ShoreV252/"+version+"-shore");renderer.sharedMaterial=material;shoreMaterials.Add(material);
   }
   var positions=new[]{new Vector3(480.576f,6,466.249f),new Vector3(492.338f,14,444.463f),new Vector3(444f,24,476f)};
   var rotations=new[]{Quaternion.Euler(10.574f,285,0),Quaternion.Euler(15.407f,248.807f,0),Quaternion.Euler(45,285,0)};
   foreach(float clock in (variants?new[]{104f,106f,108f,110f}:new[]{104f}))
   {
   if(version!="live"){oceanMaterial.SetFloat("_ReviewTime",clock);foreach(var material in shoreMaterials)material.SetFloat("_ReviewTime",clock);}
   for(int i=0;i<positions.Length;i++)
   {
    camera.transform.SetPositionAndRotation(positions[i],rotations[i]);water.transform.SetPositionAndRotation(new Vector3(positions[i].x,water.oceanHeight,positions[i].z),Quaternion.Euler(0,rotations[i].eulerAngles.y,0));
    var sky=(GameObject)typeof(OceanWorld).GetField("skyDome",Flags).GetValue(water);if(sky)sky.transform.position=positions[i];
    var active=RenderTexture.active;try{camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/time-"+clock+"-view-"+i+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;}
   }
   }
   var result=new{label,fixture,playing=Application.isPlaying,water=water.name,waterHeight=water.oceanHeight,originalHeight=originalWater.oceanHeight,amplitude=water.wave1.amplitude,originalAmplitude=originalWater.wave1.amplitude,islands=islands.Select(i=>new{name=i.name,position=i.transform.position.ToString(),radius=i.shorelineRadius}),shoreErrors=ShaderUtil.ShaderHasError(Shader.Find("DarkBrine/Procedural Shore Foam")),waterErrors=ShaderUtil.ShaderHasError(Shader.Find("DarkBrine/Procedural Ocean"))};
   File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally
  {
   foreach(var material in shoreMaterials)if(material)UnityEngine.Object.DestroyImmediate(material);if(oceanMaterial)UnityEngine.Object.DestroyImmediate(oceanMaterial);
   if(go){go.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(go);}if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);
   SceneManager.SetActiveScene(originalScene);EditorSceneManager.CloseScene(environment,true);EditorSceneManager.CloseScene(lighting,true);
   originalRenderer.enabled=originalEnabled;originalWater.transform.SetPositionAndRotation(originalPosition,originalRotation);if(originalSky)originalSky.transform.position=originalSkyPosition;
   ResetRenderer();
  }
 }
}
