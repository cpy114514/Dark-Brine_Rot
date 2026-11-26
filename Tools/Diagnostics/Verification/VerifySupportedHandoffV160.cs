using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class VerifySupportedHandoffV160
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        if(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("No duplicate benchmark");
        var holder=new GameObject("Actual released mesh support predicate fixture v160");UnityEngine.Object.DontDestroyOnLoad(holder);
        var benchmark=holder.AddComponent<OpeningBenchmark>();benchmark.waterEntry=true;benchmark.captureScreenshots=false;benchmark.scatterSeed=20261015;
        holder.AddComponent<SupportedHandoffObserverV160>().benchmark=benchmark;Time.captureFramerate=30;
        return new{started=true,actorOnlyFixture=true,woodRepositioned=false,naturalAdmissionAcceptance=false};
    }
}
[DefaultExecutionOrder(32900)]
public sealed class SupportedHandoffObserverV160:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    bool probed,settled,ascendingRejected,graceRejected,brokenRejected,grounded;float requestTime;Vector3 requestRoot;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;if(battle==null)return;
        var actor=battle.GetComponent<Story1SharkCollisionSequence>().sahur;var rider=actor.GetComponent<Story1WreckRider>();
        if(!probed && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && battle.FightElapsed>2.5f)
        {
            foreach(var board in battle.Boards)
            {
                if(board==null || !board.IsBoardable || Mathf.Abs(board.transform.up.y)<.9f)continue;
                Vector3 feet=board.BoardingPoint(board.GetComponent<Rigidbody>().worldCenterOfMass);
                Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
                if(!board.Collision.Raycast(new Ray(feet+normal*3,-normal),out var hit,6) || Vector3.Dot(hit.normal,Vector3.up)<.8f)continue;
                // Diagnostic actor setup only. The freely released board's
                // pose, velocity, buoyancy and mesh remain completely intact.
                actor.RestoreSavedPose(hit.point+Vector3.up*(.04f-actor.LowestFootWorldOffset),actor.transform.rotation);
                rider.Initialize(actor,battle.Boards,UnityEngine.Object.FindFirstObjectByType<OceanWorld>(),board,battle.WreckDetails);
                actor.NotifyPlatformLanding();settled=rider.HasStableSupport;grounded=actor.GetComponent<CharacterController>().isGrounded;
                Vector3 root=actor.transform.position;var speed=actor.GetType().GetField("verticalSpeed",Private);float previous=(float)speed.GetValue(actor);
                try
                {
                    speed.SetValue(actor,2f);ascendingRejected=!rider.HasStableSupport;speed.SetValue(actor,previous);
                    actor.transform.position=root+Vector3.up*.35f;graceRejected=rider.Support()!=null && !rider.HasStableSupport;actor.transform.position=root;
                    board.Collision.SetEnabled(false);brokenRejected=!rider.HasStableSupport;
                }
                finally{actor.transform.position=root;speed.SetValue(actor,previous);board.Collision.SetEnabled(true);}
                probed=true;requestTime=Time.time;requestRoot=actor.transform.position;battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit);break;
            }
        }
        if(probed && battle.CurrentPhase==Story1WreckBattle.Phase.Finale)
        {
            float delay=Time.time-requestTime;
            File.WriteAllText(".codex/opening-optimization-20261012/supported-handoff-v160.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,actorOnlyFixture=true,woodRepositioned=false,naturalNoTeleportAcceptance=false,performanceEvidence=false,
                settled,grounded,ascendingRejected,graceRejected,brokenRejected,delay,fromWater=battle.Finale.EntryFromWater,
                rootChange=Vector3.Distance(requestRoot,actor.transform.position),
                passed=settled && ascendingRejected && graceRejected && brokenRejected && delay<=1.05f && !battle.Finale.EntryFromWater},Newtonsoft.Json.Formatting.Indented));
            Destroy(this);
        }
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
