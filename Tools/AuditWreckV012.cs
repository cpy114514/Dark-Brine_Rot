using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;
public static class AuditWreckV012
{
 public static async Task<object> Begin()
 {
  GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(25);await Task.Delay(300);
  var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=300;sequence.BeginImpact();
  while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)await Task.Delay(30);
  sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(300);
  var shapes=UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None);
  return new{bodies=shapes.Length,parts=shapes.Sum(s=>s.Parts.Length),largestVertexCount=shapes.SelectMany(s=>s.Parts).Max(p=>p.sharedMesh.vertices.Distinct().Count()),sequenceScale=sequence.transform.lossyScale,headScale=battle.WreckContact.HeadVolume.transform.lossyScale,headRadius=battle.WreckContact.HeadVolume.radius,headHeight=battle.WreckContact.HeadVolume.height};
 }
}
