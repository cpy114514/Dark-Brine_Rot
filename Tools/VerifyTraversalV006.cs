using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class VerifyTraversalV006
{
    static Keyboard keys;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Hold(int milliseconds,params Key[] pressed)
    {keys.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keys,new KeyboardState(pressed));await Task.Delay(milliseconds);}
    static Vector3 Feet(ThirdPersonPlayerController p)=>p.transform.position+Vector3.up*p.LowestFootWorldOffset;
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying,"Play mode required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<300 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(30);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"No playable battle");
            battle.WreckContact.enabled=false;var player=sequence.sahur;var rider=player.GetComponent<Story1WreckRider>();
            var health=player.GetComponent<PlayerHealth>();health.GrantProtection(200);
            var board=rider.DrivenBoard??rider.Support();Require(board!=null,"No real supporting hull fragment");
            rider.Release();var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var body=board.GetComponent<Rigidbody>();
            // Isolate this real random hull mesh for controls testing; never production choreography.
            Vector3 isolated=board.transform.position+Vector3.right*220;isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            body.position=isolated;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;
            await Task.Delay(100);Physics.SyncTransforms();board.CommitPose();
            player.RestoreSavedPose(board.BoardingPoint(isolated)+Vector3.up*(.05f-player.LowestFootWorldOffset),Quaternion.identity);
            rider.Initialize(player,battle.Boards,ocean,board,battle.WreckDetails);
            keys=InputSystem.AddDevice<Keyboard>("Automatic traversal verification");await Hold(250);
            Require(rider.IsSurfing && player.Motion.State==SahurControlState.BoardRiding,"Landing did not automatically mount");
            Key f=GameInputSettings.Get(GameInputSettings.Action.Forward),back=GameInputSettings.Get(GameInputSettings.Action.Back),r=GameInputSettings.Get(GameInputSettings.Action.Right),jump=GameInputSettings.Get(GameInputSettings.Action.Jump);
            Vector3 start=board.transform.position;await Hold(1700,f);
            float driven=Vector3.ProjectOnPlane(board.transform.position-start,Vector3.up).magnitude,speed=board.Speed;
            Require(driven>2 && speed>1 && rider.IsSurfing,"No physical board acceleration: "+driven+"/"+speed);
            var attack=player.GetComponent<SahurAttack>();attack.TriggerAttack();await Hold(160,f,r);
            Require(attack.IsGroundComboActive && rider.IsSurfing,"Board movement cancelled the light strike");
            float yaw=board.transform.eulerAngles.y;await Hold(550,f,r);
            float steering=Mathf.Abs(Mathf.DeltaAngle(yaw,board.transform.eulerAngles.y));Require(steering>10,"Board did not steer during attack: "+steering);
            await Hold(1100);float afterBrake=board.Speed;Require(afterBrake<speed,"Board did not physically brake");
            await Hold(1500,back);float reverse=Vector3.Dot(board.Velocity,board.transform.forward);Require(reverse<-.5f,"Reverse thrust failed: "+reverse);
            float takeoff=Feet(player).y,highest=takeoff;Vector3 inherited=board.Velocity;
            await Hold(65,jump);await Hold(35);bool leftBoard=!rider.IsSurfing && !player.ExternalMovementLock;
            for(int i=0;i<16;i++){highest=Mathf.Max(highest,Feet(player).y);await Task.Delay(25);}
            Require(leftBoard && highest-takeoff>.7f,"Board jump failed: "+(highest-takeoff));
            await Hold(1100);rider.Release();
            Vector3 water=board.transform.position+board.transform.forward*35;water.y=ocean.SampleSurfaceHeight(water,Time.time)-2-player.LowestFootWorldOffset;
            player.RestoreSavedPose(water,Quaternion.identity);await Hold(350);
            Require(player.Swimming,"Did not enter ordinary swimming");Vector3 swimStart=player.transform.position;
            await Hold(600,f,GameInputSettings.Get(GameInputSettings.Action.Sprint));
            bool fast=player.FastSwimming;float swam=Vector3.ProjectOnPlane(player.transform.position-swimStart,Vector3.up).magnitude;
            Require(fast && swam>2 && player.CharacterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Swimming"),"Fast continuous swimming failed");await Hold(250);
            Vector3 edge=board.BoardingPoint(board.transform.position+board.transform.forward*100),outward=board.OpenWaterDirection(edge);
            water=edge+outward*1.1f;water.y=ocean.SampleSurfaceHeight(water,Time.time)-2-player.LowestFootWorldOffset;
            player.RestoreSavedPose(water,Quaternion.LookRotation(-outward));await Hold(250);
            Require(player.Swimming,"Climb fixture not swimming: "+Feet(player));
            await Hold(260,f);
            // Input is first observed on the next native frame. Capture/low-FPS
            // tests must wait for the same .2s simulation intent, not wall time.
            for(int i=0;i<12 && !rider.IsClimbing;i++)await Hold(30,f);
            Require(rider.IsClimbing,"Continuous swimming did not auto climb: edge="+board.EdgeDistance(Feet(player))+", nearby="+rider.NearbyBoard?.name+", board="+board.name+", hint="+rider.Hint);
            Vector3 gripStart=board.transform.position;body.AddForce(Vector3.right*1.5f,ForceMode.VelocityChange);await Hold(1300);
            Require(!rider.IsClimbing && rider.IsSurfing && rider.DrivenBoard==board && player.CanUseGroundAttack,"Moving-edge climb did not restore automatic board control");
            Require(Vector3.ProjectOnPlane(board.transform.position-gripStart,Vector3.up).magnitude>.1f,"Climb fixture board did not move");
            rider.OnBoardBroken(board);Require(!rider.IsSurfing && !player.ExternalMovementLock,"Broken support retained its movement lock");
            var result=new{automaticBoarding=true,drivenDistance=driven,boardSpeed=speed,steering,brakeSpeed=afterBrake,reverseSpeed=reverse,jumpHeight=highest-takeoff,inheritedBoardSpeed=inherited.magnitude,
                fastSwimmingDistance=swam,continuousSwim=true,movingBoardAutoClimb=true,mobileBoardAttack=true,brokenBoardReleasesSupport=true};
            Directory.CreateDirectory(".codex/encounter-v006");File.WriteAllText(".codex/encounter-v006/traversal-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            if(keys!=null)InputSystem.RemoveDevice(keys);keys=null;
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
