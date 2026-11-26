using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyBoundedPlanningV058
{
    static Task pending;
    struct Query { public Story1SharkWreckContact.AnatomyPose pose;public Vector3 marker;public Quaternion rotation;public bool tail; }
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Requires original Editor Play Mode");
        if(pending!=null && !pending.IsCompleted)throw new Exception("Validation already running");
        pending=Run();return new{started=true};
    }
    static async Task Run()
    {
        const string path="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/bounded-planning-v058.json";
        float previousScale=Time.timeScale;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);
            await Task.Delay(250);var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);
            UnityEngine.Random.InitState(20261012);sequence.BeginImpact();float deadline=Time.realtimeSinceStartup+30;
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No actual released wreck");
            Time.timeScale=0;var contact=battle.WreckContact;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            var queries=new System.Collections.Generic.List<Query>();
            foreach(string action in new[]{"Bite_Lunge","Tail_Strike","Ship_Smash"})foreach(float seconds in new[]{0f,.2f,.52f,.65f,1f})
            {
                shark.SampleCinematicAction(action,seconds);var pose=contact.CaptureAnatomy();
                foreach(bool tail in new[]{false,true})foreach(float yaw in new[]{-100f,-40f,0f,40f,100f})
                    queries.Add(new Query{pose=pose,tail=tail,marker=(tail?shark.TailWorldPoint:shark.NoseWorldPoint)+Vector3.right*(yaw/14),rotation=Quaternion.Euler(-16,yaw,0)});
            }
            var limits=new[]{0f,.0004f,.01f,.5f,5f,float.PositiveInfinity};int exact=0,rejected=0,nonzero=0;float maxExactError=0,maxResetError=0;
            for(int index=0;index<queries.Count;index++)
            {
                var q=queries[index];float full=contact.PoseObstruction(q.pose,q.marker,q.rotation,q.tail);if(full>0)nonzero++;
                foreach(float limit in limits)
                {
                    float bounded=contact.PoseObstructionBounded(q.pose,q.marker,q.rotation,q.tail,limit);
                    if(full<=limit){exact++;maxExactError=Mathf.Max(maxExactError,Mathf.Abs(full-bounded));}
                    else{rejected++;if(bounded<=limit || bounded>full+.00001f)throw new Exception("Incorrect lower bound at query "+index+" limit="+limit+" full="+full+" bounded="+bounded);}
                    float after=contact.PoseObstruction(q.pose,q.marker,q.rotation,q.tail);maxResetError=Mathf.Max(maxResetError,Mathf.Abs(full-after));
                }
                if(index%5==4)await Task.Delay(1);
            }
            // Exercise the caller's exact best-choice contract, including the
            // clear-first rule and exposure penalties, in both search directions.
            int winners=0;
            foreach(bool reverse in new[]{false,true})
            {
                float best=float.PositiveInfinity,boundedBest=float.PositiveInfinity;bool clear=false,boundedClear=false;int winner=-1,boundedWinner=-1;
                foreach(int index in reverse?Enumerable.Range(0,queries.Count).Reverse():Enumerable.Range(0,queries.Count))
                {
                    var q=queries[index];float bias=(index%13)*.17f;
                    float full=contact.PoseObstruction(q.pose,q.marker,q.rotation,q.tail),cost=full*10000+bias;bool valid=full<=.0004f;
                    if(valid && !clear || valid==clear && cost<best){best=cost;clear=valid;winner=index;}
                    float bounded=contact.PoseObstructionBounded(q.pose,q.marker,q.rotation,q.tail,Mathf.Max(.0004f,(boundedBest-bias)/10000));
                    float boundedCost=bounded*10000+bias;bool boundedValid=bounded<=.0004f;
                    if(boundedValid && !boundedClear || boundedValid==boundedClear && boundedCost<boundedBest){boundedBest=boundedCost;boundedClear=boundedValid;boundedWinner=index;}
                    if(index%5==4)await Task.Delay(1);
                }
                if(winner!=boundedWinner || Mathf.Abs(best-boundedBest)>.00001f)throw new Exception("Best candidate changed");winners++;
            }
            async Task<double> Measure(bool bounded)
            {
                double total=0;for(int repeat=0;repeat<4;repeat++)for(int index=0;index<queries.Count;index++)
                {
                    var q=queries[index];var clock=Stopwatch.StartNew();
                    if(bounded)contact.PoseObstructionBounded(q.pose,q.marker,q.rotation,q.tail,.0004f);else contact.PoseObstruction(q.pose,q.marker,q.rotation,q.tail);
                    clock.Stop();total+=clock.Elapsed.TotalMilliseconds;if(index%5==4)await Task.Delay(1);
                }
                return total;
            }
            double fullMs=await Measure(false),boundedMs=await Measure(true);
            bool passed=nonzero>0 && exact>0 && rejected>0 && maxExactError<.00001f && maxResetError<.00001f && winners==2;
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,native=true,performanceScope="actual native planning queries only; not whole-game FPS",queries=queries.Count,nonzero,exact,rejected,maxExactError,maxResetError,winners,fullMs,boundedMs},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,error=ex.ToString()},Newtonsoft.Json.Formatting.Indented));}
        finally{Time.timeScale=previousScale;}
    }
}
