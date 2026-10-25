using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditorInternal;
using Mavis;

// Editor-only experiment. All scene changes are confined to Play Mode.
public static class ProfileWorldV226
{
    public const string Root=".codex/whole-game-performance-20261019";
    public static object Start(string label="before")
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        if(UnityEngine.Object.FindAnyObjectByType<WorldSampleV226>())throw new Exception("Already sampling");
        var go=new GameObject("Temporary world benchmark v226");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var run=go.AddComponent<WorldSampleV226>();run.label=label;
        UnityEditor.EditorApplication.isPaused=false;
        return label;
    }
}
[DefaultExecutionOrder(15000)]
public sealed class WorldSampleV226:MonoBehaviour
{
    public string label;
    Camera view;Vector3 pos;Quaternion rot;
    bool measuring;int rate,sync;
    bool oldProfiler;int oldCapture;float oldScale;
    readonly List<double> frame=new List<double>(4096);
    readonly FrameTiming[] timings=new FrameTiming[1];
    readonly Dictionary<string,ProfilerRecorder> rec=new Dictionary<string,ProfilerRecorder>();
    readonly Dictionary<string,List<double>> values=new Dictionary<string,List<double>>();
    double batches,tris,gpu;int gpuN,gpuRejected;
    string folder;
    async void Start()
    {
        rate=Application.targetFrameRate;sync=QualitySettings.vSyncCount;oldCapture=Time.captureFramerate;oldProfiler=ProfilerDriver.enabled;
        oldScale=Time.timeScale;Time.timeScale=label.StartsWith("frozen")?0:1;
        ProfilerDriver.enabled=false;ProfilerDriver.ClearAllFrames();
        folder=Path.Combine(ProfileWorldV226.Root,label);Directory.CreateDirectory(folder);
        try
        {
            // Keep the in-memory authoring scene loaded and untouched in Edit Mode.
            var initial=SceneManager.GetActiveScene();
            foreach(var root in initial.GetRootGameObjects())if(root!=gameObject)root.SetActive(false);
            GameSaveManager.CancelPendingContinue();UnityEngine.Random.InitState(226);
            var op=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Main.unity",LoadSceneMode.Additive);
            while(!op.isDone)await Task.Delay(30);
            for(int i=0;i<1000 && (AdditiveSceneBootstrap.IsLoading || !SceneManager.GetSceneByName("Enemies").isLoaded);i++)await Task.Delay(30);
            await Task.Delay(2500);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(!player)throw new Exception("Missing player");
            player.gameObject.AddComponent<GameSaveExcluded>();
            var arrival=player.GetComponent<FirstIslandArrival>();if(arrival){arrival.RestoreProgress(true,true);arrival.enabled=false;}
            player.ExternalControlLock=true;player.enabled=false;
            var camera=Camera.main;if(!camera)throw new Exception("Missing camera");
            var copy=UnityEngine.Object.Instantiate(camera.gameObject);copy.name="Benchmark fixed camera v226";
            foreach(var b in copy.GetComponentsInChildren<MonoBehaviour>())if(!(b is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))b.enabled=false;
            foreach(var a in copy.GetComponentsInChildren<AudioListener>())a.enabled=false;
            camera.enabled=false;camera.tag="Untagged";
            view=copy.GetComponent<Camera>();view.enabled=true;view.tag="MainCamera";
            Time.captureFramerate=0;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            await Task.Delay(3000);
            // Runtime manager has DontSave, which ordinary FindObjectsByType omits.
            var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().FirstOrDefault(x=>x.gameObject.scene.IsValid()&&x.gameObject.scene.isLoaded);
            var routine=typeof(WorldPerformanceManager).GetField("cacheRoutine",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            for(int i=0;i<1600&&manager&&routine.GetValue(manager)!=null;i++)await Task.Delay(30);
            if(manager&&routine.GetValue(manager)!=null)throw new Exception("World cache failed to settle");
            if(!manager)throw new Exception("World Performance Manager not installed");
            var posesFile=Path.Combine(ProfileWorldV226.Root,"poses.json");
            ViewPose[] poses;
            if(File.Exists(posesFile)&&File.ReadAllText(posesFile).TrimStart().StartsWith("["))poses=Newtonsoft.Json.JsonConvert.DeserializeObject<ViewPose[]>(File.ReadAllText(posesFile));
            else
            {
                var island=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).First(x=>x.isActiveAndEnabled);
                // Published October 6 forest viewpoint, independent of renderer naming/culling.
                Vector3 c=new Vector3(15.6f,0,777.5f);float y=island.GetWorldSurfaceHeight(c);
                poses=new[]{new ViewPose("shore",view.transform.position,view.transform.rotation),new ViewPose("forest",new Vector3(c.x-8,y+5,c.z-15),Quaternion.LookRotation(new Vector3(8,-2,37))),new ViewPose("panorama",new Vector3(c.x-35,y+60,c.z-80),Quaternion.LookRotation(new Vector3(35,-60,160))),new ViewPose("ocean",new Vector3(900,6,900),Quaternion.LookRotation(new Vector3(1,-.07f,1)))};
                File.WriteAllText(posesFile,Newtonsoft.Json.JsonConvert.SerializeObject(poses,Newtonsoft.Json.Formatting.Indented));
            }
            var reports=new List<object>();
            foreach(var p in label.StartsWith("qa")||label.Contains("fast")?Array.Empty<ViewPose>():poses)
            {
                pos=p.position;rot=p.rotation;view.transform.SetPositionAndRotation(pos,rot);
                await Task.Delay(1600);
                reports.Add(await Sample(p.name));
                Dump(p.name,manager);
                await Picture(p.name);
                if(label.StartsWith("probe")){UnityEditor.EditorApplication.isPaused=true;await Task.Delay(50000);return;}
            }
            if(!label.StartsWith("qa")&&!label.StartsWith("frozen"))
            {
                var map=player.GetComponent<IslandMapUI>();map.SetOpen(true);await Task.Delay(400);reports.Add(await Sample("map"));await Picture("map");map.SetOpen(false);
                pos=poses[0].position;rot=poses[0].rotation;await Task.Delay(1000);reports.Add(await Sample("shore-repeat"));Dump("shore-repeat",manager);
            }
            else {pos=poses[0].position;rot=poses[0].rotation;view.transform.SetPositionAndRotation(pos,rot);}
            if(!label.StartsWith("frozen"))await Lifecycle(manager);
            if(label.Contains("review"))
            {
                UnityEditor.EditorApplication.isPaused=true;
                string done=Path.Combine(ProfileWorldV226.Root,"qa-complete.txt");if(File.Exists(done))File.Delete(done);
                File.WriteAllText(Path.Combine(folder,"ready-for-qa.txt"),"Paused for native/instanced comparison");
                var deadline=DateTime.UtcNow.AddMinutes(4);while(!File.Exists(done)&&DateTime.UtcNow<deadline)await Task.Delay(100);
                UnityEditor.EditorApplication.isPaused=false;
            }
            var inventory=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var result=new {label,reports,simulationTime=Time.time,timeScale=Time.timeScale,hardware=new{SystemInfo.processorType,SystemInfo.graphicsDeviceName,SystemInfo.systemMemorySize},renderScale=(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)?.renderScale,renderers=inventory.Length,skinned=inventory.OfType<SkinnedMeshRenderer>().Select(s=>new{s.name,vertices=s.sharedMesh?s.sharedMesh.vertexCount:0,s.updateWhenOffscreen,culling=s.GetComponentInParent<Animator>()?.cullingMode.ToString()}).ToArray(),behaviours=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(x=>x.enabled).GroupBy(x=>x.GetType().Name).OrderByDescending(g=>g.Count()).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),memory=Profiler.GetTotalAllocatedMemoryLong()};
            File.WriteAllText(Path.Combine(folder,"result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(Path.Combine(folder,"error.txt"),e.ToString());Debug.LogException(e);}
        finally
        {
            measuring=false;DisposeRecorders();ProfilerDriver.enabled=oldProfiler;Time.timeScale=oldScale;Time.captureFramerate=oldCapture;Application.targetFrameRate=rate;QualitySettings.vSyncCount=sync;
            UnityEditor.EditorApplication.isPlaying=false;
        }
    }
    public class ViewPose {public string name;public float[] p,q;[Newtonsoft.Json.JsonIgnore] public Vector3 position=>new Vector3(p[0],p[1],p[2]);[Newtonsoft.Json.JsonIgnore] public Quaternion rotation=>new Quaternion(q[0],q[1],q[2],q[3]);public ViewPose(){} public ViewPose(string n,Vector3 v,Quaternion r){name=n;p=new[]{v.x,v.y,v.z};q=new[]{r.x,r.y,r.z,r.w};}}
    async Task<object> Sample(string name)
    {
        if(label.Contains("allocation")&&name=="shore-repeat")
        {ProfilerDriver.enabled=true;ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU,true);ProfilerDriver.ClearAllFrames();}
        Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;frame.Clear();batches=tris=gpu=0;gpuN=gpuRejected=0;
        string[] names={"Main Thread","Render Thread","PlayerLoop","GC Allocated In Frame","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","Physics.Simulate","RenderPipelineManager.DoRenderLoop_Internal","Gfx.WaitForPresentOnGfxThread","Animator.Update","GC.Collect"};
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);if(!names.Contains(d.Name)||rec.ContainsKey(d.Name))continue;rec[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1);values[d.Name]=new List<double>(4096);}
        measuring=true;await Task.Delay(4500);measuring=false;
        var stats=values.Select(v=>new{name=v.Key,data=Stats(v.Value)}).ToArray();
        var result=new {name,frames=frame.Count,frame=Stats(frame),batches=batches/Math.Max(1,frame.Count),triangles=tris/Math.Max(1,frame.Count),gpuMs=gpuN>0?(double?)(gpu/gpuN):null,gpuSamples=gpuN,gpuRejected,markers=stats};
        DisposeRecorders();
        if(!label.Contains("trace")||(label.Contains("allocation")&&name!="shore-repeat"))return result;
        // Separate diagnostic CPU capture avoids charging Profiler overhead to frame benchmark.
        if(!label.Contains("allocation")){ProfilerDriver.enabled=true;ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU,true);ProfilerDriver.ClearAllFrames();await Task.Delay(1400);}
        else await Task.Delay(100);
        var totals=new Dictionary<string,double>();var allocations=new Dictionary<string,long>();int used=0;
        for(int f=Math.Max(ProfilerDriver.firstFrameIndex,ProfilerDriver.lastFrameIndex-40);f<=ProfilerDriver.lastFrameIndex-2;f++)
        for(int thread=0;thread<(label.Contains("allocation")?64:1);thread++)
        using(var raw=ProfilerDriver.GetRawFrameDataView(f,thread))
        {
            if(!raw.valid)break;if(thread==0)used++;var parents=new Stack<(int end,string name)>();
            for(int i=0;i<raw.sampleCount;i++)
            {
                while(parents.Count>0&&parents.Peek().end<i)parents.Pop();
                string n=raw.GetSampleName(i)??"(unnamed)";
                if(n=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)
                {string parent=raw.threadName+": "+string.Join(" > ",parents.Reverse().Select(p=>p.name));long bytes=raw.GetSampleMetadataAsLong(i,0);allocations.TryGetValue(parent,out long amount);allocations[parent]=amount+bytes;}
                int children=raw.GetSampleChildrenCountRecursive(i);if(children>0)parents.Push((i+children,n));
                double t=raw.GetSampleTimeMs(i);if(t<.005)continue;totals.TryGetValue(n,out double a);totals[n]=a+t;
            }
        }
        ProfilerDriver.enabled=oldProfiler;
        File.WriteAllText(Path.Combine(folder,name+"-cpu.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{frames=used,markers=totals.OrderByDescending(x=>x.Value).Take(100).Select(x=>new{x.Key,ms=x.Value/Math.Max(1,used)}),allocations=allocations.OrderByDescending(x=>x.Value).Take(25).Select(x=>new{path=x.Key,bytesPerFrame=x.Value/Math.Max(1,used)})},Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    static object Stats(List<double> source){var a=source.OrderBy(x=>x).ToArray();return a.Length==0?null:new{average=a.Average(),median=a[a.Length/2],p95=a[(int)((a.Length-1)*.95)],max=a[a.Length-1]};}
    void Update(){if(!measuring)return;frame.Add(Time.unscaledDeltaTime*1000);batches+=UnityEditor.UnityStats.batches;tris+=UnityEditor.UnityStats.triangles;FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0){if(timings[0].gpuFrameTime<1000){gpu+=timings[0].gpuFrameTime;gpuN++;}else gpuRejected++;}foreach(var r in rec)values[r.Key].Add(r.Value.LastValue/(r.Key=="GC Allocated In Frame"?1.0:1000000.0));}
    void LateUpdate(){if(view)view.transform.SetPositionAndRotation(pos==Vector3.zero?view.transform.position:pos,rot==default(Quaternion)?view.transform.rotation:rot);}
    void DisposeRecorders(){foreach(var r in rec.Values)r.Dispose();rec.Clear();values.Clear();}
    void Dump(string name,WorldPerformanceManager manager)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var items=((System.Collections.IEnumerable)typeof(WorldPerformanceManager).GetField("foliage",flags).GetValue(manager)).Cast<object>().ToArray();
        var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(x=>x.enabled&&!x.forceRenderingOff).ToArray();
        var summary=renderers.GroupBy(r=>r.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh?filter.sharedMesh.name:r.name).Select(g=>new{mesh=g.Key,count=g.Count(),visible=g.Count(x=>x.isVisible),materials=g.First().sharedMaterials.Select(x=>x?x.shader.name:"none").ToArray()}).OrderByDescending(g=>g.count);
        var batches=((System.Collections.IEnumerable)typeof(WorldPerformanceManager).GetField("activeGrassBatches",flags).GetValue(manager)).Cast<object>().ToArray();
        File.WriteAllText(Path.Combine(folder,name+"-renderers.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{summary,batches=batches.Length,instances=batches.Sum(b=>(int)b.GetType().GetField("count").GetValue(b)),items=items.Length,unhiddenBatchable=items.Count(o=>(bool)o.GetType().GetField("batchable").GetValue(o)&&!((MeshRenderer)o.GetType().GetField("renderer").GetValue(o)).forceRenderingOff)},Newtonsoft.Json.Formatting.Indented));
    }
    async Task Lifecycle(WorldPerformanceManager manager)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var field=typeof(WorldPerformanceManager).GetField("foliage",flags);var routine=typeof(WorldPerformanceManager).GetField("cacheRoutine",flags);
        var entries=((System.Collections.IEnumerable)field.GetValue(manager)).Cast<object>().ToArray();
        int failures=0;manager.enabled=false;
        foreach(var entry in entries)
        {
            var t=entry.GetType();var r=(MeshRenderer)t.GetField("renderer").GetValue(entry);if(!r)continue;
            if(r.forceRenderingOff!=(bool)t.GetField("forced").GetValue(entry))failures++;
            if(r.shadowCastingMode!=(ShadowCastingMode)t.GetField("shadows").GetValue(entry))failures++;
            if(r.allowOcclusionWhenDynamic!=(bool)t.GetField("occlusion").GetValue(entry))failures++;
            if(!r.sharedMaterials.SequenceEqual((Material[])t.GetField("materials").GetValue(entry)))failures++;
            if(r.GetComponent<MeshFilter>().sharedMesh!=(Mesh)t.GetField("original").GetValue(entry))failures++;
        }
        manager.enabled=true;
        for(int i=0;i<2000&&routine.GetValue(manager)!=null;i++)await Task.Delay(15);
        var refreshing=typeof(WorldPerformanceManager).GetField("foliageRefreshing",flags);
        for(int i=0;i<2000&&(bool)refreshing.GetValue(manager);i++)await Task.Delay(15);
        await Task.Delay(300);
        for(int i=0;i<2000&&((System.Collections.ICollection)typeof(WorldPerformanceManager).GetField("activeGrassBatches",flags).GetValue(manager)).Count==0;i++)await Task.Delay(15);
        var rebuilt=((System.Collections.IEnumerable)field.GetValue(manager)).Cast<object>().ToArray();
        int leaks=rebuilt.Count(o=>(bool)o.GetType().GetField("batchable").GetValue(o)&&!((MeshRenderer)o.GetType().GetField("renderer").GetValue(o)).forceRenderingOff);
        var result=new{passed=failures==0&&leaks==0&&rebuilt.Length==entries.Length,restoredEntries=entries.Length,restorationFailures=failures,rebuiltEntries=rebuilt.Length,unhiddenBatchable=leaks};
        File.WriteAllText(Path.Combine(folder,"lifecycle.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if(!result.passed)throw new Exception("Foliage lifecycle regression");
    }
    async Task Picture(string name){var rt=RenderTexture.GetTemporary(960,540,24);var old=view.targetTexture;var active=RenderTexture.active;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);try{view.targetTexture=rt;int until=Time.frameCount+4;while(Time.frameCount<until)await Task.Delay(5);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());}finally{view.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Destroy(tex);}}
    void OnDestroy(){DisposeRecorders();}
}
