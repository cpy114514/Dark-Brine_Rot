using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using Mavis;
public static class CaptureNailongMouthV232
{
 public static async Task<object> Run()
 {
  const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var observer=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x=>x.GetType().Name=="NailongIslandMouthV232");
  var type=observer.GetType();string folder=(string)type.GetField("folder").GetValue(observer);
  var boss=(NailongAI)type.GetField("boss",flags).GetValue(observer);var camera=(Camera)type.GetField("view",flags).GetValue(observer);
  var target=(RenderTexture)type.GetField("target",flags).GetValue(observer);var pixels=(Texture2D)type.GetField("pixels",flags).GetValue(observer);
  var phase=boss.GetComponent<NailongPhaseAnimation>();boss.GetComponentInChildren<Animator>().cullingMode=AnimatorCullingMode.AlwaysAnimate;
  phase.Play("BossCombat/Animations/v228/NailongRoar",1.7f,1,1f/1.7f);typeof(NailongPhaseAnimation).GetField("began",flags).SetValue(phase,Time.time-1);
  typeof(NailongPhaseAnimation).GetMethod("Update",flags).Invoke(phase,null);((PlayableGraph)typeof(NailongPhaseAnimation).GetField("graph",flags).GetValue(phase)).Evaluate(0);
  foreach(var wave in UnityEngine.Object.FindObjectsByType<NailongGroundSlam>(FindObjectsSortMode.None))foreach(var renderer in wave.GetComponentsInChildren<MeshRenderer>())renderer.enabled=false;
  Vector3 mouth=boss.GetComponent<NailongAttackMotion>().MouthPosition;
  Vector3 position=mouth+boss.transform.forward*6+Vector3.up*.5f;var rotation=Quaternion.LookRotation(mouth-position);
  type.GetField("cameraPosition",flags).SetValue(observer,position);type.GetField("cameraRotation",flags).SetValue(observer,rotation);type.GetField("cameraFov",flags).SetValue(observer,32f);
  camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();camera.usePhysicalProperties=false;camera.lensShift=Vector2.zero;camera.rect=new Rect(0,0,1,1);camera.aspect=1280f/720f;
  camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=32;
  var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());marker.transform.position=mouth;marker.transform.localScale=Vector3.one*.12f;
  var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",Color.red);marker.GetComponent<MeshRenderer>().sharedMaterial=mat;
  for(int stage=0;stage<3;stage++)
  {
   marker.SetActive(stage==1);
   if(stage==2){var go=new GameObject("Temporary release-frame sound ring");var wave=go.AddComponent<NailongGroundSlam>();wave.InitializeVoice(boss.GetComponent<NailongAttack>(),boss.GetComponent<NailongAttackMotion>(),null,14,1,1,2.4f,.4f,true);wave.Tick(.025f);}
   int rendered=0;Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> completed=(ctx,c)=>{if(c==camera)rendered++;};UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=completed;
   try{var timer=System.Diagnostics.Stopwatch.StartNew();while(rendered<8&&timer.ElapsedMilliseconds<10000)await Task.Delay(20);if(rendered<8)throw new Exception("Normal render timeout");}finally{UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=completed;}
   var previous=RenderTexture.active;try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/shout-mouth-"+stage+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
  }
  UnityEngine.Object.DestroyImmediate(marker);UnityEngine.Object.DestroyImmediate(mat);
  return new {mouth=new[]{mouth.x,mouth.y,mouth.z},projected=camera.WorldToScreenPoint(mouth).ToString(),folder};
 }
}
