using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(32600)]
public sealed class ProbeSupportContactV065:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    public string output;
    readonly List<object> frames=new List<object>();
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    object Field(Story1WreckFinale film,string name)=>typeof(Story1WreckFinale).GetField(name,Private)?.GetValue(film);
    Vector3 Property(Story1WreckFinale film,string name)=>(Vector3)typeof(Story1WreckFinale).GetProperty(name,Private).GetValue(film);
    void LateUpdate()
    {
        var film=benchmark!=null ? benchmark.Battle?.Finale : null;
        if(film==null || film.CinematicTime<0)return;
        float t=film.CinematicTime;
        if(t<1.8f)
        {
            var hero=(ThirdPersonPlayerController)Field(film,"hero");
            var shark=(TralaleroSwimAnimator)Field(film,"shark");
            var board=(Story1WreckPlank)Field(film,"board");
            Vector3 feet=(Vector3)typeof(Story1WreckFinale).GetMethod("Deck",Private).Invoke(film,new object[]{-.4f,-Mathf.Sin(Mathf.SmoothStep(0,1,t/1.8f)*Mathf.PI)*.3f});
            Vector3 futureFeet=(Vector3)typeof(Story1WreckFinale).GetMethod("Deck",Private).Invoke(film,new object[]{-.4f,-Mathf.Sin(Mathf.SmoothStep(0,1,1.184f/1.8f)*Mathf.PI)*.3f});
            Vector3 local=(Vector3)Field(film,"parryContactOffset");
            Vector3 actual=Property(film,"ParryContactAim");
            frames.Add(new{t,hero=hero.transform.position.ToString("F5"),heading=hero.transform.eulerAngles.ToString("F3"),feet=feet.ToString("F5"),futureFeet=futureFeet.ToString("F5"),local=local.ToString("F5"),predicted=(futureFeet+hero.transform.TransformVector(local)).ToString("F5"),actual=actual.ToString("F5"),nose=shark.NoseWorldPoint.ToString("F5"),root=shark.transform.position.ToString("F5"),board=board.name,anchor=Field(film,"anchorAnimation"),sole=Field(film,"plantedSoleLocal").ToString(),animation=Field(film,"activeAnimation"),fraction=Field(film,"parryShaftFraction"),film.EntryPoseCost,aim=Field(film,"attackAim").ToString(),correction=benchmark.Battle.WreckContact.LastCorrection.ToString(),gaps=film.ContactAttemptGaps.Clone()});
        }
        else
        {
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(frames,Newtonsoft.Json.Formatting.Indented));
            Destroy(this);
        }
    }
}
public static class ProbeSupportContactV065Run
{
    public static object Start(string output,bool water=true,int seed=20261012)
    {
        if(!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Requires fresh Play Mode");
        var go=new GameObject("Contact support diagnostic v065");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        var probe=go.AddComponent<ProbeSupportContactV065>();probe.benchmark=benchmark;probe.output=output;
        Time.captureFramerate=30;
        return new{started=true,output,offline=true};
    }
}
