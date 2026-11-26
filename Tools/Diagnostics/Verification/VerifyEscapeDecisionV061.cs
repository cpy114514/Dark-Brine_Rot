using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyEscapeDecisionV061
{
    delegate bool FindClear(float maximum,out Vector3 shift);
    static Task pending;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static object Start(){if(!Application.isPlaying)throw new Exception("Requires Play Mode");pending=Run();return new{started=true};}
    static async Task Run()
    {
        const string path="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/escape-decision-v061.json";
        float oldScale=Time.timeScale;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);await Task.Delay(200);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.DiagnosticScatterSeed=20261012;battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);sequence.BeginImpact();
            float deadline=Time.realtimeSinceStartup+30;while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No native wreck");
            Time.timeScale=0;var contact=battle.WreckContact;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();var type=contact.GetType();
            var nearest=(FindClear)Delegate.CreateDelegate(typeof(FindClear),contact,type.GetMethod("NearestClear",Private));
            var update=(Action)Delegate.CreateDelegate(typeof(Action),contact,type.GetMethod("UpdateVolumes",Private));
            const float budget=.65f;int comparisons=0,blocked=0,holds=0;float maxError=0;double fullMs=0,boundedMs=0;
            foreach(var support in battle.Boards.Where(p=>!p.IsBroken).Take(8))foreach(string action in new[]{"Bite_Lunge","Tail_Strike"})
            {
                shark.SampleCinematicAction(action,.52f);Quaternion rotation=Quaternion.Euler(-16,support.transform.eulerAngles.y,0);
                shark.transform.SetPositionAndRotation(support.GetComponent<Rigidbody>().worldCenterOfMass,rotation);update();
                if(!nearest(float.PositiveInfinity,out var seedShift))continue;
                Vector3 previous=shark.transform.position+seedShift;
                foreach(Vector3 offset in new[]{Vector3.right*.3f,Vector3.right*.9f,Vector3.forward*1.5f,Vector3.down*1.5f,Vector3.left*3})
                {
                    Vector3 current=previous+offset;
                    (Vector3 root,bool held) Decide(bool bounded)
                    {
                        shark.transform.SetPositionAndRotation(current,rotation);update();bool held=false;Vector3 resolved=current,escape;
                        if(bounded)
                        {
                            bool clear=nearest(Vector3.Distance(current,previous)+budget+.01f,out escape);
                            if(!clear)
                            {
                                shark.transform.position=previous;update();
                                if(nearest(budget,out var shift)){held=true;resolved=previous+shift;escape=Vector3.zero;}
                                else{shark.transform.position=current;update();escape=nearest(float.PositiveInfinity,out var far)?far:Vector3.down*20;}
                            }
                        }
                        else escape=nearest(float.PositiveInfinity,out var far)?far:Vector3.down*20;
                        resolved+=escape;
                        if(Vector3.Distance(resolved,previous)>budget)
                        {
                            shark.transform.position=previous;update();
                            if(nearest(budget,out var shift)){resolved=previous+shift;held=true;}
                        }
                        return(resolved,held);
                    }
                    shark.transform.SetPositionAndRotation(current,rotation);update();if(!nearest(0,out _))blocked++;
                    var clock=Stopwatch.StartNew();var full=Decide(false);clock.Stop();fullMs+=clock.Elapsed.TotalMilliseconds;
                    clock.Restart();var boundedResult=Decide(true);clock.Stop();boundedMs+=clock.Elapsed.TotalMilliseconds;
                    maxError=Mathf.Max(maxError,Vector3.Distance(full.root,boundedResult.root));if(full.held!=boundedResult.held)throw new Exception("Hold decision differs");
                    if(full.held)holds++;comparisons++;await Task.Delay(1);
                }
            }
            bool passed=comparisons>0 && blocked>0 && holds>0 && maxError<.0001f;
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,native=true,scope="bounded escape decision against original exhaustive native decision on actual frozen wreck",comparisons,blocked,holds,maxError,fullMs,boundedMs},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,error=ex.ToString()},Newtonsoft.Json.Formatting.Indented));}
        finally{Time.timeScale=oldScale;}
    }
}
