using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class VerifyStory1SailingControls
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object target,string method,params object[] args) => target.GetType().GetMethod(method,Private).Invoke(target,args);
    static float Value(object target,string field) => (float)target.GetType().GetField(field,Private).GetValue(target);
    static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static object Run()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play mode.");
        var storyShip = UnityEngine.Object.FindFirstObjectByType<ShipSailingMotion>();
        var storyView = UnityEngine.Object.FindFirstObjectByType<ShipFollowCamera>();
        Check(storyShip && storyView,"Story1 ship and camera not loaded");
        Check(storyShip.autoSail && storyView.mouseFreeLook,"Story1 serialized defaults are not enabled");
        var boat = new GameObject("Sailing controls verification");
        var view = new GameObject("Sailing camera verification");
        GameObject menuObject = null;
        var oldMouse = Mouse.current;
        Mouse mouse = null;
        var oldCursor = Cursor.lockState;
        bool oldVisible = Cursor.visible;
        try
        {
            var motion = boat.AddComponent<ShipSailingMotion>();
            Call(motion,"Start");
            typeof(ShipSailingMotion).GetField("ocean",Private).SetValue(motion,null);
            Vector3 start = boat.transform.position;
            Call(motion,"Simulate",1f,0f,false,false);
            Check(Vector3.Distance(start + Vector3.right * motion.forwardSpeed,boat.transform.position)<.001f,"No-input sailing failed");
            Call(motion,"Simulate",1f,0f,false,true);
            Check(motion.CurrentSpeed>0 && Mathf.Abs(boat.transform.position.x-motion.forwardSpeed*2)<.001f,"S reversed or stopped the sailboat");
            Call(motion,"Simulate",1f,0f,true,true);
            Check(Mathf.Abs(motion.CurrentSpeed-motion.forwardSpeed)<.001f,"W/S still throttle the sailboat");
            Quaternion beforeSteer = boat.transform.rotation;
            Call(motion,"Simulate",.5f,1f,false,false);
            Check(Quaternion.Angle(beforeSteer,boat.transform.rotation)>1f,"Helm steering lost");
            var camera = view.AddComponent<ShipFollowCamera>();
            camera.enabled=false; camera.target=boat.transform; camera.enabled=true;
            Call(camera,"Start");
            Check(Cursor.lockState==CursorLockMode.Locked && !Cursor.visible,"Sailing cursor not captured");
            mouse=InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse,new MouseState { delta=new Vector2(100,-20) });
            InputSystem.Update();
            float yaw = Value(camera,"orbitYaw"), pitch = Value(camera,"orbitPitch");
            Call(camera,"ReadViewInput");
            Check(Value(camera,"orbitYaw")<yaw-1 && Value(camera,"orbitPitch")>pitch,"Mouse without a button did not orbit camera in the requested horizontal direction");
            menuObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab"));
            var menu=menuObject.GetComponent<PauseSettingsMenu>(); menu.Open();
            yaw=Value(camera,"orbitYaw"); Vector3 stopped=boat.transform.position;
            Call(camera,"ReadViewInput"); Call(motion,"Simulate",1f,1f,false,false);
            Check(Value(camera,"orbitYaw")==yaw && boat.transform.position==stopped,"Pause did not block camera or sailing");
            menu.Resume();
            Check(Cursor.lockState==CursorLockMode.Locked,"Resume did not recapture sailing view");
            camera.enabled=false;
            Quaternion cinematic=Quaternion.Euler(15,45,0); view.transform.rotation=cinematic;
            Check(Quaternion.Angle(view.transform.rotation,cinematic)<.001f,"Disabled camera interfered with cinematic");
            return new {success=true,automaticForward=true,noReverseOrThrottle=true,helmSteering=true,mouseLookWithoutButton=true,pauseBlocksInput=true,resumeRestoresCursor=true,storyDefaults=true};
        }
        finally
        {
            if(menuObject) {menuObject.GetComponent<PauseSettingsMenu>().Resume();UnityEngine.Object.DestroyImmediate(menuObject);}
            if(mouse!=null) InputSystem.RemoveDevice(mouse);
            if(oldMouse!=null) oldMouse.MakeCurrent();
            Cursor.lockState=oldCursor;Cursor.visible=oldVisible;
            UnityEngine.Object.DestroyImmediate(view);UnityEngine.Object.DestroyImmediate(boat);
        }
    }
}
