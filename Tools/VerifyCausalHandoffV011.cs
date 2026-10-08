using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class VerifyCausalHandoffV011
{
 static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
 static async Task<Story1WreckBattle> Begin()
 {
  GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(20);await Task.Delay(300);
  var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
  for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
  Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"No playable fight");sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);return battle;
 }
 static void Request(ThirdPersonPlayerController p,Story1WreckBattle b){var hp=p.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth*b.finalePlayerHealthRatio+1;hp.GrantProtection(0);hp.ApplyDamage(1,p.transform.position);}
 static void Record(string name,object data){Directory.CreateDirectory(".codex/encounter-v011");File.WriteAllText(".codex/encounter-v011/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));}
 public static async Task<object> ChargeDeadline()
 {
  var oldKeys=Keyboard.current;var oldMouse=Mouse.current;var keys=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
  try
  {
   var battle=await Begin();var p=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>().sahur;var attack=p.GetComponent<SahurAttack>();
   keys.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keys,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));await Task.Delay(350);Require(attack.IsCharging,"Native charge did not start");
   Request(p,battle);float at=Time.time;
   while(battle.Finale==null && Time.time-at<2)
   {
    await Task.Delay(25);float elapsed=Time.time-at;
    if(elapsed>.2f && elapsed<.85f)Require(attack.IsCharging && p.enabled,"Safe handoff interrupted a held charge early");
   }
   float duration=Time.time-at;Require(battle.Finale!=null && duration>=.95f && duration<=1.2f,"Held charge missed the one-second safe handoff: "+duration);
   var data=new{nativeHeldCharge=true,waitedForDeadline=true,seconds=duration,state=battle.Handoff.state.ToString()};Record("charged-handoff",data);return data;
  }
  finally{InputSystem.RemoveDevice(keys);InputSystem.RemoveDevice(mouse);oldKeys?.MakeCurrent();oldMouse?.MakeCurrent();GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);}
 }
 public static async Task<object> UnconfirmedTail()
 {
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
  try
  {
   var battle=await Begin();var p=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>().sahur;Request(p,battle);
   for(int i=0;i<100 && battle.Finale==null;i++)await Task.Delay(20);var film=battle.Finale;Require(film!=null,"No cinematic");
   // Fault injection: withhold final contact authorization. A timed animation
   // cue must not independently invent stick loss, damage or narrative death.
   var times=(float[])typeof(Story1WreckFinale).GetField("contactTimes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film);times[5]=50;
   for(int i=0;i<1800 && battle.CurrentPhase==Story1WreckBattle.Phase.Finale;i++)await Task.Delay(20);
   Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && battle.Finale==null,"Unconfirmed tail did not release control");
   Require(p.enabled && !p.ExternalControlLock && p.GetComponent<SahurAttack>().enabled && p.GetComponent<PlayerHealth>().currentHealth>0,"Missed tail invented a death or retained cinematic control");
   Require(p.Motion.State!=SahurControlState.Cinematic && p.Motion.State!=SahurControlState.Unconscious,"Missed contact left narrative ownership");
   var data=new{faultInjection="Final contact window withheld",noUnconfirmedDefeat=true,currentPoseReturnsToPlayableFight=true,health=p.GetComponent<PlayerHealth>().currentHealth};Record("unconfirmed-tail",data);return data;
  }
  finally{var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(p!=null && p.GetComponent<GameSaveExcluded>()==null)p.gameObject.AddComponent<GameSaveExcluded>();GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);}
 }
}
