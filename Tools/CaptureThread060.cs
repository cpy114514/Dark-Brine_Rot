using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class CaptureThread060
{
 public static object PlanLive()
 {
  var guide=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(g=>g.GetType().Name=="FullThread060Probe");
  var objectives=guide.GetComponent<Mavis.FirstIslandObjectives>();
  if(!objectives.TryGetGuidanceTarget(out var goal,out var id))throw new Exception("No active objective");
  var solve=(System.Collections.IEnumerator)guide.GetType().GetMethod("Solve",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(guide,new object[]{guide.transform.position,goal});
  guide.StartCoroutine(solve);return $"Live path to {id}: {goal}; gameplay pause preserved";
 }
 public static object Run()
 {
  const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;
  var guide=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(g=>g.GetType().Name=="FullThread060Probe");var t=guide.GetType();
  var route=(Vector3[])t.GetField("route",f).GetValue(guide);int count=(int)t.GetProperty("RoutePointCount").GetValue(guide);
  if(count<2)throw new Exception("No full live route");
  float previousFade=(float)t.GetField("fade",f).GetValue(guide);var lineRenderer=(MeshRenderer)t.GetField("renderer",f).GetValue(guide);bool previousEnabled=lineRenderer.enabled;
  t.GetField("fade",f).SetValue(guide,1f);t.GetMethod("Draw",f).Invoke(guide,null);lineRenderer.enabled=true;
  var mesh=(Mesh)t.GetField("mesh",f).GetValue(guide);var colors=mesh.colors;
  guide.GetComponent<Mavis.FirstIslandObjectives>().TryGetGuidanceTarget(out var expected,out var objectiveId);
  var report=new{objectiveId,expected=expected.ToString(),horizontalEndpointError=Vector3.ProjectOnPlane(route[count-1]-expected,Vector3.up).magnitude,count,start=route[0].ToString(),end=route[count-1].ToString(),length=((float[])t.GetField("lengths",f).GetValue(guide))[count-1],endAlpha=colors[(count-1)*8].a,visible=t.GetProperty("Visible").GetValue(guide),width=.11f,shaderErrors=ShaderUtil.GetShaderMessages(Resources.Load<Material>("Guidance/QuestThread").shader).Select(m=>m.message).ToArray()};
  File.WriteAllText(".codex/thread060/live-route.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  var obj=new GameObject("Disposable full route review camera",typeof(Camera));var camera=obj.GetComponent<Camera>();var active=RenderTexture.active;
  RenderTexture texture=null;Texture2D pixels=null;
  try
  {
   if(Camera.main){camera.CopyFrom(Camera.main);camera.transform.SetPositionAndRotation(Camera.main.transform.position,Camera.main.transform.rotation);}
   camera.enabled=false;texture=new RenderTexture(1600,900,24);texture.Create();camera.targetTexture=texture;pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
   Capture(camera,texture,pixels,".codex/thread060/live-player.png");
   int mid=Math.Min(count-1,18);Vector3 focus=Vector3.Lerp(route[4],route[mid],.5f)+Vector3.up*2.8f;Vector3 offset=new Vector3(9,6,-14);camera.fieldOfView=58;camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset));
   Capture(camera,texture,pixels,".codex/thread060/live-near.png");
   Vector3 centre=(route[0]+route[count-1])*.5f;float distance=Vector3.Distance(route[0],route[count-1]);offset=new Vector3(distance*.55f,distance*.8f,-distance*.5f);camera.transform.SetPositionAndRotation(centre+offset,Quaternion.LookRotation(-offset));
   Capture(camera,texture,pixels,".codex/thread060/live-complete.png");
   return Newtonsoft.Json.JsonConvert.SerializeObject(report);
  }
  finally{t.GetField("fade",f).SetValue(guide,previousFade);lineRenderer.enabled=previousEnabled;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(obj);if(texture){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);}
 }
 static void Capture(Camera camera,RenderTexture texture,Texture2D pixels,string path){camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
}
