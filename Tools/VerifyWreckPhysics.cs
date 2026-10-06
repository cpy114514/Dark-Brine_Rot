using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyWreckPhysics
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Keyboard keys;
    static async Task Hold(int milliseconds,params Key[] pressed)
    {
        InputSystem.QueueStateEvent(keys,new KeyboardState(pressed));await Task.Delay(milliseconds);
    }
    public static async Task<object> Verify()
    {
        Check(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(40);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<200 && battle.ShipFragmentCount==0;i++)await Task.Delay(20);
            var bodies=UnityEngine.Object.FindObjectsByType<Story1WreckFloatBody>(FindObjectsSortMode.None);
            Check(bodies.Length==202,"Physical fragments are missing.");
            var sizes=bodies.ToDictionary(b=>b,b=>b.transform.localScale);
            var high=bodies.OrderByDescending(b=>b.transform.position.y).First();float highY=high.transform.position.y;
            for(int i=0;i<200 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(40);
            Check(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Ship break did not release controls.");
            var hero=sequence.sahur;hero.GetComponent<PlayerHealth>().GrantProtection(120);
            var testBoard=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None)
                .First(b=>b.Dimensions.y<1 && b.Dimensions.z>8);
            var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            Vector3 testAt=testBoard.transform.position+Vector3.right*160;
            testAt.y=water.SampleSurfaceHeight(testAt,Time.time);
            var testBody=testBoard.GetComponent<Rigidbody>();testBody.position=testAt;
            testBody.rotation=Quaternion.Euler(0,23,90);testBody.linearVelocity=Vector3.down;testBody.angularVelocity=Vector3.zero;
            await Task.Delay(6000);
            float recoveredUp=Mathf.Abs((testBody.rotation*Vector3.up).y);
            Check(recoveredUp>.85f,"An edge-on board remained upright after entering water; up="+recoveredUp);
            Check(highY-high.transform.position.y>8,"High ship fragments did not fall under gravity.");
            foreach(var b in bodies)
            {
                Check(!b.Body.isKinematic && b.Body.useGravity,"A fragment is still moved by a scripted pose.");
                Check(Vector3.Distance(b.transform.localScale,sizes[b])<.001f,"Fragment resized after impact.");
                Check(float.IsFinite(b.Body.position.x),"Invalid physical fragment position.");
            }
            var boards=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None);
            int afloat=boards.Count(b=>b.IsBoardable && b.GetComponent<Story1WreckFloatBody>().InWater);
            Check(afloat>=5,"Too few real fragments remained usable at the surface.");
            // Use the isolated, recovered real fragment for traversal; collisions inside a pile may capsize a board.
            var board=testBoard;
            Vector3 impulseStart=board.transform.position;board.GetComponent<Rigidbody>().AddForce(Vector3.right*5,ForceMode.VelocityChange);
            await Task.Delay(1800);
            float drift=Vector3.ProjectOnPlane(board.transform.position-impulseStart,Vector3.up).magnitude;
            Check(drift>1,"Physical impulse was overridden by a destination pose.");
            var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            keys=InputSystem.AddDevice<Keyboard>("Wreck physics verification keyboard");Cursor.lockState=CursorLockMode.Locked;
            Vector3 waterAt=board.transform.position+board.transform.right*(board.Dimensions.x*.5f+.9f);
            waterAt.y=ocean.SampleSurfaceHeight(waterAt,Time.time)-2-hero.LowestFootWorldOffset;
            hero.RestoreSavedPose(waterAt,Quaternion.LookRotation(-board.transform.right));Physics.SyncTransforms();await Hold(350);
            var rider=hero.GetComponent<Story1WreckRider>();
            Check(hero.Swimming && rider.NearbyBoard!=null,"Physical board cannot be approached from water.");
            for(int tap=0;tap<3 && !rider.IsClimbing;tap++)
            {
                await Hold(100,GameInputSettings.Get(GameInputSettings.Action.Jump));
                if(!rider.IsClimbing)await Hold(120);
            }
            Check(rider.IsClimbing,"Physical board did not allow climbing; hint="+rider.Hint+", cursor="+Cursor.lockState+", near="+rider.NearbyBoard?.name);
            await Hold(100);
            await Hold(1300);
            Check(!rider.IsClimbing && !hero.Swimming && rider.Support()!=null,"Climbing failed to land on the moving fragment.");
            var mounted=rider.Support();
            for(int tap=0;tap<3 && !rider.IsSurfing;tap++)
            {
                await Hold(160,Key.F);await Hold(180);
            }
            Check(rider.IsSurfing,"Physical fragment could not be mounted.");
            Vector3 start=mounted.transform.position;
            await Hold(2000,GameInputSettings.Get(GameInputSettings.Action.Forward));await Hold(200);
            float surf=Vector3.ProjectOnPlane(mounted.transform.position-start,Vector3.up).magnitude;
            Check(surf>2 && rider.IsSurfing,"Surfing did not accelerate the rigidbody; distance="+surf+", mounted="+rider.IsSurfing+", velocity="+mounted.Velocity+", up="+mounted.transform.up);
            GameAudioPolicy.ApplyMasterVolume(1);
            Check(AudioListener.volume==0,"Changing saved volume re-enabled sound effects.");
            Check(hero.CanUseGroundAttack && hero.GetComponent<SahurAttack>().enabled,"Surfing disabled combat.");
            return new{rigidbodyFragments=bodies.Length,originalSizePreserved=true,gravityDrop=highY-high.transform.position.y,
                afloatUsableBoards=afloat,externalImpulseDrift=drift,swimmingClimb=true,surfDistance=surf,
                surfUsesPhysics=true,mountedCombat=true,noTargetLayout=true,edgeOnBoardRecoveredUp=recoveredUp,soundMuted=true};
        }
        finally
        {
            if(keys!=null)InputSystem.RemoveDevice(keys);keys=null;
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
}
