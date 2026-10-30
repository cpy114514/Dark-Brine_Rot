using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using Mavis;

public static class DuckWave026
{
 const string Root=".codex/enemy026";
 const string Duck="Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab";
 const string Controller="Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.controller";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object x,string n)=>x.GetType().GetField(n,F).GetValue(x);
 static void Set(object x,string n,object value)=>x.GetType().GetField(n,F).SetValue(x,value);
 static object Call(object x,string n,params object[] args)=>x.GetType().GetMethod(n,F).Invoke(x,args);
 static async Task Frame(){int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount==f){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frame stalled");await Task.Delay(5);}}
 public static object Prepare()
 {
  if(Application.isPlaying||File.Exists(Root+"/workspace.json"))throw new Exception("Fresh stopped clean workspace required");
  var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");
  Directory.CreateDirectory(Root+"/save-before");foreach(string n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"})if(File.Exists(Path.Combine(Application.persistentDataPath,n)))File.Copy(Path.Combine(Application.persistentDataPath,n),Root+"/save-before/"+n,false);
  Directory.CreateDirectory(Root+"/assets-before");foreach(string path in new[]{Duck,Controller,"Assets/Game/Prefabs/Enemies/ChickenBro/Scripts/ChickenBroAI.cs","Assets/Game/Prefabs/Enemies/ChickenBro/Scripts/ChickenBroEncounter.cs"})File.Copy(path,Root+"/assets-before/"+Path.GetFileName(path),false);
  File.WriteAllText(Root+"/workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));EditorSceneManager.OpenScene("Assets/Scenes/Combat Test/CombatTest.unity",OpenSceneMode.Single);return new{prepared=true};
 }
 public static object Restore()
 {
  if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stop owned fixture first");
  foreach(string p in Directory.GetFiles(Root+"/save-before"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Save changed; preserve newer progress");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/workspace.json")));
  return new{restored=true,stopped=!Application.isPlaying,scenes=EditorSceneManager.GetSceneManagerSetup(),savesUnchanged=true};
 }
 public static object Start(string label)
 {
  if(!Application.isPlaying||SceneManager.sceneCount!=1||SceneManager.GetActiveScene().name!="CombatTest"||File.Exists(Root+"/running")||File.Exists(Root+"/"+label+".json"))throw new Exception("Fresh isolated CombatTest required");
  File.WriteAllText(Root+"/running",label);_=Run(label);return new{started=true,label};
 }
 static Rect ScreenRect(RectTransform r)
 {
  var c=new Vector3[4];r.GetWorldCorners(c);return Rect.MinMaxRect(c.Min(v=>v.x),c.Min(v=>v.y),c.Max(v=>v.x),c.Max(v=>v.y));
 }
 static object Box(Rect r)=>new{x=r.x,y=r.y,width=r.width,height=r.height};
 static async Task Capture(string label)
 {
  string path=Root+"/"+label+".png";if(File.Exists(path))throw new Exception("Preserve image");
  ScreenCapture.CaptureScreenshot(Path.GetFullPath(path));for(int i=0;i<60;i++){await Frame();if(File.Exists(path)&&new FileInfo(path).Length>100)return;}throw new Exception("Native GameView screenshot missing");
 }
 static async Task Run(string label)
 {
  GameObject root=null;Camera own=null;BossHudPanel hud=null;
  var cameras=Camera.allCameras.ToDictionary(c=>c,c=>c.enabled);float time=Time.timeScale;
  var oldPipeline=QualitySettings.renderPipeline;var source=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;UniversalRenderPipelineAsset clone=null;bool pixel=Pixel360Rendering.IsActive;
  var rows=new List<object>();Action<bool,string,object> test=(ok,name,detail)=>rows.Add(new{ok,name,detail});
  try
  {
   var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
   Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(source);clone.renderScale=1;QualitySettings.renderPipeline=clone;Time.timeScale=1;
   for(int i=0;i<5;i++)await Frame();
   var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(!player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();
   var pc=player.GetComponent<CharacterController>();player.RestoreSavedPose(new Vector3(-4,20-(pc.center.y-pc.height*.5f)*Mathf.Abs(player.transform.lossyScale.y),0),Quaternion.identity);player.enabled=false;player.GetComponent<PlayerHealth>()?.GrantProtection(120);
   root=new GameObject("Owned duck wave026 stage");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(0,19.5f,0);floor.transform.localScale=new Vector3(100,1,100);
   var holder=new GameObject("Excluded duck holder");holder.transform.SetParent(root.transform);holder.SetActive(false);var duck=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Duck),holder.transform);duck.AddComponent<GameSaveExcluded>();holder.SetActive(true);
   var ai=duck.GetComponent<ChickenBroAI>();ai.Activate(new Vector3(4,20.05f,0),Quaternion.identity,null);ai.enabled=false;var animator=duck.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var eGo=new GameObject("Owned caption",typeof(ChickenBroEncounter));eGo.transform.SetParent(root.transform);var encounter=eGo.GetComponent<ChickenBroEncounter>();encounter.enabled=false;
   hud=new BossHudPanel(root.transform,"Layout reference");((CanvasGroup)Get(hud,"group")).alpha=1;
   foreach(var c in cameras.Keys)if(c)c.enabled=false;
   own=new GameObject("Owned native view",typeof(Camera)).GetComponent<Camera>();own.transform.SetParent(root.transform);own.tag="MainCamera";own.transform.position=new Vector3(2,27,19);own.transform.LookAt(new Vector3(0,23.6f,0));own.fieldOfView=48;own.farClipPlane=150;
   Call(encounter,"Say","Wave 2  ·  Chicken Bros and long-legged ducks arrive.",100f);
   var caption=(Text)Get(encounter,"caption");var card=(RectTransform)caption.transform.parent;
   for(int i=0;i<12;i++)await Frame();animator.Play("Idle",0,.3f);animator.Update(0);
   Canvas.ForceUpdateCanvases();var cr=ScreenRect(card);var tr=ScreenRect(caption.rectTransform);var boss=ScreenRect(((CanvasGroup)Get(hud,"group")).transform.GetChild(0) as RectTransform);
   test(cr.Contains(tr.min)&&cr.Contains(tr.max),"Wave text stays inside its card",new{card=Box(cr),text=Box(tr)});
   test(!cr.Overlaps(boss),"Wave card clears the encounter HUD",new{card=Box(cr),boss=Box(boss)});
   test(!caption.raycastTarget,"Caption remains noninteractive",null);
   await Capture(label+"-size-ui");
   var body=ai.GetComponent<CharacterController>();test(body.height>pc.height*Mathf.Abs(player.transform.lossyScale.y),"Duck is visibly taller than scaled Sahur capsule",new{duck=body.height,player=pc.height*Mathf.Abs(player.transform.lossyScale.y)});
   var stages=new List<object>();
   foreach(var entry in new[]{new{label="slow-chase",speed=ai.walkSpeed,motion=1},new{label="run",speed=ai.runSpeed,motion=2},new{label="stationary",speed=0f,motion=0}})
   {
    var at=new Vector3(4,20.05f,0);ai.Activate(at,Quaternion.identity,null);ai.enabled=false;
    for(int i=0;i<30;i++)
    {
     typeof(ChickenBroAI).GetProperty("Mode").SetValue(ai,ChickenBroAI.Mood.Chase);
     Call(ai,"MoveToward",at+Vector3.forward*40,entry.speed,Time.deltaTime,false);Call(ai,"Present");await Frame();
     if(entry.label=="stationary"&&i==0)test(animator.GetInteger("Motion")==0,"Stopping immediately selects Idle instead of residual stepping",null);
     if(i==0||i==4||i==10||i==20||i==29)stages.Add(new{entry.label,frame=i,motion=animator.GetInteger("Motion"),speed=animator.GetFloat("Speed"),state=animator.GetCurrentAnimatorStateInfo(0).shortNameHash,transition=animator.IsInTransition(0)});
    }
    test(animator.GetInteger("Motion")==entry.motion,entry.label+" selects the matching locomotion",new{actual=animator.GetInteger("Motion"),expected=entry.motion,speed=animator.GetFloat("Speed")});
    string expectedState=entry.label=="slow-chase"?"Walk":entry.label=="run"?"Run":"Idle";
    test(animator.GetCurrentAnimatorStateInfo(0).shortNameHash==Animator.StringToHash(expectedState)&&!animator.IsInTransition(0),entry.label+" settles in the actual "+expectedState+" animation",null);
    await Capture(label+"-"+entry.label);
   }
   test(Mathf.Abs(ai.strikeDuration-.53f)<.001f && Mathf.Abs(ai.lungeDuration-.8f)<.001f,"Observed original duck attack durations remain",new{ai.strikeDuration,ai.lungeDuration});
   var controller=(AnimatorController)animator.runtimeAnimatorController;var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();
   test(Mathf.Abs(states.Single(s=>s.name=="Walk").speed*1.5f-1.26428568f)<.001f,"Enlarged walk retains world stride rate",null);
   test(Mathf.Abs(states.Single(s=>s.name=="Run").speed*1.5f-1.19999993f)<.001f,"Enlarged run retains world stride rate",null);
   test(controller.layers[0].stateMachine.anyStateTransitions.All(t=>!t.canTransitionToSelf),"Held motion cannot restart its own animation",null);
   var chicken=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab").GetComponent<ChickenBroAI>();
   test(!(bool)typeof(ChickenBroAI).GetField("matchLocomotionToMovement").GetValue(chicken),"Existing chicken animation policy remains unchanged",null);
   var hp=player.GetComponent<PlayerHealth>();
   Action<Vector3> place=feet=>{feet.y-=(pc.center.y-pc.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);player.RestoreSavedPose(feet,Quaternion.identity);};
   foreach(bool circling in new[]{true,false})
   {
    var group=circling?new ChickenBroFlock{maxAttackers=0}:null;
    ai.Activate(new Vector3(4,20.05f,0),Quaternion.identity,group);ai.enabled=false;group?.Add(ai);Set(ai,"player",hp);Set(ai,"nextAttack",Time.time+100);place(new Vector3(4,20,16));Physics.SyncTransforms();
    for(int i=0;i<30;i++){Call(ai,"Decide",Vector3.Distance(ai.transform.position,player.transform.position),Time.deltaTime);Call(ai,"Present");await Frame();}
    test(animator.GetInteger("Motion")== (circling?1:2),circling?"Actual occupied-slot surround decision selects Walk":"Actual chase decision selects Run",new{motion=animator.GetInteger("Motion"),mode=ai.Mode.ToString(),speed=animator.GetFloat("Speed")});group?.Close();
   }
   foreach(var action in new[]{new{mode=ChickenBroAI.Mood.Idle,motion=0},new{mode=ChickenBroAI.Mood.Strike,motion=3},new{mode=ChickenBroAI.Mood.Lunge,motion=4},new{mode=ChickenBroAI.Mood.Flinch,motion=5},new{mode=ChickenBroAI.Mood.Launch,motion=6},new{mode=ChickenBroAI.Mood.Knockdown,motion=6},new{mode=ChickenBroAI.Mood.Dead,motion=7}})
   {typeof(ChickenBroAI).GetProperty("Mode").SetValue(ai,action.mode);animator.SetFloat("Speed",1);Call(ai,"Present");test(animator.GetInteger("Motion")==action.motion,"Action/reaction overrides stale locomotion: "+action.mode,null);}
   var chick=UnityEngine.Object.Instantiate(chicken.gameObject,root.transform);chick.AddComponent<GameSaveExcluded>();var ca=chick.GetComponent<ChickenBroAI>();ca.enabled=false;var can=chick.GetComponentInChildren<Animator>();
   typeof(ChickenBroAI).GetProperty("Mode").SetValue(ca,ChickenBroAI.Mood.Patrol);Call(ca,"Present");test(can.GetInteger("Motion")==1,"Default chicken patrol retains Walk",null);
   typeof(ChickenBroAI).GetProperty("Mode").SetValue(ca,ChickenBroAI.Mood.Chase);Call(ca,"Present");test(can.GetInteger("Motion")==2,"Default chicken chase retains Run",null);
   Action reset=()=>{hp.currentHealth=hp.maxHealth;typeof(PlayerHealth).GetField("protectedUntil",F).SetValue(hp,0f);typeof(PlayerHealth).GetField("recoveryUntil",F).SetValue(hp,0f);((HashSet<PlayerHealth>)Get(ai,"struck")).Clear();};
   ai.Activate(new Vector3(4,20.05f,0),Quaternion.identity,null);ai.enabled=false;Set(ai,"player",hp);Set(ai,"nextAttack",0f);place(new Vector3(4,20,4.3f));Physics.SyncTransforms();for(int i=0;i<4;i++)await Frame();
   bool began=(bool)Call(ai,"TryBegin",ChickenBroAI.Mood.Strike);reset();Call(ai,"Strike");float afterHit=hp.currentHealth;
   test(began&&afterHit<hp.maxHealth,"Enlarged original sphere reaches scaled Sahur at close range",new{health=afterHit,max=hp.maxHealth});
   Call(ai,"Strike");test(hp.currentHealth==afterHit,"Original one-hit-per-action guard remains",null);
   place(new Vector3(4,20,20));Physics.SyncTransforms();for(int i=0;i<2;i++)await Frame();reset();Call(ai,"Strike");test(hp.currentHealth==hp.maxHealth,"Distant target remains outside strike",null);
   place(new Vector3(4,20,4.3f));var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(4,25,2.7f);wall.transform.localScale=new Vector3(8,10,.15f);Physics.SyncTransforms();for(int i=0;i<2;i++)await Frame();reset();Call(ai,"Strike");test(hp.currentHealth==hp.maxHealth,"Cover still blocks the original strike",null);UnityEngine.Object.DestroyImmediate(wall);
   var report=new{label,completed=true,rows,passed=rows.All(r=>(bool)r.GetType().GetProperty("ok").GetValue(r)),stages,screen=new{Screen.width,Screen.height},duckHeight=body.height,playerHeight=pc.height*Mathf.Abs(player.transform.lossyScale.y)};
   File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  }
  catch(Exception error){File.WriteAllText(Root+"/"+label+"-error.txt",error.ToString());}
  finally
  {
   hud?.Dispose();if(root)UnityEngine.Object.DestroyImmediate(root);foreach(var p in cameras)if(p.Key)p.Key.enabled=p.Value;
   Time.timeScale=time;Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=oldPipeline;if(pixel&&source)Pixel360Rendering.Configure(source,true);if(clone)UnityEngine.Object.DestroyImmediate(clone);
   File.Delete(Root+"/running");EditorApplication.isPlaying=false;
  }
 }
 public static object ApplyDuck()
 {
  if(Application.isPlaying)throw new Exception("Stop before asset changes");var root=PrefabUtility.LoadPrefabContents(Duck);
  try
  {
   var c=root.GetComponent<CharacterController>();if(Mathf.Abs(c.height-4.76f)>.001f)throw new Exception("Unexpected prior duck size");
   const float k=1.5f;var visual=root.transform.GetChild(0);visual.localScale*=k;visual.localPosition*=k;c.height*=k;c.center*=k;c.radius*=k;
   var ai=root.GetComponent<ChickenBroAI>();ai.meleeRange*=k;ai.lungeMin*=k;ai.lungeMax*=k;ai.attackRadius*=k;ai.separationRadius*=k;ai.surroundDistance*=k;
   var locomotion=typeof(ChickenBroAI).GetField("matchLocomotionToMovement");if(locomotion==null)throw new Exception("Compile animation correction first");locomotion.SetValue(ai,true);
   PrefabUtility.SaveAsPrefabAsset(root,Duck);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);foreach(var child in controller.layers[0].stateMachine.states)if(child.state.name=="Walk"||child.state.name=="Run")child.state.speed/=1.5f;
  foreach(var transition in controller.layers[0].stateMachine.anyStateTransitions)if(transition.destinationState&&(transition.destinationState.name=="Idle"||transition.destinationState.name=="Walk"||transition.destinationState.name=="Run"))transition.duration=.16f;
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);return new{applied=true,height=7.14f,ratio=1.5f};
 }
 public static object Finish()
 {
  if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stop owned work first");
  var setup=EditorSceneManager.GetSceneManagerSetup();var expected=Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/workspace.json"));
  bool scenes=setup.Length==expected.Length&&setup.Zip(expected,(a,b)=>a.path==b.path&&a.isLoaded==b.isLoaded&&a.isActive==b.isActive).All(x=>x);
  bool saves=Directory.GetFiles(Root+"/save-before").All(p=>File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))));
  if(!scenes||!saves||setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Workspace/save mismatch");
  var root=AssetDatabase.LoadAssetAtPath<GameObject>(Duck);var c=root.GetComponent<CharacterController>();var ai=root.GetComponent<ChickenBroAI>();var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
  var result=new{stopped=true,sceneMatch=scenes,savesUnchanged=saves,markerClear=true,scenes=setup,height=c.height,c.radius,center=c.center.ToString(),ai.matchLocomotionToMovement,ai.walkSpeed,ai.runSpeed,ai.strikeDuration,ai.lungeDuration,ai.strikeDamage,ai.lungeDamage,
   states=controller.layers[0].stateMachine.states.Select(x=>new{x.state.name,x.state.speed,clip=x.state.motion.name}).ToArray(),
   transitions=controller.layers[0].stateMachine.anyStateTransitions.Select(x=>new{state=x.destinationState.name,x.duration,x.hasFixedDuration,x.canTransitionToSelf}).ToArray()};
  File.WriteAllText(Root+"/final-native-state.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
