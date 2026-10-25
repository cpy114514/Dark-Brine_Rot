using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Mavis;
public static class TestCombatFeelNativeV245
{
    const string Folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/combat-recovery-20261023/native-input-controller-layer";
    static Keyboard keyboard;static Mouse mouse;static Key[] keys=Array.Empty<Key>();static bool left,right;
    static Task pending;static List<object> rows=new List<object>();static SahurAttack attack;static CombatHitFeedback feedback;
    static float oldScale,oldVolume;static bool recording;static int frames;
    static int lastFrame=-1;
    static bool sawLayerFreeze;
    static readonly List<object> frameRows=new List<object>();
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);rows.Add(new{passed=message});}
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        InputState.Change(keyboard,new KeyboardState(keys));
        var state=new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right);InputState.Change(mouse,state);
    }
    static async Task Wait(float seconds)
    {
        float end=Time.unscaledTime+seconds;var deadline=DateTime.UtcNow.AddSeconds(30);
        while(Time.unscaledTime<end){if(DateTime.UtcNow>deadline)throw new Exception("Native player loop stalled");await Task.Delay(8);}
    }
    static async Task Click(){left=true;await Wait(.045f);left=false;await Wait(.025f);}
    static async Task Until(Func<bool> condition,float timeout=5)
    {float end=Time.unscaledTime+timeout;while(!condition()){if(Time.unscaledTime>end)throw new Exception("Timed out waiting for state");await Wait(.01f);}}
    public static object Start()
    {
        if(SessionState.GetBool("CombatFeelTestRunning",false))throw new Exception("Combat feel test is already running; wait for its result.");
        SessionState.SetBool("CombatFeelTestRunning",true);pending=Run();return new{started=true};
    }
    static void Hit(CombatHitEvent hit)
    {if(hit.attacker==CombatFeelPreview.Player)frameRows.Add(new{eventTime=Time.unscaledTime,kind=hit.kind.ToString(),result=hit.result.ToString(),point=V(hit.point),direction=V(hit.direction),hit.damage});}
    static object V(Vector3 v)=>new{x=v.x,y=v.y,z=v.z};
    static void Frame()
    {
        if(!recording || !CombatFeelPreview.Player || lastFrame==Time.frameCount)return;
        lastFrame=Time.frameCount;
        var stop=CombatFeelPreview.Player.GetComponent<CombatLocalHitStop>();
        if(stop && stop.IsStopped && stop.UsesLayerFreeze && attack.animator.speed>0)sawLayerFreeze=true;
        frameRows.Add(new{t=Time.unscaledTime,phase=attack.CurrentAttackPhase,stage=attack.CurrentComboStage,charge=attack.Charge01,speed=attack.animator.speed,stop=CombatLocalHitStop.Active(CombatFeelPreview.Player),hits=feedback.HitCount,pos=V(CombatFeelPreview.Player.transform.position),scale=Time.timeScale});
        if(frames++%3==0)Capture("frames/frame-"+frames.ToString("D5")+".png");
    }
    static void Capture(string file)
    {
        var camera=CombatFeelPreview.Camera;var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(960,540,24);var texture=new Texture2D(960,540,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(Folder+"/"+file,texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static async Task Run()
    {
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;
        oldScale=Time.timeScale;oldVolume=AudioListener.volume;rows.Clear();frameRows.Clear();frames=0;sawLayerFreeze=false;
        Directory.CreateDirectory(Folder+"/frames");
        Directory.CreateDirectory(Folder+"/frames");
        try
        {
            if(!Application.isPlaying)throw new Exception("Play Mode required");
            CombatFeelPreview.Clear();CombatFeelPreview.Spawn();
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys=Array.Empty<Key>();left=right=false;
            InputSystem.onBeforeUpdate+=Inject;
            var player=CombatFeelPreview.Player;attack=player.GetComponent<SahurAttack>();feedback=player.GetComponent<CombatHitFeedback>();
            var input=player.GetComponent<PlayerInput>();InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();
            CombatHitFeedback.HitConfirmed+=Hit;
            var target=CombatFeelPreview.Target;var health=target.GetComponent<NailongHealth>();
            await Wait(.7f);Capture("arena.png");
            Require(attack.animator.GetFloat("LightPlaybackSpeed")==1,"Idle playback remains 1");
            // Miss: move target well away before real left-button input.
            Vector3 home=target.transform.position;target.transform.position+=Vector3.forward*30;Physics.SyncTransforms();
            int before=feedback.HitCount;await Click();await Until(()=>!attack.IsCombatMotionActive);
            Require(feedback.HitCount==before && !CombatLocalHitStop.Active(player),"A real whiff creates no hit event or stop");
            target.transform.position=home;Physics.SyncTransforms();await Wait(.12f);
            // Real stationary three-hit combo, early input carried through local stop.
            recording=true;EditorApplication.update+=Frame;
            await Click();var stages=new HashSet<int>();float maxSpeed=0,minSpeed=10;
            while(attack.IsCombatMotionActive)
            {
                int stage=attack.CurrentComboStage;stages.Add(stage);
                float speed=attack.animator.GetFloat("LightPlaybackSpeed");maxSpeed=Mathf.Max(maxSpeed,speed);minSpeed=Mathf.Min(minSpeed,speed);
                if(stage<2 && stage>=0 && attack.CurrentAttackPhase>=.24f && attack.CurrentAttackPhase<.7f)await Click();else await Wait(.01f);
            }
            rows.Add(new{stages=stages.ToArray(),minSpeed,maxSpeed,hits=feedback.HitCount});
            Require(stages.Contains(0)&&stages.Contains(1)&&stages.Contains(2),"Native left mouse completes all three combo stages");
            Require(maxSpeed-minSpeed>.5f,"Authored light clips accelerate and settle instead of constant playback");
            Require(feedback.HitCount>before,"Native club trajectory contacts the real target");
            await Wait(.25f);Capture("combo-recovery.png");
            // Reset position, actual hold/release and direction changes while charging.
            var motor=player.GetComponent<ThirdPersonPlayerController>();var cc=player.GetComponent<CharacterController>();
            Vector3 start=new Vector3(2000,500.03f-(cc.center.y-cc.height*.5f)*player.transform.lossyScale.y,2000);
            motor.RestoreSavedPose(start,Quaternion.identity);await Wait(.2f);right=true;await Until(()=>attack.IsCharging);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left)};await Wait(.12f);keys=Array.Empty<Key>();
            Require(attack.IsCharging && attack.CanMoveDuringCombat,"Moving while charging preserves the charge");
            // Let the moving release finish; teleporting a CharacterController mid-charge
            // legitimately cancels it for its ungrounded frame, so reset only between attacks.
            right=false;await Wait(.2f);await Until(()=>!attack.IsCombatMotionActive,6);
            motor.EndAttackFacing(true);motor.RestoreSavedPose(start,Quaternion.identity);await Wait(.25f);
            right=true;await Until(()=>attack.IsCharging);await Wait(1.85f);
            int heavyBefore=feedback.HitCount;right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive,6);
            Require(feedback.HitCount>heavyBefore && feedback.LastHitKind==CombatHitKind.ChargedHeavy,"Native held right mouse releases a real heavy contact");
            Require(sawLayerFreeze,"Mobile heavy freezes its attack layer while keeping locomotion Animator running");
            await Wait(.25f);Capture("heavy-recovery.png");recording=false;EditorApplication.update-=Frame;
            Require(Mathf.Approximately(Time.timeScale,1),"Combat feedback never changes global Time.timeScale");
            Require(Mathf.Approximately(attack.animator.speed,1),"Attacker animator restores its original speed");
            Require(!CombatLocalHitStop.Active(target),"Victim local stop expires");
            // Actual Ctrl input can cancel a charge and can buffer near light recovery.
            motor.EndAttackFacing(true);motor.RestoreSavedPose(start,Quaternion.identity);await Wait(.25f);
            right=true;await Until(()=>attack.IsCharging);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};
            await Wait(.08f);keys=Array.Empty<Key>();right=false;
            float roll=(float)typeof(ThirdPersonPlayerController).GetField("rollTimer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(motor);
            Require(!attack.IsCombatMotionActive && roll>0,"Native Ctrl cancels charge into the existing dodge");
            await Wait(1);motor.EndAttackFacing(true);motor.RestoreSavedPose(start,Quaternion.identity);await Wait(.25f);
            await Click();await Until(()=>attack.IsGroundComboActive && attack.CurrentAttackPhase>=.78f);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};await Wait(.045f);keys=Array.Empty<Key>();await Wait(.2f);
            roll=(float)typeof(ThirdPersonPlayerController).GetField("rollTimer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(motor);
            float flip=(float)typeof(ThirdPersonPlayerController).GetField("airFlipTimer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(motor);
            rows.Add(new{recoveryDodge=new{roll,flip,attack.IsCombatMotionActive,attack.CurrentAttackPhase,stamina=player.GetComponent<PlayerStamina>().currentStamina}});
            Require(!attack.IsCombatMotionActive && (roll>0 || flip>0),"Native Ctrl buffers into light recovery cancel without truncating contact");
            await Wait(1);
            // Rejected, armored, blocked and dead targets use one confirmed event each.
            var response=target.GetComponent<CombatImpactResponse>();float hp=health.currentHealth;
            response.blocksMelee=true;int count=feedback.HitCount;
            CombatHitFeedback.Apply(player,health,25,home,CombatHitKind.ComboOne,Vector3.right);
            Require(health.currentHealth==hp && feedback.HitCount==count+1 && feedback.LastResult==CombatHitResult.Blocked,"Blocked hit has one distinct event and no HP loss");
            response.blocksMelee=false;response.unflinching=true;
            CombatHitFeedback.Apply(player,health,25,home,CombatHitKind.ComboOne,Vector3.left);
            Require(feedback.LastResult==CombatHitResult.Unflinching,"Armored hit has its own confirmed result");await Wait(.2f);
            // Stop overlap/disable restores a non-default playback owner.
            var stop=player.GetComponent<CombatLocalHitStop>();attack.animator.speed=.73f;
            stop.Hold(attack.animator,.05f);stop.Hold(attack.animator,.09f);await Wait(.15f);
            Require(Mathf.Abs(attack.animator.speed-.73f)<.001f,"Overlapping local stops restore a non-default speed");
            stop.Hold(attack.animator,.1f);stop.enabled=false;
            Require(Mathf.Abs(attack.animator.speed-.73f)<.001f,"Disabling local stop restores speed immediately");stop.enabled=true;attack.animator.speed=1;
            // Directional camera impulses cannot accumulate drift.
            var cam=new GameObject("Isolated impulse check");var shake=cam.AddComponent<CombatCameraShake>();
            var pos=cam.transform.position;var rotation=cam.transform.rotation;
            for(int i=0;i<10;i++)shake.PulseDirectional(Vector3.right,.08f,.08f);
            await Wait(.3f);
            Require(Vector3.Distance(pos,cam.transform.position)<.00001f && Quaternion.Angle(rotation,cam.transform.rotation)<.001f,"Repeated directional impulses return to the exact camera baseline");
            UnityEngine.Object.DestroyImmediate(cam);
            health.currentHealth=0;count=feedback.HitCount;
            Require(!CombatHitFeedback.Apply(player,health,25,home,CombatHitKind.ComboOne) && feedback.HitCount==count,"Dead target creates no extra feedback");
            File.WriteAllText(Folder+"/tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,rows,frames,observations=frameRows},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(Folder+"/tests-error.txt",e.ToString());File.WriteAllText(Folder+"/tests-partial.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rows,frames,observations=frameRows},Newtonsoft.Json.Formatting.Indented));}
        finally
        {
            recording=false;EditorApplication.update-=Frame;CombatHitFeedback.HitConfirmed-=Hit;InputSystem.onBeforeUpdate-=Inject;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            if(oldKeyboard!=null && oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null && oldMouse.added)oldMouse.MakeCurrent();CombatFeelPreview.Clear();
            Time.timeScale=oldScale;AudioListener.volume=oldVolume;
            SessionState.SetBool("CombatFeelTestRunning",false);
        }
    }
}
