using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Mavis;
public static class ReviewSandFootprints
{
 public static object Pair()=>new[]{Capture("before"),Capture("after")};
 public static object Before()=>Capture("before");
 public static object After()=>Capture("after");
 static object Capture(string label)
 {
  var sample=UnityEngine.Object.FindFirstObjectByType<SandFootprints>();
  Shader oldShader=label=="before"?UnityEditor.ShaderUtil.CreateShaderAsset(File.ReadAllText(".codex/sand-footprints-20261107/before-SandFootprint.shader").Replace("Mavis/SandFootprint","Hidden/ReviewBeforeSandFootprint"),true):null;
  var material=new Material(oldShader?oldShader:Shader.Find("Mavis/SandFootprint"));
  var mesh=(Mesh)typeof(SandFootprints).GetMethod("BuildOval",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
  var root=new GameObject("Temporary footprint review"){hideFlags=HideFlags.HideAndDontSave};
  var target=new RenderTexture(1000,800,24){antiAliasing=4};var pixels=new Texture2D(1000,800,TextureFormat.RGB24,false);var oldTarget=RenderTexture.active;
  var rays=Physics.RaycastAll(new Vector3(330,100,315),Vector3.down,200,~0,QueryTriggerInteraction.Ignore);
  var ground=rays.Where(h=>h.collider.GetComponentInParent<ProceduralIsland>()).OrderBy(h=>h.distance).First();
  try
  {
   for(int i=0;i<8;i++)
   {
    Vector3 pos=ground.point+new Vector3(i%2==0?-.18f:.18f,0,(i/2-1.5f)*.7f);
    var hit=Physics.RaycastAll(pos+Vector3.up*10,Vector3.down,20,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<ProceduralIsland>()).OrderBy(h=>h.distance).First();
    var stamp=new GameObject("Review stamp",typeof(MeshFilter),typeof(MeshRenderer));stamp.transform.SetParent(root.transform);
    stamp.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=stamp.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    stamp.transform.SetPositionAndRotation(hit.point+hit.normal*.008f,Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward,hit.normal),hit.normal));
    if(label!="before")typeof(SandFootprints).GetMethod("SetAlpha",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(sample,new object[]{renderer,1f});
    var mpb=new MaterialPropertyBlock();renderer.GetPropertyBlock(mpb);mpb.SetFloat("_Mirror",i%2==0?-1:1);mpb.SetFloat("_Seed",i*.137f);renderer.SetPropertyBlock(mpb);
   }
   var go=new GameObject("Review camera",typeof(Camera));go.transform.SetParent(root.transform);var cam=go.GetComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.farClipPlane=80;
   Vector3 look=ground.point;cam.transform.SetPositionAndRotation(look+new Vector3(2,3,-2.8f),Quaternion.LookRotation(new Vector3(-2,-3,2.8f)));cam.fieldOfView=48;
   target.Create();cam.targetTexture=target;cam.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1000,800),0,0);pixels.Apply();
   File.WriteAllBytes(".codex/sand-footprints-20261107/"+label+".png",pixels.EncodeToPNG());
   return new{label,ground=ground.point.ToString(),shader=material.shader.name};
  }
  finally{RenderTexture.active=oldTarget;target.Release();UnityEngine.Object.DestroyImmediate(root);if(oldShader)UnityEngine.Object.DestroyImmediate(oldShader);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
 }
}



