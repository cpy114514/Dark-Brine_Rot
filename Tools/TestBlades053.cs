using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public static class TestBlades053
{
    public static object Start(string label,int hz=60,bool island=false)
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        string folder=".codex/blades053/"+label;if(Directory.Exists(folder))throw new Exception("Preserve prior take");Directory.CreateDirectory(folder+"/frames");
        var go=new GameObject("Native blade regression");var run=go.AddComponent<BladeReview053>();run.folder=folder;run.hz=hz;run.island=island;run.Begin();return new{started=folder};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class BladeReview053:MonoBehaviour
{
    public string folder;public int hz;public bool island;Vector3 ground;
    Keyboard keyboard;Mouse mouse;Key[] keys=Array.Empty<Key>();bool left,right;
    InputSettings oldSettings,settings;readonly List<InputDevice> disabled=new List<InputDevice>();
    float capture;bool armed;readonly List<object> checks=new List<object>(),rows=new List<object>();readonly List<string> errors=new List<string>();
    GameObject actor,root;ThirdPersonPlayerController movement;SahurAttack attack;PlayerStamina stamina;SahurWeaponLoadout loadout;SahurTwinBladeMotion rush;
    readonly List<GameObject> birds=new List<GameObject>();Camera camera;RenderTexture texture;Texture2D pixels;
    string section="setup",failure;int frame;bool record,collectJump;
    readonly HashSet<int> airStages=new HashSet<int>();int jumpHits,heavyHits,stoppedFrames;float maxStopTravel;Vector3 lastPosition;bool wasStopped;
    public void Begin(){armed=true;StartCoroutine(Guard());}
    IEnumerator Guard()
    {
        var run=Run();
        while(true)
        {
            object next;
            try{if(!run.MoveNext())yield break;next=run.Current;}
            catch(Exception exception){errors.Add(exception.ToString());failure="Native harness interrupted";Finish();yield break;}
            yield return next;
        }
    }
    void Check(bool value,string name){checks.Add(new{ok=value,name});if(!value)failure=name;}
    void Inject()
    {
        if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
    }
    void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    void Hit(CombatHitEvent hit)
    {
        if(!actor||hit.attacker!=actor)return;
        if(hit.kind==CombatHitKind.JumpSlash)jumpHits++;
        if(hit.kind==CombatHitKind.ChargedHeavy)heavyHits++;
    }
    IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
    IEnumerator TapJump(){keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};yield return Frames(3);keys=Array.Empty<Key>();}
    IEnumerator Click(){left=true;yield return Frames(2);left=false;yield return Frames(2);}
    IEnumerator ResetActor(string name)
    {
        section=name;record=false;left=right=false;keys=Array.Empty<Key>();yield return Frames(hz/4);
        attack.SuspendForSwimming();actor.GetComponent<CombatLocalHitStop>()?.Release();attack.animator.speed=1;
        var cc=actor.GetComponent<CharacterController>();float sole=(cc.center.y-cc.height*.5f)*actor.transform.lossyScale.y;
        movement.RestoreSavedPose(ground+new Vector3(0,.08f-sole,0),Quaternion.identity);movement.EndAttackFacing(true);stamina.currentStamina=100;
        foreach(var bird in birds)if(bird)Destroy(bird);birds.Clear();
        yield return Frames(hz/3);Physics.SyncTransforms();
    }
    IEnumerator Run()
    {
        capture=Time.captureDeltaTime;Time.captureDeltaTime=1f/hz;
        oldSettings=InputSystem.settings;settings=Instantiate(oldSettings);settings.hideFlags=HideFlags.DontSave;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
        foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){disabled.Add(d);InputSystem.DisableDevice(d);}
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;Application.logMessageReceived+=Log;CombatHitFeedback.HitConfirmed+=Hit;
        root=new GameObject("Disposable native blade fixture");
        if(island)
        {
            ground=new Vector3(288,28,346);
            if(!Physics.Raycast(ground+Vector3.up*100,Vector3.down,out var hit,250,~0,QueryTriggerInteraction.Ignore))throw new Exception("Find island terrain");
            ground=hit.point;
            // Fixture-only prerequisite in the disposable Play session. Keep
            // production progression rules intact when exercising rifle equip.
            var ids=(HashSet<string>[])typeof(FirstIslandBossProgression).GetField("identities",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var changes=(Dictionary<string,int>)typeof(GameWorldProgress).GetField("changes",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
            ids[(int)FirstIslandObjectives.BossKind.Nailong].Add("blade053-test-prerequisite");changes["blade053-test-prerequisite"]=0;
        }
        else
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(160,1,160);
            var sun=new GameObject("Key",typeof(Light));sun.transform.SetParent(root.transform);sun.transform.rotation=Quaternion.Euler(45,-30,0);sun.GetComponent<Light>().type=LightType.Directional;
        }
        var control=new GameObject("Control camera",typeof(Camera),typeof(AudioListener));control.tag="MainCamera";control.transform.SetParent(root.transform);control.transform.position=new Vector3(0,4,-12);control.transform.rotation=Quaternion.identity;
        var view=new GameObject("Native blade review camera",typeof(Camera));view.transform.SetParent(root.transform);camera=view.GetComponent<Camera>();camera.fieldOfView=58;camera.backgroundColor=new Color(.1f,.14f,.18f);camera.clearFlags=CameraClearFlags.SolidColor;
        texture=new RenderTexture(960,540,24);texture.Create();camera.targetTexture=texture;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);
        actor.AddComponent<GameSaveExcluded>();movement=actor.GetComponent<ThirdPersonPlayerController>();attack=actor.GetComponent<SahurAttack>();stamina=actor.GetComponent<PlayerStamina>();loadout=actor.GetComponent<SahurWeaponLoadout>();
        actor.transform.position=ground+Vector3.up*4;Cursor.lockState=CursorLockMode.Locked;yield return Frames(hz/2);loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades,true);yield return Frames(hz/2);rush=actor.GetComponent<SahurTwinBladeMotion>();
        foreach(string path in new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
        {
            yield return ResetActor(Path.GetFileNameWithoutExtension(path)+" aerial");
            attack.animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);attack.animator.Rebind();attack.animator.Update(0);
            // Use the real equip path to refresh cached state and weapon settings.
            loadout.TryEquip(SahurWeaponLoadout.Weapon.Stick);loadout.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades);
            if(path.Contains("M4A1"))
            {
                Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.M4A1),"Actual M4A1 equip succeeds");yield return Frames(hz/3);
                Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades),"Actual rifle-to-blades switch succeeds");
            }
            yield return Frames(hz/4);
            Check(attack.animator.HasState(0,Animator.StringToHash("Base Layer.Capri Air Combo 1"))&&attack.animator.HasState(0,Animator.StringToHash("Base Layer.Capri Air Combo 2")),section+" states installed");
            Check(attack.animator.GetLayerIndex("Air Attack Legs")>0,section+" authored airborne leg layer installed");
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Aerial hurtbox";target.tag=attack.enemyTag;target.transform.SetParent(root.transform);target.transform.position=ground+new Vector3(0,5,2.4f);target.transform.localScale=new Vector3(3,9,1);
            var health=target.AddComponent<Health>();health.maxHealth=health.currentHealth=10000;target.AddComponent<GameSaveExcluded>();
            airStages.Clear();jumpHits=0;collectJump=true;record=true;
            float previousHeight=actor.transform.position.y;yield return TapJump();yield return Frames(3);yield return Click();
            // Buffer the authored three-stage airborne sequence using native input.
            yield return Click();yield return Click();yield return Frames(hz*2);
            collectJump=false;record=false;
            Check(airStages.Contains(0),section+" normal jump starts the blade aerial attack");
            Check(jumpHits>0&&health.currentHealth<10000,section+" actual aerial weapon contact damages Health");
            Check(!attack.IsJumpSlashActive,section+" aerial recovery completes");Destroy(target);
            if(failure!=null||errors.Count>0){Finish();yield break;}
        }
        yield return ResetActor("Three-stage high-ledge aerial");
        var body=actor.GetComponent<CharacterController>();movement.RestoreSavedPose(actor.transform.position+Vector3.up*15,Quaternion.identity);
        airStages.Clear();collectJump=true;record=true;yield return Frames(3);yield return Click();yield return Click();yield return Click();yield return Frames(hz*3);collectJump=false;record=false;
        Check(airStages.Count==3,"All three blade airborne stages, including the finishing chop, play in sequence");
        if(failure!=null||errors.Count>0){Finish();yield break;}
        yield return ResetActor("Normal walking body contact");
        Bird(new Vector3(-.4f,0,3));Bird(new Vector3(.4f,0,3));
        keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};yield return Frames(hz*2);keys=Array.Empty<Key>();
        Check(actor.transform.position.z-ground.z<3,"Ordinary walking still collides with chickens");
        if(failure!=null||errors.Count>0){Finish();yield break;}
        yield return ResetActor("Rush through flock");
        for(int i=0;i<8;i++)Bird(new Vector3(i%2==0?-.4f:.4f,0,3+i*.8f));
        Physics.SyncTransforms();heavyHits=stoppedFrames=0;maxStopTravel=0;wasStopped=false;record=true;
        right=true;yield return Frames(Mathf.CeilToInt(hz*1.5f));right=false;yield return Frames(hz*3);record=false;
        Check(actor.transform.position.z-ground.z>12,"Blade charged rush travels through the entire flock");
        Check(heavyHits>0,"Rush retains actual confirmed damage against chickens");
        Check(stoppedFrames>0&&maxStopTravel<.01f,"Confirmed hits briefly stop rush translation");
        Check(birds.All(b=>!Physics.GetIgnoreCollision(body,b.GetComponent<CharacterController>())),"Flock collision pairs restore after separation");
        if(failure!=null||errors.Count>0){Finish();yield break;}
        yield return ResetActor("Wall behind flock");
        Bird(new Vector3(0,0,3));Bird(new Vector3(.3f,0,5));
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Real wall";wall.transform.SetParent(root.transform);wall.transform.position=ground+new Vector3(0,4,9);wall.transform.localScale=new Vector3(20,8,1);
        record=true;right=true;yield return Frames(Mathf.CeilToInt(hz*1.5f));right=false;yield return Frames(hz*3);record=false;
        Check(actor.transform.position.z-ground.z<8.6f,"Rush still stops at a real wall behind chickens");
        Destroy(wall);yield return Frames(3);
        yield return ResetActor("Boss body contact");
        var boss=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"),root.transform);
        boss.AddComponent<GameSaveExcluded>();yield return Frames(3);boss.GetComponent<NailongAI>().enabled=false;
        var bossMotor=boss.GetComponent<CharacterController>();var bossHealth=boss.GetComponent<NailongHealth>();bossHealth.maxHealth=bossHealth.currentHealth=10000;
        boss.transform.position=ground+new Vector3(0,.03f-(bossMotor.center.y-bossMotor.height*.5f)*boss.transform.lossyScale.y,9);Physics.SyncTransforms();
        record=true;right=true;yield return Frames(Mathf.CeilToInt(hz*1.5f));right=false;yield return Frames(hz*3);record=false;
        Check(actor.transform.position.z-ground.z<9,"Rush does not phase through a boss");
        Check(!Physics.GetIgnoreCollision(body,bossMotor),"Boss body collision is never leased away");
        Check(errors.Count==0,"No native runtime/animation errors");Finish();
    }
    void Bird(Vector3 point)
    {
        var bird=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab"),root.transform);bird.name="Native flock bird "+birds.Count;
        bird.GetComponent<ChickenBroAI>().enabled=false;bird.AddComponent<GameSaveExcluded>();var cc=bird.GetComponent<CharacterController>();
        point.y=.03f-(cc.center.y-cc.height*.5f)*bird.transform.lossyScale.y;bird.transform.position=ground+point;
        var health=bird.GetComponent<Health>();health.maxHealth=health.currentHealth=10000;birds.Add(bird);
    }
    void LateUpdate()
    {
        if(!armed||!actor)return;
        if(collectJump&&attack.IsJumpSlashActive)airStages.Add(attack.CurrentAirComboStage);
        bool stopped=CombatLocalHitStop.Active(actor);
        if(record&&section=="Rush through flock"&&stopped&&wasStopped){stoppedFrames++;maxStopTravel=Mathf.Max(maxStopTravel,Vector3.ProjectOnPlane(actor.transform.position-lastPosition,Vector3.up).magnitude);}
        lastPosition=actor.transform.position;wasStopped=stopped;
        if(!record||frame++%3!=0)return;
        Vector3 focus=actor.GetComponent<CharacterController>().bounds.center;camera.transform.SetPositionAndRotation(focus+new Vector3(11,6,-15),Quaternion.LookRotation(new Vector3(-11,-4,17)));
        camera.Render();var active=RenderTexture.active;RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();RenderTexture.active=active;
        File.WriteAllBytes(folder+"/frames/"+rows.Count.ToString("D5")+".jpg",pixels.EncodeToJPG(85));
        rows.Add(new{image=rows.Count,section,frame=Time.frameCount,phase=attack.HeavyAttackPhase,heavy=attack.IsHeavyAttackActive,air=attack.CurrentAirComboStage,stopped,position=new[]{actor.transform.position.x,actor.transform.position.y,actor.transform.position.z},heavyHits,jumpHits});
        File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{section,frame,images=rows.Count}));
    }
    void Finish()
    {
        File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=failure==null&&errors.Count==0,failure,errors,checks,heavyHits,stoppedFrames,maxStopTravel,hz,island,ground=new[]{ground.x,ground.y,ground.z}},Newtonsoft.Json.Formatting.Indented));
        File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));Cleanup();EditorApplication.isPaused=true;
    }
    void Cleanup()
    {
        if(!armed)return;armed=false;InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;CombatHitFeedback.HitConfirmed-=Hit;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);if(oldSettings)InputSystem.settings=oldSettings;if(settings)DestroyImmediate(settings);Time.captureDeltaTime=capture;
        if(camera)camera.targetTexture=null;if(texture){texture.Release();Destroy(texture);}if(pixels)Destroy(pixels);
    }
    void OnDisable()=>Cleanup();void OnDestroy()=>Cleanup();
}
