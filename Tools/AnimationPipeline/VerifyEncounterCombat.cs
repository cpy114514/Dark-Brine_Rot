using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyEncounterCombat
{
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    public static async Task<object> Verify()
    {
        Check(Application.isPlaying,"Play Mode required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(30);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            while(battle.CurrentPhase==Story1WreckBattle.Phase.Breaking)await Task.Delay(30);
            Check(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Ship did not hand off controls");
            Check(battle.ImpactTailGap<.03f && battle.ImpactNoseGap>15,"Tail did not contact, or nose collided with the ship");
            Check(battle.ShipFragmentCount==202 && battle.PlankCount==20,"Original wreck geometry missing");
            var hero=sequence.sahur;var hp=hero.GetComponent<PlayerHealth>();hp.maxHealth=hp.currentHealth=1000;hp.GrantProtection(0);
            var rider=hero.GetComponent<Story1WreckRider>();var board=rider.Support();Check(board!=null,"No landing support");
            var body=board.GetComponent<Rigidbody>();body.isKinematic=true;hero.enabled=false;rider.enabled=false;hero.ExternalControlLock=true;
            var knock=hero.GetComponent<CombatKnockback>();if(knock!=null)knock.enabled=false;
            float deadline=Time.realtimeSinceStartup+20;
            while(hp.currentHealth==1000 && Time.realtimeSinceStartup<deadline)await Task.Delay(30);
            Check(hp.currentHealth<1000,"Visible bite/tail marker never reached the standing player's capsule");
            float first=hp.currentHealth;int attacks=battle.SharkAttackCount;
            // Move out of a committed telegraph. The enemy must not redirect its damage area afterwards.
            while(!battle.IsTelegraphing && Time.realtimeSinceStartup<deadline+8)await Task.Delay(30);
            Check(battle.IsTelegraphing,"No second readable telegraph");
            Vector3 target=battle.StrikePoint;
            hero.transform.position+=hero.transform.right*9;Physics.SyncTransforms();
            float before=hp.currentHealth;await Task.Delay(2400);
            Check(hp.currentHealth==before,"A missed committed attack still damaged a distant player");
            var result=new{tailContactGap=battle.ImpactTailGap,noseClearance=battle.ImpactNoseGap,visualFragments=202,physicalDecks=20,
                realMarkerDamage=1000-first,committedAttackCanBeDodged=true,activeAttacks=battle.SharkAttackCount,AudioListener.volume};
            Directory.CreateDirectory(".codex/encounter-v001");File.WriteAllText(".codex/encounter-v001/combat-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally
        {
            var hero=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(hero!=null && hero.GetComponent<GameSaveExcluded>()==null)hero.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
