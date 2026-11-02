using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>Opt-in production-path replay; never runs in a normal game.</summary>
[DefaultExecutionOrder(31900)]
public sealed class OpeningTailReview : MonoBehaviour
{
    public string output;
    public bool recordAll;
    public bool includeHelm;
    public bool controllerInput;
    Gamepad controller,previousController;
    Story1SharkCollisionSequence sequence;
    Story1WreckBattle battle;
    Keyboard keyboard, previousKeyboard;
    Mouse mouse, previousMouse;
    Key[] keys=Array.Empty<Key>();
    bool left,right,done,sawTail,sawFlight,sawSplash,fitReviewed;
    int frame,gateFrames;
    float oldCapture;
    int replayFps=30,renderedFrames,images;
    RenderTexture target,previousTarget;
    Texture2D pixels;
    SkinnedMeshRenderer sharkSkin;
    int sharkHeadBone=-1;
    Vector3 sharkBindUp,sharkBindForward,sharkBindRight;
    Story1WreckBattle.Lesson previousLesson;
    bool previousWaiting;
    readonly List<object> samples=new List<object>();
    readonly List<string> errors=new List<string>();
    readonly List<object> fitSamples=new List<object>();
    readonly List<InputDevice> physicalInputs=new List<InputDevice>();
    bool started,cleaned,previousRunInBackground;
#if UNITY_EDITOR
    InputSettings previousSettings,temporarySettings;
    void EditorStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode||state==UnityEditor.PlayModeStateChange.EnteredEditMode)Cleanup();
    }
#endif
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    object Field(string name)=>typeof(Story1WreckBattle).GetField(name,Fields).GetValue(battle);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--tail-review");
#if UNITY_EDITOR
        // Editor review tools request a replay before entering Play Mode, so the crush is captured from its first frame.
        const string request="Temp/opening-review-request.txt";
        if(i<0&&File.Exists(request))
        {
            string requested=File.ReadAllText(request).Trim();File.Delete(request);
            var editorGo=new GameObject("Opt-in opening review");DontDestroyOnLoad(editorGo);
            var editorReview=editorGo.AddComponent<OpeningTailReview>();
            editorReview.output=requested;editorReview.recordAll=true;
            const string helmRequest="Temp/opening-review-helm.flag";
            editorReview.includeHelm=File.Exists(helmRequest);if(editorReview.includeHelm)File.Delete(helmRequest);
            const string padRequest="Temp/opening-review-pad.flag";
            editorReview.controllerInput=File.Exists(padRequest);if(editorReview.controllerInput)File.Delete(padRequest);
            return;
        }
#endif
        if(i<0)return;
        var go=new GameObject("Opt-in opening tail review");DontDestroyOnLoad(go);
        var review=go.AddComponent<OpeningTailReview>();
        review.output=i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"tail-review.json");
        review.recordAll=Array.IndexOf(args,"--tail-review-record")>=0;
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        Application.logMessageReceived+=Log;
        var fpsArg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--tail-review-fps="));
        if(fpsArg!=null)replayFps=Mathf.Clamp(int.Parse(fpsArg.Split('=')[1]),15,120);
        oldCapture=Time.captureDeltaTime;previousRunInBackground=Application.runInBackground;started=true;
        Time.captureDeltaTime=1f/replayFps;Application.runInBackground=true;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
        UnityEditor.EditorApplication.playModeStateChanged+=EditorStateChanged;
        UnityEditor.EditorApplication.quitting+=Cleanup;
        previousSettings=InputSystem.settings;temporarySettings=Instantiate(previousSettings);
        temporarySettings.hideFlags=HideFlags.DontSave;
        temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=temporarySettings;
