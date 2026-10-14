using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using Unity.Profiling;

/// <summary>Offline native, continuous four-view review. Never performance evidence.</summary>
[DefaultExecutionOrder(32000)]
public sealed class PairedRecorderV204 : MonoBehaviour
{
    public string folder;
    public OpeningBenchmark benchmark;
    readonly List<object> rows=new List<object>();
    RenderTexture target;
    Texture2D pixels;
    int oldCapture,frame;
    bool started;
    float initialRealTime;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string[] views={"game","front","side","three-quarter"};
    void Awake(){oldCapture=Time.captureFramerate;initialRealTime=Time.realtimeSinceStartup;}
    void LateUpdate()
    {
        try
        {
            // A previous diagnostic scene may still exist while this run's
            // async load starts. Never record that other encounter's finale.
            var battle=benchmark!=null ? benchmark.Battle : null;
            var film=battle!=null ? battle.Finale : null;
            if(film==null || film.CinematicTime<0)
            {
                if(Time.realtimeSinceStartup-initialRealTime>240)throw new Exception("No complete physical admission for the continuous review");
                return;
            }
            if(!started)
            {
                if(film.CinematicTime>.1f)throw new Exception("Continuous recorder missed the beginning of its own film");
                started=true;Time.captureFramerate=30;
                target=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);target.Create();
                pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
                Directory.CreateDirectory(folder);
            }
            var sequence=battle.GetComponent<Story1SharkCollisionSequence>();
            var camera=sequence.storyCamera;
            Vector3 savedPosition=camera.transform.position;Quaternion savedRotation=camera.transform.rotation;
            float savedFov=camera.fieldOfView;var savedTarget=camera.targetTexture;
            try
            {
                Vector3 feet=sequence.sahur.transform.position+Vector3.up*sequence.sahur.LowestFootWorldOffset;
                Vector3 head=sequence.sahur.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head).position;
                var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
                Vector3 enemy=film.CinematicTime<14 ? shark.NoseWorldPoint : shark.TailWorldPoint;
                Vector3 centre=(feet+head+enemy)/3;
                Vector3 forward=Vector3.ProjectOnPlane(sequence.sahur.transform.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
                Vector3 side=Vector3.Cross(Vector3.up,forward);
                Bounds bounds=new Bounds(feet,Vector3.zero);bounds.Encapsulate(head);bounds.Encapsulate(enemy+Vector3.up*2);bounds.Encapsulate(enemy-Vector3.up*2);
                float distance=Mathf.Clamp(bounds.extents.magnitude/Mathf.Sin(24*Mathf.Deg2Rad)*1.1f,12,40);
                for(int i=0;i<views.Length;i++)
                {
                    camera.targetTexture=target;
                    if(i==0){camera.transform.SetPositionAndRotation(savedPosition,savedRotation);camera.fieldOfView=savedFov;}
                    else
                    {
                        Vector3 axis=i==1 ? forward : i==2 ? side : (side*.6f+forward*.8f).normalized;
                        Vector3 at=centre+(axis+Vector3.up*.6f).normalized*distance;
                        var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();at.y=Mathf.Max(at.y,OceanSurfaceSampler.Height(water,at,camera)+5);camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(centre-at));camera.fieldOfView=48;
                    }
                                        var overlays=i==0 ? UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray() : new Canvas[0];
                    var oldCameras=overlays.Select(c=>c.worldCamera).ToArray();var oldDistances=overlays.Select(c=>c.planeDistance).ToArray();
                    try
                    {
                        foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;}
                        camera.Render();
                    }
                    finally
                    {
                        for(int overlay=0;overlay<overlays.Length;overlay++){overlays[overlay].renderMode=RenderMode.ScreenSpaceOverlay;overlays[overlay].worldCamera=oldCameras[overlay];overlays[overlay].planeDistance=oldDistances[overlay];}
                    }
                    var active=RenderTexture.active;RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);RenderTexture.active=active;
                    File.WriteAllBytes(Path.Combine(folder,views[i]+"-"+frame.ToString("D4")+".png"),pixels.EncodeToPNG());
                }
                rows.Add(new{frame,time=film.CinematicTime,beat=film.CurrentBeat,hero=sequence.sahur.transform.position.ToString(),shark=shark.transform.position.ToString(),
                    camera=savedPosition.ToString(),raised=typeof(Story1WreckFinale).GetField("cameraRaised",Private)?.GetValue(film)});frame++;
            }
            finally{camera.transform.SetPositionAndRotation(savedPosition,savedRotation);camera.fieldOfView=savedFov;camera.targetTexture=savedTarget;}
            if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)
            {
                File.WriteAllText(Path.Combine(folder,"review.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{stage="SECOND PASS REVIEW",native=true,offline=true,performanceEvidence=false,recordedFPS=30,sourceFPS=60,frames=frame,film.ExitReason,film.HeroCounterCount,film.SharkCounterCount,film.BoardJumpCount,film.ContactAttemptGaps,film.LargestFootHeightError,film.LargestPlantSlide,rows},Newtonsoft.Json.Formatting.Indented));
                Destroy(this);
            }
        }
        catch(Exception ex){Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"error.txt"),ex.ToString());Destroy(this);}
    }
    void OnDestroy(){Time.captureFramerate=oldCapture;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
public static class ReviewPairedV204
{
    public static object Start(string folder,bool water=true,int seed=20261012)
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode");
        var holder=new GameObject("Native continuous opening review");UnityEngine.Object.DontDestroyOnLoad(holder);
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;benchmark.naturalTrigger=true;
        foreach(var observer in UnityEngine.Object.FindObjectsByType<FramingObserverV204>(FindObjectsSortMode.None))observer.benchmark=benchmark; foreach(var observer in UnityEngine.Object.FindObjectsByType<VisibilityObserverV204>(FindObjectsSortMode.None))observer.benchmark=benchmark; foreach(var observer in UnityEngine.Object.FindObjectsByType<EndingObserverV204>(FindObjectsSortMode.None))observer.benchmark=benchmark; foreach(var observer in UnityEngine.Object.FindObjectsByType<SubmergedEntryObserverV204>(FindObjectsSortMode.None))observer.benchmark=benchmark; var recorder=holder.AddComponent<PairedRecorderV204>();recorder.folder=folder;recorder.benchmark=benchmark;
        var hopObserver=holder.AddComponent<HopEligibilityObserverV204>();hopObserver.benchmark=benchmark;hopObserver.output=folder+"/hop-eligibility.json";
        // The diagnostic timeout observes this from Start. Restored on finish.
        Time.captureFramerate=30;
        return new{started=true,folder,offline=true,performanceEvidence=false};
    }
}

