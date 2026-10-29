using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;
using Mavis.VisualStyles;
public static class VerifyVisualStyleRenderingV2
{
 const string Root=".codex/visual-style-v2";
 static Keyboard keyboard;static Mouse mouse;static bool walk,left;static InputSettings settings,temporary;
 static Keyboard previousKeyboard;static Mouse previousMouse;static HideFlags previousFlags;
 static readonly List<InputDevice> physical=new List<InputDevice>();
 static void Input(){if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keyboard,walk?new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Forward)):new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left));}
 static void CleanInput(){InputSystem.onBeforeUpdate-=Input;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;if(settings){InputSystem.settings=settings;settings.hideFlags=previousFlags;}settings=null;if(temporary)UnityEngine.Object.DestroyImmediate(temporary);temporary=null;foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);physical.Clear();if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();AssemblyReloadEvents.beforeAssemblyReload-=CleanInput;EditorApplication.playModeStateChanged-=Stopped;}
 static void Stopped(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)CleanInput();}
 static async Task Frame(){int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(30);while(f==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frame interrupted");await Task.Delay(5);}}
 static async Task<Color32[]> Capture(Camera camera,string name)
 {
  Color32[] pixels=null;Action<ScriptableRenderContext,Camera> cb=(context,cam)=>{if(cam!=camera||pixels!=null)return;var rt=RenderTexture.active;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);try{RenderTexture.active=camera.targetTexture;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();pixels=tex.GetPixels32();File.WriteAllBytes(Root+"/"+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=rt;UnityEngine.Object.DestroyImmediate(tex);}};
  RenderPipelineManager.endCameraRendering+=cb;try{for(int i=0;i<40&&pixels==null;i++)await Frame();if(pixels==null)throw new Exception("Capture failed: "+name);return pixels;}finally{RenderPipelineManager.endCameraRendering-=cb;}
 }
 public static object PrepareLaunchProbe(){if(Application.isPlaying)throw new Exception("Stopped Editor required");var state=new{hasStyle=PlayerPrefs.HasKey(VisualStyleSettings.StyleKey),hasIntensity=PlayerPrefs.HasKey(VisualStyleSettings.IntensityKey),style=VisualStyleSettings.SelectedId,intensity=PlayerPrefs.GetFloat(VisualStyleSettings.IntensityKey,.85f)};File.WriteAllText(Root+"/launch-preferences-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(state));VisualStyleSettings.Save("dark-fairytale",.72f);return new{prepared=true};}
 public static object RestoreLaunchProbe(){var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/launch-preferences-before.json"));if((bool)state["hasStyle"])PlayerPrefs.SetString(VisualStyleSettings.StyleKey,(string)state["style"]);else PlayerPrefs.DeleteKey(VisualStyleSettings.StyleKey);if((bool)state["hasIntensity"])PlayerPrefs.SetFloat(VisualStyleSettings.IntensityKey,(float)state["intensity"]);else PlayerPrefs.DeleteKey(VisualStyleSettings.IntensityKey);PlayerPrefs.Save();return new{restored=true};}
 public static object Start(){if(!Application.isPlaying||!UnityEngine.SceneManagement.SceneManager.GetSceneByName("CombatTest").isLoaded||File.Exists(Root+"/rendering-running"))throw new Exception("Unowned CombatTest Play required");File.Delete(Root+"/rendering-error.txt");File.Delete(Root+"/rendering-verification.json");File.WriteAllText(Root+"/rendering-running",DateTime.UtcNow.ToString("o"));_=Run();return new{started=true};}
 static async Task Run()
 {
  var checks=new List<string>();void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
  bool passed=false;var manager=VisualStyleManager.Instance;string oldStyle=manager.Current.id;float oldIntensity=manager.Intensity;var original=QualitySettings.renderPipeline;var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  bool pixelBefore=Pixel360Rendering.IsActive;float clock=Time.timeScale;var cursor=Cursor.lockState;bool cursorVisible=Cursor.visible;
  var originalInputSettings=InputSystem.settings;var originalInputFlags=originalInputSettings.hideFlags;var originalKeyboard=Keyboard.current;var originalMouse=Mouse.current;var originalDeviceStates=InputSystem.devices.Select(d=>(d,d.enabled)).ToArray();
  GameObject root=null;RenderTexture target=null;UniversalRenderPipelineAsset testPipeline=null,mobile=null;Material material=null;
  try
  {
   Check(manager.Current.id=="origami"&&Mathf.Abs(manager.Intensity-.72f)<.001f,"A new Play session restores the previously saved style and intensity before any selection");
   Pixel360Rendering.ReleaseCurrent();testPipeline=UnityEngine.Object.Instantiate(source);testPipeline.renderScale=1;testPipeline.msaaSampleCount=1;QualitySettings.renderPipeline=testPipeline;
   var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();Check(player,"Actual Sahur exists");var attack=player.GetComponent<SahurAttack>();var loadout=player.GetComponent<SahurWeaponLoadout>();loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick);
   previousKeyboard=Keyboard.current;previousMouse=Mouse.current;settings=InputSystem.settings;previousFlags=settings.hideFlags;if(previousFlags==HideFlags.HideAndDontSave)settings.hideFlags&=~HideFlags.NotEditable;temporary=UnityEngine.Object.Instantiate(settings);temporary.hideFlags=HideFlags.DontSave;temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
   foreach(var d in InputSystem.devices.ToArray())if((d is Keyboard||d is Mouse)&&d.enabled){physical.Add(d);InputSystem.DisableDevice(d);}keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();var input=player.GetComponent<PlayerInput>();UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(keyboard,input.user);UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();InputSystem.onBeforeUpdate+=Input;AssemblyReloadEvents.beforeAssemblyReload+=CleanInput;EditorApplication.playModeStateChanged+=Stopped;
   Time.timeScale=1;
   foreach(string id in new[]{"realistic","dark-fairytale","origami","monochrome"})
   {
    manager.Select(id,false);walk=false;left=false;for(int i=0;i<8;i++)await Frame();Vector3 before=player.transform.position;walk=true;for(int i=0;i<16;i++)await Frame();walk=false;
    Check(Vector3.ProjectOnPlane(player.transform.position-before,Vector3.up).magnitude>.1f,id+" receives real queued movement input");
    left=true;for(int i=0;i<3;i++)await Frame();left=false;
    Check(attack.IsCombatMotionActive,id+" has a real active action before changing its look");
    var animator=player.CharacterAnimator;var animationBefore=animator.GetCurrentAnimatorStateInfo(0);var positionBefore=player.transform.position;
    manager.Select(id=="monochrome"?"origami":"monochrome",false);var animationAfter=animator.GetCurrentAnimatorStateInfo(0);
    Check(animationBefore.fullPathHash==animationAfter.fullPathHash&&animationBefore.normalizedTime==animationAfter.normalizedTime&&positionBefore==player.transform.position,id+" switches style without moving or restarting its active animation");manager.Select(id,false);
    bool sawAttack=false;for(int i=0;i<20;i++){await Frame();sawAttack|=attack.IsCombatMotionActive;}Check(sawAttack,id+" receives real queued melee attack input");for(int i=0;i<45;i++)await Frame();
   }
   CleanInput();
   root=new GameObject("Temporary style renderer verification");root.AddComponent<GameSaveExcluded>();var go=new GameObject("Actual scene render probe",typeof(Camera));go.transform.SetParent(root.transform);var camera=go.GetComponent<Camera>();
   Vector3 center=player.GetComponent<CharacterController>().bounds.center;camera.transform.position=center+new Vector3(8,4,10);camera.transform.LookAt(center);camera.fieldOfView=50;camera.farClipPlane=200;
   target=new RenderTexture(960,540,24);target.Create();camera.targetTexture=target;
   var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Transparent clarity probe";quad.transform.SetParent(root.transform);quad.transform.position=camera.ViewportToWorldPoint(new Vector3(.2f,.75f,6));quad.transform.rotation=camera.transform.rotation;quad.transform.localScale=Vector3.one*1.2f;
   material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.1f,.8f,1,.94f));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;material.SetOverrideTag("RenderType","Transparent");quad.GetComponent<Renderer>().sharedMaterial=material;
   var effects=root.AddComponent<CombatImpactVisuals>();effects.Impact(new CombatHitEvent(player.gameObject,player,center+Vector3.up,-camera.transform.forward,CombatHitKind.ChargedHeavy,CombatHitResult.HeavyHit,0),CombatFeelProfile.Default.heavy,CombatMaterial.Flesh,1);
   for(int i=0;i<5;i++)await Frame();Time.timeScale=0;Check(effects.ActivePulseCount>0,"Existing combat pressure effect is active during captures");
   foreach(bool pixel in new[]{false,true})
   {
    Pixel360Rendering.Configure(testPipeline,pixel);var images=new List<Color32[]>();
    foreach(string id in new[]{"realistic","dark-fairytale","origami","monochrome"}){manager.Select(id,false);manager.SetIntensity(.85f,false);await Frame();images.Add(await Capture(camera,(pixel?"pixel360-":"native-")+id));Check(Pixel360Rendering.IsActive==pixel,"Resolution remains independent during "+id+" capture");}
    double difference=images[0].Zip(images[1],(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))/3).Average();Check(difference>1,"Actual rendered styles differ in "+(pixel?"360p":"native"));
    int probe=(int)(.75f*540)*960+(int)(.2f*960);var a=images[0][probe];var b=images[1][probe];var paper=images[2][probe];var mono=images[3][probe];
    Check(Math.Max(Math.Abs(a.r-b.r),Math.Max(Math.Abs(a.g-b.g),Math.Abs(a.b-b.b)))<10,"Transparent foreground survives Dark Fairytale in "+(pixel?"pixel":"native"));
    Check(Math.Max(Math.Abs(a.r-paper.r),Math.Max(Math.Abs(a.g-paper.g),Math.Abs(a.b-paper.b)))<10,"Transparent foreground survives Origami in "+(pixel?"pixel":"native"));
    Check(Math.Abs(mono.r-mono.g)<=1&&Math.Abs(mono.r-mono.b)<=1&&mono.r>40,"Transparent foreground stays visible and gray in Monochrome in "+(pixel?"pixel":"native"));
    Check(images[3].All(c=>Math.Abs(c.r-c.g)<=1&&Math.Abs(c.r-c.b)<=1),"Whole world including transparent effects is monochrome in "+(pixel?"pixel":"native"));
    double paperDifference=images[0].Zip(images[2],(x,y)=>(double)(Math.Abs(x.r-y.r)+Math.Abs(x.g-y.g)+Math.Abs(x.b-y.b))/3).Average();Check(paperDifference>1,"Actual folded-paper rendering differs from realistic in "+(pixel?"pixel":"native"));
   }
   Pixel360Rendering.ReleaseCurrent();
   manager.Select("origami",false);manager.SetDetail(1,0,false);await Frame();var flatPaper=await Capture(camera,"paper-fold-zero");manager.SetDetail(1,1,false);await Frame();var foldedPaper=await Capture(camera,"paper-fold-full");
   Check(flatPaper.Zip(foldedPaper,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))/3).Average()>.1,"Paper fold control updates actual rendered shading and cached uniforms");
   manager.Select("monochrome",false);manager.SetDetail(0,0,false);await Frame();var softFilm=await Capture(camera,"film-contrast-low");manager.SetDetail(0,1,false);await Frame();var hardFilm=await Capture(camera,"film-contrast-high");
   Check(softFilm.Zip(hardFilm,(a,b)=>(double)Math.Abs(a.r-b.r)).Average()>1,"Film contrast control updates actual tonal response and cached uniforms");
   Pixel360Rendering.ReleaseCurrent();
   var pressureRenderers=effects.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Pooled pressure ring")&&r.enabled).ToArray();Check(pressureRenderers.Length>0,"Existing pressure rings have live enabled renderers");
   foreach(string style in new[]{"origami","monochrome"})
   {
    manager.Select(style,false);await Frame();var withPressure=await Capture(camera,"pressure-on-"+style);
    foreach(var r in pressureRenderers)r.forceRenderingOff=true;await Frame();var withoutPressure=await Capture(camera,"pressure-off-"+style);
    foreach(var r in pressureRenderers)r.forceRenderingOff=false;await Frame();var restoredPressure=await Capture(camera,"pressure-restored-"+style);
    int changed=withPressure.Zip(withoutPressure,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)).Count(d=>d>12);
    double restored=withPressure.Zip(restoredPressure,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))/3).Average();
    Check(changed>20&&restored<.5,"Actual transparent pressure shockwaves contribute visible pixels in "+style+" with reversible off/on control");
   }
   Pixel360Rendering.ReleaseCurrent();mobile=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset"));mobile.renderScale=1;mobile.msaaSampleCount=1;QualitySettings.renderPipeline=mobile;foreach(string style in new[]{"dark-fairytale","origami","monochrome"}){manager.Select(style,false);await Frame();var forward=await Capture(camera,"mobile-forward-"+style);Check(forward.Any(c=>c.r>20||c.g>20||c.b>20),"Forward/mobile renderer produces "+style);if(style=="monochrome")Check(forward.All(c=>Math.Abs(c.r-c.g)<=1&&Math.Abs(c.r-c.b)<=1),"Forward/mobile renderer preserves monochrome");}passed=true;
  }
  catch(Exception error){File.WriteAllText(Root+"/rendering-error.txt",error.ToString());}
  finally
  {
   CleanInput();Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=original;if(pixelBefore&&source)Pixel360Rendering.Configure(source,true);
   manager.Select(oldStyle,false);manager.SetIntensity(oldIntensity,false);Time.timeScale=clock;Cursor.lockState=cursor;Cursor.visible=cursorVisible;
   if(root)UnityEngine.Object.DestroyImmediate(root);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(material)UnityEngine.Object.DestroyImmediate(material);if(testPipeline)UnityEngine.Object.DestroyImmediate(testPipeline);if(mobile)UnityEngine.Object.DestroyImmediate(mobile);
   File.WriteAllText(Root+"/rendering-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,inputSettingsRestored=InputSystem.settings==originalInputSettings&&originalInputSettings.hideFlags==originalInputFlags,currentDevicesRestored=Keyboard.current==originalKeyboard&&Mouse.current==originalMouse,deviceStatesRestored=originalDeviceStates.All(p=>p.d.added&&p.d.enabled==p.enabled)},Newtonsoft.Json.Formatting.Indented));File.Delete(Root+"/rendering-running");EditorApplication.isPlaying=false;
  }
 }
}
