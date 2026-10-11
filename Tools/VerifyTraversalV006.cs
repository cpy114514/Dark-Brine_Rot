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
    {
        keys.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        InputSystem.QueueStateEvent(keys,new KeyboardState(pressed));
        // Native rendering may be slower than wall time. Each control phase
        // must receive its intended simulation duration at any frame rate.
        float until=Time.time+milliseconds*.001f;
        var deadline=DateTime.UtcNow.AddSeconds(Math.Max(5,milliseconds*.01));
        while(Time.time<until)
        {
            if(DateTime.UtcNow>deadline)throw new Exception("Native controls simulation stopped advancing");
            await Task.Delay(15);
        }
    }
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
            // A wet released deck now correctly leaves the actor swimming.
            // The controls fixture still uses a real hull mesh, isolated below,
            // rather than requiring that random starting deck to be mounted.
            var board=rider.DrivenBoard??battle.Boards
                .Where(p=>!p.IsBroken && Story1WreckPlank.SurfboardShape(p.Dimensions))
                .OrderBy(p=>(p.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
            Require(board!=null,"No real boardable hull fragment for the controls fixture");
            rider.Release();var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var body=board.GetComponent<Rigidbody>();
            // Isolate this real random hull mesh for controls testing; never production choreography.
            Vector3 isolated=board.transform.position+Vector3.right*220;isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            body.position=isolated;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;
            await Task.Delay(1500);Physics.SyncTransforms();board.CommitPose();
            rider.Release(); // Settling can otherwise remount the old deck before fixture relocation.
            player.RestoreSavedPose(board.BoardingPoint(isolated)+Vector3.up*(.05f-player.LowestFootWorldOffset),Quaternion.identity);
            rider.Initialize(player,battle.Boards,ocean,board,battle.WreckDetails);
            keys=InputSystem.AddDevice<Keyboard>("Automatic traversal verification");await Hold(250);
            // The reset fixture can still be rising into the next crest;
            // allow native landing/mount updates rather than forcing a claim.
            for(int i=0;i<40 && !rider.IsSurfing;i++)await Hold(30);
            Require(rider.IsSurfing && player.Motion.State==SahurControlState.BoardRiding,"Landing did not automatically mount");
            Key f=GameInputSettings.Get(GameInputSettings.Action.Forward),back=GameInputSettings.Get(GameInputSettings.Action.Back),r=GameInputSettings.Get(GameInputSettings.Action.Right),jump=GameInputSettings.Get(GameInputSettings.Action.Jump);
            Vector3 start=board.transform.position;await Hold(1700,f);
            float driven=Vector3.ProjectOnPlane(board.transform.position-start,Vector3.up).magnitude,speed=board.Speed;
            // A crest can lift the outside water leg above the surface. Allow
            // subsequent genuine strokes, instead of requiring air strokes
            // to accelerate the real rigidbody in a fixed short wall window.
            for(int cycle=0;cycle<3 && (driven<=2 || speed<=1);cycle++)
            {
                await Hold(1200,f);driven=Vector3.ProjectOnPlane(board.transform.position-start,Vector3.up).magnitude;speed=board.Speed;
            }
            Require(driven>2 && speed>1 && rider.IsSurfing,"No physical board acceleration: "+driven+"/"+speed);
            var attack=player.GetComponent<SahurAttack>();attack.TriggerAttack();await Hold(160,f,r);
            Require(attack.IsGroundComboActive && rider.IsSurfing,"Board movement cancelled the light strike");
            float yaw=board.transform.eulerAngles.y;await Hold(550,f,r);
            float steering=Mathf.Abs(Mathf.DeltaAngle(yaw,board.transform.eulerAngles.y));Require(steering>10,"Board did not steer during attack: "+steering);
            await Hold(1100);float afterBrake=board.Speed;Require(afterBrake<speed,"Board did not physically brake");
            await Hold(1500,back);float reverse=Vector3.Dot(board.Velocity,player.transform.forward);
            for(int cycle=0;cycle<3 && reverse>=-.5f;cycle++)
            {
                await Hold(1200,back);reverse=Vector3.Dot(board.Velocity,player.transform.forward);
            }
            Require(reverse<-.5f,"Reverse thrust failed after genuine water strokes: "+reverse);
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
            if(!rider.IsClimbing)
            {
                var feet=Feet(player);var end=board.BoardingPoint(feet);var cc=player.GetComponent<CharacterController>();
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var toward=Vector3.ProjectOnPlane(end-feet,Vector3.up).normalized;
                var shoulder=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var elbow=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                var hand=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
                float armLength=Vector3.Distance(shoulder.position,elbow.position)+Vector3.Distance(elbow.position,hand.position);
                Vector3 actorScale=player.transform.lossyScale;
                float worldRadius=cc.radius*Mathf.Max(Mathf.Abs(actorScale.x),Mathf.Abs(actorScale.z));
                float half=Mathf.Max(0,cc.height*Mathf.Abs(actorScale.y)*.5f-worldRadius);
                Vector3 centre=end-Vector3.up*player.LowestFootWorldOffset+player.transform.rotation*Vector3.Scale(cc.center,actorScale);
                var trueBlocked=Physics.OverlapCapsule(centre-Vector3.up*half,centre+Vector3.up*half,worldRadius*.95f,~0,QueryTriggerInteraction.Ignore)
                    .Where(c=>!c.transform.IsChildOf(player.transform)&&!c.transform.IsChildOf(board.transform)).Select(c=>c.name+" "+c.GetType().Name).ToArray();
                var blocked=Physics.OverlapCapsule(end+Vector3.up*(cc.radius+.08f),end+Vector3.up*(cc.height-cc.radius),cc.radius*.8f,~0,QueryTriggerInteraction.Ignore)
                    .Where(c=>!c.transform.IsChildOf(player.transform)&&!c.transform.IsChildOf(board.transform)).Select(c=>c.name+" "+c.GetType().Name).ToArray();
                var failure=new{edge=board.EdgeDistance(feet),board=board.name,nearby=rider.NearbyBoard?.name,swimming=player.Swimming,
                    intent=typeof(Story1WreckRider).GetField("autoClimbIntent",flags).GetValue(rider),moveDot=Vector3.Dot(player.RequestedMoveDirection,toward),verticalGap=end.y-feet.y,shoulderGap=end.y-shoulder.position.y,armLength,trueBlocked,blocked};
                Directory.CreateDirectory(".codex/encounter-v016");File.WriteAllText(".codex/encounter-v016/traversal-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(failure,Newtonsoft.Json.Formatting.Indented));
                throw new Exception("Continuous swimming did not auto climb: "+Newtonsoft.Json.JsonConvert.SerializeObject(failure));
            }
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
