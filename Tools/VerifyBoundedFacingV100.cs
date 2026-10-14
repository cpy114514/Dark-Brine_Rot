using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(32800)]
public sealed class BoundedFacingObserverV100:MonoBehaviour
{
    public string output;
    readonly float[] cues={.9f,2.2f,7f,14.4f,17.8f};
    readonly List<object> rows=new List<object>();
    int next,tests,clear,blocked,mismatches;float maximumClearDifference;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || next>=cues.Length || film.CinematicTime<cues[next])return;
        var contact=battle.WreckContact;var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        var pose=contact.CaptureAnatomy();int before=tests,startMismatch=mismatches;
        foreach(bool tail in new[]{false,true})
        foreach(var offset in new[]{Vector3.zero,Vector3.down*2,Vector3.left*3,Vector3.forward*5})
        foreach(float yaw in new[]{0f,45,90,135,180,225,270,315})
        {
            Vector3 marker=(tail?shark.TailWorldPoint:shark.NoseWorldPoint)+offset;
            Quaternion rotation=Quaternion.Euler(-16,yaw,0);
            float full=contact.PoseObstruction(pose,marker,rotation,tail);
            float bounded=contact.PoseObstructionBounded(pose,marker,rotation,tail,.0004f);
            bool isClear=full<=.0004f;if(isClear)clear++;else blocked++;
            if(isClear)maximumClearDifference=Mathf.Max(maximumClearDifference,Mathf.Abs(full-bounded));
            if(isClear!=(bounded<=.0004f) || isClear && Mathf.Abs(full-bounded)>.000001f)mismatches++;
            tests++;
        }
        rows.Add(new{clock=film.CinematicTime,tests=tests-before,mismatches=mismatches-startMismatch});next++;
        if(next>=cues.Length)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{nativeGeometry=true,readOnly=true,performanceEvidence=false,
            poses=next,tests,clear,blocked,mismatches,maximumClearDifference,passed=next==cues.Length && mismatches==0 && clear>0 && blocked>0,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class VerifyBoundedFacingV100
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Native bounded endpoint classification v100");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<BoundedFacingObserverV100>().output=output;return new{started=true,readOnly=true};
    }
}
