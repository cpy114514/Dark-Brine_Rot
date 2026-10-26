using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class ReviewCheckpointV253
{
 public static object Capture()
 {
  var point=UnityEngine.Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None).FirstOrDefault(p=>p.transform.Find("Checkpoint Watchtower")||p.transform.Find("CheckpointWatchtower"));if(!point)throw new Exception("Checkpoint model missing");
  var tower=point.transform.Find("Checkpoint Watchtower");if(!tower)tower=point.transform.Find("CheckpointWatchtower");
  float terrainY=Physics.RaycastAll(point.transform.position+Vector3.up*200,Vector3.down,500,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<ProceduralIsland>()).Max(h=>h.point.y);
  var go=new GameObject("Temporary checkpoint review camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.1f;camera.farClipPlane=3020;camera.fieldOfView=55;camera.aspect=16f/9;
  var data=camera.GetUniversalAdditionalCameraData();data.requiresDepthTexture=true;data.requiresColorTexture=true;data.renderPostProcessing=true;
  var rt=new RenderTexture(1280,720,24);rt.Create();camera.targetTexture=rt;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try
  {
   Vector3 target=tower.position+Vector3.up*3.3f;camera.transform.position=target+new Vector3(14,8,-17);camera.transform.rotation=Quaternion.LookRotation(target-camera.transform.position);
   var active=RenderTexture.active;try{camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(".codex/comfort-v253/checkpoint.png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;}
   Vector3 toward=Vector3.ProjectOnPlane(tower.position-point.transform.position,Vector3.up).normalized;
   camera.transform.position=point.transform.position+Vector3.up*7-toward*7+Vector3.Cross(Vector3.up,toward)*3;
   camera.transform.rotation=Quaternion.LookRotation(target-camera.transform.position);
   active=RenderTexture.active;try{camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(".codex/comfort-v253/checkpoint-from-spawn.png",pixels.EncodeToPNG());}finally{RenderTexture.active=active;}
   var bounds=tower.GetComponentInChildren<Renderer>().bounds;
   var result=new{terrainAtSpawn=terrainY,checkpointHeight=point.transform.position.y,spawnGroundError=point.transform.position.y-terrainY,buildingHeight=bounds.size.y,colliderGap=tower.GetComponent<Collider>().ClosestPoint(point.transform.position+Vector3.up*2)-point.transform.position,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray()};
   File.WriteAllText(".codex/comfort-v253/checkpoint-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{terrainAtSpawn=terrainY,checkpointHeight=point.transform.position.y,spawnGroundError=point.transform.position.y-terrainY,buildingHeight=bounds.size.y,buildingWidth=bounds.size.x,minimumColliderGap=Vector3.ProjectOnPlane(tower.GetComponent<Collider>().ClosestPoint(point.transform.position+Vector3.up*2)-point.transform.position,Vector3.up).magnitude},Newtonsoft.Json.Formatting.Indented));return new{terrainY,checkpointY=point.transform.position.y,height=bounds.size.y,width=bounds.size.x};
  }
  finally{camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
}
