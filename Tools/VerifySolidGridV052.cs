using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifySolidGridV052
{
    static Task pending;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
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
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No actual wreck");
            Time.timeScale=0;var contact=battle.WreckContact;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            var volumes=(Collider[])contact.GetType().GetField("volumes",Private).GetValue(contact);
            var bounds=(System.Collections.Generic.Dictionary<Collider,Bounds>)contact.GetType().GetField("anatomyBounds",Private).GetValue(contact);
            var maximum=contact.GetType().GetMethod("PlanningMaximum",Private);var clear=contact.GetType().GetMethod("ClearAt",Private);
            float maxDifference=0;int queries=0,blocked=0,mismatches=0;var rows=new System.Collections.Generic.List<object>();
            foreach(string action in new[]{"Tail_Strike","Ship_Smash","Bite_Lunge"})foreach(float seconds in new[]{0f,.3f,.65f,1f})
            {
                shark.SampleCinematicAction(action,seconds);contact.CurrentPenetration();
                foreach(Vector3 offset in new[]{Vector3.zero,Vector3.right*.3f,Vector3.down*1.2f,Vector3.forward*4})
                {
                    float accelerated=0,brute=0;
                    foreach(var volume in volumes)
                    {
                        var at=bounds[volume];at.center+=offset;
                        accelerated=Mathf.Max(accelerated,(float)maximum.Invoke(contact,new object[]{volume,volume.transform.position+offset,volume.transform.rotation,at,float.PositiveInfinity}));
                        foreach(var shape in battle.Boards.Where(p=>!p.IsBroken && Time.time>=p.SharkContactAfter).Select(p=>p.Collision).Concat(battle.WreckDetails.Select(p=>p.Collision)))
                        {
                            if(shape==null || !shape.Active)continue;
                            for(int i=0;i<shape.Parts.Length;i++)
                                if(Physics.ComputePenetration(volume,volume.transform.position+offset,volume.transform.rotation,shape.Parts[i],shape.PartPosition(i),shape.PartRotation(i),out _,out float depth))brute=Mathf.Max(brute,depth);
                        }
                    }
                    bool fastClear=(bool)clear.Invoke(contact,new object[]{offset}),bruteClear=brute<=.003f;
                    maxDifference=Mathf.Max(maxDifference,Mathf.Abs(accelerated-brute));queries++;if(!bruteClear)blocked++;if(fastClear!=bruteClear)mismatches++;
                    rows.Add(new{action,seconds,offset=offset.ToString(),accelerated,brute,fastClear,bruteClear});
                }
                await Task.Delay(1);
            }
            bool passed=maxDifference<.00001f && mismatches==0 && blocked>0 && volumes.All(v=>v.enabled);
            File.WriteAllText(folder+"/solid-grid-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,queries,blocked,mismatches,maxDifference,allActualVolumesEnabled=volumes.All(v=>v.enabled),rows},Newtonsoft.Json.Formatting.Indented));
            if(!passed)throw new Exception("Live solid grid differs from native brute force");
        }
        catch(Exception ex){File.WriteAllText(folder+"/solid-grid-validation-error.txt",ex.ToString());}
        finally{Time.timeScale=oldScale;}
    }
}
