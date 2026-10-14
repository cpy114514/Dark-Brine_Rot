using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;
using Mavis;
public static class ProbePaddleV054
{
    static Task pending;
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        const string folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";
        Keyboard keyboard=null;var oldKeyboard=Keyboard.current;var oldCursor=Cursor.lockState;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)await Task.Delay(20);
            battle.WreckContact.enabled=false;var player=sequence.sahur;player.gameObject.AddComponent<GameSaveExcluded>();player.GetComponent<PlayerHealth>().GrantProtection(180);
            var rider=player.GetComponent<Story1WreckRider>();var board=battle.Boards.Where(p=>!p.IsBroken && Story1WreckPlank.SurfboardShape(p.Dimensions)).OrderBy(p=>(p.transform.position-player.transform.position).sqrMagnitude).First();
            var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var body=board.GetComponent<Rigidbody>();
            rider.Release();Vector3 isolated=board.transform.position+Vector3.right*220;isolated.y=ocean.SampleSurfaceHeight(isolated,Time.time)-.15f;
            body.position=isolated;body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;await Task.Delay(1500);Physics.SyncTransforms();board.CommitPose();rider.Release();
            player.RestoreSavedPose(board.BoardingPoint(board.transform.position)+Vector3.up*(.05f-player.LowestFootWorldOffset),Quaternion.identity);
            rider.Initialize(player,battle.Boards,ocean,board,battle.WreckDetails);
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            var movement=player.GetComponent<SahurMovementPose>();var animator=player.CharacterAnimator;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var soles=movement.GetType().GetField("rightSole",flags);var power=board.GetType().GetField("paddlePower",flags);
            var input=InputSystem.AddDevice<Keyboard>("Actual board stroke probe");keyboard=input;var rows=new List<object>();
            float start=Time.time;Vector3 origin=board.transform.position;float until=start+6f;DateTime deadline=DateTime.UtcNow.AddSeconds(30);
            while(Time.time<until)
            {
                if(DateTime.UtcNow>deadline)throw new Exception("Simulation stalled");
                keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Forward)));InputSystem.Update();
                await Task.Delay(15);
                Vector3 foot=animator.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint((Vector3)soles.GetValue(movement));
                rows.Add(new{time=Time.time-start,phase=rider.PaddlePhase,throttle=animator.GetFloat("BoardThrottle"),rider.IsPaddling,
                    movement.PaddleWaterEngagement,paddlePower=power.GetValue(board),edge=board.EdgeDistance(foot),depth=rider.PaddleWaterHeight(foot)-foot.y,
                    speed=board.Speed,wet=board.GetComponent<Story1WreckFloatBody>().InWater,cursor=Cursor.lockState.ToString(),focus=Application.isFocused,
                    surfing=rider.IsSurfing,motion=player.Motion.State.ToString(),state=animator.GetCurrentAnimatorStateInfo(0).fullPathHash,foot=foot.ToString()});
            }
            File.WriteAllText(folder+"/paddle-probe.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{board=board.name,dimensions=board.Dimensions.ToString(),distance=Vector3.Distance(origin,board.transform.position),rows},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(folder+"/paddle-probe-error.txt",ex.ToString());}
        finally{if(keyboard!=null)InputSystem.RemoveDevice(keyboard);oldKeyboard?.MakeCurrent();Cursor.lockState=oldCursor;}
    }
}