[DefaultExecutionOrder(32950)]
public sealed class HopEligibilityObserverV204:MonoBehaviour
{
    public OpeningBenchmark benchmark;public string output;
    readonly List<object> rows=new List<object>();
    readonly float[] times={3.2f,5.6f,11.8f,14.2f};int sample;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle:null;var film=battle!=null ? battle.Finale:null;
        if(film==null || sample>=times.Length || film.CinematicTime<times[sample])return;
        sample++;
        var actor=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
        Vector3 feet=actor.transform.position+Vector3.up*actor.LowestFootWorldOffset;
        var current=(Story1WreckPlank)typeof(Story1WreckFinale).GetField("board",Private).GetValue(film);
        var nearest=battle.Boards.Where(p=>p!=current && p.IsBoardable)
            .OrderBy(p=>p.EdgeDistance(feet)).Take(12).Select(p=>{
                var landing=p.BoardingPoint(feet);float gap=Vector3.ProjectOnPlane(landing-feet,Vector3.up).magnitude,height=Mathf.Abs(landing.y-feet.y);
                bool eligible=p.Dimensions.y<5 && p.Speed<5 && Mathf.Min(p.Dimensions.x,p.Dimensions.z)>1.6f && gap<12 && height<1.5f;
                return new{name=p.name,gap,height,p.Speed,dimensions=p.Dimensions.ToString("F3"),eligible};
            }).ToArray();
        var plans=(Array)typeof(Story1WreckFinale).GetField("hopPlans",Private).GetValue(film);
        var targets=new List<string>();foreach(var plan in plans)
        {var target=plan!=null ? plan.GetType().GetField("target",BindingFlags.Public|BindingFlags.Instance)?.GetValue(plan) as Story1WreckPlank:null;targets.Add(target!=null ? target.name:null);}
        rows.Add(new{time=film.CinematicTime,film.BoardJumpCount,current=current.name,plannedTargets=targets,nearest});
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{readOnly=true,performanceEvidence=false,fixturePlaced=false,rows},Newtonsoft.Json.Formatting.Indented));
    }
}

