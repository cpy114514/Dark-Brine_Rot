using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class VerifyNativeGroundV011
{
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 public static async Task<object> Verify()
 {
  Require(Application.isPlaying,"Play mode required");
  var priorKeys=Keyboard.current;var priorMouse=Mouse.current;Keyboard keys=null;Mouse mouse=null;
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");var original=File.Exists(save)?File.ReadAllBytes(save):null;
  try
  {
   GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Main.unity");while(!load.isDone)await Task.Delay(30);
   FirstIslandArrival arrival=null;
   for(int i=0;i<650;i++){arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();if(arrival!=null && arrival.IsAwake)break;await Task.Delay(30);}
   Require(arrival!=null && arrival.IsAwake,"Island arrival did not return control");
   var p=arrival.player;var attack=p.GetComponent<SahurAttack>();var hp=p.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;hp.GrantProtection(120);
   if(p.GetComponent<GameSaveExcluded>()==null)p.gameObject.AddComponent<GameSaveExcluded>();
   if(!arrival.HasStick){p.RestoreSavedPose(arrival.lostStickPoint.position+Vector3.up*(.2f-p.LowestFootWorldOffset),p.transform.rotation);Physics.SyncTransforms();Require(arrival.TryPickUp(),"Could not recover the real stick");}
   keys=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
   async Task Hold(int ms,bool charge=false,bool light=false,params Key[] held)
   {keys.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keys,new KeyboardState(held));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,charge).WithButton(MouseButton.Left,light));await Task.Delay(ms);}
   await Hold(350);Require(p.enabled && !p.ExternalControlLock && p.CanUseGroundAttack,"Island movement still locked");
   Key f=GameInputSettings.Get(GameInputSettings.Action.Forward),b=GameInputSettings.Get(GameInputSettings.Action.Back),r=GameInputSettings.Get(GameInputSettings.Action.Right),s=GameInputSettings.Get(GameInputSettings.Action.Sprint),jump=GameInputSettings.Get(GameInputSettings.Action.Jump);
   Vector3 start=p.transform.position;await Hold(1200,false,false,f);float walked=Vector3.ProjectOnPlane(p.transform.position-start,Vector3.up).magnitude;Require(walked>2,"Native walk did not move");
   await Hold(500,false,false,f,s);await Hold(300,true,false,f,s);Require(attack.IsCharging,"Native sprint charge did not start");float before=attack.ChargeElapsed;
   await Hold(300,true,false,s,r);await Hold(300,true,false,s,b);Require(attack.IsCharging && attack.ChargeElapsed>before,"Native turn/reverse cancelled charge");
   Vector3 heavyStart=p.transform.position;await Hold(200,false,false,s,r);Require(attack.IsHeavyAttackActive,"Native release did not start heavy attack");await Hold(300,false,false,r);
   float heavyMovement=Vector3.ProjectOnPlane(p.transform.position-heavyStart,Vector3.up).magnitude;Require(heavyMovement>1,"Heavy attack stopped native locomotion");
   await Hold(1400);await Hold(160,false,true,r);await Hold(180,false,false,r);Require(attack.IsGroundComboActive && attack.CanMoveDuringCombat,"Native mobile light attack failed");
   await Hold(1300);float baseY=p.transform.position.y;await Hold(150,false,false,jump);Require(p.VerticalSpeed>1,"Native island jump did not ascend");float highest=p.transform.position.y;
   for(int i=0;i<18;i++){await Hold(35);highest=Mathf.Max(highest,p.transform.position.y);}Require(highest-baseY>.7f,"Island jump had no visible height");await Hold(1000);
   Require(p.CanUseGroundAttack,"Native landing did not recover ground control");
   var result=new{nativeUpdatesOnly=true,actualIslandTerrain=true,walked,chargeSurvivesSprintTurnAndReverse=true,movingHeavyDistance=heavyMovement,mobileLight=true,jumpHeight=highest-baseY,landingReturnsControl=true};Directory.CreateDirectory(".codex/encounter-v011-controls");File.WriteAllText(".codex/encounter-v011-controls/island-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally
  {
   if(keys!=null)InputSystem.RemoveDevice(keys);if(mouse!=null)InputSystem.RemoveDevice(mouse);priorKeys?.MakeCurrent();priorMouse?.MakeCurrent();GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
  }
 }
}
