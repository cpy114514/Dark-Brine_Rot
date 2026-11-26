using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ReviewAbduction058
{
    public static object Start(string label,int hz=30,bool interrupt=false)
    {
        if(!Application.isPlaying)throw new Exception("Play first");
        string folder=".codex/claws058/"+label;if(Directory.Exists(folder))throw new Exception("Preserve review");Directory.CreateDirectory(folder+"/frames");
        var go=new GameObject("Abduction native review recorder");UnityEngine.Object.DontDestroyOnLoad(go);
        var run=go.AddComponent<AbductionReview058Recorder>();run.folder=folder;run.hz=hz;run.interrupt=interrupt;run.Begin();return new{started=folder};
    }
}
public sealed class AbductionReview058Recorder:MonoBehaviour
{
    public string folder;public int hz;public bool interrupt;
    readonly List<object> rows=new List<object>();readonly List<string> errors=new List<string>();
    readonly List<InputDevice> disabled=new List<InputDevice>();
    InputSettings oldSettings,settings;Gamepad pad;float capture;bool armed;int frame;
    BombardinoBoss boss;ThirdPersonPlayerController source;int previousLine=-2;bool nextButton;
    bool sawCarry,sawFall,sawEyes,sawGetUp,sawDialogue;float maxGrip;int heldFrames;
    bool pauseChecked,pauseFrozen=true;int pauseFrames;Vector3 pausedPosition;PauseSettingsMenu pause;
    Vector2 move;Vector3 movementOrigin;int movementFrames;float moved;
    Dictionary<EquipmentItem,int> inventory;SahurWeaponLoadout.Weapon weapon;bool transferChecked,inventoryPreserved,weaponPreserved,ammoPreserved;
    float deadline;int stable;string lastPhase="";
    public void Begin(){armed=true;StartCoroutine(Run());}
    void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    void Inject(){if(pad!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic)InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=move}.WithButton(GamepadButton.South,nextButton));}
    IEnumerator Run()
    {
        capture=Time.captureDeltaTime;Time.captureDeltaTime=1f/hz;deadline=Time.realtimeSinceStartup+180;
        oldSettings=InputSystem.settings;settings=Instantiate(oldSettings);settings.hideFlags=HideFlags.DontSave;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
        foreach(var d in InputSystem.devices.ToArray())if(d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)){disabled.Add(d);InputSystem.DisableDevice(d);}
        pad=InputSystem.AddDevice<Gamepad>();InputSystem.onBeforeUpdate+=Inject;Application.logMessageReceived+=OnLog;
        foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;
        var cameraObject=new GameObject("Native abduction camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";
        var camera=cameraObject.GetComponent<Camera>();camera.farClipPlane=3000;camera.fieldOfView=58;
        Vector3 ground=new Vector3(288,28,346);
        if(Physics.Raycast(ground+Vector3.up*100,Vector3.down,out var hit,250,~0,QueryTriggerInteraction.Ignore))ground=hit.point;
        source=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab")).GetComponent<ThirdPersonPlayerController>();
        SceneManager.MoveGameObjectToScene(source.gameObject,SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity"));
        source.gameObject.AddComponent<GameSaveExcluded>();var capsule=source.GetComponent<CharacterController>();
        float sole=(capsule.center.y-capsule.height*.5f)*Mathf.Abs(source.transform.lossyScale.y);
        source.RestoreSavedPose(ground+Vector3.up*(.08f-sole),Quaternion.identity);source.GetComponent<PlayerHealth>().currentHealth=73;
        boss=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"),ground+new Vector3(0,18,-22),Quaternion.identity).GetComponent<BombardinoBoss>();
        boss.target=source.transform;
        camera.transform.SetPositionAndRotation(ground+new Vector3(16,10,-24),Quaternion.LookRotation(new Vector3(-16,-4,24)));
        // Allow native Start/rig initialization before using the real death route.
        for(int i=0;i<8;i++)yield return null;
        typeof(BombardinoBoss).GetField("targetHealth",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(boss,source.GetComponent<PlayerHealth>());
        boss.target=source.transform;
        // Establish already-completed prerequisite fights in this disposable
        // session, then invoke the production damage/death path unchanged.
        var ids=(HashSet<string>[])typeof(FirstIslandBossProgression).GetField("identities",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        var changes=(Dictionary<string,int>)typeof(GameWorldProgress).GetField("changes",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        for(int i=0;i<2;i++){string key="abduction-review-prerequisite-"+i;ids[i].Add(key);changes[key]=0;}
        source.GetComponent<SahurWeaponLoadout>().Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades,true);
        source.GetComponent<SahurRifleCombat>().RestoreAmmo(11,64);
        for(int i=0;i<3;i++)yield return null;
        inventory=source.GetComponent<EquipmentInventory>().Items.ToDictionary(pair=>pair.Key,pair=>pair.Value);weapon=source.GetComponent<SahurWeaponLoadout>().CurrentWeapon;
        var health=boss.GetComponent<Health>();health.ApplyDamage(health.currentHealth+100,boss.transform.position);
        while(Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();
            var flight=FindFirstObjectByType<BombardinoAbduction>();var awakening=FindFirstObjectByType<SecondIslandAwakening>();
            var player=FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None).FirstOrDefault(p=>p.gameObject.activeInHierarchy);
            string phase=flight?flight.Phase:awakening?(awakening.Dialogue&&awakening.Dialogue.Active?"dialogue":awakening.GettingUp?"get-up":"eyes"):"gameplay";
            sawCarry|=phase=="climb";sawFall|=phase=="release";sawEyes|=phase=="eyes";sawGetUp|=phase=="get-up";sawDialogue|=phase=="dialogue";
            if(phase=="climb"){maxGrip=Mathf.Max(maxGrip,flight.GripError);heldFrames++;}
            if(interrupt&&phase=="climb"&&heldFrames>8)
            {
                flight.enabled=false;
                for(int i=0;i<6;i++)yield return null;
                bool released=source.enabled&&source.GetComponent<CharacterController>().enabled&&!source.ExternalControlLock&&!source.ExternalMovementLock&&!source.Motion.Owner&&!BombardinoAbduction.Playing;
                File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));
                File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=released&&errors.Count==0,interrupted=true,released,errors,scene=source.gameObject.scene.path},Newtonsoft.Json.Formatting.Indented));
                Cleanup();EditorApplication.isPaused=true;yield break;
            }
            if(phase=="island-pass"&&!transferChecked)
            {
                transferChecked=true;var target=player.GetComponent<EquipmentInventory>();
                inventoryPreserved=inventory.All(pair=>target.Count(pair.Key)==pair.Value)&&target.Items.Count==inventory.Count;
                weaponPreserved=player.GetComponent<SahurWeaponLoadout>().CurrentWeapon==weapon;
                var rifle=player.GetComponent<SahurRifleCombat>();ammoPreserved=rifle.Rounds==11&&rifle.ReserveRounds==64;
            }
            if(phase=="climb"&&!pauseChecked&&heldFrames>hz*.4f)
            {
                pause=FindFirstObjectByType<PauseSettingsMenu>();
                if(pause){pauseChecked=true;pausedPosition=player.transform.position;pause.Open();pauseFrames=1;}
            }
            else if(pauseFrames>0)
            {
                pauseFrozen&=Vector3.Distance(player.transform.position,pausedPosition)<.001f;
                if(++pauseFrames>=12){pause.Resume();pauseFrames=0;}
            }
            if(awakening&&awakening.Dialogue&&awakening.Dialogue.Active)
            {
                int line=awakening.Dialogue.CurrentLine;
                if(line!=previousLine){previousLine=line;stable=0;}
                stable++;nextButton=stable>hz*.6f&&stable%(hz/3+1)==0;
            }
            else nextButton=false;
            if(frame%2==0||phase!=lastPhase)
            {
                var image=ScreenCapture.CaptureScreenshotAsTexture();
                var rt=RenderTexture.GetTemporary(1280,720,0);Graphics.Blit(image,rt);var active=RenderTexture.active;RenderTexture.active=rt;
                var small=new Texture2D(1280,720,TextureFormat.RGB24,false);small.ReadPixels(new Rect(0,0,1280,720),0,0);small.Apply();RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);
                File.WriteAllBytes(folder+"/frames/"+rows.Count.ToString("D5")+".jpg",small.EncodeToJPG(86));Destroy(small);Destroy(image);
                rows.Add(new{image=rows.Count,frame,phase,scene=SceneManager.GetActiveScene().path,grip=flight?flight.GripError:0,player=player?new[]{player.transform.position.x,player.transform.position.y,player.transform.position.z}:null,
                    playerEnabled=player&&player.enabled,blocked=player&&player.ExternalControlLock,health=player?player.GetComponent<PlayerHealth>().currentHealth:0,gettingUp=awakening&&awakening.GettingUp,opening=awakening?awakening.Opening:-1,line=awakening&&awakening.Dialogue?awakening.Dialogue.CurrentLine:-1});
                File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,phase,images=rows.Count}));
            }
            lastPhase=phase;frame++;
            if(sawDialogue&&!awakening&&!BombardinoAbduction.Playing)
            {
                if(movementFrames==0){movementOrigin=player.transform.position;move=Vector2.up;Cursor.lockState=CursorLockMode.Locked;}
                if(++movementFrames>hz){move=Vector2.zero;moved=Vector3.ProjectOnPlane(player.transform.position-movementOrigin,Vector3.up).magnitude;Finish(player);yield break;}
            }
            if(errors.Count>0){Finish(player,"Native error");yield break;}
        }
        Finish(null,"Timeout");
    }
    void Finish(ThirdPersonPlayerController player,string failure=null)
    {
        bool unique=FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None).Count(p=>p.gameObject.activeInHierarchy)==1;
        var result=new{passed=failure==null&&errors.Count==0&&sawCarry&&sawFall&&sawEyes&&sawGetUp&&sawDialogue&&unique&&maxGrip<.06f&&pauseChecked&&pauseFrozen&&moved>.1f&&inventoryPreserved&&weaponPreserved&&ammoPreserved, failure,errors,sawCarry,sawFall,sawEyes,sawGetUp,sawDialogue,unique,maxGrip,heldFrames,frame,hz,pauseChecked,pauseFrozen,moved,inventoryPreserved,weaponPreserved,ammoPreserved,
            residentComponents=player&&player.GetComponent<SecondIslandObjectives>()&&player.GetComponent<SecondIslandStartProtection>(),health=player?player.GetComponent<PlayerHealth>().currentHealth:0,inputReleased=player&&player.enabled&&!player.ExternalControlLock&&!player.ExternalMovementLock};
        File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        Cleanup();EditorApplication.isPaused=true;
    }
    void Cleanup()
    {
        if(!armed)return;armed=false;InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=OnLog;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);foreach(var d in disabled)if(d.added)InputSystem.EnableDevice(d);
        if(oldSettings)InputSystem.settings=oldSettings;if(settings)DestroyImmediate(settings);Time.captureDeltaTime=capture;
    }
    void OnDisable()=>Cleanup();void OnDestroy()=>Cleanup();
}
