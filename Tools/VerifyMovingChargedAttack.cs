using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyMovingChargedAttack
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object o,string method)=>o.GetType().GetMethod(method,Private).Invoke(o,null);
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,Private).GetValue(o);
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task<object> Verify(){
        Require(Application.isPlaying,"Requires Play Mode.");
        var previousKeyboard=Keyboard.current;var previousMouse=Mouse.current;
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        var cursor=Cursor.lockState;float time=Time.timeScale;
        var holder=new GameObject("Moving charged attack regression");holder.SetActive(false);holder.transform.position=new Vector3(2000,500,2000);
        var floor=new GameObject("Moving charged attack floor");floor.transform.position=holder.transform.position-Vector3.up*.5f;
        floor.AddComponent<BoxCollider>().size=new Vector3(100,1,100);
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
        var movement=root.GetComponent<ThirdPersonPlayerController>();movement.enabled=false;movement.waterSplashes=false;
        var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;
        foreach(var input in root.GetComponentsInChildren<PlayerInput>(true))input.enabled=false;
        var reports=new List<object>();
        try{
            Time.timeScale=1;holder.SetActive(true);Call(movement,"Start");
            var capsule=root.GetComponent<CharacterController>();var animator=attack.animator;
            float sole=(capsule.center.y-capsule.height*.5f)*root.transform.lossyScale.y;
            var stamina=root.GetComponent<Mavis.PlayerStamina>();stamina.enabled=false;
            async Task Step(Key[] keys){
                await Task.Delay(18);keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();
                Call(movement,"Update");Call(attack,"UpdateChargeLayerWeight");Call(attack,"UpdateAttackProgress");Call(attack,"UpdateHitbox");
            }
            Key forward=GameInputSettings.Get(GameInputSettings.Action.Forward),back=GameInputSettings.Get(GameInputSettings.Action.Back);
            Key left=GameInputSettings.Get(GameInputSettings.Action.Left),sprint=GameInputSettings.Get(GameInputSettings.Action.Sprint);
            var inputs=new[]{new[]{forward},new[]{left},new[]{back,sprint}};
            foreach(var keys in inputs){
                attack.SuspendForSwimming();stamina.currentStamina=100;
                movement.RestoreSavedPose(holder.transform.position+Vector3.up*(.02f-sole),Quaternion.identity);Physics.SyncTransforms();
                for(int i=0;i<30;i++)await Step(keys);
                Require(capsule.isGrounded,"Actor is not on the test floor.");
                float baseline=Get<Vector3>(movement,"planarVelocity").magnitude;
                Require(baseline>movement.moveSpeed*.95f,"Movement did not reach baseline speed.");
                Call(attack,"StartCharge");Set(attack,"chargeStartedAt",Time.time-attack.chargeWindupTime);Call(attack,"UpdateChargePose");
                for(int i=0;i<10;i++)await Step(keys);
                Vector3 before=root.transform.position;Call(attack,"ReleaseCharge");
                float minSpeed=float.MaxValue,maxHeadingSnap=0;int hitFrames=0;float elapsed=0;Quaternion oldHeading=root.transform.rotation;
                for(int i=0;i<220&&attack.IsHeavyAttackActive;i++){
                    Require(attack.CanMoveDuringCombat,"Attack disabled locomotion.");await Step(keys);elapsed+=Time.deltaTime;
                    minSpeed=Mathf.Min(minSpeed,Get<Vector3>(movement,"planarVelocity").magnitude);
                    maxHeadingSnap=Mathf.Max(maxHeadingSnap,Quaternion.Angle(oldHeading,root.transform.rotation));oldHeading=root.transform.rotation;
                    if(attack.stickHitbox.enabled)hitFrames++;
                    Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Attack replaced the walking/running base layer.");
                }
                float distance=Vector3.ProjectOnPlane(root.transform.position-before,Vector3.up).magnitude;
                Require(!attack.IsHeavyAttackActive&&hitFrames>0,"Attack did not hit and finish.");
                Require(minSpeed>=baseline*.98f,"Release braked movement: "+minSpeed+" vs "+baseline);
                Require(distance>=baseline*elapsed*.90f,"CharacterController did not move throughout attack.");
                for(int i=0;i<12;i++){await Step(keys);maxHeadingSnap=Mathf.Max(maxHeadingSnap,Quaternion.Angle(oldHeading,root.transform.rotation));oldHeading=root.transform.rotation;}
                Require(maxHeadingSnap<2,"Attack completion snapped the heading: "+maxHeadingSnap);
                reports.Add(new{input=string.Join("+",keys.Select(k=>k.ToString())),baseline,minSpeed,distance,elapsed,hitFrames,maxHeadingSnap});
            }
            return new{cases=reports,continuousLocomotion=true,legsAnimate=true,sprintPreserved=true};
        }finally{
            UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(floor);
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent();previousMouse?.MakeCurrent();Cursor.lockState=cursor;Time.timeScale=time;
        }
    }
}
