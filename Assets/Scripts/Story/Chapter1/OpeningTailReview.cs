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
    Story1WreckBattle.Lesson previousLesson;
    bool previousWaiting;
    readonly List<object> samples=new List<object>();
    readonly List<string> errors=new List<string>();
    readonly List<object> fitSamples=new List<object>();
    readonly List<InputDevice> physicalInputs=new List<InputDevice>();
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    object Field(string name)=>typeof(Story1WreckBattle).GetField(name,Fields).GetValue(battle);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--tail-review");
        if(i<0)return;
        var go=new GameObject("Opt-in opening tail review");DontDestroyOnLoad(go);
        go.AddComponent<OpeningTailReview>().output=i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"tail-review.json");
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        Application.logMessageReceived+=Log;
        var fpsArg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--tail-review-fps="));
        if(fpsArg!=null)replayFps=Mathf.Clamp(int.Parse(fpsArg.Split('=')[1]),15,120);
        oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/replayFps;Application.runInBackground=true;
        previousKeyboard=Keyboard.current;previousMouse=Mouse.current;
        // Isolate this process's replay from desktop typing and pointer clicks.
        // Input modules can bind all devices rather than Keyboard.current.
        foreach(var device in InputSystem.devices.ToArray())
            if((device is Keyboard||device is Mouse)&&device.enabled){physicalInputs.Add(device);InputSystem.DisableDevice(device);}
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;
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
        target=new RenderTexture(960,540,24);target.Create();sequence.storyCamera.targetTexture=target;
        pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        if(!battle.RedesignActive)sequence.BeginImpact();
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
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
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
            if(lesson==Story1WreckBattle.Lesson.Attack)left=(gateFrames/3)%2==0;
            if(lesson==Story1WreckBattle.Lesson.Dodge&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};
            if(lesson==Story1WreckBattle.Lesson.ChargeHold)right=true;
            if(lesson==Story1WreckBattle.Lesson.Jump&&(gateFrames/3)%2==0)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};
        }
        else if(!battle.TutorialWaiting&&(battle.CurrentLesson==Story1WreckBattle.Lesson.Move||battle.CurrentLesson==Story1WreckBattle.Lesson.Swim))keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
        float t=(float)Field("redesignTime");bool tail=(bool)Field("redTailHit");
        if(!fitReviewed&&battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&tail)
        {fitReviewed=true;ReviewContactFit();}
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>=20)
        {
            sawTail|=tail;sawFlight|=tail&&t-(float)Field("redTailHitTime")>.7f;
            if(frame%3==0)samples.Add(new{frame,t,tail,gap=battle.RedesignContactGap,bodyGap=battle.RedesignTutorialHeroGap,hero=sequence.sahur.transform.position.ToString("F4"),shark=sequence.swimmer.transform.position.ToString("F4")});
        }
        sawSplash|=battle.RedesignStage==Story1WreckBattle.OpeningStage.Drift;
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>=21||sawSplash)RenderEvidence();
        if(frame%60==0)File.WriteAllText(output+".progress",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,stage=battle.RedesignStage.ToString(),lesson=battle.CurrentLesson.ToString(),t,tail,gap=battle.RedesignContactGap,scale=Time.timeScale,dt=Time.deltaTime,paused=PauseSettingsMenu.IsOpen,loadout=SahurLoadoutUI.BlocksInput,map=IslandMapUI.IsOpen,waiting=battle.TutorialWaiting,ownsPause=Field("ownsPause")}));
        if(sawSplash&&t>3){Finish(null);return;}
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&t>24){Finish("Tail strike never registered");return;}
        if(frame>replayFps*140)Finish("Full opening timed out");
    }
    void Finish(string error)
    {
        if(done)return;done=true;
        var result=new{passed=error==null&&sawTail&&sawFlight&&sawSplash&&errors.Count==0,error,frame,replayFps,renderedFrames,images,sawTail,sawFlight,sawSplash,scale=Time.timeScale,paused=PauseSettingsMenu.IsOpen,loadout=SahurLoadoutUI.BlocksInput,map=IslandMapUI.IsOpen,errors,health=sequence?sequence.sahur.GetComponent<PlayerHealth>().currentHealth:0,contacts=battle?battle.RedesignContacts:0,tutorialHits=battle?battle.RedesignTutorialConfirmedHits:0,events=battle?battle.RedesignEvents:null,fitSamples,samples};
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if(!Application.isEditor)Application.Quit();else enabled=false;
        Cleanup();
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
        InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=ExcludeSave;
        if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}
        foreach(var device in physicalInputs)if(device.added)InputSystem.EnableDevice(device);physicalInputs.Clear();
        if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();Time.captureDeltaTime=oldCapture;
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
    void OnDestroy(){Cleanup();}
}
