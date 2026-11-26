using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyBoundedWaterEntryV038
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying,"Native Play required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        Rigidbody heldBody=null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var hero=sequence.sahur;
            if(hero.GetComponent<GameSaveExcluded>()==null)hero.gameObject.AddComponent<GameSaveExcluded>();
            var hp=hero.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;hp.GrantProtection(120);
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"No live battle");
            var board=battle.Boards.Where(b=>!b.IsBroken && b.Dimensions.y<1 && Mathf.Min(b.Dimensions.x,b.Dimensions.z)>8)
                .OrderByDescending(b=>b.Dimensions.x*b.Dimensions.z).First();
            var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var rider=hero.GetComponent<Story1WreckRider>();rider.Release();
            heldBody=board.GetComponent<Rigidbody>();Vector3 isolated=heldBody.position+Vector3.right*220;
            isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            heldBody.position=isolated;heldBody.rotation=Quaternion.identity;heldBody.linearVelocity=heldBody.angularVelocity=Vector3.zero;
            await Task.Delay(1600);Physics.SyncTransforms();board.CommitPose();
            Vector3 swim=board.BoardingPoint(heldBody.worldCenterOfMass)+Vector3.right*(board.Dimensions.x*.5f+8);
            swim.y=ocean.SampleSurfaceHeight(swim,Time.time)-2-hero.LowestFootWorldOffset;
            hero.RestoreSavedPose(swim,Quaternion.identity);await Task.Delay(250);
            Require(hero.Swimming,"Actual fixture did not swim");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            // Stabilize only this isolated original mesh at a genuinely
            // reachable water edge before testing its later invalidation.
            heldBody.linearVelocity=heldBody.angularVelocity=Vector3.zero;heldBody.isKinematic=true;
            heldBody.rotation=Quaternion.identity;Physics.SyncTransforms();board.CommitPose();
            Vector3 grip=board.BoardingPoint(hero.transform.position);
            heldBody.position+=Vector3.up*(ocean.SampleSurfaceHeight(grip,Time.time)+.3f-grip.y);
            Physics.SyncTransforms();board.CommitPose();
            var chosen=(Story1WreckPlank)typeof(Story1WreckBattle).GetMethod("FindLandingBoard",flags).Invoke(battle,new object[]{hero.transform.position,8f,true});
            Require(chosen==board,"Fixture's actual reachable deck was not selected: "+chosen?.name);
            typeof(Story1WreckBattle).GetMethod("BeginFinale",flags).Invoke(battle,null);var film=battle.Finale;
            Require(film!=null,"Valid real water-entry support rejected");
            var actual=(Story1WreckPlank)typeof(Story1WreckFinale).GetField("board",flags).GetValue(film);
            Require(actual==board,"Fixture selected a different support");
            heldBody.isKinematic=true;heldBody.position+=Vector3.up*8;Physics.SyncTransforms();board.CommitPose();
            var waitField=typeof(Story1WreckFinale).GetField("entrySupportWaitStarted",flags);
            for(int i=0;i<1000 && battle.Finale==film && (float)waitField.GetValue(film)<0;i++)await Task.Delay(20);
            Require(battle.Finale==film && (float)waitField.GetValue(film)>=0,"No actual unready-water wait observed");
            float started=(float)waitField.GetValue(film),health=hp.currentHealth;
            for(int i=0;i<150 && battle.Finale==film;i++)await Task.Delay(20);
            float waited=film.Elapsed-started;
            Require(battle.Finale==null && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Unready entry retained cinematic ownership");
            Require(waited>=1 && waited<=1.25f,"Entry wait exceeded its one-second limit: "+waited);
            Require(hero.enabled && !hero.ExternalControlLock && hero.GetComponent<CharacterController>().enabled && hero.GetComponent<SahurAttack>().enabled,"Entry release left controls disabled");
            Require(hp.currentHealth==health && hp.currentHealth>0 && hero.Motion.State!=SahurControlState.Unconscious,"Unready entry invented defeat");
            var result=new{passed=true,fixtureOnlyOriginalDeckIsolation=true,faultInjection="Real selected support held eight metres above water after valid entry admission",actualWaterEntry=true,waited,controlsRestored=true,health=hp.currentHealth,board=board.name};
            Directory.CreateDirectory(".codex/encounter-v038");File.WriteAllText(".codex/encounter-v038/bounded-water-entry.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        catch(Exception error)
        {
            Directory.CreateDirectory(".codex/encounter-v038");
            File.WriteAllText(".codex/encounter-v038/bounded-water-entry-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error=error.ToString(),completedUtc=DateTime.UtcNow}));
            throw;
        }
        finally{if(heldBody!=null)heldBody.isKinematic=false;GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);}
    }
}
