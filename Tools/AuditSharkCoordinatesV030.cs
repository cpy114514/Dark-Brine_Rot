using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuditSharkCoordinatesV030
{
    public static async Task<object> Verify()
    {
        if(!Application.isPlaying)throw new Exception("Native Play required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;float timeScale=Time.timeScale;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No native released wreck");
            Time.timeScale=0;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            var cached=(Bounds)typeof(Story1WreckBattle).GetField("sharkLocalBounds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
            var canonical=shark.RootMeshBounds;Vector3 root=shark.transform.position;Quaternion heading=shark.transform.rotation;
            string action=shark.CinematicAction;float seconds=shark.CinematicActionSeconds;
            var rows=new System.Collections.Generic.List<object>();float worst=0;
            try
            {
                foreach(bool tail in new[]{false,true})foreach(float pitch in new[]{-25f,0f,25f})
                {
                    shark.SampleCinematicAction(tail ? "Ship_Smash" : "Bite_Lunge",tail ? .65f : .52f);
                    Vector3 local=shark.transform.InverseTransformPoint(tail ? shark.TailWorldPoint : shark.NoseWorldPoint);
                    Quaternion rotation=Quaternion.Euler(pitch,53,0);Vector3 target=root+Vector3.up*5;
                    Vector3 desired=target-rotation*Vector3.Scale(local-canonical.center,shark.transform.lossyScale);
                    battle.PoseCinematicShark(desired,rotation,absoluteHeight:true);
                    Vector3 actual=tail ? shark.TailWorldPoint : shark.NoseWorldPoint;
                    float error=Vector3.Distance(target,actual);worst=Mathf.Max(worst,error);
                    rows.Add(new{tail,pitch,target=target.ToString("F6"),actual=actual.ToString("F6"),error});
                }
            }
            finally{shark.transform.SetPositionAndRotation(root,heading);shark.SampleCinematicAction(action,seconds);}
            var result=new{canonicalCentre=canonical.center.ToString("F6"),battleCachedCentre=cached.center.ToString("F6"),
                centreDifference=Vector3.Distance(canonical.center,cached.center),maximumWorldPlacementError=worst,rows,passed=worst<.001f};
            Directory.CreateDirectory(".codex/encounter-v030");File.WriteAllText(".codex/encounter-v030/shark-coordinate-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally{Time.timeScale=timeScale;GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);}
    }
}
