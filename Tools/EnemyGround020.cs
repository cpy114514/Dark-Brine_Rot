using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;

public static class EnemyGround020
{
 const string Root=".codex/enemy020";
 const string Chicken="Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";
 const string Duck="Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab";
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static async Task Frame(){int f=Time.frameCount;var limit=DateTime.UtcNow.AddSeconds(20);while(f==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Native frame timeout");await Task.Delay(5);}}
 public static object Inspect()=>new[]{Chicken,Duck}.Select(p=>{var a=AssetDatabase.LoadAssetAtPath<GameObject>(p);var c=a.GetComponent<CharacterController>();return new{path=p,scale=a.transform.localScale.ToString(),height=c.height,center=c.center.ToString(),radius=c.radius,skin=c.skinWidth,visuals=a.transform.Cast<Transform>().Select(t=>new{t.name,position=t.localPosition.ToString(),scale=t.localScale.ToString()}).ToArray(),animators=a.GetComponentsInChildren<Animator>(true).Select(x=>new{x.name,x.enabled,x.applyRootMotion,x.isHuman,clips=x.runtimeAnimatorController?x.runtimeAnimatorController.animationClips.Select(y=>y?y.name:"null").ToArray():Array.Empty<string>()}).ToArray(),scripts=a.GetComponents<MonoBehaviour>().Select(x=>x.GetType().Name).ToArray()};}).ToArray();
 public static object SampleClips(string label)
 {
  if(Application.isPlaying||File.Exists(Root+"/"+label+"-clips.json"))throw new Exception("Stopped fresh sample required");var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;var bake=new Mesh();try{root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Chicken));SceneManager.MoveGameObjectToScene(root,scene);var ai=root.GetComponent<ChickenBroAI>();ai.enabled=false;var a=root.GetComponentInChildren<Animator>();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;var rows=new List<object>();foreach(string state in new[]{"Idle","Walk","Run","Attack","Lunge","Hit","Heavy","Death"}){a.Rebind();var values=new List<float>();for(int i=0;i<21;i++){a.Play(state,0,i/21f);a.Update(0);values.Add(MeshFoot(ai,bake));}rows.Add(new{state,min=values.Min(),max=values.Max(),average=values.Average(),samples=values});}File.WriteAllText(Root+"/"+label+"-clips.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;}finally{if(root)UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(bake);EditorSceneManager.ClosePreviewScene(scene);}
 }
 public static object ApplyPrefabs()
 {
  if(Application.isPlaying)throw new Exception("Stop Editor first");
  foreach(string path in new[]{Chicken,Duck}){var root=PrefabUtility.LoadPrefabContents(path);try{var visual=root.transform.GetChild(0);var c=root.GetComponent<CharacterController>();if(path==Chicken){if(Mathf.Abs(visual.localPosition.y-.7371f)>.0001f)throw new Exception("Unexpected chicken offset");var p=visual.localPosition;p.y-=.65f;visual.localPosition=p;}else{if(Mathf.Abs(c.height-3.4f)>.001f)throw new Exception("Unexpected duck size");const float scale=1.4f;visual.localScale*=scale;visual.localPosition*=scale;c.height*=scale;c.center*=scale;c.radius*=scale;var ai=root.GetComponent<ChickenBroAI>();ai.meleeRange*=scale;ai.lungeMin*=scale;ai.lungeMax*=scale;ai.attackRadius*=scale;ai.separationRadius*=scale;ai.surroundDistance*=scale;}PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}AssetDatabase.SaveAssets();return Inspect();
 }
 public static object Prepare()
 {
  if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stopped unowned Editor required");var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");
  Directory.CreateDirectory(Root+"/save-before");foreach(string n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"})if(File.Exists(Path.Combine(Application.persistentDataPath,n)))File.Copy(Path.Combine(Application.persistentDataPath,n),Root+"/save-before/"+n,false);
  File.WriteAllText(Root+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));EditorSceneManager.OpenScene("Assets/Scenes/Combat Test/CombatTest.unity",OpenSceneMode.Single);return new{prepared=true};
 }
 public static object Restore()
 {
  if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stop owned test first");foreach(string p in Directory.GetFiles(Root+"/save-before"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Save changed; do not overwrite");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/workspace-before.json")));return new{restored=true,savesUnchanged=true};
 }
 public static object Finish()
 {
  if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stop owned test first");var expected=Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/workspace-before.json"));var setup=EditorSceneManager.GetSceneManagerSetup();bool sceneMatch=expected.Length==setup.Length&&expected.Zip(setup,(a,b)=>a.path==b.path&&a.isActive==b.isActive&&a.isLoaded==b.isLoaded).All(v=>v);bool saves=Directory.GetFiles(Root+"/save-before").All(p=>File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))));if(!sceneMatch||!saves||setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Workspace/save mismatch");var duckController=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.controller");var report=new{stopped=true,sceneMatch,savesUnchanged=saves,markerClear=true,scenes=setup,assets=Inspect(),duckStates=duckController.layers[0].stateMachine.states.Select(c=>new{c.state.name,c.state.speed,clip=c.state.motion.name}).ToArray(),encounters=UnityEngine.Object.FindObjectsByType<ChickenBroEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(e=>new{e.name,chicken=AssetDatabase.GetAssetPath(e.prefab),duck=AssetDatabase.GetAssetPath(e.duck)}).ToArray()};File.WriteAllText(Root+"/final-native-state.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static object Start(string label="before")
 {
  if(!Application.isPlaying||SceneManager.sceneCount!=1||SceneManager.GetActiveScene().name!="CombatTest"||File.Exists(Root+"/running")||File.Exists(Root+"/"+label+".json"))throw new Exception("Fresh isolated CombatTest required");File.WriteAllText(Root+"/running",label);_=Run(label);return new{started=true};
 }
 public static object Regression(string label)
 {
  if(!Application.isPlaying||SceneManager.sceneCount!=1||SceneManager.GetActiveScene().name!="CombatTest"||File.Exists(Root+"/running")||File.Exists(Root+"/"+label+"-regression.json"))throw new Exception("Fresh isolated Play required");File.WriteAllText(Root+"/running",label);_=Check(label);return new{started=true};
 }
 static void Set(ChickenBroAI ai,string field,object value)=>typeof(ChickenBroAI).GetField(field,Flags).SetValue(ai,value);
 static void PlacePlayer(ThirdPersonPlayerController player,Vector3 feet,Quaternion rotation)
 {
  var cc=player.GetComponent<CharacterController>();feet.y-=(cc.center.y-cc.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);player.RestoreSavedPose(feet,rotation);
 }
 public static object FitDuckReach()
 {
  if(Application.isPlaying)throw new Exception("Stop first");var root=PrefabUtility.LoadPrefabContents(Duck);try{var ai=root.GetComponent<ChickenBroAI>();if(Mathf.Abs(ai.meleeRange-3.43f)>.001f)throw new Exception("Unexpected duck reach");ai.meleeRange=3.21f;PrefabUtility.SaveAsPrefabAsset(root,Duck);}finally{PrefabUtility.UnloadPrefabContents(root);}return new{range=3.21f};
 }
 public static object MatchDuckStride()
 {
  if(Application.isPlaying)throw new Exception("Stop first");var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.controller");var states=controller.layers[0].stateMachine.states.Select(c=>c.state).ToArray();foreach(var s in states.Where(s=>s.name=="Walk"||s.name=="Run")){if(Mathf.Abs(s.speed-(s.name=="Walk"?1.77f:1.68f))>.0001f)throw new Exception("Unexpected duck locomotion speed");s.speed/=1.4f;EditorUtility.SetDirty(s);}EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);return states.Select(s=>new{s.name,s.speed,motion=s.motion.name}).ToArray();
 }
 static async Task Check(string label)
 {
  GameObject root=null;float time=Time.timeScale;var rng=UnityEngine.Random.state;var rows=new List<object>();int passed=0;Action<string,bool,object> test=(name,ok,detail)=>{rows.Add(new{name,passed=ok,detail});if(!ok)throw new Exception("Failed "+name);passed++;};
  try
  {
   Time.timeScale=1;for(int i=0;i<4;i++)await Frame();var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(!player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();player.enabled=false;PlacePlayer(player,new Vector3(1000,100,1000),Quaternion.identity);var hp=player.GetComponent<PlayerHealth>();
   root=new GameObject("Owned enemy020 regressions");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(0,19.5f,0);floor.transform.localScale=new Vector3(60,1,60);var ai=Spawn(Chicken,root.transform,new Vector3(0,20,0));
   // Hold a known patrol target beyond the old .8m boundary. Native frames
   // must travel outward before a timed/random target change is allowed.
   UnityEngine.Random.InitState(2020);Set(ai,"patrol",new Vector3(4,20,0));Set(ai,"nextPatrol",Time.time+100);Set(ai,"nextPath",0f);for(int i=0;i<60;i++)await Frame();test("patrol passes old home boundary",ai.transform.position.x>1.2f,new{position=ai.transform.position.ToString()});test("repath does not overwrite patrol timer",(float)typeof(ChickenBroAI).GetField("nextPatrol",Flags).GetValue(ai)>Time.time+90,null);
   ai.Activate(new Vector3(0,20,0),Quaternion.identity,null);ai.GetComponent<CharacterController>().enabled=false;ai.transform.position=new Vector3(9,20,0);ai.GetComponent<CharacterController>().enabled=true;for(int i=0;i<450;i++){await Frame();if(ai.transform.position.x<.8f)break;}test("lost enemy returns inside inner home threshold",ai.transform.position.x<.8f,new{position=ai.transform.position.ToString()});await Frame();test("home hysteresis reset",!(bool)typeof(ChickenBroAI).GetField("returningHome",Flags).GetValue(ai),null);
   Set(ai,"returningHome",true);Set(ai,"nextPatrol",Time.time+100);ai.Activate(new Vector3(-10,20,0),Quaternion.identity,null);test("pooled activation clears old return and patrol target",!(bool)typeof(ChickenBroAI).GetField("returningHome",Flags).GetValue(ai)&&(float)typeof(ChickenBroAI).GetField("nextPatrol",Flags).GetValue(ai)==0f,null);
   foreach(string path in new[]{Chicken,Duck})
   {
    ai.gameObject.SetActive(false);ai=Spawn(path,root.transform,new Vector3(0,20,0));var cc=ai.GetComponent<CharacterController>();for(int i=0;i<5;i++)await Frame();PlacePlayer(player,new Vector3(1,20,0),Quaternion.identity);ai.Activate(new Vector3(0,21,0),Quaternion.identity,null);Set(ai,"player",hp);Set(ai,"nextAttack",Time.time+100);for(int i=0;i<60;i++)await Frame();test(ai.name+" cooldown gravity",ai.transform.position.y<20.15f,new{y=ai.transform.position.y});test(ai.name+" cooldown uses idle animation",ai.GetComponentInChildren<Animator>().GetInteger("Motion")==0,null);
    ai.enabled=false;PlacePlayer(player,new Vector3(0,20,ai.meleeRange-.05f),Quaternion.identity);ai.Activate(new Vector3(0,20,0),Quaternion.identity,null);Set(ai,"player",hp);Set(ai,"nextAttack",Time.time+100);for(int i=0;i<5;i++)await Frame();Physics.SyncTransforms();Vector3 origin=ai.transform.position+Vector3.up*(cc.height*.55f)+Vector3.forward*(cc.radius+.35f);var contacts=Physics.OverlapSphere(origin,ai.attackRadius,~0,QueryTriggerInteraction.Ignore);bool reachable=contacts.Any(c=>c.GetComponentInParent<PlayerHealth>()==hp);test(ai.name+" configured melee edge overlap",reachable,new{range=ai.meleeRange,origin=origin.ToString(),radius=ai.attackRadius,playerHeight=player.GetComponent<CharacterController>().height,contacts=contacts.Select(c=>c.name).ToArray()});
    // Check the real strike-window path, not merely a geometric estimate.
    ai.enabled=true;PlacePlayer(player,new Vector3(0,20,Mathf.Min(1.5f,ai.meleeRange-.3f)),Quaternion.identity);hp.currentHealth=hp.maxHealth;typeof(PlayerHealth).GetField("protectedUntil",Flags).SetValue(hp,0f);typeof(PlayerHealth).GetField("recoveryUntil",Flags).SetValue(hp,0f);ai.Activate(new Vector3(0,20,0),Quaternion.identity,null);Set(ai,"player",hp);Set(ai,"nextAttack",0f);for(int i=0;i<90&&hp.currentHealth>=hp.maxHealth;i++)await Frame();test(ai.name+" real strike damages player",hp.currentHealth<hp.maxHealth,new{hp=hp.currentHealth,mode=ai.Mode.ToString()});PlacePlayer(player,new Vector3(1000,100,1000),Quaternion.identity);
    ai.Activate(new Vector3(0,20,0),Quaternion.identity,null);float before=ai.GetComponent<Health>().currentHealth;ai.ResolveCombatHit(player.gameObject,1,ai.transform.position,CombatHitKind.ChargedHeavy);test(ai.name+" charged hit launches",ai.Mode==ChickenBroAI.Mood.Launch&&ai.GetComponent<Health>().currentHealth<before,null);float peak=ai.transform.position.y;for(int i=0;i<180;i++){await Frame();peak=Mathf.Max(peak,ai.transform.position.y);}test(ai.name+" launch still rises and lands",peak>20.4f&&ai.transform.position.y<20.2f&&ai.Mode!=ChickenBroAI.Mood.Launch,new{peak,y=ai.transform.position.y,mode=ai.Mode.ToString()});
    var slope=GameObject.CreatePrimitive(PrimitiveType.Cube);slope.transform.SetParent(root.transform);slope.transform.position=new Vector3(15,24,0);slope.transform.localScale=new Vector3(12,1,12);slope.transform.rotation=Quaternion.Euler(0,0,12);Physics.SyncTransforms();ai.Activate(new Vector3(15,24.6f,0),Quaternion.identity,null);Set(ai,"patrol",new Vector3(20,24.6f,0));Set(ai,"nextPatrol",Time.time+100);for(int i=0;i<60;i++)await Frame();float surface=24+.5f/Mathf.Cos(12*Mathf.Deg2Rad)+Mathf.Tan(12*Mathf.Deg2Rad)*(ai.transform.position.x-15);test(ai.name+" walks uphill with grounded capsule",ai.transform.position.x>15.75f&&Mathf.Abs(ai.transform.position.y-surface)<.4f&&cc.isGrounded,new{position=ai.transform.position.ToString(),grounded=cc.isGrounded,surface,footGap=ai.transform.position.y-surface});UnityEngine.Object.DestroyImmediate(slope);
    ai.Activate(new Vector3(0,20,0),Quaternion.identity,null);ai.enabled=false;PlacePlayer(player,new Vector3(0,20,1.4f),Quaternion.identity);Set(ai,"player",hp);Set(ai,"nextAttack",0f);for(int i=0;i<5;i++)await Frame();hp.currentHealth=hp.maxHealth;typeof(PlayerHealth).GetField("protectedUntil",Flags).SetValue(hp,0f);typeof(PlayerHealth).GetField("recoveryUntil",Flags).SetValue(hp,0f);Physics.SyncTransforms();typeof(ChickenBroAI).GetMethod("TryBegin",Flags).Invoke(ai,new object[]{ChickenBroAI.Mood.Strike});Set(ai,"actionTime",ai.strikeDuration*ai.strikeWindowStart*.5f);typeof(ChickenBroAI).GetMethod("TickAttack",Flags).Invoke(ai,new object[]{0f,1.4f});test(ai.name+" windup cannot hit",hp.currentHealth==hp.maxHealth,null);Set(ai,"actionTime",ai.strikeDuration*(ai.strikeWindowStart+ai.strikeWindowEnd)*.5f);typeof(ChickenBroAI).GetMethod("TickAttack",Flags).Invoke(ai,new object[]{0f,1.4f});float struckHp=hp.currentHealth;test(ai.name+" active window hits",struckHp<hp.maxHealth,null);typeof(PlayerHealth).GetField("recoveryUntil",Flags).SetValue(hp,0f);typeof(ChickenBroAI).GetMethod("TickAttack",Flags).Invoke(ai,new object[]{0f,1.4f});test(ai.name+" same swing does not hit twice",hp.currentHealth==struckHp,null);PlacePlayer(player,new Vector3(1000,100,1000),Quaternion.identity);
   }
   // Negative controls for the original authored spherical hit volume.
   ai.Activate(new Vector3(0,20,0),Quaternion.identity,null);ai.enabled=false;Set(ai,"player",hp);Set(ai,"nextAttack",0f);PlacePlayer(player,new Vector3(0,35,1.4f),Quaternion.identity);for(int i=0;i<5;i++)await Frame();hp.currentHealth=hp.maxHealth;typeof(PlayerHealth).GetField("protectedUntil",Flags).SetValue(hp,0f);typeof(PlayerHealth).GetField("recoveryUntil",Flags).SetValue(hp,0f);Physics.SyncTransforms();typeof(ChickenBroAI).GetMethod("TryBegin",Flags).Invoke(ai,new object[]{ChickenBroAI.Mood.Strike});Set(ai,"actionTime",ai.strikeDuration*(ai.strikeWindowStart+ai.strikeWindowEnd)*.5f);typeof(ChickenBroAI).GetMethod("TickAttack",Flags).Invoke(ai,new object[]{0f,1.4f});test("raised player outside hit volume remains unharmed",hp.currentHealth==hp.maxHealth,new{hp=hp.currentHealth,player=player.transform.position.ToString(),colliders=player.GetComponentsInChildren<Collider>().Where(c=>c.enabled).Select(c=>new{c.name,center=c.bounds.center.ToString()}).ToArray()});
   PlacePlayer(player,new Vector3(0,20,2.8f),Quaternion.identity);for(int i=0;i<5;i++)await Frame();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(0,22,1.5f);wall.transform.localScale=new Vector3(4,4,.1f);Physics.SyncTransforms();Set(ai,"nextAttack",0f);typeof(ChickenBroAI).GetMethod("TryBegin",Flags).Invoke(ai,new object[]{ChickenBroAI.Mood.Strike});Set(ai,"actionTime",ai.strikeDuration*(ai.strikeWindowStart+ai.strikeWindowEnd)*.5f);typeof(ChickenBroAI).GetMethod("TickAttack",Flags).Invoke(ai,new object[]{0f,2.8f});test("wall still blocks enlarged duck strike",hp.currentHealth==hp.maxHealth,null);
   File.WriteAllText(Root+"/"+label+"-regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{completed=true,passed,rows},Newtonsoft.Json.Formatting.Indented));
  }
  catch(Exception e){File.WriteAllText(Root+"/"+label+"-regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{completed=false,passed,rows,error=e.ToString()},Newtonsoft.Json.Formatting.Indented));}
  finally{if(root)UnityEngine.Object.DestroyImmediate(root);UnityEngine.Random.state=rng;Time.timeScale=time;File.Delete(Root+"/running");EditorApplication.isPlaying=false;}
 }
 static ChickenBroAI Spawn(string path,Transform parent,Vector3 position)
 {
  var holder=new GameObject("Excluded enemy holder");holder.transform.SetParent(parent);holder.SetActive(false);var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),holder.transform);actor.AddComponent<GameSaveExcluded>();actor.GetComponent<ChickenBroAI>().Activate(position,Quaternion.identity,null);holder.SetActive(true);return actor.GetComponent<ChickenBroAI>();
 }
 static float MeshFoot(ChickenBroAI actor,Mesh bake)
 {
  float min=float.PositiveInfinity;foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){bake.Clear();skin.BakeMesh(bake,true);foreach(var v in bake.vertices)min=Mathf.Min(min,skin.transform.TransformPoint(v).y);}if(float.IsInfinity(min)||float.IsNaN(min))throw new Exception("Invalid mesh foot");return min;
 }
 static object Feet(ChickenBroAI actor,Mesh bake)
 {
  var a=actor.GetComponentInChildren<Animator>();return new{actor=actor.name,root=actor.transform.position.ToString(),human=a.isHuman,bones=a.isHuman?new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes}.Select(b=>new{bone=b.ToString(),p=a.GetBoneTransform(b)?a.GetBoneTransform(b).position.ToString():"none"}).ToArray():null,meshes=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s=>{bake.Clear();s.BakeMesh(bake,false);float f=bake.vertices.Min(v=>s.transform.TransformPoint(v).y);bake.Clear();s.BakeMesh(bake,true);return new{s.name,scale=s.transform.lossyScale.ToString(),position=s.transform.position.ToString(),bounds=s.bounds.ToString(),footFalse=f,footTrue=bake.vertices.Min(v=>s.transform.TransformPoint(v).y)};}).ToArray()};
 }
 static async Task Capture(Camera camera,string path)
 {
  bool done=false;Action<ScriptableRenderContext,Camera> cb=(context,c)=>{if(c!=camera||done)return;done=true;var previous=RenderTexture.active;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);try{RenderTexture.active=camera.targetTexture;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);}};RenderPipelineManager.endCameraRendering+=cb;try{for(int i=0;i<20&&!done;i++)await Frame();if(!done)throw new Exception("No native capture");}finally{RenderPipelineManager.endCameraRendering-=cb;}
 }
 static async Task Run(string label)
 {
  GameObject root=null;RenderTexture target=null;Mesh bake=null;UniversalRenderPipelineAsset clone=null;var originalPipeline=QualitySettings.renderPipeline;var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool oldPixel=Pixel360Rendering.IsActive;float oldTime=Time.timeScale;
  try
  {
   for(int i=0;i<4;i++)await Frame();var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(!player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();PlacePlayer(player,new Vector3(1000,100,1000),Quaternion.identity);player.enabled=false;
   Pixel360Rendering.ReleaseCurrent();clone=UnityEngine.Object.Instantiate(source);clone.renderScale=1;QualitySettings.renderPipeline=clone;Time.timeScale=1;
   root=new GameObject("Owned enemy020 native stage");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Raised test floor";floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(0,19.5f,0);floor.transform.localScale=new Vector3(50,1,50);
   var chicken=Spawn(Chicken,root.transform,new Vector3(-2,20,0));var duck=Spawn(Duck,root.transform,new Vector3(5,20,0));bake=new Mesh();
   var cameraGo=new GameObject("Enemy020 camera",typeof(Camera));cameraGo.transform.SetParent(root.transform);var camera=cameraGo.GetComponent<Camera>();camera.transform.position=new Vector3(2,24,10);camera.transform.LookAt(new Vector3(1,21.2f,0));camera.fieldOfView=48;camera.farClipPlane=150;target=new RenderTexture(960,540,24);target.Create();camera.targetTexture=target;
   for(int i=0;i<60;i++)await Frame();var samples=new List<object>();Vector3 prior=chicken.transform.position,lastDelta=Vector3.zero;int reversals=0;
   for(int i=0;i<240;i++)
   {
    await Frame();Vector3 p=chicken.transform.position,delta=Vector3.ProjectOnPlane(p-prior,Vector3.up);if(delta.magnitude>.002f&&lastDelta.magnitude>.002f&&Vector3.Dot(delta.normalized,lastDelta.normalized)<-.5f)reversals++;if(delta.magnitude>.002f)lastDelta=delta;prior=p;
    if(i%8==0)samples.Add(new{frame=i,mode=chicken.Mode.ToString(),x=p.x,y=p.y,z=p.z,yaw=chicken.transform.eulerAngles.y,motorFoot=chicken.GetComponent<CharacterController>().bounds.min.y-20,visualFoot=MeshFoot(chicken,bake)-20,grounded=chicken.GetComponent<CharacterController>().isGrounded,verticalSpeed=(float)typeof(ChickenBroAI).GetField("verticalSpeed",Flags).GetValue(chicken)});if(label=="final-visual"&&(i==0||i==80||i==160))await Capture(camera,Root+"/"+label+"-motion-"+i+".png");
   }
   Time.timeScale=0;await Capture(camera,Root+"/"+label+".png");var feet=new[]{Feet(chicken,bake),Feet(duck,bake)};float chickenFoot=MeshFoot(chicken,bake),duckFoot=MeshFoot(duck,bake);chicken.transform.position+=Vector3.up;float translatedFoot=MeshFoot(chicken,bake);chicken.transform.position-=Vector3.up;
   var proxyControls=new List<object>();foreach(bool scale in new[]{false,true}){var proxies=new List<GameObject>();var meshes=new List<Mesh>();var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();try{foreach(var skin in skins){var mesh=new Mesh();meshes.Add(mesh);skin.BakeMesh(mesh,scale);var proxy=new GameObject("Baked qualification",typeof(MeshFilter),typeof(MeshRenderer));proxies.Add(proxy);proxy.transform.SetParent(skin.transform,false);proxy.GetComponent<MeshFilter>().sharedMesh=mesh;proxy.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;}string path=Root+"/"+label+"-proxy-"+scale+".png";await Capture(camera,path);var a=new Texture2D(2,2);var b=new Texture2D(2,2);try{a.LoadImage(File.ReadAllBytes(Root+"/"+label+".png"));b.LoadImage(File.ReadAllBytes(path));var aa=a.GetPixels32();var bb=b.GetPixels32();int diff=0;for(int i=0;i<aa.Length;i++)if(Mathf.Abs(aa[i].r-bb[i].r)>8||Mathf.Abs(aa[i].g-bb[i].g)>8||Mathf.Abs(aa[i].b-bb[i].b)>8)diff++;proxyControls.Add(new{scale,differentPixels=diff,totalPixels=aa.Length});}finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}}finally{foreach(var s in skins)s.enabled=true;foreach(var p in proxies)UnityEngine.Object.DestroyImmediate(p);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);}}
   object idleFeet=null;if(label=="final-visual"){foreach(var actor in new[]{chicken,duck}){actor.enabled=false;actor.GetComponent<CharacterController>().enabled=false;actor.transform.SetPositionAndRotation(new Vector3(actor==chicken?-2:5,actor==chicken?20.0805f:20.05f,0),Quaternion.identity);var animator=actor.GetComponentInChildren<Animator>();animator.Play("Idle",0,.3f);animator.Update(0);}await Capture(camera,Root+"/"+label+"-idle.png");idleFeet=new{chicken=MeshFoot(chicken,bake)-20,duck=MeshFoot(duck,bake)-20};chicken.enabled=true;}
   PlacePlayer(player,new Vector3(-1,20,1),Quaternion.identity);chicken.Activate(new Vector3(-2,21,0),Quaternion.identity,null);typeof(ChickenBroAI).GetField("player",Flags).SetValue(chicken,player.GetComponent<PlayerHealth>());typeof(ChickenBroAI).GetField("nextAttack",Flags).SetValue(chicken,Time.time+100);Time.timeScale=1;for(int i=0;i<60;i++)await Frame();var cooldown=new{startY=21f,endY=chicken.transform.position.y,grounded=chicken.GetComponent<CharacterController>().isGrounded,mode=chicken.Mode.ToString(),motion=chicken.GetComponentInChildren<Animator>().GetInteger("Motion")};
   File.WriteAllText(Root+"/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(new{captured=true,reversals,samples,chickenHeight=chicken.GetComponent<CharacterController>().height,duckHeight=duck.GetComponent<CharacterController>().height,chickenVisualFoot=chickenFoot-20,duckVisualFoot=duckFoot-20,footTranslationControl=translatedFoot-chickenFoot,proxyControls,idleFeet,cooldown,feet,scope="Actual prefabs, elevated flat floor, player outside awareness, 240 native roaming frames; baked skin feet versus floor qualified with 1m translation and rendered proxy controls; native camera capture; melee-cooldown gravity control"},Newtonsoft.Json.Formatting.Indented));
  }
  catch(Exception e){File.WriteAllText(Root+"/"+label+"-error.txt",e.ToString());}
  finally
  {
   Time.timeScale=oldTime;Pixel360Rendering.ReleaseCurrent();QualitySettings.renderPipeline=originalPipeline;if(oldPixel&&source)Pixel360Rendering.Configure(source,true);if(root)UnityEngine.Object.DestroyImmediate(root);if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(clone)UnityEngine.Object.DestroyImmediate(clone);if(bake)UnityEngine.Object.DestroyImmediate(bake);File.Delete(Root+"/running");EditorApplication.isPlaying=false;
  }
 }
}
