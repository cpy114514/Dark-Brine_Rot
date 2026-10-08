using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;
public static class AuditSharkBoardContact
{
    public static async Task<object> Audit()
    {
        GameSaveManager.CancelPendingContinue();
        var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
        while(!loading.isDone)await Task.Delay(40);
        await Task.Delay(400);
        var sequence=Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();
        var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
        sequence.BeginImpact();
        while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)await Task.Delay(40);
        var shark=Object.FindFirstObjectByType<TralaleroSwimAnimator>();
        return new{bounds=shark.RootMeshBounds.ToString(),scale=shark.transform.lossyScale.ToString(),
            bones=shark.BoneAnimator.GetComponentsInChildren<Transform>().Where(t=>new[]{"Body","Head","Spine_Mid","Spine_Rear","Tail_Base","Tail_Mid","Tail_Tip"}.Contains(t.name)).Select(t=>new{name=t.name,local=shark.transform.InverseTransformPoint(t.position).ToString()}).ToArray()};
    }
}
