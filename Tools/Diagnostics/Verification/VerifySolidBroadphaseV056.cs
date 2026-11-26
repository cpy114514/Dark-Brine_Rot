using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifySolidBroadphaseV056
{
    static Task pending;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    delegate bool Overlap(Collider volume,Story1WreckMeshCollision shape,Vector3 offset,out Vector3 direction,out float depth);
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        const string folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";
        float oldScale=Time.timeScale;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);
            await Task.Delay(250);var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);sequence.BeginImpact();
            float deadline=Time.realtimeSinceStartup+30;while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No actual released wreck");
            Time.timeScale=0;var contact=battle.WreckContact;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();var type=contact.GetType();
            var near=(Func<Story1WreckMeshCollision,bool>)Delegate.CreateDelegate(typeof(Func<Story1WreckMeshCollision,bool>),contact,type.GetMethod("MayOverlapSolid",Private));
            var overlap=(Overlap)Delegate.CreateDelegate(typeof(Overlap),contact,type.GetMethod("OverlapShape",Private));
            var volumes=(Collider[])type.GetField("volumes",Private).GetValue(contact);
            var shapes=battle.Boards.Select(b=>b.Collision).Concat(battle.WreckDetails.Select(b=>b.Collision)).ToArray();
            int mismatches=0,poses=0,positiveContacts=0,oldTests=0,newTests=0;float maxDepthDifference=0;Vector3 original=shark.transform.position;
            var rows=new System.Collections.Generic.List<object>();
            foreach(string action in new[]{"Tail_Strike","Ship_Smash","Bite_Lunge"})foreach(float seconds in new[]{0f,.3f,.65f,1f})foreach(Vector3 offset in new[]{Vector3.zero,Vector3.right*2,Vector3.down*1.2f,Vector3.forward*4})
            {
                shark.SampleCinematicAction(action,seconds);shark.transform.position=original+offset;contact.CurrentPenetration();int hits=0,skipped=0;
                foreach(var shape in shapes)
                {
                    bool candidate=near(shape);if(!candidate)skipped++;
                    foreach(var volume in volumes)
                    {
                        bool previous=overlap(volume,shape,Vector3.zero,out var oldDirection,out float oldDepth);oldTests++;
                        bool current=false;Vector3 direction=default;float depth=0;
                        if(candidate){current=overlap(volume,shape,Vector3.zero,out direction,out depth);newTests++;}
                        if(previous!=current || previous && (oldDirection!=direction || oldDepth!=depth))mismatches++;
                        maxDepthDifference=Mathf.Max(maxDepthDifference,Mathf.Abs(oldDepth-depth));if(previous){positiveContacts++;hits++;}
                    }
                }
                rows.Add(new{action,seconds,offset=offset.ToString(),hits,skipped});poses++;
            }
            double Scan(bool accelerated)
            {
                var clock=Stopwatch.StartNew();float checksum=0;
                for(int repeat=0;repeat<500;repeat++)foreach(var shape in shapes)
                {
                    if(accelerated && !near(shape))continue;
                    foreach(var volume in volumes)if(overlap(volume,shape,Vector3.zero,out _,out float depth))checksum+=depth;
                }
                clock.Stop();GC.KeepAlive(checksum);return clock.Elapsed.TotalMilliseconds;
            }
            Scan(false);Scan(true);var before=new double[7];var after=new double[7];for(int i=0;i<7;i++){before[i]=Scan(false);after[i]=Scan(true);}
            bool passed=mismatches==0 && maxDepthDifference==0 && positiveContacts>0 && volumes.All(v=>v.enabled);
            var result=new{passed,poses,shapes=shapes.Length,positiveContacts,mismatches,maxDepthDifference,oldTests,newTests,medianPreviousMs=before.OrderBy(t=>t).ElementAt(3),medianAcceleratedMs=after.OrderBy(t=>t).ElementAt(3),before,after,rows};
            File.WriteAllText(folder+"/solid-broadphase-v056.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!passed)throw new Exception("Union broadphase changed existing ordered native contact results");
        }
        catch(Exception ex){File.WriteAllText(folder+"/solid-broadphase-v056-error.txt",ex.ToString());}
        finally{Time.timeScale=oldScale;}
    }
}
