using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewFloatSwimV211
{
    public static object Start(string label="before",int seed=20261012,int fps=30,bool ground=false)
    {
        if(!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<FloatSwimReviewV211>()!=null)throw new Exception("Single Play Mode review required");
        var go=new GameObject("Natural water finale admission review");UnityEngine.Object.DontDestroyOnLoad(go);
        var review=go.AddComponent<FloatSwimReviewV211>();review.label=label;review.seed=seed;review.fps=fps;review.ground=ground;
        return new{started=true,label,seed,fps,actorFixture=ground};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class FloatSwimReviewV211:MonoBehaviour
{
    public string label; public int seed,fps;public bool ground;
    string folder; Story1SharkCollisionSequence sequence; Story1WreckBattle battle;
    readonly List<object> rows=new List<object>();
    float warm,next; bool ready,begun,finished; double started;
    RenderTexture target;Texture2D pixels;int filmFrames;float firstFilm=-999,lastFilm=-999;
    readonly List<object> hydro=new List<object>(); bool fixture;int mainFrames;float firstMain=-999,lastMain=-999;bool layoutCaptured;
    int pauseStage;bool paused;double pauseStart;Vector3 pauseHero,pauseShark,pauseCamera;float pauseClock,maxPausedMotion,maxPausedClock,defeatHold;int pauseSamples;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    IEnumerator Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/chase-entry-20261014/"+label;Directory.CreateDirectory(folder);
        Time.captureFramerate=fps;GameSaveManager.CancelPendingContinue();
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");yield return null;
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();
        battle.DiagnosticScatterSeed=seed;sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(180);
        var trace=gameObject.AddComponent<SharkSwimTraceV211>();trace.folder=folder;ready=true;started=Time.realtimeSinceStartupAsDouble;
    }
    void LateUpdate()
    {
        if(!ready || finished)return;
        warm+=Time.deltaTime;
        if(!begun && warm>=2){sequence.BeginImpact();begun=true;}
        var film=battle.Finale;
        if(label.Contains("pause") && film!=null)
        {
            var chase=film.GetComponent<Story1WreckChaseEntry>();
            if(!paused && (pauseStage==0 && film.CinematicTime<0 && chase!=null && chase.Age>.8f || pauseStage==1 && film.CinematicTime>5))
            {paused=true;pauseStart=Time.realtimeSinceStartupAsDouble;pauseHero=sequence.sahur.transform.position;pauseShark=sequence.swimmer.transform.position;pauseCamera=sequence.storyCamera.transform.position;pauseClock=film.CinematicTime;Time.timeScale=0;}
            if(paused)
            {
                pauseSamples++;maxPausedMotion=Mathf.Max(maxPausedMotion,Mathf.Max(Vector3.Distance(pauseHero,sequence.sahur.transform.position),Mathf.Max(Vector3.Distance(pauseShark,sequence.swimmer.transform.position),Vector3.Distance(pauseCamera,sequence.storyCamera.transform.position))));maxPausedClock=Mathf.Max(maxPausedClock,Mathf.Abs(pauseClock-film.CinematicTime));
                if(Time.realtimeSinceStartupAsDouble-pauseStart>.35){Time.timeScale=1;paused=false;pauseStage++;}
                return;
            }
        }
        if(!layoutCaptured && battle.InitialFragmentCount>0)
        {
            var playback=UnityEngine.Object.FindFirstObjectByType<Story1WreckScatterPlayback>();
            if(playback!=null && !playback.IsPlaying)
            {
                layoutCaptured=true;var origin=(Vector3)typeof(Story1WreckScatterPlayback).GetField("origin",Private).GetValue(playback);var rotation=(Quaternion)typeof(Story1WreckScatterPlayback).GetField("heading",Private).GetValue(playback);
                var layout=typeof(Story1WreckScatterPlayback).GetField("layout",Private).GetValue(playback) as Story1WreckScatterPlayback.Layout;
                var pieces=layout.pieces.Select((p,i)=>{var at=Quaternion.Inverse(rotation)*(playback.ReleasePositions[i]-origin);return new{p.name,x=at.x,z=at.z};}).OrderBy(p=>p.name,StringComparer.Ordinal).ToArray();
                File.WriteAllText(folder+"/layout.json",Newtonsoft.Json.JsonConvert.SerializeObject(pieces,Newtonsoft.Json.Formatting.Indented));
                File.WriteAllText(folder+"/release-pose.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{playback.MaximumReleasePoseError,pieces=pieces.Length},Newtonsoft.Json.Formatting.Indented));
            }
        }
        if(ground && !fixture && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && battle.FightElapsed>5)
        {
            fixture=true;var support=battle.EntryDeck;var feet=support.BoardingPoint(support.GetComponent<Rigidbody>().worldCenterOfMass);
            var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            // An AABB/footprint centre can be a hollow mesh seam. Use an
            // actual upward-facing, dry surface for this explicit fixture.
            bool found=false;
            foreach(var candidate in new[]{battle.EntryDeck}.Concat(battle.Boards.Where(p=>p!=battle.EntryDeck && p.IsBoardable && p.Dimensions.y<1.2f)))
            {
                for(int angle=0;angle<360;angle+=15)
                {
                    var at=candidate.BoardingPoint(candidate.GetComponent<Rigidbody>().worldCenterOfMass+Quaternion.AngleAxis(angle,Vector3.up)*Vector3.forward*100);
                    var normal=candidate.transform.up;if(normal.y<0)normal=-normal;
                    if(!candidate.Collision.Raycast(new Ray(at+normal*3,-normal),out var hit,6) || hit.normal.y<.7f || ocean.SampleSurfaceHeight(hit.point,Time.time)-hit.point.y>.2f)continue;
                    support=candidate;feet=hit.point;found=true;break;
                }
                if(found)break;
            }
            if(!found)throw new Exception("No actual dry deck for grounded fixture");
            sequence.sahur.RestoreSavedPose(feet+Vector3.up*(.05f-sequence.sahur.LowestFootWorldOffset),Quaternion.LookRotation(support.OpenWaterDirection(feet)));
            sequence.sahur.GetComponent<Story1WreckRider>().Initialize(sequence.sahur,battle.Boards,UnityEngine.Object.FindFirstObjectByType<OceanWorld>(),support,battle.WreckDetails);
            Physics.SyncTransforms();
            typeof(Story1WreckBattle).GetMethod("BeginFinale",Private).Invoke(battle,null);
            if(!battle.Finale.GetComponent<Story1WreckChaseEntry>().StartsOnDeck)throw new Exception("Fixture did not enter grounded branch");
        }
        if(film!=null || label.Contains("playable") && (battle.CurrentPhase==Story1WreckBattle.Phase.Fighting || label.Contains("break") && battle.CurrentPhase==Story1WreckBattle.Phase.Breaking))
        {
            var hero=sequence.sahur;var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var anim=hero.CharacterAnimator;
            var support=film!=null?typeof(Story1WreckFinale).GetField("board",Private).GetValue(film) as Story1WreckPlank:battle.EntryDeck;
            var left=anim.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint((film!=null?(Vector3)typeof(Story1WreckFinale).GetField("leftSole",Private).GetValue(film):Vector3.zero));
            var right=anim.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint((film!=null?(Vector3)typeof(Story1WreckFinale).GetField("rightSole",Private).GetValue(film):Vector3.zero));
            var head=anim.GetBoneTransform(HumanBodyBones.Head).position;var chest=anim.GetBoneTransform(HumanBodyBones.Chest).position;
            var rb=support!=null?support.GetComponent<Rigidbody>():null;
            hydro.Add(new{frame=filmFrames,time=Time.time,clock=film!=null?film.CinematicTime:-1000,beat=film!=null?film.CurrentBeat:battle.CurrentPhase.ToString(),headDry=head.y-ocean.SampleSurfaceHeight(head,Time.time),chestDry=chest.y-ocean.SampleSurfaceHeight(chest,Time.time),leftDry=left.y-ocean.SampleSurfaceHeight(left,Time.time),rightDry=right.y-ocean.SampleSurfaceHeight(right,Time.time),board=support?.name,boardY=rb!=null?rb.position.y:0,boardMass=rb!=null?rb.mass:0,boardRelativeHeight=rb!=null?rb.worldCenterOfMass.y-ocean.SampleSurfaceHeight(rb.worldCenterOfMass,Time.time):0,sharkBody=battle.SharkBody.ToString("F4"),sharkAction=sequence.swimmer.GetComponent<TralaleroSwimAnimator>().CinematicAction,boardVerticalSpeed=rb!=null?rb.linearVelocity.y:0,boardTilt=support!=null?Vector3.Angle(support.transform.up,Vector3.up):0,hero=hero.transform.position.ToString("F4"),shark=sequence.swimmer.transform.position.ToString("F4"),sharkForward=sequence.swimmer.transform.forward.ToString("F4")});
        }
        if(film!=null && film.CinematicTime>=0 && film.ExitReason==null)
        {if(mainFrames==0)firstMain=film.CinematicTime;lastMain=film.CinematicTime;mainFrames++;}
        if(film!=null || label.Contains("playable") && (battle.CurrentPhase==Story1WreckBattle.Phase.Fighting || label.Contains("break") && battle.CurrentPhase==Story1WreckBattle.Phase.Breaking))
        {
            if(target==null){target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);}
            var camera=sequence.storyCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
            var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
            try{foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;}camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(folder+"/game-"+filmFrames.ToString("D4")+".png",pixels.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}}
            if(label.Contains("multi"))
            {
                Vector3 savedPosition=camera.transform.position;Quaternion savedRotation=camera.transform.rotation;float savedFov=camera.fieldOfView;
                try
                {
                    var actor=sequence.sahur;Vector3 head=actor.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head).position;
                    var enemy=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();Vector3 marker=film!=null && film.CinematicTime>=14?enemy.TailWorldPoint:enemy.NoseWorldPoint;
                    Vector3 feet=actor.transform.position+Vector3.up*actor.LowestFootWorldOffset,focus=(head+feet+marker)/3;
                    Bounds bounds=new Bounds(head,Vector3.zero);bounds.Encapsulate(feet);bounds.Encapsulate(marker);
                    float distance=Mathf.Clamp(bounds.extents.magnitude/Mathf.Sin(24*Mathf.Deg2Rad)*1.12f,12,40);
                    Vector3 forward=Vector3.ProjectOnPlane(actor.transform.forward,Vector3.up).normalized,side=Vector3.Cross(Vector3.up,forward);
                    foreach(var view in new[]{("front",forward),("side",side),("three-quarter",(forward*.8f+side*.6f).normalized)})
                    {
                        Vector3 at=focus+(view.Item2+Vector3.up*.6f).normalized*distance;var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();at.y=Mathf.Max(at.y,ocean.SampleSurfaceHeight(at,Time.time)+4);
                        camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(focus-at));camera.fieldOfView=48;camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(folder+"/"+view.Item1+"-"+filmFrames.ToString("D4")+".png",pixels.EncodeToPNG());
                    }
                }
                finally{camera.transform.SetPositionAndRotation(savedPosition,savedRotation);camera.fieldOfView=savedFov;camera.targetTexture=previous;RenderTexture.active=active;}
            }
            if(filmFrames==0)firstFilm=film!=null?film.CinematicTime:-1000;lastFilm=film!=null?film.CinematicTime:-1000;filmFrames++;
        }
        if(warm>=next)
        {
            next=warm+.5f;
            rows.Add(new{time=warm,phase=battle.CurrentPhase.ToString(),clock=film!=null?film.CinematicTime:-999,
                beat=film!=null?film.CurrentBeat:null,exit=film!=null?film.ExitReason:null,
                attempts=film!=null?film.EntryAttempts.ToArray():null,queries=film!=null?film.EntryPlanningQueries:0,
                cache=film!=null?((IDictionary)typeof(Story1WreckFinale).GetField("plannedFeet",Private).GetValue(film)).Count:0,
                board=film!=null?((Story1WreckPlank)typeof(Story1WreckFinale).GetField("board",Private).GetValue(film))?.name:null,
                fromWater=film!=null&&film.EntryFromWater,radius=film!=null?(float)typeof(Story1WreckFinale).GetField("supportFootprintRadius",Private).GetValue(film):0,
                chaseAge=film!=null && film.GetComponent<Story1WreckChaseEntry>()!=null?film.GetComponent<Story1WreckChaseEntry>().Age:0,
                chaseStartsOnDeck=film!=null && film.GetComponent<Story1WreckChaseEntry>()!=null && film.GetComponent<Story1WreckChaseEntry>().StartsOnDeck,
                swimDistance=film!=null && film.GetComponent<Story1WreckChaseEntry>()!=null?film.GetComponent<Story1WreckChaseEntry>().SwimDistance:0,
                hero=sequence.sahur.transform.position.ToString("F4"),shark=sequence.swimmer.transform.position.ToString("F4"),
                progress=film!=null?film.EntryPlanningProgress:null});
            File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        }
        // Isolated post-film float fixture: retain the real final pose and
        // water simulation for five seconds without the automatic shore fade.
        if(battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)
        {if(label.Contains("hold"))battle.enabled=false;defeatHold+=Time.deltaTime;}
        if(battle.CurrentPhase==Story1WreckBattle.Phase.Defeat && (!label.Contains("hold") || defeatHold>=5) || Time.realtimeSinceStartupAsDouble-started>240)
        {
            finished=true;
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{phase=battle.CurrentPhase.ToString(),battle.FightElapsed,battle.FinaleAfterSeconds,filmFrames,firstFilm,lastFilm,mainFrames,firstMain,lastMain,groundFixture=fixture,pauseStage,pauseSamples,maxPausedMotion,maxPausedClock,
                film=film==null?null:new{film.ExitReason,film.CinematicTime,film.HeroCounterCount,film.SharkCounterCount,film.BoardJumpCount,film.SharkBreachCount,film.LargestLandingError,film.EntryAttempts,film.EntryPlanningQueries,film.EntryPreparationSeconds,film.EntrySharkAdmissionDelay,film.EntrySelectedBoard,film.EntryFromWater,film.ContactAttemptGaps,film.LargestFootHeightError,film.WorstFootTime,film.LargestPlantSlide,film.WorstPlantTime,film.WorstPlantDetails,film.TailTransitionPenaltyCount,film.LargestTailTransitionExcess,film.HasLostStick,film.IsFloating,film.FloatingWoodCorrection},
                boards=battle.Boards.Select(p=>new{p.name,p.IsBoardable,p.IsBroken,size=p.Dimensions.ToString(),p.Speed,distance=p.EdgeDistance(sequence.sahur.transform.position)}).ToArray()},Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(folder+"/hydro.json",Newtonsoft.Json.JsonConvert.SerializeObject(hydro)); Time.captureFramerate=0;Destroy(gameObject);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;if(paused)Time.timeScale=1;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}

