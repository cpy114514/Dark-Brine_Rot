using System;
using System.IO;
using UnityEngine;

// Observe this already-running original encounter, without replacing the scene.
[DefaultExecutionOrder(32700)]
public sealed class EndingObserverV074 : MonoBehaviour
{
    public string output;
    Story1WreckFinale film;
    Story1WreckBattle battle;
    ThirdPersonPlayerController hero;
    OceanWorld ocean;
    double completedAt=-1,pausedAt=-1;
    float completedUnscaledTime;
    string exitReason;
    int heroCounters,sharkCounters;
    int frames,uprightFrames,pausedFrames;
    float endClock,maxClockDrift,minChest=float.PositiveInfinity,maxChest=float.NegativeInfinity,
        minimumHeadClearance=float.PositiveInfinity,maxRock,maxPausedMotion;
    Quaternion firstHeading,lastHeading;
    Vector3 lastChest;
    bool wasPaused,restored,lostStick;
    void LateUpdate()
    {
        var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        if(completedAt>=0 && (arrival!=null || Time.realtimeSinceStartupAsDouble-completedAt>12))
        {Finish(arrival!=null && !arrival.HasStick && !arrival.IsAwake);return;}
        if(film==null && completedAt<0)
        {
            battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
            if(battle!=null)film=battle.Finale;
            if(film!=null)
            {
                hero=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
                ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            }
        }
        if(film==null || !film.IsComplete)return;
        var animator=hero.CharacterAnimator;
        Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position,
            hips=animator.GetBoneTransform(HumanBodyBones.Hips).position,
            chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Quaternion heading=hero.transform.rotation;
        if(completedAt<0){completedAt=Time.realtimeSinceStartupAsDouble;completedUnscaledTime=Time.unscaledTime;endClock=film.Elapsed;firstHeading=heading;}
        exitReason=film.ExitReason;heroCounters=film.HeroCounterCount;sharkCounters=film.SharkCounterCount;
        frames++;
        if(Mathf.Abs(Vector3.Dot((head-hips).normalized,Vector3.up))>.55f)uprightFrames++;
        maxClockDrift=Mathf.Max(maxClockDrift,Mathf.Abs(film.Elapsed-endClock));
        lostStick|=film.HasLostStick;
        if(Time.timeScale==0)
        {
            pausedFrames++;
            if(wasPaused)maxPausedMotion=Mathf.Max(maxPausedMotion,Mathf.Max(Vector3.Distance(chest,lastChest),Quaternion.Angle(heading,lastHeading)));
        }
        else
        {
            minChest=Mathf.Min(minChest,chest.y);maxChest=Mathf.Max(maxChest,chest.y);
            maxRock=Mathf.Max(maxRock,Quaternion.Angle(firstHeading,heading));
            minimumHeadClearance=Mathf.Min(minimumHeadClearance,head.y-ocean.SampleSurfaceHeight(head,Time.time));
        }
        wasPaused=Time.timeScale==0;lastChest=chest;lastHeading=heading;
        double since=Time.unscaledTime-completedUnscaledTime;
        if(pausedAt<0 && since>=1.5){pausedAt=Time.realtimeSinceStartupAsDouble;Time.timeScale=0;}
        if(!restored && pausedAt>=0 && Time.realtimeSinceStartupAsDouble-pausedAt>=.4){Time.timeScale=1;restored=true;}
    }
    void Finish(bool handoff)
    {
            var result=new{frames,uprightFrames,pausedFrames,maxClockDrift,lostStick,chestHeightRange=maxChest-minChest,maxRock,minimumHeadClearance,maxPausedMotion,unconsciousUnarmedIsland=handoff,exitReason,heroCounters,sharkCounters,
                posePassed=frames>=60 && uprightFrames==0 && maxClockDrift==0 && pausedFrames>=5 && maxPausedMotion<=.001f && lostStick,
                wavesPassed=maxChest-minChest>=.015f && maxRock>=.02f && minimumHeadClearance>=.025f,
                standalonePerformanceEvidence=false};
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);
    }
    void OnDestroy(){if(pausedAt>=0 && !restored)Time.timeScale=1;}
}
public static class ObserveEndingV074
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires actual original Play Mode");
        if(UnityEngine.Object.FindFirstObjectByType<EndingObserverV074>()!=null)throw new Exception("Ending observer already running");
        var go=new GameObject("Observe existing finale ending v074");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<EndingObserverV074>().output=output;
        return new{started=true,output,continuesExistingEncounter=true};
    }
}
