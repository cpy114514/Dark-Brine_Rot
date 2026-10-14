using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ReviewCinematicLetterbox
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Use the existing Editor in Play Mode");
        if(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(x=>x.GetType().Name=="CinematicFrameObserver"))throw new Exception("Review already active");
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        if(sequence==null)throw new Exception("Story1 required");
        var go=new GameObject("Diagnostic cinematic framing review");UnityEngine.Object.DontDestroyOnLoad(go);
        var observer=go.AddComponent<CinematicFrameObserver>();observer.sequence=sequence;Time.captureFramerate=20;
        return new{started=true,sequence.VoyageTime,diagnosticEarlyFinaleRequest=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class CinematicFrameObserver:MonoBehaviour
{
    public Story1SharkCollisionSequence sequence;
    const string Folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/cinematic-letterbox-20261013/";
    readonly List<object> samples=new List<object>();
    readonly HashSet<string> shots=new HashSet<string>();
    readonly List<object> outsideFrame=new List<object>();
    float sinceStart,phaseAge;string lastPhase;bool requested,finished;int filmFrames,footOutside,headOutside;
    double lastArrivalSearch;FirstIslandArrival arrival;
    void LateUpdate()
    {
        if(finished)return;
        try
        {
            sinceStart+=Time.deltaTime;
            var battle=sequence!=null ? sequence.GetComponent<Story1WreckBattle>() : null;
            string phase=battle!=null ? battle.CurrentPhase.ToString() : "Island";
            if(phase!=lastPhase){lastPhase=phase;phaseAge=0;}phaseAge+=Time.deltaTime;
            var box=UnityEngine.Object.FindFirstObjectByType<CinematicLetterbox>();
            float bars=box!=null ? ((RectTransform)box.transform.Find("Bottom")).anchorMax.y : 0;
            if(battle!=null)
            {
                if(phase=="Waiting" && phaseAge>.5f)Capture("01-voyage-fullscreen",bars);
                if(phase=="Breaking" && phaseAge>.6f)Capture("02-ship-break-wide",bars);
                if(phase=="Fighting" && phaseAge>.6f)Capture("03-player-combat-fullscreen",bars);
                if(phase=="Fighting" && phaseAge>3 && !requested){requested=battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit);}
                if(phase=="Finale" && battle.Finale!=null)
                {
                    var film=battle.Finale;var camera=sequence.storyCamera;
                    float clock=film.CinematicTime;
                    if(clock>1)Capture("04-finale-wide",bars);
                    if(clock>7)Capture("05-throw-wide",bars);
                    if(clock>13)Capture("06-reversal-wide",bars);
                    if(clock>18)Capture("07-defeat-wide",bars);
                    if(clock>0 && clock<20.8f)
                    {
                        filmFrames++;
                        var head=sequence.sahur.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head);
                        var h=camera.WorldToViewportPoint(head.position);
                        var left=camera.WorldToViewportPoint(sequence.sahur.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
                        var right=camera.WorldToViewportPoint(sequence.sahur.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                        var f=left.y<right.y ? left : right;
                        if(h.z>0 && (h.y<bars || h.y>1-bars))headOutside++;
                        if(f.z>0 && (f.y<bars || f.y>1-bars)){footOutside++;if(outsideFrame.Count<25)outsideFrame.Add(new{clock,foot=f,head=h});}
                    }
                }
            }
            else
            {
                if(arrival==null && Time.realtimeSinceStartupAsDouble-lastArrivalSearch>.3){lastArrivalSearch=Time.realtimeSinceStartupAsDouble;arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();}
                if(arrival!=null)
                {
                    if(!arrival.IsAwake && !arrival.IsGettingUp && phaseAge>3)Capture("08-shore-drift-wide",bars);
                    if(arrival.IsGettingUp)Capture("09-shore-getup-wide",bars);
                    if(arrival.IsAwake && bars<.00001f){Capture("10-island-fullscreen",bars);Finish(null);}
                }
            }
            if(sinceStart>150)Finish("Timed out before island control restored");
        }
        catch(Exception error){Finish(error.ToString());}
    }
    void Capture(string name,float bars)
    {
        if(!shots.Add(name))return;
        ScreenCapture.CaptureScreenshot(Folder+name+".png");
        samples.Add(new{name,phase=lastPhase,phaseAge,bars,Screen.width,Screen.height});
    }
    void Finish(string error)
    {
        if(finished)return;finished=true;
        File.WriteAllText(Folder+"runtime-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error,diagnosticEarlyFinaleRequest=requested,performanceEvidence=false,filmFrames,headOutside,footOutside,outsideFrame,samples},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
