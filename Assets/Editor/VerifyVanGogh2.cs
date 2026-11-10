using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
using Mavis.VisualStyles;

[InitializeOnLoad]
public static class VerifyVanGogh2
{
 const string Root=".codex/van-gogh-2/";
 static VerifyVanGogh2(){EditorApplication.playModeStateChanged+=Changed;}
 public static object Prepare()
 {
  if(File.Exists(Root+"baseline-result.json"))throw new Exception("Preserve established baseline; use Tools/ReviewVanGogh2.cs for new comparisons");
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Stopped Editor required");
  if(EditorSceneManager.GetSceneManagerSetup().Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");
  Directory.CreateDirectory(Root+"before");
  Write("scene-setup.json",EditorSceneManager.GetSceneManagerSetup());
  Write("saves-before.json",Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p))));
  SessionState.SetBool("VanGogh2.Pending",true);EditorApplication.EnterPlaymode();return "Baseline captures queued";
 }
 static void Changed(PlayModeStateChange state)
 {
  if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VanGogh2.Pending",false))
  {
   typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Path.GetFullPath(Root+"isolated-save.json"));
   GameSaveManager.BeginEditorTestSession();EditorApplication.delayCall+=Run;
  }
  if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("VanGogh2.Pending",false))
  {
   SessionState.SetBool("VanGogh2.Pending",false);
   var saved=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Root+"saves-before.json"));
   var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"scene-setup.json"));
   Write("preservation.json",new{formalSavesUnchanged=saved.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value),sceneSetupRestored=Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup())==Newtonsoft.Json.JsonConvert.SerializeObject(setup)});
  }
 }
 static void Write(string name,object value)=>File.WriteAllText(Root+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
 static async Task Frame(){int frame=Time.frameCount;var until=DateTime.UtcNow.AddSeconds(45);while(Time.frameCount==frame){if(!EditorApplication.isPlaying||DateTime.UtcNow>until)throw new Exception("Play interrupted");await Task.Delay(5);}}
 static async Task Until(Func<bool> condition,string label){var until=DateTime.UtcNow.AddSeconds(90);while(!condition()){if(!EditorApplication.isPlaying||DateTime.UtcNow>until)throw new Exception(label);await Task.Delay(30);}}
 static Color32[] Capture(Camera camera,RenderTexture rt,string path)
 {
  RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
  var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
  try{image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();}
  finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
 }
 static async void Run()
 {
  bool passed=false;string error=null;GameObject probe=null;RenderTexture rt=null;
  try
  {
   var load=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Main.unity");await Until(()=>load.isDone,"Main load");
   await Until(()=>!AdditiveSceneBootstrap.IsLoading&&UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>(),"World load");
   for(int i=0;i<30;i++)await Frame();Time.timeScale=0;
   var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var center=player.GetComponent<CharacterController>().bounds.center;
   var main=Camera.main;probe=new GameObject("Van Gogh temporary render probe",typeof(Camera));var camera=probe.GetComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.fieldOfView=55;
   rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.Create();
   var views=new List<(string,Vector3,Vector3)>{("coast",center+new Vector3(-8,4,-12),center+Vector3.up*.3f),("ocean",center+new Vector3(0,7,-4),center+new Vector3(0,8,-90))};
   var tree=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.ToLowerInvariant().Contains("tree")&&r.bounds.size.y>4).OrderBy(r=>Vector3.Distance(center,r.bounds.center)).FirstOrDefault();
   if(tree){var at=tree.bounds.center;views.Add(("vegetation",at+new Vector3(-12,3,-14),at));}
   var boss=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>();if(boss){var at=boss.transform.position+Vector3.up*3;views.Add(("boss",at+new Vector3(-11,4,-16),at));}
   Write("views.json",views.Select(v=>new{name=v.Item1,position=new[]{v.Item2.x,v.Item2.y,v.Item2.z},target=new[]{v.Item3.x,v.Item3.y,v.Item3.z}}));
   Write("world.json",new{player=center.ToString(),camera=main.transform.position.ToString(),clock=Time.time,renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length});
   foreach(var view in views)
   {
    camera.transform.position=view.Item2;camera.transform.LookAt(view.Item3);
    foreach(string mode in new[]{"realistic","van-gogh","dark-fairytale","origami","monochrome","ink-wash"})
    {
     VisualStyleManager.Instance.Select(mode,false);VisualStyleManager.Instance.SetIntensity(.9f,false);await Frame();
     Capture(camera,rt,Root+"before/"+view.Item1+"-"+mode+".png");
    }
   }
   passed=true;
  }
  catch(Exception e){error=e.ToString();}
  finally
  {
   Time.timeScale=1;if(probe)UnityEngine.Object.DestroyImmediate(probe);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
   Write("baseline-result.json",new{passed,error});EditorApplication.ExitPlaymode();
  }
 }
}
