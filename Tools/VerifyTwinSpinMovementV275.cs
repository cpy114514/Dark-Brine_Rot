using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public sealed class SpinDamageProbeV275 : MonoBehaviour, IDamageable
{
    public int hits; public float damage;
    public void ApplyDamage(float amount,Vector3 point){hits++;damage+=amount;}
}
public static class VerifyTwinSpinMovementV275
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    public static async Task<object> RunWithStateRestore()
    {
        float scale=Time.timeScale,capture=Time.captureDeltaTime;bool paused=EditorApplication.isPaused;
        try
        {
            if(!Application.isPlaying)throw new Exception("Ready Play Mode required");
            Time.timeScale=1;Time.captureDeltaTime=1f/60;EditorApplication.isPaused=false;
            var deadline=DateTime.UtcNow.AddSeconds(5);
            do{await Task.Delay(10);if(!Application.isPlaying)throw new OperationCanceledException("Play stopped");if(DateTime.UtcNow>deadline)throw new TimeoutException("No player tick");}while(Time.deltaTime<=0);
            return Run();
        }
        finally{Time.timeScale=scale;Time.captureDeltaTime=capture;EditorApplication.isPaused=paused;}
    }
    public static object Run()
    {
        if(!Application.isPlaying||Time.deltaTime<=0)throw new Exception("Native positive delta required");
        var checks=new List<string>();var travel=new List<object>();string error=null;
        void Check(bool ok,string description){if(!ok)throw new Exception(description);checks.Add(description);}
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;var roster=InputSystem.devices.Select(d=>(d.deviceId,d.enabled)).ToArray();
        var cursor=Cursor.lockState;bool visible=Cursor.visible;
        var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.tag)).ToArray();
        GameObject container=null;Keyboard keyboard=null;Mouse mouse=null;
        try
        {
            keyboard=InputSystem.AddDevice<Keyboard>("TwinSpinV275ProbeKeyboard");mouse=InputSystem.AddDevice<Mouse>("TwinSpinV275ProbeMouse");keyboard.MakeCurrent();mouse.MakeCurrent();
            InputState.Change(keyboard,new KeyboardState());InputState.Change(mouse,new MouseState());
            container=new GameObject("Temporary twin spin V275 probe");container.SetActive(false);container.transform.position=new Vector3(12000,500,12000);container.hideFlags=HideFlags.DontSave;
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),container.transform);actor.AddComponent<GameSaveExcluded>();
            foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            container.SetActive(true);foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            var motor=actor.GetComponent<ThirdPersonPlayerController>();var attack=actor.GetComponent<SahurAttack>();var weapons=actor.GetComponent<SahurWeaponLoadout>();var stamina=actor.GetComponent<PlayerStamina>();var body=actor.GetComponent<CharacterController>();
            motor.enabled=attack.enabled=weapons.enabled=true;motor.seaLevel=-1000;motor.waterSplashes=false;Cursor.lockState=CursorLockMode.Locked;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(container.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(200,1,200);
            var home=new Vector3(12000,500.03f-(body.center.y-body.height*.5f)*actor.transform.lossyScale.y,12000);
            foreach(var c in cameras)if(c.tag=="MainCamera")c.c.tag="Untagged";
            var probeCamera=new GameObject("Isolated V275 feedback camera");probeCamera.transform.SetParent(container.transform,false);probeCamera.AddComponent<Camera>().enabled=false;probeCamera.tag="MainCamera";
            motor.RestoreSavedPose(home,Quaternion.identity);motor.ExternalControlLock=motor.ExternalMovementLock=false;weapons.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);
            Check(weapons.UsesTwinBlades&&attack.UsesTwinBlades&&attack.stickHitbox,"Native fitted twin blades equipped");
            float dt=Time.deltaTime;
            void Keys(params Key[] keys){keyboard.MakeCurrent();mouse.MakeCurrent();InputState.Change(keyboard,new KeyboardState(keys));}
            void Buttons(bool chord){InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,chord).WithButton(MouseButton.Right,chord));}
            void Reset()
            {
                Keys();Buttons(false);attack.SuspendForSwimming();actor.GetComponent<CombatLocalHitStop>()?.Release();
                motor.ExternalControlLock=motor.ExternalMovementLock=false;motor.RestoreSavedPose(home,Quaternion.identity);stamina.currentStamina=100;
                Physics.SyncTransforms();body.Move(Vector3.down*.2f);Set(motor,"verticalSpeed",-motor.groundStickSpeed);Set(attack,"movementCancelFrame",-1);
            }
            void Begin(bool continuous)
            {
                if(continuous){Buttons(true);Set(attack,"chordLatched",false);Call(attack,"UpdateCombinations");Set(attack,"specialStartedAt",Time.time-1);}
                else Call(attack,"StartComboStage",2);
                int hash=Get<int>(attack,"activeAttackHash");attack.animator.Play(hash,0,.4f);attack.animator.Update(0);
                Check(attack.IsTwinBladeSpinActive&&attack.CanTranslateDuringCombat&&!attack.UsesAnimationRootMotion,"Spin translates through capsule, no second root-motion owner");
                Check(!attack.CanMoveDuringCombat,"Full-body spin keeps its authored animation and full-body hit-stop path");
                Check(continuous?attack.IsSpinning:attack.CurrentComboStage==2,"Correct continuous/third-stage spin selected");
            }
            string[] controllers={"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
            var directions=new[]{(GameInputSettings.Action.Forward,Vector3.forward),(GameInputSettings.Action.Back,Vector3.back),(GameInputSettings.Action.Left,Vector3.left),(GameInputSettings.Action.Right,Vector3.right)};
            foreach(var controller in controllers)foreach(bool continuous in new[]{false,true})
            {
                Reset();attack.animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controller);attack.animator.Rebind();
                foreach(var direction in directions)
                {
                    Reset();Begin(continuous);Keys(GameInputSettings.Get(direction.Item1));
                    int hash=Get<int>(attack,"activeAttackHash");Set(motor,"planarVelocity",direction.Item2*motor.moveSpeed);var start=actor.transform.position;
                    for(int frame=0;frame<12;frame++){Call(motor,"Update");Check(!attack.TryCancelForMovementInput(),"WASD does not cancel spin");}
                    var delta=Vector3.ProjectOnPlane(actor.transform.position-start,Vector3.up);
                    Check(Vector3.Distance(delta,direction.Item2*motor.moveSpeed*dt*12)<.015f,"Directional spin movement follows normal walking speed");
                    Check(Get<int>(attack,"activeAttackHash")==hash&&attack.animator.GetCurrentAnimatorStateInfo(0).fullPathHash==hash,"Moving does not replace the spin animation");
                    travel.Add(new{controller,continuous,direction=direction.Item1.ToString(),distance=delta.magnitude});
                }
                Reset();Begin(continuous);Keys(GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Sprint));
                float sprint=motor.moveSpeed*motor.sprintMultiplier;Set(motor,"planarVelocity",Vector3.forward*sprint);var runStart=actor.transform.position;
                for(int frame=0;frame<8;frame++)Call(motor,"Update");
                Check(motor.IsSprinting&&actor.transform.position.z-runStart.z>motor.moveSpeed*dt*8+.1f,"Sprinting remains available during twin spin");
                Keys();for(int frame=0;frame<80;frame++)Call(motor,"Update");Check(Get<Vector3>(motor,"planarVelocity").sqrMagnitude<.0001f&&attack.IsTwinBladeSpinActive,"Releasing movement brakes without ending the attack");
            }
            Reset();Begin(true);Keys(GameInputSettings.Get(GameInputSettings.Action.Forward));float before=stamina.Current;Call(attack,"UpdateCombinations");
            Check(attack.IsSpinning&&Mathf.Abs(before-stamina.Current-attack.spinStaminaPerSecond*dt)<.001f,"Continuous spin keeps its existing stamina drain");
            Buttons(false);Call(attack,"UpdateCombinations");Check(!attack.IsSpinning&&!attack.IsCombatMotionActive,"Releasing the chord ends continuous spin");
            Reset();Begin(true);stamina.currentStamina=0;Call(attack,"UpdateCombinations");Check(!attack.IsSpinning,"Exhaustion still ends continuous spin");
            Reset();Begin(false);Keys(GameInputSettings.Get(GameInputSettings.Action.Forward));Set(motor,"planarVelocity",Vector3.forward*motor.moveSpeed);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(container.transform,false);wall.transform.position=actor.transform.position+new Vector3(0,5,2);wall.transform.localScale=new Vector3(30,30,.2f);Physics.SyncTransforms();
            var wallStart=actor.transform.position;for(int i=0;i<30;i++)Call(motor,"Update");Check(actor.transform.position.z-wallStart.z<1.9f&&attack.IsTwinBladeSpinActive,"Actual wall blocks moving finisher without cancelling it");UnityEngine.Object.DestroyImmediate(wall);
            Reset();Begin(false);Keys(GameInputSettings.Get(GameInputSettings.Action.Forward));Set(motor,"planarVelocity",Vector3.forward*motor.moveSpeed);motor.ExternalMovementLock=true;var locked=actor.transform.position;Call(motor,"Update");Check(actor.transform.position==locked,"External movement ownership still wins");
            // Contact through the real fitted blade capsule while translating.
            var target=GameObject.CreatePrimitive(PrimitiveType.Sphere);target.transform.SetParent(container.transform,false);target.tag="Enemy";target.GetComponent<Collider>().isTrigger=true;target.AddComponent<GameSaveExcluded>();var damage=target.AddComponent<SpinDamageProbeV275>();
            foreach(bool continuous in new[]{false,true})
            {
                Reset();Begin(continuous);damage.hits=0;damage.damage=0;Keys(GameInputSettings.Get(GameInputSettings.Action.Forward));Set(motor,"planarVelocity",Vector3.forward*motor.moveSpeed);
                Call(motor,"Update");attack.animator.Play(Get<int>(attack,"activeAttackHash"),0,.4f);attack.animator.Update(0);
                attack.stickHitbox.enabled=true;Physics.SyncTransforms();target.transform.position=attack.stickHitbox.bounds.center;Physics.SyncTransforms();
                if(continuous)Call(attack,"UpdateSpinHitbox");else Call(attack,"UpdateHitbox");
                Check(damage.hits==1&&damage.damage>0,"Moving spin confirms real blade contact");
                var stop=actor.GetComponent<CombatLocalHitStop>();Check(stop&&stop.IsStopped&&!stop.UsesLayerFreeze,"Moving full-body spin retains original hit-stop");
                var hitStopStart=actor.transform.position;Call(motor,"Update");Check(actor.transform.position.z>hitStopStart.z,"Visual hit-stop does not freeze physical movement");stop.Release();
                target.transform.position=attack.stickHitbox.bounds.center;Physics.SyncTransforms();
                if(continuous)Call(attack,"UpdateSpinHitbox");else Call(attack,"UpdateHitbox");Check(damage.hits==1,"Same swing/turn does not double-hit the target");
            }
            Reset();Call(attack,"StartComboStage",0);Check(!attack.IsTwinBladeSpinActive&&!attack.CanTranslateDuringCombat&&attack.UsesAnimationRootMotion,"First normal slash retains its existing movement rules");
            Reset();attack.SetTwinBladeMode(false,null,null);Buttons(true);Set(attack,"chordLatched",false);Call(attack,"UpdateCombinations");Check(attack.IsSpinning&&!attack.IsTwinBladeSpinActive&&!attack.CanTranslateDuringCombat,"Club continuous spin is unchanged");
        }
        catch(Exception e){error=e.ToString();}
        finally
        {
            if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            if(container)UnityEngine.Object.DestroyImmediate(container);foreach(var saved in cameras)if(saved.c)saved.c.tag=saved.tag;
            Cursor.lockState=cursor;Cursor.visible=visible;
        }
        bool inputRestored=roster.SequenceEqual(InputSystem.devices.Select(d=>(d.deviceId,d.enabled)).ToArray());if(!inputRestored)error=(error??"")+" Input roster changed";
        Directory.CreateDirectory(".codex/twin-spin-movement-v275");File.WriteAllText(".codex/twin-spin-movement-v275/native.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=error==null,error,checks,travel,inputRestored},Newtonsoft.Json.Formatting.Indented));
        return new{passed=error==null,error,checks=checks.Count,cases=travel.Count,inputRestored};
    }
}