[DefaultExecutionOrder(32650)]
public sealed class FramingObserverV204 : MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    Story1WreckFinale film;
    ThirdPersonPlayerController hero;
    Camera shotCamera;
    SkinnedMeshRenderer[] heroSkins;
    int frames,croppedCrown,croppedFeet;
    float minY=float.PositiveInfinity,maxY=float.NegativeInfinity,maxFov;
    void LateUpdate()
    {
        if(film==null)
        {
            var battle=benchmark!=null ? benchmark.Battle:null;
            if(battle==null || battle.Finale==null)return;
            film=battle.Finale;
            var sequence=battle.GetComponent<Story1SharkCollisionSequence>();hero=sequence.sahur;shotCamera=sequence.storyCamera;
            heroSkins=hero.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }
        if(film.CinematicTime<0)return;
        if(film.CinematicTime>=18.05f)
        {
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{frames,croppedCrown,croppedFeet,minY,maxY,maxFov,
                usesActualRendererBounds=true,passed=frames>=100 && croppedCrown==0 && croppedFeet==0,
                observesScreenFramingOnly=true,contactAndArtAcceptance=false},Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);return;
        }
        bool cropped=false;
        foreach(var skin in heroSkins)
        {
            if(!skin.enabled || !skin.gameObject.activeInHierarchy)continue;
            Bounds bounds=skin.bounds;
            for(int corner=0;corner<8;corner++)
            {
                var screen=shotCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,
                    new Vector3((corner&1)==0 ? -1 : 1,(corner&2)==0 ? -1 : 1,(corner&4)==0 ? -1 : 1)));
                minY=Mathf.Min(minY,screen.y);maxY=Mathf.Max(maxY,screen.y);cropped|=Outside(screen);
            }
        }
        Vector3 feet=shotCamera.WorldToViewportPoint(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset);
        frames++;maxFov=Mathf.Max(maxFov,shotCamera.fieldOfView);
        bool Outside(Vector3 p)=>p.z<=0 || p.x<0 || p.x>1 || p.y<0 || p.y>1;
        if(cropped)croppedCrown++;if(Outside(feet))croppedFeet++;
    }
}
public static class ObserveFramingV204
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        var go=new GameObject("Observe actual crown framing v075");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<FramingObserverV204>().output=output;return new{started=true,output};
    }
}

[DefaultExecutionOrder(32650)]
public sealed class VisibilityObserverV204:MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    readonly List<object> samples=new List<object>();
    float next;
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle:null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0 || film.CinematicTime<next)return;
        next=film.CinematicTime+.1f;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var camera=sequence.storyCamera;
        var animator=sequence.sahur.CharacterAnimator;var ocean=FindFirstObjectByType<OceanWorld>();
        var surface=OceanSurfaceSampler.Capture(ocean,camera,Time.time);
        float Clearance(Vector3 point)
        {
            float minimum=float.PositiveInfinity;int count=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(camera.transform.position,point)/2),6,18);
            for(int i=1;i<count;i++){var along=Vector3.Lerp(camera.transform.position,point,(float)i/count);minimum=Mathf.Min(minimum,along.y-surface.Height(along));}
            return minimum;
        }
                string WoodBlocker(Vector3 point)
        {
            var delta=point-camera.transform.position;
            bool backfaces=Physics.queriesHitBackfaces;
            try
            {
                Physics.queriesHitBackfaces=true;
                var hits=Physics.RaycastAll(camera.transform.position,delta.normalized,Mathf.Max(0,delta.magnitude-.1f),~0,QueryTriggerInteraction.Ignore);
                return hits.OrderBy(h=>h.distance).Select(h=>h.collider.GetComponentInParent<Story1WreckMeshCollision>()).FirstOrDefault(s=>s!=null)?.name;
            }
            finally{Physics.queriesHitBackfaces=backfaces;}
        }
        var head=animator.GetBoneTransform(HumanBodyBones.Head).position;var chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        samples.Add(new{clock=film.CinematicTime,headWaterClearance=Clearance(head),chestWaterClearance=Clearance(chest),
            headWoodBlocker=WoodBlocker(head),chestWoodBlocker=WoodBlocker(chest),headFreeboard=head.y-surface.Height(head),raised=typeof(Story1WreckFinale).GetField("cameraRaised",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film),
            camera=camera.transform.position.ToString("F3"),fov=camera.fieldOfView});
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy(){if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,observesWaterAndWoodSightline=true,artAcceptance=false,samples},Newtonsoft.Json.Formatting.Indented));}
}
public static class ObserveVisibilityV204
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only camera water sightline v204");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<VisibilityObserverV204>().output=output;return new{started=true,readOnly=true};
    }
}

