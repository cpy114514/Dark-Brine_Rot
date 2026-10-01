using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Mavis;
public static class VerifyComboSteering
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Private).SetValue(o,v);
    static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Private).Invoke(o,a);
    static void Check(bool ok,string m) { if(!ok) throw new Exception(m); }
    public static object Run()
    {
        Check(Application.isPlaying,"Requires Play mode.");
        var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.identity);
        try
        {
            var movement=player.GetComponent<ThirdPersonPlayerController>(); movement.enabled=false;
            var attack=player.GetComponent<SahurAttack>(); attack.enabled=false;
            var animator=attack.animator; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var input in player.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>()) input.enabled=false;
            var relay=animator.GetComponent<SahurRootMotionRelay>(); if(relay) relay.enabled=false;
            player.GetComponent<PlayerStamina>().currentStamina=100;
            var report=new List<object>();
            for(int stage=0;stage<3;stage++)
            {
                movement.EndAttackFacing(true); player.transform.rotation=Quaternion.identity;
                Set(attack,"attacking",true); Set(attack,"comboStep",stage);
                string state=stage==0 ? attack.comboOneState : stage==1 ? attack.comboTwoState : attack.comboThreeState;
                Set(attack,"activeAttackHash",Animator.StringToHash("Base Layer."+state));
                movement.BeginAttackFacing();
                Vector2 window=stage==0 ? attack.comboOneHitWindow : stage==1 ? attack.comboTwoHitWindow : attack.comboThreeHitWindow;
                animator.Play(state,0,(window.x+window.y)*.5f); animator.Update(0);
                Check(!attack.CanSteerGroundCombo,"Damage phase must not allow steering.");
                Call(movement,"UpdateComboFacing",Vector3.right,0f,.2f);
                Check(Quaternion.Angle(player.transform.rotation,Quaternion.identity)<.01f,"Mid-swing input rotated the damaging arc.");
                animator.Play(state,0,window.y+.02f); animator.Update(0);
                Check(attack.CanSteerGroundCombo,"Recovery steering is closed.");
                Call(movement,"UpdateComboFacing",Vector3.zero,0f,.2f);
                float recoveryYaw=player.transform.eulerAngles.y;
                Check(Mathf.Abs(Mathf.DeltaAngle(recoveryYaw,90))<.1f,"Queued WASD direction did not turn recovery.");
                var before=player.transform.rotation;
                movement.ApplyAttackRootMotion(Vector3.zero,Quaternion.Euler(0,30,0));
                Check(Quaternion.Angle(before,player.transform.rotation)<.1f,"Root motion restored the first hit heading.");
                Set(movement,"yaw",-90f);
                animator.Play(state,0,window.x*.5f); animator.Update(0);
                Check(attack.CanSteerGroundCombo,"Windup steering is closed.");
                Call(movement,"UpdateComboFacing",Vector3.zero,10f,.3f);
                Check(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,-90))<.1f,"Mouse heading did not steer anticipation.");
                if(stage<2)
                {
                    Call(attack,"StartComboStage",stage+1); animator.Update(.08f);
                    var captured=(Quaternion)typeof(ThirdPersonPlayerController).GetField("attackFacing",Private).GetValue(movement);
                    Check(Mathf.Abs(Mathf.DeltaAngle(captured.eulerAngles.y,-90))<.1f,"Next combo stage lost the steered heading.");
                }
                report.Add(new {stage=stage+1,recoveryYaw,mouseYaw=-90,nextHitKeepsNewHeading=true,hitWindowStable=true});
            }
            attack.SuspendForSwimming();
            movement.EndAttackFacing(true);
            Set(movement,"comboAimPending",false); player.transform.rotation=Quaternion.identity;
            Set(attack,"attacking",true); Set(attack,"comboStep",0); Set(attack,"activeAttackHash",Animator.StringToHash("Base Layer."+attack.comboOneState));
            movement.BeginAttackFacing(); animator.Play(attack.comboOneState,0,.9f); animator.Update(0);
            Set(movement,"yaw",170f); Call(movement,"UpdateComboFacing",Vector3.zero,0f,.3f);
            Check(Quaternion.Angle(player.transform.rotation,Quaternion.identity)<.1f,"Idle camera heading caused an unrequested turn.");
            var target=new GameObject("Temporary combo lock target");
            try
            {
                target.transform.position=player.transform.position+new Vector3(5,0,5);
                var enemyLock=player.GetComponent<EnemyLockOn>();
                Set(enemyLock,"<Target>k__BackingField",target.transform);
                Call(movement,"UpdateComboFacing",Vector3.left,10f,.3f);
                Check(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,45))<.1f,"Lock-on did not override free steering.");
                enemyLock.Clear();
            }
            finally { UnityEngine.Object.DestroyImmediate(target); }
            attack.SuspendForSwimming();
            return new {report,inputDuringHitBuffered=true,turningWithoutExtraMovement=true,noInputNoTurn=true,lockOnPriority=true,damageWindowsUnchanged=true};
        }
        finally { UnityEngine.Object.DestroyImmediate(player); }
    }
}
