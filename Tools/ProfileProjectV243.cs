using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditorInternal;

// Passive, existing-Editor profiling. No camera/input/quality/simulation changes.
public static class ProfileProjectV243
{
    public static async Task<object> Run(string label)
    {
        if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
        string folder=".codex/project-performance-20261023/"+label;
        if(Directory.Exists(folder))throw new Exception("Retain previous measurements");
        Directory.CreateDirectory(folder);
        var go=new GameObject("Temporary passive performance capture v243"){hideFlags=HideFlags.DontSave};
        var sampler=go.AddComponent<ProjectSampleV243>();
        bool profiler=ProfilerDriver.enabled;
        try
        {
            sampler.Begin();await Task.Delay(8000);
            var steady=sampler.Result();sampler.measuring=false;
            ProfilerDriver.enabled=true;ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU,true);
            await Task.Delay(2200);
            var totals=new Dictionary<string,double>();var self=new Dictionary<string,double>();var allocations=new Dictionary<string,long>();int used=0;
            for(int f=Math.Max(ProfilerDriver.firstFrameIndex,ProfilerDriver.lastFrameIndex-50);f<=ProfilerDriver.lastFrameIndex-2;f++)
            using(var raw=ProfilerDriver.GetRawFrameDataView(f,0))
            {
                if(!raw.valid)continue;used++;var parents=new Stack<(int end,string name)>();
                for(int i=0;i<raw.sampleCount;i++)
                {
                    while(parents.Count>0&&parents.Peek().end<i)parents.Pop();
                    string n=raw.GetSampleName(i)??"(unnamed)";double ms=raw.GetSampleTimeMs(i),own=ms;
                    int children=raw.GetSampleChildrenCountRecursive(i),end=i+children;
                    for(int j=i+1;j<=end;j+=raw.GetSampleChildrenCountRecursive(j)+1)own-=raw.GetSampleTimeMs(j);
                    totals.TryGetValue(n,out var a);totals[n]=a+ms;self.TryGetValue(n,out a);self[n]=a+Math.Max(0,own);
                    if(n=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)
                    {string path=string.Join(" > ",parents.Reverse().Select(p=>p.name));allocations.TryGetValue(path,out long bytes);allocations[path]=bytes+raw.GetSampleMetadataAsLong(i,0);}
                    if(children>0)parents.Push((end,n));
                }
            }
            var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.enabled).Select(c=>new{c.name,position=c.transform.position.ToString(),c.pixelWidth,c.pixelHeight}).ToArray();
            var result=new{label,steady,traceFrames=used,inclusive=totals.OrderByDescending(x=>x.Value).Take(100).Select(x=>new{x.Key,ms=x.Value/Math.Max(1,used)}),self=self.OrderByDescending(x=>x.Value).Take(75).Select(x=>new{x.Key,ms=x.Value/Math.Max(1,used)}),allocations=allocations.OrderByDescending(x=>x.Value).Take(25).Select(x=>new{path=x.Key,bytes=x.Value/Math.Max(1,used)}),cameras,scene=SceneManager.GetActiveScene().path,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],cap=Application.targetFrameRate,vsync=QualitySettings.vSyncCount,scale=Time.timeScale,capture=Time.captureFramerate,hardware=new{SystemInfo.processorType,SystemInfo.graphicsDeviceName},scope="Passive Editor observation; trace overhead excluded from steady window, no controls or rendering settings changed."};
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return new{folder,frames=sampler.frames.Count,traceFrames=used};
        }
        finally{ProfilerDriver.enabled=profiler;if(go)UnityEngine.Object.DestroyImmediate(go);}
    }
}
public sealed class ProjectSampleV243:MonoBehaviour
{
    public bool measuring;public readonly List<double> frames=new List<double>(4096);
    readonly Dictionary<string,ProfilerRecorder> recorders=new Dictionary<string,ProfilerRecorder>();
    readonly Dictionary<string,List<double>> values=new Dictionary<string,List<double>>();
    readonly FrameTiming[] timing=new FrameTiming[1];readonly List<double> gpu=new List<double>();
    double batches,triangles;int warm;
    public void Begin()
    {
        string[] names={"Main Thread","Render Thread","PlayerLoop","GC Allocated In Frame","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","Physics.Simulate","RenderPipelineManager.DoRenderLoop_Internal","Gfx.WaitForPresentOnGfxThread","Animator.Update","GC.Collect","World.GrassVisibility","World.GrassSubmit"};
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);if(!(names.Contains(d.Name)||d.Name.StartsWith("Opening.")||d.Name.StartsWith("Wreck.")||d.Name.StartsWith("Sahur.")||d.Name.StartsWith("Assembly-CSharp.dll!")&&d.Name.Contains("[Invoke]"))||recorders.ContainsKey(d.Name))continue;recorders[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1);values[d.Name]=new List<double>(4096);}
        measuring=true;
    }
    void Update()
    {
        if(!measuring||warm++<30)return;frames.Add(Time.unscaledDeltaTime*1000);batches+=UnityEditor.UnityStats.batches;triangles+=UnityEditor.UnityStats.triangles;
        FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0&&timing[0].gpuFrameTime<1000)gpu.Add(timing[0].gpuFrameTime);
        foreach(var r in recorders)values[r.Key].Add(r.Value.LastValue/(r.Key=="GC Allocated In Frame"?1.0:1000000.0));
    }
    static object Stats(List<double> data){var a=data.OrderBy(x=>x).ToArray();return a.Length==0?null:new{mean=a.Average(),median=a[a.Length/2],p95=a[(int)((a.Length-1)*.95)],max=a[a.Length-1]};}
    public object Result()=>new{frames=frames.Count,frameMs=Stats(frames),gpuMs=Stats(gpu),batches=batches/Math.Max(1,frames.Count),triangles=triangles/Math.Max(1,frames.Count),markers=values.Select(v=>new{v.Key,data=Stats(v.Value)}).ToArray()};
    void OnDestroy(){foreach(var r in recorders.Values)r.Dispose();}
}
