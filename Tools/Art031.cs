using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;

public static class Art031
{
 const string Root=".codex/art031";
 static async Task Frames(int n=4){int f=Time.frameCount+n;var end=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount<f){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frames stalled");await Task.Delay(5);}}
 public static object Prepare()
 {
  if(Application.isPlaying||File.Exists(Root+"/editor-setup.json"))throw new Exception("Fresh stopped workspace required");var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");File.WriteAllText(Root+"/editor-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup,Newtonsoft.Json.Formatting.Indented));
  Directory.CreateDirectory(Root+"/save-after-user-stop");foreach(var n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"}){var p=Path.Combine(Application.persistentDataPath,n);if(File.Exists(p))File.Copy(p,Root+"/save-after-user-stop/"+n,false);}
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);return new{scenes=UnityEngine.SceneManagement.SceneManager.sceneCount};
 }
 public static async Task<object> Stage()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||GameObject.Find("Owned art031 stage"))throw new Exception("Fresh environment fixture required");
  var boss=GameObject.Find("CappuccinoAssassino");if(boss)boss.SetActive(false);
  var root=new GameObject("Owned art031 stage");root.SetActive(false);root.AddComponent<GameSaveExcluded>();
  var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.name="Owned native Sahur";actor.AddComponent<GameSaveExcluded>();actor.transform.position=new Vector3(369.59f,3.77f,731.02f);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;foreach(var c in actor.GetComponentsInChildren<Camera>(true))c.enabled=false;root.SetActive(true);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
  var animator=actor.GetComponentInChildren<Animator>();if(animator){animator.applyRootMotion=false;animator.Play("Idle",0,.25f);animator.Update(0);animator.enabled=false;}
  var bounds=actor.GetComponentsInChildren<Renderer>().First(r=>r.bounds.size.sqrMagnitude>1).bounds;
  var cam=new GameObject("Owned art031 fixture view",typeof(Camera)).GetComponent<Camera>();cam.transform.SetParent(root.transform);cam.gameObject.AddComponent<AudioListener>();cam.tag="MainCamera";cam.fieldOfView=48;cam.farClipPlane=1000;cam.transform.position=bounds.center+new Vector3(8,2,10);cam.transform.LookAt(bounds.center);
  var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(w=>w.gameObject.scene.IsValid());var end=DateTime.UtcNow.AddSeconds(60);await Frames(4);while(!manager.IsReady){if(DateTime.UtcNow>end)throw new Exception("Environment readiness timeout");await Frames(1);}var report=new{ready=true,actorBounds=bounds.ToString(),savable=GameSaveManager.HasSavableGame,camera=cam.transform.position.ToString()};File.WriteAllText(Root+"/stage.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> Arrange()
 {
  if(GameSaveManager.HasSavableGame)throw new Exception("Preview only");var actor=GameObject.Find("Owned native Sahur");var hits=Physics.RaycastAll(new Ray(new Vector3(250,400,500),Vector3.down),600);var hit=hits.First(h=>h.collider.GetComponentInParent<ProceduralIsland>());actor.transform.position=new Vector3(250,hit.point.y+5,500);await Frames(2);var parts=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var b=parts[0].bounds;foreach(var r in parts)b.Encapsulate(r.bounds);actor.transform.position+=Vector3.up*(hit.point.y-b.min.y+.03f);await Frames(2);b=parts[0].bounds;foreach(var r in parts)b.Encapsulate(r.bounds);Camera.main.transform.position=b.center+new Vector3(9,2,11);Camera.main.transform.LookAt(b.center);return new{ground=hit.point.ToString(),bounds=b.ToString()};
 }
 public static object Snapshot()
 {
  if(!Application.isPlaying||File.Exists(Root+"/live.json"))throw new Exception("Fresh live snapshot required");
  Directory.CreateDirectory(Root+"/save-before");foreach(var n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"}){var p=Path.Combine(Application.persistentDataPath,n);if(File.Exists(p))File.Copy(p,Root+"/save-before/"+n,false);}
  var s=VisualStyleManager.Instance;var c=Camera.main;var p1=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var report=new{style=s.Current.id,intensity=s.Intensity,details=s.Details.ToString("R"),time=Time.timeScale,pixel=Pixel360Rendering.IsActive,camera=new{p=c.transform.position.ToString("R"),q=c.transform.rotation.ToString("R"),c.fieldOfView},player=p1?p1.transform.position.ToString("R"):null};File.WriteAllText(Root+"/live.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static async Task<object> Verify(string label="verification")
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||File.Exists(Root+"/"+label+".json"))throw new Exception("Fresh preview verification required");
  string dir=Root+"/"+label;Directory.CreateDirectory(dir);var style=VisualStyleManager.Instance;string oldStyle=style.Current.id;var oldDetails=style.Details;float intensity=style.Intensity,time=Time.timeScale;var c=Camera.main;var p=c.transform.position;var q=c.transform.rotation;var target=c.targetTexture;var active=RenderTexture.active;
  var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
  int Changed(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Math.Max(Math.Abs(x.r-y.r),Math.Max(Math.Abs(x.g-y.g),Math.Abs(x.b-y.b)))>2?1:0).Sum();
  async Task<Color32[]> Pixels(string label){await Frames(5);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+label+".png",tex.EncodeToPNG());return tex.GetPixels32();}
  try
  {
   Time.timeScale=0;c.targetTexture=rt;style.Select("origami",false);await Frames(5);var details=style.Details;
   var f=Resources.FindObjectsOfTypeAll<VisualStyleRendererFeature>().First(v=>v.isActive);var flags=BindingFlags.Instance|BindingFlags.NonPublic;var pass=typeof(VisualStyleRendererFeature).GetField("pass",flags).GetValue(f);var copy=pass.GetType().GetField("comicDepthCopy",flags).GetValue(pass);var mat=(Material)typeof(VisualStyleRendererFeature).GetField("material",flags).GetValue(f);var depthMat=(Material)copy.GetType().GetField("m_CopyDepthMaterial",flags).GetValue(copy);int matId=mat.GetInstanceID(),depthId=depthMat.GetInstanceID();
   check(((ScriptableRenderPass)pass).input==ScriptableRenderPassInput.Depth,"Comics request complete depth without incomplete normals");check(depthMat&&depthMat.shader.isSupported,"URP depth-copy resource available");
   foreach(var mode in new[]{"ink-wash","van-gogh"})
   {
    style.Select(mode,false);var defaults=style.Details;
    for(int k=0;k<4;k++){for(int j=0;j<4;j++)style.SetDetail(j,defaults[j],false);style.SetDetail(k,0,false);var a=await Pixels(mode+"-detail"+k+"-zero");style.SetDetail(k,1,false);var b=await Pixels(mode+"-detail"+k+"-one");check(Changed(a,b)>100,"Painting detail visible response "+mode+" "+k);}
    for(int j=0;j<4;j++)style.SetDetail(j,defaults[j],false);style.SetIntensity(.4f,false);var low=await Pixels(mode+"-intensity-low");style.SetIntensity(1,false);var high=await Pixels(mode+"-intensity-high");check(Changed(low,high)>1000,"Intensity visible response "+mode);
    if(mode=="monochrome")check(high.All(x=>x.r==x.g&&x.g==x.b),"Manga world is entirely grayscale");
    style.SetIntensity(intensity,false);
   }
   var comicValues=new[]{new Vector4(.12f,.34f,.56f,.78f),new Vector4(.85f,.65f,.45f,.25f)};
   var ids=new[]{"ink-wash","van-gogh"};
   var keys=ids.SelectMany(id=>Enumerable.Range(0,4).Select(i=>VisualStyleSettings.DetailKey(id,i))).ToArray();
   if(keys.Any(PlayerPrefs.HasKey))throw new Exception("Preserve pre-existing new comic controls; require a backed fixture");
   try{for(int i=0;i<ids.Length;i++)VisualStyleSettings.SaveDetails(ids[i],comicValues[i]);for(int i=0;i<ids.Length;i++){style.Select(ids[i],false);check((style.Details-comicValues[i]).sqrMagnitude<.000001f,"Independent persisted comic controls "+ids[i]);style.enabled=false;style.enabled=true;check((style.Details-comicValues[i]).sqrMagnitude<.000001f,"Native re-enable loads saved controls "+ids[i]);}}
   finally{foreach(var key in keys)PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
   bool hadStyle=PlayerPrefs.HasKey(VisualStyleSettings.StyleKey);string savedId=VisualStyleSettings.SelectedId;
   try{foreach(var mode in ids){PlayerPrefs.SetString(VisualStyleSettings.StyleKey,mode);PlayerPrefs.Save();style.Select("realistic",false);style.RestorePreference();check(style.Current.id==mode,"Saved painting selection reloads "+mode);}}
   finally{if(hadStyle)PlayerPrefs.SetString(VisualStyleSettings.StyleKey,savedId);else PlayerPrefs.DeleteKey(VisualStyleSettings.StyleKey);PlayerPrefs.Save();}
   check(style.Presets.Count==6&&style.Presets.Select(v=>v.id).Distinct().Count()==6,"Six unique preset slots");
   style.Select("origami",false);
   for(int i=0;i<24;i++){style.Select(new[]{"realistic","dark-fairytale","origami","monochrome","ink-wash","van-gogh"}[i%6],false);await Frames(1);check(((Material)typeof(VisualStyleRendererFeature).GetField("material",flags).GetValue(f)).GetInstanceID()==matId&&((Material)copy.GetType().GetField("m_CopyDepthMaterial",flags).GetValue(copy)).GetInstanceID()==depthId,"Cached materials survive style switch "+i);}
   var returns=new Dictionary<string,int>();
   foreach(var mode in new[]{"ink-wash","van-gogh"})
   {
    style.Select(mode,false);c.transform.SetPositionAndRotation(p,q);var initial=await Pixels(mode+"-motion-start");var pivot=GameObject.Find("Owned native Sahur").GetComponentInChildren<SkinnedMeshRenderer>().bounds.center;var offset=p-pivot;
    for(int i=0;i<24;i++){c.transform.position=pivot+Quaternion.Euler(0,Mathf.Sin(i/23f*Mathf.PI*2)*22,0)*offset;c.transform.LookAt(pivot);await Frames(3);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+mode+"-motion-"+i.ToString("D3")+".png",tex.EncodeToPNG());}
    c.transform.SetPositionAndRotation(p,q);var final=await Pixels(mode+"-motion-return");returns[mode]=Changed(initial,final);check(returns[mode]<initial.Length*.1,"Camera roundtrip no wholesale pattern reshuffle "+mode);
   }
   var g=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).First(x=>x.name.StartsWith("Grass infill"));check(g.GetLODs().Length==1&&g.GetLODs()[0].renderers[0].GetComponent<MeshFilter>().sharedMesh.name=="VerifiedGrassNear","Fixed complete grass layout preserved");check(!GameSaveManager.HasSavableGame,"Preview cannot overwrite user game");
   var report=new{passed=rows.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),checks=rows.Count,roundtripChangedPixels=returns,roundtripPixels=1280*720,rows};File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{c.targetTexture=target;c.transform.SetPositionAndRotation(p,q);style.Select(oldStyle,false);style.SetIntensity(intensity,false);for(int i=0;i<4;i++)style.SetDetail(i,oldDetails[i],false);Time.timeScale=time;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 public static async Task<object> UI(string label="ui")
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||File.Exists(Root+"/"+label+".json"))throw new Exception("Fresh preview UI required");var style=VisualStyleManager.Instance;var id=style.Current.id;var details=style.Details;float time=Time.timeScale;var lockState=Cursor.lockState;bool visible=Cursor.visible;var selection=UnityEngine.EventSystems.EventSystem.current?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;GameObject proof=null;var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
  async Task<string> Screenshot(string name){string p=Root+"/"+label+"-"+name+".png";ScreenCapture.CaptureScreenshot(Path.GetFullPath(p));for(int i=0;i<60;i++){await Frames(1);if(File.Exists(p)&&new FileInfo(p).Length>100)return p;}throw new Exception("GameView image timeout");}
  try
  {
   Time.timeScale=0;style.Select("ink-wash",false);VisualStyleUIController.Open();var ui=UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>();check(ui.StyleButtons.Count==6,"Six style cards available");check(ui.DetailSliders.Count==4,"Four style-specific painting controls");
   var texts=ui.GetComponentsInChildren<UnityEngine.UI.Text>(true).Select(t=>t.text).ToArray();
   check(texts.Contains("Ink Wash")&&texts.Contains("Van Gogh")&&texts.Contains("Color Comic")&&texts.Contains("B&W Manga"),"Existing and new card titles visible");
   check(!texts.Any(t=>t.Contains("Fold shading")||t.Contains("Film grain")||t=="Origami"),"Legacy effect labels removed");foreach(var slider in ui.DetailSliders)check(slider.minValue==0&&slider.maxValue==1,"Comic control normalized range");
   Canvas.ForceUpdateCanvases();
   var cards=ui.StyleButtons.Select(b=>(RectTransform)b.transform.parent.parent).ToArray();var rectangles=new List<Rect>();
   foreach(var card in cards){var corners=new Vector3[4];card.GetWorldCorners(corners);var rectangle=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);rectangles.Add(rectangle);check(rectangle.xMin>=0&&rectangle.yMin>=0&&rectangle.xMax<=Screen.width&&rectangle.yMax<=Screen.height,"Card inside screen "+card.name);check(card.GetComponentInChildren<UnityEngine.UI.RawImage>().texture!=null,"Native preview bound "+card.name);}
   for(int a=0;a<rectangles.Count;a++)for(int b=a+1;b<rectangles.Count;b++)check(!rectangles[a].Overlaps(rectangles[b]),"Cards do not overlap "+a+" / "+b);
   foreach(var a in style.Presets)foreach(var b in style.Presets){check(ui.Comparison.SetStyles(a.id,b.id)&&ui.Comparison.LeftId==a.id&&ui.Comparison.RightId==b.id,"Matching preview pair "+a.id+" / "+b.id);}
   proof=new GameObject("Owned art031 UI color",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));var canvas=proof.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1700;GameUITheme.Scale(canvas);var patch=GameUITheme.Rect(proof.transform,"Red UI proof",Vector2.zero,new Vector2(100,60));patch.anchorMin=patch.anchorMax=Vector2.zero;patch.anchoredPosition=new Vector2(100,70);GameUITheme.Image(patch,Color.red);
   EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await Frames(8);var path=await Screenshot("gallery-ui");var im=new Texture2D(2,2);try{im.LoadImage(File.ReadAllBytes(path));var c=im.GetPixel(Mathf.RoundToInt(patch.position.x),Mathf.RoundToInt(patch.position.y));check(c.r>.95f&&c.g<.03f&&c.b<.03f,"Overlay red stays red outside paper processing");}finally{UnityEngine.Object.DestroyImmediate(im);}
   var detailButton=ui.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="DETAILS");var detailsPanel=(GameObject)typeof(VisualStyleUIController).GetField("detailPanel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);if(!detailsPanel.activeSelf)detailButton.onClick.Invoke();await Frames(3);
   check(ui.GetComponentsInChildren<UnityEngine.UI.Text>(false).Any(t=>t.text.StartsWith("Ink bleed")),"Ink-specific controls visible");await Screenshot("ink-details-ui");
   style.Select("van-gogh",false);await Frames(3);check(ui.GetComponentsInChildren<UnityEngine.UI.Text>(false).Any(t=>t.text.StartsWith("Impasto")),"Oil-specific controls visible");await Screenshot("oil-details-ui");
   style.Select("monochrome",false);await Frames(5);var mangaPath=await Screenshot("manga-gallery-ui");var monoUI=new Texture2D(2,2);try{monoUI.LoadImage(File.ReadAllBytes(mangaPath));var red=monoUI.GetPixel(Mathf.RoundToInt(patch.position.x),Mathf.RoundToInt(patch.position.y));check(red.r>.95f&&red.g<.03f&&red.b<.03f,"Manga overlay red remains red");}finally{UnityEngine.Object.DestroyImmediate(monoUI);}
   ui.Comparison.SetStyles("realistic","ink-wash");ui.ShowComparison(true);await Frames(3);ui.Comparison.leftStyle.Show();await Frames(3);
   var list=GameObject.Find("Dropdown List");var scroll=list.GetComponentInChildren<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();await Frames(3);
   var choices=list.GetComponentsInChildren<UnityEngine.UI.Toggle>(false);var last=choices.Last();
   var cornersLast=new Vector3[4];((RectTransform)last.transform).GetWorldCorners(cornersLast);var cornersView=new Vector3[4];scroll.viewport.GetWorldCorners(cornersView);
   check(cornersLast[0].y>=cornersView[0].y-2&&cornersLast[2].y<=cornersView[2].y+2,"Sixth dropdown entry scrolls into view");
   check(!last.isOn,"Sixth dropdown entry starts unselected");last.isOn=true;await Frames(4);check(ui.Comparison.LeftId=="van-gogh","Native sixth-item toggle selects Van Gogh");
   ui.Comparison.SetStyles("ink-wash","van-gogh");ui.ShowComparison(true);ui.Comparison.Split=.5f;await Frames(5);await Screenshot("comparison-ui");check(ui.Comparison.LeftId=="ink-wash"&&ui.Comparison.RightId=="van-gogh","Ink/Van Gogh reveal uses matching previews");
   var report=new{passed=rows.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),checks=rows.Count,rows};File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{var ui=UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>();if(ui)ui.Close();if(proof)UnityEngine.Object.DestroyImmediate(proof);style.Select(id,false);for(int i=0;i<4;i++)style.SetDetail(i,details[i],false);Time.timeScale=time;Cursor.lockState=lockState;Cursor.visible=visible;if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selection);}
 }
 public static async Task<object> Capture(string label,string candidatePath="",bool depthOnly=false,bool allStyles=false,bool pixelCapture=false,int msaa=1)
 {
  if(!Application.isPlaying||Directory.Exists(Root+"/"+label))throw new Exception("Fresh live capture label required");string dir=Root+"/"+label;Directory.CreateDirectory(dir);
  var style=VisualStyleManager.Instance;var oldStyle=style.Current.id;var oldDetails=style.Details;float oldIntensity=style.Intensity,time=Time.timeScale;
  var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);var c0=Camera.main;var pos=c0.transform.position;var rot=c0.transform.rotation;
  var behaviours=c0.GetComponents<MonoBehaviour>().Where(b=>!(b is UniversalAdditionalCameraData)).ToDictionary(b=>b,b=>b.enabled);
  var pipeline=QualitySettings.renderPipeline;var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool pixel=Pixel360Rendering.IsActive;
  var w=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(m=>m.gameObject.scene.IsValid());var main=typeof(WorldPerformanceManager).GetField("mainCamera",BindingFlags.Instance|BindingFlags.NonPublic);var oldMain=main.GetValue(w);
  var next=typeof(WorldPerformanceManager).GetField("nextUpdate",BindingFlags.Instance|BindingFlags.NonPublic);float oldNext=(float)next.GetValue(w);var refreshing=typeof(WorldPerformanceManager).GetField("foliageRefreshing",BindingFlags.Instance|BindingFlags.NonPublic);var refresh=typeof(WorldPerformanceManager).GetMethod("RefreshFoliage",BindingFlags.Instance|BindingFlags.NonPublic);
  var feature=Resources.FindObjectsOfTypeAll<VisualStyleRendererFeature>().First(f=>f.isActive);var mf=typeof(VisualStyleRendererFeature).GetField("material",BindingFlags.Instance|BindingFlags.NonPublic);var pf=typeof(VisualStyleRendererFeature).GetField("pass",BindingFlags.Instance|BindingFlags.NonPublic);var oldMat=(Material)mf.GetValue(feature);var pass=(ScriptableRenderPass)pf.GetValue(feature);var oldInput=pass.input;
  Camera own=null;UniversalRenderPipelineAsset clone=null;Shader shader=null;Material mat=null;RenderTexture rt=null;Texture2D tex=null;var active=RenderTexture.active;int callbacks=0;Action<ScriptableRenderContext,Camera> fence=(ctx,c)=>{if(c==own)callbacks++;};
  var rows=new List<object>();
  try
  {
   Time.timeScale=0;foreach(var b in behaviours.Keys)b.enabled=false;Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(source);clone.renderScale=1;clone.msaaSampleCount=msaa;QualitySettings.renderPipeline=clone;
   foreach(var c in cameras.Keys)if(c)c.enabled=false;
   own=new GameObject("Owned art031 camera",typeof(Camera)).GetComponent<Camera>();own.CopyFrom(c0);own.tag="MainCamera";own.enabled=true;main.SetValue(w,own);
   next.SetValue(w,float.PositiveInfinity);
   rt=RenderTexture.GetTemporary(new RenderTextureDescriptor(1280,720,RenderTextureFormat.ARGBHalf,24){msaaSamples=msaa});own.targetTexture=rt;own.allowMSAA=true;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);RenderPipelineManager.endCameraRendering+=fence;
   // Pipeline construction recreates renderer features. Override only after
   // its first renders, and prove the actual material/pass stays installed.
   await Frames(4);oldMat=(Material)mf.GetValue(feature);pass=(ScriptableRenderPass)pf.GetValue(feature);oldInput=pass.input;
   if(pixelCapture)Pixel360Rendering.Configure(clone,true);
   if(!string.IsNullOrEmpty(candidatePath)){shader=ShaderUtil.CreateShaderAsset(File.ReadAllText(candidatePath),true);mat=new Material(shader);mf.SetValue(feature,mat);}
   var poses=new[]{new{n="player",p=pos,q=rot},new{n="meadow",p=new Vector3(266.8973f,42.9872665f,482.519623f),q=new Quaternion(-.113556989f,.454448819f,-.05853899f,-.8815636f)}};
   foreach(var mode in allStyles?new[]{"realistic","dark-fairytale","origami","monochrome","ink-wash","van-gogh"}:new[]{"ink-wash","van-gogh"})
   {
    style.Select(mode,false);await Frames(2);if(depthOnly&&mode=="origami")pass.ConfigureInput(ScriptableRenderPassInput.Depth);
    foreach(var pose in poses)
    {
     own.transform.SetPositionAndRotation(pose.p,pose.q);var end=DateTime.UtcNow.AddSeconds(30);while((bool)refreshing.GetValue(w)){if(DateTime.UtcNow>end)throw new Exception("Old refresh timeout");await Frames(1);}refresh.Invoke(w,new object[]{pose.p});while((bool)refreshing.GetValue(w)){if(DateTime.UtcNow>end)throw new Exception("New refresh timeout");await Frames(1);}int before=callbacks;await Frames(6);if(callbacks<=before)throw new Exception("No native render fence");
     if(mat&&(mf.GetValue(feature) as Material)!=mat)throw new Exception("Candidate material was replaced by pipeline recreation");if(!ReferenceEquals(pass,pf.GetValue(feature)))throw new Exception("Observed pass is no longer active");if(rt.antiAliasing>1)rt.ResolveAntiAliasedSurface();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+mode+"-"+pose.n+".png",tex.EncodeToPNG());rows.Add(new{mode,pose=pose.n,cameraPosition=own.transform.position.ToString("R"),cameraRotation=own.transform.rotation.ToString("R"),callbacks=callbacks-before,input=pass.input.ToString(),effectiveShader=((Material)mf.GetValue(feature)).shader.name,details=style.Details.ToString(),intensity=style.Intensity,pixel=Pixel360Rendering.IsActive,renderScale=clone.renderScale,targetMsaa=rt.antiAliasing,pipelineMsaa=clone.msaaSampleCount});
    }
   }
   own.cullingMask=0;await Frames();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/empty.png",tex.EncodeToPNG());
   var result=new{label,rows,callbacks,shaderErrors=shader?ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.severity,m.message}).ToArray():null};File.WriteAllText(dir+"/report.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally
  {
   RenderPipelineManager.endCameraRendering-=fence;mf.SetValue(feature,oldMat);pass.ConfigureInput(oldInput);main.SetValue(w,oldMain);next.SetValue(w,oldNext);if(own)UnityEngine.Object.DestroyImmediate(own.gameObject);RenderTexture.active=active;if(rt)RenderTexture.ReleaseTemporary(rt);if(tex)UnityEngine.Object.DestroyImmediate(tex);Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=pipeline;if(clone)UnityEngine.Object.DestroyImmediate(clone);if(pixel)Pixel360Rendering.Configure(source,true);style.Select(oldStyle,false);style.SetIntensity(oldIntensity,false);for(int i=0;i<4;i++)style.SetDetail(i,oldDetails[i],false);foreach(var c in cameras)if(c.Key)c.Key.enabled=c.Value;foreach(var b in behaviours)if(b.Key)b.Key.enabled=b.Value;c0.transform.SetPositionAndRotation(pos,rot);Time.timeScale=time;if(mat)UnityEngine.Object.DestroyImmediate(mat);if(shader)UnityEngine.Object.DestroyImmediate(shader);
  }
 }
 public static object RestoreWorkspace()
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped idle Editor required");
  foreach(var p in Directory.GetFiles(Root+"/save-after-user-stop"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Preserve newer save; no rollback");
  var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(File.ReadAllText(Root+"/editor-setup.json"));
  UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DarkFairytale.shader");
  var report=new{stopped=!Application.isPlaying,scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,paths=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path).ToArray(),dirty=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty),ownedFixtures=Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.scene.IsValid()&&g.name.StartsWith("Owned art031")),savesUnchanged=true,shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{severity=m.severity.ToString(),m.message}).ToArray(),editorPid=System.Diagnostics.Process.GetCurrentProcess().Id};
  File.WriteAllText(Root+"/final-native.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }

 public static object RestoreBuildAlias(bool completedCleanupGuard=false)
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer||(File.Exists(Root+"/build/build-running")&&!completedCleanupGuard))throw new Exception("Wait for completed build");
  if(completedCleanupGuard)
  {
   var buildType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildArt031")).First(t=>t!=null);var task=(Task)buildType.GetField("pending",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
   if(!task.IsCompleted||!task.IsFaulted||task.Exception.GetBaseException().Message!="Preserve unexpected material mutation")throw new Exception("Unqualified cleanup state");
  }
  const string path="Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.mat";
  var expected=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Root+"/build-protected-hashes.json"))[path];
  string Hash()=>BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
  if(Hash()==expected)return new{restored=false,hashMatches=true};
  // The known Unity build material upgrade copies BaseMap to the unused legacy
  // alias. Restore only that exact native change; refuse any other mutation.
  var mat=AssetDatabase.LoadAssetAtPath<Material>(path);var alias=mat.GetTexture("_MainTex");
  if(!alias||alias!=mat.GetTexture("_BaseMap"))throw new Exception("Unexpected duck material change; preserve and inspect");
  mat.SetTexture("_MainTex",null);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
  if(Hash()!=expected)throw new Exception("Material has additional changes; preserve and inspect");
  if(completedCleanupGuard){File.WriteAllText(Root+"/build/cleanup-recovery.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{completedBuild=true,completedTask=true,automaticAliasGuardRejected=true,exactDuckHashRestored=true,settingsRestoredBeforeGuard=true,buildFlagsAndPreloadsRestoreExecutedBeforeGuard=true},Newtonsoft.Json.Formatting.Indented));File.Delete(Root+"/build/build-running");}
  return new{restored=true,hashMatches=true};
 }

 public static object RemoveFixturePreferenceKeys()
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped required");
  var delta=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/prefs-comparison-final.json"));
  if(delta["changedNames"].Any(v=>!((string)v).StartsWith("DarkBrine.HealingPacks.Campaign_h"))||delta["newNames"].Count()<1||delta["newNames"].Count()>3)throw new Exception("Unexpected preference delta");
  var keys=delta["newNames"].Select(t=>System.Text.RegularExpressions.Regex.Match((string)t,@"^(DarkBrine\.HealingPacks\.Remaining\.[0-9a-f]{32})_h[0-9]+$")).ToArray();
  var original=File.ReadAllText(Root+"/campaign-pref-before.txt");var current=PlayerPrefs.GetString("DarkBrine.HealingPacks.Campaign");
  if(current!=original&&!keys.Any(k=>k.Success&&k.Groups[1].Value.EndsWith(current)))throw new Exception("Preserve newer user preference");
  foreach(var key in keys){if(!key.Success)throw new Exception("Unexpected key; preserve");string id=key.Groups[1].Value.Substring("DarkBrine.HealingPacks.Remaining.".Length);if(original==id||Directory.GetFiles(Root+"/save-after-user-stop").Any(p=>File.ReadAllText(p).Contains(id)))throw new Exception("Preserve user campaign");if(PlayerPrefs.GetInt(key.Groups[1].Value,-1)!=3)throw new Exception("Preserve modified campaign");}
  PlayerPrefs.SetString("DarkBrine.HealingPacks.Campaign",original);foreach(var key in keys)PlayerPrefs.DeleteKey(key.Groups[1].Value);PlayerPrefs.Save();
  return new{removedUnusedFixtureKeys=keys.Length,existingValuesUntouched=true};
 }

 public static object AddStyles()
 {
  if(Application.isPlaying)throw new Exception("Stopped prepared Editor required");
  VisualStyleSetupV2.InstallPaintingStyles();
  return new{presets=Resources.LoadAll<VisualStylePreset>("VisualStyles/Presets").OrderBy(p=>(int)p.Mode).Select(p=>new{p.id,p.displayName,mode=(int)p.Mode}).ToArray()};
 }
 public static object Noon()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||!GameObject.Find("Owned art031 stage"))throw new Exception("Owned fixture required");
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
  var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  clock.GetType().GetField("clockRuns",flags).SetValue(clock,false);clock.GetType().GetField("startHour",flags).SetValue(clock,12f);clock.GetType().GetField("elapsedGameHours",flags).SetValue(clock,0f);clock.GetType().GetMethod("ApplyTimeOfDay",flags).Invoke(clock,null);
  return new{frozenNoon=true};
 }

 public static async Task<object> Transparent(string label="transparent")
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||File.Exists(Root+"/"+label+".json"))throw new Exception("Fresh owned visual fixture required");
  var style=VisualStyleManager.Instance;var oldId=style.Current.id;var details=style.Details;float time=Time.timeScale,intensity=style.Intensity;var cam=Camera.main;var oldTarget=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);GameObject quad=null;Material material=null;var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
  async Task<Color32[]> Shot(string imageLabel){await Frames(5);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Root+"/"+label+"-"+imageLabel+".png",tex.EncodeToPNG());return tex.GetPixels32();}
  try
  {
   Time.timeScale=0;cam.targetTexture=rt;
   quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Owned art031 transparent probe";UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());quad.transform.position=cam.transform.TransformPoint(new Vector3(-3,1,9));quad.transform.rotation=cam.transform.rotation;quad.transform.localScale=Vector3.one*1.5f;
   material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.02f,.85f,1,.8f));material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=(int)RenderQueue.Transparent;quad.GetComponent<Renderer>().sharedMaterial=material;
   var viewport=cam.WorldToViewportPoint(quad.transform.position);int center=Mathf.FloorToInt(viewport.y*720)*1280+Mathf.FloorToInt(viewport.x*1280);
   foreach(var id in new[]{"realistic","origami","monochrome","ink-wash","van-gogh"})
   {
    style.Select(id,false);var visible=await Shot(id);var p=visible[center];
    check(id=="monochrome"?p.r==p.g&&p.g==p.b:id=="ink-wash"?Math.Abs(p.r-p.b)<35:p.b>p.r+20,"Transparent probe correct color policy "+id);
    quad.SetActive(false);var hidden=await Shot(id+"-off");quad.SetActive(true);
    int contribution=visible.Zip(hidden,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>10?1:0).Sum();check(contribution>500,"Transparent probe visibly contributes "+id);
    if(id=="monochrome")check(visible.All(x=>x.r==x.g&&x.g==x.b),"Manga includes transparent world in grayscale");
    if(id!="realistic")
    {
     var feature=Resources.FindObjectsOfTypeAll<VisualStyleRendererFeature>().First(f=>f.isActive);var pass=(ScriptableRenderPass)typeof(VisualStyleRendererFeature).GetField("pass",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(feature);
     check(pass.renderPassEvent==RenderPassEvent.AfterRenderingPostProcessing&&pass.input==ScriptableRenderPassInput.Depth,"Composed world pass and complete depth "+id);
    }
   }
   check(!GameSaveManager.HasSavableGame,"Probe remains excluded from user saves");var report=new{passed=rows.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),checks=rows.Count,rows};File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{if(quad)UnityEngine.Object.DestroyImmediate(quad);if(material)UnityEngine.Object.DestroyImmediate(material);cam.targetTexture=oldTarget;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);style.Select(oldId,false);style.SetIntensity(intensity,false);for(int j=0;j<4;j++)style.SetDetail(j,details[j],false);Time.timeScale=time;}
 }

 public static async Task<object> Night(string label="night-final")
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||Directory.Exists(Root+"/"+label))throw new Exception("Fresh owned night capture required");
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.startHour=0;
  try{await Frames(4);return await Capture(label,"",false,false,false,1);}
  finally{Noon();}
 }

 public static object AuditStopped()
 {
  if(Application.isPlaying||BuildPipeline.isBuildingPlayer||File.Exists(Root+"/build/build-running"))throw new Exception("Stopped idle Editor required");
  foreach(var p in Directory.GetFiles(Root+"/save-after-user-stop"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Preserve newer save");
  var scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var expected=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(File.ReadAllText(Root+"/editor-setup.json"));
  bool match=scenes.Select(s=>s.path).SequenceEqual(expected.Select(s=>s.path));var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DarkFairytale.shader");
  var mat=new Material(shader);try{ShaderUtil.CompilePass(mat,0,true);mat.EnableKeyword("_GBUFFER_NORMALS_OCT");ShaderUtil.CompilePass(mat,0,true);}finally{UnityEngine.Object.DestroyImmediate(mat);}
  var report=new{stopped=true,scenes=scenes.Length,paths=scenes.Select(s=>s.path).ToArray(),setupMatches=match,dirty=scenes.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty),ownedFixtures=Resources.FindObjectsOfTypeAll<GameObject>().Count(g=>g.scene.IsValid()&&g.name.StartsWith("Owned art031")),savesUnchanged=true,timeScale=Time.timeScale,shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{severity=m.severity.ToString(),m.message}).ToArray(),editorPid=System.Diagnostics.Process.GetCurrentProcess().Id};File.WriteAllText(Root+"/final-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static object AssignPaintingPreviews()
 {
  foreach(var id in new[]{"realistic","dark-fairytale","origami","monochrome","ink-wash","van-gogh"})
  {
   var path="Assets/Resources/VisualStyles/Previews/"+id+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
   if(id=="ink-wash"||id=="van-gogh")
   {
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
    var preset=AssetDatabase.LoadAssetAtPath<VisualStylePreset>("Assets/Resources/VisualStyles/Presets/"+id+".asset");preset.preview=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(preset);AssetDatabase.SaveAssetIfDirty(preset);
   }
  }
  return new{nativePreviews=6};
 }

 public static async Task<object> DropdownProbe()
 {
  if(!Application.isPlaying||GameSaveManager.HasSavableGame||File.Exists(Root+"/dropdown-probe.json"))throw new Exception("Fresh owned dropdown probe required");
  var cursor=Cursor.lockState;bool visible=Cursor.visible;VisualStyleUIController.Open();var ui=UnityEngine.Object.FindFirstObjectByType<VisualStyleUIController>();
  try
  {
   ui.Comparison.SetStyles("realistic","ink-wash");ui.ShowComparison(true);await Frames(4);ui.Comparison.leftStyle.Show();await Frames(5);
   var list=GameObject.Find("Dropdown List");var scroll=list.GetComponentInChildren<UnityEngine.UI.ScrollRect>();var choices=list.GetComponentsInChildren<UnityEngine.UI.Toggle>(false);var last=choices.Last();var rows=new List<object>();
   foreach(float value in new[]{0f,1f,0f})
   {
    scroll.verticalNormalizedPosition=value;Canvas.ForceUpdateCanvases();await Frames(4);
    var a=new Vector3[4];((RectTransform)last.transform).GetWorldCorners(a);var b=new Vector3[4];scroll.viewport.GetWorldCorners(b);
    rows.Add(new{requested=value,actual=scroll.verticalNormalizedPosition,choices=choices.Length,lastName=last.name,contentRect=scroll.content.rect.ToString(),contentPosition=scroll.content.anchoredPosition.ToString(),lastBottom=a[0].y,lastTop=a[2].y,viewBottom=b[0].y,viewTop=b[2].y});
   }
   var report=new{rows};File.WriteAllText(Root+"/dropdown-probe.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
  }
  finally{ui.Comparison.leftStyle.Hide();ui.Close();Cursor.lockState=cursor;Cursor.visible=visible;}
 }
 public static async Task<object> AwaitReady()
 {
  if(!Application.isPlaying||!GameObject.Find("Owned art031 stage")||GameSaveManager.HasSavableGame)throw new Exception("Owned fixture required");
  var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(w=>w.gameObject.scene.IsValid());var end=DateTime.UtcNow.AddSeconds(90);
  while(!manager.IsReady){if(DateTime.UtcNow>end)throw new Exception("Reload cache readiness timeout");await Frames(2);}
  var group=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).First(g=>g.name.StartsWith("Grass infill"));
  var report=new{ready=true,ownedFixture=true,savable=GameSaveManager.HasSavableGame,styles=VisualStyleManager.Instance.Presets.Count,grassSingleLayout=group.GetLODs().Length==1,editorPid=System.Diagnostics.Process.GetCurrentProcess().Id};File.WriteAllText(Root+"/after-domain-reload.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }

}
