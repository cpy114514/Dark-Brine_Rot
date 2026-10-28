using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
public static class PreviewHunyuanEnvironment {
 public static async Task<object> Run(){
  if(!Application.isPlaying)throw new Exception("Requires current live editor; no new scene or player");
  var reports=new System.Collections.Generic.List<object>();Directory.CreateDirectory(".codex/hunyuan-environment");
  foreach(var kind in new[]{"card"}){
   GameObject model=null,camObject=null,lightObject=null;RenderTexture rt=null;Texture2D image=null;bool done=false;Action<ScriptableRenderContext,Camera> cb=null;
   try{
    string root="Assets/Game/Prefabs/Environment/HunyuanCandidates/";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Hunyuan Grass Card.prefab");var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"Hunyuan Grass Card.mat");
    model=UnityEngine.Object.Instantiate(prefab);model.hideFlags=HideFlags.HideAndDontSave;model.transform.position=new Vector3(10000,1000,10000);
    var renders=model.GetComponentsInChildren<MeshRenderer>();foreach(var r in renders)r.sharedMaterial=mat;Bounds b=renders[0].bounds;foreach(var r in renders.Skip(1))b.Encapsulate(r.bounds);
    lightObject=new GameObject("Temporary Hunyuan preview light");lightObject.hideFlags=HideFlags.HideAndDontSave;lightObject.transform.position=b.center+new Vector3(1,2,-2);var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=12;light.intensity=2;
    camObject=new GameObject("Temporary Hunyuan preview camera");camObject.hideFlags=HideFlags.HideAndDontSave;var camera=camObject.AddComponent<Camera>();camera.transform.position=b.center+new Vector3(1.8f,1.1f,-2.5f)*Mathf.Max(b.size.x,b.size.y,b.size.z);camera.transform.LookAt(b.center);camera.orthographic=true;camera.orthographicSize=Mathf.Max(b.size.x,b.size.y,b.size.z)*.85f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.2f);
    rt=new RenderTexture(640,640,24);rt.Create();image=new Texture2D(640,640,TextureFormat.RGB24,false);camera.targetTexture=rt;
    cb=(context,rendered)=>{if(done||rendered!=camera)return;var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,640,640),0,0,false);image.Apply();File.WriteAllBytes(".codex/hunyuan-environment/"+kind+"-unity.png",image.EncodeToPNG());RenderTexture.active=old;done=true;};
    RenderPipelineManager.endCameraRendering+=cb;var deadline=DateTime.UtcNow.AddSeconds(15);while(!done&&DateTime.UtcNow<deadline)await Task.Delay(50);if(!done)throw new Exception("Preview camera did not render");
    reports.Add(new{kind,rendererCount=renders.Length,shader=mat.shader.name,output=".codex/hunyuan-environment/"+kind+"-unity.png"});
   }finally{if(cb!=null)RenderPipelineManager.endCameraRendering-=cb;if(camObject)UnityEngine.Object.DestroyImmediate(camObject);if(lightObject)UnityEngine.Object.DestroyImmediate(lightObject);if(model)UnityEngine.Object.DestroyImmediate(model);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image)UnityEngine.Object.DestroyImmediate(image);}
  }
  return reports;
 }
}

