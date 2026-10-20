using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
public static class ProfileCrushV220
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Use the existing Editor in Play Mode");
        string folder=".codex/encounter-redesign-20261017/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve previous profile");
        Directory.CreateDirectory(folder);
        var observer=new GameObject("Crush detailed profile v220").AddComponent<CrushProfileV220>();observer.folder=folder;
        return new {started=true,label,captures=false};
    }
}
[DefaultExecutionOrder(31000)]
public sealed class CrushProfileV220:MonoBehaviour
{
    public string folder;
    readonly List<object> rows=new List<object>();
    readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
    readonly string[] names={"Physics.Simulate","Physics.Processing","FixedUpdate.ScriptRunBehaviourFixedUpdate","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","GC.Collect","GC Allocated In Frame","Camera.Render","RenderPipelineManager.DoRenderLoop_Internal","WaitForTargetFPS","PlayerLoop","Story.WreckBuoyancy","Wreck.WarmGeometry","Wreck.CrushCreate","Wreck.CrushWarmFlush","Wreck.CrushBodies","Wreck.CrushActivate","Wreck.RegisterRiderContacts"};
    Story1WreckBattle battle;double previous,started;float previousClock;int steps;bool done;
    void Start()
    {
        Time.captureFramerate=0;battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();previous=started=Time.realtimeSinceStartupAsDouble;
        foreach(var n in names.Where(n=>n.StartsWith("Wreck.")||n.StartsWith("Story.")))counters[n]=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,n);
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);if(names.Contains(d.Name)&&!counters.ContainsKey(d.Name))counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name);}
    }
    void FixedUpdate(){steps++;}
    void LateUpdate()
    {
        if(done||battle==null)return;
        double now=Time.realtimeSinceStartupAsDouble;
        if(battle.RedesignActive)rows.Add(new{clock=battle.RedesignClock,previousClock,stage=battle.RedesignStage.ToString(),frameMs=(now-previous)*1000,fixedSteps=steps,rate=Time.timeScale,maximumDeltaTime=Time.maximumDeltaTime,
            completedFrameCounters=counters.Where(p=>p.Value.Valid).ToDictionary(p=>p.Key,p=>p.Value.LastValue),
            battle.RedesignRiggingMaximumTickMs,battle.RedesignRiggingMaximumQueryMs,battle.RedesignRiggingMaximumSkinTreeRefitMs});
        previous=now;previousClock=battle.RedesignClock;steps=0;
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial||now-started>180)
        {
            done=true;
            var bodies=UnityEngine.Object.FindObjectsByType<Story1WreckFloatBody>(FindObjectsSortMode.None);
            File.WriteAllText(folder+"/profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rows,events=battle.RedesignEvents,
                markerUnits="nanoseconds, except GC Allocated In Frame is bytes; counters refer to last completed frame",
                bodies=bodies.Length,colliders=bodies.Sum(b=>b.GetComponentsInChildren<Collider>().Length),
                stage=battle.RedesignStage.ToString()},Newtonsoft.Json.Formatting.Indented));
            EditorApplication.isPlaying=false;
        }
    }
    void OnDestroy(){foreach(var p in counters)p.Value.Dispose();}
}
