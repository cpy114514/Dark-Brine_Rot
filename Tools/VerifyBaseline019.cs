using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;

// Exercises the opt-in sampler in an unsavable native scene, without loading Gameplay.
public static class VerifyBaseline019
{
    const string Root=".codex/performance019";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,Flags).SetValue(o,value);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
    static async Task Frame(){int f=Time.frameCount;var end=DateTime.UtcNow.AddSeconds(20);while(f==Time.frameCount){if(!Application.isPlaying||DateTime.UtcNow>end)throw new Exception("Native frame timeout");await Task.Delay(5);}}
    public static object Prepare()
    {
        if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stopped unowned Editor required");
        var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Keep dirty scenes");
        Directory.CreateDirectory(Root+"/save-before");foreach(string n in new[]{"dark-brine-rot-save.json","dark-brine-rot-save.json.bak"})if(File.Exists(Path.Combine(Application.persistentDataPath,n)))File.Copy(Path.Combine(Application.persistentDataPath,n),Root+"/save-before/"+n,false);
        File.WriteAllText(Root+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));EditorSceneManager.OpenScene("Assets/Scenes/Combat Test/CombatTest.unity",OpenSceneMode.Single);return new{prepared=true};
    }
    public static object Restore()
    {
        if(Application.isPlaying||File.Exists(Root+"/running"))throw new Exception("Stop owned fixture first");
        foreach(string p in Directory.GetFiles(Root+"/save-before"))if(!File.ReadAllBytes(p).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(p)))))throw new Exception("Save changed; never overwrite");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/workspace-before.json")));return new{restored=true,savesUnchanged=true};
    }
    public static object Start()
    {
        if(!Application.isPlaying||SceneManager.sceneCount!=1||SceneManager.GetActiveScene().name!="CombatTest"||File.Exists(Root+"/running"))throw new Exception("Prepared isolated CombatTest required");
        File.WriteAllText(Root+"/running","native");_=Run();return new{started=true};
    }
    static async Task Run()
    {
        checks.Clear();bool passed=false;GameObject holder=null,unrelated=null;IslandPerformanceBaseline baseline=null;
        Camera camera=Camera.main;var oldTarget=camera?camera.targetTexture:null;bool oldEnabled=camera&&camera.enabled;bool oldBackground=Application.runInBackground;
        RenderTexture priorTarget=null;WorldPerformanceManager world=null;IList foliage=null;object added=null;bool oldRefreshing=false;
        try
        {
            Check(camera,"Actual arena camera exists");
            for(int i=0;i<8;i++)await Frame();
            world=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(w=>w.gameObject.scene.IsValid());
            Check(!UnityEngine.Object.FindFirstObjectByType<WorldPerformanceManager>(),"Original ordinary search omits the installed DontSave manager");
            var found=(WorldPerformanceManager)typeof(IslandPerformanceBaseline).GetMethod("FindRuntimePerformance",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Check(found==world,"New runtime lookup resolves that actual hidden manager");
            priorTarget=new RenderTexture(640,360,24);priorTarget.Create();camera.targetTexture=priorTarget;
            holder=new GameObject("Owned baseline019 verifier");baseline=holder.AddComponent<IslandPerformanceBaseline>();baseline.enabled=false; // Awake subscribes; prevent its automatic scene-loading Start.
            Set(baseline,"output",Path.GetFullPath(Root+"/timeout-control.json"));var target=new RenderTexture(640,360,24);target.Create();Set(baseline,"captureTarget",target);
            var report=(IslandPerformanceBaseline.Report)Get(baseline,"report");
            Check(report.samples.Count==0,"Sampler starts without accepted measurements");
            for(int i=0;i<8;i++){Call(baseline,"LateUpdate");await Frame();}
            Check(report.renderedFrames>=6,"Real endCameraRendering callbacks record rendered target frames");
            Check(camera.targetTexture==target,"Sampler binds its owned camera render target");
            Set(baseline,"phase","coast");Set(baseline,"measuring",true);
            for(int i=0;i<8;i++){Call(baseline,"LateUpdate");await Frame();}
            Check(report.samples.Count>=6,"Only preceding rendered intervals become samples");
            int before=report.samples.Count,missing=report.unrenderedIntervals;camera.enabled=false;await Frame();await Frame();Call(baseline,"LateUpdate");
            Check(report.samples.Count==before&&report.unrenderedIntervals==missing+1,"A nonrendered camera interval is rejected rather than reported as FPS");camera.enabled=true;Set(baseline,"measuring",false);
            unrelated=new GameObject("Unrelated camera",typeof(Camera));unrelated.GetComponent<Camera>().enabled=false;
            int frames=report.renderedFrames;Call(baseline,"CameraRendered",default(ScriptableRenderContext),unrelated.GetComponent<Camera>());
            Check(report.renderedFrames==frames,"Other cameras cannot qualify the measured camera");
            camera.targetTexture=priorTarget;Call(baseline,"CameraRendered",default(ScriptableRenderContext),camera);
            Check(report.renderedFrames==frames,"A callback using a different target is rejected");camera.targetTexture=target;
            var proof=(IEnumerator)Call(baseline,"VerifyRenderedView","native-probe");var limit=DateTime.UtcNow.AddSeconds(30);
            while(proof.MoveNext()){if(DateTime.UtcNow>limit)throw new Exception("Proof did not finish");await Frame();}
            Check(report.renderProofs.Count==1&&report.renderProofs[0].changedPixels>640*360/20,"Actual world-versus-mask-zero pixel control qualifies the rendered scene");
            Check(camera.cullingMask!=0,"Positive control restores the original camera mask");
            Check(!(bool)Get(baseline,"finished"),"Valid render proof leaves the measurement healthy");
            var noGc=IslandPerformanceBaseline.Summarize("x",new List<IslandPerformanceBaseline.Sample>{new IslandPerformanceBaseline.Sample{phase="x",ms=20,gcBytes=-1}});
            Check(noGc.maxGcBytes==-1,"Unavailable GC measurements remain unavailable instead of zero bytes");
            var known=IslandPerformanceBaseline.Summarize("x",new List<IslandPerformanceBaseline.Sample>{new IslandPerformanceBaseline.Sample{phase="x",ms=10,gcBytes=4},new IslandPerformanceBaseline.Sample{phase="x",ms=20,gcBytes=8}});
            Check(known.frames==2&&known.averageMs==15&&known.maxGcBytes==8,"Known frame summaries retain their existing arithmetic");
            foliage=(IList)Get(world,"foliage");oldRefreshing=(bool)Get(world,"foliageRefreshing");
            if(foliage.Count==0){added=Activator.CreateInstance(typeof(WorldPerformanceManager).GetNestedType("Foliage",BindingFlags.NonPublic),true);foliage.Add(added);}
            Set(world,"foliageRefreshing",false);Check(world.IsReady,"Readiness fixture has a complete nonempty snapshot");
            var ready=(IEnumerator)Call(baseline,"WaitForPerformance",Time.realtimeSinceStartup+3);Check(!ready.MoveNext()&&!(bool)Get(baseline,"finished"),"Ready manager passes without delay");
            Set(world,"foliageRefreshing",true);var waiting=(IEnumerator)Call(baseline,"WaitForPerformance",Time.realtimeSinceStartup+3);
            Check(waiting.MoveNext()&&!(bool)Get(baseline,"finished"),"Incomplete manager actually waits");Set(world,"foliageRefreshing",false);
            Check(!waiting.MoveNext()&&!(bool)Get(baseline,"finished"),"Completion releases the readiness wait");
            Set(world,"foliageRefreshing",true);var timeout=(IEnumerator)Call(baseline,"WaitForPerformance",Time.realtimeSinceStartup-1);Check(!timeout.MoveNext()&&report.status=="foliage initialization timeout","Expired incomplete initialization fails closed");
            Set(world,"foliageRefreshing",oldRefreshing);if(added!=null){foliage.Remove(added);added=null;}
            camera.enabled=false;UnityEngine.Object.DestroyImmediate(baseline);baseline=null;
            Check(camera.targetTexture==priorTarget&&!camera.enabled,"Teardown restores the prior target without enabling a camera");
            Check(Application.runInBackground==oldBackground,"Teardown restores run-in-background state");passed=true;
        }
        catch(Exception e){File.WriteAllText(Root+"/error.txt",e.ToString());}
        finally
        {
            if(world){Set(world,"foliageRefreshing",oldRefreshing);if(added!=null)foliage.Remove(added);}
            if(baseline)UnityEngine.Object.DestroyImmediate(baseline);if(holder)UnityEngine.Object.DestroyImmediate(holder);if(unrelated)UnityEngine.Object.DestroyImmediate(unrelated);
            if(camera){camera.targetTexture=oldTarget;camera.enabled=oldEnabled;}if(priorTarget){priorTarget.Release();UnityEngine.Object.DestroyImmediate(priorTarget);}Application.runInBackground=oldBackground;
            File.WriteAllText(Root+"/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,checks,scope="Native opt-in benchmark instrumentation tests; no game FPS/GC performance conclusion"},Newtonsoft.Json.Formatting.Indented));File.Delete(Root+"/running");EditorApplication.isPlaying=false;
        }
    }
}
