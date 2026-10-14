#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mavis;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

/// <summary>Opt-in standalone diagnostic; absent from release builds and inert in normal play.</summary>
[DefaultExecutionOrder(31000)]
public sealed class OpeningBenchmark:MonoBehaviour
{
    struct Frame{public string phase;public float ms,time;public long allocation,entry,attack,solid,sweep,escape,clear,retained,volumes,overlap,damage,residual,physics,buoyancy,collection,render,wait,loop,snapshot,planning,prepareShark,anatomy,tailFin,rider,filmInit,soles,clipResolve,warm,colliderSetup;}
    readonly List<Frame> frames=new List<Frame>(12000);
    Story1WreckBattle battle;
    public Story1WreckBattle Battle=>battle;
    ProfilerRecorder allocation;
    ProfilerRecorder entryCost,attackCost,solidCost,sweepCost,escapeCost,clearCost,retainedCost,volumesCost,overlapCost,damageCost,residualCost;
    readonly Dictionary<string,ProfilerRecorder> systemCosts=new Dictionary<string,ProfilerRecorder>();
    static readonly string[] systemNames={"Physics.Simulate","Story.WreckBuoyancy","GC.Collect","Camera.Render","WaitForTargetFPS","PlayerLoop","Wreck.ShapeSnapshot","Wreck.PoseObstruction","Wreck.PreparePlayableShark","Wreck.InitializeAnatomy","Wreck.BuildTailFin","Wreck.RegisterRiderContacts","Wreck.InitializeFilm","Sahur.MeasureSoles","Wreck.ResolveClip","Wreck.WarmGeometry","Sahur.ConfigureCollider","Wreck.BenchmarkRender"};
    void StartSystemCosts()
    {
        // Register our known custom markers before their first execution, so
        // one-off handoff costs cannot disappear from an availability scan.
        foreach(var name in systemNames)
            if(name.StartsWith("Wreck.",StringComparison.Ordinal) || name.StartsWith("Sahur.",StringComparison.Ordinal))
                systemCosts.Add(name,ProfilerRecorder.StartNew(ProfilerCategory.Scripts,name));
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        foreach(var handle in handles)
        {
            var description=ProfilerRecorderHandle.GetDescription(handle);
            if(Array.IndexOf(systemNames,description.Name)<0 || systemCosts.ContainsKey(description.Name))continue;
            var recorder=ProfilerRecorder.StartNew(description.Category,description.Name);
            if(recorder.Valid)systemCosts.Add(description.Name,recorder);
        }
    }
    long SystemCost(string name)=>systemCosts.TryGetValue(name,out var recorder) && recorder.Valid ? recorder.LastValue : 0;
    public bool captureScreenshots=true;
    public bool waterEntry;
    public bool naturalTrigger;
    public int scatterSeed=20261012;
    bool recording;
    Camera renderCamera;
    RenderTexture target;
    Texture2D fencePixel;
    string output;
    readonly HashSet<string> pictures=new HashSet<string>();
    object handoffProbe;
    bool asyncFrameFence;
    int readbacksRequested,readbacksCompleted,readbackErrors,readbacksPending;
    // Offline review advances the simulation at a fixed rate. Wall-clock
    // waits made boat travel depend on screenshot/driver speed even with
    // the same fracture seed. Real-time benchmarks retain real-time waits.
    static object ScenarioWait(float seconds)=>Time.captureFramerate>0
        ? (object)new WaitForSeconds(seconds) : new WaitForSecondsRealtime(seconds);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if(!Environment.GetCommandLineArgs().Contains("--opening-benchmark"))return;
        var go=new GameObject("Opening standalone benchmark");DontDestroyOnLoad(go);go.AddComponent<OpeningBenchmark>();
    }
    IEnumerator Start()
    {
        string[] arguments=Environment.GetCommandLineArgs();int argumentIndex=Array.IndexOf(arguments,"--benchmark-output");
        output=argumentIndex>=0 && argumentIndex+1<arguments.Length ? arguments[argumentIndex+1] : Path.Combine(Application.persistentDataPath,"opening-benchmark.json");
        captureScreenshots &= !arguments.Contains("--benchmark-no-capture");waterEntry |= arguments.Contains("--benchmark-water");
        naturalTrigger |= arguments.Contains("--benchmark-natural-trigger");
        asyncFrameFence=arguments.Contains("--benchmark-async-fence") && SystemInfo.supportsAsyncGPUReadback;
        int seedIndex=Array.IndexOf(arguments,"--benchmark-seed");if(seedIndex>=0 && seedIndex+1<arguments.Length)scatterSeed=int.Parse(arguments[seedIndex+1]);
        UnityEngine.Random.InitState(scatterSeed);
        Screen.SetResolution(1920,1080,FullScreenMode.Windowed);Application.runInBackground=true;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        GameSaveManager.CancelPendingContinue();yield return SceneManager.LoadSceneAsync("Story1");
        yield return ScenarioWait(2);
        var sequence=FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();
        battle.DiagnosticScatterSeed=scatterSeed;
        // A hidden diagnostic window can be occluded by Windows. Explicitly
        // render the actual gameplay camera at the requested resolution and
        // synchronize one pixel, so an unrendered simulation cannot pass QA.
        renderCamera=sequence.storyCamera;renderCamera.enabled=false;
        target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();renderCamera.targetTexture=target;
        fencePixel=new Texture2D(1,1,TextureFormat.RGB24,false);
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.renderMode==RenderMode.ScreenSpaceOverlay){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=renderCamera;canvas.planeDistance=2;}
        sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(180);
        if(!naturalTrigger)battle.minimumFightSeconds=battle.maximumFightSeconds=120;
        allocation=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");recording=true;
        entryCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.EntryPlanning");
        attackCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.AttackFacing");
        solidCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.SolidContact");
        sweepCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.SolidSweep");
        escapeCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.SolidEscape");
        clearCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.ClearSearch");
        retainedCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.RetainedPlacement");
        volumesCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.UpdateVolumes");
        overlapCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.SolidOverlap");
        damageCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.SolvedDamage");
        residualCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.ResidualPenetration");
        StartSystemCosts();
        yield return ScenarioWait(4);sequence.BeginImpact();
        float deadline=Time.realtimeSinceStartup+35;
        while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)yield return null;
        if(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting)
        {
            yield return ScenarioWait(2);
            // Explicit supported-fixture setup, never a production handoff.
            // The separate water scenario keeps the natural swimmer state.
            if(!waterEntry)
            {
                Story1WreckPlank support=null;Vector3 standingFeet=default;
                var candidates=battle.Boards.Where(p=>p.IsBoardable && p.Dimensions.y<1.2f && p.Speed<5 && Mathf.Abs(p.transform.up.y)>.85f)
                    .Where(p=>Mathf.Min(p.Dimensions.x,p.Dimensions.z)>3 && Mathf.Max(p.Dimensions.x,p.Dimensions.z)>8)
                    .OrderByDescending(p=>p.Dimensions.x*p.Dimensions.z);
                foreach(var candidate in candidates)
                {
                    Vector3 centre=candidate.GetComponent<Rigidbody>().worldCenterOfMass;
                    for(int angle=0;angle<360;angle+=15)
                    {
                        Vector3 feet=candidate.BoardingPoint(centre+Quaternion.AngleAxis(angle,Vector3.up)*candidate.transform.forward*100);
                        feet=candidate.BoardingPoint(feet-candidate.OpenWaterDirection(feet)*1.2f);
                        bool realSurface=true;float reach=Mathf.Max(2,candidate.Dimensions.y+1);
                        for(int bearing=0;bearing<360 && realSurface;bearing+=45)
                        {
                            Vector3 sole=feet+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*1.1f;
                            realSurface=candidate.Collision.Raycast(new Ray(sole+candidate.transform.up*reach,-candidate.transform.up),out var hit,reach*2) && Vector3.Dot(hit.normal,Vector3.up)>.5f;
                        }
                        if(!realSurface)continue;
                        support=candidate;standingFeet=feet;break;
                    }
                    if(support!=null)break;
                }
                if(support!=null)
                {
                    var hero=sequence.sahur;hero.RestoreSavedPose(standingFeet+Vector3.up*(.05f-hero.LowestFootWorldOffset),Quaternion.LookRotation(support.OpenWaterDirection(standingFeet)));
                    hero.GetComponent<Story1WreckRider>().Initialize(hero,battle.Boards,FindFirstObjectByType<OceanWorld>(),support,battle.WreckDetails);
                }
                handoffProbe=new{selected=support!=null ? support.name : null,dimensions=support!=null ? support.Dimensions.ToString() : null,realStandingSurface=support!=null,thinCandidates=battle.Boards.Count(p=>p.IsBoardable && p.Dimensions.y<1.2f)};
            }
            if(naturalTrigger)
            {
                // Exercise the production random deadline and safe handoff,
                // leaving the released wreck its real playable settling time.
                deadline=Time.realtimeSinceStartup+(Time.captureFramerate>0 ? 900 : 55);
                while(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)yield return null;
            }
            else{yield return ScenarioWait(2);battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit);}
            // Offline native multi-view recording deliberately slows wall
            // time while preserving the animation/physics clock. It cannot
            // serve as a performance result and needs its own generous limit.
            deadline=Time.realtimeSinceStartup+(Time.captureFramerate>0 ? 900 : 90);
            while((battle.CurrentPhase==Story1WreckBattle.Phase.Fighting || battle.CurrentPhase==Story1WreckBattle.Phase.Finale) && Time.realtimeSinceStartup<deadline)yield return null;
        }
        recording=false;var film=battle.Finale;
        if(asyncFrameFence)
        {
            // Prove completion of queued rendered frames before reporting.
            // GPU readback latency must not serialize every normal game frame.
            float drainUntil=Time.realtimeSinceStartup+2;
            while(readbacksPending>0 && Time.realtimeSinceStartup<drainUntil)yield return null;
        }
        var report=new{standalone=!Application.isEditor,rendered=true,renderMetric="Explicit gameplay camera CPU submission; GPU frame time unmeasured",width=target.width,height=target.height,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),captureScreenshots,scatterSeed,waterEntry,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],phase=battle.CurrentPhase.ToString(),handoffProbe,
            naturalTrigger,observedFightDuration=battle.FightElapsed,scheduledRandomDeadline=battle.FinaleAfterSeconds,requestedFinaleReason=battle.RequestedFinaleReason.ToString(),
            asyncFrameFence,readbacksRequested,readbacksCompleted,readbackErrors,readbacksPending,gpuReadbackVerified=!asyncFrameFence || readbacksCompleted>100 && readbackErrors==0 && readbacksPending==0,
            film=film==null ? null : new{time=film.CinematicTime,film.ExitReason,film.HeroCounterCount,film.SharkCounterCount,film.BoardJumpCount,film.HopPlanningAttempts,film.HopPlanningDiagnostics,film.SharkBreachCount,film.LargestContactGap,film.LargestFootHeightError,film.LargestPlantSlide,film.ContactAttemptGaps,film.ContactObstructions,film.WorstFootTime,film.WorstPlantTime,film.WorstPlantDetails,film.EntryAttempts,film.EntrySelectedBoard,film.EntryPoseCost,film.EntryWoodCost,film.EntryPreparationSeconds,film.EntryFromWater,film.EntryPlanningQueries,film.EntryPlanningProgress,film.ClearRouteEarlyExits,film.PreferredClearRouteEarlyExits,film.ExactBlockedRoutePasses,film.LargestTailSkinAdjustment,film.LargestTailSkinRotation},
            facingSearchExtensions=film!=null ? film.FacingSearchExtensions : 0,
            entryStaging=film==null ? null : new{film.EntrySharkStagingDistance,film.EntrySharkStagingClear,film.EntrySharkAdmissionDelay},
            contactReach=film!=null ? film.LargestRightHandContactAdjustment : 0,
            availableSystemCosts=systemCosts.Where(p=>p.Value.Valid).Select(p=>p.Key).ToArray(),
            unavailableSystemCosts=systemNames.Where(name=>!systemCosts.TryGetValue(name,out var recorder) || !recorder.Valid).ToArray(),
            solid=battle.WreckContact==null ? null : new{battle.WreckContact.MaximumResidualPenetration,battle.WreckContact.ConstraintHoldCount,battle.WreckContact.LargeEscapeCount,battle.WreckContact.LargeEscapes,battle.WreckContact.PoseAdvanceCount,battle.WreckContact.PartialPoseAdvanceCount},
            phases=frames.GroupBy(f=>f.phase).Select(g=>{var a=g.OrderBy(f=>f.ms).ToArray();return new{phase=g.Key,count=a.Length,meanMs=a.Average(f=>f.ms),p99Ms=a[(int)((a.Length-1)*.99f)].ms,onePercentLowFps=1000/a.Skip(a.Length-Math.Max(1,(int)Math.Ceiling(a.Length*.01))).Average(f=>f.ms),maxMs=a.Last().ms,spikes50ms=a.Count(f=>f.ms>50),meanGCBytes=a.Average(f=>f.allocation),maxEntryMs=a.Max(f=>f.entry)/1000000d,maxAttackMs=a.Max(f=>f.attack)/1000000d,maxSolidMs=a.Max(f=>f.solid)/1000000d,maxPhysicsMs=a.Max(f=>f.physics)/1000000d,maxBuoyancyMs=a.Max(f=>f.buoyancy)/1000000d,maxGCMs=a.Max(f=>f.collection)/1000000d,maxRenderMs=a.Max(f=>f.render)/1000000d,maxSnapshotMs=a.Max(f=>f.snapshot)/1000000d,maxPlanningMs=a.Max(f=>f.planning)/1000000d,maxPrepareSharkMs=a.Max(f=>f.prepareShark)/1000000d,maxAnatomyMs=a.Max(f=>f.anatomy)/1000000d,maxTailFinMs=a.Max(f=>f.tailFin)/1000000d,maxRiderMs=a.Max(f=>f.rider)/1000000d,maxFilmInitMs=a.Max(f=>f.filmInit)/1000000d,maxSolesMs=a.Max(f=>f.soles)/1000000d,maxColliderSetupMs=a.Max(f=>f.colliderSetup)/1000000d,maxClipResolveMs=a.Max(f=>f.clipResolve)/1000000d,maxWarmMs=a.Max(f=>f.warm)/1000000d,slowestFrames=a.Reverse().Take(Math.Max(1,(int)Math.Ceiling(a.Length*.01))).Select(DescribeFrame).ToArray(),spikes=a.Where(f=>f.ms>35).Select(DescribeFrame).ToArray()};}).ToArray()};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        allocation.Dispose();yield return new WaitForSecondsRealtime(.2f);
        if(Application.isEditor)Destroy(gameObject);else Application.Quit();
    }
    static object DescribeFrame(Frame f)=>new{f.time,f.ms,entryMs=f.entry/1000000d,attackMs=f.attack/1000000d,solidMs=f.solid/1000000d,sweepMs=f.sweep/1000000d,escapeMs=f.escape/1000000d,clearMs=f.clear/1000000d,retainedMs=f.retained/1000000d,volumesMs=f.volumes/1000000d,overlapMs=f.overlap/1000000d,damageMs=f.damage/1000000d,residualMs=f.residual/1000000d,allocationBytes=f.allocation,physicsMs=f.physics/1000000d,buoyancyMs=f.buoyancy/1000000d,gcMs=f.collection/1000000d,renderMs=f.render/1000000d,waitMs=f.wait/1000000d,loopMs=f.loop/1000000d,snapshotMs=f.snapshot/1000000d,planningMs=f.planning/1000000d,prepareSharkMs=f.prepareShark/1000000d,anatomyMs=f.anatomy/1000000d,tailFinMs=f.tailFin/1000000d,riderMs=f.rider/1000000d,filmInitMs=f.filmInit/1000000d,solesMs=f.soles/1000000d,colliderSetupMs=f.colliderSetup/1000000d,clipResolveMs=f.clipResolve/1000000d,warmMs=f.warm/1000000d};
    void Update(){if(recording && battle!=null)frames.Add(new Frame{phase=battle.CurrentPhase==Story1WreckBattle.Phase.Finale && battle.Finale!=null && battle.Finale.CinematicTime<0 ? "Entry" : battle.CurrentPhase.ToString(),ms=Time.unscaledDeltaTime*1000,time=battle.Finale!=null ? battle.Finale.CinematicTime : battle.FightElapsed,allocation=allocation.Valid ? allocation.LastValue : 0,entry=entryCost.Valid?entryCost.LastValue:0,attack=attackCost.Valid?attackCost.LastValue:0,solid=solidCost.Valid?solidCost.LastValue:0,sweep=sweepCost.Valid?sweepCost.LastValue:0,escape=escapeCost.Valid?escapeCost.LastValue:0,clear=clearCost.Valid?clearCost.LastValue:0,retained=retainedCost.Valid?retainedCost.LastValue:0,volumes=volumesCost.Valid?volumesCost.LastValue:0,overlap=overlapCost.Valid?overlapCost.LastValue:0,damage=damageCost.Valid?damageCost.LastValue:0,residual=residualCost.Valid?residualCost.LastValue:0,physics=SystemCost("Physics.Simulate"),buoyancy=SystemCost("Story.WreckBuoyancy"),collection=SystemCost("GC.Collect"),render=SystemCost("Wreck.BenchmarkRender"),wait=SystemCost("WaitForTargetFPS"),loop=SystemCost("PlayerLoop"),snapshot=SystemCost("Wreck.ShapeSnapshot"),planning=SystemCost("Wreck.PoseObstruction"),prepareShark=SystemCost("Wreck.PreparePlayableShark"),anatomy=SystemCost("Wreck.InitializeAnatomy"),tailFin=SystemCost("Wreck.BuildTailFin"),rider=SystemCost("Wreck.RegisterRiderContacts"),filmInit=SystemCost("Wreck.InitializeFilm"),soles=SystemCost("Sahur.MeasureSoles"),clipResolve=SystemCost("Wreck.ResolveClip"),warm=SystemCost("Wreck.WarmGeometry"),colliderSetup=SystemCost("Sahur.ConfigureCollider")});}
    void LateUpdate()
    {
        if(renderCamera==null || target==null)return;
        Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
        using(new Unity.Profiling.ProfilerMarker("Wreck.BenchmarkRender").Auto())renderCamera.Render();var active=RenderTexture.active;
        if(asyncFrameFence && recording && readbacksPending<8)
        {
            readbacksRequested++;readbacksPending++;
            AsyncGPUReadback.Request(target,0,target.width/2,1,target.height/2,1,0,1,TextureFormat.RGBA32,request=>
            {
                readbacksPending--;
                if(request.hasError)readbackErrors++;
                else if(request.GetData<byte>().Length==4)readbacksCompleted++;
                else readbackErrors++;
            });
        }
        else if(!asyncFrameFence || recording && readbacksPending>=8)
        {
            // Retain the strict synchronous baseline; a slow GPU cannot
            // hide behind an unbounded queue of unfinished readbacks.
            RenderTexture.active=target;fencePixel.ReadPixels(new Rect(0,0,1,1),0,0,false);RenderTexture.active=active;
        }
        // A few explicit image checks accompany the measured run. Their frames
        // remain in the report, so screenshot overhead cannot improve a score.
        if(captureScreenshots && battle!=null && output!=null)
        {
            string name=battle.CurrentPhase.ToString();var film=battle.Finale;
            if(film!=null)name+="-"+(Mathf.FloorToInt(film.CinematicTime/5)*5);
            if((battle.CurrentPhase!=Story1WreckBattle.Phase.Finale || film.CinematicTime>=0) && pictures.Add(name))
            {
                var picture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);RenderTexture.active=active;
                string folder=Path.ChangeExtension(output,null)+"-images";Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),picture.EncodeToPNG());Destroy(picture);
            }
        }
    }
    void OnDestroy()
    {
        foreach(var recorder in systemCosts.Values)if(recorder.Valid)recorder.Dispose();systemCosts.Clear();
        if(allocation.Valid)allocation.Dispose();
        if(entryCost.Valid)entryCost.Dispose();if(attackCost.Valid)attackCost.Dispose();if(solidCost.Valid)solidCost.Dispose();
        if(sweepCost.Valid)sweepCost.Dispose();if(escapeCost.Valid)escapeCost.Dispose();if(clearCost.Valid)clearCost.Dispose();if(retainedCost.Valid)retainedCost.Dispose();if(volumesCost.Valid)volumesCost.Dispose();if(overlapCost.Valid)overlapCost.Dispose();if(damageCost.Valid)damageCost.Dispose();if(residualCost.Valid)residualCost.Dispose();
        if(renderCamera!=null){renderCamera.targetTexture=null;renderCamera.enabled=true;}
        if(target!=null){target.Release();Destroy(target);}
        if(fencePixel!=null)Destroy(fencePixel);
    }
}
#endif


