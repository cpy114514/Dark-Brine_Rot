using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifySupportAdmissionV029
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> BrokenDuringFilm()
    {
        Require(Application.isPlaying,"Native Play mode required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var p=sequence.sahur;
            p.gameObject.AddComponent<GameSaveExcluded>();var hp=p.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;hp.GrantProtection(120);
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"No playable battle");
            var board=battle.Boards.Where(b=>!b.IsBroken && b.Dimensions.y<1 && Mathf.Min(b.Dimensions.x,b.Dimensions.z)>8 && b.FractureSource!=null)
                .OrderByDescending(b=>b.Dimensions.x*b.Dimensions.z).First();
            var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var body=board.GetComponent<Rigidbody>();var rider=p.GetComponent<Story1WreckRider>();rider.Release();
            // Fixture-only isolation of an ACTUAL released original hull,
            // with its unchanged mesh, mass, gravity and native buoyancy.
            Vector3 isolated=board.transform.position+Vector3.right*220;isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            body.position=isolated;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;
            await Task.Delay(1600);Physics.SyncTransforms();board.CommitPose();rider.Release();
            p.RestoreSavedPose(board.BoardingPoint(body.worldCenterOfMass)+Vector3.up*(.05f-p.LowestFootWorldOffset),Quaternion.identity);
            rider.Initialize(p,battle.Boards,ocean,board,battle.WreckDetails);await Task.Delay(300);
            Require(rider.Support()==board,"Fixture did not establish actual native deck support");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(Story1WreckBattle).GetMethod("BeginFinale",flags).Invoke(battle,null);
            Require(battle.Finale!=null,"Valid isolated real deck was rejected");
            var film=battle.Finale;
            for(int i=0;i<1200 && film!=null && film.CinematicTime<.2f;i++)await Task.Delay(20);
            Require(film!=null && film.CinematicTime>=.2f && film.CinematicTime<4,"Did not enter supported guard on the original deck");
            Require(film.LargestFootHeightError<=.03f,"Pre-fracture stance was not actually supported");
            Vector3 before=p.transform.position;float health=hp.currentHealth;int count=battle.ShipFragmentCount;
            Require(battle.DamageBoard(board,100,board.BoardingPoint(before),Vector3.zero),"Actual original deck failed to fracture");
            Require(board.IsBroken && battle.ShipFragmentCount>count,"Actual authored original child pieces were not released");
            typeof(Story1WreckFinale).GetMethod("LateUpdate",flags).Invoke(film,null);
            Require(battle.Finale==null && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Broken support retained cinematic ownership");
            float displacement=Vector3.Distance(before,p.transform.position);
            Require(p.enabled && p.GetComponent<CharacterController>().enabled && !p.ExternalControlLock && p.GetComponent<SahurAttack>().enabled,"Broken support retained movement/attack lock");
            Require(displacement<.001f && hp.currentHealth==health && p.Motion.State!=SahurControlState.Unconscious && p.Motion.State!=SahurControlState.Cinematic,"Broken support teleported the actor or invented defeat");
            var result=new{passed=true,fixtureOnlyIsolation=true,nativeOriginalDeck=board.name,nativeOriginalChildFragments=true,
                forcedExternalDamageOnly=true,cinematicTime=film.CinematicTime,supportError=film.LargestFootHeightError,
                releasedControls=true,noRootTeleport=true,displacement,health=hp.currentHealth};
            Directory.CreateDirectory(".codex/encounter-v029");File.WriteAllText(".codex/encounter-v029/broken-film-support.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);}
    }
    public static async Task<object> NoSupport()
    {
        Require(Application.isPlaying,"Native Play mode required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        Story1WreckMeshCollision[] shapes=null;bool[][] enabled=null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!loading.isDone)await Task.Delay(20);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var p=sequence.sahur;
            p.gameObject.AddComponent<GameSaveExcluded>();var hp=p.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;hp.GrantProtection(120);
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"No playable fight");
            shapes=battle.Boards.Select(b=>b.Collision).ToArray();enabled=shapes.Select(s=>s.Parts.Select(c=>c.enabled).ToArray()).ToArray();
            foreach(var shape in shapes)shape.SetEnabled(false);Physics.SyncTransforms();
            Vector3 before=p.transform.position;var bones=p.CharacterAnimator.GetComponentsInChildren<Transform>(true);
            var pose=bones.Select(b=>b.localRotation).ToArray();
            typeof(Story1WreckBattle).GetMethod("BeginFinale",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,null);
            float displacement=Vector3.Distance(before,p.transform.position),poseChange=bones.Select((b,i)=>Quaternion.Angle(b.localRotation,pose[i])).Max();
            Require(battle.Finale==null && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"An unsupported film retained ownership");
            Require(p.enabled && p.GetComponent<CharacterController>().enabled && !p.ExternalControlLock && p.GetComponent<SahurAttack>().enabled,"Unsupported handoff left player controls disabled");
            Require(displacement<.001f && poseChange<.001f,"Rejected planning moved the actor or replaced the retained native pose");
            Require(hp.currentHealth>0 && p.Motion.State!=SahurControlState.Unconscious && p.Motion.State!=SahurControlState.Cinematic,"Unsupported planning invented narrative defeat");
            var result=new{passed=true,faultInjection="All actual native timber collision parts unavailable",noUnsupportedCinematic=true,actualControlsReleased=true,displacement,maximumBoneRotationChange=poseChange,health=hp.currentHealth};
            Directory.CreateDirectory(".codex/encounter-v029");File.WriteAllText(".codex/encounter-v029/no-support-admission.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            if(shapes!=null)for(int i=0;i<shapes.Length;i++)if(shapes[i]!=null)for(int j=0;j<shapes[i].Parts.Length;j++)if(shapes[i].Parts[j]!=null)shapes[i].Parts[j].enabled=enabled[i][j];
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