// Observe this already-running original encounter, without replacing the scene.
[DefaultExecutionOrder(32700)]
public sealed class EndingObserverV204 : MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    Story1WreckFinale film;
    Story1WreckBattle battle;
    ThirdPersonPlayerController hero;
    OceanWorld ocean;
    double completedAt=-1,pausedAt=-1;
    float completedUnscaledTime;
    string exitReason;
    int heroCounters,sharkCounters;
    int frames,uprightFrames,pausedFrames;
    float endClock,maxClockDrift,minChest=float.PositiveInfinity,maxChest=float.NegativeInfinity,
        minimumHeadClearance=float.PositiveInfinity,maxRock,maxPausedMotion;
    Quaternion firstHeading,lastHeading;
    Vector3 lastChest;
    bool wasPaused,restored,lostStick;
    void LateUpdate()
    {
        var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        if(completedAt>=0 && (arrival!=null || Time.realtimeSinceStartupAsDouble-completedAt>12))
        {Finish(arrival!=null && !arrival.HasStick && !arrival.IsAwake);return;}
        if(film==null && completedAt<0)
        {
            battle=benchmark!=null ? benchmark.Battle:null;
            if(battle!=null)film=battle.Finale;
            if(film!=null)
            {
                hero=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
                ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            }
        }
        if(film==null || !film.IsComplete)return;
        var animator=hero.CharacterAnimator;
        Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position,
            hips=animator.GetBoneTransform(HumanBodyBones.Hips).position,
            chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Quaternion heading=hero.transform.rotation;
        if(completedAt<0){completedAt=Time.realtimeSinceStartupAsDouble;completedUnscaledTime=Time.unscaledTime;endClock=film.Elapsed;firstHeading=heading;}
        exitReason=film.ExitReason;heroCounters=film.HeroCounterCount;sharkCounters=film.SharkCounterCount;
        frames++;
        if(Mathf.Abs(Vector3.Dot((head-hips).normalized,Vector3.up))>.55f)uprightFrames++;
        maxClockDrift=Mathf.Max(maxClockDrift,Mathf.Abs(film.Elapsed-endClock));
        lostStick|=film.HasLostStick;
        if(Time.timeScale==0)
        {
            pausedFrames++;
            if(wasPaused)maxPausedMotion=Mathf.Max(maxPausedMotion,Mathf.Max(Vector3.Distance(chest,lastChest),Quaternion.Angle(heading,lastHeading)));
        }
        else
        {
            minChest=Mathf.Min(minChest,chest.y);maxChest=Mathf.Max(maxChest,chest.y);
            maxRock=Mathf.Max(maxRock,Quaternion.Angle(firstHeading,heading));
            minimumHeadClearance=Mathf.Min(minimumHeadClearance,head.y-ocean.SampleSurfaceHeight(head,Time.time));
        }
        wasPaused=Time.timeScale==0;lastChest=chest;lastHeading=heading;
        double since=Time.unscaledTime-completedUnscaledTime;
        if(pausedAt<0 && since>=1.5){pausedAt=Time.realtimeSinceStartupAsDouble;Time.timeScale=0;}
        if(!restored && pausedAt>=0 && Time.realtimeSinceStartupAsDouble-pausedAt>=.4){Time.timeScale=1;restored=true;}
    }
    void Finish(bool handoff)
    {
            var result=new{frames,uprightFrames,pausedFrames,maxClockDrift,lostStick,chestHeightRange=maxChest-minChest,maxRock,minimumHeadClearance,maxPausedMotion,unconsciousUnarmedIsland=handoff,exitReason,heroCounters,sharkCounters,
                posePassed=frames>=60 && uprightFrames==0 && maxClockDrift==0 && pausedFrames>=5 && maxPausedMotion<=.001f && lostStick,
                wavesPassed=maxChest-minChest>=.015f && maxRock>=.02f && minimumHeadClearance>=.025f,
                standalonePerformanceEvidence=false};
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);
    }
    void OnDestroy(){if(pausedAt>=0 && !restored)Time.timeScale=1;}
}
public static class ObserveEndingV204
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires actual original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<EndingObserverV204>()!=null)throw new Exception("Ending observer already running");
        var go=new GameObject("Observe existing finale ending v074");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EndingObserverV204>().output=output;
        return new{started=true,output,continuesExistingEncounter=true};
    }
}

public static class RunReviewV204
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveFramingV204.Start(root+"framing-v204.json");
        ObserveVisibilityV204.Start(root+"visibility-v204.json");
        ObserveEndingV204.Start(root+"ending-v204.json");
        var motion=new GameObject("Read-only submerged entry and movement v204");UnityEngine.Object.DontDestroyOnLoad(motion);
        motion.AddComponent<SubmergedEntryObserverV204>().output=root+"submerged-entry-v204.json";
        var result=ReviewPairedV204.Start(root+"native-paired-v204",false,20261015);
        var observer=new GameObject("Read-only original impact fracture v204");UnityEngine.Object.DontDestroyOnLoad(observer);
        observer.AddComponent<InitialFractureObserverV204>().benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        AttachReviewDiagnosticsV204.Run();TraceProbeV204.Attach();
        var fixture=new GameObject("Actor-only supported remaining-route diagnostic");UnityEngine.Object.DontDestroyOnLoad(fixture);fixture.AddComponent<SupportedRouteFixtureV204>().benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        return result;
    }
}

