using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;

public static class ReviewExistingVfx
{
    static bool running,armed,recording,inLevel;static string folder,phase;static int imageCount;
    static string previousSavePath;
    static float rt,lt,oldCapture;static Gamepad pad;static InputSettings oldSettings,settings;
    static readonly List<InputDevice> disabled=new List<InputDevice>();
    static readonly List<object> frames=new List<object>(),checks=new List<object>();static readonly List<string> errors=new List<string>();
    static GameObject root,player,dummy;static Camera camera;static SahurAttack attack;static CombatImpactVisuals visuals;
    static ThirdPersonPlayerController motor;static Health target;static RenderTexture texture,controlTexture;static Texture2D pixels;
    static ReviewExistingVfxLate recorder;static Vector3 origin;static BrineVfxWorld world;static int bestScore,clipStart,lastContacts,contactStart=-1000;
    public static object Start(string label,bool level=false)
    {
        if(!Application.isPlaying||running)throw new Exception("Fresh sandbox Play required");
        folder=".codex/vfx-polish/"+label;if(Directory.Exists(folder))throw new Exception("Preserve previous review");
        Directory.CreateDirectory(folder+"/frames");inLevel=level;running=true;_ = Run();return new{started=folder};
    }
    static void Check(bool ok,string name){checks.Add(new{name,ok});if(!ok)throw new Exception(name);}
    static void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
    static void Lifecycle(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();}
    static void Inject()
    {
        if(running&&pad!=null&&pad.added&&InputState.currentUpdateType==InputUpdateType.Dynamic)
            InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=rt,leftTrigger=lt});
    }
    static async Task Frames(int count)
    {
        int end=Time.frameCount+count;while(running&&Time.frameCount<end)await Task.Delay(5);
        if(!running)throw new OperationCanceledException();
    }
    static async Task Clip(string name,int count)
    {
        phase=name;bestScore=-1;clipStart=Time.frameCount;recording=true;File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{phase,images=imageCount}));
        await Frames(count);recording=false;
    }
    static async Task PressAttack(string name)
    {rt=1;await Clip(name,3);rt=0;await Clip(name,75);}
    static async Task ResetPose()
    {
        rt=lt=0;await Frames(90);motor.RestoreSavedPose(origin,Quaternion.identity);
        var stamina=player.GetComponent<PlayerStamina>();stamina.currentStamina=stamina.maxStamina;
        Physics.SyncTransforms();await Frames(20);
    }
    static async Task Run()
    {
        string failure=null;float cpuP95=0;
        try
        {
            checks.Clear();frames.Clear();errors.Clear();imageCount=0;rt=lt=0;
            var savePath=typeof(GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            previousSavePath=(string)savePath.GetValue(null);savePath.SetValue(null,Path.GetFullPath(folder+"/fixture-save.json"));
            armed=true;EditorApplication.playModeStateChanged+=Lifecycle;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;EditorApplication.quitting+=Cleanup;
            Application.logMessageReceived+=Log;oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;
            oldSettings=InputSystem.settings;settings=oldSettings?UnityEngine.Object.Instantiate(oldSettings):ScriptableObject.CreateInstance<InputSettings>();settings.hideFlags=HideFlags.DontSave;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=settings;
            foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){disabled.Add(d);InputSystem.DisableDevice(d);}
            pad=InputSystem.AddDevice<Gamepad>();InputSystem.onBeforeUpdate+=Inject;
            // Defer startup until the ephemeral CLI call has returned, so native startup errors reach the log listener too.
            await Frames(1);
            if(inLevel){var deadline=DateTime.UtcNow.AddSeconds(150);while(AdditiveSceneBootstrap.IsLoading){if(DateTime.UtcNow>deadline)throw new Exception("Island startup timeout");await Frames(2);}}
            Spawn();await Frames(50);world=BrineVfxWorld.Get();visuals=player.GetComponent<CombatImpactVisuals>();
            // The player's cinematic camera may blend away during the isolated
            // rig. Render/cull against the review camera for both comparison takes.
            typeof(BrineVfxWorld).GetField("viewer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(world,camera);
            Check(world&&visuals,"Imported style and runtime pool available");
            Check(!ShaderUtil.ShaderHasError(world.Style.surface.shader)&&!ShaderUtil.ShaderHasError(world.Style.stone.shader)&&!ShaderUtil.ShaderHasError(world.Style.blast.shader)&&(!world.Style.smokeDetail||!ShaderUtil.ShaderHasError(world.Style.smokeDetail.shader)),"Native surface, solid, smoke and volume shaders compile");
            var body=player.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault();
            var args=new object[]{Vector3.zero,Vector3.zero,0f};typeof(SahurAttack).GetMethod("GetWeaponVolume",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(attack,args);
            File.WriteAllText(folder+"/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{playerHeight=body?body.bounds.size.y:0,weaponSpan=Vector3.Distance((Vector3)args[0],(Vector3)args[1]),
                weaponRadius=(float)args[2],origin=new[]{origin.x,origin.y,origin.z},style=Mavis.VisualStyles.VisualStyleSettings.SelectedId}));
            Capture("clean-scene.png");
            float before=target.currentHealth;
            await PressAttack("01-club-light");Check(target.currentHealth<before,"Real weapon contact accepted by Health");
            Check(visuals.Emissions>0,"Accepted contacts emit folded pressure surfaces");
            await ResetPose();dummy.AddComponent<CombatImpactResponse>().material=CombatMaterial.Wood;lt=1;await Clip("02-club-charge",120);Check(attack.IsCharging&&visuals.ChargeVisible,"Charge surface follows the real held weapon");
            before=target.currentHealth;lt=0;await Clip("03-club-heavy",100);Check(target.currentHealth<before,"Native charged release reaches target");
            await ResetPose();player.GetComponent<SahurWeaponLoadout>().Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);await Frames(40);
            dummy.GetComponent<CombatImpactResponse>().material=CombatMaterial.Metal;await PressAttack("04-twin-blade-light");
            await ResetPose();lt=1;await Clip("05-twin-blade-charge",100);lt=0;await Clip("06-twin-blade-heavy",110);
            await ResetPose();rt=lt=1;await Clip("07-twin-blade-spin",65);rt=lt=0;await Frames(70);
            Check(visuals.TrailVertexCount<=640,"Per-player loft vertex count is bounded");
            var depthData=camera.GetUniversalAdditionalCameraData();depthData.requiresDepthTexture=false;depthData.requiresColorOption=CameraOverrideOption.Off;
            var blast=new GameObject("Native Bombardino blast review");blast.transform.position=root.transform.position+new Vector3(2,0,5);
            int acceptedBeforeBlast=world.Accepted;
            blast.AddComponent<BombardinoBlastFX>().Initialize(4.2f);
            File.WriteAllText(folder+"/blast-state.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{acceptedBeforeBlast,acceptedAfter=world.Accepted,world.ActiveCount,main=Camera.main.name,mainPosition=Camera.main.transform.position.ToString(),blastPosition=blast.transform.position.ToString(),viewer=((Camera)typeof(BrineVfxWorld).GetField("viewer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(world))?.name,depth=depthData.requiresDepthTexture,color=depthData.requiresColorTexture}));
            await Frames(2);Check(depthData.requiresDepthTexture,"Volume requests occlusion depth only while needed");
            Check(depthData.requiresColorTexture,"Thermal refraction leases the opaque scene copy");await Clip("08-boss-explosion",95);Check(!depthData.requiresColorTexture,"Thermal refraction restores the opaque copy override");Check(!depthData.requiresDepthTexture,"Volume restores the camera depth override");
            Check(blast.GetComponentsInChildren<ParticleSystem>().Length==0,"Boss blast is volumetric mesh geometry, without particle stacking");
            await Frames(15);
            var capri=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),root.transform.position+new Vector3(-4,0,6),Quaternion.identity,root.transform);
            capri.AddComponent<GameSaveExcluded>();capri.GetComponent<CappuccinoAI>().enabled=false;
            // NaturalParticleEffects also checks Camera.main before routing a cue
            // to the shared pool. Keep this synchronous culling probe near the stage.
            Camera.main.transform.position=camera.transform.position;
            capri.GetComponent<CappuccinoBossPresentation>().Cue(CappuccinoBossPresentation.CueKind.Stagger);
            Check(world.ActiveCount>0,"Actual Capri stagger cue enters the shared 3D dust pool");
            await Clip("09-capri-ground-dust",60);UnityEngine.Object.Destroy(capri);
            await Frames(80);Check(world.ActiveCount==0&&world.ActiveGroundCount==0&&world.ActiveFragmentCount==0,"One-shot surfaces and ground chips retire completely");
            for(int f=0;f<7;f++){for(int i=0;i<10;i++)world.Emit(0,root.transform.position+new Vector3(i%4,1,i/4),Vector3.back,1.4f,1,true);await Frames(1);}
            Check(world.ActiveCount<=world.Style.contactCapacity&&world.ActiveGroundCount<=world.Style.groundCapacity&&world.Dropped>0,"Global contact, terrain and per-frame budgets reject excess work");
            var samples=new List<long>();
            using(var profiler=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"BrineVFX.BoundedWorld",256))
            {for(int i=0;i<90;i++){if(i%8==0)world.Emit(0,root.transform.position+Vector3.up,Vector3.back,2,1,true);await Frames(1);if(profiler.LastValue>0)samples.Add(profiler.LastValue);}}
            if(samples.Count>0){samples.Sort();cpuP95=samples[Mathf.Min(samples.Count-1,Mathf.FloorToInt(samples.Count*.95f))]/1000000f;}
            world.ClearAll();Check(world.ActiveCount==0&&world.ActiveGroundCount==0&&world.ActiveFragmentCount==0,"Explicit lifecycle cleanup clears every pooled surface");
            visuals.ClearTrail();Check(visuals.TrailVertexCount==0&&!visuals.ChargeVisible,"Weapon and charge cleanup leaves no stale geometry");
            Check(errors.Count==0,"No native runtime or shader errors");
        }
        catch(Exception e){failure=e.ToString();}
        finally
        {
            recording=false;File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(frames));
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=failure==null,error=failure,checks,errors,images=imageCount,worldScriptCpuP95Ms=cpuP95},Newtonsoft.Json.Formatting.Indented));
            Cleanup();EditorApplication.isPlaying=false;
        }
    }
    static void Spawn()
    {
        Vector3 placement=inLevel?LevelPlacement():new Vector3(2000,50,2000);
        var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        if(clock){clock.clockRuns=false;clock.startHour=12;var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;typeof(DayNightCycle).GetField("elapsedGameHours",flags).SetValue(clock,0f);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",flags).Invoke(clock,null);}
        root=new GameObject("Disposable native VFX review stage");root.transform.position=placement;
        var groundMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/Shared/Foliage/Materials/Ground_Close.mat");
        if(!inLevel)
        {
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform,false);ground.transform.localPosition=Vector3.down*.5f;ground.transform.localScale=new Vector3(100,1,100);
            var tiled=new Material(Shader.Find("Universal Render Pipeline/Lit"));tiled.SetColor("_BaseColor",new Color(.22f,.20f,.17f));tiled.SetFloat("_Smoothness",.12f);ground.GetComponent<Renderer>().sharedMaterial=tiled;
        }
        // Keep review framing separate from the camera that owns gameplay facing.
        var control=new GameObject("Native gameplay control camera",typeof(Camera),typeof(AudioListener));control.tag="MainCamera";
        control.transform.position=root.transform.position+new Vector3(0,6,-8);
        controlTexture=new RenderTexture(1280,720,24);controlTexture.Create();control.GetComponent<Camera>().targetTexture=controlTexture;
        control.GetComponent<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        var cam=new GameObject("VFX review camera",typeof(Camera));camera=cam.GetComponent<Camera>();camera.fieldOfView=60;
        camera.clearFlags=inLevel?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.17f,.20f);camera.farClipPlane=inLevel?3000:200;
        camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=inLevel;
        texture=new RenderTexture(1280,720,24){name="Native VFX review frames"};texture.Create();camera.targetTexture=texture;
        pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        if(!inLevel)
        {
            var sun=new GameObject("Review key light",typeof(Light));sun.transform.rotation=Quaternion.Euler(42,-35,0);
            var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.86f,.69f);light.intensity=1.4f;light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.37f,.43f);RenderSettings.fog=false;
        }
        player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);
        player.AddComponent<GameSaveExcluded>();motor=player.GetComponent<ThirdPersonPlayerController>();attack=player.GetComponent<SahurAttack>();
        var controller=player.GetComponent<CharacterController>();float sole=(controller.center.y-controller.height*.5f)*player.transform.lossyScale.y;
        origin=root.transform.position+Vector3.up*(.03f-sole);motor.RestoreSavedPose(origin,Quaternion.identity);
        dummy=GameObject.CreatePrimitive(PrimitiveType.Cylinder);dummy.name="Solid practice pillar";dummy.tag=attack.enemyTag;
        dummy.transform.SetParent(root.transform,false);dummy.transform.localPosition=new Vector3(0,3,4.2f);dummy.transform.localScale=new Vector3(2.2f,3,2.2f);
        dummy.GetComponent<Renderer>().sharedMaterial=groundMat;
        target=dummy.AddComponent<Health>();target.maxHealth=target.currentHealth=100000;dummy.AddComponent<GameSaveExcluded>();
        recorder=root.AddComponent<ReviewExistingVfxLate>();recorder.tick=RenderFrame;
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
    }
    static Vector3 LevelPlacement()
    {
        var existing=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        Vector3 anchor=existing?existing.transform.position:new Vector3(288,29,346);if(existing)existing.gameObject.SetActive(false);
        foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;
        foreach(var a in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))a.enabled=false;
        foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            string n=m.GetType().Name;
            if(n.EndsWith("AI")||n.Contains("Encounter")||n=="NailongAttack"||n=="BombardinoBoss"||n=="CappuccinoEnemy")m.enabled=false;
        }
        var offsets=new[]{0,-8,8,-16,16,-28,28,-48,48,-80,80};
        foreach(float x in offsets)foreach(float z in offsets)
        {
            var ray=new Vector3(anchor.x+x,anchor.y+100,anchor.z+z);
            if(!Physics.Raycast(ray,Vector3.down,out var h,250,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||h.normal.y<.86f||h.point.y<.5f)continue;
            string name=h.collider.name.ToLowerInvariant();if(name.Contains("tree")||name.Contains("trunk"))continue;
            if(Physics.CheckCapsule(h.point+Vector3.up*1.2f,h.point+Vector3.up*4.7f,.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
            return h.point;
        }
        throw new Exception("No clear walkable ground near the saved player; preserve this take and choose another review spot");
    }
    static void RenderFrame()
    {
        if(!running||!recording||Time.frameCount%2!=0)return;
        Capture("frames/"+(imageCount++).ToString("D4")+".jpg");
        int score=(visuals?visuals.TrailVertexCount:0)+(world?world.ActiveCount*120:0);
        int active=world?world.ActiveCount:0;if(active>lastContacts)contactStart=Time.frameCount;lastContacts=active;
        if(active>0)score=1000-Mathf.Abs(Time.frameCount-contactStart-2)*10+(visuals?visuals.TrailVertexCount/8:0);
        if(phase.Contains("charge"))score=attack?Mathf.RoundToInt(attack.ChargeElapsed*200):0;
        if(phase.Contains("explosion"))score=1000-Mathf.Abs(Time.frameCount-clipStart-18)*10;
        if(score>bestScore){bestScore=score;Capture(phase+".png");}
        frames.Add(new{image=imageCount-1,phase,frame=Time.frameCount,kind=attack?attack.IsHeavyAttackActive:false,
            trailVertices=visuals?visuals.TrailVertexCount:0,contacts=world?world.ActiveCount:0,ground=world?world.ActiveGroundCount:0,fragments=world?world.ActiveFragmentCount:0,charging=attack&&attack.IsCharging});
    }
    static void Capture(string name)
    {
        camera.transform.SetPositionAndRotation(root.transform.position+new Vector3(9,6,-7),Quaternion.LookRotation(new Vector3(-9,-2.7f,9)));
        var previous=RenderTexture.active;
        try{camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);
            File.WriteAllBytes(folder+"/"+name,name.EndsWith(".jpg")?pixels.EncodeToJPG(86):pixels.EncodeToPNG());}
        finally{RenderTexture.active=previous;}
    }
    static void Cleanup()
    {
        if(!armed)return;armed=false;running=false;recording=false;
        typeof(GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,previousSavePath);
        if(recorder)recorder.tick=null;
        EditorApplication.playModeStateChanged-=Lifecycle;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;EditorApplication.quitting-=Cleanup;
        InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;
        if(oldSettings)InputSystem.settings=oldSettings;else InputSystem.settings=ScriptableObject.CreateInstance<InputSettings>();
        if(settings&&InputSystem.settings!=settings)UnityEngine.Object.DestroyImmediate(settings);
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);disabled.Clear();
        Time.captureDeltaTime=oldCapture;if(camera)camera.targetTexture=null;
        if(texture){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);
        if(controlTexture){if(Camera.main)Camera.main.targetTexture=null;controlTexture.Release();UnityEngine.Object.DestroyImmediate(controlTexture);}
    }
}
[DefaultExecutionOrder(32050)]
public sealed class ReviewExistingVfxLate:MonoBehaviour{public Action tick;void LateUpdate()=>tick?.Invoke();}

