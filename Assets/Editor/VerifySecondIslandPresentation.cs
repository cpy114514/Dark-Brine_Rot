using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;

[InitializeOnLoad]
public static class VerifySecondIslandPresentation
{
    const string Folder=".codex/island2-upgrade/playtest/";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<object> checks=new List<object>();
    static Keyboard keyboard,originalKeyboard;
    static Key[] pressed=Array.Empty<Key>();
    static void Inject(){if(keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();InputState.Change(keyboard,new KeyboardState(pressed));}}
    static VerifyPresentationCamera captureCamera;
    static VerifySecondIslandPresentation(){EditorApplication.playModeStateChanged+=Changed;}
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Require stopped Editor");
        Directory.CreateDirectory(Folder);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Keep unsaved scenes");
        Write("scene-setup.json",setup);
        var saves=Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p)));
        Write("saves-before.json",saves);
        SessionState.SetBool("SecondIslandPresentation.Pending",true);
        EditorApplication.EnterPlaymode();return "Play verification queued";
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("SecondIslandPresentation.Pending",false))
        {
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Path.GetFullPath(Folder+"isolated-save.json"));
            GameSaveManager.BeginEditorTestSession();EditorApplication.delayCall+=Run;
        }
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SecondIslandPresentation.Pending",false))
        {
            SessionState.SetBool("SecondIslandPresentation.Pending",false);
            var saves=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Folder+"saves-before.json"));
            bool same=saves.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value);
            Write("preservation.json",new{formalSavesUnchanged=same,originalSceneSetupRestored=Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup())==Newtonsoft.Json.JsonConvert.SerializeObject(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Folder+"scene-setup.json")))});
        }
    }
    static async Task Wait(float seconds)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<end){if(!EditorApplication.isPlaying)throw new Exception("Play stopped");await Task.Delay(25);}
    }
    static async Task Until(Func<bool> condition,float seconds,string label)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(!condition()){if(!EditorApplication.isPlaying||Time.realtimeSinceStartup>end)throw new Exception("Timed out: "+label);await Task.Delay(25);}
    }
    static object Field(object obj,string key)=>obj.GetType().GetField(key,Private).GetValue(obj);
    static void Set(object obj,string key,object value)=>obj.GetType().GetField(key,Private).SetValue(obj,value);
    static void Write(string name,object value)=>File.WriteAllText(Folder+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static async void Run()
    {
        checks.Clear();bool passed=false;string error=null;
        try
        {
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/Second Island/Second Island.unity",LoadSceneMode.Single);
            await Until(()=>load.isDone,35,"Second Island load");await Wait(1);
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.Destroy(opening.gameObject);
            await Wait(.6f);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(!player)throw new Exception("No player");
            var protect=player.GetComponent<SecondIslandStartProtection>();if(protect)typeof(SecondIslandStartProtection).GetProperty("WaitingForMovement").SetValue(protect,false);
            var menu=player.GetComponent<SahurLoadoutUI>();
            originalKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            InputSystem.onBeforeUpdate+=Inject;pressed=new[]{Key.I};await Wait(.2f);
            pressed=Array.Empty<Key>();await Wait(.15f);
            if(!SahurLoadoutUI.IsOpen||!menu.PanelVisible||Time.timeScale!=0)throw new Exception("I did not open/pause equipment menu");
            var root=(GameObject)Field(menu,"root");
            if(root.GetComponentsInChildren<EquipmentJournalInk>(true).Length!=0)throw new Exception("Journal drawing still present");
            var panel=root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name=="Equipment panel");
            if(!panel||panel.GetComponent<UnityEngine.UI.Image>().sprite)throw new Exception("Book image still present");
            for(int i=0;i<6;i++)menu.SelectEquipment((SahurLoadoutUI.EquipmentSlot)i);
            menu.SelectEquipment(SahurLoadoutUI.EquipmentSlot.Weapon);
            await Wait(.15f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"equipment.png"));await Wait(.25f);
            pressed=new[]{Key.Escape};await Wait(.15f);pressed=Array.Empty<Key>();await Wait(.15f);
            if(SahurLoadoutUI.IsOpen||Time.timeScale<=0)throw new Exception("Esc did not restore controls");
            checks.Add(new{name="Black-white I menu",openedByKeyboard=true,paused=true,categories=6,noBookImage=true,closedByEscape=true});
            InputSystem.onBeforeUpdate-=Inject;InputSystem.RemoveDevice(keyboard);keyboard=null;if(originalKeyboard!=null&&originalKeyboard.added)originalKeyboard.MakeCurrent();
            if(SecondIslandProp.Load("Quaternius/Bench").name!="painted_wooden_bench"||SecondIslandProp.Load("Quaternius/Crate").name!="wooden_crate_01")throw new Exception("Realistic prop mapping missing");
            var natural=GameObject.Find("Second Island - Natural details");
            if(!natural||natural.transform.childCount<8)throw new Exception("Natural scenery missing");
            captureCamera=new GameObject("Scenery review camera override").AddComponent<VerifyPresentationCamera>();captureCamera.target=natural.transform.GetChild(0);
            await Wait(.25f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"island-scenery.png"));await Wait(.25f);
            UnityEngine.Object.Destroy(captureCamera.gameObject);captureCamera=null;
            checks.Add(new{name="Photogrammetry prop replacements",passed=true,sceneryPlacements=natural.transform.childCount});
            VerifyZhuBaJieBoss.Run();await Until(()=>VerifyZhuBaJieBoss.Status!="running",65,"Zhu Bajie combat regression");
            if(VerifyZhuBaJieBoss.Status!="passed")throw new Exception(VerifyZhuBaJieBoss.Result);
            checks.Add(new{name="Zhu Bajie two phases",result=VerifyZhuBaJieBoss.Result});
            player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.Destroy(opening.gameObject);await Wait(.3f);
            var boss=UnityEngine.Object.FindFirstObjectByType<HuangMeiBoss>();if(!boss)throw new Exception("Huang Mei did not spawn");
            var hp=player.GetComponent<PlayerHealth>();hp.maxHealth=hp.currentHealth=10000;
            captureCamera=new GameObject("Presentation review camera override").AddComponent<VerifyPresentationCamera>();captureCamera.target=boss.transform;
            foreach(HuangMeiBoss.Attack attack in Enum.GetValues(typeof(HuangMeiBoss.Attack)))
            {
                var dir=boss.transform.forward;
                var surface=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();var bounds=surface.bounds;
                var site=boss.transform.position+dir*(attack==HuangMeiBoss.Attack.StaffSweep?8:11);
                if(!surface.Raycast(new Ray(new Vector3(site.x,bounds.max.y+10,site.z),Vector3.down),out var ground,bounds.size.y+20))throw new Exception("No attack test footing");
                var body=player.GetComponent<CharacterController>();float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
                player.RestoreSavedPose(ground.point+Vector3.up*(.1f-sole),Quaternion.LookRotation(-dir));
                player.ExternalControlLock=false;player.ExternalMovementLock=false;
                Set(hp,"protectedUntil",0f);Set(hp,"recoveryUntil",0f);
                typeof(HuangMeiBoss).GetMethod("Engage",Private).Invoke(boss,null);
                captureCamera.recoveryObserved=false;
                if(attack==HuangMeiBoss.Attack.GoldenMandala)
                {
                    boss.Health.ApplyDamage(boss.Health.currentHealth-boss.Health.maxHealth*.49f,boss.transform.position);
                    await Until(()=>boss.SecondPhase&&boss.CurrentState==HuangMeiBoss.State.Invocation,2,"Huang half-health invocation");
                    await Wait(.25f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"huang-invocation.png"));
                }
                else typeof(HuangMeiBoss).GetMethod("BeginAttack",Private).Invoke(boss,new object[]{attack});
                await Until(()=>boss.CurrentState==HuangMeiBoss.State.Strike,4,"Huang windup "+attack);
                await Wait(attack==HuangMeiBoss.Attack.GoldenMandala?1.05f:.5f);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"huang-"+attack+".png"));
                await Until(()=>captureCamera.recoveryObserved,6,"Huang recovery "+attack+" ("+boss.CurrentState+")");
                if(!boss.GetComponent<HuangMeiRig>().skin.GetComponentInParent<Animation>())throw new Exception("Authored clip player inactive");
                checks.Add(new{name="Huang "+attack,recovery=true,hits=boss.HitsLanded});
            }
            boss.Health.Kill();await Wait(.2f);
            if(boss.CurrentState!=HuangMeiBoss.State.Dead||boss.GetComponent<CharacterController>().enabled)throw new Exception("Huang defeat did not stop movement");
            if(boss.GetComponentsInChildren<ParticleSystem>(true).Any(p=>p.particleCount>0))throw new Exception("Huang effects persisted after defeat");
            checks.Add(new{name="Huang half-health invocation and death",secondPhase=boss.SecondPhase,dead=boss.Health.IsDead});
            passed=true;
        }
        catch(Exception e){error=e.ToString();}
        finally
        {
            InputSystem.onBeforeUpdate-=Inject;pressed=Array.Empty<Key>();if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(originalKeyboard!=null&&originalKeyboard.added)originalKeyboard.MakeCurrent();
            if(captureCamera)UnityEngine.Object.Destroy(captureCamera.gameObject);
            Write("result.json",new{passed,error,checks});SessionState.SetBool("SecondIslandPresentation.Finished",true);EditorApplication.ExitPlaymode();
        }
    }
}
[DefaultExecutionOrder(50000)]
public sealed class VerifyPresentationCamera : MonoBehaviour
{
    public Transform target;
    public bool recoveryObserved;
    void LateUpdate()
    {
        if(target&&target.TryGetComponent<HuangMeiBoss>(out var boss)&&boss.CurrentState==HuangMeiBoss.State.Recover)recoveryObserved=true;
        var camera=Camera.main;if(!target||!camera)return;
        Vector3 center=target.position+Vector3.up*4;
        camera.transform.position=center+target.forward*18+target.right*12+Vector3.up*5;
        camera.transform.LookAt(center);camera.fieldOfView=48;
    }
}
