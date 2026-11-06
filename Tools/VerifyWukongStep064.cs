using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyWukongStep064
{
    static string folder;static int fps;static bool terrain;
    static bool running,armed,mouseDown;
    static Keyboard keyboard,oldKeyboard;static Mouse mouse,oldMouse;static Gamepad pad;
    static InputSettings oldSettings,settings;static float oldCapture;
    static HideFlags oldSettingsFlags;
    static readonly List<InputDevice> physical=new List<InputDevice>();
    static readonly List<object> checks=new List<object>();static readonly List<string> errors=new List<string>();
    static Key[] keys=Array.Empty<Key>();static GamepadButton[] buttons=Array.Empty<GamepadButton>();static Vector2 stick;static float trigger;
    static UnityEngine.SceneManagement.Scene arena;
    static GameObject root,player,enemy;static ThirdPersonPlayerController motor;static SahurAttack attack;static PlayerHealth hp;
    static TimedThreat064 threat;static StepCapture064 recorder;
    static CursorLockMode oldCursor;static bool oldVisible;
    static Vector3 StandingPosition
    {
        get{var c=player.GetComponent<CharacterController>();return root.transform.position+Vector3.up*(.03f-(c.center.y-c.height*.5f)*player.transform.lossyScale.y);}
    }
    public static object Start(string label,int rate=60,bool island=false)
    {
        if(!Application.isPlaying)throw new Exception("Owned sandbox Play required");
        foreach(var name in new[]{"Gameplay","Enemies","Main"}){var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName(name);if(scene.isLoaded)foreach(var go in scene.GetRootGameObjects())go.SetActive(false);}
        folder=".codex/wukong-step064/"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous take");Directory.CreateDirectory(folder);
        fps=rate;terrain=island;running=true;_ = Run();return new{started=folder,fps,terrain};
    }
    static void Check(bool pass,string name)
    {
        checks.Add(new{name,pass,frame=Time.frameCount});File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{name,checks=checks.Count}));
        if(!pass)throw new Exception(name);
    }
    static async Task Frames(int count){int end=Time.frameCount+count;while(running&&Time.frameCount<end)await Task.Delay(5);if(!running)throw new OperationCanceledException();}
    static async Task Wait(float seconds)=>await Frames(Mathf.CeilToInt(seconds*fps));
    static void Inject()
    {
        if(!running||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,mouseDown));
        var state=new GamepadState{leftStick=stick,rightTrigger=trigger};foreach(var b in buttons)state=state.WithButton(b);InputSystem.QueueStateEvent(pad,state);
    }
    static void Neutral(){keys=Array.Empty<Key>();buttons=Array.Empty<GamepadButton>();stick=Vector2.zero;trigger=0;mouseDown=false;}
    static void Logs(string m,string t,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(m);}
    static void Lifecycle(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();}
    static async Task Reset()
    {
        Neutral();threat.due=Time.time+100;await Wait(1.05f);
        motor.RestoreSavedPose(StandingPosition,Quaternion.identity);motor.ClearPerfectCounter();
        hp.currentHealth=hp.maxHealth;player.GetComponent<PlayerStamina>().currentStamina=100;
        enemy.transform.SetPositionAndRotation(root.transform.position+Vector3.forward*2.3f+Vector3.up*2.4f,Quaternion.Euler(0,180,0));
        enemy.GetComponent<Health>().currentHealth=10000;threat.due=Time.time+100;threat.sequence++;threat.attempts=0;threat.damageEvents=0;
        Physics.SyncTransforms();Cursor.lockState=CursorLockMode.Locked;await Frames(3);
    }
    static async Task Dodge(bool gamepad=false)
    {
        if(gamepad){stick=Vector2.left;buttons=new[]{GamepadButton.East};}
        else keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left),GameInputSettings.Get(GameInputSettings.Action.Dodge)};
        await Frames(2);Neutral();
    }
    static async Task Run()
    {
        string failure=null;
        try
        {
            armed=true;oldKeyboard=Keyboard.current;oldMouse=Mouse.current;oldSettings=InputSystem.settings;oldCapture=Time.captureDeltaTime;
            oldCursor=Cursor.lockState;oldVisible=Cursor.visible;
            EditorApplication.playModeStateChanged+=Lifecycle;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;EditorApplication.quitting+=Cleanup;Application.logMessageReceived+=Logs;
            // InputManager destroys a replaced HideAndDontSave default. Retain
            // that exact object until cleanup, then restore its original flags.
            oldSettingsFlags=oldSettings.hideFlags;
            if(oldSettingsFlags==HideFlags.HideAndDontSave)oldSettings.hideFlags=HideFlags.DontSave;
            settings=UnityEngine.Object.Instantiate(oldSettings);settings.hideFlags=HideFlags.DontSave;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard||d is Mouse||d is Gamepad){physical.Add(d);InputSystem.DisableDevice(d);}
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();pad=InputSystem.AddDevice<Gamepad>();
            InputSystem.onBeforeUpdate+=Inject;Time.captureDeltaTime=1f/fps;
            Spawn();await Wait(.65f);Check(motor.CanUseGroundAttack,"Actual Sahur starts grounded with the installed controller");
            if(terrain)
            {
                foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward})
                {
                    await Reset();enemy.transform.position=root.transform.position-direction*2.3f+Vector3.up*2.4f;enemy.transform.rotation=Quaternion.LookRotation(direction);Physics.SyncTransforms();
                    threat.due=Time.time+.23f;recorder.section="real island "+direction;
                    keys=new[]{GameInputSettings.Get(direction.x<0?GameInputSettings.Action.Left:direction.x>0?GameInputSettings.Action.Right:direction.z<0?GameInputSettings.Action.Back:GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Dodge)};
                    await Frames(2);Neutral();Check(motor.IsPerfectStepping,"Real island input enters directional perfect step "+direction);await Wait(.55f);
                    Check(motor.CanUseGroundAttack,"Real island step returns to grounded player control "+direction);
                }
                Check(errors.Count==0,"No real island runtime errors");return;
            }
            await Reset();int perfect=motor.PerfectDodgesPerformed,roll=motor.DodgesPerformed;
            threat.due=Time.time+.65f;await Dodge();
            Check(motor.DodgesPerformed==roll+1&&!motor.IsPerfectStepping&&motor.PerfectDodgesPerformed==perfect,"Early dodge remains the original roll");
            await Reset();threat.due=Time.time+.20f;enemy.transform.rotation=Quaternion.identity;await Dodge();
            Check(!motor.IsPerfectStepping,"Enemy facing away cannot award perfect dodge");
            await Reset();threat.due=Time.time+.20f;enemy.transform.position+=Vector3.forward*15;await Dodge();
            Check(!motor.IsPerfectStepping,"Distant attack cannot award perfect dodge");
            await Reset();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=root.transform.position+new Vector3(0,1.2f,1.1f);wall.transform.localScale=new Vector3(4,2.4f,.2f);Physics.SyncTransforms();
            threat.due=Time.time+.20f;await Dodge();Check(!motor.IsPerfectStepping,"Cover blocks the imminent-attack forecast");UnityEngine.Object.DestroyImmediate(wall);
            await Reset();Vector3 start=player.transform.position;perfect=motor.PerfectDodgesPerformed;
            threat.due=Time.time+.25f;threat.sequence++;recorder.section="keyboard buffered counter";await Dodge();
            Check(motor.IsPerfectStepping&&motor.PerfectDodgesPerformed==perfect+1,"Widened .28s forecast creates a real perfect step");
            Check(motor.DodgeInvulnerable,"Perfect step protects from its startup");
            await Wait(.14f);mouseDown=true;await Frames(2);mouseDown=false;await Wait(.19f);
            Check(attack.PerfectCountersPerformed==1&&attack.IsPerfectCounterActive,"Last .10s fresh mouse press buffers exactly one returning sweep");
            Check(hp.currentHealth==hp.maxHealth&&threat.attempts==1,"Real hostile damage during the step is evaded");
            Check(Vector3.Distance(start,player.transform.position)>1.65f&&Vector3.Distance(start,player.transform.position)<2.2f,"Step moves about two metres through CharacterController");
            await Wait(.18f);
            Check(Vector3.Angle(player.transform.forward,Vector3.ProjectOnPlane(enemy.transform.position-player.transform.position,Vector3.up))<3,"Counter faces the actual attacking enemy before contact");
            await Wait(.5f);
            Check(enemy.GetComponent<Health>().currentHealth<10000,"Returning sweep deals real weapon-contact damage");
            Check(threat.damageEvents==1,"One counter does not multiply damage across contact frames");
            await Reset();int counters=attack.PerfectCountersPerformed;
            threat.due=Time.time+.23f;threat.sequence++;recorder.section="gamepad immediate counter";await Dodge(true);await Wait(.30f);
            trigger=1;await Frames(2);trigger=0;await Wait(.18f);
            Check(attack.PerfectCountersPerformed==counters+1,"Gamepad dodge then normal attack uses the same counter");await Wait(.6f);
            await Reset();counters=attack.PerfectCountersPerformed;
            threat.due=Time.time+.23f;threat.sequence++;await Dodge();await Wait(.8f);
            mouseDown=true;await Frames(2);mouseDown=false;
            Check(attack.PerfectCountersPerformed==counters,"Expired opportunity does not turn a later normal attack into a counter");
            await Reset();enemy.transform.rotation=Quaternion.Euler(0,180,0);threat.due=Time.time+.23f;threat.sequence++;await Dodge();await Wait(.3f);
            motor.ExternalControlLock=true;motor.RefreshPerfectCounter();motor.ExternalControlLock=false;
            Check(motor.PerfectDodgeAttacker==null,"External cinematic control clears the counter opportunity");
            await Reset();int spent=motor.PerfectDodgesPerformed;threat.due=Time.time+.22f;long sequence=threat.sequence;await Dodge();await Wait(.6f);
            threat.due=Time.time+.22f;threat.sequence=sequence;await Dodge();
            Check(motor.PerfectDodgesPerformed==spent+1,"The same hostile strike cannot award repeated perfect dodges");
            await Reset();
            var loadout=player.GetComponent<SahurWeaponLoadout>();loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);await Wait(.3f);
            Check(attack.UsesTwinBlades,"Actual loadout supplies twin-blade grips and hitboxes");
            counters=attack.PerfectCountersPerformed;threat.due=Time.time+.23f;threat.sequence++;recorder.section="twin blade counter";
            await Dodge();await Wait(.29f);mouseDown=true;await Frames(2);mouseDown=false;await Wait(.55f);
            Check(attack.PerfectCountersPerformed==counters+1,"Twin blades use their own returning sweep state");
            Check(enemy.GetComponent<Health>().currentHealth<10000&&threat.damageEvents==1,"Twin-blade sweep uses genuine contacts and one shared victim set");
            loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick);await Reset();
            enemy.SetActive(true);
            foreach(var pair in new[]{(Key.W,Vector2.up),(Key.S,Vector2.down),(Key.A,Vector2.left),(Key.D,Vector2.right)})
            {
                await Reset();recorder.section="direction "+pair.Item1;
                Vector3 away=new Vector3(pair.Item2.x,0,pair.Item2.y);
                enemy.transform.position=root.transform.position-away*2.3f+Vector3.up*2.4f;
                enemy.transform.rotation=Quaternion.LookRotation(away,Vector3.up);Physics.SyncTransforms();
                Vector3 begin=player.transform.position;Quaternion heading=player.transform.rotation;threat.due=Time.time+.23f;threat.sequence++;
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge),pair.Item1};await Frames(2);Neutral();
                Check(motor.IsPerfectStepping,"Directional perfect step entered: "+pair.Item1);
                float yawError=0;while(motor.IsPerfectStepping){yawError=Mathf.Max(yawError,Quaternion.Angle(heading,player.transform.rotation));await Frames(1);}
                Check(yawError<.1f,"Perfect step preserves combat facing: "+pair.Item1);
                Check(Vector3.Distance(begin,player.transform.position)>1.65f&&Vector3.Distance(begin,player.transform.position)<2.2f,"Directional step retains collision-owned distance: "+pair.Item1);
            }
            enemy.SetActive(false);
            var nativeBoss=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"),root.transform);
            nativeBoss.AddComponent<GameSaveExcluded>();var nailong=nativeBoss.GetComponent<NailongAI>();
            var agent=nativeBoss.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
            nativeBoss.transform.SetPositionAndRotation(root.transform.position+Vector3.forward*(nailong.EffectiveAttackReach-.25f),Quaternion.Euler(0,180,0));
            nativeBoss.transform.position+=Vector3.up*(root.transform.position.y-NailongSize.Feet(nativeBoss.transform).y);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            typeof(NailongAI).GetField("target",flags).SetValue(nailong,player.transform);
            typeof(NailongAI).GetField("engaged",flags).SetValue(nailong,true);
            typeof(NailongAI).GetField("nextActionTime",flags).SetValue(nailong,Time.time+100);
            await Wait(.35f);
            var pattern=typeof(NailongAI).GetNestedType("AttackPattern",BindingFlags.NonPublic);
            typeof(NailongAI).GetMethod("BeginAttack",flags).Invoke(nailong,new[]{Enum.Parse(pattern,"Slap")});
            await Wait(nailong.attackWindup-.25f);
            if(!((IPerfectDodgeThreatSource)nailong).ForecastDodge(hp,.28f,out _))
                File.WriteAllText(folder+"/boss-diagnostic.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{nailong.FightActive,state=typeof(NailongAI).GetField("state",flags).GetValue(nailong).ToString(),dead=nativeBoss.GetComponent<NailongHealth>().IsDead,feet=NailongSize.Feet(nativeBoss.transform).ToString(),playerFeet=player.GetComponent<CharacterController>().bounds.min.y,elapsed=Time.time-(float)typeof(NailongAI).GetField("attackStartedAt",flags).GetValue(nailong),nailong.attackWindup,reach=nailong.EffectiveAttackReach,range=Vector3.Distance(nativeBoss.transform.position,player.transform.position)},Newtonsoft.Json.Formatting.Indented));
            Check(((IPerfectDodgeThreatSource)nailong).ForecastDodge(hp,.28f,out _),"Native Nailong exposes the actual imminent slap timing and geometry");
            perfect=motor.PerfectDodgesPerformed;counters=attack.PerfectCountersPerformed;recorder.section="native Nailong counter";
            await Dodge();Check(motor.PerfectDodgesPerformed==perfect+1,"Actual boss attack triggers the same player-input perfect step");
            await Wait(.29f);mouseDown=true;await Frames(2);mouseDown=false;await Wait(.2f);
            Check(attack.PerfectCountersPerformed==counters+1&&motor.PerfectDodgeAttacker==null,"Native boss counter consumes its opportunity exactly once");
            await Wait(.45f);UnityEngine.Object.DestroyImmediate(nativeBoss);
            var shader=Resources.Load<Material>("Combat/PerfectDodge/Echo").shader;
            Check(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"Echo shader compiles without errors");
            Check(errors.Count==0,"No native runtime errors in step/counter input regression");
        }
        catch(Exception e){failure=e.ToString();}
        finally
        {
            var report=new{passed=failure==null,error=failure,fps,checks=checks.ToArray(),errors=errors.ToArray(),perfectDodges=motor?motor.PerfectDodgesPerformed:0,counters=attack?attack.PerfectCountersPerformed:0};
            Cleanup();File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        }
    }
    static void Spawn()
    {
        root=new GameObject("Owned perfect dodge064 fixture");arena=UnityEngine.SceneManagement.SceneManager.CreateScene("WukongStep064_"+Path.GetFileName(folder));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,arena);root.transform.position=new Vector3(2000,50,2000);
        if(terrain){if(!Physics.Raycast(new Vector3(288,150,346),Vector3.down,out var ground,200,~0,QueryTriggerInteraction.Ignore))throw new Exception("Actual island ground missing");root.transform.position=ground.point;}
        else{var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);}
        var cam=new GameObject("Perfect dodge review camera",typeof(Camera));cam.tag="MainCamera";cam.transform.SetParent(root.transform);cam.transform.position=root.transform.position+new Vector3(6,3,-7);cam.GetComponent<Camera>().fieldOfView=46;
        var sun=new GameObject("Fixture sunlight",typeof(Light));sun.transform.rotation=Quaternion.Euler(40,-30,0);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=1.6f;sun.transform.SetParent(root.transform);
        player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);
        player.AddComponent<GameSaveExcluded>();motor=player.GetComponent<ThirdPersonPlayerController>();attack=player.GetComponent<SahurAttack>();hp=player.GetComponent<PlayerHealth>();
        var loadout=player.GetComponent<SahurWeaponLoadout>();loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick);
        motor.RestoreSavedPose(StandingPosition,Quaternion.identity);
        enemy=GameObject.CreatePrimitive(PrimitiveType.Capsule);enemy.name="Actual damage / threat fixture";enemy.tag="Enemy";enemy.transform.localScale=new Vector3(2,2.4f,2);
        enemy.transform.position=root.transform.position+Vector3.forward*2.3f+Vector3.up*2.4f;enemy.transform.rotation=Quaternion.Euler(0,180,0);
        var health=enemy.AddComponent<Health>();health.maxHealth=health.currentHealth=10000;health.OnDamaged??=new UnityEngine.Events.UnityEvent<float>();health.OnDeath??=new UnityEngine.Events.UnityEvent();health.OnDamaged.AddListener(_=>threat.damageEvents++);
        threat=enemy.AddComponent<TimedThreat064>();threat.player=hp;threat.due=Time.time+100;
        recorder=root.AddComponent<StepCapture064>();recorder.folder=folder;recorder.actor=player.transform;recorder.motor=motor;recorder.attack=attack;recorder.camera=cam.GetComponent<Camera>();
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
    }
    static void Cleanup()
    {
        if(!armed)return;armed=false;running=false;Neutral();
        InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Logs;EditorApplication.playModeStateChanged-=Lifecycle;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;EditorApplication.quitting-=Cleanup;
        if(oldSettings){InputSystem.settings=oldSettings;oldSettings.hideFlags=oldSettingsFlags;}if(settings)UnityEngine.Object.DestroyImmediate(settings);
        foreach(var d in new InputDevice[]{pad,keyboard,mouse})if(d!=null&&d.added)InputSystem.RemoveDevice(d);
        foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);physical.Clear();
        if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
        Time.captureDeltaTime=oldCapture;Cursor.lockState=oldCursor;Cursor.visible=oldVisible;
        if(recorder){recorder.Save();if(recorder.camera)UnityEngine.Object.DestroyImmediate(recorder.camera.gameObject);}
        if(enemy)UnityEngine.Object.DestroyImmediate(enemy);if(root)UnityEngine.Object.DestroyImmediate(root);if(arena.IsValid()&&arena.isLoaded)UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(arena);arena=default;
    }
}
public sealed class TimedThreat064:MonoBehaviour,IPerfectDodgeThreatSource
{
    public PlayerHealth player;public float due;public long sequence;public int attempts,damageEvents;long attempted=-1;
    void OnEnable()=>PerfectDodgeThreats.Register(this);
    public bool ForecastDodge(PlayerHealth p,float horizon,out PerfectDodgeThreat t)
    {
        Vector3 feet=transform.position;feet.y=GetComponent<Collider>().bounds.min.y;
        return PerfectDodgeThreat.Sector(this,transform,p,due-Time.time,horizon,feet,transform.forward,3.4f,140,3,((long)GetInstanceID()<<32)^sequence,out t);
    }
    void Update(){if(Time.time>=due&&attempted!=sequence){attempted=sequence;attempts++;transform.position+=transform.forward*.25f;Physics.SyncTransforms();player.ApplyMeleeDamage(10,player.transform.position+Vector3.up,transform.position);}}
}
[DefaultExecutionOrder(32000)] public sealed class StepCapture064:MonoBehaviour
{
    public string folder,section="rejections";public Transform actor;public ThirdPersonPlayerController motor;public SahurAttack attack;public new Camera camera;
    readonly List<object> rows=new List<object>();RenderTexture target;Texture2D pixels;int frame;
    void LateUpdate()
    {
        if(!actor||!camera)return;
        camera.transform.position=actor.position+new Vector3(6,3,-7);camera.transform.LookAt(actor.position+Vector3.up*1.1f);
        var weapon=attack.stickHitbox;object[] volume={Vector3.zero,Vector3.zero,0f};
        if(weapon)typeof(SahurAttack).GetMethod("GetWeaponVolume",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(attack,volume);
        var a=(Vector3)volume[0];var b=(Vector3)volume[1];float radius=(float)volume[2];
        var victim=GameObject.Find("Actual damage / threat fixture")?.GetComponent<Collider>();float gap=100;
        if(victim)for(int i=0;i<=32;i++){var p=Vector3.Lerp(a,b,i/32f);gap=Mathf.Min(gap,Vector3.Distance(p,victim.ClosestPoint(p))-radius);}
        var animator=motor.CharacterAnimator;float sole=0;
        if(motor.IsPerfectStepping){
            Vector3 l=Vector3.zero,r=Vector3.zero;SahurSupportPose.MeasureSolesFast(animator,ref l,ref r);
            sole=Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(l).y,animator.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(r).y);}
        float hips=animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
        int echoes=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.name.StartsWith("Perfect dodge body echo"));
        rows.Add(new{bodyYaw=animator.bodyRotation.eulerAngles.y,hipsY=hips,soleY=sole,echoes,frame,time=Time.time,section,step=motor.IsPerfectStepping,counter=attack.IsPerfectCounterActive,phase=attack.CurrentAttackPhase,weaponEnabled=weapon&&weapon.enabled,a=a.ToString("F3"),b=b.ToString("F3"),gap,position=actor.position.ToString("F3"),rotation=actor.eulerAngles.ToString("F2")});
        if(section=="rejections"){frame++;return;}
        if(!target){target=new RenderTexture(960,540,24);pixels=new Texture2D(960,540,TextureFormat.RGB24,false);Directory.CreateDirectory(folder+"/frames");}
        var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();File.WriteAllBytes(folder+"/frames/"+frame.ToString("D5")+".jpg",pixels.EncodeToJPG(90));}
        finally{camera.targetTexture=old;RenderTexture.active=active;}frame++;
    }
    public void Save(){File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));}
    void OnDestroy(){if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
}



