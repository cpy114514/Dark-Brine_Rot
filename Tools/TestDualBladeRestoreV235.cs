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
public static class TestDualBladeRestoreV235
{
    static string Folder=>".codex/twin-blade-restore-20261021/combinations-"+(recoilOnly?"direction-after-":"final-")+hz+"hz";
    static Keyboard keyboard;static Mouse mouse;static Key[] keys=Array.Empty<Key>();static bool left,right;
    static SahurAttack attack;static ThirdPersonPlayerController motor;static PlayerStamina stamina;static GameObject player;
    public sealed class HitRow {public string section,kind,result;public int turn;public float time,damage;}
    static readonly List<object> checks=new List<object>(),frames=new List<object>();static readonly List<HitRow> events=new List<HitRow>();static Task pending;
    static bool recoilOnly; static NailongHitReaction watchedNailong; static CappuccinoEnemy watchedCapri; static float reactionPeak, minimumProjection, maximumProjection, currentProjection; static CapsuleCollider[] bladeCaps; static Transform reactionFocus; static int hz=60;static string section;static int lastFrame=-1;static bool recording;static bool sawLocalStop;
    static void Inject(){if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
    static async Task Wait(float seconds){float until=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(25);while(Time.time<until){if(DateTime.UtcNow>deadline)throw new Exception("Native frame loop stalled: "+section);await Task.Delay(8);}}
    static async Task Until(Func<bool> condition,float timeout=4){float end=Time.time+timeout;while(!condition()){if(Time.time>end)throw new Exception("State timeout: "+section);await Wait(.01f);}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(new{passed=text});}
    static void Hit(CombatHitEvent hit){if(hit.attacker==player)events.Add(new HitRow{section=section,turn=attack.CompletedSpinTurns,time=Time.time,kind=hit.kind.ToString(),result=hit.result.ToString(),damage=hit.damage});}
    public static object StartRecoil60(){recoilOnly=true;hz=60;return Start();}
    public static object Start30(){recoilOnly=false;hz=30;return Start();}
    public static object Start60(){recoilOnly=false;hz=60;return Start();}
    public static object Start(){if(SessionState.GetBool("CombatCombinationTests",false))throw new Exception("Already running");SessionState.SetBool("CombatCombinationTests",true);EditorApplication.isPaused=false;pending=Run();return new{started=true};}
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
    public static void Frame()
    {
        if(!recording || !player || lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        SampleReaction();bool stop=CombatLocalHitStop.Active(player);sawLocalStop|=stop;
        frames.Add(new{section,time=Time.time,spin=attack.IsSpinning,guard=attack.IsGuarding,stage=attack.CurrentComboStage,phase=attack.CurrentAttackPhase,turn=attack.CompletedSpinTurns,stop,leftTip=V(bladeCaps!=null?BladeDirection(bladeCaps[0]):Vector3.zero),rightTip=V(bladeCaps!=null?BladeDirection(bladeCaps[1]):Vector3.zero),handLine=V(attack.animator.GetBoneTransform(HumanBodyBones.RightHand).position-attack.animator.GetBoneTransform(HumanBodyBones.LeftHand).position),rootYaw=player.transform.eulerAngles.y,recoilDegrees=reactionPeak,recoilAlongHit=currentProjection,position=player.transform.position.ToString()});
        Capture(Folder+"/frames/"+frames.Count.ToString("D5")+".png");
    }
    static void Capture(string path)
    {
        var camera=CombatFeelPreview.Camera;Vector3 centre=player.GetComponent<CharacterController>().bounds.center+Vector3.up*.3f;
        if(reactionFocus)centre=reactionFocus.position+Vector3.up*2;camera.transform.position=centre+new Vector3(8,4,-10);camera.transform.LookAt(centre);camera.fieldOfView=52;
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(640,360,24);var texture=new Texture2D(640,360,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
    static void SampleReaction()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        if(watchedNailong && watchedNailong.IsReacting && !CombatLocalHitStop.Active(watchedNailong.gameObject))
        {
            var bone=(Transform)typeof(NailongHitReaction).GetField("spine",flags).GetValue(watchedNailong);
            var basis=(Quaternion)typeof(NailongHitReaction).GetField("spineBase",flags).GetValue(watchedNailong);
            if(bone){float angle=Quaternion.Angle(bone.localRotation,basis);reactionPeak=Mathf.Max(reactionPeak,angle);var head=(Transform)typeof(NailongHitReaction).GetField("head",flags).GetValue(watchedNailong);if(head && angle>4){var matrix=bone.parent.localToWorldMatrix*Matrix4x4.TRS(bone.localPosition,basis,bone.localScale);var delta=head.position-matrix.MultiplyPoint3x4(bone.InverseTransformPoint(head.position));currentProjection=Vector3.Dot(delta,Vector3.forward);minimumProjection=Mathf.Min(minimumProjection,currentProjection);maximumProjection=Mathf.Max(maximumProjection,currentProjection);}}
        }
        if(watchedCapri && !CombatLocalHitStop.Active(watchedCapri.gameObject))
        {
            var basis=(Quaternion)typeof(CappuccinoEnemy).GetField("restRotation",flags).GetValue(watchedCapri);
            float angle=Quaternion.Angle(watchedCapri.visual.localRotation,basis);reactionPeak=Mathf.Max(reactionPeak,angle);if(angle>4){var visual=watchedCapri.visual;var rest=visual.parent?visual.parent.rotation*basis:basis;var axis=Quaternion.Inverse(rest)*Vector3.up;currentProjection=Vector3.Dot(visual.rotation*axis,Vector3.forward);minimumProjection=Mathf.Min(minimumProjection,currentProjection);maximumProjection=Mathf.Max(maximumProjection,currentProjection);}
        }
    }
    static Vector3 BladeDirection(CapsuleCollider blade)
    {
        if(!blade)return Vector3.zero;var mesh=blade.GetComponent<MeshFilter>().sharedMesh;int axis=blade.direction;float length=mesh.bounds.size[axis],sign=Mathf.Sign(mesh.bounds.center[axis]);if(sign==0)sign=1;
        float start=sign>0?mesh.bounds.min[axis]:mesh.bounds.max[axis],end=sign>0?mesh.bounds.max[axis]:mesh.bounds.min[axis];
        Vector3 a=Vector3.zero,b=Vector3.zero;int na=0,nb=0;foreach(var v in mesh.vertices){if(Mathf.Abs(v[axis]-start)<length*.18f){a+=v;na++;}if(Mathf.Abs(v[axis]-end)<length*.12f){b+=v;nb++;}}
        return blade.transform.TransformVector(b/nb-a/na).normalized;
    }
    static async Task VerifyRecoil()
    {
        await Reset();reactionFocus=CombatFeelPreview.Target.transform;reactionFocus.position=new Vector3(2000,500,2005);
        var hp=reactionFocus.GetComponent<NailongHealth>();var recoil=reactionFocus.GetComponent<NailongHitReaction>();
        float[] peaks=new float[2];
        for(int index=0;index<2;index++)
        {
            section=index==0?"Nailong light recoil":"Nailong heavy recoil";reactionPeak=0;minimumProjection=0;maximumProjection=0;watchedNailong=recoil;
            CombatHitFeedback.Apply(player,hp,1,reactionFocus.position+Vector3.up*2,index==0?CombatHitKind.ComboOne:CombatHitKind.ChargedHeavy,Vector3.forward);
            await Wait(.8f);peaks[index]=reactionPeak;checks.Add(new{actor="Nailong",index,minimumProjection,maximumProjection});Check(minimumProjection>=-.001f && maximumProjection>.02f,"Nailong head moves away along the actual hit direction");
            Check(!recoil.IsReacting,"Nailong recoil recovers after "+index);
        }
        checks.Add(new{nailongLight=peaks[0],nailongHeavy=peaks[1]});Check(peaks[0]>4&&peaks[1]>peaks[0]*1.4f&&peaks[1]<40,"Visible bounded Nailong recoil scales from light to heavy after local stop");
        watchedNailong=null;var enemy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),new Vector3(2000,500,2005),Quaternion.Euler(0,180,0),player.transform.parent);
        enemy.AddComponent<GameSaveExcluded>();enemy.GetComponent<CappuccinoAI>().enabled=false;enemy.GetComponent<CappuccinoUltimate>().enabled=false;
        var presentation=enemy.GetComponent<CappuccinoEnemy>();presentation.attackOnHit=false;var health=enemy.GetComponent<Health>();reactionFocus=enemy.transform;await Wait(.2f);
        for(int index=0;index<2;index++)
        {
            section=index==0?"Capri light recoil":"Capri heavy recoil";reactionPeak=0;minimumProjection=0;maximumProjection=0;watchedCapri=presentation;
            CombatHitFeedback.Apply(player,health,1,enemy.transform.position+Vector3.up*2,index==0?CombatHitKind.ComboOne:CombatHitKind.ChargedHeavy,Vector3.forward);
            await Wait(1.25f);peaks[index]=reactionPeak;checks.Add(new{actor="Capri",index,minimumProjection,maximumProjection});Check(minimumProjection>=-.001f && maximumProjection>.02f,"Capri tilts away along the actual hit direction");
        }
        checks.Add(new{capriLight=peaks[0],capriHeavy=peaks[1]});Check(peaks[0]>8&&peaks[1]>peaks[0]*1.1f&&peaks[1]<26,"Capri visual leans away from confirmed hits with stronger heavy reaction");
        watchedCapri=null;reactionFocus=null;UnityEngine.Object.DestroyImmediate(enemy);
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
            CombatHitFeedback.HitConfirmed+=Hit;player.AddComponent<DualBladeRecordRestoreV235>();recording=true;await Wait(.7f);
            var rifleLoadout=player.GetComponent<SahurWeaponLoadout>();rifleLoadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);await Wait(.3f);
            Check(rifleLoadout.UsesRifle && attack.animator.runtimeAnimatorController.name=="SahurRifle","Actual rifle equip uses the shared controller");
            var rifle=player.GetComponent<SahurRifleCombat>();int shots=rifle.ShotsFired;left=true;await Wait(.22f);left=false;await Wait(.2f);
            Check(rifle.ShotsFired>shots,"Rifle can still fire before switching weapons");
            Check(rifleLoadout.TryEquip(SahurWeaponLoadout.Weapon.Stick),"Rifle switches back to stick");await Wait(.3f);
            if(recoilOnly){await VerifyRecoil();File.WriteAllText(Folder+"/tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,nativeInput=true,simulationHz=hz,checks,events,frames},Newtonsoft.Json.Formatting.Indented));return;}
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
            section="Blade combo3";CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2030);Physics.SyncTransforms();
            bladeCaps=player.GetComponentsInChildren<CapsuleCollider>(true).Where(x=>x.name=="Right blade"||x.name=="Left blade").ToArray();
            Check(bladeCaps.Length==2 && bladeCaps.All(x=>Mathf.Abs(x.height*x.transform.lossyScale.x-2.52f)<.01f),"Both blades and real hit volumes are 1.2x their previous size");
            await Reset();var comboRoot=player.transform.rotation;left=true;await Until(()=>attack.CurrentComboStage==0);left=false;
            await Until(()=>attack.CurrentAttackPhase>=.76f);left=true;await Wait(.04f);left=false;await Until(()=>attack.CurrentComboStage==1);
            await Until(()=>attack.CurrentAttackPhase>=.76f);left=true;await Wait(.04f);left=false;await Until(()=>attack.CurrentComboStage==2);
            await Until(()=>!attack.IsCombatMotionActive);Check(Quaternion.Angle(player.transform.rotation,comboRoot)<1,"Authored finisher completes without an extra controller-root turn");
            await Reset();section="Blade X charge and rush";

            Check(attack.UsesTwinBlades,"Native equipment selects Capri dual blades");
            right=true;await Until(()=>attack.IsCharging);await Wait(1.25f);
            Check(attack.ChargePose01>.95f,"Dual blades reach and hold the authored X pose");
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left)};await Wait(.12f);keys=Array.Empty<Key>();Check(attack.IsCharging,"Changing direction preserves dual-blade charge");
            right=false;await Until(()=>attack.IsHeavyAttackActive);Vector3 rushStart=player.transform.position;
            Check(!attack.UsesAnimationRootMotion,"Rush has exactly one physical displacement owner");await Until(()=>!attack.IsCombatMotionActive);float rushDistance=Vector3.ProjectOnPlane(player.transform.position-rushStart,Vector3.up).magnitude;
            checks.Add(new{rushDistance});Check(rushDistance>15.5f && rushDistance<16.7f,"X release performs the longer collision-resolved 16m rush");
            await Reset();section="Rush contact";CombatFeelPreview.Target.transform.position=new Vector3(2001.5f,500,2003);Physics.SyncTransforms();
            int rushHits=events.Count;right=true;await Until(()=>attack.IsCharging);await Wait(1.25f);right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive);
            Check(events.Skip(rushHits).Any(x=>x.kind=="ChargedHeavy"),"Outward rush cut resolves a real blade contact");
            await Reset();CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2030);section="Rush wall";
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(player.transform.parent);wall.transform.position=new Vector3(2000,504,2002.2f);wall.transform.localScale=new Vector3(8,8,.3f);Physics.SyncTransforms();
            right=true;await Until(()=>attack.IsCharging);await Wait(1.25f);Vector3 blockedStart=player.transform.position;right=false;await Until(()=>attack.IsHeavyAttackActive);await Until(()=>!attack.IsCombatMotionActive);
            float blockedDistance=Vector3.Dot(player.transform.position-blockedStart,Vector3.forward);UnityEngine.Object.DestroyImmediate(wall);
            checks.Add(new{blockedDistance});Check(blockedDistance>=0 && blockedDistance<2.05f,"Physical rush stops before an actual wall");
            await Reset();section="Blade guard";keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};await Until(()=>attack.GuardReady);
            var hp=player.GetComponent<PlayerHealth>();float health=hp.currentHealth,energy=stamina.Current;Vector3 point=player.GetComponent<CharacterController>().bounds.center+Vector3.forward;
            Check(!(player.GetComponent<SahurBoomerang>()?.IsBusy ?? false),"Dual-blade E guard never starts the club throw");
            Check(!hp.ApplyMeleeDamage(20,point,player.transform.position+Vector3.forward*5) && hp.currentHealth==health && stamina.Current<=energy-11.9f,"E guard blocks frontal melee with a real stamina cost");
            Check(hp.ApplyMeleeDamage(10,point,player.transform.position-Vector3.forward*5) && hp.currentHealth==health-10,"Rear melee bypasses frontal guard");
            hp.ApplyDamage(5,point);Check(hp.currentHealth==health-15,"Ordinary damage and hazards do not accidentally become guardable");await Wait(.2f);
            stamina.currentStamina=1;Check(!hp.ApplyMeleeDamage(20,point,player.transform.position+Vector3.forward*5) && !attack.IsGuarding,"Last active frontal guard absorbs the hit before exhaustion");keys=Array.Empty<Key>();
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
            await Reset();section="Repeated rifle blade switching";
            for(int cycle=0;cycle<3;cycle++){
                Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.M4A1),"Rifle re-equip "+cycle);await Wait(.25f);
                Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades),"Blade re-equip "+cycle);await Wait(.25f);
                Check(attack.enabled && attack.UsesTwinBlades && attack.animator.GetLayerIndex("Twin Blade Charge")>0,"Blade mode and charge layer survive rifle switching "+cycle);
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};await Until(()=>attack.GuardReady);keys=Array.Empty<Key>();await Until(()=>!attack.IsGuarding);await Wait(.2f);
                Check(attack.animator.GetLayerWeight(attack.animator.GetLayerIndex("Rifle Locomotion"))<.001f,"Rifle layers release blade animation ownership "+cycle);
            }
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

[DefaultExecutionOrder(32000)] public sealed class DualBladeRecordRestoreV235:MonoBehaviour { void LateUpdate()=>TestDualBladeRestoreV235.Frame(); }
