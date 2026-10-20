using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Mavis;
public static class TestCombatCombinations
{
    const string Folder=".codex/combat-combinations-20261019";
    static Keyboard keyboard;static Mouse mouse;static Key[] keys=Array.Empty<Key>();static bool left,right;
    static SahurAttack attack;static ThirdPersonPlayerController motor;static PlayerStamina stamina;static GameObject player;
    public sealed class HitRow {public string section,kind,result;public int turn;public float time,damage;}
    static readonly List<object> checks=new List<object>(),frames=new List<object>();static readonly List<HitRow> events=new List<HitRow>();static Task pending;
    static int hz=60;static string section;static int lastFrame=-1;static bool recording;static bool sawLocalStop;
    static void Inject(){if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
    static async Task Wait(float seconds){float until=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(25);while(Time.time<until){if(DateTime.UtcNow>deadline)throw new Exception("Native frame loop stalled: "+section);await Task.Delay(8);}}
    static async Task Until(Func<bool> condition,float timeout=4){float end=Time.time+timeout;while(!condition()){if(Time.time>end)throw new Exception("State timeout: "+section);await Wait(.01f);}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(new{passed=text});}
    static void Hit(CombatHitEvent hit){if(hit.attacker==player)events.Add(new HitRow{section=section,turn=attack.CompletedSpinTurns,time=Time.time,kind=hit.kind.ToString(),result=hit.result.ToString(),damage=hit.damage});}
    public static object Start30(){hz=30;return Start();}
    public static object Start60(){hz=60;return Start();}
    public static object Start(){if(SessionState.GetBool("CombatCombinationTests",false))throw new Exception("Already running");SessionState.SetBool("CombatCombinationTests",true);pending=Run();return new{started=true};}
    static Vector3 Origin()
    {
        var cc=player.GetComponent<CharacterController>();return new Vector3(2000,500.03f-(cc.center.y-cc.height*.5f)*player.transform.lossyScale.y,2000);
    }
    static async Task Reset()
    {
        left=right=false;keys=Array.Empty<Key>();await Wait(.2f);attack.SuspendForSwimming();
        player.GetComponent<CombatLocalHitStop>()?.Release();attack.animator.speed=1;
        motor.EndAttackFacing(true);motor.RestoreSavedPose(Origin(),Quaternion.identity);stamina.currentStamina=100;await Wait(.3f);
    }
    static void Frame()
    {
        if(!recording || !player || lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        bool stop=CombatLocalHitStop.Active(player);sawLocalStop|=stop;
        frames.Add(new{section,time=Time.time,spin=attack.IsSpinning,guard=attack.IsGuarding,phase=attack.CurrentAttackPhase,turn=attack.CompletedSpinTurns,stop,position=player.transform.position.ToString()});
        if(frames.Count%4==0)Capture(Folder+"/frames/"+frames.Count.ToString("D5")+".png");
    }
    static void Capture(string path)
    {
        var camera=CombatFeelPreview.Camera;Vector3 centre=player.GetComponent<CharacterController>().bounds.center+Vector3.up*.3f;
        camera.transform.position=centre+new Vector3(6,3,-7);camera.transform.LookAt(centre);camera.fieldOfView=52;
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(960,540,24);var texture=new Texture2D(960,540,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static async Task Run()
    {
        float previousCapture=Time.captureDeltaTime,previousScale=Time.timeScale;var previousKeyboard=Keyboard.current;var previousMouse=Mouse.current;
        checks.Clear();events.Clear();frames.Clear();Directory.CreateDirectory(Folder+"/frames");
        try
        {
            CombatFeelPreview.Spawn();Time.captureDeltaTime=1f/hz;
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();left=right=false;keys=Array.Empty<Key>();InputSystem.onBeforeUpdate+=Inject;
            player=CombatFeelPreview.Player;attack=player.GetComponent<SahurAttack>();motor=player.GetComponent<ThirdPersonPlayerController>();stamina=player.GetComponent<PlayerStamina>();
            var input=player.GetComponent<PlayerInput>();InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();
            CombatHitFeedback.HitConfirmed+=Hit;EditorApplication.update+=Frame;recording=true;await Wait(.7f);
            section="Club spin";CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2003);Physics.SyncTransforms();left=right=true;await Until(()=>attack.IsSpinning);Check(!attack.IsCharging && attack.CurrentComboStage<0,"Both-button chord has priority over light and charge");
            float before=stamina.Current;Vector3 root=player.transform.position;await Wait(3.1f);
            Check(attack.IsSpinning && attack.CompletedSpinTurns>=2,"Native club spin loops while both buttons are held");
            Check(stamina.Current<before-20,"Continuous spin drains stamina");
            Check(!attack.UsesAnimationRootMotion && Vector3.ProjectOnPlane(player.transform.position-root,Vector3.up).magnitude<.1f,"Authored spin never teleports the collision root");
            Check(events.Count>=2 && events.Where(x=>x.section=="Club spin").GroupBy(x=>x.turn).All(g=>g.Count()==1),"Club spin hits the real target once per revolution, with no per-frame or duplicate-collider hits");
            left=right=false;await Until(()=>!attack.IsCombatMotionActive);await Wait(.3f);
            Check(!attack.stickHitbox.enabled && !attack.IsHeavyAttackActive,"Releasing the chord clears damage without releasing a heavy attack");
            await Reset();section="Low stamina spin";stamina.currentStamina=5;left=right=true;await Wait(.8f);
            Check(!attack.IsSpinning && stamina.Current>=0,"Spin stops safely on stamina exhaustion");await Wait(.3f);Check(!attack.IsSpinning,"Held chord cannot restart exhausted spin each frame");
            await Reset();section="Blade X charge and rush";
            var loadout=player.GetComponent<SahurWeaponLoadout>();loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);await Wait(.25f);
            Check(attack.UsesTwinBlades,"Native equipment selects Capri dual blades");
            right=true;await Until(()=>attack.IsCharging);await Wait(.65f);
            Check(attack.ChargePose01>.95f,"Dual blades reach and hold the authored X pose");
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left)};await Wait(.12f);keys=Array.Empty<Key>();Check(attack.IsCharging,"Changing direction preserves dual-blade charge");
            right=false;await Until(()=>attack.IsHeavyAttackActive);Vector3 rushStart=player.transform.position;
            Check(!attack.UsesAnimationRootMotion,"Rush has exactly one physical displacement owner");await Until(()=>!attack.IsCombatMotionActive);float rushDistance=Vector3.ProjectOnPlane(player.transform.position-rushStart,Vector3.up).magnitude;
            checks.Add(new{rushDistance});Check(rushDistance>.4f && rushDistance<3.5f,"X release performs bounded forward rush");
            await Reset();section="Rush contact";CombatFeelPreview.Target.transform.position=new Vector3(2001.5f,500,2003);Physics.SyncTransforms();
            int rushHits=events.Count;right=true;await Until(()=>attack.IsCharging);await Wait(.65f);right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive);
            Check(events.Skip(rushHits).Any(x=>x.kind=="ChargedHeavy"),"Outward rush cut resolves a real blade contact");
            await Reset();CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2030);section="Rush wall";
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(player.transform.parent);wall.transform.position=new Vector3(2000,504,2002.2f);wall.transform.localScale=new Vector3(8,8,.3f);Physics.SyncTransforms();
            right=true;await Until(()=>attack.IsCharging);await Wait(.6f);Vector3 blockedStart=player.transform.position;right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive);
            float blockedDistance=Vector3.Dot(player.transform.position-blockedStart,Vector3.forward);UnityEngine.Object.DestroyImmediate(wall);
            checks.Add(new{blockedDistance});Check(blockedDistance>=0 && blockedDistance<2.05f,"Physical rush stops before an actual wall");
            await Reset();section="Blade guard";keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};await Until(()=>attack.GuardReady);
            var hp=player.GetComponent<PlayerHealth>();float health=hp.currentHealth,energy=stamina.Current;Vector3 point=player.GetComponent<CharacterController>().bounds.center+Vector3.forward;
            Check(!(player.GetComponent<SahurBoomerang>()?.IsBusy ?? false),"Dual-blade E guard never starts the club throw");
            Check(!hp.ApplyMeleeDamage(20,point,player.transform.position+Vector3.forward*5) && hp.currentHealth==health && stamina.Current<=energy-11.9f,"E guard blocks frontal melee with a real stamina cost");
            Check(hp.ApplyMeleeDamage(10,point,player.transform.position-Vector3.forward*5) && hp.currentHealth==health-10,"Rear melee bypasses frontal guard");
            hp.ApplyDamage(5,point);Check(hp.currentHealth==health-15,"Ordinary damage and hazards do not accidentally become guardable");await Wait(.2f);
            stamina.currentStamina=1;Check(hp.ApplyMeleeDamage(20,point,player.transform.position+Vector3.forward*5) && !attack.IsGuarding,"Exhausted guard breaks and receives damage");keys=Array.Empty<Key>();
            await Reset();section="Blade spin";CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2003);
            int hitStart=events.Count;left=right=true;await Until(()=>attack.IsSpinning);await Wait(2.2f);Check(events.Count>hitStart,"Dual-blade spin contacts the real target");
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};await Wait(.08f);Check(!attack.IsSpinning,"Ctrl cancels an established spin into dodge");
            await Reset();section="Jump chop";loadout.TryEquip(SahurWeaponLoadout.Weapon.Stick);CombatFeelPreview.Target.transform.position=new Vector3(2000.5f,500,2002.2f);int chopHits=events.Count;await Wait(.25f);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};await Wait(.12f);keys=Array.Empty<Key>();Check(motor.CanUseAirAttack,"Native Space produces actual airborne motion");left=true;await Wait(.06f);left=false;
            Check(attack.IsCombatMotionActive && attack.CurrentComboStage<0 && !attack.UsesAnimationRootMotion,"Airborne LMB starts authored chop without owning jump travel");
            await Wait(.1f);left=true;await Wait(.04f);left=false;await Until(()=>!attack.IsCombatMotionActive,5);await Wait(.3f);
            Check(events.Skip(chopHits).Any(x=>x.kind=="JumpSlash"),"Jump chop confirms actual weapon contact rather than a landing-radius hit");
            Check(attack.animator.speed==1 && !attack.stickHitbox.enabled,"Chop recovery restores animation and clears hit volumes");
            Check(Time.timeScale==1 && sawLocalStop,"Real confirmed impacts use local stop and preserve global time");
            await Reset();section="Combination interruptions";left=right=true;await Until(()=>attack.IsSpinning);attack.CinematicHandoffPending=true;await Wait(.08f);
            Check(!attack.IsCombatMotionActive && !attack.stickHitbox.enabled,"Cinematic handoff cancels spin and clears contact");attack.CinematicHandoffPending=false;
            await Reset();left=right=true;await Until(()=>attack.IsSpinning);attack.enabled=false;Check(!attack.IsSpinning && !attack.stickHitbox.enabled,"Disabling combat releases the authored spin safely");left=right=false;attack.enabled=true;await Wait(.2f);
            await Reset();section="Original club E throw";keys=new[]{Key.E};await Wait(.12f);
            Check(player.GetComponent<SahurBoomerang>()?.IsBusy ?? false,"Club E retains the original throw action");keys=Array.Empty<Key>();typeof(SahurAttack).GetMethod("CancelForMovement",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(attack,null);await Wait(.1f);
            recording=false;EditorApplication.update-=Frame;
            File.WriteAllText(Folder+"/tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,nativeInput=true,simulationHz=hz,checks,events,frames},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception error){File.WriteAllText(Folder+"/tests-error.txt",error.ToString());File.WriteAllText(Folder+"/tests-partial.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,events,frames},Newtonsoft.Json.Formatting.Indented));}
        finally
        {
            recording=false;EditorApplication.update-=Frame;CombatHitFeedback.HitConfirmed-=Hit;InputSystem.onBeforeUpdate-=Inject;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            if(previousKeyboard!=null && previousKeyboard.added)previousKeyboard.MakeCurrent();if(previousMouse!=null && previousMouse.added)previousMouse.MakeCurrent();
            CombatFeelPreview.Clear();Time.captureDeltaTime=previousCapture;Time.timeScale=previousScale;SessionState.SetBool("CombatCombinationTests",false);
        }
    }
}
