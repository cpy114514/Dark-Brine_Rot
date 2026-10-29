using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;

public static class ReadPlayerProfile025
{
    const string Root=".codex/performance025";
    [Serializable] public class Saved
    {
        public bool enabled,gpu,editor,deep;
        public int connection,first,last;
        public string selected,fingerprint;
    }
    static string Fingerprint()
    {
        var text=new StringBuilder();int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex;
        foreach(int f in new[]{first,first+(last-first)/3,first+2*(last-first)/3,last}.Distinct())
        using(var raw=ProfilerDriver.GetRawFrameDataView(f,0))
        {
            text.Append(f).Append(':').Append(raw.valid).Append(';');if(!raw.valid)continue;
            text.Append(raw.frameStartTimeNs).Append(':').Append(raw.sampleCount).Append(';');
            for(int i=0;i<raw.sampleCount;i++)text.Append(raw.GetSampleName(i)).Append(':').Append(raw.GetSampleTimeNs(i)).Append(';');
        }
        using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
    }
    public static object Snapshot()
    {
        if(Application.isPlaying||File.Exists(Root+"/profiler-before.json"))throw new Exception("Fresh stopped profiler snapshot required");
        var saved=new Saved{enabled=ProfilerDriver.enabled,gpu=ProfilerDriver.profileGPU,editor=ProfilerDriver.profileEditor,
            deep=ProfilerDriver.deepProfiling,connection=ProfilerDriver.connectedProfiler,selected=ProfilerDriver.selectedPropertyPath,
            first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex,fingerprint=Fingerprint()};
        if(saved.last>=saved.first && !ProfilerDriver.SaveProfile(Path.GetFullPath(Root+"/user-before.raw")))throw new Exception("Cannot preserve existing profile; do not import");
        File.WriteAllText(Root+"/profiler-before.json",JsonUtility.ToJson(saved,true));
        return new{saved=true,first=saved.first,last=saved.last,bytes=new FileInfo(Root+"/user-before.raw").Length};
    }
    class Totals
    {
        public int frames;
        public double frameMs;
        public Dictionary<string,double> inclusive=new Dictionary<string,double>(),self=new Dictionary<string,double>();
    }
    static void Add(Totals totals,RawFrameDataView raw)
    {
        totals.frames++;totals.frameMs+=raw.frameTimeMs;
        for(int i=0;i<raw.sampleCount;i++)
        {
            string n=raw.GetSampleName(i)??"<unnamed native sample>";double ms=raw.GetSampleTimeMs(i),self=ms;
            int end=i+raw.GetSampleChildrenCountRecursive(i);
            for(int j=i+1;j<=end;j+=raw.GetSampleChildrenCountRecursive(j)+1)self-=raw.GetSampleTimeMs(j);
            totals.inclusive.TryGetValue(n,out double a);totals.inclusive[n]=a+ms;
            totals.self.TryGetValue(n,out double b);totals.self[n]=b+Math.Max(0,self);
        }
    }
    static object Row(string phase,string thread,Totals t)=>new{phase,thread,frames=t.frames,frameMs=t.frameMs/Math.Max(1,t.frames),
        inclusive=t.inclusive.OrderByDescending(x=>x.Value).Take(45).Select(x=>new{name=x.Key,ms=x.Value/Math.Max(1,t.frames)}).ToArray(),
        self=t.self.OrderByDescending(x=>x.Value).Take(35).Select(x=>new{name=x.Key,ms=x.Value/Math.Max(1,t.frames)}).ToArray()};
    public static object Analyze()
    {
        if(Application.isPlaying||!File.Exists(Root+"/profiler-before.json")||File.Exists(Root+"/profile-analysis.json"))throw new Exception("Stopped prepared fresh analysis required");
        var saved=JsonUtility.FromJson<Saved>(File.ReadAllText(Root+"/profiler-before.json"));
        if(ProfilerDriver.enabled!=saved.enabled || ProfilerDriver.firstFrameIndex!=saved.first || ProfilerDriver.lastFrameIndex!=saved.last || Fingerprint()!=saved.fingerprint)
            throw new Exception("Profiler changed since snapshot: preserve latest data before importing");
        string path=Path.GetFullPath(Root+"/runs/a-profile/player-report/forest.raw");
        if(!File.Exists(path)||new FileInfo(path).Length==0)throw new Exception("Completed native capture required");
        var main=new Dictionary<string,Totals>();var render=new Dictionary<string,Totals>();
        var threads=new Dictionary<int,string>();int first=0,last=0;bool restored=false;
        try
        {
            ProfilerDriver.enabled=false;
            if(!ProfilerDriver.LoadProfile(path,false))throw new Exception("Native profile import failed");
            first=ProfilerDriver.firstFrameIndex;last=ProfilerDriver.lastFrameIndex;
            for(int f=first;f<=last;f++)
            using(var raw=ProfilerDriver.GetRawFrameDataView(f,0))
            {
                if(!raw.valid)continue;string phase=null;
                for(int i=0;i<raw.sampleCount;i++){string n=raw.GetSampleName(i);if(n!=null&&n.StartsWith("ForestPhase.",StringComparison.Ordinal)){phase=n.Substring(12);break;}}
                if(phase==null)continue;
                if(!main.TryGetValue(phase,out var m)){m=new Totals();main.Add(phase,m);}Add(m,raw);
                if(threads.Count==0)
                    for(int t=1;t<128;t++)using(var view=ProfilerDriver.GetRawFrameDataView(f,t))
                    {if(!view.valid)break;threads[t]=view.threadName;}
                foreach(var entry in threads.Where(x=>x.Value!=null&&x.Value.IndexOf("Render Thread",StringComparison.OrdinalIgnoreCase)>=0))
                using(var view=ProfilerDriver.GetRawFrameDataView(f,entry.Key))
                {if(!view.valid)continue;if(!render.TryGetValue(phase,out var r)){r=new Totals();render.Add(phase,r);}Add(r,view);}
            }
            var result=new{first,last,threads=threads.Select(x=>new{index=x.Key,name=x.Value}).ToArray(),
                main=main.Select(x=>Row(x.Key,"Main Thread",x.Value)).ToArray(),render=render.Select(x=>Row(x.Key,"Render Thread",x.Value)).ToArray(),
                scope="Actual Development Player binary CPU profile,phase-labelled native measured frames; profiler overhead is not a throughput delta or GPU timing"};
            File.WriteAllText(Root+"/profile-analysis.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return new{analyzed=true,phaseFrames=main.Select(x=>new{phase=x.Key,frames=x.Value.frames}).ToArray(),renderPhases=render.Count};
        }
        finally
        {
            if(saved.last>=saved.first && !ProfilerDriver.LoadProfile(Path.GetFullPath(Root+"/user-before.raw"),false))throw new Exception("Restore original profile from private user-before.raw");
            ProfilerDriver.connectedProfiler=saved.connection;ProfilerDriver.profileEditor=saved.editor;ProfilerDriver.profileGPU=saved.gpu;
            ProfilerDriver.deepProfiling=saved.deep;ProfilerDriver.selectedPropertyPath=saved.selected;ProfilerDriver.enabled=saved.enabled;
            restored=ProfilerDriver.firstFrameIndex==saved.first&&ProfilerDriver.lastFrameIndex==saved.last&&Fingerprint()==saved.fingerprint;
            File.WriteAllText(Root+"/profiler-restored.json",JsonUtility.ToJson(new Saved{enabled=ProfilerDriver.enabled,gpu=ProfilerDriver.profileGPU,
                editor=ProfilerDriver.profileEditor,deep=ProfilerDriver.deepProfiling,connection=ProfilerDriver.connectedProfiler,
                first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex,fingerprint=Fingerprint()},true));
            File.WriteAllText(Root+"/profiler-restoration-proof.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{restored,flags=ProfilerDriver.enabled==saved.enabled&&ProfilerDriver.profileGPU==saved.gpu&&ProfilerDriver.profileEditor==saved.editor&&ProfilerDriver.deepProfiling==saved.deep&&ProfilerDriver.connectedProfiler==saved.connection}));
            if(!restored)throw new Exception("Profiler frame fingerprint mismatch; preserve backups and inspect before further profile work");
        }
    }
}
