using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class VerifyWreckWalkingV055
{
    const string Folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";
    static Task pending;static Keyboard keyboard;static Key[] pressed=new Key[0];
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(pressed));
    }
    static async Task Hold(float seconds,params Key[] keys)
    {
        pressed=keys;float until=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(Math.Max(10,seconds*10));
        while(Time.time<until){if(DateTime.UtcNow>deadline)throw new Exception("Native simulation stopped");await Task.Delay(15);}
    }
    static Vector3 Feet(ThirdPersonPlayerController p)=>p.transform.position+Vector3.up*p.LowestFootWorldOffset;
    static void NoVehicle(ThirdPersonPlayerController p,Story1WreckRider r)
    {
        Require(!r.IsSurfing && r.DrivenBoard==null && !r.IsPaddling,"Wreck vehicle control is still active");
        Require(!p.ExternalMovementLock && p.Motion.State!=SahurControlState.BoardRiding,"Standing on wood claimed vehicle movement");
        Require(!p.CharacterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Wreck Surf"),"Paddle animation is still active");
    }
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        var oldKeyboard=Keyboard.current;var oldCursor=Cursor.lockState;
        var background=InputSystem.settings.backgroundBehavior;var routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            Require(Application.isPlaying,"Play mode required");
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            var timeout=DateTime.UtcNow.AddSeconds(60);while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting){if(DateTime.UtcNow>timeout)throw new Exception("Opening never entered combat");await Task.Delay(20);}
            var p=sequence.sahur;p.gameObject.AddComponent<GameSaveExcluded>();p.GetComponent<PlayerHealth>().GrantProtection(200);battle.WreckContact.enabled=false;
            var rider=p.GetComponent<Story1WreckRider>();var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            var board=battle.Boards.Where(b=>b.IsBoardable && Story1WreckPlank.SurfboardShape(b.Dimensions)).OrderByDescending(b=>Mathf.Max(b.Dimensions.x,b.Dimensions.z)).First();
            var body=board.GetComponent<Rigidbody>();rider.Release();Vector3 isolated=board.transform.position+Vector3.right*220;isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            body.position=isolated;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;await Task.Delay(1500);Physics.SyncTransforms();board.CommitPose();rider.Release();
            Quaternion heading=Quaternion.LookRotation(board.Dimensions.x>board.Dimensions.z ? Vector3.right : Vector3.forward);
            p.RestoreSavedPose(board.BoardingPoint(board.transform.position)+Vector3.up*(.05f-p.LowestFootWorldOffset),heading);rider.Initialize(p,battle.Boards,ocean,board,battle.WreckDetails);
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>("Wreck walking regression v055");InputSystem.onAfterUpdate+=Inject;await Hold(.35f);
            NoVehicle(p,rider);Require(p.OnFloatingWreckDeck && rider.Support()==board,"Standing support was lost");
            Vector3 startLocal=board.transform.InverseTransformPoint(Feet(p)),startBoard=board.transform.position;
            var f=GameInputSettings.Get(GameInputSettings.Action.Forward);var jump=GameInputSettings.Get(GameInputSettings.Action.Jump);
            board.Drive(1,1,p.transform);await Hold(.22f,f);
            float walked=Vector3.ProjectOnPlane(board.transform.TransformVector(board.transform.InverseTransformPoint(Feet(p))-startLocal),Vector3.up).magnitude;
            NoVehicle(p,rider);Require(walked>.25f && p.OnFloatingWreckDeck,"W did not move the actor normally across the deck: "+walked);
            var attack=p.GetComponent<SahurAttack>();attack.TriggerAttack();await Hold(.16f,f);bool mobileAttack=attack.IsGroundComboActive && p.RequestedMoveDirection.sqrMagnitude>.5f;Require(mobileAttack,"Moving ground attack failed on wreckage");NoVehicle(p,rider);await Hold(1.1f);
            float boardDrift=Vector3.ProjectOnPlane(board.transform.position-startBoard,Vector3.up).magnitude;Require(boardDrift<.25f,"Input propelled the isolated floating wood: "+boardDrift);
            float takeoff=Feet(p).y,highest=takeoff;Vector3 inherited=board.Velocity;
            await Hold(.065f,jump);await Hold(.035f);bool released=!p.ExternalMovementLock && p.Motion.State==SahurControlState.Airborne;
            float end=Time.time+.65f;while(Time.time<end){highest=Mathf.Max(highest,Feet(p).y);await Hold(.02f);}
            Require(released && highest-takeoff>.7f,"Normal deck jump failed: "+(highest-takeoff)+" released="+released);await Hold(1.1f);rider.Release();
            Vector3 water=board.transform.position+board.transform.forward*35;water.y=ocean.SampleSurfaceHeight(water,Time.time)-2-p.LowestFootWorldOffset;p.RestoreSavedPose(water,heading);await Hold(.35f);
            Require(p.Swimming,"Ordinary swimming did not start");Vector3 swimStart=p.transform.position;await Hold(.6f,f,GameInputSettings.Get(GameInputSettings.Action.Sprint));
            float swam=Vector3.ProjectOnPlane(p.transform.position-swimStart,Vector3.up).magnitude;Require(p.FastSwimming && swam>2,"Fast swimming failed: "+swam);await Hold(.25f);
            Vector3 edge=board.BoardingPoint(board.transform.position+board.transform.forward*100),outward=board.OpenWaterDirection(edge);
            water=edge+outward*1.1f;water.y=ocean.SampleSurfaceHeight(water,Time.time)-2-p.LowestFootWorldOffset;p.RestoreSavedPose(water,Quaternion.LookRotation(-outward));await Hold(.25f);await Hold(.26f,f);
            for(int i=0;i<12 && !rider.IsClimbing;i++)await Hold(.03f,f);
            Require(rider.IsClimbing,"Swimming towards an actual edge did not start climbing");Vector3 gripStart=board.transform.position;body.AddForce(Vector3.right*1.5f,ForceMode.VelocityChange);await Hold(1.3f);
            Require(!rider.IsClimbing && p.OnFloatingWreckDeck && p.CanUseGroundAttack && rider.Support()==board,"Moving-edge climb did not return to normal walking");NoVehicle(p,rider);
            Require(Vector3.ProjectOnPlane(board.transform.position-gripStart,Vector3.up).magnitude>.1f,"Climb fixture did not move");
            board.MarkBroken();rider.OnBoardBroken(board);await Hold(.2f);Require(!p.OnFloatingWreckDeck && !p.ExternalMovementLock && rider.Support()==null,"Broken timber retained support");
            var result=new{passed=true,vehicleControlRemoved=true,paddlingRemoved=true,walkingAcrossRealDeckMetres=walked,inputBoardDriftMetres=boardDrift,movingGroundAttack=mobileAttack,jumpHeight=highest-takeoff,inheritedBoardSpeed=inherited.magnitude,fastSwimmingDistance=swam,movingEdgeClimbReturnsToWalking=true,brokenWoodReleasesSupport=true,originalWalkRunRollRetained=true,fixture="Isolated original physical hull fragment; no production relocation"};
            File.WriteAllText(Folder+"/wreck-walking-v055.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(Folder+"/wreck-walking-v055-error.txt",ex.ToString());}
        finally
        {
            InputSystem.onAfterUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;pressed=new Key[0];oldKeyboard?.MakeCurrent();Cursor.lockState=oldCursor;
            InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;GameSaveManager.CancelPendingContinue();
        }
    }
}
