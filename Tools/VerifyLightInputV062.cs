using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
public static class VerifyLightInputV062
{
    static Keyboard keyboard;static Mouse mouse;static Key[] keys=Array.Empty<Key>();static bool left;
    static Task pending;
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        InputState.Change(keyboard,new KeyboardState(keys));
        InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left));
    }
    static async Task Hold(float seconds)
    {
        float until=Time.time+seconds;var limit=DateTime.UtcNow.AddSeconds(15);
        while(Time.time<until){if(DateTime.UtcNow>limit)throw new Exception("Native frame loop stalled");await Task.Delay(12);}
    }
    static async Task Click(){left=true;await Hold(.05f);left=false;await Hold(.05f);}
    public static object Start(){pending=Run();return new{started=true,nativeLeftMouseBinding=true};}
    static async Task Run()
    {
        const string output="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/light-input-v062.json";
        GameObject holder=null,floor=null;MonoBehaviour[] originals=Array.Empty<MonoBehaviour>();
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;var oldCursor=Cursor.lockState;bool oldVisible=Cursor.visible;
        try
        {
            Require(Application.isPlaying,"Play Mode required");
            originals=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(b=>b.enabled && (b is ThirdPersonPlayerController || b is SahurAttack || b is SahurBoomerang || b is PlayerInput || b is MainMenuController)).ToArray();
            foreach(var b in originals)b.enabled=false;
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys=Array.Empty<Key>();left=false;
            InputSystem.onBeforeUpdate+=Inject;
            holder=new GameObject("Native left mouse walking combo fixture");holder.SetActive(false);holder.transform.position=new Vector3(2000,500,2000);
            floor=new GameObject("Native combo floor");floor.transform.position=holder.transform.position-Vector3.up*.5f;floor.AddComponent<BoxCollider>().size=new Vector3(150,1,150);
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
            player.AddComponent<GameSaveExcluded>();holder.SetActive(true);
            var movement=player.GetComponent<ThirdPersonPlayerController>();movement.waterSplashes=false;
            var attack=player.GetComponent<SahurAttack>();var stamina=player.GetComponent<PlayerStamina>();stamina.enabled=false;stamina.currentStamina=100;
            var input=player.GetComponent<PlayerInput>();Require(input!=null,"Prefab PlayerInput missing");
            InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();
            var capsule=player.GetComponent<CharacterController>();float sole=(capsule.center.y-capsule.height*.5f)*player.transform.lossyScale.y;
            movement.RestoreSavedPose(holder.transform.position+Vector3.up*(.02f-sole),Quaternion.identity);Physics.SyncTransforms();await Hold(.3f);
            await Click();Require(attack.CurrentComboStage==0,"Actual left mouse binding did not start Combo 1");
            bool restoredFullBody=attack.UsesAnimationRootMotion && !attack.CanMoveDuringCombat;
            Require(restoredFullBody,"Standing left attack bypassed the original whole-body animation");
            while(attack.CurrentAttackPhase<.22f && attack.IsGroundComboActive)await Hold(.02f);
            float before=attack.CurrentAttackPhase;Vector3 origin=player.transform.position;
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Sprint)};
            await Hold(.08f);Require(attack.IsGroundComboActive && attack.CanMoveDuringCombat && !attack.UsesAnimationRootMotion,"W + sprint cancelled or locked the swing");
            float after=attack.CurrentAttackPhase;Require(after>=before-.02f,"Movement restarted the attack phase");
            var stages=new System.Collections.Generic.List<int>{0};
            var deadline=DateTime.UtcNow.AddSeconds(20);
            while(attack.IsGroundComboActive && DateTime.UtcNow<deadline)
            {
                int stage=attack.CurrentComboStage;
                if(!stages.Contains(stage))stages.Add(stage);
                if(stage<2 && attack.CurrentAttackPhase>=.2f && attack.CurrentAttackPhase<.7f)await Click();
                else await Hold(.025f);
                if(stage==1)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Right),GameInputSettings.Get(GameInputSettings.Action.Sprint)};
                if(stage==2)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left)};
            }
            Require(!attack.IsGroundComboActive && stages.SequenceEqual(new[]{0,1,2}),"Three-hit left combo did not complete while changing running direction");
            float distance=Vector3.ProjectOnPlane(player.transform.position-origin,Vector3.up).magnitude;
            Require(distance>.5f,"Moving combo did not physically move Sahur");
            keys=Array.Empty<Key>();await Hold(.3f);
            int layer=attack.animator.GetLayerIndex("Light Attack Upper Body");
            Require(attack.animator.GetLayerWeight(layer)==0,"Light attack layer remained on after recovery");
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,nativeLeftMouseBinding=true,restoredFullBody,
                before,after,stages,distance,moveTurnAndSprintPreserved=true,finished=true},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(output+".error.txt",ex.ToString());}
        finally
        {
            InputSystem.onBeforeUpdate-=Inject;
            if(holder!=null)UnityEngine.Object.DestroyImmediate(holder);if(floor!=null)UnityEngine.Object.DestroyImmediate(floor);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();foreach(var b in originals)if(b!=null)b.enabled=true;
            Cursor.lockState=oldCursor;Cursor.visible=oldVisible;
        }
    }
}
