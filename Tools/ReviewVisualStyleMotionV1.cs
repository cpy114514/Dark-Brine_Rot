using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;
public static class ReviewVisualStyleMotionV1
{
 const string Root=".codex/visual-style-v1";
 static async Task Frame(){int n=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(30);while(n==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Frame stalled");await Task.Delay(5);}}
 static async Task<Color32[]> Capture(Camera camera,string path)
 {
  Color32[] pixels=null;Action<ScriptableRenderContext,Camera> cb=(context,c)=>{if(c!=camera||pixels!=null)return;var rt=RenderTexture.active;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);try{RenderTexture.active=camera.targetTexture;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();pixels=tex.GetPixels32();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{RenderTexture.active=rt;UnityEngine.Object.DestroyImmediate(tex);}};
  RenderPipelineManager.endCameraRendering+=cb;try{for(int i=0;i<60&&pixels==null;i++)await Frame();if(pixels==null)throw new Exception("Camera did not render");return pixels;}finally{RenderPipelineManager.endCameraRendering-=cb;}
 }
 public static object Start(){_=Run();return new{started=true};}
 static async Task Run()
 {
  var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
  bool passed=false;var manager=VisualStyleManager.Instance;string id=manager.Current.id;float intensity=manager.Intensity,time=Time.timeScale;var original=QualitySettings.renderPipeline;var current=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool pixel=Pixel360Rendering.IsActive;
  GameObject root=null;RenderTexture target=null;UniversalRenderPipelineAsset pipeline=null;double staticDifference=-1;
  try
  {
   Pixel360Rendering.ReleaseCurrent();pipeline=UnityEngine.Object.Instantiate(current);pipeline.renderScale=1;QualitySettings.renderPipeline=pipeline;
   var arena=UnityEngine.Object.FindFirstObjectByType<CombatTestArena>();arena.player.GetComponent<PlayerHealth>().GrantProtection(60);
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var spawn=typeof(CombatTestArena).GetMethod("SpawnOne",flags);var clear=typeof(CombatTestArena).GetMethod("ClearSpawned",flags);
   foreach(var boss in new[]{arena.cappuccino,arena.nailong,arena.bombardino})
   {
    Time.timeScale=1;spawn.Invoke(arena,new object[]{boss,16f});for(int i=0;i<8;i++)await Frame();
    var actor=UnityEngine.Object.FindObjectsByType<GameSaveExcluded>(FindObjectsSortMode.None).Select(x=>x.gameObject).First(g=>g!=arena.player.gameObject&&g.name.StartsWith(boss.name));
    var animators=actor.GetComponentsInChildren<Animator>();var controllers=animators.Select(a=>a.runtimeAnimatorController).ToArray();var colliders=actor.GetComponentsInChildren<Collider>();
    foreach(string style in new[]{"realistic","dark-fairytale"}){manager.Select(style,false);for(int i=0;i<8;i++)await Frame();Check(actor.activeInHierarchy&&animators.Select(a=>a.runtimeAnimatorController).SequenceEqual(controllers)&&colliders.All(c=>c),boss.name+" keeps original live animator/colliders under "+style);}
    clear.Invoke(arena,null);await Frame();
   }
   root=new GameObject("Temporary camera motion review");root.AddComponent<GameSaveExcluded>();var go=new GameObject("Motion review camera",typeof(Camera));go.transform.SetParent(root.transform);var camera=go.GetComponent<Camera>();camera.fieldOfView=48;camera.farClipPlane=250;
   Vector3 center=arena.player.GetComponent<CharacterController>().bounds.center;camera.transform.position=center+new Vector3(10,4,10);camera.transform.LookAt(center);
   target=new RenderTexture(960,540,24);target.Create();camera.targetTexture=target;manager.Select("dark-fairytale",false);manager.SetIntensity(.85f,false);Time.timeScale=0;for(int i=0;i<3;i++)await Frame();
   Directory.CreateDirectory(Root+"/camera-motion");var first=await Capture(camera,Root+"/camera-motion/static-a.png");var next=await Capture(camera,Root+"/camera-motion/static-b.png");
   staticDifference=first.Zip(next,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))/3).Average();Check(staticDifference<.25,"Paper/ink remain stable in consecutive frozen native frames");
   for(int i=0;i<16;i++){float a=Mathf.Lerp(15,85,i/15f)*Mathf.Deg2Rad;camera.transform.position=center+new Vector3(Mathf.Sin(a)*14,4,Mathf.Cos(a)*14);camera.transform.LookAt(center);await Capture(camera,Root+"/camera-motion/"+i.ToString("D2")+".png");}
   Check(true,"Sixteen native camera-orbit frames captured for silhouette review");passed=true;
  }
  catch(Exception e){File.WriteAllText(Root+"/motion-error.txt",e.ToString());}
  finally
  {
   manager.Select(id,false);manager.SetIntensity(intensity,false);Time.timeScale=time;Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=original;if(pixel&&current)Pixel360Rendering.Configure(current,true);
   if(root)UnityEngine.Object.DestroyImmediate(root);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(pipeline)UnityEngine.Object.DestroyImmediate(pipeline);
   File.WriteAllText(Root+"/motion-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,staticMeanByteDifference=staticDifference},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;
  }
 }
}
