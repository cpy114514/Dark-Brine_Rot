using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class VerifySupportedHandoffV156
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original native Play Mode required");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");
        var holder=new GameObject("Explicit actor-only supported handoff fixture v156");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.captureScreenshots=false;benchmark.waterEntry=false;benchmark.scatterSeed=20261015;
        holder.AddComponent<SupportedHandoffObserverV156>().benchmark=benchmark;Time.captureFramerate=30;
        return new{started=true,actorOnlyFixture=true,fragmentsPlaced=false,performanceEvidence=false,naturalAdmissionAcceptance=false};
    }
}

[DefaultExecutionOrder(32900)]
public sealed class SupportedHandoffObserverV156:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    bool probed,supportedSeen,unsupportedGraceRejected,ascendingRejected,groundedAtProbe;
    float requestTime=-1;Vector3 lastRoot;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    void LateUpdate()
    {
        var battle=benchmark!=null ? benchmark.Battle:null;if(battle==null)return;
        var actor=battle.GetComponent<Story1SharkCollisionSequence>().sahur;
        var rider=actor.GetComponent<Story1WreckRider>();
        if(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting)
        {
            lastRoot=actor.transform.position;
            if(battle.FinalePending && requestTime<0)requestTime=Time.time;
            if(!probed && battle.FightElapsed>2.5f && rider!=null && rider.HasStableSupport)
            {
                probed=true;supportedSeen=true;groundedAtProbe=actor.GetComponent<CharacterController>().isGrounded;
                var speed=actor.GetType().GetField("verticalSpeed",Private);float oldSpeed=(float)speed.GetValue(actor);Vector3 root=actor.transform.position;
                try
                {
                    actor.transform.position=root+Vector3.up*.35f;
                    unsupportedGraceRejected=rider.Support()!=null && !rider.HasStableSupport;
                    actor.transform.position=root;speed.SetValue(actor,2f);ascendingRejected=!rider.HasStableSupport;
                }
                finally{actor.transform.position=root;speed.SetValue(actor,oldSpeed);}
            }
        }
        else if(battle.CurrentPhase==Story1WreckBattle.Phase.Finale)
        {
            float delay=requestTime>=0 ? Time.time-requestTime:-1;
            var result=new{native=true,actorOnlyFixture=true,fragmentsPlaced=false,performanceEvidence=false,naturalNoTeleportAcceptance=false,
                supportedSeen,groundedAtProbe,unsupportedGraceRejected,ascendingRejected,actualPhase=battle.CurrentPhase.ToString(),
                reason=battle.RequestedFinaleReason.ToString(),handoffDelay=delay,fromWater=battle.Finale.EntryFromWater,
                rootChange=Vector3.Distance(lastRoot,actor.transform.position),
                passed=supportedSeen && unsupportedGraceRejected && ascendingRejected && requestTime>=0 && delay<=1.05f && !battle.Finale.EntryFromWater};
            File.WriteAllText(".codex/opening-optimization-20261012/supported-handoff-v156.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Destroy(this);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
