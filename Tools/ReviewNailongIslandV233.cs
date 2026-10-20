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

public static class ReviewNailongIslandV233
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Use existing Editor Play Mode.");
        string folder=".codex/nailong-balance-20261020/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve previous evidence.");Directory.CreateDirectory(folder);
        var go=new GameObject("Temporary native island Nailong readability review");UnityEngine.Object.DontDestroyOnLoad(go);
        var run=go.AddComponent<NailongIslandV233>();run.folder=folder;EditorApplication.isPaused=false;
        return new{started=true,folder};
    }
}
[DefaultExecutionOrder(31900)]
public sealed class NailongIslandV233:MonoBehaviour
{
    public string folder;
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    Camera view;NailongAI boss;ThirdPersonPlayerController player;PlayerHealth health;ProceduralIsland island;
    Type patterns;MethodInfo tick,begin;
    RenderTexture target;Texture2D pixels;
    readonly List<object> rows=new List<object>();
    float cameraFov;bool reviewingAngles;bool ready;int phase=-1,frame,oldCapture;float began,oldScale;
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
            var mouthSocket=(Vector3)typeof(NailongAttackMotion).GetField("mouthSocket",Private).GetValue(boss.GetComponent<NailongAttackMotion>());
            if(Vector3.Distance(mouthSocket,new Vector3(-.058f,.07f,.35f))>.000001f)throw new Exception("Native island retained a stale mouth offset: "+mouthSocket);
            if(boss.GetComponent<NailongHealth>().maxHealth!=500||boss.GetComponent<NailongAttack>().damage!=6||boss.sonarDamage!=4)throw new Exception("Native boss retained stale balance overrides.");
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
            cameraFov=view.fieldOfView;cameraRotation=Quaternion.LookRotation(toward);cameraPosition=focus-cameraRotation*Vector3.forward*distance;
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
                maxHealth=boss.GetComponent<NailongHealth>().maxHealth,baseDamage=boss.GetComponent<NailongAttack>().damage,sonarDamage=boss.sonarDamage,flightTimeScale=(float)typeof(NailongCombatEffects).GetField("spitFlightTimeScale",Private).GetValue(boss.GetComponent<NailongCombatEffects>()),mouthSocket=new[]{mouthSocket.x,mouthSocket.y,mouthSocket.z},boss=boss.name,bossPosition=new[]{boss.transform.position.x,boss.transform.position.y,boss.transform.position.z},
                playerPosition=new[]{player.transform.position.x,player.transform.position.y,player.transform.position.z},
                camera=view.name,view.fieldOfView,player.cameraDistance,actualDistance=distance,
                playerBodyHeight=body.bounds.size.y,
                quality=QualitySettings.names[QualitySettings.GetQualityLevel()],
                colour=NailongCombatEffects.SceneColourAvailable,depth=NailongCombatEffects.SceneDepthAvailable},Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(folder+"/status.txt","Recording actual island attacks");
        }
        catch(Exception error){File.WriteAllText(folder+"/failure.txt",error.ToString());Finish();}
    }
    void Update()
    {
        if(!ready||reviewingAngles)return;float clock=Time.time-began;int next=clock<4?0:clock<11?1:clock<14?2:3;
        if(next!=phase)
        {
            phase=next;health.currentHealth=health.maxHealth;
            typeof(NailongAI).GetField("state",Private).SetValue(boss,NailongAI.State.Idle);
            begin.Invoke(boss,new[]{Enum.Parse(patterns,phase==1?"GroundSlam":phase==2?"Roar":"ScoldingSpit")});
        }
        tick.Invoke(boss,null);
    }
    void LateUpdate()
    {
        if(!ready)return;view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);view.fieldOfView=cameraFov;
        if(reviewingAngles)return;
        float clock=Time.time-began;
        var shots=UnityEngine.Object.FindObjectsByType<NailongSpitProjectile>(FindObjectsSortMode.None);
        rows.Add(new{clock,phase,health.currentHealth,
            mouth=new[]{boss.GetComponent<NailongAttackMotion>().MouthPosition.x,boss.GetComponent<NailongAttackMotion>().MouthPosition.y,boss.GetComponent<NailongAttackMotion>().MouthPosition.z},
            waves=UnityEngine.Object.FindObjectsByType<NailongGroundSlam>(FindObjectsSortMode.None).Select(w=>new {w.FromMouth,w.PulsesReleased,w.DamageHits,sections=w.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>().enabled).Select(f=>new{origin=new[]{f.transform.position.x,f.transform.position.y,f.transform.position.z},axis=new[]{f.transform.forward.x,f.transform.forward.y,f.transform.forward.z},radius=f.sharedMesh.bounds.size.y*.5f,travel=f.sharedMesh.bounds.center.z,diameterPixels=Vector3.Distance(view.WorldToScreenPoint(f.transform.TransformPoint(f.sharedMesh.bounds.center+Vector3.up*f.sharedMesh.bounds.extents.y)),view.WorldToScreenPoint(f.transform.TransformPoint(f.sharedMesh.bounds.center-Vector3.up*f.sharedMesh.bounds.extents.y)))}).ToArray()}).ToArray(),
            projectiles=shots.Select(s=>new{s.HasImpacted,s.CollisionRadius,pos=new[]{s.transform.position.x,s.transform.position.y,s.transform.position.z},screen=view.WorldToViewportPoint(s.transform.position).ToString(),diameterPixels=Vector3.Distance(view.WorldToScreenPoint(s.transform.position+view.transform.right*s.CollisionRadius),view.WorldToScreenPoint(s.transform.position-view.transform.right*s.CollisionRadius))}).ToArray()});
        if(frame>3&&frame%3==0)
        {
            var previous=RenderTexture.active;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/frame-"+(frame/3).ToString("D4")+".png",pixels.EncodeToPNG());}
            finally{RenderTexture.active=previous;}
        }
        frame++;
        if(clock<18)return;
        File.WriteAllText(folder+"/motion.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));ReviewAngles();
    }
    async void ReviewAngles()
    {
        reviewingAngles=true;Time.timeScale=0;
        try
        {
            var effects=boss.GetComponent<NailongCombatEffects>();effects.Cancel();
            var waveGo=new GameObject("Temporary native terrain 3D pressure inspection");waveGo.transform.position=boss.transform.position;
            var wave=waveGo.AddComponent<NailongGroundSlam>();wave.InitializeVoice(boss.GetComponent<NailongAttack>(),boss.GetComponent<NailongAttackMotion>(),player.transform,14,3,.65f,2.4f,.4f,true);wave.Tick(1.5f);
            var ring=waveGo.GetComponentInChildren<MeshFilter>().transform;
            Vector3 forward=ring.forward,right=ring.right;
            Vector3 centre=ring.position+forward*5;
            var angles=new[]{right*14+Vector3.up*3,forward*12+Vector3.up,Vector3.up*16};
            for(int a=0;a<angles.Length;a++)
            {
                cameraPosition=centre+angles[a];cameraRotation=Quaternion.LookRotation(centre-cameraPosition);view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
                int renders=0;Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> rendered=(ctx,cam)=>{if(cam==view)renders++;};
                UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=rendered;
                try{var clock=System.Diagnostics.Stopwatch.StartNew();while(renders<6&&clock.ElapsedMilliseconds<10000)await Task.Delay(20);if(renders<6)throw new Exception("Normal camera render timeout");}
                finally{UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=rendered;}
                var previous=RenderTexture.active;
                try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/volume-angle-"+a+".png",pixels.EncodeToPNG());}
                finally{RenderTexture.active=previous;}
            }
            UnityEngine.Object.DestroyImmediate(waveGo);
            var closeGo=new GameObject("Temporary mouth emission close-up");var close=closeGo.AddComponent<NailongGroundSlam>();
            close.InitializeVoice(boss.GetComponent<NailongAttack>(),boss.GetComponent<NailongAttackMotion>(),player.transform,14,1,1,2.4f,.4f,true);close.Tick(.015f);
            Vector3 mouth=boss.GetComponent<NailongAttackMotion>().MouthPosition;
            cameraPosition=mouth+boss.transform.forward*6+Vector3.up*.5f;cameraRotation=Quaternion.LookRotation(mouth+boss.transform.forward*.2f-cameraPosition);cameraFov=32;view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);view.fieldOfView=cameraFov;
            int closeRenders=0;Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> closeRendered=(ctx,cam)=>{if(cam==view)closeRenders++;};
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=closeRendered;
            try{var timer=System.Diagnostics.Stopwatch.StartNew();while(closeRenders<6&&timer.ElapsedMilliseconds<10000)await Task.Delay(20);if(closeRenders<6)throw new Exception("Mouth close-up render timeout");}
            finally{UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=closeRendered;}
            var oldTarget=RenderTexture.active;try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0,false);File.WriteAllBytes(folder+"/mouth-emission-closeup.png",pixels.EncodeToPNG());}finally{RenderTexture.active=oldTarget;}
            File.WriteAllText(folder+"/status.txt","Native recording, three forward-wave views and mouth close-up complete");
        }
        catch(Exception error){File.WriteAllText(folder+"/angle-failure.txt",error.ToString());}
        Finish();
    }
    void Finish(){ready=false;Time.captureFramerate=oldCapture;Time.timeScale=oldScale;EditorApplication.isPaused=false;EditorApplication.isPlaying=false;}
    void OnDestroy(){Time.captureFramerate=oldCapture;Time.timeScale=oldScale;if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
}
