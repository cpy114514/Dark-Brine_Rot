using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;
public static class TakeSahur063
{
 public static object Start(string label)
 {
  if(!Application.isPlaying)throw new Exception("Play required");
  if(File.Exists(".codex/sahur-polish063/"+label+"/result.json"))throw new Exception("Choose a new take label to preserve existing evidence");
  foreach(var sceneName in new[]{"Gameplay","Enemies","Main"}){var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);if(scene.isLoaded)foreach(var root in scene.GetRootGameObjects())root.SetActive(false);}
  var go=new GameObject("Sahur animation polish take");var r=go.AddComponent<SahurTake063>();r.label=label;r.Begin();return "Started "+label;
 }
}
[DefaultExecutionOrder(32000)]public sealed class SahurTake063:MonoBehaviour
{
 public string label;string dir,section="setup";int frame;Keyboard keyboard;Mouse mouse;Key[] keys=Array.Empty<Key>();bool right,left;
 InputSettings originalSettings,settings;string originalSettingsJson;HideFlags originalSettingsFlags;List<InputDevice> disabled=new List<InputDevice>();
 GameObject actor,root;ThirdPersonPlayerController motor;SahurAttack attack;SahurWeaponLoadout weapons;Animator anim;CharacterController body;Camera view;RenderTexture rt;Texture2D pixels;
 readonly List<object> rows=new List<object>(),checks=new List<object>();readonly List<string> errors=new List<string>();float previousCapture;
 Vector3 ground=new Vector3(20000,1000,20000);float firstLand=-1,lastLand=-1,lastArmWeight=-1,maxCancelDrop;bool recording;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string name)=>typeof(ThirdPersonPlayerController).GetField(name,F).GetValue(motor);
 void Check(bool ok,string name,object evidence=null)=>checks.Add(new{ok,name,evidence});
 void Feed(){if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,right).WithButton(MouseButton.Left,left));}
 void Log(string s,string t,LogType ty){if(ty==LogType.Exception||ty==LogType.Error)errors.Add(s);}
 public void Begin(){dir=".codex/sahur-polish063/"+label;Directory.CreateDirectory(dir+"/frames");StartCoroutine(Guard());}
 IEnumerator Guard(){var e=Run();while(true){object value;try{if(!e.MoveNext())break;value=e.Current;}catch(Exception ex){errors.Add(ex.ToString());break;}yield return value;}Finish();}
 IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
 IEnumerator Reset(string name)
 {
  recording=false;section=name;keys=Array.Empty<Key>();left=right=false;attack.SuspendForSwimming();yield return Frames(15);
  float sole=(body.center.y-body.height*.5f)*actor.transform.lossyScale.y;motor.RestoreSavedPose(ground+Vector3.up*(.08f-sole),Quaternion.identity);attack.animator.speed=1;actor.GetComponent<PlayerStamina>().currentStamina=100;Cursor.lockState=CursorLockMode.Locked;
  yield return Frames(30);recording=true;
 }
 IEnumerator Run()
 {
  previousCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;
  originalSettings=InputSystem.settings;originalSettingsJson=EditorJsonUtility.ToJson(originalSettings);originalSettingsFlags=originalSettings.hideFlags;settings=Instantiate(originalSettings);settings.hideFlags=HideFlags.DontSave;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
  foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){disabled.Add(d);InputSystem.DisableDevice(d);}keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Feed;Application.logMessageReceived+=Log;
  bool island=label.StartsWith("island",StringComparison.Ordinal);
  if(island){if(!Physics.Raycast(new Vector3(288,150,346),Vector3.down,out var hit,200,~0,QueryTriggerInteraction.Ignore))throw new Exception("No real island ground");ground=hit.point;}
  root=new GameObject("Disposable Sahur animation fixture");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=ground-Vector3.up*.5f;floor.transform.localScale=new Vector3(180,1,180);if(island)Destroy(floor.GetComponent<Collider>());
  var sun=new GameObject("Native review key",typeof(Light));sun.transform.SetParent(root.transform);sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=island?0:1.2f;if(island)floor.GetComponent<Renderer>().enabled=false;
  var control=new GameObject("Control view",typeof(Camera));control.tag="MainCamera";control.transform.SetParent(root.transform);control.transform.rotation=Quaternion.identity;
  var cameraGO=new GameObject("Native review view",typeof(Camera));cameraGO.transform.SetParent(root.transform);view=cameraGO.GetComponent<Camera>();view.enabled=false;view.fieldOfView=58;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.09f,.13f,.16f);
  rt=new RenderTexture(960,540,24);rt.Create();view.targetTexture=rt;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
  actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.AddComponent<GameSaveExcluded>();actor.transform.position=ground+Vector3.up*4;
  motor=actor.GetComponent<ThirdPersonPlayerController>();attack=actor.GetComponent<SahurAttack>();weapons=actor.GetComponent<SahurWeaponLoadout>();body=actor.GetComponent<CharacterController>();
  yield return Frames(30);anim=motor.CharacterAnimator;
  yield return Reset("walk-turn-stop");keys=new[]{Key.W};yield return Frames(40);keys=new[]{Key.D};yield return Frames(25);keys=Array.Empty<Key>();yield return Frames(30);
  yield return Reset("moving-landing");keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Sprint)};yield return Frames(25);keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Sprint),GameInputSettings.Get(GameInputSettings.Action.Jump)};yield return Frames(2);keys=new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Sprint)};yield return Frames(95);
  Check(firstLand>=0,"Moving jump reaches landing");Check(lastLand-firstLand<.26f,"Moving landing recovers promptly",new{hold=lastLand-firstLand});
  yield return Reset("wall-contact");var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=ground+new Vector3(0,4,6);wall.transform.localScale=new Vector3(20,8,1);keys=new[]{Key.W};yield return Frames(90);Check(anim.GetFloat("Speed")<.2f,"Blocked feet settle instead of walking in place",new{animationSpeed=anim.GetFloat("Speed"),realSpeed=body.velocity.magnitude});Destroy(wall);
  foreach(bool blades in new[]{false,true})
  {
   yield return Reset(blades?"blade-charge-cancel":"club-charge-cancel");weapons.Restore(true,blades?SahurWeaponLoadout.Weapon.CapriTwinBlades:SahurWeaponLoadout.Weapon.Stick,false);yield return Frames(15);maxCancelDrop=0;lastArmWeight=-1;right=true;yield return Frames(20);Check(attack.IsCharging,"Actual charge is active before cancellation");typeof(SahurAttack).GetMethod("CancelCharge",F).Invoke(attack,null);right=false;yield return Frames(22);Check(!attack.IsHeavyAttackActive,"Charge cancellation does not release a damage attack");Check(maxCancelDrop<.3f,(blades?"Blade":"Club")+" charge cancellation keeps smooth arm release",new{maxWeightDrop=maxCancelDrop});
  }
  yield return Reset("cinematic-idle-handoff");recording=false;motor.enabled=false;anim.SetFloat("Speed",0);anim.Play("Base Layer.Locomotion",0,.6f);anim.Update(0);float before=anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
  var finish=typeof(ThirdPersonPlayerController).GetMethod("FinishCinematicPose");if(finish!=null)finish.Invoke(motor,new object[]{actor.transform.position,actor.transform.rotation});else motor.RestoreSavedPose(actor.transform.position,actor.transform.rotation);
  anim.Update(.2f);float after=anim.GetCurrentAnimatorStateInfo(0).normalizedTime;Check(after>.55f,"Cinematic handoff retains the current idle pose",new{before,after});motor.enabled=true;recording=true;yield return Frames(10);
  Check(errors.Count==0,"No native runtime errors",errors.ToArray());
 }
 void LateUpdate()
 {
  if(!recording||!actor||!anim)return;frame++;int state=(int)Get("motionState");
  if(section=="moving-landing"&&state==4){if(firstLand<0)firstLand=Time.time;lastLand=Time.time+Time.deltaTime;}
  int layer=(int)typeof(SahurAttack).GetField("chargeLayerIndex",F).GetValue(attack);float weight=layer>=0?anim.GetLayerWeight(layer):0;
  if(section.Contains("charge-cancel")&&lastArmWeight>=0&&lastArmWeight>weight)maxCancelDrop=Mathf.Max(maxCancelDrop,lastArmWeight-weight);lastArmWeight=weight;
  var row=new{frame,section,state,time=Time.time,speed=anim.GetFloat("Speed"),realSpeed=Vector3.ProjectOnPlane(body.velocity,Vector3.up).magnitude,position=actor.transform.position.ToString(),charging=attack.IsCharging,weight};rows.Add(row);
  if(frame%4==0)
  {
   var focus=actor.transform.position+Vector3.up*3;var offset=new Vector3(8,3,-10);view.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset));var previous=RenderTexture.active;
   try{view.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();File.WriteAllBytes(dir+"/frames/"+frame.ToString("D4")+"-"+section+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
  }
 }
 void Finish()
 {
  recording=false;InputSystem.onBeforeUpdate-=Feed;Application.logMessageReceived-=Log;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(originalSettings)InputSystem.settings=originalSettings;else if(!string.IsNullOrEmpty(originalSettingsJson)){var restored=ScriptableObject.CreateInstance<InputSettings>();EditorJsonUtility.FromJsonOverwrite(originalSettingsJson,restored);restored.hideFlags=originalSettingsFlags;InputSystem.settings=restored;}foreach(var d in disabled)if(d.added){InputSystem.EnableDevice(d);if(d is Keyboard k)k.MakeCurrent();if(d is Mouse m)m.MakeCurrent();}Time.captureDeltaTime=previousCapture;
  File.WriteAllText(dir+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,errors,frames=frame},Newtonsoft.Json.Formatting.Indented));File.WriteAllText(dir+"/samples.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));
  if(root)Destroy(root);if(rt){rt.Release();Destroy(rt);}if(pixels)Destroy(pixels);if(settings)Destroy(settings);Destroy(gameObject);
 }
}
