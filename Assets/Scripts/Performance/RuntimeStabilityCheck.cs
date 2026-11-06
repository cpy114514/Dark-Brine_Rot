using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Explicit standalone QA only; never installed in a normal session.</summary>
    [DefaultExecutionOrder(31000)]
    public sealed partial class RuntimeStabilityCheck : MonoBehaviour
    {
        readonly List<object> checks=new List<object>(),samples=new List<object>();
        readonly List<string> errors=new List<string>();
        readonly HashSet<string> species=new HashSet<string>();
        string path,phase="loading";
        Keyboard keyboard;Mouse mouse;
        bool held,walking,measuring,finished;
        float nextSample,nextAttack,nextMove;
        double previous,maxFrameMs;
        ThirdPersonPlayerController player;
        new Camera camera;RenderTexture target;int attackFrames;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-stability-check")&&!Environment.GetCommandLineArgs().Contains("-stability-first-island"))return;
            var go=new GameObject("Opt-in standalone stability check");DontDestroyOnLoad(go);go.AddComponent<RuntimeStabilityCheck>();
        }
        void Check(bool passed,string name)
        {
            checks.Add(new{passed,name});if(!passed)throw new InvalidOperationException(name);
        }
        void Logs(string message,string stack,LogType type)
        {if((type==LogType.Error||type==LogType.Exception)&&errors.Count<32)errors.Add(message);}
        void Inputs()
        {
            if(keyboard==null||!keyboard.added)return;
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
            InputSystem.QueueStateEvent(keyboard,firstIsland?new KeyboardState(islandKeys):walking?new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Forward)):new KeyboardState());
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,held));
        }
        IEnumerator Start()
        {
            firstIsland=Environment.GetCommandLineArgs().Contains("-stability-first-island");
            path=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("-stability-output="))?.Substring(18)??Path.Combine(Application.persistentDataPath,"stability-check.json");
            Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.logMessageReceived+=Logs;
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();InputSystem.onBeforeUpdate+=Inputs;
            var pending=new Stack<IEnumerator>();pending.Push(firstIsland?RunFirstIsland():Run());
            while(pending.Count>0)
            {
                bool more;var flow=pending.Peek();
                try{more=flow.MoveNext();}
                catch(Exception e){errors.Add(e.ToString());break;}
                if(!more){(flow as IDisposable)?.Dispose();pending.Pop();continue;}
                if(flow.Current is IEnumerator nested)pending.Push(nested);
                else yield return flow.Current;
            }
            foreach(var flow in pending)(flow as IDisposable)?.Dispose();
            Finish();
        }
        IEnumerator Load(string scene)
        {
            measuring=false;held=walking=false;phase="loading "+scene;
            yield return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
            float deadline=Time.realtimeSinceStartup+120;
            while((AdditiveSceneBootstrap.IsLoading||!(player=FindFirstObjectByType<ThirdPersonPlayerController>()))&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(player&&!AdditiveSceneBootstrap.IsLoading,"World loads: "+scene);
            player.gameObject.AddComponent<GameSaveExcluded>();player.GetComponent<PlayerHealth>().GrantProtection(600);
            if(player.GetComponent<FirstIslandArrival>() is FirstIslandArrival arrival)
            {arrival.StopAllCoroutines();arrival.enabled=false;arrival.RestoreProgress(true,true);}
            var opening=FindFirstObjectByType<SecondIslandAwakening>();if(opening){Destroy(opening.gameObject);yield return null;}
            player.ExternalControlLock=player.ExternalMovementLock=false;player.GetComponent<CharacterController>().enabled=true;
            var objectives=player.GetComponent<FirstIslandObjectives>();if(objectives)objectives.Restore(new FirstIslandObjectives.Progress{stage=4,combatLesson=4});
            camera=Camera.main;Check(camera,"Production gameplay camera present");
            if(!firstIsland){if(!target)target=new RenderTexture(1280,720,24);camera.targetTexture=target;}
            Cursor.lockState=CursorLockMode.Locked;
            yield return new WaitForSecondsRealtime(3);
            measuring=true;previous=0;
        }
        IEnumerator Run()
        {
            yield return Load("Assets/Scenes/First Island/Main.unity");GameWorldProgress.Reset();
            var encounter=ChickenBroEncounter.Active;
            Check(encounter&&encounter.prefab&&encounter.duck&&encounter.goose,"All flock prefabs survive Player build");
            Check(!encounter.Started,"Fresh flock respects Cappuccino prerequisite");
            FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include).GetComponent<Health>().Kill();
            float deadline=Time.realtimeSinceStartup+20;Vector3 point=default;
            while(!encounter.TryGetGuidanceTarget(out point)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(encounter.TryGetGuidanceTarget(out point),"Player build resolves flock guidance target");
            Check(player.GetComponent<FirstIslandObjectives>().TryGetGuidanceTarget(out _,out var id)&&id==encounter.completionId,"Player quest directs to flock");
            player.RestoreSavedPose(point+Vector3.back*10+Vector3.up*(-player.LowestFootWorldOffset+.1f),Quaternion.identity);
            phase="flock combat";deadline=Time.realtimeSinceStartup+80;float nextHit=0;
            while(!encounter.Completed&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.time>nextHit&&ChickenBroFlock.Active!=null)
                {
                    nextHit=Time.time+.4f;
                    foreach(var bro in ChickenBroFlock.Active.Members.ToArray())
                    {species.Add(bro.name.Replace("(Clone)",""));CombatHitFeedback.Apply(player.gameObject,bro.GetComponent<Health>(),500,bro.transform.position+Vector3.up,CombatHitKind.ChargedHeavy);}
                }
                yield return null;
            }
            Check(encounter.Completed&&species.Count>=3,"Three actual waves/species complete through production damage pipeline");
            Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Flock clear unlocks Nailong in Player");
            yield return Load("Assets/Scenes/Second Island/Second Island.unity");
            var enemies=FindObjectsByType<MarshmallowEnemyHealth>(FindObjectsSortMode.None);
            Check(enemies.Length>=60,"Second island marshmallow enemies included in build");
            var cap=FindFirstObjectByType<GravityWristbandReward>();var centers=Enumerable.Range(0,cap.CampCount).Select(x=>cap.CampMapAnchor(x).position).ToArray();
            player.GetComponent<SahurWeaponLoadout>().Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);
            phase="second island combat soak";float until=Time.realtimeSinceStartup+120;int camp=0;
            while(Time.realtimeSinceStartup<until)
            {
                if(Time.realtimeSinceStartup>=nextMove)
                {
                    nextMove=Time.realtimeSinceStartup+8;var at=centers[camp++%centers.Length];
                    if(Physics.Raycast(at+Vector3.up*80,Vector3.down,out var ground,160,~0,QueryTriggerInteraction.Ignore))at=ground.point;
                    player.RestoreSavedPose(at+Vector3.up*(-player.LowestFootWorldOffset+.1f),Quaternion.identity);walking=true;
                }
                if(Time.realtimeSinceStartup>=nextAttack){nextAttack=Time.realtimeSinceStartup+.25f;held=!held;}
                yield return null;
            }
            held=walking=false;
            Check(attackFrames>0,"Native input actually attacks during soak");
            yield return Load("Assets/Scenes/First Island/Main.unity");phase="repeat first island";yield return new WaitForSecondsRealtime(10);
            yield return Load("Assets/Scenes/Second Island/Second Island.unity");phase="repeat second island";yield return new WaitForSecondsRealtime(10);
            Check(errors.Count==0,"No runtime errors through combat and scene reloads");
        }
        void LateUpdate()
        {
            if(!measuring||finished)return;
            MaintainTestHealth();
            ObserveBoss();
            if(player&&player.GetComponent<SahurAttack>().IsCombatMotionActive)attackFrames++;
            double now=Time.realtimeSinceStartupAsDouble;double ms=previous==0?0:(now-previous)*1000;previous=now;maxFrameMs=Math.Max(maxFrameMs,ms);
            if(Time.realtimeSinceStartup<nextSample)return;nextSample=Time.realtimeSinceStartup+1;
            samples.Add(new{phase,seconds=now,frame=Time.frameCount,ms,nativeBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),managedBytes=GC.GetTotalMemory(false),meshes=Resources.FindObjectsOfTypeAll<Mesh>().Length,materials=Resources.FindObjectsOfTypeAll<Material>().Length,health=player?player.GetComponent<PlayerHealth>().currentHealth:0});
        }
        void Finish()
        {
            if(finished)return;finished=true;held=walking=false;
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,errors,checks,species,samples,maxFrameMs},Newtonsoft.Json.Formatting.Indented));
            Cleanup();if(!Application.isEditor)Application.Quit();
        }
        void Cleanup()
        {
            InputSystem.onBeforeUpdate-=Inputs;Application.logMessageReceived-=Logs;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            if(camera&&camera.targetTexture==target)camera.targetTexture=null;
            if(target){target.Release();Destroy(target);target=null;}
        }
        void OnDestroy()=>Cleanup();
    }
}