[DefaultExecutionOrder(31900)]public class SharkSwimTraceV211:MonoBehaviour{
public string folder;readonly List<object> rows=new List<object>();Vector3 previous;bool has;Story1WreckBattle battle;
void LateUpdate(){if(Time.deltaTime<=0)return;if(battle==null)battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();if(battle==null)return;var film=battle.Finale;
if(film!=null && film.CinematicTime>=0){File.WriteAllText(folder+"/swim-heading.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));Destroy(this);return;}
if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && film==null)return;var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();if(shark==null)return;
Vector3 at=battle.SharkBody,delta=has?Vector3.ProjectOnPlane(at-previous,Vector3.up):Vector3.zero;previous=at;has=true;
var state=typeof(Story1WreckBattle).GetField("sharkPhase",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle).ToString();
rows.Add(new{frame=Time.frameCount,time=Time.time,state=film!=null?((int)typeof(Story1WreckFinale).GetField("entrySwimWaypoint",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film)>0?"EntryDock":"EntryChase"):state,depth=film!=null?(film.GetComponent<Story1WreckChaseEntry>()?.SwimDepth??0):(float)typeof(Story1WreckBattle).GetField("swimDiveDepth",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle),animatorSpeed=shark.BoneAnimator!=null?shark.BoneAnimator.speed:0,animationPhase=shark.BoneAnimator!=null?shark.BoneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime:0,speed=delta.magnitude/Time.deltaTime,alignment=delta.sqrMagnitude>.0001f?Vector3.Dot(delta.normalized,Vector3.ProjectOnPlane(shark.transform.forward,Vector3.up).normalized):1,solidCorrection=battle.WreckContact.LastCorrection.magnitude});
}}
