using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
// Native geometry equivalence probe. Offline diagnostics only, not FPS evidence.
[DefaultExecutionOrder(32800)]
public sealed class EscapeOrderObserverV089:MonoBehaviour
{
    public string output;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<object> rows=new List<object>();
    readonly float[] cues={1.1f,7f,9f,14.5f,18f};
    int next,mismatches,clear,blocked;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || next>=cues.Length || film.CinematicTime<cues[next])return;
        var contact=battle.WreckContact;var type=typeof(Story1SharkWreckContact);
        var volumes=(Collider[])type.GetField("volumes",flags).GetValue(contact);
        var bounds=(Dictionary<Collider,Bounds>)type.GetField("anatomyBounds",flags).GetValue(contact);
        var maximum=type.GetMethod("PlanningMaximum",flags);var current=type.GetMethod("ClearAt",flags);
        type.GetMethod("SnapshotPlanningShapes",flags).Invoke(contact,null);
        bool Reference(Vector3 offset)
        {
            foreach(var volume in volumes)
            {
                Bounds shifted=bounds[volume];shifted.center+=offset;
                float depth=(float)maximum.Invoke(contact,new object[]{volume,volume.transform.position+offset,volume.transform.rotation,shifted,.003f});
                if(depth>.003f)return false;
            }
            return true;
        }
        foreach(float distance in new[]{0f,.1f,.5f,2f,5f})
        foreach(var direction in new[]{Vector3.down,Vector3.up,Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
        {
            Vector3 offset=direction*distance;
            bool expected=Reference(offset),actual=(bool)current.Invoke(contact,new object[]{offset});
            if(expected)clear++;else blocked++;if(expected!=actual)mismatches++;
            rows.Add(new{clock=film.CinematicTime,offset=offset.ToString("F3"),expected,actual});
        }
        next++;
        if(next>=cues.Length)
        {
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{nativeGeometry=true,offlineDiagnostic=true,performanceEvidence=false,
                probes=rows.Count,clear,blocked,mismatches,passed=mismatches==0 && clear>0 && blocked>0,rows},Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);
        }
    }
}
public static class VerifyEscapeOrderV089
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var go=new GameObject("Native escape predicate equivalence v089");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EscapeOrderObserverV089>().output=output;return new{started=true,readOnlyGeometry=true,performanceEvidence=false};
    }
}
