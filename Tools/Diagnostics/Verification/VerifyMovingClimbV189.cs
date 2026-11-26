using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyMovingClimbV189
{
    static Keyboard keys;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Hold(float seconds,params Key[] held)
    {
        keys.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        InputSystem.QueueStateEvent(keys,new KeyboardState(held));
        float until=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(20);
        while(Time.time<until){if(DateTime.UtcNow>deadline)throw new Exception("Simulation stalled");await Task.Delay(15);}
    }
    static Vector3 Feet(ThirdPersonPlayerController p)=>p.transform.position+Vector3.up*p.LowestFootWorldOffset;
    public static async Task<object> Run()
    {
        Require(Application.isPlaying,"Existing Editor Play Mode required");
        Require(UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()==null,"Do not overlap the opening review");
        const string folder=".codex/animation-continuation-20261014/";
        int oldCapture=Time.captureFramerate;Time.captureFramerate=30;
        keys=InputSystem.AddDevice<Keyboard>("Moving timber climb diagnostic");
        ThirdPersonPlayerController player=null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            player=sequence.sahur;player.gameObject.AddComponent<GameSaveExcluded>();
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=300;
            player.GetComponent<PlayerHealth>().GrantProtection(300);
            sequence.BeginImpact();
            var deadline=DateTime.UtcNow.AddSeconds(40);
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)
            {if(DateTime.UtcNow>deadline)throw new Exception("Real ship did not release into combat");await Task.Delay(30);}
            battle.WreckContact.enabled=false;
            var rider=player.GetComponent<Story1WreckRider>();var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            var board=battle.Boards.Where(p=>p.IsBoardable && p.Dimensions.y<1.2f &&
                Mathf.Min(p.Dimensions.x,p.Dimensions.z)>3 && p.FractureSource!=null && p.FractureSource.splinters.Length>1)
                .OrderBy(p=>(p.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
            Require(board!=null,"No real thin original hull available");
            rider.Release();var body=board.GetComponent<Rigidbody>();
            // Isolated physical fixture only. Production never places timber.
            Vector3 isolated=board.transform.position+Vector3.right*220;
            isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            body.position=isolated;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;
            await Hold(2);Physics.SyncTransforms();board.CommitPose();
            rider.Release();rider.Initialize(player,battle.Boards,ocean,null,battle.WreckDetails);
            Key forward=GameInputSettings.Get(GameInputSettings.Action.Forward),sprint=GameInputSettings.Get(GameInputSettings.Action.Sprint);
            Vector3 water=body.position+Vector3.forward*35;
            water.y=ocean.SampleSurfaceHeight(water,Time.time)-2-player.LowestFootWorldOffset;
            player.RestoreSavedPose(water,Quaternion.identity);await Hold(.35f);
            Require(player.Swimming,"Water fixture did not enter swimming");
            Vector3 swimStart=player.transform.position;await Hold(.6f,forward,sprint);
            float swimDistance=Vector3.ProjectOnPlane(player.transform.position-swimStart,Vector3.up).magnitude;
            Require(player.FastSwimming && swimDistance>2,"Fast swimming did not continue");await Hold(.2f);
            Vector3 edge=board.BoardingPoint(body.worldCenterOfMass+Vector3.forward*100),outward=board.OpenWaterDirection(edge);
            water=edge+outward*1.1f;water.y=ocean.SampleSurfaceHeight(water,Time.time)-2-player.LowestFootWorldOffset;
            player.RestoreSavedPose(water,Quaternion.LookRotation(-outward));await Hold(.25f);
            Require(player.Swimming,"Edge fixture is not swimming");
            float intentionStart=Time.time;
            for(int i=0;i<30 && !rider.IsClimbing;i++)await Hold(.0334f,forward);
            if(!rider.IsClimbing)
                throw new Exception("Held swim did not climb: edge="+board.EdgeDistance(Feet(player))+" nearby="+rider.NearbyBoard?.name+" state="+player.Motion.State+" root="+player.transform.position);
            float intentSeconds=Time.time-intentionStart;
            Require(!rider.IsSurfing && rider.DrivenBoard==null,"Retired vehicle control reappeared");
            Vector3 start=body.position;body.AddForce(Vector3.right*1.5f,ForceMode.VelocityChange);
            await Hold(1.4f);
            float boardTravel=Vector3.ProjectOnPlane(body.position-start,Vector3.up).magnitude;
            Require(boardTravel>.1f,"Fixture did not physically move");
            Require(!rider.IsClimbing && rider.Support()==board && !player.ExternalMovementLock && player.CanUseGroundAttack,
                "Moving-edge climb did not restore supported walking: "+player.Motion.State+" support="+rider.Support()?.name);
            var attack=player.GetComponent<SahurAttack>();attack.TriggerAttack();Vector3 attackStart=player.transform.position;await Hold(.2f,forward);
            Require(attack.IsGroundComboActive && Vector3.ProjectOnPlane(player.transform.position-attackStart,Vector3.up).magnitude>.1f,
                "Ordinary attack blocked supported walking");
            await Hold(.8f);
            bool broken=battle.DamageBoard(board,10000,Feet(player),Vector3.zero);await Hold(.1f);
            Require(broken && board.IsBroken && rider.Support()!=board && !player.ExternalMovementLock,"Actual fracture retained support/lock");
            var result=new{passed=true,native=true,fixtureUsesReleasedOriginalHull=true,fixtureIsolatedOnly=true,
                board=board.name,dimensions=board.Dimensions.ToString(),fastSwimmingDistance=swimDistance,
                intentSeconds,movingBoardTravel=boardTravel,automaticMovingEdgeClimb=true,movingOrdinaryAttack=true,
                actualFractureReleasesSupport=true,surfing=false,standalonePerformanceEvidence=false};
            File.WriteAllText(folder+"moving-climb-v189.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        catch(Exception ex){File.WriteAllText(folder+"moving-climb-v189-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,error=ex.ToString()},Newtonsoft.Json.Formatting.Indented));throw;}
        finally
        {
            if(keys!=null)InputSystem.RemoveDevice(keys);keys=null;Time.captureFramerate=oldCapture;GameSaveManager.CancelPendingContinue();
            // Human save is restored AFTER confirmed Editor stop by the caller.
        }
    }
}
