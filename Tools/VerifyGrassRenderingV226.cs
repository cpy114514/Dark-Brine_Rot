using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Mavis;
// Compare normal rendered frames after unrelated LOD/cache work has settled.
public static class VerifyGrassRenderingV226
{
    const string Root=".codex/whole-game-performance-20261019";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public class Pose {public string name;public float[] p,q;}
    public static async Task<object> Run()
    {
        if(!Application.isPlaying||!UnityEditor.EditorApplication.isPaused)throw new Exception("Pause a loaded world first");
        var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m=>m.gameObject.scene.IsValid());
        var driver=manager.GetComponents<MonoBehaviour>().Single(x=>x.GetType().Name=="GrassRenderSubmission");
        var observer=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(x=>x.GetType().Name=="WorldSampleV226");
        var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;
        var next=typeof(WorldPerformanceManager).GetField("nextUpdate",Flags);float oldNext=(float)next.GetValue(manager);
        var refresh=typeof(WorldPerformanceManager).GetMethod("RefreshFoliage",Flags);
        var running=typeof(WorldPerformanceManager).GetField("foliageRefreshing",Flags);
        bool oldDriver=driver.enabled,oldObserver=observer.enabled;float scale=Time.timeScale;
        var entries=((System.Collections.IEnumerable)typeof(WorldPerformanceManager).GetField("foliage",Flags).GetValue(manager)).Cast<object>().Where(e=>(bool)e.GetType().GetField("batchable").GetValue(e)&&!(bool)e.GetType().GetField("forced").GetValue(e)&&!(bool)e.GetType().GetField("duplicate").GetValue(e)).ToArray();
        var renderers=entries.Select(e=>(MeshRenderer)e.GetType().GetField("renderer").GetValue(e)).ToArray();
        var originals=entries.Select(e=>(Mesh)e.GetType().GetField("original").GetValue(e)).ToArray();
        var states=renderers.Select(r=>r.forceRenderingOff).ToArray();var meshes=renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).ToArray();
        var poses=Newtonsoft.Json.JsonConvert.DeserializeObject<Pose[]>(File.ReadAllText(Path.Combine(Root,"poses.json")));
        var views=new List<(string name,Vector3 p,Quaternion q)>();
        foreach(var p in poses.Take(3))views.Add((p.name,new Vector3(p.p[0],p.p[1],p.p[2]),new Quaternion(p.q[0],p.q[1],p.q[2],p.q[3])));
        for(int i=0;i<8;i++)views.Add(("turn-"+i,views[0].p,Quaternion.Euler(-10,i*45,0)));
        views.Add(("second-camera",views[0].p,views[0].q));
        var owned=new List<GameObject>();var reports=new List<object>();var folder=Path.Combine(Root,"render-equivalence");Directory.CreateDirectory(folder);
        try
        {
            observer.enabled=false;next.SetValue(manager,float.PositiveInfinity);Time.timeScale=0;UnityEditor.EditorApplication.isPaused=false;
            await Idle(manager,running);
            double worst=0;int maximum=0;
            for(int k=0;k<views.Count;k++)
            {
                var v=views[k];cam.transform.SetPositionAndRotation(v.name=="second-camera"?views[1].p:v.p,v.name=="second-camera"?views[1].q:v.q);
                refresh.Invoke(manager,new object[]{cam.transform.position});await Idle(manager,running);
                var go=UnityEngine.Object.Instantiate(cam.gameObject);owned.Add(go);go.name="Temporary grass verification "+v.name;go.tag="Untagged";
                foreach(var b in go.GetComponentsInChildren<MonoBehaviour>())if(!(b is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))b.enabled=false;
                foreach(var listener in go.GetComponentsInChildren<AudioListener>())listener.enabled=false;
                var camera=go.GetComponent<Camera>();camera.enabled=false;camera.transform.SetPositionAndRotation(v.p,v.q);
                driver.enabled=false;
                for(int i=0;i<renderers.Length;i++){renderers[i].forceRenderingOff=false;renderers[i].GetComponent<MeshFilter>().sharedMesh=originals[i];}
                var expected=await Pixels(camera);
                for(int i=0;i<renderers.Length;i++){renderers[i].forceRenderingOff=states[i];renderers[i].GetComponent<MeshFilter>().sharedMesh=meshes[i];}
                driver.enabled=oldDriver;var actual=await Pixels(camera);
                int max=0,changed=0;long sum=0;
                for(int i=0;i<actual.Length;i++){int d=Math.Max(Math.Abs(actual[i].r-expected[i].r),Math.Max(Math.Abs(actual[i].g-expected[i].g),Math.Abs(actual[i].b-expected[i].b)));max=Math.Max(max,d);sum+=d;if(d>1)changed++;}
                double mean=sum/(double)actual.Length;worst=Math.Max(worst,mean);maximum=Math.Max(maximum,max);reports.Add(new{v.name,maxError=max,meanChannelError=mean,pixelsOverOne=changed});
                if(k<3||v.name=="second-camera"){Write(Path.Combine(folder,v.name+"-native.png"),expected);Write(Path.Combine(folder,v.name+"-instanced.png"),actual);}
            }
            var result=new{passed=worst<.1,views=views.Count,renderers=renderers.Length,worstMean=worst,maxError=maximum,reports};File.WriteAllText(Path.Combine(folder,"result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));if(result.passed)File.WriteAllText(Path.Combine(Root,"qa-complete.txt"),"Passed");return result;
        }
        finally
        {
            for(int i=0;i<renderers.Length;i++)if(renderers[i]){renderers[i].forceRenderingOff=states[i];renderers[i].GetComponent<MeshFilter>().sharedMesh=meshes[i];}
            foreach(var go in owned)UnityEngine.Object.DestroyImmediate(go);
            driver.enabled=oldDriver;observer.enabled=oldObserver;next.SetValue(manager,oldNext);cam.transform.SetPositionAndRotation(pos,rot);Time.timeScale=scale;UnityEditor.EditorApplication.isPaused=true;
        }
    }
    static async Task Idle(WorldPerformanceManager manager,FieldInfo running){var limit=DateTime.UtcNow.AddSeconds(20);while((bool)running.GetValue(manager)){if(DateTime.UtcNow>limit)throw new Exception("Refresh did not finish");await Task.Delay(5);}}
    static async Task<Color32[]> Pixels(Camera cam)
    {
        var rt=RenderTexture.GetTemporary(640,360,24);var active=RenderTexture.active;var target=cam.targetTexture;var tex=new Texture2D(640,360,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.enabled=true;int until=Time.frameCount+4;while(Time.frameCount<until)await Task.Delay(5);RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,640,360),0,0);tex.Apply();return tex.GetPixels32();}finally{cam.enabled=false;cam.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    static void Write(string path,Color32[] pixels){var tex=new Texture2D(640,360,TextureFormat.RGB24,false);try{tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(tex);}}
}
