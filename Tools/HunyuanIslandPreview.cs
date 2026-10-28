using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
public static class HunyuanIslandPreview {
 static Bounds AccurateBounds(MeshFilter f){var b=f.sharedMesh.bounds;var t=f.transform;Bounds result=new Bounds(t.TransformPoint(b.center),Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)result.Encapsulate(t.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return result;}
 public static object Run(){
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var original=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).First(r=>r.sharedMaterial&&r.sharedMaterial.name=="Meadow Rocks"&&r.name=="rock_tallB");
  var mesh=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Environment/HunyuanCandidates/rock-optimized.fbx").OfType<Mesh>().First();
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/HunyuanCandidates/Hunyuan Rock.mat");
  GameObject cameraGO=null,model=null;RenderTexture rt=null;Texture2D tex=null;bool was=original.enabled;float oldCull=mat.GetFloat("_Cull");mat.SetFloat("_Cull",0);string detail="";
  try{
   Bounds target=original.bounds;cameraGO=new GameObject("Temporary environment review camera");cameraGO.hideFlags=HideFlags.HideAndDontSave;
   var camera=cameraGO.AddComponent<Camera>();camera.enabled=false;camera.transform.position=target.center+new Vector3(1.6f,.9f,-2f)*Mathf.Max(target.extents.x,target.extents.y,target.extents.z);camera.transform.LookAt(target.center);camera.fieldOfView=45;camera.farClipPlane=1200;
   rt=new RenderTexture(960,640,24);rt.Create();tex=new Texture2D(960,640,TextureFormat.RGB24,false);var request=new RenderPipeline.StandardRequest{destination=rt};
   if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("URP render request unsupported");Directory.CreateDirectory(".codex/hunyuan-environment");
   for(int i=0;i<2;i++){
    if(i==1){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/HunyuanCandidates/rock-optimized.fbx");model=UnityEngine.Object.Instantiate(prefab);model.hideFlags=HideFlags.HideAndDontSave;foreach(var rr in model.GetComponentsInChildren<MeshRenderer>())rr.sharedMaterial=mat;
     model.transform.rotation=Quaternion.Euler(0,original.transform.eulerAngles.y,0)*model.transform.rotation;var renderer=model.GetComponentInChildren<MeshRenderer>();var filter=renderer.GetComponent<MeshFilter>();var bounds=AccurateBounds(filter);float scale=target.size.y/bounds.size.y;model.transform.localScale*=scale;bounds=AccurateBounds(filter);model.transform.position+=target.center-bounds.center;detail="scale="+scale+" before="+bounds+" after="+AccurateBounds(filter)+" active="+model.activeInHierarchy;original.enabled=false;}
    RenderPipeline.SubmitRenderRequest(camera,request);var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,640),0,0);tex.Apply();File.WriteAllBytes(".codex/hunyuan-environment/island-"+(i==0?"before":"rock-test")+".png",tex.EncodeToPNG());RenderTexture.active=old;
   }
   return new{original=original.name,scene=original.gameObject.scene.path,bounds=target.size.ToString(),position=target.center.ToString(),rendered=true,detail};
  }finally{if(mat)mat.SetFloat("_Cull",oldCull);if(original)original.enabled=was;if(model)UnityEngine.Object.DestroyImmediate(model);if(cameraGO)UnityEngine.Object.DestroyImmediate(cameraGO);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(tex)UnityEngine.Object.DestroyImmediate(tex);}
 }
}