#endif
        previousKeyboard=Keyboard.current;previousMouse=Mouse.current;previousController=Gamepad.current;
        // Isolate this process's replay from desktop typing and pointer clicks.
        // Input modules can bind all devices rather than Keyboard.current.
        foreach(var device in InputSystem.devices.ToArray())
            if((device is Keyboard||device is Mouse||device is Gamepad)&&device.enabled){physicalInputs.Add(device);InputSystem.DisableDevice(device);}
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;
        if(controllerInput)controller=InputSystem.AddDevice<Gamepad>();
        SceneManager.sceneLoaded+=ExcludeSave;
        if(SceneManager.GetActiveScene().name!="Story1")
        {
            GameSaveManager.CancelPendingContinue();yield return SceneManager.LoadSceneAsync("Story1");
        }
        ExcludeSave(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        yield return null;
        sequence=FindFirstObjectByType<Story1SharkCollisionSequence>();
        if(!sequence){Finish("Missing story sequence");yield break;}
        battle=sequence.GetComponent<Story1WreckBattle>();
        previousTarget=sequence.storyCamera.targetTexture;
        target=new RenderTexture(960,540,24);target.Create();
        if(!recordAll)sequence.storyCamera.targetTexture=target;
        pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        if(!includeHelm&&!battle.RedesignActive)sequence.BeginImpact();
    }
    void ExcludeSave(Scene scene,LoadSceneMode mode)
    {
        foreach(var p in FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
            if(!p.GetComponent<GameSaveExcluded>())p.gameObject.AddComponent<GameSaveExcluded>();
    }
    void Log(string text,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(text);}
    void Inject()
    {
        if(done||keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        foreach(var device in physicalInputs)if(device.added&&device.enabled)InputSystem.DisableDevice(device);
        keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        if(controllerInput&&controller!=null)
        {
            bool Has(GameInputSettings.Action action)=>keys.Contains(GameInputSettings.Get(action));
            var state=new GamepadState{leftStick=new Vector2((Has(GameInputSettings.Action.Right)?1:0)-(Has(GameInputSettings.Action.Left)?1:0),
                (Has(GameInputSettings.Action.Forward)?1:0)-(Has(GameInputSettings.Action.Back)?1:0)),rightTrigger=left?1:0,leftTrigger=right?1:0};
            if(Has(GameInputSettings.Action.Sprint))state=state.WithButton(GamepadButton.LeftStick);
            if(Has(GameInputSettings.Action.Jump))state=state.WithButton(GamepadButton.South);
            if(Has(GameInputSettings.Action.Dodge))state=state.WithButton(GamepadButton.East);
            if(battle&&battle.CurrentLesson==Story1WreckBattle.Lesson.Special&&keys.Contains(Key.E))state=state.WithButton(GamepadButton.RightShoulder);
            InputSystem.QueueStateEvent(controller,state);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());
        }
        else
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
        }
    }
    void LateUpdate()
    {
        if(done||!battle)return;
        frame++;keys=Array.Empty<Key>();left=right=false;
        if(previousLesson!=battle.CurrentLesson||previousWaiting!=battle.TutorialWaiting)gateFrames=0;else gateFrames++;
        previousLesson=battle.CurrentLesson;previousWaiting=battle.TutorialWaiting;
        if(battle.TutorialWaiting&&gateFrames>=12)
        {
            var lesson=battle.CurrentLesson;
            if(lesson==Story1WreckBattle.Lesson.Move||lesson==Story1WreckBattle.Lesson.Swim)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
            if(lesson==Story1WreckBattle.Lesson.Sprint)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Back),GameInputSettings.Get(GameInputSettings.Action.Sprint)};
            if(lesson==Story1WreckBattle.Lesson.Special&&(gateFrames/3)%2==0)keys=new[]{Key.E};
            if(lesson==Story1WreckBattle.Lesson.Attack)left=(gateFrames/3)%2==0;
            if(lesson==Story1WreckBattle.Lesson.Dodge&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};
            if(lesson==Story1WreckBattle.Lesson.ChargeHold)right=true;
            if(lesson==Story1WreckBattle.Lesson.Jump&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};
        }
        else if(!battle.TutorialWaiting&&(battle.CurrentLesson==Story1WreckBattle.Lesson.Move||battle.CurrentLesson==Story1WreckBattle.Lesson.Swim))keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
        else if(!battle.TutorialWaiting&&battle.CurrentLesson==Story1WreckBattle.Lesson.Sprint)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Back),GameInputSettings.Get(GameInputSettings.Action.Sprint)};
        float t=(float)Field("redesignTime");bool tail=(bool)Field("redTailHit");
        if(!fitReviewed&&battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&tail)
        {fitReviewed=true;ReviewContactFit();}
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>=20)
        {
            sawTail|=tail;sawFlight|=tail&&t-(float)Field("redTailHitTime")>.7f;
            if(frame%3==0)samples.Add(new{frame,t,tail,gap=battle.RedesignContactGap,bodyGap=battle.RedesignTutorialHeroGap,hero=sequence.sahur.transform.position.ToString("F4"),shark=sequence.swimmer.transform.position.ToString("F4")});
        }
        sawSplash|=battle.RedesignStage==Story1WreckBattle.OpeningStage.Drift;
        if(recordAll)RecordFrame();
        else if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>=21||sawSplash)RenderEvidence();
        if(frame%60==0)File.WriteAllText(output+".progress",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,stage=battle.RedesignStage.ToString(),lesson=battle.CurrentLesson.ToString(),t,tail,gap=battle.RedesignContactGap,scale=Time.timeScale,dt=Time.deltaTime,paused=PauseSettingsMenu.IsOpen,loadout=SahurLoadoutUI.BlocksInput,map=IslandMapUI.IsOpen,waiting=battle.TutorialWaiting,ownsPause=Field("ownsPause")}));
        if(sawSplash&&(t>(recordAll?7:3)||battle.RedesignStage==Story1WreckBattle.OpeningStage.Finished)){Finish(null);return;}
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>24&&!tail){Finish("Tail strike never registered");return;}
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&tail&&t-(float)Field("redTailHitTime")>8){Finish("Airborne exit never reached water");return;}
        if(frame>replayFps*(includeHelm?180:140))Finish("Full opening timed out");
    }
    void Finish(string error)
    {
        if(done)return;done=true;
        var result=new{passed=error==null&&sawTail&&sawFlight&&sawSplash&&errors.Count==0&&battle&&battle.RedesignContacts==3&&battle.RedesignTutorialConfirmedHits==2,error,frame,replayFps,renderedFrames,images,sawTail,sawFlight,sawSplash,includeHelm,helmSeconds=sequence?sequence.VoyageTime:0,scale=Time.timeScale,paused=PauseSettingsMenu.IsOpen,loadout=SahurLoadoutUI.BlocksInput,map=IslandMapUI.IsOpen,errors,health=sequence?sequence.sahur.GetComponent<PlayerHealth>().currentHealth:0,contacts=battle?battle.RedesignContacts:0,tutorialHits=battle?battle.RedesignTutorialConfirmedHits:0,events=battle?battle.RedesignEvents:null,fitSamples,samples};
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if(recordAll)File.WriteAllText(output+"-frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(recorded));
        if(!Application.isEditor)Application.Quit();else enabled=false;
        Cleanup();
#if UNITY_EDITOR
        // An unattended completed replay must not continue into First Island,
        // where a newly spawned campaign player can overwrite the user's save.
        if(recordAll)UnityEditor.EditorApplication.isPaused=true;
#endif
    }
    void ReviewContactFit()
    {
        // Same native pose and envelope, with sub-step variations in initial
        // root placement. Compare the former coarse solve to contact refinement.
        var root=sequence.swimmer;Vector3 saved=root.position,axis=(Vector3)Field("redRight");
        var solve=typeof(Story1WreckBattle).GetMethod("RedSeparateTutorialShark",Fields);
        var gap=typeof(Story1WreckBattle).GetMethod("RedTailSkinBodyGap",Fields);
        try
        {
            for(int i=0;i<12;i++)
            {
                Vector3 start=saved+axis*(.03f+i*.0125f);
                root.position=start;solve.Invoke(battle,new object[]{0f,(Vector3?)(-axis),false});
                var args=new object[]{Vector3.zero};float coarse=(float)gap.Invoke(battle,args);
                root.position=start;solve.Invoke(battle,new object[]{.003f,(Vector3?)(-axis),true});
                float precise=(float)gap.Invoke(battle,args);
                fitSamples.Add(new{offset=.03f+i*.0125f,coarse,precise,bodyGap=battle.RedesignTutorialHeroGap});
            }
        }
        finally{root.position=saved;}
    }
    void Cleanup()
    {
        if(!started||cleaned)return;cleaned=true;
        InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=ExcludeSave;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
        UnityEditor.EditorApplication.playModeStateChanged-=EditorStateChanged;
        UnityEditor.EditorApplication.quitting-=Cleanup;
        if(previousSettings)InputSystem.settings=previousSettings;previousSettings=null;
        if(temporarySettings)DestroyImmediate(temporarySettings);temporarySettings=null;
#endif
        if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}
        if(controller!=null){InputSystem.RemoveDevice(controller);controller=null;}
        foreach(var device in physicalInputs)if(device.added)InputSystem.EnableDevice(device);physicalInputs.Clear();
        if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();Time.captureDeltaTime=oldCapture;Application.runInBackground=previousRunInBackground;
        if(previousController!=null&&previousController.added)previousController.MakeCurrent();
        if(sequence&&sequence.storyCamera)sequence.storyCamera.targetTexture=previousTarget;
        if(target){target.Release();Destroy(target);target=null;}if(pixels){Destroy(pixels);pixels=null;}
    }
    void RenderEvidence()
    {
        sequence.storyCamera.Render();renderedFrames++;
        if(frame%Mathf.Max(1,replayFps/10)!=0)return;
        var active=RenderTexture.active;
        try
        {
            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);
            string folder=output+"-images";Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,(images++).ToString("D4")+".png"),pixels.EncodeToPNG());
        }
        finally{RenderTexture.active=active;}
    }
    readonly List<object> recorded=new List<object>();
    TralaleroSwimAnimator swim;
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static float[] Q(Quaternion q)=>new[]{q.x,q.y,q.z,q.w};
    void RecordFrame()
    {
        if(!swim)swim=FindFirstObjectByType<TralaleroSwimAnimator>();
        // The tutorial hands the view to the player camera; film beats use the story camera.
        Camera view=null;
        foreach(var c in Camera.allCameras)
            if(c.isActiveAndEnabled&&c.targetTexture==null&&(view==null||c.depth>view.depth))view=c;
        if(view==null)return;
        var active=RenderTexture.active;
        try
        {
            view.targetTexture=target;view.Render();
            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);
            string folder=output+"-images";Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,(images++).ToString("D5")+".jpg"),pixels.EncodeToJPG(88));
            renderedFrames++;
            var animator=(Animator)Field("redAnimator");
            var water=(OceanWorld)Field("ocean");
            Transform head=animator?animator.GetBoneTransform(HumanBodyBones.Head):null;
            Transform chest=animator?animator.GetBoneTransform(HumanBodyBones.Chest):null;
            Transform lf=animator?animator.GetBoneTransform(HumanBodyBones.LeftFoot):null;
            Transform rf=animator?animator.GetBoneTransform(HumanBodyBones.RightFoot):null;
            var hv=head?view.WorldToViewportPoint(head.position):Vector3.zero;
            var cv=chest?view.WorldToViewportPoint(chest.position):Vector3.zero;
            var sharkFacing=ReviewSharkFacing(view);
            recorded.Add(new{frame,time=Time.time,stage=battle.RedesignStage.ToString(),t=(float)Field("redesignTime"),shot=battle.RedesignShot,beat=battle.RedesignBeat,lesson=battle.CurrentLesson.ToString(),waiting=battle.TutorialWaiting,camera=view.name,fov=view.fieldOfView,aspect=view.aspect,
                hero=sequence.sahur.transform.position.ToString("F3"),shark=sequence.swimmer.transform.position.ToString("F3"),gap=battle.RedesignContactGap,
                nose=swim?swim.NoseWorldPoint.ToString("F2"):null,body=swim&&swim.VisualRenderer?swim.VisualRenderer.bounds.center.ToString("F2"):null,
                extent=swim&&swim.VisualRenderer?swim.VisualRenderer.bounds.extents.ToString("F2"):null,cam=view.transform.position.ToString("F2"),look=view.transform.forward.ToString("F3"),
                rot=sequence.swimmer.transform.eulerAngles.ToString("F2"),sharkFacing=V(sharkFacing),heroPush=battle.RedesignSolidHeroPush.ToString("F3"),woodPush=battle.RedesignSolidWoodPush.ToString("F3"),
                headView=V(hv),chestView=V(cv),headWater=head&&water?head.position.y-water.SampleSurfaceHeight(head.position,Time.time):999,
                chestWater=chest&&water?chest.position.y-water.SampleSurfaceHeight(chest.position,Time.time):999,
                lensWater=water?view.transform.position.y-water.SampleSurfaceHeight(view.transform.position,Time.time):999,
                headRotation=Q(head?head.localRotation:Quaternion.identity),chestRotation=Q(chest?chest.localRotation:Quaternion.identity),
                leftFoot=V(lf?lf.position:Vector3.zero),rightFoot=V(rf?rf.position:Vector3.zero),
                woodObstruction=battle.RedesignCameraWoodObstruction,lensInsideWood=battle.RedesignCameraLensInsideWood,sharkObstruction=battle.RedesignCameraSharkObstruction,
                heroGap=battle.RedesignTutorialHeroGap,supportLift=battle.RedesignSolidSupportLift,
                fractures=battle.RedesignSolidFractures,contacts=battle.RedesignContacts,rate=Time.timeScale,
                controllerInput,inputDevice=GameControls.UsingGamepad?GameControls.ControllerFamily:"KeyboardMouse",prompt=((UnityEngine.UI.Text)Field("controls"))?.text});
        }
        finally{view.targetTexture=null;RenderTexture.active=active;}
    }
    // World roll, projected facial slope, front-facing amount. Cache the bind
    // axes from the unanimated prefab, never from the current animated skin.
    Vector3 ReviewSharkFacing(Camera view)
    {
        if(!swim||!swim.UsesBlenderRig)return Vector3.zero;
        if(!sharkSkin)
        {
            sharkSkin=swim.VisualRenderer as SkinnedMeshRenderer;
            if(!sharkSkin)return Vector3.zero;
            sharkHeadBone=Array.FindIndex(sharkSkin.bones,b=>b.name=="Head");
            var prefab=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations").rigPrefab;
            var rest=prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            sharkBindUp=rest.transform.InverseTransformDirection(prefab.transform.up);
            sharkBindForward=rest.transform.InverseTransformDirection(prefab.transform.forward);
            sharkBindRight=rest.transform.InverseTransformDirection(prefab.transform.right);
        }
        if(sharkHeadBone<0)return Vector3.zero;
        var matrix=sharkSkin.bones[sharkHeadBone].localToWorldMatrix*sharkSkin.sharedMesh.bindposes[sharkHeadBone];
        Vector3 forward=matrix.MultiplyVector(sharkBindForward).normalized;
        Vector3 up=matrix.MultiplyVector(sharkBindUp).normalized,right=matrix.MultiplyVector(sharkBindRight).normalized;
        float roll=Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up,forward),Vector3.ProjectOnPlane(up,forward),forward);
        float slope=Mathf.Atan2(Vector3.Dot(right,view.transform.up),Vector3.Dot(right,view.transform.right))*Mathf.Rad2Deg;
        slope=Mathf.Repeat(slope+90,180)-90;
        return new Vector3(roll,slope,Vector3.Dot(forward,(view.transform.position-sharkSkin.bones[sharkHeadBone].position).normalized));
    }
    void OnDestroy(){Cleanup();}
    void OnDisable(){Cleanup();}
}