[DefaultExecutionOrder(32970)]
public sealed class InitialFractureObserverV204:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.InitialFragmentCount==0)return;
        var source=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        var matrix=(Matrix4x4)typeof(Story1WreckBattle).GetField("shipModelToWorld",Private).GetValue(battle);
        var impact=battle.GetComponent<Story1SharkCollisionSequence>().HullImpactPoint;
        var retained=battle.Boards.Where(p=>p.FractureSource!=null && source.decks.Contains(p.FractureSource) && p.Dimensions.y<=1.2f).ToArray();
        var rows=retained.Select(p=>new{p.name,size=p.Dimensions.ToString("F3"),
            sourcePositionError=Vector3.Distance(p.ShipSourcePosition,matrix.MultiplyPoint3x4(p.FractureSource.sourceCenter)),
            impactDistance=Vector3.ProjectOnPlane(p.ShipSourcePosition-impact,Vector3.up).magnitude,
            preciseMesh=p.Collision.Parts.All(c=>c is MeshCollider && c.convex),breakable=p.FractureSource.splinters!=null && p.FractureSource.splinters.Length>1}).ToArray();
        File.WriteAllText(".codex/animation-continuation-20261014/initial-fracture-v204.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            native=true,readOnly=true,performanceEvidence=false,originalDecks=source.decks.Length,
            battle.InitialFragmentCount,battle.ExpectedInitialFragments,battle.InitiallyFracturedSources,retainedThinDecks=rows,
            passed=rows.Length>0 && rows.All(r=>r.sourcePositionError<.001f && r.preciseMesh && r.breakable) && battle.InitialFragmentCount==battle.ExpectedInitialFragments},Newtonsoft.Json.Formatting.Indented));
        Destroy(gameObject);
    }
}

[DefaultExecutionOrder(32750)]
public sealed class SubmergedEntryObserverV204:MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    readonly List<object> rows=new List<object>();
    Vector3 lastRoot;bool hadRoot;
    int overruns;float maxOverrun,maxPenetration;
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle:null;var film=battle!=null ? battle.Finale:null;
        if(film==null || film.CinematicTime<-.2f)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
        var water=FindFirstObjectByType<OceanWorld>();var surface=OceanSurfaceSampler.Capture(water,sequence.storyCamera,Time.time);
        var bounds=shark.RootMeshBounds;var heading=shark.transform.rotation;
        Vector3 center=shark.transform.TransformPoint(bounds.center),half=Vector3.Scale(bounds.extents,shark.transform.lossyScale);
        float extent=Mathf.Abs((heading*new Vector3(half.x,0,0)).y)+Mathf.Abs((heading*new Vector3(0,half.y,0)).y)+Mathf.Abs((heading*new Vector3(0,0,half.z)).y);
        float height=surface.Height(center),exposure=Mathf.Clamp01((center.y+extent-height)/Mathf.Max(.001f,extent*2));
        Vector3 root=shark.transform.position;float distance=hadRoot ? Vector3.Distance(lastRoot,root):0;
        float overrun=Mathf.Max(0,distance-(24*Mathf.Max(.01f,Time.deltaTime)+.30f));
        if(overrun>.01f)overruns++;maxOverrun=Mathf.Max(maxOverrun,overrun);
        float penetration=battle.WreckContact.CurrentPenetration();maxPenetration=Mathf.Max(maxPenetration,penetration);
        rows.Add(new{time=film.CinematicTime,exposure,waterHeight=height,bodyCenterY=center.y,noseY=shark.NoseWorldPoint.y,
            root=root.ToString("F3"),distance,overrun,penetration});lastRoot=root;hadRoot=true;
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy(){if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,
        performanceEvidence=false,observesOriginalBoundsExposureOnly=true,overruns,maxOverrun,maxPenetration,rows},Newtonsoft.Json.Formatting.Indented));}
}



