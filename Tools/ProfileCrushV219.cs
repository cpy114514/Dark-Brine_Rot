using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class ProfileCrushV219
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Use the existing Editor in Play Mode");
        string folder=".codex/encounter-redesign-20261017/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve previous profile");
        Directory.CreateDirectory(folder);
        var go=new GameObject("Crush realtime profile v219");
        var observer=go.AddComponent<CrushProfileV219>();observer.folder=folder;
        return new {started=true,label,captures=false,fixedCaptureRate=false};
    }
}
[DefaultExecutionOrder(31000)]
public sealed class CrushProfileV219:MonoBehaviour
{
    public string folder;
    readonly List<object> rows=new List<object>();
    Story1WreckBattle battle;double previous,started;bool done;
    void Start(){Time.captureFramerate=0;battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();previous=started=Time.realtimeSinceStartupAsDouble;}
    void LateUpdate()
    {
        if(done||battle==null)return;
        double now=Time.realtimeSinceStartupAsDouble;
        if(battle.RedesignActive)rows.Add(new{battle.RedesignClock,stage=battle.RedesignStage.ToString(),frameMs=(now-previous)*1000,rate=Time.timeScale,battle.RedesignRiggingPrepareMs,battle.RedesignRiggingMaximumTickMs,battle.RedesignRiggingMaximumGeometryMs,battle.RedesignRiggingMaximumQueryMs,battle.RedesignRiggingMaximumSkinTreeBuildMs,battle.RedesignRiggingMaximumSkinTreeRefitMs,battle.RedesignRiggingMaximumReplaceMs,battle.RedesignRiggingReleasedCount});
        previous=now;
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial||now-started>180)
        {done=true;File.WriteAllText(folder+"/profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {rows,stage=battle.RedesignStage.ToString(),events=battle.RedesignEvents},Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;}
    }
}
