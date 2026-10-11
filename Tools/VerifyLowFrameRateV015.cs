using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
[DefaultExecutionOrder(-20000)]
public sealed class V015LowFrameStall : MonoBehaviour
{
    public int frames;public float elapsed;
    void Update()
    {
        if(Time.deltaTime>.01f && Time.deltaTime<.3f){frames++;elapsed+=Time.deltaTime;}
        // Native Update, Animator, Physics and LateUpdate still run normally.
        // A bounded main-thread stall produces real low-rate time steps.
        System.Threading.Thread.Sleep(55);
    }
}
public static class VerifyLowFrameRateV015
{
    public static async Task<object> Verify(string revision="v015-low-frame-rate")
    {
        var routine=AppDomain.CurrentDomain.GetAssemblies().Reverse().Select(a=>a.GetType("VerifyEncounterV006")).FirstOrDefault(t=>t!=null);
        if(routine==null)throw new Exception("Run the current normal encounter verifier first.");
        int rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;float maximum=Time.maximumDeltaTime;
        var go=new GameObject("Native low-frame-rate regression");UnityEngine.Object.DontDestroyOnLoad(go);var stall=go.AddComponent<V015LowFrameStall>();
        try
        {
            Application.targetFrameRate=15;QualitySettings.vSyncCount=0;Time.maximumDeltaTime=.2f;
            var task=(Task<object>)routine.GetMethod("Verify").Invoke(null,new object[]{false,true,false,false,true,revision});
            object encounter=await task;
            float mean=stall.elapsed/Mathf.Max(1,stall.frames);
            if(stall.frames<100 || mean<.05f)throw new Exception("Fixture did not actually produce low-frame-rate native simulation.");
            var result=new{nativeFrames=stall.frames,meanFrameSeconds=mean,meanFramesPerSecond=1/mean,encounter};
            System.IO.File.WriteAllText(".codex/encounter-"+revision+"/low-frame-rate-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            string folder=".codex/encounter-"+revision;System.IO.Directory.CreateDirectory(folder);
            float mean=stall.elapsed/Mathf.Max(1,stall.frames);
            System.IO.File.WriteAllText(folder+"/low-frame-rate-sampling.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{nativeFrames=stall.frames,meanFrameSeconds=mean,meanFramesPerSecond=mean>0 ? 1/mean : 0},Newtonsoft.Json.Formatting.Indented));
            UnityEngine.Object.DestroyImmediate(go);Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;Time.maximumDeltaTime=maximum;
        }
    }
}