[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV204:MonoBehaviour
{
    public string folder,label; public OpeningBenchmark benchmark;
    DateTime startedUtc;
    readonly System.Collections.Generic.List<object> samples=new System.Collections.Generic.List<object>();
    double started,next;
    Story1WreckFinale film;
    void Awake(){started=Time.realtimeSinceStartupAsDouble;next=started;startedUtc=DateTime.UtcNow;}
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle : null;
        if(battle!=null && battle.Finale!=null)film=battle.Finale;
        if(Time.realtimeSinceStartupAsDouble>=next)
        {
            next=Time.realtimeSinceStartupAsDouble+1;
            int cacheCount=0;
            if(film!=null)cacheCount=((System.Collections.IDictionary)typeof(Story1WreckFinale).GetField("plannedFeet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film)).Count;
            samples.Add(new{wall=Time.realtimeSinceStartupAsDouble-started,phase=battle!=null?battle.CurrentPhase.ToString():null,clock=film!=null?film.CinematicTime:-999,cacheCount,queries=film!=null?film.EntryPlanningQueries:0,progress=film!=null?film.EntryPlanningProgress:null,tailReplans=film!=null?film.TailRouteReplans:0,biteReplans=film!=null?film.BiteRouteReplans:0});
            File.WriteAllText(folder+"/entry-observer-"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));
        }
    }
    void OnDestroy()
    {
        string original=Path.Combine(Application.persistentDataPath,"opening-benchmark.json");
        if(File.Exists(original) && File.GetLastWriteTimeUtc(original)>=startedUtc)File.Copy(original,folder+"/native-profile-"+label+".json",true);
        Time.captureFramerate=0;
    }
}
public static class BenchmarkOpeningV204
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.naturalTrigger=true;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV204>();observer.benchmark=benchmark;observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014";observer.label=label;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV204>(FindObjectsSortMode.None))monitor.benchmark=benchmark; foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV204>(FindObjectsSortMode.None))monitor.benchmark=benchmark; var contacts=go.AddComponent<ContactRegistrationObserverV204>();contacts.benchmark=benchmark; Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV204:MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    readonly string[] names={"SolidContact","SolidSweep","SolidEscape","ClearSearch","RetainedPlacement","UpdateVolumes","SolidOverlap","SolvedDamage","ResidualPenetration","ShapeSnapshot","AdaptivePose"};
    ProfilerRecorder[] costs;
    readonly List<object> samples=new List<object>();
    bool enteredFilm;
    void Awake(){costs=names.Select(n=>ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck."+n)).ToArray();}
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle : null;
        if(battle==null)return;
        var film=battle.Finale;var solid=battle.WreckContact;
        if(film!=null)enteredFilm=true;
        samples.Add(new{frame=Time.frameCount,clock=film!=null?film.CinematicTime:-999,
            phase=battle.CurrentPhase.ToString(),ms=costs.Select(c=>c.Valid?c.LastValue/1000000d:0).ToArray(),
            budgetYields=film!=null?film.FacingBudgetYields:0,recenters=solid!=null?solid.CurrentPoseRecenterCount:0,partialRoots=solid!=null?solid.PartialRootAdvanceCount:0, geometryCreated=solid!=null?solid.PlanningGeometryCreated:0, geometryReuses=solid!=null?solid.PlanningGeometryReuses:0, correction=solid!=null?solid.LastCorrection.magnitude:0,
            holds=solid!=null?solid.ConstraintHoldCount:0,escapes=solid!=null?solid.LargeEscapeCount:0});
        if(enteredFilm && battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        foreach(var cost in costs)if(cost.Valid)cost.Dispose();
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{readOnly=true,editorDiagnostic=true,names,samples},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObserveSolidV204
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV204>().output=output;return new{started=true,readOnly=true};
    }
}

public static class RunSolidProfileV204
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveSolidV204.Start(root+"solid-stages-v204.json"); ObservePoseMotionV204.Start(root+"pose-motion-v204.json");
        return BenchmarkOpeningV204.Start(true,20261016,30,"v204");
    }
}


[DefaultExecutionOrder(32800)]
public sealed class PoseMotionObserverV204:MonoBehaviour
{
    public string output; public OpeningBenchmark benchmark;
    readonly List<object> rows=new List<object>();
    bool sampled;Vector3 last;Quaternion heading;
    int frames,travelOverruns;float maximumPenetration,maximumTravel,maximumOverrun;
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle : null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0)return;
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        var solid=battle.WreckContact;Vector3 at=shark.transform.position;
        float penetration=solid.CurrentPenetration(),distance=sampled?Vector3.Distance(last,at):0;
        float overrun=Mathf.Max(0,distance-(24*Mathf.Max(.01f,Time.deltaTime)+.30f));
        if(sampled && overrun>.01f)travelOverruns++;
        maximumPenetration=Mathf.Max(maximumPenetration,penetration);maximumTravel=Mathf.Max(maximumTravel,distance);maximumOverrun=Mathf.Max(maximumOverrun,overrun);
        rows.Add(new{clock=film.CinematicTime,penetration,distance,overrun,headingStep=sampled?Quaternion.Angle(heading,shark.transform.rotation):0,
            solid.PoseAdvanceCount,solid.PartialPoseAdvanceCount,solid.ConstraintHoldCount,solid.LargeEscapeCount,solid.PartialRootAdvanceCount});
        last=at;heading=shark.transform.rotation;sampled=true;frames++;
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,
            frames,maximumPenetration,maximumTravel,maximumOverrun,travelOverruns,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObservePoseMotionV204
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only actual solved pose motion v204");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<PoseMotionObserverV204>().output=output;return new{started=true,readOnly=true};
    }
}

