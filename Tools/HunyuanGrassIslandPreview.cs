using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
public static class HunyuanGrassIslandPreview {
 public static object Run(){
  if(Application.isPlaying)throw new Exception("Edit mode required");var original=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).First(r=>r.gameObject.scene.name=="Environment"&&r.name=="r12_b"&&r.bounds.size.y>.3f);
  var target=original.bounds;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/HunyuanCandidates/Hunyuan Grass Card.prefab");GameObject go=null,camGO=null;RenderTexture rt=null;Texture2D tex=null;bool enabled=original.enabled;
  try{
   camGO=new GameObject("Temporary grass comparison camera");camGO.hideFlags=HideFlags.HideAndDontSave;var camera=camGO.AddComponent<Camera>();camera.enabled=false;camera.transform.position=target.center+new Vector3(.6f,.45f,-.7f);camera.transform.LookAt(target.center);camera.fieldOfView=38;camera.farClipPlane=600;
   rt=new RenderTexture(960,640,24);rt.Create();tex=new Texture2D(960,640,TextureFormat.RGB24,false);var request=new RenderPipeline.StandardRequest{destination=rt};if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("No URP render request");
   for(int i=0;i<2;i++){
    if(i==1){go=UnityEngine.Object.Instantiate(prefab);go.hideFlags=HideFlags.HideAndDontSave;go.transform.position=new Vector3(target.center.x,target.min.y,target.center.z);go.transform.rotation=Quaternion.Euler(0,original.transform.eulerAngles.y,0);go.transform.localScale=new Vector3(Mathf.Max(target.size.x,target.size.z),target.size.y,Mathf.Max(target.size.x,target.size.z));original.enabled=false;}
    RenderPipeline.SubmitRenderRequest(camera,request);var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,640),0,0);tex.Apply();File.WriteAllBytes(".codex/hunyuan-environment/grass-island-"+(i==0?"before":"card")+".png",tex.EncodeToPNG());RenderTexture.active=old;
   }
   return new{scene=original.gameObject.scene.path,position=target.center.ToString(),size=target.size.ToString(),rendered=true};
  }finally{if(original)original.enabled=enabled;if(go)UnityEngine.Object.DestroyImmediate(go);if(camGO)UnityEngine.Object.DestroyImmediate(camGO);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(tex)UnityEngine.Object.DestroyImmediate(tex);}
 }
}
