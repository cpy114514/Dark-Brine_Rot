using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;

/// <summary>Offline native, continuous four-view review. Never performance evidence.</summary>
[DefaultExecutionOrder(32000)]
public sealed class PairedRecorderV195 : MonoBehaviour
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
public static class ReviewPairedV195
{
    public static object Start(string folder,bool water=true,int seed=20261012)
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode");
        var holder=new GameObject("Native continuous opening review");UnityEngine.Object.DontDestroyOnLoad(holder);
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;benchmark.naturalTrigger=true;
        foreach(var observer in UnityEngine.Object.FindObjectsByType<FramingObserverV195>(FindObjectsSortMode.None))observer.benchmark=benchmark; foreach(var observer in UnityEngine.Object.FindObjectsByType<VisibilityObserverV195>(FindObjectsSortMode.None))observer.benchmark=benchmark; foreach(var observer in UnityEngine.Object.FindObjectsByType<EndingObserverV195>(FindObjectsSortMode.None))observer.benchmark=benchmark; foreach(var observer in UnityEngine.Object.FindObjectsByType<SubmergedEntryObserverV195>(FindObjectsSortMode.None))observer.benchmark=benchmark; var recorder=holder.AddComponent<PairedRecorderV195>();recorder.folder=folder;recorder.benchmark=benchmark;
        var hopObserver=holder.AddComponent<HopEligibilityObserverV195>();hopObserver.benchmark=benchmark;hopObserver.output=folder+"/hop-eligibility.json";
        // The diagnostic timeout observes this from Start. Restored on finish.
        Time.captureFramerate=30;
        return new{started=true,folder,offline=true,performanceEvidence=false};
    }
}

[DefaultExecutionOrder(32950)]
public sealed class HopEligibilityObserverV195:MonoBehaviour
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
public sealed class FramingObserverV195 : MonoBehaviour
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
public static class ObserveFramingV195
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        var go=new GameObject("Observe actual crown framing v075");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<FramingObserverV195>().output=output;return new{started=true,output};
    }
}

[DefaultExecutionOrder(32650)]
public sealed class VisibilityObserverV195:MonoBehaviour
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
public static class ObserveVisibilityV195
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only camera water sightline v195");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<VisibilityObserverV195>().output=output;return new{started=true,readOnly=true};
    }
}

// Observe this already-running original encounter, without replacing the scene.
[DefaultExecutionOrder(32700)]
public sealed class EndingObserverV195 : MonoBehaviour
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
public static class ObserveEndingV195
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires actual original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<EndingObserverV195>()!=null)throw new Exception("Ending observer already running");
        var go=new GameObject("Observe existing finale ending v074");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EndingObserverV195>().output=output;
        return new{started=true,output,continuesExistingEncounter=true};
    }
}

public static class RunReviewV195
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-continuation-20261014/";
        ObserveFramingV195.Start(root+"framing-v195.json");
        ObserveVisibilityV195.Start(root+"visibility-v195.json");
        ObserveEndingV195.Start(root+"ending-v195.json");
        var motion=new GameObject("Read-only submerged entry and movement v195");UnityEngine.Object.DontDestroyOnLoad(motion);
        motion.AddComponent<SubmergedEntryObserverV195>().output=root+"submerged-entry-v195.json";
        var result=ReviewPairedV195.Start(root+"native-paired-v195",true,20261016);
        var observer=new GameObject("Read-only original impact fracture v195");UnityEngine.Object.DontDestroyOnLoad(observer);
        observer.AddComponent<InitialFractureObserverV195>().benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        return result;
    }
}

[DefaultExecutionOrder(32970)]
public sealed class InitialFractureObserverV195:MonoBehaviour
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
        File.WriteAllText(".codex/animation-continuation-20261014/initial-fracture-v195.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            native=true,readOnly=true,performanceEvidence=false,originalDecks=source.decks.Length,
            battle.InitialFragmentCount,battle.ExpectedInitialFragments,battle.InitiallyFracturedSources,retainedThinDecks=rows,
            passed=rows.Length>0 && rows.All(r=>r.sourcePositionError<.001f && r.preciseMesh && r.breakable) && battle.InitialFragmentCount==battle.ExpectedInitialFragments},Newtonsoft.Json.Formatting.Indented));
        Destroy(gameObject);
    }
}

[DefaultExecutionOrder(32750)]
public sealed class SubmergedEntryObserverV195:MonoBehaviour
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

