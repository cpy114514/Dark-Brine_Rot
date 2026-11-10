using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;
public static class ProbeGameplayFeel20261109
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string key,object value)=>o.GetType().GetField(key,F).SetValue(o,value);
 static object Call(object o,string key,params object[] values)=>o.GetType().GetMethod(key,F).Invoke(o,values);
 static async Task Frame(){int f=Time.frameCount;while(f==Time.frameCount){if(!Application.isPlaying)throw new Exception("Play ended");await Task.Delay(5);}}
 public static async Task<object> Run(string label="before",int hz=60)
 {
  if(!Application.isPlaying)throw new Exception("Owned isolated Play required");
  float oldScale=Time.timeScale,oldCapture=Time.captureDeltaTime;var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;var oldCursor=Cursor.lockState;
  var tags=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.tag)).ToArray();
  Scene scene=default;GameObject root=null;Keyboard keyboard=null;Mouse mouse=null;
  var report=new List<object>();
  try
  {
   Time.timeScale=1;Time.captureDeltaTime=1f/hz;await Frame();await Frame();
   scene=SceneManager.CreateScene("Gameplay feel isolated probes");root=new GameObject("Temporary feel probe");SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(18000,800,18000);root.SetActive(false);
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.AddComponent<GameSaveExcluded>();
   foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   root.SetActive(true);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   var motor=actor.GetComponent<ThirdPersonPlayerController>();var body=actor.GetComponent<CharacterController>();var attack=actor.GetComponent<SahurAttack>();var animator=attack.animator;
   motor.moveSpeed=9.6f;motor.sprintMultiplier=1.75f;motor.acceleration=68;motor.deceleration=84;motor.turnSpeedDegrees=780;motor.seaLevel=-2000;motor.waterSplashes=false;motor.cameraCollision=false;
   motor.enabled=true;motor.ExternalControlLock=motor.ExternalMovementLock=false;
   keyboard=InputSystem.AddDevice<Keyboard>("Feel probe keyboard");mouse=InputSystem.AddDevice<Mouse>("Feel probe mouse");keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(mouse,new MouseState());
   foreach(var t in tags)if(t.tag=="MainCamera")t.c.tag="Untagged";
   var camera=new GameObject("Probe feedback camera",typeof(Camera));camera.transform.SetParent(root.transform,false);camera.tag="MainCamera";camera.GetComponent<Camera>().enabled=false;
   foreach(float angle in new[]{0f,15f,30f,40f})foreach(bool sprint in new[]{false,true})
   {
    var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="Measured slope";ramp.transform.SetParent(root.transform,false);ramp.transform.localPosition=Vector3.zero;ramp.transform.localScale=new Vector3(40,1,160);ramp.transform.rotation=Quaternion.Euler(angle,0,0);Physics.SyncTransforms();
    Vector3 at=root.transform.position+Vector3.back*35;
    if(!ramp.GetComponent<Collider>().Raycast(new Ray(at+Vector3.up*120,Vector3.down),out var contact,240))throw new Exception("No slope");
    float sole=(body.center.y-body.height*.5f)*Mathf.Abs(actor.transform.lossyScale.y);
    motor.RestoreSavedPose(contact.point+Vector3.up*(.02f-sole),Quaternion.identity);Set(motor,"verticalSpeed",-motor.groundStickSpeed);Set(motor,"yaw",0f);Set(motor,"motionState",0);
    animator.Play("Base Layer.Locomotion",0,0);animator.Update(0);Physics.SyncTransforms();body.Move(Vector3.down*.15f);
    InputState.Change(keyboard,new KeyboardState(sprint?new[]{Key.W,GameInputSettings.Get(GameInputSettings.Action.Sprint)}:new[]{Key.W}));
    int grounded=0;float maxGap=0;var start=actor.transform.position;
    for(int i=0;i<hz*2;i++)
    {
     Call(motor,"Update");if(body.isGrounded)grounded++;
     var foot=new Vector3(body.bounds.center.x,body.bounds.min.y,body.bounds.center.z);
     if(ramp.GetComponent<Collider>().Raycast(new Ray(foot+Vector3.up*20,Vector3.down),out var ground,60))maxGap=Mathf.Max(maxGap,foot.y-ground.point.y);
    }
    report.Add(new{kind="slope",hz,angle,sprint,groundedFrames=grounded,totalFrames=hz*2,maxBottomGap=maxGap,travel=Vector3.ProjectOnPlane(actor.transform.position-start,Vector3.up).magnitude});
    UnityEngine.Object.DestroyImmediate(ramp);
   }
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(200,1,200);Physics.SyncTransforms();
   var weapons=actor.GetComponent<SahurWeaponLoadout>();weapons.enabled=attack.enabled=true;
   motor.ExternalControlLock=motor.ExternalMovementLock=false;weapons.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);
   foreach(string controller in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
   {
    attack.SuspendForSwimming();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controller);animator.Rebind();animator.SetFloat("LightPlaybackSpeed",1);
    motor.RestoreSavedPose(root.transform.position+Vector3.up*(.02f-(body.center.y-body.height*.5f)*Mathf.Abs(actor.transform.lossyScale.y)),Quaternion.identity);Set(motor,"verticalSpeed",-motor.groundStickSpeed);Set(motor,"motionState",0);Set(motor,"yaw",0f);
    Physics.SyncTransforms();body.Move(Vector3.down*.1f);
    Call(attack,"StartComboStage",0);int hash=(int)typeof(SahurAttack).GetField("activeAttackHash",F).GetValue(attack);animator.Play(hash,0,.4f);animator.Update(0);
    var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var foot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
    var armBefore=arm.localRotation;var handBefore=hand.localRotation;var footBefore=foot.localRotation;
    InputState.Change(keyboard,new KeyboardState(Key.W));
    for(int i=0;i<12;i++){Call(motor,"Update");Call(motor,"UpdateLockFootwork");animator.Update(0);}
    int layer=animator.GetLayerIndex("Mobile Blade Legs");
    report.Add(new{kind="blade-footwork",controller,active=motor.BladeFootworkActive,weight=layer>0?animator.GetLayerWeight(layer):0,armDelta=Quaternion.Angle(armBefore,arm.localRotation),handDelta=Quaternion.Angle(handBefore,hand.localRotation),footDelta=Quaternion.Angle(footBefore,foot.localRotation)});
    Call(attack,"StartComboStage",2);Call(motor,"UpdateLockFootwork");
    report.Add(new{kind="finisher-priority",controller,ordinaryFootworkActive=motor.BladeFootworkActive});
   }
   attack.SuspendForSwimming();motor.enabled=false;InputState.Change(keyboard,new KeyboardState());
   var stop=actor.GetComponent<CombatLocalHitStop>();if(!stop)stop=actor.AddComponent<CombatLocalHitStop>();stop.enabled=true;
   animator.speed=1;animator.Play("Base Layer.Locomotion",0,0);animator.Update(0);
   float started=Time.unscaledTime;int frames=0,resumed=0;float before=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
   while(Time.unscaledTime-started<.38f)
   {
    stop.Hold(animator,.06f);await Frame();frames++;
    float after=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;if(after>before+.0001f)resumed++;before=after;
   }
   report.Add(new{kind="repeated-hitstop",hz,elapsed=Time.unscaledTime-started,frames,resumedFrames=resumed,stopped=stop.IsStopped});stop.Release();
  }
  finally
  {
   Time.timeScale=oldScale;Time.captureDeltaTime=oldCapture;
   if(root)UnityEngine.Object.DestroyImmediate(root);if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);
   if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
   if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();foreach(var t in tags)if(t.c)t.c.tag=t.tag;Cursor.lockState=oldCursor;
  }
  Directory.CreateDirectory(".codex/gameplay-feel-20261109");File.WriteAllText(".codex/gameplay-feel-20261109/"+label+"-"+hz+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
