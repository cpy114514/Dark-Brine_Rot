using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;

public static class TestPressureV256
{
 public static object Open()
 {
  if(Application.isPlaying)throw new Exception("Stop before opening the temporary test workspace");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scenes");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;return new {starting=true};
 }
 public static object Start(string take="native-60",int hz=60)
 {
  if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
  string folder=".codex/pressure-vfx-v256/"+take;if(Directory.Exists(folder))throw new Exception("Preserve the previous take");Directory.CreateDirectory(folder);
  var go=new GameObject("Temporary weapon flow review");var runner=go.AddComponent<PressureObserverV256>();runner.folder=folder;runner.hz=hz;return new {started=true};
 }
}
[DefaultExecutionOrder(32000)] public sealed class PressureObserverV256:MonoBehaviour
{
 public string folder;public int hz;
 Keyboard keyboard,oldKeyboard;Mouse mouse,oldMouse;Key[] keys=Array.Empty<Key>();bool left,right,finished;
 GameObject actor,floor;Camera reviewCamera;SahurAttack attack;ThirdPersonPlayerController motor;SahurWeaponLoadout weapons;SahurRifleCombat rifle;PlayerStamina stamina;CharacterController capsule;
 TaskCompletionSource<bool> tick;float oldDelta,oldTime;int oldRate,oldSync,frame;string section="settle";Vector3 home;
 readonly List<object> checks=new List<object>();readonly List<object> rows=new List<object>();
 int lastRush,lastLanding,rushCapture=-1,landingCapture=-1;
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
 void Check(bool condition,string description){checks.Add(new {description,passed=condition,frame});if(!condition)throw new Exception(description);}
 async void Start()
 {
  try
  {
   File.WriteAllText(folder+"/started.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {playing=Application.isPlaying,hz}));
   oldDelta=Time.captureDeltaTime;oldTime=Time.timeScale;Time.timeScale=1;oldRate=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;Time.captureDeltaTime=1f/hz;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
   oldKeyboard=Keyboard.current;oldMouse=Mouse.current;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;
   reviewCamera=new GameObject("Review camera").AddComponent<Camera>();reviewCamera.gameObject.AddComponent<AudioListener>();reviewCamera.tag="MainCamera";reviewCamera.nearClipPlane=.05f;reviewCamera.farClipPlane=1000;reviewCamera.fieldOfView=50;reviewCamera.backgroundColor=new Color(.1f,.14f,.2f);reviewCamera.clearFlags=CameraClearFlags.SolidColor;
   var sun=new GameObject("Review sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(35,-30,0);
   var container=new GameObject("Isolated native test actor");container.SetActive(false);container.transform.position=new Vector3(2000,500,2000);
   actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),container.transform);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;actor.AddComponent<GameSaveExcluded>();
   foreach(var input in actor.GetComponentsInChildren<PlayerInput>(true))input.enabled=false;
   container.SetActive(true);motor=actor.GetComponent<ThirdPersonPlayerController>();attack=actor.GetComponent<SahurAttack>();weapons=actor.GetComponent<SahurWeaponLoadout>();stamina=actor.GetComponent<PlayerStamina>();capsule=actor.GetComponent<CharacterController>();
   motor.waterSplashes=false;motor.seaLevel=-1000;motor.cameraCollision=false;
   motor.PrepareRuntime();
   floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Review floor";floor.transform.position=new Vector3(2000,capsule.bounds.min.y-.52f,2000);floor.transform.localScale=new Vector3(600,1,600);
   home=actor.transform.position;Physics.SyncTransforms();
   await Step(.6f);rifle=actor.GetComponent<SahurRifleCombat>();
   File.WriteAllText(folder+"/settled.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {Time.timeScale,motorEnabled=motor.enabled,motor.ExternalControlLock,motor.ExternalMovementLock,motor.VerticalSpeed,motor.CanUseGroundAttack,motor.CanMaintainGroundCharge,capsule.isGrounded,capsuleEnabled=capsule.enabled,position=actor.transform.position.ToString("F4"),bounds=capsule.bounds.ToString("F4"),floor=floor.GetComponent<Collider>().bounds.ToString("F4"),pause=PauseSettingsMenu.IsOpen,ui=SahurLoadoutUI.BlocksInput,keyboard=Keyboard.current?.name,cursor=Cursor.lockState.ToString()},Newtonsoft.Json.Formatting.Indented));
   Check(motor.CanUseGroundAttack,"Native actor grounded on test floor");
   section="club moving charge";await Step(.4f,false,false,Forward,Sprint);float z=actor.transform.position.z;
   await Step(.55f,false,true,Forward,Sprint);
   Check(attack.IsCharging&&attack.ChargeElapsed>.45f&&actor.transform.position.z>z+.3f,"Club starts and maintains charge while sprinting");
   float elapsed=attack.ChargeElapsed;await Step(.2f,false,true,Right,Sprint);await Step(.2f,false,true,Back,Sprint);
   Check(attack.IsCharging&&attack.ChargeElapsed>elapsed+.3f,"Direction changes preserve charge clock");
   int chargeLayer=attack.animator.GetLayerIndex("Charge Upper Body");
   Check(attack.animator.GetLayerWeight(chargeLayer)>.95f&&attack.animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Club charge overlays walking/running legs");Capture("club-run-charge");
   // A physical one-frame floor interruption must not discard a held charge.
   floor.GetComponent<Collider>().enabled=false;await Frames(1);floor.GetComponent<Collider>().enabled=true;await Frames(1);
   Check(attack.IsCharging,"Brief ground-contact interruption preserves charge");
   await Step(.06f,false,false,Forward,Sprint);Check(attack.IsHeavyAttackActive,"Moving club charge releases into its strike");
   await Reset();await Step(.25f,false,true);float stationaryCharge=attack.ChargeElapsed;await Step(.3f,false,true,Forward);
   Check(attack.IsCharging&&attack.ChargeElapsed>stationaryCharge+.2f,"Starting movement during a stationary charge preserves it");
   floor.GetComponent<Collider>().enabled=false;await Step(.25f,false,true);
   Check(!attack.IsCharging,"A real fall beyond ground grace ends the charge");floor.GetComponent<Collider>().enabled=true;
   await Reset();weapons.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades,true);await Step(.25f);
   section="partial blade rush";await Step(.4f,false,false,Forward);await Step(.45f,false,true,Forward);
   Check(attack.IsCharging,"Twin blades charge while already walking");Capture("blade-moving-charge");
   await Step(.03f);Check(attack.IsHeavyAttackActive,"Blade release below old 1.2s gate performs rush");
   float partialDamage=Get<float>(attack,"swingDamage");Vector3 from=actor.transform.position;bool sawImmune=false;
   while(attack.IsHeavyAttackActive){sawImmune|=actor.GetComponent<SahurTwinBladeMotion>().IsDamageImmune;await Frames(1);}
   Check(actor.GetComponent<CombatImpactVisuals>().RushEmissions>0,"Native blade rush emits its pressure ring");
   float partialDistance=Vector3.Distance(from,actor.transform.position);Check(sawImmune,"Partial blade rush keeps existing travel immunity");Capture("blade-partial-finish");
   await Reset();section="full blade rush";await Step(attack.EffectiveFullChargeTime+.15f,false,true);await Step(.03f);
   Check(attack.IsHeavyAttackActive,"Full blade charge releases");float fullDamage=Get<float>(attack,"swingDamage");from=actor.transform.position;
   while(attack.IsHeavyAttackActive)await Frames(1);float fullDistance=Vector3.Distance(from,actor.transform.position);
   checks.Add(new {description="Measured partial/full blade release",partialDamage,fullDamage,partialDistance,fullDistance});
   Check(partialDamage<fullDamage&&partialDistance>fullDistance+1,"Partial charge has lower damage and longer travel");
   Check(Mathf.Abs(fullDistance-16)<.4f&&partialDistance>18,"Full rush retains 16m; partial rush exceeds 18m");
   await Reset();weapons.TryEquip(SahurWeaponLoadout.Weapon.M4A1);await Step(.4f);
   section="rifle aim walk";await Step(.25f,false,true);Check(rifle.IsAiming,"RMB aims while standing");Capture("rifle-standing-aim");
   await Step(.55f,false,true,Forward);Check(rifle.IsAiming&&!motor.IsSprinting,"Walking permits aiming");Capture("rifle-walk-aim");
   int shots=rifle.ShotsFired;await Step(.35f,true,true,Forward);Check(rifle.ShotsFired>shots,"Walking aim permits automatic fire");
   section="rifle sprint";shots=rifle.ShotsFired;from=actor.transform.position;await Step(.5f,true,true,Forward,Sprint);
   Check(motor.IsSprinting&&!rifle.IsAiming,"Sprint overrides held RMB aiming");
   Check(rifle.ShotsFired>shots&&Vector3.Distance(from,actor.transform.position)>motor.moveSpeed*.5f*.5f,"Sprint moves faster than rifle walk and still fires");
   int locomotion=attack.animator.GetLayerIndex("Rifle Locomotion"),holding=attack.animator.GetLayerIndex("Rifle Holding Upper Body");
   Check(attack.animator.GetLayerWeight(locomotion)<.01f&&attack.animator.GetLayerWeight(holding)>.99f,"Rifle sprint uses run legs and retains two-hand carry");Capture("rifle-sprint-fire");
   await Step(.3f,false,true,Forward);Check(rifle.IsAiming&&!motor.IsSprinting,"Releasing sprint restores held RMB walk aim");
   await Step(.25f,false,false,Forward);Check(!rifle.IsAiming,"Releasing RMB exits aiming");Capture("rifle-walk-carry");
   shots=rifle.ShotsFired;await Step(.25f,true,false,Forward,Sprint);Check(rifle.ShotsFired>shots&&!rifle.IsAiming,"Sprint hip fire works without RMB");
   await Step(.5f,false,false,Right,Sprint);Check(motor.IsSprinting&&Vector3.Dot(actor.transform.forward,Vector3.right)>.8f,"Unfired sideways sprint turns the body and carried rifle into travel");Capture("rifle-side-sprint-carry");
   await Step(.1f);Check(rifle.TryReload(),"Rifle reload remains available");await Step(.1f,false,true,Forward);Check(!rifle.IsAiming&&rifle.IsReloading,"Reload does not enter aim");
   await VerifyPressure();
   Finish(null);
  }
  catch(Exception e){Finish(e);}
 }
 async Task VerifyPressure()
 {
  section="pressure mesh verification";rifle.CancelReload();await Reset();weapons.TryEquip(SahurWeaponLoadout.Weapon.Stick);await Step(.3f);
  var visuals=actor.GetComponent<CombatImpactVisuals>();
  Check(visuals&&visuals.GetComponentsInChildren<ParticleSystem>(true).Length==0,"Combat presentation contains no particle systems");
  int landed=visuals.LandingEmissions;
  await Step(.08f,false,false,GameInputSettings.Get(GameInputSettings.Action.Jump));attack.TriggerAttack();
  await Step(1.5f);Check(visuals.LandingEmissions==landed+1,"Native jump chop landing emits exactly one ground pressure ring");
  await Reset();
  var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Native confirmed-hit review target";target.transform.position=capsule.bounds.center+Vector3.forward*8;target.transform.localScale=new Vector3(2,3,1.5f);
  var receiver=target.AddComponent<PressureDamageReceiverV256>();var response=target.AddComponent<CombatImpactResponse>();
  var materials=new Material[2];for(int i=0;i<2;i++){materials[i]=new Material(Shader.Find("Universal Render Pipeline/Lit"));materials[i].color=i==0?new Color(.13f,.23f,.28f):new Color(.7f,.57f,.35f);}
  target.GetComponent<Renderer>().sharedMaterial=materials[0];
  for(int i=0;i<9;i++){var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Refraction review backdrop";panel.transform.position=target.transform.position+new Vector3((i-4)*.65f,0,2);panel.transform.localScale=new Vector3(.5f,8,.15f);panel.GetComponent<Renderer>().sharedMaterial=materials[i%2];panel.GetComponent<Collider>().enabled=false;}
  var data=reviewCamera.GetUniversalAdditionalCameraData();data.requiresColorOption=CameraOverrideOption.On;data.requiresDepthOption=CameraOverrideOption.On;await Step(.55f);
  Vector3 point=target.transform.position-Vector3.forward*.8f;
  CapturePressure("normal-before",point,false);
  int emitted=visuals.Emissions;
  Check(CombatHitFeedback.Apply(actor,receiver,5,point,CombatHitKind.ComboOne,Vector3.forward),"Confirmed light hit resolves actual damage");
  Check(receiver.damage==5&&visuals.Emissions==emitted+1,"One confirmed light hit drives one pressure ring");
  await Step(.055f);CapturePressure("normal-pressure",point,true);await Step(.4f);
  emitted=visuals.Emissions;CombatHitFeedback.Apply(actor,receiver,9,point,CombatHitKind.ChargedHeavy,Vector3.forward);
  Check(visuals.Emissions==emitted+2,"Confirmed heavy hit drives a paired pressure front");await Step(.07f);CapturePressure("heavy-pressure",point,true);await Step(.4f);
  response.blocksMelee=true;float damage=receiver.damage;emitted=visuals.Emissions;CombatHitFeedback.Apply(actor,receiver,9,point,CombatHitKind.ComboOne,Vector3.forward);
  Check(receiver.damage==damage&&visuals.Emissions==emitted+1,"Blocked contact gets one ring without damage");await Step(.06f);CapturePressure("block-pressure",point,true);response.blocksMelee=false;await Step(.4f);
  var originalRenderers=visuals.GetComponentsInChildren<MeshRenderer>(true).Select(x=>x.GetInstanceID()).OrderBy(x=>x).ToArray();
  var config=CombatFeelProfile.Default.heavy;var hit=new CombatHitEvent(actor,receiver,point,Vector3.forward,CombatHitKind.ChargedHeavy,CombatHitResult.HeavyHit,1);
  emitted=visuals.Emissions;for(int i=0;i<50;i++)visuals.Impact(hit,config,CombatMaterial.Flesh,1);
  Check(visuals.Emissions-emitted<=4&&visuals.ActivePulseCount<=CombatImpactVisuals.PulseCapacity,"Same-frame multi-hit burst respects pressure budget");
  for(int i=0;i<50;i++){visuals.Impact(hit,config,CombatMaterial.Flesh,1);await Frames(1);}
  Check(visuals.ActivePulseCount<=CombatImpactVisuals.PulseCapacity&&originalRenderers.SequenceEqual(visuals.GetComponentsInChildren<MeshRenderer>(true).Select(x=>x.GetInstanceID()).OrderBy(x=>x)),"Sustained hits reuse the same bounded renderers");
  await Step(.5f);Check(visuals.ActivePulseCount==0,"All pressure rings expire and return to idle");
  visuals.Impact(hit,config,CombatMaterial.Flesh,1);await Step(.06f);int active=visuals.ActivePulseCount;Time.timeScale=0;await Frames(5);Check(visuals.ActivePulseCount==active,"Pause freezes pressure lifetime");Time.timeScale=1;await Step(.5f);
  // Explicit fallback: keep the same shader and geometry without opaque/depth copies.
  data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;await Step(.55f);
  var field=typeof(CombatImpactVisuals).GetField("ringMaterial",F);var material=(Material)field.GetValue(visuals);
  Check(material.GetFloat("_UseRefraction")==0&&material.GetFloat("_UseDepth")==0,"Camera without scene textures selects transparent fallback");
  CapturePressure("fallback-before",point,false);visuals.Impact(hit,config,CombatMaterial.Flesh,1);await Step(.07f);CapturePressure("fallback-pressure",point,true);await Step(.5f);
  emitted=visuals.Emissions;visuals.Rush(point+Vector3.one*1000,Vector3.forward);Check(visuals.Emissions==emitted,"Distant pressure visuals are culled");
  visuals.pressureVisibility=0;visuals.Impact(hit,config,CombatMaterial.Flesh,1);Check(visuals.ActivePulseCount==0,"Visibility zero suppresses pressure");visuals.pressureVisibility=.85f;
  visuals.Impact(hit,config,CombatMaterial.Flesh,1);visuals.enabled=false;Check(visuals.ActivePulseCount==0,"Disabling the effect clears active rings");visuals.enabled=true;
  var template=Resources.Load<Material>("Combat/PressureAir");Check(template&&!ShaderUtil.ShaderHasError(template.shader),"Referenced pressure shader compiles and is retained through Resources");
  Check(visuals.GetComponentsInChildren<ParticleSystem>(true).Length==0,"No particles were created during pressure playback");
 }
 void CapturePressure(string name,Vector3 focus,bool effect)
 {
  reviewCamera.transform.position=focus+new Vector3(1.7f,1.2f,-7);reviewCamera.transform.LookAt(focus);reviewCamera.fieldOfView=42;
  var rt=RenderTexture.GetTemporary(960,540,24);var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try
  {
   reviewCamera.targetTexture=rt;reviewCamera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/"+name+".png",pixels.EncodeToPNG());
   if(effect)
   {
    var renderers=actor.GetComponentsInChildren<MeshRenderer>().Where(x=>x.sharedMaterial&&x.sharedMaterial.shader.name=="DarkBrine/Combat Pressure").ToArray();var enabled=renderers.Select(x=>x.enabled).ToArray();
    try{foreach(var renderer in renderers)renderer.enabled=false;reviewCamera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/"+name+"-without.png",pixels.EncodeToPNG());}
    finally{for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];}
   }
  }
  finally{reviewCamera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);DestroyImmediate(pixels);}
 }
 Key Forward=>GameInputSettings.Get(GameInputSettings.Action.Forward);Key Back=>GameInputSettings.Get(GameInputSettings.Action.Back);Key Right=>GameInputSettings.Get(GameInputSettings.Action.Right);Key Sprint=>GameInputSettings.Get(GameInputSettings.Action.Sprint);
 async Task Reset()
 {
  left=right=false;keys=Array.Empty<Key>();attack.SuspendForSwimming();motor.RestoreSavedPose(home,Quaternion.identity);stamina.currentStamina=100;await Step(.7f);stamina.currentStamina=100;
 }
 async Task Step(float seconds,bool l=false,bool r=false,params Key[] input)
 {
  left=l;right=r;keys=input;await Frames(Mathf.CeilToInt(seconds*hz));
 }
 async Task Frames(int count){for(int i=0;i<count;i++){tick=new TaskCompletionSource<bool>();await tick.Task;}}
 void Inject()
 {
  if(finished||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
  keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
 }
 void LateUpdate()
 {
  if(finished)return;frame++;
  if(frame>hz*60){Finish(new Exception("Native input review timed out at "+section));return;}
  if(actor&&motor&&attack&&rifle&&frame%5==0)rows.Add(new {frame,section,position=actor.transform.position.ToString("F4"),motor.CanUseGroundAttack,motor.CanMaintainGroundCharge,motor.IsSprinting,attack.IsCharging,attack.ChargeElapsed,attack.IsHeavyAttackActive,rifle.IsAiming,rifle.ShotsFired});
  if(actor&&capsule)
  {
   var pressure=actor.GetComponent<CombatImpactVisuals>();
   if(pressure&&pressure.RushEmissions>lastRush){lastRush=pressure.RushEmissions;rushCapture=frame+3;}
   if(pressure&&pressure.LandingEmissions>lastLanding){lastLanding=pressure.LandingEmissions;landingCapture=frame+3;}
   if(frame==rushCapture)Capture("native-rush-pressure-"+lastRush);
   if(frame==landingCapture)Capture("native-landing-pressure-"+lastLanding);
  }
  if(frame%120==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {frame,section}));
  var complete=tick;tick=null;complete?.TrySetResult(true);
 }
 void Capture(string name)
 {
  Vector3 focus=capsule.bounds.center;reviewCamera.transform.position=focus+new Vector3(8,3,-10);reviewCamera.transform.LookAt(focus);
  var rt=RenderTexture.GetTemporary(960,540,24);var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);var old=RenderTexture.active;
  try{reviewCamera.targetTexture=rt;reviewCamera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/"+name+".png",pixels.EncodeToPNG());}
  finally{reviewCamera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);DestroyImmediate(pixels);}
 }
 void Finish(Exception error)
 {
  if(finished)return;finished=true;if(error!=null)File.WriteAllText(folder+"/error.txt",error.ToString());
  File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=error==null,hz,frame,checks,rows},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;
 }
 void OnDestroy()
 {
  InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();Time.captureDeltaTime=oldDelta;Time.timeScale=oldTime;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;
 }
}

public sealed class PressureDamageReceiverV256 : MonoBehaviour, Mavis.IDamageable
{
 public float damage;public void ApplyDamage(float amount,Vector3 point){damage+=amount;}
}
