using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;

public static class BenchmarkOpeningCacheV243
{
    public static async Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if(!battle)throw new Exception("Story1 encounter required");
        if(!battle.RedesignActive)battle.GetComponent<Story1SharkCollisionSequence>().BeginImpact();
        for(int i=0;i<1600&&(!battle.TutorialWaiting||battle.CurrentLesson!=Story1WreckBattle.Lesson.Move);i++)await Task.Delay(30);
        if(!battle.TutorialWaiting||battle.CurrentLesson!=Story1WreckBattle.Lesson.Move)throw new Exception("Move gate not reached");
        var go=new GameObject("Temporary exact-cache A B v243"){hideFlags=HideFlags.DontSave};
        var capture=go.AddComponent<OpeningCacheSampleV243>();capture.battle=battle;
        int cap=Application.targetFrameRate,sync=QualitySettings.vSyncCount;
        try
        {
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            await Task.Delay(1200);var reports=new List<object>();
            foreach(bool reference in new[]{true,false,true,false})
            {
                capture.reference=reference;await Task.Delay(400);capture.Begin();await Task.Delay(3500);
                capture.measuring=false;reports.Add(capture.Result());
                await Task.Delay(200);var image=ScreenCapture.CaptureScreenshotAsTexture();
                string imagePath=Root+"/"+(reference?"reference-":"cached-")+reports.Count+".png";
                File.WriteAllBytes(imagePath,image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
            var result=new{reports,scope="Same paused native encounter; reference invalidates result/vertex caches once before each director tick, preserving within-frame reuse. Neither branch reduces exact geometry or changes simulation/input. Caps removed only during this test."};
            File.WriteAllText(Root+"/paired-cache-benchmark.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally{Application.targetFrameRate=cap;QualitySettings.vSyncCount=sync;if(go)UnityEngine.Object.DestroyImmediate(go);}
    }
    public const string Root=".codex/project-performance-20261023";
}
[DefaultExecutionOrder(32000)]public sealed class OpeningCacheSampleV243:MonoBehaviour
{
    public Story1WreckBattle battle;public bool reference,measuring;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<double> frames=new List<double>();readonly Dictionary<string,ProfilerRecorder> recorders=new Dictionary<string,ProfilerRecorder>();readonly Dictionary<string,List<double>> times=new Dictionary<string,List<double>>();
    float[] initial;double largestChange;
    object Field(string n)=>typeof(Story1WreckBattle).GetField(n,Flags).GetValue(battle);
    void Update()
    {
        if(!reference)return;
        typeof(Story1WreckBattle).GetField("redTutorialVerticesFrame",Flags).SetValue(battle,-2);
        foreach(DictionaryEntry item in (IDictionary)Field("redSolidWood"))item.Value.GetType().GetField("poseVersion").SetValue(item.Value,-2);
    }
    float[] State()
    {
        var result=new List<float>();var shark=(Transform)Field("shark");var player=(ThirdPersonPlayerController)Field("player");
        foreach(var root in new[]{shark,player.transform}){var p=root.position;var q=root.rotation;result.AddRange(new[]{p.x,p.y,p.z,q.x,q.y,q.z,q.w});}
        var animator=player.CharacterAnimator;
        foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){var b=animator.GetBoneTransform(bone);var p=b.position;var q=b.rotation;result.AddRange(new[]{p.x,p.y,p.z,q.x,q.y,q.z,q.w});}
        foreach(DictionaryEntry item in (IDictionary)Field("redSolidWood")){var p=((Story1WreckMeshCollision)item.Key).transform.localToWorldMatrix;for(int i=0;i<16;i++)result.Add(p[i]);}
        return result.ToArray();
    }
    public void Begin()
    {
        Dispose();frames.Clear();times.Clear();largestChange=0;initial=State();
        foreach(var n in new[]{"Opening.NativeSolids","Opening.SharkWood","Opening.SharkHero","Main Thread","PlayerLoop","GC Allocated In Frame"}){recorders[n]=ProfilerRecorder.StartNew(n=="GC Allocated In Frame"?ProfilerCategory.Memory:n=="Main Thread"||n=="PlayerLoop"?ProfilerCategory.Internal:ProfilerCategory.Scripts,n,1);times[n]=new List<double>();}
        measuring=true;
    }
    void LateUpdate()
    {
        if(!measuring)return;
        if(!battle.TutorialWaiting||battle.CurrentLesson!=Story1WreckBattle.Lesson.Move||Time.timeScale!=0)throw new Exception("Paused comparison changed; discard run");
        frames.Add(Time.unscaledDeltaTime*1000);
        foreach(var p in recorders)times[p.Key].Add(p.Value.LastValue/(p.Key=="GC Allocated In Frame"?1.0:1000000.0));
        // Check the actual final transforms without forcing any reference pose.
        if(frames.Count%15==0){var now=State();if(now.Length!=initial.Length)largestChange=double.PositiveInfinity;else for(int i=0;i<now.Length;i++)largestChange=Math.Max(largestChange,Math.Abs(now[i]-initial[i]));}
    }
    static object Stats(List<double> data){var a=data.OrderBy(x=>x).ToArray();return new{mean=a.Average(),median=a[a.Length/2],p95=a[(int)((a.Length-1)*.95)],max=a[a.Length-1]};}
    public object Result()=>new{reference,frames=frames.Count,frameMs=Stats(frames),largestTransformComponentChange=largestChange,finalState=State(),markers=times.Select(p=>new{p.Key,stats=Stats(p.Value)}).ToArray()};
    void Dispose(){foreach(var r in recorders.Values)r.Dispose();recorders.Clear();}
    void OnDestroy()=>Dispose();
}
