using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Mavis;
public static class AuditNailongVoiceAnchorV232
{
 public static async Task<object> Run()
 {
  const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var observer=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x=>x.GetType().Name=="NailongIslandAnchorV232");
  string folder=(string)observer.GetType().GetField("folder").GetValue(observer);
  var boss=(NailongAI)observer.GetType().GetField("boss",flags).GetValue(observer);
  var view=(Camera)observer.GetType().GetField("view",flags).GetValue(observer);
  var target=(RenderTexture)observer.GetType().GetField("target",flags).GetValue(observer);
  var pixels=(Texture2D)observer.GetType().GetField("pixels",flags).GetValue(observer);
  var motion=boss.GetComponent<NailongAttackMotion>();var animator=boss.GetComponentInChildren<Animator>();
  var skin=boss.GetComponentInChildren<SkinnedMeshRenderer>();var head=animator.GetBoneTransform(HumanBodyBones.Head);
  var baked=new Mesh();skin.BakeMesh(baked);var vertices=baked.vertices;var weights=skin.sharedMesh.boneWeights;
  int headIndex=Array.IndexOf(skin.bones,head);Bounds headBounds=default;bool started=false;int count=0;
  for(int i=0;i<vertices.Length;i++)
  {
   var w=weights[i];float total=(w.boneIndex0==headIndex?w.weight0:0)+(w.boneIndex1==headIndex?w.weight1:0)+(w.boneIndex2==headIndex?w.weight2:0)+(w.boneIndex3==headIndex?w.weight3:0);
   if(total<.35f)continue;var p=boss.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
   if(!started){headBounds=new Bounds(p,Vector3.zero);started=true;}else headBounds.Encapsulate(p);count++;
  }
  UnityEngine.Object.DestroyImmediate(baked);
  object Vec(Vector3 v)=>new[]{v.x,v.y,v.z};
  var result=new {head=Vec(head.position),headRoot=Vec(boss.transform.InverseTransformPoint(head.position)),mouth=Vec(motion.MouthPosition),mouthRoot=Vec(boss.transform.InverseTransformPoint(motion.MouthPosition)),root=Vec(boss.transform.position),forward=Vec(boss.transform.forward),scale=Vec(boss.transform.lossyScale),headVertices=count,unvalidatedHeadBoundsMin=Vec(headBounds.min),unvalidatedHeadBoundsMax=Vec(headBounds.max)};
  File.WriteAllText(folder+"/anchor-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  foreach(var wave in UnityEngine.Object.FindObjectsByType<NailongGroundSlam>(FindObjectsSortMode.None))foreach(var renderer in wave.GetComponentsInChildren<MeshRenderer>())renderer.enabled=false;
  Vector3 aim=head.position+Vector3.up*(boss.transform.lossyScale.y*.22f);
  view.transform.position=aim+boss.transform.forward*7;view.transform.LookAt(aim);view.fieldOfView=32;
  var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());marker.transform.localScale=Vector3.one*.15f;
  var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",Color.red);marker.GetComponent<MeshRenderer>().sharedMaterial=material;
  for(int c=-1;c<3;c++)
  {
   marker.SetActive(c>=0);float offset=c==0?0:c==1?.03f:.06f;
   marker.transform.position=motion.MouthPosition+Vector3.up*(boss.transform.lossyScale.y*offset)+(c>0?boss.transform.forward*(boss.transform.lossyScale.z*.12f):Vector3.zero);
   int renders=0;Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> rendered=(ctx,cam)=>{if(cam==view)renders++;};
   UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=rendered;
   try{var clock=System.Diagnostics.Stopwatch.StartNew();while(renders<6&&clock.ElapsedMilliseconds<10000)await Task.Delay(20);if(renders<6)throw new Exception("Camera did not render enough normal frames");}
   finally{UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=rendered;}
   var previous=RenderTexture.active;try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/face-rendered-candidate-"+c+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
  }
  UnityEngine.Object.DestroyImmediate(marker);UnityEngine.Object.DestroyImmediate(material);return result;
 }
}
