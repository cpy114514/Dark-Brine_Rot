using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

[DefaultExecutionOrder(32700)]
public sealed class SolidObserverV093:MonoBehaviour
{
    public string output;
    readonly string[] names={"SolidContact","SolidSweep","SolidEscape","ClearSearch","RetainedPlacement","UpdateVolumes","SolidOverlap","SolvedDamage","ResidualPenetration"};
    ProfilerRecorder[] costs;
    readonly List<object> samples=new List<object>();
    bool enteredFilm;
    void Awake(){costs=names.Select(n=>ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck."+n)).ToArray();}
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();
        if(battle==null)return;
        var film=battle.Finale;var solid=battle.WreckContact;
        if(film!=null)enteredFilm=true;
        samples.Add(new{frame=Time.frameCount,clock=film!=null?film.CinematicTime:-999,
            phase=battle.CurrentPhase.ToString(),ms=costs.Select(c=>c.Valid?c.LastValue/1000000d:0).ToArray(),
            correction=solid!=null?solid.LastCorrection.magnitude:0,
            holds=solid!=null?solid.ConstraintHoldCount:0,escapes=solid!=null?solid.LargeEscapeCount:0});
        if(enteredFilm && battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        foreach(var cost in costs)if(cost.Valid)cost.Dispose();
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{readOnly=true,editorDiagnostic=true,names,samples},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObserveSolidV093
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only solid profiler v093");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<SolidObserverV093>().output=output;return new{started=true,readOnly=true};
    }
}
