using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;
using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using Mavis;
public static class ReviewCombatVfxGameplay
{
 public static object Open(){if(Application.isPlaying)throw new Exception("Stopped Editor required");if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve dirty scenes");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;return "Disposable native control review";}
 public static object Start(string label,int hz=60,string scope="all",string preset="")
 {if(!Application.isPlaying)throw new Exception("Play required");if(scope=="terrain"||scope=="regression-terrain")foreach(var name in new[]{"Gameplay","Enemies","Main"}){var scene=SceneManager.GetSceneByName(name);if(scene.isLoaded)foreach(var originalRoot in scene.GetRootGameObjects())originalRoot.SetActive(false);}var dir=".codex/vfx-quality/"+label;if(Directory.Exists(dir))throw new Exception("Preserve earlier evidence");var go=new GameObject("Sahur control review 072");go.AddComponent<GameSaveExcluded>();var r=go.AddComponent<CombatVfxGameplayReview>();r.label=label;r.hz=hz;r.scope=scope;r.preset=preset;r.Begin();return new{started=dir};}
 public static object OpenTerrain()
 {
  if(Application.isPlaying)throw new Exception("Stopped Editor required");
  var source=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.name=="Gameplay");
  File.WriteAllText(".codex/vfx-quality/actual-scene-config.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=source.gameObject.scene.path,source.moveSpeed,source.sprintMultiplier,source.acceleration,source.deceleration,source.turnSpeedDegrees,source.rollDuration,source.cameraHeight,source.cameraDistance,scale=source.transform.lossyScale.ToString(),movement=EditorJsonUtility.ToJson(source),attack=EditorJsonUtility.ToJson(source.GetComponent<SahurAttack>())},Newtonsoft.Json.Formatting.Indented));
  EditorApplication.isPlaying=true;return "Actual terrain + copied scene movement configuration";
 }
 public static object Batch()
 {
  _=RunBatch();return "Native weapon, muzzle/contact and staff-return regression";
 }
 static async System.Threading.Tasks.Task RunBatch()
 {
  var definitions=new[]{new[]{"final-native-weapons","60","weapons",""},new[]{"final-native-rifle","60","rifle-vfx",""},new[]{"final-native-staff","60","staff",""}};
  try
  {
   foreach(var row in definitions)
   {
    Start(row[0],int.Parse(row[1]),row[2],row[3]);var deadline=DateTime.UtcNow.AddMinutes(4);string result=".codex/vfx-quality/"+row[0]+"/result.json";
    while(!File.Exists(result)){if(DateTime.UtcNow>deadline||!Application.isPlaying)throw new Exception("Batch interrupted: "+row[0]);await System.Threading.Tasks.Task.Delay(100);}
    await System.Threading.Tasks.Task.Delay(200);
   }
   File.WriteAllText(".codex/vfx-quality/batch-complete.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=true,definitions}));
  }catch(Exception e){File.WriteAllText(".codex/vfx-quality/batch-failure.txt",e.ToString());}
 }
 public static object RestoreScenes(string snapshotPath=".codex/vfx-quality/workspace.json"){if(Application.isPlaying)throw new Exception("Stop first");var j=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(snapshotPath));EditorSceneManager.RestoreSceneManagerSetup(j["scenes"].ToObject<SceneSetup[]>());EditorApplication.isPaused=false;Time.timeScale=1;Time.captureDeltaTime=0;Cursor.lockState=(CursorLockMode)Enum.Parse(typeof(CursorLockMode),(string)j["cursor"]);Cursor.visible=(bool)j["visible"];return "Restored workspace scenes";}
}
[DefaultExecutionOrder(32500)]public sealed class CombatVfxGameplayReview:MonoBehaviour
{
 public string label,scope,preset;public int hz;string dir,section="setup",previousSavePath;int frame,shot;bool cleaned,recording;
 readonly List<object> rows=new List<object>(),checks=new List<object>();readonly Dictionary<string,object> metrics=new Dictionary<string,object>();readonly List<string> errors=new List<string>();
 Keyboard keyboard;Mouse mouse;Key[] keys=Array.Empty<Key>();bool left,right;Vector2 look;readonly List<InputDevice> disabled=new List<InputDevice>();InputSettings oldSettings,settings;HideFlags oldFlags;string oldSettingsJson;float oldCapture;int oldVSync,oldTarget;CursorLockMode oldCursor;bool oldVisible;
 GameObject root,actor;ThirdPersonPlayerController motor;SahurAttack attack;SahurWeaponLoadout loadout;Animator anim;CharacterController body;new Camera camera;RenderTexture rt;Texture2D image;Vector3 floor=new Vector3(20000,1000,20000);Quaternion heading;CombatFeelProfile feel;object previousPreset;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 float GetFloat(string name)=>(float)typeof(ThirdPersonPlayerController).GetField(name,F).GetValue(motor);
 int GetState()=>(int)typeof(ThirdPersonPlayerController).GetField("motionState",F).GetValue(motor);
 void Check(bool ok,string name,object evidence=null){checks.Add(new{ok,name,evidence});}
 void Log(string s,string t,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(s);}
 void Feed(){if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState{delta=look}.WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));Cursor.lockState=CursorLockMode.Locked;}
 public void Begin(){dir=".codex/vfx-quality/"+label;Directory.CreateDirectory(dir+"/frames");StartCoroutine(Guard());}
 IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object value=null;Exception failure=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){failure=e;}if(failure!=null){errors.Add(failure.ToString());break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}Finish();}
 IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
 IEnumerator Wait(float seconds){yield return Frames(Mathf.Max(1,Mathf.CeilToInt(seconds*hz)));}
 IEnumerator Until(Func<bool> predicate,float seconds=3){int n=Mathf.CeilToInt(seconds*hz);while(!predicate()&&n-->0)yield return null;if(!predicate())throw new Exception("Timeout: "+section);}
 IEnumerator Reset(string name)
 {
  recording=false;keys=Array.Empty<Key>();left=right=false;look=Vector2.zero;attack.SuspendForSwimming();yield return Wait(.18f);
  motor.RestoreSavedPose(floor+Vector3.up*(-motor.LowestFootWorldOffset+.07f),Quaternion.identity);motor.GetComponent<PlayerStamina>().currentStamina=100;
  typeof(ThirdPersonPlayerController).GetField("yaw",F).SetValue(motor,0f);typeof(ThirdPersonPlayerController).GetField("pitch",F).SetValue(motor,10f);motor.cameraCollision=true;camera.transform.rotation=Quaternion.Euler(10,0,0);section=name;yield return Wait(.3f);heading=Quaternion.Euler(0,GetFloat("yaw"),0);recording=true;
 }
 IEnumerator Click(){left=true;yield return Frames(1);left=false;yield return Frames(1);}
 IEnumerator Run()
 {
  var saveField=typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static);previousSavePath=(string)saveField.GetValue(null);saveField.SetValue(null,Path.GetFullPath(dir+"/fixture-save.json"));
  oldCapture=Time.captureDeltaTime;oldVSync=QualitySettings.vSyncCount;oldTarget=Application.targetFrameRate;Time.captureDeltaTime=scope=="realtime-actions"?0:1f/hz;if(scope=="realtime-actions"){QualitySettings.vSyncCount=0;Application.targetFrameRate=hz;}oldCursor=Cursor.lockState;oldVisible=Cursor.visible;
  oldSettings=InputSystem.settings;oldSettingsJson=EditorJsonUtility.ToJson(oldSettings);oldFlags=oldSettings.hideFlags;if(oldFlags==HideFlags.HideAndDontSave)oldSettings.hideFlags=HideFlags.DontSave;settings=Instantiate(oldSettings);settings.hideFlags=HideFlags.HideAndDontSave;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
  foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){disabled.Add(d);InputSystem.DisableDevice(d);}keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Feed;Application.logMessageReceived+=Log;
  root=new GameObject("Excluded control fixture");root.AddComponent<GameSaveExcluded>();SceneManager.MoveGameObjectToScene(root,SceneManager.CreateScene("Control review 072 "+label));
  bool terrain=scope=="terrain"||scope=="regression-terrain";if(terrain){var island=UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();if(!island.GetComponent<MeshCollider>().Raycast(new Ray(new Vector3(288,150,346),Vector3.down),out var contact,400))throw new Exception("No production island ground");floor=contact.point;}var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform);ground.transform.position=floor-Vector3.up*.5f;ground.transform.localScale=new Vector3(200,1,200);if(terrain){ground.GetComponent<Collider>().enabled=false;ground.GetComponent<Renderer>().enabled=false;}
  var sun=new GameObject("Review sun",typeof(Light));sun.transform.SetParent(root.transform);sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.GetComponent<Light>().type=LightType.Directional;if(terrain)sun.GetComponent<Light>().intensity=0;
  foreach(var existingCamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))existingCamera.enabled=false;foreach(var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))listener.enabled=false;
  var view=new GameObject("Control review camera",typeof(Camera),typeof(AudioListener));view.transform.SetParent(root.transform);view.tag="MainCamera";camera=view.GetComponent<Camera>();camera.fieldOfView=58;camera.farClipPlane=500;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.15f,.2f);if(terrain)camera.clearFlags=CameraClearFlags.Skybox;
  rt=new RenderTexture(960,540,24);rt.Create();camera.targetTexture=rt;image=new Texture2D(960,540,TextureFormat.RGB24,false);
  var staging=new GameObject("Inactive actor staging");staging.transform.SetParent(root.transform);staging.SetActive(false);actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),staging.transform);actor.AddComponent<GameSaveExcluded>();
  if(terrain){var source=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.scene.name=="Gameplay");EditorUtility.CopySerialized(source,actor.GetComponent<ThirdPersonPlayerController>());actor.GetComponent<ThirdPersonPlayerController>().enabled=true;actor.transform.localScale=source.transform.lossyScale;}
  actor.transform.position=floor+Vector3.up*4;staging.SetActive(true);
  motor=actor.GetComponent<ThirdPersonPlayerController>();motor.showControlDiagnostics=true;attack=actor.GetComponent<SahurAttack>();loadout=actor.GetComponent<SahurWeaponLoadout>();body=actor.GetComponent<CharacterController>();feel=attack.feel?attack.feel:CombatFeelProfile.Default;
  var selection=typeof(CombatFeelProfile).GetField("controlPreset");if(selection!=null&&!string.IsNullOrEmpty(preset)){previousPreset=selection.GetValue(feel);selection.SetValue(feel,Enum.Parse(selection.FieldType,preset));}
  yield return Wait(.5f);anim=motor.CharacterAnimator;
  File.WriteAllText(dir+"/runtime-config.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{motor.moveSpeed,motor.acceleration,motor.deceleration,motor.turnSpeedDegrees,motor.rollDuration,motor.cameraFollowSharpness,feel.lightDodgeCancel,feel.heavyDodgeCancel,feel.inputBuffer,controller=anim.runtimeAnimatorController.name,anim.applyRootMotion,one=new[]{attack.comboOneHitWindow.x,attack.comboOneHitWindow.y},two=new[]{attack.comboTwoHitWindow.x,attack.comboTwoHitWindow.y},three=new[]{attack.comboThreeHitWindow.x,attack.comboThreeHitWindow.y},attack.cooldown},Newtonsoft.Json.Formatting.Indented));
  if(scope=="all"||scope=="move"||scope=="terrain")
  {
   yield return Reset("start-stop");float t=Time.time;keys=new[]{Key.W};int frames=0;while(motor.WorldVelocity.z<.1f&&frames++<hz)yield return null;metrics["startLatencyFrames"]=frames;metrics["startLatencyMs"]=(Time.time-t)*1000;while(motor.WorldVelocity.z<motor.moveSpeed*.9f&&Time.time-t<1)yield return null;metrics["walk90Ms"]=(Time.time-t)*1000;yield return Wait(.3f);var origin=actor.transform.position;t=Time.time;keys=Array.Empty<Key>();yield return Until(()=>Vector3.ProjectOnPlane(motor.WorldVelocity,Vector3.up).magnitude<.1f,2);metrics["stopMs"]=(Time.time-t)*1000;metrics["stopDistance"]=Vector3.Distance(origin,actor.transform.position);Capture("stop");Check(frames<=2,"Movement starts within two frames",frames);
   yield return Reset("sprint-reverse");keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Sprint)};yield return Wait(.65f);t=Time.time;keys=new[]{Key.S,GameInputSettings.Get(GameInputSettings.Action.Sprint)};yield return Until(()=>motor.WorldVelocity.z<-.1f,2);metrics["sprintReverseMs"]=(Time.time-t)*1000;yield return Until(()=>motor.WorldVelocity.z< -motor.moveSpeed*motor.sprintMultiplier*.9f,2);metrics["sprintReverse90Ms"]=(Time.time-t)*1000;Capture("sprint-reverse");
   yield return Reset("alternating");float maximum=0;for(int i=0;i<12;i++){keys=new[]{i%2==0?Key.A:Key.D};yield return Wait(.08f);maximum=Mathf.Max(maximum,Vector3.ProjectOnPlane(motor.WorldVelocity,Vector3.up).magnitude);}Check(!float.IsNaN(maximum)&&maximum<motor.moveSpeed*1.1f,"Rapid A/D input stays finite and speed limited",maximum);
   yield return Reset("diagonal");keys=new[]{Key.W,Key.D};yield return Wait(.5f);float diagonal=Vector3.ProjectOnPlane(motor.WorldVelocity,Vector3.up).magnitude;Check(Mathf.Abs(diagonal-motor.moveSpeed)<.05f,"Diagonal movement has no speed boost",diagonal);
   yield return Reset("jump-land");float rootY=actor.transform.position.y,maxY=rootY;keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Jump)};yield return Frames(1);keys=new[]{Key.W};bool airborne=false;float landed=-1;for(int i=0;i<hz*3;i++){yield return null;maxY=Mathf.Max(maxY,actor.transform.position.y);airborne|=motor.VerticalSpeed>0;if(airborne&&GetState()==4&&landed<0)landed=Time.time;if(landed>=0&&GetState()==0)break;}metrics["jumpHeight"]=maxY-rootY;metrics["landingRecoveryMs"]=landed>=0?(Time.time-landed)*1000:-1;Check(airborne&&landed>=0&&GetState()==0,"Jump lands and hands control back");Capture("land");
  }
  if(scope=="all"||scope=="terrain"||scope=="actions"||scope=="realtime-actions")
  {
   yield return Reset("dodge-exit");float t=Time.time;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};int n=0;while(!motor.IsDodging&&n++<hz)yield return null;metrics["dodgeLatencyFrames"]=n;Check(motor.IsDodging,"Ordinary dodge begins");t=Time.time;yield return Frames(1);keys=new[]{Key.D};yield return Until(()=>!motor.IsDodging,2);metrics["dodgeActiveMs"]=(Time.time-t)*1000;metrics["exitLateralSpeed"]=motor.WorldVelocity.x;yield return Frames(1);Check(motor.WorldVelocity.x>.1f,"Dodge exit immediately accepts new move direction",motor.WorldVelocity.x);Capture("dodge-exit");
   yield return Reset("roll-attack-buffer");keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Until(()=>motor.IsDodging);keys=Array.Empty<Key>();yield return Until(()=>GetFloat("rollTimer")<.08f);yield return Click();yield return Wait(.22f);metrics["rollAttackBuffered"]=attack.IsCombatMotionActive;if(!label.StartsWith("baseline"))Check(attack.IsCombatMotionActive,"Late dodge attack buffer executes after handoff");Capture("roll-attack-buffer");
   foreach(bool blades in new[]{false,true})
   {
    yield return Reset(blades?"blade-combo":"club-combo");loadout.Restore(true,blades?SahurWeaponLoadout.Weapon.CapriTwinBlades:SahurWeaponLoadout.Weapon.Stick,false);yield return Wait(.2f);yield return Click();var stages=new HashSet<int>();float deadline=Time.time+4;int last=-1;while(attack.IsCombatMotionActive&&Time.time<deadline){stages.Add(attack.CurrentComboStage);if(attack.CurrentComboStage>=0&&attack.CurrentComboStage<2&&attack.CurrentComboStage!=last&&attack.CurrentAttackPhase>.32f){last=attack.CurrentComboStage;yield return Click();}else yield return null;}Check(stages.Contains(0)&&stages.Contains(1)&&stages.Contains(2),(blades?"Twin blades":"Club")+" retains all three combo stages");
    yield return Reset(blades?"blade-buffered-dodge":"club-buffered-dodge");yield return Click();yield return Until(()=>attack.CurrentAttackPhase>=.62f);float phase=attack.CurrentAttackPhase;float t0=Time.time;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(1);keys=Array.Empty<Key>();bool seen=false;float delay=-1;for(int i=0;i<hz;i++){yield return null;if(motor.IsDodging){seen=true;delay=Time.time-t0;break;}}if(!label.StartsWith("baseline"))Check(seen,(blades?"Blade":"Club")+" late attack dodge buffer executes");metrics[blades?"bladeDodgeBuffered":"clubDodgeBuffered"]=seen;metrics[blades?"bladeCancelWaitMs":"clubCancelWaitMs"]=delay*1000;metrics[blades?"bladeCancelRequestPhase":"clubCancelRequestPhase"]=phase;Capture(blades?"blade-dodge":"club-dodge");
    yield return Reset(blades?"blade-early-commitment":"club-early-commitment");yield return Click();yield return Until(()=>attack.CurrentAttackPhase>=.1f);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(1);keys=Array.Empty<Key>();yield return Wait(.08f);Check(!motor.IsDodging&&attack.IsCombatMotionActive,"Early dodge respects "+(blades?"blade":"club")+" attack commitment");
    yield return Reset(blades?"blade-move-commitment":"club-moving-attack");yield return Click();yield return Until(()=>attack.CurrentAttackPhase>=.1f);keys=new[]{Key.D};yield return Frames(2);if(!label.StartsWith("baseline"))Check(attack.IsCombatMotionActive,(blades?"Blade":"Club")+" movement does not erase an active early swing");metrics[blades?"bladeMovePreservesAttack":"clubMovePreservesAttack"]=attack.IsCombatMotionActive;Capture(blades?"blade-move":"club-move");
   }
  }
  if(scope=="all"||scope=="camera"||scope=="terrain")
  {
   yield return Reset("camera-mouse-step");motor.cameraCollision=false;float before=camera.transform.eulerAngles.y;look=new Vector2(250,0);yield return Frames(1);look=Vector2.zero;yield return new WaitForEndOfFrame();float intended=Mathf.Abs(Mathf.DeltaAngle(0,GetFloat("yaw")));metrics["mouseTargetDegrees"]=intended;metrics["cameraFirstFrameDegrees"]=Mathf.Abs(Mathf.DeltaAngle(before,camera.transform.eulerAngles.y));if(!label.StartsWith("baseline"))Check(Mathf.Abs(Mathf.DeltaAngle(GetFloat("yaw"),camera.transform.eulerAngles.y))<.1f,"Mouse look reaches the camera on the input frame");yield return Wait(.25f);metrics["cameraSettledErrorDegrees"]=Mathf.Abs(Mathf.DeltaAngle(GetFloat("yaw"),camera.transform.eulerAngles.y));Capture("camera-step");
   yield return Reset("camera-obstacle");var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=floor+new Vector3(0,5,-4);wall.transform.localScale=new Vector3(14,12,1);Physics.SyncTransforms();yield return Wait(.15f);bool inside=wall.GetComponent<Collider>().bounds.Contains(camera.transform.position);Check(!inside,"Camera contracts outside a solid wall");Destroy(wall);yield return Wait(.25f);Capture("camera-release");
   yield return Reset("lock-on");var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Lock-on target";target.tag="Enemy";target.transform.SetParent(root.transform);target.transform.position=floor+new Vector3(0,2,12);target.transform.localScale=new Vector3(2,4,2);target.AddComponent<GameSaveExcluded>();var hp=target.AddComponent<Health>();hp.currentHealth=hp.maxHealth=99999;Physics.SyncTransforms();bool locked=false;
   foreach(float side in new[]{0f,-5f,5f,-8f,8f})
   {
    var point=floor+new Vector3(side,150,12);
    if(terrain&&UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>().Raycast(new Ray(point,Vector3.down),out var h,400))target.transform.position=h.point+Vector3.up*2;
    else target.transform.position=floor+new Vector3(side,2,12);
    Physics.SyncTransforms();locked=actor.GetComponent<EnemyLockOn>().Toggle(camera);if(locked)break;
   }
   keys=new[]{Key.D};yield return Wait(.3f);float facingError=Quaternion.Angle(actor.transform.rotation,actor.GetComponent<EnemyLockOn>().FacingRotation());metrics["lockFacingErrorDegrees"]=facingError;if(!label.StartsWith("baseline"))Check(facingError<5,"Lock-on lateral footwork keeps Sahur facing the target",facingError);Check(locked&&actor.GetComponent<EnemyLockOn>().IsLocked,"Real lock-on acquisition survives lateral movement");Capture("lock-on");actor.GetComponent<EnemyLockOn>().Clear();Destroy(target);
  }
  if(scope=="weapons")
  {
   foreach(bool blades in new[]{false,true})
   {
    yield return Reset(blades?"blade-charge-release":"club-charge-release");loadout.Restore(true,blades?SahurWeaponLoadout.Weapon.CapriTwinBlades:SahurWeaponLoadout.Weapon.Stick,false);yield return Wait(.15f);right=true;yield return Until(()=>attack.IsCharging);keys=new[]{Key.D};yield return Wait(.3f);Check(attack.IsCharging&&attack.CanMoveDuringCombat,(blades?"Blade":"Club")+" charge retains locomotion");keys=Array.Empty<Key>();right=false;yield return Until(()=>attack.IsHeavyAttackActive);Check(attack.ReleasedCharge01>0,(blades?"Blade":"Club")+" deliberate partial charge releases the heavy attack");yield return Until(()=>!attack.IsCombatMotionActive,5);
    yield return Reset(blades?"blade-charge-dodge":"club-charge-dodge");right=true;yield return Until(()=>attack.IsCharging);yield return Wait(.2f);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(2);right=false;keys=Array.Empty<Key>();Check(motor.IsDodging&&!attack.IsCharging,(blades?"Blade":"Club")+" charge cancels into real dodge");
    yield return Reset(blades?"blade-spin":"club-spin");right=left=true;yield return Wait(.3f);Check(attack.IsSpinning,(blades?"Blade":"Club")+" original chord still starts its spin");keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(2);right=left=false;keys=Array.Empty<Key>();Check(motor.IsDodging&&!attack.IsSpinning,(blades?"Blade":"Club")+" spin retains its legal dodge exit");
   }
   yield return Reset("blade-guard");loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades,false);yield return Wait(.15f);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};yield return Wait(.25f);Check(attack.GuardReady,"Twin-blade guard remains ready after entry");var health=actor.GetComponent<PlayerHealth>();float originalHealth=health.currentHealth;health.ApplyMeleeDamage(10,actor.transform.position+Vector3.up,actor.transform.position+actor.transform.forward*2);Check(health.currentHealth==originalHealth,"Actual frontal hostile damage still reaches the guard resolver");keys=Array.Empty<Key>();yield return Wait(.2f);
   yield return Reset("rifle-fire-aim");loadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);var rifle=actor.GetComponent<SahurRifleCombat>();rifle.RestoreAmmo(11,64);yield return Wait(.3f);int shots=rifle.ShotsFired;left=true;yield return Wait(.08f);left=false;Check(rifle.ShotsFired>shots&&rifle.Rounds<11,"Rifle still fires via real input and consumes ammo");float fov=camera.fieldOfView;right=true;yield return Wait(.3f);Check(rifle.IsAiming&&camera.fieldOfView<fov-2,"Rifle aim retains its zoom on the responsive camera");right=false;yield return Wait(.6f);Check(Mathf.Abs(camera.fieldOfView-fov)<.3f,"Rifle zoom restores the ordinary field of view");
  }
  if(scope=="handoffs")
  {
   yield return Reset("dodge-dodge-handoff");int before=motor.DodgesPerformed;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Until(()=>motor.IsDodging);keys=Array.Empty<Key>();yield return Until(()=>motor.DodgeTimeRemaining<.055f);float request=Time.time;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(1);keys=Array.Empty<Key>();yield return Wait(.3f);Check(motor.DodgesPerformed==before+2,"Fresh late dodge press survives the actual recovery cooldown");metrics["queuedDodgeCount"]=motor.DodgesPerformed-before;
   yield return Reset("dodge-jump-handoff");keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Until(()=>motor.IsDodging);keys=Array.Empty<Key>();yield return Until(()=>motor.DodgeTimeRemaining<.055f);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};yield return Frames(1);keys=Array.Empty<Key>();yield return Wait(.15f);Check(motor.VerticalSpeed>1,"Fresh late jump press survives the dodge handoff",motor.VerticalSpeed);
   yield return Reset("preset-during-dodge");keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Until(()=>motor.IsDodging);keys=Array.Empty<Key>();float originalDuration=GetFloat("sampledRollDuration");feel.controlPreset=CombatFeelProfile.ControlPreset.Weighty;yield return Frames(2);Check(Mathf.Abs(GetFloat("sampledRollDuration")-originalDuration)<.0001f,"Preset switching leaves the active dodge timeline intact");feel.controlPreset=CombatFeelProfile.ControlPreset.Balanced;
  }
  if(scope=="regression-terrain")yield return Regression();
  if(scope=="polish")yield return PolishCases();
  if(scope=="rifle-micro")
  {
   yield return Reset("rifle-aim-blend");loadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);typeof(ThirdPersonPlayerController).GetField("pitch",F).SetValue(motor,40f);yield return Wait(.5f);
   var gun=loadout.HeldRifle;var rifle=actor.GetComponent<SahurRifleCombat>();float maxIn=0,maxOut=0;Quaternion previous=gun.rotation;
   right=true;for(int i=0;i<18;i++){yield return null;maxIn=Mathf.Max(maxIn,Quaternion.Angle(previous,gun.rotation));previous=gun.rotation;if(i<8)Capture("aim-in-"+i);}
   right=false;for(int i=0;i<18;i++){yield return null;maxOut=Mathf.Max(maxOut,Quaternion.Angle(previous,gun.rotation));previous=gun.rotation;if(i<8)Capture("aim-out-"+i);}
   metrics["aimInPeakDegreesPerFrame"]=maxIn;metrics["aimOutPeakDegreesPerFrame"]=maxOut;
   Check(maxIn<15&&maxOut<15,"Rifle carry/aim has no single-frame pose cut",new{maxIn,maxOut});
   left=true;yield return Frames(2);left=false;float error=Vector3.Angle(gun.TransformDirection(Vector3.left),(rifle.GetAimPoint()-gun.TransformPoint(SahurRifleCombat.MuzzleLocal)).normalized);metrics["firingBoreErrorDegrees"]=error;Check(error<1,"Firing keeps bore aligned to actual aim point",error);
  }
  if(scope=="nonrifle-micro")
  {
   yield return Reset("club-moving-entry");loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick,false);yield return Wait(.2f);keys=new[]{Key.W};yield return Wait(.45f);
   int light=anim.GetLayerIndex("Light Upper Body");
   // Read the actual layer name from the component, rather than assuming a controller layout.
   light=(int)typeof(SahurAttack).GetField("lightLayerIndex",F).GetValue(attack);
   left=true;yield return Until(()=>attack.IsCombatMotionActive);left=false;
   light=(int)typeof(SahurAttack).GetField("lightLayerIndex",F).GetValue(attack);
   metrics["clubEntryWeight"]=anim.GetLayerWeight(light);
   Check(attack.CanMoveDuringCombat,"Moving staff entry retains locomotion ownership");
   var arm=anim.GetBoneTransform(HumanBodyBones.RightUpperArm);Quaternion prior=arm.localRotation;float peak=0;
   for(int i=0;i<12;i++){yield return new WaitForEndOfFrame();peak=Mathf.Max(peak,Quaternion.Angle(prior,arm.localRotation));prior=arm.localRotation;if(i<6)Capture("club-entry-"+i);yield return null;}
   metrics["clubEntryArmPeakDegrees"]=peak;
   Check(anim.GetLayerWeight(light)>.99f,"Staff upper body reaches full weight before contact");
   yield return Until(()=>!attack.IsCombatMotionActive,4);
   yield return Reset("blade-repeated-moving-recovery");loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades,false);yield return Wait(.2f);
   for(int attempt=0;attempt<3;attempt++)
   {
    // Keep the layer alive between attempts: a prior recovery may still hold its old clock.
    section="blade-moving-recovery-"+attempt;right=true;yield return Until(()=>attack.IsCharging);yield return Wait(.35f);right=false;yield return Until(()=>attack.IsHeavyAttackActive);keys=new[]{Key.W};
    bool handed=false;float sourcePhase=0,returnedPhase=0,deadline=Time.time+4;
    while(attack.IsHeavyAttackActive&&Time.time<deadline){sourcePhase=attack.HeavyAttackPhase;yield return null;if((bool)typeof(SahurAttack).GetField("usesBladeRecovery",F).GetValue(attack)){handed=true;returnedPhase=attack.HeavyAttackPhase;break;}}
    metrics["bladeHandoff"+attempt]=new{handed,sourcePhase,returnedPhase};
    Check(handed&&attack.IsHeavyAttackActive,"Blade recovery preserves the existing follow-through, attempt "+attempt);
    if(handed){Check(returnedPhase>=sourcePhase-.02f&&returnedPhase<.98f,"Blade recovery clock stays continuous, attempt "+attempt,returnedPhase);for(int i=0;i<6;i++){yield return new WaitForEndOfFrame();Capture("blade-recovery-"+attempt+"-"+i);yield return null;}}
    yield return Until(()=>!attack.IsCombatMotionActive,4);keys=Array.Empty<Key>();yield return Wait(.6f);
   }
  }
  if(scope=="rifle-vfx")
  {
   yield return Reset("rifle-native-muzzle");loadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);yield return Wait(.35f);var rifle=actor.GetComponent<SahurRifleCombat>();rifle.RestoreAmmo(20,80);right=true;yield return Wait(.2f);
   int fired=rifle.ShotsFired;left=true;yield return Until(()=>rifle.ShotsFired>fired);left=false;
   for(int i=0;i<8;i++){yield return new WaitForEndOfFrame();Capture("rifle-muzzle-"+i);yield return null;}
   Check(rifle.Rounds==19,"Real rifle shot consumes exactly one round");
   var muzzle=(BrineMuzzleFlash)typeof(SahurRifleCombat).GetField("muzzle",F).GetValue(rifle);yield return Wait(.15f);Check(muzzle!=null&&!muzzle.Visible,"Actual attached muzzle expires after shooting");
   foreach(var surface in new[]{CombatMaterial.Rock,CombatMaterial.Wood,CombatMaterial.Metal})
   {
    var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name=surface+" rifle contact fixture";obstacle.transform.SetParent(root.transform);obstacle.AddComponent<CombatSurfaceResponse>().material=surface;
    var aim=rifle.AimRay;obstacle.transform.position=aim.GetPoint(8);obstacle.transform.localScale=Vector3.one*2;Physics.SyncTransforms();yield return Wait(.15f);int accepted=BrineVfxWorld.Get().Accepted;fired=rifle.ShotsFired;left=true;yield return Until(()=>rifle.ShotsFired>fired);left=false;yield return Until(()=>BrineVfxWorld.Get().Accepted>accepted,2);Check(BrineVfxWorld.Get().Accepted>accepted,"Real projectile emits "+surface+" scenery contact");Capture("rifle-contact-"+surface);Destroy(obstacle);yield return Wait(.4f);
   }
  }
  if(scope=="recovery-exits")
  {
   foreach(string exit in new[]{"dodge","weapon","swim"})
   {
    yield return Reset("blade-recovery-"+exit);loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades,false);yield return Wait(.2f);
    right=true;yield return Until(()=>attack.IsCharging);yield return Wait(.35f);right=false;yield return Until(()=>attack.IsHeavyAttackActive);keys=new[]{Key.W};yield return Until(()=>(bool)typeof(SahurAttack).GetField("usesBladeRecovery",F).GetValue(attack));int layer=anim.GetLayerIndex("Twin Blade Recovery");
    if(exit=="dodge"){yield return Until(()=>attack.CanDodgeCancel);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(2);Check(motor.IsDodging,"Blade follow-through cancels into dodge at the original legal phase");}
    else if(exit=="weapon"){loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick,false);yield return Frames(1);Check(!attack.UsesTwinBlades,"Weapon swap is accepted during blade recovery");}
    else {attack.SuspendForSwimming();yield return Frames(1);}
    Check(!attack.IsHeavyAttackActive&&anim.GetLayerWeight(layer)==0,"Blade recovery layer clears on "+exit);
   }
   yield return Reset("staff-contact-weight");loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick,false);keys=new[]{Key.W};yield return Wait(.35f);yield return Click();yield return Until(()=>attack.CurrentAttackPhase>=attack.comboOneHitWindow.x);int light=(int)typeof(SahurAttack).GetField("lightLayerIndex",F).GetValue(attack);Check(anim.GetLayerWeight(light)>.999f,"Moving staff reaches full authored pose by the original hit window",anim.GetLayerWeight(light));
  }
  if(scope=="air")
  {
   foreach(bool blades in new[]{false,true})
   {
    yield return Reset(blades?"blade-air":"club-air");loadout.Restore(true,blades?SahurWeaponLoadout.Weapon.CapriTwinBlades:SahurWeaponLoadout.Weapon.Stick,false);yield return Wait(.2f);
    keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Jump)};yield return Until(()=>motor.CanUseAirAttack);keys=new[]{Key.W};yield return Click();yield return Until(()=>attack.IsJumpSlashActive);Check(attack.IsJumpSlashActive,(blades?"Blade":"Staff")+" airborne attack starts");Capture(blades?"blade-air":"club-air");yield return Until(()=>!attack.IsCombatMotionActive,5);yield return Until(()=>motor.CanUseGroundAttack,5);Check(!attack.IsJumpSlashActive&&motor.CanUseGroundAttack,(blades?"Blade":"Staff")+" jump attack returns to ground locomotion");
   }
  }
  if(scope=="staff")
  {
   yield return Reset("staff-throw");loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick,false);yield return Wait(.2f);var thrown=actor.GetComponent<SahurBoomerang>();Check(thrown.TryThrow(),"Staff original throw accepts legal grounded request");yield return Until(()=>thrown.IsFlying,3);Check(thrown.IsFlying,"Throw releases real flying staff from the animated grip");yield return Until(()=>!thrown.IsBusy,5);Check(!thrown.IsBusy,"Staff returns and catches without stale throw state");
  }
  Check(errors.Count==0,"No runtime exceptions or asserts",errors.ToArray());
 }
 IEnumerator PolishCases()
 {
  yield return Reset("forward-dodge-handoff");keys=new[]{Key.W};yield return Wait(.5f);keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Until(()=>motor.IsDodging);keys=new[]{Key.W};yield return Until(()=>!motor.IsDodging);float exit=Vector3.ProjectOnPlane(motor.WorldVelocity,Vector3.up).magnitude;metrics["forwardDodgeExitSpeed"]=exit;Check(exit>motor.moveSpeed*.75f,"Forward dodge preserves running legs at handoff",exit);
  yield return Reset("bounded-hostile-push");motor.ApplyCombatPush(Vector3.right,6,.28f);float peak=0;for(int i=0;i<Mathf.CeilToInt(hz*.3f);i++){yield return null;peak=Mathf.Max(peak,Vector3.ProjectOnPlane(motor.WorldVelocity,Vector3.up).magnitude);}metrics["hostilePushPeak"]=peak;Check(peak<=6.1f,"Hostile push does not compound into locomotion",peak);yield return Wait(.5f);
  yield return Reset("rifle-tap-buffer");loadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);var rifle=actor.GetComponent<SahurRifleCombat>();rifle.RestoreAmmo(30,90);yield return Wait(.3f);left=true;yield return Frames(1);left=false;yield return Frames(1);int shots=rifle.ShotsFired;left=true;yield return Frames(1);left=false;yield return Wait(.18f);Check(rifle.ShotsFired==shots+1,"Fresh rifle tap survives fire cooldown",rifle.ShotsFired-shots);
  yield return Reset("rifle-run-rules");right=left=true;keys=new[]{Key.W};yield return Wait(.3f);Check(rifle.IsAiming&&rifle.FirePoseActive&&motor.WorldVelocity.magnitude>1,"Walking can aim and fire");keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Sprint)};shots=rifle.ShotsFired;yield return Wait(.4f);Check(motor.IsSprinting&&!rifle.IsAiming&&rifle.ShotsFired>shots,"Sprinting fires while rejecting aim");right=false;rifle.RestoreAmmo(30,90);yield return Wait(.2f);shots=rifle.ShotsFired;yield return Wait(1);metrics["rifleShotsPerSecond"]=rifle.ShotsFired-shots;Check(rifle.ShotsFired-shots>=9&&rifle.ShotsFired-shots<=11,"Automatic fire cadence stays within one round of 10Hz",rifle.ShotsFired-shots);left=false;keys=Array.Empty<Key>();
 }
 IEnumerator Regression()
 {
  yield return Reset("walking-stamina");var stamina=actor.GetComponent<PlayerStamina>();stamina.currentStamina=50;yield return Wait(1);keys=new[]{Key.W};yield return Wait(.4f);Check(stamina.currentStamina>50,"Walking remains free and regenerates stamina",stamina.currentStamina);
  yield return Reset("local-impact");loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick,false);keys=new[]{Key.W};yield return Wait(.1f);yield return Click();yield return Until(()=>attack.CurrentAttackPhase>.25f);
  var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Real swept contact target";target.tag="Enemy";target.transform.SetParent(root.transform);target.AddComponent<GameSaveExcluded>();var hp=target.AddComponent<Health>();hp.maxHealth=hp.currentHealth=10000;target.transform.localScale=Vector3.one*1.5f;
  var sample=new object[]{Vector3.zero,Vector3.zero,0f};typeof(SahurAttack).GetMethod("GetWeaponVolume",F).Invoke(attack,sample);target.transform.position=((Vector3)sample[0]+(Vector3)sample[1])*.5f;Physics.SyncTransforms();
  // Keep the hurtbox on the real blade/club segment until a genuine sweep contacts it.
  float deadline=Time.time+1.5f;while(hp.currentHealth>=10000&&Time.time<deadline){typeof(SahurAttack).GetMethod("GetWeaponVolume",F).Invoke(attack,sample);target.transform.position=((Vector3)sample[0]+(Vector3)sample[1])*.5f;Physics.SyncTransforms();yield return null;}
  Check(hp.currentHealth<10000,"Actual weapon sweep still damages a real Health target");Check(Mathf.Approximately(Time.timeScale,1),"Confirmed impacts never stop global time");var stop=actor.GetComponent<CombatLocalHitStop>();Check(stop&&stop.IsStopped&&stop.UsesLayerFreeze&&anim.speed>0,"Mobile hit stop freezes the attack layer and leaves locomotion live");yield return Wait(.2f);Check(!stop.IsStopped,"Local hit stop releases after contact");Capture("real-impact");Destroy(target);
  yield return Reset("external-owner");motor.ExternalControlLock=true;motor.ExternalMovementLock=true;var held=actor.transform.position;keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Dodge),GameInputSettings.Get(GameInputSettings.Action.Jump)};left=true;yield return Wait(.15f);Check(Vector3.Distance(held,actor.transform.position)<.001f&&!motor.IsDodging&&!attack.IsCombatMotionActive,"External/scripted owners retain movement and combat control");keys=Array.Empty<Key>();left=false;motor.ExternalMovementLock=false;motor.ExternalControlLock=false;
  yield return Reset("pause-buffer");Time.timeScale=0;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};yield return Frames(3);keys=Array.Empty<Key>();Time.timeScale=1;yield return Frames(3);Check(!motor.IsDodging,"Paused dodge presses do not execute after resume");
  yield return Reset("camera-handoff");camera.transform.position+=new Vector3(2,1,1);motor.ContinueCameraFromCurrentView(camera);var old=camera.transform.position;float jump=0;for(int i=0;i<100;i++){yield return null;jump=Mathf.Max(jump,Vector3.Distance(old,camera.transform.position));old=camera.transform.position;}Check(jump<.65f,"Cinematic-to-player camera handoff has no cut",jump);
  yield return Reset("low-gravity");var originalScene=root.scene;var softScene=SceneManager.CreateScene("Second Island");SceneManager.MoveGameObjectToScene(root,softScene);var soft=actor.AddComponent<MarshmallowTraversal>();motor.BindMarshmallowTraversal();float y=actor.transform.position.y,peak=y;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};yield return Frames(1);keys=Array.Empty<Key>();for(int i=0;i<hz*2.5f;i++){yield return null;peak=Mathf.Max(peak,actor.transform.position.y);}Check(soft.LowGravityActive&&peak-y>4,"Second Island low gravity and authored jump height remain active",peak-y);SceneManager.MoveGameObjectToScene(root,originalScene);Destroy(soft);SceneManager.UnloadSceneAsync(softScene);
  yield return Reset("swimming");var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();Check(ocean!=null,"Production island ocean is present");var oldFloor=floor;motor.RestoreSavedPose(new Vector3(900,ocean.oceanHeight-motor.LowestFootWorldOffset-2,591),Quaternion.identity);keys=new[]{Key.W};yield return Wait(.45f);Check(motor.Swimming&&!attack.IsCombatMotionActive,"Actual ocean takes ownership from ground combat");Check(!float.IsNaN(motor.WorldVelocity.y),"Swimming buoyancy remains finite");Capture("swimming");keys=Array.Empty<Key>();motor.RestoreSavedPose(oldFloor+Vector3.up*(-motor.LowestFootWorldOffset+.07f),Quaternion.identity);yield return Wait(.3f);Check(!motor.Swimming,"Returning to shore restores ground movement");
 }
 void LateUpdate()
 {
  if(!recording||!actor||!anim)return;frame++;if(frame%30==0)File.WriteAllText(dir+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,section}));var v=motor.WorldVelocity;rows.Add(new{frame,section,time=Time.time,unscaledTime=Time.unscaledTime,dt=Time.deltaTime,input=new[]{GameControls.Move.x,GameControls.Move.y},velocity=new[]{v.x,v.y,v.z},position=new[]{actor.transform.position.x,actor.transform.position.y,actor.transform.position.z},state=GetState(),speed=anim.GetFloat("Speed"),attack=attack.CurrentComboStage,phase=attack.CurrentAttackPhase,heldDodge=GameInputSettings.Pressed(GameInputSettings.Action.Dodge),pressedDodge=GameInputSettings.PressedThisFrame(GameInputSettings.Action.Dodge),combatActive=attack.IsCombatMotionActive,canCancel=attack.CanDodgeCancel,canGround=motor.CanUseGroundAttack,canAir=motor.CanUseAirAttack,cooldown=GetFloat("rollCooldownTimer"),buffer=(float)typeof(SahurAttack).GetField("dodgeBufferedUntil",F).GetValue(attack),decision=motor.LastControlEvent.decision,locked=attack.IsCombatMotionActive&&!attack.CanTranslateDuringCombat,dodge=motor.IsDodging,animator=anim.GetCurrentAnimatorStateInfo(0).fullPathHash,transition=anim.IsInTransition(0),yaw=GetFloat("yaw"),cameraYaw=camera.transform.eulerAngles.y});
 }
 void Capture(string name)
 {
  var previous=RenderTexture.active;try{camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(dir+"/frames/"+(shot++).ToString("D2")+"-"+name+".png",image.EncodeToPNG());}finally{RenderTexture.active=previous;}
 }
 void Finish()
 {
  recording=false;Cleanup();File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0&&checks.All(c=>(bool)c.GetType().GetProperty("ok").GetValue(c)),hz,scope,preset,metrics,checks,errors,frames=frame},Newtonsoft.Json.Formatting.Indented));File.WriteAllText(dir+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));if(root)Destroy(root);if(rt){rt.Release();Destroy(rt);}if(image)Destroy(image);Destroy(gameObject);
 }
 void Cleanup(){if(cleaned)return;cleaned=true;typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,previousSavePath);InputSystem.onBeforeUpdate-=Feed;Application.logMessageReceived-=Log;Time.captureDeltaTime=oldCapture;QualitySettings.vSyncCount=oldVSync;Application.targetFrameRate=oldTarget;if(previousPreset!=null&&feel)typeof(CombatFeelProfile).GetField("controlPreset").SetValue(feel,previousPreset);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(oldSettings){oldSettings.hideFlags=oldFlags;InputSystem.settings=oldSettings;}else if(!string.IsNullOrEmpty(oldSettingsJson)){var restored=ScriptableObject.CreateInstance<InputSettings>();EditorJsonUtility.FromJsonOverwrite(oldSettingsJson,restored);restored.hideFlags=oldFlags;InputSystem.settings=restored;}foreach(var d in disabled)if(d.added){InputSystem.EnableDevice(d);if(d is Keyboard k)k.MakeCurrent();if(d is Mouse m)m.MakeCurrent();}if(settings)Destroy(settings);Cursor.lockState=oldCursor;Cursor.visible=oldVisible;}
 void OnDisable(){Cleanup();}void OnDestroy(){Cleanup();}
}

