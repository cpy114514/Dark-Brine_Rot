using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;
public static class VerifyEncounterBuoyancy
{
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task<object> Verify()
    {
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            Require(Application.isPlaying,"Requires Play Mode.");GameSaveManager.CancelPendingContinue();
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(30);await Task.Delay(350);
            var seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=seq.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;seq.BeginImpact();
            while(battle.ShipFragmentCount==0)await Task.Delay(20);
            var decks=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None);
            var debris=UnityEngine.Object.FindObjectsByType<Story1WreckDebris>(FindObjectsSortMode.None);
            Require(decks.Length==20 && debris.Length==182,"Fragment budgets changed.");
            var top=debris.OrderByDescending(d=>d.transform.position.y).First();float startY=top.transform.position.y;
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)await Task.Delay(30);
            seq.sahur.GetComponent<PlayerHealth>().GrantProtection(100);
            var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var deck=decks.First(d=>d.Dimensions.y<1 && d.Dimensions.z>8);
            var body=deck.GetComponent<Rigidbody>();var originalScale=deck.transform.localScale;
            var point=deck.transform.position+Vector3.right*160;point.y=water.SampleSurfaceHeight(point,Time.time);
            body.position=point;body.rotation=Quaternion.Euler(0,23,90);body.linearVelocity=Vector3.down;body.angularVelocity=Vector3.zero;
            await Task.Delay(6500);
            float up=Mathf.Abs((body.rotation*Vector3.up).y),fall=startY-top.transform.position.y;
            Require(up>.85f,"Edge-on deck failed to float onto its broad face: "+up);
            Require(fall>8,"High original ship debris failed to fall under gravity: "+fall);
            Require(decks.All(d=>!d.GetComponent<Rigidbody>().isKinematic && d.GetComponent<Rigidbody>().useGravity),"Deck physics disabled.");
            Require(Vector3.Distance(deck.transform.localScale,originalScale)<.001f,"Board was resized.");
            int usable=decks.Count(d=>d.IsBoardable&&d.GetComponent<Story1WreckFloatBody>().InWater);Require(usable>=5,"Wreck lost usable floating decks.");
            return new{physicalDecks=decks.Length,ballisticVisualPieces=debris.Length,edgeOnRecoveryUp=up,highPieceFall=fall,usableFloatingDecks=usable,retainsGravity=true,retainsScale=true};
        }
        finally
        {
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(actor!=null&&actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
}
