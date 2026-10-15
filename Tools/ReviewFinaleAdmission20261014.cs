using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewFinaleAdmission20261014
{
    public static object Start(string label="before",int seed=20261012,int fps=30)
    {
        if(!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<FinaleAdmissionReview>()!=null)throw new Exception("Single Play Mode review required");
        var go=new GameObject("Natural water finale admission review");UnityEngine.Object.DontDestroyOnLoad(go);
        var review=go.AddComponent<FinaleAdmissionReview>();review.label=label;review.seed=seed;review.fps=fps;
        return new{started=true,label,seed,fps,actorFixture=false};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class FinaleAdmissionReview:MonoBehaviour
{
    public string label; public int seed,fps;
    string folder; Story1SharkCollisionSequence sequence; Story1WreckBattle battle;
    readonly List<object> rows=new List<object>();
    float warm,next; bool ready,begun,finished; double started;
    RenderTexture target;Texture2D pixels;int filmFrames;float firstFilm=-999,lastFilm=-999;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    IEnumerator Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/finale-admission-20261014/"+label;Directory.CreateDirectory(folder);
        Time.captureFramerate=fps;GameSaveManager.CancelPendingContinue();
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");yield return null;
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();
        battle.DiagnosticScatterSeed=seed;sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(180);
        ready=true;started=Time.realtimeSinceStartupAsDouble;
    }
    void LateUpdate()
    {
        if(!ready || finished)return;
        warm+=Time.deltaTime;
        if(!begun && warm>=2){sequence.BeginImpact();begun=true;}
        var film=battle.Finale;
        if(film!=null && film.CinematicTime>=0 && film.ExitReason==null)
        {
            if(target==null){target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);}
            var camera=sequence.storyCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(folder+"/game-"+filmFrames.ToString("D4")+".png",pixels.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;}
            if(filmFrames==0)firstFilm=film.CinematicTime;lastFilm=film.CinematicTime;filmFrames++;
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
                progress=film!=null?film.EntryPlanningProgress:null});
            File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        }
        if(battle.CurrentPhase==Story1WreckBattle.Phase.Defeat || Time.realtimeSinceStartupAsDouble-started>240)
        {
            finished=true;
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{phase=battle.CurrentPhase.ToString(),battle.FightElapsed,battle.FinaleAfterSeconds,filmFrames,firstFilm,lastFilm,
                film=film==null?null:new{film.ExitReason,film.CinematicTime,film.HeroCounterCount,film.SharkCounterCount,film.EntryAttempts,film.EntryPlanningQueries,film.EntryPreparationSeconds,film.EntrySelectedBoard,film.EntryFromWater,film.ContactAttemptGaps,film.LargestFootHeightError,film.LargestPlantSlide},
                boards=battle.Boards.Select(p=>new{p.name,p.IsBoardable,p.IsBroken,size=p.Dimensions.ToString(),p.Speed,distance=p.EdgeDistance(sequence.sahur.transform.position)}).ToArray()},Newtonsoft.Json.Formatting.Indented));
            Time.captureFramerate=0;Destroy(gameObject);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
