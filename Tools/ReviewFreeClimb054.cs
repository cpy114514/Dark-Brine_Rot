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

public static class ReviewFreeClimb054
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Play first");
        string folder=".codex/opening054/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve take");Directory.CreateDirectory(folder+"/frames");
        var go=new GameObject("Disposable free swimming climb regression");var run=go.AddComponent<FreeClimb054>();run.folder=folder;
        EditorApplication.isPaused=false;run.Begin();return new{folder};
    }
}
[DefaultExecutionOrder(-150)]
public sealed class FreeClimb054:MonoBehaviour
{
    public string folder;bool armed;Keyboard keyboard;Mouse mouse;Key[] keys=Array.Empty<Key>();
    InputSettings original,settings;readonly List<InputDevice> physical=new List<InputDevice>();float capture;
    GameObject root,actor;ThirdPersonPlayerController movement;Story1WreckRider rider;Story1WreckPlank board;OceanWorld ocean;
    Camera view;RenderTexture texture;Texture2D pixels;Vector3 origin,chestAxis;Animator animator;
    int frame,climbFrames;float minFacing=1;bool sawSwim,sawClimb,record;readonly List<object> rows=new List<object>(),checks=new List<object>();readonly List<string> errors=new List<string>();
    public void Begin(){armed=true;StartCoroutine(Guard());}
    IEnumerator Guard()
    {
        var run=Run();while(true)
        {
            object next;try{if(!run.MoveNext())yield break;next=run.Current;}
            catch(Exception e){errors.Add(e.ToString());Finish();yield break;}yield return next;
        }
    }
    IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
    void Check(bool ok,string name)=>checks.Add(new{ok,name});
    void Inject(){if(keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.QueueStateEvent(mouse,new MouseState());}}
    void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
    IEnumerator Run()
    {
        capture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;Time.timeScale=1;
        foreach(var b in FindObjectsByType<Story1WreckBattle>(FindObjectsSortMode.None))b.enabled=false;
        foreach(var s in FindObjectsByType<Story1SharkCollisionSequence>(FindObjectsSortMode.None))s.enabled=false;
        foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;
        original=InputSystem.settings;settings=Instantiate(original);settings.hideFlags=HideFlags.DontSave;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
        foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){physical.Add(d);InputSystem.DisableDevice(d);}
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inject;Application.logMessageReceived+=Log;
        root=new GameObject("Temporary climb fixture");ocean=FindFirstObjectByType<OceanWorld>();if(!ocean)throw new Exception("Existing ocean required");
        origin=new Vector3(1500,ocean.SampleSurfaceHeight(new Vector3(1500,0,1500),Time.time),1500);
        var deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.transform.SetParent(root.transform);DestroyImmediate(deck.GetComponent<BoxCollider>());
        board=deck.AddComponent<Story1WreckPlank>();board.Initialize(origin+Vector3.up*.4f,Quaternion.identity,new Vector3(18,.6f,18),ocean,Vector3.zero,Vector3.zero);
        deck.GetComponent<Rigidbody>().isKinematic=true;deck.GetComponent<Rigidbody>().interpolation=RigidbodyInterpolation.None;
        var guide=new GameObject("Climb control view",typeof(Camera),typeof(AudioListener));guide.transform.SetParent(root.transform);guide.tag="MainCamera";guide.transform.position=origin+new Vector3(0,5,-20);guide.transform.rotation=Quaternion.identity;
        var cameraGO=new GameObject("Climb review camera",typeof(Camera));cameraGO.transform.SetParent(root.transform);view=cameraGO.GetComponent<Camera>();view.enabled=false;view.fieldOfView=50;
        texture=new RenderTexture(960,540,24);texture.Create();view.targetTexture=texture;pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.AddComponent<GameSaveExcluded>();
        movement=actor.GetComponent<ThirdPersonPlayerController>();animator=movement.CharacterAnimator;rider=actor.GetComponent<Story1WreckRider>();if(!rider)rider=actor.AddComponent<Story1WreckRider>();rider.Initialize(movement,new[]{board},ocean);
        Cursor.lockState=CursorLockMode.Locked;
        actor.transform.position=origin+Vector3.up*10;yield return Frames(20);
        var chest=animator.GetBoneTransform(HumanBodyBones.Chest);chestAxis=chest.InverseTransformDirection(actor.transform.forward);
        float water=ocean.SampleSurfaceHeight(origin,Time.time);
        movement.RestoreSavedPose(new Vector3(origin.x,water-2-movement.LowestFootWorldOffset,origin.z-10),Quaternion.identity);
        Physics.SyncTransforms();yield return Frames(25);sawSwim=movement.Swimming;record=true;
        // Isolate the requested direction/recovery bug using the production
        // rider entry. Automatic grab proximity is a separate gameplay policy.
        typeof(Story1WreckRider).GetMethod("BeginClimb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rider,new object[]{board});
        keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
        for(int i=0;i<240;i++){if(sawClimb&&!rider.IsClimbing){keys=Array.Empty<Key>();break;}yield return null;}
        keys=Array.Empty<Key>();yield return Frames(20);record=false;
        Check(sawSwim,"Actual controller starts swimming");Check(sawClimb,"Production rider entry plays the native climb from swimming");
        Check(climbFrames>8&&minFacing>.3f,"Authored chest faces towards the deck throughout the measured pull-up");
        Check(!rider.IsClimbing&&movement.enabled&&!movement.ExternalMovementLock&&movement.CanUseGroundAttack,"Climb releases control back to ordinary grounded movement");
        Check(rider.Support()==board,"Character ends supported by the solid deck");Check(errors.Count==0,"No native runtime errors");Finish();
    }
    void Update()
    {
        if(!armed||!rider||!board)return;
        // A stable test platform follows the existing ocean heave. The real
        // rider owns all grab intent, reach checks, hand support and movement.
        var position=board.transform.position;position.y=ocean.SampleSurfaceHeight(position,Time.time)+.4f;board.transform.position=position;
        rider.BeforePlanks(Time.deltaTime);rider.AfterPlanks(Time.deltaTime);
    }
    void LateUpdate()
    {
        if(!armed||!record||!actor)return;
        if(rider.IsClimbing)
        {
            sawClimb=true;climbFrames++;var state=animator.GetCurrentAnimatorStateInfo(0);
            if(state.IsName("Wreck Climb")&&state.normalizedTime>.2f&&state.normalizedTime<.8f)
                minFacing=Mathf.Min(minFacing,Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.Chest).TransformDirection(chestAxis).normalized,Vector3.forward));
        }
        if(frame++%3!=0)return;
        Vector3 focus=actor.GetComponent<CharacterController>().bounds.center;Vector3 offset=new Vector3(10,4,-11);view.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset));view.Render();
        var previous=RenderTexture.active;RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();RenderTexture.active=previous;
        File.WriteAllBytes(folder+"/frames/"+rows.Count.ToString("D5")+".jpg",pixels.EncodeToJPG(88));rows.Add(new{frame=Time.frameCount,climbing=rider.IsClimbing,swimming=movement.Swimming,minFacing,position=new[]{actor.transform.position.x,actor.transform.position.y,actor.transform.position.z}});
    }
    void Finish()
    {
        File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{errors,checks,climbFrames,minFacing,sawSwim,sawClimb},Newtonsoft.Json.Formatting.Indented));File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));Cleanup();EditorApplication.isPaused=true;
    }
    void Cleanup()
    {
        if(!armed)return;armed=false;InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);if(original)InputSystem.settings=original;if(settings)Destroy(settings);Time.captureDeltaTime=capture;
        if(view)view.targetTexture=null;if(texture){texture.Release();Destroy(texture);}if(pixels)Destroy(pixels);
    }
    void OnDisable()=>Cleanup();void OnDestroy()=>Cleanup();
}
