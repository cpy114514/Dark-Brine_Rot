using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Mavis;
public static class RegressionCombatMovement0545
{
    static Keyboard keyboard;static Mouse mouse;static Key[] keys=Array.Empty<Key>();static bool left;static Task pending;
    static void Inject(){if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left));}
    static async Task Wait(float seconds){float end=Time.time+seconds;var limit=DateTime.UtcNow.AddSeconds(30);while(Time.time<end){if(DateTime.UtcNow>limit)throw new Exception("Native frames stalled");await Task.Delay(8);}}
    static void Check(bool x,string message){if(!x)throw new Exception(message);}
    static async Task Click(){left=true;await Wait(.06f);left=false;await Wait(.06f);}
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        const string output="D:/indiegameDev/unity/Dark Brine_Rot/.codex/combat-combinations-20261019/heartbeat-0545/moving-light-regression.json";
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;
        float originalCapture=Time.captureDeltaTime;
        try
        {
            CombatFeelPreview.Spawn();CombatFeelPreview.Target.transform.position+=Vector3.forward*60;
            Time.captureDeltaTime=1f/60;
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys=Array.Empty<Key>();left=false;InputSystem.onBeforeUpdate+=Inject;
            var player=CombatFeelPreview.Player;var attack=player.GetComponent<SahurAttack>();var motor=player.GetComponent<ThirdPersonPlayerController>();var input=player.GetComponent<PlayerInput>();
            InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();await Wait(.8f);
            await Click();Check(attack.CurrentComboStage==0,"Native left mouse starts Combo 1");Check(attack.UsesAnimationRootMotion,"Standing combo retains its authored body/root motion");
            while(attack.CurrentAttackPhase<.22f && attack.IsGroundComboActive)await Wait(.01f);
            float before=attack.CurrentAttackPhase;Vector3 origin=player.transform.position;
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Sprint)};await Wait(.08f);
            Check(attack.IsGroundComboActive && attack.CanMoveDuringCombat && !attack.UsesAnimationRootMotion,"Movement blends into the upper-body attack without canceling");
            Check(attack.CurrentAttackPhase>=before-.02f,"Movement preserves current attack time");
            var stages=new HashSet<int>{0};float end=Time.unscaledTime+10;
            while(attack.IsGroundComboActive && Time.unscaledTime<end)
            {
                int stage=attack.CurrentComboStage;stages.Add(stage);
                if(stage<2 && attack.CurrentAttackPhase>=.2f && attack.CurrentAttackPhase<.7f)await Click();else await Wait(.02f);
                if(stage==1)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Right),GameInputSettings.Get(GameInputSettings.Action.Sprint)};
                if(stage==2)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left)};
            }
            float distance=Vector3.ProjectOnPlane(player.transform.position-origin,Vector3.up).magnitude;
            Check(!attack.IsGroundComboActive && stages.Count==3 && distance>.5f,"All three stages complete while sprinting and changing direction");keys=Array.Empty<Key>();await Wait(.35f);
            int layer=attack.animator.GetLayerIndex("Light Attack Upper Body");Check(attack.animator.GetLayerWeight(layer)==0,"Mobile layer returns to zero");
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,nativeInput=true,fixedSimulationHz=60,stages,distance,phasePreserved=true,rootMotionOwnershipPreserved=true,layerRestored=true},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(output+".error.txt",e.ToString());}
        finally
        {
            InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            if(oldKeyboard!=null && oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null && oldMouse.added)oldMouse.MakeCurrent();CombatFeelPreview.Clear();
            Time.captureDeltaTime=originalCapture;
        }
    }
}
