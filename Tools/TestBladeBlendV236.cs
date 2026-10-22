using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Mavis;
using Unity.Profiling;
public static class TestRushGuardV236
{
    static string Folder=>".codex/blade-animation-polish-20261021/rush-"+take+"-"+hz+"hz";
    static string take="final",section="Setup";
    static int hz=60,lastFrame=-1,immuneSamples,blockedEvents,playerHitEvents,wallContacts,protectedWallContacts;
    static Keyboard keyboard;static Mouse mouse;static bool left,right,recording,probeRush;
    static string probeFailure;
    static ProfilerRecorder sweep;
    static Key[] keys=Array.Empty<Key>();static GameObject player,enemy;static SahurAttack attack;static SahurTwinBladeMotion rush;
    static ThirdPersonPlayerController motor;static PlayerHealth hp;static PlayerStamina stamina;static Task pending;
    static readonly List<object> checks=new List<object>(),frames=new List<object>(),events=new List<object>();
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(new{passed=message});}
    static async Task Wait(float seconds){float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(90);while(Time.time<end){if(DateTime.UtcNow>deadline)throw new Exception("Frame loop stalled: "+section);await Task.Delay(8);}}
    static async Task Until(Func<bool> condition,float seconds=5){float end=Time.time+seconds;while(!condition()){if(Time.time>end)throw new Exception("Timeout: "+section);await Wait(.001f);}}
    static void Inject(){if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
    static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
    static void Hit(CombatHitEvent hit){if(hit.target==hp){playerHitEvents++;if(hit.result==CombatHitResult.Blocked)blockedEvents++;}events.Add(new{section,time=Time.time,attacker=hit.attacker.name,target=hit.target.name,result=hit.result.ToString(),kind=hit.kind.ToString(),hit.damage});}
    public static object Start60(){hz=60;return Start();}
    public static object Start30(){hz=30;return Start();}
    public static object Start(){if(!EditorApplication.isPlaying||SessionState.GetBool("CombatCombinationTests",false))throw new Exception("Not ready");SessionState.SetBool("CombatCombinationTests",true);EditorApplication.isPaused=false;pending=Run();return new{started=true,Folder};}
    static Vector3 Origin(){var cc=player.GetComponent<CharacterController>();return new Vector3(2000,500.03f-(cc.center.y-cc.height*.5f)*player.transform.lossyScale.y,2000);}
    static async Task Reset(string next)
    {
        probeRush=false;left=right=false;keys=Array.Empty<Key>();await Wait(.15f);attack.SuspendForSwimming();player.GetComponent<CombatLocalHitStop>()?.Release();attack.animator.speed=1;
        motor.EndAttackFacing(true);motor.RestoreSavedPose(Origin(),Quaternion.identity);stamina.currentStamina=100;hp.currentHealth=100;section=next;await Wait(.3f);Physics.SyncTransforms();
    }
    static async Task Guard(){keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};await Until(()=>attack.GuardReady);stamina.currentStamina=100;}
    static async Task Charge(float seconds){right=true;await Until(()=>attack.IsCharging);await Wait(seconds);right=false;await Until(()=>attack.IsHeavyAttackActive);}
    public static void ControllerHit(ControllerColliderHit contact){if(!probeRush||!attack.IsHeavyAttackActive||!(bool)typeof(SahurTwinBladeMotion).GetField("movingRush",Private).GetValue(rush))return;wallContacts++;if(hp.IsProtected)protectedWallContacts++;}
    public static void Frame()
    {
        if(!recording||!player||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        bool immune=rush.IsDamageImmune;
        if(probeRush&&immune&&attack.animator.GetLayerWeight(attack.animator.GetLayerIndex("Twin Blade Charge"))>.001f)
            probeFailure="Outgoing X pose leaked into the damaging rush";
        if(probeRush&&immune)
        {
            float before=hp.currentHealth;int beforeEvents=playerHitEvents;
            hp.ApplyDamage(20,player.transform.position);hp.ApplyAttackDamage(20,player.transform.position,player.transform.position+Vector3.back*3);
            bool feedback=CombatHitFeedback.Apply(enemy,hp,20,player.transform.position,CombatHitKind.ChargedHeavy);
            if(hp.currentHealth!=before||feedback||beforeEvents!=playerHitEvents)probeFailure="Rush immunity leaked damage/feedback";
            immuneSamples++;
        }
        frames.Add(new{section,time=Time.time,frame=Time.frameCount,position=V(player.transform.position),charge=attack.ChargeElapsed,charge01=attack.Charge01,ready=attack.ChargeReady,heavy=attack.IsHeavyAttackActive,phase=attack.HeavyAttackPhase,sweepMs=sweep.Valid?sweep.LastValue/1000000.0:0,immune,guard=attack.GuardReady,health=hp.currentHealth,stamina=stamina.Current});
        Capture(Folder+"/frames/"+frames.Count.ToString("D5")+".png");
    }
    static void Capture(string path)
    {
        var camera=CombatFeelPreview.Camera;Vector3 centre=player.GetComponent<CharacterController>().bounds.center+Vector3.up*.3f;
        camera.transform.position=centre+new Vector3(8,4,-10);camera.transform.LookAt(centre);camera.fieldOfView=52;
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(640,360,24);var texture=new Texture2D(640,360,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static async Task Projectile(bool front)
    {
        float before=hp.currentHealth;int beforeBlocks=blockedEvents;
        var cc=player.GetComponent<CharacterController>();var dir=front?player.transform.forward:-player.transform.forward;
        var go=new GameObject(front?"Frontal spit":"Rear spit");go.transform.SetParent(player.transform.parent);go.transform.position=cc.bounds.center+dir*5;
        var projectile=go.AddComponent<NailongSpitProjectile>();projectile.Launch(enemy.transform,player.transform,-dir*40,9,.11f);Physics.SyncTransforms();
        await Until(()=>!projectile||projectile.HasImpacted,2);await Wait(.04f);
        Check(front?hp.currentHealth==before&&blockedEvents==beforeBlocks+1:hp.currentHealth==before-9,"Real spit trajectory "+(front?"blocks once from front":"damages from behind"));
        if(go)UnityEngine.Object.DestroyImmediate(go);
    }
    static async Task Blast(BombardinoBoss owner,bool front,BombardinoOrdnance.Kind kind)
    {
        float before=hp.currentHealth;int beforeBlocks=blockedEvents;
        Vector3 point=player.GetComponent<CharacterController>().bounds.center+(front?Vector3.forward:Vector3.back)*3;
        var go=new GameObject(kind+" test");go.transform.SetParent(player.transform.parent);go.transform.position=point;
        var bomb=go.AddComponent<BombardinoOrdnance>();bomb.Launch(owner,kind,Vector3.down,player.transform,point,12,8);bomb.enabled=false;Physics.SyncTransforms();
        typeof(BombardinoOrdnance).GetMethod("Blast",Private).Invoke(bomb,new object[]{12f,false});
        Check(front?hp.currentHealth==before&&blockedEvents==beforeBlocks+1:hp.currentHealth<before,"Real "+kind+" blast "+(front?"blocks from front":"damages from behind"));
        UnityEngine.Object.DestroyImmediate(go);await Wait(.1f);
    }
    static async Task Sonar(bool front)
    {
        float before=hp.currentHealth;int beforeBlocks=blockedEvents;
        var source=CombatFeelPreview.Target.GetComponent<NailongAttack>();source.enabled=true;
        var go=new GameObject("Sonar test");go.transform.SetParent(player.transform.parent);go.transform.position=new Vector3(2000,500,2000)+(front?Vector3.forward:Vector3.back)*4;
        var sonar=go.AddComponent<NailongGroundSlam>();sonar.Initialize(source,8,1,.4f,.8f,.4f);sonar.enabled=false;Physics.SyncTransforms();sonar.Tick(.8f);
        Check(front?hp.currentHealth==before&&blockedEvents==beforeBlocks+1&&sonar.DamageHits==0:hp.currentHealth==before-NailongGroundSlam.PulseDamage&&sonar.DamageHits==1,"Real expanding sonar ring "+(front?"blocks from front without damage impulse":"damages from behind"));
        source.enabled=false;UnityEngine.Object.DestroyImmediate(go);await Wait(.1f);
    }
    static async Task Run()
    {
        float oldCapture=Time.captureDeltaTime,oldScale=Time.timeScale;var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;
        checks.Clear();frames.Clear();events.Clear();probeFailure=null;immuneSamples=blockedEvents=playerHitEvents=wallContacts=protectedWallContacts=0;Directory.CreateDirectory(Folder+"/frames");bool passed=false;
        try
        {
            CombatFeelPreview.Spawn();Time.captureDeltaTime=1f/hz;player=CombatFeelPreview.Player;attack=player.GetComponent<SahurAttack>();motor=player.GetComponent<ThirdPersonPlayerController>();hp=player.GetComponent<PlayerHealth>();stamina=player.GetComponent<PlayerStamina>();
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;var input=player.GetComponent<PlayerInput>();InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();
            enemy=new GameObject("Incoming attack source");enemy.transform.SetParent(player.transform.parent);enemy.transform.position=new Vector3(2000,500,2010);
            CombatHitFeedback.HitConfirmed+=Hit;await Wait(.6f);sweep=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Sahur.WeaponSweep",512);
            var restored=player.GetComponent<SahurWeaponLoadout>();restored.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);await Wait(.3f);
            Check(restored.UsesRifle && attack.animator.runtimeAnimatorController.name=="SahurRifle","Reproduce actual rifle controller ownership before restoring dual blades");
            Check(restored.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades),"Switch from real rifle to owned twin blades");await Wait(.3f);
            Check(attack.animator.HasState(0,Animator.StringToHash("Base Layer.Blade Spin")) && attack.animator.HasState(0,Animator.StringToHash("Base Layer.Blade Guard")) && attack.animator.GetLayerIndex("Twin Blade Charge")>0,"Shared rifle graph retains complete approved blade move set");rush=player.GetComponent<SahurTwinBladeMotion>();
            CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2040);Physics.SyncTransforms();player.AddComponent<RushGuardRecordV236>();recording=true;
            await Reset("Early charge release");right=true;await Until(()=>attack.IsCharging);await Wait(.7f);Check(!attack.ChargeReady&&attack.Charge01<.4f,"Blade charge is unready before minimum hold");Vector3 start=player.transform.position;float energy=stamina.Current;right=false;await Wait(.85f);
            Check(!attack.IsHeavyAttackActive&&!attack.IsCharging&&Vector3.Distance(start,player.transform.position)<.1f&&stamina.Current>=energy,"Early release cancels without rush, stamina spend or delayed launch");
            await Reset("Charge blend pause and ownership cleanup");right=true;await Until(()=>attack.IsCharging);await Wait(.6f);right=false;await Until(()=>!attack.IsCharging);
            int blendLayer=attack.animator.GetLayerIndex("Twin Blade Charge");float blendWeight=attack.animator.GetLayerWeight(blendLayer);
            Check(blendWeight>.5f && !hp.IsProtected && !attack.stickHitbox.enabled,"Early cancel blends presentation without retaining damage or immunity");
            Time.timeScale=0;await Task.Delay(160);Check(Mathf.Abs(attack.animator.GetLayerWeight(blendLayer)-blendWeight)<.001f,"Pause freezes outgoing charge blend");Time.timeScale=1;await Wait(.3f);
            Check(attack.animator.GetLayerWeight(blendLayer)<.001f,"Charge cancellation fully releases its layer");
            right=true;await Until(()=>attack.IsCharging);await Wait(.6f);right=false;await Until(()=>!attack.IsCharging);attack.SuspendForSwimming();
            Check(attack.animator.GetLayerWeight(blendLayer)==0,"Swimming handoff immediately clears outgoing X pose");
            await Wait(.3f);right=true;await Until(()=>attack.IsCharging);await Wait(.6f);right=false;await Until(()=>!attack.IsCharging);attack.enabled=false;
            Check(attack.animator.GetLayerWeight(blendLayer)==0,"Disabling combat clears outgoing charge layer");attack.enabled=true;
            await Wait(.3f);right=true;await Until(()=>attack.IsCharging);await Wait(.6f);right=false;await Until(()=>!attack.IsCharging);
            Check(player.GetComponent<SahurWeaponLoadout>().TryEquip(SahurWeaponLoadout.Weapon.Stick) && attack.animator.GetLayerWeight(blendLayer)==0,"Weapon switch cannot inherit outgoing blade charge");
            player.GetComponent<SahurWeaponLoadout>().TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades);
            await Reset("Long charge and 16m rush");right=true;await Until(()=>attack.IsCharging);await Wait(1.25f);Check(attack.ChargeReady&&attack.Charge01>.45f&&attack.Charge01<.55f&&attack.ChargePose01>.99f,"1.2s minimum and 2.6s full charge preserve held X pose");
            hp.ApplyAttackDamage(3,player.transform.position,enemy.transform.position);Check(hp.currentHealth==97&&!hp.IsProtected,"Charge windup remains vulnerable");
            right=false;await Until(()=>attack.IsHeavyAttackActive);start=player.transform.position;Check(!hp.IsProtected&&!attack.UsesAnimationRootMotion,"Rush startup is vulnerable and has one displacement owner");probeRush=true;await Until(()=>!attack.IsCombatMotionActive);probeRush=false;
            float distance=Vector3.ProjectOnPlane(player.transform.position-start,Vector3.up).magnitude;checks.Add(new{rushDistance=distance,immuneSamples});Check(distance>15.5f&&distance<16.7f&&immuneSamples>=3&&probeFailure==null,"Native rush travels 16m with damage immunity sampled throughout travel");
            Check(!hp.IsProtected,"Recovery clears rush immunity");hp.ApplyDamage(3,player.transform.position);Check(hp.currentHealth==94,"Generic damage works again after rush");
            await Reset("Fully charged rush contact");CombatFeelPreview.Target.transform.position=new Vector3(2001.5f,500,2007);Physics.SyncTransforms();right=true;await Until(()=>attack.IsCharging);await Wait(2.7f);Check(attack.Charge01==1,"Dual blades fully charge after 2.6 seconds");int beforeHits=events.Count;right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive);
            Check(events.Skip(beforeHits).Any(x=>x.ToString().Contains("ChargedHeavy")&&x.ToString().Contains("Nailong")),"Long rush still confirms actual continuous blade contact");
            CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2040);await Reset("Wall-resolved rush");var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(player.transform.parent);wall.transform.position=new Vector3(2000,504,2002.2f);wall.transform.localScale=new Vector3(8,8,.3f);Physics.SyncTransforms();
            await Charge(1.25f);start=player.transform.position;probeRush=true;await Until(()=>!attack.IsCombatMotionActive);probeRush=false;float blockedDistance=Vector3.Dot(player.transform.position-start,Vector3.forward);UnityEngine.Object.DestroyImmediate(wall);checks.Add(new{blockedDistance,wallContacts,protectedWallContacts});Check(blockedDistance>=0&&blockedDistance<2.05f&&wallContacts>0&&wallContacts==protectedWallContacts,"Real wall stops 16m rush and Move collision callbacks remain protected");
            await Reset("Rush cancellation");await Charge(1.25f);await Until(()=>hp.IsProtected);attack.SuspendForSwimming();Check(!hp.IsProtected,"Cancel immediately removes rush immunity");
            await Reset("Rush disable");await Charge(1.25f);await Until(()=>hp.IsProtected);attack.enabled=false;Check(!hp.IsProtected,"Disabling combat removes immunity in the same call");attack.enabled=true;
            await Reset("Rush control handoff");await Charge(1.25f);await Until(()=>hp.IsProtected);attack.CinematicHandoffPending=true;Check(!hp.IsProtected,"Narrative handoff cannot inherit rush protection");attack.CinematicHandoffPending=false;attack.SuspendForSwimming();
            await Reset("Rush equip change");await Charge(1.25f);await Until(()=>hp.IsProtected);var equipment=player.GetComponent<SahurWeaponLoadout>();Check(equipment.TryEquip(SahurWeaponLoadout.Weapon.Stick)&&!hp.IsProtected,"Unequipping blades clears immunity immediately");equipment.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades);
            await Reset("Rush external lock");await Charge(1.25f);await Until(()=>hp.IsProtected);motor.ExternalControlLock=true;Check(!hp.IsProtected,"External control lock clears rush protection");motor.ExternalControlLock=false;attack.SuspendForSwimming();
            await Reset("Rush death gate");await Charge(1.25f);await Until(()=>hp.IsProtected);hp.currentHealth=0;Check(!hp.IsProtected,"Dead character never retains rush immunity");hp.currentHealth=100;attack.SuspendForSwimming();
            await Reset("All frontal damage guard");await Guard();Check(Mathf.Abs(attack.frontalGuardArc-180)<.01f,"New guard config replaces legacy 130 degree serialized cone");
            foreach(float angle in new[]{-89f,0f,89f,90f})
            {stamina.currentStamina=100;float before=hp.currentHealth;int blockCount=blockedEvents;Vector3 from=player.transform.position+Quaternion.Euler(0,angle,0)*Vector3.forward*5;Check(!hp.ApplyAttackDamage(10000,player.transform.position,from)&&hp.currentHealth==before&&blockedEvents==blockCount+1,"Frontal high-damage attack fully blocks at "+angle+" degrees");}
            foreach(float angle in new[]{-91f,91f,180f})
            {float before=hp.currentHealth;Vector3 from=player.transform.position+Quaternion.Euler(0,angle,0)*Vector3.forward*5;Check(hp.ApplyAttackDamage(1,player.transform.position,from)&&hp.currentHealth==before-1,"Rear hemisphere remains vulnerable at "+angle+" degrees");}
            enemy.transform.position=player.transform.position+Vector3.forward*5;int oldEvents=playerHitEvents;stamina.currentStamina=100;float oldHp=hp.currentHealth;
            Check(CombatHitFeedback.Apply(enemy,hp,1000,player.transform.position+Vector3.forward,CombatHitKind.ChargedHeavy)&&hp.currentHealth==oldHp&&playerHitEvents==oldEvents+1,"Unified hit resolver blocks heavy attack with exactly one feedback event");
            await Projectile(true);await Projectile(false);stamina.currentStamina=100;
            var bossGo=new GameObject("Bombardment owner");bossGo.transform.SetParent(player.transform.parent);bossGo.transform.position=new Vector3(2040,550,2040);bossGo.AddComponent<Health>();bossGo.AddComponent<GameSaveExcluded>();var boss=bossGo.AddComponent<BombardinoBoss>();boss.enabled=false;
            foreach(var kind in new[]{BombardinoOrdnance.Kind.Bomb,BombardinoOrdnance.Kind.Missile,BombardinoOrdnance.Kind.Fire}){stamina.currentStamina=100;await Blast(boss,true,kind);await Blast(boss,false,kind);}
            typeof(BombardinoBoss).GetProperty("CurrentAttack").SetValue(boss,BombardinoBoss.AttackKind.SpeedRun);
            var run=typeof(BombardinoBoss).GetMethod("RunAttack",Private,null,new[]{typeof(float),typeof(Vector3)},null);
            var rushVictims=(HashSet<PlayerHealth>)typeof(BombardinoBoss).GetField("rushVictims",Private).GetValue(boss);
            foreach(bool front in new[]{true,false})
            {
                oldHp=hp.currentHealth;int blocksBefore=blockedEvents;stamina.currentStamina=100;rushVictims.Clear();
                Vector3 from=new Vector3(2000,520,2000)+(front?Vector3.forward:Vector3.back)*8;boss.transform.position=new Vector3(2000,520,2000)+(front?Vector3.back:Vector3.forward)*8;
                Physics.SyncTransforms();run.Invoke(boss,new object[]{.1f,from});
                Check(front?hp.currentHealth==oldHp&&blockedEvents==blocksBefore+1:hp.currentHealth<oldHp,"Real high-speed strafing sweep "+(front?"blocks using incoming segment origin":"damages from rear segment origin"));
            }
            UnityEngine.Object.DestroyImmediate(bossGo);
            stamina.currentStamina=100;await Sonar(true);await Sonar(false);
            stamina.currentStamina=1;oldHp=hp.currentHealth;Check(!hp.ApplyAttackDamage(10000,player.transform.position,player.transform.position+Vector3.forward*5)&&hp.currentHealth==oldHp&&!attack.IsGuarding&&stamina.Current==0,"Last active guard absorbs even huge hit before exhausting stamina");keys=Array.Empty<Key>();
            await Reset("Generic hazard and narrative floor");hp.ApplyDamage(4,player.transform.position);Check(hp.currentHealth==96,"Generic hazard still damages without a false frontal origin");hp.NarrativeHealthFloor=1;hp.ApplyAttackDamage(10000,player.transform.position,enemy.transform.position);Check(hp.currentHealth==1,"Nonlethal narrative health floor remains intact");hp.NarrativeHealthFloor=0;
            await Reset("Club charge regression");player.GetComponent<SahurWeaponLoadout>().TryEquip(SahurWeaponLoadout.Weapon.Stick);await Wait(.2f);right=true;await Until(()=>attack.IsCharging);await Wait(.3f);Check(attack.ChargeReady&&Mathf.Abs(attack.EffectiveFullChargeTime-attack.fullChargeTime)<.001f,"Club retains original charge duration and early release");right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive);Check(!hp.IsProtected&&Time.timeScale==1,"Club heavy never gains blade immunity or alters global time");await Wait(.3f);
            passed=true;
        }
        catch(Exception error){File.WriteAllText(Folder+"/error.txt",error.ToString());}
        finally
        {
            recording=false;if(sweep.Valid)sweep.Dispose();File.WriteAllText(Folder+"/tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,nativeInput=true,simulationHz=hz,checks,events,frames},Newtonsoft.Json.Formatting.Indented));
            CombatHitFeedback.HitConfirmed-=Hit;InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();CombatFeelPreview.Clear();Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;SessionState.SetBool("CombatCombinationTests",false);
        }
    }
}
[DefaultExecutionOrder(32000)]public sealed class RushGuardRecordV236:MonoBehaviour
{
    void LateUpdate()=>TestRushGuardV236.Frame();
    void OnControllerColliderHit(ControllerColliderHit hit)=>TestRushGuardV236.ControllerHit(hit);
}