[DefaultExecutionOrder(32900)]
public sealed class ContactRegistrationObserverV204:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    readonly List<object> rows=new List<object>();
    readonly HashSet<string> phases=new HashSet<string>();
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle:null;if(battle==null)return;
        string phase=battle.CurrentPhase.ToString();
        if(phase!="Waiting" && phase!="Fighting")return;
        if(phases.Contains(phase))return;phases.Add(phase);
        var actor=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
        var controller=actor.GetComponent<CharacterController>();
        var bodies=actor.GetComponentsInChildren<Collider>(true).Where(c=>c!=controller && !c.isTrigger).ToArray();
        var root=(GameObject)typeof(Story1WreckBattle).GetField("debrisRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
        int tested=0,ignored=0,activeShapes=0,shapes=0;var failures=new List<string>();
        foreach(var shape in root.GetComponentsInChildren<Story1WreckMeshCollision>(true))
        {
            if(shape.Parts==null)continue;
            bool active=shape.gameObject.activeInHierarchy;if(phase=="Fighting" && !active)continue;
            shapes++;if(active)activeShapes++;
            foreach(var part in shape.Parts)foreach(var body in bodies)
            {
                tested++;
                if(Physics.GetIgnoreCollision(part,body))ignored++;
                else if(failures.Count<12)failures.Add(shape.name+" / "+part.name+" / "+body.name);
            }
        }
        rows.Add(new{phase,shapes,activeShapes,bodies=bodies.Length,tested,ignored,
            cacheShapes=actor.GetComponent<Story1WreckRider>().RegisteredContactShapeCount,passed=tested>0 && tested==ignored,failures});
        File.WriteAllText(".codex/animation-continuation-20261014/rider-contact-registration-v204.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,rows},Newtonsoft.Json.Formatting.Indented));
    }
}

public static class AttachReviewDiagnosticsV204
{
    public static object Run()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Existing single review required");
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveSolidV204.Start(root+"solid-stages-v204.json");
        ObservePoseMotionV204.Start(root+"pose-motion-v204.json");
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<SolidObserverV204>(FindObjectsSortMode.None))monitor.benchmark=benchmark;
        foreach(var monitor in UnityEngine.Object.FindObjectsByType<PoseMotionObserverV204>(FindObjectsSortMode.None))monitor.benchmark=benchmark;
        var observer=benchmark.gameObject.AddComponent<EntryObserverV204>();observer.benchmark=benchmark;observer.folder=root.TrimEnd('/');observer.label="v204";
        benchmark.gameObject.AddComponent<ContactRegistrationObserverV204>().benchmark=benchmark;
        return new{attached=true,readOnly=true,newBenchmark=false};
    }
}

