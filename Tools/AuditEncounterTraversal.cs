using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class AuditEncounterTraversal
{
    public static async Task<object> Inspect()
    {
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] before=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(30);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.BeginImpact();
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)await Task.Delay(30);
            sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(100);
            var hero=sequence.sahur;Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
            var result=new{feet=feet.ToString(),clips=hero.CharacterAnimator.runtimeAnimatorController.animationClips.Distinct().Select(c=>new{c.name,c.length,c.frameRate}),
                boards=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).Where(p=>p.IsBoardable).Select(p=>new{p.name,point=p.BoardingPoint(feet).ToString(),size=p.Dimensions.ToString(),gap=p.EdgeDistance(feet),speed=p.Speed})};
            File.WriteAllText(Path.GetFullPath(".codex/encounter-v003/audit.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{GameSaveManager.CancelPendingContinue();if(before!=null)File.WriteAllBytes(save,before);}
    }
}
