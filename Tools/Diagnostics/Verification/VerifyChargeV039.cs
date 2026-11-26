using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public static class VerifyChargeV039
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object o,string method)=>o.GetType().GetMethod(method,Private).Invoke(o,null);
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,Private).GetValue(o);
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static Task pending;
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run(){try{var result=await Verify();System.IO.File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/charge-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));}catch(Exception ex){System.IO.File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/charge-validation-error.txt",ex.ToString());}}
    public static async Task<object> Verify(bool probeOnly=false)
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;
        var oldCursor=Cursor.lockState;bool oldVisible=Cursor.visible;float oldTime=Time.timeScale;
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        // Input events exercise the same Update paths as gameplay, without moving the real player.
        var originals=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(b=>b.enabled && (b is ThirdPersonPlayerController || b is SahurAttack || b is SahurBoomerang || b is PlayerInput || b is MainMenuController)).ToArray();
        foreach(var b in originals)b.enabled=false;
        var holder=new GameObject("Charge sprint input fixture");holder.SetActive(false);holder.transform.position=new Vector3(2000,500,2000);
        var floor=new GameObject("Charge sprint input floor");floor.transform.position=holder.transform.position-Vector3.up*.5f;
        floor.AddComponent<BoxCollider>().size=new Vector3(150,1,150);
        var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
        player.AddComponent<GameSaveExcluded>();
        var movement=player.GetComponent<ThirdPersonPlayerController>();movement.waterSplashes=false;
        var attack=player.GetComponent<SahurAttack>();
        foreach(var input in player.GetComponentsInChildren<PlayerInput>(true))input.enabled=false;
        var reports=new List<object>();
        try
        {
            Time.timeScale=1f;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();
            holder.SetActive(true);
            var capsule=player.GetComponent<CharacterController>();
            float sole=(capsule.center.y-capsule.height*.5f)*player.transform.lossyScale.y;
            movement.RestoreSavedPose(holder.transform.position+Vector3.up*(.02f-sole),Quaternion.identity);Physics.SyncTransforms();
            var stamina=player.GetComponent<PlayerStamina>();stamina.enabled=false;stamina.currentStamina=100;
            Key f=GameInputSettings.Get(GameInputSettings.Action.Forward),b=GameInputSettings.Get(GameInputSettings.Action.Back);
            Key l=GameInputSettings.Get(GameInputSettings.Action.Left),r=GameInputSettings.Get(GameInputSettings.Action.Right),s=GameInputSettings.Get(GameInputSettings.Action.Sprint);
            async Task Step(bool held,params Key[] keys)
            {
                Cursor.lockState=CursorLockMode.Locked;keyboard.MakeCurrent();mouse.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,held));InputSystem.Update();
                Call(movement,"Update");Call(attack,"Update");
                await Task.Delay(25);
            }
            for(int i=0;i<12;i++)await Step(false);
            Require(movement.CanUseGroundAttack,"Fixture did not settle on its floor.");
            await Step(true);
            if(probeOnly && !attack.IsCharging)return new{started=false,cursor=Cursor.lockState.ToString(),mouseHeld=Mouse.current.rightButton.isPressed,
                movement.CanUseGroundAttack,movement.ExternalControlLock,movement.ExternalMovementLock,pause=PauseSettingsMenu.IsOpen,ui=SahurLoadoutUI.BlocksInput,
                attack.enabled,attack.IsCombatMotionActive,lastFire=Get<float>(attack,"lastFireTime"),now=Time.time,boomerang=player.GetComponent<SahurBoomerang>().IsBusy};
            Require(attack.IsCharging,"Right mouse did not begin charging.");
            float elapsed=attack.ChargeElapsed;
            await Step(true,f,s);
            if(probeOnly)return new{cancelledOnStartingRun=!attack.IsCharging,elapsedBefore=elapsed,elapsedAfter=attack.ChargeElapsed};
            Require(attack.IsCharging,"Starting to sprint cancelled charge.");
            await Step(true,s,r);Require(attack.IsCharging,"Changing sprint direction cancelled charge.");
            await Step(true,s,b);Require(attack.IsCharging,"Reversing sprint direction cancelled charge.");
            Require(attack.ChargeElapsed>elapsed,"Changing direction restarted the charge timer.");
            reports.Add(new{scenario="Stationary charge, start sprint, turn and reverse",preserved=true});
            while(attack.ChargeElapsed<attack.fullChargeTime*.5f)await Step(true,s,b);
            float halfPhase=attack.ChargePose01;
            Require(halfPhase>attack.maxChargePosePhase*.40f && halfPhase<attack.maxChargePosePhase*.65f,"Charge pose reached its hold too early.");
            while(attack.ChargeElapsed<attack.fullChargeTime)await Step(true,s,b);
            Require(Mathf.Abs(attack.ChargePose01-attack.maxChargePosePhase)<.001f,"Full charge did not reach its held pose.");
            float releaseStarted=Time.time;float firstHitTime=-1;float lastPhase=-1;
            var hand=attack.animator.GetBoneTransform(HumanBodyBones.RightHand);Quaternion heldHand=Quaternion.Inverse(player.transform.rotation)*hand.rotation;
            await Step(false,s,b);Require(attack.IsHeavyAttackActive,"Releasing right mouse did not start the charged attack.");
            await Step(false,s,l);Require(attack.IsHeavyAttackActive,"Changing direction cancelled the movable heavy strike.");
            float deadline=Time.realtimeSinceStartup+5;
            float initialPhase=attack.HeavyAttackPhase;float joinAngle=Quaternion.Angle(heldHand,Quaternion.Inverse(player.transform.rotation)*hand.rotation);
            while(attack.IsHeavyAttackActive && Time.realtimeSinceStartup<deadline)
            {
                float phase=attack.HeavyAttackPhase;
                Require(phase+.02f>=lastPhase,"Release restarted or reversed the attack clip.");lastPhase=phase;
                if(firstHitTime<0 && phase>=attack.heavySwingWindowStart)firstHitTime=Time.time-releaseStarted;
                await Step(false,s,l);
            }
            reports.Add(new{scenario="Gradual 1.8 second windup and continuous mobile release",halfPhase,fullPhase=attack.maxChargePosePhase,initialPhase,joinAngleDegrees=joinAngle,firstHitSeconds=firstHitTime,releaseSeconds=Time.time-releaseStarted});
            Require(firstHitTime>=.16f && firstHitTime<.36f,"Full release failed to accelerate into its hit window.");
            Require(Time.time-releaseStarted<1.2f,"Full release stalled before recovery.");
            Require(!attack.IsHeavyAttackActive,"Charged strike did not finish.");
            for(int i=0;i<14;i++)await Step(false,s,f);
            stamina.currentStamina=100;
            await Step(true,s,f);Require(attack.IsCharging,"Charging while already sprinting failed.");
            float first=attack.ChargeElapsed;
            await Step(true,s,r);await Step(true,s,b);await Step(true,s,l);
            Require(attack.IsCharging && attack.ChargeElapsed>first,"Sprint turns cleared or reset the second charge.");
            // Let acceleration settle after reversing; a turn naturally reduces speed briefly.
            for(int i=0;i<20;i++)await Step(true,s,l);
            Require(attack.IsCharging && attack.ChargeElapsed>first,"Holding sprint reset the charge timer.");
            var velocity=Get<Vector3>(movement,"planarVelocity");
            Require(velocity.magnitude>movement.moveSpeed,"Charging prevented sprint speed.");
            int layer=attack.animator.GetLayerIndex("Charge Upper Body");
            Require(layer>0 && attack.animator.GetLayerWeight(layer)>.1f,"Charge arm overlay disappeared while sprinting.");
            Require(attack.animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Charge replaced locomotion.");
            reports.Add(new{scenario="Charge while sprinting, turn through three directions",preserved=true,speed=velocity.magnitude});
            await Step(false,s,l);Require(attack.IsHeavyAttackActive,"Second charge failed to release.");
            attack.SuspendForSwimming();
            // Respect the normal .5s attack cooldown after the second heavy release.
            for(int i=0;i<28;i++)await Step(false);
            attack.TriggerAttack();Require(attack.IsGroundComboActive,"Light attack fixture failed to start.");
            await Step(false,r);Require(attack.IsGroundComboActive,"Moving unexpectedly cancelled the light attack.");
            return new{reports,chargeTimerContinuous=true,releaseAttacks=true,movableHeavyTurnPreserved=true,movableLightAttackPreserved=true};
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(floor);
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();
            foreach(var original in originals)if(original!=null)original.enabled=true;
            Cursor.lockState=oldCursor;Cursor.visible=oldVisible;Time.timeScale=oldTime;
        }
    }
}
