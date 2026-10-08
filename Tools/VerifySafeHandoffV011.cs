using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class VerifySafeHandoffV011
{
    static Keyboard keys;
    static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
    static async Task Hold(int ms,params Key[] held)
    {
        keys.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keys,new KeyboardState(held));await Task.Delay(ms);
    }
    public static async Task<object> VerifyAirborne()
    {
        Require(Application.isPlaying,"Requires Play mode");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        var oldKeyboard=Keyboard.current;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(20);await Task.Delay(350);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Break did not end");var player=sequence.sahur;var health=player.GetComponent<PlayerHealth>();health.GrantProtection(200);
            keys=InputSystem.AddDevice<Keyboard>();await Hold(200);var rider=player.GetComponent<Story1WreckRider>();Require(rider.IsSurfing,"No actual mounted support");
            float start=Time.time;await Hold(60,GameInputSettings.Get(GameInputSettings.Action.Jump));await Hold(80);
            Require(!rider.IsSurfing && player.VerticalSpeed>1,"Jump did not leave support");
            health.currentHealth=health.maxHealth*battle.finalePlayerHealthRatio+1;health.GrantProtection(0);health.ApplyDamage(1,player.transform.position);
            Vector3 airStart=player.transform.position;await Hold(100);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && battle.Finale==null && player.enabled,"Cinematic interrupted actual airborne movement");
            Require(player.transform.position.y>airStart.y,"Requested handoff froze airborne trajectory");
            for(int i=0;i<250 && battle.Finale==null;i++)await Hold(20);
            Require(battle.Finale!=null,"Safe airborne handoff never arrived");
            Require(battle.Handoff.state==SahurControlState.Swimming || battle.Handoff.state==SahurControlState.BoardRiding || battle.Handoff.state==SahurControlState.Grounded,"Handoff claimed airborne state: "+battle.Handoff.state);
            var v=battle.Handoff.velocity;var result=new{airborneMotionContinues=true,handoffWaitedForSupportOrWater=true,state=battle.Handoff.state.ToString(),afterJumpSeconds=Time.time-start,handoffVelocity=new{x=v.x,y=v.y,z=v.z}};Directory.CreateDirectory(".codex/encounter-v011");File.WriteAllText(".codex/encounter-v011/airborne-handoff.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            if(keys!=null)InputSystem.RemoveDevice(keys);keys=null;if(oldKeyboard!=null)oldKeyboard.MakeCurrent();var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(p!=null && p.GetComponent<GameSaveExcluded>()==null)p.gameObject.AddComponent<GameSaveExcluded>();GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
