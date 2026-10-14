using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ReviewVoyageSharkPatrol
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Editor Play Mode required");
        if(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(x=>x.GetType().Name=="VoyageSharkObserver"))throw new Exception("No duplicate review");
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        if(sequence==null || sequence.HasImpacted)throw new Exception("Need live sailing phase");
        float originalClock=sequence.VoyageTime;
        // Explicit diagnostic setup before the first captured frame. Domain
        // reload/CLI compilation can otherwise miss the beginning. Never
        // reset a live wreck encounter or move the shark within a recording.
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        var type=typeof(Story1SharkCollisionSequence);
        foreach(string field in new[]{"voyageTime","voyageSwimClock","breachVisibilityAt"})type.GetField(field,Private).SetValue(sequence,0f);
        foreach(string field in new[]{"voyageBreaching","breachLaunched","voyageOwnsAnimation"})type.GetField(field,Private).SetValue(sequence,false);
        type.GetProperty("VoyageBreachCount").GetSetMethod(true).Invoke(sequence,new object[]{0});
        var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();shark.ReleaseCinematicAnimation();
        type.GetMethod("InitializeVoyagePatrol",Private).Invoke(sequence,null);
        shark.SampleCinematicAction("Swim",0);shark.ReleaseCinematicAnimation();
        var go=new GameObject("Native voyage patrol review");
        var observer=go.AddComponent<VoyageSharkObserver>();observer.sequence=sequence;
        Time.captureFramerate=30;
        return new{started=true,diagnosticInitialStagingReset=true,originalClock,firstObservedVoyageTime=sequence.VoyageTime,patrolDuration=sequence.PatrolDuration,sequence.PatrolRadius,offline=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class VoyageSharkObserver:MonoBehaviour
{
    public Story1SharkCollisionSequence sequence;
    const string Folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/shark-voyage-patrol-20261013/native-review/";
    readonly List<object> rows=new List<object>();
    readonly List<object> recordedFrames=new List<object>();
    readonly List<float> breachTimes=new List<float>();
    TralaleroSwimAnimator shark;
    Collider[] hull;
    RenderTexture target;Texture2D pixels;
    int images,breaches,pausedFrames,visibleBreachFrames;float lastAngle,orbitDegrees,maxSpeed,maxStepExcess,maxNormalExposure,minimumBoundsClearance=float.PositiveInfinity;
    Vector3 lastBody,pausedRoot;Quaternion pausedHeading;
    bool sampled,pauseTested,pausing,finished;
    double pauseAt,started;
    float impactTime=-1;
    void Awake(){Directory.CreateDirectory(Folder);started=Time.realtimeSinceStartupAsDouble;}
    void LateUpdate()
    {
        try
        {
            if(sequence==null)return;
            if(shark==null)
            {
                shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
                hull=sequence.ship.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
                target=new RenderTexture(960,540,24);target.Create();pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            }
            Vector3 body=sequence.swimmer.TransformPoint(shark.RootMeshBounds.center);
            float dt=Time.deltaTime;
            if(pausing)
            {
                pausedFrames++;
                if(Vector3.Distance(sequence.swimmer.position,pausedRoot)>.0001f || Quaternion.Angle(sequence.swimmer.rotation,pausedHeading)>.01f)throw new Exception("Voyage moves while paused");
                if(Time.realtimeSinceStartupAsDouble-pauseAt>.4){Time.timeScale=1;pausing=false;}
                return;
            }
            if(!pauseTested && sequence.IsVoyageBreaching && sequence.VoyageTime>10)
            {
                pauseTested=true;pausing=true;pauseAt=Time.realtimeSinceStartupAsDouble;pausedRoot=sequence.swimmer.position;pausedHeading=sequence.swimmer.rotation;sampled=false;Time.timeScale=0;return;
            }
            Vector3 forward=Vector3.ProjectOnPlane(sequence.ship.transform.right,Vector3.up).normalized,side=Vector3.Cross(Vector3.up,forward);
            Vector3 relative=Vector3.ProjectOnPlane(body-sequence.ship.transform.position,Vector3.up);
            float angle=Mathf.Atan2(Vector3.Dot(relative,side),Vector3.Dot(relative,forward))*Mathf.Rad2Deg;
            float speed=sampled&&dt>0 ? Vector3.ProjectOnPlane(body-lastBody,Vector3.up).magnitude/dt : 0;
            if(sequence.IsPatrolling)
            {
                if(sampled)orbitDegrees+=Mathf.Abs(Mathf.DeltaAngle(lastAngle,angle));
                maxSpeed=Mathf.Max(maxSpeed,speed);
                float allowance=Mathf.Max(sequence.maximumSwimSpeed,sequence.patrolSwimSpeed+sequence.escapePursuitSpeed)*dt;
                if(sampled)maxStepExcess=Mathf.Max(maxStepExcess,Vector3.ProjectOnPlane(body-lastBody,Vector3.up).magnitude-allowance);
                float radius=Vector3.Scale(shark.RootMeshBounds.extents,sequence.swimmer.lossyScale).magnitude;
                foreach(var c in hull)minimumBoundsClearance=Mathf.Min(minimumBoundsClearance,Mathf.Sqrt(c.bounds.SqrDistance(body))-radius);
                if(!sequence.IsVoyageBreaching)
                {
                    float water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>().SampleSurfaceHeight(body,Time.time);
                    Vector3 half=Vector3.Scale(shark.RootMeshBounds.extents,sequence.swimmer.lossyScale);Quaternion rotation=sequence.swimmer.rotation;
                    float extent=Mathf.Abs((rotation*new Vector3(half.x,0,0)).y)+Mathf.Abs((rotation*new Vector3(0,half.y,0)).y)+Mathf.Abs((rotation*new Vector3(0,0,half.z)).y);
                    maxNormalExposure=Mathf.Max(maxNormalExposure,Mathf.Clamp01((body.y+extent-water)/(2*extent)));
                }
            }
            if(sequence.VoyageBreachCount>breaches){breaches=sequence.VoyageBreachCount;breachTimes.Add(sequence.VoyageTime);}
            Vector3 viewport=sequence.storyCamera.WorldToViewportPoint(body);
            if(sequence.IsVoyageBreaching && viewport.z>0 && viewport.x>.05f && viewport.x<.95f && viewport.y>0 && viewport.y<1)visibleBreachFrames++;
            var battle=sequence.GetComponent<Story1WreckBattle>();
            if(sequence.HasImpacted && impactTime<0)impactTime=sequence.VoyageTime;
            rows.Add(new{time=sequence.VoyageTime,sequence.IsPatrolling,sequence.IsVoyageBreaching,sequence.VoyageBreachCount,
                body=body.ToString("F4"),ship=sequence.ship.transform.position.ToString("F4"),viewport=viewport.ToString("F4"),angle,speed,action=shark.CinematicAction,phase=battle.CurrentPhase.ToString()});
            lastBody=body;lastAngle=angle;sampled=true;
            if(Time.frameCount%2==0 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)Capture(body,forward,side);
            if(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting || Time.realtimeSinceStartupAsDouble-started>300)Finish(battle);
        }
        catch(Exception ex){File.WriteAllText(Folder+"error.txt",ex.ToString());Finish(sequence!=null?sequence.GetComponent<Story1WreckBattle>():null);}
    }
    void Capture(Vector3 body,Vector3 forward,Vector3 side)
    {
        var camera=sequence.storyCamera;var position=camera.transform.position;var heading=camera.transform.rotation;float fov=camera.fieldOfView;var old=camera.targetTexture;var active=RenderTexture.active;
        try
        {
            camera.targetTexture=target;
            for(int view=0;view<2;view++)
            {
                if(view==1)
                {
                    Vector3 centre=Vector3.Lerp(sequence.ship.transform.position,body,.55f);
                    float distance=Mathf.Max(110,sequence.PatrolRadius*1.45f);
                    Vector3 at=centre+(side*.7f-forward*.25f+Vector3.up*.7f).normalized*distance;
                    camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(centre-at));camera.fieldOfView=62;
                }
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);
                File.WriteAllBytes(Folder+(view==0?"game-":"wide-")+images.ToString("D4")+".png",pixels.EncodeToPNG());
            }
            recordedFrames.Add(new{image=images,time=sequence.VoyageTime,sequence.IsVoyageBreaching,phase=sequence.GetComponent<Story1WreckBattle>().CurrentPhase.ToString()});images++;
        }
        finally{camera.transform.SetPositionAndRotation(position,heading);camera.fieldOfView=fov;camera.targetTexture=old;RenderTexture.active=active;}
    }
    void Finish(Story1WreckBattle battle)
    {
        if(finished)return;finished=true;
        File.WriteAllText(Folder+"review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            native=true,offline=true,performanceEvidence=false,diagnosticInitialStagingReset=true,sourceSimulationFPS=30,recordedFPS=15,images,
            patrolDuration=sequence!=null?sequence.PatrolDuration:0,patrolRadius=sequence!=null?sequence.PatrolRadius:0,orbitDegrees,breaches,breachTimes,
            maxSpeed,maxStepExcess,maxNormalExposure,minimumBoundsClearance,pauseTested,pausedFrames,visibleBreachFrames,impactTime,
            phase=battle!=null?battle.CurrentPhase.ToString():null,tailGap=battle!=null?battle.ImpactTailGap:999,recordedFrames,rows},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;Time.timeScale=1;EditorApplication.isPlaying=false;
    }
    void OnDestroy(){Time.captureFramerate=0;Time.timeScale=1;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
