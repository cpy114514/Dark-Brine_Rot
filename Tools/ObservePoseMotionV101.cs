using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(32800)]
public sealed class PoseMotionObserverV101:MonoBehaviour
{
    public string output;
    readonly List<object> rows=new List<object>();
    bool sampled;Vector3 last;Quaternion heading;
    int frames,travelOverruns;float maximumPenetration,maximumTravel,maximumOverrun;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0)return;
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        var solid=battle.WreckContact;Vector3 at=shark.transform.position;
        float penetration=solid.CurrentPenetration(),distance=sampled?Vector3.Distance(last,at):0;
        float overrun=Mathf.Max(0,distance-(24*Mathf.Max(.01f,Time.deltaTime)+.30f));
        if(sampled && overrun>.01f)travelOverruns++;
        maximumPenetration=Mathf.Max(maximumPenetration,penetration);maximumTravel=Mathf.Max(maximumTravel,distance);maximumOverrun=Mathf.Max(maximumOverrun,overrun);
        rows.Add(new{clock=film.CinematicTime,penetration,distance,overrun,headingStep=sampled?Quaternion.Angle(heading,shark.transform.rotation):0,
            solid.PoseAdvanceCount,solid.PartialPoseAdvanceCount,solid.ConstraintHoldCount,solid.LargeEscapeCount});
        last=at;heading=shark.transform.rotation;sampled=true;frames++;
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,performanceEvidence=false,
            frames,maximumPenetration,maximumTravel,maximumOverrun,travelOverruns,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class ObservePoseMotionV101
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only actual solved pose motion v101");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<PoseMotionObserverV101>().output=output;return new{started=true,readOnly=true};
    }
}
