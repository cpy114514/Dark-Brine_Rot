using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Profiling;
using Mavis;
using Object=UnityEngine.Object;

public static class ProfileEcology074
{
    const string W=".codex/island-ecology074";
    static void Json(string name,object value) => File.WriteAllText(W+"/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    public static object Prepare()
    {
        if(Application.isPlaying)throw new Exception("Stopped editor required");
        var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Dirty scene must be preserved first");
        Directory.CreateDirectory(W+"/save-before");
        if(!File.Exists(W+"/setup.json"))File.WriteAllText(W+"/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));
        foreach(var f in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*"))
            if(!File.Exists(W+"/save-before/"+Path.GetFileName(f)))File.Copy(f,W+"/save-before/"+Path.GetFileName(f));
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Single);
        EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);
        return new{prepared=true,persistentPath=Application.persistentDataPath};
    }
    public static object Restore()
    {
        if(Application.isPlaying)throw new Exception("Stop owned test first");
        foreach(var f in Directory.GetFiles(W+"/save-before"))
            if(!File.ReadAllBytes(f).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(f)))))throw new Exception("User save changed");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"/setup.json")));
        return new{restored=true,savesUnchanged=true};
    }
    static async Task Frame()
    {
        int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(30);
        while(f==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Frame stalled");await Task.Delay(1);}
    }
    public static object Start(string label="baseline")
    {
        if(!Application.isPlaying||SceneManager.sceneCount!=2)throw new Exception("Isolated Environment/Lighting Play required");
        if(File.Exists(W+"/profile-running"))throw new Exception("Already profiling");
        File.WriteAllText(W+"/profile-running",label);_ = Run(label);return new{started=true,label};
    }
    static async Task Run(string label)
    {
        var original=QualitySettings.renderPipeline;var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        int rate=Application.targetFrameRate,sync=QualitySettings.vSyncCount;GameObject root=null;RenderTexture rt=null;UniversalRenderPipelineAsset pipeline=null;
        try
        {
            Pixel360Rendering.ReleaseCurrent();pipeline=Object.Instantiate(source);pipeline.renderScale=1;QualitySettings.renderPipeline=pipeline;
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            root=new GameObject("Ecology074 1080p measurement",typeof(Camera));root.tag="MainCamera";var c=root.GetComponent<Camera>();c.fieldOfView=57;c.farClipPlane=1500;c.nearClipPlane=.15f;
            c.transform.SetPositionAndRotation(new Vector3(253,54.25f,496),Quaternion.LookRotation(new Vector3(211,44.06f,535)-new Vector3(253,54.25f,496)));
            c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            rt=new RenderTexture(1920,1080,24);rt.Create();c.targetTexture=rt;
            var end=DateTime.UtcNow.AddSeconds(120);
            while(!Object.FindObjectsByType<NaturalGrassScatter>(FindObjectsSortMode.None).All(s=>s.IsReady)||!Object.FindObjectsByType<WorldPerformanceManager>(FindObjectsSortMode.None).All(s=>s.IsReady))
            {if(DateTime.UtcNow>end)throw new Exception("Foliage initialization timeout");await Frame();}
            for(int i=0;i<90;i++)await Frame();
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1))
            using(var tris=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1))
            using(var mem=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1))
            using(var cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
            {
                var frames=new List<double>();var dc=new List<long>();var tc=new List<long>();var mc=new List<long>();var cc=new List<long>();
                for(int i=0;i<240;i++){await Frame();frames.Add(Time.unscaledDeltaTime*1000);dc.Add(draws.LastValue);tc.Add(tris.LastValue);mc.Add(mem.LastValue);cc.Add(cpu.LastValue);}
                var sorted=frames.OrderBy(x=>x).ToArray();
                Json("performance-"+label,new{scope="Unity Editor Play, isolated actual Environment + Lighting, fixed 1920x1080 RT, renderScale=1, vsync off, 90 warmup + 240 measured frames; editor other views may contribute render counters; no standalone GPU/FPS guarantee",hardware=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,samples=frames.Count,meanMs=frames.Average(),medianMs=sorted[120],p95Ms=sorted[228],maxMs=frames.Max(),fps=1000/frames.Average(),drawCalls=dc.Average(),triangles=tc.Average(),usedMemoryMiB=mc.Average()/1048576,cpuMainMs=cc.Average()/1000000,valid=new{draws=draws.Valid,triangles=tris.Valid,memory=mem.Valid,cpu=cpu.Valid},grass=Object.FindObjectsByType<NaturalGrassScatter>(FindObjectsSortMode.None).Select(s=>new{s.InstanceCount,s.VisibleBatches,s.IsReady}).ToArray()});
            }
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(W+"/profile-"+label+".png",image.EncodeToPNG());Object.DestroyImmediate(image);RenderTexture.active=previous;
        }
        catch(Exception e){File.WriteAllText(W+"/profile-"+label+"-error.txt",e.ToString());Debug.LogException(e);}
        finally
        {
            QualitySettings.renderPipeline=original;Application.targetFrameRate=rate;QualitySettings.vSyncCount=sync;
            if(root)Object.DestroyImmediate(root);if(rt){rt.Release();Object.DestroyImmediate(rt);}if(pipeline)Object.DestroyImmediate(pipeline);
            File.Delete(W+"/profile-running");EditorApplication.isPlaying=false;
        }
    }
}
