using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

public static class ProfileWholeGame
{
    public static async Task<object> Run(string label="before")
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        var reports=new List<object>();
        int rate=Application.targetFrameRate, sync=QualitySettings.vSyncCount;
        try
        {
            Application.targetFrameRate=-1; QualitySettings.vSyncCount=0;
            reports.Add(await Sample("menu"));
            GameSaveManager.CancelPendingContinue();
            await Load("Assets/Scenes/First Island/Main.unity");
            for(int i=0;i<300 && (!SceneManager.GetSceneByName("Enemies").isLoaded || !Camera.main);i++)await Task.Delay(30);
            await Task.Delay(1500);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player && !player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();
            var arrival=player.GetComponent<FirstIslandArrival>(); if(arrival)arrival.RestoreProgress(true,true);
            if(arrival)arrival.enabled=false;
            player.ExternalControlLock=true;
            var originalCamera=Camera.main;
            var cameraObject=UnityEngine.Object.Instantiate(originalCamera.gameObject);
            cameraObject.name="Performance comparison camera";
            foreach(var script in cameraObject.GetComponentsInChildren<MonoBehaviour>())
                if(!(script is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))script.enabled=false;
            foreach(var listener in cameraObject.GetComponentsInChildren<AudioListener>())listener.enabled=false;
            originalCamera.enabled=false;originalCamera.tag="Untagged";
            var camera=cameraObject.GetComponent<Camera>();camera.tag="MainCamera";camera.enabled=true;
            player.enabled=false;
            reports.Add(await Sample("island-shore"));
            var trees=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r=>r.name.Contains("LOD0") && !r.forceRenderingOff).ToArray();
            if(trees.Length>0)
            {
                Vector3 center=trees.OrderBy(r=>(r.transform.position-player.transform.position).sqrMagnitude).ElementAt(Math.Min(25,trees.Length-1)).bounds.center;
                var island=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled);
                float y=island.GetWorldSurfaceHeight(center);
                camera.transform.position=new Vector3(center.x-8,y+5,center.z-15);
                camera.transform.LookAt(new Vector3(center.x,y+3,center.z+22));
                await Task.Delay(600);
                reports.Add(await Sample("island-forest"));
                camera.transform.position=new Vector3(center.x-35,y+60,center.z-80);
                camera.transform.LookAt(new Vector3(center.x,y,center.z+80));
                await Task.Delay(600);
                reports.Add(await Sample("island-panorama"));
            }
            var world=InspectWorld();
            var map=player.GetComponent<IslandMapUI>(); map.SetOpen(true); await Task.Delay(200);
            reports.Add(await Sample("map"));map.SetOpen(false);
            string path=Path.GetFullPath(".codex/performance/whole-game-"+label+".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var result=new {label,reports,world};
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player && !player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();
            Time.timeScale=1;await Load("Assets/Scenes/Main Menu/MainMenu.unity");
            Application.targetFrameRate=rate;QualitySettings.vSyncCount=sync;
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
    static async Task Load(string path){var op=SceneManager.LoadSceneAsync(path);while(!op.isDone)await Task.Delay(30);}
    static async Task<object> Sample(string name)
    {
        // Scene settings restore the user's cap on load; remove it for each measurement.
        Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
        var holder=new GameObject("Temporary whole-game performance sampler");
        var sampler=holder.AddComponent<WholeGameFrameSampler>();
        try {sampler.Begin();await Task.Delay(3600);return sampler.Result(name);}
        finally {UnityEngine.Object.DestroyImmediate(holder);}
    }
    static object InspectWorld()
    {
        var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        var meshes=renderers.Select(r=>new{renderer=r,mesh=r.GetComponent<MeshFilter>()?.sharedMesh})
            .Where(x=>x.mesh).ToArray();
        var textures=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m)
            .SelectMany(m=>m.GetTexturePropertyNames().Select(n=>m.GetTexture(n))).Where(t=>t).Distinct().ToArray();
        return new {
            renderers=renderers.Length,
            behaviours=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.enabled).GroupBy(b=>b.GetType().Name).OrderByDescending(g=>g.Count()).Take(25).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),
            geometry=meshes.GroupBy(x=>x.mesh).OrderByDescending(g=>(long)g.Key.GetIndexCount(0)*g.Count()).Take(20).Select(g=>new{mesh=g.Key.name,copies=g.Count(),trianglesPerCopy=g.Key.GetIndexCount(0)/3,visible=g.Count(x=>x.renderer.enabled&&!x.renderer.forceRenderingOff),shadows=g.Count(x=>x.renderer.shadowCastingMode!=ShadowCastingMode.Off)}).ToArray(),
            textures=textures.OrderByDescending(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)).Take(15).Select(t=>new{name=t.name,width=t.width,height=t.height,bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)}).ToArray(),
            shaders=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).GroupBy(m=>m.shader.name).OrderByDescending(g=>g.Count()).Select(g=>new{name=g.Key,count=g.Count(),uniqueMaterials=g.Distinct().Count()}).ToArray(),
            colliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).GroupBy(c=>c.GetType().Name).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),
            lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>new{name=l.name,type=l.type.ToString(),shadows=l.shadows.ToString()}).ToArray()
        };
    }
}
[DefaultExecutionOrder(10000)]
public sealed class WholeGameFrameSampler:MonoBehaviour
{
    sealed class Counter{public string name;public ProfilerRecorder recorder;public double sum,peak;}
    readonly List<Counter> counters=new List<Counter>();
    readonly List<double> intervals=new List<double>(1200);
    readonly FrameTiming[] timing=new FrameTiming[1];
    int warmup,gpuSamples;double batches,triangles,gpu;
    Camera view;Vector3 viewPosition;Quaternion viewRotation;
    public void Begin()
    {
        view=Camera.main;if(view){viewPosition=view.transform.position;viewRotation=view.transform.rotation;}
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        string[] names={"Main Thread","Render Thread","PlayerLoop","GC Allocated In Frame","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","FixedUpdate.ScriptRunBehaviourFixedUpdate","Physics.Simulate","RenderPipelineManager.DoRenderLoop_Internal","Gfx.WaitForPresentOnGfxThread","BehaviourUpdate","Animator.Update","GC.Collect"};
        foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);if(!names.Contains(d.Name))continue;var r=ProfilerRecorder.StartNew(d.Category,d.Name,1);if(r.Valid)counters.Add(new Counter{name=d.Name,recorder=r});}
    }
    void LateUpdate(){if(view)view.transform.SetPositionAndRotation(viewPosition,viewRotation);}
    void Update()
    {
        FrameTimingManager.CaptureFrameTimings();
        if(warmup++<10)return;
        intervals.Add(Time.unscaledDeltaTime*1000);batches+=UnityEditor.UnityStats.batches;triangles+=UnityEditor.UnityStats.triangles;
        if(FrameTimingManager.GetLatestTimings(1,timing)>0 && timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuSamples++;}
        foreach(var c in counters){double v=c.recorder.LastValue;c.sum+=v;c.peak=Math.Max(c.peak,v);}
    }
    public object Result(string name)
    {
        var samples=intervals.OrderBy(x=>x).ToArray();int n=samples.Length;if(n==0)throw new Exception("No frames captured.");
        return new {name,frames=n,frameMs=samples.Average(),p95Ms=samples[(int)((n-1)*.95)],batches=batches/n,triangles=triangles/n,gpuMs=gpuSamples>0?gpu/gpuSamples:0,
            camera=Camera.main?Camera.main.transform.position.ToString():"none",
            markers=counters.Select(c=>new{c.name,average=c.sum/n,peak=c.peak}).ToArray()};
    }
    void OnDestroy(){foreach(var c in counters)c.recorder.Dispose();}
}
