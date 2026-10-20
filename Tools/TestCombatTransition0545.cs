using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;
public static class TestCombatTransition0545
{
    const string Folder=".codex/combat-combinations-20261019/heartbeat-0545";
    static Mouse mouse;static Keyboard keyboard;static bool right;static Key[] keys=Array.Empty<Key>();static Task pending;
    static Vector3 blockPoint;static void Hit(CombatHitEvent hit){if(hit.result==CombatHitResult.Blocked)blockPoint=hit.point;}
    static void Inject(){if(mouse==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;mouse.MakeCurrent();keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(mouse,new MouseState().WithButton(MouseButton.Right,right));InputState.Change(keyboard,new KeyboardState(keys));}
    static async Task Wait(float seconds){float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(25);while(Time.time<end){if(DateTime.UtcNow>deadline)throw new Exception("Native frame loop stalled");await Task.Delay(5);}}
    static async Task Until(Func<bool> condition,float timeout=4){float end=Time.time+timeout;while(!condition()){if(Time.time>end)throw new Exception("Native state timeout");await Wait(.01f);}}
    public static object Before(){return Start(false);}
    public static object After(){return Start(true);}
    static object Start(bool fixedExpected){if(SessionState.GetBool("Transition0545Running",false))throw new Exception("Already running");SessionState.SetBool("Transition0545Running",true);pending=Run(fixedExpected);return new{started=true,fixedExpected};}
    static void Capture(Camera camera,string path)
    {
        var player=CombatFeelPreview.Player;Vector3 centre=player.GetComponent<CharacterController>().bounds.center+Vector3.up*.3f;
        camera.transform.position=centre+new Vector3(6,3,-7);camera.transform.LookAt(centre);camera.fieldOfView=52;
        var original=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=original;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    static async Task Run(bool fixedExpected)
    {
        var previousMouse=Mouse.current;var previousKeyboard=Keyboard.current;float capture=Time.captureDeltaTime;var checks=new List<object>();
        try
        {
            CombatFeelPreview.Spawn();Time.captureDeltaTime=1f/60;
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();right=false;keys=Array.Empty<Key>();InputSystem.onBeforeUpdate+=Inject;CombatHitFeedback.HitConfirmed+=Hit;
            await Wait(.7f);var player=CombatFeelPreview.Player;var attack=player.GetComponent<SahurAttack>();var motor=player.GetComponent<ThirdPersonPlayerController>();var energy=player.GetComponent<PlayerStamina>();
            var loadout=player.GetComponent<SahurWeaponLoadout>();loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);await Wait(.2f);
            var originalPosition=player.transform.position;var target=CombatFeelPreview.Target;target.transform.position=new Vector3(2000,500,2030);Physics.SyncTransforms();
            right=true;await Until(()=>attack.IsCharging);await Wait(.7f);
            // A normal locomotion loop can be at this phase when a charge is released.
            attack.animator.Play("Base Layer.Locomotion",0,.95f);await Wait(.016f);right=false;await Until(()=>attack.IsHeavyAttackActive);await Wait(.025f);
            float reported=attack.CurrentAttackPhase,authored=attack.HeavyAttackPhase;bool blending=attack.animator.IsInTransition(0);
            bool early=attack.TryConsumeDodgeCancel(true);
            checks.Add(new{test="Heavy incoming transition uses destination attack clock",reported,authored,blending,earlyDodgeAccepted=early,passed=Mathf.Abs(reported-authored)<.025f && !early});
            attack.SuspendForSwimming();attack.TryConsumeDodgeCancel(false);await Wait(.65f);energy.currentStamina=100;
            attack.heavySwingWindowEnd=.9f;right=true;await Until(()=>attack.IsCharging);await Wait(.65f);right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>attack.HeavyAttackPhase>=.63f);
            float recovery=attack.HeavyAttackPhase;bool recovered=attack.TryConsumeDodgeCancel(true);
            checks.Add(new{test="Rush recovery dodge opens after outward cut",phase=recovery,accepted=recovered,passed=recovered && recovery<.71f});
            attack.SuspendForSwimming();attack.TryConsumeDodgeCancel(false);await Wait(.35f);energy.currentStamina=100;
            motor.RestoreSavedPose(originalPosition,Quaternion.identity);await Wait(.25f);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};await Until(()=>attack.GuardReady);var hp=player.GetComponent<PlayerHealth>();
            target.transform.position=new Vector3(2000,500,2003.2f);target.transform.rotation=Quaternion.Euler(0,180,0);Physics.SyncTransforms();
            float health=hp.currentHealth,stamina=energy.Current;blockPoint=new Vector3(9999,9999,9999);
            bool front=target.GetComponent<NailongAttack>().TryHit(player.transform,5,1,2);
            float pointGap=Vector3.Distance(blockPoint,player.GetComponent<CharacterController>().bounds.ClosestPoint(blockPoint));
            checks.Add(new{test="Actual Nailong melee caller is blocked with cost and body contact",front,hpBefore=health,hpAfter=hp.currentHealth,staminaBefore=stamina,staminaAfter=energy.Current,pointGap,passed=front && hp.currentHealth==health && energy.Current<stamina && pointGap<.02f});
            await Wait(.016f);Capture(CombatFeelPreview.Camera,Folder+(fixedExpected?"/guard-after.png":"/guard-before.png"));
            await Wait(.15f);target.transform.position=new Vector3(2000,500,1996.8f);target.transform.rotation=Quaternion.identity;Physics.SyncTransforms();health=hp.currentHealth;
            bool rear=target.GetComponent<NailongAttack>().TryHit(player.transform,5,1,0);
            checks.Add(new{test="Actual rear Nailong melee bypasses guard",rear,hpBefore=health,hpAfter=hp.currentHealth,passed=rear && hp.currentHealth<health});
            var result=new{fixedExpected,checks};File.WriteAllText(Folder+(fixedExpected?"/after.json":"/before.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(Folder+(fixedExpected?"/after-error.txt":"/before-error.txt"),e.ToString());File.WriteAllText(Folder+"/partial.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));}
        finally
        {
            CombatHitFeedback.HitConfirmed-=Hit;InputSystem.onBeforeUpdate-=Inject;if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);mouse=null;keyboard=null;
            if(previousMouse!=null && previousMouse.added)previousMouse.MakeCurrent();if(previousKeyboard!=null && previousKeyboard.added)previousKeyboard.MakeCurrent();CombatFeelPreview.Clear();Time.captureDeltaTime=capture;SessionState.SetBool("Transition0545Running",false);
        }
    }
}
