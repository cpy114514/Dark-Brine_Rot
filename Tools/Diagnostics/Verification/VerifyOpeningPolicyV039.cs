using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyOpeningPolicyV039
{
    static Task pending;
    public static object Start(){if(pending!=null&&!pending.IsCompleted)throw new Exception("Already running");pending=Run();return new{started=true};}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Run()
    {
        const string folder=".codex/opening-optimization-20261012";
        try
        {
            var results=new List<object>();
            for(int scenario=0;scenario<3;scenario++)
            {
                GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(20);await Task.Delay(250);
                var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();var hero=sequence.sahur;var hp=hero.GetComponent<PlayerHealth>();
                battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
                for(int i=0;i<800&&battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
                Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"No playable fight");
                battle.enabled=false;hp.currentHealth=hp.maxHealth;hp.GrantProtection(0);
                Require(battle.SharkHealth.maxHealth==1000&&battle.SharkHealth.currentHealth==1000,"Shark did not start at 1000");
                if(scenario==0){battle.SharkHealth.ApplyDamage(699,hero.transform.position);Require(!battle.FinalePending&&battle.SharkHealth.currentHealth==301,"Triggered above 30%");battle.SharkHealth.ApplyDamage(50000,hero.transform.position);Require(battle.SharkHealth.currentHealth==300,"One heavy hit skipped shark floor");}
                else if(scenario==1){hp.ApplyDamage(hp.maxHealth*.69f,hero.transform.position);Require(!battle.FinalePending,"Triggered above player 30%");hp.ApplyDamage(50000,hero.transform.position);Require(Mathf.Approximately(hp.currentHealth,hp.maxHealth*.3f),"One hit skipped player floor");}
                else Require(battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit),"Timer request rejected");
                Require(battle.FinalePending&&hp.IsProtected&&battle.SharkHealth.IsProtected,"Same-frame protection not latched: scenario="+scenario+" reason="+battle.RequestedFinaleReason+" phase="+battle.CurrentPhase+" player="+hp.currentHealth+" protected="+hp.IsProtected+" shark="+battle.SharkHealth.currentHealth+" protected="+battle.SharkHealth.IsProtected);
                float playerHP=hp.currentHealth,sharkHP=battle.SharkHealth.currentHealth;hp.ApplyDamage(50000,hero.transform.position);battle.SharkHealth.ApplyDamage(50000,hero.transform.position);
                Require(hp.currentHealth==playerHP&&battle.SharkHealth.currentHealth==sharkHP,"Queued damage changed protected health");
                Require(hero.GetComponent<SahurAttack>().CinematicHandoffPending,"New attack chains remain enabled");
                Require(!battle.RequestFinale(Story1WreckBattle.FinaleReason.TimeLimit),"Repeated trigger rescheduled handoff");
                Require(!PlayerDeathRespawn.IsOpen,"Narrative threshold opened respawn");
                results.Add(new{scenario,reason=battle.RequestedFinaleReason.ToString(),playerHP,sharkHP,immediateProtection=true});
            }
            File.WriteAllText(folder+"/health-policy.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,results},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(folder+"/health-policy-error.txt",ex.ToString());}
    }
}