public sealed class ProbeTraceStoreV204
{
    public OpeningBenchmark benchmark;
    public bool valid,done;
    public Vector3 rawRoot,rawNose,previousRoot,aim;
    public Quaternion rawHead,rawBody;
    public readonly List<object> rows=new List<object>();
    public static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    public static T Read<T>(object instance,string name)=>(T)instance.GetType().GetField(name,Flags).GetValue(instance);
    public void Finish(GameObject holder)
    {
        if(done)return;done=true;
        File.WriteAllText(".codex/animation-continuation-20261014/probe-trace-v204.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            readOnly=true,actorOrWoodMoved=false,sampledBeforeAndAfterNativeSolver=true,rows,performanceEvidence=false},Newtonsoft.Json.Formatting.Indented));
        UnityEngine.Object.Destroy(holder);
    }
}
[DefaultExecutionOrder(1190)]
public sealed class ProbeTraceBeforeV204:MonoBehaviour
{
    public ProbeTraceStoreV204 store;
    void LateUpdate()
    {
        var battle=store.benchmark!=null?store.benchmark.Battle:null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0 || store.done)return;
        if(film.CinematicTime>1.8f){store.Finish(gameObject);return;}
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        store.rawRoot=shark.transform.position;store.rawNose=shark.NoseWorldPoint;
        store.previousRoot=ProbeTraceStoreV204.Read<Vector3>(battle.WreckContact,"previousRoot");
        store.aim=ProbeTraceStoreV204.Read<Vector3>(film,"attackAim");
        store.rawHead=ProbeTraceStoreV204.Read<Transform>(battle.WreckContact,"head").localRotation;
        store.rawBody=ProbeTraceStoreV204.Read<Transform>(battle.WreckContact,"body").localRotation;
        store.valid=true;
    }
}
[DefaultExecutionOrder(33110)]
public sealed class ProbeTraceAfterV204:MonoBehaviour
{
    public ProbeTraceStoreV204 store;
    void LateUpdate()
    {
        if(!store.valid || store.done)return;store.valid=false;
        var battle=store.benchmark!=null?store.benchmark.Battle:null;var film=battle!=null?battle.Finale:null;if(film==null)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
        var contact=battle.WreckContact;var head=ProbeTraceStoreV204.Read<Transform>(contact,"head");var body=ProbeTraceStoreV204.Read<Transform>(contact,"body");
        var board=ProbeTraceStoreV204.Read<Story1WreckPlank>(film,"board");
        store.rows.Add(new{clock=film.CinematicTime,dt=Time.deltaTime,
            rawRoot=ProbeTraceStoreV204.V(store.rawRoot),root=ProbeTraceStoreV204.V(shark.transform.position),
            rawNose=ProbeTraceStoreV204.V(store.rawNose),nose=ProbeTraceStoreV204.V(shark.NoseWorldPoint),
            previousRoot=ProbeTraceStoreV204.V(store.previousRoot),aim=ProbeTraceStoreV204.V(store.aim),
            rootCorrection=Vector3.Distance(store.rawRoot,shark.transform.position),
            noseCorrection=Vector3.Distance(store.rawNose,shark.NoseWorldPoint),
            requestedTravel=Vector3.Distance(store.rawRoot,store.previousRoot),actualTravel=Vector3.Distance(shark.transform.position,store.previousRoot),
            headPoseCorrectionDegrees=Quaternion.Angle(store.rawHead,head.localRotation),bodyPoseCorrectionDegrees=Quaternion.Angle(store.rawBody,body.localRotation),
            board=board!=null?board.name:null,actorRoot=ProbeTraceStoreV204.V(sequence.sahur.transform.position),
            route=ProbeTraceStoreV204.Read<float>(film,"attackRouteCost"),
            contact.ConstraintHoldCount,contact.PartialRootAdvanceCount,contact.LargeEscapeCount,
            obstacles=contact.LastObstacles.ToArray(),gaps=(float[])film.ContactAttemptGaps.Clone()});
    }
}
public static class TraceProbeV204
{
    public static object Attach()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Single existing native run required");
        var holder=new GameObject("Read-only first probe before and after solving");UnityEngine.Object.DontDestroyOnLoad(holder);
        var store=new ProbeTraceStoreV204{benchmark=benchmark};
        holder.AddComponent<ProbeTraceBeforeV204>().store=store;holder.AddComponent<ProbeTraceAfterV204>().store=store;
        return new{attached=true,readOnly=true};
    }
}
// Explicit integration fixture: only the diagnostic actor is placed, once.
// No wood transform, velocity, gravity, collider, damage or timeline is edited.
[DefaultExecutionOrder(32980)]
public sealed class SupportedRouteFixtureV204:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    bool placed;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || placed || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<8)return;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var actor=sequence.sahur;
        var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        foreach(var board in battle.Boards.Where(p=>p!=null && p.IsBoardable && p.Dimensions.y<1.2f && Mathf.Abs(p.transform.up.y)>.9f && p.Speed<4).OrderByDescending(p=>p.Dimensions.x*p.Dimensions.z))
        {
            var body=board.GetComponent<Rigidbody>();Vector3 centre=body.worldCenterOfMass;
            for(int angle=0;angle<360;angle+=15)
            {
                Vector3 feet=board.BoardingPoint(centre+Quaternion.AngleAxis(angle,Vector3.up)*board.transform.forward*100);
                feet=board.BoardingPoint(feet-board.OpenWaterDirection(feet)*1.2f);
                Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
                bool clear=true;float reach=Mathf.Max(2,board.Dimensions.y+1);
                for(int bearing=0;bearing<360 && clear;bearing+=45)
                {
                    Vector3 sole=feet+Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward*1.1f;
                    clear=board.Collision.Raycast(new Ray(sole+normal*reach,-normal),out var hit,reach*2) && Vector3.Dot(hit.normal,Vector3.up)>.5f;
                }
                if(!clear || ocean.SampleSurfaceHeight(feet,Time.time)-feet.y>.8f || Mathf.Abs(body.GetPointVelocity(feet).y)>3)continue;
                actor.RestoreSavedPose(feet+Vector3.up*(.04f-actor.LowestFootWorldOffset),Quaternion.LookRotation(board.OpenWaterDirection(feet)));
                var rider=actor.GetComponent<Story1WreckRider>();rider.Initialize(actor,battle.Boards,ocean,board,battle.WreckDetails);actor.NotifyPlatformLanding();
                bool stable=rider.HasStableSupport;
                if(!stable)continue;
                placed=true;bool requested=battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit);
                File.WriteAllText(".codex/animation-continuation-20261014/supported-route-fixture-v204.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{actorOnlyFixture=true,woodMoved=false,naturalHandoffAcceptance=false,performanceEvidence=false,stable,requested,board=board.name,feet=feet.ToString("F4"),fightTime=battle.FightElapsed},Newtonsoft.Json.Formatting.Indented));
                Destroy(this);return;
            }
        }
    }
}
