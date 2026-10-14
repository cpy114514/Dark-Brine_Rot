using System;
using System.IO;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(32500)]
public sealed class EntryObserverV058:MonoBehaviour
{
    public string folder,label;
    DateTime startedUtc;
    readonly System.Collections.Generic.List<object> samples=new System.Collections.Generic.List<object>();
    double started,next;
    Story1WreckFinale film;
    void Awake(){started=Time.realtimeSinceStartupAsDouble;next=started;startedUtc=DateTime.UtcNow;}
    void LateUpdate()
    {
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
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
public static class BenchmarkOpeningV058
{
    public static object Start(bool water=true,int seed=20261012,int fixedFps=0,string label="v058")
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Benchmark already running");
        var go=new GameObject("Original opening regression v058");UnityEngine.Object.DontDestroyOnLoad(go);
        var benchmark=go.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=water;benchmark.scatterSeed=seed;
        var observer=go.AddComponent<EntryObserverV058>();observer.folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";observer.label=label;
        Time.captureFramerate=fixedFps;
        return new{started=true,water,seed,fixedFps,label,editorDiagnostic=true,standaloneAcceptance=false};
    }
}
