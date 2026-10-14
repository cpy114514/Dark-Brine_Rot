using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;

/// <summary>Offline native, continuous four-view review. Never performance evidence.</summary>
[DefaultExecutionOrder(32000)]
public sealed class PairedRecorderV096 : MonoBehaviour
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
                    camera.Render();var active=RenderTexture.active;RenderTexture.active=target;
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
public static class ReviewPairedV096
{
    public static object Start(string folder,bool water=true,int seed=20261012)
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode");
        var holder=new GameObject("Native continuous opening review");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        var recorder=holder.AddComponent<PairedRecorderV096>();recorder.folder=folder;recorder.benchmark=benchmark;
        // The diagnostic timeout observes this from Start. Restored on finish.
        Time.captureFramerate=30;
        return new{started=true,folder,offline=true,performanceEvidence=false};
    }
}

[DefaultExecutionOrder(32650)]
public sealed class FramingObserverV075 : MonoBehaviour
{
    public string output;
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
            var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
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
public static class ObserveFramingV075
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        var go=new GameObject("Observe actual crown framing v075");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<FramingObserverV075>().output=output;return new{started=true,output};
    }
}

[DefaultExecutionOrder(32650)]
public sealed class VisibilityObserverV096:MonoBehaviour
{
    public string output;
    readonly List<object> samples=new List<object>();
    float next;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
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
        var head=animator.GetBoneTransform(HumanBodyBones.Head).position;var chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        samples.Add(new{clock=film.CinematicTime,headWaterClearance=Clearance(head),chestWaterClearance=Clearance(chest),
            headFreeboard=head.y-surface.Height(head),raised=typeof(Story1WreckFinale).GetField("cameraRaised",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film),
            camera=camera.transform.position.ToString("F3"),fov=camera.fieldOfView});
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy(){if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,observesWaterSightlineOnly=true,artAcceptance=false,samples},Newtonsoft.Json.Formatting.Indented));}
}
public static class ObserveVisibilityV096
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only camera water sightline v096");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<VisibilityObserverV096>().output=output;return new{started=true,readOnly=true};
    }
}

// Observe this already-running original encounter, without replacing the scene.
[DefaultExecutionOrder(32700)]
public sealed class EndingObserverV074 : MonoBehaviour
{
    public string output;
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
            battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
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
public static class ObserveEndingV074
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires actual original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<EndingObserverV074>()!=null)throw new Exception("Ending observer already running");
        var go=new GameObject("Observe existing finale ending v074");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EndingObserverV074>().output=output;
        return new{started=true,output,continuesExistingEncounter=true};
    }
}

public static class RunReviewV096
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveFramingV075.Start(root+"framing-v096.json");
        ObserveVisibilityV096.Start(root+"visibility-v096.json");
        ObserveEndingV074.Start(root+"ending-v096.json");
        return ReviewPairedV096.Start(root+"native-paired-v096",false,20261014);
    }
}
