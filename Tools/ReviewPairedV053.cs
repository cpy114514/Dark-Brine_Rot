using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Offline native, continuous four-view review. Never performance evidence.</summary>
[DefaultExecutionOrder(32000)]
public sealed class PairedRecorderV053 : MonoBehaviour
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
                Bounds bounds=shark.VisualRenderer.bounds;bounds.Encapsulate(feet);bounds.Encapsulate(head);
                float distance=Mathf.Clamp(bounds.extents.magnitude/Mathf.Sin(24*Mathf.Deg2Rad)*1.1f,14,60);
                for(int i=0;i<views.Length;i++)
                {
                    camera.targetTexture=target;
                    if(i==0){camera.transform.SetPositionAndRotation(savedPosition,savedRotation);camera.fieldOfView=savedFov;}
                    else
                    {
                        Vector3 axis=i==1 ? forward : i==2 ? side : (side*.6f+forward*.8f).normalized;
                        Vector3 at=centre+(axis+Vector3.up*.33f).normalized*distance;
                        camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(centre-at));camera.fieldOfView=48;
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
public static class ReviewPairedV053
{
    public static object Start(string folder,bool water=true,int seed=20261012)
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode");
        var holder=new GameObject("Native continuous opening review");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        var recorder=holder.AddComponent<PairedRecorderV053>();recorder.folder=folder;recorder.benchmark=benchmark;
        // The diagnostic timeout observes this from Start. Restored on finish.
        Time.captureFramerate=30;
        return new{started=true,folder,offline=true,performanceEvidence=false};
    }
}
