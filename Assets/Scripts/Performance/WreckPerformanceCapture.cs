using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Opt-in standalone encounter capture. Normal game sessions do not create this component.</summary>
    public sealed class WreckPerformanceCapture : MonoBehaviour
    {
        [Serializable] public struct Frame
        {
            public string phase;
            public float dtMs, playerLoopMs, physicsMs, contactMs, buoyancyMs, releaseMs, warmupMs, riderSetupMs, entryPlanningMs, facingPlanningMs, supportSamplingMs, cinematicTime;
            public long allocated;
        }
        [Serializable] public sealed class Report
        {
            public int width, height, frameLimit, vsync, errors, initialFragments, sharkAttacks, brokenBoards, heroCounters, sharkCounters;
            public string gpu, cpu;
            public bool finished;
            public float residualPenetration;
            public List<Frame> frames;
        }
        readonly List<Frame> frames = new List<Frame>(5000);
        ProfilerRecorder loop, physics, contact, buoyancy, release, warmup, riderSetup, entryPlanning, facingPlanning, supportSampling, allocations;
        Story1WreckBattle battle;
        float start;
        int errors;
        bool saved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallIfRequested()
        {
            string flag=Path.GetFullPath(Path.Combine(Application.dataPath,"..","wreck-performance-profile.flag"));
            bool enabled=File.Exists(flag) || Array.IndexOf(Environment.GetCommandLineArgs(),"-profile-wreck")>=0;
            if(!enabled || Application.isEditor)return;
            var go=new GameObject("Optional wreck performance capture");DontDestroyOnLoad(go);go.AddComponent<WreckPerformanceCapture>();
        }
        void Awake()
        {
            Application.runInBackground=true;start=Time.realtimeSinceStartup;
            // Register custom markers before opening the encounter.
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Story1WreckBattle).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Story1WreckRider).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Story1SharkWreckContact).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Story1WreckFloatBody).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Story1WreckFinale).TypeHandle);
            loop=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"PlayerLoop",1);
            physics=ProfilerRecorder.StartNew(ProfilerCategory.Physics,"Physics.Simulate",1);
            contact=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckContact",1);
            buoyancy=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckBuoyancy",1);
            release=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckRelease",1);
            warmup=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckWarmup",1);
            riderSetup=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckRiderSetup",1);
            entryPlanning=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckEntryPlanning",1);
            facingPlanning=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckFacingPlanning",1);
            supportSampling=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Story.WreckSupportSampling",1);
            allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            Application.logMessageReceived+=CaptureError;
        }
        IEnumerator Start()
        {
            yield return null;
            if(SceneManager.GetActiveScene().name!="Story1")
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity",LoadSceneMode.Single);
            yield return null;
            var sequence=FindFirstObjectByType<Story1SharkCollisionSequence>();
            if(!sequence){errors++;Finish(false);yield break;}
            battle=sequence.GetComponent<Story1WreckBattle>();
            sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);
            if(!sequence.sahur.GetComponent<GameSaveExcluded>())sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();
            // Ordinary sailing allows preparation before impact. Retain the same
            // preparation period in this repeatable capture rather than cold-cutting.
            float ready=Time.realtimeSinceStartup+8;
            while(battle && battle.CurrentPhase==Story1WreckBattle.Phase.Waiting && Time.realtimeSinceStartup<ready)yield return null;
            if(battle && battle.CurrentPhase==Story1WreckBattle.Phase.Waiting)sequence.BeginImpact();
        }
        static float Ms(ProfilerRecorder r)=>r.Valid?r.LastValue/1000000f:0;
        void LateUpdate()
        {
            if(saved)return;
            if(battle)
            {
                frames.Add(new Frame{phase=battle.CurrentPhase.ToString(),dtMs=Time.unscaledDeltaTime*1000,
                    playerLoopMs=Ms(loop),physicsMs=Ms(physics),contactMs=Ms(contact),buoyancyMs=Ms(buoyancy),
                    releaseMs=Ms(release),warmupMs=Ms(warmup),riderSetupMs=Ms(riderSetup),entryPlanningMs=Ms(entryPlanning),facingPlanningMs=Ms(facingPlanning),
                    supportSamplingMs=Ms(supportSampling),cinematicTime=battle.Finale?battle.Finale.CinematicTime:-1,allocated=allocations.Valid?allocations.LastValue:0});
                if(battle.CurrentPhase==Story1WreckBattle.Phase.Finished){Finish(true);return;}
            }
            if(Time.realtimeSinceStartup-start>110)Finish(false);
        }
        void CaptureError(string message,string trace,LogType type){if(type==LogType.Exception || type==LogType.Error)errors++;}
        void Finish(bool finished)
        {
            if(saved)return;saved=true;
            var report=new Report{width=Screen.width,height=Screen.height,frameLimit=Application.targetFrameRate,vsync=QualitySettings.vSyncCount,
                errors=errors,finished=finished,frames=frames,initialFragments=battle?battle.InitialFragmentCount:0,
                gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,sharkAttacks=battle?battle.SharkAttackCount:0,brokenBoards=battle?battle.BrokenBoardCount:0,
                heroCounters=battle && battle.Finale?battle.Finale.HeroCounterCount:0,sharkCounters=battle && battle.Finale?battle.Finale.SharkCounterCount:0,
                residualPenetration=battle && battle.WreckContact?battle.WreckContact.MaximumResidualPenetration:0};
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"..","wreck-performance-report.json"));
            File.WriteAllText(path,JsonUtility.ToJson(report));Application.Quit();
        }
        void OnDestroy()
        {
            Application.logMessageReceived-=CaptureError;
            loop.Dispose();physics.Dispose();contact.Dispose();buoyancy.Dispose();release.Dispose();warmup.Dispose();riderSetup.Dispose();entryPlanning.Dispose();facingPlanning.Dispose();supportSampling.Dispose();allocations.Dispose();
        }
    }
}
