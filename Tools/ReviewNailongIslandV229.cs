using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Mavis;

public static class ReviewNailongIslandV229
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Use existing Editor Play Mode.");
        string folder=".codex/nailong-readability-20261020/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve previous evidence.");Directory.CreateDirectory(folder);
        var go=new GameObject("Temporary native island Nailong readability review");UnityEngine.Object.DontDestroyOnLoad(go);
        var run=go.AddComponent<NailongIslandV229>();run.folder=folder;EditorApplication.isPaused=false;
        return new{started=true,folder};
    }
}
[DefaultExecutionOrder(31900)]
public sealed class NailongIslandV229:MonoBehaviour
{
    public string folder;
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    Camera view;NailongAI boss;ThirdPersonPlayerController player;PlayerHealth health;ProceduralIsland island;
    Type patterns;MethodInfo tick,begin;
    RenderTexture target;Texture2D pixels;
    readonly List<object> rows=new List<object>();
    bool ready;int phase=-1,frame,oldCapture;float began,oldScale;
    Vector3 cameraPosition;Quaternion cameraRotation;
    async void Start()
    {
        oldCapture=Time.captureFramerate;oldScale=Time.timeScale;
        try
        {
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())root.SetActive(false);
            GameSaveManager.CancelPendingContinue();UnityEngine.Random.InitState(229);
            File.WriteAllText(folder+"/status.txt","Loading native island additively");
            var operation=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Main.unity",LoadSceneMode.Additive);
            while(!operation.isDone)await Task.Delay(30);
            for(int i=0;i<1500&&(AdditiveSceneBootstrap.IsLoading||!SceneManager.GetSceneByName("Enemies").isLoaded);i++)await Task.Delay(30);
            await Task.Delay(2500);
            player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(!player)throw new Exception("Native island player missing.");
            if(!player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();
            var arrival=player.GetComponent<FirstIslandArrival>();
            if(arrival)
            {
                arrival.StopAllCoroutines();arrival.RestoreProgress(true,true);
                typeof(FirstIslandArrival).GetMethod("ReleaseCinematic",Private).Invoke(arrival,null);arrival.enabled=false;
            }
            player.ExternalControlLock=true;player.enabled=false;
            health=player.GetComponent<PlayerHealth>();health.currentHealth=health.maxHealth;health.GrantProtection(0);
            boss=UnityEngine.Object.FindObjectsByType<NailongAI>(FindObjectsSortMode.None).OrderBy(n=>(n.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
            if(!boss)throw new Exception("Native island Nailong missing.");
            foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if(new[]{"NailongAI","CappuccinoAI","BombardinoBoss","EnemyAI"}.Contains(b.GetType().Name))b.enabled=false;
            var agent=boss.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
            island=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled);
            // Use the actual boss spawn and authored terrain/foliage, retaining native lighting and camera settings.
            Vector3 forward=Vector3.ProjectOnPlane(boss.transform.forward,Vector3.up).normalized;
            Vector3 destination=boss.transform.position+forward*9;
            destination.y=island.GetWorldSurfaceHeight(destination);
            var body=player.GetComponent<CharacterController>();body.enabled=false;
            player.transform.rotation=Quaternion.LookRotation(-forward);
            player.transform.position=destination-player.transform.TransformVector(body.center-Vector3.up*body.height*.5f)+Vector3.up*.03f;
            player.RestoreSavedPose(player.transform.position,player.transform.rotation);
            var animator=player.CharacterAnimator;animator.speed=1;animator.Play("Locomotion",0,0);animator.Update(0);
            body.enabled=true;Physics.SyncTransforms();
            typeof(NailongAI).GetField("target",Private).SetValue(boss,player.transform);
            var camera=Camera.main;if(!camera)throw new Exception("Native camera missing.");
            var copy=UnityEngine.Object.Instantiate(camera.gameObject);copy.name="Native island readability camera";
            foreach(var b in copy.GetComponents<MonoBehaviour>())if(!(b is UniversalAdditionalCameraData))b.enabled=false;
            foreach(var listener in copy.GetComponents<AudioListener>())listener.enabled=false;
            camera.enabled=false;camera.tag="Untagged";view=copy.GetComponent<Camera>();view.enabled=true;view.tag="MainCamera";
            Vector3 focus=player.transform.position+Vector3.up*player.cameraHeight;
            Vector3 aim=boss.GetComponentInChildren<SkinnedMeshRenderer>().bounds.center;
            Vector3 toward=aim-focus;focus=Vector3.Lerp(focus,aim,.25f);
            float distance=Mathf.Clamp(Mathf.Max(player.cameraDistance,toward.magnitude*.45f+2),player.cameraDistance,12);
            cameraRotation=Quaternion.LookRotation(toward);cameraPosition=focus-cameraRotation*Vector3.forward*distance;
            view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            target=new RenderTexture(1280,720,24);target.Create();view.targetTexture=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            File.WriteAllText(folder+"/status.txt","Waiting for native foliage cache");
            var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().FirstOrDefault(m=>m.gameObject.scene.IsValid()&&m.gameObject.scene.isLoaded);
            var routine=typeof(WorldPerformanceManager).GetField("cacheRoutine",Private);
            for(int i=0;i<2400&&manager&&routine.GetValue(manager)!=null;i++)await Task.Delay(30);
            Time.captureFramerate=30;Time.timeScale=1;
            patterns=typeof(NailongAI).GetNestedType("AttackPattern",BindingFlags.NonPublic);
            begin=typeof(NailongAI).GetMethod("BeginAttack",Private);tick=typeof(NailongAI).GetMethod("TickAttack",Private);
            began=Time.time;ready=true;
            File.WriteAllText(folder+"/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                boss=boss.name,bossPosition=new[]{boss.transform.position.x,boss.transform.position.y,boss.transform.position.z},
                playerPosition=new[]{player.transform.position.x,player.transform.position.y,player.transform.position.z},
                camera=view.name,view.fieldOfView,player.cameraDistance,actualDistance=distance,
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                colour=NailongCombatEffects.SceneColourAvailable,depth=NailongCombatEffects.SceneDepthAvailable},Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(folder+"/status.txt","Recording actual island attacks");
        }
        catch(Exception error){File.WriteAllText(folder+"/failure.txt",error.ToString());Finish();}
    }
    void Update()
    {
        if(!ready)return;float clock=Time.time-began;int next=clock<4?0:clock<11?1:2;
        if(next!=phase)
        {
            phase=next;health.currentHealth=health.maxHealth;
            typeof(NailongAI).GetField("state",Private).SetValue(boss,NailongAI.State.Idle);
            begin.Invoke(boss,new[]{Enum.Parse(patterns,phase==1?"GroundSlam":"ScoldingSpit")});
        }
        tick.Invoke(boss,null);
    }
    void LateUpdate()
    {
        if(!ready)return;view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
        float clock=Time.time-began;
        var shots=UnityEngine.Object.FindObjectsByType<NailongSpitProjectile>(FindObjectsSortMode.None);
        rows.Add(new{clock,phase,health.currentHealth,
            projectiles=shots.Select(s=>new{s.HasImpacted,s.CollisionRadius,pos=new[]{s.transform.position.x,s.transform.position.y,s.transform.position.z},screen=view.WorldToViewportPoint(s.transform.position).ToString(),diameterPixels=Vector3.Distance(view.WorldToScreenPoint(s.transform.position+view.transform.right*s.CollisionRadius),view.WorldToScreenPoint(s.transform.position-view.transform.right*s.CollisionRadius))}).ToArray()});
        if(frame>3&&frame%3==0)
        {
            var previous=RenderTexture.active;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/frame-"+(frame/3).ToString("D4")+".png",pixels.EncodeToPNG());}
            finally{RenderTexture.active=previous;}
        }
        frame++;
        if(clock<15)return;
        File.WriteAllText(folder+"/motion.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));Finish();
    }
    void Finish(){ready=false;Time.captureFramerate=oldCapture;Time.timeScale=oldScale;EditorApplication.isPaused=false;EditorApplication.isPlaying=false;}
    void OnDestroy(){Time.captureFramerate=oldCapture;Time.timeScale=oldScale;if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
}
