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

// Synchronous native-controller probe: no Play toggle, input pumping, physical-device
// disable or global time changes. Temporary devices are current only inside this call.
public static class VerifyJumpMomentumV274
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Update = typeof(ThirdPersonPlayerController).GetMethod("Update", F);
    static readonly MethodInfo Chop = typeof(SahurAttack).GetMethod("StartJumpSlash", F);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, F).SetValue(owner, value);
    static T Get<T>(object owner, string name) => (T)owner.GetType().GetField(name, F).GetValue(owner);
    static float[] V(Vector3 v) => new[] {v.x,v.y,v.z};
    // Only call with the human's approval to briefly resume their frozen session.
    public static async Task<object> RunWithPauseRestore()
    {
        if(!Application.isPlaying)throw new Exception("Keep the existing Play session");
        float scale=Time.timeScale,capture=Time.captureDeltaTime;
        bool paused=EditorApplication.isPaused;
        try
        {
            Time.captureDeltaTime=1f/60;Time.timeScale=1;EditorApplication.isPaused=false;
            var deadline=DateTime.UtcNow.AddSeconds(5);
            do {await Task.Delay(10);if(!Application.isPlaying)throw new OperationCanceledException("Play stopped");if(DateTime.UtcNow>deadline)throw new TimeoutException("Player loop did not resume");}while(Time.deltaTime<=0);
            return Run();
        }
        finally
        {Time.timeScale=scale;Time.captureDeltaTime=capture;EditorApplication.isPaused=paused;}
    }
    public static object Run()
    {
        if (!Application.isPlaying || Time.deltaTime <= 0) throw new Exception("Live player loop required");
        var checks = new List<string>(); var rows = new List<object>();
        void Check(bool ok, string description) { if (!ok) throw new Exception(description); checks.Add(description); }
        var oldKeyboard = Keyboard.current; var oldMouse = Mouse.current;
        var cursor = Cursor.lockState; bool visible = Cursor.visible;
        var roster = InputSystem.devices.Select(d => (d.deviceId,d.enabled)).ToArray();
        Keyboard keyboard = null; Mouse mouse = null; GameObject container = null;
        var oldCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>(c,c.transform.position,c.transform.rotation,c.fieldOfView)).ToArray();
        string failure = null;
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>("JumpMomentumV274ProbeKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("JumpMomentumV274ProbeMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent(); InputState.Change(keyboard,new KeyboardState()); InputState.Change(mouse,new MouseState());
            container = new GameObject("Temporary synchronous jump momentum probe"); container.SetActive(false); container.hideFlags=HideFlags.DontSave;
            container.transform.position = new Vector3(12000,500,12000);
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),container.transform);
            actor.AddComponent<GameSaveExcluded>();
            foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            container.SetActive(true);
            foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            var motor=actor.GetComponent<ThirdPersonPlayerController>(); var attack=actor.GetComponent<SahurAttack>();
            var stamina=actor.GetComponent<PlayerStamina>();var body=actor.GetComponent<CharacterController>();
            motor.enabled=true;attack.enabled=true;motor.seaLevel=-1000;motor.waterSplashes=false;
            motor.ExternalControlLock=motor.ExternalMovementLock=false; Cursor.lockState=CursorLockMode.Locked;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(container.transform,false);
            floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(200,1,200);
            float footOffset=(body.center.y-body.height*.5f)*actor.transform.lossyScale.y;
            var home=new Vector3(12000,500.03f-footOffset,12000);float dt=Time.deltaTime;
            void Reset(Vector3 velocity, float height=3, Vector3 carry=default)
            {
                attack.SuspendForSwimming();motor.ExternalControlLock=motor.ExternalMovementLock=false;
                motor.RestoreSavedPose(home+Vector3.up*height,Quaternion.identity);
                Set(motor,"planarVelocity",velocity);Set(motor,"platformCarryVelocity",carry);Set(motor,"verticalSpeed",4f);
                stamina.currentStamina=100;Physics.SyncTransforms();
            }
            void Begin()
            {Check(motor.CanUseAirAttack,"Physical airborne state allows jump chop");Chop.Invoke(attack,null);Check(attack.IsJumpSlashActive&&!attack.UsesAnimationRootMotion,"Aerial chop reports its state and keeps physical movement ownership");}
            void Step()
            {keyboard.MakeCurrent();mouse.MakeCurrent();Update.Invoke(motor,null);}
            string[] controllers={"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
            foreach(var path in controllers)foreach(bool blades in new[]{false,true})
            {
                attack.animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);attack.animator.Rebind();
                attack.SetTwinBladeMode(blades,null,null);
                string mode=Path.GetFileNameWithoutExtension(path)+"/"+(blades?"blades":"club");
                foreach(bool sprint in new[]{false,true})
                {
                    float speed=motor.moveSpeed*(sprint?motor.sprintMultiplier:1);
                    Vector3 velocity=new Vector3(-.6f,0,.8f)*speed;
                    Reset(velocity);Begin();Vector3 origin=actor.transform.position;
                    float initialY=origin.y,initialVertical=motor.VerticalSpeed;int frames=0;
                    while(!motor.CanUseGroundAttack&&frames<400)
                    {
                        Vector3 before=actor.transform.position;Step();frames++;
                        var planar=Get<Vector3>(motor,"planarVelocity");
                        Check(Vector3.Distance(planar,velocity)<.0001f,mode+" "+(sprint?"sprint":"walk")+" retains horizontal speed while airborne, frame "+frames);
                        if(frames<4)Check(Vector3.Distance(Vector3.ProjectOnPlane(actor.transform.position-before,Vector3.up),velocity*dt)<.004f,"Real capsule advances with inherited momentum");
                        if(frames==3)
                        {
                            float time=frames*dt,expected=initialY+initialVertical*time-.5f*motor.gravity*time*time;
                            Check(Mathf.Abs(actor.transform.position.y-expected)<.005f,"Jump chop keeps the original ballistic gravity");
                        }
                        rows.Add(new{mode,sprint,frame=frames,dt,position=V(actor.transform.position-origin),velocity=V(motor.WorldVelocity)});
                    }
                    Check(frames<400&&frames>2,mode+" lands through capsule physics");
                    Step();Check(Get<Vector3>(motor,"planarVelocity").magnitude<speed-.001f,"Landing restores normal braking during recovery");
                    for(int i=0;i<100;i++)Step();Check(Get<Vector3>(motor,"planarVelocity").sqrMagnitude<.0001f,"Grounded chop recovery cannot slide forever");
                }
            }
            attack.SetTwinBladeMode(false,null,null);
            Reset(Vector3.zero);Begin();var stationary=actor.transform.position;for(int i=0;i<6;i++)Step();
            Check(Vector3.ProjectOnPlane(actor.transform.position-stationary,Vector3.up).magnitude<.003f,"Stationary jump chop adds no artificial horizontal boost");
            Reset(Vector3.forward*7,3,new Vector3(2,0,-1));Begin();var platformStart=actor.transform.position;Step();
            Check(Vector3.Distance(Vector3.ProjectOnPlane(actor.transform.position-platformStart,Vector3.up),new Vector3(2,0,6)*dt)<.004f,"Moving-platform carry is added exactly once");
            Reset(Vector3.forward*7);Begin();motor.ExternalControlLock=true;Step();
            Check(Get<Vector3>(motor,"planarVelocity").z<7,"External control overrides retained attack momentum");
            Reset(Vector3.forward*7);Begin();motor.ExternalMovementLock=true;var locked=actor.transform.position;Step();
            Check(Get<Vector3>(motor,"planarVelocity")==Vector3.zero&&actor.transform.position==locked,"External movement owner can still stop the actor");
            Reset(Vector3.forward*7);Step();Check(Get<Vector3>(motor,"planarVelocity").z<7,"Ordinary airborne locomotion retains its existing braking");
            // Real wall contact must stop travel while gravity continues.
            Reset(new Vector3(3,0,10),8);Begin();var wallStart=actor.transform.position;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(container.transform,false);
            wall.transform.position=wallStart+new Vector3(0,0,1.8f);wall.transform.localScale=new Vector3(100,100,.2f);Physics.SyncTransforms();
            for(int i=0;i<Mathf.CeilToInt(.45f/dt);i++)Step();
            float z=actor.transform.position.z-wallStart.z;
            Check(z>=-.01f&&z<1.7f,"Inherited attack velocity is blocked by an actual wall");
            Check(actor.transform.position.x-wallStart.x>.2f,"Wall contact preserves the free lateral component");
            Check(motor.VerticalSpeed<4,"Wall contact does not freeze gravity");
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            // Restore inputs before report IO or any possible teardown exception.
            if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
            if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            if(container)UnityEngine.Object.DestroyImmediate(container);
            Cursor.lockState=cursor;Cursor.visible=visible;
            foreach(var saved in oldCameras)if(saved.c){saved.c.transform.SetPositionAndRotation(saved.position,saved.rotation);saved.c.fieldOfView=saved.fieldOfView;}
        }
        var restored=InputSystem.devices.Select(d=>(d.deviceId,d.enabled)).ToArray();
        if(!roster.SequenceEqual(restored))failure=(failure??"")+" Input roster changed";
        var result=new{passed=failure==null,error=failure,checks,rows,dt=Time.deltaTime};
        Directory.CreateDirectory(".codex/jump-momentum-v274");
        File.WriteAllText(".codex/jump-momentum-v274/native.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new{passed=failure==null,error=failure,checks=checks.Count,frames=rows.Count,dt=Time.deltaTime,inputRestored=roster.SequenceEqual(restored)};
    }
}
